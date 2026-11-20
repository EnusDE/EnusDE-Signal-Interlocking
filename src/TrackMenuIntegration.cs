using System;
using System.Reflection;
using HarmonyLib;
using Track;
using UI.ContextMenu;

namespace EnusDE.Signaltechnik {
    // Normal clicks extend the stock menu. Explicit assignment consumes the click
    // before native point operation or a second confirmation menu can run.
    public sealed class TrackMenuIntegration : IDisposable {
        const string PatchId="enusde.signaltechnik.track-menu";
        sealed class Scope { public Location Location;public bool Added; }
        static Scope scope;
        static Func<bool> available;
        static Action<Location,string> selected;
        static Action<string> warning;
        static Func<bool> pointPicking;
        static Func<TrackNode,bool> directPointPick;
        Harmony harmony;
        public void Start(Func<bool> canPlace,Action<Location,string> onSelected,Action<string> warn,Func<bool> pickingPoint=null,Func<TrackNode,bool> pickPoint=null) {
            Dispose();available=canPlace;selected=onSelected;warning=warn;pointPicking=pickingPoint;directPointPick=pickPoint;
            try {
                harmony=new Harmony(PatchId);
                var method=typeof(TrackContextMenu).GetMethod("TryShow",new[]{typeof(Location)})??throw new MissingMethodException("TrackContextMenu.TryShow(Location)");
                harmony.Patch(method,prefix:new HarmonyMethod(typeof(TrackMenuIntegration),nameof(Begin)),finalizer:new HarmonyMethod(typeof(TrackMenuIntegration),nameof(End)));
                harmony.Patch(typeof(ContextMenu).GetMethod("Show",new[]{typeof(string)}),prefix:new HarmonyMethod(typeof(TrackMenuIntegration),nameof(BeforeShow)));
                var stand=typeof(TrackNode).Assembly.GetType("SwitchStandClick",true);
                var activate=stand.GetMethod("Activate",new[]{typeof(PickableActivateEvent)});
                var node=stand.GetField("node",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                if(activate==null||node?.FieldType!=typeof(TrackNode))throw new MissingMethodException("SwitchStandClick.Activate / node");
                harmony.Patch(activate,prefix:new HarmonyMethod(typeof(TrackMenuIntegration),nameof(BeforePointActivate)));
            }catch{Dispose();throw;}
        }
        static bool Begin(Location __0,ref bool __result,out Scope __state) {
            __state=scope;scope=new Scope{Location=__0};
            if(pointPicking?.Invoke()!=true||available?.Invoke()!=true||!__0.IsValid)return true;
            scope.Added=true;__result=true;
            try{selected?.Invoke(__0,"assign-point");}catch(Exception e){warning?.Invoke(e.Message);}return false;
        }
        static bool BeforePointActivate(TrackNode ___node) {
            try{return directPointPick?.Invoke(___node)!=true;}
            catch(Exception e){warning?.Invoke(e.Message);return pointPicking?.Invoke()!=true;}
        }
        static Exception End(Exception __exception,Scope __state){scope=__state;return __exception;}
        static void BeforeShow(ContextMenu __instance) {
            var current=scope;
            try {
                if(current==null||current.Added||available?.Invoke()!=true||!current.Location.IsValid)return;
                current.Added=true;var at=current.Location;
                if(pointPicking?.Invoke()==true){__instance.Clear();Add(__instance,LocalText.Select("Assign point","Weiche zuordnen"),at,"assign-point");return;}
                Add(__instance,LocalText.Select("Signalling · build menu","Signaltechnik · Baumenü"),at,"build");
                Add(__instance,LocalText.Select("Track / point identifiers","Gleis-/Weichenkennungen"),at,"identifiers");
                Add(__instance,LocalText.Select("Assign point","Weiche zuordnen"),at,"assign-point");
                Add(__instance,LocalText.Select("Edit point","Weiche bearbeiten"),at,"edit-point");
                Add(__instance,LocalText.Select("Enable point detection boundaries","Weichen-GFM-Grenzen aktivieren"),at,"point-circuit-enable");
                Add(__instance,LocalText.Select("Place automation contact","Automatik-Kontakt platzieren"),at,"automation-contact");
            }catch(Exception e){warning?.Invoke("Gleismenü: "+e.Message);}
        }
        static void Add(ContextMenu menu,string title,Location at,string kind) {
            menu.AddButton(ContextMenuQuadrant.General,title,()=>{try{selected?.Invoke(at,kind);}catch(Exception e){warning?.Invoke(e.Message);}});
        }
        public void Dispose(){scope=null;available=null;selected=null;warning=null;pointPicking=null;directPointPick=null;if(harmony!=null){harmony.UnpatchAll(PatchId);harmony=null;}}
    }
}
