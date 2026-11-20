using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class OnDemandRouteTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static bool RejectTab(Type type){try{AutomationHooks.VehicleTabTarget(type);return false;}catch(MissingMethodException){return true;}}
    static TrackSection S(string id)=>new TrackSection{Segment=id,From=0,To=100};
    static RouteDefinition Route()=>new RouteDefinition{Code="existing",Entry="A",Exit="B",Sections=new List<TrackSection>{S("track")},
        Approach=new List<TrackSection>{S("approach")},Path=new List<DirectedRouteSection>{new DirectedRouteSection{Section=S("track")}},ViaSignals=new List<RouteViaSignal>()};
    sealed class World:IInterlockingWorld {
        public TrackOccupancy State=TrackOccupancy.Clear;
        public bool Valid=true;
        public bool PathValid(RouteDefinition route)=>Valid;
        public TrackOccupancy Occupancy(TrackSection section)=>State;
        public bool SwitchPosition(string id,out bool thrown){thrown=false;return false;}
        public bool SetSwitch(string id,bool thrown)=>false;
    }
    public static void Run() {
        Check(AutomationHooks.VehicleTabTarget()!=null,"vehicle signalling hooks target the separate native tab builder instead of appending to the fixed car panel");
        Check(AutomationHooks.VehicleTabTarget(typeof(LegacyInspector)).Name.StartsWith("<PopulatePanel>",StringComparison.Ordinal)&&
            AutomationHooks.VehicleTabTarget(typeof(CurrentInspector)).Name.StartsWith("<BuildContent>",StringComparison.Ordinal),
            "vehicle tab resolution supports the legacy and actual 0.6.3 callback names with the same exact builder signature");
        Check(RejectTab(typeof(UnknownInspector))&&RejectTab(typeof(WrongBuilderInspector)),
            "unknown callback names or a fixed-panel builder cannot be selected as a vehicle tab hook");
        Check(RejectTab(typeof(AmbiguousInspector)),
            "ambiguous native vehicle tab callbacks fail explicitly instead of patching an arbitrary method");
        var configured=Route();var request=RouteRequestPolicy.ForRequest(configured);
        var repeat=configured.Clone();repeat.Id=Guid.NewGuid().ToString("N");repeat.Code="renamed";
        Check(request.Id==RouteRequestPolicy.ForRequest(repeat).Id&&request.Id!=configured.Id&&configured.Code=="existing",
            "request identity ignores catalogue naming and never mutates the archived definition");
        repeat.ProtectionWaitSeconds=30;
        Check(request.Id!=RouteRequestPolicy.ForRequest(repeat).Id,"changed protection produces a distinct request identity instead of replacing an active snapshot");
        var world=new World();var engine=new InterlockingEngine();engine.RequestChain(new[]{request},world);
        Check(!engine.Active.Single().Permitted,"an on-demand route does not clear a signal before normal staged protection checks");
        for(int i=0;i<4;i++)engine.Tick(world);
        Check(engine.Active.Single().Permitted&&Reject(()=>engine.Request(RouteRequestPolicy.ForRequest(configured),world)),
            "calculated routes use ordinary staged admission and reject a second conflicting request");
        var status=SharedRouteStatus.From(engine.Active.Single());status.Definition=request.Clone();
        var frame=new SharedLiveFrame{Epoch=Guid.NewGuid().ToString("N"),Sequence=1,Runs=new List<SharedRouteStatus>{status}};
        var received=JsonConvert.DeserializeObject<SharedLiveFrame>(JsonConvert.SerializeObject(frame));received.Validate();
        var view=received.Runs.Single().ToRun(null);
        Check(view.Route.Id==request.Id&&view.Route.Path.Single().Section.Key==request.Path.Single().Section.Key&&view.Permitted,
            "clients reconstruct a live calculated route with no persistent catalogue entry");
        status.Definition.Id=Guid.NewGuid().ToString("N");
        bool rejected=false;try{frame.Validate();}catch(System.IO.InvalidDataException){rejected=true;}
        Check(rejected,"a mismatched live definition is rejected before accepting the shared frame");
        foreach(var state in new[]{TrackOccupancy.Occupied,TrackOccupancy.Unknown}) {
            world=new World{State=state};engine=new InterlockingEngine();
            Check(Reject(()=>engine.Request(RouteRequestPolicy.ForRequest(configured),world))&&engine.Active.Count==0,
                "on-demand admission rejects "+state+" track without leaving a route run");
        }
        world=new World{Valid=false};engine=new InterlockingEngine();
        Check(Reject(()=>engine.Request(request,world))&&engine.Active.Count==0,"a changed topology cannot reuse an old calculated authority");
        for(int bit=0;bit<4;bit++)Check(Reject(()=>RouteRequestPolicy.RequireModeChange(bit==0,bit==1,bit==2,bit==3)),
            "route-mode change is blocked by active resource category "+bit);
        RouteRequestPolicy.RequireModeChange(false,false,false,false);
        var legacy=JsonConvert.DeserializeObject<AutomationConfiguration>("{}");var disabled=new AutomationConfiguration{OnDemandRoutes=false};
        Check(legacy.OnDemandRoutes&&!JsonConvert.DeserializeObject<AutomationConfiguration>(JsonConvert.SerializeObject(disabled)).OnDemandRoutes,
            "old installations default to on-demand calculation while an explicit fallback survives persistence");
        double now=0;var buttons=new PanelButtons(()=>now);buttons.Press("A",id=>true);now=4.9;
        Check(!buttons.ExpireSelection()&&buttons.Start=="A","start selection remains valid before its five-second deadline");now=5;
        Check(buttons.ExpireSelection()&&buttons.Start==null,"only an incomplete start selection expires after five seconds");
        buttons.Press("A",id=>true);buttons.Press("B",id=>true);now=100;
        Check(!buttons.ExpireSelection()&&buttons.End=="B","a complete start-destination request is not cancelled by the selection timer");
        TargetedSearch();
    }
    sealed class LegacyInspector {
        Action<UI.Builder.UITabbedPanelBuilder> callback;int calls;
        void PopulatePanel(){callback=b=>calls++;}
    }
    sealed class CurrentInspector {
        Action<UI.Builder.UITabbedPanelBuilder> callback;int calls;
        void BuildContent(){callback=b=>calls++;}
    }
    sealed class UnknownInspector {
        Action<UI.Builder.UITabbedPanelBuilder> callback;int calls;
        void OtherPanel(){callback=b=>calls++;}
    }
    sealed class WrongBuilderInspector {
        Action<UI.Builder.UIPanelBuilder> callback;int calls;
        void BuildContent(){callback=b=>calls++;}
    }
    sealed class AmbiguousInspector {
        Action<UI.Builder.UITabbedPanelBuilder> first,second;int calls;
        void BuildContent(){first=b=>calls++;second=b=>calls--;}
    }
    static void TargetedSearch() {
        var graph=new NetworkInput();
        for(int i=0;i<=13;i++)graph.Nodes.Add(new PlanNode{Id="N"+i,Point=i>0&&i<13,Common="M"+(i-1),Normal="M"+i});
        for(int i=0;i<13;i++)graph.Edges.Add(new PlanEdge{Id="M"+i,A="N"+i,B="N"+(i+1),Length=10});
        for(int i=1;i<=12;i++)for(int j=0;j<110;j++) {
            string end="B"+i+"-"+j;graph.Nodes.Add(new PlanNode{Id=end});
            graph.Edges.Add(new PlanEdge{Id="spur"+i+"-"+j,A=j==0?"N"+i:"B"+i+"-"+(j-1),B=end,Length=10});
        }
        var start=new MarkerRecord{Id="A",Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="M0",Meters=1}};
        var target=new MarkerRecord{Id="Z",Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="M12",Meters=9}};
        var signals=new[]{start,target};var scanner=new RouteDiscovery(graph,signals);var report=new RouteSearchReport();
        scanner.Find(start,RouteKind.Train,new HashSet<string>{target.Id},report:report,includeAlternatives:true);
        Check(report.LimitReached,"fixture reproduces broad route search exhausting its budget on unrelated yard branches");
        var allowed=RouteTargetScope.Build(graph,target);
        Check(allowed.Contains(RouteTargetScope.Key("M0",false))&&!allowed.Contains(RouteTargetScope.Key("spur1-0",false)),"target scope includes the legal main corridor and excludes branches leading away from the destination");
        var plans=AutomaticRoutePlanner.Find(graph,signals,start,target,RouteKind.Train);
        Check(plans.Count==1&&plans[0].Count==1&&plans[0][0].Switches.Count==12&&plans[0][0].Path.All(p=>p.Section.Segment.StartsWith("M")),"targeted planning finds the direct route without raising search limits or silently truncating alternatives");
        var intermediate=new MarkerRecord{Id="I",Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="M6",Meters=5}};
        plans=AutomaticRoutePlanner.Find(graph,signals.Append(intermediate),start,target,RouteKind.Train);
        Check(plans.Single().Count==2&&plans[0][0].Exit==intermediate.Id&&plans[0][1].Entry==intermediate.Id,"target pruning still splits the path at every intervening main signal");
        graph.Nodes.Single(n=>n.Id=="N6").Common="missing";
        Check(AutomaticRoutePlanner.Find(graph,signals,start,target,RouteKind.Train).Count==0,"incomplete turnout metadata cannot invent a traversable connection during target pruning");
        graph.Nodes.Single(n=>n.Id=="N6").Common="M5";target.FacingReverse=true;
        Check(AutomaticRoutePlanner.Find(graph,signals,start,target,RouteKind.Train).Count==0,"target scope respects the destination signal's direction");
        target.FacingReverse=false;report=new RouteSearchReport();scanner.Find(start,RouteKind.Train,new HashSet<string>{target.Id},workLimit:2,report:report,allowedDirections:RouteTargetScope.Build(graph,target));
        Check(report.LimitReached,"a genuinely insufficient relevant search budget still rejects the result");
    }
}
