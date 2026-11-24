using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed partial class InterlockingEngine {
        readonly Func<double> clock;
        public InterlockingEngine(Func<double> clock=null){this.clock=clock??(()=>System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency);}
        public double RemainingWait(RouteRun run)=>run.ReleaseSelection!=null?Math.Max(0,run.ReleaseAt-clock()):run.Stage==RouteStage.WaitingProtection?Math.Max(0,run.WaitUntil-clock()):0;
        static void Fault(RouteRun run,string reason){run.Stage=RouteStage.Fault;run.Reason=reason;if(string.IsNullOrEmpty(run.FaultReason))run.FaultReason=reason;run.ClearChecks=0;run.DestinationChecks=0;run.ReleaseChecks.Clear();}
        static double WaitSeconds(RouteDefinition route,double fallback) {
            double value=route.ProtectionWaitSeconds>=0?route.ProtectionWaitSeconds:fallback;
            if(double.IsNaN(value)||double.IsInfinity(value)||value<0||value>300)throw new InvalidOperationException("Schutzwartezeit muss 0–300 Sekunden betragen.");return value;
        }
        public RouteRun RequestWaypoint(RouteDefinition definition,IInterlockingWorld world,string[] trainCars,double protectionWait=15,Action prepare=null)=>
            RequestWaypoint(definition,world,trainCars,protectionWait,prepare,null);

        public RouteRun RequestWaypoint(RouteDefinition definition,IInterlockingWorld world,string[] trainCars,double protectionWait,Action prepare,Func<bool> proveApproach,double? turnbackDistance=null) {
            RouteRun result=null;
            ObserveRequest(new[]{definition},world,()=>{
                var expected=trainCars?.OrderBy(id=>id,StringComparer.Ordinal).ToArray();
                if(expected==null||expected.Length==0||expected.Length>256||expected.Any(string.IsNullOrEmpty)||expected.Distinct(StringComparer.Ordinal).Count()!=expected.Length)
                    throw new InvalidOperationException(LocalText.Select("Waypoint train identity is incomplete.","Der Wegpunktzug ist nicht vollständig zugeordnet."));
                RequireWaypointTrain(definition,world,expected,proveApproach);
                using(var admission=Admission?.Reserve(new[]{definition})) {
                    BeginRequest(definition,world,protectionWait,expected);
                    result=active[definition.Id];
                    if(proveApproach!=null) {
                        result.WaypointApproachProof=proveApproach;
                        result.WaypointReservedAhead=true;
                        result.WaypointTurnbackDistance=turnbackDistance;
                        result.TrainCars=null;
                    }
                    try{prepare?.Invoke();admission?.Commit();}catch{active.Remove(definition.Id);RefreshChains();throw;}
                }
            });
            return result;
        }
        static void RequireWaypointTrain(RouteDefinition route,IInterlockingWorld world,string[] expected,Func<bool> proveApproach=null) {
            if(proveApproach!=null) {
                if(proveApproach())return;
                throw new InvalidOperationException(LocalText.Select("The directed approach of the waypoint train is no longer verified.","Die gerichtete Annäherung des Wegpunktzugs ist nicht mehr nachgewiesen."));
            }
            if(!(world is ISectionalWorld sectional)||!sectional.CaptureTrain(route,out var actual)||actual==null||
                !actual.OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(expected))
                throw new InvalidOperationException(LocalText.Select("Waiting for the requesting train to be identified at the signal.","Wartet, bis der anfragende Zug am Signal eindeutig erkannt wird."));
        }
        static void RequireClearWaypointRoute(RouteDefinition route,IInterlockingWorld world) {
            if(Read(route.Sections,world)!=TrackOccupancy.Clear)
                throw new InvalidOperationException(LocalText.Select("Waypoint route needs a free, readable path and destination.","Wegpunktfahrt braucht einen freien, lesbaren Fahrweg und ein freies Ziel."));
        }
        void BeginRequest(RouteDefinition definition,IInterlockingWorld world,double fallback,string[] waypointCars=null) {
            definition.Validate();var route=definition.Clone();double wait=WaitSeconds(route,fallback);
            if(active.TryGetValue(route.Id,out var existing))throw new RouteConflictException(new[]{existing});
            if(active.Count>=16)throw new InvalidOperationException("Grenze von 16 aktiven Festlegungen erreicht. Unter Festlegungen prüfen.");
            var conflicts=active.Values.Where(r=>Conflict(route,r)).ToArray();
            if(waypointCars!=null) {
                if(conflicts.Length>0)throw new RouteConflictException(conflicts);
                RequireClearWaypointRoute(route,world);
            }
            if(Incorporate(route,conflicts,world))return;
            if(conflicts.Length>0&&(route.Shunting||conflicts.Any(r=>!r.Route.Shunting)))throw new RouteConflictException(conflicts);
            if(!world.PathValid(route))throw new InvalidOperationException("Fahrwegzuordnung ungültig. Fahrweg zuerst aktualisieren.");
            if(conflicts.Length==0){var obstruction=RequestObstruction(route,world);if(obstruction!=null)throw new InvalidOperationException(obstruction);}
            var run=new RouteRun{Route=route,Stage=conflicts.Length==0?RouteStage.Preparing:RouteStage.WaitingProtection,StartedAt=clock(),WaitUntil=clock()+(conflicts.Length==0?0:wait)};
            if(waypointCars!=null){run.ExpectedWaypointCars=waypointCars;run.TrainCars=waypointCars.ToArray();}
            run.Reason=conflicts.Length==0?"Fahrstraße läuft ein; Signale Halt":"Rangierzustimmung zurückgenommen; Schutzwartezeit und Freiprüfung";
            run.WithdrawnRoutes=conflicts.Select(r=>r.Route.Id).ToArray();
            foreach(var old in conflicts){old.Stage=RouteStage.Cancelled;old.Reason=LocalText.Get("Zugfahrt angefordert: Rangierzustimmung zurückgenommen; Verschlüsse bis zur Freiprüfung behalten.");}
            active.Add(route.Id,run);
            RefreshChains();
        }
        void ContinueWithdrawal(RouteRun run,IInterlockingWorld world) {
            if(clock()>run.WaitUntil+300)throw new InvalidOperationException("Rücknahme nicht abgeschlossen. Restverschlüsse prüfen.");
            if(!world.PathValid(run.Route))throw new InvalidOperationException("Fahrweg während der Rücknahme geändert.");
            if(clock()<run.WaitUntil){run.Reason=LocalText.Get("Rangiersignale Halt; Schutzwartezeit noch ")+Math.Ceiling(run.WaitUntil-clock())+" s";return;}
            foreach(string id in run.WithdrawnRoutes) {
                RouteRun old;if(!active.TryGetValue(id,out old))continue;
                if(!world.PathValid(old.Route)||Read(old.HeldSections,world)!=TrackOccupancy.Clear){old.ApproachStopChecks=0;old.ApproachStopIdentity=null;run.Reason=LocalText.Get("Warten auf ")+old.Route.Code+": "+BlockingReason(old,world);return;}
                string protection;if(!ReleaseApproach(old,world,out protection)){run.Reason=LocalText.Get("Warten auf ")+old.Route.Code+": "+protection;return;}active.Remove(id);
            }
            var obstruction=RequestObstruction(run.Route,world);if(obstruction!=null){run.Reason=obstruction;return;}
            run.Stage=RouteStage.Preparing;run.StartedAt=clock();run.Reason=LocalText.Get("Fahrweg frei; Weichen werden geprüft und festgelegt.");
        }
        void ContinuePreparation(RouteRun run,IInterlockingWorld world) {
            if(run.ExpectedWaypointCars!=null){RequireWaypointTrain(run.Route,world,run.ExpectedWaypointCars,run.WaypointApproachProof);RequireClearWaypointRoute(run.Route,world);}
            CheckFlanks(run,world);
            if(clock()-run.StartedAt>120)throw new InvalidOperationException("Weichen-/Endlagenprüfung nach 120 Sekunden nicht abgeschlossen.");
            if(!world.PathValid(run.Route))throw new InvalidOperationException("Fahrwegzuordnung ungültig. Fahrweg zuerst aktualisieren.");
            var obstruction=RequestObstruction(run.Route,world);if(obstruction!=null)throw new InvalidOperationException(obstruction);
            bool ready=true;var feedback=world as ISwitchFeedbackWorld;
            foreach(var point in run.Route.Switches) {
                bool actual;if(!world.SwitchPosition(point.Node,out actual))throw new InvalidOperationException("Weiche nicht verfügbar: "+point.Node);
                if(actual!=point.Thrown){if(!world.SetSwitch(point.Node,point.Thrown))throw new InvalidOperationException("Weiche konnte nicht gestellt werden: "+point.Node);run.PointChecks[point.Node]=0;run.PointStates[point.Node]="Umlauf angefordert";ready=false;continue;}
                string detail="Stellung bestätigt";bool settled=feedback==null||feedback.SwitchSettled(point.Node,point.Thrown,out detail);int count;run.PointChecks.TryGetValue(point.Node,out count);count=settled?Math.Min(2,count+1):0;run.PointChecks[point.Node]=count;
                run.PointStates[point.Node]=count>=2?"Endlage bestätigt / verschlossen":detail;ready&=count>=2;
            }
            run.AlignmentChecks=ready?run.AlignmentChecks+1:0;
            if(run.AlignmentChecks<2){run.Reason=ready?"Endlagen bestätigt; abschließende Freiprüfung":"Fahrstraße läuft ein / Weichenumlauf; Signale Halt";return;}
            obstruction=RequestObstruction(run.Route,world);if(obstruction!=null)throw new InvalidOperationException(obstruction);
            if(!world.PathValid(run.Route))throw new InvalidOperationException("Abschließende Fahrwegprüfung fehlgeschlagen.");
            run.WaypointApproachProof=null;
            run.Stage=RouteStage.Set;run.Reason=LocalText.Get("Fahrstraße festgelegt; alle Endlagen und Freiprüfungen bestätigt.");
        }
        public void RequestRelease(string id,string selection,double fallback=15) {
            RouteRun run;if(!active.TryGetValue(id,out run))throw new InvalidOperationException("Fahrstraße nicht aktiv.");
            if(run.ReleaseSelection!=null)throw new InvalidOperationException("Hilfsauflösung läuft bereits.");
            bool valid=selection=="all"||run.Route.Switches.Any(p=>selection=="point:"+p.Node&&!run.ReleasedSwitches.Contains(p.Node))||run.HeldSections.Any(s=>selection=="section:"+s.Key);
            if(!valid)throw new InvalidOperationException("Restverschluss nicht vorhanden.");
            run.ReleaseSelection=selection;run.ReleaseAt=clock()+WaitSeconds(run.Route,fallback);run.Stage=RouteStage.Cancelled;run.Reason=LocalText.Get("Halt; geprüfte Hilfsauflösung angefordert");run.ReleaseStatus="Schutzwartezeit läuft";
            run.ReleaseWaitCompleted=false;run.ApproachStopChecks=0;run.ApproachStopIdentity=null;RefreshChains();
        }
        public void ConfirmOperatorRelease(string id){RouteRun run;if(!active.TryGetValue(id,out run)||run.ReleaseSelection==null||run.Stage!=RouteStage.Cancelled)throw new InvalidOperationException("Zuerst Halt und eine konkrete Hilfsauflösung anfordern.");run.OperatorReleaseConfirmed=true;run.ReleaseStatus="Vom Fahrdienstleiter örtlich geprüft und bestätigt; Schutzwartezeit bleibt wirksam.";}
        bool ProcessRelease(RouteRun run,IInterlockingWorld world) {
            if(clock()<run.ReleaseAt){run.ReleaseStatus="Noch "+Math.Ceiling(run.ReleaseAt-clock())+" s Schutzwartezeit";return false;}
            run.ReleaseWaitCompleted=true;
            if(run.OperatorReleaseConfirmed)return ApplyRelease(run,"Fdl-bestätigte Hilfsauflösung; physische Belegung unverändert");
            if(!world.PathValid(run.Route)){run.ReleaseStatus="Blockiert: Fahrwegzuordnung nicht lesbar/ungültig.";return false;}
            if(Read(run.HeldSections,world)!=TrackOccupancy.Clear){run.ApproachStopChecks=0;run.ApproachStopIdentity=null;run.ReleaseStatus="Blockiert: "+BlockingReason(run,world);return false;}
            string approach;if(!ReleaseApproach(run,world,out approach)){run.ReleaseStatus="Warten: "+approach;return false;}
            return ApplyRelease(run,"Restbereiche frei; "+approach);
        }
        bool ApplyRelease(RouteRun run,string reason) {
            if(run.ReleaseSelection=="all"){run.ReleaseStatus=reason+"; aufgelöst.";return true;}
            if(run.ReleaseSelection.StartsWith("point:",StringComparison.Ordinal))run.ReleasedSwitches.Add(run.ReleaseSelection.Substring(6));
            else run.ReleasedSections.Add(run.ReleaseSelection.Substring(8));
            run.ReleaseSelection=null;run.OperatorReleaseConfirmed=false;run.ReleaseStatus=reason+"; gewählte Gruppe freigegeben, weitere Verschlüsse bleiben.";
            return !run.HeldSections.Any()&&run.Route.Switches.All(p=>run.ReleasedSwitches.Contains(p.Node));
        }
        public string BlockingReason(RouteRun run,IInterlockingWorld world) {
            foreach(var section in run.HeldSections){var state=world.Occupancy(section);if(state!=TrackOccupancy.Clear)return "Abschnitt "+section.Key+": "+(state==TrackOccupancy.Occupied?"belegt":"nicht lesbar");}
            foreach(var section in run.Route.Approach){var state=world.Occupancy(section);if(state!=TrackOccupancy.Clear)return "Annäherung "+section.Key+": "+(state==TrackOccupancy.Occupied?"belegt":"nicht lesbar");}return "Restfestlegung prüfen";
        }
        static void ReleaseDestination(RouteRun run,IInterlockingWorld world,double head,double tail) {
            if((run.Route.Shunting&&!run.WaypointTurnbackDistance.HasValue)||!run.Route.Switches.All(p=>run.ReleasedSwitches.Contains(p.Node))||!(world is ITrainStopWorld))return;
            var path=run.Route.Path;double at=0,start=0;
            foreach(var piece in path){at+=piece.Section.To-piece.Section.From;if(run.Route.Switches.Any(p=>p.Protection!=null&&p.Protection.Any(s=>s.Overlaps(piece.Section))))start=at;}
            double total=path.Sum(p=>p.Section.To-p.Section.From);
            double? minimumHead=run.WaypointTurnbackDistance.HasValue?run.WaypointTurnbackDistance-.5:run.Route.AuxiliaryExit?(double?)(total-1):null;
            bool stopped=tail>start+.5&&head<=total+.5&&(!minimumHead.HasValue||head>=minimumHead.Value)&&((ITrainStopWorld)world).TrainStopped(run.TrainCars);
            // Never release a preceding point group or an unknown occupied area merely because a train stopped.
            var destination=new List<TrackSection>();at=0;foreach(var piece in path){if(at>=start-.001)destination.Add(piece.Section);at+=piece.Section.To-piece.Section.From;}
            stopped&=run.HeldSections.All(s=>destination.Any(d=>d.Key==s.Key))&&run.HeldSections.All(s=>world.Occupancy(s)!=TrackOccupancy.Unknown);
            run.DestinationChecks=stopped?run.DestinationChecks+1:0;
            if(run.DestinationChecks>=3)foreach(var section in destination)run.ReleasedSections.Add(section.Key);
        }
    }
}
