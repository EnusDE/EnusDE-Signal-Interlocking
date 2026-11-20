using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;

static class OperatingTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static string Id()=>Guid.NewGuid().ToString("N");
    sealed class World:IInterlockingWorld {
        public int Writes;public bool Position;
        public bool PathValid(RouteDefinition route)=>true;
        public TrackOccupancy Occupancy(TrackSection section)=>TrackOccupancy.Clear;
        public bool SwitchPosition(string id,out bool position){position=Position;return true;}
        public bool SetSwitch(string id,bool position){Writes++;Position=position;return true;}
    }
    public static void Run() {
        string a=Id(),b=Id(),back=Id(),unbound=Id(),key=new string('f',64);
        var library=new DeskLibrary{WorldKey=key};
        var master=new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey=key}};
        var local=new DeskDefinition{Code="WHB",Name="Whittier",Panel=new PanelDocument{WorldKey=key}};
        var other=new DeskDefinition{Code="ELA",Name="Ela",Panel=new PanelDocument{WorldKey=key}};
        library.Desks.AddRange(new[]{master,local,other});
        local.Panel.Tiles.Add(new PanelTile{Kind=PanelTileKind.Signal,X=1,Y=1,Reference=a});other.Panel.Tiles.Add(new PanelTile{Kind=PanelTileKind.Signal,X=1,Y=1,Reference=b});
        local.Panel.Tiles.Add(new PanelTile{Kind=PanelTileKind.Switch,X=3,Y=1,Reference="W1"});library.Points["W1"]=new PointAssignment{Code="W001",Desk=local.Id,ElectricStand=true};
        OperatingStateIndex Index()=>new OperatingStateIndex(library,Array.Empty<RouteDefinition>());
        Check(Index().Signal(unbound)==SignalOperatingState.Unbound&&Index().Signal(a)==SignalOperatingState.Active&&!Index().PointReleased("W1"),"only bound signals are commissioned and in-service electric points retain AI protection");
        library.Automation.PausedStations.Add(local.Id);local.ShowInDispatcher=false;local.AutomaticOnly=true;local.Acceptance=StationAcceptance.Closed;
        Check(Index().Signal(a)==SignalOperatingState.Active&&!Index().PointReleased("W1"),"visibility, acceptance and automation pause cannot silently disable signals or release points");
        library.Automation.Signals[a]=new AutomaticSignal{OutOfService=true};
        Check(Index().Signal(a)==SignalOperatingState.OutOfService&&!Index().PointReleased("W1")&&Index().Signal(unbound)==SignalOperatingState.Unbound,"individual signal shutdown shows its own disabled state without releasing the area's points or commissioning unrelated signals");
        library.Automation.Signals[a].OutOfService=false;local.OutOfService=true;
        Check(Index().Signal(a)==SignalOperatingState.OutOfService&&Index().Signal(b)==SignalOperatingState.Active&&Index().PointReleased("W1")&&!Index().PointReleased("unknown"),"an explicit local shutdown releases only its own points and leaves unrelated and unknown areas protected");
        Check(Index().Signal(back,a)==SignalOperatingState.OutOfService,"a separate distant signal can inherit its announced main signal's commissioned area and shutdown state");
        other.Panel.Tiles.Add(new PanelTile{Kind=PanelTileKind.Switch,X=3,Y=1,Reference="W1"});
        Check(!Index().PointReleased("W1"),"a point still used by an active neighbouring interlocking cannot be released for waypoint AI");
        other.Panel.Tiles.RemoveAt(other.Panel.Tiles.Count-1);local.OutOfService=false;
        library.Modules.Add(new DeskModule{Desk=local.Id,Master=master.Id});master.OutOfService=true;
        Check(Index().Signal(a)==SignalOperatingState.OutOfService&&Index().PointReleased("W1")&&Index().Signal(b)==SignalOperatingState.Active,"master shutdown reaches linked locals while an unlinked local interlocking remains in service");
        master.OutOfService=false;
        Check(Index().Signal(a)==SignalOperatingState.Active&&!Index().PointReleased("W1"),"restoring the master reapplies signal authority checks and electric-point AI protection");
        var legacy=new RouteDefinition{Entry=unbound,Exit=back};
        Check(new OperatingStateIndex(library,new[]{legacy}).Signal(unbound)==SignalOperatingState.Active&&Index().Signal(unbound)==SignalOperatingState.Unbound,"existing route-only installations retain their legacy commissioning, while unused placed signals are invalid");
        var round=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(library));round.Desks[1].OutOfService=true;round.Automation.Signals[a].OutOfService=true;
        var clone=round.Clone();clone.Desks[1].OutOfService=false;clone.Automation.Signals[a].OutOfService=false;
        Check(round.Desks[1].OutOfService&&round.Automation.Signals[a].OutOfService&&!library.Desks[1].OutOfService&&!library.Automation.Signals[a].OutOfService,"desk and per-direction shutdown settings persist and are isolated by shared configuration cloning");
        var route=new RouteDefinition{Code="F1",Entry=a,Exit=b,Sections=new(){new TrackSection{Segment="S1",From=0,To=100}},Approach=new(){new TrackSection{Segment="approach",From=0,To=200}},Switches=new(){new RouteSwitch{Node="W1",Thrown=true}}};
        var world=new World();var engine=new InterlockingEngine{Admission=new OperatingRouteAdmission(plan=>Index().RequireRoute(plan,id=>Index().Signal(id)))};
        local.OutOfService=true;
        Check(Reject(()=>engine.Request(route,world))&&engine.Active.Count==0&&world.Writes==0&&Reject(()=>engine.RequestChain(new[]{route},world)),"single and chained routes into an out-of-service area are rejected before any native switch movement or engine reservation");
        Check(Reject(()=>Index().RequireRoute(new[]{route},id=>SignalOperatingState.Active))&&Reject(()=>Index().RequireRoute(new[]{route},id=>SignalOperatingState.Unknown))&&world.Writes==0,"released points and unavailable operating data independently reject route admission even if signal checks are otherwise permissive");
        local.OutOfService=false;engine.Request(route,world);for(int i=0;i<8;i++)engine.Tick(world);
        Check(engine.ForSignal(a)?.Permitted==true&&world.Writes>0,"restoring service allows the existing checked route engine to align and authorize the route normally");
        var run=engine.ForSignal(a);var noSignals=new HashSet<string>();var noPoints=new HashSet<string>();
        Check(OperatingTransitionPolicy.Conflicts(run,new HashSet<string>{a},noPoints,Array.Empty<TrackSection>())&&OperatingTransitionPolicy.Conflicts(run,noSignals,new HashSet<string>{"W1"},Array.Empty<TrackSection>())&&OperatingTransitionPolicy.Conflicts(run,noSignals,noPoints,new[]{new TrackSection{Segment="S1",From=50,To=60}}),"operating transitions refuse affected live signal, point and retained-section locks");
        Check(!OperatingTransitionPolicy.Conflicts(run,new HashSet<string>{unbound},new HashSet<string>{"W9"},new[]{new TrackSection{Segment="S9",From=0,To=10}})&&OperatingTransitionPolicy.Overlap(new[]{new TrackSection{Segment="S1",From=0,To=100}},new[]{new TrackSection{Segment="S1",From=99,To=101}}),"unrelated running routes do not block a scoped shutdown, while overlapping direction-lock sections do");
        var states=new[]{LocalSignalAspect.Invalid,LocalSignalAspect.Kennlicht};var kinds=new[]{MarkerRecord.Main,MarkerRecord.Shunt,MarkerRecord.Distant};
        Check(kinds.All(kind=>states.All(state=>SignalLensPlan.For(kind,state).All(c=>c==SignalLensColor.Off)))&&SignalLampPlan.For(LocalSignalAspect.Kennlicht,LocalSignalAspect.Clear).Shunt==LocalSignalAspect.Dark,"invalidity and identification light turn ordinary lamps off without accidentally displaying Hp or Sh permission");
        Check(NativeSignalAuthority.For(LocalSignalAspect.Dark,null,a)==Track.Signals.SignalAspect.Stop&&states.All(state=>NativeSignalAuthority.For(state,null,a)==Track.Signals.SignalAspect.Stop),"dark lamps and presentation-only states never become a route permission through the normal aspect mapping");
        var cross=MarkerMeshData.CreateInvalidityCross();
        Check(cross.Triangles[0].Count>0&&cross.Triangles[1].Count>0&&cross.Triangles.All(t=>t.Count%3==0&&t.All(i=>i>=0&&i<cross.Vertices.Count))&&cross.Vertices.All(v=>v.All(n=>!float.IsNaN(n)&&!float.IsInfinity(n)&&Math.Abs(n)<1)),"the original own-signal cross has bounded black-outline and white-face geometry");
        var live=new SharedLiveFrame{Epoch=Id(),Sequence=1};live.Aspects[a]=LocalSignalAspect.Invalid;live.Aspects[b]=LocalSignalAspect.Kennlicht;live.Validate();
        Check(JsonConvert.DeserializeObject<SharedLiveFrame>(JsonConvert.SerializeObject(live)).Aspects[b]==LocalSignalAspect.Kennlicht,"invalidity and identification-light displays are validated and transferred to clients as explicit states");
    }
}
