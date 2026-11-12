using System;
using System.Collections;
using System.Reflection;
using Game.State;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    sealed class ModePicker : IDisposable {
        const string Id="enusde.signaltechnik.modes";
        const string Title="EnusDE Signal & Interlocking";
        readonly BuildToolbar toolbar;
        readonly ModePickerLanguage language=new ModePickerLanguage();
        readonly Func<ModePickerAccess> readAccess;
        readonly Action<WorkspaceMode> openMode;
        WindowManager manager;
        CustomWindowDefinition definition;
        Button[] buttons;
        RawImage[] icons;
        readonly Texture2D[] textures=new Texture2D[3];
        WorkspaceMode? hovered;
        ModePickerAccess access;
        public bool German {get;private set;}

        public ModePicker(Func<ModePickerAccess> readAccess,Action<WorkspaceMode> openMode) {
            this.readAccess=readAccess;
            this.openMode=openMode;
            toolbar=new BuildToolbar(Toggle);
        }

        public void Update(bool showToolbar) {
            German=language.ReadGerman();
            if(showToolbar)toolbar.Ensure(ModePickerText.ToolbarCaption(German));
            else {
                toolbar.Dispose();
                Close();
            }
            if(OwnsWindow()&&CustomWindowHandle.Create(Id)?.IsOpen()==true)Refresh();
        }

        void Toggle() {
            if(StateManager.IsUnloading)return;
            var current=WindowManager.Shared;
            if(current==null)return;
            if(manager!=current) {
                RemoveWindow();
                manager=current;
            }
            if(!OwnsWindow()) {
                var size=new Vector2Int(310,142);
                definition=new CustomWindowDefinition {
                    Identifier=Id,
                    Title=Title,
                    BuilderAction=Build,
                    Config=WindowConfig.Resizable(size,size,new Vector2Int(440,180),Window.Position.CenterRight)
                };
                manager.RegisterCustomWindow(definition);
            }
            German=language.ReadGerman();
            Refresh();
            CustomWindowHandle.Create(Id)?.Toggle();
        }

        void Build(UIPanelBuilder builder) {
            var row=builder.HStack(_=>{});
            var layout=row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing=6;
            layout.childControlWidth=layout.childControlHeight=true;
            layout.childForceExpandWidth=true;
            layout.childForceExpandHeight=false;
            buttons=new Button[3];
            icons=new RawImage[3];
            hovered=null;
            for(int i=0;i<buttons.Length;i++) {
                var mode=(WorkspaceMode)i;
                var created=UIBuilderAssets.Shared.CreateButton(UIBuilderAssets.ButtonStyle.Compact,row,()=>Select(mode));
                var rect=created.Item1;
                var sizing=rect.GetComponent<LayoutElement>()??rect.gameObject.AddComponent<LayoutElement>();
                sizing.minWidth=72;
                sizing.preferredWidth=88;
                sizing.flexibleWidth=1;
                sizing.minHeight=sizing.preferredHeight=62;
                buttons[i]=rect.GetComponent<Button>();
                created.Item2.gameObject.SetActive(false);
                var iconRect=WorkspaceSurface.Rect(rect,"Mode icon");
                iconRect.anchorMin=iconRect.anchorMax=iconRect.pivot=new Vector2(.5f,.5f);
                iconRect.anchoredPosition=Vector2.zero;
                iconRect.sizeDelta=new Vector2(46,46);
                iconRect.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;
                var icon=icons[i]=iconRect.gameObject.AddComponent<RawImage>();
                icon.texture=textures[i]??(textures[i]=CreateIcon(mode));
                icon.raycastTarget=false;
                var hint=rect.gameObject.AddComponent<ModePickerHint>();
                hint.Enter=()=>hovered=mode;
                hint.Exit=()=>{if(hovered==mode)hovered=null;};
            }
            builder.AddLabel(ReadHint,UIPanelBuilder.Frequency.Periodic);
            Refresh();
        }

        static Texture2D CreateIcon(WorkspaceMode mode) {
            var alpha=ModeIconRaster.Alpha(mode);
            var colors=new Color32[alpha.Length];
            for(int i=0;i<colors.Length;i++)colors[i]=new Color32(255,255,255,alpha[i]);
            var texture=new Texture2D(ModeIconRaster.Size,ModeIconRaster.Size,TextureFormat.RGBA32,false) {
                name="EnusDE "+mode,hideFlags=HideFlags.HideAndDontSave,
                filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp
            };
            texture.SetPixels32(colors);
            texture.Apply(false,true);
            return texture;
        }

        string ReadHint() {
            if(!hovered.HasValue)return ModePickerText.Status(access,German);
            var mode=hovered.Value;
            string caption=ModePickerText.Caption(mode,German);
            if(!access.CanOpen(mode))return caption+" · "+(German?"nicht verfügbar":"unavailable");
            if(mode==WorkspaceMode.Dispatcher&&!access.Host)return caption+" · "+(German?"nur Ansicht":"view only");
            return caption;
        }

        void Refresh() {
            access=readAccess();
            if(buttons==null)return;
            for(int i=0;i<buttons.Length;i++) {
                if(buttons[i]==null)continue;
                var mode=(WorkspaceMode)i;
                bool allowed=access.CanOpen(mode);
                buttons[i].interactable=allowed;
                icons[i].color=allowed?WorkspaceSurface.Ink:new Color(.43f,.48f,.51f);
            }
        }

        void Select(WorkspaceMode mode) {
            if(StateManager.IsUnloading||!readAccess().CanOpen(mode))return;
            Close();
            openMode(mode);
        }

        bool OwnsWindow()=>manager!=null&&definition!=null&&ReferenceEquals(manager.GetDefinition(Id),definition);
        void Close(){hovered=null;if(OwnsWindow()&&!StateManager.IsUnloading&&CustomWindowHandle.Create(Id)?.IsOpen()==true)manager.CloseWindow(Id);}

        void RemoveWindow() {
            if(OwnsWindow()) {
                try{manager.CloseWindow(Id);}catch{}
                var instance=manager.GetCustomWindow(Id);
                definition.BuilderAction=null;
                const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
                (typeof(WindowManager).GetField("_customDefinitions",flags)?.GetValue(manager) as IDictionary)?.Remove(Id);
                (typeof(WindowManager).GetField("_customWindows",flags)?.GetValue(manager) as IDictionary)?.Remove(Id);
                manager.UnregisterActiveWindow(Id);
                if(instance!=null)UnityEngine.Object.Destroy(instance.gameObject);
            }
            manager=null;
            definition=null;
            buttons=null;
            icons=null;
        }

        public void Dispose() {
            try{toolbar.Dispose();}
            finally{
                try{RemoveWindow();}
                finally{foreach(var texture in textures)if(texture!=null)UnityEngine.Object.Destroy(texture);}
            }
        }
    }

    sealed class ModePickerHint:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler {
        public Action Enter,Exit;
        public void OnPointerEnter(PointerEventData data)=>Enter?.Invoke();
        public void OnPointerExit(PointerEventData data)=>Exit?.Invoke();
        public void OnSelect(BaseEventData data)=>Enter?.Invoke();
        public void OnDeselect(BaseEventData data)=>Exit?.Invoke();
    }
}
