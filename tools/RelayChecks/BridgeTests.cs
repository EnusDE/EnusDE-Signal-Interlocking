using System;

using EnusDE.Signaltechnik;
using Game.Scripting.Modules.Railroader;
using Railroader.Modding;
using Relay;
using Relay.Modules;
using Relay.Native;
using System.Reflection;

public static class BridgeTests {
    sealed class Provider : IModuleProvider {
        public string ModuleName=>"railroader";
        public bool IsSingleton=>true;
        public NativeModule CreateModule()=>RailroaderModule.Shared;
    }
    sealed class Context : IModContext {
        public string ModIdentifier=>"enusde/signaltechnik";
        public string ModDirectory=>"test";
        public IModLogger Log=>new Logger();
        public bool CheckDependency(string expression)=>true;
    }
    sealed class Logger : IModLogger {
        public void Debug(string text){} public void Info(string text){} public void Warning(string text){}
        public void Error(string text){throw new Exception(text);}public void Error(Exception problem,string text){throw new Exception(text,problem);}
    }
    public static void Run() {
        ModuleRegistry.RegisterProvider(new Provider());
        typeof(Entry).GetMethod("RegisterBridge",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{RailroaderModule.Shared});
        var entry=new Entry();
        var vm=new Interpreter();vm.RegisterModule(Relay.StandardLibrary.CoreModule.Shared);
        vm.RegisterNamedModule(ModuleRegistry.GetModule("railroader"));
        vm.Interpret("from railroader import enusdeRecorderStatus, enusdeRecorderCommand, enusdeVehicleSample, enusdeDiagnosticWindow, enusdeGrenzzeichen\nvar status = enusdeRecorderStatus()\nvar text = enusdeRecorderCommand(\"status\")\nvar sample = enusdeVehicleSample(\"missing\")\nvar window = enusdeDiagnosticWindow(\"toggle\")\nvar marker = enusdeGrenzzeichen(\"open\")", "actual-game-module-bridge.rl");
        var state=vm.GetGlobal("status").AsDictionary();
        if(state[Value.String("recording")].AsBool() || !vm.GetGlobal("text").AsString().Contains("nicht bereit"))throw new Exception("Bridge did not return the inactive native state.");
        if(vm.GetGlobal("sample").Type!=Relay.ValueType.Null||!state.ContainsKey(Value.String("capture_signs")))throw new Exception("Settings/sample bridge missing.");
        if(!vm.GetGlobal("window").AsString().Contains("nicht bereit"))throw new Exception("Window bridge failed without an active world.");
        if(!vm.GetGlobal("marker").AsString().Contains("nicht bereit"))throw new Exception("Marker bridge must remain inactive without a loaded world.");
        entry.OnModUnload();
        System.Console.WriteLine("PASS: recorder bridge imports through the actual installed game's railroader module template");
    }
}
