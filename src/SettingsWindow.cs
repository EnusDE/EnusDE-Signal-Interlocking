using System;
using System.Collections;
using System.Reflection;
using Game.State;
using UI.Builder;
using UI.Windows;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Owns only the mod's standalone window. The game's mod-management pages stay native.
    internal sealed class SettingsWindow : IDisposable {
        internal const string Id="enusde.signaltechnik.settings-window";
        readonly Action<UIPanelBuilder> draw;
        readonly Action<string> warning;
        WindowManager manager;
        CustomWindowDefinition definition;
        bool disposed;

        public SettingsWindow(Action<UIPanelBuilder> draw,Action<string> warning) {
            this.draw=draw??throw new ArgumentNullException(nameof(draw));
            this.warning=warning;
        }

        public void Open()=>Open(false);

        internal void Open(bool refresh) {
            if(disposed||StateManager.IsUnloading)return;
            var current=WindowManager.Shared;
            if(current==null)return;
            try {
                if(manager!=current){Release();manager=current;}
                if(!OwnsWindow()) {
                    definition=new CustomWindowDefinition {
                        Identifier=Id,Title="EnusDE Signal & Interlocking",BuilderAction=Build,
                        Config=WindowConfig.Resizable(new Vector2Int(650,700),new Vector2Int(440,320),new Vector2Int(1200,1400),Window.Position.CenterRight)
                    };
                    manager.RegisterCustomWindow(definition);
                }
                var handle=CustomWindowHandle.Create(Id);
                bool wasOpen=handle?.IsOpen()==true;
                handle?.Show();
                if(refresh||!wasOpen)handle?.SetNeedsRebuild();
            }catch(Exception e){warning?.Invoke("Settings window: "+e.Message);}
        }

        void Build(UIPanelBuilder builder) {
            if(disposed||!OwnsWindow()||StateManager.IsUnloading)return;
            builder.VScrollView(content=>{
                content.AddTitle(LocalText.Select("Settings","Einstellungen"),"EnusDE Signal & Interlocking");
                try{draw(content);}
                catch(Exception e){
                    warning?.Invoke("Settings window: "+e.Message);
                    content.AddLabel(LocalText.Select("Settings could not be displayed. Close and reopen this window.","Einstellungen konnten nicht angezeigt werden. Dieses Fenster schließen und erneut öffnen."));
                }
            });
        }

        bool OwnsWindow()=>manager!=null&&definition!=null&&ReferenceEquals(manager.GetDefinition(Id),definition);

        // Adapted from the MIT-licensed native window ownership cleanup.
        public void Release() {
            try {
                if(!OwnsWindow())return;
                manager.CloseWindow(Id);
                var instance=manager.GetCustomWindow(Id);
                definition.BuilderAction=null;
                const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
                (typeof(WindowManager).GetField("_customDefinitions",flags)?.GetValue(manager) as IDictionary)?.Remove(Id);
                (typeof(WindowManager).GetField("_customWindows",flags)?.GetValue(manager) as IDictionary)?.Remove(Id);
                manager.UnregisterActiveWindow(Id);
                if(instance!=null)UnityEngine.Object.Destroy(instance.gameObject);
            }catch(Exception e){warning?.Invoke("Settings window cleanup: "+e.Message);}
            finally{manager=null;definition=null;}
        }

        public void Dispose(){if(disposed)return;disposed=true;Release();}
    }
}
