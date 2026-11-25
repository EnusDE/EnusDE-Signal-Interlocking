using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Game.State;
using Track;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        PanelDocument panel;
        readonly PanelLookup panelLookup=new PanelLookup();
        PanelHistory history;
        string historyError;
        PanelStore panelStore;
        SignalWorldBridge panelWorld;
        readonly PanelButtons panelButtons=new PanelButtons();
        readonly Dictionary<string,TrackOccupancy> panelReadings=new Dictionary<string,TrackOccupancy>();
        string panelError,trackPicked,switchPicked,segmentInput="",fromInput="0",toInput="",columnsInput="24",rowsInput="12";
        int panelPage,tool=-1,selectedX=-1,selectedY=-1;
        PanelTile tileDraft;
        UIPanel inspector;
        PanelView panelView;
        ScrollRect panelScroll;
        Vector2 panelScrollPosition=new Vector2(0,1);
        double nextPanelRead,panelReadTime=-100;
        static readonly string[] ToolNames={"Gerade","Diagonale","Bogen","Weiche","Kreuzung","Signaltaste","Beschriftung","Zugnummernfeld","Hilfsziel","Strecken-/Erlaubnismelder"};
        bool PanelVisible=>manager!=null&&manager.GetCustomWindow(ActiveWindowId)?.gameObject.activeInHierarchy==true;
        void LoadPanel() {
            panelWorld?.Dispose();panelWorld=null;panelReadings.Clear();panelReadTime=-100;panelButtons.Clear();tileDraft=null;selectedX=selectedY=-1;
            panelStore=new PanelStore(Path.Combine(Path.GetDirectoryName(directory),"Panels"),scope);panelError=null;
            try{deskStore=new DeskStore(Path.Combine(Path.GetDirectoryName(directory),"Desks"),scope,Entry.Shared.Desks,Entry.Shared.PutDesks);desks=deskStore.Load(null);SelectDesk(desks.Desks.Single(d=>d.Global).Id,false);}
            catch(Exception e){panel=null;panelError="Gleisbilddatei bleibt erhalten: "+e.Message;}
        }
        void RequirePanelEdit(){
            RequireEdit();
            if(!editingWorkspace||!PanelVisible||panelPage==4)throw new InvalidOperationException(WindowText("Choose an interlocking in the editor before editing.","Zum Bearbeiten ein Stellwerk im Editor auswählen."));
            if(panelStore==null||panel==null||panelError!=null)throw new InvalidOperationException(panelError??"Gleisbild noch nicht geladen.");
        }
        void RequireDispatcher(){
            RequireEdit();
            if(editingWorkspace||!PanelVisible)throw new InvalidOperationException(WindowText("Open the dispatcher window to operate the interlocking.","Zum Bedienen das Fahrdienstleiter-Fenster öffnen."));
        }
        void EditPanel(Action<PanelDocument> edit,bool recordHistory=true,TrackBlock addBlock=null) {
            RequirePanelEdit();var before=panel;var next=panel.Clone();edit(next);var scrollBefore=panelScroll?.content?.anchoredPosition;
            CheckTargetEdit(before,next,desks.Desks.Where(d=>d.Id!=deskId).SelectMany(d=>d.Panel.Tiles));
            desks=deskStore.PutPanel(desks,deskId,next,addBlock);panel=desks.Desks.Single(d=>d.Id==deskId).Panel;
            int dx=panel.OriginX-before.OriginX,dy=panel.OriginY-before.OriginY;RebaseEditorCanvas(dx,dy);columnsInput=panel.Columns.ToString();rowsInput=panel.Rows.ToString();
            if(recordHistory&&history!=null)try{history.Record(before,panel);historyError=null;}catch(Exception e){historyError="Änderung gespeichert, Verlauf nicht geschrieben: "+e.Message;log?.Warning(historyError);}
            panelWorld?.Dispose();panelWorld=new SignalWorldBridge(this,transform,2048);panelReadings.Clear();panelReadTime=-100;nextPanelRead=0;
            if(panelView!=null){panelView.ReplaceDocument(panel);
                if(editingWorkspace&&scrollBefore.HasValue&&panelScroll?.content!=null){Canvas.ForceUpdateCanvases();panelScroll.StopMovement();panelScroll.content.anchoredPosition=scrollBefore.Value+new Vector2(-dx*PanelGeometry.CellWidth*panelZoom,dy*PanelGeometry.CellHeight*panelZoom);}
                if(editorMode!=3)RefreshWorkspaceFields();}else Rebuild();
        }
        void ReadPanel() {
            if(panel==null||panelView==null||!PanelVisible||Time.realtimeSinceStartup<nextPanelRead)return;
            nextPanelRead=Time.realtimeSinceStartup+1;panelReadings.Clear();panelReadTime=-100;
            if(StateManager.IsUnloading)return;
            ReadBlockPanel();
            panelReadTime=Time.realtimeSinceStartup;
        }
        TrackOccupancy PanelOccupancy(PanelTile tile) {
            if(tile==null||Time.realtimeSinceStartup-panelReadTime>=5)return TrackOccupancy.Unknown;TrackOccupancy result;if(tile.Section!=null)return panelReadings.TryGetValue(tile.Section.Key,out result)?result:TrackOccupancy.Unknown;bool any=false,unknown=false;foreach(string id in TileBlocks(tile)){any=true;if(!DisplayMonitored(id)||!panelReadings.TryGetValue("block:"+id,out result))unknown=true;else if(result==TrackOccupancy.Occupied)return TrackOccupancy.Occupied;else unknown|=result==TrackOccupancy.Unknown;}return !any||unknown?TrackOccupancy.Unknown:TrackOccupancy.Clear;
        }
        PanelVisual Visual(int x,int y) {
            var preview=PreviewTile();var tile=preview!=null&&preview.X==x&&preview.Y==y?preview:panelLookup.Read(panel,x,y);var value=new PanelVisual{Selected=(editingWorkspace||tile?.Kind!=PanelTileKind.Signal)&&x==selectedX&&y==selectedY||editingWorkspace&&region?.Contains(x,y)==true,Caption="",Rail=new Color(.4f,.42f,.4f),Lamp=new Color(.4f,.42f,.4f)};if(DiagramSetupVisible&&deskId==setupDiagramDesk){value.SetupAvailable=DiagramFieldSetup.Eligible(tile);value.SetupActive=value.SetupAvailable&&setupDiagramTarget!=null&&setupDiagramTarget.Desk==deskId&&setupDiagramTarget.X==x&&setupDiagramTarget.Y==y;}
            if(TutorialSelectedSignal(tile))value.SetupActive=true;
            if(tile==null)return value;
            if(GfmInspectionVisible){gfmInspectionPorts.TryGetValue((x,y),out value.InspectionPorts);value.InspectionBoundary=gfmInspectionEnds.Contains((x,y));value.InspectionGap=gfmInspectionGaps.Contains((x,y));}
            value.Caption=tile.Kind==PanelTileKind.TrainNumber?NumberCaption(tile):tile.Label;
            if(tile.Kind==PanelTileKind.LinePermission&&!tile.InlinePermission)return LinePermissionVisual(tile,value);
            if(tile.Kind==PanelTileKind.Label)return value;
            if(NumberMirrors.IsMirror(tile)){var source=NumberMirrorBindings().Source(tile);var read=source==null||!StateManager.IsHost&&Entry.Shared?.LiveFresh!=true?TrackOccupancy.Unknown:PanelOccupancy(source.Tile);value.Unknown=source==null||read==TrackOccupancy.Unknown;value.Occupied=read==TrackOccupancy.Occupied;value.Blink=!value.Occupied&&!value.Unknown&&source!=null&&InRoute(source.Tile,true);value.Rail=value.Occupied?Color.red:value.Unknown?new Color(.65f,.5f,.18f):tile.InlineNumberMirror?(source!=null?RouteRailColor(read,TileRouteKind(source.Tile)):freeRail):new Color(.25f,.7f,.8f);return value;}
            value.Retained=RetainedRouteDisplay.OnTile(tile,desks,VisibleRuns);
            if(tile.Kind==PanelTileKind.AuxiliaryTarget) {
                value.Caption=tile.Target?.Caption??"Hilfsziel";
                if(tile.Target!=null&&panelButtons.End==tile.Target.Id) {
                    if(tile.Target.Train&&tile.Target.Shunt) {
                        value.Selected=false;
                        bool yard=editingWorkspace?editorRouteKind==RouteKind.Shunt:shunting;
                        value.LeftSelected=!yard;value.RightSelected=yard;
                    } else value.Selected=true;
                }
            }
            var occupied=PanelOccupancy(tile);var cue=PanelTrackPolicy.Resolve(occupied,InRoute(tile),InRoute(tile,true));value.Occupied=cue==PanelTrackCue.Occupied;value.Unknown=cue==PanelTrackCue.Unknown;value.Blink=cue==PanelTrackCue.Setting;
            value.Rail=RouteRailColor(occupied,cue==PanelTrackCue.Route?TileRouteKind(tile):PanelRouteKind.None);
            if(tile.Kind==PanelTileKind.Signal) {
                var signal=Object(tile.Reference);bool missing=signal?.IsSignal!=true;value.Distant=signal?.Kind==MarkerRecord.Distant;value.Caption=signal!=null&&(!string.IsNullOrWhiteSpace(signal.DisplayName)||!string.IsNullOrWhiteSpace(signal.MastName)&&signal.MastName!=signal.Name)?signal.DisplayTitle:string.IsNullOrWhiteSpace(tile.Label)?signal?.DisplayTitle??"Signal":tile.Label;
                value.ShuntOnly=Object(tile.Reference)?.Kind==MarkerRecord.Shunt;value.HasShunt=Object(tile.Reference)?.Kind==MarkerRecord.Main&&Object(tile.Reference).AllowSh1;var aspect=Aspect(tile.Reference);value.Lamp=missing?new Color(.9f,.6f,.1f):aspect==LocalSignalAspect.Clear?Color.green:aspect==LocalSignalAspect.Slow?Color.yellow:aspect==LocalSignalAspect.Shunt?Color.white:value.Distant?Color.yellow:Color.red;
                if(aspect==LocalSignalAspect.Invalid){value.Lamp=Color.gray;value.Caption+=" ×";}else if(aspect==LocalSignalAspect.Kennlicht){value.Lamp=Color.white;value.Caption+=" · K";}
                value.Invalid=aspect==LocalSignalAspect.Invalid;value.Identification=aspect==LocalSignalAspect.Kennlicht;
                if(value.Distant)value.AlternateLamp=aspect==LocalSignalAspect.Slow?Color.green:value.Lamp;
                if(tile.AlternateSignal!=null) {
                    value.Split=true;
                    value.SharedCaption=tile.Reference==tile.AlternateSignal;
                    value.AlternateCaption=Object(tile.AlternateSignal)?.DisplayTitle??"Sh";
                    var alternate=Aspect(tile.AlternateSignal);
                    value.AlternateLamp=!ButtonSignal(tile.AlternateSignal)?new Color(.9f,.6f,.1f):
                        alternate==LocalSignalAspect.Shunt?Color.white:Color.red;
                    value.AlternateInvalid=alternate==LocalSignalAspect.Invalid;value.AlternateIdentification=alternate==LocalSignalAspect.Kennlicht;
                    if(value.AlternateInvalid)value.AlternateCaption+=" ×";if(value.AlternateIdentification)value.AlternateCaption+=" · K";
                }
                if(SignalCellLayout.HasAuxiliaryHalf(tile)) {
                    value.Split=true;value.SharedCaption=false;value.AuxiliaryHalf=true;
                    value.AlternateCaption=tile.Target.Caption;
                }
                bool startSelected=panelButtons.Start!=null&&(panelButtons.Start==tile.Reference||panelButtons.Start==tile.AlternateSignal);
                bool endSelected=panelButtons.End!=null&&(panelButtons.End==tile.Reference||panelButtons.End==tile.AlternateSignal);
                if(editingWorkspace||!value.Split)value.Selected|=startSelected||endSelected||
                    (!editingWorkspace&&x==selectedX&&y==selectedY);
                else if(startSelected) {
                    value.LeftSelected=panelButtons.StartHalf(tile,false);
                    value.RightSelected=panelButtons.StartHalf(tile,true);
                } else if(x==selectedX&&y==selectedY) {
                    value.LeftSelected=!dispatcherContextRight;
                    value.RightSelected=dispatcherContextRight;
                }
                if(value.AuxiliaryHalf) {
                    value.Selected=false;
                    value.LeftSelected=startSelected&&!SignalCellLayout.AuxiliaryAt(tile,false)||panelButtons.End==tile.Target.Id&&SignalCellLayout.AuxiliaryAt(tile,false);
                    value.RightSelected=startSelected&&!SignalCellLayout.AuxiliaryAt(tile,true)||panelButtons.End==tile.Target.Id&&SignalCellLayout.AuxiliaryAt(tile,true);
                    if(editingWorkspace&&x==selectedX&&y==selectedY)value.Selected=true;
                }
                value.Blink=!value.Occupied&&VisibleRuns.Any(r=>IsSetting(r)&&(r.Route.Entry==tile.Reference||r.Route.Entry==tile.AlternateSignal));
            } else if(tile.Kind==PanelTileKind.Switch) {
                bool thrown;bool known=world.SwitchPosition(tile.Reference,out thrown);value.Unknown|=!known;value.Thrown=thrown;value.PointKnown=known;
                value.Caption=PointAssignments.Caption(desks,tile.Reference);if(value.Caption=="Weiche"&&!string.IsNullOrWhiteSpace(tile.Label))value.Caption=tile.Label;
                value.Caption+=" "+(known?thrown?"−":"+":"?");
                if(!known&&!value.Occupied)value.Rail=new Color(.9f,.6f,.1f);
                value.Blink=!value.Occupied&&VisibleRuns.Any(r=>IsSetting(r)&&r.Route.Switches.Any(s=>s.Node==tile.Reference));
            }
            ReadSymbolRail(tile,ref value);
            if(tile.Kind==PanelTileKind.LinePermission)return LinePermissionVisual(tile,value);
            return value;
        }
        bool ButtonSignal(string id){var signal=Object(id);return signal!=null&&signal.Kind!=MarkerRecord.Distant&&signal.IsSignal&&OperatingState(id)==SignalOperatingState.Active;}
        void PressCell(int x,int y,bool rightHalf=false) {
            Act(()=>{
                if(editingWorkspace&&axisSelection.Row.HasValue){region=null;editorMode=1;}axisSelection.Clear();selectedX=x;selectedY=y;
                if(!editingWorkspace)dispatcherContextRight=rightHalf;
                var tile=panel?.At(x,y);
                if(PressLineTutorialSignal(x,y,rightHalf))return;
                if(PressDiagramSetupField(x,y))return;
                if(!editingWorkspace&&(tile?.Kind==PanelTileKind.Signal||tile?.Kind==PanelTileKind.LinePermission)&&tile.ReadOnlySignal){status=WindowText("Neighbour signal: display only. Operate it from its owning interlocking.","Nachbarsignal: nur Anzeige. Bedienung am zuständigen Stellwerk.");OpenWorkspaceContext();return;}
                if(editingWorkspace&&panelPage==1) {
                    RequirePanelEdit();if(pendingDelete!=null){status=LocalText.Get("Löschung zuerst bestätigen oder abbrechen.");return;}
                    if(editorMode==3) {
                        if(tool<0){status=WindowText("Choose a field symbol first.","Zuerst ein Feldsymbol wählen.");return;}
                        var growth=EditCanvas(new PanelRegion{X=x,Y=y},(doc,dx,dy)=>PanelEditing.PlaceField(doc,x+dx,y+dy,(PanelTileKind)tool));x+=growth.Left;y+=growth.Top;
                        if((PanelTileKind)tool==PanelTileKind.LinePermission){editorMode=1;tool=-1;OpenLineSetupForField(x,y);return;}
                        status=WindowText("Field added. Click the next cell, or switch to Select.","Feld gesetzt. Nächstes Feld anklicken oder zu Auswählen wechseln.");
                        return;
                    }
                    if(PressRegion(x,y))return;
                    if(EditorClick(x,y))return;
                    if(assigningPoint&&(tile==null||tile.Kind==PanelTileKind.Switch&&string.IsNullOrEmpty(tile.Reference))) {
                        AcceptPoint(x,y);CloseWorkspaceContext();panelView?.Refresh();return;
                    }
                    tileDraft=tile?.Clone();
                    if(tileDraft?.Kind==PanelTileKind.LinePermission){OpenLineSetupForField(x,y);return;}
                    if(HasPendingSignal&&tile==null) {
                        tileDraft=PanelEditing.PreviewField(panel,x,y,PanelTileKind.Signal);
                        tileDraft.Reference=pendingSignalId;
                        var linkedSignal=Object(pendingSignalId);
                        if(linkedSignal?.Kind==MarkerRecord.Main&&linkedSignal.AllowSh1)tileDraft.AlternateSignal=pendingSignalId;
                        LoadTileFields();
                        status=LocalText.Get("Vorgemerktes Signal als Vorschau gesetzt. Feld und Zuordnung speichern.");
                    } else if(tileDraft!=null){LoadTileFields();status=WindowText("Field selected. Right-click for categories.","Feld ausgewählt. Rechtsklick öffnet die Kategorien.");}
                    else status=WindowText("Empty cell. Use Add fields to place a symbol.","Leeres Feld. Wähle Felder setzen, um ein Symbol einzufügen.");
                    if(HasPendingSignal||assigningPoint){editorFieldCategory=EditorFieldCategory.Properties;OpenWorkspaceContext();}
                    else if(tileDraft!=null&&!EditorContextPolicy.FieldCategories(tileDraft).Contains(editorFieldCategory))editorFieldCategory=EditorFieldCategory.Properties;
                } else if(editingWorkspace) {
                    if(panelPage==2&&tile?.Kind==PanelTileKind.AuxiliaryTarget)PressAuxiliary(tile,rightHalf,true);
                    if(panelPage==2&&tile?.Kind==PanelTileKind.Signal&&SignalCellLayout.AuxiliaryAt(tile,rightHalf))PressAuxiliary(tile,rightHalf,true);
                    else if(panelPage==2&&tile?.Kind==PanelTileKind.Signal) {
                        ClearRouteProposals();
                        string picked=SignalCellLayout.SignalAt(tile,rightHalf);
                        bool starting=panelButtons.Start==null||panelButtons.End!=null;
                        bool yard=SignalCellLayout.ShuntHalf(tile,rightHalf)||Object(picked)?.Kind==MarkerRecord.Shunt;
                        if(!starting&&tile.AlternateSignal!=null&&yard!=(editorRouteKind==RouteKind.Shunt))
                            throw new InvalidOperationException("Am Ziel die Hälfte für die gewählte Fahrart anklicken.");
                        if(panelButtons.Press(picked,RouteButton))status=LocalText.Get("Start und Ziel gewählt. Fahrweg speichern oder nach Vorschlägen suchen.");
                        else {
                            if(starting)editorRouteKind=RouteMovement.Default(Object(picked),yard);
                            status=panelButtons.Start==null?"Startwahl aufgehoben.":"Start "+Code(panelButtons.Start)+" gewählt. Jetzt Zieltaste drücken.";
                        }
                    }
                    OpenWorkspaceContext();
                } else if(tile?.Kind==PanelTileKind.LinePermission) {
                    selectedLine=tile.Reference;panelPage=9;OpenWorkspaceContext();
                } else if(!StateManager.IsHost) {
                    panelPage=0;
                    OpenWorkspaceContext();
                    status=tile?.Kind==PanelTileKind.Signal?Code(tile.Reference)+": "+Aspect(tile.Reference):tile?.Section!=null?"Gleis: "+PanelOccupancy(tile):"Anlagenansicht. Bau und Bedienung übernimmt der Host.";
                } else if(tile?.Kind==PanelTileKind.Switch) {
                    panelPage=0;SwitchBound(tile.Reference,null,scope,deskId);CloseWorkspaceContext();
                } else if(tile?.Kind==PanelTileKind.AuxiliaryTarget||SignalCellLayout.AuxiliaryAt(tile,rightHalf)) {
                    panelPage=0;
                    PressAuxiliary(tile,rightHalf,false);
                } else if(tile?.Kind==PanelTileKind.Signal) {
                    panelPage=0;
                    string signal=SignalCellLayout.SignalAt(tile,rightHalf);
                    if(Object(signal)?.Kind==MarkerRecord.Distant) {
                        CloseWorkspaceContext();
                        status=Code(signal)+" ist ein Vorsignal. Für Start und Ziel eine Haupt- oder Rangiertaste wählen.";
                    } else PressDispatcherSignal(signal,SignalCellLayout.ShuntHalf(tile,rightHalf),tile);
                } else {
                    panelPage=0;
                    OpenWorkspaceContext();
                    status=tile==null?"Leeres Feld.":"Feld "+(x+1)+" / "+(y+1)+" gewählt.";
                }
                RefreshWorkspaceFields();panelView?.Refresh();
            });
        }
        void PressDispatcherSignal(string id,bool shuntHalf,PanelTile tile) {
            RequireDispatcher();
            if(!ButtonSignal(id))throw new InvalidOperationException("Signalfeld zuerst im Editor mit einem betriebsbereiten Signal verknüpfen.");
            CloseWorkspaceContext();
            bool split=tile.AlternateSignal!=null;
            bool sameField=split&&(panelButtons.Start==tile.Reference||panelButtons.Start==tile.AlternateSignal);
            if(panelButtons.Start==null||sameField&&panelButtons.StartShunt!=shuntHalf) {
                StartFrom(id,shuntHalf||Object(id).Kind==MarkerRecord.Shunt);
                return;
            }
            if(split&&shunting!=shuntHalf&&panelButtons.Start!=id)
                throw new InvalidOperationException("Am Ziel die Hälfte für die gewählte Fahrart anklicken.");
            RequestSelectedDestination(id);
        }

        void RequestSelectedDestination(string id) {
            panelButtons.ClearEnd();
            if(!panelButtons.Press(id,RouteButton)) {
                status=LocalText.Get("Startwahl aufgehoben.");
                return;
            }
            try {
                QueueDestination(panelButtons.Start,id,shunting?RouteKind.Shunt:reduced?RouteKind.Slow:RouteKind.Train);
                panelButtons.Clear();
            } catch {
                panelButtons.ClearEnd();
                throw;
            }
        }

        void LoadTileFields(){LoadTargetFields(tileDraft);segmentInput=tileDraft.Section?.Segment??"";fromInput=SignUnits.Number(tileDraft.Section?.From??0);toInput=tileDraft.Section==null?"":SignUnits.Number(tileDraft.Section.To);}
        void SaveTile() {
            RequirePanelEdit();var tile=tileDraft?.Clone()??throw new InvalidOperationException("Zuerst ein Rasterfeld wählen.");
            var previousPoint=panel.At(tile.X,tile.Y);if(tile.Kind==PanelTileKind.Switch&&previousPoint!=null&&(tile.Stem!=previousPoint.Stem||tile.Normal!=previousPoint.Normal)){RequireIdleAdministration();tile.CircuitRails=null;}
            if(tile.Kind==PanelTileKind.LinePermission){SaveCanvasTile(tile);tileDraft=tile.Clone();RefreshWorkspaceFields();return;}
            if(NumberMirrors.IsMirror(tile)){NumberMirrors.Validate(tile);SaveCanvasTile(tile);tileDraft=tile.Clone();status=LocalText.Get("Anzeigekopie gespeichert. Zugnummer und Quelle werden zentral gelesen.");RefreshWorkspaceFields();return;}
            if(SignalCellLayout.HasAuxiliaryHalf(tile)) {
                if(Object(tile.Reference)?.Kind!=MarkerRecord.Shunt&&!(Object(tile.Reference)?.LowMain==true&&Object(tile.Reference).AllowSh1))throw new InvalidOperationException("Eine eingebaute Rangier-Hilfszieltaste benötigt ein Rangiersignal oder niedriges Hauptsignal mit Sh 1.");
                SaveAuxiliaryTarget(tile);
            }
            if(tile.Kind==PanelTileKind.AuxiliaryTarget)SaveAuxiliaryTarget(tile);
            else if(tile.Kind==PanelTileKind.Signal||tile.Kind==PanelTileKind.Switch||tile.Kind==PanelTileKind.Label)tile.Section=null;
            else if(string.IsNullOrWhiteSpace(segmentInput))tile.Section=null;
            else {
                double from,to;if(!double.TryParse(fromInput.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out from)||!double.TryParse(toInput.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out to))throw new InvalidOperationException("Abschnittsgrenzen als Meter ab Ende A eingeben.");
                var segment=Graph.Shared?.GetSegment(segmentInput.Trim());if(segment==null||to>segment.GetLength()+.001)throw new InvalidOperationException("Gleis-ID oder Abschnittsende ungültig.");
                tile.Section=new TrackSection{Segment=segment.id,From=from,To=to};tile.Section.Validate();
            }
            if(tile.Kind==PanelTileKind.Signal&&!string.IsNullOrWhiteSpace(tile.Reference)&&Object(tile.Reference)?.IsSignal!=true)
                throw new InvalidOperationException("Signal-ID nicht verfügbar.");
            if(tile.Kind==PanelTileKind.Signal&&!tile.ReadOnlySignal&&CurrentDesk?.IsMaster!=true&&StationAutomationPolicy.Owner(desks,tile.Reference) is DeskDefinition owner&&owner.Id!=deskId)
                throw new InvalidOperationException(WindowText("This signal belongs to another interlocking. Use a display-only neighbour signal.","Dieses Signal gehört einem anderen Stellwerk. Als Nachbarsignal nur anzeigen."));
            if(tile.Kind==PanelTileKind.TrainNumber&&(tile.Reference!=""&&Object(tile.Reference)?.Kind!=MarkerRecord.Main||
                tile.AlternateSignal!=null&&(Object(tile.AlternateSignal)?.Kind!=MarkerRecord.Main||
                    tile.Reference==""||tile.Reference==tile.AlternateSignal)))
                throw new InvalidOperationException("Für die Zugnummer zwei unterschiedliche Hauptsignale wählen oder die Gegenrichtung leer lassen.");
            if(tile.Kind==PanelTileKind.TrainNumber&&tile.AlternateSignal!=null&&
                tile.AlternateSignal!=panel.At(tile.X,tile.Y)?.AlternateSignal&&Entry.Shared?.FieldNumber(tile.AlternateSignal)!="")
                throw new InvalidOperationException("Das Gegensignal hat bereits eine eigene Zugnummer. Diese vor der Zusammenlegung im Fahrdienstleiter leeren.");
            if(tile.Kind==PanelTileKind.Signal&&tile.AlternateSignal!=null&&
                (Object(tile.Reference)?.Kind!=MarkerRecord.Main||!SignalCatalogue.Supports(Object(tile.AlternateSignal),true,false)))
                throw new InvalidOperationException("Ein Hauptsignal und als Rangiertaste ein Rangiersignal oder einen Hauptsignal-Kopf mit Sh 1 wählen.");
            if(tile.Kind==PanelTileKind.Switch&&!string.IsNullOrWhiteSpace(tile.Reference)){bool thrown;if(!world.SwitchPosition(tile.Reference,out thrown))throw new InvalidOperationException("Weichen-ID nicht verfügbar.");}
            SaveCanvasTile(tile);
            if(tile.Kind==PanelTileKind.Signal&&tile.Reference==pendingSignalId)pendingSignalId=null;
            tileDraft=tile.Clone();
            status=LocalText.Get("Gleisbildfeld mit Verknüpfung gespeichert.");
            RefreshWorkspaceFields();
        }
        void SwitchSelected(bool thrown) {
            RequireDispatcher();var tile=panel?.At(selectedX,selectedY);if(tile?.Kind!=PanelTileKind.Switch)throw new InvalidOperationException("Weichentaste wählen.");
            SwitchBound(tile.Reference,thrown,scope,deskId);
        }
        void ResetWorkspacePage(int page){axisSelection.Clear();ClearAutomaticRouteChoices();panelPage=page;tool=-1;regionAction=0;ResetEditor();if(page!=1)region=null;panelButtons.Clear();tileDraft=null;}
        void Mode(int page){ResetWorkspacePage(page);Rebuild();}
        string SelectedFieldStatus() {
            var tile=panel?.At(selectedX,selectedY);if(tile==null)return "Ein Feld auf der Tafel anklicken.";
            if(tile.Kind==PanelTileKind.Signal)return string.IsNullOrWhiteSpace(tile.Reference)?"Signal nicht verknüpft. Im Editor Signalkennung wählen und Feld speichern.":ButtonSignal(tile.Reference)?"Signal "+Code(tile.Reference)+" ist verknüpft.":"Verknüpftes Signal fehlt oder seine Richtung ist deaktiviert.";
            if(tile.Kind==PanelTileKind.Switch){bool thrown;return string.IsNullOrWhiteSpace(tile.Reference)?"Weiche nicht verknüpft. Im Editor native Weichenkennung eintragen und speichern.":world.SwitchPosition(tile.Reference,out thrown)?"Weiche "+PointAssignments.Caption(desks,tile.Reference)+" · "+(thrown?"Abzweig":"Grundstellung"):"Weichenkennung nicht im aktuellen Gleisnetz gefunden.";}
            if(tile.Kind==PanelTileKind.Label)return "Beschriftungsfeld ohne Fahrwegfunktion.";
            if(tile.Section==null)return "Nur gezeichnet. Für eine Belegungsanzeige im Editor Gleis-ID und Meterabschnitt verknüpfen.";
            return "Abschnitt "+tile.Section.Key+" · "+(PanelOccupancy(tile)==TrackOccupancy.Clear?"frei":PanelOccupancy(tile)==TrackOccupancy.Occupied?"belegt":"Belegung derzeit nicht lesbar; keine Freigabe daraus ableiten.");
        }
        void BuildActive(UIPanelBuilder c) {
            c.AddField(LocalText.Get("Festlegemelder"),()=>ActiveHere().Length==0?"Keine Festlegung":string.Join("\n",ActiveHere().Select(RouteStatus)),UIPanelBuilder.Frequency.Periodic);
            var active=ActiveHere();if(active.Length==0)return;
            if(!active.Any(r=>r.Route.Id==routeId))routeId=active[0].Route.Id;
            c.AddSearchChoice(LocalText.Get("Auflösen"),active.Select(r=>r.Route.Code).ToList(),Array.FindIndex(active,r=>r.Route.Id==routeId),i=>{routeId=active[i].Route.Id;RefreshWorkspaceFields();});
            c.AddButton(LocalText.Get("Halt und sicher auflösen"),()=>Act(()=>{CancelRoute();RefreshWorkspaceFields();}));
            BuildRelease(c,active.First(r=>r.Route.Id==routeId));
        }
        void BuildRoutes(UIPanelBuilder c) {
            if(OnDemandRoutes){c.AddLabel(LocalText.Select("Routes are calculated when you select start and destination in Dispatcher. Existing saved definitions are retained but are not used in this mode.","Fahrwege werden bei Start-/Zielwahl im Fahrdienstleiter berechnet. Gespeicherte Definitionen bleiben erhalten, werden in diesem Modus aber nicht verwendet."));}
            c.AddSection(LocalText.Get("Fahrweg einrichten"));c.AddField(LocalText.Get("Start → Ziel"),()=>panelButtons.Start==null?"Im Gleisbild Start und Ziel anklicken":SignalCaption(panelButtons.Start)+" → "+(panelButtons.End==null?"Ziel wählen":SignalCaption(panelButtons.End)),UIPanelBuilder.Frequency.Periodic);
            BuildRouteDiscovery(c);
            if(panelButtons.Start!=null&&panelButtons.End!=null)c.AddButton(LocalText.Get("Gewählten Fahrweg speichern"),()=>Act(CreateRoute));
            if(routes.Count>0) {
                var choices=routes.Where(RouteHere).ToArray();if(choices.Length==0){c.AddLabel(LocalText.Get("Keine gespeicherte Fahrstraße mit Startsignal auf dieser Tafel."));return;}if(!choices.Any(r=>r.Id==routeId))routeId=choices[0].Id;
                c.AddSection(LocalText.Get("Gespeicherte Fahrstraßen"));c.AddSearchChoice(LocalText.Get("Fahrstraße"),choices.Select(r=>r.Code+" · "+SignalCaption(r.Entry)+" → "+SignalCaption(r.Exit)).ToList(),Array.FindIndex(choices,r=>r.Id==routeId),i=>{routeId=choices[i].Id;RefreshWorkspaceFields();});
                var selected=routes.FirstOrDefault(r=>r.Id==routeId);if(selected!=null){c.AddField(LocalText.Get("Flankenschutz"),FlankCaption(selected));c.AddLabel(LocalText.Get("Weichen: ")+(selected.Switches.Count==0?"keine":string.Join(", ",selected.Switches.Select(s=>PointAssignments.Caption(desks,s.Node)+(s.Thrown?" −":" +")))));c.AddLabel(selected.Path==null?"Alter Fahrweg: vor Bedienung einmal aktualisieren.":"Mitlaufende Rangiersignale: "+(selected.ViaSignals.Count==0?"keine":string.Join(", ",selected.ViaSignals.Select(s=>Code(s.Id)))));}
                BuildRouteWait(c,selected);
                c.AddButton(LocalText.Get("Fahrweg neu berechnen"),()=>Act(RefreshRoute));
                c.AddButton(LocalText.Get("Definition löschen"),()=>Act(DeleteRoute));
            }
        }
        void BuildIdentifiers(UIPanelBuilder c) {
            c.AddSection(LocalText.Get("Zuordnung aus der Spielwelt"));c.AddLabel(LocalText.Get("Im Spiel nahe einer Weiche rechtsklicken → Weiche zuordnen. Zielstellwerk wählen, dann dort das Zielfeld rechtsklicken → Weichenzuordnung übernehmen."));
            c.AddField(LocalText.Get("Angeklicktes Gleis"),trackPicked??"Noch keines");
            if(!string.IsNullOrEmpty(switchPicked)){string picked=switchPicked;c.AddField(LocalText.Get("Weichenkennung"),()=>PointAssignments.Caption(desks,picked),UIPanelBuilder.Frequency.Periodic);c.AddField(LocalText.Get("Aktuelle Lage"),()=>PointPosition(picked),UIPanelBuilder.Frequency.Periodic);c.AddField(LocalText.Get("Native ID"),picked);}
            BuildPointAssignment(c);
        }
        void RefreshRoute() {
            RequirePanelEdit();var old=routes.FirstOrDefault(r=>r.Id==routeId)??throw new InvalidOperationException("Fahrstraße wählen.");if(VisibleRuns.Any(r=>r.Route.Id==old.Id))throw new InvalidOperationException("Aktive Fahrstraße zuerst sicher auflösen.");
            var next=world.Design(Object(old.Entry),RouteEndpoint(old.Exit),old.Code,old.Reduced,old.Shunting);next.Id=old.Id;next.ProtectionWaitSeconds=old.ProtectionWaitSeconds;var updated=routes.Select(r=>r.Id==old.Id?next:r).ToList();store.Save(updated);routes=updated;status=LocalText.Get("Fahrweg, Rangiersignale und Freimeldebereiche aktualisiert. Keine Fahrtfreigabe geladen.");Rebuild();
        }
        public void InspectTrack(Location at) {
            if(!WorkspaceEditable||!at.IsValid)return;trackPicked=at.segment.id;RememberTargetClick(at);
            var nodes=new[]{at.segment.a,at.segment.b}.Where(n=>n!=null&&Graph.Shared.IsSwitch(n)).ToArray();if(nodes.Length>0)switchPicked=nodes.OrderBy(n=>at.segment.DistanceBetween(n,at)).First().id;
            ShowWorkspace(true,3,true);status=LocalText.Get("Gleis-ID ")+trackPicked+(nodes.Length>0?" · nahe Weiche "+switchPicked: " · keine Weiche an den Segmentenden");
        }
        void RememberPanelScroll(){if(panelScroll!=null)panelScrollPosition=panelScroll.normalizedPosition;}
    }
}
