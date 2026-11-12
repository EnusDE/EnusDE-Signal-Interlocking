using System;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using Relay;
using Relay.Native;
using Relay.Types;

// Runs the shipped Relay source in the installed game's real Relay VM.
// Only the game-facing objects are fixtures; no game write API is provided.
static class BehaviorTests {
    sealed class Car { public string Id="test/lok-4711", Name="Prueflok 4711"; public bool Loco=true; public float Speed=10; }
    sealed class World { public bool Host=true, Fail; public Car Selected, Inspector; }
    sealed class Window { public Value Build; public bool Open, Dirty, Unavailable; }
    sealed class TaskHandle { public bool Stopped; }
    sealed class Builder { }
    sealed class Ui { }
    static Interpreter vm;
    static World world;
    static Window window;
    static TaskHandle task;
    static bool recording=true, capture=true, showVehicles=true, showSpeed=true, showPositions=true;
    static int selectionReads;
    static float realTime=0;
    static readonly Builder builder=new Builder();
    static readonly List<(Value function, Value[] args)> pending=new List<(Value, Value[])>();
    static readonly Dictionary<string,Value> commands=new Dictionary<string,Value>();
    static readonly Dictionary<string,Value> buttons=new Dictionary<string,Value>();
    static readonly List<string> labels=new List<string>();
    static readonly Dictionary<string,string> fields=new Dictionary<string,string>();
    static NativeClassDefinition<Car> carType;
    static NativeClassDefinition<World> worldType;
    static NativeClassDefinition<Window> windowType;
    static NativeClassDefinition<TaskHandle> taskType;
    static NativeClassDefinition<Builder> builderType;
    static NativeClassDefinition<Ui> uiType;
    static Value Wrap<T>(NativeClassDefinition<T> type,T item) => item==null?Value.Null():Value.TypeInstance(new TypeInstance(type.CreateTypeDefinition(),item));
    static void Check(bool result,string name) { if(!result)throw new Exception("FAIL: "+name); System.Console.WriteLine("PASS: "+name); }
    static void Invoke(Value function,params Value[] args) { vm.SetGlobal("__test_callback",function); vm.CallFunction("__test_callback",args); }
    static string Command(string name) { Invoke(commands[name],Value.Array(new List<Value>())); return vm.GetGlobal("__command_result").ToString(); }
    static string Status() => vm.CallFunction("describeStatus",new Value[0]).ToString();
    static void Build() {
        fields.Clear(); labels.Clear(); buttons.Clear(); pending.Clear();
        Invoke(window.Build,Wrap(builderType,builder));
        while(pending.Count>0) { var item=pending[0];pending.RemoveAt(0);Invoke(item.function,item.args); }
    }
    public static void Run(string path,bool memory=false) {
        vm=new Interpreter(); world=new World();
        vm.RegisterModule(Relay.StandardLibrary.CoreModule.Shared);
        carType=new NativeClassDefinition<Car>("FixtureCar")
            .Property("id",c=>Value.String(c.Id),null)
            .Property("name",c=>Value.String(c.Name),null)
            .Property("isLocomotive",c=>Value.Bool(c.Loco),null)
            .Property("speedMph",c=>Value.Float(c.Speed),null)
            .Property("locationFront",c=>Value.String("<test-segment, A -> 12.500>"),null)
            .Property("locationRear",c=>Value.String("<test-segment, A -> 2.500>"),null);
        worldType=new NativeClassDefinition<World>("FixtureWorld")
            .Property("isHost",w=>{if(w.Fail)throw new Exception("world unavailable");return Value.Bool(w.Host);},null)
            .Method("getSelectedCar",w=>{selectionReads++;return Wrap(carType,w.Selected);})
            .Method("getInspectorCar",w=>Wrap(carType,w.Inspector));
        windowType=new NativeClassDefinition<Window>("FixtureWindow")
            .Method("size",2,(w,a)=>Wrap(windowType,w))
            .Method("show",w=>{w.Open=true;return Value.Null();})
            .Method("close",w=>{if(w.Unavailable)throw new Exception("window manager unloaded");w.Open=false;return Value.Null();})
            .Method("isOpen",w=>{if(w.Unavailable)throw new Exception("window manager unloaded");return Value.Bool(w.Open);})
            .Method("toggle",w=>{w.Open=!w.Open;return Value.Null();})
            .Method("setNeedsRebuild",w=>{w.Dirty=true;return Value.Null();});
        taskType=new NativeClassDefinition<TaskHandle>("FixtureTask")
            .Method("stop",t=>{t.Stopped=true;return Value.Null();});
        builderType=new NativeClassDefinition<Builder>("FixtureBuilder")
            .Method("title",2,(b,a)=>{labels.Add(a[0].AsString());return Value.Null();})
            .Method("label",(b,a)=>{labels.Add(a.AsString());return Value.Null();})
            .Method("field",(b,a,v)=>{fields[a.AsString()]=v.AsString();return Value.Null();})
            .Method("section",(b,title,fn)=>{pending.Add((fn,new[]{Wrap(builderType,b)}));return Value.Null();})
            .Method("hStack",(b,fn)=>{pending.Add((fn,new[]{Wrap(builderType,b)}));return Value.Null();})
            .Method("vScrollView",(b,fn)=>{pending.Add((fn,new[]{Wrap(builderType,b)}));return Value.Null();})
            .Method("button",(b,title,fn)=>{buttons[title.AsString()]=fn;return Value.Null();});
        uiType=new NativeClassDefinition<Ui>("FixtureUi")
            .Method("createWindow",3,(u,a)=>{window=new Window{Build=a[2]};return Wrap(windowType,window);})
            .Method("registerCommand",3,(u,a)=>{commands[a[0].AsString()]=a[2];return Value.Null();});
        var fixtures=new NativeModule("railroader")
            .DefineClass(carType).DefineClass(worldType).DefineClass(windowType).DefineClass(taskType).DefineClass(builderType).DefineClass(uiType)
            .DefineConstant("world",Wrap(worldType,world)).DefineConstant("ui",Wrap(uiType,new Ui()))
            .DefineFunction("enusdeRecorderStatus",0,a=>Value.Dictionary(new Dictionary<Value,Value>{
                                {Value.String("capture_vehicles"),Value.Bool(capture)}, {Value.String("show_vehicles"),Value.Bool(showVehicles)},
                {Value.String("show_speed"),Value.Bool(showSpeed)}, {Value.String("show_positions"),Value.Bool(showPositions)},
                {Value.String("show_recorder"),Value.Bool(true)}, {Value.String("suppressed"),Value.Integer(0)},
                {Value.String("show_signals"),Value.Bool(false)}, {Value.String("show_blocks"),Value.Bool(false)},
                {Value.String("show_switches"),Value.Bool(false)}, {Value.String("show_signs"),Value.Bool(false)},
                {Value.String("show_game_log"),Value.Bool(false)}, {Value.String("show_performance"),Value.Bool(false)},
                {Value.String("recording"),Value.Bool(recording)}, {Value.String("written"),Value.Integer(100)},
                {Value.String("locomotives"),Value.Integer(2)}, {Value.String("cars"),Value.Integer(42)},
                {Value.String("signals"),Value.Integer(15)}, {Value.String("dropped"),Value.Integer(0)}, {Value.String("error"),Value.String("")}
            }))
            .DefineFunction("enusdeVehicleSample",1,a=>{
                var car=world.Selected??world.Inspector;if(car==null||car.Id!=a[0].AsString())return Value.Null();
                return Value.Dictionary(new Dictionary<Value,Value>{
                    {Value.String("id"),Value.String(car.Id)}, {Value.String("name"),Value.String(car.Name)},
                    {Value.String("speedMph"),Value.String(car.Speed.ToString(System.Globalization.CultureInfo.InvariantCulture))},
                    {Value.String("speedKmh"),Value.String((car.Speed*1.609344).ToString(System.Globalization.CultureInfo.InvariantCulture))},
                    {Value.String("front"),Value.String("<test-segment, A -> 12.500>")},
                    {Value.String("rear"),Value.String("<test-segment, A -> 2.500>")},
                    {Value.String("sampleTime"),Value.String("2026-11-08T00:00:00Z")},
                    {Value.String("locomotive"),Value.Bool(car.Loco)}
                });
            })
            .DefineFunction("enusdeRecorderCommand",1,a=>{string value=a[0].AsString();if(value=="aus")recording=false;if(value=="an")recording=true;return Value.String("Recorder "+(recording?"aktiv":"aus"));});
        vm.RegisterNamedModule(fixtures);
        vm.RegisterModule(new NativeModule("fixtureClock")
            .DefineFunction("realtimeSinceStartup",0,a=>Value.Float(realTime))
            .DefineFunction("Coroutine",1,a=>{task=new TaskHandle();return Wrap(taskType,task);}));
        vm.Interpret(File.ReadAllText(path),path);

        vm.CallFunction("onWorldLoad",new[]{Value.Null()});vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(!window.Open && commands.Count==3 && fields["Instanz"]=="Host" && labels.Any(t=>t.Contains("Kein Fahrzeug")),"startup stays in background and registers three console commands");
        Invoke(commands["enus_signal"],Value.Array(new List<Value>()));Check(window.Open,"console command opens the initially hidden window");

        world.Selected=new Car();vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(fields["Fahrzeug-ID"]=="test/lok-4711" && Math.Abs(float.Parse(fields["Geschwindigkeit (km/h)"],System.Globalization.CultureInfo.InvariantCulture)-16.09344f)<0.0001f && fields["Gleisposition vorne"].Contains("test-segment"),"selected locomotive, track position and mph-to-kmh conversion");

        world.Selected=null;world.Inspector=new Car{Id="test/inspector"};vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(fields["Fahrzeug-ID"]=="test/inspector","inspector fallback when selection is empty");

        world.Host=false;vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(fields["Instanz"]=="Client","client can read diagnostics without any world write API");

        world.Inspector=null;vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(!fields.ContainsKey("Fahrzeug-ID") && Status().Contains("Kein Fahrzeug"),"cleared selection removes stale locomotive data");

        world.Fail=true;vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(Status().Contains("Diagnosefehler") && !fields.ContainsKey("Geschwindigkeit (km/h)") && labels.Any(t=>t.Contains("world unavailable")),"unavailable world clears stale values and displays a diagnostic error");

        world.Fail=false;world.Selected=new Car();vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(Status().Contains("test/lok-4711") && !Status().Contains("Diagnosefehler"),"diagnostics recover after world access returns");

        int beforeReads=selectionReads;capture=false;vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(selectionReads==beforeReads&&!fields.ContainsKey("Fahrzeug-ID")&&labels.Any(t=>t.Contains("Erfassung")||t.Contains("erfassung")),"disabled vehicle capture prevents selection reads and removes stale values");
        capture=true;showVehicles=false;vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(selectionReads==beforeReads&&!fields.ContainsKey("Fahrzeug-ID"),"hidden vehicle display does not query selection while background recording remains enabled");
        showVehicles=true;showSpeed=false;showPositions=false;vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(fields.ContainsKey("Fahrzeug-ID")&&!fields.ContainsKey("Geschwindigkeit (km/h)")&&!fields.ContainsKey("Gleisposition vorne"),"speed and position display options hide only their selected fields");
        showSpeed=showPositions=true;vm.CallFunction("refreshWindow",new Value[0]);Build();
        Invoke(buttons["Schließen"]);Check(!window.Open,"close button callback hides the diagnostic window");
        Invoke(commands["enus_signal"],Value.Array(new List<Value>()));Check(window.Open,"registered console command reopens the diagnostic window");
        vm.CallFunction("onWorldUnload",new[]{Value.Null()});
        Check(task.Stopped && !window.Open && vm.GetGlobal("snapshot").Type==Relay.ValueType.Null,"world unload closes UI, stops coroutine and clears vehicle state");
        var oldWindow=window;vm.CallFunction("onWorldLoad",new[]{Value.Null()});vm.CallFunction("refreshWindow",new Value[0]);Build();
        Check(commands.Count==3 && !oldWindow.Open && !window.Open,"second session re-registers three commands and stays hidden");
        Invoke(commands["enus_signal_record"],Value.Array(new List<Value>{Value.String("aus")}));Build();
        Check(!recording && fields["Aufzeichnung"]=="Aus","console command pauses only the local recorder");
        Invoke(commands["enus_signal_record"],Value.Array(new List<Value>{Value.String("an")}));Build();
        Check(recording && fields["Aufzeichnung"].StartsWith("Aktiv"),"console command resumes local recording while the window stays closed");
        vm.CallFunction("onWorldUnload",new[]{Value.Null()});
        vm.CallFunction("onWorldLoad",new[]{Value.Null()});window.Unavailable=true;
        Check(!vm.CallFunction("windowIsOpenSafe",new Value[0]).AsBool(),"unloaded window manager is handled safely during visibility checks");
        vm.CallFunction("onWorldUnload",new[]{Value.Null()});
        Check(task.Stopped&&vm.GetGlobal("snapshot").Type==Relay.ValueType.Null,"world unload tolerates an already destroyed window manager");
        if(memory) {
            vm.CallFunction("onWorldLoad",new[]{Value.Null()});world.Selected=new Car();
            for(int i=0;i<100;i++){vm.CallFunction("refreshWindow",new Value[0]);Build();}
            long start=GC.GetTotalMemory(true);
            for(int phase=1;phase<=3;phase++) {
                for(int i=0;i<1000;i++){world.Selected.Speed=i;vm.CallFunction("refreshWindow",new Value[0]);Build();}
                long held=GC.GetTotalMemory(true);
                System.Console.WriteLine("MEMORY: builds="+(phase*1000)+" retained_delta="+(held-start)+" total_managed="+held);
            }
            vm.CallFunction("onWorldUnload",new[]{Value.Null()});
        }
        System.Console.WriteLine("Behavior checks completed in the installed Relay VM; fixtures are not an in-game test.");
    }
}
