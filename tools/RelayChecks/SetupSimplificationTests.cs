using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class SetupSimplificationTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static DirectedRouteSection Part(string segment)=>new DirectedRouteSection{Section=new TrackSection{Segment=segment,From=0,To=100}};
    public static void Run(string root) {
        var old=JsonConvert.DeserializeObject<DiagnosticSettings>("{}").Normalize();Check(old.Markers.NewSignalsWithGfm,"legacy construction settings retain automatic signal GFM boxes by default");
        var options=new MarkerOptions{NewSignalsWithGfm=false,UseGameSpacing=false};
        Check(!PlacementDefaults.Resolve(options).NewSignalsWithGfm,"disabling new-signal GFM survives resolved construction defaults");
        var repository=new MarkerRepository(Path.Combine(root,"build","gfm-default-"+Guid.NewGuid().ToString("N")),new string('a',64),"map");
        var session=new MarkerEditSession();session.Start(options,MarkerRecord.Main);session.Offer(new MarkerAnchor{Segment="S",Meters=10});session.Freeze();var noBox=session.Commit(repository,_=>true);
        Check(!CircuitIdentity.Enabled(noBox),"the real placement session can build a new signal without a detection boundary");
        session.Start(new MarkerOptions(),MarkerRecord.Main);session.Offer(new MarkerAnchor{Segment="S",Meters=50});session.Freeze();var withBox=session.Commit(repository,_=>true);
        Check(CircuitIdentity.Enabled(withBox)&&withBox.CircuitNumber>0,"default signal placement includes and registers its A boundary in the same save");
        session.Edit(noBox);session.NewSignalGfm=true;session.Commit(repository,_=>true);
        Check(!CircuitIdentity.Enabled(repository.Records.Single(r=>r.Id==noBox.Id)),"changing the build default cannot enable a boundary while editing an existing signal");
        var graph=new NetworkInput{Nodes=new List<PlanNode>{new PlanNode{Id="A"},new PlanNode{Id="B"}},Edges=new List<PlanEdge>{new PlanEdge{Id="S",A="A",B="B",Length=100}}};
        var contact=new AutomationContact{Anchor=new MarkerAnchor{Segment="S",Meters=5}};
        var first=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="S",Meters=20}};
        var second=first.Clone();second.Id=Guid.NewGuid().ToString("N");second.Anchor.Meters=60;
        Check(ContactSignalPolicy.NextMains(graph,new[]{first,second},contact).SequenceEqual(new[]{first.Id}),"automatic contact discovery selects the first main signal and cannot skip to the next one");
        contact.Reverse=true;Check(ContactSignalPolicy.NextMains(graph,new[]{first,second},contact).Length==0,"contact discovery does not bind to a main signal behind its configured direction");
        contact.Reverse=false;contact.AutomaticSignal=true;contact.Signal=first.Id;
        Check(JsonConvert.DeserializeObject<AutomationContact>(JsonConvert.SerializeObject(contact)).AutomaticSignal,"automatic contact binding provenance survives the shared configuration");
        ExistingDiagram();
    }
    static void ExistingDiagram() {
        string key=new string('d',64),a=Guid.NewGuid().ToString("N"),b=Guid.NewGuid().ToString("N");
        var global=new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey=key}};
        var local=new DeskDefinition{Code="WHB",Name="Whittier",Panel=new PanelDocument{WorldKey=key,Columns=12,Rows=6}};
        PanelEditing.Draw(local.Panel,1,3,9,3);
        local.Panel.Put(new PanelTile{Kind=PanelTileKind.Signal,X=1,Y=3,Reference=a,Ports=17});local.Panel.Put(new PanelTile{Kind=PanelTileKind.Signal,X=9,Y=3,Reference=b,Ports=17});
        var library=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{global,local}};
        var block=new TrackBlock{Id="c:"+Guid.NewGuid().ToString("N"),Name="A–B",BoundaryA=a,BoundaryB=b,Path=new List<DirectedRouteSection>{Part("S")}};
        var network=new NetworkResult();network.Blocks.Add(block.Id,block);
        bool Boundary(PanelTile tile,string id)=>tile.Kind==PanelTileKind.Signal&&tile.Reference==id;
        string before=JsonConvert.SerializeObject(library);var proposal=ExistingDiagramDetection.Build(library,local.Id,network,Boundary);var panel=proposal.Library.Desks.Single(d=>d.Id==local.Id).Panel;
        Check(proposal.Circuits==1&&proposal.Fields==9&&panel.At(5,3).BlockId==block.Id&&panel.At(1,3).CircuitRails[0]==block.Id,"existing-diagram detection assigns a complete physical section and its inner signal faces without redrawing");
        Check(JsonConvert.SerializeObject(library)==before&&panel.Tiles.Select(t=>(t.X,t.Y,t.Kind)).SequenceEqual(local.Panel.Tiles.Select(t=>(t.X,t.Y,t.Kind))),"detection remains an isolated preview and preserves every drawn coordinate and field kind");
        var broken=library.Clone();broken.Desks.Single(d=>d.Id==local.Id).Panel.Tiles.RemoveAll(t=>t.X==5);proposal=ExistingDiagramDetection.Build(broken,local.Id,network,Boundary);
        Check(proposal.Circuits==0&&proposal.Unresolved.Count==1&&!proposal.Library.Blocks.ContainsKey(block.Id),"an incomplete schematic path reports unresolved boundaries instead of claiming detection coverage");
        library.Points["discovered"]=new PointAssignment{Code="W1",Desk=global.Id,ElectricStand=true};
        global.Panel.Put(new PanelTile{Kind=PanelTileKind.Switch,X=1,Y=1,Reference="discovered"});
        Check(!PointDrivePolicy.Electric(library,"discovered"),"a globally discovered point and its recognition diagram do not count as an electrically controlled local point");
        local.Panel.Put(new PanelTile{Kind=PanelTileKind.Switch,X=10,Y=2,Reference="discovered"});
        Check(PointDrivePolicy.Electric(library,"discovered")&&PointDrivePolicy.LinkedDesk(library,"discovered")==local.Id,"electric selection applies only after one real local diagram assignment");
        var other=new DeskDefinition{Code="ELA",Name="Ela",Panel=new PanelDocument{WorldKey=key}};other.Panel.Put(new PanelTile{Kind=PanelTileKind.Switch,X=1,Y=1,Reference="discovered"});library.Desks.Add(other);
        Check(!PointDrivePolicy.Electric(library,"discovered"),"ambiguous duplicate local ownership cannot enable an electric drive");
    }
}
