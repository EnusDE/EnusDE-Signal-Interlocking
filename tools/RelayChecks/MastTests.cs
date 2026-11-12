using System;
using System.IO;
using System.Linq;
using EnusDE.Signaltechnik;
using Newtonsoft.Json.Linq;

public static class MastTests {
    static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL: "+label);System.Console.WriteLine("PASS: "+label);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    public static void Run(string root) {
        string directory=Path.Combine(root,"build","mast-tests-"+Guid.NewGuid().ToString("N"));
        string key=MarkerRepository.Key(Path.Combine(directory,"one.railroad"),"map");var repo=new MarkerRepository(directory,key,"map");
        var anchor=new MarkerAnchor{Segment="S1",Meters=30,Lateral=2.3};
        var mast=repo.Put(anchor,null,MarkerRecord.Main);
        var config=MastConfiguration.Effective(mast);string back=config.Back.Id;
        Check(config.Front.Id==mast.Id&&!config.Back.Enabled&&config.Front.Code==mast.Name,"new mast starts with one forward signal and a distinct stable reserved reverse-direction ID");
        config.Front.Code="A1";config.Front.Hp1=true;config.Front.Hp2=false;config.Front.Sh1=true;
        config.Back.Code="B1";config.Back.Enabled=true;config.Back.Hp1=false;config.Back.Hp2=true;config.Back.Sh1=false;
        repo.ConfigureMast(mast.Id,config,"Mast West");config.Front.Code="external mutation";
        var loaded=new MarkerRepository(directory,key,"map");var stored=loaded.Records.Single();var signals=SignalCatalogue.Expand(loaded.Records);
        Check(signals.Length==2&&signals.Any(s=>s.Id==mast.Id&&s.Name=="A1")&&signals.Any(s=>s.Id==back&&s.Name=="B1"),"both mast directions retain independent signal IDs and codes across atomic save and reload");
        Check(signals[0].Anchor.Segment==signals[1].Anchor.Segment&&signals[0].Anchor.Meters==signals[1].Anchor.Meters&&signals[0].FacingReverse!=signals[1].FacingReverse&&signals.All(s=>s.PhysicalId==mast.Id),"two opposite directed signals share one physical mast and one track anchor");
        Check(SignalCatalogue.Supports(signals[0],false,false)&&!SignalCatalogue.Supports(signals[0],false,true)&&SignalCatalogue.Supports(signals[0],true,false)&&!SignalCatalogue.Supports(signals[1],true,false)&&SignalCatalogue.Supports(signals[1],false,true),"Hp 1, Hp 2 and Sh 1 permissions are evaluated independently for each mast direction");
        var invalid=MastConfiguration.Effective(stored);invalid.Back.Code="a1";
        Check(Reject(()=>loaded.ConfigureMast(mast.Id,invalid))&&SignalCatalogue.Expand(loaded.Records).Any(s=>s.Name=="B1"),"duplicate direction codes are rejected without partially replacing a saved mast");
        invalid=MastConfiguration.Effective(stored);invalid.Front.Id=Guid.NewGuid().ToString("N");Check(Reject(()=>loaded.ConfigureMast(mast.Id,invalid)),"configuration cannot replace the forward signal ID referenced by existing routes");
        var legacy=stored.Clone();legacy.Mast=null;legacy.Name="old HS";
        var first=MastConfiguration.Effective(legacy);var second=MastConfiguration.Effective(legacy);
        Check(first.Front.Id==legacy.Id&&first.Back.Id==second.Back.Id&&SignalCatalogue.Expand(new[]{legacy}).Single().Id==legacy.Id,"legacy 0.2.0 main signals preserve their original forward ID and get deterministic reverse metadata without duplicate poles");
        var oldJson=JObject.Parse(File.ReadAllText(loaded.FilePath));((JObject)oldJson["Markers"][0]).Remove("Mast");File.WriteAllText(loaded.FilePath,oldJson.ToString());
        var oldReload=new MarkerRepository(directory,key,"map");Check(oldReload.Records.Single().Id==mast.Id&&SignalCatalogue.Expand(oldReload.Records).Length==1,"existing files without a mast configuration load as compatible one-direction masts");
        oldReload.ConfigurePlacement(mast.Id,3.1,.2,1,false);var defaults=new MarkerOptions{UseGameSpacing=false,Lateral=2.7,Height=.1,Scale=1.5};oldReload.ApplyHostPlacement(defaults,r=>true);
        Check(oldReload.Records.Single().Anchor.Lateral==3.1&&!oldReload.Records.Single().UseHostPlacement,"host placement defaults do not overwrite explicitly independent mast offsets");
        oldReload.ConfigurePlacement(mast.Id,3.1,.2,1,true);oldReload.ApplyHostPlacement(defaults,r=>true);
        Check(oldReload.Records.Single().Anchor.Lateral==2.7&&oldReload.Records.Single().Anchor.Height==.1&&oldReload.Records.Single().Anchor.Meters==30,"host defaults update side/height/scale while preserving track distance and object identity");
        defaults.Lateral=4;
        Check(Reject(()=>oldReload.ApplyHostPlacement(defaults,r=>false))&&oldReload.Records.Single().Anchor.Lateral==2.7,"a locked mast rejects bulk placement changes atomically");
        var left=PlacementDefaults.Resolve(new MarkerOptions{UseGameSpacing=true,Lateral=-1.8,DefaultRightSide=false});Check(Math.Abs(PlacementDefaults.GameSideOffset-2.3)<.00001&&left.Lateral==2.3&&PlacementDirection.Side(left.Lateral,left.DefaultRightSide,false)==-2.3&&PlacementDirection.Side(left.Lateral,left.DefaultRightSide,true)==2.3,"game-default placement reads the real 2.30 m spacing and applies the chosen side relative to travel direction");
        var shunt=SignalLampPlan.For(LocalSignalAspect.Shunt,LocalSignalAspect.Clear);
        Check(shunt.Main==LocalSignalAspect.Stop&&shunt.Shunt==LocalSignalAspect.Shunt&&shunt.Distant==LocalSignalAspect.Dark,"Sh 1 on a mast keeps Hp 0 and cannot accidentally illuminate main-signal green");
        var clear=SignalLampPlan.For(LocalSignalAspect.Clear,LocalSignalAspect.Slow);
        Check(clear.Main==LocalSignalAspect.Clear&&clear.Shunt==LocalSignalAspect.Dark&&clear.Distant==LocalSignalAspect.Slow,"main running authority keeps attached shunt lamps dark and separately announces the linked signal");
        var stop=SignalLampPlan.For(LocalSignalAspect.Stop,LocalSignalAspect.Clear);Check(stop.Main==LocalSignalAspect.Stop&&stop.Distant==LocalSignalAspect.Dark,"a stopped mast does not announce a green next signal on its attached distant head");
        Check(MarkerMeshData.CreateSignal(false,false,true).Triangles[1].Count==0&&MarkerMeshData.CreateSignal(true,false,true).Triangles[1].Count==0&&MarkerMeshData.CreateMast().Triangles[1].Count>0,"attached signal heads contain no duplicate post; one separate mast mesh carries both directions");
        var speed=MarkerMeshData.CreateSpeedBoard();
        Check(Math.Abs(MarkerMeshData.CreateMast().Vertices.Max(v=>v[1])-6.75)<.00001&&Math.Abs(4.15+MarkerMeshData.MainLift-6.75)<.00001,"taller 6.75 m mast and lifted main head share the same height definition without scaling the whole signal");
        Check(speed.Vertices.Take(8).Max(v=>v[1])<speed.Vertices.Skip(16).Take(8).Min(v=>v[1]),"speed-board post ends below the white face instead of projecting through the displayed number");
    }
}
