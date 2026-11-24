using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Model;
using Model.AI;
using Track;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public static class WaypointSwitchPolicy {
        public static bool Protect(bool electric,bool blockCoordination,bool optionalProtection,Func<bool> mapped)=>
            electric||blockCoordination&&optionalProtection&&mapped();
    }
    // Restrict only native waypoint-planner commands. Player input and the route engine keep their normal paths.
    public sealed class AutomationHooks:IDisposable {
        const string PatchId="enusde.signaltechnik.automation";
        static Func<string,bool> protectedPoint;
        static Action<UIPanelBuilder,Car,Func<bool>> carPanel;
        Harmony harmony;
        static readonly MethodInfo ctc=typeof(TrackNode).GetProperty("IsCTCSwitch").GetGetMethod();
        static readonly MethodInfo unlocked=typeof(TrackNode).GetProperty("IsCTCSwitchUnlocked").GetGetMethod();
        public static MethodInfo SwitchSearchTarget()=>typeof(AutoEngineerPlanner).GetMethod("CheckThrowable",BindingFlags.Static|BindingFlags.NonPublic);
        public static MethodInfo VehicleTabTarget()=>VehicleTabTarget(typeof(UI.CarInspector.CarInspector));
        public static MethodInfo VehicleTabTarget(Type inspector) {
            var matches=inspector.GetMethods(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly)
                .Where(m=>(m.Name.StartsWith("<BuildContent>",StringComparison.Ordinal)||m.Name.StartsWith("<PopulatePanel>",StringComparison.Ordinal))
                    &&m.ReturnType==typeof(void)&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==typeof(UITabbedPanelBuilder)).ToArray();
            if(matches.Length!=1)throw new MissingMethodException(inspector.FullName,"Unique vehicle tab builder (BuildContent/PopulatePanel, UITabbedPanelBuilder)");
            return matches[0];
        }
        public void Start(Func<string,bool> protect,Action<UIPanelBuilder,Car,Func<bool>> panel) {
            Dispose();protectedPoint=protect;carPanel=panel;
            try {
                harmony=new Harmony(PatchId);var flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
                harmony.Patch(typeof(AutoEngineerPlanner).GetMethod("TrySetSwitch",flags,null,new[]{typeof(TrackNode),typeof(bool)},null)??throw new MissingMethodException("AutoEngineerPlanner.TrySetSwitch"),
                    transpiler:new HarmonyMethod(typeof(AutomationHooks),nameof(GateCtc)));
                harmony.Patch(typeof(AutoEngineerPlanner).GetMethod("SetSwitchThrown",flags,null,new[]{typeof(TrackNode),typeof(bool)},null)??throw new MissingMethodException("AutoEngineerPlanner.SetSwitchThrown"),
                    prefix:new HarmonyMethod(typeof(AutomationHooks),nameof(BeforeSetSwitch)));
                harmony.Patch(SwitchSearchTarget()??throw new MissingMethodException("AutoEngineerPlanner.CheckThrowable"),
                    transpiler:new HarmonyMethod(typeof(AutomationHooks),nameof(GateCtc)));
                harmony.Patch(VehicleTabTarget(),postfix:new HarmonyMethod(typeof(AutomationHooks),nameof(AfterVehicleTabs)));
            }catch{Dispose();throw;}
        }
        static bool Protected(TrackNode node){try{return node!=null&&protectedPoint?.Invoke(node.id)==true;}catch{return true;}}
        static bool IsCtc(TrackNode node)=>Protected(node)||node.IsCTCSwitch;
        static bool IsUnlocked(TrackNode node)=>!Protected(node)&&node.IsCTCSwitchUnlocked;
        static bool BeforeSetSwitch(TrackNode __0)=>!Protected(__0);
        static IEnumerable<CodeInstruction> GateCtc(IEnumerable<CodeInstruction> instructions) {
            var list=instructions.ToList();int first=0,second=0;
            foreach(var code in list) {
                if(code.Calls(ctc)){code.opcode=OpCodes.Call;code.operand=AccessTools.Method(typeof(AutomationHooks),nameof(IsCtc));first++;}
                else if(code.Calls(unlocked)){code.opcode=OpCodes.Call;code.operand=AccessTools.Method(typeof(AutomationHooks),nameof(IsUnlocked));second++;}
            }
            if(first!=1||second!=1)throw new InvalidOperationException("Native Weichen-KI hat eine unbekannte CTC-Prüfung. Automatik bleibt nicht verfügbar.");return list;
        }
        static void AfterVehicleTabs(object __instance,UITabbedPanelBuilder __0) {
            try {
                var field=AccessTools.Field(__instance.GetType(),"_car");var car=field?.GetValue(__instance) as Car;
                if(car==null||!car.IsLocomotive)return;
                __0.AddTab(LocalText.Select("Signalling","Signaltechnik"),"enusde.signaltechnik.vehicle",builder=>{
                    var selected=field.GetValue(__instance) as Car;
                    var draw=carPanel;
                    if(selected!=null&&draw!=null)
                        draw(builder,selected,()=>selected!=null&&ReferenceEquals(carPanel,draw)&&ReferenceEquals(field.GetValue(__instance),selected));
                });
            }catch{/* An optional info tab must not break the native inspector. */}
        }
        public void Dispose(){if(harmony!=null){harmony.UnpatchAll(PatchId);harmony=null;}protectedPoint=null;carPanel=null;}
    }
}
