using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using EnusDE.Signaltechnik;

public static class SignalApiTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    public static void Run() {
        var game=Assembly.Load("Railroader");var planner=game.GetType("Model.AI.AutoEngineerPlanner",true);var original=planner.GetMethod("UpdateTargets",BindingFlags.Instance|BindingFlags.NonPublic);
        var harmony=Assembly.Load("0Harmony");var code=harmony.GetType("HarmonyLib.CodeInstruction",true);
        var instructions=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(code));var ctor=code.GetConstructor(new[]{typeof(OpCode),typeof(object)});
        var table=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(o=>(ushort)o.Value);
        var il=original.GetMethodBody().GetILAsByteArray();
        for(int i=0;i<il.Length;) {
            ushort value=il[i++];if(value==0xfe)value=(ushort)(0xfe00|il[i++]);var op=table[value];int size;
            switch(op.OperandType){case OperandType.InlineNone:size=0;break;case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:size=1;break;case OperandType.InlineVar:size=2;break;case OperandType.InlineI8:case OperandType.InlineR:size=8;break;case OperandType.InlineSwitch:size=4+4*BitConverter.ToInt32(il,i);break;default:size=4;break;}
            object member=size==0?null:BitConverter.ToString(il,i,size);
            if(op.OperandType==OperandType.InlineMethod)member=original.Module.ResolveMethod(BitConverter.ToInt32(il,i));
            instructions.Add(ctor.Invoke(new[]{(object)op,member}));i+=size;
        }
        var rows=instructions.Cast<object>().ToArray();var operand=code.GetField("operand");var opcode=code.GetField("opcode");
        var before=rows.Select(r=>opcode.GetValue(r)+" "+operand.GetValue(r)).ToArray();
        Check(rows.Count(r=>(operand.GetValue(r) as MethodInfo)?.Name=="get_IsIntermediate")==1,"installed native AE evaluates exactly one IsIntermediate decision in its red-signal branch");
        var rewrite=typeof(SignalHooks).GetMethod("StrictStopBranch",BindingFlags.NonPublic|BindingFlags.Static);
        var after=((IEnumerable)rewrite.Invoke(null,new[]{instructions})).Cast<object>().ToArray();
        Check(after.Length==rows.Length&&after.Count(r=>(operand.GetValue(r) as MethodInfo)?.Name=="IntermediateForAi")==1&&after.Count(r=>(operand.GetValue(r) as MethodInfo)?.Name=="ForecastTarget")==6&&after.Select((r,i)=>before[i]!=opcode.GetValue(r)+" "+operand.GetValue(r)).Count(v=>v)==7,"production AE transpiler accepts real game IL and replaces exactly its stop classification and six native target call sites");
        var search=planner.GetMethod("Search",BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);var searchRows=Instructions(search,code,table);var signature=searchRows.Cast<object>().Select(r=>opcode.GetValue(r)+" "+operand.GetValue(r)).ToArray();
        var stock=typeof(StockSignalHooks).GetMethod("FilterSearch");var filtered=((IEnumerable)stock.Invoke(null,new[]{searchRows})).Cast<object>().ToArray();Check(filtered.Length==signature.Length&&filtered.Count(r=>(operand.GetValue(r) as MethodInfo)?.Name=="SignalForAi")==1&&filtered.Select((r,i)=>signature[i]!=opcode.GetValue(r)+" "+operand.GetValue(r)).Count(v=>v)==1,"stock invalidity hook accepts real AE search IL and filters only its single native signal-marker lookup");
        var further=planner.GetMethod("DistanceToNextSignalAfter",BindingFlags.Instance|BindingFlags.NonPublic);var furtherRows=Instructions(further,code,table);var furtherFiltered=((IEnumerable)stock.Invoke(null,new[]{furtherRows})).Cast<object>().ToArray();Check(furtherFiltered.Count(r=>(operand.GetValue(r) as MethodInfo)?.Name=="SignalForAi")==1,"native lookahead also skips explicitly invalidated stock signals instead of using them as next-signal boundaries");
        var empty=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(code));bool rejected=false;try{stock.Invoke(null,new object[]{empty});}catch(TargetInvocationException){rejected=true;}Check(rejected,"unknown stock-signal search shape rejects the AI exception instead of silently patching another operation");
        var state=game.GetType("Game.State.StateManager",true);var register=state.GetMethods().Single(m=>m.Name=="RegisterPropertyObject"&&m.GetParameters().Length==4&&m.GetParameters().Any(p=>p.ParameterType.FullName=="Game.AccessControl.AuthorizationRequirement"));Check(register.GetParameters().Any(p=>p.ParameterType.FullName=="Game.AccessControl.AuthorizationRequirement")&&state.GetMethod("TransactionScope").IsStatic&&state.GetProperty("HasRestoredProperties")!=null,"installed native shared-property API supports host authorization, restore readiness and static transactions");
        var propertyManager=game.GetType("Game.State.PropertyObjectManager",true);var restore=propertyManager.GetMethod("RegisterPropertyObject",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);var restoreRows=Instructions(restore,code,table);Check(restoreRows.Cast<object>().Any(r=>(operand.GetValue(r) as MethodInfo)?.Name=="ResetData"),"native late object registration reapplies received/saved property data via ResetData");
        var runtime=typeof(SignalRuntime);
        const BindingFlags local=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        string[] References(string method)=>Instructions(runtime.GetMethod(method,local),code,table).Cast<object>()
            .Select(r=>operand.GetValue(r) as MethodInfo).Where(m=>m?.DeclaringType==runtime).Select(m=>m.Name).ToArray();
        string[] Calls(MethodInfo method)=>Instructions(method,code,table).Cast<object>()
            .Select(r=>operand.GetValue(r) as MethodInfo).Where(m=>m!=null).Select(m=>m.DeclaringType.Name+"."+m.Name).ToArray();
        var reopen=Calls(runtime.GetMethod("ShowWorkspace",local));
        var window=game.GetType("UI.Windows.CustomWindow",true);
        Check(Calls(window.GetMethod("SetNeedsRebuild")).Contains("Window.get_IsShown")&&
            Array.IndexOf(reopen,"CustomWindowHandle.Show")>=0&&
            Array.IndexOf(reopen,"CustomWindowHandle.Show")<Array.IndexOf(reopen,"CustomWindowHandle.SetNeedsRebuild")&&
            reopen.Contains("SignalRuntime.CloseWorkspaceMode"),
            "cached workspaces reopen before rebuild is queued because the installed game ignores rebuild requests for hidden windows");
        Check(Calls(typeof(SharedRuntime).GetMethod("FinishSandbox",local)).Contains("StateManager.get_IsSandbox")&&
            Calls(typeof(SharedRuntime).GetMethod("FinishSandbox",local)).Contains("SharedRuntime.RequireReady"),
            "sandbox completion checks both the installed sandbox mode and host readiness at the command boundary");
        Check(Calls(typeof(SignalHooks).GetMethod("ForecastTarget",local)).Contains("SignalRuntime.ObserveStopTarget")&&
            planner.GetField("_locomotive",local)?.FieldType.FullName=="Model.BaseLocomotive",
            "passive stop-target trace uses the real planner locomotive field and does not replace another native decision");
        var batch=Calls(runtime.GetMethod("SaveNewRoutes",local));
        Check(batch.Contains("SignalRuntime.RequirePanelEdit")&&batch.Count(m=>m=="InterlockingStore.Save")==1&&!batch.Any(m=>m.StartsWith("InterlockingEngine.")),
            "confirmed proposals save all definitions in one editor operation without making a live route request");
        Check(Calls(runtime.GetMethod("BuildDispatcherNumberField",local)).Contains("SharedRuntime.FieldNumber")&&
            Calls(runtime.GetMethod("ReadEditorDeleteKey",local)).Contains("EventSystem.RaycastAll"),
            "dispatcher number form reads the live field registry and the Delete shortcut checks the frontmost pointed UI");
        var build=Calls(typeof(MarkerRuntime).GetMethod("ShowTool",local));
        Check(Array.IndexOf(build,"CustomWindowHandle.Show")>=0&&
            Array.IndexOf(build,"CustomWindowHandle.Show")<Array.IndexOf(build,"MarkerRuntime.RebuildTool"),
            "a cached build window opens before refreshing the page and its captured track position");
        var click=Calls(runtime.GetMethod("PressDispatcherSignal",local));
        var destination=Calls(runtime.GetMethod("RequestSelectedDestination",local));
        Check(click.Contains("SignalRuntime.CloseWorkspaceContext")&&click.Contains("SignalRuntime.RequestSelectedDestination")&&
            destination.Contains("RouteChains.ToTarget")&&destination.Contains("PanelButtons.ClearEnd"),
            "direct start and destination close the overlay and a rejected destination keeps the selected start for another attempt");
        Check(Calls(runtime.GetMethod("ChooseAuxiliary",local)).Contains("SignalRuntime.RequestSelectedDestination")&&
            Calls(typeof(SignalWorldBridge).GetMethod("PathValid")).Contains("AuxiliaryTarget.Accepts")&&
            Calls(typeof(SignalWorldBridge).GetMethod("PathValid")).Contains("AuxiliaryTarget.HasBlockingSignal")&&
            !References("RefreshProxies").Contains("AuxiliaryEndpoint"),
            "dispatcher auxiliary requests share the ordinary route engine, revalidate endpoint and intervening signals, and add no native signal proxy");
        var registration=References("EnsureWindow");
        Check(!Equals(runtime.GetField("WindowId",local).GetRawConstantValue(),runtime.GetField("EditorWindowId",local).GetRawConstantValue())
            &&registration.Contains("BuildDispatcherWindow")&&registration.Contains("BuildEditorWindow")
            &&References("BuildDispatcherWindow").Contains("BuildDispatcherFields")
            &&References("BuildEditorWindow").Contains("BuildEditorPanelContent")&&!References("BuildEditorWindow").Contains("CreateWorkspaceSurface")
            &&References("BuildEditorPanelContent").Contains("BuildEditorFields")&&References("BuildEditorPanelContent").Contains("BuildEditorCatalogue"),"editor and dispatcher register different native windows and use different control builders");
        Check(new[]{"EditPanel","CreateRoute","DeleteRoute","RefreshRoute","SaveTile","AcceptPoint"}.All(m=>References(m).Contains("RequirePanelEdit")),"layout and route definition writes require the active editor window");
        Check(new[]{"SetRoute","SetRoutePlan","CancelRoute","SwitchSelected","StartFrom"}.All(m=>References(m).Contains("RequireDispatcher")),"operating commands require the active dispatcher window");
    }
    static IList Instructions(MethodInfo method,Type code,System.Collections.Generic.Dictionary<ushort,OpCode> table){var result=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(code));var ctor=code.GetConstructor(new[]{typeof(OpCode),typeof(object)});var il=method.GetMethodBody().GetILAsByteArray();for(int i=0;i<il.Length;){ushort value=il[i++];if(value==0xfe)value=(ushort)(0xfe00|il[i++]);var op=table[value];int size;switch(op.OperandType){case OperandType.InlineNone:size=0;break;case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:size=1;break;case OperandType.InlineVar:size=2;break;case OperandType.InlineI8:case OperandType.InlineR:size=8;break;case OperandType.InlineSwitch:size=4+4*BitConverter.ToInt32(il,i);break;default:size=4;break;}object operand=size==0?null:BitConverter.ToString(il,i,size);if(op.OperandType==OperandType.InlineMethod)operand=method.Module.ResolveMethod(BitConverter.ToInt32(il,i));result.Add(ctor.Invoke(new[]{(object)op,operand}));i+=size;}return result;}
}
