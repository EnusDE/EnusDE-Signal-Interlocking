using System;
using System.Collections.Generic;
using System.Linq;
using Game.State;
using Model;
using Track;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        AutomationHooks automationHooks;AutomationContactScene contactScene;
        string automationHookError,automationStatus="Kontakt-Automatik ausgeschaltet.";
        readonly PointPassageTracker contactPassages=new PointPassageTracker();
        readonly Dictionary<string,ContactObservation> contactObservations=new Dictionary<string,ContactObservation>();
        readonly HashSet<string> contactLatches=new HashSet<string>();
        readonly List<AutomaticRequest> automaticRequests=new List<AutomaticRequest>();
        double nextContactRead;int contactCursor;
        void ReadAutomationContacts(double now) {
            if(Automation==null||now<nextContactRead)return;nextContactRead=now+.1;
            var contacts=Automation.Contacts.ToArray();
            if(StateManager.IsHost&&TrainController.Shared!=null) {
                var cars=TrainController.Shared.Cars.Where(c=>c!=null&&!c.IsInBardo&&!c.IsDerailed).ToArray();
                for(int i=0;i<Math.Min(8,contacts.Length);i++) {
                    var contact=contacts[contactCursor++%contacts.Length];
                    try{ObserveContact(contact,cars,now);}catch(Exception e){ContactReading(contact.Id).Detail="Kontakt nicht lesbar: "+e.Message;}
                }
                contactPassages.Prune(now);
            }
            contactScene?.Refresh(contacts,StateManager.IsHost?AutomationFrame():Entry.Shared?.LiveFresh==true?Entry.Shared.Live.Automation:null,Entry.SettingsSnapshot().Markers.Visible);
        }
        ContactObservation ContactReading(string id) {
            if(!contactObservations.TryGetValue(id,out var value)){value=new ContactObservation{Id=id};contactObservations[id]=value;}return value;
        }
        void ObserveContact(AutomationContact contact,Car[] cars,double now) {
            var reading=ContactReading(contact.Id);int axles=0;var geometry=new MarkerTrackGeometry();
            if(!geometry.Resolve(contact.Anchor,out var origin,out _,out string error)){reading.Detail=error;return;}
            var seen=new HashSet<string>();
            foreach(var car in cars) {
                if(!car.WheelBoundsA.IsValid||!car.WheelBoundsB.IsValid)continue;
                if((car.WheelBoundsA.GetPosition(PositionAccuracy.High)-origin).sqrMagnitude>160*160&&
                   (car.WheelBoundsB.GetPosition(PositionAccuracy.High)-origin).sqrMagnitude>160*160)continue;
                if(contact.AxleTest)foreach(var axle in AutomationNative.Axles(car)) {
                    var at=AutomationNative.Distance(contact.Anchor,contact.Reverse,axle.Value,200);if(!at.HasValue)continue;axles++;
                    int pass=contactPassages.Observe(contact.Id+"/"+axle.Key,at.Value,now);
                    if(pass>0)reading.ForwardAxles++;if(pass<0)reading.ReverseAxles++;if(pass!=0)reading.PulseUntil=now+.4;
                }
                int crossing=0;bool near=false;
                foreach(var end in new[]{car.WheelBoundsA,car.WheelBoundsB}) {
                    var distance=AutomationNative.Distance(contact.Anchor,contact.Reverse,end,200);if(!distance.HasValue)continue;
                    int pass=contactPassages.Observe(contact.Id+"/body/"+car.id+"/"+(end.Equals(car.WheelBoundsA)?"A":"B"),distance.Value,now);
                    if(pass!=0)crossing=pass;near|=Math.Abs(distance.Value)<.5&&car.IsStopped(.05f);
                }
                if(!contact.Enabled||Automation.Enabled!=true||OperatingState(contact.Signal)!=SignalOperatingState.Active||crossing<=0&&!near)continue;
                var train=AutomationNative.Train(car,Automation);if(train==null||!seen.Add(train.Consist))continue;
                string latch=contact.Id+"/"+train.Consist;if(!contactLatches.Add(latch))continue;
                EnqueueAutomaticRequest(contact,train,now);
            }
            // Re-arm after the complete configured train has left the contact, in either direction.
            foreach(string key in contactLatches.Where(k=>k.StartsWith(contact.Id+"/",StringComparison.Ordinal)).ToArray()) {
                string hash=key.Substring(contact.Id.Length+1);var duty=Automation.Locomotives.FirstOrDefault(p=>p.Value.Consist==hash&&p.Value.Role==MovementRole.Train);
                if(duty.Key==null||!TrainController.Shared.TryGetCarForId(duty.Key,out var loco)){contactLatches.Remove(key);continue;}
                var train=AutomationNative.Train(loco,Automation);if(train==null){contactLatches.Remove(key);continue;}
                var distances=train.Cars.SelectMany(c=>new[]{c.WheelBoundsA,c.WheelBoundsB}).Select(p=>AutomationNative.Distance(contact.Anchor,contact.Reverse,p,300)).ToArray();
                if(distances.All(d=>d.HasValue&&d.Value>3)||distances.All(d=>d.HasValue&&d.Value< -5))contactLatches.Remove(key);
            }
            reading.Detail=contact.AxleTest?(axles>0?"Experiment: native Rad-/Achspunkte erfasst. Vollständigkeit modellabhängig; keine Freimeldegrundlage.":"Keine nativen Achspunkte im Nahbereich lesbar. Keine Achsfreimeldung."):"Achstest ausgeschaltet.";
        }
        void EnqueueAutomaticRequest(AutomationContact contact,AutomationTrain train,double now) {
            if(automaticRequests.Any(r=>r.Contact==contact.Id&&r.Consist==train.Consist&&(r.Stage==AutomaticRequestStage.Waiting||r.Stage==AutomaticRequestStage.Submitted)))return;
            while(automaticRequests.Count>=64) {var old=automaticRequests.FirstOrDefault(r=>r.Stage==AutomaticRequestStage.Completed||r.Stage==AutomaticRequestStage.Cancelled);if(old==null)return;automaticRequests.Remove(old);}
            automaticRequests.Add(new AutomaticRequest{Contact=contact.Id,Locomotive=train.Master,Consist=train.Consist,Signal=contact.Signal,Created=now,Reason="Kontakt erreicht; Fahrwegprüfung steht an."});
        }
        void TickAutomation(double now) {
            RefreshDirectionBlocks();RefreshLineDirections();directionBlocks.Observe(engine.Active,now);lineDirections.Observe(engine.Active,now);stationCapacity.Observe(now);
            automationStatus=Automation?.Enabled==true?"Kontakt-Automatik aktiv · nur ausdrücklich festgelegte Zugfahrten.":"Kontakt-Automatik ausgeschaltet.";
            if(blockError!=null)automationStatus=blockError;
            bool admittedThisTick=false;
            foreach(var request in automaticRequests.Where(r=>r.Stage==AutomaticRequestStage.Waiting||r.Stage==AutomaticRequestStage.Submitted).ToArray()) {
                var active=engine.Active.Where(r=>request.Routes.Contains(r.Route.Id)).ToArray();
                if(request.Stage==AutomaticRequestStage.Submitted&&active.Length==0){request.Stage=AutomaticRequestStage.Completed;request.Reason=LocalText.Get("Fahrstraßenfolge abgearbeitet / sicher aufgelöst.");continue;}
                if(request.Stage==AutomaticRequestStage.Submitted&&active.Any(r=>r.Entered)){request.Reason=LocalText.Get("Zug eingefahren; normale Zugschluss- und Verschlussprüfung läuft.");continue;}
                try {
                    if(Automation?.Enabled!=true)throw new OperationCanceledException("Automatik ausgeschaltet.");
                    if(OperatingState(request.Signal)!=SignalOperatingState.Active)throw new OperationCanceledException(WindowText("Signal is not in service.","Signal ist nicht in Betrieb."));
                    var contact=Automation.Contacts.FirstOrDefault(c=>c.Id==request.Contact);
                    if(contact==null||!contact.Enabled||contact.Signal!=request.Signal)throw new OperationCanceledException("Kontakt entfernt, deaktiviert oder geändert.");
                    if(!Automation.Signals.TryGetValue(request.Signal,out var rule)||!rule.Enabled)throw new OperationCanceledException("Signalautomatik ausgeschaltet.");
                    if(TrainController.Shared==null||!TrainController.Shared.TryGetCarForId(request.Locomotive,out var loco))throw new OperationCanceledException("Lok nicht mehr verfügbar.");
                    var train=AutomationNative.Train(loco,Automation);
                    if(train==null||train.Master!=request.Locomotive||train.Consist!=request.Consist)throw new OperationCanceledException("Betriebsart oder gekuppelter Verband geändert. Neu festlegen.");
                    var intent=AutomationNative.Intent(loco);
                    if(request.Intent!=null&&request.Intent!=intent.Signature)throw new OperationCanceledException("Fahrtrichtung / KI-Auftrag geändert.");
                    if(!intent.Ready&&request.Stage==AutomaticRequestStage.Submitted)throw new OperationCanceledException(intent.Reason);
                    var source=Object(request.Signal)??throw new OperationCanceledException("Startsignal fehlt.");
                    var positions=train.Cars.SelectMany(c=>new[]{c.WheelBoundsA,c.WheelBoundsB}).Select(p=>AutomationNative.Distance(source.Anchor,source.FacingReverse,p,10000)).ToArray();
                    if(positions.Any(p=>!p.HasValue))throw new InvalidOperationException("Zuglage zum Startsignal nicht eindeutig lesbar.");
                    double head=positions.Max(p=>p.Value);
                    if(head>.5&&request.Stage==AutomaticRequestStage.Waiting)throw new OperationCanceledException("Startsignal bereits überfahren; keine nachträgliche Anforderung.");
                    if(!double.IsNaN(request.LastHead)&&head<request.LastHead-.75)throw new OperationCanceledException("Zug fährt vom Startsignal zurück.");
                    request.LastHead=double.IsNaN(request.LastHead)?head:Math.Max(head,request.LastHead);
                    if(request.Stage==AutomaticRequestStage.Submitted){request.Reason=string.Join(" · ",active.Select(r=>r.Route.Code+": "+r.Reason));continue;}
                    var owner=SignalStation(request.Signal);
                    if(owner!=null&&Automation.PausedStations.Contains(owner.Id)){request.Reason=LocalText.Get("Selbststellbetrieb zentral pausiert: ")+owner.Code;continue;}
                    if(admittedThisTick)continue;
                    bool prepared=preparedAutomaticRoutes.TryGetValue(request.Id,out var preparation);
                    if(!prepared&&now<request.NextAttempt)continue;
                    if(prepared&&!preparation.Ready&&routeWork.Contains(preparation.Key)){request.Reason=preparation.Progress;continue;}
                    request.NextAttempt=now+rule.RetrySeconds;
                    if(!aiEnabled||loadFailed||automationHookError!=null)throw new InvalidOperationException(automationHookError??"AE-Signalanbindung nicht aktiv / nicht bereit.");
                    if(!intent.Ready)throw new InvalidOperationException(intent.Reason);
                    request.Intent=intent.Signature;
                    var failures=new List<string>();bool submitted=false,preparing=false;
                    var targets=new[]{rule.Primary,rule.Alternate}.Where(s=>!string.IsNullOrEmpty(s));
                    if(prepared&&targets.Contains(preparation.Target))targets=targets.SkipWhile(t=>t!=preparation.Target);
                    foreach(string target in targets) {
                        try {
                            // Always split a contact request at each physical main signal, including in legacy catalogue mode.
                            if(!TryPreparedAutomaticRoutes(request,contact,rule,target,out var choices)){preparing=true;break;}
                            admittedThisTick=true;
                            choices=AutomationPlanPolicy.MatchingPlans(choices,rule,intent);
                            choices=AutomaticRoutePlanner.PreferCurrent(choices,id=>world.SwitchPosition(id,out bool thrown)?(bool?)thrown:null);
                            if(choices.Count!=1)throw new InvalidOperationException(choices.Count==0?"Kein durchgehender Fahrweg.":"Mehrere Fahrwege; eindeutiges Ziel / Weichenweg konfigurieren.");
                            var plan=choices[0];RequireMonitoredPlan(plan);
                            foreach(var part in plan.Routes){var station=SignalStation(part.Entry);if(station!=null&&Automation.PausedStations.Contains(station.Id))throw new InvalidOperationException(LocalText.Select("Automatic operation paused: ","Selbststellbetrieb pausiert: ")+station.Code);}
                            foreach(var route in plan.Routes){RoutePointProtection.RequireCalculated(route);FlankProtection.RequireCalculated(route);if(!world.PathValid(route))throw new InvalidOperationException("Fahrweg geändert.");}
                            PrepareRequestedPlan(plan);
                            world.BeginRead();engine.RequestChain(plan.Routes,world,Entry.SettingsSnapshot().Markers.ProtectionWaitSeconds);
                            request.Routes=plan.Routes.Select(r=>r.Id).ToArray();request.Target=plan.Routes.Last().Exit;request.Stage=AutomaticRequestStage.Submitted;
                            request.Reason=LocalText.Get("Vollständige Folge angefordert. Fahrt erst nach normaler Festlegung.");submitted=true;break;
                        }catch(Exception e){failures.Add(Code(target)+": "+e.Message);}
                    }
                    if(!submitted&&!preparing)request.Reason=string.Join(" · ",failures)+" · Erneute Prüfung in "+rule.RetrySeconds+" s.";
                }catch(OperationCanceledException e){WithdrawAutomaticRequest(request,e.Message);}
                 catch(Exception e){if(request.Stage==AutomaticRequestStage.Submitted)WithdrawAutomaticRequest(request,e.Message);else request.Reason=e.Message;}
            }
        }
        void WithdrawAutomaticRequest(AutomaticRequest request,string reason) {
            if(preparedAutomaticRoutes.TryGetValue(request.Id,out var preparation)){routeWork.Cancel(preparation.Key);preparedAutomaticRoutes.Remove(request.Id);}
            if(request.Stage==AutomaticRequestStage.Submitted){string id=request.Routes.FirstOrDefault(value=>engine.Active.Any(r=>r.Route.Id==value));if(id!=null)try{engine.CancelChain(id,world);}catch(Exception e){reason+=" · Rücknahme wartet: "+e.Message;}}
            request.Stage=AutomaticRequestStage.Cancelled;request.Reason=reason+" Vorhandene Restverschlüsse bleiben geprüft gebunden.";
        }
        static string AutomationText(string value,int length)=>string.IsNullOrEmpty(value)?"":value.Length<=length?value:value.Substring(0,length);
        public AutomationLiveFrame AutomationFrame() {
            var frame=new AutomationLiveFrame{Status=AutomationText(automationStatus,512)};
            frame.Lines=LineDirectionFrame().ToList();
            frame.Requests=automaticRequests.Select(r=>new AutomationStatus{Id=r.Id,State=r.Stage.ToString(),Reason=AutomationText(r.Reason,512),Locomotive=r.Locomotive??"",Signal=r.Signal??"",Target=r.Target??""}).ToList();
            frame.Blocks=directionBlocks.Leases.Take(512).Select(b=>new AutomationStatus{Id=b.Id,State="Richtung gebunden",Direction=b.Direction,Reason=AutomationText(b.Reason+" · "+string.Join(", ",b.Routes.Select(id=>OperationalRoutes.FirstOrDefault(r=>r.Id==id)?.Code??id)),512)}).ToList();
            frame.Contacts=Automation?.Contacts.Select(c=>{var r=ContactReading(c.Id);return new AutomationStatus{Id=c.Id,State=c.Enabled?"aktiv":"deaktiviert",Signal=c.Signal,Count=r.ForwardAxles+r.ReverseAxles,Balance=r.ForwardAxles-r.ReverseAxles,Reason=AutomationText(r.Detail,512)};}).ToList()??new List<AutomationStatus>();
            return frame;
        }
    }
}
