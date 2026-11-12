using System;
using System.Collections.Generic;
using System.Reflection;
using EnusDE.Signaltechnik;

namespace UnityEngine {
    public class Object {public static void Destroy(Object value){}}
    public class GameObject:Object{}
    public class MonoBehaviour:Object{public GameObject gameObject=new GameObject();}
    public struct Vector2Int{public Vector2Int(int x,int y){}}
    public static class Time{public static double realtimeSinceStartup;}
}
namespace Railroader.Modding {public interface IModLogger{void Warning(string text);}}
namespace Game.State {public static class StateManager{public static bool IsUnloading;}}
namespace Game.Scripting.Modules.Railroader {
    public sealed class Car{public string id="fixture/1";}
    public static class World {
        public static int Reads;
        public static bool Fail;
        public static Car getSelectedCar(){Reads++;if(Fail)throw new Exception("fixture unavailable");return new Car();}
        public static Car getInspectorCar(){Reads++;return null;}
    }
}
namespace UI.Builder {
    public struct UIPanelBuilder {
        public enum Frequency{Periodic}
        public void VScrollView(Action<UIPanelBuilder> draw){WindowFixture.ScrollViews++;draw(this);}
        public void AddTitle(string a,string b){}
        public void AddLabel(string value){}
        public void AddField(string label,Func<string> getter,Frequency frequency){WindowFixture.Fields[label]=getter;WindowFixture.Elements++;}
        public void AddSection(string label,Action<UIPanelBuilder> draw){draw(this);}
        public void AddButton(string label,Action callback){WindowFixture.Buttons[label]=callback;WindowFixture.Elements++;}
    }
}
namespace UI.Windows {
    public class Window{public enum Position{CenterRight}}
    public sealed class CustomWindow:UnityEngine.MonoBehaviour{}
    public sealed class WindowConfig {public static WindowConfig Resizable(UnityEngine.Vector2Int a,UnityEngine.Vector2Int b,UnityEngine.Vector2Int c,Window.Position p)=>new WindowConfig();}
    public sealed class CustomWindowDefinition{public string Identifier,Title;public WindowConfig Config;public Action<UI.Builder.UIPanelBuilder> BuilderAction;}
    public sealed class WindowManager {
        public static WindowManager Shared=new WindowManager();
        readonly Dictionary<string,CustomWindowDefinition> _customDefinitions=new Dictionary<string,CustomWindowDefinition>();
        readonly Dictionary<string,CustomWindow> _customWindows=new Dictionary<string,CustomWindow>();
        public bool Open;
        public CustomWindowDefinition GetDefinition(string id){CustomWindowDefinition d;return _customDefinitions.TryGetValue(id,out d)?d:null;}
        public CustomWindow GetCustomWindow(string id){CustomWindow w;return _customWindows.TryGetValue(id,out w)?w:null;}
        public void RegisterCustomWindow(CustomWindowDefinition definition){_customDefinitions[definition.Identifier]=definition;}
        public void CloseWindow(string id){Open=false;}
        public void UnregisterActiveWindow(string id){}
        public void Build(string id) {
            _customWindows[id]=new CustomWindow();WindowFixture.Fields.Clear();WindowFixture.Buttons.Clear();WindowFixture.Elements=0;WindowFixture.ScrollPosition=0;
            _customDefinitions[id].BuilderAction(new UI.Builder.UIPanelBuilder());
        }
    }
    public sealed class CustomWindowHandle {
        string id;public static CustomWindowHandle Create(string id)=>new CustomWindowHandle{id=id};
        public bool IsOpen()=>WindowManager.Shared.Open;
        public void Toggle(){var m=WindowManager.Shared;m.Open=!m.Open;if(m.Open&&m.GetCustomWindow(id)==null)m.Build(id);}
        public void SetNeedsRebuild(){if(IsOpen())WindowManager.Shared.Build(id);}
    }
}
namespace EnusDE.Signaltechnik {
    public sealed class CategoryOptions {public bool Visible=true,Enabled=true;}
    public sealed class DiagnosticSettings {
        public static readonly string[] Categories={"vehicles","signals","blocks","switches","signs","game_log","performance"};
        public bool ShowRecorder=true,ShowSpeed=true,ShowPositions=true;
        readonly Dictionary<string,CategoryOptions> groups=new Dictionary<string,CategoryOptions>();
        public DiagnosticSettings(){foreach(string key in Categories)groups[key]=new CategoryOptions();}
        public CategoryOptions this[string key]=>groups[key];
        public DiagnosticSettings Clone(){var copy=new DiagnosticSettings{ShowRecorder=ShowRecorder,ShowSpeed=ShowSpeed,ShowPositions=ShowPositions};foreach(string key in Categories){copy[key].Enabled=this[key].Enabled;copy[key].Visible=this[key].Visible;}return copy;}
    }
    public static class SettingsMenu {public static string Caption(string key)=>key;}
    public static class Entry {
        public const string Version="fixture";public static bool IsRecording=true;public static int Value;
        public static DiagnosticSettings SettingsSnapshot()=>new DiagnosticSettings();
        public static string RecorderCommand(string command)=>command;
        public static Dictionary<string,string> DisplayData(string id)=>new Dictionary<string,string>{{"vehicle_status","ready"},{"name","name "+Value},{"kmh",Value.ToString()}};
    }
}
public static class WindowFixture {
    public static readonly Dictionary<string,Func<string>> Fields=new Dictionary<string,Func<string>>();
    public static readonly Dictionary<string,Action> Buttons=new Dictionary<string,Action>();
    public static int Elements,ScrollViews;public static double ScrollPosition;
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);Console.WriteLine("PASS: "+text);}
    static readonly MethodInfo update=typeof(DiagnosticWindow).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
    static void Tick(DiagnosticWindow window,int i){UnityEngine.Time.realtimeSinceStartup=i;update.Invoke(window,null);}
    public static void Main() {
        var window=new DiagnosticWindow();window.Initialize(null);Tick(window,0);
        Check(window.Builds==0&&Game.Scripting.Modules.Railroader.World.Reads==0,"closed native window creates no UI hierarchy and makes no selection reads");
        window.Command("toggle");int elements=Elements;ScrollPosition=.65;
        for(int i=1;i<=1200;i++){Entry.Value=i;Tick(window,i);foreach(var getter in Fields.Values)getter();}
        Check(window.Builds==1&&ScrollViews==1&&Elements==elements&&ScrollPosition==.65,"1200 timed updates change no UI hierarchy, bindings or scroll position");
        Check(Fields["Geschwindigkeit (km/h)"]()=="1200"&&Fields["Name"]()=="name 1200","existing text bindings display new cached values without rebuilding");
        Game.Scripting.Modules.Railroader.World.Fail=true;Tick(window,1201);
        bool cleared=Fields["Name"]()=="—"&&Fields["Messstatus"]().Contains("nicht verfügbar");
        Game.Scripting.Modules.Railroader.World.Fail=false;Tick(window,1202);
        Check(cleared&&Fields["Name"]()=="name 1200"&&window.Builds==1,"failed selection clears stale fields and recovery updates the same UI hierarchy");
        Buttons["Werte aktualisieren"]();Check(window.Builds==1&&ScrollPosition==.65,"manual value refresh also preserves the scroll hierarchy");
        var config=new DiagnosticSettings();config["vehicles"].Enabled=false;window.ApplySettings(config);int reads=Game.Scripting.Modules.Railroader.World.Reads;Tick(window,1203);
        Check(Game.Scripting.Modules.Railroader.World.Reads==reads&&window.Builds==1,"capture changes do not rebuild the window and disabled capture avoids selection reads");
        config.ShowSpeed=false;window.ApplySettings(config);Tick(window,1204);
        Check(window.Builds==2&&!Fields.ContainsKey("Geschwindigkeit (km/h)"),"an explicit visibility change rebuilds layout once");
        window.Command("toggle");reads=Game.Scripting.Modules.Railroader.World.Reads;Tick(window,1205);
        Check(window.Builds==2&&Game.Scripting.Modules.Railroader.World.Reads==reads,"closed window stops view sampling");
        var foreign=new UI.Windows.CustomWindowDefinition{Identifier="foreign/keep",BuilderAction=b=>{}};UI.Windows.WindowManager.Shared.RegisterCustomWindow(foreign);
        window.Shutdown();
        Check(UI.Windows.WindowManager.Shared.GetDefinition("enusde.signaltechnik.diagnose")==null&&ReferenceEquals(UI.Windows.WindowManager.Shared.GetDefinition("foreign/keep"),foreign),"shutdown unregisters only the owned native window and leaves foreign definitions intact");
        window.Shutdown();Check(true,"native window shutdown is idempotent");
    }
}
