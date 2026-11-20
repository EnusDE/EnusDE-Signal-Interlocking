using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;

static class PassageOccupancyTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static TrackSection S(string id,double a,double b)=>new(){Segment=id,From=a,To=b};
    sealed class World:IInterlockingWorld,ISectionalWorld,IVehicleOccupancyWorld {
        public double Head=-2,Tail=-20;public bool Occupied,Foreign,Unknown;public string Segment="S";public TrackSection OccupiedRange;
        public bool PathValid(RouteDefinition route)=>true;
        public TrackOccupancy Occupancy(TrackSection section)=>section.Segment!=Segment?TrackOccupancy.Clear:Unknown?TrackOccupancy.Unknown:Occupied&&(OccupiedRange==null||OccupiedRange.Overlaps(section))?TrackOccupancy.Occupied:TrackOccupancy.Clear;
        public TrackOccupancy SectionWithCars(TrackSection section,out string[] cars){var state=Occupancy(section);cars=state==TrackOccupancy.Unknown?null:state==TrackOccupancy.Occupied?new[]{Foreign?"other":"own"}:Array.Empty<string>();return state;}
        public bool SwitchPosition(string id,out bool position){position=false;return true;}
        public bool SetSwitch(string id,bool position)=>true;
        public bool CaptureTrain(RouteDefinition route,out string[] cars){cars=new[]{"own"};return true;}
        public bool TrainProgress(RouteDefinition route,string[] cars,out double head,out double tail){head=Head;tail=Tail;return true;}
    }
    static RouteDefinition Route(bool shunt=false)=>new(){Code="R",Entry="A",Exit="B",Shunting=shunt,Sections=new(){S("S",0,100)},Approach=new(){S("rear",0,30)},Path=new(){new DirectedRouteSection{Section=S("S",0,100)}},ViaSignals=new()};
    static void Ready(InterlockingEngine engine,RouteDefinition route,World world){engine.Request(route,world);for(int i=0;i<5;i++)engine.Tick(world);}
    public static void Run() {
        var index=new VehicleOccupancyIndex(new[]{new VehicleTrackFootprint{Id="front",Sections=new(){S("S",30,50)}},new VehicleTrackFootprint{Id="parallel",Sections=new(){S("parallel",0,100)}}});
        Check(index.Read(S("S",19.921890258789,27.908390045166),out var cars)==TrackOccupancy.Clear&&cars.Length==0,
            "the reported Sm3i9-sized interval stays clear when vehicle endpoints lie farther along the same segment");
        Check(index.Read(S("S",35,40),out cars)==TrackOccupancy.Occupied&&cars.SequenceEqual(new[]{"front"}),"numeric occupancy reports actual overlap and excludes a parallel track's vehicle");
        Check(index.Read(S("S",10,30),out _)==TrackOccupancy.Clear&&index.Read(S("S",50,60),out _)==TrackOccupancy.Clear,
            "touching an interval boundary does not occupy the neighbouring interval before the vehicle crosses it");
        index=new VehicleOccupancyIndex(new[]{new VehicleTrackFootprint{Id="spanning",Sections=new(){S("left",90,100),S("right",0,12)}}});
        Check(index.Read(S("left",95,100),out cars)==TrackOccupancy.Occupied&&index.Read(S("right",0,5),out _)==TrackOccupancy.Occupied,
            "a vehicle across a segment boundary occupies both physical sections until its tail clears");
        index=new VehicleOccupancyIndex(new[]{new VehicleTrackFootprint{Id="unreadable",UnknownSegments=new(){"S"}}});
        Check(index.Read(S("S",0,10),out cars)==TrackOccupancy.Unknown&&cars==null&&index.Read(S("other",0,10),out _)==TrackOccupancy.Clear,
            "a known segment with unreadable vehicle geometry is unknown while unrelated tracks remain readable");
        index=new VehicleOccupancyIndex(new[]{new VehicleTrackFootprint{Id="lost",GlobalUnknown=true}});
        Check(index.Read(S("S",0,10),out _)==TrackOccupancy.Unknown,"unlocatable vehicle geometry cannot manufacture a clear-track reading");

        var engine=new InterlockingEngine{SignalPassageMeters=5};var world=new World();var route=Route();Ready(engine,route,world);
        world.Occupied=true;engine.Tick(world);var run=engine.ForSignal("A");
        Check(run.Permitted&&!run.Entered&&!run.StartPassageVerified,"occupation alone no longer infers entry while the assigned train's front remains behind the signal");
        world.Head=4.99;engine.Tick(world);Check(run.Permitted&&!run.StartPassageVerified,"signal and number remain unchanged before the configured five-metre passage point");
        world.Head=5.01;engine.Tick(world);Check(!run.Permitted&&run.StartPassageVerified&&run.Stage==RouteStage.Running,"crossing the same directional five-metre point both drops the signal and verifies number passage");
        var numbers=new Dictionary<string,string>{{"A","00123"}};var completed=new HashSet<string>();
        Check(TrainNumbers.AdvanceSignalNumbers(numbers,new[]{run},completed:completed)&&numbers.ContainsKey("B")&&completed.Contains(route.Id),"verified halt-fall passage advances the number without waiting for arrival or train tail");
        engine=new(){SignalPassageMeters=5};world=new World();Ready(engine,Route(),world);world.Occupied=world.Foreign=true;engine.Tick(world);
        Check(engine.ForSignal("A").Stage==RouteStage.Fault&&!engine.ForSignal("A").Permitted,"an unrelated train intruding ahead still removes authority rather than being treated as the assigned train's passage");
        engine=new(){SignalPassageMeters=5};world=new World();Ready(engine,Route(),world);world.Unknown=true;engine.Tick(world);
        Check(engine.ForSignal("A").Stage==RouteStage.Fault&&!engine.ForSignal("A").Permitted,"unknown route occupancy still drops authority immediately instead of waiting for the passage offset");
        var reversed=Route();reversed.Path[0].Reverse=true;
        Check(RouteSignals.Position(reversed,"S",94,out double head)&&head==6&&RouteSignals.Position(reversed,"S",106,out _)==false,
            "reverse-direction passage uses progress from that route's start rather than increasing world metres");
        engine=new(){SignalPassageMeters=10};world=new World();var shortRoute=Route();shortRoute.Path[0].Section.To=4;Ready(engine,shortRoute,world);world.Head=2.01;engine.Tick(world);
        Check(!engine.ForSignal("A").Permitted&&engine.ForSignal("A").StartPassageVerified,"short routes cap the configured passage offset at half the traversed distance");

        var yard=Route(true);yard.Sections=new(){S("S",0,40),S("S",40,60),S("S",60,100)};yard.Path=yard.Sections.Select(s=>new DirectedRouteSection{Section=s}).ToList();
        yard.Switches.Add(new RouteSwitch{Node="W",Thrown=false,ReleaseAt=50,Protection=new(){S("S",40,60)}});
        Check(RouteSignals.ShuntOccupationAllowed(yard,S("S",0,40))&&RouteSignals.ShuntOccupationAllowed(yard,S("S",60,100))&&!RouteSignals.ShuntOccupationAllowed(yard,S("S",40,60)),
            "known shunt occupancy is allowed before and after points but never in a point's protected area");
        Check(!RouteSignals.TargetContains(yard,S("S",0,40)),"allowing an occupied intermediate shunt section does not turn it into an early destination-release zone");
        engine=new();world=new World{Occupied=true,Foreign=true,OccupiedRange=S("S",10,20)};Ready(engine,yard,world);
        Check(engine.ForSignal("A").Permitted,"staged shunt admission permits a known occupied intermediate track while its point guards remain clear");
        engine=new();world=new World{Occupied=true,OccupiedRange=S("S",45,55)};bool rejected=false;try{engine.Request(yard,world);}catch(InvalidOperationException){rejected=true;}
        Check(rejected&&engine.Active.Count==0,"the same shunt request is rejected when the actual point protection interval is occupied");

        var circuit=new TrackBlock{Id="c",BoundaryA="a",BoundaryB="b",Path=new(){new DirectedRouteSection{Section=S("S",0,200)}}};
        var station=Route();station.Path[0].Section.From=100;station.Path[0].Section.To=200;
        var intervals=new List<TrackSection>();
        string error=StationRouteProtection.CircuitError(new[]{station},new[]{circuit},_=>true,_=>TrackOccupancy.Occupied,_=>"A/B",s=>{intervals.Add(s);return s.From<100?TrackOccupancy.Occupied:TrackOccupancy.Clear;});
        Check(error==null&&intervals.Count==1&&intervals[0].From==100,"a continuous A/B section extending behind the start checks only the actual requested route interval");
        Check(StationRouteProtection.CircuitError(new[]{station},new[]{circuit},_=>true,_=>TrackOccupancy.Clear,_=>"A/B",_=>TrackOccupancy.Occupied)!=null,
            "a genuinely occupied part ahead is still rejected after clipping the extended A/B circuit");
    }
}
