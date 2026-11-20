using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;

static class LineSetupTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static DirectedRouteSection Part(string segment,bool reverse=false,double from=0,double to=100)=>new DirectedRouteSection{Reverse=reverse,Section=new TrackSection{Segment=segment,From=from,To=to}};
    public static void Run() {
        string key=new string('f',64);
        var master=new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey=key}};
        var whittier=new DeskDefinition{Code="WHB",Name="Whittier",Panel=new PanelDocument{WorldKey=key,Columns=8,Rows=6}};
        var ela=new DeskDefinition{Code="ELA",Name="Ela",Panel=new PanelDocument{WorldKey=key,Columns=8,Rows=6}};
        var third=new DeskDefinition{Code="OTHER",Name="Other",Panel=new PanelDocument{WorldKey=key,Columns=8,Rows=6}};
        var library=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{master,whittier,ela,third}};
        var line=LineSetupWorkflow.Create(library,whittier.Id);
        Check(line.StationA==whittier.Id&&line.StationB==ela.Id&&!line.Enabled&&line.Mode==LineTrackMode.SingleTrack&&line.Code=="STR-001","new local block setup keeps Whittier as neighbour A and creates an inactive single-track connection to Ela");
        PanelEditing.PlaceField(whittier.Panel,2,2,PanelTileKind.LinePermission);
        Check(whittier.Panel.At(2,2).Kind==PanelTileKind.LinePermission,"block indicator can actually be placed through the editor placement tool");
        Check(Reject(()=>PanelEditing.PlaceField(whittier.Panel,2,2,PanelTileKind.LinePermission)),"block placement never overwrites an existing indicator");
        ela.Panel.Put(new PanelTile{Kind=PanelTileKind.LinePermission,X=3,Y=2});
        master.Panel.Put(new PanelTile{Kind=PanelTileKind.LinePermission,X=1,Y=1});
        var target=LineIndicatorTarget.Capture(library,whittier.Id,2,2);
        LineSetupWorkflow.Put(library,line,target);library.Validate(key);
        Check(library.FreeLines.Count==1&&whittier.Panel.At(2,2).Reference==line.Id&&library.FreeLines[0].Id==line.Id,"saving a connection and binding its originating local indicator occurs together with one shared line ID");
        var second=LineIndicatorTarget.Capture(library,ela.Id,3,2);second.Bind(library,line);
        var central=LineIndicatorTarget.Capture(library,master.Id,1,1);central.Bind(library,line);library.Validate(key);
        Check(ela.Panel.At(3,2).Reference==whittier.Panel.At(2,2).Reference&&master.Panel.At(1,1).Reference==line.Id,"Ela and master indicators link to Whittier's existing connection without creating independent permissions");
        Check(LineSetupWorkflow.Visible(library,whittier.Id).Single().Id==line.Id&&LineSetupWorkflow.Visible(library,ela.Id).Single().Id==line.Id&&
            LineSetupWorkflow.Visible(library,master.Id).Single().Id==line.Id&&!LineSetupWorkflow.Visible(library,third.Id).Any(),"local setup shows only connected lines while the master sees all shared connections");
        string saved=JsonConvert.SerializeObject(library);
        var changed=line.Clone();changed.Name="Whittier – Ela updated";
        target.Reference=line.Id;LineSetupWorkflow.Put(library,changed,target);
        Check(library.FreeLines.Count==1&&LineSetupWorkflow.Visible(library,ela.Id).Single().Name==changed.Name&&line.Name!=changed.Name,"editing the same connection from either end updates the shared definition while keeping the draft isolated");
        Check(LineSetupWorkflow.Create(library,ela.Id).Code=="STR-002","new line codes avoid existing shared connections");
        var stale=LineIndicatorTarget.Capture(library,whittier.Id,2,2);whittier.Panel.At(2,2).Kind=PanelTileKind.Label;
        string before=JsonConvert.SerializeObject(library);var newLine=LineSetupWorkflow.Create(library,whittier.Id);
        Check(Reject(()=>LineSetupWorkflow.Put(library,newLine,stale))&&JsonConvert.SerializeObject(library)==before,"stale or replaced originating fields reject setup before adding a partial connection");
        whittier.Panel.At(2,2).Kind=PanelTileKind.LinePermission;
        third.Panel.Put(new PanelTile{Kind=PanelTileKind.LinePermission,X=0,Y=0});
        var unrelated=LineIndicatorTarget.Capture(library,third.Id,0,0);
        Check(Reject(()=>unrelated.Bind(library,line)),"an unrelated local interlocking cannot be silently assigned another station's permission indicator");
        Check(Reject(()=>LineIndicatorTarget.Capture(library,whittier.Id,7,7))&&Reject(()=>LineSetupWorkflow.Create(new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{master,whittier}},whittier.Id)),"missing indicator fields and fewer than two local interlockings produce actionable setup failures");
        var pair=LineSetupWorkflow.MatchDirections(new List<DirectedRouteSection>[] {new(){Part("S")}},new List<DirectedRouteSection>[] {new(){Part("OTHER",true)},new(){Part("S",true)}});
        Check(pair.Forward==0&&pair.Reverse==1,"line recognition chooses a physically matching opposite path instead of blindly selecting the first reverse candidate");
        Check(Reject(()=>LineSetupWorkflow.MatchDirections(new List<DirectedRouteSection>[] {new(){Part("S")}},new List<DirectedRouteSection>[] {new(){Part("OTHER",true)}}))&&
            Reject(()=>LineSetupWorkflow.MatchDirections(new List<DirectedRouteSection>[] {new(){Part("S")}},new List<DirectedRouteSection>[] {new(){Part("S")}})),"opposite paths on other tracks or facing the same way cannot form an enabled single-track axis");
        line.Forward=new List<DirectedRouteSection>{Part("S")};line.Reverse=new List<DirectedRouteSection>{Part("S",true)};
        string a=Guid.NewGuid().ToString("N"),b=Guid.NewGuid().ToString("N");
        var detected=new NetworkResult{Panel=new PanelDocument{WorldKey=key}};
        TrackBlock Circuit(string id,string segment,double from=0,double to=100)=>new TrackBlock{Id=id,Name=id,BoundaryA=a,BoundaryB=b,Path=new(){Part(segment,false,from,to)}};
        detected.Blocks["covered"]=Circuit("covered","S");detected.Blocks["outside"]=Circuit("outside","OTHER");
        detected.Blocks["extends"]=Circuit("extends","S",0,120);
        detected.Blocks["raw"]=new TrackBlock{Id="raw",Name="unmonitored",Path=new(){Part("S")}};
        detected.Blocks["incomplete"]=new TrackBlock{Id="incomplete",Name="incomplete",BoundaryA=a,Path=new(){Part("S")}};
        detected.Blocks["point"]=new TrackBlock{Id="point",Name="point",Point="W1",Guards=new(){new TrackSection{Segment="S",From=30,To=40}}};
        library.Points["W1"]=new PointAssignment{Code="W1",Desk=whittier.Id,ShowCircuit=false};
        var candidates=LineSetupWorkflow.DetectionCandidates(library,line,detected);
        Check(candidates.Length==1&&candidates[0].Id=="covered","line GFM proposals include complete in-scope boundary sections and exclude unrelated, incomplete, extending and unmonitored track paths");
        library.Points["W1"].ShowCircuit=true;candidates=LineSetupWorkflow.DetectionCandidates(library,line,detected);
        Check(candidates.Length==2&&candidates.Any(c=>c.Point=="W1"),"already enabled point detection may be included without opting other points into detection");
        candidates.First(c=>c.Id=="covered").Name="changed";
        Check(detected.Blocks["covered"].Name=="covered"&&library.Blocks.Count==0,"reviewing GFM proposals does not mutate source detection or install sections before confirmation");
        library=JsonConvert.DeserializeObject<DeskLibrary>(saved);library.Validate(key);
        var restored=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(library));restored.Validate(key);
        Check(restored.FreeLines.Single().Id==restored.Desks.First(d=>d.Id==whittier.Id).Panel.At(2,2).Reference,"save transfer retains the connection and its originating module binding together");
        library.Modules.Add(new DeskModule{Desk=whittier.Id,X=8,Y=0});library.Modules.Add(new DeskModule{Desk=ela.Id,X=22,Y=0});
        var projection=MasterDiagram.Project(library);
        Check(projection.At(10,2).Reference==line.Id&&projection.At(25,2).Reference==line.Id,"linked local indicators remain bound to the same line in the projected master diagram");
    }
}
