using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using Newtonsoft.Json.Linq;

public static class InterlockingTests {
    static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL: "+label);System.Console.WriteLine("PASS: "+label);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    sealed class World:IInterlockingWorld {
        public TrackOccupancy Route=TrackOccupancy.Clear,Approach=TrackOccupancy.Clear;
        public bool Valid=true,Switch,SetOk=true,ThrowRead;
        public int Writes;
        public bool PathValid(RouteDefinition r)=>Valid;
        public TrackOccupancy Occupancy(TrackSection s){if(ThrowRead)throw new IOException("fixture lost");return s.Segment=="approach"?Approach:Route;}
        public bool SwitchPosition(string id,out bool value){value=Switch;return id=="W1";}
        public bool SetSwitch(string id,bool value){Writes++;if(SetOk)Switch=value;return SetOk;}
    }
    static RouteDefinition Route(string code="F1",string entry="A",double from=0,double to=100)=>new RouteDefinition{Code=code,Entry=entry,Exit="B",Sections=new List<TrackSection>{new TrackSection{Segment="S1",From=from,To=to}},Approach=new List<TrackSection>{new TrackSection{Segment="approach",From=0,To=300}},Switches=new List<RouteSwitch>{new RouteSwitch{Node="W1",Thrown=true}}};
    static void Ready(InterlockingEngine engine,RouteDefinition route,IInterlockingWorld world) {
        engine.Request(route,world);for(int i=0;i<8&&engine.Active.Any(r=>r.Route.Id==route.Id&&r.Stage==RouteStage.Preparing);i++)engine.Tick(world);
        var run=engine.Active.Single(r=>r.Route.Id==route.Id);if(run.Stage==RouteStage.Fault)throw new InvalidOperationException(run.Reason);
        if(!run.Permitted)throw new InvalidOperationException("Fixture route did not finish its normal staged checks.");
    }
    public static void Run(string root) {
        var world=new World();var engine=new InterlockingEngine();var route=Route();
        Ready(engine,route,world);
        Check(engine.ForSignal("A").Permitted&&world.Switch&&world.Writes==1,"route is authorized only after confirmed native switch alignment and final clear checks");
        Check(engine.Locked("W1",false)&&!engine.Locked("W1",true)&&engine.UsesSignal("B"),"active route locks its switch against contrary changes and protects both endpoint signals");
        Check(Reject(()=>Ready(engine,Route("F2","C",50,150),world))&&engine.Active.Count==1,"overlapping routes are rejected without mutating the active authority");
        var sameSwitch=Route("F3","D",200,300);Check(Reject(()=>Ready(engine,sameSwitch,world)),"even nonoverlapping routes cannot concurrently own the same switch");
        world.Route=TrackOccupancy.Occupied;engine.Tick(world);
        Check(!engine.ForSignal("A").Permitted&&engine.ForSignal("A").Stage==RouteStage.Running&&engine.Locked("W1",false),"first route occupation returns the entry signal to stop while retaining switch locks");
        world.Route=TrackOccupancy.Clear;engine.Tick(world);Check(engine.Active.Count==1,"a single clear sample after occupation does not release the train's route");
        engine.Tick(world);Check(engine.Active.Count==0&&!engine.Locked("W1",false),"second consecutive fully clear sample releases a used route and its locks");
        engine=new InterlockingEngine();world=new World{Route=TrackOccupancy.Unknown};
        Check(Reject(()=>Ready(engine,route,world))&&engine.Active.Count==0&&world.Writes==0,"unknown occupancy cannot create authority or move switches");
        world.Route=TrackOccupancy.Occupied;Check(Reject(()=>Ready(engine,route,world))&&world.Writes==0,"occupied route cannot move points or grant a route");
        world=new World{SetOk=false};engine=new InterlockingEngine();
        Check(Reject(()=>Ready(engine,route,world))&&engine.ForSignal("A").Stage==RouteStage.Fault&&!engine.ForSignal("A").Permitted&&engine.Locked("W1",false),"failed switch alignment leaves stop and a retained fault lock");
        world=new World();engine=new InterlockingEngine();Ready(engine,route,world);world.Route=TrackOccupancy.Unknown;engine.Tick(world);
        Check(!engine.ForSignal("A").Permitted&&engine.ForSignal("A").Stage==RouteStage.Fault,"lost occupancy during an authorized route immediately fails closed");
        world.Route=TrackOccupancy.Clear;engine.Tick(world);engine.Tick(world);
        Check(engine.Active.Count==1&&!engine.ForSignal("A").Permitted,"recovered data does not silently reauthorize or unlock a faulted route");
        world.Approach=TrackOccupancy.Occupied;
        Check(Reject(()=>engine.Cancel(route.Id,world))&&engine.Active.Count==1&&!engine.ForSignal("A").Permitted,"cancellation sets stop but an approaching train prevents unlocking");
        world.Approach=TrackOccupancy.Unknown;Check(Reject(()=>engine.Cancel(route.Id,world))&&engine.Active.Count==1,"unknown approach occupancy also prevents cancellation release");
        world.Approach=TrackOccupancy.Clear;world.Valid=false;
        Check(Reject(()=>engine.Cancel(route.Id,world))&&engine.Active.Count==1,"changed topology cannot be treated as clear merely to release a lock");
        world.Valid=true;engine.Cancel(route.Id,world);Check(engine.Active.Count==0,"explicit safe cancellation releases only after valid path, clear route and clear approach");
        world=new World();engine=new InterlockingEngine();Ready(engine,route,world);world.Switch=false;engine.Tick(world);
        Check(engine.ForSignal("A").Stage==RouteStage.Fault&&!engine.ForSignal("A").Permitted,"unexpected physical switch state forces stop even if an external action bypassed the guard");
        world=new World();engine=new InterlockingEngine();Ready(engine,route,world);world.ThrowRead=true;engine.Tick(world);
        Check(!engine.ForSignal("A").Permitted&&engine.Locked("W1",false),"exceptions in occupancy reading retain protection instead of continuing a previous green");
        world=new World();engine=new InterlockingEngine();Ready(engine,route,world);engine.HaltAll();
        Check(!engine.ForSignal("A").Permitted&&engine.Active.Count==1,"all-stop withdraws permissions without dropping route locks");
        Check(!new TrackSection{Segment="S",From=0,To=10}.Overlaps(new TrackSection{Segment="S",From=10,To=20}),"adjacent route intervals can meet at a signal without a false geometric overlap");
        string directory=Path.Combine(root,"build","interlocking-tests-"+Guid.NewGuid().ToString("N"));
        var store=new InterlockingStore(directory,"fixture-world");store.Save(new[]{route});var loaded=store.Load();
        Check(loaded.Single().Entry=="A"&&new InterlockingEngine().Active.Count==0,"stored interlocking definitions retain stable signal IDs while runtime permissions always start empty");
        loaded[0].Code="changed";Check(store.Load()[0].Code=="F1","editing loaded route data cannot mutate persisted configuration");
        string saved=Path.Combine(directory,"fixture-world.json");File.WriteAllText(saved,"{broken");
        Check(Reject(()=>store.Load())&&File.ReadAllText(saved)=="{broken","corrupt interlocking configuration is preserved and never replaced by an empty plant");
        var unknown=Route();unknown.Sections.Clear();Check(Reject(()=>unknown.Validate()),"route definitions without protected track sections are rejected");
    }
    public static void Units(string root) {
        Check(SignUnits.PlayerId("player:abc")=="abc"&&SignUnits.PlayerId("abc")=="abc","player-sign lookups strip exactly the prefix already added internally by the game");
        double metric=SignUnits.Input("40,0",true),imperial=SignUnits.Input("25",false);
        Check(Math.Abs(metric*SignUnits.MphToKmh-40)<1e-10&&imperial==25,"mph and km/h inputs preserve the same canonical physical speed without double conversion");
        Check(SignUnits.Caption(25,"original")=="25"&&SignUnits.Caption(25,"metric")=="40"&&SignUnits.Caption(25,"mixed")=="40","world speed boards show one rounded number without units or a second line, preserving canonical speed");
        Check(Reject(()=>SignUnits.Input("NaN",true))&&Reject(()=>SignUnits.Input("0",false))&&Reject(()=>SignUnits.Input("201",false)),"nonfinite, zero and out-of-range sign speeds cannot be persisted");
        Check(SignUnits.Mode(JObject.Parse("{\"speedUnits\":\"metric\"}"))=="metric"&&SignUnits.Mode(JObject.Parse("{\"convertExplicitUnitDisplays\":false}"))=="original","translation speed-unit preference and legacy fallback have explicit compatible semantics");
        Check(SignUnits.Mode(JObject.Parse("{\"language\":\"en\",\"speedUnits\":\"metric\"}"))=="original","English language selection restores original sign units even when a metric preference remains saved");
        string directory=Path.Combine(root,"build","unit-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);string path=Path.Combine(directory,"translation.json");
        var pref=new SignUnitPreference(path);pref.Refresh();Check(pref.Mode=="original","absent translation mod settings default to mph without creating a dependency or file");
        File.WriteAllText(path,"{\"speedUnits\":\"mixed\"}");pref.Refresh();Check(pref.Mode=="mixed"&&!pref.Refresh(),"unit preference reloads changes and reuses unchanged files");
        File.WriteAllText(path,"{broken");File.SetLastWriteTimeUtc(path,DateTime.UtcNow.AddSeconds(2));pref.Refresh();
        Check(pref.Mode=="original"&&File.ReadAllText(path)=="{broken","unreadable translation settings fall back to explicit mph and are never overwritten");
        string session=Path.Combine(directory,"diagnostic-session");
        using(var writer=new TelemetryWriter(session,new{schema=2,version="0.2.0",start_utc="2026-11-10T01:00:00Z"})) {
            writer.Enqueue(new{kind="signal_bridge_health",data=new{available=false,error="fixture missing method"}});
            writer.Enqueue(new{kind="route_state",data=new{id="R",stage="Set"}});
            writer.Enqueue(new{kind="ai_signal_read",data=new{id="A",aspect="Clear",movement_verified=false}});
            writer.Enqueue(new{kind="local_signal_aspect",data=new{id="A",aspect="Clear"}});
            writer.Enqueue(new{kind="ai_stop_target",data=new{vehicle_id="C1",signal_id="P1-guid",code="P1",distance_m=17.5,movement_verified=false}});
        }
        var summary=DiagnosticsReport.ReadSession(session);
        Check(summary.own_signal_events==3&&summary.ai_signal_reads==1&&summary.speed_drops.Count==0,"analysis separates own signal state and AI queries without inventing a train movement or braking response");
        Check(summary.ai_stop_targets==1&&summary.ai_stop_history.Single().Contains("C1 · P1 (P1-guid) · 17.5 m")&&summary.speed_drops.Count==0,"diagnostics preserve locomotive, target ID and distance without inferring a braking response");
        Check(summary.signal_bridge_status.Contains("Start fehlgeschlagen")&&summary.signal_bridge_status.Contains("fixture missing method"),"analysis exposes native-hook startup failure separately from the enabled setting and train reaction");
    }
}
