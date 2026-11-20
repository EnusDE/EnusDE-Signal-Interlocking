using System;
using System.Linq;
using System.Collections.Generic;
using Model;
using Model.Ops;
using Model.Ops.Definition;
using Game.State;
using Game.History;
using Track;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Functional logistics adapter. No diagnostic code changes shared properties.
    public sealed partial class SharedRuntime {
        public string SelectedDepotId;
        public const double WorkRadius=250,UnloadRadius=30;
        void RequireReady(){RequireHost();if(!Ready)throw new InvalidOperationException("Gemeinsame Anlage noch nicht bereit.");}
        static Model.Car FreightCar(string id) {
            Model.Car car;if(TrainController.Shared==null||!TrainController.Shared.TryGetCarForId(id,out car)||car==null||!car.IsOwnedByPlayer||car.IsDerailed||car.IsInBardo||!car.IsStopped(.05f))throw new InvalidOperationException("Eigenen, stehenden, aufgegleisten Güterwagen auswählen.");
            if(car.Waybill.HasValue)throw new InvalidOperationException("Wagen hat einen Frachtauftrag. Erst den Auftrag abschließen; fremde Fracht wird nicht verändert.");return car;
        }
        static Vector3 Position(MarkerAnchor anchor)=>SignalWorldBridge.Location(new MarkerRecord{Anchor=anchor}).GetPosition(PositionAccuracy.High);
        public static double Distance(MarkerAnchor a,MarkerAnchor b)=>Vector3.Distance(Position(a),Position(b));
        string DepotForPoint(string id) {
            var node=Graph.Shared?.GetNode(id);if(node==null)throw new InvalidOperationException("Weiche nicht im Gleisnetz: "+id);
            int b=(int)Math.Ceiling(EconomyFactor),t=(int)Math.Ceiling(2*EconomyFactor);
            return installation.Economy.Depots.Where(d=>d.Stock.B>=b&&d.Stock.T>=t).Select(d=>new{d.Id,Distance=Vector3.Distance(node.Position,Position(d.Anchor))}).Where(d=>d.Distance<=WorkRadius).OrderBy(d=>d.Distance).Select(d=>d.Id).FirstOrDefault();
        }
        public void CreateDepot(string carId,string name) {
            RequireReady();var car=FreightCar(carId);if(installation.Economy.Depots.Count>=16)throw new InvalidOperationException("Höchstens 16 Materiallager.");var at=car.OpsLocation;if(!at.IsValid)throw new InvalidOperationException("Gleisposition des Wagens nicht verfügbar.");if(!at.EndIsA)at=at.Flipped();
            name=(name??"").Trim();if(string.IsNullOrEmpty(name)||name.Length>48||name.Any(char.IsControl))throw new InvalidOperationException("Lagernamen mit 1–48 Zeichen eingeben.");
            var next=installation.Clone();var depot=new MaterialDepot{Name=name,Anchor=new MarkerAnchor{Segment=at.segment.id,Meters=at.distance,Lateral=0,Height=0,Scale=1}};next.Economy.Depots.Add(depot);CommitInstall(next);SelectedDepotId=depot.Id;
        }
        public void Finish(string id,string depotId) {
            RequireReady();var marker=installation.Objects.Markers.FirstOrDefault(m=>m.Id==id)??throw new InvalidOperationException("Bauauftrag nicht vorhanden.");if(!marker.Planned)return;
            if(!StateManager.IsSandbox)throw new InvalidOperationException(LocalText.Select("Finish construction with an active MOW work train.","Bau mit einem aktiven MOW-Arbeitszug fertigstellen."));
            var depot=installation.Economy.Depots.FirstOrDefault(d=>d.Id==depotId);if(EconomyEnabled&&(depot==null||Distance(marker.Anchor,depot.Anchor)>WorkRadius))throw new InvalidOperationException("Materiallager muss höchstens 250 m von der Baustelle entfernt sein.");
            CommitInstall(MaterialEconomy.Complete(installation,id,depotId));
        }
        internal void FinishSandbox(string id) {
            RequireReady();
            if(!StateManager.IsSandbox)throw new InvalidOperationException("Kostenlose Einzelfertigstellung ist nur in einer Sandbox möglich.");
            CommitInstall(MaterialEconomy.Complete(installation,id,null,true));
        }
        static Load SupplyLoad(bool technology)=>CarLoadLibrary.GetLoad(technology?MaterialEconomy.TechnologyLoad:MaterialEconomy.BuildingLoad)??throw new InvalidOperationException("Signalbau-Frachtdefinition nicht geladen.");
        static int Slot(Model.Car car,Load load,out CarLoadInfo? previous,out float capacity) {
            string type=car.CarType.ToString();if(!load.carTypeFilter.Matches(type))throw new InvalidOperationException("Baumaterial benötigt einen FM-Flachwagen, Signaltechnik einen XM-Güterwagen.");
            var slots=car.Definition.LoadSlots;int chosen=-1;previous=null;capacity=0;
            for(int i=0;i<slots.Count;i++){var info=car.GetLoadInfo(i);if(info.HasValue&&info.Value.Quantity>0&&info.Value.LoadId!=load.id)throw new InvalidOperationException("Wagen enthält andere Fracht; zuerst regulär entladen.");if(chosen<0&&(string.IsNullOrEmpty(slots[i].RequiredLoadIdentifier)||slots[i].RequiredLoadIdentifier==load.id)){chosen=i;previous=info;capacity=slots[i].CalculateMaximumCapacity(load,car);}}
            if(chosen<0||float.IsNaN(capacity)||float.IsInfinity(capacity)||capacity<=0)throw new InvalidOperationException("Kein geeigneter Laderaum für diese Fracht.");return chosen;
        }
        static Interchange Source(Model.Car car) {
            if(OpsController.Shared==null)throw new InvalidOperationException("Betriebsverwaltung nicht bereit.");
            foreach(var source in OpsController.Shared.EnabledInterchanges.Where(i=>!i.Disabled&&i.SuppliesFromOffMap))foreach(var span in source.TrackSpans)if(span.IsValid&&span.Contains(car.WheelBoundsF)&&span.Contains(car.WheelBoundsR))return source;
            throw new InvalidOperationException("Wagen vollständig auf ein aktives Übergabegleis mit Materialimport stellen.");
        }
        public void Buy(string carId,bool technology,int quantity,string token) {
            RequireReady();if(installation.Economy.Receipts.Contains(token))return;if(quantity<1||quantity>100)throw new InvalidOperationException("Menge zwischen 1 und 100 Baupaketen eingeben.");
            var car=FreightCar(carId);var load=SupplyLoad(technology);var source=Source(car);CarLoadInfo? old;float capacity;int slot=Slot(car,load,out old,out capacity);float before=old?.Quantity??0;if(before+quantity>capacity+.0001f)throw new InvalidOperationException("Wagenkapazität reicht nicht; verfügbar: "+Math.Floor(capacity-before)+" Baupakete.");
            int price=checked((int)Math.Ceiling(load.costPerUnit*quantity));var state=StateManager.Shared;if(price<1||!state.CanAfford(price))throw new InvalidOperationException("Unternehmensguthaben reicht nicht für die Materialbestellung.");
            var next=installation.Clone();if(!MaterialEconomy.Receipt(next.Economy,token))return;next.Validate();int balance=state.Balance;
            Atomic(()=>{car.SetLoadInfo(slot,new CarLoadInfo(load.id,before+quantity,source.Identifier));var applied=car.GetLoadInfo(slot);if(!applied.HasValue||applied.Value.LoadId!=load.id||Math.Abs(applied.Value.Quantity-before-quantity)>.0001f)throw new InvalidOperationException("Beladung nicht bestätigt.");CommitInstall(next);state.ApplyToBalance(-price,Ledger.Category.MaintenanceOfWay,null,"Signalbau: "+quantity+" × "+load.description,1,false);},()=>{car.SetLoadInfo(slot,old);if(state.Balance!=balance)state.ApplyToBalance(balance-state.Balance,Ledger.Category.MaintenanceOfWay,null,"Rücknahme fehlgeschlagener Signalbaubestellung",1,false);});
        }
        public void Unload(string carId,string depotId,string token) {
            RequireReady();if(installation.Economy.Receipts.Contains(token))return;var car=FreightCar(carId);var depot=installation.Economy.Depots.FirstOrDefault(d=>d.Id==depotId)??throw new InvalidOperationException("Materiallager auswählen.");
            var at=car.OpsLocation;if(!at.IsValid)throw new InvalidOperationException("Wagenposition unbekannt.");if(!at.EndIsA)at=at.Flipped();if(Distance(depot.Anchor,new MarkerAnchor{Segment=at.segment.id,Meters=at.distance,Lateral=0,Height=0})>UnloadRadius)throw new InvalidOperationException("Wagen höchstens 30 m vom Materiallager entfernt abstellen.");
            var before=new Dictionary<int,CarLoadInfo?>();var next=installation.Clone();var target=next.Economy.Depots.First(d=>d.Id==depotId);int count=car.Definition.LoadSlots.Count,total=0;
            for(int i=0;i<count;i++){var info=car.GetLoadInfo(i);if(!info.HasValue)continue;bool b=info.Value.LoadId==MaterialEconomy.BuildingLoad,t=info.Value.LoadId==MaterialEconomy.TechnologyLoad;if(!b&&!t)continue;float amount=info.Value.Quantity;if(float.IsNaN(amount)||float.IsInfinity(amount)||amount<0||amount>1000000)throw new InvalidOperationException("Materialladung ungültig.");int units=(int)Math.Floor(amount);if(units==0)continue;before[i]=info;total=checked(total+units);if(b)target.Stock.B=checked(target.Stock.B+units);else target.Stock.T=checked(target.Stock.T+units);}
            if(total==0)throw new InvalidOperationException("Keine vollständigen Signalbaupakete im Wagen.");if(!MaterialEconomy.Receipt(next.Economy,token))return;next.Validate();
            Atomic(()=>{foreach(var entry in before){var info=entry.Value.Value;float remain=info.Quantity-(float)Math.Floor(info.Quantity);car.SetLoadInfo(entry.Key,remain>.0001f?(CarLoadInfo?)new CarLoadInfo(info.LoadId,remain,info.OriginId):null);var after=car.GetLoadInfo(entry.Key);if((after?.Quantity??0)>remain+.0001f)throw new InvalidOperationException("Entladung nicht bestätigt.");}CommitInstall(next);},()=>{foreach(var entry in before)car.SetLoadInfo(entry.Key,entry.Value);});
        }
    }
}
