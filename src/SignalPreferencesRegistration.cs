using System;
using System.Reflection;
using HarmonyLib;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    internal static class SignalPreferencesRegistration {
        const string PatchId="enusde.signaltechnik.preferences.own-tab";
        static Action<UIPanelBuilder> mine;
        static Action<string> warning;
        static Harmony harmony;
        static bool reported;
        internal static IDisposable Register(Action<UIPanelBuilder> draw,Action<string> log) {
            if(draw==null)throw new ArgumentNullException(nameof(draw));
            Remove();mine=draw;warning=log;reported=false;
            var type=typeof(UIPanelBuilder).Assembly.GetType("UI.PreferencesWindow.PreferencesBuilder",true);
            var target=type.GetMethod("BuildTabs",BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,
                null,new[]{typeof(UITabbedPanelBuilder)},null);
            if(target==null){Remove();throw new MissingMethodException(type.FullName,"BuildTabs");}
            try{harmony=new Harmony(PatchId);harmony.Patch(target,postfix:new HarmonyMethod(typeof(SignalPreferencesRegistration),nameof(BuildTabsPostfix)));return new Registration(draw);}
            catch{Remove();throw;}
        }
        sealed class Registration:IDisposable {
            Action<UIPanelBuilder> draw;
            public Registration(Action<UIPanelBuilder> value){draw=value;}
            public void Dispose(){if(draw!=null&&mine==draw)Remove();draw=null;}
        }
        static void Remove(){mine=null;try{harmony?.UnpatchAll(PatchId);}finally{harmony=null;warning=null;}}
        static void BuildTabsPostfix(UITabbedPanelBuilder __0) {
            if(mine==null)return;
            try{__0.AddTab("Signaltechnik","enusde.signaltechnik.settings",DrawTab);}catch(Exception error){Report(error);}
        }
        static void DrawTab(UIPanelBuilder builder) {
            try{builder.VScrollView(content=>mine?.Invoke(content));}catch(Exception error){Report(error);}
        }
        static void Report(Exception error){if(reported)return;reported=true;warning?.Invoke("Signaltechnik-Einstellungen: "+error.Message);}
    }
}
