using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;

public static class AutomationTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static DirectedRouteSection Piece(string segment,double a=0,double b=100,bool reverse=false)=>new DirectedRouteSection{Section=new TrackSection{Segment=segment,From=a,To=b},Reverse=reverse};
    static RouteDefinition Route(string code,string entry,string exit,params DirectedRouteSection[] path)=>new RouteDefinition{Code=code,Entry=entry,Exit=exit,Path=path.ToList(),ViaSignals=new List<RouteViaSignal>(),Sections=path.Select(p=>p.Section.Clone()).ToList(),Approach=new List<TrackSection>{new TrackSection{Segment="approach",From=0,To=50}}};
    sealed class World:IInterlockingWorld {
        public string Blocked;
        public bool PathValid(RouteDefinition r)=>true;
        public TrackOccupancy Occupancy(TrackSection s)=>s.Segment==Blocked?TrackOccupancy.Occupied:TrackOccupancy.Clear;
        public bool SwitchPosition(string id,out bool thrown){thrown=false;return true;}
        public bool SetSwitch(string id,bool value)=>true;
    }
    public static void Run() {
        AutomationTopologyTests.Run();
        var config=new AutomationConfiguration();config.Validate();
        Check(!config.Enabled&&!config.BlockEnabled&&config.ProtectWaypointSwitches&&config.Role("L1","c")==MovementRole.Unassigned,"automation and directional blocks default off; unassigned locomotives cannot become contact masters");
        config.Assign("L1",new[]{"L1","L2"},"consist",MovementRole.Train);
        Check(config.Role("L1","consist")==MovementRole.Train&&config.Role("L2","consist")==MovementRole.Unassigned,"selecting one locomotive resets other coupled locomotives to unassigned");
        Check(config.Role("L1","changed")==MovementRole.Unassigned,"a changed physical consist invalidates the previous train role");
        config.Assign("L2",new[]{"L1","L2"},"consist",MovementRole.Shunt);
        Check(config.Role("L1","consist")==MovementRole.Unassigned&&config.Role("L2","consist")==MovementRole.Shunt,"shunting is an explicit distinct role and does not retain another locomotive's train role");
        Check(Reject(()=>config.Assign("absent",new[]{"L1","L2"},"consist",MovementRole.Train))&&config.Role("L2","consist")==MovementRole.Shunt,"an absent master rejects without partly changing the coupled role assignment");
        config.Enabled=true;Check(Reject(config.Validate),"contact automation cannot be enabled without common block protection");config.Enabled=false;
        var contact=new AutomationContact{Anchor=new MarkerAnchor{Segment="S",Meters=10}};
        Check(!contact.Enabled,"new track contacts require explicit activation");contact.Enabled=true;
        Check(Reject(contact.Validate),"active contacts must name their controlling signal");contact.Enabled=false;
        config.Contacts.Add(contact);config.Validate();var serialized=JsonConvert.DeserializeObject<AutomationConfiguration>(JsonConvert.SerializeObject(config));
        Check(serialized.Contacts.Single().Id==contact.Id&&serialized.Locomotives["L2"].Role==MovementRole.Shunt,"contacts, direction and explicit locomotive roles survive shared JSON serialization");
        var legacy=JsonConvert.DeserializeObject<DeskLibrary>("{\"WorldKey\":\"w\",\"Desks\":[{\"Global\":true,\"Code\":\"ESTW\",\"Name\":\"Global\",\"Panel\":{\"WorldKey\":\"w\"}}]}");legacy.Validate("w");
        Check(legacy.Desks.Single().ShowInDispatcher&&!legacy.Automation.Enabled,"old desk documents retain dispatcher visibility and load with automation disabled");
        legacy.Automation=config;var clone=legacy.Clone();clone.Automation.Contacts[0].Name="other";clone.Automation.Locomotives["L2"].Role=MovementRole.Train;
        Check(legacy.Automation.Contacts[0].Name!="other"&&legacy.Automation.Locomotives["L2"].Role==MovementRole.Shunt,"automation drafts deeply isolate nested contacts and locomotive roles");
        var tracker=new PointPassageTracker();Check(tracker.Observe("axle",-.2,0)==0&&tracker.Observe("axle",0,.1)==0&&tracker.Observe("axle",.2,.2)==1,"axle observations count a real forward crossing through the hysteresis band once");
        Check(tracker.Observe("axle",.25,.2)==0&&tracker.Observe("axle",.5,.3)==0,"duplicate time reads and repeated positions do not add axle passages");
        Check(tracker.Observe("axle",-.2,.4)==-1,"reverse axle crossing changes the signed balance");
        Check(tracker.Observe("axle",200,.5)==0&&tracker.Observe("axle",-.2,4)==0,"teleportation and missing observation intervals reseed instead of inventing axle events");
        tracker.Observe("gap",-.2,0);tracker.Observe("gap",0,3);Check(tracker.Observe("gap",.2,3.1)==0,"a gap ending in the hysteresis band does not reuse the pre-gap side");
        tracker.Prune(20);Check(tracker.Observe("axle",.2,20)==0,"expired contact samples do not produce crossings when the train returns");
        var links=new[]{new DirectionBlockLink{Id="first",A="stationA",B="block",Path=new List<DirectedRouteSection>{Piece("S1")}},
            new DirectionBlockLink{Id="second",A="stationB",B="block",Path=new List<DirectedRouteSection>{Piece("S2",0,100,true)}}};
        var groups=DirectionBlockBuilder.Build(links,new[]{"stationA","stationB"});
        Check(groups.Count==1&&groups[0].Error==null&&groups[0].Axis.All(p=>!p.Reverse),"oppositely stored neighboring GFM sections form one continuous single-track direction across a block signal");
        var duplicates=new[]{new DirectionBlockLink{Id="one",A="a",B="b",Path=new List<DirectedRouteSection>{Piece("dup")}},new DirectionBlockLink{Id="two",A="b",B="a",Path=new List<DirectedRouteSection>{Piece("dup",0,100,true)}}};
        var same=DirectionBlockBuilder.Build(duplicates,new string[0]);
        Check(same.Count==1&&same[0].Error==null&&same[0].Axis.Count==1,"oppositely stored duplicate GFM paths share one physical direction axis instead of inventing a contradiction");
        Check(DirectionBlockBuilder.Build(links,new[]{"block"}).Count==2,"an entry or exit station boundary ends a common directional chain");
        var divided=DirectionBlockBuilder.Split(new DirectionBlockLink{Id="whole",A="a",B="b",Path=new List<DirectedRouteSection>{Piece("S",0,100,true)}},new[]{new MarkerAnchor{Segment="S",Meters=40}});
        Check(divided.Count==2&&divided[0].Path[0].Section.From==40&&divided[1].Path[0].Section.To==40&&DirectionBlockBuilder.Build(divided,new[]{"S@40.00"}).Count==2,"station boundaries inside existing GFM paths split the directional chain without reversing or losing track coverage");
        var fork=links.Concat(new[]{new DirectionBlockLink{Id="third",A="block",B="c",Path=new List<DirectedRouteSection>{Piece("S3")}}});
        Check(DirectionBlockBuilder.Build(fork,new string[0]).Any(b=>b.Error!=null),"a branched boundary is not guessed to be an unambiguous single-track block chain");
        Check(DirectionBlockBuilder.Direction(new[]{Piece("S1",0,10,true)},groups[0].Axis)==-1,"route direction is determined from directed physical sections rather than signal labels");
        Check(Reject(()=>DirectionBlockBuilder.Direction(new[]{Piece("S1"),Piece("S2",0,100,true)},groups[0].Axis)),"a route that reverses inside one protected chain is rejected");
        Check(DirectionBlockBuilder.Covers(new[]{Piece("S1",10,90)},groups)&&!DirectionBlockBuilder.Covers(new[]{Piece("missing")},groups),"contact auto-plans require physical monitored coverage for every requested interval");
        var authority=new DirectionBlockAuthority{Enabled=true,Blocks=groups,Occupancy=s=>TrackOccupancy.Clear};
        var forward=Route("F1","A","B",Piece("S1",0,50));var following=Route("F2","B","C",Piece("S2",0,50));var reverse=Route("G1","Z","Y",Piece("S2",50,100,true));
        using(var reservation=authority.Reserve(new[]{forward})){Check(authority.Leases.Count==1,"reservation binds the complete block chain before requesting its route");}
        Check(authority.Leases.Count==0,"uncommitted route admission rolls back its direction reservation");
        using(var reservation=authority.Reserve(new[]{forward}))reservation.Commit();
        Check(Reject(()=>authority.Reserve(new[]{reverse}))&&authority.Leases.Count==1,"an opposing request is denied even when its own local section does not overlap the existing route");
        using(var reservation=authority.Reserve(new[]{following}))reservation.Commit();
        Check(authority.Leases.Single().Routes.Count==2,"same-direction following requests share the direction while ordinary engine checks remain responsible for local separation");
        authority.Observe(new[]{new RouteRun{Route=forward,Stage=RouteStage.Cancelled}},1);authority.Observe(new RouteRun[0],2);authority.Observe(new RouteRun[0],2);
        Check(authority.Leases.Single().ClearChecks==1,"a retained cancelled route holds direction and repeated reads at one time cannot count as multiple clear samples");
        authority.Occupancy=s=>TrackOccupancy.Unknown;authority.Observe(new RouteRun[0],3);authority.Observe(new RouteRun[0],4);
        Check(authority.Leases.Count==1&&authority.Leases.Single().ClearChecks==0,"unknown occupancy retains the direction and resets its full-chain clear proof");
        authority.Occupancy=s=>throw new Exception("lost");authority.Observe(new RouteRun[0],5);
        Check(authority.Leases.Count==1,"read failures retain a block lease instead of dropping protection");
        authority.Occupancy=s=>TrackOccupancy.Clear;authority.Observe(new RouteRun[0],6);authority.Observe(new RouteRun[0],7);authority.Observe(new RouteRun[0],8);
        Check(authority.Leases.Count==0,"direction releases only after all routes are gone and three distinct full-chain clear samples");
        authority.Occupancy=s=>s.Segment=="S2"?TrackOccupancy.Occupied:TrackOccupancy.Clear;
        Check(Reject(()=>authority.Reserve(new[]{forward}))&&authority.Leases.Count==0,"an occupied distant block prevents a new direction despite a clear departure section");
        authority.Occupancy=s=>TrackOccupancy.Clear;var bad=Route("bad","a","b",Piece("S1"));bad.Path=null;
        Check(Reject(()=>authority.Reserve(new[]{bad})),"legacy routes without directed paths cannot bypass enabled direction admission");
        var engine=new InterlockingEngine{Admission=authority};var world=new World{Blocked="S2"};
        var second=Route("second","B","C",Piece("S2"));
        Check(Reject(()=>engine.RequestChain(new[]{forward,second},world))&&engine.Active.Count==0&&authority.Leases.Count==0,"failure in the last subroute atomically removes all new subroutes and their shared direction reservation");
        world.Blocked=null;engine.RequestChain(new[]{forward,second},world);
        Check(engine.Active.Count==2&&authority.Leases.Count==1&&engine.Active.All(r=>!r.Permitted),"a complete accepted route chain shares one direction reservation and stays stop during native alignment checks");
        var plan=new RouteChainPlan{Routes=new List<RouteDefinition>{forward,second,Route("third","C","D",Piece("S3"))}};var rule=new AutomaticSignal{Primary="D"};
        Check(Reject(()=>AutomationPlanPolicy.Limit(plan,rule,new AutomationIntent{Mode=AutomationDrivingMode.Yard})),"native yard mode never requests an automatic train route");
        var intent=new AutomationIntent{Mode=AutomationDrivingMode.Waypoint,Target=new MarkerAnchor{Segment="S2",Meters=25},Segments=new List<string>{"S1","S2","S3"}};
        Check(AutomationPlanPolicy.Limit(plan,rule,intent).Routes.Count==2,"a waypoint within the second target section prevents clearing unnecessary following signals");
        var wrong=Route("wrong","A","B",Piece("wrong-track"));var right=Route("right","A","B",Piece("S1"));
        right.Switches.Add(new RouteSwitch{Node="W",Thrown=true});wrong.Switches.Add(new RouteSwitch{Node="W",Thrown=false});
        var matching=AutomationPlanPolicy.MatchingPlans(new[]{new RouteChainPlan{Routes=new(){wrong}},new RouteChainPlan{Routes=new(){right}}},rule,new AutomationIntent{Mode=AutomationDrivingMode.Waypoint,Target=new MarkerAnchor{Segment="S1",Meters=50},Segments=new(){"S1"}});
        Check(matching.Count==1&&matching[0].Routes[0].Id==right.Id&&matching[0].Routes[0].Switches[0].Thrown,"waypoint route matching selects the requested branch before preferring the currently lined straight route");
        intent.Target=new MarkerAnchor{Segment="S1",Meters=0};Check(Reject(()=>AutomationPlanPolicy.Limit(plan,rule,intent)),"a waypoint at or before the start signal never triggers through-clearing");
        intent.Target=new MarkerAnchor{Segment="other",Meters=25};intent.Segments=new List<string>{"other"};Check(Reject(()=>AutomationPlanPolicy.Limit(plan,rule,intent)),"a waypoint on a neighboring track is not inferred to be an onward mainline journey");
        intent.Target=new MarkerAnchor{Segment="S2",Meters=25};intent.Segments=new List<string>{"S1","S2","S1"};Check(Reject(()=>AutomationPlanPolicy.Limit(plan,rule,intent)),"ambiguous looping waypoint paths are not guessed by segment identity");
        rule.AllowWaypoint=false;Check(Reject(()=>AutomationPlanPolicy.Limit(plan,rule,intent)),"per-signal waypoint exclusion is respected by the actual auto-plan policy");
        var frame=new AutomationLiveFrame{Requests=new List<AutomationStatus>{new AutomationStatus{Reason=new string('x',513)}}};
        Check(Reject(frame.Validate),"shared automation live frames enforce bounded diagnostic data");
    }
}
