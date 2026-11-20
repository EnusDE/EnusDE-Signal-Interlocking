using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class ConstructionPolicy {
        public const int UnlockCost=7500;
        public static bool MaterialsRequired(bool sandbox,bool requireInSandbox,bool freeInEconomy)=>sandbox?requireInSandbox:!freeInEconomy;
        public static InstallationDocument MigrateLegacy(InstallationDocument installation,bool sandbox) {
            if(installation.Economy.PolicyVersion!=0)return installation.Clone();
            var next=installation.Clone();next.Economy.PolicyVersion=1;
            if(next.Objects.Markers.Count>0)next.Economy.Unlocked=true;
            if(sandbox)next.Economy.MaterialsInSandbox=next.Economy.Enabled;
            else next.Economy.FreeInEconomy=!next.Economy.Enabled;
            return next;
        }
        public static MaterialCost TotalMissing(InstallationDocument installation) {
            var cost=new MaterialCost();
            foreach(var marker in installation.Objects.Markers.Where(m=>m.Planned)){var missing=MaterialEconomy.Missing(installation,marker);cost.B=checked(cost.B+missing.B);cost.T=checked(cost.T+missing.T);}
            return cost;
        }
    }
    public sealed class SupplyDebit {
        public string Car;public int Slot,Units;public bool Technology;
    }
    public sealed class SupplyStock {
        public string Car;public int Slot,Units;public bool Technology;
    }
    public static class SignalSupplyPlan {
        public static List<SupplyDebit> Allocate(MaterialCost cost,IEnumerable<SupplyStock> stock) {
            cost.Validate();var result=new List<SupplyDebit>();int building=cost.B,technology=cost.T;
            foreach(var item in stock) {
                if(item==null||item.Units<0||item.Slot<0||string.IsNullOrWhiteSpace(item.Car))throw new ArgumentException("Ungültiger Materialwagen.");
                int take=Math.Min(item.Units,item.Technology?technology:building);if(take==0)continue;
                result.Add(new SupplyDebit{Car=item.Car,Slot=item.Slot,Units=take,Technology=item.Technology});
                if(item.Technology)technology-=take;else building-=take;
            }
            if(building!=0||technology!=0)throw new InvalidOperationException("Signalbaumaterial im Arbeitszug fehlt: "+building+" / "+technology);
            if(result.Select(p=>p.Car+":"+p.Slot).Distinct().Count()!=result.Count)throw new ArgumentException("Doppelter Laderaum im Materialplan.");
            return result;
        }
    }
}
