using System;
using System.Reflection;
using HarmonyLib;
using UI.Builder;
using Game.State;

namespace EnusDE.Signaltechnik {
    internal sealed class CompanySettingsRegistration:IDisposable {
        const string Owner="enusde.signaltechnik.company-settings";
        static Action<UIPanelBuilder> draw;
        static Action<string> warning;
        static int page=1;
        Harmony harmony;
        internal static MethodInfo FindTarget()=>typeof(UIPanelBuilder).Assembly.GetType("UI.CompanyWindow.SettingsPanelBuilder",true)
            .GetMethod("BuildMods",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(UIPanelBuilder)},null);
        public CompanySettingsRegistration(Action<UIPanelBuilder> builder,Action<string> log) {
            draw=builder;warning=log;
            try {
                var target=FindTarget()??throw new MissingMethodException("Company.SettingsPanelBuilder.BuildMods");
                harmony=new Harmony(Owner);harmony.Patch(target,prefix:new HarmonyMethod(typeof(CompanySettingsRegistration),nameof(BuildMods)));
                var visible=target.DeclaringType.GetProperty("ShouldShowMods",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public)?.GetGetMethod(true)
                    ??throw new MissingMethodException("ShouldShowMods");
                harmony.Patch(visible,postfix:new HarmonyMethod(typeof(CompanySettingsRegistration),nameof(ShowMods)));
            }catch{Dispose();throw;}
        }
        static void ShowMods(ref bool __result){if(draw!=null)__result=true;}
        static bool BuildMods(UIPanelBuilder __0) {
            if(draw==null)return true;
            try {
                if(StateManager.IsHost) {
                    var builder=__0;
                    builder.AddSearchChoice(LocalText.Select("Mod section","Mod-Bereich"),new System.Collections.Generic.List<string>{
                        LocalText.Select("Installed mods","Installierte Mods"),LocalText.Select("Signal & Interlocking","Signaltechnik")},page,
                        i=>{page=i;builder.SetNeedsRebuild();});
                    if(page==0)return true;
                }
                // Clients may edit local preferences here, never the native host mod list.
                __0.VScrollView(c=>draw(c));
            }catch(Exception e){warning?.Invoke("Company signalling settings: "+e.Message);__0.AddLabel(LocalText.Select("Settings unavailable. Reopen Company settings.","Einstellungen nicht verfügbar. Company-Optionen erneut öffnen."));}
            return false;
        }
        public static void Open() {
            var company=UI.CompanyWindow.CompanyWindow.Shared;if(company==null)throw new InvalidOperationException(LocalText.Select("Load a world before opening Company settings.","Vor dem Öffnen der Company-Optionen eine Welt laden."));
            page=1;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            foreach(var item in new[]{Tuple.Create("_selectedTabState","settings"),Tuple.Create("_selectedSettingsItem","mods")}) {
                var field=company.GetType().GetField(item.Item1,flags)??throw new MissingFieldException(item.Item1);
                var state=(UIState<string>)field.GetValue(company);state.Value=item.Item2;field.SetValue(company,state);
            }
            var show=company.GetType().GetMethod("Show",flags)??throw new MissingMethodException("CompanyWindow.Show");
            show.Invoke(company,null);
        }
        public void Dispose(){draw=null;warning=null;page=1;if(harmony!=null){harmony.UnpatchAll(Owner);harmony=null;}}
    }
}
