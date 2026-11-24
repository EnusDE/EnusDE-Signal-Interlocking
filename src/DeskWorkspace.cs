using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Game.State;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly List<(Image Image,Func<bool> Selected)> workspaceTools=new List<(Image,Func<bool>)>();
        WorkspaceSurface workspace;
        bool workspaceContext,editingWorkspace;
        bool workspaceFit=true,editorViewSaved,dispatcherViewSaved;
        string workspaceActionError;
        float editorZoom=1,dispatcherZoom=1;
        Vector2 editorScroll=new Vector2(0,1),dispatcherScroll=new Vector2(0,1);

        internal bool WorkspaceAvailable=>!stopped&&!StateManager.IsUnloading&&panel!=null&&Entry.Shared?.ConstructionUnlocked==true;
        internal bool WorkspaceEditable=>WorkspaceAvailable&&StateManager.IsHost&&!loadFailed&&store!=null&&panelStore!=null&&panelError==null;
        string ActiveWindowId=>editingWorkspace?EditorWindowId:WindowId;
        string WindowText(string english,string german)=>LocalText.Select(english,german);

        internal void OpenMode(bool editing) {
            if(!WorkspaceAvailable||editing&&!WorkspaceEditable)return;
            ShowWorkspace(editing,editing?4:0,editing);
        }

        void ShowWorkspace(bool editing,int page,bool showTools) {
            if(!WorkspaceAvailable||editing&&!WorkspaceEditable)return;
            if(objects.IsPlacing)objects.CloseForWorldSelection();
            EnsureWindow();
            if(manager==null)return;
            if(!editing&&CurrentDesk!=null&&!CurrentDesk.Global&&!CurrentDesk.ShowInDispatcher)SelectDesk(desks.Desks.Single(d=>d.Global).Id,false);
            if(editingWorkspace!=editing) {
                CloseWorkspaceMode();
                editingWorkspace=editing;
                panelZoom=editing?editorZoom:dispatcherZoom;
                panelScrollPosition=editing?editorScroll:dispatcherScroll;
                workspaceFit=editing?!editorViewSaved:!dispatcherViewSaved;
                selectedX=selectedY=-1;
                region=null;
                workspaceActionError=null;
            }
            panel=DeskPanel(CurrentDesk);panelReadings.Clear();panelReadTime=-100;nextPanelRead=0;
            workspaceContext=showTools;
            if(editing) {
                editorMode=1;
                editorCatalogueForm=null;
                pendingDeskRemoval=null;
                editorLinksAfterSelection=page==3;
                page=page==7?7:4;editorDetailsOpen=showTools&&page==7;
            }
            ResetWorkspacePage(page);
            var window=CustomWindowHandle.Create(ActiveWindowId);
            bool cached=manager.GetCustomWindow(ActiveWindowId)!=null;
            window?.Show();
            if(cached)window?.SetNeedsRebuild();
            if(editing&&editorDetailsOpen)OpenEditorToolsWindow();
        }

        internal void CloseWorkspaceMode() {
            CloseRouteChoiceWindow();
            masterLayoutPopup?.Dismiss();masterLayoutPopup=null;
            if(manager?.GetCustomWindow(MasterLayoutWindowId)!=null)manager.CloseWindow(MasterLayoutWindowId);
            if(manager?.GetCustomWindow(EditorToolsWindowId)!=null)manager.CloseWindow(EditorToolsWindowId);
            if(panelScroll!=null) {
                RememberPanelScroll();
                if(editingWorkspace)SaveEditorView();
                else {dispatcherZoom=panelZoom;dispatcherScroll=panelScrollPosition;dispatcherViewSaved=true;}
            }
            if(manager!=null&&CustomWindowHandle.Create(ActiveWindowId)?.IsOpen()==true)manager.CloseWindow(ActiveWindowId);
            ReleaseWorkspaceCanvas();
        }

        void ReleaseWorkspaceCanvas() {
            DismissEditorMenu();
            if(catalogueInspector!=null&&editorSurface!=null)catalogueInspector.OnRebuild-=editorSurface.ThemeSoon;
            catalogueInspector=null;
            if(inspector!=null&&workspace!=null)inspector.OnRebuild-=workspace.ThemeSoon;
            if(inspector!=null&&editorSurface!=null)inspector.OnRebuild-=editorSurface.ThemeSoon;
            if(workspace!=null){workspace.gameObject.SetActive(false);Destroy(workspace.gameObject);}
            if(editorSurface!=null){editorSurface.gameObject.SetActive(false);Destroy(editorSurface.gameObject);}
            editorSurface=null;
            workspace=null;panelView=null;panelScroll=null;inspector=null;workspaceTools.Clear();
        }

        void RefreshWorkspaceFields() {
            SizeDispatcherPopup();
            RefreshWorkspaceTools();
            inspector?.SetNeedsRebuild();
            catalogueInspector?.SetNeedsRebuild();
            workspace?.ThemeSoon();
            editorSurface?.ThemeSoon();
        }

        void RefreshWorkspaceTools() {
            foreach(var tool in workspaceTools)if(tool.Image!=null)
                tool.Image.color=tool.Selected?.Invoke()==true?new Color(.12f,.38f,.48f):WorkspaceSurface.Panel;
        }

        void SizeDispatcherPopup() {
            if(workspace==null||!workspaceContext)return;
            var tile=panelLookup.Read(panel,selectedX,selectedY);
            workspace.SetCompactPopup(panelPage==0&&tile?.Kind==PanelTileKind.Signal&&!SignalCellLayout.HasAuxiliaryHalf(tile)&&
                !SignalHasRouteControls(tile.Reference)&&!SignalHasRouteControls(tile.AlternateSignal)&&
                (panelButtons.Start==null||tile.LinkedAuxiliary==null));
        }

        void OpenWorkspaceContext(){workspaceContext=true;workspace?.ShowContext(true);if(editingWorkspace){editorDetailsOpen=true;OpenEditorToolsWindow();}RefreshWorkspaceFields();}
        void WorkspacePage(int page){workspaceContext=true;if(editingWorkspace)editorDetailsOpen=true;Mode(page);if(editingWorkspace)OpenEditorToolsWindow();}
        void CloseWorkspaceContext(){workspaceContext=false;workspace?.ShowContext(false);if(editingWorkspace){editorDetailsOpen=false;if(manager?.GetCustomWindow(EditorToolsWindowId)!=null)manager.CloseWindow(EditorToolsWindowId);}}

        void BuildDispatcherWindow(UIPanelBuilder builder) {
            if(editingWorkspace)return;
            CreateWorkspaceSurface(builder);
            AddWorkspaceTool("STW",WindowText("Choose interlocking","Stellwerk wählen"),()=>WorkspacePage(4),()=>panelPage==4);
            AddWorkspaceTool("DISPATCH",WindowText("Operate from the track diagram","Gleisbild bedienen"),()=>{CloseWorkspaceContext();Mode(0);},()=>panelPage==0);
            if(!OnDemandRoutes)AddWorkspaceTool("FS",WindowText("Operate stored routes","Gespeicherte Fahrstraßen bedienen"),()=>WorkspacePage(2),()=>panelPage==2);
            AddWorkspaceTool("LOCK",WindowText("All routes and retained locks","Festlegungen – alle Stellwerke"),()=>WorkspacePage(6),()=>panelPage==6);
            AddWorkspaceTool("REQUESTS",WindowText("Automatic requests","Automatische Anfragen"),()=>WorkspacePage(7),()=>panelPage==7);
            AddWorkspaceTool("MODULES",WindowText("Master modules and central takeover","ESTW-Module und zentrale Übernahme"),()=>WorkspacePage(8),()=>panelPage==8);
            AddWorkspaceTool("↔",WindowText("Open line / GWB permissions","Freie Strecke / GWB-Erlaubnisse"),OpenFreeLines,()=>panelPage==9);
            AddWorkspaceTool("?",WindowText("Handbook","Handbuch"),()=>WorkspacePage(5),()=>panelPage==5);
            if(StateManager.IsHost) {
                var stop=workspace.Button(workspace.Root,WindowText("STOP","HALT"),()=>Act(()=>{RequireDispatcher();PauseWaypointRequests();CancelRouteWork();foreach(var pending in automaticRequests.Where(r=>r.Stage==AutomaticRequestStage.Waiting||r.Stage==AutomaticRequestStage.Submitted).ToArray())WithdrawAutomaticRequest(pending,"Alle Signale Halt angefordert.");engine.HaltAll();panelButtons.Clear();status=LocalText.Get("Alle Signale Halt. Erforderliche Verschlüsse bleiben erhalten.");}),WindowText("Set all own signals to stop","Alle eigenen Signale auf Halt"));
                var rect=stop.GetComponent<RectTransform>();
                rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
                rect.anchoredPosition=new Vector2(8,8);rect.sizeDelta=new Vector2(46,38);
                (stop.targetGraphic as Image).color=new Color(.48f,.09f,.08f);
            }
            CompleteWorkspace(builder,BuildDispatcherFields,WorkspacePaneTitle);
        }

        void CreateWorkspaceSurface(UIPanelBuilder builder) {
            var placeholder=builder.VStack(_=>{});
            var nativeWindow=placeholder.GetComponentInParent<Window>();
            if(nativeWindow==null)throw new InvalidOperationException("Interlocking window anchor is missing.");
            ReleaseWorkspaceCanvas();
            var root=WorkspaceSurface.Rect(nativeWindow.transform,"EnusDE Dispatcher");
            WorkspaceSurface.Fill(root,8,34,8,18);
            root.SetAsLastSibling();
            workspace=root.gameObject.AddComponent<WorkspaceSurface>();
            workspace.Initialize(root);
            workspace.ReadTitle=()=>CurrentDeskLabel+" · "+Math.Round(panelZoom*100)+" %";
            workspace.ReadStatus=()=>panelError??workspaceActionError??status;
            workspace.ReadSelection=()=>DispatcherReleaseFeedback()??(panelButtons.Start==null?SelectedFieldStatus():SignalCaption(panelButtons.Start)+(panelButtons.End==null?" → "+WindowText("Choose destination","Ziel wählen"):" → "+SignalCaption(panelButtons.End)));
            workspace.Closed=()=>{workspaceContext=false;};

        }

        void AddWorkspaceTool(string glyph,string hint,Action action,Func<bool> selected) {
            var owner=workspace;
            var button=owner.Button(owner.Root,glyph,()=>{if(workspace==owner&&PanelVisible)action();},hint);
            WorkspaceSurface.Place(button.GetComponent<RectTransform>(),new WorkspaceBox(8,8+workspaceTools.Count*46,44,40));
            button.GetComponentInChildren<TMPro.TMP_Text>().gameObject.SetActive(false);
            WorkspaceIcon.Attach(button.transform,glyph,25,0);
            workspaceTools.Add(((Image)button.targetGraphic,selected));
        }

        string WorkspacePaneTitle()=>panelPage switch {
            2=>WindowText("Routes","Fahrstraßen"),
            4=>WindowText("Interlockings","Stellwerke"),
            5=>WindowText("Handbook","Handbuch"),
            6=>WindowText("All routes and retained locks","Festlegungen – alle Stellwerke"),
            7=>WindowText("Automatic requests","Automatische Anfragen"),
            8=>WindowText("Master modules and central takeover","ESTW-Module und zentrale Übernahme"),
            9=>WindowText("Open line / GWB permissions","Freie Strecke / GWB-Erlaubnisse"),
            _=>WindowText("Selection","Auswahl")
        };

        void CompleteWorkspace(UIPanelBuilder builder,Action<UIPanelBuilder> fields,Func<string> paneTitle) {
            var zoom=WorkspaceSurface.Rect(workspace.Header,"Zoom");
            zoom.anchorMin=zoom.anchorMax=zoom.pivot=new Vector2(1,.5f);
            zoom.sizeDelta=new Vector2(298,32);
            string[] labels={"−","+","100 %",WindowText("Fit","Einpassen"),WindowText("Selection","Auswahl")};
            float[] widths={30,30,62,80,76};
            Action[] actions={ ()=>ChangeZoom(panelZoom/1.25f),()=>ChangeZoom(panelZoom*1.25f),()=>ChangeZoom(1),FitWorkspace,OpenWorkspaceContext };
            float left=0;
            var owner=workspace;
            for(int i=0;i<labels.Length;i++) {
                var action=actions[i];
                var button=owner.Button(zoom,labels[i],()=>{if(workspace==owner&&PanelVisible)action();});
                WorkspaceSurface.Place(button.GetComponent<RectTransform>(),new WorkspaceBox(left,0,widths[i],32));
                left+=widths[i]+3;
            }
            if(panel!=null)BuildWorkspaceDiagram(workspace.ViewFrame,()=>workspace==owner);
            var popup=builder.VScrollView(c=>inspector=c.AddRebuildableSection(fields));
            popup.SetParent(workspace.PopupBody,false);
            WorkspaceSurface.Fill(popup);
            (popup.GetComponent<LayoutElement>()??popup.gameObject.AddComponent<LayoutElement>()).ignoreLayout=true;
            inspector.OnRebuild+=workspace.ThemeSoon;
            workspace.ReadPopupTitle=paneTitle;
            SizeDispatcherPopup();
            workspace.ShowContext(workspaceContext);
            RefreshWorkspaceTools();
        }

        void FitWorkspace() {
            var viewport=panelScroll?.viewport;
            if(panel!=null&&viewport!=null&&viewport.rect.width>16&&viewport.rect.height>16)
                ChangeZoom(Mathf.Min((viewport.rect.width-16)/(panel.Columns*PanelGeometry.CellWidth),(viewport.rect.height-16)/(panel.Rows*PanelGeometry.CellHeight)));
        }

        void BuildWorkspaceDiagram(RectTransform frame,Func<bool> isCurrent,bool editor=false) {
            var viewport=WorkspaceSurface.Rect(frame,"Viewport");
            WorkspaceSurface.Fill(viewport,editor?42:0,editor?24:0,12,12);
            viewport.gameObject.AddComponent<RectMask2D>();
            panelScroll=frame.gameObject.AddComponent<ScrollRect>();
            panelScroll.viewport=viewport;
            panelScroll.horizontal=panelScroll.vertical=true;
            panelScroll.movementType=ScrollRect.MovementType.Clamped;
            panelScroll.inertia=false;
            panelScroll.scrollSensitivity=0;
            var content=editor?WorkspaceSurface.Rect(viewport,"Editor working area"):null;
            if(content!=null){content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,1);content.anchoredPosition=Vector2.zero;}
            var drawing=WorkspaceSurface.Rect(content!=null?content:viewport,"Track diagram");
            panelView=drawing.gameObject.AddComponent<PanelView>();
            panelView.Initialize(panel,Visual,PressCell,()=>!stopped&&isCurrent()&&PanelVisible,CellContext,ChangeZoom,PreviewTile,SelectedRegion,editor);
            if(editor){var surface=content.gameObject.AddComponent<EditorCanvasSurface>();surface.Initialize(panelView);panelView.ConfigureCanvas(surface);}
            if(editor)panelView.ConfigureMove(()=>editingWorkspace&&panelPage==1&&(editorMode==1||editorMode==2)&&regionAction==0&&pendingDelete==null&&!assigningPoint&&!HasPendingSignal,
                pending=>Act(()=>{
                    RequirePanelEdit();if(pending.X==pending.Source.X&&pending.Y==pending.Source.Y)return;
                    var growth=pending.Growth(panel);EditPanel(doc=>pending.Apply(doc));region=new PanelRegion{X=pending.X+growth.Left,Y=pending.Y+growth.Top,Width=pending.Source.Width,Height=pending.Source.Height};
                    axisSelection.Clear();tileDraft=null;status=WindowText("Moved with all bindings. Undo is available.","Mit allen Zuordnungen verschoben. Rückgängig ist verfügbar.");panelView?.Refresh();RefreshWorkspaceFields();
                }));
            panelScroll.content=content!=null?content:drawing;
            panelView.SetZoom(panelZoom);
            panelView.AttachScroll(panelScroll);
            panelScroll.horizontalScrollbar=WorkspaceBar(frame,false);
            panelScroll.verticalScrollbar=WorkspaceBar(frame,true);
            if(editor) {
                var columns=WorkspaceSurface.Rect(frame,"Column headers");columns.anchorMin=new Vector2(0,1);columns.anchorMax=new Vector2(1,1);columns.pivot=new Vector2(0,1);columns.offsetMin=new Vector2(42,-24);columns.offsetMax=new Vector2(-12,0);columns.gameObject.AddComponent<RectMask2D>();
                var rows=WorkspaceSurface.Rect(frame,"Row headers");rows.anchorMin=new Vector2(0,0);rows.anchorMax=new Vector2(0,1);rows.pivot=new Vector2(0,1);rows.offsetMin=new Vector2(0,12);rows.offsetMax=new Vector2(42,-24);rows.gameObject.AddComponent<RectMask2D>();
                var grid=panelView;bool Alive()=>editingWorkspace&&PanelVisible&&isCurrent()&&panelView==grid;
                columns.gameObject.AddComponent<EditorGridHeader>().Initialize(grid,false,SelectEditorAxis,SelectedRegion,()=>axisSelection.Row,Alive);
                rows.gameObject.AddComponent<EditorGridHeader>().Initialize(grid,true,SelectEditorAxis,SelectedRegion,()=>axisSelection.Row,Alive);
                var all=EditorSurface.Button(frame,WindowText("All","Alle"),()=>{if(Alive())Act(SelectAll);});WorkspaceSurface.Place(all.GetComponent<RectTransform>(),new WorkspaceBox(0,0,42,24));
            }
            StartCoroutine(RestoreWorkspaceView(panelScroll,isCurrent,editor));
        }

        IEnumerator RestoreWorkspaceView(ScrollRect target,Func<bool> isCurrent,bool editor) {
            yield return null;
            yield return null;
            if(target==null||!isCurrent()||target!=panelScroll)yield break;
            Canvas.ForceUpdateCanvases();
            if(workspaceFit&&target.viewport.rect.width>16&&target.viewport.rect.height>16){workspaceFit=false;if(editor)FitEditor();else FitWorkspace();}
            else target.normalizedPosition=panelScrollPosition;
        }

        internal static Scrollbar WorkspaceBar(RectTransform parent,bool vertical) {
            var rect=WorkspaceSurface.Rect(parent,vertical?"Vertical scroll":"Horizontal scroll",new Color(.035f,.055f,.07f));
            var bar=rect.gameObject.AddComponent<Scrollbar>();
            bar.direction=vertical?Scrollbar.Direction.BottomToTop:Scrollbar.Direction.LeftToRight;
            var track=WorkspaceSurface.Rect(rect,"Sliding area");
            WorkspaceSurface.Fill(track,1,1,1,1);
            var handle=WorkspaceSurface.Rect(track,"Handle",new Color(.25f,.37f,.43f));
            WorkspaceSurface.Fill(handle);
            bar.handleRect=handle;
            bar.targetGraphic=handle.GetComponent<Image>();
            if(vertical){rect.anchorMin=new Vector2(1,0);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(1,.5f);rect.offsetMin=new Vector2(-10,12);rect.offsetMax=Vector2.zero;}
            else{rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);rect.offsetMin=Vector2.zero;rect.offsetMax=new Vector2(-12,10);}
            return bar;
        }
    }
}

