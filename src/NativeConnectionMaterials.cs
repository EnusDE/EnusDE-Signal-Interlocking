using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SharedRuntime {
        sealed class WarehousePayment {
            public NativeMaterialWarehouse Warehouse;
            public float BeforeB,BeforeT,AfterB,AfterT;
        }
        InstallationDocument ConnectionMaterials(IEnumerable<string> points,out List<WarehousePayment> payments) {
            var next=installation.Clone();payments=new List<WarehousePayment>();
            var warehouses=MaterialWarehouses;
            foreach(string id in points.Distinct().Where(p=>!next.Economy.ConnectedPoints.Contains(p))) {
                if(next.Economy.Enabled) {
                    var node=Track.Graph.Shared?.GetNode(id)??throw new InvalidOperationException(LocalText.Select("Point is missing: ","Weiche fehlt: ")+id);
                    var cost=new MaterialCost((int)Math.Ceiling(next.Economy.Factor),(int)Math.Ceiling(2*next.Economy.Factor));
                    var legacy=next.Economy.Depots.Where(d=>d.Stock.B>=cost.B&&d.Stock.T>=cost.T)
                        .Select(d=>new{Depot=d,Distance=Vector3.Distance(node.Position,Position(d.Anchor))}).Where(d=>d.Distance<=WorkRadius).OrderBy(d=>d.Distance).FirstOrDefault();
                    WarehousePayment selected=null;float nearest=legacy?.Distance??float.PositiveInfinity;
                    foreach(var warehouse in warehouses) {
                        float distance=Vector3.Distance(node.Position,warehouse.Industry.Position);if(distance>WorkRadius||distance>=nearest)continue;
                        var payment=payments.FirstOrDefault(p=>p.Warehouse.Id==warehouse.Id);
                        if(payment==null){float b=warehouse.Quantity(false),t=warehouse.Quantity(true);NativeMaterialAmounts.Packages(b);NativeMaterialAmounts.Packages(t);payment=new WarehousePayment{Warehouse=warehouse,BeforeB=b,AfterB=b,BeforeT=t,AfterT=t};}
                        if(payment.AfterB<cost.B||payment.AfterT<cost.T)continue;
                        selected=payment;nearest=distance;
                    }
                    if(selected!=null){if(!payments.Contains(selected))payments.Add(selected);selected.AfterB-=cost.B;selected.AfterT-=cost.T;}
                    else if(legacy!=null)MaterialEconomy.Debit(legacy.Depot,cost);
                    else throw new InvalidOperationException(LocalText.Select("Point connection needs signal materials at a nearby MOW warehouse. Deliver materials first.","Die Weichenanbindung benötigt Signalbaumaterial in einem nahen MOW-Lager. Zuerst Material liefern."));
                }
                next.Economy.ConnectedPoints.Add(id);
            }
            next.Validate();return next;
        }
        void WithConnectionMaterials(IEnumerable<string> points,Action<InstallationDocument> commit) {
            var next=ConnectionMaterials(points,out var payments);
            foreach(var p in payments)if(p.Warehouse.Quantity(false)!=p.BeforeB||p.Warehouse.Quantity(true)!=p.BeforeT)throw new InvalidOperationException(LocalText.Select("Material stock changed. Retry the action.","Materialbestand wurde geändert. Aktion erneut ausführen."));
            Atomic(()=>{
                foreach(var p in payments){p.Warehouse.Set(false,p.AfterB);p.Warehouse.Set(true,p.AfterT);if(Math.Abs(p.Warehouse.Quantity(false)-p.AfterB)>.001f||Math.Abs(p.Warehouse.Quantity(true)-p.AfterT)>.001f)throw new InvalidOperationException(LocalText.Select("Point material consumption was not confirmed.","Materialverbrauch der Weichenanbindung wurde nicht bestätigt."));}
                commit(next);
            },()=>{foreach(var p in payments){p.Warehouse.Set(false,p.BeforeB);p.Warehouse.Set(true,p.BeforeT);}});
        }
    }
}
