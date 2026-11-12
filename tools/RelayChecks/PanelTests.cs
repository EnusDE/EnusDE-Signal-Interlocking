using System;
using System.IO;
using System.Linq;
using EnusDE.Signaltechnik;

public static class PanelTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    public static void Run(string root) {
        string directory=Path.Combine(root,"build","panel-tests-"+Guid.NewGuid().ToString("N"));var store=new PanelStore(directory,"world-A");var doc=store.Load();
        Check(doc.Tiles.Count==0&&doc.Columns==24&&doc.Rows==12&&!File.Exists(store.FilePath),"new panel starts truly blank and creates no generated track layout or file");
        doc.Put(new PanelTile{X=2,Y=3,Kind=PanelTileKind.Signal,Reference="stable-A",Label="Ausfahrt",Rotation=2});store.Save(doc);var loaded=store.Load();
        Check(loaded.At(2,3).Reference=="stable-A"&&loaded.At(2,3).Rotation==2&&loaded.At(2,3).Label=="Ausfahrt","manual panel geometry and stable signal binding survive atomic save/reload");
        loaded.At(2,3).Reference="changed";Check(store.Load().At(2,3).Reference=="stable-A","panel drafts cannot mutate persisted world bindings");
        doc.Put(new PanelTile{X=4,Y=3,Kind=PanelTileKind.Switch,Reference="W1",Mirror=true});store.Save(doc);
        Check(new PanelStore(directory,"world-B").Load().Tiles.Count==0&&File.Exists(store.FilePath+".bak"),"panel storage isolates railroads and retains a previous-file backup");
        Check(Reject(()=>doc.Put(new PanelTile{X=-1,Y=0}))&&doc.Tiles.Count==2,"out-of-bounds tile edits reject atomically without losing existing tiles");
        var invalid=doc.Clone();invalid.Columns=3;Check(Reject(()=>store.Save(invalid))&&store.Load().Columns==24,"invalid panel resizing cannot overwrite the saved diagram");
        invalid=doc.Clone();invalid.Tiles.Add(invalid.Tiles[0].Clone());Check(Reject(()=>store.Save(invalid)),"duplicate panel cells are rejected instead of ambiguously operating a signal");
        var buttons=new PanelButtons();Check(!buttons.Press("A",id=>true)&&buttons.Start=="A"&&buttons.End==null,"first valid signal button selects the route start only");
        Check(buttons.Press("B",id=>true)&&buttons.End=="B","second distinct button completes explicit A-to-B selection");
        var route=new RouteDefinition{Entry="A",Exit="B"};Check(buttons.Match(new[]{route},false,false)==route&&Reject(()=>buttons.Match(new[]{route},true,false)),"two-button operation selects only the requested saved route and movement type");
        Check(Reject(()=>buttons.Match(new[]{route,route.Clone()},false,false)),"ambiguous route definitions require explicit selection rather than choosing a random route");
        buttons.Clear();buttons.Press("A",id=>true);Check(Reject(()=>buttons.Press("B",id=>id=="B"))&&buttons.Start==null,"a deleted start signal invalidates a pending two-button action");
        File.WriteAllText(store.FilePath,"{broken");Check(Reject(()=>store.Load())&&File.ReadAllText(store.FilePath)=="{broken","corrupt panel files are preserved and cannot silently become an empty replacement");
        double mph;Check(NativeSignProfile.TrySpeed("Speed Sign","SignsGroup01",448,978,.005588,.0007175,.0265922,out mph)&&mph==25,"base-map speed conversion recognizes the inspected 0.6.2 mesh profile with canonical 25 mph");
        Check(!NativeSignProfile.TrySpeed("Other Sign","SignsGroup01",448,978,.005588,.0007175,.0265922,out mph)&&!NativeSignProfile.TrySpeed("Speed Sign","SignsGroup01",449,978,.005588,.0007175,.0265922,out mph),"unknown scenery and changed meshes are not assigned guessed speed limits");
    }
}
