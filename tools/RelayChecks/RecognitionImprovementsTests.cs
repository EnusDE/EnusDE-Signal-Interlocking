using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;

static class RecognitionImprovementsTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static string Id()=>Guid.NewGuid().ToString("N");
    static PanelDocument Base(string a,string b) {
        var panel=new PanelDocument{WorldKey=new string('e',64),Columns=12,Rows=8};PanelEditing.Draw(panel,1,3,9,3);
        panel.Put(new PanelTile{Kind=PanelTileKind.Signal,Reference=a,CircuitBoundary=a,X=1,Y=3,Ports=17});panel.Put(new PanelTile{Kind=PanelTileKind.Signal,Reference=b,CircuitBoundary=b,X=9,Y=3,Ports=17});return panel;
    }
    static TrackBlock Block(string a,string b,string name)=>new TrackBlock{Id="c:"+Id(),Name=name,BoundaryA=a,BoundaryB=b,Path=new List<DirectedRouteSection>{new DirectedRouteSection{Section=new TrackSection{Segment=name,From=0,To=100}}}};
    public static void Run() {
        string a=Id(),b=Id(),c=Id(),d=Id();bool Boundary(PanelTile tile,string id)=>tile.CircuitBoundary==id;
        var panel=Base(a,b);var original=panel.At(1,3).Clone();original.MainSignalDirection=0;original.ShuntSignalDirection=2;var rotated=original.Clone();for(int i=0;i<4;i++)rotated=PanelQuickActions.Rotate(rotated);
        Check(rotated.SameContent(original),"four quick rotations preserve every signal link, detector, position and head direction");
        rotated=PanelQuickActions.Rotate(PanelQuickActions.Rotate(original));Check(rotated.MainSignalDirection==2&&rotated.ShuntSignalDirection==0&&rotated.Reference==a&&rotated.CircuitBoundary==a,"a quick half turn changes both displayed heads without changing physical signal or GFM identity");
        var target=new AuxiliaryTarget{Id=Id(),Name="Virtual destination",Train=true,Shunt=true,Anchor=new MarkerAnchor{Segment="H",Meters=50}};
        panel.Put(new PanelTile{Kind=PanelTileKind.AuxiliaryTarget,Target=target,X=5,Y=3,Ports=17});var horizontal=Block(a,b,"H");
        Check(CircuitDiagramAssignment.Assign(panel,3,3,horizontal,Boundary)==9&&panel.At(5,3).BlockId==horizontal.Id,"a virtual destination does not break continuous section occupancy between two physical signal boundaries");
        panel=Base(a,b);PanelEditing.Draw(panel,5,0,5,6);panel.Put(new PanelTile{Kind=PanelTileKind.Signal,Reference=c,CircuitBoundary=c,X=5,Y=0,Ports=68});panel.Put(new PanelTile{Kind=PanelTileKind.Signal,Reference=d,CircuitBoundary=d,X=5,Y=6,Ports=68});
        var vertical=Block(c,d,"V");CircuitDiagramAssignment.Assign(panel,3,3,horizontal,Boundary);CircuitDiagramAssignment.Assign(panel,5,2,vertical,Boundary);var cross=panel.At(5,3);
        Check(cross.CircuitRails[0]==horizontal.Id&&cross.CircuitRails[4]==horizontal.Id&&cross.CircuitRails[2]==vertical.Id&&cross.CircuitRails[6]==vertical.Id&&cross.BlockId=="","crossing detection binds each independent rail to its own occupancy rather than mixing both tracks");
        Check(Reject(()=>CircuitDiagramAssignment.Find(panel,3,3,a,c,Boundary)),"a drawn crossing cannot invent a turnout or a connection between its independent tracks");panel.Validate(panel.WorldKey);
        LongSection(a,b);StandaloneC(a,b);SharedPoint(a);BothFaces(a,b);
    }
    static void LongSection(string a,string b) {
        var input=new NetworkInput{World=new string('e',64),Nodes=new List<PlanNode>{new PlanNode{Id="x"},new PlanNode{Id="y",X=3500}},Edges=new List<PlanEdge>{new PlanEdge{Id="long",A="x",B="y",Length=3500}},Cuts=new List<PlanCut>{
            new PlanCut{Segment="long",Meters=100,X=100,Signal=a,Boundary=a,BoundaryKind=1},new PlanCut{Segment="long",Meters=1750,X=1750,Signal=Id()},new PlanCut{Segment="long",Meters=3300,X=3300,Signal=b,Boundary=b,BoundaryKind=1}}};
        var catalog=DetectionNetwork.Build(input);
        var complete=catalog.Blocks.Values.Single(p=>p.IsCircuit);
        Check(complete.Path.Sum(p=>p.Section.To-p.Section.From)==3200&&TrackCircuit.Pair(complete.BoundaryA,complete.BoundaryB)==TrackCircuit.Pair(a,b),"a long GFM section covers its full 3200 metres despite an intermediate signal without its own boundary");
        var display=Base(a,b);
        foreach(var tile in new[]{display.At(4,3),display.At(6,3)})tile.AutoSegment="long";
        display.At(4,3).AutoMeters=1500;display.At(6,3).AutoMeters=1850;
        DetectionNetwork.RebindAutomatic(display,catalog.Blocks);
        Check(display.At(4,3).BlockId==complete.Id&&display.At(6,3).BlockId==complete.Id,"both sides of a non-detector signal use the physical GFM circuit instead of overlapping fallback chains");
        input.Cuts[1].Boundary=Id();input.Cuts[1].BoundaryKind=3;
        Check(!DetectionNetwork.Build(input).Blocks.Values.Any(p=>p.IsCircuit&&p.Path.Sum(s=>s.Section.To-s.Section.From)==3200),"an actual one-sided C boundary still interrupts a long section and is never bypassed for a larger display");
    }
    static void StandaloneC(string a,string b) {
        string c=Id(),key=new string('e',64);
        var input=new NetworkInput{World=key,Nodes=new List<PlanNode>{new PlanNode{Id="x"},new PlanNode{Id="y",X=1000}},Edges=new List<PlanEdge>{new PlanEdge{Id="S",A="x",B="y",Length=1000}},Cuts=new List<PlanCut>{
            new PlanCut{Segment="S",Meters=100,X=100,Boundary=a,BoundaryKind=1},new PlanCut{Segment="S",Meters=500,X=500,Boundary=c,BoundaryKind=3},new PlanCut{Segment="S",Meters=900,X=900,Boundary=b,BoundaryKind=1}}};
        var network=DetectionNetwork.Build(input);var section=network.Blocks.Values.Single(p=>p.IsCircuit);
        Check(TrackCircuit.Pair(section.BoundaryA,section.BoundaryB)==TrackCircuit.Pair(c,b),"a free-standing C detector creates a circuit only on its monitored side without requiring a signal at C");
        var global=new DeskDefinition{Global=true,Code="ESTW",Name="Global",Panel=new PanelDocument{WorldKey=key}};
        var local=new DeskDefinition{Code="WHB",Name="Whittier",Panel=Base(a,b)};
        local.Panel.Put(new PanelTile{Kind=PanelTileKind.Straight,CircuitBoundary=c,X=5,Y=3,Ports=17});
        var library=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{global,local}};
        var proposal=ExistingDiagramDetection.Build(library,local.Id,network,(tile,id)=>tile.CircuitBoundary==id);var panel=proposal.Library.Desks.Single(d=>d.Id==local.Id).Panel;
        Check(proposal.Circuits==1&&panel.At(5,3).CircuitBoundary==c&&panel.At(6,3).BlockId==section.Id&&panel.At(4,3).BlockId==""&&panel.At(5,3).CircuitRails.Count==1,
            "an assigned C boundary persists in the existing diagram and leaves its unmonitored side without an invented occupancy section");
        Check(Reject(()=>CircuitDiagramAssignment.Find(panel,3,3,a,b,(tile,id)=>tile.CircuitBoundary==id)),"existing-diagram assignment cannot continue through a free-standing C monitoring limit");
        input.Cuts[1].BoundaryReverse=true;
        var reversed=DetectionNetwork.Build(input).Blocks.Values.Single(p=>p.IsCircuit);
        Check(TrackCircuit.Pair(reversed.BoundaryA,reversed.BoundaryB)==TrackCircuit.Pair(a,c),"reversing a C detector changes its monitored side rather than creating two-sided coverage");
    }
    static void SharedPoint(string a) {
        string key=new string('e',64);var global=new DeskDefinition{Global=true,Code="ESTW",Name="Global",Panel=new PanelDocument{WorldKey=key}};var local=new DeskDefinition{Code="WHB",Name="Whittier",Panel=new PanelDocument{WorldKey=key}};PanelEditing.Draw(local.Panel,1,3,5,3);
        local.Panel.Put(new PanelTile{Kind=PanelTileKind.Signal,Reference=a,CircuitBoundary=a,X=1,Y=3,Ports=17});local.Panel.Put(new PanelTile{Kind=PanelTileKind.Switch,Reference="W",X=5,Y=3,Ports=19,Stem=16,Normal=1});
        var library=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{global,local}};library.Points["W"]=new PointAssignment{Code="W1",Desk=local.Id,ShowCircuit=true};
        var network=new NetworkResult();network.Blocks["p:W"]=new TrackBlock{Id="p:W",Name="W1",Point="W",Guards=new List<TrackSection>{new TrackSection{Segment="common",From=0,To=25}}};string raw=TrackCircuit.PointId("W",0);
        var proposal=ExistingDiagramDetection.Build(library,local.Id,network,(t,id)=>t.CircuitBoundary==id,new Dictionary<string,string>{{raw,a}});var panel=proposal.Library.Desks.Single(s=>s.Id==local.Id).Panel;
        Check(proposal.Shared==1&&panel.At(1,3).CircuitRails[0]=="p:W"&&panel.At(3,3).BlockId=="p:W"&&panel.At(5,3).CircuitRails[4]=="p:W","a shared A/B box automatically assigns the drawn link to the actual point-guard occupancy");
        Check(proposal.Library.Blocks.Values.All(p=>!p.IsCircuit)&&local.Panel.At(3,3).BlockId=="","A/B assignment remains a preview and creates no fictitious zero-length circuit between aliases of the same boundary");
    }
    static void BothFaces(string a,string b) {
        string physical=Id();var input=new NetworkInput{World=new string('e',64),Nodes=new List<PlanNode>{new PlanNode{Id="x"},new PlanNode{Id="y",X=100}},Edges=new List<PlanEdge>{new PlanEdge{Id="S",A="x",B="y",Length=100}},Cuts=new List<PlanCut>{new PlanCut{Segment="S",Meters=50,X=50,Signal=a,PhysicalSignal=physical,Kind=MarkerRecord.Main},new PlanCut{Segment="S",Meters=50,X=50,Signal=b,PhysicalSignal=physical,Kind=MarkerRecord.Main,Reverse=true},new PlanCut{Segment="S",Meters=50,X=50,Boundary=physical,BoundaryKind=1}}};
        var result=NetworkPlan.Build(input);var fields=result.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.Signal).ToArray();
        Check(result.Missing.Count==0&&fields.Length==2&&fields.Select(t=>t.Reference).ToHashSet().SetEquals(new[]{a,b})&&Math.Abs(fields[0].MainSignalDirection.Value-fields[1].MainSignalDirection.Value)==2,"network drawing registers both opposing faces of a mast with separate correctly directed signal buttons");
        Check(fields.Count(t=>t.ProjectedMastFace)==1&&fields.Count(t=>t.CircuitBoundary==physical)==1&&fields.All(t=>PanelConnections.Count(PanelConnections.Ports(t))==2),"opposing face projection keeps one physical GFM boundary and never rotates the rail connections with the head");
    }
}
