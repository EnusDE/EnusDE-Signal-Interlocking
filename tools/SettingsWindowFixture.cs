using System;
using System.Collections.Generic;
using System.Linq;
using EnusDE.Signaltechnik;

// Exercises the production window and picker callbacks with controlled native UI
// stand-ins. It does not claim to render Unity or exercise multiplayer transport.
namespace UnityEngine {
    public class Object {public static int Destroyed;public static void Destroy(Object value){if(value!=null)Destroyed++;}}
    public class GameObject:Object {
        readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
        public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components[typeof(T)]=c;return c;}
        public T GetComponent<T>() where T:class=>components.TryGetValue(typeof(T),out var c)?c as T:null;
        public void SetActive(bool value){}
    }
    public class Component:Object {public GameObject gameObject;public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();}
    public class MonoBehaviour:Component{}
    public class RectTransform:Component {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta;}
    public struct Vector2 {public Vector2(float x,float y){}public static Vector2 zero=>new Vector2();}
    public struct Vector2Int {public Vector2Int(int x,int y){}}
    public struct Color {public Color(float r,float g,float b){}}
    public struct Color32 {public Color32(byte r,byte g,byte b,byte a){}}
    public enum TextureFormat{RGBA32}public enum HideFlags{HideAndDontSave}public enum FilterMode{Bilinear}public enum TextureWrapMode{Clamp}
    public sealed class Texture2D:Object {
        public string name;public HideFlags hideFlags;public FilterMode filterMode;public TextureWrapMode wrapMode;
        public Texture2D(int w,int h,TextureFormat format,bool mipmaps){}
        public void SetPixels32(Color32[] pixels){}public void Apply(bool mipmaps,bool unreadable){}
    }
}
namespace UnityEngine.EventSystems {
    public class BaseEventData{}public class PointerEventData:BaseEventData{}
    public interface IPointerEnterHandler{void OnPointerEnter(PointerEventData data);}public interface IPointerExitHandler{void OnPointerExit(PointerEventData data);}
    public interface ISelectHandler{void OnSelect(BaseEventData data);}public interface IDeselectHandler{void OnDeselect(BaseEventData data);}
}
namespace UnityEngine.UI {
    public sealed class Button:UnityEngine.Component {public bool interactable;public Action Click;public void Press(){if(interactable)Click();}}
    public sealed class RawImage:UnityEngine.Component {public UnityEngine.Texture2D texture;public UnityEngine.Color color;public bool raycastTarget;}
    public sealed class LayoutElement:UnityEngine.Component {public float minWidth,preferredWidth,flexibleWidth,minHeight,preferredHeight;public bool ignoreLayout;}
    public sealed class HorizontalLayoutGroup:UnityEngine.Component {public float spacing;public bool childControlWidth,childControlHeight,childForceExpandWidth,childForceExpandHeight;}
}
namespace Game.State {public static class StateManager {public static bool IsUnloading;}}
namespace UI.Builder {
    public struct UIPanelBuilder {
        public enum Frequency{Periodic}
        public static readonly List<string> Labels=new List<string>();
        public void VScrollView(Action<UIPanelBuilder> draw){draw(this);}
        public void AddTitle(string text,string subtitle){Labels.Add(text);}
        public void AddLabel(string text){Labels.Add(text);}
        public void AddLabel(Func<string> read,Frequency frequency){Labels.Add(read());}
        public UnityEngine.RectTransform HStack(Action<UIPanelBuilder> draw){draw(this);var rect=EnusDE.Signaltechnik.WorkspaceSurface.Rect(null,"");rect.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();return rect;}
    }
    public sealed class FakeText {public UnityEngine.GameObject gameObject=new UnityEngine.GameObject();}
    public sealed class UIBuilderAssets {
        public enum ButtonStyle{Compact}
        public static UIBuilderAssets Shared=new UIBuilderAssets();
        public static readonly List<UnityEngine.UI.Button> Buttons=new List<UnityEngine.UI.Button>();
        public Tuple<UnityEngine.RectTransform,FakeText> CreateButton(ButtonStyle style,UnityEngine.RectTransform parent,Action click){
            var rect=EnusDE.Signaltechnik.WorkspaceSurface.Rect(parent,"");var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.Click=click;Buttons.Add(button);
            return Tuple.Create(rect,new FakeText());
        }
    }
}
namespace UI.Windows {
    public class Window {public enum Position{CenterRight}}
    public sealed class CustomWindow:UnityEngine.MonoBehaviour{}
    public sealed class WindowConfig {public static WindowConfig Resizable(UnityEngine.Vector2Int a,UnityEngine.Vector2Int b,UnityEngine.Vector2Int c,Window.Position p)=>new WindowConfig();}
    public sealed class CustomWindowDefinition {public string Identifier,Title;public WindowConfig Config;public Action<UI.Builder.UIPanelBuilder> BuilderAction;}
    public sealed class WindowManager {
        public static WindowManager Shared=new WindowManager();
        readonly Dictionary<string,CustomWindowDefinition> _customDefinitions=new Dictionary<string,CustomWindowDefinition>();
        readonly Dictionary<string,CustomWindow> _customWindows=new Dictionary<string,CustomWindow>();
        readonly HashSet<string> open=new HashSet<string>();
        public int Builds;
        public CustomWindowDefinition GetDefinition(string id)=>_customDefinitions.TryGetValue(id,out var definition)?definition:null;
        public CustomWindow GetCustomWindow(string id)=>_customWindows.TryGetValue(id,out var window)?window:null;
        public void RegisterCustomWindow(CustomWindowDefinition definition){_customDefinitions[definition.Identifier]=definition;}
        public void CloseWindow(string id){open.Remove(id);}
        public void UnregisterActiveWindow(string id){open.Remove(id);}
        public bool IsOpen(string id)=>open.Contains(id);
        public void Show(string id){open.Add(id);if(GetCustomWindow(id)==null)Build(id);}
        public void Build(string id){Builds++;if(GetCustomWindow(id)==null)_customWindows[id]=new UnityEngine.GameObject().AddComponent<CustomWindow>();_customDefinitions[id].BuilderAction?.Invoke(new UI.Builder.UIPanelBuilder());}
    }
    public sealed class CustomWindowHandle {
        string id;public static CustomWindowHandle Create(string id)=>new CustomWindowHandle{id=id};
        public bool IsOpen()=>WindowManager.Shared.IsOpen(id);
        public void Show(){WindowManager.Shared.Show(id);}
        public void Toggle(){if(IsOpen())WindowManager.Shared.CloseWindow(id);else Show();}
        public void SetNeedsRebuild(){if(IsOpen())WindowManager.Shared.Build(id);}
    }
}
namespace EnusDE.Signaltechnik {
    public sealed class BuildToolbar:IDisposable {
        public static Action Toggle;public static int Ensures;
        public BuildToolbar(Action toggle){Toggle=toggle;}
        public void Ensure(string caption){Ensures++;}public void Dispose(){}
    }
    public sealed class ModePickerLanguage {public bool ReadGerman()=>LocalText.German;}
    public static class WorkspaceSurface {
        public static UnityEngine.Color Ink=>new UnityEngine.Color(1,1,1);
        public static UnityEngine.RectTransform Rect(UnityEngine.RectTransform parent,string name)=>new UnityEngine.GameObject().AddComponent<UnityEngine.RectTransform>();
    }
}
public static class SettingsWindowFixture {
    static void Check(bool value,string text){if(!value)throw new Exception("FAIL: "+text);Console.WriteLine("PASS: "+text);}
    public static void Main() {
        const string id=SettingsWindow.Id;
        LocalText.Configure(()=>"en");
        int draws=0,warnings=0,modes=0;string draft="existing local settings";
        var window=new SettingsWindow(_=>{draws++;if(draft==null)throw new Exception("draft lost");},_=>warnings++);
        Check(UI.Windows.WindowManager.Shared.GetDefinition(id)==null,"settings construction registers no window and patches no game menu");
        window.Open();var manager=UI.Windows.WindowManager.Shared;var owned=manager.GetDefinition(id);int firstDraws=draws;
        Check(manager.IsOpen(id)&&firstDraws>0&&UI.Builder.UIPanelBuilder.Labels.Contains("Settings"),"standalone settings opens its own scrollable window with an English heading");
        window.Open();
        Check(draws==firstDraws&&ReferenceEquals(owned,manager.GetDefinition(id)),"opening already visible settings does not toggle it closed or rebuild its contents");
        window.Open(refresh:true);
        Check(draws==firstDraws+1&&manager.IsOpen(id)&&ReferenceEquals(owned,manager.GetDefinition(id)),
            "an operation settings shortcut can refresh the existing window without toggling it closed");
        draft="unsaved edits";manager.CloseWindow(id);window.Open();
        Check(manager.IsOpen(id)&&draft=="unsaved edits"&&draws>firstDraws,"close and reopen retains the existing settings editor and pending values");
        LocalText.Configure(()=>"de");manager.CloseWindow(id);window.Open();
        Check(UI.Builder.UIPanelBuilder.Labels.Contains("Einstellungen"),"reopening settings respects the selected German interface language");
        var picker=new ModePicker(()=>new ModePickerAccess(false,true,true,true,true),_=>modes++,window.Open);
        picker.Update();UI.Builder.UIBuilderAssets.Buttons.Clear();BuildToolbar.Toggle();
        var buttons=UI.Builder.UIBuilderAssets.Buttons.ToArray();
        Check(buttons.Length==4&&buttons.Take(3).All(b=>!b.interactable)&&buttons[3].interactable,"before milestone unlock the three workspaces are disabled while the settings gear remains interactive for a client");
        manager.CloseWindow(id);buttons[3].Press();
        Check(manager.IsOpen(id)&&!manager.IsOpen("enusde.signaltechnik.modes")&&modes==0,"the real settings button opens the separate window without unlocking or entering a workspace");
        int ensures=BuildToolbar.Ensures;picker.Update();
        Check(BuildToolbar.Ensures==ensures+1,"the picker keeps its toolbar entry reachable independently of the old hide-button setting");
        picker.Dispose();
        Check(manager.GetDefinition("enusde.signaltechnik.modes")==null&&manager.IsOpen(id),"disposing the mode picker leaves the independent settings window intact");
        var foreign=new UI.Windows.CustomWindowDefinition{Identifier="foreign.mod",BuilderAction=_=>{}};
        manager.RegisterCustomWindow(foreign);manager.Show(foreign.Identifier);
        window.Release();
        Check(manager.GetDefinition(id)==null&&manager.GetCustomWindow(id)==null&&!manager.IsOpen(id)&&manager.IsOpen(foreign.Identifier),"world cleanup removes only the owned settings window and preserves another mod window");
        draft="new world settings";window.Open();
        Check(manager.IsOpen(id)&&!ReferenceEquals(owned,manager.GetDefinition(id)),"the same settings service can reopen with a new definition after world cleanup");
        var oldManager=manager;UI.Windows.WindowManager.Shared=manager=new UI.Windows.WindowManager();window.Open();
        Check(oldManager.GetDefinition(id)==null&&manager.IsOpen(id),"a replaced native manager releases the former window before registering a new one");
        var callback=manager.GetDefinition(id).BuilderAction;int before=draws;
        window.Dispose();callback(new UI.Builder.UIPanelBuilder());window.Open();
        Check(manager.GetDefinition(id)==null&&draws==before,"disposed windows reject delayed builder callbacks and cannot reopen");
        var noManager=new SettingsWindow(_=>draws++,_=>warnings++);
        UI.Windows.WindowManager.Shared=null;noManager.Open();
        Check(draws==before,"settings requested before a native window manager exists stays inert");
        UI.Windows.WindowManager.Shared=manager;Game.State.StateManager.IsUnloading=true;noManager.Open();
        Check(manager.GetDefinition(id)==null,"world unloading cannot register or open a settings window");
        Game.State.StateManager.IsUnloading=false;noManager.Dispose();
        var failure=new SettingsWindow(_=>throw new Exception("fixture draw failure"),_=>warnings++);failure.Open();
        Check(warnings>0&&UI.Builder.UIPanelBuilder.Labels.Any(l=>l.Contains("Einstellungen konnten")),"a failed settings builder reports a localized in-window error");
        var replacement=new UI.Windows.CustomWindowDefinition{Identifier=id,BuilderAction=_=>{}};manager.RegisterCustomWindow(replacement);failure.Dispose();
        Check(ReferenceEquals(manager.GetDefinition(id),replacement),"cleanup never removes a replacement window owned by another component");
        var gear=ModeIconRaster.SettingsAlpha();var modeImages=new[]{WorkspaceMode.Dispatcher,WorkspaceMode.Build,WorkspaceMode.Editor}.Select(ModeIconRaster.Alpha);
        Check(gear.Length==48*48&&gear.Count(a=>a>180)>100&&gear.Count(a=>a>0)<gear.Length*3/4&&!modeImages.Any(a=>a.SequenceEqual(gear)),"the gear has a distinct bounded raster while all three existing mode symbols remain unchanged");
    }
}
