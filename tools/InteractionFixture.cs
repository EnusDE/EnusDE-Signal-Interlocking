using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using EnusDE.Signaltechnik;

// Executes production menu and native-hook callbacks against small data-only fixtures.
namespace HarmonyLib {
    public sealed class CodeInstruction {public OpCode opcode;public object operand;public CodeInstruction(OpCode opcode,object operand=null){this.opcode=opcode;this.operand=operand;}}
    public sealed class HarmonyMethod {public MethodInfo Method;public HarmonyMethod(Type type,string name){Method=type.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);}}
    public sealed class Harmony {
        readonly string id;public static readonly List<string> Owners=new List<string>();
        public Harmony(string value){id=value;}
        public void Patch(MethodInfo target,HarmonyMethod prefix=null,HarmonyMethod postfix=null,HarmonyMethod finalizer=null,HarmonyMethod transpiler=null){if(target==null)throw new Exception("Missing target");Owners.Add(id);}
        public void UnpatchAll(string owner){Owners.RemoveAll(o=>o==owner);}
    }
}
namespace UnityEngine {}
namespace Helpers {}
namespace Game.State {}
namespace Railroader.Modding {}
namespace Track {
    public struct Location {public string Id;public bool IsValid=>Id!=null;}
    public sealed class TrackNode {public string id;public bool isThrown{get;set;}}
}
namespace Track.Signals {
    public enum SignalAspect {Stop,Clear,DivergingClear,Restricting}
    public abstract class CTCSignal {
        protected enum StopReason {NoRoute}
        protected abstract SignalAspect CalculateAspect(out StopReason reason);
        void OnEnable(){}
        public void SetNeedsUpdate(){}
        public void ShowAspect(SignalAspect value){}
        SignalAspect AspectDisplayedBySignal(CTCSignal value)=>SignalAspect.Stop;
        public SignalAspect LastShownAspect{get;private set;}
        public bool IsCTC=>false;
        public bool IsIntermediate=>true;
        public string DisplayName=>"original";
    }
}
namespace Game.Messages {public struct SetSwitch {public string NodeId;public bool Thrown;}}
namespace Game.Mow {
    public struct PlayerSign {}
    public sealed class SignController {public static TooltipInfo PlayerSignTooltip(PlayerSign sign)=>new TooltipInfo();}
}
namespace Model.AI {public sealed class AutoEngineerPlanner {void CallSignalIfNeeded(Track.Signals.CTCSignal signal,Track.Signals.SignalAspect aspect){}void UpdateTargets(float speed,int direction){}void SetSignalSpeedRestriction(int? speed,int? approach,float distance,Track.Signals.CTCSignal signal,bool limited){}}}
public struct TooltipInfo {}
public sealed class TrainController {void HandleSetSwitch(Game.Messages.SetSwitch change){}}
namespace UI.ContextMenu {
    public enum ContextMenuQuadrant {General}
    public sealed class ContextMenu {
        public readonly List<Tuple<string,Action>> Items=new List<Tuple<string,Action>>();
        public void Show(string title){}
        public void AddButton(ContextMenuQuadrant quadrant,string title,Action action){Items.Add(Tuple.Create(title,action));}
    }
    public static class TrackContextMenu {public static bool TryShow(Track.Location at)=>true;}
}
namespace EnusDE.Signaltechnik {
    public static class Entry {public static bool WantsSignalEvents=>false;public static void FormatSignTooltip(Game.Mow.PlayerSign sign,ref TooltipInfo result){}}
    public sealed class SignalRuntime {
        public bool Permit,Locked=true;
        public int AiCalls;
        public Track.Signals.SignalAspect NativeAspect(string id)=>Permit?Track.Signals.SignalAspect.Clear:Track.Signals.SignalAspect.Stop;
        public string Code(string id)=>"A1";
        public bool SwitchAllowed(string id,bool thrown)=>!Locked||thrown;
        public void AiRead(string id,Track.Signals.SignalAspect aspect){AiCalls++;}
        public void PublishNativeAspects(){AiCalls++;}
        public void ForecastRestriction(ref int? speed,ref int? approach,ref float distance,ref Track.Signals.CTCSignal signal,ref bool limited){}
        public void ForecastFailure(Exception error){}
        public void ObserveStopTarget(Model.AI.AutoEngineerPlanner planner,Track.Signals.CTCSignal signal,float distance){}
    }
}
public static class InteractionFixture {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);Console.WriteLine("PASS: "+text);}
    static object Call(Type type,string method,params object[] values)=>type.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,values);
    sealed class OriginalSignal:Track.Signals.CTCSignal {protected override Track.Signals.SignalAspect CalculateAspect(out StopReason reason){reason=StopReason.NoRoute;return Track.Signals.SignalAspect.Stop;}}
    public static void Main() {
        bool available=true;string chosen=null,at=null;int warnings=0;
        var contribution=new TrackMenuIntegration();contribution.Start(()=>available,(location,kind)=>{chosen=kind;at=location.Id;},text=>warnings++);
        var menu=new UI.ContextMenu.ContextMenu();menu.AddButton(UI.ContextMenu.ContextMenuQuadrant.General,"Foreign",()=>{});
        Call(typeof(TrackMenuIntegration),"BeforeShow",menu);Check(menu.Items.Count==1,"non-track context menus retain their original contents without signal entries");
        object[] begin={new Track.Location{Id="clicked-segment"},null};Call(typeof(TrackMenuIntegration),"Begin",begin);
        Call(typeof(TrackMenuIntegration),"BeforeShow",menu);Call(typeof(TrackMenuIntegration),"BeforeShow",menu);
        Check(menu.Items.Count==4&&menu.Items[0].Item1=="Foreign","track menu adds a single build entry and two direct linking actions without disturbing stock entries");
        menu.Items.First(i=>i.Item1=="Signaltechnik · Baumenü").Item2();
        Check(chosen=="build"&&at=="clicked-segment","the compact track menu forwards the captured track position to construction");
        menu.Items.First(i=>i.Item1=="Weiche zuordnen").Item2();Check(chosen=="assign-point"&&at=="clicked-segment","direct turnout assignment remains available from the same track click");
        var originalError=new Exception("original");var returned=Call(typeof(TrackMenuIntegration),"End",originalError,begin[1]);
        Check(ReferenceEquals(originalError,returned),"track-menu finalizer preserves the game's original exception");
        var other=new UI.ContextMenu.ContextMenu();Call(typeof(TrackMenuIntegration),"BeforeShow",other);Check(other.Items.Count==0,"track-menu scope is cleared after completion or exception");
        available=false;begin=new object[]{new Track.Location{Id="S"},null};Call(typeof(TrackMenuIntegration),"Begin",begin);Call(typeof(TrackMenuIntegration),"BeforeShow",other);Call(typeof(TrackMenuIntegration),"End",null,begin[1]);
        Check(other.Items.Count==0,"unavailable host/editor state prevents context placement entries");
        contribution.Dispose();chosen=null;menu.Items[1].Item2();Check(chosen==null&&HarmonyLib.Harmony.Owners.Count==0&&warnings==0,"menu unload removes only its patches and invalidates old placement callbacks");
        var runtime=new SignalRuntime();var hooks=new SignalHooks();hooks.Start(runtime);
        int critical=HarmonyLib.Harmony.Owners.Count;int tooltipWarnings=0;var optional=new SignTooltipHooks();optional.Start(text=>tooltipWarnings++,()=>null);
        Check(tooltipWarnings==1&&HarmonyLib.Harmony.Owners.Count==critical,"failed optional sign-tooltip lookup cannot disable the independent native signal hooks");
        optional.Start(text=>tooltipWarnings++);Check(SignTooltipHooks.FindTarget().IsStatic&&HarmonyLib.Harmony.Owners.Count>critical,"optional tooltip hook accepts the real static method shape");optional.Dispose();
        var proxy=new RuntimeSignalProxy{ObjectId="own"};var original=new OriginalSignal();
        Check(!(bool)Call(typeof(SignalHooks),"IntermediateForAi",proxy)&&(bool)Call(typeof(SignalHooks),"IntermediateForAi",original),"own signals are absolute stops, while native intermediate signals retain Stop-and-Proceed semantics");
        var call=new HarmonyLib.CodeInstruction(OpCodes.Callvirt,typeof(Track.Signals.CTCSignal).GetProperty("IsIntermediate").GetGetMethod());
        var setter=typeof(Model.AI.AutoEngineerPlanner).GetMethod("SetSignalSpeedRestriction",BindingFlags.Instance|BindingFlags.NonPublic);var fixture=new[]{call}.Concat(Enumerable.Range(0,6).Select(_=>new HarmonyLib.CodeInstruction(OpCodes.Call,setter))).ToArray();
        var rewritten=((IEnumerable<HarmonyLib.CodeInstruction>)Call(typeof(SignalHooks),"StrictStopBranch",(object)fixture)).ToArray();
        Check(rewritten.Length==7&&rewritten[0].opcode==OpCodes.Call&&((MethodInfo)rewritten[0].operand).Name=="IntermediateForAi"&&rewritten.Skip(1).All(c=>((MethodInfo)c.operand).Name=="ForecastTarget"),"production transpiler replaces the native stop decision and every forecast target call without depending on getter or setter inlining");
        bool rejected=false;try{Call(typeof(SignalHooks),"StrictStopBranch",(object)Array.Empty<HarmonyLib.CodeInstruction>());}catch(TargetInvocationException){rejected=true;}
        Check(rejected,"unexpected native stop-branch shape rejects hook startup instead of silently allowing permissive red running");
        object[] aspect={proxy,Track.Signals.SignalAspect.Clear};bool proceed=(bool)Call(typeof(SignalHooks),"Aspect",aspect);
        Check(!proceed&&(Track.Signals.SignalAspect)aspect[1]==Track.Signals.SignalAspect.Stop,"own native signal reports stop when no route authority is available");
        runtime.Permit=true;aspect=new object[]{proxy,Track.Signals.SignalAspect.Stop};Call(typeof(SignalHooks),"Aspect",aspect);
        Check((Track.Signals.SignalAspect)aspect[1]==Track.Signals.SignalAspect.Clear,"own native signal exposes its interlocking authority through the game's aspect accessor");
        aspect=new object[]{proxy,Track.Signals.SignalAspect.Stop};Call(typeof(SignalHooks),"LastAspect",aspect);
        Check((Track.Signals.SignalAspect)aspect[1]==Track.Signals.SignalAspect.Clear,"the actual AI LastShownAspect access path receives the same authority as the visible signal");
        proxy.Publish(Track.Signals.SignalAspect.Clear);Check(proxy.LastShownAspect==Track.Signals.SignalAspect.Clear,"own proxy also publishes its real backing value for native code that inlines LastShownAspect");
        aspect=new object[]{original,Track.Signals.SignalAspect.Restricting};proceed=(bool)Call(typeof(SignalHooks),"Aspect",aspect);
        Check(proceed&&(Track.Signals.SignalAspect)aspect[1]==Track.Signals.SignalAspect.Restricting,"original game signals are never replaced or recolored by the native aspect hook");
        Check(!(bool)Call(typeof(SignalHooks),"Enable",proxy)&&(bool)Call(typeof(SignalHooks),"Enable",original),"only own proxies bypass native storage subscriptions; original signal lifecycle continues");
        var node=new Track.TrackNode{id="W"};
        Check(!(bool)Call(typeof(SignalHooks),"Throw",node,false)&&(bool)Call(typeof(SignalHooks),"Throw",node,true),"central switch-setter hook rejects contrary moves while allowing the locked position");
        Check(!(bool)Call(typeof(SignalHooks),"SwitchMessage",new Game.Messages.SetSwitch{NodeId="W",Thrown=false}),"normal switch message path is also blocked before mutation");
        Call(typeof(SignalHooks),"AiRead",original,Track.Signals.SignalAspect.Stop);Call(typeof(SignalHooks),"AiRead",proxy,Track.Signals.SignalAspect.Clear);
        Check(runtime.AiCalls==1,"AI observation records only the planner's calls for own signals");
        Call(typeof(SignalHooks),"BeforeAiTargets");Check(runtime.AiCalls==2,"AI target evaluation refreshes own published signal values before the native planner executes");
        hooks.Dispose();aspect=new object[]{proxy,Track.Signals.SignalAspect.Clear};Call(typeof(SignalHooks),"Aspect",aspect);
        Check((Track.Signals.SignalAspect)aspect[1]==Track.Signals.SignalAspect.Stop&&HarmonyLib.Harmony.Owners.Count==0,"hook shutdown leaves no authority callback or owned patches behind");
    }
}
