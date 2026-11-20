using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Game.Progression;
using Game.State;
using UI.Builder;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed class SignalConstructionMilestone:Milestone {
        public const string Id="enusde.signaltechnik.construction";
        public void RestoreUnlocked(){State=MilestoneState.InProgress;}
        public override void OnMilestoneStart(){base.OnMilestoneStart();Entry.Shared?.SyncConstructionMilestone();}
        public override void DebugAdvance(){if(State!=MilestoneState.InProgress)base.OnMilestoneStart();}
        public override void BuildPanelInProgress(UIPanelBuilder c) {
            c.AddLabel(LocalText.Select("Permanent construction programme. New planned installations appear here; completed work disappears automatically.","Dauerhafter Bauauftrag. Neue geplante Anlagen erscheinen hier; fertiggestellte Anlagen verschwinden automatisch."));
            var snapshot=Entry.Shared?.Snapshot();
            if(snapshot==null){c.AddLabel(LocalText.Select("Load the installation first.","Anlagenbestand zuerst laden."));return;}
            c.AddField(LocalText.Select("Materials still required","Noch benötigtes Material"),ConstructionPolicy.TotalMissing(snapshot).ToString());
            foreach(var plan in snapshot.Objects.Markers.Where(m=>m.Planned))c.AddLabel(plan.Name+" · "+LocalText.Get(MarkerRecord.KindLabel(plan.Kind))+" · "+MaterialEconomy.Missing(snapshot,plan));
            if(!snapshot.Objects.Markers.Any(m=>m.Planned))c.AddLabel(LocalText.Select("No pending installations. The programme remains available.","Keine offenen Anlagen. Der Bauauftrag bleibt verfügbar."));
            c.AddButton(LocalText.Select("Open materials and work orders","Material und Bauaufträge öffnen"),Entry.OpenMaterials);
        }
    }
    public sealed class ConstructionIntegration:IDisposable {
        const string Owner="enusde.signaltechnik.construction";
        Harmony harmony;static Action<string> warning;
        public ConstructionIntegration(Action<string> log) {
            warning=log;harmony=new Harmony(Owner);
            try {
                harmony.Patch(AccessTools.Method(typeof(Progression),"Configure"),prefix:new HarmonyMethod(typeof(ConstructionIntegration),nameof(BeforeConfigure)));
                harmony.Patch(AccessTools.PropertyGetter(typeof(Milestone),"PrerequisitesCompleted"),postfix:new HarmonyMethod(typeof(ConstructionIntegration),nameof(Prerequisites)));
                harmony.Patch(AccessTools.Method(typeof(Progression),"PayToStartMilestone"),prefix:new HarmonyMethod(typeof(ConstructionIntegration),nameof(BeforePurchase)));
                harmony.Patch(AccessTools.Method(typeof(UI.CarInspector.CarInspector),"BuildLoads"),postfix:new HarmonyMethod(typeof(ConstructionIntegration),nameof(CarLoads)));
                harmony.Patch(AccessTools.Method(typeof(Game.Mow.WorkCar),"PerformWork",new[]{typeof(float),typeof(List<Track.TrackSegment>)}),postfix:new HarmonyMethod(typeof(ConstructionIntegration),nameof(Work)));
            }catch{Dispose();throw;}
        }
        static void BeforeConfigure(Progression __instance) {
            ConstructionMapRules.Reset();
            if(__instance.GetComponentsInChildren<SignalConstructionMilestone>(true).Length>0)return;
            var child=new GameObject("EnusDE permanent signal construction");child.transform.SetParent(__instance.transform,false);
            var milestone=child.AddComponent<SignalConstructionMilestone>();
            Set(milestone,"identifier",SignalConstructionMilestone.Id);Set(milestone,"title",LocalText.Select("Signal & Interlocking construction","Signaltechnik – Ausbau"));
            Set(milestone,"description",LocalText.Select("Signalling availability follows the map's progression rules. Where a purchase is required, its prerequisites and base price are shown here. Deliver materials with supply wagons and build with an active MOW car. This programme remains open for extensions.","Die Freischaltung folgt den Vorgaben der Karte. Ist ein Kauf erforderlich, stehen Voraussetzungen und Grundpreis hier. Material mit Güterwagen liefern und mit einem aktiven MOW-Wagen bauen. Der Auftrag bleibt für Erweiterungen offen."));
            Set(milestone,"cost",ConstructionPolicy.UnlockCost);Set(milestone,"prerequisites",new List<Milestone>());
            foreach(string field in new[]{"enableFeaturesOnStart","enableFeaturesOnComplete","disableFeaturesOnComplete"})Set(milestone,field,new List<MapFeature>());
            var native=__instance.GetComponentsInChildren<Milestone>().Where(m=>!(m is SignalConstructionMilestone)).ToArray();
            // Native milestone properties are assigned inside Configure, after this
            // prefix. Read serialized IDs/prerequisites here, never live State yet.
            Apply(milestone,ConstructionMapRules.Resolve(null,native,initialized:false),native);
        }
        internal static void Apply(SignalConstructionMilestone milestone,ConstructionUnlockPlan plan,IEnumerable<Milestone> native) {
            if(milestone.Cost!=plan.Cost)Set(milestone,"cost",plan.Cost);
            var requirements=plan.Prerequisites.Select(id=>native.FirstOrDefault(m=>m.Identifier==id)).Where(m=>m!=null).ToList();
            if(!milestone.Prerequisites.SequenceEqual(requirements))Set(milestone,"prerequisites",requirements);
        }
        static void Set(Milestone item,string field,object value)=>AccessTools.Field(typeof(Milestone),field).SetValue(item,value);
        static void Prerequisites(Milestone __instance,ref bool __result) {
            if(!(__instance is SignalConstructionMilestone))return;
            var shared=Entry.Shared;
            if(shared?.Ready!=true){__result=false;return;}
            var plan=shared.ConstructionPlan();Apply((SignalConstructionMilestone)__instance,plan,ConstructionMapRules.NativeMilestones());__result=plan.CanStart;
        }
        static bool BeforePurchase(Milestone __0) {
            if(!(__0 is SignalConstructionMilestone signal)||!StateManager.IsHost)return true;
            var shared=Entry.Shared;
            if(shared?.Ready!=true)throw new Game.DisplayableException(LocalText.Select("Wait for the signalling installation to finish loading.","Das vollständige Laden der Signalanlage abwarten."),null);
            var plan=shared.ConstructionPlan();Apply(signal,plan,ConstructionMapRules.NativeMilestones());
            if(!plan.CanStart)throw new Game.DisplayableException(LocalText.Select("Signalling unlock prerequisites are not completed, or the map progression rule needs correction.","Die Voraussetzungen für die Signaltechnik sind noch nicht erfüllt oder die Karten-Freischaltung muss korrigiert werden."),null);
            if(!plan.ChargeRequired){shared.SyncConstructionMilestone();return false;}
            return true;
        }
        static void CarLoads(object __instance,UIPanelBuilder __0) {
            try{var car=AccessTools.Field(__instance.GetType(),"_car")?.GetValue(__instance) as Model.Car;if(car!=null)Entry.Shared?.BuildSignalSupplies(__0,car);}catch(Exception e){warning?.Invoke("Signal supplies: "+e.Message);}
        }
        static void Work(){try{Entry.Shared?.PerformSignalWork();}catch(Exception e){warning?.Invoke("Signal construction: "+e.Message);}}
        public void Dispose(){harmony?.UnpatchAll(Owner);harmony=null;
            foreach(var progression in UnityEngine.Object.FindObjectsByType<Progression>(FindObjectsSortMode.None)) {
                if(progression.Milestones!=null)AccessTools.PropertySetter(typeof(Progression),"Milestones").Invoke(progression,new object[]{progression.Milestones.Where(m=>!(m is SignalConstructionMilestone)).ToArray()});
                foreach(var item in progression.GetComponentsInChildren<SignalConstructionMilestone>(true))UnityEngine.Object.Destroy(item.gameObject);
            }
            ConstructionMapRules.Reset();warning=null;
        }
    }
    public sealed partial class SharedRuntime {
        internal bool ConstructionUnlocked=>StateManager.IsSandbox||installation?.Economy.Unlocked==true;
        internal void RequireConstruction(){RequireReady();SyncConstructionMilestone();if(!ConstructionUnlocked)throw new InvalidOperationException(LocalText.Select("Unlock Signal & Interlocking construction in Milestones first. Check the map prerequisites and purchase price.","Signalbau zuerst unter Meilensteine freischalten. Kartenvoraussetzungen und Kaufpreis beachten."));}
        internal void SyncConstructionMilestone() {
            if(!StateManager.IsHost||!Ready)return;
            var milestone=Progression.Shared?.Milestones?.OfType<SignalConstructionMilestone>().SingleOrDefault();
            if(installation.Economy.PolicyVersion==0) {
                CommitInstall(ConstructionPolicy.MigrateLegacy(installation,StateManager.IsSandbox));
            }
            var native=ConstructionMapRules.NativeMilestones();var plan=ConstructionMapRules.Resolve(installation.Economy,native,milestone!=null&&milestone.State!=MilestoneState.NotStarted,StateManager.IsSandbox);
            if(milestone!=null)ConstructionIntegration.Apply(milestone,plan,native);
            if(plan.Error!=null&&plan.Error!=constructionRuleWarning){constructionRuleWarning=plan.Error;log?.Warning("Signal construction progression: "+plan.Error);}
            bool unlocked=plan.AutoUnlock;
            if(unlocked&&!installation.Economy.Unlocked){var next=installation.Clone();next.Economy.Unlocked=true;CommitInstall(next);}
            if(unlocked&&milestone!=null&&milestone.State!=MilestoneState.InProgress)milestone.RestoreUnlocked();
            bool materials=ConstructionPolicy.MaterialsRequired(StateManager.IsSandbox,installation.Economy.MaterialsInSandbox,installation.Economy.FreeInEconomy);
            if(materials!=installation.Economy.Enabled)ApplyMaterialPolicy(installation.Economy.MaterialsInSandbox,installation.Economy.FreeInEconomy,installation.Economy.Factor);
        }
        public void ApplyMaterialPolicy(bool sandboxMaterials,bool economyFree,double factor) {
            RequireReady();var next=installation.Clone();next.Economy.MaterialsInSandbox=sandboxMaterials;next.Economy.FreeInEconomy=economyFree;
            next.Economy.Enabled=ConstructionPolicy.MaterialsRequired(StateManager.IsSandbox,sandboxMaterials,economyFree);next.Economy.Factor=factor;
            if(!next.Economy.Enabled)foreach(var pending in next.Objects.Markers.Where(m=>m.Planned).ToArray())next=MaterialEconomy.Complete(next,pending.Id,null,true);
            CommitInstall(next);
        }
    }
}
