using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed partial class InterlockingEngine {
        readonly Func<double> clock;
        public InterlockingEngine(Func<double> clock=null){this.clock=clock??(()=>System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency);}
        public double RemainingWait(RouteRun run)=>Math.Max(0,(run.ReleaseSelection==null?run.WaitUntil:run.ReleaseAt)-clock());
        static void Fault(RouteRun run,string reason){run.Stage=RouteStage.Fault;run.Reason=reason;if(string.IsNullOrEmpty(run.FaultReason))run.FaultReason=reason;run.ClearChecks=0;run.DestinationChecks=0;run.ReleaseChecks.Clear();}
        static double WaitSeconds(RouteDefinition route,double fallback) {
            double value=route.ProtectionWaitSeconds>=0?route.ProtectionWaitSeconds:fallback;
            if(double.IsNaN(value)||double.IsInfinity(value)||value<0||value>300)throw new InvalidOperationException("Schutzwartezeit muss 0–300 Sekunden betragen.");return value;
        }
        void BeginRequest(RouteDefinition definition,IInterlockingWorld world,double fallback) {
            definition.Validate();var route=definition.Clone();double wait=WaitSeconds(route,fallback);
            if(active.Count>=16||active.ContainsKey(route.Id))throw new InvalidOperationException("Fahrstraße bereits aktiv oder Grenze von 16 erreicht.");
            var conflicts=active.Values.Where(r=>Conflict(route,r)).ToArray();
            if(Incorporate(route,conflicts,world))return;
            if(conflicts.Length>0&&(route.Shunting||conflicts.Any(r=>!r.Route.Shunting)))throw new InvalidOperationException("Widersprechende Fahrstraße/Festlegung vorhanden. Erst sicher auflösen.");
            if(!world.PathValid(route))throw new InvalidOperationException("Fahrwegzuordnung ungültig. Fahrweg zuerst aktualisieren.");
            if(conflicts.Length==0&&!RequestClear(route,world))throw new InvalidOperationException("Fahrweg/Weichenbereich belegt oder nicht lesbar.");
            var run=new RouteRun{Route=route,Stage=conflicts.Length==0?RouteStage.Preparing:RouteStage.WaitingProtection,StartedAt=clock(),WaitUntil=clock()+(conflicts.Length==0?0:wait)};
            run.Reason=conflicts.Length==0?"Fahrstraße läuft ein; Signale Halt":"Rangierzustimmung zurückgenommen; Schutzwartezeit und Freiprüfung";
            run.WithdrawnRoutes=conflicts.Select(r=>r.Route.Id).ToArray();
            foreach(var old in conflicts){old.Stage=RouteStage.Cancelled;old.Reason="Zugfahrt angefordert: Rangierzustimmung zurückgenommen; Verschlüsse bis zur Freiprüfung behalten.";}
            active.Add(route.Id,run);
            RefreshChains();
        }
        void ContinueWithdrawal(RouteRun run,IInterlockingWorld world) {
            if(clock()>run.WaitUntil+300)throw new InvalidOperationException("Rücknahme nicht abgeschlossen. Restverschlüsse prüfen.");
            if(!world.PathValid(run.Route))throw new InvalidOperationException("Fahrweg während der Rücknahme geändert.");
            if(clock()<run.WaitUntil){run.Reason="Rangiersignale Halt; Schutzwartezeit noch "+Math.Ceiling(run.WaitUntil-clock())+" s";return;}
            foreach(string id in run.WithdrawnRoutes) {
                RouteRun old;if(!active.TryGetValue(id,out old))continue;
                if(!world.PathValid(old.Route)||Read(old.HeldSections,world)!=TrackOccupancy.Clear){old.ApproachStopChecks=0;old.ApproachStopIdentity=null;run.Reason="Warten auf "+old.Route.Code+": "+BlockingReason(old,world);return;}
                string protection;if(!ReleaseApproach(old,world,out protection)){run.Reason="Warten auf "+old.Route.Code+": "+protection;return;}active.Remove(id);
            }
            if(!RequestClear(run.Route,world)){run.Reason="Schutzwartezeit abgelaufen; Fahrweg noch belegt oder nicht lesbar.";return;}
            run.Stage=RouteStage.Preparing;run.StartedAt=clock();run.Reason="Fahrweg frei; Weichen werden geprüft und festgelegt.";
        }
        void ContinuePreparation(RouteRun run,IInterlockingWorld world) {
            CheckFlanks(run,world);
            if(clock()-run.StartedAt>120)throw new InvalidOperationException("Weichen-/Endlagenprüfung nach 120 Sekunden nicht abgeschlossen.");
            if(!world.PathValid(run.Route)||!RequestClear(run.Route,world))throw new InvalidOperationException("Einlaufen abgebrochen: Fahrweg geändert, belegt oder nicht lesbar.");
            bool ready=true;var feedback=world as ISwitchFeedbackWorld;
            foreach(var point in run.Route.Switches) {
                bool actual;if(!world.SwitchPosition(point.Node,out actual))throw new InvalidOperationException("Weiche nicht verfügbar: "+point.Node);
                if(actual!=point.Thrown){if(!world.SetSwitch(point.Node,point.Thrown))throw new InvalidOperationException("Weiche konnte nicht gestellt werden: "+point.Node);run.PointChecks[point.Node]=0;run.PointStates[point.Node]="Umlauf angefordert";ready=false;continue;}
                string detail="Stellung bestätigt";bool settled=feedback==null||feedback.SwitchSettled(point.Node,point.Thrown,out detail);int count;run.PointChecks.TryGetValue(point.Node,out count);count=settled?Math.Min(2,count+1):0;run.PointChecks[point.Node]=count;
                run.PointStates[point.Node]=count>=2?"Endlage bestätigt / verschlossen":detail;ready&=count>=2;
            }
            run.AlignmentChecks=ready?run.AlignmentChecks+1:0;
            if(run.AlignmentChecks<2){run.Reason=ready?"Endlagen bestätigt; abschließende Freiprüfung":"Fahrstraße läuft ein / Weichenumlauf; Signale Halt";return;}
            if(!RequestClear(run.Route,world)||!world.PathValid(run.Route))throw new InvalidOperationException("Abschließende Fahrwegprüfung fehlgeschlagen.");
            run.Stage=RouteStage.Set;run.Reason="Fahrstraße festgelegt; alle Endlagen und Freiprüfungen bestätigt.";
        }
        public void RequestRelease(string id,string selection,double fallback=15) {
            RouteRun run;if(!active.TryGetValue(id,out run))throw new InvalidOperationException("Fahrstraße nicht aktiv.");
            if(run.ReleaseSelection!=null)throw new InvalidOperationException("Hilfsauflösung läuft bereits.");
            bool valid=selection=="all"||run.Route.Switches.Any(p=>selection=="point:"+p.Node&&!run.ReleasedSwitches.Contains(p.Node))||run.HeldSections.Any(s=>selection=="section:"+s.Key);
            if(!valid)throw new InvalidOperationException("Restverschluss nicht vorhanden.");
            run.ReleaseSelection=selection;run.ReleaseAt=clock()+WaitSeconds(run.Route,fallback);run.Stage=RouteStage.Cancelled;run.Reason="Halt; geprüfte Hilfsauflösung angefordert";run.ReleaseStatus="Schutzwartezeit läuft";
            run.ApproachStopChecks=0;run.ApproachStopIdentity=null;RefreshChains();
        }
        public void ConfirmOperatorRelease(string id){RouteRun run;if(!active.TryGetValue(id,out run)||run.ReleaseSelection==null||run.Stage!=RouteStage.Cancelled)throw new InvalidOperationException("Zuerst Halt und eine konkrete Hilfsauflösung anfordern.");run.OperatorReleaseConfirmed=true;run.ReleaseStatus="Vom Fahrdienstleiter örtlich geprüft und bestätigt; Schutzwartezeit bleibt wirksam.";}
        bool ProcessRelease(RouteRun run,IInterlockingWorld world) {
            if(clock()<run.ReleaseAt){run.ReleaseStatus="Noch "+Math.Ceiling(run.ReleaseAt-clock())+" s Schutzwartezeit";return false;}
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
            if(run.Route.Shunting||!run.Route.Switches.All(p=>run.ReleasedSwitches.Contains(p.Node))||!(world is ITrainStopWorld))return;
            var path=run.Route.Path;double at=0,start=0;
            foreach(var piece in path){at+=piece.Section.To-piece.Section.From;if(run.Route.Switches.Any(p=>p.Protection!=null&&p.Protection.Any(s=>s.Overlaps(piece.Section))))start=at;}
            double total=path.Sum(p=>p.Section.To-p.Section.From);
            bool stopped=tail>start+.5&&head<=total+.5&&(!run.Route.AuxiliaryExit||head>=total-1)&&((ITrainStopWorld)world).TrainStopped(run.TrainCars);
            // Never release a preceding point group or an unknown occupied area merely because a train stopped.
            var destination=new List<TrackSection>();at=0;foreach(var piece in path){if(at>=start-.001)destination.Add(piece.Section);at+=piece.Section.To-piece.Section.From;}
            stopped&=run.HeldSections.All(s=>destination.Any(d=>d.Key==s.Key))&&run.HeldSections.All(s=>world.Occupancy(s)!=TrackOccupancy.Unknown);
            run.DestinationChecks=stopped?run.DestinationChecks+1:0;
            if(run.DestinationChecks>=3)foreach(var section in destination)run.ReleasedSections.Add(section.Key);
        }
    }
}
