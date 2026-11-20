using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using EnusDE.Signaltechnik;

// Executes production menu and native-hook callbacks against small data-only fixtures.
namespace HarmonyLib {
    public sealed class CodeInstruction {public OpCode opcode;public object operand;public CodeInstruction(OpCode opcode,object operand=null){this.opcode=opcode;this.operand=operand;}public bool Calls(MethodInfo method)=>Equals(operand,method);}
    public static class AccessTools {
        public static MethodInfo Method(Type type,string name)=>type.GetMethod(name,BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
        public static FieldInfo Field(Type type,string name)=>type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
    }
    public sealed class HarmonyMethod {public MethodInfo Method;public HarmonyMethod(Type type,string name){Method=type.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);}}
    public sealed class Harmony {
        readonly string id;public static readonly List<string> Owners=new List<string>();
        public Harmony(string value){id=value;}
        public void Patch(MethodInfo target,HarmonyMethod prefix=null,HarmonyMethod postfix=null,HarmonyMethod finalizer=null,HarmonyMethod transpiler=null){if(target==null)throw new Exception("Missing target");Owners.Add(id);}
        public void UnpatchAll(string owner){Owners.RemoveAll(o=>o==owner);}
    }
}
namespace UnityEngine {}
public struct PickableActivateEvent {}
public sealed class SwitchStandClick {public Track.TrackNode node;public void Activate(PickableActivateEvent value){}}
namespace Helpers {}
namespace Game.State {}
namespace Railroader.Modding {}
namespace Track {
    public struct Location {public string Id;public bool IsValid=>Id!=null;}
    public sealed class TrackNode {public string id;public bool isThrown{get;set;}public bool NativeCtc,NativeUnlocked;public bool IsCTCSwitch=>NativeCtc;public bool IsCTCSwitchUnlocked=>NativeUnlocked;}
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
namespace Model {public sealed class Car {public bool IsLocomotive=true;}}
namespace UI.Builder {public struct UIPanelBuilder {}public sealed class UITabbedPanelBuilder {public void AddTab(string title,string id,Action<UIPanelBuilder> draw){draw(new UIPanelBuilder());}}}
namespace UI.CarInspector {public sealed class CarInspector {Model.Car _car;void PopulatePanel(){Action<UI.Builder.UITabbedPanelBuilder> draw=tabs=>{if(_car!=null)_car.IsLocomotive=true;};draw(new UI.Builder.UITabbedPanelBuilder());}}}
namespace Model.AI {public sealed class AutoEngineerPlanner {public int? LastSpeed;public Track.Signals.CTCSignal LastSignal;public int Calls;void CallSignalIfNeeded(Track.Signals.CTCSignal signal,Track.Signals.SignalAspect aspect){}void UpdateTargets(float speed,int direction){}void SetSignalSpeedRestriction(int? speed,int? approach,float distance,Track.Signals.CTCSignal signal,bool limited){LastSpeed=speed;LastSignal=signal;Calls++;}bool TrySetSwitch(Track.TrackNode node,bool thrown)=>true;void SetSwitchThrown(Track.TrackNode node,bool thrown){}static bool CheckThrowable(Track.TrackNode node,TrainController trains,System.Collections.Generic.ICollection<Model.Car> cars,ref int reason)=>true;}}
public struct TooltipInfo {}
public sealed class TrainController {void HandleSetSwitch(Game.Messages.SetSwitch change){}}
namespace UI.ContextMenu {
    public enum ContextMenuQuadrant {General}
    public sealed class ContextMenu {
        public readonly List<Tuple<string,Action>> Items=new List<Tuple<string,Action>>();
        public void Clear(){Items.Clear();}
        public void Show(string title){}
        public void AddButton(ContextMenuQuadrant quadrant,string title,Action action){Items.Add(Tuple.Create(title,action));}
    }
    public static class TrackContextMenu {public static bool TryShow(Track.Location at)=>true;}
}
namespace EnusDE.Signaltechnik {
    public static class LocalText {public static string Select(string english,string german)=>german;}
    public static class Entry {public static bool WantsSignalEvents=>false;public static void FormatSignTooltip(Game.Mow.PlayerSign sign,ref TooltipInfo result){}}
    public sealed class SignalRuntime {
        public bool Permit,Locked=true;
        public bool Ignore;
        public bool IgnoreOwnSignal(string id)=>Ignore;
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
        bool available=true,picking=false;string chosen=null,at=null;int warnings=0;
        var contribution=new TrackMenuIntegration();contribution.Start(()=>available,(location,kind)=>{chosen=kind;at=location.Id;},text=>warnings++,()=>picking,node=>{if(!picking)return false;chosen="direct-point";at=node.id;return true;});
        var menu=new UI.ContextMenu.ContextMenu();menu.AddButton(UI.ContextMenu.ContextMenuQuadrant.General,"Foreign",()=>{});
        Call(typeof(TrackMenuIntegration),"BeforeShow",menu);Check(menu.Items.Count==1,"non-track context menus retain their original contents without signal entries");
        object[] begin={new Track.Location{Id="clicked-segment"},false,null};Call(typeof(TrackMenuIntegration),"Begin",begin);
        Call(typeof(TrackMenuIntegration),"BeforeShow",menu);Call(typeof(TrackMenuIntegration),"BeforeShow",menu);
        Check(menu.Items.Count==7&&menu.Items[0].Item1=="Foreign","track menu adds construction, linking, direct point editing, detection and automation contact actions without disturbing stock entries");
        menu.Items.First(i=>i.Item1=="Signaltechnik · Baumenü").Item2();
        Check(chosen=="build"&&at=="clicked-segment","the compact track menu forwards the captured track position to construction");
        menu.Items.First(i=>i.Item1=="Weiche zuordnen").Item2();Check(chosen=="assign-point"&&at=="clicked-segment","direct turnout assignment remains available from the same track click");
        menu.Items.First(i=>i.Item1=="Weiche bearbeiten").Item2();Check(chosen=="edit-point"&&at=="clicked-segment","direct point editing forwards the world point without activating diagram assignment");
        menu.Items.First(i=>i.Item1=="Weichen-GFM-Grenzen aktivieren").Item2();Check(chosen=="point-circuit-enable"&&at=="clicked-segment","one-action point detection uses the captured world click without requiring a point-list selection");
        menu.Items.First(i=>i.Item1=="Automatik-Kontakt platzieren").Item2();Check(chosen=="automation-contact"&&at=="clicked-segment","automation contact placement forwards the clicked native track location");
        var originalError=new Exception("original");var returned=Call(typeof(TrackMenuIntegration),"End",originalError,begin[2]);
        Check(ReferenceEquals(originalError,returned),"track-menu finalizer preserves the game's original exception");
        var other=new UI.ContextMenu.ContextMenu();Call(typeof(TrackMenuIntegration),"BeforeShow",other);Check(other.Items.Count==0,"track-menu scope is cleared after completion or exception");
        available=false;begin=new object[]{new Track.Location{Id="S"},false,null};Call(typeof(TrackMenuIntegration),"Begin",begin);Call(typeof(TrackMenuIntegration),"BeforeShow",other);Call(typeof(TrackMenuIntegration),"End",null,begin[2]);
        Check(other.Items.Count==0,"unavailable host/editor state prevents context placement entries");
        available=true;picking=true;begin=new object[]{new Track.Location{Id="pick-only"},false,null};bool nativeMenu=(bool)Call(typeof(TrackMenuIntegration),"Begin",begin);Call(typeof(TrackMenuIntegration),"End",null,begin[2]);
        Check(!nativeMenu&&(bool)begin[1]&&other.Items.Count==0&&chosen=="assign-point"&&at=="pick-only","point assignment consumes the first track click without opening any confirmation menu");
        Check(!(bool)Call(typeof(TrackMenuIntegration),"BeforePointActivate",new Track.TrackNode{id="direct-stand"})&&chosen=="direct-point"&&at=="direct-stand","clicking the actual point stand assigns it without native operation or a second click");picking=false;
        Check((bool)Call(typeof(TrackMenuIntegration),"BeforePointActivate",new Track.TrackNode{id="ordinary-stand"}),"outside assignment mode ordinary point-stand clicks retain their native behaviour");
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
        var planner=new Model.AI.AutoEngineerPlanner();runtime.Ignore=true;
        Call(typeof(SignalHooks),"ForecastTarget",planner,0,15,10f,proxy,true);
        Check(planner.Calls==1&&planner.LastSpeed==null&&planner.LastSignal==null,"a previously cached stop target for an ignored own signal is withdrawn through the native restriction setter");
        runtime.Ignore=false;Call(typeof(SignalHooks),"ForecastTarget",planner,0,null,10f,proxy,true);
        Check(planner.Calls==2&&planner.LastSpeed==0&&ReferenceEquals(planner.LastSignal,proxy),"returning a signal to service restores native Stop treatment instead of retaining the bypass");
        var automation=new AutomationHooks();
        automation.Start(id=>WaypointSwitchPolicy.Protect(id=="electric",false,false,()=>false),(_,car)=>{});
        var electric=new Track.TrackNode{id="electric"};var hand=new Track.TrackNode{id="hand"};
        Check(!(bool)Call(typeof(AutomationHooks),"BeforeSetSwitch",electric)&&(bool)Call(typeof(AutomationHooks),"BeforeSetSwitch",hand),"native waypoint switch setter cannot move configured electric points with block coordination and optional protection disabled, but can move ordinary hand points");
        Check((bool)Call(typeof(AutomationHooks),"IsCtc",electric)&&!(bool)Call(typeof(AutomationHooks),"IsUnlocked",electric),"native waypoint planning treats own electric points as unavailable for AI throwing, regardless of signal aspect or requested speed");
        hand.NativeCtc=true;hand.NativeUnlocked=true;
        Check((bool)Call(typeof(AutomationHooks),"IsCtc",hand)&&(bool)Call(typeof(AutomationHooks),"IsUnlocked",hand),"unrelated native CTC points retain their original AI unlock rules");
        Check(WaypointSwitchPolicy.Protect(true,false,false,()=>throw new Exception("not needed"))&&!WaypointSwitchPolicy.Protect(false,false,true,()=>true)&&WaypointSwitchPolicy.Protect(false,true,true,()=>true),"electric point ownership is unconditional while additional mapped-point protection retains its explicit block-setting requirements");
        var ctcCall=new HarmonyLib.CodeInstruction(OpCodes.Callvirt,typeof(Track.TrackNode).GetProperty("IsCTCSwitch").GetGetMethod());
        var unlockedCall=new HarmonyLib.CodeInstruction(OpCodes.Callvirt,typeof(Track.TrackNode).GetProperty("IsCTCSwitchUnlocked").GetGetMethod());
        var gates=((IEnumerable<HarmonyLib.CodeInstruction>)Call(typeof(AutomationHooks),"GateCtc",(object)new[]{ctcCall,unlockedCall})).ToArray();
        Check(gates.All(i=>i.opcode==OpCodes.Call)&&((MethodInfo)gates[0].operand).Name=="IsCtc"&&((MethodInfo)gates[1].operand).Name=="IsUnlocked","native waypoint transpiler gates both CTC ownership and unlock checks");
        Check(AutomationHooks.SwitchSearchTarget()!=null,"electric point protection also patches the native look-ahead throwability test, so a wrong point can become a stop target instead of merely refusing its throw command");
        rejected=false;try{Call(typeof(AutomationHooks),"GateCtc",(object)Array.Empty<HarmonyLib.CodeInstruction>());}catch(TargetInvocationException){rejected=true;}
        Check(rejected,"unknown native waypoint switch code rejects hook startup instead of silently losing electric-point protection");
        automation.Dispose();
        Call(typeof(SignalHooks),"BeforeAiTargets");Check(runtime.AiCalls==2,"AI target evaluation refreshes own published signal values before the native planner executes");
        hooks.Dispose();aspect=new object[]{proxy,Track.Signals.SignalAspect.Clear};Call(typeof(SignalHooks),"Aspect",aspect);
        Check((Track.Signals.SignalAspect)aspect[1]==Track.Signals.SignalAspect.Stop&&HarmonyLib.Harmony.Owners.Count==0,"hook shutdown leaves no authority callback or owned patches behind");
    }
}
