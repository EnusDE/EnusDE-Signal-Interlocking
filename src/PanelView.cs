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
        public bool ShuntOnly,HasShunt;
        public bool PointKnown,HasMiddleRail,Terminal;
        public int RailPorts;
        public Dictionary<int,Color> PortColors;
        public Color MiddleRail;
        public string Boundary,OtherBoundary;
    }
    // One input surface and bounded mesh chunks. No GameObject for every grid cell.
    public sealed class PanelView : MaskableGraphic,IPointerClickHandler,IScrollHandler {
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
        public float Zoom=>scale;
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
            scale=Mathf.Clamp(value,.005f,2f);if(document==null)return;
            float width=document.Columns*PanelGeometry.CellWidth*scale,height=document.Rows*PanelGeometry.CellHeight*scale;rectTransform.sizeDelta=new Vector2(width,height);
            size.minWidth=size.preferredWidth=width;size.minHeight=size.preferredHeight=height;size.flexibleHeight=size.flexibleWidth=0;Refresh();
        }
        public bool IsVisible(PanelTile tile)=>visible.Contains(tile.X,tile.Y);
        public void AttachScroll(ScrollRect value){scroll=value;Refresh();}
        void ReadWindow() {
            if(scroll==null)scroll=GetComponentInParent<ScrollRect>();
            if(scroll==null||scroll.viewport==null){visible=new PanelWindow(0,0,document.Columns-1,document.Rows-1);return;}
            scroll.viewport.GetWorldCorners(corners);float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minY=minX,maxY=maxX;
            foreach(var corner in corners){var p=rectTransform.InverseTransformPoint(corner);minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
            visible=PanelWindow.Visible(document.Columns,document.Rows,PanelGeometry.CellWidth*scale,PanelGeometry.CellHeight*scale,minX,maxX,minY,maxY);
        }
        void Update(){if(document==null||Time.realtimeSinceStartup<next||alive?.Invoke()!=true)return;next=Time.realtimeSinceStartup+.2;Refresh();}
        public void Refresh() {
            if(document==null||read==null)return;ReadWindow();var preview=draft?.Invoke();
            var drawings=document.Tiles.Where(t=>visible.Contains(t.X,t.Y)&&!(preview!=null&&preview.X==t.X&&preview.Y==t.Y)).Concat(preview!=null&&visible.Contains(preview.X,preview.Y)?new[]{preview}:new PanelTile[0]).Select(t=>new Drawing{Tile=t,State=read(t.X,t.Y)}).ToArray();
            int count=(drawings.Length+ChunkSize-1)/ChunkSize;
            while(chunks.Count<count){var go=new GameObject("EnusDE Gleisbild-Mesh",typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(transform,false);rect.SetAsFirstSibling();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);var chunk=go.AddComponent<PanelChunk>();chunk.raycastTarget=false;chunks.Add(chunk);}
            for(int i=0;i<chunks.Count;i++){bool active=i<count;chunks[i].gameObject.SetActive(active);if(active)chunks[i].Set(drawings.Skip(i*ChunkSize).Take(ChunkSize).ToArray(),scale,rectTransform.sizeDelta);}
            int label=0;foreach(var item in drawings) {
                if(scale<.7f||string.IsNullOrEmpty(item.State.Caption)||label>=320)continue;
                var caption=LabelAt(label++);caption.gameObject.SetActive(true);caption.alignment=TextAlignmentOptions.Center;float sx=PanelGeometry.CellWidth*scale,sy=PanelGeometry.CellHeight*scale;var rect=caption.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=item.Tile.Kind==PanelTileKind.TrainNumber?new Vector2(.5f,.5f):new Vector2(.5f,0);rect.anchoredPosition=new Vector2((item.Tile.X+.5f)*sx,-(item.Tile.Y+(item.Tile.Kind==PanelTileKind.TrainNumber?.5f:1))*sy);rect.sizeDelta=new Vector2(sx-2,15*scale);caption.fontSize=11*scale;caption.color=item.Tile.Kind==PanelTileKind.TrainNumber?new Color(1,.19f,.14f):new Color(.92f,.92f,.83f);string labelText=item.State.Caption;
                string slots=null;
                bool numeric=item.Tile.Kind==PanelTileKind.TrainNumber&&TrainNumbers.TryDisplaySlots(labelText,out slots);
                caption.richText=numeric;
                if(numeric)labelText="<mspace=0.56em>"+slots.Replace(' ','\u00A0')+"</mspace>";
                if(caption.text!=labelText)caption.text=labelText;
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
            }
            foreach(var item in drawings) {
                if(scale<.75f||label>=320||item.State.Boundary==null&&item.State.OtherBoundary==null)continue;
                var caption=LabelAt(label++);
                caption.gameObject.SetActive(true);
                caption.alignment=TextAlignmentOptions.Center;
                caption.richText=false;
                caption.fontSize=7.5f*scale;
                caption.color=new Color(.48f,.91f,.88f);
                float sx=PanelGeometry.CellWidth*scale,sy=PanelGeometry.CellHeight*scale;
                var rect=caption.rectTransform;
                rect.anchorMin=rect.anchorMax=new Vector2(0,1);
                rect.pivot=new Vector2(.5f,1);
                rect.anchoredPosition=new Vector2((item.Tile.X+.5f)*sx,-item.Tile.Y*sy-2);
                rect.sizeDelta=new Vector2(sx-2,11*scale);
                var names=string.Join(" / ",new[]{item.State.Boundary,item.State.OtherBoundary}.Where(s=>s!=null));
                if(caption.text!=names)caption.text=names;
            }
            foreach(var area in document.Areas){if(scale<.25f||label>=320||area.X>visible.Right||area.Y>visible.Bottom||area.X+area.Width<=visible.Left||area.Y+area.Height<=visible.Top)continue;if(label==labels.Count){var go=new GameObject("EnusDE Bereichsname",typeof(RectTransform));go.transform.SetParent(transform,false);var text=go.AddComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;text.alignment=TextAlignmentOptions.TopLeft;text.richText=false;text.textWrappingMode=TextWrappingModes.NoWrap;text.raycastTarget=false;labels.Add(text);}var caption=labels[label++];caption.gameObject.SetActive(true);float sx=PanelGeometry.CellWidth*scale,sy=PanelGeometry.CellHeight*scale;var rect=caption.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(area.X*sx+3,-area.Y*sy-2);rect.sizeDelta=new Vector2(area.Width*sx-6,18);caption.fontSize=Mathf.Max(10,12*scale);caption.color=new Color(.4f,.85f,.9f);caption.richText=false;caption.text=area.Code+" · "+area.Name;}
            for(int i=label;i<labels.Count;i++)labels[i].gameObject.SetActive(false);SetVerticesDirty();
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
            if(document==null||alive?.Invoke()!=true)return;Vector2 p;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,data.position,data.pressEventCamera,out p))return;
            int x,y;if(!PanelPicking.Cell(document.Columns,document.Rows,scale,p.x,p.y,out x,out y))return;
            if(data.button==PointerEventData.InputButton.Right)right?.Invoke(x,y,PanelPicking.RightHalf(x,scale,p.x));else if(data.button==PointerEventData.InputButton.Left)press?.Invoke(x,y,PanelPicking.RightHalf(x,scale,p.x));
        }
        public void OnScroll(PointerEventData data){if(alive?.Invoke()==true&&data.scrollDelta.y!=0)zoom?.Invoke(scale*(data.scrollDelta.y>0?1.2f:1/1.2f));}
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
        static void DrawTarget(VertexHelper vh,Vector2 center,float zoom,Color color,bool shunt,int? direction) {
            float r=Mathf.Max(2,6*zoom);
            var top=center+new Vector2(0,r);var right=center+new Vector2(r,0);
            var bottom=center-new Vector2(0,r);var left=center-new Vector2(r,0);
            if(shunt) {
                top=center+new Vector2(-r,r);right=center+new Vector2(r,r);
                bottom=center+new Vector2(r,-r);left=center+new Vector2(-r,-r);
            }
            float pen=Mathf.Max(1,1.6f*zoom);
            Line(vh,top,right,pen,color);Line(vh,right,bottom,pen,color);Line(vh,bottom,left,pen,color);Line(vh,left,top,pen,color);
            if(!direction.HasValue)return;
            Vector2 forward=direction.Value==0?Vector2.right:direction.Value==1?Vector2.down:direction.Value==2?Vector2.left:Vector2.up;
            var side=new Vector2(-forward.y,forward.x);
            var tip=center+forward*(r*.63f);
            var tail=center-forward*(r*.55f);
            Line(vh,tail,tip,pen,color);
            Line(vh,tip,center+side*(r*.36f),pen,color);
            Line(vh,tip,center-side*(r*.36f),pen,color);
        }

        static void SplitHead(VertexHelper vh,Vector2 center,float railY,int direction,bool shunt,Color lamp,Color rail,float zoom) {
            Vector2 forward=direction==0?Vector2.right:direction==1?Vector2.down:direction==2?Vector2.left:Vector2.up;
            var side=new Vector2(-forward.y,forward.x);
            var stem=center-forward*(8*zoom);
            Line(vh,new Vector2(stem.x,railY),stem,Mathf.Max(1,2*zoom),rail);
            Line(vh,stem,center,Mathf.Max(1,2*zoom),rail);
            float r=Mathf.Max(1.5f,4.5f*zoom);
            if(!shunt) {
                int n=vh.currentVertCount;
                vh.AddVert(center+forward*(1.5f*r),lamp,Vector2.zero);
                vh.AddVert(center-forward*r+side*r,lamp,Vector2.zero);
                vh.AddVert(center-forward*r-side*r,lamp,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);
                return;
            }
            Quad(vh,center-new Vector2(r*1.45f,r),center+new Vector2(r*1.45f,r),new Color(.06f,.1f,.12f));
            float dot=Mathf.Max(1,1.55f*zoom);
            for(int i=-1;i<=1;i+=2) {
                var led=center+i*(forward+side*.55f)*r*.64f;
                Quad(vh,led-new Vector2(dot,dot),led+new Vector2(dot,dot),lamp);
            }
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
                if(state.LeftSelected)Quad(vh,new Vector2(x-sx/2,y-sy/2),new Vector2(x,y+sy/2),selection);
                if(state.RightSelected)Quad(vh,new Vector2(x,y-sy/2),new Vector2(x+sx/2,y+sy/2),selection);
                if(state.Blink){state.Rail=flash?new Color(1,.7f,.1f):new Color(.25f,.22f,.12f);if(tile.Kind==PanelTileKind.Signal&&!state.LeftSelected&&!state.RightSelected&&!state.Selected)Quad(vh,new Vector2(x-sx/2,y-sy/2),new Vector2(x+sx/2,y+sy/2),flash?new Color(.42f,.32f,.025f):new Color(.025f,.1f,.11f));}
                var center=new Vector2(x,y);
                var lines=PanelConnections.Lines(tile);
                int lineStart=state.PortColors==null?0:tile.Kind==PanelTileKind.Signal?lines.Count-2:lines.Count;
                for(int i=lineStart;i<lines.Count-(state.Split?2:0);i++){var a=PanelGeometry.World(lines[i].A,tile.Rotation,tile.Mirror,0,0);var b=PanelGeometry.World(lines[i].B,tile.Rotation,tile.Mirror,0,0);Line(vh,new Vector2(x+a.X*zoom,y+a.Y*zoom),new Vector2(x+b.X*zoom,y+b.Y*zoom),Mathf.Max(1,3*zoom),tile.Kind==PanelTileKind.Switch&&state.PointKnown&&!PanelConnections.LineActive(tile,i,state.Thrown)?new Color(.18f,.2f,.21f):state.Rail);}
                if(tile.Kind==PanelTileKind.TrainNumber){float half=sy/2-4*zoom,height=7*zoom;var a=new Vector2(x-half,y-height);var b=new Vector2(x+half,y+height);Quad(vh,a,b,new Color(.035f,.055f,.065f));if(tile.Ports==0&&state.PortColors==null){Line(vh,new Vector2(x-sx/2,y),new Vector2(x-half,y),Mathf.Max(1,3*zoom),state.Rail);Line(vh,new Vector2(x+half,y),new Vector2(x+sx/2,y),Mathf.Max(1,3*zoom),state.Rail);}Line(vh,a,new Vector2(b.x,a.y),1,state.Rail);Line(vh,new Vector2(b.x,a.y),b,1,state.Rail);Line(vh,b,new Vector2(a.x,b.y),1,state.Rail);Line(vh,new Vector2(a.x,b.y),a,1,state.Rail);}
                if(state.PortColors!=null)SymbolRails(vh,tile,state,center,sx,sy,zoom);
                if(tile.Kind==PanelTileKind.TrainNumber&&state.PortColors!=null) {
                    float w=sy/2-4*zoom,h=7*zoom;
                    Quad(vh,new Vector2(x-w,y-h)+Vector2.one,new Vector2(x+w,y+h)-Vector2.one,new Color(.035f,.055f,.065f));
                }
                if(state.Terminal) {
                    int ports=PanelConnections.Ports(tile);
                    for(int i=0;i<8;i++)if((ports&(1<<i))!=0) {
                        var normal=new Vector2(PanelConnections.Dy[i],PanelConnections.Dx[i])*5*zoom;
                        Line(vh,center-normal,center+normal,Mathf.Max(1,2*zoom),state.Rail);
                    }
                }
                if(state.Boundary!=null||state.OtherBoundary!=null) {
                    float r=Mathf.Max(1.6f,3*zoom);
                    var ink=new Color(.48f,.91f,.88f);
                    var mark=new Vector2(x,y+4*zoom);
                    Line(vh,mark+new Vector2(-r,r),mark+new Vector2(r,-r),Mathf.Max(1,1.3f*zoom),ink);
                    Line(vh,mark+new Vector2(-r,-r),mark+new Vector2(r,r),Mathf.Max(1,1.3f*zoom),ink);
                }
                if(tile.Kind==PanelTileKind.AuxiliaryTarget) {
                    var target=tile.Target;
                    float half=sx/4;
                    var color=target==null?new Color(.43f,.47f,.49f):new Color(.94f,.64f,.26f);
                    if(target==null||target.Train)DrawTarget(vh,new Vector2(x-(target?.Shunt==true?half:0),y),zoom,color,false,target==null?(int?)null:SignalCellLayout.TargetDirection(tile));
                    if(target?.Shunt==true)DrawTarget(vh,new Vector2(x+(target.Train?half:0),y),zoom,color,true,SignalCellLayout.TargetDirection(tile));
                } else if(state.Split) {
                    float dx=(tile.SwapSignalHalves?-1:1)*sx/4,dy=sy*.23f*(tile.Mirror?-1:1);
                    Line(vh,new Vector2(x,y-sy/2),new Vector2(x,y+sy/2),1,new Color(.21f,.33f,.37f));
                    SplitHead(vh,new Vector2(x-dx,y+dy),y,SignalCellLayout.MainDirection(tile),false,state.Lamp,state.Rail,zoom);
                    SplitHead(vh,new Vector2(x+dx,y+dy),y,SignalCellLayout.ShuntDirection(tile),true,state.AlternateLamp,state.Rail,zoom);
                } else if(tile.Kind==PanelTileKind.Signal){var p=PanelGeometry.World(new PanelPoint(.5f,.375f),tile.Rotation,tile.Mirror,0,0);float px=x+p.X*zoom,py=y+p.Y*zoom,radius=Mathf.Max(2,5*zoom);if(state.Distant){float dot=Mathf.Max(1,2*zoom);
                    Quad(vh,new Vector2(px-3*dot,py-3*dot),new Vector2(px-dot,py-dot),state.Lamp);
                    Quad(vh,new Vector2(px+dot,py+dot),new Vector2(px+3*dot,py+3*dot),state.AlternateLamp);
                }else if(state.ShuntOnly){Quad(vh,new Vector2(px-radius,py-radius),new Vector2(px+radius,py+radius),new Color(.1f,.15f,.16f));float dot=Mathf.Max(1,radius*.3f);Quad(vh,new Vector2(px-radius*.5f-dot,py-radius*.5f-dot),new Vector2(px-radius*.5f+dot,py-radius*.5f+dot),state.Lamp);Quad(vh,new Vector2(px+radius*.5f-dot,py+radius*.5f-dot),new Vector2(px+radius*.5f+dot,py+radius*.5f+dot),state.Lamp);}else{var a=PanelGeometry.World(new PanelPoint(.5f+7f/48,.375f),tile.Rotation,tile.Mirror,0,0);var b=PanelGeometry.World(new PanelPoint(.5f-5f/48,.375f+6f/48),tile.Rotation,tile.Mirror,0,0);var c=PanelGeometry.World(new PanelPoint(.5f-5f/48,.375f-6f/48),tile.Rotation,tile.Mirror,0,0);int n=vh.currentVertCount;vh.AddVert(new Vector3(x+a.X*zoom,y+a.Y*zoom),state.Lamp,Vector2.zero);vh.AddVert(new Vector3(x+b.X*zoom,y+b.Y*zoom),state.Lamp,Vector2.zero);vh.AddVert(new Vector3(x+c.X*zoom,y+c.Y*zoom),state.Lamp,Vector2.zero);vh.AddTriangle(n,n+1,n+2);if(state.HasShunt){var marker=PanelGeometry.World(new PanelPoint(.5f,.375f-11f/48),tile.Rotation,tile.Mirror,0,0);float dot=Mathf.Max(1,2*zoom);Quad(vh,new Vector2(x+marker.X*zoom-dot,y+marker.Y*zoom-dot),new Vector2(x+marker.X*zoom+dot,y+marker.Y*zoom+dot),Color.white);}}}
            }
        }
    }
}
