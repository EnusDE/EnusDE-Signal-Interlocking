using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using EnusDE.Signaltechnik;

public static class SettingsChecks {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    sealed class Source : IDiagnosticSource {
        public readonly Dictionary<string,int> Calls=new Dictionary<string,int>(),Discoveries=new Dictionary<string,int>();
        public string Allowed="signals";
        public bool Available=true;
        public string Aspect="Stop";
        public bool Empty;
        public ReadBatch Override;
        public ReadBatch Read(string key,DiagnosticSettings settings,bool discover) {
            if(key!=Allowed)throw new Exception("Unexpected read of "+key);
            Calls[key]=Calls.ContainsKey(key)?Calls[key]+1:1;
            if(discover)Discoveries[key]=Discoveries.ContainsKey(key)?Discoveries[key]+1:1;
            if(!Available)return ReadBatch.Unavailable("fixture absent");
            if(Override!=null)return Override;
            var batch=new ReadBatch();
            if(!Empty){batch.Rows.Add(new Observation("S1",new {kind="signal",id="S1",aspect=Aspect}));batch.Rows.Add(new Observation("S2",new {kind="signal",id="S2",aspect=Aspect}));}
            return batch;
        }
        public void Reset(){}
    }
    public static void Run(string root) {
        var game=Assembly.Load("Railroader");
        var graph=game.GetType("Track.Graph",true);
        Check(graph.GetMethod("TryGetLocationFromWorldPoint")!=null&&graph.GetMethod("GetPositionRotation")!=null&&game.GetType("Helpers.WorldTransformerExtensions").GetMethod("GameToWorld")!=null,"installed game exposes track projection, spline pose and floating-origin coordinate conversion for the P0 marker");
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var nativeSignal=game.GetType("Track.Signals.CTCSignal",true);
        foreach(string name in new[]{"OnEnable","SetNeedsUpdate","ShowAspect","AspectDisplayedBySignal","get_LastShownAspect","get_IsCTC","get_DisplayName"})if(nativeSignal.GetMethod(name,flags)==null)throw new Exception("Missing native signal bridge target: "+name);
        var updateTargets=game.GetType("Model.AI.AutoEngineerPlanner",true).GetMethod("UpdateTargets",flags);
        var targetIl=updateTargets.GetMethodBody().GetILAsByteArray();bool readsLast=false;
        for(int offset=0;offset<targetIl.Length-4;offset++)if(targetIl[offset]==0x28||targetIl[offset]==0x6f)try{var called=updateTargets.Module.ResolveMethod(BitConverter.ToInt32(targetIl,offset+1));if(called.DeclaringType==nativeSignal&&called.Name=="get_LastShownAspect")readsLast=true;}catch{}
        Check(readsLast,"actual installed AutoEngineerPlanner.UpdateTargets reads LastShownAspect and that accessor is included in the proxy bridge");
        Check(nativeSignal.GetField("<LastShownAspect>k__BackingField",flags)?.FieldType==game.GetType("Track.Signals.SignalAspect"),"installed native signal backing state is available for publishing only own proxy aspects");
        var named=game.GetType("Track.NamedLocationExtensions",true).GetMethod("GetSignal");
        var markerIl=named.GetMethodBody().GetILAsByteArray();
        Check(markerIl[0]==0x02&&markerIl[1]==0x17&&nativeSignal.IsAssignableFrom(typeof(RuntimeSignalProxy)),"native signal marker tag is exactly 1 and own proxy inherits the game's actual CTCSignal type");
        Check(game.GetType("TrainController").GetMethod("TrySetSwitch",flags)!=null&&game.GetType("Track.TrackNode").GetMethod("set_isThrown",flags)!=null&&game.GetType("UI.TopRightArea").GetField("timetableButton",flags)!=null,"installed game exposes native checked switch setting, central setter guard and actual clock-toolbar anchor");
        var preferences=game.GetType("UI.PreferencesWindow.PreferencesBuilder",true);
        var tabs=game.GetType("UI.Builder.UITabbedPanelBuilder",true);
        Check(preferences.GetMethod("BuildTabs",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static,null,new[]{tabs},null)!=null,"real installed game contains the exact Mods-tab integration target");
        var signController=game.GetType("Game.Mow.SignController",true);
        var tooltip=SignTooltipHooks.FindTarget();
        Check(tooltip!=null&&tooltip.IsStatic&&tooltip.ReturnType.FullName=="TooltipInfo","optional tooltip resolver finds the actual static PlayerSignTooltip method which failed in 0.2.0");
        foreach(string method in new[]{"HandleAddSign","HandleAddPlayerSign","HandleRemoveSign"}) {
            var target=signController.GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance);
            if(target==null||target.GetParameters()[0].ParameterType!=typeof(string))throw new Exception("Unsupported sign observation target: "+method);
        }
        Check(signController.GetField("_instances",BindingFlags.Instance|BindingFlags.NonPublic)!=null,"real installed game contains the sign inventory and observed handler signatures");
        string path=Path.Combine(root,"build","settings-tests-"+Guid.NewGuid().ToString("N"),"settings.json");
        var store=new SettingsStore(path);var config=store.Load();
        Check(config.Recording&&!config["signs"].Enabled&&config["vehicles"].IntervalSeconds>=2,"default selection avoids sign hooks and maximum-rate recording");
        config["vehicles"].Ids=" test/Ä ; test/B, test/Ä ";config["vehicles"].Visible=false;config["signs"].Enabled=true;config["signals"].IntervalSeconds=5;
        config.Markers.Lateral=-1.8;config.Markers.Visible=false;
        store.Save(config);var loaded=store.Load();
        Check(loaded["vehicles"].Ids=="test/Ä,test/B"&&!loaded["vehicles"].Visible&&loaded["signs"].Enabled&&loaded["signals"].IntervalSeconds==5,"settings survive atomic save/load with Unicode IDs and independent display/capture selection");
        Check(!loaded.Markers.Visible&&loaded.Markers.Lateral==-1.8,"placement display and default lateral side persist independently of diagnostic capture");
        var clone=loaded.Clone();clone["vehicles"].Enabled=false;
        Check(loaded["vehicles"].Enabled,"editing a draft does not mutate the applied settings");
        clone["vehicles"].IntervalSeconds=double.NaN;clone["signals"].IntervalSeconds=-1;clone["performance"].IntervalSeconds=1;clone.Normalize();
        Check(clone["vehicles"].IntervalSeconds==2&&clone["signals"].IntervalSeconds==1&&clone["performance"].IntervalSeconds==5,"invalid intervals normalize to bounded safe values");
        var filter=new IdFilter(" L1; L2, L1 ");
        Check(filter.Allows("L1")&&!filter.Allows("L10")&&!filter.Allows("l1")&&new IdFilter("").Allows("anything"),"ID filters use exact identifiers and an explicit unrestricted empty selection");
        File.WriteAllText(path,"{");var damaged=store.Load();
        Check(!damaged.Recording&&store.Error!=null&&File.ReadAllText(path)=="{","damaged settings pause automatic recording and preserve the original file");
        store.Save(loaded);Check(store.Error==null&&store.Load()["signs"].Enabled,"saving valid settings recovers from a damaged file");

        var source=new Source();var events=new List<JObject>();
        Action<string,object> emit=(kind,data)=>events.Add(JObject.FromObject(new {kind=kind,data=data}));
        config=new DiagnosticSettings();foreach(string key in DiagnosticSettings.Categories)config[key].Enabled=false;
        var sampler=new DiagnosticSampler(config);sampler.Tick(0,source,emit);sampler.Tick(60,source,emit);
        Check(source.Calls.Count==0&&events.Count==0,"disabled categories perform zero reads, discoveries and data emission");
        config["signals"].Enabled=true;config["signals"].Visible=false;config["signals"].IntervalSeconds=5;config["signals"].Ids="S1";
        sampler.Configure(config);sampler.Tick(0,source,emit);sampler.Tick(1,source,emit);sampler.Tick(4.9,source,emit);sampler.Tick(5,source,emit);
        Check(source.Calls["signals"]==2&&source.Discoveries["signals"]==1&&sampler.Counts["signals"]==1,"hidden enabled category records only selected IDs at its configured interval with cached discovery");
        Check(events.Count(e=>(string)e["kind"]=="infrastructure")==1&&(bool)events.First(e=>(string)e["kind"]=="infrastructure")["data"]["snapshot"],"first observation is a baseline and unchanged states do not produce repeated transitions");
        source.Aspect="Clear";sampler.Tick(10,source,emit);
        Check(!(bool)events.Last(e=>(string)e["kind"]=="infrastructure")["data"]["snapshot"],"changed signal state is distinct from the baseline");
        source.Available=false;sampler.Tick(15,source,emit);
        Check(sampler.Counts["signals"]==0&&sampler.Views["signals"].Contains("Nicht verfügbar"),"unavailable data clears counts and is explicitly marked");
        source.Available=true;sampler.Tick(20,source,emit);
        Check((bool)events.Last(e=>(string)e["kind"]=="infrastructure")["data"]["snapshot"],"recovered source produces a new baseline rather than an invented transition across a data gap");
        source.Empty=true;sampler.Tick(25,source,emit);
        Check(events.Any(e=>(string)e["kind"]=="object_removed")&&sampler.Views["signals"].Contains("Keine Objekte"),"observed removal and an empty filtered inventory are distinct from unavailable data");
        int calls=source.Calls["signals"];config["signals"].Enabled=false;sampler.Configure(config);sampler.Tick(30,source,emit);
        Check(source.Calls["signals"]==calls&&sampler.Views["signals"]=="Erfassung aus","switching capture off immediately stops source work and clears old display state");
        config["signals"].Enabled=true;config.Recording=false;sampler.Configure(config);sampler.Tick(40,source,emit);
        Check(source.Calls["signals"]==calls,"global pause prevents all category reads");

        string session=Path.Combine(Path.GetDirectoryName(path),"report-session");
        config.Recording=true;config["vehicles"].Enabled=false;config["performance"].Enabled=false;config["signs"].Enabled=true;
        using(var writer=new TelemetryWriter(session,new {schema=2,start_utc="2026-11-08T00:00:00Z",configuration=config})) {
            writer.Enqueue(new {kind="configuration",utc="2026-11-08T00:00:01Z",data=config});
            writer.Enqueue(new {kind="category_status",data=new {category="signs",available=false,reason="fixture unavailable"}});
            writer.Enqueue(new {kind="sign_handler",utc="2026-11-08T00:00:02Z",data=new {id="sign/1",handler="HandleAddPlayerSign",is_train_trigger=false}});
            writer.Enqueue(new {kind="object_removed",data=new {category="signs",id="sign/2"}});
            writer.Enqueue(new {kind="recorder_health",data=new {suppressed=3,dropped=0}});
            writer.Enqueue(new {kind="session_end",data=new {suppressed=3,dropped=0}});
        }
        var summary=DiagnosticsReport.ReadSession(session);
        Check(summary.latest_configuration!=null&&summary.category_status["vehicles"]=="Erfassung aus"&&summary.category_status["signs"].Contains("Nicht verfügbar")&&summary.sign_handlers==1&&summary.removed_objects==1&&summary.suppressed==3,"analysis preserves selected configuration, unavailable categories and bounded event losses without inventing train triggers");
        string report=Path.Combine(Path.GetDirectoryName(path),"report");DiagnosticsReport.Run(Path.GetDirectoryName(session),report);
        string markdown=File.ReadAllText(Path.Combine(report,"Signaltechnik-Diagnose.md"));
        Check(markdown.Contains("Keine Leistungswerte erfasst")&&markdown.Contains("Verwaltungsrückrufe sind keine bestätigten")&&markdown.Contains("Erfassung aus"),"report explains disabled measurements and callback limits instead of reporting false zero FPS or train reactions");

        config=new DiagnosticSettings();foreach(string key in DiagnosticSettings.Categories)config[key].Enabled=false;
        config["blocks"].Enabled=true;
        source=new Source{Allowed="blocks",Override=new ReadBatch()};
        source.Override.Rows.Add(new Observation("bk-w",new {kind="block",id="bk-w",occupied=false}));
        source.Override.Rows.Add(new Observation(GameReadAdapter.InterlockingIdentity("bk-w"),new {kind="interlocking",id="bk-w",direction="None"},"bk-w"));
        sampler=new DiagnosticSampler(config);events.Clear();
        for(int i=0;i<1000;i++)sampler.Tick(i*2,source,emit);
        Check(events.Count(e=>(string)e["kind"]=="infrastructure")==2,"1000 polls of a block and interlocking with the same game ID emit only their two distinct baselines");
        source.Override.Rows[1].State=new {kind="interlocking",id="bk-w",direction="Left"};sampler.Tick(2000,source,emit);
        Check(events.Count(e=>(string)e["kind"]=="infrastructure")==3&&events.Last(e=>(string)e["kind"]=="infrastructure")["data"]["key"].ToString().Contains("interlocking:"),"an interlocking change cannot overwrite its same-ID block state");
        var cache=new LiveViewCache();cache.Replace(new Dictionary<string,string>{{"value","old"}});
        long retainedStart=GC.GetTotalMemory(true);
        for(int i=0;i<10000;i++)cache.Replace(new Dictionary<string,string>{{"value",i.ToString()}});
        Check(cache.Count==1&&cache.Get("value")=="9999"&&GC.GetTotalMemory(true)-retainedStart<1024*1024,"10000 live-value replacements retain only the current bounded snapshot");
        var budget=new PendingTextBudget(1024);
        Check(budget.Reserve(800)&&!budget.Reserve(300)&&budget.Used==800,"writer character budget rejects overflowing entries independently of record count");
        budget.Release(800);Check(budget.Used==0&&budget.Reserve(1024)&&!budget.Reserve(1),"writer budget is reusable and never accepts more than its configured capacity");budget.Release(1024);
        using(var writer=new TelemetryWriter(Path.Combine(Path.GetDirectoryName(path),"oversize"),new {})) {
            Check(!writer.Enqueue(new {text=new string('x',300*1024)})&&writer.Dropped==1&&writer.PendingCharacters==0,"oversized records are counted and rejected without occupying the write queue");
        }
        var memory=MemorySnapshot.Read();Check(memory.managed_bytes>0,"performance diagnostics can sample managed memory without forcing garbage collection");
    }
}
