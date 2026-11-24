using System;
using System.Collections.Generic;
using System.Linq;
using Game.State;
using Model;
using UI.Windows;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        sealed class WaypointDuty {
            public string Consist;
            public MovementRole Movement;
            public bool Automatic;
            public string Status;
            public string SuppressedSignal,SuppressedIntent;
        }

        // A fresh session always starts with explicit driver control.
        readonly Dictionary<string,WaypointDuty> waypointDuties=new Dictionary<string,WaypointDuty>(StringComparer.Ordinal);
        int waypointCursor;

        bool WaypointAutomationEnabled(Car car)=>StateManager.IsHost&&car!=null&&
            waypointDuties.TryGetValue(car.id,out var duty)&&duty.Automatic;

        string WaypointAutomationStatus(Car car) {
            if(car==null)return "";
            if(StateManager.IsHost) {
                if(waypointDuties.TryGetValue(car.id,out var duty))return LocalText.Get(duty.Status??WaypointOff());
                return WaypointOff();
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

        void CheckWaypointCar(Car car) {
            if(!RouteWorkAvailable||!OperatingReady||Automation==null||car==null||!car.IsLocomotive||!car.IsOwnedByPlayer||car.IsDerailed||car.IsInBardo)
                throw new InvalidOperationException(LocalText.Select("Choose an available locomotive in the host session.","Eine verfügbare Lok in der Host-Sitzung auswählen."));
        }

        WaypointDuty ReadWaypointDuty(Car car) {
            CheckWaypointCar(car);
            if(!aiEnabled||automationHookError!=null)
                throw new InvalidOperationException(LocalText.Select("Enable the Auto Engineer signal connection in Settings.","In den Einstellungen die Auto-Engineer-Signalanbindung einschalten."));
            if(!Automation.BlockEnabled)throw new InvalidOperationException(LocalText.Select("Enable block coordination for waypoint requests.","Für Wegpunktanfragen die Blockabstimmung einschalten."));
            var cars=AutomationNative.Consist(car);
            string signature=AutomationNative.Signature(cars);
            var role=Automation.Role(car.id,signature);
            if(role!=MovementRole.Train&&role!=MovementRole.Shunt)
                throw new InvalidOperationException(LocalText.Select("Apply Train or Shunt for this locomotive first.","Für diese Lok zuerst Zugfahrt oder Rangierfahrt übernehmen."));
            return new WaypointDuty{Consist=signature,Movement=role,Status=AwaitWaypoint()};
        }

        void StoreWaypointDuty(Car car,WaypointDuty duty) {
            if(!waypointDuties.ContainsKey(car.id)&&waypointDuties.Count>=512)
                throw new InvalidOperationException(LocalText.Select("Too many locomotive selections in this session.","Zu viele Lok-Auswahlen in dieser Spielsitzung."));
            waypointDuties[car.id]=duty;
        }

        void SetWaypointAutomation(Car car,bool enabled) {
            CheckWaypointCar(car);
            if(enabled) {
                var duty=ReadWaypointDuty(car);
                duty.Automatic=true;
                StoreWaypointDuty(car,duty);
                return;
            }
            if(!waypointDuties.TryGetValue(car.id,out var current))return;
            current.Automatic=false;
            current.Status=WaypointOff();
            foreach(var request in automaticRequests.Where(r=>r.Waypoint&&r.Locomotive==car.id&&PendingWaypointRequest(r)).ToArray()) {
                if(request.WaypointRun?.Entered==true||request.WaypointRun?.Permitted==true)request.HandedOver=true;
                else WithdrawAutomaticRequest(request,current.Status);
            }
        }

        void RequestWaypointRoute(Car car) {
            var duty=ReadWaypointDuty(car);
            if(WaypointAutomationEnabled(car))return;
            if(automaticRequests.Any(r=>r.Locomotive==car.id&&PendingWaypointRequest(r)&&r.WaypointRun?.Entered!=true))
                throw new InvalidOperationException(LocalText.Select("This locomotive already has a route request.","Für diese Lok läuft bereits eine Fahrstraßenanfrage."));
            var intent=AutomationNative.Intent(car,allowPausedWaypoint:true);
            if(intent.Mode!=AutomationDrivingMode.Waypoint)throw new InvalidOperationException(AwaitWaypoint());
            bool located=TryWaypointStart(car,intent,duty.Movement,out _,out var signal,out var reason,allowPausedWaypoint:true);
            StoreWaypointDuty(car,duty);
            QueueWaypointRequest(car,duty,intent,located?signal:null,Time.realtimeSinceStartup,true,reason);
        }

        bool TryWaypointStart(Car car,AutomationIntent intent,MovementRole role,out WaypointApproach approach,out MarkerRecord source,out string reason,bool allowPausedWaypoint=false) {
            approach=null;source=null;
            if(intent.Mode!=AutomationDrivingMode.Waypoint){reason=AwaitWaypoint();return false;}
            if(!intent.Ready){reason=intent.Reason??WaitNativePath();return false;}
            if(!AutomationNative.TryWaypointApproach(car,intent,out approach,allowPausedWaypoint)){reason=WaitNativePath();return false;}
            var currentApproach=approach;
            source=currentApproach.NextSignal(SignalObjects.Where(s=>!s.Planned&&
                (s.Kind==MarkerRecord.Main||role==MovementRole.Shunt&&s.Kind==MarkerRecord.Shunt)&&
                currentApproach.TryLocateAhead(s,out _)&&!IgnoreOwnSignal(s.Id)));
            if(source==null){reason=LocalText.Select("No signal to request before the waypoint within 300 m.","Innerhalb von 300 m liegt vor dem Wegpunkt kein anzufragendes Signal.");return false;}
            if(OperatingState(source.Id)!=SignalOperatingState.Active||lampTest.Holds(source.Id))
                {reason=LocalText.Select("The next signal is unavailable.","Das nächste Signal ist nicht verfügbar.");return false;}
            if(!SignalCatalogue.Supports(source,role==MovementRole.Shunt,false)&&
                !(role==MovementRole.Train&&SignalCatalogue.Supports(source,false,true)))
                {reason=LocalText.Select("The next signal does not support this movement.","Das nächste Signal unterstützt diese Fahrart nicht.");return false;}
            var owner=SignalStation(source.Id);
            if(owner==null){reason=LocalText.Select("Assign the next signal to a local interlocking.","Das nächste Signal einem Ortsstellwerk zuordnen.");return false;}
            if(Automation.Signals.TryGetValue(source.Id,out var rule)&&!rule.AllowWaypoint)
                {reason=LocalText.Select("Waypoint requests are switched off at this signal.","Wegpunktanfragen sind an diesem Signal ausgeschaltet.");return false;}
            reason=null;return true;
        }

        void QueueWaypointRequest(Car car,WaypointDuty duty,AutomationIntent intent,MarkerRecord source,double now,bool driver,string reason=null) {
            if(!MakeAutomaticRequestRoom()) {
                duty.Status=LocalText.Select("The request list is full.","Die Anfragenliste ist voll.");
                return;
            }
            var request=new AutomaticRequest{Waypoint=true,DriverRequested=driver,Locomotive=car.id,Consist=duty.Consist,
                Signal=source?.Id,Intent=intent.Signature,Movement=duty.Movement,Created=now,
                Reason=reason??LocalText.Select("Route requested.","Fahrstraße angefragt.")};
            automaticRequests.Add(request);duty.Status=request.Reason;
        }

        void DiscoverWaypointRequests(double now) {
            if(TrainController.Shared==null||!aiEnabled||!OperatingReady||automationHookError!=null||Automation?.BlockEnabled!=true)return;
            var enabled=waypointDuties.Where(p=>p.Value.Automatic).ToArray();
            for(int i=0;i<Math.Min(8,enabled.Length);i++) {
                if(waypointCursor>=enabled.Length)waypointCursor=0;
                var pair=enabled[waypointCursor++];
                var duty=pair.Value;
                if(automaticRequests.Any(r=>r.Locomotive==pair.Key&&PendingWaypointRequest(r)&&r.WaypointRun?.Entered!=true))continue;
                try {
                    if(!TryCurrentWaypointCar(pair.Key,duty,out var car,out _)) {
                        duty.Automatic=false;duty.Status=ChangedWaypoint();continue;
                    }
                    var intent=AutomationNative.Intent(car);
                    if(!TryWaypointStart(car,intent,duty.Movement,out _,out var source,out var reason)){duty.Status=reason;continue;}
                    if(duty.SuppressedSignal==source.Id&&duty.SuppressedIntent==intent.Signature)continue;
                    if(automaticRequests.Any(r=>r.Waypoint&&r.Locomotive==pair.Key&&PendingWaypointRequest(r)&&r.Signal==source.Id))continue;
                    QueueWaypointRequest(car,duty,intent,source,now,false);
                }catch(Exception e){duty.Status=LocalText.Get(e.Message);}
            }
        }

        bool TryCurrentWaypointCar(string id,WaypointDuty duty,out Car car,out Car[] cars) {
            car=null;cars=null;
            if(TrainController.Shared==null||!TrainController.Shared.TryGetCarForId(id,out car)||car==null||
                !car.IsOwnedByPlayer||!car.IsLocomotive||car.IsDerailed||car.IsInBardo)return false;
            cars=AutomationNative.Consist(car);
            return AutomationNative.Signature(cars)==duty.Consist&&Automation.Role(id,duty.Consist)==duty.Movement;
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
                if(duty==null||!request.DriverRequested&&!duty.Automatic||!aiEnabled||!OperatingReady||automationHookError!=null||Automation?.BlockEnabled!=true)
                    throw new OperationCanceledException(LocalText.Select("Waypoint requests paused.","Wegpunktanfragen pausiert."));
                if(duty.Consist!=request.Consist||duty.Movement!=request.Movement||!TryCurrentWaypointCar(request.Locomotive,duty,out var car,out var cars))
                    throw new OperationCanceledException(ChangedWaypoint());
                var intent=AutomationNative.Intent(car,allowPausedWaypoint:request.DriverRequested);
                if(intent.Signature!=request.Intent||intent.Mode!=AutomationDrivingMode.Waypoint)throw new OperationCanceledException(ChangedWaypoint());
                if(run?.Permitted==true&&intent.Ready&&run.TrainCars!=null) {
                    world.TrainProgress(run.Route,run.TrainCars,out var head,out _);
                    // The front has reached the route. Let the usual passage offset and train-tail checks finish.
                    if(head>=0){request.HandedOver=request.DriverRequested;request.Reason=LocalText.Select("Train reaching the route.","Zug erreicht die Fahrstraße.");return;}
                }
                if(!TryWaypointStart(car,intent,request.Movement,out var approach,out var source,out var reason,request.DriverRequested)) {
                    if(run!=null)throw new OperationCanceledException(reason);
                    CancelWaypointPreparation(request.Id);
                    request.Reason=reason;return;
                }
                if(request.Signal==null&&request.DriverRequested)request.Signal=source.Id;
                if(source.Id!=request.Signal)throw new OperationCanceledException(LocalText.Select("The approaching signal has changed.","Das Signal vor dem Zug hat sich geändert."));
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
                if(!approach.TryLocateAhead(source,out _,25)) {
                    request.Reason=LocalText.Select("Route prepared. Waiting for the train near the signal.","Fahrweg vorbereitet. Wartet auf den Zug am Signal.");return;
                }
                var route=plan.Routes.Single();
                RequireMonitoredPlan(plan);
                RoutePointProtection.RequireCalculated(route);if(!route.Shunting)FlankProtection.RequireCalculated(route);
                world.BeginRead();
                var obstruction=InterlockingEngine.RequestObstruction(route,world);
                if(obstruction!=null)throw new InvalidOperationException(obstruction);
                admittedThisTick=true;
                run=engine.RequestWaypoint(route,world,cars.Select(c=>c.id).ToArray(),Entry.SettingsSnapshot().Markers.ProtectionWaitSeconds,()=>PrepareRequestedPlan(plan));
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
                    duty.Status=request.Reason;
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
            if(waypointDuties.TryGetValue(request.Locomotive,out var duty))duty.Status=reason;
        }

        void PauseWaypointRequests() {
            foreach(var duty in waypointDuties.Values){duty.Automatic=false;duty.Status=WaypointOff();}
            CustomWindowHandle.Create(CarInfoWindowId)?.SetNeedsRebuild();
        }

        void ResetWaypointSession() {
            ClearWaypointPreparations();waypointDuties.Clear();
            automaticRequests.RemoveAll(r=>r.Waypoint);waypointCursor=0;
        }
    }
}
