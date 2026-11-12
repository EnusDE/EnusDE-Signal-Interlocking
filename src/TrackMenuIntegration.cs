using System;
using System.Reflection;
using HarmonyLib;
using Track;
using UI.ContextMenu;

namespace EnusDE.Signaltechnik {
    // Extend the original menu before it lays out its items. No replacement menu or input polling.
    public sealed class TrackMenuIntegration : IDisposable {
        const string PatchId="enusde.signaltechnik.track-menu";
        sealed class Scope { public Location Location;public bool Added; }
        static Scope scope;
        static Func<bool> available;
        static Action<Location,string> selected;
        static Action<string> warning;
        Harmony harmony;
        public void Start(Func<bool> canPlace,Action<Location,string> onSelected,Action<string> warn) {
            Dispose();available=canPlace;selected=onSelected;warning=warn;
            try {
                harmony=new Harmony(PatchId);
                var method=typeof(TrackContextMenu).GetMethod("TryShow",new[]{typeof(Location)})??throw new MissingMethodException("TrackContextMenu.TryShow(Location)");
                harmony.Patch(method,prefix:new HarmonyMethod(typeof(TrackMenuIntegration),nameof(Begin)),finalizer:new HarmonyMethod(typeof(TrackMenuIntegration),nameof(End)));
                harmony.Patch(typeof(ContextMenu).GetMethod("Show",new[]{typeof(string)}),prefix:new HarmonyMethod(typeof(TrackMenuIntegration),nameof(BeforeShow)));
            }catch{Dispose();throw;}
        }
        static void Begin(Location __0,out Scope __state){__state=scope;scope=new Scope{Location=__0};}
        static Exception End(Exception __exception,Scope __state){scope=__state;return __exception;}
        static void BeforeShow(ContextMenu __instance) {
            var current=scope;
            try {
                if(current==null||current.Added||available?.Invoke()!=true||!current.Location.IsValid)return;
                current.Added=true;var at=current.Location;
                Add(__instance,"Signaltechnik · Baumenü",at,"build");
                Add(__instance,"Gleis-/Weichenkennungen",at,"identifiers");
                Add(__instance,"Weiche zuordnen",at,"assign-point");
            }catch(Exception e){warning?.Invoke("Gleismenü: "+e.Message);}
        }
        static void Add(ContextMenu menu,string title,Location at,string kind) {
            menu.AddButton(ContextMenuQuadrant.General,title,()=>{try{selected?.Invoke(at,kind);}catch(Exception e){warning?.Invoke(e.Message);}});
        }
        public void Dispose(){scope=null;available=null;selected=null;warning=null;if(harmony!=null){harmony.UnpatchAll(PatchId);harmony=null;}}
    }
}
