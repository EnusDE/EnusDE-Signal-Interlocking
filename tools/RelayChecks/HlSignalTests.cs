using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class HlSignalTests {
    static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL: "+label);System.Console.WriteLine("PASS: "+label);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    public static void Run(string root) {
        var legacySettings=JsonConvert.DeserializeObject<DiagnosticSettings>("{}").Normalize();
        Check(!legacySettings.Markers.EnableHlSignalsTest,"existing settings keep new Hl test construction disabled until explicitly enabled");
        legacySettings.Markers.EnableHlSignalsTest=true;var enabledSettings=legacySettings.Clone();legacySettings.Markers.EnableHlSignalsTest=false;
        Check(enabledSettings.Markers.EnableHlSignalsTest&&!legacySettings.Markers.EnableHlSignalsTest,"Hl test opt-in persists independently in a settings clone");
        var hv=new MarkerRecord{Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="test",Meters=1}};var hl=hv.Clone();hl.HlSignal=true;
        Check(HlTestFeature.RequiresEnable(null,hl)&&HlTestFeature.RequiresEnable(hv,hl)&&Reject(()=>HlTestFeature.Require(false)),"new Hl builds and H/V-to-Hl conversion require the explicit Test setting");
        Check(!HlTestFeature.RequiresEnable(hl,hl.Clone())&&!HlTestFeature.RequiresEnable(hl,hv)&&!HlTestFeature.RequiresEnable(hv,hv.Clone()),"disabled Hl opt-in still permits existing Hl edits and conversion back to H/V");
        HlTestFeature.Require(true);
        var speeds=new[]{HlSpeed.Line,HlSpeed.Hundred,HlSpeed.Sixty,HlSpeed.Forty};
        var ahead=new[]{HlSpeed.Line,HlSpeed.Hundred,HlSpeed.Forty,HlSpeed.Stop};
        string[][] expected={new[]{"Hl 1","Hl 4","Hl 7","Hl 10"},new[]{"Hl 2","Hl 5","Hl 8","Hl 11"},
            new[]{"Hl 3b","Hl 6b","Hl 9b","Hl 12b"},new[]{"Hl 3a","Hl 6a","Hl 9a","Hl 12a"}};
        var gallery=new List<object>();
        for(int row=0;row<4;row++)for(int col=0;col<4;col++) {
            var plan=HlLampPlan.For(speeds[row],ahead[col]);
            Check(plan.Code==expected[row][col]&&plan.Lamps[2]==SignalLensColor.Off&&plan.Lamps[4]==SignalLensColor.Off&&plan.Lamps[5]==SignalLensColor.Off&&
                plan.Lamps[3]==(row>0?SignalLensColor.Yellow:SignalLensColor.Off)&&
                plan.Lamps[6]==(row==2?SignalLensColor.Yellow:SignalLensColor.Off)&&
                plan.Lamps[7]==(row==1?SignalLensColor.Green:SignalLensColor.Off)&&
                plan.Lamps[0]==(col<2?SignalLensColor.Green:SignalLensColor.Off)&&
                plan.Lamps[1]==(col>=2?SignalLensColor.Yellow:SignalLensColor.Off),
                expected[row][col]+" follows the Ril301 upper-light and lower-speed combination without red or shunting whites");
            Check(plan.At(.25).SequenceEqual(plan.Lamps)&&
                plan.At(.75).Select((color,i)=>color==((col==1&&i==0||col==2&&i==1)?SignalLensColor.Off:plan.Lamps[i])).All(x=>x),
                expected[row][col]+" flashes only its announcing light and retains the lower yellow and speed strip");
            Check(HlLampPlan.For(speeds[row],HlSpeed.Sixty).Code==HlLampPlan.For(speeds[row],HlSpeed.Forty).Code,
                expected[row][col]+" shares the documented 40/60 km/h advance indication");
            gallery.Add(new{plan.Code,current=(int)speeds[row],next=(int)ahead[col],lamps=plan.Lamps.Select(c=>c.ToString()).ToArray(),plan.FlashMask});
        }
        var stop=HlLampPlan.Stop();Check(stop.Code=="Hp 0"&&stop.FlashMask==0&&stop.Lamps.Count(c=>c!=SignalLensColor.Off)==1&&stop.Lamps[2]==SignalLensColor.Red,"Hl stop emits only steady red; modern Ril301 name is Hp0");
        Check(HlLampPlan.For((HlSpeed)55,HlSpeed.Line).Code=="Hp 0"&&HlLampPlan.For(HlSpeed.Line,(HlSpeed)55).Code=="Hp 0","invalid Hl speed inputs cannot synthesize a proceed aspect");
        Check(HlLampPlan.Runtime(MarkerRecord.Main,LocalSignalAspect.Stop,LocalSignalAspect.Clear,true).Code=="Hp 0"&&
            HlLampPlan.Runtime(MarkerRecord.Main,(LocalSignalAspect)99,LocalSignalAspect.Clear,true).Code=="Hp 0","main authority has priority over any next-signal state and unknown authority fails to stop");
        Check(HlLampPlan.Runtime(MarkerRecord.Main,LocalSignalAspect.Clear,LocalSignalAspect.Stop,true).Code=="Hl 10"&&
            HlLampPlan.Runtime(MarkerRecord.Main,LocalSignalAspect.Slow,LocalSignalAspect.Stop,true).Code=="Hl 12a"&&
            HlLampPlan.Runtime(MarkerRecord.Main,LocalSignalAspect.Slow,LocalSignalAspect.Clear,true).Code=="Hl 3a"&&
            HlLampPlan.Runtime(MarkerRecord.Main,LocalSignalAspect.Clear,LocalSignalAspect.Slow,true).Code=="Hl 7"&&
            HlLampPlan.Runtime(MarkerRecord.Main,LocalSignalAspect.Slow,LocalSignalAspect.Slow,true).Code=="Hl 9a","live Hl main display combines current clear/40 authority with next clear/40/stop warning");
        Check(HlLampPlan.Runtime(MarkerRecord.Main,LocalSignalAspect.Clear,LocalSignalAspect.Stop,false).Code=="Hl 1"&&
            HlLampPlan.Runtime(MarkerRecord.Main,LocalSignalAspect.Clear,LocalSignalAspect.Invalid,true).Code=="Hl 10"&&
            HlLampPlan.Runtime(MarkerRecord.Main,LocalSignalAspect.Slow,LocalSignalAspect.Kennlicht,true).Code=="Hl 12a","pure main display omits advance function; unbound or inactive targets never announce clear");
        Check(new[]{LocalSignalAspect.Clear,LocalSignalAspect.Slow,LocalSignalAspect.Stop}.Select(a=>HlLampPlan.Runtime(MarkerRecord.Distant,a,LocalSignalAspect.Clear,false).Code).SequenceEqual(new[]{"Hl 1","Hl 7","Hl 10"}),"freestanding Hl distant uses Hl1/Hl7/Hl10 and does not turn expect-stop into red authority");
        Check(new[]{LocalSignalAspect.Invalid,LocalSignalAspect.Kennlicht,LocalSignalAspect.Dark}.All(a=>HlLampPlan.Runtime(MarkerRecord.Main,a,LocalSignalAspect.Clear,true).Lamps.All(c=>c==SignalLensColor.Off)),"invalidity cross, identification light and darkness suppress every Hl operating light");
        var shunt=HlLampPlan.Runtime(MarkerRecord.Main,LocalSignalAspect.Shunt,LocalSignalAspect.Clear,true);
        Check(shunt.Lamps[2]==SignalLensColor.Red&&shunt.Lamps[4]==SignalLensColor.White&&shunt.Lamps[5]==SignalLensColor.White&&shunt.Lamps.Count(c=>c!=SignalLensColor.Off)==3&&shunt.FlashMask==0,"Ra12 keeps stop plus two rising diagonal white lights and suppresses advance indications");
        foreach(bool distant in new[]{false,true}) {
            var mesh=MarkerMeshData.CreateHlHead(distant);var lamps=HlSignalGeometry.Lamps(distant);
            Check(mesh.Vertices.Count<400&&mesh.Triangles.All(t=>t.Count%3==0&&t.All(i=>i>=0&&i<mesh.Vertices.Count))&&
                lamps.Length==(distant?2:8)&&lamps[0][0]>0&&lamps[1][0]<0&&lamps[0][1]==lamps[1][1],"Hl "+(distant?"distant":"main")+" original screen geometry has bounded valid triangles and separated announcing lenses");
        }
        var anchor=new MarkerAnchor{Segment="hl-test",Meters=15};
        string directory=Path.Combine(root,"build","hl-tests-"+Guid.NewGuid().ToString("N"));
        string key=MarkerRepository.Key(Path.Combine(directory,"test.railroad"),"map");var repo=new MarkerRepository(directory,key,"map");
        var marker=repo.Put(anchor,null,MarkerRecord.Main);var original=marker.Clone();
        repo.ConfigureHlSignal(marker.Id,true);marker=repo.Records.Single();
        Check(marker.HlSignal&&marker.VisualRevision>original.VisualRevision&&marker.Id==original.Id&&marker.Name==original.Name&&marker.CircuitNumber==original.CircuitNumber&&marker.Anchor.Meters==original.Anchor.Meters,"H/V to Hl conversion retains physical identity, technical code, detection boundary and placement");
        var mast=MastConfiguration.Effective(marker);mast.Back.Enabled=true;repo.ConfigureMast(marker.Id,mast);
        Check(SignalCatalogue.Expand(repo.Records).Count(m=>m.HlSignal&&m.PhysicalId==marker.Id)==2,"both enabled mast directions expand with the same Hl model profile and distinct signal IDs");
        repo=new MarkerRepository(directory,key,"map");Check(repo.Records.Single().HlSignal&&repo.Records.Single().Clone().HlSignal,"Hl profile survives repository round-trip and cloning");
        Check(Reject(()=>repo.ConfigureLowMain(marker.Id,true)),"unsupported low Hl conversion is rejected instead of displaying H/V optics under an Hl label");
        repo.ConfigureHlSignal(marker.Id,false);Check(!repo.Records.Single().HlSignal&&repo.Records.Single().Id==original.Id,"conversion back to H/V retains the original signal identity");
        repo.ConfigureLowMain(marker.Id,true);Check(Reject(()=>repo.ConfigureHlSignal(marker.Id,true)),"low mast must be converted to a high mast before choosing Hl");
        var distantMarker=repo.Put(anchor,null,MarkerRecord.Distant,hlSignal:true);
        Check(distantMarker.HlSignal&&new MarkerRepository(directory,key,"map").Records.Single(m=>m.Id==distantMarker.Id).HlSignal,"new freestanding Hl distant is persisted at initial placement");
        var session=new MarkerEditSession();session.Start(new MarkerOptions(),MarkerRecord.Main);session.HlSignal=true;session.Offer(anchor);session.Freeze();
        var placed=session.Commit(repo,a=>true);
        Check(placed.HlSignal&&placed.Kind==MarkerRecord.Main,"Hl build selection reaches the initial placement transaction without a separate conversion write");
        session.Start(new MarkerOptions(),MarkerRecord.Main);Check(!session.HlSignal,"starting an ordinary H/V placement clears the previous Hl selection");
        Check(Reject(()=>repo.Put(anchor,null,MarkerRecord.Shunt,hlSignal:true)),"Hl profile on an unsupported standalone shunting object is rejected");
        var legacy=JsonConvert.DeserializeObject<MarkerRecord>(JsonConvert.SerializeObject(original));Check(!legacy.HlSignal,"legacy signals remain H/V without an installation migration");
        gallery.Add(new{stop.Code,current=0,next=0,lamps=stop.Lamps.Select(c=>c.ToString()).ToArray(),stop.FlashMask});
        File.WriteAllText(Path.Combine(root,"build","checks","hl-signalplans.json"),JsonConvert.SerializeObject(new{lamps=HlSignalGeometry.Lamps(),screen=MarkerMeshData.CreateHlHead(false,true),gallery},Formatting.Indented));
    }
}
