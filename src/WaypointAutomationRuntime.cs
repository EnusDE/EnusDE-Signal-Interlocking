using System;
using System.Collections.Generic;
using System.Linq;
using Game.State;
using Model;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        sealed class WaypointDuty {
            public string Consist;
            public string CurrentIntent;
            public MovementRole Selection;
            public bool? AutomaticOverride;
            public string Status,RequestStatusKey,ObservationStatusKey;
            public string SuppressedSignal,SuppressedIntent;
            public string ClearedTurnback;
        }

        // Saved world permission is independent of the current driver's local pause.
        readonly Dictionary<string,WaypointDuty> waypointDuties=new Dictionary<string,WaypointDuty>(StringComparer.Ordinal);
        Car[] waypointRoster=Array.Empty<Car>();
        double nextWaypointRoster;
        int waypointCursor;
        bool waypointDiscoveryHeld;
        bool WaypointDiscoveryHeld=>waypointDiscoveryHeld;
        bool WorldWaypointAutomatic=>Automation?.AutoDiscoverWaypoints==true&&!waypointDiscoveryHeld;
        bool IsWaypointAutomatic(WaypointDuty duty)=>Automation?.AutoDiscoverWaypoints==true&&(duty?.AutomaticOverride??!waypointDiscoveryHeld);
        int WaypointAutomationCount()=>waypointDuties.Values.Count(IsWaypointAutomatic);
        void ResumeWaypointDiscovery(){waypointDiscoveryHeld=false;nextWaypointRoster=0;}

        bool WaypointAutomationEnabled(Car car) {
            if(!StateManager.IsHost||!AvailableWaypointCar(car))return false;
            waypointDuties.TryGetValue(car.id,out var duty);
            return IsWaypointAutomatic(duty);
        }

        MovementRole WaypointMovementSelection(Car car)=>car!=null&&waypointDuties.TryGetValue(car.id,out var duty)?duty.Selection:MovementRole.Unassigned;

        string WaypointAutomationStatus(Car car) {
            if(car==null)return "";
            if(StateManager.IsHost) {
                string issue=WaypointReadinessIssue(car);
                if(issue!=null)return issue;
                if(waypointDuties.TryGetValue(car.id,out var duty))return LocalText.Get(duty.Status??WaypointOff());
                if(WaypointDiscoveryHeld)return LocalText.Select("STOP has paused automatic waypoint discovery.","HALT hat die automatische Wegpunkterkennung pausiert.");
                return WorldWaypointAutomatic?AwaitWaypoint():WaypointOff();
            }
            if(Entry.Shared?.LiveFresh!=true)return LocalText.Select("Waiting for host status.","Wartet auf Status vom Host.");
            var request=Entry.Shared.Live.Automation?.Requests.LastOrDefault(r=>r.Locomotive==car.id&&r.Source!="");
            return request==null?LocalText.Select("No waypoint request from this locomotive.","Keine Wegpunktanfrage dieser Lok."):LocalText.Get(request.Reason);
        }

        static string WaypointOff()=>LocalText.Select("Manual requests selected.","Manuelle Anfragen ausgewählt.");
        static string AwaitWaypoint()=>LocalText.Select("Set a fixed Auto Engineer waypoint.","Einen festen Auto-Engineer-Wegpunkt setzen.");
        static string WaitNativePath()=>LocalText.Select("Waiting for a readable waypoint path.","Wartet auf einen lesbaren Wegpunkt-Fahrweg.");
        static string ChangedWaypoint()=>LocalText.Select("Waypoint, direction or consist changed.","Wegpunkt, Fahrtrichtung oder Verband geändert.");
        static bool PendingWaypointRequest(AutomaticRequest request)=>request.Stage==AutomaticRequestStage.Waiting||request.Stage==AutomaticRequestStage.Submitted;

        static bool AvailableWaypointCar(Car car)=>car!=null&&car.IsLocomotive&&car.IsOwnedByPlayer&&!car.IsDerailed&&!car.IsInBardo;

        void CheckWaypointCar(Car car) {
            if(!RouteWorkAvailable||!OperatingReady||Automation==null||!AvailableWaypointCar(car))
                throw new InvalidOperationException(LocalText.Select("Choose an available locomotive in the host session.","Eine verfügbare Lok in der Host-Sitzung auswählen."));
        }

        string WaypointReadinessIssue(Car car) {
            if(!StateManager.IsHost)return null;
            if(!RouteWorkAvailable||!OperatingReady||Automation==null||!AvailableWaypointCar(car))
                return LocalText.Select("Choose an available locomotive in the host session.","Eine verfügbare Lok in der Host-Sitzung auswählen.");
            if(automationHookError!=null)return LocalText.Select("Native waypoint integration unavailable: ","Native Wegpunktanbindung nicht verfügbar: ")+automationHookError;
            if(!aiEnabled)
                return LocalText.Select("Enable the Auto Engineer signal connection in Settings.","In den Einstellungen die Auto-Engineer-Signalanbindung einschalten.");
            if(!Automation.BlockEnabled)return LocalText.Select("Enable block coordination for waypoint requests.","Für Wegpunktanfragen die Blockabstimmung einschalten.");
            return null;
        }

        void RequireWaypointReady(Car car) {
            CheckWaypointCar(car);
            string issue=WaypointReadinessIssue(car);
            if(issue!=null)throw new InvalidOperationException(issue);
        }

        WaypointDuty GetWaypointDuty(Car car) {
            if(waypointDuties.TryGetValue(car.id,out var duty))return duty;
            if(waypointDuties.Count>=512)
                throw new InvalidOperationException(LocalText.Select("Too many locomotive selections in this session.","Zu viele Lok-Auswahlen in dieser Spielsitzung."));
            duty=new WaypointDuty();waypointDuties.Add(car.id,duty);return duty;
        }

        const double WaypointFirstSignalRange=650;
        const double WaypointFollowSignalRange=6000;
        const int WaypointLookAhead=3;

        void SetWaypointMovement(Car car,MovementRole selection) {
            CheckWaypointCar(car);
            if(!Enum.IsDefined(typeof(MovementRole),selection))throw new ArgumentOutOfRangeException(nameof(selection));
            var duty=GetWaypointDuty(car);
            if(duty.Selection==selection)return;
            duty.Selection=selection;
            TransferWaypointControl(car,LocalText.Select("Waypoint movement selection changed.","Fahrart für Wegpunktfahrten geändert."));
            duty.SuppressedSignal=duty.SuppressedIntent=null;
            SetWaypointStatus(car.id,duty,"movement_changed",IsWaypointAutomatic(duty)?AwaitWaypoint():WaypointOff());
        }

        void SetWaypointAutomation(Car car,bool enabled)=>SetWaypointAutomation(car,enabled,WaypointMovementSelection(car));

        void SetWaypointAutomation(Car car,bool enabled,MovementRole selection) {
            CheckWaypointCar(car);
            string signature=null;
            if(enabled) {
                RequireWaypointReady(car);
                if(Automation.AutoDiscoverWaypoints!=true)
                    throw new InvalidOperationException(LocalText.Select("Enable automatic waypoint requests for this world first.","Zuerst automatische Wegpunktanfragen für diese Welt einschalten."));
                signature=AutomationNative.Signature(AutomationNative.Consist(car));
            }
            SetWaypointMovement(car,selection);
            var duty=GetWaypointDuty(car);
            duty.AutomaticOverride=enabled;
            nextWaypointRoster=0;
            if(enabled) {
                duty.Consist=signature;
                duty.CurrentIntent=null;
                duty.SuppressedSignal=duty.SuppressedIntent=null;
                SetWaypointStatus(car.id,duty,"enabled",AwaitWaypoint());
            } else {
                TransferWaypointControl(car,WaypointOff());
                SetWaypointStatus(car.id,duty,"paused",WaypointOff());
            }
        }

        void TransferWaypointControl(Car car,string reason) {
            foreach(var request in automaticRequests.Where(r=>r.Waypoint&&r.Locomotive==car.id&&PendingWaypointRequest(r)).ToArray()) {
                if(request.WaypointRun?.Entered==true||request.WaypointRun?.Permitted==true)request.HandedOver=true;
                else WithdrawAutomaticRequest(request,reason);
            }
        }

        void RequestWaypointRoute(Car car)=>RequestWaypointRoute(car,WaypointMovementSelection(car));

        void RequestWaypointRoute(Car car,MovementRole selection) {
            RequireWaypointReady(car);
            if(WaypointAutomationEnabled(car))return;
            if(automaticRequests.Any(r=>r.Locomotive==car.id&&PendingWaypointRequest(r)&&r.WaypointRun?.Entered!=true))
                throw new InvalidOperationException(LocalText.Select("This locomotive already has a route request.","Für diese Lok läuft bereits eine Fahrstraßenanfrage."));
            var intent=AutomationNative.Intent(car,allowPausedWaypoint:true);
            if(intent.Mode!=AutomationDrivingMode.Waypoint)throw new InvalidOperationException(AwaitWaypoint());
            var consist=AutomationNative.Consist(car);
            if(!AutomationNative.HasUniqueWaypointDriver(car,consist))throw new InvalidOperationException(WaypointDriverConflict());
            SetWaypointMovement(car,selection);
            var duty=GetWaypointDuty(car);
            duty.Consist=AutomationNative.Signature(consist);duty.CurrentIntent=intent.Signature;
            bool located=TryWaypointStart(car,consist,intent,duty.Selection,out _,out var signal,out var movement,out var reason,allowPausedWaypoint:true);
            QueueWaypointRequest(car,duty,intent,located?signal:null,movement,Time.realtimeSinceStartup,true,reason);
        }

        bool TryWaypointStart(Car car,Car[] consist,AutomationIntent intent,MovementRole selection,out WaypointApproach approach,out MarkerRecord source,out MovementRole movement,out string reason,bool allowPausedWaypoint=false,string requested=null,bool follow=false) {
            approach=null;source=null;movement=selection==MovementRole.Shunt?MovementRole.Shunt:MovementRole.Train;
            if(intent.Mode!=AutomationDrivingMode.Waypoint){reason=AwaitWaypoint();return false;}
            if(!intent.Ready){reason=intent.Reason??WaitNativePath();return false;}
            if(!AutomationNative.TryWaypointApproach(car,intent,out approach,out reason,allowPausedWaypoint)){reason=reason??WaitNativePath();return false;}
            var currentApproach=approach;
            SignalWorldBridge.ReadWaypointContinuation(currentApproach);
            if(!CheckWaypointTurnback(car,consist,intent,currentApproach)) {
                reason=LocalText.Select("Waiting for the whole train to stop clear of the previous points before reversing.","Wartet vor dem Richtungswechsel auf Stillstand und freie Weichen hinter dem ganzen Zug.");
                return false;
            }
            double range=follow||requested!=null?WaypointFollowSignalRange:WaypointFirstSignalRange;
            var forward=currentApproach.SignalsAhead(SignalObjects.Where(s=>!s.Planned&&(s.Kind==MarkerRecord.Main||s.Kind==MarkerRecord.Shunt)),range);
            var cars=consist.Select(c=>c.id).ToArray();
            var governing=forward.Where(s=>!IgnoreOwnSignal(s.Id)&&!CoveredWaypointVia(s,cars)).ToArray();
            source=currentApproach.NextSignal(governing,requested!=null?WaypointFollowSignalRange:WaypointFirstSignalRange);
            for(int i=1;i<WaypointLookAhead&&source!=null&&source.Id!=requested&&(follow||requested!=null);i++) {
                string entry=source.Id;
                var previous=automaticRequests.FirstOrDefault(r=>r.Waypoint&&r.Locomotive==car.id&&r.Intent==intent.Signature&&
                    PendingWaypointRequest(r)&&r.Signal==entry&&r.WaypointRun?.Permitted==true&&!r.HandedOver);
                if(previous==null)break;
                var route=previous.WaypointRun.Route;
                if(!WaypointRoutePolicy.Matches(currentApproach,source,RouteEndpoint(route.Exit),RouteMovement.Default(source,route.Shunting),
                    new RouteChainPlan{Routes=new List<RouteDefinition>{route}}))break;
                var next=RouteEndpoint(route.Exit);
                if(!governing.Any(s=>s.Id==next?.Id)||!currentApproach.TryLocateAhead(next,out _,WaypointFollowSignalRange))break;
                source=next;
            }
            if(source==null) {
                source=forward.FirstOrDefault(s=>IgnoreOwnSignal(s.Id));
                if(source==null)reason=NoWaypointSignal(currentApproach,out source);
                else reason=source.DisplayTitle+" · "+LocalText.Select("Signal is not in service or is not bound to the interlocking.","Signal ist außer Betrieb oder nicht mit dem Stellwerk verbunden.");
                return false;
            }
            string caption=source.DisplayTitle+" · ";
            if(OperatingState(source.Id)!=SignalOperatingState.Active||lampTest.Holds(source.Id))
                {reason=caption+LocalText.Select("The next signal is unavailable.","Das nächste Signal ist nicht verfügbar.");return false;}
            bool shuntOnly=!SignalCatalogue.Supports(source,false,false)&&!SignalCatalogue.Supports(source,false,true)&&SignalCatalogue.Supports(source,true,false);
            if(shuntOnly&&selection==MovementRole.Train)
                {reason=caption+LocalText.Select("The nearest signal is for shunting. Choose Shunting for this waypoint.","Das nächste Signal gehört zur Rangierfahrt. Für diesen Wegpunkt Rangierfahrt wählen.");return false;}
            if(shuntOnly&&selection==MovementRole.Unassigned) {
                // A declared shunt-only signal with a local destination can determine the movement type.
                try{WaypointDestination(source.Id,MovementRole.Shunt,approach,out _,out _);movement=MovementRole.Shunt;}
                catch(InvalidOperationException failure){reason=caption+LocalText.Get(failure.Message);return false;}
            }
            if(!SignalCatalogue.Supports(source,movement==MovementRole.Shunt,false)&&
                !(movement==MovementRole.Train&&SignalCatalogue.Supports(source,false,true)))
                {reason=caption+LocalText.Select("The next signal does not support this movement.","Das nächste Signal unterstützt diese Fahrart nicht.");return false;}
            var owner=SignalStation(source.Id);
            if(owner==null){reason=caption+LocalText.Select("Assign the next signal to a local interlocking.","Das nächste Signal einem Ortsstellwerk zuordnen.");return false;}
            if(Automation.Signals.TryGetValue(source.Id,out var rule)&&!rule.AllowWaypoint)
                {reason=caption+LocalText.Select("Waypoint requests are switched off at this signal.","Wegpunktanfragen sind an diesem Signal ausgeschaltet.");return false;}
            reason=null;return true;
        }

        bool CoveredWaypointVia(MarkerRecord signal,string[] cars) {
            if(signal.Kind!=MarkerRecord.Shunt)return false;
            var route=engine.ForViaSignal(signal.Id);
            return route!=null&&WaypointRoutePolicy.HasViaAuthority(route,signal.Id,cars,Aspect(signal.Id));
        }

        string NoWaypointSignal(WaypointApproach approach,out MarkerRecord opposite) {
            opposite=approach.SignalsAhead(SignalObjects.Where(s=>!s.Planned&&(s.Kind==MarkerRecord.Main||s.Kind==MarkerRecord.Shunt)),includeOpposite:true)
                .FirstOrDefault(s=>!approach.TryLocateAhead(s,out _));
            if(opposite!=null)return opposite.DisplayTitle+" · "+LocalText.Select("Faces the opposite direction from this waypoint path.","Zeigt entgegen der Fahrtrichtung dieses Wegpunkts.");
            return LocalText.Select("No governing signal in range before the waypoint.","Kein zuständiges Signal in Reichweite vor dem Wegpunkt.");
        }

        bool CheckWaypointTurnback(Car car,Car[] consist,AutomationIntent intent,WaypointApproach approach) {
            if(approach.Phase==0)return true;
            var duty=GetWaypointDuty(car);
            var key=intent.Signature+"|"+approach.Phase;
            if(duty.ClearedTurnback==key)return true;
            if(!world.TrainStopped(consist.Select(c=>c.id).ToArray()))return false;
            var retained=automaticRequests.Where(r=>r.Waypoint&&r.Locomotive==car.id&&r.WaypointRun?.Entered==true&&engine.Active.Contains(r.WaypointRun));
            if(retained.Any(r=>r.WaypointRun.Route.Switches.Any(p=>p.FlankOf==null&&!r.WaypointRun.ReleasedSwitches.Contains(p.Node))))return false;
            duty.ClearedTurnback=key;
            return true;
        }

        static string WaypointDriverConflict()=>LocalText.Select("The consist needs exactly one Auto Engineer driver for this waypoint.","Der Verband braucht genau eine Auto-Engineer-Lok für diesen Wegpunkt.");

        void SetWaypointStatus(string locomotive,WaypointDuty duty,string state,string reason,MarkerRecord source=null,AutomaticRequest request=null,AutomationIntent intent=null,WaypointApproach approach=null) {
            string observed=source?.Id??request?.Signal;
            string key=state+"|"+reason+"|"+observed+"|"+request?.Target+"|"+intent?.Signature+"|"+request?.Id;
            if(request==null) {
                if(duty.ObservationStatusKey==key)return;
                duty.ObservationStatusKey=key;
            } else {
                if(duty.RequestStatusKey==key)return;
                duty.RequestStatusKey=key;
            }
            duty.Status=reason;
            string caption=source?.DisplayTitle??(observed==null?null:Code(observed));
            double? distance=null;
            if(source?.Anchor!=null&&approach!=null&&approach.TryLocate(source.Anchor.Segment,source.Anchor.Meters,out double progress,out _))distance=Math.Round(progress,1);
            try {
                log?.Info("Waypoint "+locomotive+" · "+state+(caption==null?"":" · "+caption)+" · "+reason);
                Entry.RecordSignalEvent("waypoint_state",new {id=observed??locomotive,code=caption,vehicle_id=locomotive,state,reason,
                    entry=observed,exit=request?.Target,movement=request?.Movement.ToString()??duty.Selection.ToString(),request=request?.Id,
                    distance_m=distance,waypoint_segment=intent?.Target?.Segment,direction=source?.FacingReverse,source="host_waypoint",movement_verified=false});
            }catch{/* Observations never change an interlocking result. */}
        }

        void QueueWaypointRequest(Car car,WaypointDuty duty,AutomationIntent intent,MarkerRecord source,MovementRole movement,double now,bool driver,string reason=null) {
            if(!MakeAutomaticRequestRoom()) {
                SetWaypointStatus(car.id,duty,"no_request",LocalText.Select("The request list is full.","Die Anfragenliste ist voll."),source,intent:intent);
                return;
            }
            var request=new AutomaticRequest{Waypoint=true,DriverRequested=driver,Locomotive=car.id,Consist=duty.Consist,
                Signal=source?.Id,Intent=intent.Signature,Movement=movement,Created=now,
                Reason=reason??LocalText.Select("Route requested.","Fahrstraße angefragt.")};
            automaticRequests.Add(request);
            SetWaypointStatus(car.id,duty,request.Stage.ToString(),request.Reason,source,request);
        }

        void DiscoverWaypointRequests(double now) {
            if(TrainController.Shared==null||Automation?.AutoDiscoverWaypoints!=true||
                (waypointDiscoveryHeld&&!waypointDuties.Values.Any(IsWaypointAutomatic)))return;
            if(now>=nextWaypointRoster) {
                waypointRoster=TrainController.Shared.Cars.Where(AvailableWaypointCar).ToArray();
                nextWaypointRoster=now+2;
            }
            for(int i=0;i<Math.Min(8,waypointRoster.Length);i++) {
                if(waypointCursor>=waypointRoster.Length)waypointCursor=0;
                var car=waypointRoster[waypointCursor++];
                if(!AvailableWaypointCar(car))continue;
                waypointDuties.TryGetValue(car.id,out var duty);
                if(!IsWaypointAutomatic(duty))continue;
                var pending=automaticRequests.Where(r=>r.Locomotive==car.id&&PendingWaypointRequest(r)&&r.WaypointRun?.Entered!=true).ToArray();
                if(pending.Length>=WaypointLookAhead||pending.Any(r=>!r.Waypoint||r.WaypointRun?.Permitted!=true||r.HandedOver))continue;
                bool activeWaypoint=AutomationNative.HasActiveWaypoint(car);
                if(!activeWaypoint) {
                    if(duty!=null)SetWaypointStatus(car.id,duty,"no_request",AwaitWaypoint());
                    continue;
                }
                try {
                    duty=GetWaypointDuty(car);
                    string issue=WaypointReadinessIssue(car);
                    if(issue!=null){SetWaypointStatus(car.id,duty,"no_request",issue);continue;}
                    var intent=AutomationNative.Intent(car);
                    var consist=AutomationNative.Consist(car);
                    if(!AutomationNative.HasUniqueWaypointDriver(car,consist)) {
                        SetWaypointStatus(car.id,duty,"no_request",WaypointDriverConflict(),intent:intent);
                        continue;
                    }
                    string signature=AutomationNative.Signature(consist);
                    if(duty.Consist!=null&&signature!=duty.Consist&&intent.Signature==duty.CurrentIntent) {
                        SetWaypointStatus(car.id,duty,"no_request",LocalText.Select("Consist changed. Set a new waypoint or re-enable automation for this locomotive.","Verband geändert. Einen neuen Wegpunkt setzen oder die Automatik an dieser Lok erneut einschalten."),intent:intent);
                        continue;
                    }
                    if(duty.CurrentIntent!=intent.Signature)duty.ClearedTurnback=null;
                    duty.Consist=signature;duty.CurrentIntent=intent.Signature;
                    if(!TryWaypointStart(car,consist,intent,duty.Selection,out var approach,out var source,out var movement,out var reason,follow:pending.Length>0)) {
                        SetWaypointStatus(car.id,duty,"no_request",reason,source,intent:intent,approach:approach);
                        continue;
                    }
                    if(duty.SuppressedSignal==source.Id&&duty.SuppressedIntent==intent.Signature)continue;
                    if(automaticRequests.Any(r=>r.Waypoint&&r.Locomotive==car.id&&PendingWaypointRequest(r)&&r.Signal==source.Id))continue;
                    QueueWaypointRequest(car,duty,intent,source,movement,now,false);
                }catch(Exception e){if(duty!=null)SetWaypointStatus(car.id,duty,"no_request",LocalText.Get(e.Message));}
            }
        }

        bool TryCurrentWaypointCar(string id,WaypointDuty duty,out Car car,out Car[] cars) {
            car=null;cars=null;
            if(TrainController.Shared==null||!TrainController.Shared.TryGetCarForId(id,out car)||!AvailableWaypointCar(car))return false;
            cars=AutomationNative.Consist(car);
            return AutomationNative.Signature(cars)==duty.Consist;
        }

        void TickWaypointRequest(AutomaticRequest request,double now,ref bool admittedThisTick) {
            waypointDuties.TryGetValue(request.Locomotive,out var duty);
            var run=request.WaypointRun;
            try {
                if(run!=null) {
                    if(!engine.Active.Contains(run)) {
                        request.Stage=run.Entered?AutomaticRequestStage.Completed:AutomaticRequestStage.Cancelled;
                        request.Reason=run.Entered?LocalText.Select("Route finished.","Fahrstraße beendet."):LocalText.Select("Route withdrawn.","Fahrstraße zurückgenommen.");
                        if(!run.Entered)SuppressWaypointRetry(request);
                        return;
                    }
                    if(run.Stage==RouteStage.Fault||run.Stage==RouteStage.Cancelled){request.Reason=run.FaultReason??run.Reason;return;}
                    if(run.Entered||request.HandedOver) {
                        request.Reason=run.Entered?LocalText.Select("Train entered. Normal locking and release are active.","Zug eingefahren. Verschluss und Auflösung laufen regulär."):LocalText.Select("Route set. Driver has taken over.","Fahrstraße gestellt. Fahrer hat übernommen.");
                        return;
                    }
                }
                if(duty==null||!aiEnabled||!OperatingReady||automationHookError!=null||Automation?.BlockEnabled!=true)
                    throw new OperationCanceledException(LocalText.Select("Waypoint requests paused.","Wegpunktanfragen pausiert."));
                if(!request.DriverRequested&&!IsWaypointAutomatic(duty)) {
                    if(run?.Permitted==true) {
                        request.HandedOver=true;
                        request.Reason=LocalText.Select("Route set. Driver has taken over.","Fahrstraße gestellt. Fahrer hat übernommen.");
                        return;
                    }
                    throw new OperationCanceledException(LocalText.Select("Waypoint requests paused.","Wegpunktanfragen pausiert."));
                }
                if(duty.Consist!=request.Consist||!TryCurrentWaypointCar(request.Locomotive,duty,out var car,out var cars))
                    throw new OperationCanceledException(ChangedWaypoint());
                var intent=AutomationNative.Intent(car,allowPausedWaypoint:request.DriverRequested);
                if(intent.Signature!=request.Intent||intent.Mode!=AutomationDrivingMode.Waypoint)throw new OperationCanceledException(ChangedWaypoint());
                if(!AutomationNative.HasUniqueWaypointDriver(car,cars))throw new OperationCanceledException(WaypointDriverConflict());
                if(run?.Permitted==true&&intent.Ready&&run.ExpectedWaypointCars!=null) {
                    world.TrainProgress(run.Route,run.ExpectedWaypointCars,out var head,out _);
                    // The front has reached the route. Let the usual passage offset and train-tail checks finish.
                    if(head>=0){request.HandedOver=request.DriverRequested;request.Reason=LocalText.Select("Train reaching the route.","Zug erreicht die Fahrstraße.");return;}
                }
                if(!TryWaypointStart(car,cars,intent,duty.Selection,out var approach,out var source,out var movement,out var reason,request.DriverRequested,request.Signal)) {
                    if(run!=null)throw new OperationCanceledException(reason);
                    CancelWaypointPreparation(request.Id);
                    request.Reason=reason;return;
                }
                if(request.Signal==null&&request.DriverRequested){request.Signal=source.Id;request.Movement=movement;}
                if(source.Id!=request.Signal)throw new OperationCanceledException(LocalText.Select("The approaching signal has changed.","Das Signal vor dem Zug hat sich geändert."));
                if(movement!=request.Movement)throw new OperationCanceledException(LocalText.Select("The movement type changed.","Die Fahrart hat sich geändert."));
                var owner=SignalStation(source.Id);
                if(Automation.PausedStations.Contains(owner.Id)&&run?.Permitted!=true) {
                    if(run!=null)throw new OperationCanceledException(LocalText.Select("The dispatcher has taken over this interlocking.","Der Fahrdienstleiter hat dieses Stellwerk übernommen."));
                    CancelWaypointPreparation(request.Id);
                    request.Reason=LocalText.Select("The dispatcher has taken over this interlocking.","Der Fahrdienstleiter hat dieses Stellwerk übernommen.");return;
                }
                if(run!=null){
                    RequireWaypointPlan(request,request.Movement,approach,new RouteChainPlan{Routes=new List<RouteDefinition>{run.Route}});
                    if(request.DriverRequested&&run.Permitted)request.HandedOver=true;
                    request.Reason=run.Permitted?LocalText.Select("Route set.","Fahrstraße gestellt."):run.Reason;return;
                }
                if(admittedThisTick||now<request.NextAttempt)return;
                if(!TryPreparedWaypointRoutes(request,intent,request.Movement,approach,out var plans))return;
                if(plans.Count!=1)throw new InvalidOperationException(LocalText.Select("The waypoint does not identify a unique route.","Der Wegpunkt ergibt keinen eindeutigen Fahrweg."));
                var plan=plans[0];RequireWaypointPlan(request,request.Movement,approach,plan);
                var route=plan.Routes.Single();
                RequireMonitoredPlan(plan);
                RoutePointProtection.RequireCalculated(route);if(!route.Shunting)FlankProtection.RequireCalculated(route);
                world.BeginRead();
                var obstruction=InterlockingEngine.RequestObstruction(route,world);
                if(obstruction!=null)throw new InvalidOperationException(obstruction);
                admittedThisTick=true;
                var expected=cars.Select(c=>c.id).ToArray();
                bool ProveApproach() {
                    if(!RouteWorkAvailable||!aiEnabled||!OperatingReady||(!request.DriverRequested&&!IsWaypointAutomatic(duty))||
                        Automation.PausedStations.Contains(owner.Id))return false;
                    if(!TryCurrentWaypointCar(request.Locomotive,duty,out var lead,out var currentCars)||!AutomationNative.HasUniqueWaypointDriver(lead,currentCars))return false;
                    var currentIntent=AutomationNative.Intent(lead,allowPausedWaypoint:request.DriverRequested);
                    if(currentIntent.Signature!=request.Intent||!TryWaypointStart(lead,currentCars,currentIntent,duty.Selection,out var live,out var signal,out var role,out _,request.DriverRequested,request.Signal)||
                        signal.Id!=request.Signal||role!=request.Movement)return false;
                    RequireWaypointPlan(request,role,live,plan);
                    return WaypointRoutePolicy.ClearApproach(live,signal,world,expected);
                }
                double? turnback=null;
                if(approach.EndsAtTurnback&&approach.TryLocateAhead(source,out double startDistance,WaypointFollowSignalRange)) {
                    double distance=approach.Distance-startDistance;
                    if(distance>0&&distance<=route.Path.Sum(p=>p.Section.To-p.Section.From)+.02)turnback=distance;
                }
                run=engine.RequestWaypoint(route,world,expected,Entry.SettingsSnapshot().Markers.ProtectionWaitSeconds,()=>PrepareRequestedPlan(plan),ProveApproach,turnback);
                request.WaypointRun=run;
                request.Routes=new[]{route.Id};request.Target=route.Exit;request.Stage=AutomaticRequestStage.Submitted;
                CancelWaypointPreparation(request.Id);
                request.Reason=LocalText.Select("Route preparing. Signal remains at Stop during checks.","Fahrstraße läuft ein. Das Signal bleibt während der Prüfung auf Halt.");
            }catch(OperationCanceledException e){WithdrawAutomaticRequest(request,e.Message);}
            catch(Exception e) {
                if(request.WaypointRun!=null)WithdrawAutomaticRequest(request,e.Message);
                else{request.NextAttempt=now+3;request.Reason=LocalText.Get(e.Message);}
            }finally {
                if(duty!=null&&(!automaticRequests.Any(r=>r.Waypoint&&r.Locomotive==request.Locomotive&&PendingWaypointRequest(r)&&r.Created>request.Created)))
                    SetWaypointStatus(request.Locomotive,duty,request.Stage.ToString(),request.Reason,request:request);
            }
        }

        void SuppressWaypointRetry(AutomaticRequest request) {
            if(!waypointDuties.TryGetValue(request.Locomotive,out var duty))return;
            duty.SuppressedSignal=request.Signal;duty.SuppressedIntent=request.Intent;
        }

        void WithdrawWaypointRequest(AutomaticRequest request,string reason) {
            CancelWaypointPreparation(request.Id);
            var run=request.WaypointRun;
            if(run!=null&&engine.Active.Contains(run)&&!run.Entered) {
                try{engine.CancelChain(run.Route.Id,world);}
                catch(Exception e){reason+=" · "+LocalText.Get(e.Message);}
            }
            SuppressWaypointRetry(request);
            request.Stage=AutomaticRequestStage.Cancelled;request.Reason=reason;
            if(waypointDuties.TryGetValue(request.Locomotive,out var duty))
                SetWaypointStatus(request.Locomotive,duty,request.Stage.ToString(),reason,request:request);
        }

        void PauseWaypointRequests() {
            waypointDiscoveryHeld=true;
            foreach(var pair in waypointDuties) {
                if(pair.Value.AutomaticOverride==true)pair.Value.AutomaticOverride=null;
                SetWaypointStatus(pair.Key,pair.Value,"paused",WaypointOff());
            }
        }

        void ResetWaypointSession() {
            ClearWaypointPreparations();waypointDuties.Clear();
            automaticRequests.RemoveAll(r=>r.Waypoint);waypointCursor=0;waypointRoster=Array.Empty<Car>();nextWaypointRoster=0;
            waypointDiscoveryHeld=false;
        }
    }
}
