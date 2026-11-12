// Copyright (c) 2026 EnusDE. MIT License.
// Interoperates with the existing Mods preferences-tab protocol used by the
// ashenmare mods and EnusDE Translation. No shared menu DLL or toolbar button.
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UI.Builder;

namespace EnusDE.Signaltechnik
{
    internal static class ModsPreferencesRegistration
    {
        internal const string SlotKey="ashenmare.preferences-mods.v1";
        private const string PatchId="enusde.signaltechnik.preferences";
        private static Action<UIPanelBuilder> mine;
        private static Action<string> warning;
        private static Harmony harmony;
        private static bool reported;

        private static List<Action<UIPanelBuilder>> Slot(bool create)
        {
            var domain=AppDomain.CurrentDomain;
            lock(domain)
            {
                var existing=domain.GetData(SlotKey);
                var list=existing as List<Action<UIPanelBuilder>>;
                if(existing!=null&&list==null)throw new InvalidOperationException("Incompatible Mods preferences registry.");
                if(list==null&&create){list=new List<Action<UIPanelBuilder>>();domain.SetData(SlotKey,list);}
                return list;
            }
        }
        internal static IDisposable Register(Action<UIPanelBuilder> draw,Action<string> log)
        {
            if(draw==null)throw new ArgumentNullException(nameof(draw));
            Remove();warning=log;reported=false;
            var type=typeof(UIPanelBuilder).Assembly.GetType("UI.PreferencesWindow.PreferencesBuilder",true);
            var target=type.GetMethod("BuildTabs",BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,
                null,new[]{typeof(UITabbedPanelBuilder)},null);
            if(target==null)throw new MissingMethodException(type.FullName,"BuildTabs");
            var slot=Slot(true);mine=draw;
            lock(slot)slot.Add(draw);
            try
            {
                harmony=new Harmony(PatchId);
                harmony.Patch(target,postfix:new HarmonyMethod(typeof(ModsPreferencesRegistration),nameof(BuildTabsPostfix)));
                return new Registration(draw);
            }
            catch{Remove();throw;}
        }
        private sealed class Registration:IDisposable
        {
            private Action<UIPanelBuilder> draw;
            internal Registration(Action<UIPanelBuilder> value){draw=value;}
            public void Dispose(){if(draw!=null&&mine==draw)Remove();draw=null;}
        }
        private static void Remove()
        {
            var previous=mine;mine=null;
            try
            {
                var slot=Slot(false);
                if(slot!=null&&previous!=null)lock(slot)slot.Remove(previous);
            }
            finally
            {
                try{harmony?.UnpatchAll(PatchId);}
                finally{harmony=null;warning=null;}
            }
        }
        private static void BuildTabsPostfix(UITabbedPanelBuilder __0)
        {
            try
            {
                var slot=Slot(false);
                if(mine==null||slot==null)return;
                lock(slot)if(slot.Count==0||slot[0]!=mine)return;
                __0.AddTab("Mods","ashenmare.mods",DrawTab);
            }
            catch(Exception e){Report(e);}
        }
        private static void DrawTab(UIPanelBuilder builder)
        {
            builder.VScrollView(content=>
            {
                content.AddLabel("Einstellungen der unterstützten Mods. Die Speicherung bestimmt die jeweilige Mod.");
                var slot=Slot(false);if(slot==null)return;
                Action<UIPanelBuilder>[] contributors;
                lock(slot)contributors=slot.ToArray();
                foreach(var draw in contributors)try{draw(content);}catch(Exception e){Report(e);}
            });
        }
        private static void Report(Exception error)
        {
            if(reported)return;reported=true;
            warning?.Invoke("Could not display Mods settings: "+error.Message);
        }
    }
}
