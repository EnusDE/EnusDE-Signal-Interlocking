using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EnusDE.Signaltechnik {
    // A virtual border around the saved panel. Clicking/dragging here can request growth.
    public sealed class EditorCanvasSurface:MaskableGraphic,IPointerClickHandler,IScrollHandler,IInitializePotentialDragHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerMoveHandler,IPointerExitHandler {
        PanelView view;
        public void Initialize(PanelView value){view=value;raycastTarget=true;color=new Color(.054f,.086f,.106f);}
        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();if(view?.Document==null)return;var doc=view.Document;float sx=PanelGeometry.CellWidth*view.Zoom,sy=PanelGeometry.CellHeight*view.Zoom;
            PanelChunk.Quad(vh,new Vector2(0,-rectTransform.rect.height),new Vector2(rectTransform.rect.width,0),color);
            var window=view.Window;int stride=EditorLayout.GridStride(sy),pad=EditorCanvasGeometry.Border;
            for(int x=(int)Math.Floor((double)window.Left/stride)*stride;x<=window.Right+1;x+=stride)PanelChunk.Line(vh,new Vector2((x+pad)*sx,-(window.Top+pad)*sy),new Vector2((x+pad)*sx,-(window.Bottom+1+pad)*sy),1,new Color(.12f,.18f,.21f));
            for(int y=(int)Math.Floor((double)window.Top/stride)*stride;y<=window.Bottom+1;y+=stride)PanelChunk.Line(vh,new Vector2((window.Left+pad)*sx,-(y+pad)*sy),new Vector2((window.Right+1+pad)*sx,-(y+pad)*sy),1,new Color(.12f,.18f,.21f));
        }
        public void Refresh()=>SetVerticesDirty();
        public void OnPointerClick(PointerEventData d)=>view.OnPointerClick(d);
        public void OnScroll(PointerEventData d)=>view.OnScroll(d);
        public void OnInitializePotentialDrag(PointerEventData d)=>view.OnInitializePotentialDrag(d);
        public void OnBeginDrag(PointerEventData d)=>view.OnBeginDrag(d);
        public void OnDrag(PointerEventData d)=>view.OnDrag(d);
        public void OnEndDrag(PointerEventData d)=>view.OnEndDrag(d);
        public void OnPointerMove(PointerEventData d)=>view.OnPointerMove(d);
        public void OnPointerExit(PointerEventData d)=>view.OnPointerExit(d);
    }
}
