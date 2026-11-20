using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;

static class RoutePointProtectionTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static TrackSection S(string id,double a,double b)=>new(){Segment=id,From=a,To=b};
    static NetworkInput Graph()=>new(){Nodes=new(){new PlanNode{Id="A"},new PlanNode{Id="W",Point=true,Common="stem",Normal="normal"},new PlanNode{Id="B"},new PlanNode{Id="C"}},Edges=new(){new PlanEdge{Id="stem",A="A",B="W",Length=100},new PlanEdge{Id="normal",A="W",B="B",Length=100},new PlanEdge{Id="branch",A="W",B="C",Length=100}}};
    sealed class World:IInterlockingWorld {
        public VehicleOccupancyIndex OccupancyIndex;public Func<string,IEnumerable<TrackSection>> Guards;
        public bool PathValid(RouteDefinition r)=>RoutePointProtection.Current(r,Guards);
        public TrackOccupancy Occupancy(TrackSection section)=>OccupancyIndex.Read(section,out _);
        public bool SwitchPosition(string id,out bool position){position=false;return true;}
        public bool SetSwitch(string id,bool position)=>true;
    }
    public static void Run() {
        foreach(bool reverse in new[]{false,true})foreach(bool shunting in new[]{false,true}) {
            var graph=Graph();var boundary=new MarkerAnchor{Segment=reverse?"normal":"stem",Meters=reverse?10:85};
            IEnumerable<TrackSection> Guards(string id)=>PointGuardGeometry.Build(id,id,graph.Edges,new[]{boundary}).Guards;
            var path=reverse?new List<DirectedRouteSection>{new(){Section=S("normal",0,10),Reverse=true},new(){Section=S("stem",20,100),Reverse=true}}:
                new List<DirectedRouteSection>{new(){Section=S("stem",85,100)},new(){Section=S("normal",0,90)}};
            var route=new RouteDefinition{Code=reverse?"reverse":"forward",Entry="entry",Exit="exit",Shunting=shunting,Sections=path.Select(p=>p.Section.Clone()).ToList(),Path=path,Approach=new(){S("rear",0,30)},Switches=new(){new RouteSwitch{Node="W",Thrown=false,ReleaseAt=reverse?10:15}},ViaSignals=new()};
            RoutePointProtection.Bind(route,Guards);FlankProtection.Plan(route,graph,Array.Empty<MarkerRecord>(),Guards);route.PointGuardSignature=RoutePointProtection.Signature(route,Guards);RouteSignals.SplitProtection(route);route.Validate();
            var waiting=reverse?S("normal",11,18):S("stem",75,84.5);
            var world=new World{Guards=Guards,OccupancyIndex=new VehicleOccupancyIndex(new[]{new VehicleTrackFootprint{Id="waiting",Sections=new(){waiting}}})};
            var engine=new InterlockingEngine();engine.Request(route,world);for(int i=0;i<5;i++)engine.Tick(world);
            Check(engine.ForSignal("entry").Permitted&&!route.Sections.Any(s=>s.Overlaps(waiting)),
                (shunting?"shunt":"train")+" route in "+(reverse?"reverse":"forward")+" direction excludes the waiting train beyond the actual source detector, retaining the same physical point boundary as the diagram");
            var realGuard=route.Switches.Single(p=>p.Node=="W").Protection;
            Check(realGuard.Any(s=>s.Segment==boundary.Segment&&(reverse?s.To==10:s.From==85))&&realGuard.Any(s=>s.Segment=="branch"&&s.To==25),
                "source detector limits its own arm without dropping the other point protection legs");
            string signature=route.PointGuardSignature;
            Check(RoutePointProtection.Current(route,id=>Guards(id).Reverse())&&signature==route.Clone().PointGuardSignature,
                "point-boundary signature survives persistence and guard enumeration order");
            boundary.Meters+=reverse?1:-1;
            Check(!RoutePointProtection.Current(route,Guards),"moving an effective detector invalidates the old route protection before another request can reuse it");
            engine.Tick(world);Check(engine.ForSignal("entry").Stage==RouteStage.Fault&&engine.Locked("W",true),"changed physical point guards withdraw authority while retaining the established point lock");
        }
        var g=Graph();IEnumerable<TrackSection> Default(string id)=>PointGuardGeometry.Build(id,id,g.Edges,Array.Empty<MarkerAnchor>()).Guards;
        Check(Default("W").Single(s=>s.Segment=="stem").From==75,"without a physical detector the standard twenty-five-metre guard is preserved instead of assuming clear track at a drawn signal");
        bool incomplete=false;try{RoutePointProtection.Read("W",_=>new[]{S("stem",85,100)});}catch(InvalidOperationException){incomplete=true;}
        Check(incomplete,"a missing point arm cannot silently produce a shortened valid protection set");
        var legacy=new RouteDefinition{Code="old",Switches=new(){new RouteSwitch{Node="W"}}};bool needsUpdate=false;try{RoutePointProtection.RequireCalculated(legacy);}catch(InvalidOperationException){needsUpdate=true;}
        Check(needsUpdate&&!RoutePointProtection.Current(legacy,Default),"saved routes without a physical point-boundary signature require explicit recalculation instead of retaining obsolete twenty-five-metre locks");

    }
}
