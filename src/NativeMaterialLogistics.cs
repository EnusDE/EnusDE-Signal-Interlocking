using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using Game.Mow;
using HarmonyLib;
using Model.Ops;
using Model.Ops.Definition;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed class NativeMaterialWarehouse {
        public string Id,Name,Prefix;
        public Industry Industry;
        public float BuildingCapacity,TechnologyCapacity;
        public float Capacity(bool technology)=>technology?TechnologyCapacity:BuildingCapacity;
        public float Quantity(bool technology)=>Industry.Storage.QuantityInStorage(CarLoadLibrary.GetLoad(technology?MaterialEconomy.TechnologyLoad:MaterialEconomy.BuildingLoad),Prefix);
        public void Set(bool technology,float value)=>Industry.Storage.SetStorage(CarLoadLibrary.GetLoad(technology?MaterialEconomy.TechnologyLoad:MaterialEconomy.BuildingLoad),value,Prefix);
    }
    // Changes only the two mod cargo entries in existing MOW storage components.
    // Real stock remains in the game's host-owned industry storage properties.
    public sealed class NativeMaterialLogistics:IDisposable {
        const string PatchId="enusde.signaltechnik.native-material-labels";
        readonly List<Action> cleanup=new List<Action>();
        readonly HashSet<Industry> inspected=new HashSet<Industry>();
        readonly List<NativeMaterialWarehouse> warehouses=new List<NativeMaterialWarehouse>();
        Harmony labels;
        string language;
        static readonly MethodInfo prefixGetter=AccessTools.PropertyGetter(typeof(IndustryComponent),"StoragePrefix");
        public static string StoragePrefix(IndustryComponent component)=>(string)prefixGetter.Invoke(component,null);
        void AllowLegacyFlatCars(IndustryComponent component) {
            var field=AccessTools.Field(typeof(IndustryComponent),"carTypeFilter");
            var previous=field?.GetValue(component) as CarTypeFilter;
            if(previous==null||previous.Matches("FM"))return;
            var extended=new CarTypeFilter(previous.queryString+",FM");field.SetValue(component,extended);
            cleanup.Add(()=>{if(ReferenceEquals(field.GetValue(component),extended))field.SetValue(component,previous);});
        }
        public IReadOnlyList<NativeMaterialWarehouse> Warehouses=>warehouses;
        public string Error {get;private set;}
        public void Start() {
            labels=new Harmony(PatchId);
            labels.Patch(AccessTools.PropertyGetter(typeof(Load),"description"),postfix:new HarmonyMethod(typeof(NativeMaterialLogistics),nameof(LoadDescription)));
        }
        static void ClearOrderLabels() {
            var type=typeof(UIPanelBuilder).Assembly.GetType("UI.CompanyWindow.EquipmentPanelBuilder");
            if(type!=null)(AccessTools.Field(type,"_captiveLoadsByCarType")?.GetValue(null) as IDictionary)?.Clear();
        }
        static void LoadDescription(Load __instance,ref string __result) {
            if(__instance.id==MaterialEconomy.BuildingLoad)__result=LocalText.Select("Signal building supplies","Signalbaumaterial");
            else if(__instance.id==MaterialEconomy.TechnologyLoad)__result=LocalText.Select("Signal equipment","Signaltechnik");
        }
        public void Refresh() {
            try {
                if(Game.State.StateManager.Shared?.IsSessionReady!=true||!Game.State.StateManager.Shared.HasRestoredProperties)return;
                if(language!=LocalText.Language){ClearOrderLabels();language=LocalText.Language;}
                var controller=TrackConditionController.Shared;
                var profile=controller==null?null:AccessTools.Field(typeof(TrackConditionController),"profile")?.GetValue(controller) as TrackMaintenanceProfile;
                var industries=OpsController.Shared?.AllIndustries;
                if(profile==null||industries==null)return;
                var materialIds=new HashSet<string>((profile.unloadRates??Array.Empty<TrackMaintenanceProfile.LoadUnloadRate>()).Select(r=>r.loadId).Where(id=>!string.IsNullOrEmpty(id)));
                foreach(var requirement in profile.constructionRequirements??Array.Empty<TrackMaintenanceProfile.ConstructionRequirement>())
                    foreach(var byClass in requirement.requirementsByClass??Array.Empty<TrackMaintenanceProfile.ClassRequirementsByDifficulty>())
                        foreach(var load in (byClass.low??Array.Empty<TrackMaintenanceProfile.LoadRequirement>()).Concat(byClass.medium??Array.Empty<TrackMaintenanceProfile.LoadRequirement>()).Concat(byClass.high??Array.Empty<TrackMaintenanceProfile.LoadRequirement>()))
                            if(!string.IsNullOrEmpty(load.loadId))materialIds.Add(load.loadId);
                materialIds.Remove(MaterialEconomy.BuildingLoad);materialIds.Remove(MaterialEconomy.TechnologyLoad);
                if(materialIds.Count==0)return;
                foreach(var industry in industries.Where(i=>i!=null&&!inspected.Contains(i))) {
                    var components=industry.Components;if(components==null)continue;
                    foreach(var loader in components.OfType<StorageLoader>().Where(l=>l.loads!=null&&l.loads.Any(e=>materialIds.Contains(e.loadId)))) {
                        string prefix=StoragePrefix(loader);
                        var unloader=components.OfType<StorageUnloader>().FirstOrDefault(u=>u.loads!=null&&StoragePrefix(u)==prefix&&u.loads.Any(e=>materialIds.Contains(e.loadId)&&loader.loads.Any(l=>l.loadId==e.loadId)));
                        if(unloader==null)continue;
                        AllowLegacyFlatCars(loader);AllowLegacyFlatCars(unloader);
                        var from=loader.loads.First(e=>materialIds.Contains(e.loadId));var into=unloader.loads.First(e=>materialIds.Contains(e.loadId));
                        int capacity=Math.Max(1,into.storage);
                        foreach(string loadId in new[]{MaterialEconomy.BuildingLoad,MaterialEconomy.TechnologyLoad}) {
                            if(CarLoadLibrary.GetLoad(loadId)==null)throw new InvalidOperationException("Signal cargo definitions are not loaded.");
                            if(!loader.loads.Any(e=>e.loadId==loadId)) {
                                var entry=new StorageLoader.LoadEntry{loadId=loadId,carLoadRate=Math.Max(1,from.carLoadRate)};loader.loads.Add(entry);cleanup.Add(()=>loader.loads.Remove(entry));
                            }
                            if(!unloader.loads.Any(e=>e.loadId==loadId)) {
                                var entry=new StorageUnloader.LoadEntry{loadId=loadId,storage=capacity,unloadRate=Math.Max(1,into.unloadRate)};unloader.loads.Add(entry);cleanup.Add(()=>unloader.loads.Remove(entry));
                            }
                        }
                        float buildingCapacity=unloader.loads.First(e=>e.loadId==MaterialEconomy.BuildingLoad).storage*CarLoadLibrary.GetLoad(MaterialEconomy.BuildingLoad).NominalQuantityPerCarLoad;
                        float technologyCapacity=unloader.loads.First(e=>e.loadId==MaterialEconomy.TechnologyLoad).storage*CarLoadLibrary.GetLoad(MaterialEconomy.TechnologyLoad).NominalQuantityPerCarLoad;
                        if(!warehouses.Any(w=>w.Industry==industry&&w.Prefix==prefix))warehouses.Add(new NativeMaterialWarehouse{Id=loader.Identifier,Name=industry.DisplayName,Prefix=prefix,Industry=industry,BuildingCapacity=buildingCapacity,TechnologyCapacity=technologyCapacity});
                    }
                    inspected.Add(industry);
                }
                Error=null;
            }catch(Exception e){Error=e.Message;}
        }
        public static void OpenOrders() {
            var company=UI.CompanyWindow.CompanyWindow.Shared;
            if(company==null)throw new InvalidOperationException(LocalText.Select("Load a world first.","Zuerst eine Spielwelt laden."));
            var field=AccessTools.Field(company.GetType(),"_selectedTabState")??throw new MissingFieldException("Company equipment tab state");
            var state=(UIState<string>)field.GetValue(company);state.Value="equipment";field.SetValue(company,state);
            AccessTools.Method(company.GetType(),"Show",Type.EmptyTypes).Invoke(company,null);
        }
        public void Dispose() {
            for(int i=cleanup.Count-1;i>=0;i--)cleanup[i]();cleanup.Clear();warehouses.Clear();inspected.Clear();
            labels?.UnpatchAll(PatchId);labels=null;ClearOrderLabels();
        }
    }
}
