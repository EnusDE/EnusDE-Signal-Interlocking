using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using EnusDE.Signaltechnik;

namespace HarmonyLib {
    public sealed class HarmonyMethod {public MethodInfo Method;public HarmonyMethod(Type type,string name){Method=type.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);}}
    public sealed class Harmony {
        readonly string owner;
        public static readonly Dictionary<string,List<MethodInfo>> Patches=new Dictionary<string,List<MethodInfo>>();
        public Harmony(string id){owner=id;}
        public void Patch(MethodInfo target,HarmonyMethod prefix=null,HarmonyMethod postfix=null){if(target==null)throw new Exception("No target");if(!Patches.ContainsKey(owner))Patches[owner]=new List<MethodInfo>();Patches[owner].Add((prefix??postfix).Method);}
        public void UnpatchAll(string id){Patches.Remove(id);}
    }
}
namespace Game.State {public static class StateManager {public static bool IsHost;}}
namespace UI.Builder {
    public sealed class UIState<T> {public T Value{get;set;}public UIState(T value){Value=value;}}
    public struct UIPanelBuilder {
        public static Action<int> Choose;public static int Rebuilds,Choices;public static readonly List<string> Labels=new List<string>();
        public void VScrollView(Action<UIPanelBuilder> draw){draw(this);}
        public void AddLabel(string text){Labels.Add(text);}
        public void AddSearchChoice(string label,List<string> values,int index,Action<int> choose){Choices++;Choose=choose;}
        public void SetNeedsRebuild(){Rebuilds++;}
    }
}
namespace UI.CompanyWindow {
    public static class SettingsPanelBuilder {static void BuildMods(UI.Builder.UIPanelBuilder builder){}static bool ShouldShowMods=>Game.State.StateManager.IsHost;}
    public sealed class CompanyWindow {
        public static CompanyWindow Shared{get;set;}
        UI.Builder.UIState<string> _selectedTabState=new UI.Builder.UIState<string>("railroad"),_selectedSettingsItem=new UI.Builder.UIState<string>("general");
        public int Shows;public string Path=>_selectedTabState.Value+"/"+_selectedSettingsItem.Value;
        void Show(){Shows++;}
    }
}
public static class CompanySettingsFixture {
    static void Check(bool value,string text){if(!value)throw new Exception("FAIL: "+text);Console.WriteLine("PASS: "+text);}
    static object Call(string name,params object[] args)=>typeof(CompanySettingsRegistration).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
    public static void Main() {
        LocalText.Configure(()=>"en");int draws=0,warnings=0;
        var settings=new CompanySettingsRegistration(_=>draws++,_=>warnings++);
        Check(CompanySettingsRegistration.FindTarget()!=null&&HarmonyLib.Harmony.Patches.Single().Value.Count==2,"Company settings register against the Mods page and its visibility getter");
        object[] visible={false};Call("ShowMods",visible);Check((bool)visible[0],"Company Mods remain visible for a client to reach its local preferences");
        Game.State.StateManager.IsHost=true;
        Check(!(bool)Call("BuildMods",new UI.Builder.UIPanelBuilder())&&draws==1,"host opens signalling settings directly inside Company Mods");
        UI.Builder.UIPanelBuilder.Choose(0);
        Check((bool)Call("BuildMods",new UI.Builder.UIPanelBuilder())&&draws==1&&UI.Builder.UIPanelBuilder.Rebuilds==1,"host can still select the game's original installed-mod management page");
        Game.State.StateManager.IsHost=false;int choices=UI.Builder.UIPanelBuilder.Choices;
        Check(!(bool)Call("BuildMods",new UI.Builder.UIPanelBuilder())&&draws==2&&choices==UI.Builder.UIPanelBuilder.Choices,"client never reaches native host mod management, even after the host selected it");
        UI.CompanyWindow.CompanyWindow.Shared=new UI.CompanyWindow.CompanyWindow();CompanySettingsRegistration.Open();
        Check(UI.CompanyWindow.CompanyWindow.Shared.Path=="settings/mods"&&UI.CompanyWindow.CompanyWindow.Shared.Shows==1,"old settings shortcut navigates to Company Settings Mods using its actual private Show entry point");
        settings.Dispose();visible=new object[]{false};Call("ShowMods",visible);
        Check(!(bool)visible[0]&&(bool)Call("BuildMods",new UI.Builder.UIPanelBuilder())&&HarmonyLib.Harmony.Patches.Count==0,"unload restores native Company visibility and mod-list behavior");
        settings=new CompanySettingsRegistration(_=>throw new Exception("fixture failure"),_=>warnings++);
        Check(!(bool)Call("BuildMods",new UI.Builder.UIPanelBuilder())&&warnings==1&&UI.Builder.UIPanelBuilder.Labels.Count==1,"a broken signalling page reports its failure without exposing the client to native host controls");
        settings.Dispose();
    }
}
