using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using EnusDE.Signaltechnik;

// Compiles the production registration source against small fixtures. This checks
// registry ownership/load order; it does not pretend to render a Unity window.
namespace HarmonyLib {
    public sealed class HarmonyMethod { public MethodInfo Method;public HarmonyMethod(Type type,string name){Method=type.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);} }
    public sealed class Harmony {
        string id;public static readonly Dictionary<string,MethodInfo> Patches=new Dictionary<string,MethodInfo>();
        public Harmony(string value){id=value;}
        public void Patch(MethodInfo target,HarmonyMethod postfix){Patches[id]=postfix.Method;}
        public void UnpatchAll(string owner){Patches.Remove(owner);}
    }
}
namespace UI.Builder {
    public struct UIPanelBuilder {
        public void VScrollView(Action<UIPanelBuilder> draw){draw(this);}
        public void AddLabel(string value){}
    }
    public sealed class UITabbedPanelBuilder {
        public int Tabs;public readonly List<string> Titles=new List<string>(),Ids=new List<string>();
        public void AddTab(string title,string id,Action<UIPanelBuilder> draw){Tabs++;Titles.Add(title);Ids.Add(id);draw(new UIPanelBuilder());}
    }
}
namespace UI.PreferencesWindow { public static class PreferencesBuilder {public static void BuildTabs(UI.Builder.UITabbedPanelBuilder builder){}} }
public static class PreferencesFixture {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);Console.WriteLine("PASS: "+text);}
    static void Draw(UI.Builder.UITabbedPanelBuilder builder){foreach(var patch in HarmonyLib.Harmony.Patches.Values.ToArray())patch.Invoke(null,new object[]{builder});}
    static void ForeignPostfix(UI.Builder.UITabbedPanelBuilder builder){builder.AddTab("Rusty Rails","foreign.fixture",_=>{});}
    public static void Main() {
        EnusDE.Signaltechnik.LocalText.Configure(()=>"de");
        const string slot=ModsPreferencesRegistration.SlotKey;
        int ours=0,foreign=0,warnings=0;
        var contribution=ModsPreferencesRegistration.Register(b=>ours++,m=>warnings++);
        var tab=new UI.Builder.UITabbedPanelBuilder();Draw(tab);
        Check(tab.Tabs==1&&ours==1,"standalone registration creates one Mods tab and invokes its settings page");
        var list=(List<Action<UI.Builder.UIPanelBuilder>>)AppDomain.CurrentDomain.GetData(slot);
        list.Add(b=>foreign++);tab=new UI.Builder.UITabbedPanelBuilder();Draw(tab);
        Check(tab.Tabs==1&&ours==2&&foreign==1,"existing registry renders foreign and Signaltechnik contributors together");
        contribution.Dispose();
        Check(list.Count==1&&HarmonyLib.Harmony.Patches.Count==0,"unload removes only its own registration and patch");
        contribution=ModsPreferencesRegistration.Register(b=>ours++,m=>warnings++);
        tab=new UI.Builder.UITabbedPanelBuilder();Draw(tab);
        Check(tab.Tabs==0&&list.Count==2,"foreign-first registration leaves tab ownership to the existing contributor");
        contribution.Dispose();list.Clear();
        var first=ModsPreferencesRegistration.Register(b=>ours++,m=>warnings++);
        var second=ModsPreferencesRegistration.Register(b=>ours++,m=>warnings++);first.Dispose();
        Check(list.Count==1&&HarmonyLib.Harmony.Patches.Count==1,"re-registration replaces the old callback without old tokens removing the replacement");
        second.Dispose();
        contribution=ModsPreferencesRegistration.Register(b=>throw new Exception("fixture page failed"),m=>warnings++);
        list.Add(b=>foreign++);int before=foreign;tab=new UI.Builder.UITabbedPanelBuilder();Draw(tab);
        Check(foreign==before+1&&warnings==1,"a broken contributor is isolated and later pages still draw");
        contribution.Dispose();list.Clear();
        AppDomain.CurrentDomain.SetData(slot,"incompatible");bool rejected=false;
        try{ModsPreferencesRegistration.Register(b=>{},m=>{});}catch(InvalidOperationException){rejected=true;}
        Check(rejected&&(string)AppDomain.CurrentDomain.GetData(slot)=="incompatible","incompatible registry data is preserved instead of overwritten");
        AppDomain.CurrentDomain.SetData(slot,null);
        var untouched=new List<Action<UI.Builder.UIPanelBuilder>>{_=>{}};AppDomain.CurrentDomain.SetData(slot,untouched);
        var foreignPatch=new HarmonyLib.Harmony("fixture.foreign");foreignPatch.Patch(typeof(UI.PreferencesWindow.PreferencesBuilder).GetMethod("BuildTabs"),new HarmonyLib.HarmonyMethod(typeof(PreferencesFixture),nameof(ForeignPostfix)));
        int ownDraws=0,ownWarnings=0;
        var own=SignalPreferencesRegistration.Register(_=>ownDraws++,_=>ownWarnings++);
        tab=new UI.Builder.UITabbedPanelBuilder();Draw(tab);
        Check(tab.Titles.SequenceEqual(new[]{"Rusty Rails","Signaltechnik"})&&tab.Ids.Contains("enusde.signaltechnik.settings")&&ownDraws==1,
            "dedicated Signaltechnik settings tab coexists with a foreign native tab");
        Check(ReferenceEquals(AppDomain.CurrentDomain.GetData(slot),untouched)&&untouched.Count==1,
            "dedicated settings never add, remove or replace a foreign shared Mods registry");
        own.Dispose();tab=new UI.Builder.UITabbedPanelBuilder();Draw(tab);
        Check(tab.Titles.SequenceEqual(new[]{"Rusty Rails"})&&HarmonyLib.Harmony.Patches.Count==1,
            "unloading dedicated settings removes only its own tab hook");
        var oldOwn=SignalPreferencesRegistration.Register(_=>ownDraws++,_=>ownWarnings++);
        var latestOwn=SignalPreferencesRegistration.Register(_=>ownDraws++,_=>ownWarnings++);oldOwn.Dispose();
        tab=new UI.Builder.UITabbedPanelBuilder();Draw(tab);
        Check(tab.Titles.Count(t=>t=="Signaltechnik")==1&&ownDraws==2,
            "an old registration token cannot remove a newer dedicated settings tab");
        latestOwn.Dispose();own=SignalPreferencesRegistration.Register(_=>throw new Exception("own page failed"),_=>ownWarnings++);
        tab=new UI.Builder.UITabbedPanelBuilder();Draw(tab);Draw(new UI.Builder.UITabbedPanelBuilder());
        Check(ownWarnings==1&&tab.Titles.Contains("Rusty Rails"),
            "a failed Signaltechnik page reports once and leaves foreign preference tabs available");
        own.Dispose();foreignPatch.UnpatchAll("fixture.foreign");AppDomain.CurrentDomain.SetData(slot,null);

    }
}
