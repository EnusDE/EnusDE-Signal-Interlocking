using System;
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
        public bool PointThrown=true;
        public int PointCommands;

        public bool PathValid(RouteDefinition route)=>true;
        public TrackOccupancy Occupancy(TrackSection section)=>section.Segment=="target"?Destination:TrackOccupancy.Clear;
        public bool SwitchPosition(string id,out bool thrown){thrown=PointThrown;return true;}
        public bool SetSwitch(string id,bool thrown){PointCommands++;PointThrown=thrown;return true;}
    }

    sealed class SectionalWorld:World,ISectionalWorld,IVehicleOccupancyWorld {
        public string[] Approaching={"engine","wagon"};
        public string[] DestinationCars=Array.Empty<string>();
        public bool CaptureTrain(RouteDefinition route,out string[] cars){cars=Approaching;return cars!=null;}
        public bool TrainProgress(RouteDefinition route,string[] cars,out double head,out double tail){head=-3;tail=-20;return true;}
        public TrackOccupancy SectionWithCars(TrackSection section,out string[] cars) {
            var occupancy=Occupancy(section);
            cars=occupancy==TrackOccupancy.Occupied?DestinationCars:Array.Empty<string>();
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
