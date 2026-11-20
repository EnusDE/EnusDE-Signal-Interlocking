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
        int editorMode,drawX=-1,drawY=-1;bool drawingActive;
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
            status=LocalText.Get("Signal ")+Code(id)+" vorgemerkt. Stellwerk und Signalfeld wählen.";
        }

        void BuildPendingSignal(UIPanelBuilder c,PanelDraftBinding binding) {
            if(!HasPendingSignal)return;
            string id=pendingSignalId;
            c.AddButton(LocalText.Get("Vorgemerktes Signal ")+Code(id)+" übernehmen",()=>binding.Do(()=>Act(()=>{
                RequirePanelEdit();
                if(!HasPendingSignal||pendingSignalId!=id)return;
                string previous=binding.Draft.Reference;
                binding.Draft.Reference=id;
                var signal=Object(id);
                if(signal?.Kind!=MarkerRecord.Main)binding.Draft.AlternateSignal=null;
                else if(signal.AllowSh1&&(binding.Draft.AlternateSignal==null||binding.Draft.AlternateSignal==previous))
                    binding.Draft.AlternateSignal=id;
                status=LocalText.Get("Signal vorgemerkt. Feld und Zuordnung speichern.");
                panelView?.Refresh();
            })));
        }
        PanelDeletion pendingDelete;
        string deletionDesk,transferTarget;
        readonly UIState<bool> advancedEditor=new UIState<bool>(false);
        readonly UIState<bool> alternateSignalEditor=new UIState<bool>(false);
        void ResetEditor(){drawX=drawY=-1;drawingActive=false;pastePending=false;pendingDelete=null;ClearRouteProposals();ResetCircuitSelection();}
        PanelTile PreviewTile()=>editingWorkspace&&panelPage==1?tileDraft:null;
        PanelRegion SelectedRegion()=>editingWorkspace?region??(selectedX>=0&&selectedY>=0?new PanelRegion{X=selectedX,Y=selectedY}:null):null;
        void EditorMode(int mode) {
            editorMode=mode;tool=-1;regionAction=0;ResetEditor();axisSelection.Clear();tileDraft=null;
            status=mode==0?WindowText("Choose the first cell of the track.","Erstes Gleisfeld wählen."):
                mode==1?WindowText("Left-click to select; right-click for categories.","Linksklick wählt aus; Rechtsklick öffnet die Kategorien."):
                mode==3?WindowText("Choose a field symbol on the right.","Rechts ein Feldsymbol auswählen."):
                WindowText("Select a region to move or copy it.","Bereich zum Verschieben oder Kopieren markieren.");
            editorFieldCategory=EditorFieldCategory.Properties;
            if(mode==0||mode==1)CloseWorkspaceContext();else OpenWorkspaceContext();
            panelView?.Refresh();
        }
        void ChooseSymbol(PanelTileKind kind) {
            tool=(int)kind;
            workspaceActionError=null;
            status=WindowText("Click grid cells to place the selected symbol.","Ins Raster klicken, um das gewählte Symbol zu setzen.");
            RefreshWorkspaceFields();
        }
        void SelectAll(){EditorMode(2);region=new PanelRegion{X=0,Y=0,Width=panel.Columns,Height=panel.Rows};status=LocalText.Get("Gesamte Zeichnung markiert. Löschen benötigt eine Bestätigung.");panelView?.Refresh();RefreshWorkspaceFields();}
        bool EditorClick(int x,int y) {
            if(pastePending){var clip=clipboard??throw new InvalidOperationException("Kein Bereich kopiert.");int width=Math.Max(1,Math.Max(clip.Tiles.Select(t=>t.X+1).DefaultIfEmpty(1).Max(),clip.Areas.Select(a=>a.X+a.Width).DefaultIfEmpty(1).Max())),height=Math.Max(1,Math.Max(clip.Tiles.Select(t=>t.Y+1).DefaultIfEmpty(1).Max(),clip.Areas.Select(a=>a.Y+a.Height).DefaultIfEmpty(1).Max()));var growth=EditCanvas(new PanelRegion{X=x,Y=y,Width=width,Height=height},(doc,dx,dy)=>PanelEditing.Paste(doc,clip,x+dx,y+dy));pastePending=false;region=new PanelRegion{X=x+growth.Left,Y=y+growth.Top,Width=width,Height=height};status=LocalText.Get("Bereich übernommen. Feste Bindungen erhalten; ursprüngliche Tafel unverändert.");return true;}
            if(editorMode!=0||tool>=0)return false;
            if(!drawingActive){drawingActive=true;drawX=x;drawY=y;tileDraft=null;status=LocalText.Get("Zeichenstart gesetzt. Nächsten Eck- oder Endpunkt anklicken.");panelView?.Refresh();return true;}
            var added=EditCanvas(new PanelRegion{X=Math.Min(drawX,x),Y=Math.Min(drawY,y),Width=Math.Abs(drawX-x)+1,Height=Math.Abs(drawY-y)+1},(doc,dx,dy)=>PanelEditing.Draw(doc,drawX+dx,drawY+dy,x+dx,y+dy));drawX=x+added.Left;drawY=y+added.Top;tileDraft=null;status=LocalText.Get("Gleiszug gespeichert. Nächsten Punkt anklicken oder Zeichnung beenden. Anschlüsse und Weichenzeichen werden angepasst.");return true;
        }
        void InsertSpace(bool row) {
            RequirePanelEdit();int at=row?selectedY:selectedX;if(at<0)throw new InvalidOperationException(WindowText("Select a row or column first.","Zuerst eine Zeile oder Spalte auswählen."));InsertCanvasAxes(row,at);
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
                status=LocalText.Get("Zeichnungsfeld gelöscht. Mit Rückgängig wiederherstellbar.");
                RefreshWorkspaceFields();
            });
        }
        void AskDelete() {
            RequirePanelEdit();var selection=region??(selectedX>=0?new PanelRegion{X=selectedX,Y=selectedY}:null);if(selection==null)throw new InvalidOperationException("Feld/Bereich auswählen oder Alles markieren.");
            OpenWorkspaceContext();pendingDelete=new PanelDeletion(panel,selection,region==null);deletionDesk=deskId;drawX=drawY=-1;drawingActive=false;tool=-1;status=LocalText.Get("Löschung bitte bestätigen. Nur Zeichnungsfelder dieser Tafel sind betroffen.");RefreshWorkspaceFields();
        }
        void BuildDeleteConfirmation(UIPanelBuilder c) {
            var request=pendingDelete;
            c.AddLabel(request.KeepsTrack?"Symbol entfernen und Gleislinie erhalten?":request.Count+" Zeichnungsfelder löschen? Platzierte Spielobjekte und Fahrstraßendefinitionen bleiben erhalten.");
            c.HStack(row=>{row.AddButton(request.KeepsTrack?"Symbol entfernen":"Felder löschen",()=>Act(()=>{if(!ReferenceEquals(request,pendingDelete)||deletionDesk!=deskId)return;EditPanel(doc=>request.Apply(doc,true));pendingDelete=null;tileDraft=null;region=null;status=request.KeepsTrack?"Symbol entfernt. Die Gleislinie bleibt verbunden.":"Bestätigte Zeichnungsfelder gelöscht.";}));row.AddButton(LocalText.Get("Abbrechen"),()=>{if(!ReferenceEquals(request,pendingDelete))return;pendingDelete=null;status=LocalText.Get("Löschung abgebrochen.");RefreshWorkspaceFields();});});
        }
        void CopyCurrentRegion(){RequirePanelEdit();var selected=region??new PanelRegion{X=0,Y=0,Width=panel.Columns,Height=panel.Rows};clipboard=selected.Extract(panel);if(clipboard.Tiles.Count==0)throw new InvalidOperationException("Bereich enthält keine Felder.");}
        void PasteInto(string target) {
            CopyCurrentRegion();SelectDesk(target,false);panelPage=1;editorMode=2;pastePending=true;status=LocalText.Get("Zielfeld für die linke obere Ecke anklicken. Bei belegtem Ziel wird nichts überschrieben.");Rebuild();
        }
        void BuildTransfers(UIPanelBuilder c) {
            BuildNamedAreas(c);
            if(clipboard!=null&&clipboard.WorldKey==scope)c.AddButton(LocalText.Get("Kopierten Bereich hier einfügen …"),()=>{ResetEditor();editorMode=2;pastePending=true;status=LocalText.Get("Freie linke obere Zielecke anklicken.");RefreshWorkspaceFields();});
            if(CurrentDesk?.Global!=true)c.AddButton(LocalText.Get("Diesen Bereich ins ESTW übernehmen …"),()=>Act(()=>PasteInto(desks.Desks.Single(d=>d.Global).Id)));
            var targets=desks.Desks.Where(d=>d.Id!=deskId).ToArray();if(targets.Length>0){if(!targets.Any(d=>d.Id==transferTarget))transferTarget=targets[0].Id;c.AddSearchChoice(LocalText.Get("Vorhandenes Zielstellwerk"),targets.Select(d=>d.Code+" · "+d.Name).ToList(),Array.FindIndex(targets,d=>d.Id==transferTarget),i=>transferTarget=targets[i].Id);c.AddButton(LocalText.Get("Bereich ins Zielstellwerk übernehmen …"),()=>Act(()=>PasteInto(transferTarget)));}
            c.AddLabel(LocalText.Get("Übernommen wird eine unabhängige Zeichnung mit denselben Spiel-IDs. Spätere Geometrieänderungen werden nicht automatisch zwischen den Tafeln gespiegelt."));
        }
        void ChangeZoom(float value) {
            value=Mathf.Clamp(value,.025f,2);
            if(editingWorkspace&&panelView?.CanvasSurface!=null&&panelScroll?.viewport!=null){panelZoom=value;panelView.ZoomAt(value,panelView.WheelAnchor??panelScroll.viewport.TransformPoint(panelScroll.viewport.rect.center));nextPanelRead=0;return;}
            if(panelView==null){panelZoom=value;return;}var old=panelZoom;var position=panelScroll==null?new Vector2(0,1):panelScroll.normalizedPosition;var viewport=panelScroll?.viewport;float width=viewport==null?0:viewport.rect.width,height=viewport==null?0:viewport.rect.height;
            float cx=(Mathf.Max(0,panel.Columns*PanelGeometry.CellWidth*old-width)*position.x+width/2)/(PanelGeometry.CellWidth*old),cy=(Mathf.Max(0,panel.Rows*PanelGeometry.CellHeight*old-height)*(1-position.y)+height/2)/(PanelGeometry.CellHeight*old);
            panelZoom=value;panelView.SetZoom(value);Canvas.ForceUpdateCanvases();if(panelScroll!=null){float dx=Mathf.Max(0,panel.Columns*PanelGeometry.CellWidth*value-width),dy=Mathf.Max(0,panel.Rows*PanelGeometry.CellHeight*value-height);panelScroll.normalizedPosition=new Vector2(dx==0?0:Mathf.Clamp01((cx*PanelGeometry.CellWidth*value-width/2)/dx),dy==0?1:1-Mathf.Clamp01((cy*PanelGeometry.CellHeight*value-height/2)/dy));}nextPanelRead=0;
        }
        bool dispatcherContextRight;
        void CellContext(int x,int y,bool rightHalf=false) {
            Act(()=>{
                if(editingWorkspace) {
                    RequirePanelEdit();
                    if(pendingDelete!=null){OpenWorkspaceContext();return;}
                    // Right-click selects its own target before constructing any menu.
                    if(region==null||!region.Contains(x,y)) {
                        panelPage=1;editorMode=1;tool=-1;regionAction=0;region=null;drawX=drawY=-1;drawingActive=false;
                        selectedX=x;selectedY=y;tileDraft=panel.At(x,y)?.Clone();
                        if(tileDraft!=null)LoadTileFields();
                        editorFieldCategory=EditorFieldCategory.Properties;panelView?.Refresh();RebuildEditorToolsWindow();
                    }
                    ShowEditorCategories(x,y);return;
                } else {
                    panelPage=0;
                    dispatcherContextRight=rightHalf;
                }
                selectedX=x;selectedY=y;
                OpenWorkspaceContext();panelView?.Refresh();
            });
        }
        void StartFrom(string id,bool yard,bool slow=false){RequireDispatcher();ClearAutomaticRouteChoices();var signal=Object(id);if(!yard&&!slow&&!SignalCatalogue.Supports(signal,false,false)&&SignalCatalogue.Supports(signal,false,true))slow=true;if(signal==null||!SignalCatalogue.Supports(signal,yard,slow))throw new InvalidOperationException("Signal unterstützt diese Fahrart nicht.");panelPage=0;shunting=yard;reduced=slow;panelButtons.Clear();panelButtons.Press(id,ButtonSignal,yard);status=LocalText.Get("Start ")+SignalCaption(id)+" · "+(yard?"Rangierfahrt":slow?"Langsamfahrt":"Zugfahrt")+". Zielsignal anklicken.";CloseWorkspaceContext();panelView?.Refresh();}
        void BuildFieldAssignment(UIPanelBuilder c) {
            var binding=new PanelDraftBinding(tileDraft,()=>tileDraft);var draft=binding.Draft;
            if(draft==null){c.AddLabel(LocalText.Get("Feld anklicken oder rechtsklicken → Feld zuordnen / bearbeiten. Gezeichnete Gleise sind zunächst nur Darstellung."));return;}
            c.AddSection(LocalText.Get("Feld ")+(draft.X+1)+" / "+(draft.Y+1)+" · "+ToolNames[(int)draft.Kind]);
            c.AddLabel(WindowText("Caption","Beschriftung"));
            c.AddInputField(draft.Label,v=>binding.Do(()=>{draft.Label=v;panelView?.Refresh();}),WindowText("Use signal code if empty","Leer: Signalkennung verwenden"),48);
            c.AddSection(WindowText("Preview","Vorschau"));
            var preview=c.HStack(_=>{});
            AddEditorIcon(preview,WindowText("Rotate 90°","90° drehen"),"ROTATE",()=>binding.Do(()=>Act(()=>QuickFieldTransform(draft.X,draft.Y))));
            AddEditorIcon(preview,WindowText("Mirror","Spiegeln"),"MIRROR",()=>binding.Do(()=>Act(()=>QuickFieldTransform(draft.X,draft.Y,true))));
            if(draft.Kind==PanelTileKind.LinePermission)BuildLineIndicatorEditor(c,binding);
            else if(draft.Kind==PanelTileKind.TrainNumber||draft.Kind==PanelTileKind.Signal) {
                if(draft.Kind==PanelTileKind.TrainNumber) {
                    BuildNumberField(c,binding);
                    if(!NumberMirrors.IsMirror(draft))BuildCircuitEditor(c,new PanelRegion{X=draft.X,Y=draft.Y});
                }
                else {
                    BuildPendingSignal(c,binding);
                    if(draft.Target==null)BuildLinkedAuxiliary(c,binding);
                    BuildShuntAuxiliary(c,binding);
                }
                if(!NumberMirrors.IsMirror(draft))BuildSignalSearch(c,binding);
                if(draft.Kind==PanelTileKind.Signal) {
                    c.AddFieldToggle(WindowText("Neighbour signal · display only","Nachbarsignal · nur anzeigen"),()=>draft.ReadOnlySignal,v=>binding.Do(()=>draft.ReadOnlySignal=v),true);
                    if(draft.ReadOnlySignal)c.AddLabel(WindowText("Aspect and GFM occupancy remain visible. Signal operation stays with the owning interlocking.","Signalzustand und GFM-Belegung bleiben sichtbar. Die Bedienung verbleibt beim zuständigen Stellwerk."));
                }
                if(draft.Kind==PanelTileKind.Signal&&!draft.ReadOnlySignal&&Object(draft.Reference)?.Kind==MarkerRecord.Main) {
                    string roleSignal=draft.Reference,roleDesk=deskId;
                    BuildSignalRoleEditor(c,roleSignal,()=>{
                        RequirePanelEdit();if(!binding.Active||deskId!=roleDesk||draft.Reference!=roleSignal)throw new InvalidOperationException(WindowText("Signal assignment changed. Reopen the field.","Signalzuordnung geändert. Feld erneut öffnen."));
                    });
                }
                else if(draft.Kind==PanelTileKind.Signal&&!draft.ReadOnlySignal)BuildSignalOperatingEditor(c,draft.Reference,()=>{RequirePanelEdit();if(!binding.Active)throw new InvalidOperationException(WindowText("Reopen the field.","Feld erneut öffnen."));});
                var caption=draft.Kind==PanelTileKind.TrainNumber?
                    WindowText("Opposite direction","Gegenrichtung"):WindowText("Split signal field","Signalfeld teilen");
                if(!NumberMirrors.IsMirror(draft)&&draft.Target==null&&Object(draft.Reference)?.Kind!=MarkerRecord.Shunt)c.AddExpandable(caption,alternateSignalEditor,more=>{
                    BuildSignalSearch(more,binding,true);
                    if(draft.Kind==PanelTileKind.Signal)BuildSplitLayout(more,binding);
                },ExpandableStyle.SectionHeader);
            }
            else if(draft.Kind==PanelTileKind.AuxiliaryTarget)BuildAuxiliaryEditor(c,binding);
            else if(draft.Kind==PanelTileKind.Switch){
                c.AddField(LocalText.Get("Weiche"),PointAssignments.Caption(desks,draft.Reference));
                c.AddField(LocalText.Get("Aktuelle Lage"),()=>PointPosition(draft.Reference),UIPanelBuilder.Frequency.Periodic);
                c.AddLabel(LocalText.Get("Im Spiel nahe der gewünschten Weiche rechtsklicken → Weiche zuordnen. Zielstellwerk wählen, hier das Feld rechtsklicken → Zuordnung übernehmen."));
                if(assigningPoint)c.AddButton(LocalText.Get("Vorgemerkte Weiche diesem Feld zuordnen"),()=>binding.Do(()=>Act(()=>AcceptPoint(draft.X,draft.Y))));
                if(!string.IsNullOrEmpty(switchPicked))c.AddButton(LocalText.Get("Zuletzt in der Spielwelt gewählte Weiche übernehmen"),()=>binding.Do(()=>{draft.Reference=switchPicked;RefreshWorkspaceFields();}));
                if(PanelConnections.Count(draft.Ports)==3){int stem,normal,branch;PanelConnections.Arms(draft,out stem,out normal,out branch);var ports=Enumerable.Range(0,8).Where(i=>(draft.Ports&(1<<i))!=0).ToArray();c.AddField(LocalText.Get("Stammast im Symbol"),c.AddDropdown(ports.Select(i=>PanelConnections.Name(draft,i)).ToList(),Array.FindIndex(ports,i=>(1<<i)==stem),i=>binding.Do(()=>{draft.Stem=1<<ports[i];draft.Normal=0;RefreshWorkspaceFields();})));var choices=ports.Where(i=>(1<<i)!=stem).ToArray();c.AddField(LocalText.Get("Grundstellungsast im Symbol"),c.AddDropdown(choices.Select(i=>PanelConnections.Name(draft,i)).ToList(),Array.FindIndex(choices,i=>(1<<i)==normal),i=>binding.Do(()=>draft.Normal=1<<choices[i])));}
            } else if(draft.Kind!=PanelTileKind.Label) {
                if(CircuitField(draft)) {
                    BuildBoundarySymbol(c,binding);
                    if(!NumberMirrors.IsMirror(draft))BuildCircuitEditor(c,new PanelRegion{X=draft.X,Y=draft.Y});
                }
                c.AddLabel(LocalText.Get("Für Besetztanzeigen ein reales Gleis zuordnen: Rechtsklick im Spiel → Gleis-/Weichenkennungen, dann hier übernehmen."));
                if(!string.IsNullOrEmpty(trackPicked))c.AddButton(LocalText.Get("Zuletzt in der Spielwelt angeklicktes Gleis übernehmen"),()=>binding.Do(()=>{var segment=Track.Graph.Shared?.GetSegment(trackPicked);if(segment!=null){segmentInput=segment.id;fromInput="0";toInput=SignUnits.Number(segment.GetLength());RefreshWorkspaceFields();}}));
                c.AddField(LocalText.Get("Gleis-ID"),c.AddInputField(segmentInput,v=>binding.Do(()=>segmentInput=v),"Leer: reine Zeichnung",128));c.AddField(LocalText.Get("Von Meter (ab Ende A)"),c.AddInputField(fromInput,v=>binding.Do(()=>fromInput=v),"0",24));c.AddField(LocalText.Get("Bis Meter (ab Ende A)"),c.AddInputField(toInput,v=>binding.Do(()=>toInput=v),"Gleislänge",24));
            }
            if(draft.Kind==PanelTileKind.Signal)
                BuildCircuitEditor(c,new PanelRegion{X=draft.X,Y=draft.Y});
            c.AddButton(WindowText("Save field and link","Feld und Zuordnung speichern"),()=>binding.Do(()=>Act(SaveTile)));
            c.AddButton(WindowText("Delete field…","Feld löschen …"),()=>binding.Do(()=>Act(()=>{region=null;selectedX=draft.X;selectedY=draft.Y;AskDelete();})));
            c.AddExpandable(LocalText.Get("Einzelbaustein / technische Kennung"),advancedEditor,details=>{
                details.AddSearchChoice(LocalText.Get("Baustein"),ToolNames.ToList(),(int)draft.Kind,i=>binding.Do(()=>{if(draft.Kind!=(PanelTileKind)i)draft.NumberId=draft.NumberMirror=null;draft.Kind=(PanelTileKind)i;draft.InlinePermission=draft.Kind==PanelTileKind.LinePermission;draft.PermissionAuxiliary=null;draft.AlternateSignal=null;draft.Target=null;draft.LinkedAuxiliary=null;draft.CircuitRails=null;draft.MainSignalDirection=draft.ShuntSignalDirection=null;
                    draft.SwapSignalHalves=false;draft.Ports=draft.Stem=draft.Normal=0;LoadTargetFields(draft);RefreshWorkspaceFields();}));
                if(draft.Kind==PanelTileKind.Signal||draft.Kind==PanelTileKind.Switch)details.AddField(LocalText.Get("Feste Spiel-ID"),details.AddInputField(draft.Reference,v=>binding.Do(()=>draft.Reference=v),"Interne Kennung",128));
            },ExpandableStyle.SectionHeader);
        }
        void BuildShuntAuxiliary(UIPanelBuilder c,PanelDraftBinding binding) {
            var draft=binding.Draft;
            if(draft.Kind!=PanelTileKind.Signal)return;
            var signal=Object(draft.Reference);
            if(signal?.Kind!=MarkerRecord.Shunt&&draft.Target==null)return;
            c.AddSection(LocalText.Get("Rangiersignal mit Hilfszieltaste"));
            c.AddField(LocalText.Get("Signalrichtung im Gleisbild"),c.AddDropdown(new[]{"Rechts","Unten","Links","Oben"}.ToList(),SignalCellLayout.MainDirection(draft),i=>binding.Do(()=>{
                draft.MainSignalDirection=i;panelView?.Refresh();
            })));
            if(draft.Target==null) {
                c.AddButton(LocalText.Get("Rangier-Hilfszieltaste hinzufügen"),()=>binding.Do(()=>{
                    if(signal?.Kind!=MarkerRecord.Shunt||signal.Anchor==null)return;
                    draft.AlternateSignal=null;draft.LinkedAuxiliary=null;draft.SwapSignalHalves=false;
                    draft.Target=new AuxiliaryTarget{Name="Ziel "+signal.Name.Substring(0,Math.Min(43,signal.Name.Length)),Anchor=signal.Anchor.Clone(),Train=false,Shunt=true,Reverse=!signal.FacingReverse};
                    LoadTargetFields(draft);RefreshWorkspaceFields();panelView?.Refresh();
                }));
                return;
            }
            c.AddLabel(LocalText.Get("Start und Hilfsziel sind getrennte Tasten. Das Ziel liegt im Gleisbild hinter der Starttaste und zeigt in die Gegenrichtung. Es erzeugt kein zusätzliches Signal."));
            c.AddField(LocalText.Get("Zielbezeichnung"),c.AddInputField(draft.Target.Name,v=>binding.Do(()=>{draft.Target.Name=v;panelView?.Refresh();}),"Rangierziel",48));
            BuildAuxiliaryEditor(c,binding);
            c.AddButton(LocalText.Get("Hilfszieltaste entfernen"),()=>binding.Do(()=>{draft.Target=null;LoadTargetFields(draft);RefreshWorkspaceFields();panelView?.Refresh();}));
        }
        void BuildLinkedAuxiliary(UIPanelBuilder c,PanelDraftBinding binding) {
            var choices=desks.Desks.SelectMany(d=>d.Panel.Tiles).Select(t=>t.Target).Where(t=>t!=null)
                .GroupBy(t=>t.Id,StringComparer.Ordinal).Select(g=>g.First()).OrderBy(t=>t.Caption,StringComparer.OrdinalIgnoreCase).ToArray();
            if(choices.Length==0)return;
            var labels=new[]{"Ohne Hilfsziel"}.Concat(choices.Select(t=>t.Caption)).ToList();
            c.AddSearchChoice(LocalText.Get("Hilfsziel dahinter"),labels,Array.FindIndex(choices,t=>t.Id==binding.Draft.LinkedAuxiliary)+1,
                i=>binding.Do(()=>binding.Draft.LinkedAuxiliary=i==0?null:choices[i-1].Id));
        }

        void BuildSplitLayout(UIPanelBuilder c,PanelDraftBinding binding) {
            var draft=binding.Draft;
            if(Object(draft.Reference) is MarkerRecord signal&&signal.Kind==MarkerRecord.Main&&signal.AllowSh1&&draft.AlternateSignal!=draft.Reference)
                c.AddButton(LocalText.Get("Sh 1 dieses Hauptsignalkopfes verwenden"),()=>binding.Do(()=>{
                    draft.AlternateSignal=draft.Reference;RefreshWorkspaceFields();panelView?.Refresh();
                }));
            var choices=new[]{"Rechts","Unten","Links","Oben"};
            c.AddLabel(LocalText.Get("Richtung im Gleisbild"));
            c.AddField(LocalText.Get("Hauptsignal"),c.AddDropdown(choices.ToList(),SignalCellLayout.MainDirection(draft),i=>binding.Do(()=>{
                draft.MainSignalDirection=i;panelView?.Refresh();
            })));
            c.AddField(LocalText.Get("Rangiersignal"),c.AddDropdown(choices.ToList(),SignalCellLayout.ShuntDirection(draft),i=>binding.Do(()=>{
                draft.ShuntSignalDirection=i;panelView?.Refresh();
            })));
            c.AddButton(LocalText.Get("Hälften tauschen"),()=>binding.Do(()=>{
                draft.SwapSignalHalves=!draft.SwapSignalHalves;
                panelView?.Refresh();
            }));
            c.AddLabel(()=>draft.SwapSignalHalves?"Links Rangieren · rechts Zugfahrt":"Links Zugfahrt · rechts Rangieren",
                UIPanelBuilder.Frequency.Periodic);
        }

        void BuildCanvasSize(UIPanelBuilder c) {
            c.AddField(LocalText.Select("Columns (8–2048)","Spalten (8–2048)"),c.AddInputField(columnsInput,v=>columnsInput=v,"96",4));c.AddField(LocalText.Select("Rows (6–512)","Zeilen (6–512)"),c.AddInputField(rowsInput,v=>rowsInput=v,"48",3));
            c.AddButton(LocalText.Get("Flächengröße übernehmen"),()=>Act(()=>{int columns,rows;if(!int.TryParse(columnsInput,out columns)||!int.TryParse(rowsInput,out rows))throw new InvalidOperationException("Größe als ganze Zahlen eingeben.");EditPanel(doc=>{doc.Columns=columns;doc.Rows=rows;});ResetEditor();tileDraft=null;region=null;status=LocalText.Get("Flächengröße gespeichert. Vorhandene Felder bleiben erhalten.");}));
        }
    }
}
