using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EnusDE.Signaltechnik {
    public sealed class MasterLayoutGeometry {
        public WorkspaceBox Header,Sidebar,Diagram,Footer;
        public static MasterLayoutGeometry For(double width,double height) {
            if(double.IsNaN(width)||double.IsInfinity(width)||double.IsNaN(height)||double.IsInfinity(height)||width<640||height<420)throw new ArgumentOutOfRangeException(nameof(width));
            return new MasterLayoutGeometry{Header=new WorkspaceBox(12,8,width-24,72),Sidebar=new WorkspaceBox(12,88,240,height-132),Diagram=new WorkspaceBox(264,88,width-276,height-132),Footer=new WorkspaceBox(12,height-36,width-24,28)};
        }
    }
    // All controls and the diagram have separate fixed regions inside the native window.
    sealed class MasterLayoutSurface:MonoBehaviour {
        RectTransform root,header,footer;
        TMP_Text notice;
        Vector2 lastSize;
        bool fitPending=true;
        public RectTransform Sidebar,Frame;
        internal RectTransform Header=>header;
        public PanelView View;
        public ScrollRect Scroll;
        public PanelDocument Document;
        public Func<string> Notice;
        public Action RestoreView;
        public void Initialize(RectTransform value,string title,string hint) {
            root=value;root.gameObject.AddComponent<Image>().color=WorkspaceSurface.Background;
            header=WorkspaceSurface.Rect(root,"Module header");footer=WorkspaceSurface.Rect(root,"Module status");
            Sidebar=WorkspaceSurface.Rect(root,"Module controls",WorkspaceSurface.Panel);Frame=WorkspaceSurface.Rect(root,"Module diagram",WorkspaceSurface.Panel);
            var label=WorkspaceSurface.Text(header,"Title",title,17);WorkspaceSurface.Fill(label.rectTransform,0,0,SignalRuntime.WorkspaceCommandsWidth+300,36);
            var instructions=WorkspaceSurface.Text(header,"Help",hint,13);WorkspaceSurface.Fill(instructions.rectTransform,0,38,0,0);
            var zoom=WorkspaceSurface.Rect(header,"Module zoom");zoom.anchorMin=zoom.anchorMax=zoom.pivot=new Vector2(1,1);zoom.sizeDelta=new Vector2(290,32);zoom.anchoredPosition=new Vector2(-SignalRuntime.WorkspaceCommandsWidth,-2);
            string[] names={"−","+","100 %",LocalText.Select("Fit diagram","Tafel einpassen")};int[] widths={30,30,58,148};int left=0;
            for(int i=0;i<names.Length;i++){int action=i;var button=EditorSurface.Button(zoom,names[i],()=>{if(View==null)return;if(action==3)Fit();else View.SetZoom(action==2?1:View.Zoom*(action==0?.8f:1.25f));});WorkspaceSurface.Place(button.GetComponent<RectTransform>(),new WorkspaceBox(left,0,widths[i],32));left+=widths[i]+6;}
            notice=WorkspaceSurface.Text(footer,"Status","",13);
        }
        public void SetInitialView(bool fit){fitPending=fit;if(!fit)StartCoroutine(Restore());}
        IEnumerator Restore(){yield return null;Canvas.ForceUpdateCanvases();RestoreView?.Invoke();}
        public void ThemeSoon()=>StartCoroutine(Theme());
        IEnumerator Theme(){yield return null;yield return null;WorkspaceSurface.Theme(Sidebar);}
        void Update() {
            if(root==null)return;var size=root.rect.size;
            if(size.x>=640&&size.y>=420&&size!=lastSize){lastSize=size;var layout=MasterLayoutGeometry.For(size.x,size.y);WorkspaceSurface.Place(header,layout.Header);WorkspaceSurface.Place(Sidebar,layout.Sidebar);WorkspaceSurface.Place(Frame,layout.Diagram);WorkspaceSurface.Place(footer,layout.Footer);}
            notice.text=Notice?.Invoke()??"";
            if(fitPending&&Scroll?.viewport!=null&&Scroll.viewport.rect.width>16){fitPending=false;Fit();}
        }
        void Fit() {
            if(View==null||Scroll?.viewport==null||Document==null)return;
            float width=Scroll.viewport.rect.width,height=Scroll.viewport.rect.height;var fit=EditorLayout.Fit(Document,width,height);View.SetZoom((float)fit.Zoom);Canvas.ForceUpdateCanvases();
            float sx=PanelGeometry.CellWidth*View.Zoom,sy=PanelGeometry.CellHeight*View.Zoom,dx=Document.Columns*sx-width,dy=Document.Rows*sy-height;
            Scroll.normalizedPosition=new Vector2(dx<=0?0:Mathf.Clamp01(((float)fit.CenterX*sx-width/2)/dx),dy<=0?1:1-Mathf.Clamp01(((float)fit.CenterY*sy-height/2)/dy));
        }
    }
    sealed class MasterModuleDrag:MonoBehaviour,IInitializePotentialDragHandler,IBeginDragHandler,IDragHandler,IEndDragHandler {
        public PanelView View;public ScrollRect Scroll;
        public Func<int,int,DeskModule> Find;
        public Func<string,PanelDocument> Local;
        public Action<string,int,int> Commit;
        public Func<bool> Alive;
        DeskModule module;Vector2 start;int targetX,targetY;RectTransform ghost;
        bool Point(PointerEventData e,Vector2 screen,out Vector2 local)=>RectTransformUtility.ScreenPointToLocalPointInRectangle(View.rectTransform,screen,e.pressEventCamera,out local);
        public void OnInitializePotentialDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Scroll.OnInitializePotentialDrag(e);}
        public void OnBeginDrag(PointerEventData e) {
            if(e.button!=PointerEventData.InputButton.Left||Alive?.Invoke()!=true||!Point(e,e.pressPosition,out start))return;
            module=Find((int)Math.Floor(start.x/(PanelGeometry.CellWidth*View.Zoom)),(int)Math.Floor(-start.y/(PanelGeometry.CellHeight*View.Zoom)))?.Clone();
            if(module==null){Scroll.OnBeginDrag(e);return;}
            Scroll.StopMovement();targetX=module.X;targetY=module.Y;
            ghost=WorkspaceSurface.Rect(View.transform,"Module move preview",new Color(.2f,.8f,.85f,.3f));ghost.GetComponent<Image>().raycastTarget=false;DrawGhost();
        }
        void DrawGhost(){var doc=Local(module.Desk);if(doc!=null)WorkspaceSurface.Place(ghost,new WorkspaceBox(targetX*PanelGeometry.CellWidth*View.Zoom,targetY*PanelGeometry.CellHeight*View.Zoom,doc.Columns*PanelGeometry.CellWidth*View.Zoom,doc.Rows*PanelGeometry.CellHeight*View.Zoom));}
        public void OnDrag(PointerEventData e) {
            if(e.button!=PointerEventData.InputButton.Left||Alive?.Invoke()!=true)return;
            if(module==null){Scroll.OnDrag(e);return;}
            if(!Point(e,e.position,out var now))return;
            targetX=module.X+Mathf.RoundToInt((now.x-start.x)/(PanelGeometry.CellWidth*View.Zoom));targetY=module.Y-Mathf.RoundToInt((now.y-start.y)/(PanelGeometry.CellHeight*View.Zoom));DrawGhost();
        }
        public void OnEndDrag(PointerEventData e) {
            if(e.button!=PointerEventData.InputButton.Left)return;
            if(module==null){Scroll.OnEndDrag(e);return;}
            var moved=module;module=null;if(ghost!=null)Destroy(ghost.gameObject);ghost=null;
            if(Alive?.Invoke()==true&&(targetX!=moved.X||targetY!=moved.Y))Commit(moved.Desk,targetX,targetY);
        }
        void OnDisable(){module=null;if(ghost!=null)Destroy(ghost.gameObject);ghost=null;}
    }
}
