using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EnusDE.Signaltechnik {
    // Two fixed, virtualized rulers, independent of the number of saved grid cells.
    public sealed class EditorGridHeader:MaskableGraphic,IPointerClickHandler,IScrollHandler {
        PanelView view;bool row;
        Action<bool,int,bool,bool> select;
        Func<PanelRegion> selection;
        Func<bool?> axis;
        Func<bool> alive;
        readonly List<TMP_Text> labels=new List<TMP_Text>();
        int first,last,stride;float cell,start;
        public void Initialize(PanelView drawing,bool rows,Action<bool,int,bool,bool> choose,Func<PanelRegion> selected,Func<bool?> selectedAxis,Func<bool> current) {
            view=drawing;row=rows;select=choose;selection=selected;axis=selectedAxis;alive=current;raycastTarget=true;
        }
        void LateUpdate() {
            if(alive?.Invoke()!=true||view?.Document==null)return;
            var origin=rectTransform.InverseTransformPoint(view.rectTransform.TransformPoint(Vector3.zero));
            cell=(row?PanelGeometry.CellHeight:PanelGeometry.CellWidth)*view.Zoom;start=row?-origin.y:origin.x;
            float length=row?rectTransform.rect.height:rectTransform.rect.width;int count=row?view.Document.Rows:view.Document.Columns;
            first=Math.Max(-EditorCanvasGeometry.Border,(int)Math.Floor(-start/cell));last=Math.Min(count+EditorCanvasGeometry.Border-1,(int)Math.Floor((length-start)/cell));stride=Math.Max(1,(int)Math.Ceiling((row?18:32)/cell));
            int used=0;
            for(int i=first;i<=last&&used<128;i++) {
                if(i>=0&&i<count&&i%stride!=0)continue;
                if(i<0||i>=count){if(stride>1)continue;}
                if(used==labels.Count){var go=new GameObject("Editor axis label",typeof(RectTransform));go.transform.SetParent(transform,false);var text=go.AddComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;text.raycastTarget=false;text.richText=false;text.alignment=TextAlignmentOptions.Center;text.fontSize=11;labels.Add(text);}
                var label=labels[used++];label.gameObject.SetActive(true);var r=label.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
                float span=Math.Max(cell,row?18:32);r.anchoredPosition=row?new Vector2(0,-(start+(i+.5f)*cell)+span/2):new Vector2(start+(i+.5f)*cell-span/2,0);r.sizeDelta=row?new Vector2(rectTransform.rect.width,span):new Vector2(span,rectTransform.rect.height);
                label.color=new Color(.68f,.85f,.85f);label.text=i<0||i>=count?"+":row?(i+1).ToString():EditorCanvasGeometry.ColumnName(i);
            }
            for(int i=used;i<labels.Count;i++)labels[i].gameObject.SetActive(false);SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();PanelChunk.Quad(vh,new Vector2(0,-rectTransform.rect.height),new Vector2(rectTransform.rect.width,0),new Color(.08f,.13f,.16f));
            var selected=selection?.Invoke();
            if(selected!=null&&axis?.Invoke()==row){int from=row?selected.Y:selected.X,to=from+(row?selected.Height:selected.Width);float a=start+from*cell,b=start+to*cell;
                PanelChunk.Quad(vh,row?new Vector2(0,-b):new Vector2(a,-rectTransform.rect.height),row?new Vector2(rectTransform.rect.width,-a):new Vector2(b,0),new Color(.12f,.36f,.32f));}
            if(cell<=0)return;
            for(int i=first;i<=last+1;i++)if(i%stride==0){float at=start+i*cell;PanelChunk.Line(vh,row?new Vector2(0,-at):new Vector2(at,0),row?new Vector2(rectTransform.rect.width,-at):new Vector2(at,-rectTransform.rect.height),1,new Color(.18f,.28f,.3f));}
        }
        public void OnPointerClick(PointerEventData d) {
            if(alive?.Invoke()!=true||cell<=0||d.button!=PointerEventData.InputButton.Left&&d.button!=PointerEventData.InputButton.Right)return;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(view.rectTransform,d.position,d.pressEventCamera,out var p))return;
            int index=(int)Math.Floor((row?-p.y:p.x)/cell);bool shift=Keyboard.current?.leftShiftKey.isPressed==true||Keyboard.current?.rightShiftKey.isPressed==true;
            select?.Invoke(row,index,shift,d.button==PointerEventData.InputButton.Right);
        }
        public void OnScroll(PointerEventData d){if(alive?.Invoke()==true)view.OnScroll(d);}
    }
}
