using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace EnusDE.Signaltechnik {
    public struct PanelVisual {
        public string Caption,AlternateCaption;
        public Color Rail,Lamp,AlternateLamp;
        public bool Split,Distant,SharedCaption,LeftSelected,RightSelected;
        public bool Selected,Thrown,Unknown,Blink,Occupied;
        public bool SetupAvailable,SetupActive;
        public bool ShuntOnly,HasShunt,Retained,AuxiliaryHalf;
        public bool Invalid,Identification,AlternateInvalid,AlternateIdentification;
        public bool PointKnown,HasMiddleRail,Terminal;
        public int RailPorts,LineDirection,PermissionRequest;
        public int InspectionPorts;
        public bool InspectionBoundary,InspectionGap;
        public Dictionary<int,Color> PortColors;
        public Color MiddleRail;
        public string Boundary,OtherBoundary;
    }
    // One input surface and bounded mesh chunks. No GameObject for every grid cell.
    public sealed class PanelView : MaskableGraphic,IPointerClickHandler,IScrollHandler,IInitializePotentialDragHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerMoveHandler,IPointerExitHandler {
        public const int ChunkSize=1024;
        public sealed class Drawing {public PanelTile Tile;public PanelVisual State;}
        readonly List<PanelChunk> chunks=new List<PanelChunk>();
        readonly List<TMP_Text> labels=new List<TMP_Text>();
        readonly Vector3[] corners=new Vector3[4];
        PanelDocument document;
        Func<int,int,PanelVisual> read;
        Func<bool> alive;
        Func<PanelTile> draft;
        Func<PanelRegion> selected;
        Action<int,int,bool> press;
        Action<int,int,bool> right;
        Action<float> zoom;
        ScrollRect scroll;
        LayoutElement size;
        PanelWindow visible;
        double next;
        float scale=1;
        bool editorStyle;
        Func<bool> canMove;
        Action<PanelDrag> move;
        PanelDrag dragging;
        PanelChunk ghost;
        PanelDragOverlay dropOverlay;
        bool forwardingDrag,suppressClick;
        public Action<int,int> Hover;
        public void ConfigureMove(Func<bool> enabled,Action<PanelDrag> commit){canMove=enabled;move=commit;}
        public float Zoom=>scale;
        public PanelDocument Document=>document;
        public PanelWindow Window=>visible;
        public EditorCanvasSurface CanvasSurface {get;private set;}
        public Vector3? WheelAnchor {get;private set;}
        public void ConfigureCanvas(EditorCanvasSurface surface){CanvasSurface=surface;raycastTarget=false;SetZoom(scale);}
        public void ZoomAt(float value,Vector3 anchor) {
            var local=rectTransform.InverseTransformPoint(anchor);float oldZoom=scale;
            SetZoom(value);Canvas.ForceUpdateCanvases();
            var scaled=new Vector3((float)(EditorCanvasGeometry.ZoomScroll(0,local.x,oldZoom,scale)+local.x),(float)(EditorCanvasGeometry.ZoomScroll(0,local.y,oldZoom,scale)+local.y),local.z);
            if(scroll?.content!=null){var offset=scroll.content.parent.InverseTransformVector(anchor-rectTransform.TransformPoint(scaled));scroll.StopMovement();scroll.content.anchoredPosition+=(Vector2)offset;}
            WheelAnchor=null;Refresh();
        }
        public void Initialize(PanelDocument doc,Func<int,int,PanelVisual> read,Action<int,int,bool> press,Func<bool> alive,Action<int,int,bool> right,Action<float> zoom,Func<PanelTile> draft,Func<PanelRegion> selected,bool editor=false) {
            document=doc;this.read=read;this.press=press;this.alive=alive;this.right=right;this.zoom=zoom;this.draft=draft;this.selected=selected;
            editorStyle=editor;
            color=editor?new Color(.054f,.086f,.106f):new Color(.025f,.1f,.11f);raycastTarget=true;rectTransform.pivot=new Vector2(0,1);rectTransform.anchorMin=rectTransform.anchorMax=new Vector2(0,1);
            size=gameObject.AddComponent<LayoutElement>();scroll=GetComponentInParent<ScrollRect>();SetZoom(1);
        }
        public void ReplaceDocument(PanelDocument next) {
            if(next==null||ReferenceEquals(document,next))return;
            bool resize=document==null||document.Columns!=next.Columns||document.Rows!=next.Rows;
            document=next;
            if(resize)SetZoom(scale);else Refresh();
        }
        public void SetZoom(float value) {
            CancelMove();
            scale=Mathf.Clamp(value,.005f,2f);if(document==null)return;
            float width=document.Columns*PanelGeometry.CellWidth*scale,height=document.Rows*PanelGeometry.CellHeight*scale;rectTransform.sizeDelta=new Vector2(width,height);
            size.minWidth=size.preferredWidth=width;size.minHeight=size.preferredHeight=height;size.flexibleHeight=size.flexibleWidth=0;
            if(CanvasSurface!=null){float pad=EditorCanvasGeometry.Border;CanvasSurface.rectTransform.sizeDelta=new Vector2(width+2*pad*PanelGeometry.CellWidth*scale,height+2*pad*PanelGeometry.CellHeight*scale);rectTransform.anchoredPosition=new Vector2(pad*PanelGeometry.CellWidth*scale,-pad*PanelGeometry.CellHeight*scale);}
            Refresh();
        }
        public bool RevealCell(int x,int y) {
            if(scroll?.viewport==null||scroll.content==null||document==null||scroll.viewport.rect.width<=0||scroll.viewport.rect.height<=0)return false;
            float pad=CanvasSurface!=null?EditorCanvasGeometry.Border:0;
            float width=scroll.content.rect.width-scroll.viewport.rect.width,height=scroll.content.rect.height-scroll.viewport.rect.height;
            if(width>0)scroll.horizontalNormalizedPosition=Mathf.Clamp01(((x+.5f+pad)*PanelGeometry.CellWidth*scale-scroll.viewport.rect.width/2)/width);
            if(height>0)scroll.verticalNormalizedPosition=1-Mathf.Clamp01(((y+.5f+pad)*PanelGeometry.CellHeight*scale-scroll.viewport.rect.height/2)/height);
            Refresh();return true;
        }
        public bool IsVisible(PanelTile tile)=>visible.Contains(tile.X,tile.Y);
        public void AttachScroll(ScrollRect value){scroll=value;Refresh();}
        void ReadWindow() {
            if(scroll==null)scroll=GetComponentInParent<ScrollRect>();
            if(scroll==null||scroll.viewport==null){visible=new PanelWindow(0,0,document.Columns-1,document.Rows-1);return;}
            scroll.viewport.GetWorldCorners(corners);float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minY=minX,maxY=maxX;
            foreach(var corner in corners){var p=rectTransform.InverseTransformPoint(corner);minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
            visible=PanelWindow.Visible(document.Columns,document.Rows,PanelGeometry.CellWidth*scale,PanelGeometry.CellHeight*scale,minX,maxX,minY,maxY);
            if(CanvasSurface!=null){int pad=EditorCanvasGeometry.Border;visible=new PanelWindow(Math.Max(-pad,(int)Math.Floor(minX/(PanelGeometry.CellWidth*scale))),Math.Max(-pad,(int)Math.Floor(-maxY/(PanelGeometry.CellHeight*scale))),Math.Min(document.Columns+pad-1,(int)Math.Floor(maxX/(PanelGeometry.CellWidth*scale))),Math.Min(document.Rows+pad-1,(int)Math.Floor(-minY/(PanelGeometry.CellHeight*scale))));}
        }
        void Update(){if(dragging!=null&&(alive?.Invoke()!=true||canMove?.Invoke()!=true||UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true))CancelMove();if(document==null||Time.realtimeSinceStartup<next||alive?.Invoke()!=true)return;next=Time.realtimeSinceStartup+.2;Refresh();}
        public void Refresh() {
            if(document==null||read==null)return;ReadWindow();var preview=draft?.Invoke();
            var drawings=document.Tiles.Where(t=>visible.Contains(t.X,t.Y)&&!(preview!=null&&preview.X==t.X&&preview.Y==t.Y)).Concat(preview!=null&&visible.Contains(preview.X,preview.Y)?new[]{preview}:new PanelTile[0]).Select(t=>new Drawing{Tile=t,State=read(t.X,t.Y)}).ToArray();
            int count=(drawings.Length+ChunkSize-1)/ChunkSize;
            while(chunks.Count<count){var go=new GameObject("EnusDE Gleisbild-Mesh",typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(transform,false);rect.SetAsFirstSibling();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);var chunk=go.AddComponent<PanelChunk>();chunk.raycastTarget=false;chunks.Add(chunk);}
            for(int i=0;i<chunks.Count;i++){bool active=i<count;chunks[i].gameObject.SetActive(active);if(active)chunks[i].Set(drawings.Skip(i*ChunkSize).Take(ChunkSize).ToArray(),scale,rectTransform.sizeDelta);}
            int label=0;foreach(var item in drawings) {
                if(scale<.7f||string.IsNullOrEmpty(item.State.Caption)||label>=320)continue;
                var caption=LabelAt(label++);caption.gameObject.SetActive(true);caption.alignment=TextAlignmentOptions.Center;float sx=PanelGeometry.CellWidth*scale,sy=PanelGeometry.CellHeight*scale;var rect=caption.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=item.Tile.Kind==PanelTileKind.TrainNumber?new Vector2(.5f,.5f):new Vector2(.5f,0);rect.anchoredPosition=new Vector2((item.Tile.X+.5f)*sx,-(item.Tile.Y+(item.Tile.Kind==PanelTileKind.TrainNumber?.5f:1))*sy);rect.sizeDelta=new Vector2(sx-2,15*scale);caption.enableAutoSizing=false;caption.textWrappingMode=TextWrappingModes.NoWrap;caption.overflowMode=TextOverflowModes.Truncate;caption.fontSize=11*scale;caption.color=item.Tile.Kind==PanelTileKind.TrainNumber?new Color(1,.19f,.14f):new Color(.92f,.92f,.83f);string labelText=item.State.Caption;
                string slots=null;
                bool numeric=item.Tile.Kind==PanelTileKind.TrainNumber&&TrainNumbers.TryDisplaySlots(labelText,out slots);
                caption.richText=numeric;
                if(numeric)labelText="<mspace=0.56em>"+slots.Replace(' ','\u00A0')+"</mspace>";
                if(caption.text!=labelText)caption.text=labelText;
                PlaceSignalCaption(caption,item,false);
                if(NumberMirrors.IsMirror(item.Tile)&&!item.Tile.InlineNumberMirror&&label<320) {
                    var tag=LabelAt(label++);tag.gameObject.SetActive(true);tag.richText=false;tag.alignment=TextAlignmentOptions.Center;tag.enableAutoSizing=false;
                    tag.fontSize=9*scale;tag.color=new Color(.4f,.8f,.88f);tag.textWrappingMode=TextWrappingModes.NoWrap;tag.overflowMode=TextOverflowModes.Truncate;
                    tag.rectTransform.anchorMin=tag.rectTransform.anchorMax=new Vector2(0,1);tag.rectTransform.pivot=new Vector2(.5f,1);
                    tag.rectTransform.anchoredPosition=new Vector2((item.Tile.X+.5f)*sx,-(item.Tile.Y+.5f)*sy-10*scale);tag.rectTransform.sizeDelta=new Vector2(sx-4,11*scale);
                    tag.text=string.IsNullOrWhiteSpace(item.Tile.Label)?"Anzeige":item.Tile.Label;
                }

                if(!item.State.Split||item.State.SharedCaption||label>=320)continue;
                float main=item.Tile.SwapSignalHalves?.75f:.25f;
                float yard=1-main;
                rect.anchoredPosition=new Vector2((item.Tile.X+main)*sx,rect.anchoredPosition.y);
                rect.sizeDelta=new Vector2(sx*.5f-2,15*scale);
                caption.fontSize=9*scale;
                var second=LabelAt(label++);
                second.gameObject.SetActive(true);
                second.richText=false;
                second.alignment=TextAlignmentOptions.Center;
                second.fontSize=9*scale;
                second.color=caption.color;
                var half=second.rectTransform;
                half.anchorMin=half.anchorMax=new Vector2(0,1);half.pivot=new Vector2(.5f,0);
                half.anchoredPosition=new Vector2((item.Tile.X+yard)*sx,rect.anchoredPosition.y);
                half.sizeDelta=rect.sizeDelta;
                if(second.text!=item.State.AlternateCaption)second.text=item.State.AlternateCaption;
                PlaceSignalCaption(caption,item,false);
                if(NumberMirrors.IsMirror(item.Tile)&&!item.Tile.InlineNumberMirror&&label<320) {
                    var tag=LabelAt(label++);tag.gameObject.SetActive(true);tag.richText=false;tag.alignment=TextAlignmentOptions.Center;tag.enableAutoSizing=false;
                    tag.fontSize=9*scale;tag.color=new Color(.4f,.8f,.88f);tag.textWrappingMode=TextWrappingModes.NoWrap;tag.overflowMode=TextOverflowModes.Truncate;
                    tag.rectTransform.anchorMin=tag.rectTransform.anchorMax=new Vector2(0,1);tag.rectTransform.pivot=new Vector2(.5f,1);
                    tag.rectTransform.anchoredPosition=new Vector2((item.Tile.X+.5f)*sx,-(item.Tile.Y+.5f)*sy-10*scale);tag.rectTransform.sizeDelta=new Vector2(sx-4,11*scale);
                    tag.text=string.IsNullOrWhiteSpace(item.Tile.Label)?"Anzeige":item.Tile.Label;
                }
PlaceSignalCaption(second,item,true);
            }
            foreach(var item in drawings) {
                if(scale<.75f||label>=320||item.State.Boundary==null&&item.State.OtherBoundary==null)continue;
                var caption=LabelAt(label++);
                caption.gameObject.SetActive(true);
                caption.alignment=TextAlignmentOptions.Center;
                caption.richText=false;
                caption.enableAutoSizing=true;caption.textWrappingMode=TextWrappingModes.NoWrap;caption.overflowMode=TextOverflowModes.Ellipsis;
                caption.fontSizeMin=6*scale;caption.fontSizeMax=7.5f*scale;
                caption.color=new Color(.48f,.91f,.88f);
                float sx=PanelGeometry.CellWidth*scale,sy=PanelGeometry.CellHeight*scale;
                var rect=caption.rectTransform;
                rect.anchorMin=rect.anchorMax=new Vector2(0,1);
                var box=PanelSignalSymbols.BoundaryCaption(item.Tile,item.State.Split);
                rect.pivot=new Vector2(.5f,.5f);
                rect.anchoredPosition=new Vector2((item.Tile.X+.5f)*sx+box.X*scale,-(item.Tile.Y+.5f)*sy+box.Y*scale);
                rect.sizeDelta=new Vector2(box.Width*scale,box.Height*scale);
                var names=string.Join(" / ",new[]{item.State.Boundary,item.State.OtherBoundary}.Where(s=>s!=null));
                if(caption.text!=names)caption.text=names;
            }
            foreach(var area in document.Areas){if(scale<.25f||label>=320||area.X>visible.Right||area.Y>visible.Bottom||area.X+area.Width<=visible.Left||area.Y+area.Height<=visible.Top)continue;if(label==labels.Count){var go=new GameObject("EnusDE Bereichsname",typeof(RectTransform));go.transform.SetParent(transform,false);var text=go.AddComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;text.alignment=TextAlignmentOptions.TopLeft;text.richText=false;text.textWrappingMode=TextWrappingModes.NoWrap;text.raycastTarget=false;labels.Add(text);}var caption=labels[label++];caption.gameObject.SetActive(true);float sx=PanelGeometry.CellWidth*scale,sy=PanelGeometry.CellHeight*scale;var rect=caption.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(area.X*sx+3,-area.Y*sy-2);rect.sizeDelta=new Vector2(area.Width*sx-6,18);caption.enableAutoSizing=false;caption.textWrappingMode=TextWrappingModes.NoWrap;caption.overflowMode=TextOverflowModes.Truncate;caption.fontSize=Mathf.Max(10,12*scale);caption.color=new Color(.4f,.85f,.9f);caption.richText=false;caption.text=area.Code+" · "+area.Name;}
            for(int i=label;i<labels.Count;i++)labels[i].gameObject.SetActive(false);SetVerticesDirty();CanvasSurface?.Refresh();
        }
        void PlaceSignalCaption(TMP_Text text,Drawing item,bool alternate) {
            if(item.Tile.Kind!=PanelTileKind.Signal&&item.Tile.Kind!=PanelTileKind.AuxiliaryTarget&&item.Tile.Kind!=PanelTileKind.LinePermission)return;
            var box=PanelSignalSymbols.Caption(item.Tile,item.State.Split,item.State.SharedCaption,alternate,item.State.Boundary!=null||item.State.OtherBoundary!=null);
            var rect=text.rectTransform;rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(((item.Tile.X+.5f)*PanelGeometry.CellWidth+box.X)*scale,
                (-(item.Tile.Y+.5f)*PanelGeometry.CellHeight+box.Y)*scale);
            rect.sizeDelta=new Vector2(box.Width*scale,box.Height*scale);
            text.enableAutoSizing=true;text.fontSizeMin=6*scale;text.fontSizeMax=(box.Height<10?8:item.State.Split&&!item.State.SharedCaption?9:11)*scale;
            text.textWrappingMode=box.Height>20?TextWrappingModes.Normal:TextWrappingModes.NoWrap;
            text.overflowMode=TextOverflowModes.Ellipsis;
        }
        TMP_Text LabelAt(int index) {
            if(index<labels.Count)return labels[index];
            var go=new GameObject("EnusDE Tafeltext",typeof(RectTransform));
            go.transform.SetParent(transform,false);
            var text=go.AddComponent<TextMeshProUGUI>();
            text.font=TMP_Settings.defaultFontAsset;
            text.textWrappingMode=TextWrappingModes.NoWrap;
            text.overflowMode=TextOverflowModes.Truncate;
            text.raycastTarget=false;
            labels.Add(text);
            return text;
        }
        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();if(document==null)return;float sx=PanelGeometry.CellWidth*scale,sy=PanelGeometry.CellHeight*scale;PanelChunk.Quad(vh,new Vector2(0,-document.Rows*sy),new Vector2(document.Columns*sx,0),color);
            if(editorStyle||scale>=.6f){
                int stride=editorStyle?EditorLayout.GridStride(sy):1;
                var gridColor=editorStyle?new Color(.12f,.18f,.21f):new Color(.14f,.17f,.18f);
                for(int x=visible.Left/stride*stride;x<=visible.Right+1;x+=stride)PanelChunk.Line(vh,new Vector2(x*sx,-visible.Top*sy),new Vector2(x*sx,-(visible.Bottom+1)*sy),1,gridColor);
                for(int y=visible.Top/stride*stride;y<=visible.Bottom+1;y+=stride)PanelChunk.Line(vh,new Vector2(visible.Left*sx,-y*sy),new Vector2((visible.Right+1)*sx,-y*sy),1,gridColor);
            }
            var region=selected?.Invoke();
            if(region!=null){
                var low=new Vector2(region.X*sx,-(region.Y+region.Height)*sy);
                var high=new Vector2((region.X+region.Width)*sx,-region.Y*sy);
                PanelChunk.Quad(vh,low,high,editorStyle?new Color(.10f,.34f,.32f,.5f):new Color(.2f,.3f,.45f,.6f));
                if(editorStyle){
                    var topLeft=new Vector2(low.x,high.y);var bottomRight=new Vector2(high.x,low.y);
                    var edge=new Color(.37f,.86f,.73f);
                    PanelChunk.Line(vh,low,topLeft,1.5f,edge);PanelChunk.Line(vh,topLeft,high,1.5f,edge);
                    PanelChunk.Line(vh,high,bottomRight,1.5f,edge);PanelChunk.Line(vh,bottomRight,low,1.5f,edge);
                }
            }
            foreach(var area in document.Areas){var a=new Vector2(area.X*sx,-area.Y*sy);var b=new Vector2((area.X+area.Width)*sx,-(area.Y+area.Height)*sy);var line=new Color(.25f,.55f,.6f);PanelChunk.Line(vh,a,new Vector2(b.x,a.y),1,line);PanelChunk.Line(vh,new Vector2(b.x,a.y),b,1,line);PanelChunk.Line(vh,b,new Vector2(a.x,b.y),1,line);PanelChunk.Line(vh,new Vector2(a.x,b.y),a,1,line);}
        }
        public void OnPointerClick(PointerEventData data) {
            if(suppressClick){suppressClick=false;return;}
            if(document==null||alive?.Invoke()!=true)return;Vector2 p;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,data.position,data.pressEventCamera,out p))return;
            int x,y;if(!PickLocal(p,out x,out y))return;
            bool half=PanelPicking.SecondHalf(document.At(x,y),x,y,scale,p.x,p.y);
            if(data.button==PointerEventData.InputButton.Right)right?.Invoke(x,y,half);else if(data.button==PointerEventData.InputButton.Left)press?.Invoke(x,y,half);
        }
        public void OnScroll(PointerEventData data){if(alive?.Invoke()!=true||data.scrollDelta.y==0)return;
            if(CanvasSurface!=null&&RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform,data.position,data.pressEventCamera,out var point))WheelAnchor=point;
            try{zoom?.Invoke(scale*(data.scrollDelta.y>0?1.2f:1/1.2f));}finally{WheelAnchor=null;}
        }
        bool PickLocal(Vector2 p,out int x,out int y)=>CanvasSurface==null?PanelPicking.Cell(document.Columns,document.Rows,scale,p.x,p.y,out x,out y):EditorCanvasGeometry.Pick(document,scale,p.x,p.y,out x,out y);
        bool Pick(PointerEventData data,out int x,out int y,bool origin=false) {
            x=y=-1;
            if(scroll?.viewport!=null&&!RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport,origin?data.pressPosition:data.position,data.pressEventCamera))return false;
            if(document==null||!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,origin?data.pressPosition:data.position,data.pressEventCamera,out var p))return false;
            return PickLocal(p,out x,out y);
        }
        public void OnInitializePotentialDrag(PointerEventData data){suppressClick=false;data.useDragThreshold=true;scroll?.OnInitializePotentialDrag(data);}
        public void OnBeginDrag(PointerEventData data) {
            if(data.button==PointerEventData.InputButton.Left&&alive?.Invoke()==true&&canMove?.Invoke()==true&&Pick(data,out int x,out int y,true)&&(document.At(x,y)!=null||selected?.Invoke()?.Contains(x,y)==true||document.Areas.Any(a=>(a.BlockLine!=null||a.NumberLine!=null)&&a.Contains(x,y)))) {
                dragging=new PanelDrag();dragging.Begin(document,selected?.Invoke(),x,y,CanvasSurface!=null);suppressClick=true;data.eligibleForClick=false;
                OnDrag(data);return;
            }
            forwardingDrag=true;scroll?.OnBeginDrag(data);
        }
        public void OnDrag(PointerEventData data) {
            if(forwardingDrag){scroll?.OnDrag(data);return;}
            if(dragging==null)return;
            if(!Pick(data,out int x,out int y)){dragging.Invalidate();ShowMove();return;}
            dragging.Update(document,x,y);ShowMove();
        }
        void ShowMove() {
            if(dragging?.Source==null)return;
            var ink=dragging.Valid?new Color(.25f,1,.6f,.65f):new Color(1,.25f,.25f,.65f);
            var items=document.Tiles.Where(t=>dragging.Source.Contains(t.X,t.Y)&&visible.Contains(t.X+dragging.X-dragging.Source.X,t.Y+dragging.Y-dragging.Source.Y)).Take(256).Select(t=>{
                var copy=t.Clone();copy.X+=dragging.X-dragging.Source.X;copy.Y+=dragging.Y-dragging.Source.Y;
                var state=read(t.X,t.Y);state.Selected=state.LeftSelected=state.RightSelected=state.Blink=false;state.Rail=state.Lamp=state.AlternateLamp=ink;state.PortColors=null;
                return new Drawing{Tile=copy,State=state};
            }).ToArray();
            ShowGhost(items,ink);
        }
        public void PreviewCells(PanelTile[] tiles,bool valid) {
            var ink=valid?new Color(.25f,1,.6f,.65f):new Color(1,.25f,.25f,.65f);
            ShowGhost(tiles.Take(256).Select(t=>new Drawing{Tile=t,State=new PanelVisual{Rail=ink,Lamp=ink,AlternateLamp=ink,PointKnown=true}}).ToArray(),ink);
        }
        void ShowGhost(Drawing[] items,Color ink) {
            var parent=CanvasSurface!=null?CanvasSurface.rectTransform:rectTransform;
            if(CanvasSurface!=null)items=items.Select(item=>{var tile=item.Tile.Clone();tile.X+=EditorCanvasGeometry.Border;tile.Y+=EditorCanvasGeometry.Border;return new Drawing{Tile=tile,State=item.State};}).ToArray();
            if(ghost==null) {
                var bg=new GameObject("EnusDE move destination",typeof(RectTransform));bg.transform.SetParent(parent,false);dropOverlay=bg.AddComponent<PanelDragOverlay>();dropOverlay.raycastTarget=false;
                var go=new GameObject("EnusDE move ghost",typeof(RectTransform));go.transform.SetParent(parent,false);ghost=go.AddComponent<PanelChunk>();ghost.raycastTarget=false;
                foreach(var rect in new[]{dropOverlay.rectTransform,ghost.rectTransform})rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            }
            dropOverlay.gameObject.SetActive(true);dropOverlay.Set(items,scale,parent.sizeDelta,ink);
            ghost.gameObject.SetActive(true);ghost.Set(items,scale,parent.sizeDelta);dropOverlay.transform.SetAsLastSibling();ghost.transform.SetAsLastSibling();
        }
        public void OnPointerMove(PointerEventData data){if(Hover!=null&&alive?.Invoke()==true){if(Pick(data,out int x,out int y))Hover(x,y);else Hover(-1,-1);}}
        public void OnPointerExit(PointerEventData data)=>Hover?.Invoke(-1,-1);
        public void OnEndDrag(PointerEventData data) {
            if(forwardingDrag){forwardingDrag=false;scroll?.OnEndDrag(data);return;}
            var pending=dragging;CancelMove();data.eligibleForClick=false;
            if(pending?.Valid==true&&canMove?.Invoke()==true&&alive?.Invoke()==true)move?.Invoke(pending);
        }
        public void CancelMove(){dragging=null;if(ghost!=null)ghost.gameObject.SetActive(false);if(dropOverlay!=null)dropOverlay.gameObject.SetActive(false);}
    }
    public sealed class PanelDragOverlay:MaskableGraphic {
        PanelView.Drawing[] items=Array.Empty<PanelView.Drawing>();float zoom;Color ink;
        public void Set(PanelView.Drawing[] value,float scale,Vector2 size,Color color){items=value;zoom=scale;ink=color;rectTransform.sizeDelta=size;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();var fill=ink;fill.a=.22f;
            foreach(var item in items){var t=item.Tile;PanelChunk.Quad(vh,new Vector2(t.X*PanelGeometry.CellWidth*zoom,-(t.Y+1)*PanelGeometry.CellHeight*zoom),new Vector2((t.X+1)*PanelGeometry.CellWidth*zoom,-t.Y*PanelGeometry.CellHeight*zoom),fill);}
        }
    }
    public sealed class PanelChunk : MaskableGraphic {
        PanelView.Drawing[] drawings=new PanelView.Drawing[0];
        float zoom;
        public void Set(PanelView.Drawing[] items,float scale,Vector2 size){drawings=items;zoom=scale;rectTransform.sizeDelta=size;SetVerticesDirty();}
        public static void Quad(VertexHelper vh,Vector2 lo,Vector2 hi,Color color) {
            int n=vh.currentVertCount;vh.AddVert(new Vector3(lo.x,lo.y),color,Vector2.zero);vh.AddVert(new Vector3(lo.x,hi.y),color,Vector2.zero);vh.AddVert(new Vector3(hi.x,hi.y),color,Vector2.zero);vh.AddVert(new Vector3(hi.x,lo.y),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        public static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color) {
            var delta=b-a;if(delta.sqrMagnitude<.00001f)return;var normal=new Vector2(-delta.y,delta.x).normalized*(width/2);int n=vh.currentVertCount;
            var p=a-normal;vh.AddVert(new Vector3(p.x,p.y),color,Vector2.zero);p=a+normal;vh.AddVert(new Vector3(p.x,p.y),color,Vector2.zero);p=b+normal;vh.AddVert(new Vector3(p.x,p.y),color,Vector2.zero);p=b-normal;vh.AddVert(new Vector3(p.x,p.y),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        static void Dot(VertexHelper vh,Vector2 center,float radius,Color color) {
            int n=vh.currentVertCount;vh.AddVert(center,color,Vector2.zero);
            for(int i=0;i<12;i++){float angle=i*Mathf.PI/6;vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,color,Vector2.zero);}
            for(int i=0;i<12;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%12);
        }
        static void SignalGlyph(VertexHelper vh,Vector2 railPoint,int direction,bool shunt,bool target,Color lamp,Color rail,float zoom,bool mirror=false,int? sideDirection=null,bool invalid=false,bool identification=false) {
            Vector2 forward=direction==0?Vector2.right:direction==1?Vector2.down:direction==2?Vector2.left:Vector2.up;
            var side=new Vector2(-forward.y,forward.x);
            var offset=PanelSignalSymbols.HeadOffset(sideDirection??direction,mirror);
            var headOffset=new Vector2(offset.X,offset.Y)*zoom;
            var center=railPoint+headOffset;
            float pen=Mathf.Max(1,1.4f*zoom);
            var stem=railPoint-forward*(7*zoom);
            Line(vh,stem,stem+headOffset,pen,rail);
            Line(vh,stem+headOffset,center-forward*(4*zoom),pen,rail);
            Vector2 Map(PanelPoint p)=>center+(forward*p.X+side*p.Y)*zoom;
            var outline=PanelSignalSymbols.Screen(shunt);
            int n=vh.currentVertCount;
            foreach(var point in outline)vh.AddVert(Map(point),new Color(.025f,.055f,.065f),Vector2.zero);
            for(int i=1;i<outline.Length-1;i++)vh.AddTriangle(n,n+i,n+i+1);
            var border=target?new Color(1,.7f,.27f):new Color(.65f,.73f,.75f);
            for(int i=0;i<outline.Length;i++)Line(vh,Map(outline[i]),Map(outline[(i+1)%outline.Length]),pen,border);
            if(invalid)foreach(int sign in new[]{-1,1}){var a=Map(new PanelPoint(-4,-4*sign));var b=Map(new PanelPoint(4,4*sign));Line(vh,a,b,pen*3,Color.black);Line(vh,a,b,pen,Color.white);}
            else if(identification)Dot(vh,center,Mathf.Max(.85f,1.8f*zoom),Color.white);
            else foreach(var point in PanelSignalSymbols.Lamps(shunt,lamp==Color.white))Dot(vh,Map(point),Mathf.Max(.85f,1.8f*zoom),lamp);
            // Direction and operating button remain centered on the drawn track.
            var arrow=PanelSignalSymbols.Arrow();n=vh.currentVertCount;
            foreach(var point in arrow)vh.AddVert(railPoint+(forward*point.X+side*point.Y)*zoom,target?border:lamp,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);
            if(target) {
                var mark=center+headOffset*(8f/11);
                foreach(var line in PanelSignalSymbols.TargetMark())Line(vh,mark+new Vector2(line.A.X,line.A.Y)*zoom,mark+new Vector2(line.B.X,line.B.Y)*zoom,pen,border);
            }
        }
        static void DrawTarget(VertexHelper vh,Vector2 center,float zoom,Color color,bool shunt,int? direction,bool mirror=false) {
            SignalGlyph(vh,center,direction??0,shunt,true,direction.HasValue?Color.red:color,color,zoom,mirror);
        }
        static void SplitHead(VertexHelper vh,Vector2 center,float railY,int direction,bool shunt,Color lamp,Color rail,float zoom,bool mirror=false,int? sideDirection=null,bool invalid=false,bool identification=false) {
            SignalGlyph(vh,new Vector2(center.x,railY),direction,shunt,false,lamp,rail,zoom,mirror,sideDirection,invalid,identification);
        }
        static void SymbolRails(VertexHelper vh,PanelTile tile,PanelVisual state,Vector2 center,float sx,float sy,float zoom) {
            float pen=Mathf.Max(1,3*zoom);
            for(int port=0;port<8;port++) {
                if((state.RailPorts&(1<<port))==0||!state.PortColors.TryGetValue(port,out var color))continue;
                var end=center+new Vector2(PanelConnections.Dx[port]*sx*.5f,-PanelConnections.Dy[port]*sy*.5f);
                var start=center;
                if(state.HasMiddleRail)start=Vector2.Lerp(center,end,.5f);
                Line(vh,start,end,pen,color);
                if(state.HasMiddleRail)Line(vh,center,start,pen,state.MiddleRail);
                if(tile.Kind==PanelTileKind.TrainNumber&&(color==Color.red||color==Color.green))
                    Line(vh,center+Vector2.down*(8*zoom),Vector2.Lerp(center,end,.72f)+Vector2.down*(8*zoom),pen,color);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();float sx=PanelGeometry.CellWidth*zoom,sy=PanelGeometry.CellHeight*zoom;bool flash=((int)(Time.realtimeSinceStartup*2)%2)==0;
            foreach(var item in drawings) {
                var tile=item.Tile;var state=item.State;float x=(tile.X+.5f)*sx,y=-(tile.Y+.5f)*sy;
                if(state.Selected)Quad(vh,new Vector2(x-sx/2,y-sy*(tile.Kind==PanelTileKind.TrainNumber?.18f:.5f)),new Vector2(x+sx/2,y+sy*(tile.Kind==PanelTileKind.TrainNumber?.18f:.5f)),new Color(.2f,.27f,.38f));
                var selection=new Color(.2f,.27f,.38f);
                if(state.AuxiliaryHalf&&SignalCellLayout.MainDirection(tile)%2==1) {
                    if(state.LeftSelected)Quad(vh,new Vector2(x-sx/2,y),new Vector2(x+sx/2,y+sy/2),selection);
                    if(state.RightSelected)Quad(vh,new Vector2(x-sx/2,y-sy/2),new Vector2(x+sx/2,y),selection);
                } else {
                if(state.LeftSelected)Quad(vh,new Vector2(x-sx/2,y-sy/2),new Vector2(x,y+sy/2),selection);
                if(state.RightSelected)Quad(vh,new Vector2(x,y-sy/2),new Vector2(x+sx/2,y+sy/2),selection);
                }
                if(state.Blink){state.Rail=flash?new Color(1,.7f,.1f):new Color(.25f,.22f,.12f);if(tile.Kind==PanelTileKind.Signal&&!state.LeftSelected&&!state.RightSelected&&!state.Selected)Quad(vh,new Vector2(x-sx/2,y-sy/2),new Vector2(x+sx/2,y+sy/2),flash?new Color(.42f,.32f,.025f):new Color(.025f,.1f,.11f));}
                if(state.SetupAvailable||state.SetupActive) {
                    var lo=new Vector2(x-sx/2+1,y-sy/2+1);var hi=new Vector2(x+sx/2-1,y+sy/2-1);
                    Quad(vh,lo,hi,state.SetupActive?new Color(.08f,.46f,.20f):new Color(.035f,.19f,.095f));
                    if(state.SetupActive){var ink=new Color(.35f,1,.5f);float pen=Mathf.Max(2,2*zoom);Line(vh,lo,new Vector2(hi.x,lo.y),pen,ink);Line(vh,new Vector2(hi.x,lo.y),hi,pen,ink);Line(vh,hi,new Vector2(lo.x,hi.y),pen,ink);Line(vh,new Vector2(lo.x,hi.y),lo,pen,ink);}
                }
                var center=new Vector2(x,y);
                if(state.InspectionPorts!=0)for(int port=0;port<8;port++)if((state.InspectionPorts&(1<<port))!=0)
                    Line(vh,center,center+new Vector2(PanelConnections.Dx[port]*sx*.5f,-PanelConnections.Dy[port]*sy*.5f),Mathf.Max(5,9*zoom),new Color(.05f,.65f,.8f,.75f));
                if(state.InspectionBoundary||state.InspectionGap) {
                    Color mark=state.InspectionGap?new Color(1,.55f,.08f):new Color(.1f,.95f,1);
                    var a=center-new Vector2(sx*.45f,sy*.43f);var b=center+new Vector2(sx*.45f,sy*.43f);
                    Line(vh,a,new Vector2(b.x,a.y),2,mark);Line(vh,new Vector2(b.x,a.y),b,2,mark);Line(vh,b,new Vector2(a.x,b.y),2,mark);Line(vh,new Vector2(a.x,b.y),a,2,mark);
                }
                var lines=PanelConnections.Lines(tile);
                int railEnd=PanelConnections.RailLineCount(tile);
                int lineStart=state.PortColors==null?0:railEnd;
                for(int i=lineStart;i<railEnd;i++){var a=PanelGeometry.World(lines[i].A,tile.Rotation,tile.Mirror,0,0);var b=PanelGeometry.World(lines[i].B,tile.Rotation,tile.Mirror,0,0);Line(vh,new Vector2(x+a.X*zoom,y+a.Y*zoom),new Vector2(x+b.X*zoom,y+b.Y*zoom),Mathf.Max(1,3*zoom),tile.Kind==PanelTileKind.Switch&&state.PointKnown&&!PanelConnections.LineActive(tile,i,state.Thrown)?new Color(.18f,.2f,.21f):state.Rail);}
                if(tile.Kind==PanelTileKind.TrainNumber){float half=sy/2-4*zoom,height=7*zoom;var a=new Vector2(x-half,y-height);var b=new Vector2(x+half,y+height);Quad(vh,a,b,new Color(.035f,.055f,.065f));if(tile.Ports==0&&(!NumberMirrors.IsMirror(tile)||tile.InlineNumberMirror)&&state.PortColors==null){Line(vh,new Vector2(x-sx/2,y),new Vector2(x-half,y),Mathf.Max(1,3*zoom),state.Rail);Line(vh,new Vector2(x+half,y),new Vector2(x+sx/2,y),Mathf.Max(1,3*zoom),state.Rail);}Line(vh,a,new Vector2(b.x,a.y),1,state.Rail);Line(vh,new Vector2(b.x,a.y),b,1,state.Rail);Line(vh,b,new Vector2(a.x,b.y),1,state.Rail);Line(vh,new Vector2(a.x,b.y),a,1,state.Rail);}
                if(state.PortColors!=null)SymbolRails(vh,tile,state,center,sx,sy,zoom);
                if(tile.Kind==PanelTileKind.TrainNumber&&state.PortColors!=null) {
                    float w=sy/2-4*zoom,h=7*zoom;
                    Quad(vh,new Vector2(x-w,y-h)+Vector2.one,new Vector2(x+w,y+h)-Vector2.one,new Color(.035f,.055f,.065f));
                }
                if(state.Retained) {
                    var amber=new Color(1,.63f,.16f);
                    for(int i=-1;i<=1;i+=2)Line(vh,center+new Vector2(i*9-2,-3)*zoom,center+new Vector2(i*9+2,3)*zoom,Mathf.Max(1,1.5f*zoom),amber);
                }
                if(state.Terminal) {
                    int ports=PanelConnections.Ports(tile);
                    for(int i=0;i<8;i++)if((ports&(1<<i))!=0) {
                        var normal=new Vector2(PanelConnections.Dy[i],PanelConnections.Dx[i])*5*zoom;
                        Line(vh,center-normal,center+normal,Mathf.Max(1,2*zoom),state.Rail);
                    }
                }
                if(state.Boundary!=null||state.OtherBoundary!=null) {
                    float r=tile.Kind==PanelTileKind.Signal||tile.Kind==PanelTileKind.AuxiliaryTarget?2*zoom:Mathf.Max(1.6f,3*zoom);
                    var ink=new Color(.48f,.91f,.88f);
                    var location=PanelSignalSymbols.BoundaryMark(tile,state.Split);
                    var mark=center+new Vector2(location.X,location.Y)*zoom;
                    Line(vh,mark+new Vector2(-r,r),mark+new Vector2(r,-r),Mathf.Max(1,1.3f*zoom),ink);
                    Line(vh,mark+new Vector2(-r,-r),mark+new Vector2(r,r),Mathf.Max(1,1.3f*zoom),ink);
                }
                if(tile.Kind==PanelTileKind.LinePermission) {
                    Vector2 forward=tile.Rotation==0?Vector2.right:tile.Rotation==1?Vector2.down:tile.Rotation==2?Vector2.left:Vector2.up;
                    var side=new Vector2(-forward.y,forward.x);
                    foreach(int direction in new[]{-1,1}) {
                        var ink=state.LineDirection==direction?state.Lamp:state.PermissionRequest==direction&&(Time.unscaledTime%1)<.5f?Color.white:new Color(.25f,.33f,.35f);
                        var offset=side*(-direction*10*zoom);
                        if(state.LineDirection==direction) {
                            Vector2 Map(float x,float y)=>center+offset+forward*(direction*x*zoom)+side*(y*zoom);
                            int n=vh.currentVertCount;
                            foreach(var p in new[]{Map(-8,-3),Map(-8,3),Map(13,3),Map(13,6),Map(23,0),Map(13,-6),Map(13,-3)})vh.AddVert(p,ink,Vector2.zero);
                            foreach(var triangle in new[]{new[]{0,1,2},new[]{0,2,6},new[]{3,4,5},new[]{2,3,5},new[]{2,5,6}})vh.AddTriangle(n+triangle[0],n+triangle[1],n+triangle[2]);
                        }
                        var points=new[]{new Vector2(23,0),new Vector2(13,6),new Vector2(13,3),new Vector2(-8,3),new Vector2(-8,-3),new Vector2(13,-3),new Vector2(13,-6)};
                        for(int a=0;a<points.Length;a++) {
                            var p=points[a];var q=points[(a+1)%points.Length];
                            Line(vh,center+offset+forward*(direction*p.x*zoom)+side*(p.y*zoom),center+offset+forward*(direction*q.x*zoom)+side*(q.y*zoom),Mathf.Max(1,1.4f*zoom),ink);
                        }
                    }
                } else if(tile.Kind==PanelTileKind.AuxiliaryTarget) {
                    var target=tile.Target;
                    float half=sx/4;
                    var color=target==null?new Color(.43f,.47f,.49f):new Color(.94f,.64f,.26f);
                    if(target?.Function==AuxiliaryFunction.LinePermission) {
                        Line(vh,center+new Vector2(-14,0)*zoom,center+new Vector2(14,0)*zoom,2*zoom,color);
                        foreach(int sign in new[]{-1,1}){var tip=center+new Vector2(sign*14,0)*zoom;Line(vh,tip,tip+new Vector2(-sign*6,5)*zoom,2*zoom,color);Line(vh,tip,tip+new Vector2(-sign*6,-5)*zoom,2*zoom,color);}
                    } else {
                        if(target==null||target.Train)DrawTarget(vh,new Vector2(x-(target?.Shunt==true?half:0),y),zoom,color,false,target==null?(int?)null:SignalCellLayout.TargetDirection(tile),tile.Mirror);
                        if(target?.Shunt==true)DrawTarget(vh,new Vector2(x+(target.Train?half:0),y),zoom,color,true,SignalCellLayout.TargetDirection(tile),tile.Mirror);
                    }
                } else if(state.AuxiliaryHalf) {
                    var start=SignalCellLayout.HalfPoint(tile,false);var target=SignalCellLayout.HalfPoint(tile,true);
                    bool vertical=SignalCellLayout.MainDirection(tile)%2==1;
                    Line(vh,center+new Vector2(vertical?-sx/2:0,vertical?0:-sy/2),center+new Vector2(vertical?sx/2:0,vertical?0:sy/2),1,new Color(.21f,.33f,.37f));
                    SignalGlyph(vh,center+new Vector2(start.X,start.Y)*zoom,SignalCellLayout.MainDirection(tile),true,false,state.Lamp,state.Rail,zoom,tile.Mirror,invalid:state.Invalid,identification:state.Identification);
                    SignalGlyph(vh,center+new Vector2(target.X,target.Y)*zoom,SignalCellLayout.AuxiliaryDirection(tile),true,true,Color.red,state.Rail,zoom,tile.Mirror,SignalCellLayout.MainDirection(tile));
                } else if(state.Split) {
                    float dx=(tile.SwapSignalHalves?-1:1)*sx/4,dy=sy*.23f*(tile.Mirror?-1:1);
                    Line(vh,new Vector2(x,y-sy/2),new Vector2(x,y+sy/2),1,new Color(.21f,.33f,.37f));
                    SplitHead(vh,new Vector2(x-dx,y+dy),y,SignalCellLayout.MainDirection(tile),false,state.Lamp,state.Rail,zoom,tile.Mirror,SignalCellLayout.HeadSideDirection(tile),state.Invalid,state.Identification);
                    SplitHead(vh,new Vector2(x+dx,y+dy),y,SignalCellLayout.ShuntDirection(tile),true,state.AlternateLamp,state.Rail,zoom,tile.Mirror,SignalCellLayout.HeadSideDirection(tile),state.AlternateInvalid,state.AlternateIdentification);
                } else if(tile.Kind==PanelTileKind.Signal) {
                    if(state.Distant&&!state.Invalid&&!state.Identification) {
                        var p=PanelSignalSymbols.HeadOffset(SignalCellLayout.MainDirection(tile),tile.Mirror);
                        var head=new Vector2(x+p.X*zoom,y+p.Y*zoom);
                        Dot(vh,head+new Vector2(-3*zoom,-3*zoom),Mathf.Max(1.2f,2*zoom),state.Lamp);
                        Dot(vh,head+new Vector2(3*zoom,3*zoom),Mathf.Max(1.2f,2*zoom),state.AlternateLamp);
                    } else SignalGlyph(vh,new Vector2(x,y),SignalCellLayout.MainDirection(tile),state.ShuntOnly,false,state.Lamp,state.Rail,zoom,tile.Mirror,invalid:state.Invalid,identification:state.Identification);
                }
            }
        }
    }
}
