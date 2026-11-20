using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Fit after native layout has assigned the actual viewport size, and again on resize.
    // User zoom/panning is retained until the viewport itself changes size.
    public sealed class PanelViewportFit : MonoBehaviour {
        PanelView view;RectTransform drawing,viewport;PanelDocument document;
        Vector2 previous=new Vector2(-1,-1);
        public void Initialize(PanelView target,RectTransform content,PanelDocument doc){view=target;drawing=content;document=doc;viewport=GetComponent<RectTransform>();}
        void LateUpdate() {
            if(view==null||drawing==null||viewport==null||document==null)return;
            var size=viewport.rect.size;if(size.x<100||size.y<100||(size-previous).sqrMagnitude<1)return;previous=size;
            var fit=EditorLayout.Fit(document,size.x,size.y);view.SetZoom((float)fit.Zoom);
            drawing.anchoredPosition=new Vector2((float)(size.x/2-fit.CenterX*PanelGeometry.CellWidth*fit.Zoom),(float)(fit.CenterY*PanelGeometry.CellHeight*fit.Zoom-size.y/2));
        }
    }
}
