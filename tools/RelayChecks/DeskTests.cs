using EnusDE.Signaltechnik;
using System;
using System.IO;
using System.Linq;

public static class DeskTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    public static void Run(string root) {
        string directory=Path.Combine(root,"build","desk-tests-"+Guid.NewGuid().ToString("N"));
        var legacy=new PanelStore(Path.Combine(directory,"Panels"),"world");var panel=new PanelDocument{WorldKey="world"};panel.Put(new PanelTile{X=2,Y=2,Kind=PanelTileKind.Signal,Reference="A",Label="P1"});panel.Put(new PanelTile{X=3,Y=2,Kind=PanelTileKind.Straight,Section=new TrackSection{Segment="S1",From=0,To=25}});legacy.Save(panel);string original=File.ReadAllText(legacy.FilePath);
        var store=new DeskStore(Path.Combine(directory,"Desks"),"world");var desks=store.Load(legacy.Load());string global=desks.Desks.Single().Id;
        Check(desks.Desks.Single().Global&&desks.Desks.Single().Panel.At(2,2).Reference=="A"&&File.ReadAllText(legacy.FilePath)==original,"legacy panel migrates into one global ESTW without modifying the old file");
        var region=new PanelRegion{X=2,Y=2,Width=2,Height=1};desks=store.CreateLocal(desks,"WIT","Whittier",region.Extract(panel));var local=desks.Desks.Last();
        Check(local.Panel.At(0,0).Reference=="A"&&local.Panel.At(1,0).Section.Key==panel.At(3,2).Section.Key&&desks.Desks[0].Panel.At(2,2)!=null,"region copy preserves physical signal and occupancy bindings in an independent local geometry");
        local.Panel.At(0,0).Label="A1";Check(desks.Desks[0].Panel.At(2,2).Label=="P1","editing local labels does not mutate the global board");
        desks=store.Rename(desks,local.Id,"WIT2","Whittier Ost");Check(desks.Desks.Last().Id==local.Id&&desks.Desks.Single(d=>d.Global).Id==global,"renaming local codes preserves all stable desk IDs");
        string saved=File.ReadAllText(store.FilePath);Check(Reject(()=>store.Rename(desks,local.Id,"estw","Duplicate"))&&File.ReadAllText(store.FilePath)==saved,"case-insensitive duplicate desk codes fail before changing the saved library");
        var moved=panel.Clone();region.Move(moved,6,4);Check(moved.At(6,4).Reference=="A"&&moved.At(7,4).Section.Key==panel.At(3,2).Section.Key&&moved.At(2,2)==null,"moving a selected rectangle preserves references and removes its old cells");
        moved.Put(new PanelTile{X=9,Y=4});string before=Newtonsoft.Json.JsonConvert.SerializeObject(moved);Check(Reject(()=>new PanelRegion{X=6,Y=4,Width=2,Height=1}.Move(moved,8,4))&&Newtonsoft.Json.JsonConvert.SerializeObject(moved)==before,"region collision leaves the original board unchanged");
        Check(Reject(()=>new PanelRegion{X=6,Y=4,Width=2,Height=1}.Move(moved,23,4))&&Newtonsoft.Json.JsonConvert.SerializeObject(moved)==before,"moving a region past the board edge is rejected transactionally");
        var reload=store.Load(null);Check(reload.Desks.Count==2&&reload.Desks.Last().Code=="WIT2"&&reload.Desks.Single(d=>d.Global).Panel.At(2,2).Reference=="A","multiple boards reload without depending on the obsolete single-board file");
        reload.Points["W1"]=new PointAssignment{Code="W1",Desk=local.Id};
        PointAssignments.Boundaries(reload);
        Check(Reject(()=>new DeskRemoval(reload,global).Apply(reload,new[]{new RouteDefinition{Entry="A",Exit="B"}})),"removing the default master still rejects an active route using its signal");
        reload.Desks.Single(d=>d.Id==local.Id).Panel.At(0,0).AlternateSignal="RS-alternate";
        var removal=new DeskRemoval(reload,local.Id);
        Check(removal.RelatesTo(new RouteDefinition{Entry="RS-alternate",Exit="B"}),
            "a route using only the shunting half still blocks removing its local desk");
        var related=new RouteDefinition{Entry="A",Exit="B"};
        Check(removal.RelatesTo(related)&&Reject(()=>removal.Apply(reload,new[]{related}))&&reload.Desks.Count==2,
            "removing a local board is blocked while one of its associated routes is active");
        var kept=removal.Apply(reload,Array.Empty<RouteDefinition>());
        Check(kept.Desks.Count==1&&kept.Points["W1"].Desk==global&&kept.Desks.Single().Panel.At(2,2).Reference=="A"
            &&kept.Desks.Single().Panel.Areas.All(a=>a.Desk!=local.Id)&&reload.Points["W1"].Desk==local.Id&&related.Entry=="A",
            "confirmed local board removal preserves the global drawing and route definitions and returns point ownership to ESTW");
        File.WriteAllText(store.FilePath,"broken");Check(Reject(()=>store.Load(panel))&&File.ReadAllText(store.FilePath)=="broken","invalid desk library is preserved rather than replaced by an empty or legacy board");
    }
}
