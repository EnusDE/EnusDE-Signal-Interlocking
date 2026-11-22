using System;
using System.IO;
using System.Collections.Generic;
using Relay;
using Relay.Native;
using Relay.Types;

static class CommandTests {
    sealed class Ui{}
    static void Check(bool value,string name){if(!value)throw new Exception("FAIL: "+name);System.Console.WriteLine("PASS: "+name);}
    public static void Run(string path) {
        var vm=new Interpreter();vm.RegisterModule(Relay.StandardLibrary.CoreModule.Shared);
        var commands=new Dictionary<string,Value>();int toggles=0,statuses=0,markerCalls=0;string markerAction=null;string packAction=null;int packCalls=0;bool recording=true;
        var uiType=new NativeClassDefinition<Ui>("CommandUi").Method("registerCommand",3,(u,a)=>{commands[a[0].AsString()]=a[2];return Value.Null();});
        var fixture=new NativeModule("railroader").DefineClass(uiType)
            .DefineConstant("ui",Value.TypeInstance(new TypeInstance(uiType.CreateTypeDefinition(),new Ui())))
            .DefineFunction("enusdeDiagnosticWindow",1,a=>{if(a[0].AsString()=="toggle")toggles++;else if(a[0].AsString()=="status")statuses++;return Value.String("window");})
            .DefineFunction("enusdeAssetPacks",1,a=>{packCalls++;packAction=a[0].AsString();return Value.String("packs");})
            .DefineFunction("enusdeGrenzzeichen",1,a=>{markerCalls++;markerAction=a[0].AsString();return Value.String("marker");})
            .DefineFunction("enusdeRecorderCommand",1,a=>{if(a[0].AsString()=="aus")recording=false;if(a[0].AsString()=="an")recording=true;return Value.String(recording?"an":"aus");});
        vm.RegisterNamedModule(fixture);vm.Interpret(File.ReadAllText(path),path);vm.CallFunction("onWorldLoad",new[]{Value.Null()});
        Check(commands.Count==5&&toggles==0&&markerCalls==0&&packCalls==0,"world load registers five commands without opening tools or starting placement");
        Value Invoke(string name,params Value[] args){vm.SetGlobal("__command",commands[name]);return vm.CallFunction("__command",new[]{Value.Array(new List<Value>(args))});}
        Invoke("enus_assets");Check(packAction=="status","pack console defaults to read-only status without activation");
        Invoke("enus_assets",Value.String("unlock:enusde.runenwacht"));Check(packAction=="unlock:enusde.runenwacht"&&packCalls==2,"explicit package unlock arguments reach the native host gate unchanged");
        Invoke("enus_signal");Invoke("enus_signal");
        Check(toggles==2,"diagnostic command toggles the native stable window");
        Invoke("enus_signal_status");Check(statuses==1&&toggles==2,"status command does not open or rebuild the window");
        Invoke("enus_signal_record",Value.String("aus"));Check(!recording,"record command pauses local recording");
        Invoke("enus_signal_record",Value.String("an"));Check(recording,"record command resumes local recording");
        Invoke("enus_grenzzeichen");Check(markerAction=="open","marker command opens its tool without implicitly placing an object");
        Invoke("enus_grenzzeichen",Value.String("setzen"));Check(markerAction=="setzen"&&markerCalls==2,"explicit marker placement is forwarded to the separate P0 module");
        vm.CallFunction("onWorldUnload",new[]{Value.Null()});vm.CallFunction("onWorldLoad",new[]{Value.Null()});
        Check(commands.Count==5&&toggles==2&&markerCalls==2,"world unload/reload leaves native window lifecycle to the native entry point");
        for(int i=0;i<100;i++)Invoke("enus_signal_status");long before=GC.GetTotalMemory(true);
        for(int i=0;i<3000;i++)Invoke("enus_signal_status");long retained=GC.GetTotalMemory(true)-before;
        Check(retained<1024*1024,"3000 real-Relay command calls retain less than 1 MiB after collection (no automatic UI coroutine)");
    }
}
