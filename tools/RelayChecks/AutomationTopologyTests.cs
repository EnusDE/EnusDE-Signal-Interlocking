using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;

public static class AutomationTopologyTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static PlanEdge Edge(string id,string a,string b,double length)=>new PlanEdge{Id=id,A=a,B=b,Length=length};
    static DirectedRouteSection Piece()=>new DirectedRouteSection{Section=new TrackSection{Segment="S",From=0,To=100}};
    public static void Run() {
        var edges=new[]{Edge("start","A","B",100),Edge("normal","B","C",50),Edge("branch","B","D",50),Edge("rear","Q","A",60)}.ToDictionary(e=>e.Id);
        var nodes=new Dictionary<string,PlanNode>{{"A",new PlanNode{Id="A"}},{"B",new PlanNode{Id="B",Point=true,Common="start"}}};
        double? Distance(string a,double from,string b,double to,bool reverse=false,double limit=300)=>ContactDistance.Find(new MarkerAnchor{Segment=a,Meters=from},reverse,new MarkerAnchor{Segment=b,Meters=to},
            id=>edges.TryGetValue(id,out var e)?e:null,id=>nodes.TryGetValue(id,out var n)?n:null,id=>edges.Values.Where(e=>e.A==id||e.B==id),limit);
        Check(Distance("start",90,"normal",20)==30&&Distance("start",90,"branch",5)==15,"contact distance measures valid facing turnout paths without commanding a switch");
        Check(Distance("start",10,"rear",50)== -20&&Distance("start",90,"branch",5,true)== -15,"contact position signs follow the configured direction on either side of a native segment boundary");
        Check(!Distance("normal",5,"branch",5).HasValue,"contact measurement never invents a direct trailing-branch to trailing-branch turnout traversal");
        Check(!Distance("start",90,"branch",5,false,10).HasValue&&!Distance("start",0,"start",50,false,20).HasValue,"contact distance limits apply to both joined and same-segment positions");
        nodes["B"].Common=null;Check(!Distance("start",90,"normal",20).HasValue,"unreadable turnout anatomy prevents guessed contact position data");nodes["B"].Common="start";
        edges=new[]{Edge("start","A","B",100),Edge("up","B","U",50),Edge("down","B","D",50),Edge("upEnd","U","Z",50),Edge("downEnd","D","Z",50),Edge("target","Z","K",100)}.ToDictionary(e=>e.Id);
        nodes=new Dictionary<string,PlanNode>{{"B",new PlanNode{Id="B",Point=true,Common="start"}},{"U",new PlanNode{Id="U"}},{"D",new PlanNode{Id="D"}},{"Z",new PlanNode{Id="Z",Point=true,Common="target"}}};
        Check(!Distance("start",90,"target",20).HasValue,"equally short physical approaches around a loop are reported as ambiguous");
        edges["down"].Length=80;Check(Distance("start",90,"target",20)==130,"a longer distant loop does not hide a unique local contact approach or exhaust a map-wide depth-first search");
        var library=new DeskLibrary();
        DeskDefinition Station(string code,params string[] signals)=>new DeskDefinition{Code=code,Panel=new PanelDocument{Tiles=signals.Select((s,i)=>new PanelTile{X=i,Kind=PanelTileKind.Signal,Reference=s}).ToList()}};
        var a=Station("A","a");var b=Station("B","b","bx");var c=Station("C","c");library.Desks.AddRange(new[]{a,b,c});
        RouteDefinition Route(string entry,string exit)=>new RouteDefinition{Entry=entry,Exit=exit,Path=new List<DirectedRouteSection>{Piece()}};
        var arrival=Route("a","b");var inside=Route("b","bx");var onward=Route("bx","c");b.Acceptance=StationAcceptance.DestinationOnly;
        Check(StationAutomationPolicy.Admission(library,new[]{arrival,inside})==null&&StationAutomationPolicy.Admission(library,new[]{arrival,inside,onward})!=null,
            "destination-only neighbors accept a complete arrival within their own station and reject requests passing onward");
        b.Acceptance=StationAcceptance.Closed;
        Check(StationAutomationPolicy.Admission(library,new[]{arrival})!=null&&StationAutomationPolicy.Admission(library,new[]{onward})==null,"closed neighbor acceptance blocks new arrivals while permitting departure from that neighbor");
        inside.Shunting=true;
        Check(StationAutomationPolicy.LocalShunt(library,inside,new MarkerAnchor[0])&&!StationAutomationPolicy.LocalShunt(library,inside,new[]{new MarkerAnchor{Segment="S",Meters=50}}),
            "local shunting stays in ordinary route protection only if it remains within one assigned station and crosses no entry or exit boundary");
        inside.Shunting=false;Check(!StationAutomationPolicy.LocalShunt(library,inside,new MarkerAnchor[0]),"train routes never opt out of direction admission through the local shunt exemption");
        var target=new AuxiliaryTarget{Name="Rangierziel",Anchor=new MarkerAnchor{Segment="S",Meters=100}};b.Panel.Tiles.Add(new PanelTile{X=5,Kind=PanelTileKind.AuxiliaryTarget,Target=target});
        Check(StationAutomationPolicy.Owner(library,target.Id)==b,"auxiliary destinations inherit their local station's acceptance and shunting classification");
        library.Desks.Add(Station("Duplicate","b"));Check(Reject(()=>StationAutomationPolicy.Owner(library,"b")),"conflicting local station ownership is rejected instead of guessing a neighbor");
        var hidden=JsonConvert.DeserializeObject<DeskDefinition>(JsonConvert.SerializeObject(new DeskDefinition{ShowInDispatcher=false}));
        Check(!hidden.ShowInDispatcher,"explicitly hidden stations stay hidden after shared persistence despite legacy visible defaults");
    }
}
