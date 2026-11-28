using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    // Own editor chrome. The Railroader window supplies focus and resizing.
    sealed class EditorSurface:MonoBehaviour {
        static readonly Color Back=new Color(.052f,.073f,.095f);
        static readonly Color Surface=new Color(.091f,.124f,.158f);
        static readonly Color Border=new Color(.16f,.23f,.28f);
        static readonly Color ButtonColor=new Color(.13f,.19f,.23f);
        static readonly Color Active=new Color(.09f,.32f,.32f);
        static readonly Color Accent=new Color(.38f,.87f,.74f);
        readonly List<(Image Background,Func<bool> Selected)> tools=new List<(Image,Func<bool>)>();
        RectTransform root,header,footer,toolRail,catalogue,detailsFrame;
        TMP_Text title,notice;
        Vector2 lastSize;
        float nextRead;
        bool catalogueMode,themeQueued;
        public Action CloseDetails;
        public void ShowDetails(bool value) {
            if(detailsFrame==null||detailsFrame.gameObject.activeSelf==value)return;
            detailsFrame.gameObject.SetActive(value);lastSize=Vector2.zero;
        }
        public RectTransform ViewportFrame {get;private set;}
        public RectTransform Content=>catalogue;
        public RectTransform Details {get;private set;}
        public RectTransform Header=>header;
        public Func<string> Title, Heading, Notice;

        public void Initialize(RectTransform parent,bool chooseDesk) {
            root=parent;catalogueMode=chooseDesk;
            root.gameObject.AddComponent<Image>().color=Back;
            header=Frame(root,"Editor header",out var headerBody);
            title=WorkspaceSurface.Text(headerBody,"Title","",18);
            WorkspaceSurface.Fill(title.rectTransform,chooseDesk?14:110,0,SignalRuntime.WorkspaceCommandsWidth+(chooseDesk?14:280),0);
            footer=Frame(root,"Editor guidance",out var footerBody);
            notice=WorkspaceSurface.Text(footerBody,"Guidance","",13);notice.color=Accent;
            detailsFrame=Frame(root,"Editor details",out var details);
            Details=details;detailsFrame.gameObject.SetActive(false);
            if(chooseDesk) {
                Frame(root,"Interlocking catalogue",out catalogue).gameObject.name="Editor catalogue";
                return;
            }
            toolRail=Frame(root,"Editor tools",out _);toolRail.gameObject.SetActive(false);
            Frame(root,"Editor diagram",out var diagramBody);
            ViewportFrame=WorkspaceSurface.Rect(diagramBody,"Editor viewport frame",Back);
            WorkspaceSurface.Fill(ViewportFrame,2,2,2,2);
        }

        static RectTransform Frame(Transform parent,string name,out RectTransform body) {
            var frame=WorkspaceSurface.Rect(parent,name,Border);
            body=WorkspaceSurface.Rect(frame,name+" body",Surface);
            WorkspaceSurface.Fill(body,1,1,1,1);
            return frame;
        }

        public void Tool(string caption,string symbol,Action click,Func<bool> selected) {
            var button=IconButton(toolRail,caption,symbol,click);
            WorkspaceSurface.Place(button.GetComponent<RectTransform>(),new WorkspaceBox(10,8+tools.Count*44,40,40));
            tools.Add(((Image)button.targetGraphic,selected));
        }

        public static Button IconButton(Transform parent,string caption,string symbol,Action click) {
            var button=CreateButton(parent,caption,click);
            var rect=button.GetComponent<RectTransform>();
            WorkspaceIcon.Attach(rect,symbol,24,0);
            rect.Tooltip(caption,"");
            return button;
        }

        public static Button Button(Transform parent,string caption,Action click,string icon=null) {
            var button=CreateButton(parent,caption,click);
            var rect=button.GetComponent<RectTransform>();
            var label=WorkspaceSurface.Text(rect,"Caption",caption,13,icon==null?TextAlignmentOptions.Center:TextAlignmentOptions.MidlineLeft);
            if(icon!=null) {
                WorkspaceSurface.Fill(label.rectTransform,35,0,3,0);
                var glyph=WorkspaceIcon.Attach(rect,icon,19,0).rectTransform;
                glyph.anchorMin=glyph.anchorMax=new Vector2(0,.5f);
                glyph.anchoredPosition=new Vector2(9,-9.5f);
            }
            return button;
        }

        static Button CreateButton(Transform parent,string caption,Action click) {
            var rect=WorkspaceSurface.Rect(parent,caption,ButtonColor);
            var button=rect.gameObject.AddComponent<Button>();
            button.targetGraphic=rect.GetComponent<Image>();
            var colors=button.colors;
            colors.highlightedColor=new Color(.79f,1,1);colors.pressedColor=new Color(.55f,.86f,.83f);colors.selectedColor=colors.highlightedColor;
            button.colors=colors;button.onClick.AddListener(()=>click());
            return button;
        }

        public void ThemeSoon() {
            if(themeQueued||!isActiveAndEnabled)return;
            themeQueued=true;
            StartCoroutine(ThemeContent());
        }
        IEnumerator ThemeContent() {
            yield return null;
            themeQueued=false;
            if(Content!=null)WorkspaceSurface.Theme(Content);
            if(Details!=null)WorkspaceSurface.Theme(Details);
        }
        void OnDisable(){themeQueued=false;}

        void Update() {
            var size=root.rect.size;
            if(size!=lastSize&&size.x>=640&&size.y>=420) {
                lastSize=size;
                bool showDetails=detailsFrame.gameObject.activeSelf;
                var layout=EditorLayout.For(size.x,size.y,showDetails,false);
                WorkspaceSurface.Place(header,layout.Header);
                WorkspaceSurface.Place(footer,layout.Footer);
                if(showDetails)WorkspaceSurface.Place(detailsFrame,layout.Details);
                if(catalogueMode){
                    var box=layout.Catalogue;
                    if(showDetails){box.X=12;box.Width=layout.Diagram.Width;}
                    WorkspaceSurface.Place((RectTransform)catalogue.parent,box);
                }
                else {
                    WorkspaceSurface.Place(toolRail,layout.Tools);
                    WorkspaceSurface.Place((RectTransform)ViewportFrame.parent.parent,layout.Diagram);
                }
            }
            if(Time.realtimeSinceStartup<nextRead)return;
            nextRead=Time.realtimeSinceStartup+.15f;
            Set(title,Title?.Invoke());Set(notice,Notice?.Invoke());
            foreach(var tool in tools)if(tool.Background!=null)tool.Background.color=tool.Selected?.Invoke()==true?Active:ButtonColor;
        }

        static void Set(TMP_Text label,string value) {
            value=value??"";
            if(label!=null&&label.text!=value)label.text=value;
        }
    }
}
