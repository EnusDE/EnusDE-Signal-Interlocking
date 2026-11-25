using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;
using Game.State;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly InstallationProblemReport installationProblems=new InstallationProblemReport();
        List<InstallationProblem> installationAudit=>installationProblems.Items;
        string installationAuditStatus;
        int installationAuditPage;
        bool installationAuditDetails;
        void OpenInstallationAudit(){RequirePanelEdit();ReadInstallationAudit();WorkspacePage(14);}
        void OpenProblemsFromDispatcher() {
            if(!StateManager.IsHost)return;
            string target=deskId;ShowWorkspace(true,4,false);EnterEditorDesk(target);OpenInstallationAudit();
        }
        string AuditOwner(string id)=>desks.Desks.FirstOrDefault(d=>d.Id==id) is DeskDefinition d?d.Code+" · "+d.Name:WindowText("Whole installation / assignment unresolved","Gesamte Anlage / Zuordnung ungeklärt");
        void AuditIssue(string module,string owner,string item,string cause,string effect,string action,string technical=null,bool incomplete=false) {
            installationAudit.Add(new InstallationProblem{Module=module,Owner=AuditOwner(owner),Item=item,Cause=cause,Effect=effect,Action=action,Technical=technical,Incomplete=incomplete});
        }
        void AuditCheck(string module,Action check,string owner=null,string item=null)=>installationProblems.Check(module,AuditOwner(owner),item??WindowText("Module check","Modulprüfung"),check);
        void ReadInstallationAudit() {
            RequirePanelEdit();installationProblems.Begin(scope,Entry.Shared.ConfigurationVersion,Entry.Shared.InstallationVersion);installationAuditPage=0;
            string gfm=WindowText("Track detection (GFM)","Gleisfreimeldung (GFM)"),links=WindowText("Diagram bindings","Gleisbild-Zuordnungen"),points=WindowText("Points","Weichen"),paths=WindowText("Routes and protection","Fahrwege und Schutz"),lines=WindowText("Open line / GWB","Freie Strecke / GWB"),native=WindowText("Game integration","Spielanbindung"),packs=WindowText("Optional signal models","Optionale Signalmodelle");
            AuditCheck(native,ReadSignalInventory);
            AuditCheck(gfm,()=>{
                var network=CaptureNetwork();
                foreach(var block in desks.Blocks.Values.Where(b=>b.IsCircuit)) {
                    var owners=desks.Desks.Where(d=>d.Panel.Tiles.Any(t=>t.BlockId==block.Id)).Select(d=>d.Id).DefaultIfEmpty(null).ToArray();
                    string item=string.IsNullOrWhiteSpace(block.Name)?WindowText("Detection section","Meldeabschnitt"):block.Name;
                    bool boundaryFault=false;
                    foreach(bool first in new[]{true,false}) {
                        string id=first?block.BoundaryA:block.BoundaryB;
                        var fault=GfmFaults.Classify(id,RawGfm(id),CircuitEndpoint(id),GfmFaults.End(block,first));
                        if(fault==GfmBoundaryFault.None)continue;
                        boundaryFault=true;
                        foreach(string owner in owners)AuditIssue(gfm,owner,item+" · "+(first?WindowText("Boundary A","Grenze A"):WindowText("Boundary B","Grenze B")),GfmFaults.Reason(fault),
                            WindowText("This section cannot provide a reliable clear-track report.","Dieser Abschnitt kann keine verlässliche Freimeldung liefern."),InstallationProblem.BoundaryAction(fault),id);
                    }
                    if(boundaryFault)continue;
                    var diagnosis=CircuitPathValidation.Diagnose(network,block,CircuitEndpoint(block.BoundaryA),CircuitEndpoint(block.BoundaryB),id=>PointAssignments.Caption(desks,id));
                    if(diagnosis.Fault!=CircuitPathFault.None)foreach(string owner in owners)AuditIssue(gfm,owner,item,
                        InstallationProblem.PathCause(diagnosis.Fault),
                        WindowText("Clear-track detection for this section is not verified.","Die Freimeldung dieses Abschnitts ist nicht bestätigt."),
                        InstallationProblem.PathAction(diagnosis.Fault),diagnosis.Message);
                }
            });
            AuditCheck(links,()=>{
                var numbers=NumberBindings(Entry.Shared);
                foreach(var desk in desks.Desks)foreach(var tile in desk.Panel.Tiles) {
                    string item=WindowText("Diagram field","Gleisbildfeld")+" "+(tile.X+1)+" / "+(tile.Y+1);
                    if(tile.Kind==PanelTileKind.Signal&&Object(tile.Reference)==null)AuditIssue(links,desk.Id,item,
                        WindowText("The assigned signal is missing or unavailable.","Das zugeordnete Signal fehlt oder ist nicht verfügbar."),
                        WindowText("The field cannot operate its intended signal.","Das Feld kann das vorgesehene Signal nicht bedienen."),
                        WindowText("Select the field in the editor and assign the intended completed world signal.","Feld im Editor auswählen und das vorgesehene fertiggestellte Weltsignal zuordnen."),tile.Reference);
                    if(tile.Target?.Function==AuxiliaryFunction.RouteDestination&&!string.IsNullOrEmpty(tile.Target.ReferenceSignal)&&tile.Target.Endpoint(Object)==null)AuditIssue(links,desk.Id,item,
                        WindowText("The auxiliary destination's reference signal is missing or unfinished.","Das Bezugssignal des Hilfsziels fehlt oder ist nicht fertiggestellt."),
                        WindowText("The auxiliary destination cannot be resolved.","Das Hilfsziel kann nicht bestimmt werden."),
                        WindowText("Select the auxiliary destination and assign its reference signal again.","Hilfsziel auswählen und sein Bezugssignal erneut zuordnen."),tile.Target.ReferenceSignal);
                    if(tile.Kind==PanelTileKind.TrainNumber&&numbers.ConflictingField(tile))AuditIssue(links,desk.Id,item,
                        WindowText("Several independent train-number fields own the same signal.","Mehrere eigenständige Zugnummernfelder sind für dasselbe Signal zuständig."),
                        WindowText("Train-number ownership is ambiguous.","Die Zugnummern-Zuständigkeit ist mehrdeutig."),
                        WindowText("Keep one owning number field; configure additional displays as mirrors.","Ein zuständiges Nummernfeld behalten; weitere Anzeigen als Spiegel einrichten."));
                }
            });
            AuditCheck(points,()=>{foreach(var pair in desks.Points) {
                string item=pair.Value.Code;
                AuditCheck(points,()=>{
                    if(!world.SwitchPosition(pair.Key,out _))AuditIssue(points,pair.Value.Desk,item,
                        WindowText("The point is missing from the loaded world.","Die Weiche fehlt in der geladenen Spielwelt."),
                        WindowText("Its position and route protection cannot be verified.","Ihre Stellung und der Fahrwegschutz sind nicht nachweisbar."),
                        WindowText("Check the world and reassign the intended point in the editor.","Spielwelt prüfen und die vorgesehene Weiche im Editor erneut zuordnen."),pair.Key);
                    else if(PointCircuitVisibility.Enabled(desks,pair.Key)&&PointBoundaries(pair.Key).Count(b=>b!=null)!=3)AuditIssue(points,pair.Value.Desk,item,
                        WindowText("The point's three detection boundaries are incomplete.","Die drei Freimeldegrenzen der Weiche sind unvollständig."),
                        WindowText("The point area cannot be reliably reported clear.","Der Weichenbereich kann nicht verlässlich freigemeldet werden."),InstallationProblem.BoundaryAction(GfmBoundaryFault.PointUnavailable),pair.Key);
                },pair.Value.Desk,item);
            }});
            AuditCheck(paths,()=>{foreach(var route in routes) {
                string owner=SignalStation(route.Entry)?.Id;
                if(!world.PathValid(route))AuditIssue(paths,owner,route.Code,
                    WindowText("The saved route no longer matches the track bindings.","Der gespeicherte Fahrweg passt nicht mehr zur Gleiszuordnung."),
                    WindowText("This route is not ready for admission.","Dieser Fahrweg ist nicht bereit zum Einstellen."),
                    WindowText("Review the route in the editor and recalculate its path and protection.","Fahrweg im Editor prüfen und Fahrweg sowie Schutz neu berechnen."));
                else try{FlankProtection.RequireCalculated(route);}catch(Exception e){AuditIssue(paths,owner,route.Code,
                    WindowText("A current flank-protection calculation is missing or invalid.","Eine aktuelle Flankenschutzberechnung fehlt oder ist ungültig."),
                    WindowText("Route protection is not confirmed.","Der Fahrwegschutz ist nicht bestätigt."),
                    WindowText("Review and recalculate flank protection in route setup; an auxiliary destination alone is not flank protection.","Flankenschutz in der Fahrwegeinrichtung prüfen und neu berechnen; ein Hilfsziel allein ist kein Flankenschutz."),e.Message);}
            }
            foreach(var run in VisibleRuns.Where(r=>InstallationProblem.RouteFault(r.Stage)))AuditIssue(paths,SignalStation(run.Route.Entry)?.Id,run.Route.Code,
                WindowText("An active route has entered a fault state.","Ein aktiver Fahrweg ist in den Störungszustand gewechselt."),
                WindowText("Required protective locks remain in place.","Erforderliche Schutzverschlüsse bleiben bestehen."),
                WindowText("Open all routes and retained locks, inspect the cause and use only the normal protected release procedure.","Festlegungen aller Stellwerke öffnen, Ursache prüfen und ausschließlich das reguläre gesicherte Auflöseverfahren verwenden."),run.FaultReason??run.Reason);
            });
            AuditCheck(lines,()=>{foreach(var state in lineDirections.States.Where(s=>!string.IsNullOrEmpty(s.Error)))AuditIssue(lines,state.Definition.StationA,state.Definition.Code+" · "+state.Definition.Name,
                WindowText("The line connection reports a configuration or direction-protection problem.","Die Streckenverbindung meldet ein Konfigurations- oder Richtungsschutzproblem."),
                WindowText("A new line permission may be blocked; existing protection must remain.","Eine neue Streckenerlaubnis kann gesperrt sein; bestehender Schutz muss erhalten bleiben."),
                WindowText("Open the line / GWB connection and check its end signals, detection and retained direction.","Strecken-/GWB-Verbindung öffnen und Endsignale, Freimeldung sowie gebundene Richtung prüfen."),state.Error);});
            if(operatingNativeFault)AuditIssue(native,null,WindowText("Signal-state synchronization","Signalzustands-Abgleich"),
                WindowText("The last signal-state synchronization with the game failed.","Der letzte Signalzustands-Abgleich mit dem Spiel ist fehlgeschlagen."),
                WindowText("Operating readiness is restricted.","Die Betriebsbereitschaft ist eingeschränkt."),
                WindowText("Check the world state and repeat inspection after the normal retry; export diagnostics if the fault persists.","Spielweltzustand prüfen und nach dem regulären Wiederholversuch erneut prüfen; bei anhaltender Störung Diagnose exportieren."),operatingNativeError);
            AuditCheck(packs,()=>{foreach(var marker in objects.Objects.Where(m=>!string.IsNullOrEmpty(m.PresentationPack)&&SignalPresentationPacks.Find(m.PresentationPack)==null))AuditIssue(packs,SignalStation(marker.Id)?.Id,marker.DisplayTitle,
                WindowText("The selected optional model pack is not registered on this computer.","Das gewählte optionale Modellpaket ist auf diesem Rechner nicht registriert."),
                WindowText("The built-in fallback is shown; this is not a movement permission.","Die eingebaute Ersatzdarstellung wird gezeigt; dies ist keine Fahrterlaubnis."),
                WindowText("Check the pack's installation and compatibility, or select the built-in model.","Installation und Kompatibilität des Pakets prüfen oder das eingebaute Modell wählen."),marker.PresentationPack);});
            if(!string.IsNullOrEmpty(SignalPresentationPacks.LastError))AuditIssue(packs,null,WindowText("Model renderer","Modelldarstellung"),
                WindowText("An optional model provider reported a rendering failure.","Ein optionaler Modellanbieter hat einen Darstellungsfehler gemeldet."),
                WindowText("The model may use a fallback. Signal authority is still owned by the main mod.","Das Modell kann die Ersatzdarstellung verwenden. Die Signalhoheit bleibt beim Hauptmod."),
                WindowText("Check pack compatibility and export diagnostics if the failure repeats; this is the last reported error and may already have recovered.","Paketkompatibilität prüfen und bei Wiederholung Diagnose exportieren; dies ist der zuletzt gemeldete Fehler und kann bereits behoben sein."),SignalPresentationPacks.LastError);
            if(aiEnabled&&automationHookError!=null)AuditIssue(WindowText("Waypoint automation","Wegpunkt-Automatik"),null,WindowText("Native AI connection","Native KI-Anbindung"),
                WindowText("The waypoint automation connection to the game is unavailable.","Die Spielanbindung der Wegpunkt-Automatik ist nicht verfügbar."),
                WindowText("Automatic waypoint movements cannot be handled through this connection.","Automatische Wegpunktfahrten können über diese Anbindung nicht abgewickelt werden."),
                WindowText("Review automation settings and diagnostics. Do not bypass signal or route admission checks.","Automatikeinstellungen und Diagnose prüfen. Signal- und Fahrwegprüfungen nicht umgehen."),automationHookError);
            installationAuditStatus=DateTime.Now.ToString("HH:mm:ss")+WindowText(" · Findings: "," · Befunde: ")+installationAudit.Count;
        }
        void RefreshInstallationElements() {
            RequirePanelEdit();RequireIdleAdministration();string previous=deskId;
            PauseGfmBackground();Entry.ReloadBackupConfiguration();ReadSignalInventory();
            numberIndex=null;targetLibrary=null;sharedPointCircuits=null;circuitValidationInstall=-1;circuitValidationConfiguration=-1;
            ShowWorkspace(true,4,false);if(desks.Desks.Any(d=>d.Id==previous))EnterEditorDesk(previous);
            ReadInstallationAudit();WorkspacePage(14);
            status=WindowText("All installation elements reloaded. Saved settings and IDs retained; review unresolved bindings below.","Alle Anlagenelemente neu eingelesen. Gespeicherte Einstellungen und IDs erhalten; ungeklärte Zuordnungen unten prüfen.");
        }
        void BuildInstallationAudit(UIPanelBuilder c) {
            c.AddSection(WindowText("Module problems and next steps","Modulprobleme und nächste Schritte"));
            c.AddLabel(WindowText("Read-only snapshot of saved GFM, diagram bindings, points, routes/protection, known line faults, game/AI integration and missing model packs. It does not inspect every automation request or prove complete detection coverage. Occupied track, normal waiting, disabled connections and deliberately paused automation are not faults by themselves.","Lesender Schnappschuss gespeicherter GFM, Gleisbild-Zuordnungen, Weichen, Fahrwege/Schutz, bekannter Streckenstörungen, Spiel-/KI-Anbindung und fehlender Modellpakete. Nicht jede Automatikanfrage und keine vollständige Freimeldeabdeckung werden geprüft. Belegte Gleise, normales Warten, abgeschaltete Verbindungen und bewusst pausierte Automatik sind allein keine Störungen."));
            c.AddButton(WindowText("Check modules again","Module erneut prüfen"),()=>Act(()=>{ReadInstallationAudit();RefreshWorkspaceFields();}));
            if(installationAuditStatus!=null)c.AddLabel(installationAuditStatus);
            bool stale=installationProblems.Stale(scope,Entry.Shared.ConfigurationVersion,Entry.Shared.InstallationVersion);
            if(stale)c.AddLabel(WindowText("Configuration or world changed: check again before relying on these findings.","Konfiguration oder Welt geändert: Vor Verwendung dieser Befunde erneut prüfen."));
            c.AddLabel(WindowText("Live states may change after this snapshot. No detection, route, lock or automation setting is changed by this check.","Betriebszustände können sich nach diesem Schnappschuss ändern. Diese Prüfung verändert keine Freimeldung, Fahrstraße, Verschlüsse oder Automatikeinstellung."));
            if(installationAudit.Count==0&&!stale)c.AddLabel(WindowText("No findings in the checks listed above. This is not a clear-track report or an in-game acceptance test.","Keine Befunde in den oben genannten Prüfungen. Dies ist keine Gleisfreimeldung und kein Ingame-Abnahmetest."));
            c.AddButton(installationAuditDetails?WindowText("Hide technical details","Technische Einzelheiten ausblenden"):WindowText("Show technical details","Technische Einzelheiten anzeigen"),()=>{installationAuditDetails=!installationAuditDetails;RefreshWorkspaceFields();});
            int pages=Math.Max(1,(installationAudit.Count+24)/25);installationAuditPage=Math.Min(installationAuditPage,pages-1);
            c.AddLabel(WindowText("Page ","Seite ")+(installationAuditPage+1)+" / "+pages);
            if(installationAuditPage>0)c.AddButton(WindowText("Previous findings","Vorherige Befunde"),()=>{installationAuditPage--;RefreshWorkspaceFields();});
            if(installationAuditPage+1<pages)c.AddButton(WindowText("More findings","Weitere Befunde"),()=>{installationAuditPage++;RefreshWorkspaceFields();});
            foreach(var group in installationAudit.OrderBy(i=>i.Owner).ThenBy(i=>i.Module).ThenBy(i=>i.Item).Skip(installationAuditPage*25).Take(25).GroupBy(i=>i.Owner)) {
                c.AddSection(group.Key);
                string ownerId=desks.Desks.FirstOrDefault(d=>AuditOwner(d.Id)==group.Key)?.Id;
                if(ownerId!=null)c.AddButton(WindowText("Open this interlocking's diagram","Gleisbild dieses Stellwerks öffnen"),()=>Act(()=>{
                    RequirePanelEdit();
                    if(installationProblems.Stale(scope,Entry.Shared.ConfigurationVersion,Entry.Shared.InstallationVersion))throw new InvalidOperationException(WindowText("Check modules again before opening an outdated finding.","Module erneut prüfen, bevor ein veralteter Befund geöffnet wird."));
                    OpenEditorCatalogue();EnterEditorDesk(ownerId);
                }));
                foreach(var issue in group) {
                    c.AddLabel(issue.Module+" · "+issue.Item);
                    c.AddLabel((issue.Incomplete?WindowText("Not fully checked: ","Nicht vollständig geprüft: "):WindowText("Problem: ","Problem: "))+issue.Cause);
                    c.AddLabel(WindowText("Effect: ","Auswirkung: ")+issue.Effect);
                    c.AddLabel(WindowText("Next step: ","Nächster Schritt: ")+issue.Action);
                    if(installationAuditDetails&&!string.IsNullOrEmpty(issue.Technical))c.AddLabel(WindowText("Technical detail: ","Technisches Detail: ")+issue.Technical);
                }
            }
            c.AddButton(WindowText("Open world bindings","Spielwelt-Zuordnungen öffnen"),()=>WorkspacePage(3));
            c.AddButton(WindowText("Open saved routes","Gespeicherte Fahrwege öffnen"),()=>WorkspacePage(2));
            c.AddButton(WindowText("Open line / GWB connections","Strecken-/GWB-Verbindungen öffnen"),OpenFreeLines);
            c.AddButton(WindowText("Open routes and retained locks","Fahrwege und Festlegungen öffnen"),()=>{ShowWorkspace(false,6,true);WorkspacePage(6);});
            c.AddButton(WindowText("Open section manager / GFM inspection","Abschnittmanager / GFM-Prüfung öffnen"),OpenGfmInspection);
            c.AddButton(WindowText("Open GFM detection / rescan","GFM-Erkennung / Neuscan öffnen"),OpenExistingDetection);
            c.AddSection(WindowText("Separate maintenance action","Gesonderte Wartungsaktion"));
            c.AddLabel(WindowText("Reload saved installation elements separately. This is not part of the problem scan and retains saved settings and IDs.","Gespeicherte Anlagenelemente gesondert neu einlesen. Dies gehört nicht zur Problemprüfung; gespeicherte Einstellungen und IDs bleiben erhalten."));
            c.AddButton(WindowText("Refresh all installation elements","Alle Anlagenelemente aktualisieren"),()=>Act(RefreshInstallationElements));
            c.AddButton(WindowText("Export diagnostic ZIP","Diagnose-ZIP exportieren"),Entry.ExportDiagnostics);
            c.AddField(WindowText("Export result","Exportergebnis"),()=>Entry.DiagnosticExportStatus,UIPanelBuilder.Frequency.Periodic);
        }
    }
}
