using EnusDE.Signaltechnik;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public static class StagingTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static TrackSection S(string id,double a,double b)=>new TrackSection{Segment=id,From=a,To=b};
    sealed class World:IInterlockingWorld,ISwitchFeedbackWorld,ISectionalWorld,ITrainStopWorld {
        public bool Position,Settled,Valid=true,Unknown,Approach,Occupied,Known=true,Stopped,Capture;
        public int Commands;
        public double Head=-1,Tail=-20;
        public bool PathValid(RouteDefinition route)=>Valid;
        public TrackOccupancy Occupancy(TrackSection section)=>Unknown?TrackOccupancy.Unknown:section.Segment=="approach"?Approach?TrackOccupancy.Occupied:TrackOccupancy.Clear:Occupied&&section.Segment=="S2"&&section.From>=25?TrackOccupancy.Occupied:TrackOccupancy.Clear;
        public bool SwitchPosition(string node,out bool thrown){thrown=Position;return true;}
        public bool SetSwitch(string node,bool thrown){Commands++;Position=thrown;return true;}
        public bool SwitchSettled(string node,bool thrown,out string detail){detail=Settled?"Endlage":"Zungen im Umlauf";return Settled;}
        public bool CaptureTrain(RouteDefinition route,out string[] cars){cars=Capture?new[]{"loco","tail"}:null;return Capture;}
        public bool TrainProgress(RouteDefinition route,string[] cars,out double head,out double tail){head=Known?Head:double.NaN;tail=Known?Tail:double.NaN;return Known;}
        public bool TrainStopped(string[] cars)=>Stopped&&Known&&cars?.Length==2;
    }
    static RouteDefinition Route(bool shunt=false) {
        var route=new RouteDefinition{Code="F1",Entry="A",Exit="B",Shunting=shunt,Sections=new List<TrackSection>{S("S1",0,100),S("S2",0,100),S("S1",75,100),S("S2",0,25),S("side",0,25)},Approach=new List<TrackSection>{S("approach",0,30)},Path=new List<DirectedRouteSection>{new DirectedRouteSection{Section=S("S1",0,100)},new DirectedRouteSection{Section=S("S2",0,100)}},Switches=new List<RouteSwitch>{new RouteSwitch{Node="W1",Thrown=true,ReleaseAt=125,Protection=new List<TrackSection>{S("S1",75,100),S("S2",0,25),S("side",0,25)}}},ViaSignals=new List<RouteViaSignal>()};
        RouteSignals.SplitProtection(route);route.Validate();return route;
    }
    static RouteDefinition OpposedShunt(){var route=Route(true);foreach(var piece in route.Path)piece.Reverse=!piece.Reverse;return route;}
    static void Ticks(InterlockingEngine engine,World world,int n=4){for(int i=0;i<n;i++)engine.Tick(world);}
    public static void Run(string root) {
        double now=0;var e=new InterlockingEngine(()=>now);var w=new World();var r=Route();
        e.Request(r,w);Check(e.ForSignal("A").Stage==RouteStage.Preparing&&!e.ForSignal("A").Permitted,"staged route reserves the path with entry stop before issuing point commands");
        e.Tick(w);Ticks(e,w,8);Check(w.Commands==1&&!e.ForSignal("A").Permitted&&e.ForSignal("A").PointStates["W1"].Contains("Umlauf"),"logical point target alone never clears the signal while physical feedback is moving");
        w.Settled=true;e.Tick(w);Check(!e.ForSignal("A").Permitted,"first physical end-position confirmation leaves the signal at stop");e.Tick(w);Check(!e.ForSignal("A").Permitted,"two point confirmations still require a separate final route check");e.Tick(w);Check(e.ForSignal("A").Permitted,"confirmed point end positions and final free check permit the route");
        w.Settled=false;e.Tick(w);string fault=e.ForSignal("A").FaultReason;w.Settled=true;e.Tick(w);Check(!e.ForSignal("A").Permitted&&e.ForSignal("A").FaultReason==fault,"lost point end-position drops authority and preserves the first fault after recovery");
        e=new InterlockingEngine(()=>now);w=new World();r=Route();e.Request(r,w);w.Unknown=true;e.Tick(w);Check(e.ForSignal("A").Stage==RouteStage.Fault&&e.Locked("W1",false),"unknown occupancy during preparation retains the point lock and prevents authority");
        now=0;e=new InterlockingEngine(()=>now);w=new World{Position=true,Settled=true};var shunt=OpposedShunt();e.Request(shunt,w);Ticks(e,w);var main=Route();main.Code="F2";e.Request(main,w,15);
        Check(!e.ForSignal("A").Permitted&&e.Active.First(a=>a.Route.Id==main.Id).Stage==RouteStage.WaitingProtection,"conflicting train request withdraws shunting authority before waiting");
        now=14.99;e.Tick(w);Check(e.Active.First(a=>a.Route.Id==main.Id).Stage==RouteStage.WaitingProtection,"the corrected fifteen-second protection time is not rounded down");
        w.Approach=true;now=15;e.Tick(w);Check(e.Active.Any(a=>a.Route.Id==shunt.Id)&&!e.Active.Any(a=>a.Permitted),"elapsed protection time cannot override an occupied approach");
        w.Approach=false;e.Tick(w);Check(e.Active.Count==1&&e.Active.Single().Stage==RouteStage.Preparing,"clear old protection is released only after the timer and known-free checks");Ticks(e,w);Check(e.Active.Single().Permitted,"new train authority follows fresh point and route checks after shunting withdrawal");
        now=0;e=new InterlockingEngine(()=>now);w=new World{Position=true,Settled=true};shunt=OpposedShunt();e.Request(shunt,w);Ticks(e,w);main=Route();main.ProtectionWaitSeconds=3;e.Request(main,w,15);now=2.9;e.Tick(w);Check(e.Active.Count==2,"route-specific protection override still waits its full duration");now=3;e.Tick(w);Check(e.Active.Count==1&&e.Active.Single().Stage==RouteStage.Preparing,"route-specific protection time overrides the host default");
        now=0;e=new InterlockingEngine(()=>now);w=new World{Position=true,Settled=true};r=Route();e.Request(r,w);Ticks(e,w);e.RequestRelease(r.Id,"point:W1",15);now=14.99;e.Tick(w);Check(e.Locked("W1",false)&&!e.ForSignal("A").Permitted,"individual auxiliary release first withdraws authority and waits fifteen seconds");
        now=15;w.Unknown=true;e.Tick(w);Check(e.Locked("W1",false),"auxiliary release cannot bypass unknown occupancy");w.Unknown=false;e.Tick(w);Check(!e.Locked("W1",false)&&e.Active.Single().HeldSections.Any(),"a checked individual point release retains the other track reservations");
        e.RequestRelease(r.Id,"all",15);now=30;w.Approach=true;e.Tick(w);Check(e.Active.Count==1,"whole auxiliary release cannot bypass an occupied approach");w.Approach=false;e.Tick(w);Check(e.Active.Count==0,"whole auxiliary release completes after its time and confirmed-free checks");
        now=0;e=new InterlockingEngine(()=>now);w=new World{Position=true,Settled=true};r=Route();e.Request(r,w);Ticks(e,w);w.Capture=true;w.Head=190;w.Tail=140;w.Occupied=true;Ticks(e,w,5);Check(e.Active.Count==1&&!e.Locked("W1",false),"moving train releases cleared points but retains its occupied destination reservation");
        w.Stopped=true;Ticks(e,w,2);Check(e.Active.Count==1,"one or two stopped observations do not release the destination");e.Tick(w);Check(e.Active.Count==0&&w.Occupancy(S("S2",25,100))==TrackOccupancy.Occupied,"known whole train stopped beyond all points releases the route without falsely clearing destination occupancy");Check(Reject(()=>e.Request(Route(),w)),"following train route remains blocked by actual destination occupancy after route release");
        e=new InterlockingEngine(()=>now);w=new World{Position=true,Settled=true};r=Route();e.Request(r,w);Ticks(e,w);w.Capture=true;w.Head=190;w.Tail=110;w.Occupied=true;w.Stopped=true;Ticks(e,w,6);Check(e.Active.Count==1&&e.Locked("W1",false),"a stopped train whose tail has not cleared point protection cannot release the destination");
        e=new InterlockingEngine(()=>now);w=new World{Position=true,Settled=true};r=Route();e.Request(r,w);Ticks(e,w);w.Capture=true;w.Head=190;w.Tail=140;w.Occupied=true;Ticks(e,w,4);w.Stopped=true;Ticks(e,w,2);w.Known=false;e.Tick(w);w.Known=true;e.Tick(w);Check(e.Active.Count==1,"unknown train data resets consecutive stopped-destination checks instead of retaining stale release evidence");Ticks(e,w,2);Check(e.Active.Count==0,"three new valid stopped observations can finish a retained destination lock after data recovery");
        now=0;e=new InterlockingEngine(()=>now);w=new World{Position=true,Settled=true};shunt=OpposedShunt();e.Request(shunt,w);Ticks(e,w);main=Route();main.ProtectionWaitSeconds=300;e.Request(main,w);now=300.5;e.Tick(w);Check(e.Active.Count==1&&e.Active.Single().Stage==RouteStage.Preparing,"maximum configurable protection wait can finish on the next simulation tick without premature timeout");
        var viaRun=new RouteRun{Route=Route(),Stage=RouteStage.Set};viaRun.Route.ViaSignals.Add(new RouteViaSignal{Id="RS1"});
        Check(NativeSignalAuthority.For(LocalSignalAspect.Shunt,viaRun,"RS1")==Track.Signals.SignalAspect.Clear&&NativeSignalAuthority.For(LocalSignalAspect.Shunt,null,"RS1")==Track.Signals.SignalAspect.Restricting,"a cleared through-shunt in a train route adds no native shunting-speed restriction while genuine shunting stays restrictive");
        viaRun.Route.Shunting=true;Check(NativeSignalAuthority.For(LocalSignalAspect.Shunt,viaRun,"RS1")==Track.Signals.SignalAspect.Restricting&&NativeSignalAuthority.For(LocalSignalAspect.Stop,viaRun,"RS1")==Track.Signals.SignalAspect.Stop,"shunting-route speed semantics and stop are never bypassed by the through-shunt mapping");
        var marker=new MarkerOptions();Check(marker.ProtectionWaitSeconds==15,"new host settings use fifteen seconds, not fifty");
        var native=typeof(Track.SwitchPointRails);var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;Check(new[]{"_node","state","_normalSequence","_reversedSequence"}.All(n=>native.GetField(n,flags)!=null)&&native.GetField("_normalSequence",flags).FieldType.GetProperty("isAlive",flags)?.PropertyType==typeof(bool),"installed point-rail animation fields and actual PrimeTween liveness property match the feedback adapter");
    }
}
