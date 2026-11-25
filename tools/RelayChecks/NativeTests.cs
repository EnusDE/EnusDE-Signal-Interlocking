using System;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json.Linq;
using EnusDE.Signaltechnik;

public static class NativeTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    public static void Run(string root) {
        string temporary=Path.Combine(root,"build","native-tests-"+Guid.NewGuid().ToString("N"));
        string session=Path.Combine(temporary,"sessions","test");
        Directory.CreateDirectory(temporary);
        var writer=new TelemetryWriter(session,new {schema=1,start_utc=DateTime.UtcNow.ToString("o"),role="Client",map="test"},1024,2048);
        for(int i=0;i<500;i++)CheckEnqueue(writer.Enqueue(new {seq=i,kind="test",data=new {text="Umlaut ä",i=i}}));
        writer.Dispose();
        var files=Directory.GetFiles(session,"events-*.jsonl").OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        var rows=files.SelectMany(File.ReadAllLines).Select(JObject.Parse).ToArray();
        Check(writer.LastError==null && writer.Written==500 && writer.Dropped==0 && rows.Length==500,"background writer preserves 500 queued records and drains on shutdown");
        Check(files.Length>1 && rows.Select((x,i)=>(int)x["seq"]==i).All(x=>x),"rotation preserves event order across files");
        Check(rows[0]["data"]["text"].ToString()=="Umlaut ä","diagnostic files preserve UTF-8 text");
        Check(!writer.Enqueue(new {seq=501}),"closed recorder rejects further entries without throwing");

        var tracker=new ChangeTracker();
        Check(tracker.Changed("signal",new {aspect="Stop"}) && !tracker.Changed("signal",new {aspect="Stop"}) && tracker.Changed("signal",new {aspect="Clear"}),"change tracker records state transitions and suppresses unchanged states");
        tracker.Clear();Check(tracker.Changed("signal",new {aspect="Clear"}),"new session gets a fresh infrastructure baseline");
        var frames=new FrameStats();frames.Add(1d/60);frames.Add(.2);frames.Add(double.NaN);
        var performance=JObject.FromObject(frames.Take());
        Check((int)performance["frames"]==2 && (int)performance["frames_over_100ms"]==1 && Math.Abs((double)performance["worst_frame_ms"]-200)<.01,"performance aggregation records long frames and rejects invalid durations");

        string blocked=Path.Combine(temporary,"not-a-directory");File.WriteAllText(blocked,"x");
        bool rejected=false;try{new TelemetryWriter(blocked,new {});}catch(IOException){rejected=true;}
        Check(rejected,"unwritable output is reported before starting a background writer");

        string analysisSession=Path.Combine(temporary,"analysis-data","session-1");Directory.CreateDirectory(analysisSession);
        File.WriteAllText(Path.Combine(analysisSession,"session.json"),"{\"start_utc\":\"2026-11-07T14:00:00Z\",\"role\":\"Client\",\"map\":\"test\"}");
        File.WriteAllLines(Path.Combine(analysisSession,"events-0001.jsonl"),new[]{
            "{\"kind\":\"vehicles\",\"real_seconds\":1,\"utc\":\"t1\",\"data\":{\"full\":false,\"cars\":[{\"id\":\"L1\",\"locomotive\":true,\"kmh\":30}]}}",
            "{\"kind\":\"vehicles\",\"real_seconds\":2,\"utc\":\"t2\",\"data\":{\"full\":false,\"cars\":[{\"id\":\"L1\",\"locomotive\":true,\"kmh\":15}]}}",
            "{\"kind\":\"infrastructure\",\"data\":{\"snapshot\":true,\"state\":{\"kind\":\"signal\",\"id\":\"S1\",\"aspect\":\"Stop\"}}}",
            "{\"kind\":\"infrastructure\",\"data\":{\"snapshot\":false,\"state\":{\"kind\":\"signal\",\"id\":\"S1\",\"aspect\":\"Clear\"}}}",
            "{\"kind\":\"session_end\",\"data\":{\"dropped\":2}}",
            "{\"unfinished\":"
        });
        var summary=DiagnosticsReport.ReadSession(analysisSession);
        Check(summary.cars.Count==1 && summary.locos.Count==1 && summary.signal_changes==1 && summary.speed_drops.Count==1 && summary.invalid==1 && summary.dropped==2 && summary.ended,"analysis distinguishes baselines, real changes, braking, dropped data and partial final lines");
        File.AppendAllText(Path.Combine(analysisSession,"events-0001.jsonl"),"\n{\"kind\":\"performance\",\"data\":{\"frames\":426,\"mean_fps\":41.669353868096266,\"worst_frame_ms\":586.78728342056274,\"frames_over_100ms\":6}}\n");
        var oldCulture=System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("de-DE");
            var localized=DiagnosticsReport.ReadSession(analysisSession);
            Check(localized.mean_fps>41.66&&localized.mean_fps<41.68&&localized.worst_frame_ms>586.78,"decimal telemetry stays correct under German number formatting");
        }finally{System.Globalization.CultureInfo.CurrentCulture=oldCulture;}
        string report=Path.Combine(temporary,"analysis-report");DiagnosticsReport.Run(Path.GetDirectoryName(analysisSession),report);
        string cache=File.ReadAllText(Path.Combine(report,"analysis-cache.json"));
        DiagnosticsReport.Run(Path.GetDirectoryName(analysisSession),report);
        Check(File.ReadAllText(Path.Combine(report,"analysis-cache.json"))==cache,"unchanged sessions reuse the cached analysis");
        File.AppendAllText(Path.Combine(analysisSession,"events-0001.jsonl"),"\n{\"kind\":\"collector_error\",\"data\":{\"message\":\"changed\"}}\n");
        DiagnosticsReport.Run(Path.GetDirectoryName(analysisSession),report);
        Check(File.ReadAllText(Path.Combine(report,"Signaltechnik-Diagnose.md")).Contains("changed"),"new diagnostic data refreshes the report automatically");
        CheckReadOnlyAdapter();
        string repeatSession=Path.Combine(temporary,"repeated-states");Directory.CreateDirectory(repeatSession);
        File.WriteAllText(Path.Combine(repeatSession,"session.json"),"{\"version\":\"0.1.3\"}");
        var repeatRows=new List<string>{
            "{\"kind\":\"infrastructure\",\"data\":{\"snapshot\":true,\"key\":\"blocks:bk-w\",\"state\":{\"kind\":\"block\",\"id\":\"bk-w\",\"occupied\":false}}}",
            "{\"kind\":\"infrastructure\",\"data\":{\"snapshot\":true,\"key\":\"blocks:bk-w\",\"state\":{\"kind\":\"interlocking\",\"id\":\"bk-w\",\"direction\":\"None\"}}}"
        };
        for(int i=0;i<20;i++)repeatRows.Add("{\"kind\":\"infrastructure\",\"data\":{\"snapshot\":false,\"key\":\"blocks:bk-w\",\"state\":{\"kind\":\"block\",\"id\":\"bk-w\",\"occupied\":false}}}");
        repeatRows.Add("{\"kind\":\"performance\",\"data\":{\"managed_bytes\":1048576,\"process_private_bytes\":10485760,\"window_builds\":1}}");
        repeatRows.Add("{\"kind\":\"performance\",\"data\":{\"managed_bytes\":2097152,\"process_private_bytes\":12582912,\"window_builds\":1}}");
        File.WriteAllLines(Path.Combine(repeatSession,"events-0001.jsonl"),repeatRows);
        var repeated=DiagnosticsReport.ReadSession(repeatSession);
        Check(repeated.block_changes==0&&repeated.repeated_infrastructure==20,"legacy same-ID block/interlocking spam is not misreported as real occupancy changes");
        Check(repeated.memory_samples==2&&repeated.managed_first==1048576&&repeated.managed_peak==2097152&&repeated.private_last==12582912&&repeated.window_builds_peak==1,"memory history and stable window-build count are summarized without attributing whole-process usage to this mod");
        SettingsChecks.Run(root);
        MarkerTests.Run(root);
        InterlockingTests.Run(root);
        InterlockingTests.Units(root);
        MastTests.Run(root);
        PanelTests.Run(root);
        RouteExtensionTests.Run(root);
        StagingTests.Run(root);
        DeskTests.Run(root);
        EditorTests.Run(root);
        SharedFeatureTests.Run(root);
        BackupTests.Run(root);
        TransferTests.Run();
        EditorPerformanceTests.Run(root);
        ApproachReleaseTests.Run();
        ForecastTests.Run();
        FlankTests.Run();WorkspaceTests.Run();ConstructionModeTests.Run();NativeMaterialApiTests.Run(root);NativeImportTests.Run();WaypointApproachTests.Run();WaypointRouteTests.Run();WaypointAdmissionTests.Run();RouteStyleMovementTests.Run();InstallationProblemTests.Run();SignalGlowTests.Run();
        PracticalSetupTests.Run(root);
        BlockUpdateTests.Run(root);
        GfmRepairTests.Run();
        LineTargetTests.Run();
        LineNumberOverviewTests.Run();
        EditorCanvasTests.Run(root);
        Game063Tests.Run();
        PresentationPackTests.Run(root);
        SignalLampTestTests.Run();
        ConstructionSnapshotTests.Run();
        NetworkTests.Run(Path.Combine(Path.GetDirectoryName(typeof(Model.Car).Assembly.Location),"..","StreamingAssets","AssetPacksCore","nc-bryson-terrain","Data","Map.json"));
        LineTutorialTests.Run();
        DiagramFieldSetupTests.Run();
        LineRemovalTests.Run();
        SetupAssistantTests.Run();
        SignalApiTests.Run();
        FixRegressionTests.Run(root);
    }
    static void CheckEnqueue(bool ok){if(!ok)throw new Exception("test queue unexpectedly full");}
    static void CheckReadOnlyAdapter() {
        var table=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(o=>(ushort)o.Value);
        var adapter=typeof(GameReadAdapter);
        foreach(var type in adapter.Assembly.GetTypes().Where(t=>t==adapter || t.DeclaringType==adapter))foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly)) {
            var body=method.GetMethodBody();if(body==null)continue;var il=body.GetILAsByteArray();int i=0;
            while(i<il.Length) {
                ushort code=il[i++];if(code==0xfe)code=(ushort)(0xfe00|il[i++]);var op=table[code];
                if(op.OperandType==OperandType.InlineMethod) {
                    int token=BitConverter.ToInt32(il,i);
                    try {
                        var called=method.Module.ResolveMethod(token,type.IsGenericType?type.GetGenericArguments():null,method.IsGenericMethod?method.GetGenericArguments():null);
                        if(called.DeclaringType.Assembly.GetName().Name=="Railroader") {
                            string name=called.Name;
                            if(name.StartsWith("set_") || new[]{"Set","Send","Apply","Handle","Request","Place","Move","Remove","Code","Clear"}.Any(p=>name.StartsWith(p,StringComparison.Ordinal)))
                                throw new Exception("Game write API found in read adapter: "+called);
                        }
                    }catch(ArgumentException){}
                }
                switch(op.OperandType) {
                    case OperandType.InlineNone:break;
                    case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:i++;break;
                    case OperandType.InlineVar:i+=2;break;
                    case OperandType.InlineI8:case OperandType.InlineR:i+=8;break;
                    case OperandType.InlineSwitch:int count=BitConverter.ToInt32(il,i);i+=4+4*count;break;
                    default:i+=4;break;
                }
            }
        }
        Check(true,"compiled game-read adapter contains no calls to world-write methods");
    }
}
