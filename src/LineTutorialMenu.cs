using System;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string guidedLineHelp;
        LineControlPlan guidedControlPreview;
        void TutorialTry(Action action) {
            try{guidedLineHelp=null;workspaceActionError=null;action();}
            catch(Exception error){guidedLineHelp=error.Message;workspaceActionError=null;status=WindowText("This step needs attention. Follow the instruction below.","Dieser Schritt benötigt noch eine Anpassung. Den Hinweis unten befolgen.");}
            RefreshWorkspaceFields();panelView?.Refresh();
        }
        bool TutorialSignalGroup(LineTrackDefinition draft,out LineSignalGroup group) {
            var steps=LineTutorial.SignalSteps(draft.Mode);int index=guidedLineStep-1;group=index>=0&&index<steps.Length?steps[index]:LineSignalGroup.AArrivals;return index>=0&&index<steps.Length;
        }
        string TutorialSignalTitle(LineTrackDefinition draft,LineSignalGroup group) {
            string station=SetupStationName(LineSignalGroups.Station(draft,group));bool departure=LineSignalGroups.Departure(group);
            string neighbour=SetupStationName(LineSignalGroups.Station(draft,group)==draft.StationA?draft.StationB:draft.StationA);
            return station+(departure?WindowText(" · departure toward "," · Ausfahrt Richtung ")+neighbour:WindowText(" · entry from "," · Einfahrt aus ")+neighbour);
        }
        void SetTutorialSignal(LineTrackDefinition draft,LineSignalGroup group,string id) {
            RequirePanelEdit();if(!guidedLineSetup||panelPage!=9||!ReferenceEquals(draft,lineDraft)||!TutorialSignalGroup(draft,out var active)||active!=group)return;
            bool added=LineTutorial.ToggleSelection(desks,draft,group,id,Object);
            ResetLineDetection(draft);guidedControlPreview=null;guidedLineHelp=null;status=added?WindowText("Signal added. Select more signals or check and continue.","Signal hinzugefügt. Weitere Signale auswählen oder prüfen und weitergehen."):WindowText("Signal deselected. The other selected signals are retained.","Signal abgewählt. Die übrigen ausgewählten Signale bleiben erhalten.");RefreshWorkspaceFields();panelView?.Refresh();
        }
        void ResumeLineTutorial(LineTrackDefinition draft,int step,string station,string worldScope) {
            if(scope!=worldScope||!ReferenceEquals(draft,lineDraft)||!guidedLineSetup||guidedLineStep!=step)return;
            ShowWorkspace(true,4,false);EnterEditorDesk(station);WorkspacePage(9);
        }
        bool PressLineTutorialSignal(int x,int y,bool half) {
            if(!editingWorkspace||panelPage!=9||!guidedLineSetup||lineDraft==null||!TutorialSignalGroup(lineDraft,out var group))return false;
            var tile=panel?.At(x,y);string id=tile?.Kind==PanelTileKind.Signal?SignalCellLayout.SignalAt(tile,half):null;
            TutorialTry(()=>SetTutorialSignal(lineDraft,group,id));return true;
        }
        bool TutorialSelectedSignal(PanelTile tile) {
            if(!editingWorkspace||panelPage!=9||!guidedLineSetup||lineDraft==null||!TutorialSignalGroup(lineDraft,out var group)||tile?.Kind!=PanelTileKind.Signal)return false;
            return LineTutorial.Selected(lineDraft,group,tile);
        }
        void BuildTutorialSignal(UIPanelBuilder c,LineTrackDefinition draft,LineSignalGroup group,Func<bool> current) {
            bool departure=LineSignalGroups.Departure(group);string station=LineSignalGroups.Station(draft,group);int step=guidedLineStep;string worldScope=scope;
            c.AddLabel(departure?WindowText("Select all main signals that release trains from this station onto this same line track toward the neighbour. Each must face outward.","Alle Hauptsignale auswählen, die Züge aus diesem Bahnhof auf dasselbe Streckengleis zum Nachbarn ausfahren lassen. Jedes muss nach außen gerichtet sein."):WindowText("Select the main signals protecting entry into this station from the neighbour on this line track. The first selected signal supplies the reference for the later destination button.","Die Hauptsignale auswählen, die die Einfahrt vom Nachbarn in diesen Bahnhof an diesem Streckengleis schützen. Das zuerst gewählte Signal liefert später den Bezug für die Zieltaste."));
            var selected=LineSignalGroups.Get(draft,group);c.AddLabel(WindowText("Selected signals: ","Ausgewählte Signale: ")+selected.Count+(selected.Count>0?" · "+string.Join(", ",selected.Select(Code)):""));
            c.AddLabel(WindowText("Click signals to add them; click again to deselect. All selected fields stay green. Check and continue only when this role is complete. Previous/next retains your selections.","Signale zum Hinzufügen anklicken; ein erneuter Klick wählt sie ab. Alle ausgewählten Felder bleiben grün. Erst wenn diese Rolle vollständig ist, prüfen und weitergehen. Zurück/Weiter erhält die Auswahl."));
            bool Allowed(string id)=>Object(id)?.Kind==MarkerRecord.Main&&Object(id)?.Planned==false&&StationAutomationPolicy.Owner(desks,id)?.Id==station;
            c.AddButton(WindowText("Choose in this diagram…","In diesem Gleisbild wählen …"),()=>{if(current())PickSignalInDiagram(station,TutorialSignalTitle(draft,group),Allowed,id=>{if(current())TutorialTry(()=>SetTutorialSignal(draft,group,id));},id=>current()&&LineSignalGroups.Get(draft,group).Contains(id));});
            c.AddButton(WindowText("Choose signal in the world…","Signal in der Welt wählen …"),()=>{if(current())BeginWorldSignalPick(id=>{if(current())TutorialTry(()=>SetTutorialSignal(draft,group,id));},()=>ResumeLineTutorial(draft,step,station,worldScope));});
            foreach(string id in selected) {
                c.AddButton(WindowText("Inspect: ","Prüfen: ")+Code(id),()=>{if(current())Entry.OpenMarker(id);});
                c.AddButton(WindowText("Deselect: ","Abwählen: ")+Code(id),()=>{if(current())TutorialTry(()=>SetTutorialSignal(draft,group,id));});
            }
        }
        void ShowTutorialStation(LineTrackDefinition draft) {
            if(TutorialSignalGroup(draft,out var group)){string station=LineSignalGroups.Station(draft,group);if(deskId!=station){SelectDesk(station,false);WorkspacePage(9);}}
        }
        LineControlPlan PrepareTutorialControls(LineTrackDefinition draft) {
            var library=desks.Clone();LineSetupWorkflow.Put(library,draft,lineIndicatorTarget);ApplyGuidedPermissions(library,draft);return LineControlSetup.Preview(library,draft,SignalObjects);
        }
        void TutorialAdvance(LineTrackDefinition draft) {
            RequirePanelEdit();int count=LineTutorial.SignalSteps(draft.Mode).Length;
            if(guidedLineStep==0) {
                if(draft.Mode==LineTrackMode.Directional)throw new InvalidOperationException(WindowText("Use manual settings for this saved directional track, or explicitly choose single-track/GWB operation.","Für dieses gespeicherte Richtungsgleis die manuellen Einstellungen nutzen oder ausdrücklich eingleisigen Betrieb/GWB wählen."));
                if(draft.StationA==draft.StationB||string.IsNullOrWhiteSpace(draft.Track))throw new InvalidOperationException(WindowText("Choose two different neighbours and a track label.","Zwei verschiedene Nachbarn und eine Gleisbezeichnung wählen."));
                var matches=LineSetupWorkflow.Connections(desks,draft.StationA,draft.StationB,draft.Track);
                if(matches.Length>1)throw new InvalidOperationException(WindowText("Several older connections describe this track. Resolve these duplicates first: ","Mehrere ältere Verbindungen beschreiben dieses Gleis. Diese Duplikate zuerst klären: ")+string.Join(", ",matches.Select(l=>l.Code)));
                if(matches.Length==1&&matches[0].Id!=draft.Id){lineDraft=draft=matches[0].Clone();selectedLine=draft.Id;guidedPermissions.Clear();ResetLineDetection(draft);}
            } else if(TutorialSignalGroup(draft,out var group)) {
                string issue=LineTutorial.SelectionIssue(desks,draft,group,Object);if(issue!=null)throw new InvalidOperationException(issue);
                if(group==LineSignalGroup.BArrivals&&draft.ForwardStart!="")_ = FindLinePlans(draft.ForwardStart,draft.ForwardEnd);
                if(guidedLineStep==count)DetectLine(draft);
            } else if(guidedLineStep==count+1) {
                if(draft.Forward.Count==0||draft.Mode!=LineTrackMode.Directional&&draft.Reverse.Count==0)DetectLine(draft);
                _ = draft.Axis;
            } else if(guidedLineStep==count+2)guidedControlPreview=PrepareTutorialControls(draft);
            guidedLineStep++;guidedLineHelp=null;
            ShowTutorialStation(draft);
            status=WindowText("Step checked. Continue with the instruction shown here.","Schritt geprüft. Mit der hier angezeigten Anleitung fortfahren.");
        }
        void ChooseTutorialPath(LineTrackDefinition draft,bool forward,int selected) {
            if(draft.Mode==LineTrackMode.Directional){lineForwardChoice=selected;CaptureLineDetection(draft);return;}
            var a=forward?new[]{lineForwardPlans[selected]}:lineForwardPlans.ToArray();var b=forward?lineReversePlans.ToArray():new[]{lineReversePlans[selected]};
            var pair=LineSetupWorkflow.MatchDirections(a.Select(p=>LineTrackGeometry.Normalize(p.Routes.SelectMany(r=>r.Path))).ToArray(),b.Select(p=>LineTrackGeometry.Normalize(p.Routes.SelectMany(r=>r.Path))).ToArray());
            lineForwardChoice=forward?selected:pair.Forward;lineReverseChoice=forward?pair.Reverse:selected;CaptureLineDetection(draft);
        }
        void BuildGuidedLineEditor(UIPanelBuilder c,LineTrackDefinition draft,Func<bool> current) {
            int count=LineTutorial.SignalSteps(draft.Mode).Length,path=count+1,controls=count+2,blocks=count+3,finish=count+4;
            c.AddSection(WindowText("GWB tutorial · step ","GWB-Tutorial · Schritt ")+(guidedLineStep+1)+" / "+(finish+1));
            c.AddLabel(SetupStationName(draft.StationA)+" ↔ "+SetupStationName(draft.StationB));
            if(guidedLineStep==0){c.AddLabel(WindowText("First choose the two neighbouring interlockings. One connection protects one physical line track. Signals, detection and controls follow in separate steps.","Zuerst die beiden Nachbarstellwerke wählen. Eine Verbindung sichert ein physisches Streckengleis. Signale, Freimeldung und Bedienfelder folgen in getrennten Schritten."));BuildLineConnection(c,draft,current);}
            else if(TutorialSignalGroup(draft,out var group)){c.AddSection(TutorialSignalTitle(draft,group));BuildTutorialSignal(c,draft,group,current);}
            else if(guidedLineStep==path) {
                c.AddSection(WindowText("Check the track and prepare detection","Gleisweg prüfen und Freimeldung vorbereiten"));
                c.AddLabel(WindowText("This searches track geometry without requiring an operational route, target button or complete GFM first. It does not clear a signal. Review the proposed path; missing detection is configured next and must be complete before activation.","Hier wird der reale Gleisweg gesucht, ohne schon eine Betriebsfahrstraße, Zieltaste oder vollständige GFM vorauszusetzen. Kein Signal wird auf Fahrt gestellt. Den Vorschlag prüfen; fehlende Freimeldung anschließend einrichten. Vor Aktivierung muss sie vollständig sein."));
                c.AddButton(WindowText("Recheck physical line path","Physischen Streckenweg neu prüfen"),()=>TutorialTry(()=>{if(current())DetectLine(draft);}));
                if(lineForwardPlans.Count>1)c.AddSearchChoice(SetupStationName(draft.StationA)+" → "+SetupStationName(draft.StationB),lineForwardPlans.Select(LinePlanCaption).ToList(),lineForwardChoice,i=>TutorialTry(()=>{if(current())ChooseTutorialPath(draft,true,i);}));
                if(lineReversePlans.Count>1)c.AddSearchChoice(SetupStationName(draft.StationB)+" → "+SetupStationName(draft.StationA),lineReversePlans.Select(LinePlanCaption).ToList(),lineReverseChoice,i=>TutorialTry(()=>{if(current())ChooseTutorialPath(draft,false,i);}));
                if(draft.Forward.Count>0) {
                    c.AddLabel(WindowText("Shared track length (m): ","Gemeinsame Gleislänge (m): ")+Math.Round(draft.Axis.Sum(p=>p.Section.To-p.Section.From)));
                    var chosenForward=lineForwardPlans.Skip(lineForwardChoice).Take(1);var chosenReverse=draft.Mode==LineTrackMode.Directional?Enumerable.Empty<RouteChainPlan>():lineReversePlans.Skip(lineReverseChoice).Take(1);
                    var points=chosenForward.Concat(chosenReverse).SelectMany(p=>p.Routes).SelectMany(r=>r.Switches).Select(s=>s.Node).Distinct().Where(id=>!PointCircuitVisibility.Enabled(desks,id)).ToArray();
                    foreach(string point in points){string id=point;c.AddButton(WindowText("Set up detection for point ","Freimeldung für Weiche einrichten: ")+PointAssignments.Caption(desks,id),()=>TutorialTry(()=>{
                        if(!current())return;RequirePanelEdit();if(!desks.Points.ContainsKey(id)){var next=desks.Clone();PointAssignments.NameDiscovered(next,new[]{id});deskStore.Save(next);desks=next;panel=DeskPanel(CurrentDesk);}ConfigurePointCircuit(id,true,true);
                    }));}
                    BuildLineDetectionProposal(c,draft,current);
                    c.AddLabel(LineConfigurationError(draft)??WindowText("Current detection coverage verified.","Aktuelle Freimeldeabdeckung geprüft."));
                }
            } else if(guidedLineStep==controls) {
                c.AddSection(WindowText("Create missing destination and permission controls","Fehlende Ziel- und Erlaubnisfelder ergänzen"));
                c.AddLabel(WindowText("A destination button sends a train toward the neighbour. The permission control authorizes that direction. The assistant links both and derives the destination from the local entry signal. Existing suitable controls are reused. Proposed fields are saved only on completion or an explicitly confirmed block build.","Die Zieltaste schickt einen Zug zum Nachbarn. Die Erlaubnisbedienung gibt diese Richtung frei. Der Assistent verknüpft beide und leitet das Ziel vom örtlichen Einfahrsignal ab. Passende vorhandene Felder werden wiederverwendet. Vorschläge werden erst beim Abschluss oder ausdrücklich bestätigten Blockbau gespeichert."));
                c.AddButton(WindowText("Preview the missing controls","Fehlende Bedienfelder als Vorschau vorbereiten"),()=>TutorialTry(()=>{if(current())guidedControlPreview=PrepareTutorialControls(draft);}));
                BuildGuidedPermissionPositions(c,draft);
                if(guidedControlPreview==null)try{guidedControlPreview=PrepareTutorialControls(draft);}catch(Exception error){guidedLineHelp=error.Message;}
                if(guidedControlPreview!=null)foreach(var position in guidedControlPreview.Positions)c.AddLabel(SetupStationName(position.Desk)+" · "+WindowText("destination ","Ziel ")+EditorCanvasGeometry.ColumnName(position.TargetX)+(position.TargetY+1)+" · "+WindowText("permission ","Erlaubnis ")+EditorCanvasGeometry.ColumnName(position.PermissionX)+(position.PermissionY+1));
            } else if(guidedLineStep==blocks) {
                c.AddSection(WindowText("Optional intermediate blocks","Optionale Zwischenblöcke"));
                c.AddLabel(WindowText("Intermediate blocks let following trains enter separate clear sections. Skip this construction step if you already configured the detection you need. The line is activated only in the final step.","Zwischenblöcke lassen folgende Züge in getrennte freie Abschnitte einfahren. Diesen Bauschritt überspringen, wenn die benötigte Freimeldung schon eingerichtet ist. Die Strecke wird erst im letzten Schritt aktiviert."));
                if(draft.Forward.Count>0)BuildBlockLayout(c,draft,current);
            } else {
                c.AddSection(WindowText("Final check and activation","Abschlussprüfung und Aktivierung"));
                c.AddLabel(WindowText("Check the path, complete GFM and the linked controls together. Any remaining issue stays here with its instruction; the assistant does not activate an incomplete line.","Gleisweg, vollständige GFM und verknüpfte Bedienfelder gemeinsam prüfen. Ein offener Punkt bleibt hier mit seinem Hinweis sichtbar; eine unvollständige Strecke wird nicht aktiviert."));
                string issue=LineConfigurationError(draft);c.AddLabel(issue??WindowText("Path and detection checks passed.","Gleisweg- und Freimeldeprüfung bestanden."));
                if(issue!=null)c.AddButton(WindowText("Return to detection step","Zum Freimeldeschritt zurück"),()=>{guidedLineStep=path;guidedLineHelp=null;RefreshWorkspaceFields();});
                c.AddButton(WindowText("Complete setup and activate line protection","Einrichtung abschließen und Streckensicherung aktivieren"),()=>TutorialTry(()=>{
                    if(!current())return;bool previous=draft.Enabled;try{draft.Enabled=true;ApplyLineDraft(draft);}catch{draft.Enabled=previous;throw;}
                }));
                c.AddButton(WindowText("Return to track diagram","Zurück zum Gleisbild"),()=>{panelPage=1;ResetEditor();CloseWorkspaceContext();Rebuild(false);});
            }
            if(guidedLineHelp!=null)c.AddLabel(WindowText("Next action: ","Nächste Handlung: ")+guidedLineHelp);
            if(guidedLineStep>0)c.AddButton(WindowText("Previous step","Vorheriger Schritt"),()=>{if(current()){guidedLineStep--;guidedLineHelp=null;guidedControlPreview=null;ShowTutorialStation(draft);RefreshWorkspaceFields();}});
            if(guidedLineStep<finish) {
                string selectionIssue=TutorialSignalGroup(draft,out var pendingGroup)?LineTutorial.SelectionIssue(desks,draft,pendingGroup,Object):null;
                if(selectionIssue==null)c.AddButton(WindowText("Check and continue","Prüfen und weiter"),()=>TutorialTry(()=>{if(current())TutorialAdvance(draft);}));
                else c.AddLabel(selectionIssue);
            }
            c.AddButton(WindowText("Open manual settings","Manuelle Einstellungen öffnen"),()=>{guidedLineSetup=false;guidedLineHelp=null;RefreshWorkspaceFields();});
        }
    }
}
