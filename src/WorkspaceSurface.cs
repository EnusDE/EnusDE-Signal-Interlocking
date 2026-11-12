using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace EnusDE.Signaltechnik {
    // Own navigation, geometry and diagram viewport. The game supplies window focus/close only.
    public sealed class WorkspaceSurface : MonoBehaviour {
        public static readonly Color Background=new Color(.035f,.055f,.08f,1),Panel=new Color(.07f,.10f,.14f,1),Accent=new Color(.12f,.58f,.75f,1),Ink=new Color(.88f,.93f,.96f,1);
        public RectTransform Root,Popup,PopupBody,PopupHeader,ViewFrame,Header,Status;
        public TMP_Text Title,State,Selection,Hint,PopupTitle;
        public Func<string> ReadTitle,ReadStatus,ReadSelection,ReadPopupTitle;
        bool compactPopup;
        public void SetCompactPopup(bool value){if(value==compactPopup)return;compactPopup=value;lastSize=new Vector2(-1,-1);}
        public Action Closed;
        Vector2 lastSize=new Vector2(-1,-1);
        bool large,themePending;
        double px=double.NaN,py=double.NaN,next;
        public static RectTransform Rect(Transform parent,string name,Color? color=null) {
            var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            if(color.HasValue){var image=go.AddComponent<Image>();image.color=color.Value;image.raycastTarget=true;}return rect;
        }
        public static void Place(RectTransform rect,WorkspaceBox box){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2((float)box.X,-(float)box.Y);rect.sizeDelta=new Vector2((float)box.Width,(float)box.Height);}
        public static void Fill(RectTransform rect,float left=0,float top=0,float right=0,float bottom=0){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);}
        public static TMP_Text Text(Transform parent,string name,string text,float size=14,TextAlignmentOptions alignment=TextAlignmentOptions.MidlineLeft) {
            var rect=Rect(parent,name);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=TMP_Settings.defaultFontAsset;label.fontSize=size;label.color=Ink;label.text=text;label.alignment=alignment;label.richText=false;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Truncate;Fill(rect,8,0,8,0);return label;
        }
        public Button Button(Transform parent,string caption,Action click,string hint=null) {
            var rect=Rect(parent,caption,Panel);var button=rect.gameObject.AddComponent<Button>();var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.65f,.85f,1);colors.pressedColor=new Color(.3f,.65f,.9f);button.colors=colors;button.targetGraphic=rect.GetComponent<Image>();button.onClick.AddListener(()=>click?.Invoke());
            Text(rect,"Label",caption,13,TextAlignmentOptions.Center);var tooltip=rect.gameObject.AddComponent<WorkspaceHint>();tooltip.Owner=this;tooltip.Caption=hint??caption;return button;
        }
        public void Initialize(RectTransform root) {
            Root=root;var background=root.GetComponent<Image>()??root.gameObject.AddComponent<Image>();background.color=Background;background.raycastTarget=true;
            Header=Rect(root,"Kopfzeile");Status=Rect(root,"Status");ViewFrame=Rect(root,"Gleisbild",new Color(.025f,.075f,.08f));
            Title=Text(Header,"Stellwerk","",16);Place(Title.rectTransform,new WorkspaceBox(0,0,500,36));
            State=Text(Status,"Meldung","",14);Place(State.rectTransform,new WorkspaceBox(0,0,800,25));State.color=new Color(1,.78f,.36f);
            Selection=Text(Status,"Auswahl","",12);Place(Selection.rectTransform,new WorkspaceBox(0,26,600,22));
            Hint=Text(root,"Bedienhinweis","",12);Hint.gameObject.SetActive(false);
            Popup=Rect(root,"Bedienfenster",Panel);PopupHeader=Rect(Popup,"Fensterkopf",new Color(.10f,.17f,.23f));PopupTitle=Text(PopupHeader,"Titel","Auswahl",15);PopupBody=Rect(Popup,"Inhalt");
            var close=Button(PopupHeader,"X",()=>{ShowContext(false);Closed?.Invoke();},"Bedienfenster schließen");Fill(close.GetComponent<RectTransform>());close.GetComponent<RectTransform>().anchorMin=close.GetComponent<RectTransform>().anchorMax=new Vector2(1,1);close.GetComponent<RectTransform>().pivot=new Vector2(1,1);close.GetComponent<RectTransform>().sizeDelta=new Vector2(34,30);
            var bigger=Button(PopupHeader,"+",()=>{large=!large;lastSize=new Vector2(-1,-1);},"Bedienfenster größer / kleiner");var br=bigger.GetComponent<RectTransform>();br.anchorMin=br.anchorMax=br.pivot=new Vector2(1,1);br.sizeDelta=new Vector2(34,30);br.anchoredPosition=new Vector2(-36,0);
            var drag=PopupHeader.gameObject.AddComponent<WorkspaceDrag>();drag.Owner=this;
            ShowContext(false);
        }
        public void ShowContext(bool value){if(Popup!=null){Popup.gameObject.SetActive(value);Popup.SetAsLastSibling();}if(value)ThemeSoon();}
        public void SetPopupTitle(string text){if(PopupTitle!=null)PopupTitle.text=text;}
        public void Drag(Vector2 delta){float scale=GetComponentInParent<Canvas>()?.scaleFactor??1;var point=Popup.anchoredPosition+delta/Mathf.Max(.01f,scale);px=point.x;py=-point.y;lastSize=new Vector2(-1,-1);Layout();}
        public void HintText(string value){if(Hint==null)return;Hint.gameObject.SetActive(!string.IsNullOrEmpty(value));Hint.text=value;Hint.transform.SetAsLastSibling();}
        void Layout() {
            var size=Root.rect.size;
            if(size.x<240||size.y<180||size==lastSize)return;
            lastSize=size;
            var layout=WorkspaceLayout.For(size.x,size.y,large,px,py,compactPopup);
            Place(Header,layout.Header);Place(Status,layout.Status);Place(ViewFrame,layout.Diagram);Place(Popup,layout.Popup);px=layout.Popup.X;py=layout.Popup.Y;
            Place(PopupHeader,new WorkspaceBox(0,0,layout.Popup.Width,34));Place(PopupBody,new WorkspaceBox(8,42,layout.Popup.Width-16,layout.Popup.Height-50));
            Place(PopupTitle.rectTransform,new WorkspaceBox(8,0,layout.Popup.Width-90,34));Place(Title.rectTransform,new WorkspaceBox(0,0,Math.Max(100,layout.Header.Width-310),36));Place(State.rectTransform,new WorkspaceBox(0,0,layout.Status.Width,25));Place(Selection.rectTransform,new WorkspaceBox(0,26,layout.Status.Width,22));Place(Hint.rectTransform,new WorkspaceBox(68,layout.Canvas.Height-88,Math.Min(660,layout.Canvas.Width-90),28));
        }
        void Update(){if(Root==null)return;Layout();if(Time.realtimeSinceStartup<next)return;next=Time.realtimeSinceStartup+.15;Set(Title,ReadTitle?.Invoke());Set(State,ReadStatus?.Invoke());Set(Selection,ReadSelection?.Invoke());Set(PopupTitle,ReadPopupTitle?.Invoke());}
        static void Set(TMP_Text text,string value){value=value??"";if(text!=null&&text.text!=value)text.text=value;}
        public void ThemeSoon(){if(themePending||!isActiveAndEnabled)return;themePending=true;StartCoroutine(ThemeAfterBuild());}
        void OnEnable(){if(Root!=null)ThemeSoon();}
        void OnDisable(){themePending=false;}
        IEnumerator ThemeAfterBuild(){yield return null;yield return null;themePending=false;if(PopupBody==null)yield break;Theme(PopupBody);}
        public static void Theme(RectTransform body) {
            foreach(var text in body.GetComponentsInChildren<TMP_Text>(true)){text.color=Ink;text.fontSize=Mathf.Min(15,text.fontSize);text.enableAutoSizing=false;}
            foreach(var button in body.GetComponentsInChildren<Button>(true)){var image=button.targetGraphic as Image;if(image!=null){image.sprite=null;image.color=new Color(.12f,.23f,.30f);}var size=button.GetComponent<LayoutElement>()??button.gameObject.AddComponent<LayoutElement>();size.minHeight=size.preferredHeight=32;size.flexibleHeight=0;size.minWidth=0;size.preferredWidth=-1;size.flexibleWidth=1;}
            foreach(var input in body.GetComponentsInChildren<TMP_InputField>(true)){if(input.targetGraphic is Image image){image.sprite=null;image.color=new Color(.025f,.045f,.065f);}input.textComponent.color=Ink;}
            foreach(var bar in body.GetComponentsInChildren<Scrollbar>(true)){if(bar.targetGraphic is Image image){image.sprite=null;image.color=new Color(.25f,.4f,.48f);}}
        }
    }
    public sealed class WorkspaceHint:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler {public WorkspaceSurface Owner;public string Caption;public void OnPointerEnter(PointerEventData e){Owner?.HintText(Caption);}public void OnPointerExit(PointerEventData e){Owner?.HintText("");}}
    public sealed class WorkspaceDrag:MonoBehaviour,IDragHandler {public WorkspaceSurface Owner;public void OnDrag(PointerEventData e){Owner?.Drag(e.delta);}}
    public sealed class WorkspaceIcon:MaskableGraphic {
        public string Kind;
        internal static WorkspaceIcon Attach(Transform parent,string kind,float size,float verticalOffset) {
            var rect=WorkspaceSurface.Rect(parent,"Icon");
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.pivot=Vector2.zero;
            rect.sizeDelta=new Vector2(size,size);
            rect.anchoredPosition=new Vector2(-size/2,verticalOffset-size/2);
            var icon=rect.gameObject.AddComponent<WorkspaceIcon>();
            icon.Kind=kind;
            icon.raycastTarget=false;
            return icon;
        }
        protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var rect=rectTransform.rect;float sx=rect.width/32,sy=rect.height/32;foreach(var line in WorkspaceSymbols.For(Kind))PanelChunk.Line(vh,new Vector2(rect.xMin+line[0]*sx,rect.yMin+line[1]*sy),new Vector2(rect.xMin+line[2]*sx,rect.yMin+line[3]*sy),1.7f,WorkspaceSurface.Ink);
        }
    }
}
