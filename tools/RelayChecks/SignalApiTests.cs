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
        var switchBuilder=game.GetType("Track.TrackObjectBuilder",true).GetMethod("CreateSwitchStand",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
        var switchRows=Instructions(switchBuilder,code,table);
        var switchBefore=switchRows.Cast<object>().Select(r=>opcode.GetValue(r)+" "+operand.GetValue(r)).ToArray();
        var switchAfter=((IEnumerable)typeof(NativeSwitchModels).GetMethod("ChoosePrefab").Invoke(null,new object[]{switchRows})).Cast<object>().ToArray();
        Check(switchAfter.Length==switchBefore.Length&&switchAfter.Count(r=>(operand.GetValue(r) as MethodInfo)?.Name=="UseElectricModel")==1&&
            switchAfter.Select((r,i)=>switchBefore[i]!=opcode.GetValue(r)+" "+operand.GetValue(r)).Count(v=>v)==1,
            "electric model selection rewrites exactly the native prefab-choice getter without changing creation, animation or click binding");
        var emptySwitch=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(code));bool switchRejected=false;
        try{typeof(NativeSwitchModels).GetMethod("ChoosePrefab").Invoke(null,new object[]{emptySwitch});}catch(TargetInvocationException){switchRejected=true;}
        Check(switchRejected,"an unsupported native prefab selector is rejected instead of patching unrelated switch control");
        var choice=typeof(NativeSwitchModels).GetMethod("UseElectricModel",BindingFlags.Static|BindingFlags.NonPublic);
        Check(!Instructions(choice,code,table).Cast<object>().Any(r=>(operand.GetValue(r) as MethodInfo)?.Name.StartsWith("set_")==true),
            "electric stand presentation never writes logical CTC, unlock or thrown-state properties");
        var aiSwitch=planner.GetMethod("TrySetSwitch",BindingFlags.Instance|BindingFlags.NonPublic);
        var aiSwitchRows=Instructions(aiSwitch,code,table);var aiSwitchBefore=aiSwitchRows.Cast<object>().Select(r=>opcode.GetValue(r)+" "+operand.GetValue(r)).ToArray();
        var autoGate=typeof(AutomationHooks).GetMethod("GateCtc",BindingFlags.Static|BindingFlags.NonPublic);
        var aiSwitchAfter=((IEnumerable)autoGate.Invoke(null,new object[]{aiSwitchRows})).Cast<object>().ToArray();
        Check(aiSwitchAfter.Length==aiSwitchBefore.Length&&aiSwitchAfter.Count(r=>(operand.GetValue(r) as MethodInfo)?.Name=="IsCtc")==1&&
            aiSwitchAfter.Count(r=>(operand.GetValue(r) as MethodInfo)?.Name=="IsUnlocked")==1&&
            aiSwitchAfter.Select((r,i)=>aiSwitchBefore[i]!=opcode.GetValue(r)+" "+operand.GetValue(r)).Count(v=>v)==2,
            "automation turnout gate accepts installed planner IL and replaces exactly its two CTC decisions");
        bool gateRejected=false;try{autoGate.Invoke(null,new object[]{Activator.CreateInstance(typeof(List<>).MakeGenericType(code))});}catch(TargetInvocationException){gateRejected=true;}
        Check(gateRejected,"an unknown planner CTC shape prevents automatic switch gating instead of patching unrelated decisions");
        var privateStatic=BindingFlags.Static|BindingFlags.NonPublic;
        Check(!new[]{"IsCtc","IsUnlocked","BeforeSetSwitch"}.SelectMany(name=>Instructions(typeof(AutomationHooks).GetMethod(name,privateStatic),code,table).Cast<object>())
            .Any(r=>(operand.GetValue(r) as MethodInfo)?.Name.StartsWith("set_")==true),
            "AI-only turnout protection never rewrites native CTC or player unlock flags");
        var privateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        Check(game.GetType("Model.BaseLocomotive").GetProperty("AutoEngineerPlanner")!=null&&
            planner.GetField("_orders",privateInstance)?.FieldType.FullName=="Model.AI.Orders"&&
            planner.GetField("_routeTargetLocation",privateInstance)!=null&&planner.GetField("_route",privateInstance)!=null&&planner.GetField("_startStepIndex",privateInstance)!=null,
            "installed native locomotive and waypoint planner expose the exact order, target and route data used by contact automation");
        Check(game.GetType("RollingStock.Wheelset").GetField("wheels",privateInstance|BindingFlags.Public)!=null&&
            game.GetType("UI.CarInspector.CarInspector").GetField("_car",privateInstance)!=null&&
            game.GetType("UI.CarInspector.CarInspector").GetMethod("PopulateCarPanel",privateInstance,null,new[]{typeof(UI.Builder.UIPanelBuilder)},null)!=null,
            "native axle-test and locomotive info extensions match the installed wheel and inspector API");
        Check(!Instructions(typeof(ExistingSignDisplay).GetMethod("Refresh"),code,table).Cast<object>().Any(r=>
            (operand.GetValue(r) as MethodInfo)?.Name is "set_color" or "set_sharedMaterial")&&
            !Instructions(typeof(BakedSignDisplay).GetMethod("Refresh"),code,table).Cast<object>().Any(r=>(operand.GetValue(r) as MethodInfo)?.Name=="set_enabled"),
            "speed-text conversion retains original player colors and keeps native map renderers enabled");
        var prepareSign=typeof(BakedSignDisplay).GetMethod("Prepare",BindingFlags.Instance|BindingFlags.NonPublic);
        var prepareCalls=Instructions(prepareSign,code,table).Cast<object>().Select(r=>operand.GetValue(r) as MethodBase).Where(m=>m!=null).ToArray();
        Check(prepareCalls.Any(m=>m is ConstructorInfo&&m.DeclaringType.FullName=="UnityEngine.Material"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType.FullName=="UnityEngine.Material")&&
            !prepareCalls.Any(m=>m.Name=="SetColor"||m.Name=="set_enabled"||m.Name=="set_sharedMesh"),
            "world-sign conversion copies the native material and retains its shader and tint without hiding or rebuilding the native model");
        Check(prepareCalls.Any(m=>m.DeclaringType==typeof(SignFaceConversion)&&m.Name=="TryRead")&&
            prepareCalls.Any(m=>m.DeclaringType==typeof(NativeSignFace)&&m.Name=="Paint")&&
            typeof(UnityEngine.Mesh).GetMethod("GetVertexBuffer")!=null&&typeof(UnityEngine.Texture2D).GetMethod("ReadPixels",new[]{typeof(UnityEngine.Rect),typeof(int),typeof(int),typeof(bool)})!=null,
            "runtime world-sign preparation uses the tested per-face reader and atlas painter and the supplied game supports GPU mesh and texture reads");
        var runtime=typeof(SignalRuntime);
        const BindingFlags local=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        string[] References(string method)=>Instructions(runtime.GetMethod(method,local),code,table).Cast<object>()
            .Select(r=>operand.GetValue(r) as MethodInfo).Where(m=>m?.DeclaringType==runtime).Select(m=>m.Name).ToArray();
        string[] Calls(MethodInfo method)=>Instructions(method,code,table).Cast<object>()
            .Select(r=>operand.GetValue(r) as MethodInfo).Where(m=>m!=null).Select(m=>m.DeclaringType.Name+"."+m.Name).ToArray();
        var sharedRuntime=typeof(SharedRuntime);
        var installCommit=Calls(sharedRuntime.GetMethod("CommitInstall",local));
        var configurationCommit=Calls(sharedRuntime.GetMethod("Atomic",local));
        Check(Array.IndexOf(installCommit,"SharedDocuments.Write")<Array.IndexOf(installCommit,"SharedRuntime.BackupChanged")&&
            configurationCommit.Contains("SharedRuntime.BackupChanged"),
            "host configuration commits invoke the external backup path after native document publication and after complete multi-document transactions");
        var backupRestoreCalls=Calls(sharedRuntime.GetMethod("RestoreBackup",local));
        Check(Array.IndexOf(backupRestoreCalls,"SharedRuntime.RequireBackupRestore")<Array.IndexOf(backupRestoreCalls,"SharedDocuments.Replace")&&
            backupRestoreCalls.Contains("InstallationBackupData.RestoreFor")&&backupRestoreCalls.Contains("InstallationBackups.SaveJournal")&&backupRestoreCalls.Contains("Entry.ReloadBackupConfiguration"),
            "production restore enforces quiescent host operation, validates/rebases configuration, journals overwritten properties and refreshes runtime views");
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
        var routeBuild=Calls(typeof(SignalWorldBridge).GetMethod("BuildDesign",local));
        Check(routeBuild.Contains("RoutePointProtection.Bind")&&routeBuild.Contains("SignalRuntime.RoutePointGuards")&&
            Calls(runtime.GetMethod("RoutePointGuards",local)).Contains("SignalRuntime.ReadPointDefinition")&&
            Calls(typeof(SignalWorldBridge).GetMethod("PathValid")).Contains("RoutePointProtection.Current"),
            "native route construction uses the same live point definition as GFM and rejects saved protection after physical boundaries change");
        Check(batch.Contains("SignalRuntime.RequirePanelEdit")&&batch.Count(m=>m=="InterlockingStore.Save")==1&&!batch.Any(m=>m.StartsWith("InterlockingEngine.")),
            "confirmed proposals save all definitions in one editor operation without making a live route request");
        Check(Calls(runtime.GetMethod("BuildDispatcherNumberField",local)).Contains("SharedRuntime.FieldNumber")&&
            Calls(runtime.GetMethod("ReadEditorDeleteKey",local)).Contains("EventSystem.RaycastAll"),
            "dispatcher number form reads the live field registry and the Delete shortcut checks the frontmost pointed UI");
        var mirrorForm=Calls(runtime.GetMethod("BuildDispatcherNumberField",local));
        Check(Array.IndexOf(mirrorForm,"SignalRuntime.BuildNumberMirrorStatus")<Array.IndexOf(mirrorForm,"SharedRuntime.FieldNumber")&&
            Calls(runtime.GetMethod("ReadBlockPanel",local)).Contains("NumberMirrorIndex.Inputs")&&Calls(runtime.GetMethod("NumberCaption",local)).Contains("NumberMirrorIndex.Caption"),
            "live number copies use the read-only dispatcher branch and explicitly read remote source inputs through the tested mirror resolver");
        var build=Calls(typeof(MarkerRuntime).GetMethod("ShowTool",local));
        Check(Array.IndexOf(build,"CustomWindowHandle.Show")>=0&&
            Array.IndexOf(build,"CustomWindowHandle.Show")<Array.IndexOf(build,"MarkerRuntime.RebuildTool"),
            "a cached build window opens before refreshing the page and its captured track position");
        var click=Calls(runtime.GetMethod("PressDispatcherSignal",local));
        var destination=Calls(runtime.GetMethod("RequestSelectedDestination",local));
        Check(click.Contains("SignalRuntime.CloseWorkspaceContext")&&click.Contains("SignalRuntime.RequestSelectedDestination")&&
            destination.Contains("SignalRuntime.QueueDestination")&&!destination.Contains("SignalRuntime.DiscoverAutomaticPlans")&&destination.Contains("PanelButtons.ClearEnd")&&destination.Contains("PanelButtons.Clear")&&
            Calls(runtime.GetMethod("QueueDestination",local)).Contains("FrameWorkQueue.Enqueue"),
            "direct start and destination close the overlay and enqueue preparation without synchronously calculating the entire chain or retaining the completed button pair");
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
        var entryCalls=Calls(typeof(Entry).GetMethod("OnModLoad"));
        var picker=typeof(Entry).Assembly.GetType("EnusDE.Signaltechnik.ModePicker",true);
        var settingsWindow=typeof(Entry).Assembly.GetType("EnusDE.Signaltechnik.SettingsWindow",true);
        var entryConstructors=Instructions(typeof(Entry).GetMethod("OnModLoad"),code,table).Cast<object>()
            .Select(r=>operand.GetValue(r) as ConstructorInfo).Where(m=>m!=null);
        Check(entryConstructors.Any(c=>c.DeclaringType==settingsWindow)&&
            !entryCalls.Any(c=>c.Contains("PreferencesRegistration")||c.Contains("CompanySettingsRegistration"))&&
            Calls(typeof(Entry).GetMethod("OpenSettings")).Contains("SettingsWindow.Open")&&
            Calls(picker.GetMethod("SelectSettings",local)).Contains("ModePicker.Close")&&
            !Calls(picker.GetMethod("SelectSettings",local)).Any(c=>c.Contains("CanOpen")||c.Contains("OpenWorkspaceMode"))&&
            Calls(settingsWindow.GetMethod("Open",local,null,new[]{typeof(bool)},null)).Contains("WindowManager.RegisterCustomWindow"),
            "launcher settings use their own native window without patching Company or preferences pages or gating access on workspace permissions");
        var automatic=Calls(runtime.GetMethod("CommitAutomaticPlan",local));
        var sharedAdmission=Calls(runtime.GetMethod("AdmitAutomaticPlan",local));
        var centralRequest=Calls(runtime.GetMethod("AdmitRoutePlan",local));
        var prepare=Calls(runtime.GetMethod("PrepareRequestedPlan",local));
        Check(automatic.Contains("SignalRuntime.RequireDispatcher")&&automatic.Contains("SignalRuntime.AdmitAutomaticPlan")&&
            sharedAdmission.Contains("SignalRuntime.PrepareRequestedPlan")&&sharedAdmission.Contains("SignalRuntime.AdmitRoutePlan")&&
            prepare.Contains("SignalWorldBridge.PathValid")&&prepare.Contains("SharedRuntime.ConnectRoutePoints")&&
            Calls(runtime.GetMethod("ApplyRoutePlan",local)).Contains("SignalRuntime.RequireDispatcher")&&
            centralRequest.Contains("StationAutomationPolicy.RequireControl")&&centralRequest.Contains("InterlockingEngine.RequestChain")&&
            Calls(runtime.GetMethod("SetRoutePlan",local)).Contains("SignalRuntime.QueueSavedPlan"),
            "calculated dispatcher routes revalidate topology and material connections before entering the guarded engine without mandatory catalogue persistence");
        var webSet=Calls(runtime.GetMethod("SetWebRoute",local));
        var webPreview=Calls(runtime.GetMethod("QueueWebPreview",local));
        var webField=Calls(runtime.GetMethod("OpenWebField",local));
        Check(webSet.Contains("SignalRuntime.RequireEdit")&&webSet.Contains("SignalRuntime.get_WebMayDispatch")&&
            webSet.Contains("SignalRuntime.AdmitAutomaticPlan")&&webPreview.Contains("FrameWorkQueue.Enqueue")&&
            !webPreview.Contains("SignalRuntime.AdmitAutomaticPlan"),"browser preview queues planning and only explicit confirmation enters shared route admission");
        Check(webField.Contains("SignalRuntime.RequireEdit")&&webField.Contains("SignalRuntime.BeginPanelSignalPick")&&
            webField.Contains("SignalRuntime.OpenEditorField")&&Calls(runtime.GetMethod("Shutdown")).Contains("SignalRuntime.Cleanup"),
            "browser opens native editor and world assignment after game authorization");
        var server=runtime.Assembly.GetType("EnusDE.Signaltechnik.InterlockingWebServer",true);
        bool Private(string address)=>(bool)server.GetMethod("PrivateAddress",local).Invoke(null,new object[]{System.Net.IPAddress.Parse(address)});
        Check(Private("192.168.1.20")&&Private("10.0.0.4")&&Private("172.31.8.8")&&!Private("172.32.0.1")&&!Private("8.8.8.8")&&!Private("0.0.0.0"),
            "home network binding accepts private addresses only");
        using(var html=runtime.Assembly.GetManifestResourceStream("EnusDE.Interlocking.html"))
            Check(html!=null&&html.Length>1000,"mod assembly contains the browser UI and needs no separate web asset installation");
        Check(Calls(runtime.GetMethod("TickAutomation",local)).Contains("SignalRuntime.PrepareRequestedPlan")&&
            Calls(runtime.GetMethod("TickAutomation",local)).Contains("InterlockingEngine.RequestChain")&&
            Calls(runtime.GetMethod("TickAutomation",local)).Contains("SignalRuntime.TryPreparedAutomaticRoutes")&&
            !Calls(runtime.GetMethod("TickAutomation",local)).Contains("SignalRuntime.DiscoverAutomaticPlans"),
            "contact automation consumes queued preparation then uses normal current-data chain admission without synchronous path discovery");
        Check(Calls(runtime.GetMethod("Update",local)).Contains("SignalRuntime.TickRouteWork")&&Calls(runtime.GetMethod("Shutdown")).Contains("SignalRuntime.CancelRouteWork")&&
            Calls(runtime.GetMethod("BuildDispatcherFields",local)).Contains("SignalRuntime.BuildRouteWorkControls"),
            "the frame loop advances queued work, shutdown cancels it and the dispatcher exposes pending preparation controls");
        var pointCommand=Calls(typeof(SignalWorldBridge).GetMethod("SetSwitch"));
        Check(pointCommand.Contains("TrainController.TrySetSwitch")&&!pointCommand.Contains("StateManager.ApplyLocal")&&
            !pointCommand.Any(name=>name.EndsWith(".set_IsCTCSwitch")||name.EndsWith(".set_IsCTCSwitchUnlocked")),
            "dispatcher point movement uses the native guarded command without clearing CTC ownership or issuing unchecked state changes");
        Check(new[]{"SwitchSelected","SwitchBound"}.All(m=>References(m).Contains("RequireDispatcher"))&&
            References("ReleaseWorkspaceCanvas").Contains("DismissEditorMenu"),
            "direct and contextual point commands retain dispatcher authorization and editor menus are dismissed with their owning workspace");
        var popup=game.GetType("UI.PopupMenu",true);
        Check(popup.GetMethods().Any(m=>m.Name=="PresentAt"&&m.IsStatic)&&popup.GetMethods().Any(m=>m.Name=="PresentUnder"&&m.IsStatic)&&popup.GetMethod("Dismiss")!=null,
            "installed game provides the native popup positioning and owned-menu dismissal API used by editor categories");
        Check(new[]{"EditPanel","CreateRoute","DeleteRoute","RefreshRoute","SaveTile","AcceptPoint"}.All(m=>References(m).Contains("RequirePanelEdit")),"manual layout and route definition editing requires the active editor window");
        Check(new[]{"SetRoute","SetRoutePlan","CancelRoute","SwitchSelected","StartFrom"}.All(m=>References(m).Contains("RequireDispatcher")),"operating commands require the active dispatcher window");
    }
    static IList Instructions(MethodInfo method,Type code,System.Collections.Generic.Dictionary<ushort,OpCode> table){var result=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(code));var ctor=code.GetConstructor(new[]{typeof(OpCode),typeof(object)});var il=method.GetMethodBody().GetILAsByteArray();for(int i=0;i<il.Length;){ushort value=il[i++];if(value==0xfe)value=(ushort)(0xfe00|il[i++]);var op=table[value];int size;switch(op.OperandType){case OperandType.InlineNone:size=0;break;case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:size=1;break;case OperandType.InlineVar:size=2;break;case OperandType.InlineI8:case OperandType.InlineR:size=8;break;case OperandType.InlineSwitch:size=4+4*BitConverter.ToInt32(il,i);break;default:size=4;break;}object operand=size==0?null:BitConverter.ToString(il,i,size);if(op.OperandType==OperandType.InlineMethod)operand=method.Module.ResolveMethod(BitConverter.ToInt32(il,i));result.Add(ctor.Invoke(new[]{(object)op,operand}));i+=size;}return result;}
}
