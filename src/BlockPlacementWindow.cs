using System;
using System.Linq;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const string BlockPlacementWindowId="enusde.signaltechnik.block-placement";
        CustomWindowDefinition blockPlacementDefinition;
        RectTransform blockPlacementRoot;
        LineBlockBuild blockPlacementBuild;
        string blockPlacementDesk;
        void OpenBlockPlacement(LineBlockBuild build,string target) {
            RequirePanelEdit();EnsureWindow();blockPlacementBuild=build;blockPlacementDesk=target;
            if(blockPlacementDefinition==null) {
                blockPlacementDefinition=new CustomWindowDefinition{Identifier=BlockPlacementWindowId,Title=WindowText("Place block assembly","Blockverbund platzieren"),BuilderAction=BuildBlockPlacement,
                    Config=WindowConfig.Resizable(new Vector2Int(1100,720),new Vector2Int(700,460),new Vector2Int(3840,2160),Window.Position.CenterRight)};
                manager.RegisterCustomWindow(blockPlacementDefinition);
            }
            var handle=CustomWindowHandle.Create(BlockPlacementWindowId);handle?.Show();handle?.SetNeedsRebuild();
        }
        void BuildBlockPlacement(UIPanelBuilder c) {
            if(blockPlacementRoot!=null){blockPlacementRoot.gameObject.SetActive(false);Destroy(blockPlacementRoot.gameObject);}
            var build=blockPlacementBuild;string target=blockPlacementDesk;
            if(build==null||!ReferenceEquals(build,blockBuild))return;
            var desk=build.Library.Desks.Single(d=>d.Id==target);var actual=desk.Panel;var group=BlockDiagram.Area(actual,build.Line.Id);
            c.AddLabel(desk.Code+" · "+desk.Name+" · "+group.Width+" × "+group.Height);
            c.AddLabel(WindowText("Move the pointer to preview the complete assembly. Click its new top-left corner when green. Red means collision. The working area grows if needed. This changes only the proposal; confirm construction afterwards.","Maus bewegen: Vorschau des gesamten Verbunds. Bei Grün die neue linke obere Ecke anklicken. Rot bedeutet Kollision. Die Arbeitsfläche wächst bei Bedarf. Dies ändert nur den Vorschlag; anschließend den Bau bestätigen."));
            c.AddButton(WindowText("Edit display detail and reflection…","Darstellungsstufe und Spiegelung bearbeiten …"),()=>{if(ReferenceEquals(build,blockBuild)){manager.CloseWindow(BlockPlacementWindowId);OpenBlockViewEditor(build.Line.Id,target,build);}});
            c.AddButton(WindowText("Cancel placement","Platzierung abbrechen"),()=>{worldSelectionInput.Complete();blockPlacementBuild=null;manager.CloseWindow(BlockPlacementWindowId);});
            var window=c.VStack(_=>{}).GetComponentInParent<Window>();
            var root=blockPlacementRoot=WorkspaceSurface.Rect(window.transform,"Block placement viewport");WorkspaceSurface.Fill(root,8,190,8,16);
            var viewport=WorkspaceSurface.Rect(root,"Viewport");WorkspaceSurface.Fill(viewport,0,0,12,12);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=scroll.vertical=true;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var background=BlockDiagramPlacement.Display(actual,build.Line.Id);
            var parts=BlockDiagram.Tiles(actual,build.Line.Id);var drawing=WorkspaceSurface.Rect(viewport,"Diagram");var view=drawing.gameObject.AddComponent<PanelView>();
            bool Alive()=>root==blockPlacementRoot&&ReferenceEquals(blockBuild,build)&&ReferenceEquals(blockPlacementBuild,build)&&scope==blockPlanWorld;
            PanelVisual Read(int x,int y) {var tile=background.At(x,y);return new PanelVisual{Rail=new Color(.5f,.6f,.6f),Lamp=Color.red,Caption=tile?.Kind==PanelTileKind.Signal?Code(tile.Reference):tile?.Label??"",PointKnown=true};}
            void Preview(int x,int y) {
                if(!Alive())return;if(x<0||y<0){view.CancelMove();return;}var tiles=parts.Select(t=>{var copy=t.Clone();copy.X+=x-group.X;copy.Y+=y-group.Y;return copy;}).ToArray();view.PreviewCells(tiles,BlockDiagramPlacement.Issue(actual,build.Line.Id,x,y)==null);
            }
            void Place(int x,int y,bool half)=>Act(()=>{
                RequirePanelEdit();if(!Alive()||Entry.Shared.ConfigurationVersion!=blockPlanConfig||Entry.Shared.ConstructionSignature!=blockPlanInstall)throw new InvalidOperationException(WindowText("Placement proposal is outdated. Preview the block layout again.","Platzierungsvorschlag ist veraltet. Blockvorschau erneut erstellen."));
                var source=build.Indicator;bool follows=source?.Desk==target&&group.Contains(source.X,source.Y);int dx=x-group.X,dy=y-group.Y;
                var growth=BlockDiagramPlacement.Move(actual,build.Line.Id,x,y);if(source?.Desk==target)build.Indicator=LineIndicatorTarget.Capture(build.Library,target,source.X+growth.Left+(follows?dx:0),source.Y+growth.Top+(follows?dy:0));
                blockConfirmedPositions.Add(target);blockPlacementBuild=null;manager.CloseWindow(BlockPlacementWindowId);RefreshWorkspaceFields();
            });
            view.Initialize(background,Read,Place,Alive,(x,y,half)=>{},z=>view.SetZoom(z),()=>null,()=>null,true);view.Hover=Preview;scroll.content=drawing;view.AttachScroll(scroll);
            var fit=EditorLayout.Fit(actual,900,420);view.SetZoom((float)fit.Zoom);drawing.anchoredPosition=new Vector2((float)(450-fit.CenterX*PanelGeometry.CellWidth*fit.Zoom),(float)(fit.CenterY*PanelGeometry.CellHeight*fit.Zoom-210));
            scroll.horizontalScrollbar=WorkspaceBar(root,false);scroll.verticalScrollbar=WorkspaceBar(root,true);Preview(group.X,group.Y);
        }
    }
}
