using System;
using System.Linq;
using System.Collections.Generic;
using Game.State;
using UI.Builder;
using UI.ContextMenu;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        int editorMode,drawX=-1,drawY=-1;
        float panelZoom=1;
        PanelDocument clipboard;
        bool pastePending;
        string pendingSignalId,pendingSignalWorld;
        bool HasPendingSignal=>pendingSignalWorld==scope&&Object(pendingSignalId)?.IsSignal==true;

        internal void AssignPanelSignal(string id) {
            RequireEdit();
            if(!WorkspaceEditable||Object(id)?.IsSignal!=true)throw new InvalidOperationException("Signal oder Stellwerk noch nicht bereit.");
            pendingSignalId=id;
            pendingSignalWorld=scope;
            assigningPoint=false;
            ShowWorkspace(true,4,true);
            status="Signal "+Code(id)+" vorgemerkt. Stellwerk und Signalfeld wählen.";
        }

        void BuildPendingSignal(UIPanelBuilder c,PanelDraftBinding binding) {
            if(!HasPendingSignal)return;
            string id=pendingSignalId;
            c.AddButton("Vorgemerktes Signal "+Code(id)+" übernehmen",()=>binding.Do(()=>Act(()=>{
                RequirePanelEdit();
                if(!HasPendingSignal||pendingSignalId!=id)return;
                string previous=binding.Draft.Reference;
                binding.Draft.Reference=id;
                var signal=Object(id);
                if(signal?.Kind!=MarkerRecord.Main)binding.Draft.AlternateSignal=null;
                else if(signal.AllowSh1&&(binding.Draft.AlternateSignal==null||binding.Draft.AlternateSignal==previous))
                    binding.Draft.AlternateSignal=id;
                status="Signal vorgemerkt. Feld und Zuordnung speichern.";
                panelView?.Refresh();
            })));
        }
        PanelDeletion pendingDelete;
        string deletionDesk,transferTarget;
        readonly UIState<bool> advancedEditor=new UIState<bool>(false);
        readonly UIState<bool> alternateSignalEditor=new UIState<bool>(false);
        void ResetEditor(){drawX=drawY=-1;pastePending=false;pendingDelete=null;ClearRouteProposals();ResetCircuitSelection();}
        PanelTile PreviewTile()=>editingWorkspace&&panelPage==1?tileDraft:null;
        PanelRegion SelectedRegion()=>editingWorkspace?region??(selectedX>=0&&selectedY>=0?new PanelRegion{X=selectedX,Y=selectedY}:null):null;
        void EditorMode(int mode) {
            editorMode=mode;tool=-1;regionAction=0;ResetEditor();tileDraft=null;
            status=mode==0?WindowText("Choose the first cell of the track.","Erstes Gleisfeld wählen."):
                mode==1?WindowText("Click a field to view its properties.","Feld anklicken, um seine Eigenschaften zu öffnen."):
                mode==3?WindowText("Choose a field symbol on the right.","Rechts ein Feldsymbol auswählen."):
                WindowText("Select a region to move or copy it.","Bereich zum Verschieben oder Kopieren markieren.");
            OpenWorkspaceContext();
            panelView?.Refresh();
        }
        void ChooseSymbol(PanelTileKind kind) {
            tool=(int)kind;
            workspaceActionError=null;
            status=WindowText("Click grid cells to place the selected symbol.","Ins Raster klicken, um das gewählte Symbol zu setzen.");
            RefreshWorkspaceFields();
        }
        void SelectAll(){EditorMode(2);region=new PanelRegion{X=0,Y=0,Width=panel.Columns,Height=panel.Rows};status="Gesamte Zeichnung markiert. Löschen benötigt eine Bestätigung.";panelView?.Refresh();RefreshWorkspaceFields();}
        bool EditorClick(int x,int y) {
            if(pastePending){var clip=clipboard??throw new InvalidOperationException("Kein Bereich kopiert.");EditPanel(doc=>PanelEditing.Paste(doc,clip,x,y));pastePending=false;region=new PanelRegion{X=x,Y=y,Width=clip.Tiles.Count==0?1:clip.Tiles.Max(t=>t.X)+1,Height=clip.Tiles.Count==0?1:clip.Tiles.Max(t=>t.Y)+1};status="Bereich übernommen. Feste Bindungen erhalten; ursprüngliche Tafel unverändert.";return true;}
            if(editorMode!=0||tool>=0)return false;
            if(drawX<0){drawX=x;drawY=y;tileDraft=null;status="Zeichenstart gesetzt. Nächsten Eck- oder Endpunkt anklicken.";panelView?.Refresh();return true;}
            EditPanel(doc=>PanelEditing.Draw(doc,drawX,drawY,x,y));drawX=x;drawY=y;tileDraft=null;status="Gleiszug gespeichert. Nächsten Punkt anklicken oder Zeichnung beenden. Anschlüsse und Weichenzeichen werden angepasst.";return true;
        }
        void InsertSpace(bool row) {
            RequirePanelEdit();int at=row?selectedY:selectedX;if(at<0)throw new InvalidOperationException("Zuerst ein Feld an der Einfügestelle anklicken oder rechtsklicken.");
            EditPanel(doc=>PanelEditing.Insert(doc,row,at));if(row)selectedY++;else selectedX++;ResetEditor();region=null;tileDraft=null;columnsInput=panel.Columns.ToString();rowsInput=panel.Rows.ToString();status=(row?"Leere Zeile":"Leere Spalte")+" eingefügt. Nachfolgende Felder samt Bindungen verschoben; gezeichnete Lücken bei Bedarf verbinden.";
        }
        void ReadEditorDeleteKey() {
            if(!editingWorkspace||editorSurface==null||panelPage!=1||editorMode!=1||region!=null||pendingDelete!=null||
                Keyboard.current?.deleteKey.wasPressedThisFrame!=true||Mouse.current==null||!PanelVisible)return;
            var events=EventSystem.current;
            if(events==null)return;
            var focus=events.currentSelectedGameObject;
            if(focus!=null&&(focus.GetComponentInParent<TMPro.TMP_InputField>()?.isFocused==true||
                focus.GetComponentInParent<InputField>()?.isFocused==true))return;
            var hits=new List<RaycastResult>();
            events.RaycastAll(new PointerEventData(events){position=Mouse.current.position.ReadValue()},hits);
            if(hits.Count==0||!hits[0].gameObject.transform.IsChildOf(editorSurface.transform))return;
            if(panel?.At(selectedX,selectedY)==null)return;
            int x=selectedX,y=selectedY;
            Act(()=>{
                EditPanel(doc=>doc.Tiles.RemoveAll(t=>t.X==x&&t.Y==y));
                tileDraft=null;
                status="Zeichnungsfeld gelöscht. Mit Rückgängig wiederherstellbar.";
                RefreshWorkspaceFields();
            });
        }
        void AskDelete() {
            RequirePanelEdit();var selection=region??(selectedX>=0?new PanelRegion{X=selectedX,Y=selectedY}:null);if(selection==null)throw new InvalidOperationException("Feld/Bereich auswählen oder Alles markieren.");
            OpenWorkspaceContext();pendingDelete=new PanelDeletion(panel,selection,region==null);deletionDesk=deskId;drawX=drawY=-1;tool=-1;status="Löschung bitte bestätigen. Nur Zeichnungsfelder dieser Tafel sind betroffen.";RefreshWorkspaceFields();
        }
        void BuildDeleteConfirmation(UIPanelBuilder c) {
            var request=pendingDelete;
            c.AddLabel(request.KeepsTrack?"Symbol entfernen und Gleislinie erhalten?":request.Count+" Zeichnungsfelder löschen? Platzierte Spielobjekte und Fahrstraßendefinitionen bleiben erhalten.");
            c.HStack(row=>{row.AddButton(request.KeepsTrack?"Symbol entfernen":"Felder löschen",()=>Act(()=>{if(!ReferenceEquals(request,pendingDelete)||deletionDesk!=deskId)return;EditPanel(doc=>request.Apply(doc,true));pendingDelete=null;tileDraft=null;region=null;status=request.KeepsTrack?"Symbol entfernt. Die Gleislinie bleibt verbunden.":"Bestätigte Zeichnungsfelder gelöscht.";}));row.AddButton("Abbrechen",()=>{if(!ReferenceEquals(request,pendingDelete))return;pendingDelete=null;status="Löschung abgebrochen.";RefreshWorkspaceFields();});});
        }
        void CopyCurrentRegion(){RequirePanelEdit();var selected=region??new PanelRegion{X=0,Y=0,Width=panel.Columns,Height=panel.Rows};clipboard=selected.Extract(panel);if(clipboard.Tiles.Count==0)throw new InvalidOperationException("Bereich enthält keine Felder.");}
        void PasteInto(string target) {
            CopyCurrentRegion();SelectDesk(target,false);panelPage=1;editorMode=2;pastePending=true;status="Zielfeld für die linke obere Ecke anklicken. Bei belegtem Ziel wird nichts überschrieben.";Rebuild();
        }
        void BuildTransfers(UIPanelBuilder c) {
            BuildNamedAreas(c);
            if(clipboard!=null&&clipboard.WorldKey==scope)c.AddButton("Kopierten Bereich hier einfügen …",()=>{ResetEditor();editorMode=2;pastePending=true;status="Freie linke obere Zielecke anklicken.";RefreshWorkspaceFields();});
            if(CurrentDesk?.Global!=true)c.AddButton("Diesen Bereich ins ESTW übernehmen …",()=>Act(()=>PasteInto(desks.Desks.Single(d=>d.Global).Id)));
            var targets=desks.Desks.Where(d=>d.Id!=deskId).ToArray();if(targets.Length>0){if(!targets.Any(d=>d.Id==transferTarget))transferTarget=targets[0].Id;c.AddField("Vorhandenes Zielstellwerk",c.AddDropdown(targets.Select(d=>d.Code+" · "+d.Name).ToList(),Array.FindIndex(targets,d=>d.Id==transferTarget),i=>transferTarget=targets[i].Id));c.AddButton("Bereich ins Zielstellwerk übernehmen …",()=>Act(()=>PasteInto(transferTarget)));}
            c.AddLabel("Übernommen wird eine unabhängige Zeichnung mit denselben Spiel-IDs. Spätere Geometrieänderungen werden nicht automatisch zwischen den Tafeln gespiegelt.");
        }
        void ChangeZoom(float value) {
            value=Mathf.Clamp(value,.025f,2);if(panelView==null){panelZoom=value;return;}var old=panelZoom;var position=panelScroll==null?new Vector2(0,1):panelScroll.normalizedPosition;var viewport=panelScroll?.viewport;float width=viewport==null?0:viewport.rect.width,height=viewport==null?0:viewport.rect.height;
            float cx=(Mathf.Max(0,panel.Columns*PanelGeometry.CellWidth*old-width)*position.x+width/2)/(PanelGeometry.CellWidth*old),cy=(Mathf.Max(0,panel.Rows*PanelGeometry.CellHeight*old-height)*(1-position.y)+height/2)/(PanelGeometry.CellHeight*old);
            panelZoom=value;panelView.SetZoom(value);Canvas.ForceUpdateCanvases();if(panelScroll!=null){float dx=Mathf.Max(0,panel.Columns*PanelGeometry.CellWidth*value-width),dy=Mathf.Max(0,panel.Rows*PanelGeometry.CellHeight*value-height);panelScroll.normalizedPosition=new Vector2(dx==0?0:Mathf.Clamp01((cx*PanelGeometry.CellWidth*value-width/2)/dx),dy==0?1:1-Mathf.Clamp01((cy*PanelGeometry.CellHeight*value-height/2)/dy));}nextPanelRead=0;
        }
        bool dispatcherContextRight;
        void CellContext(int x,int y,bool rightHalf=false) {
            Act(()=>{
                if(editingWorkspace) {
                    RequirePanelEdit();
                    if(pendingDelete!=null||panelPage==1&&editorMode==3)return;
                    panelPage=1;editorMode=1;tool=-1;regionAction=0;
                    ResetEditor();
                    tileDraft=panel.At(x,y)?.Clone();
                    if(tileDraft==null&&HasPendingSignal) {
                        tileDraft=PanelEditing.PreviewField(panel,x,y,PanelTileKind.Signal);
                        tileDraft.Reference=pendingSignalId;
                    }
                    if(tileDraft!=null)LoadTileFields();
                } else {
                    panelPage=0;
                    dispatcherContextRight=rightHalf;
                }
                selectedX=x;selectedY=y;
                OpenWorkspaceContext();panelView?.Refresh();
            });
        }
        void StartFrom(string id,bool yard,bool slow=false){RequireDispatcher();var signal=Object(id);if(signal==null||!SignalCatalogue.Supports(signal,yard,slow))throw new InvalidOperationException("Signal unterstützt diese Fahrart nicht.");panelPage=0;shunting=yard;reduced=slow;panelButtons.Clear();panelButtons.Press(id,ButtonSignal,yard);status="Start "+SignalCaption(id)+" · "+(yard?"Rangierfahrt":slow?"Langsamfahrt":"Zugfahrt")+". Zielsignal anklicken.";CloseWorkspaceContext();panelView?.Refresh();}
        void BuildFieldAssignment(UIPanelBuilder c) {
            var binding=new PanelDraftBinding(tileDraft,()=>tileDraft);var draft=binding.Draft;
            if(draft==null){c.AddLabel("Feld anklicken oder rechtsklicken → Feld zuordnen / bearbeiten. Gezeichnete Gleise sind zunächst nur Darstellung.");return;}
            c.AddSection("Feld "+(draft.X+1)+" / "+(draft.Y+1)+" · "+ToolNames[(int)draft.Kind]);
            c.AddLabel(WindowText("Caption","Beschriftung"));
            c.AddInputField(draft.Label,v=>binding.Do(()=>{draft.Label=v;panelView?.Refresh();}),WindowText("Use signal code if empty","Leer: Signalkennung verwenden"),48);
            c.AddSection(WindowText("Preview","Vorschau"));
            var preview=c.HStack(_=>{});
            AddEditorIcon(preview,WindowText("Rotate 90°","90° drehen"),"ROTATE",()=>binding.Do(()=>{
                draft.Rotation=(draft.Rotation+1)%4;RefreshWorkspaceFields();
            }));
            AddEditorIcon(preview,WindowText("Mirror","Spiegeln"),"MIRROR",()=>binding.Do(()=>{
                draft.Mirror=!draft.Mirror;RefreshWorkspaceFields();
            }));
            if(draft.Kind==PanelTileKind.TrainNumber||draft.Kind==PanelTileKind.Signal) {
                if(draft.Kind==PanelTileKind.TrainNumber) {
                    BuildNumberField(c,binding);
                    BuildCircuitEditor(c,new PanelRegion{X=draft.X,Y=draft.Y});
                }
                else {
                    BuildPendingSignal(c,binding);
                    BuildLinkedAuxiliary(c,binding);
                }
                BuildSignalSearch(c,binding);
                var caption=draft.Kind==PanelTileKind.TrainNumber?
                    WindowText("Opposite direction","Gegenrichtung"):WindowText("Split signal field","Signalfeld teilen");
                c.AddExpandable(caption,alternateSignalEditor,more=>{
                    BuildSignalSearch(more,binding,true);
                    if(draft.Kind==PanelTileKind.Signal)BuildSplitLayout(more,binding);
                },ExpandableStyle.SectionHeader);
            }
            else if(draft.Kind==PanelTileKind.AuxiliaryTarget)BuildAuxiliaryEditor(c,binding);
            else if(draft.Kind==PanelTileKind.Switch){
                c.AddField("Weiche",PointAssignments.Caption(desks,draft.Reference));
                c.AddField("Aktuelle Lage",()=>PointPosition(draft.Reference),UIPanelBuilder.Frequency.Periodic);
                c.AddLabel("Im Spiel nahe der gewünschten Weiche rechtsklicken → Weiche zuordnen. Zielstellwerk wählen, hier das Feld rechtsklicken → Zuordnung übernehmen.");
                if(assigningPoint)c.AddButton("Vorgemerkte Weiche diesem Feld zuordnen",()=>binding.Do(()=>Act(()=>AcceptPoint(draft.X,draft.Y))));
                if(!string.IsNullOrEmpty(switchPicked))c.AddButton("Zuletzt in der Spielwelt gewählte Weiche übernehmen",()=>binding.Do(()=>{draft.Reference=switchPicked;RefreshWorkspaceFields();}));
                if(PanelConnections.Count(draft.Ports)==3){int stem,normal,branch;PanelConnections.Arms(draft,out stem,out normal,out branch);var ports=Enumerable.Range(0,8).Where(i=>(draft.Ports&(1<<i))!=0).ToArray();c.AddField("Stammast im Symbol",c.AddDropdown(ports.Select(i=>PanelConnections.Name(draft,i)).ToList(),Array.FindIndex(ports,i=>(1<<i)==stem),i=>binding.Do(()=>{draft.Stem=1<<ports[i];draft.Normal=0;RefreshWorkspaceFields();})));var choices=ports.Where(i=>(1<<i)!=stem).ToArray();c.AddField("Grundstellungsast im Symbol",c.AddDropdown(choices.Select(i=>PanelConnections.Name(draft,i)).ToList(),Array.FindIndex(choices,i=>(1<<i)==normal),i=>binding.Do(()=>draft.Normal=1<<choices[i])));}
            } else if(draft.Kind!=PanelTileKind.Label) {
                if(CircuitField(draft)) {
                    BuildBoundarySymbol(c,binding);
                    BuildCircuitEditor(c,new PanelRegion{X=draft.X,Y=draft.Y});
                }
                c.AddLabel("Für Besetztanzeigen ein reales Gleis zuordnen: Rechtsklick im Spiel → Gleis-/Weichenkennungen, dann hier übernehmen.");
                if(!string.IsNullOrEmpty(trackPicked))c.AddButton("Zuletzt in der Spielwelt angeklicktes Gleis übernehmen",()=>binding.Do(()=>{var segment=Track.Graph.Shared?.GetSegment(trackPicked);if(segment!=null){segmentInput=segment.id;fromInput="0";toInput=SignUnits.Number(segment.GetLength());RefreshWorkspaceFields();}}));
                c.AddField("Gleis-ID",c.AddInputField(segmentInput,v=>binding.Do(()=>segmentInput=v),"Leer: reine Zeichnung",128));c.AddField("Von Meter (ab Ende A)",c.AddInputField(fromInput,v=>binding.Do(()=>fromInput=v),"0",24));c.AddField("Bis Meter (ab Ende A)",c.AddInputField(toInput,v=>binding.Do(()=>toInput=v),"Gleislänge",24));
            }
            if(draft.Kind==PanelTileKind.Signal)
                BuildCircuitEditor(c,new PanelRegion{X=draft.X,Y=draft.Y});
            c.AddButton(WindowText("Save field and link","Feld und Zuordnung speichern"),()=>binding.Do(()=>Act(SaveTile)));
            c.AddButton(WindowText("Delete field…","Feld löschen …"),()=>binding.Do(()=>Act(()=>{region=null;selectedX=draft.X;selectedY=draft.Y;AskDelete();})));
            c.AddExpandable("Einzelbaustein / technische Kennung",advancedEditor,details=>{
                details.AddField("Baustein",details.AddDropdown(ToolNames.ToList(),(int)draft.Kind,i=>binding.Do(()=>{draft.Kind=(PanelTileKind)i;draft.AlternateSignal=null;draft.Target=null;draft.LinkedAuxiliary=null;draft.CircuitRails=null;draft.MainSignalDirection=draft.ShuntSignalDirection=null;
                    draft.SwapSignalHalves=false;draft.Ports=draft.Stem=draft.Normal=0;LoadTargetFields(draft);RefreshWorkspaceFields();})));
                if(draft.Kind==PanelTileKind.Signal||draft.Kind==PanelTileKind.Switch)details.AddField("Feste Spiel-ID",details.AddInputField(draft.Reference,v=>binding.Do(()=>draft.Reference=v),"Interne Kennung",128));
            },ExpandableStyle.SectionHeader);
        }
        void BuildLinkedAuxiliary(UIPanelBuilder c,PanelDraftBinding binding) {
            var choices=desks.Desks.SelectMany(d=>d.Panel.Tiles).Select(t=>t.Target).Where(t=>t!=null)
                .GroupBy(t=>t.Id,StringComparer.Ordinal).Select(g=>g.First()).OrderBy(t=>t.Caption,StringComparer.OrdinalIgnoreCase).ToArray();
            if(choices.Length==0)return;
            var labels=new[]{"Ohne Hilfsziel"}.Concat(choices.Select(t=>t.Caption)).ToList();
            c.AddField("Hilfsziel dahinter",c.AddDropdown(labels,Array.FindIndex(choices,t=>t.Id==binding.Draft.LinkedAuxiliary)+1,
                i=>binding.Do(()=>binding.Draft.LinkedAuxiliary=i==0?null:choices[i-1].Id)));
        }

        void BuildSplitLayout(UIPanelBuilder c,PanelDraftBinding binding) {
            var draft=binding.Draft;
            if(Object(draft.Reference) is MarkerRecord signal&&signal.Kind==MarkerRecord.Main&&signal.AllowSh1&&draft.AlternateSignal!=draft.Reference)
                c.AddButton("Sh 1 dieses Hauptsignalkopfes verwenden",()=>binding.Do(()=>{
                    draft.AlternateSignal=draft.Reference;RefreshWorkspaceFields();panelView?.Refresh();
                }));
            var choices=new[]{"Rechts","Unten","Links","Oben"};
            c.AddLabel("Richtung im Gleisbild");
            c.AddField("Hauptsignal",c.AddDropdown(choices.ToList(),SignalCellLayout.MainDirection(draft),i=>binding.Do(()=>{
                draft.MainSignalDirection=i;panelView?.Refresh();
            })));
            c.AddField("Rangiersignal",c.AddDropdown(choices.ToList(),SignalCellLayout.ShuntDirection(draft),i=>binding.Do(()=>{
                draft.ShuntSignalDirection=i;panelView?.Refresh();
            })));
            c.AddButton("Hälften tauschen",()=>binding.Do(()=>{
                draft.SwapSignalHalves=!draft.SwapSignalHalves;
                panelView?.Refresh();
            }));
            c.AddLabel(()=>draft.SwapSignalHalves?"Links Rangieren · rechts Zugfahrt":"Links Zugfahrt · rechts Rangieren",
                UIPanelBuilder.Frequency.Periodic);
        }

        void BuildCanvasSize(UIPanelBuilder c) {
            c.AddField("Spalten (8–512)",c.AddInputField(columnsInput,v=>columnsInput=v,"96",3));c.AddField("Zeilen (6–320)",c.AddInputField(rowsInput,v=>rowsInput=v,"48",3));
            c.AddButton("Flächengröße übernehmen",()=>Act(()=>{int columns,rows;if(!int.TryParse(columnsInput,out columns)||!int.TryParse(rowsInput,out rows))throw new InvalidOperationException("Größe als ganze Zahlen eingeben.");EditPanel(doc=>{doc.Columns=columns;doc.Rows=rows;});ResetEditor();tileDraft=null;region=null;status="Flächengröße gespeichert. Vorhandene Felder bleiben erhalten.";}));
        }
    }
}
