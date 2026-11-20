using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Track;
using Track.Signals;
using Game.State;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // It is discovered by the game's own named-location signal search. No AE speed-slider writes.
    public sealed class RuntimeSignalProxy : CTCSignal {
        public string ObjectId;
        public bool Bypassed;
        static readonly FieldInfo displayed=typeof(CTCSignal).GetField("<LastShownAspect>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
        SignalAspect published;
        bool hasPublished;
        public static SignalAspect PublishedAspect(CTCSignal signal)=>signal==null||displayed==null?SignalAspect.Stop:(SignalAspect)displayed.GetValue(signal);
        public void Publish(SignalAspect aspect) {
            if(hasPublished&&published==aspect)return;
            if(displayed==null||displayed.FieldType!=typeof(SignalAspect))throw new MissingFieldException("CTCSignal.LastShownAspect backing field");
            // Publish only on our own object, also covering native callers which inline the getter.
            displayed.SetValue(this,aspect);published=aspect;hasPublished=true;
        }
        protected override SignalAspect CalculateAspect(out StopReason reason){reason=StopReason.NoRoute;return SignalAspect.Stop;}
    }
    public sealed class SignalHooks : IDisposable {
        const string PatchId="enusde.signaltechnik.native-signals";
        Harmony harmony;
        static SignalRuntime runtime;
        static readonly MethodInfo restriction=typeof(Model.AI.AutoEngineerPlanner).GetMethod("SetSignalSpeedRestriction",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly Action<Model.AI.AutoEngineerPlanner,int?,int?,float,CTCSignal,bool> setRestriction=(Action<Model.AI.AutoEngineerPlanner,int?,int?,float,CTCSignal,bool>)Delegate.CreateDelegate(typeof(Action<Model.AI.AutoEngineerPlanner,int?,int?,float,CTCSignal,bool>),restriction);
        public void Start(SignalRuntime owner) {
            Dispose();runtime=owner;
            try {
                harmony=new Harmony(PatchId);
                Patch(typeof(CTCSignal),"OnEnable",nameof(Enable));
                Patch(typeof(CTCSignal),"SetNeedsUpdate",nameof(Enable));
                Patch(typeof(CTCSignal),"ShowAspect",nameof(Enable));
                Patch(typeof(CTCSignal),"AspectDisplayedBySignal",nameof(Aspect));
                Patch(typeof(CTCSignal),"get_LastShownAspect",nameof(LastAspect));
                Patch(typeof(CTCSignal),"get_IsCTC",nameof(IsCtc));
                Patch(typeof(CTCSignal),"get_IsIntermediate",nameof(IsIntermediate));
                Patch(typeof(CTCSignal),"get_DisplayName",nameof(Name));
                Patch(typeof(TrackNode),"set_isThrown",nameof(Throw));
                Patch(typeof(TrainController),"HandleSetSwitch",nameof(SwitchMessage));
                Patch(typeof(Model.AI.AutoEngineerPlanner),"CallSignalIfNeeded",nameof(AiRead));
                Patch(typeof(Model.AI.AutoEngineerPlanner),"UpdateTargets",nameof(BeforeAiTargets));
                var targets=typeof(Model.AI.AutoEngineerPlanner).GetMethod("UpdateTargets",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                harmony.Patch(targets,transpiler:new HarmonyMethod(typeof(SignalHooks),nameof(StrictStopBranch)));
            }catch{Dispose();throw;}
        }
        void Patch(Type type,string name,string prefix) {
            var method=type.GetMethod(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)??throw new MissingMethodException(type.FullName,name);
            harmony.Patch(method,prefix:new HarmonyMethod(typeof(SignalHooks),prefix));
        }
        static bool Enable(CTCSignal __instance)=>!(__instance is RuntimeSignalProxy);
        static bool Aspect(CTCSignal __0,ref SignalAspect __result)=>OwnAspect(__0,ref __result);
        static bool LastAspect(CTCSignal __instance,ref SignalAspect __result)=>OwnAspect(__instance,ref __result);
        static bool OwnAspect(CTCSignal signal,ref SignalAspect result) {
            var proxy=signal as RuntimeSignalProxy;if(proxy==null)return true;
            result=runtime==null?SignalAspect.Stop:runtime.NativeAspect(proxy.ObjectId);return false;
        }
        static bool IsCtc(CTCSignal __instance,ref bool __result){if(!(__instance is RuntimeSignalProxy))return true;__result=true;return false;}
        static bool IsIntermediate(CTCSignal __instance,ref bool __result){if(!(__instance is RuntimeSignalProxy))return true;__result=false;return false;}
        static bool IntermediateForAi(CTCSignal signal)=>!(signal is RuntimeSignalProxy)&&signal.IsIntermediate;
        // The native stop branch uses IsIntermediate, not IsCTC. Replace that one call
        // so JIT inlining cannot turn an unowned native interlocking into Stop-and-Proceed.
        static IEnumerable<CodeInstruction> StrictStopBranch(IEnumerable<CodeInstruction> instructions) {
            var result=instructions.ToList();int matches=0,targets=0;
            var getter=typeof(CTCSignal).GetProperty("IsIntermediate").GetGetMethod();
            var replacement=typeof(SignalHooks).GetMethod(nameof(IntermediateForAi),BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var instruction in result)if((instruction.opcode==OpCodes.Call||instruction.opcode==OpCodes.Callvirt)&&Equals(instruction.operand,getter)){instruction.opcode=OpCodes.Call;instruction.operand=replacement;matches++;}
            if(matches!=1)throw new InvalidOperationException("AE-Haltzweig unerwartet: "+matches+" IsIntermediate-Aufrufe. Keine Signal-Freigabe.");
            var forecast=typeof(SignalHooks).GetMethod(nameof(ForecastTarget),BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var instruction in result)if((instruction.opcode==OpCodes.Call||instruction.opcode==OpCodes.Callvirt)&&Equals(instruction.operand,restriction)){instruction.opcode=OpCodes.Call;instruction.operand=forecast;targets++;}
            if(targets!=6)throw new InvalidOperationException("AE-Bremszielzweig unerwartet: "+targets+" Zielaufrufe. Keine Signal-Freigabe.");
            return result;
        }
        static bool Name(CTCSignal __instance,ref string __result){var proxy=__instance as RuntimeSignalProxy;if(proxy==null)return true;__result=runtime?.Code(proxy.ObjectId)??"Signaltechnik: Halt";return false;}
        static bool Throw(TrackNode __instance,bool __0)=>runtime==null||runtime.SwitchAllowed(__instance.id,__0);
        static bool SwitchMessage(Game.Messages.SetSwitch __0)=>runtime==null||runtime.SwitchAllowed(__0.NodeId,__0.Thrown);
        static void AiRead(CTCSignal __0,SignalAspect __1){var proxy=__0 as RuntimeSignalProxy;if(proxy!=null)runtime?.AiRead(proxy.ObjectId,__1);}
        static void Forecast(ref int? __0,ref int? __1,ref float __2,ref CTCSignal __3,ref bool __4){try{runtime?.ForecastRestriction(ref __0,ref __1,ref __2,ref __3,ref __4);}catch(Exception e){runtime?.ForecastFailure(e);}}
        static void ForecastTarget(Model.AI.AutoEngineerPlanner planner,int? speed,int? approach,float distance,CTCSignal signal,bool limited){
            if(signal is RuntimeSignalProxy proxy&&runtime?.IgnoreOwnSignal(proxy.ObjectId)==true){speed=null;approach=null;distance=0;signal=null;limited=false;}
            else Forecast(ref speed,ref approach,ref distance,ref signal,ref limited);
            setRestriction(planner,speed,approach,distance,signal,limited);
            if(speed==0&&Entry.WantsSignalEvents)runtime?.ObserveStopTarget(planner,signal,distance);
        }
        static void BeforeAiTargets(){runtime?.PublishNativeAspects();}
        public void Dispose(){runtime=null;if(harmony!=null){harmony.UnpatchAll(PatchId);harmony=null;}}
    }
    // Presentation must never prevent safety/control hooks from starting.
    public sealed class SignTooltipHooks : IDisposable {
        const string PatchId="enusde.signaltechnik.sign-tooltip";
        Harmony harmony;
        public static MethodInfo FindTarget()=>typeof(Game.Mow.SignController).GetMethod("PlayerSignTooltip",BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,null,new[]{typeof(Game.Mow.PlayerSign)},null);
        public void Start(Action<string> warning,Func<MethodInfo> resolve=null) {
            Dispose();try{var target=(resolve??FindTarget)();if(target==null||target.ReturnType!=typeof(TooltipInfo))throw new MissingMethodException("PlayerSignTooltip");harmony=new Harmony(PatchId);harmony.Patch(target,postfix:new HarmonyMethod(typeof(SignTooltipHooks),nameof(Tooltip)));}
            catch(Exception e){Dispose();warning?.Invoke("Schild-Tooltip unverändert: "+e.Message);}
        }
        static void Tooltip(Game.Mow.PlayerSign __0,ref TooltipInfo __result){Entry.FormatSignTooltip(__0,ref __result);}
        public void Dispose(){if(harmony!=null){harmony.UnpatchAll(PatchId);harmony=null;}}
    }
}
