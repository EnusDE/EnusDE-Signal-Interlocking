using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using Model.Ops;
using System.Reflection;
using Game.Mow;
using Game.State;
using HarmonyLib;
using UI.Builder;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SharedRuntime {
        double nextSignalWork;string signalWorkStatus="";
        readonly Dictionary<string,string> signalWorkStates=new Dictionary<string,string>();
        public string SignalWorkStatus(string id)=>signalWorkStates.TryGetValue(id,out var state)?state:LocalText.Select("Waiting for an active MOW work car and loaded supply wagons.","Wartet auf aktiven MOW-Arbeitswagen und beladene Materialwagen.");
        internal void BuildSignalSupplies(UIPanelBuilder c,Car car) {
            if(!Ready||car==null||!car.IsOwnedByPlayer)return;
            string type=car.CarType.ToString();bool building=type.StartsWith("FM",StringComparison.Ordinal)||type.StartsWith("FL",StringComparison.Ordinal),technology=type.StartsWith("XM",StringComparison.Ordinal);
            if(!building&&!technology&&!WorkCar.IsWorkCar(car))return;
            c.AddSection(LocalText.Select("Signal & Interlocking construction","Signaltechnik – Bau"));
            c.AddLabel(LocalText.Select("Pending material: ","Offenes Material: ")+ConstructionPolicy.TotalMissing(installation));
            if(!StateManager.IsHost){c.AddLabel(LocalText.Select("The host orders and consumes construction materials.","Der Host bestellt und verbraucht Baumaterial."));return;}
            if(!ConstructionUnlocked){c.AddLabel(LocalText.Select("Unlock construction in Milestones; check the map prerequisites and purchase price.","Signalbau unter Meilensteine freischalten; Kartenvoraussetzungen und Kaufpreis beachten."));return;}
            if(building||technology) {
                c.AddLabel(LocalText.Select("Use the normal captive wagon order and MOW warehouse loading/unloading. Signal building supplies use FL/FM flat cars; signal equipment uses XM boxcars.","Normale Bestellung als captive Materialwagen und Be-/Entladung am MOW-Lager verwenden. Signalbaumaterial gehört in FL/FM-Flachwagen, Signaltechnik in XM-Güterwagen."));
                c.AddButton(LocalText.Select("Open native wagon ordering","Normale Fahrzeugbestellung öffnen"),NativeMaterialLogistics.OpenOrders);
                c.AddLabel(LocalText.Select("To buy materials, choose the native Buy destination for Signal building supplies or Signal equipment at an interchange. Send the wagon there and wait for the normal interchange service and return; selecting a wagon alone does not purchase cargo.","Zum Materialkauf das native Kaufen-Ziel für Signalbaumaterial oder Signaltechnik an einem Interchange wählen. Den Wagen dorthin bringen und die normale Übergabebedienung mit Rücklieferung abwarten; die reine Wagenauswahl kauft keine Fracht."));
            }
            if(WorkCar.IsWorkCar(car))c.AddLabel(LocalText.Select("Activate the native MOW equipment. Stop within 250 m along the track of a planned signal, with the loaded supply wagons coupled. Signal materials are consumed when each installation is completed.","Native MOW-Ausrüstung aktivieren. Mit angekuppelten Materialwagen innerhalb von 250 Gleismetern bei einer geplanten Anlage anhalten. Das Signalbaumaterial wird beim Fertigstellen verbraucht."));
            c.AddLabel(()=>LocalText.Get(signalWorkStatus),UIPanelBuilder.Frequency.Periodic);
            c.AddButton(LocalText.Select("Open construction overview","Bauübersicht öffnen"),Entry.OpenMaterials);
        }
        static bool NativeMowEnabled(Car car) {
            var type=typeof(WorkCar).Assembly.GetType("Game.Mow.MowCarProperties",true);
            var properties=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{car},null);
            return (bool)AccessTools.PropertyGetter(type,"Enabled").Invoke(properties,null);
        }
        internal void PerformSignalWork() {
            if(!StateManager.IsHost||!Ready||StateManager.IsUnloading||!ConstructionUnlocked||Time.realtimeSinceStartup<nextSignalWork||TrainController.Shared==null)return;
            nextSignalWork=Time.realtimeSinceStartup+1;
            var plans=installation.Objects.Markers.Where(m=>m.Planned).ToArray();if(plans.Length==0)return;
            signalWorkStates.Clear();
            int completed=0;
            foreach(var workCar in TrainController.Shared.Cars.Where(c=>c!=null&&c.IsOwnedByPlayer&&!c.IsDerailed&&!c.IsInBardo&&c.IsStopped(.05f)&&WorkCar.IsWorkCar(c)&&NativeMowEnabled(c))) {
                var method=AccessTools.Method(typeof(WorkCar),"GetSupplyCars",new[]{typeof(Car)});
                var supplies=(method?.Invoke(null,new object[]{workCar}) as IEnumerable<Car>)?.Where(c=>c!=null&&c.IsOwnedByPlayer&&!c.IsDerailed&&!c.IsInBardo&&c.IsStopped(.05f)).Distinct().ToArray()??new Car[0];
                foreach(var plan in plans.Where(p=>installation.Objects.Markers.Any(m=>m.Id==p.Id&&m.Planned))) {
                    var distance=AutomationNative.Distance(plan.Anchor,false,workCar.OpsLocation,WorkRadius);
                    if(!distance.HasValue||Math.Abs(distance.Value)>WorkRadius)continue;
                    try{CompleteFromWorkTrain(plan.Id,supplies);signalWorkStates[plan.Id]=LocalText.Select("Completed","Fertiggestellt");signalWorkStatus=plan.Name+LocalText.Select(" completed."," fertiggestellt.");if(++completed>=8)return;}
                    catch(InvalidOperationException e){signalWorkStates[plan.Id]=LocalText.Get(e.Message);signalWorkStatus=plan.Name+": "+e.Message;}
                }
            }
        }
        void CompleteFromWorkTrain(string id,Car[] supplies) {
            RequireConstruction();var marker=installation.Objects.Markers.Single(m=>m.Id==id);if(!marker.Planned)return;
            if(!EconomyEnabled){CommitInstall(MaterialEconomy.Complete(installation,id,null,true));return;}
            var previous=new Dictionary<string,CarLoadInfo>();var stock=new List<SupplyStock>();var cars=supplies.ToDictionary(c=>c.id);
            foreach(var car in supplies)for(int slot=0;slot<car.Definition.LoadSlots.Count;slot++) {
                var info=car.GetLoadInfo(slot);if(!info.HasValue)continue;var value=info.Value;
                bool b=value.LoadId==MaterialEconomy.BuildingLoad,t=value.LoadId==MaterialEconomy.TechnologyLoad;if(!b&&!t)continue;
                if(float.IsNaN(value.Quantity)||float.IsInfinity(value.Quantity)||value.Quantity<0||value.Quantity>1000000)throw new InvalidOperationException("Invalid signal material quantity.");
                previous[car.id+":"+slot]=value;stock.Add(new SupplyStock{Car=car.id,Slot=slot,Units=(int)Math.Floor(value.Quantity),Technology=t});
            }
            var cost=MaterialEconomy.Missing(installation,marker);var debits=SignalSupplyPlan.Allocate(cost,stock);
            var next=installation.Clone();next.Economy.Built[id].Paid.B=checked(next.Economy.Built[id].Paid.B+cost.B);next.Economy.Built[id].Paid.T=checked(next.Economy.Built[id].Paid.T+cost.T);next.Economy.Built[id].Depot=null;
            next=MaterialEconomy.Complete(next,id,null,true);
            Atomic(()=>{
                foreach(var debit in debits) {
                    var before=previous[debit.Car+":"+debit.Slot];var car=cars[debit.Car];float quantity=before.Quantity-debit.Units;
                    car.SetLoadInfo(debit.Slot,quantity>.0001f?(CarLoadInfo?)new CarLoadInfo(before.LoadId,quantity,before.OriginId):null);
                    var after=car.GetLoadInfo(debit.Slot);
                    if(Math.Abs((after?.Quantity??0)-quantity)>.001f||after.HasValue&&after.Value.LoadId!=before.LoadId)throw new InvalidOperationException("Signal material consumption was not confirmed.");
                }
                CommitInstall(next);
            },()=>{foreach(var debit in debits)cars[debit.Car].SetLoadInfo(debit.Slot,previous[debit.Car+":"+debit.Slot]);});
        }
    }
}
