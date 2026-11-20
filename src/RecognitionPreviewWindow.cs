using System;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const string RecognitionPreviewWindowId="enusde.signaltechnik.recognition-preview";
        CustomWindowDefinition recognitionPreviewDefinition;
        RectTransform recognitionPreviewRoot;
        void OpenRecognitionPreview() {
            if(recognitionProposal==null||!recognitionDrawing)return;EnsureWindow();
            if(recognitionPreviewDefinition==null) {
                recognitionPreviewDefinition=new CustomWindowDefinition{Identifier=RecognitionPreviewWindowId,Title=WindowText("Diagram proposal · read only","Gleisbildvorschlag · schreibgeschützt"),BuilderAction=BuildRecognitionPreview,
                    Config=WindowConfig.Resizable(new Vector2Int(1100,700),new Vector2Int(700,450),new Vector2Int(3840,2160),Window.Position.CenterRight)};
                manager.RegisterCustomWindow(recognitionPreviewDefinition);
            }
            var handle=CustomWindowHandle.Create(RecognitionPreviewWindowId);handle?.Show();handle?.SetNeedsRebuild();
        }
        void BuildRecognitionPreview(UIPanelBuilder builder) {
            if(recognitionPreviewRoot!=null){recognitionPreviewRoot.gameObject.SetActive(false);Destroy(recognitionPreviewRoot.gameObject);}
            var proposal=recognitionProposal;if(proposal==null)return;
            var window=builder.VStack(_=>{}).GetComponentInParent<Window>();
            var root=recognitionPreviewRoot=WorkspaceSurface.Rect(window.transform,"Recognition preview");WorkspaceSurface.Fill(root,8,34,8,16);
            var viewport=WorkspaceSurface.Rect(root,"Preview viewport");WorkspaceSurface.Fill(viewport,0,0,12,12);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=scroll.vertical=true;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var drawing=WorkspaceSurface.Rect(viewport,"Preview diagram");var view=drawing.gameObject.AddComponent<PanelView>();
            bool Alive()=>ReferenceEquals(proposal,recognitionProposal)&&scope==recognitionWorld&&root==recognitionPreviewRoot;
            PanelVisual Read(int x,int y) {
                var tile=proposal.Panel.At(x,y);var signal=tile==null?null:Object(tile.Reference);
                return new PanelVisual{Rail=Color.yellow,Lamp=Color.red,Caption=tile?.Kind==PanelTileKind.Signal?signal?.Name??tile.Reference:tile?.Kind==PanelTileKind.Switch?PointAssignments.Caption(desks,tile.Reference):tile?.Label??"",
                    Distant=signal?.Kind==MarkerRecord.Distant,ShuntOnly=signal?.Kind==MarkerRecord.Shunt,Unknown=true};
            }
            view.Initialize(proposal.Panel,Read,(x,y,half)=>{},Alive,(x,y,half)=>{},v=>view.SetZoom(Mathf.Clamp(v,.025f,2)),()=>null,()=>null,true);
            scroll.content=drawing;view.SetZoom(.06f);view.AttachScroll(scroll);
            scroll.horizontalScrollbar=WorkspaceBar(root,false);scroll.verticalScrollbar=WorkspaceBar(root,true);
        }
    }
}
