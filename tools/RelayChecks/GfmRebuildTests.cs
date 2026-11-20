using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class GfmRebuildTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static string Id()=>Guid.NewGuid().ToString("N");
    static DirectedRouteSection Piece(string segment,double from=0,double to=100)=>new DirectedRouteSection{Section=new TrackSection{Segment=segment,From=from,To=to}};
    static TrackBlock Circuit(string a,string b,string name)=>new TrackBlock{Id="c:"+Id(),Name=name,BoundaryA=a,BoundaryB=b,Path=new List<DirectedRouteSection>{Piece(name)}};
    static bool Boundary(PanelTile tile,string id)=>tile.CircuitBoundary==id||tile.Kind==PanelTileKind.Switch&&TrackCircuit.TryPointId(id,out var point,out _)&&tile.Reference==point;
    static (DeskLibrary library,DeskDefinition local,NetworkResult network,string a,string b,string c) Fixture() {
        string key=new string('e',64),a=Id(),b=Id(),c=Id();
        var global=new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey=key}};
        var local=new DeskDefinition{Code="WHB",Name="Station",Panel=new PanelDocument{WorldKey=key,Columns=12,Rows=8}};
        var panel=local.Panel;PanelEditing.Draw(panel,1,3,9,3);PanelEditing.Draw(panel,5,3,7,1);
        panel.Put(new PanelTile{X=1,Y=3,Kind=PanelTileKind.Signal,Reference=a,CircuitBoundary=a,Ports=17});
        panel.Put(new PanelTile{X=9,Y=3,Kind=PanelTileKind.Signal,Reference=b,CircuitBoundary=b,Ports=17});
        panel.Put(new PanelTile{X=7,Y=1,Kind=PanelTileKind.Signal,Reference=c,CircuitBoundary=c,Ports=34});
        panel.Put(new PanelTile{X=5,Y=3,Kind=PanelTileKind.Switch,Reference="W",Ports=19,Stem=16,Normal=1});
        panel.Put(new PanelTile{X=2,Y=3,Kind=PanelTileKind.TrainNumber,Ports=17,NumberId=Id(),Number="12345"});
        var library=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{global,local}};
        library.Points["W"]=new PointAssignment{Code="W001",Desk=local.Id,ShowCircuit=true};
        var network=new NetworkResult();var circuits=new[]{Circuit(a,TrackCircuit.PointId("W",0),"stem"),Circuit(b,TrackCircuit.PointId("W",2),"branch"),Circuit(c,TrackCircuit.PointId("W",1),"normal")};
        foreach(var block in circuits)network.Blocks[block.Id]=block;
        network.Blocks["p:W"]=new TrackBlock{Id="p:W",Name="W001",Point="W",Guards=new List<TrackSection>{new TrackSection{Segment="stem",From=75,To=100},new TrackSection{Segment="normal",From=0,To=25},new TrackSection{Segment="branch",From=0,To=25}}};
        return (library,local,network,a,b,c);
    }
    public static void Run() {
        var f=Fixture();string before=JsonConvert.SerializeObject(f.library);
        var detected=ExistingDiagramDetection.Build(f.library,f.local.Id,f.network,Boundary);var panel=detected.Library.Desks.Single(d=>d.Id==f.local.Id).Panel;
        Check(detected.Calibrated==1&&panel.At(5,3).Stem==16&&panel.At(5,3).Normal==2&&detected.Circuits==3,
            "physical boundary connections correct swapped native normal/branch arms in a manually drawn point and assign all three sections");
        Check(JsonConvert.SerializeObject(f.library)==before&&panel.Tiles.Select(t=>(t.X,t.Y,t.Kind)).SequenceEqual(f.local.Panel.Tiles.Select(t=>(t.X,t.Y,t.Kind))),
            "point calibration and GFM assignment are isolated previews that retain the complete diagram geometry");
        Check(CircuitDiagramAssignment.PointArm(panel.At(5,3),1)==1&&CircuitDiagramAssignment.PointArm(panel.At(5,3),0)==2,
            "corrected arm roles are shared by boundary assignment and live point rendering instead of assuming the drawn straight arm is native normal");
        var partial=new NetworkResult();var first=f.network.Blocks.Values.First(b=>b.IsCircuit);partial.Blocks[first.Id]=first;
        var uncertain=ExistingDiagramDetection.Build(f.library,f.local.Id,partial,Boundary);
        Check(uncertain.Calibrated==0&&uncertain.Library.Desks.Single(d=>d.Id==f.local.Id).Panel.At(5,3).Normal==1,
            "one known point connection does not guess the roles of the other two arms");
        var conflict=new NetworkResult();var bad=Circuit(f.b,TrackCircuit.PointId("W",0),"conflict");conflict.Blocks[first.Id]=first;conflict.Blocks[bad.Id]=bad;
        var rejected=ExistingDiagramDetection.Build(f.library,f.local.Id,conflict,Boundary);
        Check(rejected.Calibrated==0&&rejected.Unresolved.Count>0,"contradictory physical arm evidence remains unresolved instead of changing the drawing arbitrarily");
        var missing=Fixture();missing.local.Panel.Tiles.RemoveAll(t=>t.Reference==missing.b);
        var issueResult=ExistingDiagramDetection.Build(missing.library,missing.local.Id,missing.network,Boundary);
        var issue=issueResult.Issues.First(i=>i.Block.BoundaryA==missing.b||i.Block.BoundaryB==missing.b);
        double saved=missing.network.Blocks[issue.Block.Id].Path[0].Section.To;issue.Block.Path[0].Section.To++;
        Check(issue.Desk==missing.local.Id&&missing.network.Blocks[issue.Block.Id].Path[0].Section.To==saved,
            "clickable unresolved GFM issues retain the correct desk and an isolated physical-section snapshot");
        foreach(bool rotate in new[]{false,true})foreach(bool mirror in new[]{false,true}) {
            var variant=Fixture();var diagram=variant.local.Panel;diagram.Rows=12;
            foreach(var tile in diagram.Tiles){if(mirror){tile.Y=7-tile.Y;tile.Mirror=!tile.Mirror;}if(rotate){int x=tile.X;tile.X=11-tile.Y;tile.Y=x;tile.Rotation=(tile.Rotation+1)%4;}}
            var result=ExistingDiagramDetection.Build(variant.library,variant.local.Id,variant.network,Boundary);
            var point=result.Library.Desks.Single(d=>d.Id==variant.local.Id).Panel.Tiles.Single(t=>t.Kind==PanelTileKind.Switch);
            Check(result.Circuits==3&&point.Stem==16&&point.Normal==2,"native point-arm calibration preserves all connections under diagram rotation="+rotate+" and mirror="+mirror);
        }
        Rebuild(f);Aliases();Gaps();
    }
    static void Rebuild((DeskLibrary library,DeskDefinition local,NetworkResult network,string a,string b,string c) f) {
        var old=Circuit(f.a,f.b,"old across point");f.library.Blocks[old.Id]=old;
        var oldPanel=f.library.Desks.Single(d=>d.Id==f.local.Id).Panel;oldPanel.At(3,3).BlockId=old.Id;oldPanel.At(4,3).Section=new TrackSection{Segment="legacy",From=1,To=500};oldPanel.At(2,3).AutoSegment="legacy";
        string before=JsonConvert.SerializeObject(f.library),number=oldPanel.At(2,3).NumberId;
        var rebuilt=GfmDetectionRefresh.Build(f.library,new[]{f.local.Id},f.network,Boundary,null,null,true);var panel=rebuilt.Library.Desks.Single(d=>d.Id==f.local.Id).Panel;
        Check(rebuilt.Removed==1&&!rebuilt.Library.Blocks.ContainsKey(old.Id)&&rebuilt.ResetFields==3&&rebuilt.Circuits==3,
            "full GFM rescan removes obsolete manual circuit definitions before assigning current physical sections");
        Check(panel.Tiles.All(t=>t.Section==null&&t.AutoSegment==""&&!GfmDetectionRefresh.References(t).Contains(old.Id))&&panel.At(2,3).NumberId==number&&panel.At(2,3).Number=="12345"&&panel.At(1,3).Reference==f.a&&panel.At(1,3).CircuitBoundary==f.a&&panel.At(5,3).Reference=="W",
            "GFM reset clears obsolete raw spans and links while retaining train-number identities, boundaries, world links and point ownership");
        Check(JsonConvert.SerializeObject(f.library)==before,"a reset preview never changes the live library before explicit application");
        var neighbour=new DeskDefinition{Code="ELA",Name="Neighbour",Panel=new PanelDocument{WorldKey=f.library.WorldKey}};neighbour.Panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Straight,BlockId=old.Id});f.library.Desks.Add(neighbour);
        var localOnly=GfmDetectionRefresh.Build(f.library,new[]{f.local.Id},f.network,Boundary,null,null,true);
        Check(localOnly.Library.Blocks.ContainsKey(old.Id)&&localOnly.Library.Desks.Single(d=>d.Id==neighbour.Id).Panel.At(1,1).BlockId==old.Id,
            "local rescan preserves a shared old circuit still used by an untouched neighbouring diagram");
        var all=GfmDetectionRefresh.Build(f.library,f.library.Desks.Select(d=>d.Id),f.network,Boundary,null,null,true);
        Check(!all.Library.Blocks.ContainsKey(old.Id)&&all.Library.Desks.Single(d=>d.Id==neighbour.Id).Panel.At(1,1).BlockId=="",
            "installation-wide rescan clears obsolete circuit references in every diagram so stale shared records cannot survive through another desk");
        var empty=GfmDetectionRefresh.Build(f.library,f.library.Desks.Select(d=>d.Id),new NetworkResult(),Boundary,null,null,true);
        Check(empty.Library.Blocks.Values.All(b=>!b.IsCircuit)&&empty.Library.Desks.SelectMany(d=>d.Panel.Tiles).All(t=>!GfmDetectionRefresh.References(t).Any()),
            "a rescan finding no complete sections leaves the diagrams unmonitored rather than retaining stale clear-track bindings");
    }
    static void Aliases() {
        var f=Fixture();var aliases=new Dictionary<string,string>{{TrackCircuit.PointId("W",0),f.a}};
        var panel=f.local.Panel;panel.At(1,3).CircuitBoundary=null;panel.At(1,3).Reference="";panel.At(1,3).Kind=PanelTileKind.Straight;
        // The canonical A/B end is represented at the point, with two other raw point arms providing independent evidence.
        bool End(PanelTile tile,string id)=>Boundary(tile,id)||tile.Kind==PanelTileKind.Switch&&id==f.a;
        var unresolved=new List<string>();int changed=PointDiagramCalibration.Apply(panel,f.network,End,aliases,null,unresolved);
        Check(changed==1&&panel.At(5,3).Normal==2,"independent native point-arm evidence calibrates the remaining arm even when a shared A/B alias is present");
    }
    static void Gaps() {
        var path=new[]{new TrackSection{Segment="S",From=0,To=100}};
        var coverage=new[]{new TrackSection{Segment="S",From=0,To=20},new TrackSection{Segment="S",From=30,To=60},new TrackSection{Segment="S",From=50,To=80},new TrackSection{Segment="parallel",From=0,To=100}};
        var gaps=GfmCoverage.Gaps(path,coverage);
        Check(gaps.Count==2&&gaps[0].From==20&&gaps[0].To==30&&gaps[1].From==80&&gaps[1].To==100,
            "diagnostic route coverage reports exact missing intervals while merging overlapping coverage and ignoring parallel tracks");
        Check(GfmCoverage.Gaps(path,new[]{new TrackSection{Segment="S",From=0,To=100}}).Count==0&&GfmCoverage.Gaps(path,Array.Empty<TrackSection>()).Single().To==100,
            "read-only gap inspection distinguishes full coverage from a completely unmonitored route");
        var cross=new PanelTile{Kind=PanelTileKind.Cross,Ports=85,CircuitRails=new Dictionary<int,string>{{0,"h"},{4,"h"},{2,"v"},{6,"v"}}};
        Check(GfmCoverage.Ports(cross,new HashSet<string>{"h"})==17&&GfmCoverage.Ports(cross,new HashSet<string>{"v"})==68,
            "diagnostic highlighting selects only the chosen independent rail at a crossing");
        var number=new PanelTile{Kind=PanelTileKind.TrainNumber,BlockId="h"};
        Check(GfmCoverage.Ports(number,new HashSet<string>{"h"})==17,"GFM inspection highlights continuous train-number fields with their implicit rail ports");
    }
}
