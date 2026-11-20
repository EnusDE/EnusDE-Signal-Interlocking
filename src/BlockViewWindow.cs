using System;
using System.Linq;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const string BlockViewWindowId="enusde.signaltechnik.block-view";
        CustomWindowDefinition blockViewDefinition;
        RectTransform blockViewRoot;
        DeskLibrary blockViewBase,blockViewWorking,blockViewPreview;
        LineBlockBuild blockViewBuild;
        BlockViewOptions blockViewOptions;
        string blockViewLine,blockViewDesk,blockViewScope;
        long blockViewConfig;string blockViewInstall,blockViewMessage;int blockViewGeneration,blockViewRender;
        string BlockViewIssue() {
            if(Entry.Shared==null||!Entry.Shared.ConfigurationReady)return WindowText("Installation data is not ready. Wait and try again.","Anlagendaten sind noch nicht bereit. Kurz warten und erneut versuchen.");
            if(scope!=blockViewScope)return WindowText("The world changed. Close and reopen the view editor.","Die Welt wurde gewechselt. Ansichtseditor schließen und erneut öffnen.");
            if(Entry.Shared.ConfigurationVersion!=blockViewConfig)return WindowText("The saved configuration changed. Close and reopen the view editor before applying.","Die gespeicherte Konfiguration wurde geändert. Ansichtseditor vor dem Übernehmen schließen und erneut öffnen.");
            if(Entry.Shared.ConstructionSignature!=blockViewInstall)return WindowText("The construction changed. Recreate the block proposal or reopen the view editor.","Die Anlage wurde baulich geändert. Blockvorschlag neu erstellen oder Ansichtseditor erneut öffnen.");
            if(blockViewBuild!=null&&!ReferenceEquals(blockViewBuild,blockBuild))return WindowText("The block proposal changed. Reopen the view editor from the current proposal.","Der Blockvorschlag wurde geändert. Ansichtseditor vom aktuellen Vorschlag erneut öffnen.");
            return null;
        }
        bool BlockViewFresh()=>BlockViewIssue()==null;
        void BlockViewAction(Action action)=>Act(()=>{
            try{RequirePanelEdit();string issue=BlockViewIssue();if(issue!=null)throw new InvalidOperationException(issue);action();}
            catch(Exception error){blockViewMessage=error.Message;throw;}
        });
        void OpenBlockViewEditor(string line,string desk,LineBlockBuild build=null) {
            RequirePanelEdit();EnsureWindow();blockViewBuild=build;blockViewBase=(build?.Library??Entry.Shared.Desks()??desks).Clone();blockViewLine=line;blockViewDesk=desk;blockViewScope=scope;
            blockViewConfig=Entry.Shared.ConfigurationVersion;blockViewInstall=Entry.Shared.ConstructionSignature;blockViewMessage=WindowText("Choose settings, then prepare the preview.","Einstellungen wählen und anschließend die Vorschau vorbereiten.");
            LoadBlockViewOptions();
            if(blockViewDefinition==null) {
                blockViewDefinition=new CustomWindowDefinition{Identifier=BlockViewWindowId,Title=WindowText("Edit block assembly view","Ansicht des Blockverbunds bearbeiten"),BuilderAction=BuildBlockViewEditor,
                    Config=WindowConfig.Resizable(new Vector2Int(1050,850),new Vector2Int(640,480),new Vector2Int(3840,2160),Window.Position.CenterRight)};
                manager.RegisterCustomWindow(blockViewDefinition);
            }
            CustomWindowHandle.Create(BlockViewWindowId)?.Show();
        }
        void LoadBlockViewOptions() {
            var line=blockViewBase.FreeLines.Single(l=>l.Id==blockViewLine);var area=BlockDiagram.Area(blockViewBase.Desks.Single(d=>d.Id==blockViewDesk).Panel,line.Id)??throw new InvalidOperationException(WindowText("Select an existing block assembly.","Vorhandenen Blockverbund auswählen."));
            blockViewOptions=BlockDiagramViews.Effective(area.BlockView,line.BlockSections.Count,blockViewDesk==line.BlockOwner);blockViewWorking=blockViewBase.Clone();blockViewPreview=null;blockViewGeneration++;RefreshBlockViewPreview();
        }
        bool RefreshBlockViewPreview() {
            try {
                var next=BlockDiagramViews.Edit(blockViewWorking??blockViewBase,blockViewLine,blockViewDesk,blockViewOptions);
                blockViewWorking=blockViewPreview=next;
                blockViewMessage=WindowText("Preview ready. Continue editing or apply this view.","Vorschau bereit. Weiter bearbeiten oder diese Ansicht übernehmen.");return true;
            } catch(Exception error){blockViewPreview=null;blockViewMessage=error.Message;return false;}
        }
        void RebuildBlockViewWindow()=>CustomWindowHandle.Create(BlockViewWindowId)?.SetNeedsRebuild();
        void BuildBlockViewEditor(UIPanelBuilder c) {
            if(blockViewRoot!=null){blockViewRoot.gameObject.SetActive(false);Destroy(blockViewRoot.gameObject);}
            blockViewRoot=null;blockViewRender++;c.VScrollView(BuildBlockViewSettings);
        }
        void BuildBlockViewSettings(UIPanelBuilder c) {
            if(blockViewBase==null)return;
            c.AddField(WindowText("View editor status","Status des Ansichtseditors"),()=>BlockViewIssue()??blockViewMessage??"",UIPanelBuilder.Frequency.Periodic);
            var line=blockViewBase.FreeLines.Single(l=>l.Id==blockViewLine);var options=blockViewOptions;int generation=blockViewGeneration,render=blockViewRender;bool current(){if(generation!=blockViewGeneration||render!=blockViewRender||!ReferenceEquals(options,blockViewOptions))return false;var issue=BlockViewIssue();if(issue==null)return true;blockViewMessage=issue;return false;}
            void Changed(){blockViewGeneration++;RefreshBlockViewPreview();RebuildBlockViewWindow();}
            var targets=blockViewBase.Desks.Where(d=>BlockDiagram.Area(d.Panel,line.Id)!=null).ToArray();
            c.AddField(WindowText("View on interlocking","Ansicht auf Stellwerk"),c.AddDropdown(targets.Select(d=>d.Code+" · "+d.Name).ToList(),Array.FindIndex(targets,d=>d.Id==blockViewDesk),i=>{if(!current()||targets[i].Id==blockViewDesk)return;blockViewDesk=targets[i].Id;LoadBlockViewOptions();RebuildBlockViewWindow();}));
            bool owner=line.BlockOwner==blockViewDesk;int total=line.BlockSections.Count;
            c.AddLabel(line.Code+" · "+line.Name+" · "+line.Track);
            c.AddLabel(WindowText("Operating interlocking: ","Bedienendes Stellwerk: ")+blockViewBase.Desks.Single(d=>d.Id==line.BlockOwner).Code);
            var neighbourA=blockViewBase.Desks.Single(d=>d.Id==line.StationA);var neighbourB=blockViewBase.Desks.Single(d=>d.Id==line.StationB);
            c.AddLabel(WindowText("Left to right: ","Von links nach rechts: ")+((options.Reverse^options.Turn180)?neighbourB.Code+" → "+neighbourA.Code:neighbourA.Code+" → "+neighbourB.Code));
            c.AddLabel(owner?WindowText("Operating interlocking: retains all signal assignments and original number sources. Reflection is available. Reduced views belong on additional display panels.","Bedienendes Stellwerk: Alle Signalzuordnungen und ursprünglichen Nummernquellen bleiben erhalten. Spiegelung ist möglich. Reduzierte Ansichten gehören auf zusätzliche Anzeigetafeln."):WindowText("Display only: choose detail and sections independently. Original numbers, signal control and physical blocks remain at the operating interlocking.","Nur Anzeige: Darstellungsstufe und Abschnitte unabhängig wählen. Ursprüngliche Nummern, Signalbedienung und reale Blöcke bleiben beim bedienenden Stellwerk."));
            if(!owner) {
                c.AddField(WindowText("Display detail","Darstellungsstufe"),c.AddDropdown(new[]{WindowText("Complete track diagram with signals","Vollständiges Gleisbild mit Signalen"),WindowText("Track and train numbers","Gleis und Zugnummern"),WindowText("Train-number fields only","Nur Zugnummernfelder")}.ToList(),(int)options.Detail,i=>{if(!current()||options.Detail==(BlockViewDetail)i)return;options.Detail=(BlockViewDetail)i;Changed();}));
                c.AddField(WindowText("First section (A → B)","Erster Abschnitt (A → B)"),c.AddDropdown(Enumerable.Range(1,total).Select(i=>i.ToString()).ToList(),options.FirstSection,i=>{if(!current()||options.FirstSection==i)return;options.FirstSection=i;options.SectionCount=Math.Min(options.SectionCount,total-i);Changed();}));
                c.AddField(WindowText("Visible section count","Anzahl sichtbarer Abschnitte"),c.AddDropdown(new[]{WindowText("All remaining sections","Alle verbleibenden Abschnitte")}.Concat(Enumerable.Range(1,total-options.FirstSection).Select(i=>i.ToString())).ToList(),options.SectionCount,i=>{if(!current()||options.SectionCount==i)return;options.SectionCount=i;Changed();}));
            }
            c.AddFieldToggle(WindowText("Reflect horizontally (B → A on screen)","Horizontal spiegeln (B → A auf dem Bildschirm)"),()=>options.Reverse,v=>{if(!current()||options.Reverse==v)return;options.Reverse=v;Changed();},true);
            c.AddFieldToggle(WindowText("Rotate the complete assembly 180°","Gesamten Verbund um 180° drehen"),()=>options.Turn180,v=>{if(!current()||options.Turn180==v)return;options.Turn180=v;Changed();},true);
            c.AddButton(WindowText("Refresh preview","Vorschau aktualisieren"),()=>BlockViewAction(()=>{if(!current())return;bool ready=RefreshBlockViewPreview();blockViewGeneration++;RebuildBlockViewWindow();if(!ready)throw new InvalidOperationException(blockViewMessage);}));
            var preview=blockViewPreview;
            c.AddButton(WindowText("Apply this view","Diese Ansicht übernehmen"),()=>BlockViewAction(()=>{
                if(!current()||!ReferenceEquals(preview,blockViewPreview))return;
                if(preview==null)throw new InvalidOperationException(blockViewMessage??WindowText("No valid preview. Review the status and refresh the preview.","Keine gültige Vorschau vorhanden. Status prüfen und die Vorschau aktualisieren."));
                if(blockViewBuild!=null) {
                    var build=blockViewBuild;build.Indicator=build.Indicator?.FollowView(build.Library,preview,line);build.Library=preview;blockConfirmedPositions.Remove(blockViewDesk);
                    manager.CloseWindow(BlockViewWindowId);OpenBlockPlacement(build,blockViewDesk);RefreshWorkspaceFields();
                } else {
                    lineIndicatorTarget=lineIndicatorTarget?.FollowView(blockViewBase,preview,line);blockBuild=null;blockLayout=null;CommitNumberOverview(preview);
                    blockViewBase=(Entry.Shared.Desks()??preview).Clone();blockViewConfig=Entry.Shared.ConfigurationVersion;blockViewInstall=Entry.Shared.ConstructionSignature;
                    LoadBlockViewOptions();status=blockViewMessage=WindowText("View saved. You can continue editing or close the window.","Ansicht gespeichert. Weiterbearbeiten oder das Fenster schließen ist möglich.");
                    RebuildBlockViewWindow();return;
                }
                blockViewGeneration++;blockViewWorking=blockViewPreview=null;
            }));
            c.AddButton(WindowText("Close without applying","Ohne Übernehmen schließen"),()=>{if(generation!=blockViewGeneration||render!=blockViewRender)return;blockViewGeneration++;blockViewWorking=blockViewPreview=null;manager.CloseWindow(BlockViewWindowId);});
            if(preview==null)return;
            var panelDoc=preview.Desks.Single(d=>d.Id==blockViewDesk).Panel;var area=BlockDiagram.Area(panelDoc,line.Id);
            c.AddLabel(WindowText("Preview: ","Vorschau: ")+area.Width+" × "+area.Height+WindowText(" cells. Click a free top-left position, then apply the view. Green fits; red means a collision. The working area grows if needed."," Felder. Freie linke obere Position anklicken, danach die Ansicht übernehmen. Grün passt; Rot bedeutet eine Kollision. Die Arbeitsfläche wächst bei Bedarf."));
            var root=blockViewRoot=c.VStack(_=>{}).GetComponent<RectTransform>();
            foreach(var layout in root.GetComponents<LayoutGroup>())layout.enabled=false;
            foreach(var fitter in root.GetComponents<ContentSizeFitter>())fitter.enabled=false;
            var size=root.GetComponent<LayoutElement>()??root.gameObject.AddComponent<LayoutElement>();size.ignoreLayout=false;size.minWidth=0;size.preferredWidth=-1;size.flexibleWidth=1;size.minHeight=size.preferredHeight=360;size.flexibleHeight=0;
            var viewport=WorkspaceSurface.Rect(root,"Viewport");WorkspaceSurface.Fill(viewport,0,0,12,12);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=scroll.vertical=true;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var display=BlockDiagramPlacement.Display(panelDoc,line.Id);
            var parts=BlockDiagram.Tiles(panelDoc,line.Id);var drawing=WorkspaceSurface.Rect(viewport,"Diagram");var view=drawing.gameObject.AddComponent<PanelView>();
            string positionText=WindowText("Current preview position: ","Aktuelle Vorschauposition: ")+EditorCanvasGeometry.ColumnName(area.X)+(area.Y+1);
            bool Alive()=>current()&&blockViewRoot==root&&ReferenceEquals(preview,blockViewPreview);
            PanelVisual Read(int x,int y){var tile=display.At(x,y);return new PanelVisual{Rail=new Color(.5f,.6f,.6f),Lamp=Color.red,Caption=tile?.Kind==PanelTileKind.Signal?Code(tile.Reference):tile?.Kind==PanelTileKind.TrainNumber?"-----":tile?.Label??"",PointKnown=true,SetupActive=tile!=null&&area.Contains(x,y)};}
            void Hover(int x,int y){if(!Alive())return;if(x<0||y<0){view.CancelMove();return;}view.PreviewCells(parts.Select(t=>{var copy=t.Clone();copy.X+=x-area.X;copy.Y+=y-area.Y;return copy;}).ToArray(),BlockDiagramPlacement.Issue(panelDoc,line.Id,x,y)==null);}
            void Place(int x,int y,bool half)=>BlockViewAction(()=>{
                if(!Alive())return;RequirePanelEdit();var issue=BlockDiagramPlacement.Issue(panelDoc,line.Id,x,y);if(issue!=null){positionText=issue;return;}
                var growth=BlockDiagramPlacement.Move(panelDoc,line.Id,x,y);area=BlockDiagram.Area(panelDoc,line.Id);parts=BlockDiagram.Tiles(panelDoc,line.Id);display=BlockDiagramPlacement.Display(panelDoc,line.Id);
                view.ReplaceDocument(display);drawing.anchoredPosition+=new Vector2(-growth.Left*PanelGeometry.CellWidth*view.Zoom,growth.Top*PanelGeometry.CellHeight*view.Zoom);view.CancelMove();
                positionText=WindowText("Position set: ","Position übernommen: ")+EditorCanvasGeometry.ColumnName(area.X)+(area.Y+1)+WindowText(". Apply this view to save it.",". Zum Speichern diese Ansicht übernehmen.");
            });
            view.Initialize(display,Read,Place,Alive,(x,y,half)=>{},z=>view.SetZoom(z),()=>null,()=>null,true);view.Hover=Hover;scroll.content=drawing;view.AttachScroll(scroll);
            viewport.gameObject.AddComponent<PanelViewportFit>().Initialize(view,drawing,panelDoc);
            scroll.horizontalScrollbar=WorkspaceBar(root,false);scroll.verticalScrollbar=WorkspaceBar(root,true);
            c.AddField(WindowText("Placement status","Platzierungsstatus"),()=>positionText,UIPanelBuilder.Frequency.Periodic);
            c.HStack(row=>{
                row.AddButton(WindowText("Move left","Nach links"),()=>Place(area.X-1,area.Y,false));row.AddButton(WindowText("Move right","Nach rechts"),()=>Place(area.X+1,area.Y,false));
                row.AddButton(WindowText("Move up","Nach oben"),()=>Place(area.X,area.Y-1,false));row.AddButton(WindowText("Move down","Nach unten"),()=>Place(area.X,area.Y+1,false));
            });
        }
    }
}
