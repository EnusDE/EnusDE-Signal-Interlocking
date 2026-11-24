using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Game.State;
using Model;
using Track;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        int automationPage;string automationSignal,automationContact,automationRequest,automationSite;
        AutomaticSignal signalEditorDraft;string signalEditorId,signalEditorWorld;
        internal void OpenAutomation(){if(WorkspaceAvailable)ShowWorkspace(StateManager.IsHost,7,true);}
        internal void OpenAutomationContact(string id){contactPlacementDraft=null;automationContact=id;automationPage=2;OpenAutomation();}
        internal void PlaceAutomationContact(Location location) {
            PrepareContactPlacement(location);
        }
        internal void BuildAutomationSettings(UIPanelBuilder c) {
            if(Automation==null){c.AddLabel(LocalText.Get("Für Automatik-Einstellungen zuerst eine Welt laden."));return;}
            if(!StateManager.IsHost){c.AddLabel(LocalText.Get("Blockabstimmung und Automatik werden vom Host festgelegt."));return;}
            BuildRouteModeSettings(c);
            string selectedScope=scope;bool enabled=Automation.Enabled,blocks=Automation.BlockEnabled,protect=Automation.ProtectWaypointSwitches;
            c.AddFieldToggle(LocalText.Get("Blockrichtungen automatisch abstimmen"),()=>blocks,v=>blocks=v,true);
            c.AddFieldToggle(LocalText.Get("Kontakt-Automatik für Zugfahrten"),()=>enabled,v=>enabled=v,true);
            c.AddFieldToggle(LocalText.Get("Zugeordnete Weichen gegen Wegpunkt-KI schützen"),()=>protect,v=>protect=v,true);
            c.AddLabel(LocalText.Select("Electric drives are always protected against native waypoint AI. This option additionally protects mapped points while block coordination is enabled.","Elektrische Antriebe sind immer gegen die native Wegpunkt-KI geschützt. Diese Option schützt zusätzlich zugeordnete Weichen bei aktiver Blockabstimmung."));
            c.AddLabel(LocalText.Get("Gilt für diese Welt. Bei aktiver Blockprüfung benötigen auch manuelle Zugfahrstraßen vollständige GFM-Abdeckung. Ortsinterne Rangierfahrten bleiben in ihrer normalen Fahrstraßenprüfung. Bestehende Fahrstraßen und Blockbindungen werden beim Ausschalten der Kontakt-Automatik weiter überwacht. Blockabstimmung nur bei vollständig aufgelösten Fahrstraßen ändern."));
            c.AddButton(LocalText.Get("Automatik-Einstellungen speichern"),()=>{Act(()=>{
                if(scope!=selectedScope)throw new InvalidOperationException("Welt gewechselt. Einstellungen neu öffnen.");
                if(blocks!=Automation.BlockEnabled&&(engine.Active.Count>0||directionBlocks.Leases.Count>0))throw new InvalidOperationException("Erst alle Fahrstraßen und Blockbindungen vollständig freifahren / sicher auflösen.");
                if(!blocks&&Automation.AutoDiscoverWaypoints)throw new InvalidOperationException(LocalText.Select("Disable automatic waypoint requests before disabling block coordination.","Automatische Wegpunktanfragen vor der Blockabstimmung ausschalten."));
                if(enabled&&(!aiEnabled||automationHookError!=null))throw new InvalidOperationException(automationHookError??"Zuerst AE-Signalanbindung einschalten und anwenden.");
                SaveAutomation(d=>{d.Automation.Enabled=enabled;d.Automation.BlockEnabled=blocks;d.Automation.ProtectWaypointSwitches=protect;});status=LocalText.Get("Automatik-Einstellungen gespeichert.");
            });c.SetNeedsRebuild();});
        }
        void BuildAutomationMenu(UIPanelBuilder c) {
            if(Automation==null){c.AddLabel(LocalText.Get("Anlage wird geladen."));return;}
            if(!editingWorkspace||!StateManager.IsHost){BuildAutomationStatus(c);return;}
            var pages=new List<string>{LocalText.Get("Status und Anforderungen"),LocalText.Get("Signalrollen und Zielautomatik"),LocalText.Get("Kontaktpunkte und Achstest"),LocalText.Get("Stellwerke und Annahme"),LocalText.Select("Contact automation locomotive role","Lok-Betriebsart für Kontakt-Automatik")};
            c.AddSearchChoice(LocalText.Get("Automatisierung und KI"),pages,automationPage,i=>{automationPage=i;RefreshWorkspaceFields();});
            if(automationPage==0){BuildRouteModeSettings(c);BuildAutomationStatus(c);}
            if(automationPage==1)BuildAutomaticSignal(c);
            if(automationPage==2)BuildContactEditor(c);
            if(automationPage==3)BuildStationAutomation(c);
            if(automationPage==4)BuildContactLocomotiveRole(c,TrainController.Shared?.SelectedCar);
        }
        void BuildAutomaticSignal(UIPanelBuilder c) {
            var ids=SignalObjects.Where(s=>s.Kind==MarkerRecord.Main&&!s.Planned).Select(s=>s.Id).OrderBy(Code).ToArray();
            if(ids.Length==0){c.AddLabel(LocalText.Get("Zuerst ein Hauptsignal platzieren."));return;}
            int choice=Math.Max(0,Array.IndexOf(ids,automationSignal));automationSignal=ids[choice];string id=automationSignal,selectedScope=scope;
            c.AddSearchChoice(LocalText.Get("Startsignal"),ids.Select(Code).ToList(),choice,i=>{automationSignal=ids[i];RefreshWorkspaceFields();});
            if(signalEditorId!=id||signalEditorWorld!=scope){signalEditorDraft=Automation.Signals.TryGetValue(id,out var saved)?Newtonsoft.Json.JsonConvert.DeserializeObject<AutomaticSignal>(Newtonsoft.Json.JsonConvert.SerializeObject(saved)):new AutomaticSignal();signalEditorId=id;signalEditorWorld=scope;}
            var draft=signalEditorDraft;
            BuildSignalContactStatus(c,id);
            var duties=SignalRoleText.Choices();
            c.AddField(LocalText.Get("Signalfunktion"),c.AddDropdown(duties,(int)draft.Duty,i=>draft.Duty=(SignalDuty)i));
            c.AddLabel(()=>SignalRoleText.Description(draft.Duty),UIPanelBuilder.Frequency.Periodic);
            c.AddButton(WindowText("Select start in diagram…","Start im Gleisbild auswählen …"),()=>PickSignalInDiagram(SignalStation(id)?.Id??deskId,WindowText("Choose the main signal whose target automation you want to configure.","Das Hauptsignal auswählen, dessen Zielautomatik eingerichtet werden soll."),candidate=>Object(candidate)?.Kind==MarkerRecord.Main,candidate=>{automationSignal=candidate;signalEditorId=null;RefreshWorkspaceFields();}));
            c.AddFieldToggle(LocalText.Get("Zielautomatik an diesem Signal"),()=>draft.Enabled,v=>draft.Enabled=v,true);
            c.AddFieldToggle(LocalText.Get("Langsamfahrt anfordern"),()=>draft.Reduced,v=>draft.Reduced=v,true);
            var targets=new[]{""}.Concat(SignalObjects.Where(s=>s.Kind==MarkerRecord.Main&&!s.Planned&&s.Id!=id).Select(s=>s.Id))
                .Concat(AuxiliaryTarget.Index(desks.Desks.SelectMany(d=>d.Panel.Tiles)).Where(p=>p.Value.Function==AuxiliaryFunction.RouteDestination&&p.Value.Train).Select(p=>p.Key)).Append(draft.Primary).Append(draft.Alternate).Distinct().ToArray();
            c.AddButton(WindowText("Select primary destination in diagram…","Vorrangiges Ziel im Gleisbild auswählen …"),()=>PickSignalInDiagram(SignalStation(draft.Primary)?.Id??deskId,WindowText("Choose the destination interlocking and click its main signal or train target.","Zielstellwerk auswählen und dessen Hauptsignal oder Zugziel anklicken."),candidate=>targets.Contains(candidate)&&candidate!="",candidate=>{if(signalEditorDraft!=draft)throw new InvalidOperationException(WindowText("Signal selection changed. Start again.","Signalauswahl geändert. Erneut starten."));draft.Primary=candidate;RefreshWorkspaceFields();}));
            c.AddButton(WindowText("Select alternate destination in diagram…","Ausweichziel im Gleisbild auswählen …"),()=>PickSignalInDiagram(SignalStation(draft.Alternate)?.Id??deskId,WindowText("Choose the alternate destination.","Das Ausweichziel auswählen."),candidate=>targets.Contains(candidate)&&candidate!="",candidate=>{if(signalEditorDraft!=draft)throw new InvalidOperationException(WindowText("Signal selection changed. Start again.","Signalauswahl geändert. Erneut starten."));draft.Alternate=candidate;RefreshWorkspaceFields();}));
            c.AddSearchChoice(LocalText.Get("Vorrangiges Ziel"),targets.Select(s=>s==""?LocalText.Get("Kein Ziel"):Code(s)).ToList(),Array.IndexOf(targets,draft.Primary),i=>draft.Primary=targets[i]);
            c.AddSearchChoice(LocalText.Get("Ausweichziel"),targets.Select(s=>s==""?LocalText.Get("Kein Ausweichziel"):Code(s)).ToList(),Array.IndexOf(targets,draft.Alternate),i=>draft.Alternate=targets[i]);
            c.AddButton(LocalText.Select("Assign primary target in world…","Vorrangiges Ziel in der Welt zuordnen …"),()=>BeginWorldSignalPick(chosen=>draft.Primary=chosen,()=>{automationSignal=id;automationPage=1;OpenAutomation();}));
            c.AddButton(LocalText.Select("Assign alternate target in world…","Ausweichziel in der Welt zuordnen …"),()=>BeginWorldSignalPick(chosen=>draft.Alternate=chosen,()=>{automationSignal=id;automationPage=1;OpenAutomation();}));
            c.AddFieldToggle(LocalText.Get("Manuellen Fahrbetrieb zulassen"),()=>draft.AllowManual,v=>draft.AllowManual=v,true);
            c.AddFieldToggle(LocalText.Get("Wegpunktaufträge zulassen"),()=>draft.AllowWaypoint,v=>draft.AllowWaypoint=v,true);
            string retry=SignUnits.Number(draft.RetrySeconds);c.AddField(LocalText.Get("Erneute Prüfung (s)"),c.AddInputField(retry,v=>retry=v,"5",8));
            c.AddLabel(LocalText.Get("Zuerst vollständige Folge zum vorrangigen Ziel prüfen, bei Scheitern das Ausweichziel. Beide gesperrt: warten und erneut prüfen. Rangierbetrieb löst keine Zugautomatik aus."));
            c.AddButton(LocalText.Get("Signalrolle und Automatik speichern"),()=>Act(()=>{
                if(scope!=selectedScope||automationSignal!=id)throw new InvalidOperationException("Signal erneut auswählen.");
                draft.RetrySeconds=ReadAutomationNumber(retry);draft.Validate(id);
                if(draft.Enabled&&(!SignalCatalogue.Supports(Object(id),false,draft.Reduced)||RouteEndpoint(draft.Primary)==null||draft.Alternate!=""&&RouteEndpoint(draft.Alternate)==null))throw new InvalidOperationException("Signalbegriff oder Ziel nicht vorhanden.");
                if(SignalRolePolicy.Current(desks,id)!=draft.Duty)RequireIdleAdministration();
                SignalRolePolicy.RequireLineCompatibility(desks,id,draft.Duty);
                SaveAutomation(d=>d.Automation.Signals[id]=draft);signalEditorId=null;status=LocalText.Get("Signalfunktion und Zielautomatik gespeichert.");RefreshWorkspaceFields();
            }));
        }
        static double ReadAutomationNumber(string text) {
            if(!double.TryParse((text??"").Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double result)||double.IsNaN(result)||double.IsInfinity(result))throw new InvalidOperationException("Gültige Zahl eingeben.");return result;
        }
        void BuildContactEditor(UIPanelBuilder c) {
            if(contactPlacementDraft!=null){if(contactPlacementWorld==scope){BuildContactPlacement(c);return;}contactPlacementDraft=null;}
            var contacts=Automation.Contacts.ToArray();
            c.AddLabel(LocalText.Get("Platzieren: Gleis rechts anklicken → Automatik-Kontakt platzieren. Anschließend Startsignal und Richtung prüfen."));
            if(contacts.Length==0)return;
            int index=Math.Max(0,Array.FindIndex(contacts,a=>a.Id==automationContact));automationContact=contacts[index].Id;string selectedScope=scope,id=automationContact;
            c.AddSearchChoice(LocalText.Get("Kontakt"),contacts.Select(a=>a.Name+" · "+(a.Enabled?"aktiv":"aus")).ToList(),index,i=>{automationContact=contacts[i].Id;RefreshWorkspaceFields();});
            if(contactEditorDraft?.Id!=id||contactEditorWorld!=scope){contactEditorDraft=Newtonsoft.Json.JsonConvert.DeserializeObject<AutomationContact>(Newtonsoft.Json.JsonConvert.SerializeObject(contacts[index]));contactEditorWorld=scope;}
            var draft=contactEditorDraft;
            c.AddField(LocalText.Get("Bezeichnung"),c.AddInputField(draft.Name,v=>draft.Name=v,"Kontakt",48));
            var signals=new[]{""}.Concat(SignalObjects.Where(s=>s.Kind==MarkerRecord.Main&&!s.Planned).Select(s=>s.Id)).Append(draft.Signal).Distinct().ToArray();
            c.AddSearchChoice(LocalText.Get("Angesteuertes Startsignal"),signals.Select(s=>s==""?"Kein Signal":Code(s)).ToList(),Array.IndexOf(signals,draft.Signal),i=>{draft.Signal=signals[i];draft.AutomaticSignal=false;});
            c.AddButton(LocalText.Select("Detect first main signal","Erstes Hauptsignal erkennen"),()=>Act(()=>{DetectContactStart(draft);RefreshWorkspaceFields();}));
            c.AddButton(LocalText.Select("Assign signal in world…","Signal in der Welt zuordnen …"),()=>BeginWorldSignalPick(chosen=>{draft.Signal=chosen;draft.AutomaticSignal=false;},()=>OpenAutomationContact(id)));
            c.AddField(LocalText.Get("Gleissegment"),c.AddInputField(draft.Anchor.Segment,v=>draft.Anchor.Segment=v,"Segment-ID",200));
            string meter=SignUnits.Number(draft.Anchor.Meters),side=SignUnits.Number(draft.Anchor.Lateral),height=SignUnits.Number(draft.Anchor.Height);
            c.AddField(LocalText.Get("Meter ab Segmentende A"),c.AddInputField(meter,v=>meter=v,"0",12));
            c.AddField(LocalText.Get("Seitenabstand zum Gleis (m)"),c.AddInputField(side,v=>side=v,"0,82",10));
            c.AddField(LocalText.Get("Höhe zum Gleis (m)"),c.AddInputField(height,v=>height=v,"-0,04",10));
            c.AddFieldToggle(LocalText.Get("Fahrtrichtung zum Segmentende A (sonst B)"),()=>draft.Reverse,v=>draft.Reverse=v,true);
            c.AddFieldToggle(LocalText.Get("Kontakt für Zugautomatik aktivieren"),()=>draft.Enabled,v=>draft.Enabled=v,true);
            c.AddFieldToggle(LocalText.Get("Experimentellen Achstest zählen"),()=>draft.AxleTest,v=>draft.AxleTest=v,true);
            c.AddFieldToggle(LocalText.Select("Show axle-test display on contact","Achstest-Display am Kontakt anzeigen"),()=>draft.ShowCounter,v=>draft.ShowCounter=v,true);
            c.AddLabel(LocalText.Select("Counting and the physical display are separate options. Apply both with Save contact; this axle test does not provide track-clear proof.","Zählen und sichtbares Display sind getrennte Optionen. Beides mit Kontakt speichern übernehmen; der Achstest ist kein Gleisfreimeldenachweis."));
            c.AddLabel(LocalText.Select("Contact ID: ","Kontaktkennung: ")+"AZ-"+id.Substring(0,8));
            c.AddLabel(LocalText.Select("Save the contact first, then configure the start signal's primary and alternate destination. Every intermediate main signal is included. Waypoint requests stop at the current waypoint; a new assignment needs a new trigger or manual request.","Zuerst Kontakt speichern, danach vorrangiges und alternatives Ziel des Startsignals einrichten. Jedes Zwischenhauptsignal wird einbezogen. Wegpunktanforderungen enden am aktuellen Wegpunkt; ein neuer Auftrag benötigt einen neuen Auslöser oder eine manuelle Anforderung."));
            if(!string.IsNullOrEmpty(contacts[index].Signal))c.AddButton(LocalText.Select("Configure start-signal automation","Startsignal-Automatik einrichten"),()=>Act(()=>{
                if(scope!=selectedScope||Object(contacts[index].Signal)==null)throw new InvalidOperationException(LocalText.Select("Start signal changed. Reopen the contact.","Startsignal geändert. Kontakt erneut öffnen."));
                automationSignal=contacts[index].Signal;automationPage=1;RefreshWorkspaceFields();
            }));
            c.AddButton(LocalText.Get("Kontakt speichern"),()=>Act(()=>{
                if(scope!=selectedScope||automationContact!=id)throw new InvalidOperationException("Kontakt erneut auswählen.");
                draft.Anchor.Meters=ReadAutomationNumber(meter);draft.Anchor.Lateral=ReadAutomationNumber(side);draft.Anchor.Height=ReadAutomationNumber(height);draft.Validate();
                if(!new MarkerTrackGeometry().Resolve(draft.Anchor,out _,out _,out string error))throw new InvalidOperationException(error);
                if(draft.Signal!=""&&(Object(draft.Signal)==null||!SignalCatalogue.Supports(Object(draft.Signal),false,false)))throw new InvalidOperationException("Gültiges Hauptsignal auswählen.");
                if(draft.Signal!="") {
                    if(!ContactSignalPolicy.IsNextMain(world.RoutePlanningGraph(),SignalObjects,draft,draft.Signal))throw new InvalidOperationException(LocalText.Select("A contact cannot skip a main signal. Select the first main signal in its direction.","Ein Kontakt darf kein Hauptsignal überspringen. Das erste Hauptsignal in seiner Richtung wählen."));
                    var signal=Object(draft.Signal);var segment=Graph.Shared.GetSegment(signal.Anchor.Segment);var location=new Location(segment,(float)signal.Anchor.Meters,TrackSegment.End.A);
                    var distance=AutomationNative.Distance(draft.Anchor,draft.Reverse,location,10000);
                    if(!distance.HasValue||distance.Value<.5||distance.Value>10000)throw new InvalidOperationException("Startsignal muss eindeutig mindestens 0,5 m vor dem Kontakt in seiner Fahrtrichtung liegen (höchstens 10 km). Richtung / Position prüfen.");
                }
                SaveAutomation(d=>{if(draft.Signal!=""&&!d.Automation.Signals.ContainsKey(draft.Signal))d.Automation.Signals[draft.Signal]=new AutomaticSignal();int at=d.Automation.Contacts.FindIndex(a=>a.Id==id);if(at<0)throw new InvalidOperationException("Kontakt fehlt.");d.Automation.Contacts[at]=draft;});
                contactPassages.Clear();contactLatches.RemoveWhere(k=>k.StartsWith(id+"/",StringComparison.Ordinal));contactEditorDraft=null;status=LocalText.Get("Kontakt gespeichert. Zähler sind ausschließlich ein Achstest.");RefreshWorkspaceFields();
            }));
            c.AddField(LocalText.Get("Passagen / Saldo"),()=>{var live=AutomationFrame().Contacts.FirstOrDefault(a=>a.Id==id);return live==null?"–":live.Count+" / "+live.Balance+" · "+live.Reason;},UIPanelBuilder.Frequency.Periodic);
            c.AddButton(LocalText.Get("Kontakt entfernen"),()=>Act(()=>{if(scope!=selectedScope)return;SaveAutomation(d=>d.Automation.Contacts.RemoveAll(a=>a.Id==id));RefreshWorkspaceFields();}));
        }
        void BuildStationAutomation(UIPanelBuilder c) {
            var locals=desks.Desks.Where(d=>!d.IsMaster).OrderBy(d=>d.Code).ToArray();
            if(CurrentDesk?.IsMaster==true&&locals.Length>0) {
                if(!locals.Any(d=>d.Id==automationSite))automationSite=locals[0].Id;
                c.AddSearchChoice(WindowText("Configure operating site","Betriebsstelle einrichten"),locals.Select(d=>d.Code+" · "+d.Name).ToList(),Array.FindIndex(locals,d=>d.Id==automationSite),i=>automationSite=locals[i].Id);
                c.AddButton(WindowText("Open site settings","Einstellungen der Betriebsstelle öffnen"),()=>Act(()=>{SelectDesk(automationSite,false);automationPage=3;OpenAutomation();}));
            }
            var desk=CurrentDesk;if(desk==null)return;string id=desk.Id,selectedScope=scope;bool show=desk.ShowInDispatcher,only=desk.AutomaticOnly;var acceptance=desk.Acceptance;var kind=desk.SiteKind;
            c.AddLabel(desk.Code+" · "+desk.Name);
            BuildDeskOperatingEditor(c,id);
            if(desk.IsMaster){c.AddLabel(LocalText.Get("Für Annahme und Sichtbarkeit ein örtliches Stellwerk auswählen. Das übergeordnete ESTW bleibt verfügbar."));return;}
            c.AddField(LocalText.Select("Operating site type","Art der Betriebsstelle"),c.AddDropdown(new List<string>{LocalText.Select("Station","Bahnhof"),LocalText.Select("Passing loop","Begegnungsbahnhof"),LocalText.Select("Block post / line siding","Blockstelle / Anschluss auf freier Strecke"),LocalText.Select("Line siding","Anschlussstelle"),LocalText.Select("Overtaking station","Überholbahnhof")},(int)kind,i=>kind=(OperatingSiteKind)i));
            c.AddFieldToggle(LocalText.Get("Im Fahrdienstleiter auswählbar"),()=>show,v=>show=v,true);
            c.AddFieldToggle(LocalText.Get("Automatikbetrieb – keine manuelle Startanforderung"),()=>only,v=>only=v,true);
            c.AddField(LocalText.Get("Annahme"),c.AddDropdown(new List<string>{LocalText.Get("Durchfahrt möglich"),LocalText.Get("Nur Zielankunft"),LocalText.Get("Keine Annahme")},(int)acceptance,i=>acceptance=(StationAcceptance)i));
            c.AddLabel(LocalText.Get("Zuordnung aus Signalfeldern des Ortsstellwerks. Ein-/Ausfahrsignale als Bahnhofsgrenzen festlegen; Blocksignale trennen die gemeinsame Streckenrichtung nicht."));
            c.AddButton(LocalText.Get("Stellwerksbetrieb speichern"),()=>Act(()=>{if(scope!=selectedScope||deskId!=id)throw new InvalidOperationException("Stellwerk erneut auswählen.");if(kind!=CurrentDesk.SiteKind)RequireIdleAdministration();SaveAutomation(d=>{var target=d.Desks.Single(a=>a.Id==id);target.ShowInDispatcher=show;target.AutomaticOnly=only;target.Acceptance=acceptance;target.SiteKind=kind;});status=LocalText.Get("Stellwerksbetrieb gespeichert.");RefreshWorkspaceFields();}));
        }
        void BuildCarAutomation(UIPanelBuilder c,Car car,Func<bool> stillCurrent) {
            if(car==null||!car.IsLocomotive)return;
            string worldScope=scope,actionError=null;
            float actionErrorUntil=0;
            bool Current()=>car!=null&&scope==worldScope&&stillCurrent();
            c.AddSection(LocalText.Select("Waypoint routes","Wegpunkt-Fahrstraßen"));
            c.AddField(LocalText.Select("Route status","Fahrstraßenstatus"),()=>!Current()?"–":
                actionError!=null&&UnityEngine.Time.realtimeSinceStartup<actionErrorUntil?actionError:CarWaypointCaption(car),UIPanelBuilder.Frequency.Periodic);

            if(!StateManager.IsHost||!car.IsOwnedByPlayer)return;
            if(Automation==null||!Automation.AutoDiscoverWaypoints||WaypointReadinessIssue(car)!=null) {
                if(Automation?.AutoDiscoverWaypoints!=true)c.AddLabel(LocalText.Select("World default: manual requests","Welt-Voreinstellung: manuelle Anfragen"));
                c.AddButton(LocalText.Select("Operation and automation settings…","Einstellungen für Betrieb und Automatik …"),Entry.OpenAutomationSettings);
            }

            void Run(Action change) {
                if(!Current())return;
                Act(()=>{
                    if(!StateManager.IsHost||!car.IsOwnedByPlayer)
                        throw new InvalidOperationException(LocalText.Select("The host can change requests for owned locomotives.","Der Host kann Anfragen eigener Lokomotiven ändern."));
                    change();
                });
                actionError=workspaceActionError;
                actionErrorUntil=UnityEngine.Time.realtimeSinceStartup+5;
            }
            var movements=new[]{MovementRole.Unassigned,MovementRole.Train,MovementRole.Shunt};
            var labels=new List<string>{LocalText.Select("Automatic detection","Automatisch erkennen"),LocalText.Get("Zugfahrt"),LocalText.Get("Rangierfahrt")};
            int choice=Math.Max(0,Array.IndexOf(movements,WaypointMovementSelection(car)));
            c.AddField(LocalText.Select("Movement type","Fahrart"),c.AddDropdown(labels,choice,i=>Run(()=>SetWaypointMovement(car,movements[i]))));
            c.AddFieldToggle(LocalText.Select("Automatic route requests","Fahrstraßen automatisch anfragen"),
                ()=>Current()&&WaypointAutomationEnabled(car),enabled=>Run(()=>SetWaypointAutomation(car,enabled,WaypointMovementSelection(car))),true);
            c.AddButton(LocalText.Select("Request next route once","Nächste Fahrstraße einmal anfragen"),()=>Run(()=>{
                if(WaypointAutomationEnabled(car))throw new InvalidOperationException(LocalText.Select("Automatic route requests are active for this locomotive.","Diese Lok fragt bereits automatisch Fahrstraßen an."));
                RequestWaypointRoute(car,WaypointMovementSelection(car));
            }));
        }

        string CarWaypointCaption(Car car) {
            string start=null,target=null,movement=null;
            if(StateManager.IsHost) {
                string issue=WaypointReadinessIssue(car);
                if(issue!=null)return issue;
                var request=automaticRequests.LastOrDefault(r=>r.Waypoint&&r.Locomotive==car.id&&PendingWaypointRequest(r));
                start=request?.Signal;target=request?.Target;movement=request?.Movement.ToString();
            } else if(Entry.Shared?.LiveFresh==true) {
                var request=Entry.Shared.Live.Automation?.Requests.LastOrDefault(r=>r.Source!=""&&r.Locomotive==car.id&&
                    (r.State==nameof(AutomaticRequestStage.Waiting)||r.State==nameof(AutomaticRequestStage.Submitted)));
                start=request?.Signal;target=request?.Target;movement=request?.Movement;
            }
            string message=WaypointAutomationStatus(car);
            return string.IsNullOrEmpty(start)?message:RequestMovementPrefix(movement)+Code(start)+" → "+(string.IsNullOrEmpty(target)?"…":Code(target))+" · "+message;
        }

        void BuildContactLocomotiveRole(UIPanelBuilder c,Car car) {
            if(car==null||!car.IsLocomotive||!car.IsOwnedByPlayer||Automation==null) {
                c.AddLabel(LocalText.Select("Select an owned locomotive first.","Zuerst eine eigene Lok auswählen."));
                return;
            }
            string signature;
            try{signature=AutomationNative.Signature(AutomationNative.Consist(car));}
            catch(Exception e){c.AddLabel(e.Message);return;}
            string selectedScope=scope;
            int role=(int)Automation.Role(car.id,signature);
            c.AddField(LocalText.Get("Festlegung für diese Lok"),c.AddDropdown(new List<string>{LocalText.Get("Keine Festlegung"),LocalText.Get("Rangierfahrt"),LocalText.Get("Zugfahrt")},role,i=>role=i));
            c.AddLabel(LocalText.Get("Nur Zugfahrt kann Kontakte auslösen. Andere gekuppelte Loks erhalten Keine Festlegung. Nach Kuppeln/Trennen erneut festlegen."));
            c.AddButton(LocalText.Get("Betriebsart für den Verband übernehmen"),()=>Act(()=>{
                if(scope!=selectedScope||car==null||TrainController.Shared?.SelectedCar!=car||!car.IsOwnedByPlayer)return;
                var current=AutomationNative.Consist(car);
                if(AutomationNative.Signature(current)!=signature)throw new InvalidOperationException(LocalText.Select("The consist changed. Select the locomotive again.","Der Verband hat sich geändert. Lok erneut auswählen."));
                SaveAutomation(d=>d.Automation.Assign(car.id,current.Where(a=>a.IsLocomotive).Select(a=>a.id),signature,(MovementRole)role));
                status=LocalText.Get("Lok-Betriebsart gespeichert.");
                c.SetNeedsRebuild();
            }));
        }
        static string RequestStageText(string stage) {
            switch(stage) {
                case nameof(AutomaticRequestStage.Waiting): return LocalText.Select("Waiting","Wartet");
                case nameof(AutomaticRequestStage.Submitted): return LocalText.Select("Submitted to interlocking","Beim Stellwerk angefordert");
                case nameof(AutomaticRequestStage.Completed): return LocalText.Select("Finished","Abgeschlossen");
                case nameof(AutomaticRequestStage.Cancelled): return LocalText.Select("Withdrawn","Zurückgenommen");
                default: return stage;
            }
        }
        static string RequestMovementPrefix(string movement)=>movement==nameof(MovementRole.Train)?LocalText.Get("Zugfahrt")+" · ":
            movement==nameof(MovementRole.Shunt)?LocalText.Get("Rangierfahrt")+" · ":"";
        string RequestCaption(AutomationStatus request) {
            string start=string.IsNullOrEmpty(request.Signal)?"…":Code(request.Signal);
            string destination=string.IsNullOrEmpty(request.Target)?"…":Code(request.Target);
            string locomotive=string.IsNullOrEmpty(request.Locomotive)?"":request.Locomotive+" · ";
            string movement=RequestMovementPrefix(request.Movement);
            string source=request.Source=="Driver"?LocalText.Select("Driver request","Fahreranfrage")+" · ":"";
            return locomotive+movement+start+" → "+destination+" · "+source+RequestStageText(request.State);
        }
        void BuildAutomationStatus(UIPanelBuilder c) {
            AutomationLiveFrame Read()=>StateManager.IsHost?AutomationFrame():Entry.Shared?.LiveFresh==true?Entry.Shared.Live.Automation:null;
            c.AddField(LocalText.Get("Automatik"),()=>Read()?.Status??"Keine frischen Host-Daten; Signale bleiben Halt.",UIPanelBuilder.Frequency.Periodic);
            if(automationHookError!=null)c.AddLabel(LocalText.Get("Native Anbindung: ")+automationHookError);
            if(StateManager.IsHost)c.AddLabel(directionBlocks.Blocks.Count+" automatisch ermittelte Blockketten · "+directionBlocks.Leases.Count+" Richtungsbindungen.");
            c.AddField(LocalText.Get("Blockrichtungen"),()=>string.Join("\n",Read()?.Blocks.Select(b=>b.Id+(b.Direction>0?" → ":" ← ")+b.Reason)??new string[0]),UIPanelBuilder.Frequency.Periodic);
            var requests=Read()?.Requests.AsEnumerable().Reverse().ToArray()??new AutomationStatus[0];
            c.AddButton(LocalText.Get("Statusliste aktualisieren"),RefreshWorkspaceFields);
            if(requests.Length==0) {
                c.AddField(LocalText.Get("Anforderung"),()=>{
                    var latest=Read()?.Requests.LastOrDefault();
                    return latest==null?LocalText.Select("No route requests yet.","Noch keine Fahrstraßenanfragen."):RequestCaption(latest)+" · "+latest.Reason;
                },UIPanelBuilder.Frequency.Periodic);
                return;
            }
            int index=Math.Max(0,Array.FindIndex(requests,r=>r.Id==automationRequest));automationRequest=requests[index].Id;string id=automationRequest,selectedWorld=scope;
            c.AddSearchChoice(LocalText.Get("Anforderung"),requests.Select(RequestCaption).ToList(),index,i=>{automationRequest=requests[i].Id;RefreshWorkspaceFields();});
            c.AddField(LocalText.Get("Zustand"),()=>{
                var request=Read()?.Requests.FirstOrDefault(a=>a.Id==id);
                return request==null?LocalText.Select("No longer available","Nicht mehr vorhanden"):RequestStageText(request.State)+" · "+request.Reason;
            },UIPanelBuilder.Frequency.Periodic);
            if(StateManager.IsHost)c.AddButton(LocalText.Get("Anforderung zurücknehmen"),()=>Act(()=>{
                if(!StateManager.IsHost||scope!=selectedWorld)return;
                var request=automaticRequests.FirstOrDefault(a=>a.Id==id);
                if(request==null||request.Stage==AutomaticRequestStage.Completed||request.Stage==AutomaticRequestStage.Cancelled)return;
                if(engine.Active.Any(a=>request.Routes.Contains(a.Route.Id)&&a.Entered))throw new InvalidOperationException(LocalText.Select("Train has entered the route. Use the normal route release.","Zug bereits eingefahren. Normale Fahrstraßenauflösung benutzen."));
                WithdrawAutomaticRequest(request,LocalText.Select("Withdrawn by the host.","Vom Host zurückgenommen."));
                RefreshWorkspaceFields();
            }));
        }
    }
}
