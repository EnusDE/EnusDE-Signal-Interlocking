using System;
using System.Linq;
using System.Collections.Generic;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string selectedLine;LineTrackDefinition lineDraft;
        readonly List<RouteChainPlan> lineForwardPlans=new List<RouteChainPlan>(),lineReversePlans=new List<RouteChainPlan>();
        int lineForwardChoice,lineReverseChoice;
        string lineGeometryShape;
        string lineOperatingStation;
        LineIndicatorTarget lineIndicatorTarget;
        void OpenFreeLines(){OpenLineSetup(null);}
        void OpenLineSetup(LineIndicatorTarget target) {
            if(editingWorkspace)RequirePanelEdit();else if(!WorkspaceAvailable)return;
            pendingLineRemoval=null;guidedLineStep=0;guidedLineHelp=null;guidedControlPreview=null;guidedPermissions.Clear();lineIndicatorTarget=target;selectedLine=target?.Reference;lineDraft=desks.FreeLines.FirstOrDefault(l=>l.Id==selectedLine)?.Clone();
            lineForwardPlans.Clear();lineReversePlans.Clear();ClearLineDetectionProposal();
            workspaceActionError=null;if(guidedLineSetup&&editingWorkspace)status=WindowText("Follow the current tutorial step.","Dem aktuellen Tutorialschritt folgen.");
            WorkspacePage(9);
            if(editingWorkspace&&target!=null&&lineDraft==null)NewLineDraft();
        }
        void OpenLineSetupForField(int x,int y){RequirePanelEdit();OpenLineSetup(LineIndicatorTarget.Capture(desks,deskId,x,y));}
        void NewLineDraft() {
            RequirePanelEdit();pendingLineRemoval=null;guidedLineStep=0;guidedLineHelp=null;guidedControlPreview=null;guidedPermissions.Clear();lineDraft=LineSetupWorkflow.Create(desks,deskId);if(guidedLineSetup)lineDraft.Mode=LineTrackMode.Gleiswechselbetrieb;selectedLine=lineDraft.Id;
            lineForwardPlans.Clear();lineReversePlans.Clear();ClearLineDetectionProposal();RefreshWorkspaceFields();
        }
        void SelectLine(string id) {if(selectedLine!=id){guidedLineStep=0;guidedLineHelp=null;guidedControlPreview=null;}pendingLineRemoval=null;guidedPermissions.Clear();selectedLine=id;lineDraft=desks.FreeLines.FirstOrDefault(l=>l.Id==id)?.Clone();lineForwardPlans.Clear();lineReversePlans.Clear();ClearLineDetectionProposal();RefreshWorkspaceFields();}
        void BuildFreeLines(UIPanelBuilder c) {
            c.AddSection(WindowText("Open line, blocks and bidirectional working","Freie Strecke, Block und GWB"));
            if(pendingLineRemoval!=null){BuildLineRemoval(c);return;}
            BuildConfigurationBlockers(c);
            c.AddLabel(WindowText("One entry per physical line track between neighbouring interlockings. Separate parallel tracks have independent permissions. Detection sections must already be defined.","Ein Eintrag je physischem Streckengleis zwischen Nachbarstellwerken. Parallele Gleise erhalten getrennte Erlaubnisse. Freimeldeabschnitte müssen vorher eingerichtet sein."));
            if(desks==null)return;
            var choices=LineSetupWorkflow.Visible(desks,deskId).ToArray();
            if(choices.Length==0)c.AddLabel(WindowText("No connection for this interlocking yet.","Für dieses Stellwerk ist noch keine Verbindung eingerichtet."));
            if(choices.Length>0) {
                if(!choices.Any(l=>l.Id==selectedLine)&&(!editingWorkspace||lineDraft==null)){selectedLine=choices[0].Id;lineDraft=choices[0].Clone();lineForwardPlans.Clear();lineReversePlans.Clear();}
                bool unsaved=lineDraft!=null&&!desks.FreeLines.Any(l=>l.Id==lineDraft.Id);int offset=unsaved?1:0;
                var labels=(unsaved?new[]{WindowText("New connection draft","Neue Verbindung – Entwurf")}:Array.Empty<string>()).Concat(choices.Select(l=>l.Code+" · "+l.Name+" · "+l.Track)).ToList();
                c.AddSearchChoice(WindowText("Line / track","Strecke / Gleis"),labels,Math.Max(0,Array.FindIndex(choices,l=>l.Id==selectedLine)+offset),i=>{if(i>=offset)SelectLine(choices[i-offset].Id);});
            }
            if(editingWorkspace&&StateManager.IsHost) {
                c.AddButton(WindowText("Create block / line connection","Block-/Streckenverbindung anlegen"),()=>Act(NewLineDraft));
                c.AddButton(guidedLineSetup?WindowText("Switch to manual setup","Zur manuellen Einrichtung wechseln"):WindowText("Start guided setup","Geführte Einrichtung starten"),()=>{guidedLineSetup=!guidedLineSetup;guidedLineStep=0;guidedLineHelp=null;guidedControlPreview=null;workspaceActionError=null;RefreshWorkspaceFields();});
                if(lineDraft!=null)BuildLineEditor(c,lineDraft);
            } else {
                foreach(var line in choices.Where(l=>selectedLine==null||l.Id==selectedLine))BuildLineOperation(c,line);
            }
            c.AddButton(WindowText("Refresh line indications","Streckenanzeigen aktualisieren"),RefreshWorkspaceFields);
            if(editingWorkspace)c.AddButton(WindowText("Back to track diagram","Zurück zum Gleisbild"),()=>{lineIndicatorTarget=null;panelPage=1;ResetEditor();CloseWorkspaceContext();Rebuild(false);});
        }
        void ResetLineDetection(LineTrackDefinition draft){draft.Forward.Clear();draft.Reverse.Clear();draft.Anchors.Clear();draft.Topology="";lineForwardPlans.Clear();lineReversePlans.Clear();ClearLineDetectionProposal();}
        void BuildLineEditor(UIPanelBuilder c,LineTrackDefinition draft) {
            string worldScope=scope;int step=guidedLineStep;bool guided=guidedLineSetup;bool current()=>ReferenceEquals(draft,lineDraft)&&selectedLine==draft.Id&&scope==worldScope&&editingWorkspace&&panelPage==9&&guided==guidedLineSetup&&(!guided||step==guidedLineStep);
            if(desks.FreeLines.Exists(l=>l.Id==draft.Id))c.AddButton(WindowText("Delete this line connection…","Diese Streckenverbindung löschen …"),()=>Act(()=>{if(current())PreviewLineRemoval(draft.Id);}));
            if(BlockDiagram.Area(panel,draft.Id)!=null)c.AddButton(WindowText("Edit block assembly view…","Ansicht des Blockverbunds bearbeiten …"),()=>{if(current())OpenBlockViewEditor(draft.Id,deskId);});
            if(guidedLineSetup){BuildGuidedLineEditor(c,draft,current);return;}
            if(!guidedLineSetup)c.AddLabel(WindowText("1. Choose neighbouring interlockings. 2. Assign their departure/entry signals. 3. Detect the track and review detection. 4. Apply; the selected indicator is linked automatically.","1. Nachbarstellwerke wählen. 2. Aus-/Einfahrsignale zuordnen. 3. Gleisweg erkennen und GFM prüfen. 4. Übernehmen; der gewählte Melder wird automatisch verknüpft."));
            if(lineIndicatorTarget!=null)c.AddField(WindowText("Indicator to link","Zu verknüpfender Melder"),LineStationCode(lineIndicatorTarget.Desk)+" · "+(lineIndicatorTarget.X+1)+" / "+(lineIndicatorTarget.Y+1));
            if(LineStep(0)) {
            BuildLineConnection(c,draft,current);
            }
            if(LineStep(1)) {
            c.AddSection(WindowText("2 · End signals","2 · Endsignale"));
            BuildLineQuickSetup(c,draft,current);
            c.AddLabel(WindowText("Configure departures and arrivals for each direction. Several departure signals may share the same line. An arrival can be a main signal or a configured train target; four physical signals are not required. The first item supplies the reference path; every added item is checked against it.","Ausfahrten und Ankunftsziele je Richtung einrichten. Mehrere Ausfahrsignale dürfen dieselbe Strecke nutzen. Ein Ankunftsziel kann ein Hauptsignal oder ein eingerichtetes Zugziel sein; vier physische Signale sind nicht erforderlich. Der erste Eintrag liefert den Referenzfahrweg; weitere Einträge werden daran geprüft."));
            BuildLineSignalGroup(c,draft,LineSignalGroup.ADepartures,SetupStationName(draft.StationA)+WindowText(" · departures / starts"," · Ausfahrten / Starts"),current);
            BuildLineSignalGroup(c,draft,LineSignalGroup.BArrivals,SetupStationName(draft.StationB)+WindowText(" · entries / arrival targets"," · Einfahrten / Ankunftsziele"),current);
            if(draft.Mode!=LineTrackMode.Directional) {
                BuildLineSignalGroup(c,draft,LineSignalGroup.BDepartures,SetupStationName(draft.StationB)+WindowText(" · departures / starts"," · Ausfahrten / Starts"),current);
                BuildLineSignalGroup(c,draft,LineSignalGroup.AArrivals,SetupStationName(draft.StationA)+WindowText(" · entries / arrival targets"," · Einfahrten / Ankunftsziele"),current);
            }
            }
            if(LineStep(2)) {
            c.AddSection(WindowText("3 · Track path and detection","3 · Gleisweg und Gleisfreimeldung"));
            c.AddButton(WindowText("Detect line path automatically","Streckenverlauf automatisch erkennen"),()=>Act(()=>{if(!current())return;RequirePanelEdit();try{ClearLineDetectionProposal();DetectLine(draft);}finally{RefreshWorkspaceFields();}}));
            if(lineForwardPlans.Count>1)c.AddSearchChoice(SetupStationName(draft.StationA)+" → "+SetupStationName(draft.StationB),lineForwardPlans.Select(LinePlanCaption).ToList(),lineForwardChoice,i=>Act(()=>{if(!current())return;lineForwardChoice=i;CaptureLineDetection(draft);RefreshWorkspaceFields();}));
            if(lineReversePlans.Count>1)c.AddSearchChoice(SetupStationName(draft.StationB)+" → "+SetupStationName(draft.StationA),lineReversePlans.Select(LinePlanCaption).ToList(),lineReverseChoice,i=>Act(()=>{if(!current())return;lineReverseChoice=i;CaptureLineDetection(draft);RefreshWorkspaceFields();}));
            if(draft.Forward.Count>0) {
                c.AddLabel(WindowText("Detected forward / reverse sections: ","Erkannte Abschnitte hin / zurück: ")+draft.Forward.Count+" / "+draft.Reverse.Count);
                try {var axis=draft.Axis;c.AddLabel(WindowText("Shared line length (m): ","Gemeinsame Streckenlänge (m): ")+Math.Round(axis.Sum(p=>p.Section.To-p.Section.From)));}catch(Exception e){c.AddLabel(e.Message);}
                c.AddLabel(LineConfigurationError(draft)??WindowText("Path and detection coverage verified.","Gleisweg und Freimeldeabdeckung geprüft."));
                BuildLineDetectionProposal(c,draft,current);
                BuildBlockLayout(c,draft,current);
            }
            }
            if(LineStep(3)) {
            BuildLineNumberOverview(c,draft,current);
            if(guidedLineSetup)BuildGuidedPermissionPositions(c,draft);
            c.AddSection(WindowText("4 · Save and enable","4 · Speichern und aktivieren"));
            c.AddFieldToggle(WindowText("Enable line protection","Streckensicherung aktivieren"),()=>draft.Enabled,v=>draft.Enabled=v,true);
            if(draft.Mode!=LineTrackMode.Directional) {
                c.AddField(WindowText("Normal permission after startup clear proving","Regelerlaubnis nach Freiprüfung beim Start"),c.AddDropdown(new[]{LineStationCode(draft.StationA)+" → "+LineStationCode(draft.StationB),LineStationCode(draft.StationB)+" → "+LineStationCode(draft.StationA)}.ToList(),draft.NormalDirection==1?0:1,i=>{if(current())draft.NormalDirection=i==0?1:-1;}));
                c.AddFieldToggle(WindowText("Require a neighbour request before giving permission","Vor Erlaubnisabgabe Anfrage des Nachbarn verlangen"),()=>draft.RequireRequest,v=>draft.RequireRequest=v,true);
                c.AddFieldToggle(WindowText("Automatically grant explicit permission requests when safe","Ausdrückliche Erlaubnisanfragen bei erfüllten Bedingungen automatisch bestätigen"),()=>draft.Automatic,v=>draft.Automatic=v,true);
            }
            c.AddLabel(WindowText("Permission is retained between trains. Reversal requires the entire line clear, no routes or residual locks, and three separate clear readings. Route requests never take the neighbour's permission. These settings only configure interlocking logic; no world construction is performed.","Die Erlaubnis bleibt zwischen Zügen bestehen. Richtungswechsel erfordern eine vollständig freie Strecke, keine Fahrstraßen oder Restverschlüsse und drei getrennte Freimeldungen. Fahrstraßen nehmen dem Nachbarn keine Erlaubnis weg. Diese Einstellungen betreffen nur Stellwerkslogik; es wird nichts in der Spielwelt gebaut."));
            c.AddLabel(WindowText("Enabling also enables world block checks and assigns departure/entry roles to the end signals. Route discovery is a preview; only Apply saves it. Signal target automation and approach contacts are configured separately.","Aktivieren schaltet auch die Blockprüfung dieser Welt ein und ordnet den Endsignalen Aus-/Einfahrtrollen zu. Die Erkennung ist zunächst eine Vorschau; erst Übernehmen speichert. Signal-Zielautomatik und Anforderungskontakte werden separat eingerichtet."));
            c.AddButton(WindowText("Apply line configuration","Streckenkonfiguration übernehmen"),()=>Act(()=>{
                if(current())ApplyLineDraft(draft);
            }));
            }
        }
        void BuildLineConnection(UIPanelBuilder c,LineTrackDefinition draft,Func<bool> current) {
            c.AddSection(WindowText("1 · Connection","1 · Verbindung"));
            c.AddField(WindowText("Code","Kennung"),c.AddInputField(draft.Code,v=>draft.Code=v,"STR-001",24));
            c.AddField(WindowText("Name","Bezeichnung"),c.AddInputField(draft.Name,v=>draft.Name=v,"Whittier – Ela",64));
            c.AddField(WindowText("Track label","Gleisbezeichnung"),c.AddInputField(draft.Track,v=>draft.Track=v,"1",24));
            var stations=desks.Desks.Where(d=>!d.IsMaster).OrderBy(d=>d.Code).ToArray();
            void Station(string label,string value,Action<string> set) {c.AddSearchChoice(label,stations.Select(s=>s.Code+" · "+s.Name).ToList(),Math.Max(0,Array.FindIndex(stations,s=>s.Id==value)),i=>{if(!current())return;set(stations[i].Id);ResetLineDetection(draft);RefreshWorkspaceFields();});}
            Station(WindowText("First neighbouring interlocking","Erstes Nachbarstellwerk"),draft.StationA,id=>{draft.StationA=id;LineSignalGroups.Put(draft,LineSignalGroup.ADepartures,Array.Empty<string>());LineSignalGroups.Put(draft,LineSignalGroup.AArrivals,Array.Empty<string>());});
            Station(WindowText("Second neighbouring interlocking","Zweites Nachbarstellwerk"),draft.StationB,id=>{draft.StationB=id;LineSignalGroups.Put(draft,LineSignalGroup.BArrivals,Array.Empty<string>());LineSignalGroups.Put(draft,LineSignalGroup.BDepartures,Array.Empty<string>());});
            if(guidedLineSetup) {
                c.AddField(WindowText("Operating mode","Betriebsart"),c.AddDropdown(new[]{WindowText("Single-track line","Eingleisige Strecke"),WindowText("Bidirectional working (GWB)","Gleiswechselbetrieb (GWB)")}.ToList(),draft.Mode==LineTrackMode.SingleTrack?0:1,i=>{if(!current())return;draft.Mode=i==0?LineTrackMode.SingleTrack:LineTrackMode.Gleiswechselbetrieb;ResetLineDetection(draft);RefreshWorkspaceFields();}));
                if(draft.Mode==LineTrackMode.Directional)c.AddLabel(WindowText("This saved directional track can be edited in manual settings. Choose a two-ended operating mode explicitly to use this tutorial.","Dieses gespeicherte Richtungsgleis kann in den manuellen Einstellungen bearbeitet werden. Für dieses Tutorial ausdrücklich eine Betriebsart für beide Enden wählen."));
            } else c.AddField(WindowText("Operating mode","Betriebsart"),c.AddDropdown(new List<string>{WindowText("Single-track line","Eingleisige Strecke"),WindowText("Directional track","Richtungsgleis")+" · "+LineStationCode(draft.StationA)+" → "+LineStationCode(draft.StationB),WindowText("Bidirectional working (GWB)","Gleiswechselbetrieb (GWB)")},(int)draft.Mode,i=>{if(!current())return;draft.Mode=(LineTrackMode)i;ResetLineDetection(draft);RefreshWorkspaceFields();}));
        }
        void ApplyLineDraft(LineTrackDefinition draft) {
            RequirePanelEdit();CheckLineConfigurationChange(new[]{draft.Id},new[]{draft});draft.Validate();
                if(draft.Enabled){string error=LineConfigurationError(draft);if(error!=null)throw new InvalidOperationException(error);}
                var indicator=lineIndicatorTarget;
                SaveAutomation(d=>{var value=draft.Clone();LineSetupWorkflow.Put(d,value,indicator);ApplyGuidedPermissions(d,value);if(guidedLineSetup)LineControlSetup.Apply(d,value,SignalObjects);
                    if(value.Enabled){d.Automation.BlockEnabled=true;foreach(var group in LineSignalGroups.Used(value))foreach(string id in LineSignalGroups.Get(value,group))if(Object(id)?.Kind==MarkerRecord.Main)SetLineRole(d,id,LineSignalGroups.Departure(group)?SignalDuty.Exit:SignalDuty.Entry);}
                });if(indicator!=null)indicator.Reference=draft.Id;RefreshLineDirections();SelectLine(draft.Id);status=draft.Enabled?WindowText("Line saved. Permission starts without authority; wait for complete clear checks.","Strecke gespeichert. Erlaubnis startet ohne Freigabe; vollständige Freiprüfung abwarten."):WindowText("Connection saved as an inactive draft. Complete signals and detection before enabling it.","Verbindung als inaktiver Entwurf gespeichert. Vor Aktivierung Signale und GFM vervollständigen.");
        }
        static void SetLineRole(DeskLibrary library,string id,SignalDuty duty){if(!library.Automation.Signals.TryGetValue(id,out var signal))library.Automation.Signals[id]=signal=new AutomaticSignal();signal.Duty=duty;}
        void LineSignalPicker(UIPanelBuilder c,LineTrackDefinition draft,string caption,string station,string value,Action<string> set,Func<bool> current) {
            var owned=desks.Desks.FirstOrDefault(d=>d.Id==station)?.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.Signal).SelectMany(t=>new[]{t.Reference,t.AlternateSignal}).Where(id=>!string.IsNullOrEmpty(id)).ToHashSet()??new HashSet<string>();
            var signals=SignalObjects.Where(s=>s.Kind==MarkerRecord.Main&&!s.Planned&&owned.Contains(s.Id)).OrderBy(s=>s.Name).ToArray();
            if(signals.Length==0)c.AddLabel(caption+" · "+WindowText("No assigned main signals. Link the station's signal fields to world signals first.","Keine zugeordneten Hauptsignale. Zuerst die Signalfelder des Ortsstellwerks mit den Weltsignalen verknüpfen."));
            c.AddSearchChoice(caption,new[]{WindowText("Choose signal","Signal wählen")}.Concat(signals.Select(s=>s.Name)).ToList(),Array.FindIndex(signals,s=>s.Id==value)+1,i=>{if(!current())return;set(i==0?"":signals[i-1].Id);ResetLineDetection(draft);RefreshWorkspaceFields();});
        }
        List<RouteChainPlan> FindLinePlans(string start,string end)=>LineSetupSearch.Find(CaptureNetwork(),SignalObjects,Object(start),RouteEndpoint(end));
        string LinePlanCaption(RouteChainPlan plan)=>string.Join(" → ",plan.Routes.Select(r=>r.Code))+" · "+string.Join(", ",plan.Routes.SelectMany(r=>r.Switches).Select(s=>PointAssignments.Caption(desks,s.Node)+(s.Thrown?" −":" +")).Distinct());
        void DetectLine(LineTrackDefinition draft) {
            if(draft.StationA==draft.StationB)throw new InvalidOperationException(WindowText("Choose different neighbours.","Verschiedene Nachbarbetriebsstellen wählen."));
            lineForwardPlans.Clear();lineReversePlans.Clear();lineForwardChoice=lineReverseChoice=0;draft.Topology="";lineGeometryShape=LineSetupSearch.NetworkShape(CaptureNetwork());
            lineForwardPlans.AddRange(FindLinePlans(draft.ForwardStart,draft.ForwardEnd));
            if(draft.Mode!=LineTrackMode.Directional)lineReversePlans.AddRange(FindLinePlans(draft.ReverseStart,draft.ReverseEnd));
            if(draft.Mode!=LineTrackMode.Directional) {
                var pair=LineSetupWorkflow.MatchDirections(lineForwardPlans.Select(p=>LineTrackGeometry.Normalize(p.Routes.SelectMany(r=>r.Path))).ToArray(),lineReversePlans.Select(p=>LineTrackGeometry.Normalize(p.Routes.SelectMany(r=>r.Path))).ToArray());
                lineForwardChoice=pair.Forward;lineReverseChoice=pair.Reverse;
            }
            CaptureLineDetection(draft);
        }
        void CaptureLineDetection(LineTrackDefinition draft) {
            draft.Topology="";draft.Anchors.Clear();
            if(lineForwardPlans.Count==0||draft.Mode!=LineTrackMode.Directional&&lineReversePlans.Count==0)throw new InvalidOperationException(WindowText("Detect both line directions first.","Zuerst beide Streckenrichtungen erkennen."));
            var selected=lineForwardPlans[lineForwardChoice].Routes.Concat(draft.Mode==LineTrackMode.Directional?Enumerable.Empty<RouteDefinition>():lineReversePlans[lineReverseChoice].Routes);
            if(lineGeometryShape!=LineSetupSearch.NetworkShape(CaptureNetwork()))throw new InvalidOperationException(WindowText("Track path changed since detection. Detect again.","Fahrweg seit der Erkennung geändert. Erneut erkennen."));
            draft.Forward=LineTrackGeometry.Normalize(lineForwardPlans[lineForwardChoice].Routes.SelectMany(r=>r.Path));
            draft.Reverse=draft.Mode==LineTrackMode.Directional?new List<DirectedRouteSection>():LineTrackGeometry.Normalize(lineReversePlans[lineReverseChoice].Routes.SelectMany(r=>r.Path));
            _ = draft.Axis;
            foreach(var group in LineSignalGroups.Used(draft))foreach(string id in LineSignalGroups.Get(draft,group)) {
                bool forward=group==LineSignalGroup.ADepartures||group==LineSignalGroup.BArrivals;
                string start=LineSignalGroups.Departure(group)?id:forward?draft.ForwardStart:draft.ReverseStart;
                string end=LineSignalGroups.Departure(group)?forward?draft.ForwardEnd:draft.ReverseEnd:id;
                if(start!=(forward?draft.ForwardStart:draft.ReverseStart)||end!=(forward?draft.ForwardEnd:draft.ReverseEnd)) {
                    bool matches=FindLinePlans(start,end).Any(p=>DirectionBlockBuilder.Covers(draft.Axis,new[]{new DirectionBlock{Axis=p.Routes.SelectMany(r=>r.Path).ToList()}})&&DirectionBlockBuilder.Direction(p.Routes.SelectMany(r=>r.Path),draft.Axis)==(forward?1:-1));
                    if(!matches)throw new InvalidOperationException(WindowText("Configured signal does not use the same line: ","Eingerichtetes Signal benutzt nicht dieselbe Strecke: ")+Code(id));
                }
                draft.Anchors[id]=SignalWorldBridge.AnchorKey(RouteEndpoint(id));
            }
            draft.Topology=SignalWorldBridge.TrackFingerprint(draft.Forward.Concat(draft.Reverse).Select(p=>p.Section));
            status=WindowText("Line path detected. Review before applying.","Streckenverlauf erkannt. Vor dem Übernehmen prüfen.");
        }
        void BuildLineOperation(UIPanelBuilder c,LineTrackDefinition line) {
            c.AddSection(line.Code+" · "+line.Name+" · "+line.Track);
            c.AddField(WindowText("Permission","Erlaubnis"),()=>LineDirectionCaption(line,LineLive(line.Id)),UIPanelBuilder.Frequency.Periodic);
            c.AddField(WindowText("Details","Details"),()=>LineLive(line.Id)?.Reason??"?",UIPanelBuilder.Frequency.Periodic);
            if(StateManager.IsHost)c.AddButton(WindowText("Review all GFM setup faults in Editor","Alle GFM-Einrichtungsfehler im Editor prüfen"),()=>Act(OpenGfmFaultsFromDispatcher));
            var field=panel?.At(selectedX,selectedY);
            var assembly=CurrentDesk==null?null:BlockDiagram.Area(CurrentDesk.Panel,line.Id);
            if(StateManager.IsHost&&assembly!=null) {
                string targetDesk=deskId;
                c.AddButton(WindowText("Move this assembly in the editor","Diesen Verbund im Editor verschieben"),()=>Act(()=>{
                    var selected=BlockDiagram.Area(desks.Desks.Single(d=>d.Id==targetDesk).Panel,line.Id)??throw new InvalidOperationException(WindowText("The block assembly is missing.","Der Blockverbund fehlt."));
                    ShowWorkspace(true,4,false);EnterEditorDesk(targetDesk);region=new PanelRegion{X=selected.X,Y=selected.Y,Width=selected.Width,Height=selected.Height};regionAction=0;editorMode=1;tool=-1;tileDraft=null;
                    status=WindowText("Drag any field of the selected assembly. Green fits; red is blocked.","Ein Feld des markierten Verbunds ziehen. Grün passt, Rot ist gesperrt.");panelView?.Refresh();
                }));
            }
            if(field?.Kind==PanelTileKind.LinePermission&&field.Reference==line.Id&&field.ReadOnlySignal)return;
            if(field?.Kind==PanelTileKind.LinePermission&&field.Reference==line.Id&&field.PermissionAuxiliary!=null) {
                var target=AuxiliaryTarget.Index(desks.Desks.SelectMany(d=>d.Panel.Tiles)).Values.FirstOrDefault(t=>t.Id==field.PermissionAuxiliary);
                if(target!=null){c.AddField(WindowText("Linked auxiliary button","Verknüpfte Hilfstaste"),target.Caption+" · "+(target.Function==AuxiliaryFunction.LinePermission?WindowText("Permission controls","Erlaubnisbedienung"):WindowText("Route destination","Fahrstraßenziel")));if(target.Function==AuxiliaryFunction.RouteDestination&&panelButtons.Start!=null)c.AddButton(WindowText("Set to linked destination","Zum verknüpften Ziel stellen"),()=>Act(()=>ChooseAuxiliary(target,shunting,false)));}
            }
            if(!StateManager.IsHost)return;
            string id=line.Id,selectedScope=scope,selectedDesk=deskId;
            if(CurrentDesk?.IsMaster==true) {
                if(lineOperatingStation!=line.StationA&&lineOperatingStation!=line.StationB)lineOperatingStation=line.StationA;
                c.AddField(WindowText("Operate on behalf of","Bedienen für"),c.AddDropdown(new[]{LineStationCode(line.StationA),LineStationCode(line.StationB)}.ToList(),lineOperatingStation==line.StationA?0:1,i=>{lineOperatingStation=i==0?line.StationA:line.StationB;RefreshWorkspaceFields();}));
            }
            string actor=CurrentDesk?.IsMaster==true?lineOperatingStation:deskId;
            if(actor!=line.StationA&&actor!=line.StationB)return;
            var snapshot=LineLive(id);if(snapshot==null)return;long revision=snapshot.Revision;int end=actor==line.StationA?1:-1;
            void Change(string operation){if(scope!=selectedScope||deskId!=selectedDesk||CurrentDesk?.IsMaster==true&&lineOperatingStation!=actor||!desks.FreeLines.Any(l=>l.Id==id))throw new InvalidOperationException(LocalText.Select("Line changed.","Strecke geändert."));OperateLinePermission(id,actor,revision,operation);}
            if(line.Mode!=LineTrackMode.Directional) {
                if(snapshot.Direction==end)c.AddButton(WindowText("Give permission to neighbour","Erlaubnis an Nachbarn abgeben"),()=>Act(()=>Change("give")));
                else if(snapshot.RequestedDirection==end)c.AddButton(WindowText("Cancel own permission request","Eigene Erlaubnisanfrage zurücknehmen"),()=>Act(()=>Change("cancel")));
                else c.AddButton(WindowText("Request permission from neighbour","Erlaubnis beim Nachbarn anfragen"),()=>Act(()=>Change("request")));
                if(line.Automatic)c.AddButton(snapshot.Balance==1?WindowText("Pause automatic granting","Automatische Bestätigung pausieren"):WindowText("Resume automatic granting","Automatische Bestätigung übernehmen"),()=>Act(()=>Change(snapshot.Balance==1?"pause":"resume")));
            }
            c.AddButton(WindowText("Refresh permission controls","Erlaubnisbedienung aktualisieren"),RefreshWorkspaceFields);
            c.AddLabel(WindowText("Only the current owner can give permission. Requests may wait while the line is occupied. A transfer requires the entire line clear and all route and residual locks released.","Nur der aktuelle Inhaber darf die Erlaubnis abgeben. Anfragen dürfen bei belegter Strecke warten. Die Abgabe erfordert die vollständig freie Strecke und aufgelöste Fahrstraßen- und Restverschlüsse."));
        }
    }
}
