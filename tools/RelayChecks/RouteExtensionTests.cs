using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;

public static class RouteExtensionTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static TrackSection S(string id,double a,double b)=>new TrackSection{Segment=id,From=a,To=b};
    sealed class World:IInterlockingWorld,ISectionalWorld {
        public readonly List<TrackSection> Occupied=new List<TrackSection>(),Unknown=new List<TrackSection>();
        public bool Switch=true,Known=true,Capture=true;
        public double Head=-1,Tail=-50;
        public bool PathValid(RouteDefinition route)=>true;
        public TrackOccupancy Occupancy(TrackSection section)=>Unknown.Any(s=>s.Overlaps(section))?TrackOccupancy.Unknown:Occupied.Any(s=>s.Overlaps(section))?TrackOccupancy.Occupied:TrackOccupancy.Clear;
        public bool SwitchPosition(string id,out bool thrown){thrown=Switch;return id=="W1";}
        public bool SetSwitch(string id,bool thrown){Switch=thrown;return true;}
        public bool CaptureTrain(RouteDefinition route,out string[] cars){cars=Capture?new[]{"loco","tail"}:null;return Capture;}
        public bool TrainProgress(RouteDefinition route,string[] cars,out double head,out double tail){head=Head;tail=Tail;return Known;}
    }
    static RouteDefinition Route(bool shunting=false) {
        var route=new RouteDefinition{Code="F1",Entry="A",Exit="B",Shunting=shunting,Sections=new List<TrackSection>{S("S1",0,100),S("S2",0,100),S("S1",75,100),S("S2",0,25),S("side",0,25)},Approach=new List<TrackSection>{S("approach",0,30)},Path=new List<DirectedRouteSection>{new DirectedRouteSection{Section=S("S1",0,100)},new DirectedRouteSection{Section=S("S2",0,100)}},Switches=new List<RouteSwitch>{new RouteSwitch{Node="W1",Thrown=true,ReleaseAt=100,Protection=new List<TrackSection>{S("S1",75,100),S("S2",0,25),S("side",0,25)}}},ViaSignals=new List<RouteViaSignal>()};
        RouteSignals.SplitProtection(route);route.Validate();return route;
    }
    static MarkerRecord Signal(string id,string kind,string segment,double meters,bool reverse=false)=>new MarkerRecord{Id=id,Name=id,Kind=kind,FacingReverse=reverse,Anchor=new MarkerAnchor{Segment=segment,Meters=meters}};
    static void Ready(InterlockingEngine engine,RouteDefinition route,IInterlockingWorld world) {
        engine.Request(route,world);for(int i=0;i<8&&engine.Active.Any(r=>r.Route.Id==route.Id&&r.Stage==RouteStage.Preparing);i++)engine.Tick(world);
        var run=engine.Active.Single(r=>r.Route.Id==route.Id);if(run.Stage==RouteStage.Fault)throw new InvalidOperationException(run.Reason);
        if(!run.Permitted)throw new InvalidOperationException("Fixture route did not finish its normal staged checks.");
    }
    public static void Run(string root) {
        var route=Route();Check(route.Sections.Count==5&&route.Path.Count==4&&route.Path.Sum(p=>p.Section.To-p.Section.From)==200,"point protection splits reservations without changing the actual directed route length");
        var rs=Signal("RS1",MarkerRecord.Shunt,"S2",50);var opposite=Signal("RS2",MarkerRecord.Shunt,"S2",60,true);var neighbor=Signal("RS3",MarkerRecord.Shunt,"side",10);
        route.ViaSignals=RouteSignals.Bind(route.Path,new[]{rs,opposite,neighbor},"A","B");
        Check(route.ViaSignals.Count==1&&route.ViaSignals[0].Id=="RS1"&&route.ViaSignals[0].Distance==150,"only same-direction shunt signals on the traversed path join the route; opposing and flank-guard signals stay stop");
        Check(Reject(()=>RouteSignals.Bind(route.Path,new[]{Signal("HS",MarkerRecord.Main,"S2",60)},"A","B")),"an intermediate main signal requires a separate route instead of authorizing a train through Hp 0");
        var engine=new InterlockingEngine();var world=new World();Ready(engine,route,world);Check(engine.ForViaSignal("RS1").PermitsVia("RS1")&&engine.UsesSignal("RS1"),"through shunt signal receives authority and remains protected against editing");
        world.Head=20;world.Tail=-50;world.Occupied.Add(S("S1",0,20));engine.Tick(world);
        Check(!engine.ForSignal("A").Permitted&&engine.ForViaSignal("RS1").PermitsVia("RS1"),"entry falls to stop after entry while the unpassed downstream shunt remains at Sh 1");
        world.Head=160;world.Tail=40;world.Occupied.Clear();world.Occupied.Add(S("S1",40,100));world.Occupied.Add(S("S2",0,60));engine.Tick(world);
        Check(!engine.ForViaSignal("RS1").PermitsVia("RS1")&&engine.Locked("W1",false),"passed shunt falls to stop while the train tail still protects the point");
        world.Head=190;world.Tail=130;world.Occupied.Clear();world.Occupied.Add(S("S2",30,90));engine.Tick(world);
        Check(engine.Locked("W1",false),"one clear observation after known tail passage does not release a point");
        engine.Tick(world);Check(!engine.Locked("W1",false)&&engine.Active.Count==1,"point becomes movable after the full train clears its protected area, before reaching the next main signal");
        engine.Tick(world);Check(engine.Active.Single().ReleasedSections.Count>=3&&engine.Active.Single().HeldSections.All(s=>s.Segment=="S2"),"released rear track and flank protection no longer reserve the entire original route");
        var following=Route();following.Id=Guid.NewGuid().ToString("N");following.Code="F2";following.Entry="C";following.Exit="D";following.Sections=following.Sections.Where(s=>s.Segment!="S2").ToList();following.Path=following.Path.Where(p=>p.Section.Segment=="S1").ToList();following.Switches.Clear();following.ViaSignals.Clear();
        Ready(engine,following,world);Check(engine.Active.Count==2,"a following nonconflicting route can reuse released rear sections while the first train remains farther ahead");
        world.Switch=false;engine.Tick(world);Check(engine.Active.First(r=>r.Route.Id==route.Id).Stage==RouteStage.Running,"changing a released point does not invalidate the first train's remaining forward protection");
        engine=new InterlockingEngine();world=new World();route=Route();Ready(engine,route,world);world.Head=150;world.Tail=130;world.Occupied.Add(S("S2",30,50));engine.Tick(world);world.Known=false;engine.Tick(world);world.Known=true;engine.Tick(world);
        Check(engine.Locked("W1",false),"unknown train-tail data interrupts the consecutive release checks and never unlocks a point");
        engine.Tick(world);Check(!engine.Locked("W1",false),"two new valid clear observations can release the point after tail data recovers");
        engine=new InterlockingEngine();world=new World();route=Route(true);world.Occupied.Add(S("S2",50,80));Ready(engine,route,world);engine.Tick(world);
        Check(engine.ForSignal("A").Permitted&&!engine.ForSignal("A").Entered,"Sh 1 can be requested into a known occupied target without mistaking its stationary cars for the approaching train");
        world.Head=5;world.Tail=-20;engine.Tick(world);Check(!engine.ForSignal("A").Permitted&&engine.Locked("W1",false),"shunt entry falls to stop after its own train passes while the point stays locked until the tail clears");
        engine=new InterlockingEngine();world=new World();world.Occupied.Add(S("S2",50,80));Check(Reject(()=>Ready(engine,Route(false),world)),"main-signal train routes still reject occupied target tracks");
        world.Occupied.Clear();world.Occupied.Add(S("S2",0,10));Check(Reject(()=>Ready(engine,Route(true),world)),"shunting into an occupied point protection area remains prohibited");
        world.Occupied.Clear();world.Unknown.Add(S("S2",50,80));Check(Reject(()=>Ready(engine,Route(true),world)),"unknown target occupancy never counts as permitted known occupation");
        double progress;route=Route();Check(RouteSignals.Position(route,"S2",30,out progress)&&progress==130,"train progress follows ordered track intervals rather than world-space proximity");
        route.Path.Add(new DirectedRouteSection{Section=S("S2",0,100)});Check(!RouteSignals.Position(route,"S2",30,out progress),"ambiguous repeated track in a loop cannot prove tail passage");
    }
}
