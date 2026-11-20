using System;
using System.Linq;
using System.Collections.Generic;
using Game.Progression;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    internal static class ConstructionMapRules {
        static string path,error;
        static bool loaded;
        static ConstructionMapRule rule;
        internal static void Reset(){loaded=false;path=error=null;rule=null;}
        internal static Milestone[] NativeMilestones()=>Progression.Shared?.Milestones?.Where(m=>m!=null&&!(m is SignalConstructionMilestone)).ToArray()??new Milestone[0];
        internal static ConstructionUnlockPlan Resolve(MaterialLedger ledger,IEnumerable<Milestone> native,bool started=false,bool sandbox=false,bool initialized=true) {
            string current=Game.ActiveMapDefinition.MapDataAbsolutePath;
            if(!loaded||path!=current) {
                path=current;loaded=true;rule=null;error=null;
                try{rule=ConstructionMapRule.Read(path);}catch(Exception e){error=e.Message;}
            }
            return ConstructionProgressionPolicy.Resolve(ledger?.UnlockMode??ConstructionUnlockMode.MapDefault,ledger?.UnlockPrerequisite??"",rule,
                native.Select(m=>new ConstructionMilestoneInfo{Id=m.Identifier,Completed=initialized&&m.IsCompleted,Prerequisites=m.Prerequisites?.Where(p=>p!=null).Select(p=>p.Identifier).ToArray()??new string[0]}),
                started||ledger?.Unlocked==true,sandbox,error);
        }
    }
    public sealed partial class SharedRuntime {
        string constructionRuleWarning;
        internal ConstructionUnlockPlan ConstructionPlan() {
            var own=Progression.Shared?.Milestones?.OfType<SignalConstructionMilestone>().SingleOrDefault();
            return ConstructionMapRules.Resolve(installation?.Economy,ConstructionMapRules.NativeMilestones(),own!=null&&own.State!=MilestoneState.NotStarted,StateManager.IsSandbox);
        }
        static string ConstructionSource(string source) {
            switch(source) {
                case "native-signals":return LocalText.Select("Native signal / CTC prerequisites; CTC purchase not required","Voraussetzungen des nativen Signal-/CTC-Ausbaus; CTC-Kauf nicht erforderlich");
                case "custom-map-free":return LocalText.Select("No native signal expansion on this map: immediately available","Kein nativer Signalausbau auf dieser Karte: sofort verfügbar");
                case "map-rule":return LocalText.Select("Map creator's progression file","Freischaltungsdatei des Kartenherstellers");
                case "host-free":return LocalText.Select("Host override: immediately available","Host-Vorgabe: sofort verfügbar");
                case "host-purchase":return LocalText.Select("Host override: purchase with optional prerequisite","Host-Vorgabe: Kauf mit optionaler Voraussetzung");
                default:return LocalText.Select("Map progression rule could not be read","Karten-Freischaltung konnte nicht gelesen werden");
            }
        }
        internal void BuildConstructionSettings(UIPanelBuilder c) {
            if(!Ready||!StateManager.IsHost)return;
            var milestones=ConstructionMapRules.NativeMilestones();var plan=ConstructionPlan();
            c.AddField(LocalText.Select("Signalling availability","Freischaltung der Signaltechnik"),ConstructionSource(plan.Source));
            c.AddField(LocalText.Select("Base unlock price","Grundpreis der Freischaltung"),plan.Free?LocalText.Select("Free","Kostenlos"):"$"+plan.Cost);
            if(plan.Prerequisites.Length>0)c.AddField(LocalText.Select("Required milestones","Benötigte Meilensteine"),string.Join(", ",plan.Prerequisites.Select(id=>milestones.FirstOrDefault(m=>m.Identifier==id)?.Title??id)));
            if(plan.Error!=null)c.AddLabel(LocalText.Select("Map progression error: ","Fehler der Karten-Freischaltung: ")+plan.Error);
            if(plan.Missing.Length>0)c.AddLabel(LocalText.Select("Not yet completed or missing: ","Noch nicht abgeschlossen oder nicht vorhanden: ")+string.Join(", ",plan.Missing));
            if(ConstructionUnlocked)c.AddLabel(LocalText.Select("Already unlocked. Changing defaults does not revoke an existing installation.","Bereits freigeschaltet. Geänderte Vorgaben sperren eine bestehende Anlage nicht wieder."));
            var chosenMode=installation.Economy.UnlockMode;
            if(chosenMode==ConstructionUnlockMode.MapDefault&&!string.IsNullOrEmpty(installation.Economy.UnlockPrerequisite))chosenMode=ConstructionUnlockMode.Purchase;
            c.AddField(LocalText.Select("Host unlock override","Host-Vorgabe zur Freischaltung"),c.AddDropdown(new List<string>{LocalText.Select("Use map defaults","Kartenvorgabe verwenden"),LocalText.Select("Immediately available","Sofort verfügbar"),LocalText.Select("Purchase for $7,500 base price","Kauf zum Grundpreis von 7.500 $")},(int)chosenMode,i=>chosenMode=(ConstructionUnlockMode)i));
            var ids=new[]{""}.Concat(milestones.Select(m=>m.Identifier)).ToArray();var labels=new[]{LocalText.Select("No prerequisite","Keine Voraussetzung")}.Concat(milestones.Select(m=>m.Title)).ToList();
            string chosen=installation.Economy.UnlockPrerequisite;int selected=Array.IndexOf(ids,chosen);
            if(selected<0){c.AddLabel(LocalText.Select("Configured prerequisite is missing on this map: ","Eingestellte Voraussetzung fehlt auf dieser Karte: ")+chosen);selected=0;}
            c.AddSearchChoice(LocalText.Select("Prerequisite for host purchase override","Voraussetzung für die Host-Kaufvorgabe"),labels,selected,i=>chosen=ids[i]);
            c.AddLabel(LocalText.Select("The prerequisite selector applies only to the host purchase override. Map defaults use the native signal prerequisites or the map creator's file. Existing purchases are retained.","Die Voraussetzungswahl gilt nur für die Host-Kaufvorgabe. Kartenvorgaben verwenden die nativen Signalvoraussetzungen oder die Datei des Kartenherstellers. Bestehende Freischaltungen bleiben erhalten."));
            string world=WorldKey;
            c.AddButton(LocalText.Select("Apply unlock policy","Freischaltung übernehmen"),()=>{
                try{RequireReady();if(WorldKey!=world)throw new InvalidOperationException("World changed.");var next=installation.Clone();next.Economy.UnlockMode=chosenMode;next.Economy.UnlockPrerequisite=chosenMode==ConstructionUnlockMode.Purchase?chosen:"";CommitInstall(next);SyncConstructionMilestone();}
                catch(Exception e){c.AddLabel(e.Message);}c.SetNeedsRebuild();
            });
        }
    }
}
