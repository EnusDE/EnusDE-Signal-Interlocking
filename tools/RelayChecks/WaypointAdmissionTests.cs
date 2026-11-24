using System;
using System.Collections.Generic;
using System.Linq;
using EnusDE.Signaltechnik;

public static class WaypointAdmissionTests {
    static void Check(bool success,string description) {
        if(!success)throw new Exception("FAIL: "+description);
        System.Console.WriteLine("PASS: "+description);
    }

    static bool Rejected<T>(Action request) where T:Exception {
        try{request();return false;}catch(T){return true;}
    }

    class World:IInterlockingWorld {
        public TrackOccupancy Destination=TrackOccupancy.Clear;
        public TrackOccupancy Approach=TrackOccupancy.Clear;
        public bool PointThrown=true;
        public int PointCommands;

        public bool PathValid(RouteDefinition route)=>true;
        public TrackOccupancy Occupancy(TrackSection section)=>section.Segment=="target"?Destination:section.Segment=="approach"?Approach:TrackOccupancy.Clear;
        public bool SwitchPosition(string id,out bool thrown){thrown=PointThrown;return true;}
        public bool SetSwitch(string id,bool thrown){PointCommands++;PointThrown=thrown;return true;}
    }

    sealed class SectionalWorld:World,ISectionalWorld,IVehicleOccupancyWorld,ITrainStopWorld {
        public string[] Approaching={"engine","wagon"};
        public string[] DestinationCars=Array.Empty<string>();
        public string[] ApproachCars=Array.Empty<string>();
        public double Head=-3;
        public double Tail=-20;
        public bool Stopped;
        public bool CaptureTrain(RouteDefinition route,out string[] cars){cars=Approaching;return cars!=null;}
        public bool TrainProgress(RouteDefinition route,string[] cars,out double head,out double tail){head=Head;tail=Tail;return !double.IsNaN(Head);}
        public bool TrainStopped(string[] cars)=>Stopped;
        public TrackOccupancy SectionWithCars(TrackSection section,out string[] cars) {
            var occupancy=Occupancy(section);
            cars=occupancy==TrackOccupancy.Occupied?(section.Segment=="approach"?ApproachCars:DestinationCars):Array.Empty<string>();
            return occupancy;
        }
    }

    static RouteDefinition Route(bool shunting=false,bool withPoint=false) {
        var target=new TrackSection{Segment="target",From=0,To=100};
        var route=new RouteDefinition {
            Code=shunting?"S1":"T1",Entry="A",Exit="B",Shunting=shunting,
            Sections={target.Clone()},Approach={new TrackSection{Segment="approach",From=0,To=50}},
            Path=new(){new DirectedRouteSection{Section=target.Clone()}},ViaSignals=new()
        };
        if(withPoint) {
            var guard=new TrackSection{Segment="point",From=0,To=20};
            route.Sections.Add(guard);
            route.Switches.Add(new RouteSwitch{Node="P1",Thrown=true,ReleaseAt=20,Protection=new(){guard.Clone()}});
        }
        return route;
    }

    static void Tick(InterlockingEngine engine,IInterlockingWorld world,int count=5) {
        for(int i=0;i<count;i++)engine.Tick(world);
    }

    public static void Run() {
        string[] cars={"engine","wagon"};
        var engine=new InterlockingEngine();
        var world=new SectionalWorld{Approaching=new[]{"engine","different wagon"}};
        Check(Rejected<InvalidOperationException>(()=>engine.RequestWaypoint(Route(),world,cars))&&
            Rejected<InvalidOperationException>(()=>engine.RequestWaypoint(Route(),new World(),cars))&&engine.Active.Count==0,
            "waypoint admission rejects a mismatched consist or an approach without vehicle identity before creating a route");

        world.Approaching=new[]{"wagon","engine"};
        var intended=Route(withPoint:true);
        engine.RequestWaypoint(intended,world,cars);
        var run=engine.Active.Single();
        Check(run.Stage==RouteStage.Preparing&&!run.Permitted&&run.TrainCars.OrderBy(id=>id).SequenceEqual(cars),
            "waypoint admission binds the exact approaching vehicles regardless of capture order and starts at stop");
        Tick(engine,world);
        Check(run.Permitted&&run.TrainCars.OrderBy(id=>id).SequenceEqual(cars),
            "the intended approaching consist retains its association through normal end-position and clear-track checks");

        engine=new InterlockingEngine();
        world=new SectionalWorld();
        engine.RequestWaypoint(Route(withPoint:true),world,cars);
        engine.Tick(world);
        world.Approaching=new[]{"engine","replacement"};
        Tick(engine,world);
        Check(engine.Active.Single().Stage==RouteStage.Fault&&!engine.Active.Single().Permitted&&engine.Locked("P1",false),
            "a changed approaching consist during waypoint preparation keeps the signal at stop and retains the point lock");

        engine=new InterlockingEngine();
        world=new SectionalWorld{Destination=TrackOccupancy.Occupied};
        var shunt=Route(shunting:true);
        Check(Rejected<InvalidOperationException>(()=>engine.RequestWaypoint(shunt,world,cars))&&engine.Active.Count==0,
            "automatic waypoint shunting rejects a destination that is already occupied");
        engine.Request(shunt,world);
        Tick(engine,world);
        Check(engine.Active.Single().Permitted,"manual shunting retains the established occupied-target rules");

        engine=new InterlockingEngine();
        world=new SectionalWorld();
        engine.RequestWaypoint(Route(shunting:true),world,cars);
        world.Destination=TrackOccupancy.Occupied;
        Tick(engine,world);
        Check(engine.Active.Single().Stage==RouteStage.Fault&&!engine.Active.Single().Permitted,
            "automatic waypoint shunting stays at stop if the route becomes occupied during preparation");

        engine=new InterlockingEngine();
        world=new SectionalWorld();
        var clearedShunt=engine.RequestWaypoint(Route(shunting:true,withPoint:true),world,cars);
        Tick(engine,world);
        bool shuntCleared=clearedShunt.Permitted;
        world.Destination=TrackOccupancy.Occupied;
        world.DestinationCars=new[]{"foreign wagon"};
        engine.Tick(world);
        Check(shuntCleared&&clearedShunt.Stage==RouteStage.Fault&&!clearedShunt.Permitted&&engine.Locked("P1",false),
            "foreign vehicle intrusion drops a cleared automatic shunt to stop and retains its point lock");

        foreach(bool shunting in new[]{false,true}) {
            engine=new InterlockingEngine();
            world=new SectionalWorld{Approaching=null,Head=double.NaN};
            var early=engine.RequestWaypoint(Route(shunting),world,cars,15,null,()=>true);
            Tick(engine,world);
            Check(early.Permitted&&early.TrainCars==null&&!early.Entered,
                "verified directed approach permits advance reservation without a nearby capture or premature passage");
            world.Head=10;world.Destination=TrackOccupancy.Occupied;world.DestinationCars=cars;
            engine.Tick(world);
            Check(early.Entered&&early.TrainCars.SequenceEqual(cars)&&early.Stage==RouteStage.Running,
                "advance reservation binds only the planned consist when it reaches the route");
        }
        bool proof=true;
        engine=new InterlockingEngine();
        world=new SectionalWorld{Approaching=null,Head=double.NaN};
        var revoked=engine.RequestWaypoint(Route(withPoint:true),world,cars,15,null,()=>proof);
        proof=false;
        Tick(engine,world);
        Check(revoked.Stage==RouteStage.Fault&&!revoked.Permitted&&engine.Locked("P1",false),
            "lost approach proof during advance preparation keeps stop and retains protection");

        var approach=WaypointApproach.Create(new List<DirectedRouteSection>{
            new DirectedRouteSection{Section=new TrackSection{Segment="approach",From=0,To=600}},
            new DirectedRouteSection{Section=new TrackSection{Segment="target",From=0,To=100}}
        },new MarkerAnchor{Segment="approach",Meters=0},false,new MarkerAnchor{Segment="target",Meters=100});
        var start=new MarkerRecord{Anchor=new MarkerAnchor{Segment="target",Meters=0}};
        world=new SectionalWorld{Approach=TrackOccupancy.Occupied,ApproachCars=cars};
        Check(WaypointRoutePolicy.ClearApproach(approach,start,world,cars),
            "waypoint advance reads the approach to the start signal and admits only the expected train on it");
        world.ApproachCars=new[]{"foreign"};
        Check(!WaypointRoutePolicy.ClearApproach(approach,start,world,cars),
            "a foreign vehicle ahead of the lead consist blocks advance requests");
        world.Approach=TrackOccupancy.Unknown;
        Check(!WaypointRoutePolicy.ClearApproach(approach,start,world,cars),
            "unreadable approach occupancy blocks advance requests");

        engine=new InterlockingEngine();
        world=new SectionalWorld{Approaching=null,Head=double.NaN};
        var turning=engine.RequestWaypoint(Route(shunting:true,withPoint:true),world,cars,15,null,()=>true,80);
        Tick(engine,world);
        world.Head=80;world.Tail=19;world.Stopped=true;
        world.Destination=TrackOccupancy.Occupied;world.DestinationCars=cars;
        Tick(engine,world);
        Check(turning.Entered&&engine.Locked("P1",false),"a stopped turnback keeps its point locked while the train tail is short of clearance");
        world.Tail=30;world.Head=70;
        Tick(engine,world);
        Check(engine.Active.Contains(turning),"a shunt turnback remains reserved when the front has not reached the planned turning point");
        world.Head=80;
        Tick(engine,world);
        Check(engine.Active.Count==0&&world.Destination==TrackOccupancy.Occupied,
            "a verified turnback releases after the entire train stands beyond the point without changing reported occupancy");

        engine=new InterlockingEngine();
        world=new SectionalWorld();
        var conflictingShunt=Route(shunting:true);
        conflictingShunt.Path[0].Reverse=true;
        engine.Request(conflictingShunt,world);
        Tick(engine,world);
        var existing=engine.Active.Single();
        Check(existing.Permitted&&Rejected<RouteConflictException>(()=>engine.RequestWaypoint(Route(),world,cars))&&
            ReferenceEquals(engine.Active.Single(),existing)&&existing.Permitted,
            "a conflicting waypoint request leaves the already cleared manual shunt intact");
    }
}
