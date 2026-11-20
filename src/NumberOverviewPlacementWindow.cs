using System;
using System.Linq;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const string NumberOverviewPlacementId="enusde.signaltechnik.number-overview-placement";
        CustomWindowDefinition numberOverviewPlacementDefinition;
        RectTransform numberOverviewPlacementRoot;
        DeskLibrary numberOverviewProposal,numberOverviewOriginal;
        string numberOverviewTarget,numberOverviewArea,numberOverviewScope,numberOverviewEditor;
        int numberOverviewSettings;
        long numberOverviewConfiguration,numberOverviewInstallation;
        void OpenNumberOverviewPlacement(DeskLibrary proposal,string target,string area) {
            RequirePanelEdit();EnsureWindow();numberOverviewProposal=proposal;numberOverviewOriginal=desks;numberOverviewSettings=overviewGeneration;numberOverviewTarget=target;numberOverviewArea=area;
            numberOverviewScope=scope;numberOverviewEditor=deskId;numberOverviewConfiguration=Entry.Shared.ConfigurationVersion;numberOverviewInstallation=Entry.Shared.InstallationVersion;
            if(numberOverviewPlacementDefinition==null) {
                numberOverviewPlacementDefinition=new CustomWindowDefinition{Identifier=NumberOverviewPlacementId,Title=WindowText("Place train-number overview","Zugnummernübersicht platzieren"),BuilderAction=BuildNumberOverviewPlacement,
                    Config=WindowConfig.Resizable(new Vector2Int(1100,720),new Vector2Int(700,460),new Vector2Int(3840,2160),Window.Position.CenterRight)};
                manager.RegisterCustomWindow(numberOverviewPlacementDefinition);
            }
            var handle=CustomWindowHandle.Create(NumberOverviewPlacementId);handle?.Show();handle?.SetNeedsRebuild();
        }
        void BuildNumberOverviewPlacement(UIPanelBuilder c) {
            if(numberOverviewPlacementRoot!=null){numberOverviewPlacementRoot.gameObject.SetActive(false);Destroy(numberOverviewPlacementRoot.gameObject);}
            var proposal=numberOverviewProposal;if(proposal==null)return;string target=numberOverviewTarget,id=numberOverviewArea;
            var desk=proposal.Desks.Single(d=>d.Id==target);var actual=desk.Panel;var group=LineNumberOverview.Area(actual,id);
            c.AddLabel(desk.Code+" · "+desk.Name+" · "+group.Width+" × 1");
            c.AddLabel(WindowText("Move the pointer for a preview. Click a green position to save the complete number strip there. Red means occupied or outside the panel. Cancel leaves the original configuration unchanged.","Maus bewegen für eine Vorschau. Eine grüne Position anklicken, um die vollständige Nummernleiste dort zu speichern. Rot bedeutet belegt oder außerhalb der Tafel. Abbrechen lässt die ursprüngliche Konfiguration unverändert."));
            c.AddButton(WindowText("Cancel placement","Platzierung abbrechen"),()=>{worldSelectionInput.Complete();numberOverviewProposal=null;manager.CloseWindow(NumberOverviewPlacementId);});
            var window=c.VStack(_=>{}).GetComponentInParent<Window>();var root=numberOverviewPlacementRoot=WorkspaceSurface.Rect(window.transform,"Number overview viewport");WorkspaceSurface.Fill(root,8,150,8,16);
            var viewport=WorkspaceSurface.Rect(root,"Viewport");WorkspaceSurface.Fill(viewport,0,0,12,12);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=scroll.vertical=true;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var background=desk.IsMaster?MasterDiagram.Project(proposal,target,true):actual.Clone();background.Tiles.RemoveAll(t=>group.Contains(t.X,t.Y));background.Areas.RemoveAll(a=>a.Id==id);
            var parts=actual.Tiles.Where(t=>group.Contains(t.X,t.Y)).Select(t=>t.Clone()).ToArray();var drawing=WorkspaceSurface.Rect(viewport,"Diagram");var view=drawing.gameObject.AddComponent<PanelView>();
            bool Alive()=>!stopped&&root==numberOverviewPlacementRoot&&ReferenceEquals(numberOverviewProposal,proposal)&&ReferenceEquals(desks,numberOverviewOriginal)&&overviewGeneration==numberOverviewSettings&&scope==numberOverviewScope&&deskId==numberOverviewEditor&&editingWorkspace;
            PanelVisual Read(int x,int y){var tile=background.At(x,y);return new PanelVisual{Rail=new Color(.5f,.6f,.6f),Lamp=Color.red,Caption=tile?.Kind==PanelTileKind.Signal?Code(tile.Reference):tile?.Label??"",PointKnown=true};}
            void Preview(int x,int y){if(!Alive())return;view.PreviewCells(parts.Select(t=>{var copy=t.Clone();copy.X+=x-group.X;copy.Y+=y-group.Y;return copy;}).ToArray(),LineNumberOverview.CanPlace(proposal,target,id,x,y));}
            void Place(int x,int y,bool half)=>Act(()=>{
                RequirePanelEdit();if(!Alive()||Entry.Shared.ConfigurationVersion!=numberOverviewConfiguration||Entry.Shared.InstallationVersion!=numberOverviewInstallation)throw new InvalidOperationException(WindowText("Configuration changed. Reopen the number overview preview.","Konfiguration geändert. Vorschau der Zugnummernübersicht erneut öffnen."));
                LineNumberOverview.Move(proposal,target,id,x,y);proposal.Validate(scope);CommitNumberOverview(proposal);overviewGroup=id;numberOverviewProposal=null;
                worldSelectionInput.Complete();manager.CloseWindow(NumberOverviewPlacementId);status=WindowText("Number overview saved. Existing sources are mirrored; no new block installation created.","Zugnummernübersicht gespeichert. Vorhandene Quellen werden gespiegelt; keine neue Blockanlage angelegt.");RefreshWorkspaceFields();
            });
            view.Initialize(background,Read,Place,Alive,(x,y,half)=>{},z=>view.SetZoom(z),()=>null,()=>null,true);view.Hover=Preview;scroll.content=drawing;view.AttachScroll(scroll);
            var fit=EditorLayout.Fit(actual,900,420);view.SetZoom((float)fit.Zoom);drawing.anchoredPosition=new Vector2((float)(450-fit.CenterX*PanelGeometry.CellWidth*fit.Zoom),(float)(fit.CenterY*PanelGeometry.CellHeight*fit.Zoom-210));
            scroll.horizontalScrollbar=WorkspaceBar(root,false);scroll.verticalScrollbar=WorkspaceBar(root,true);Preview(group.X,group.Y);
        }
    }
}
