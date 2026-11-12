using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public sealed class MaterialCost {
        public int B,T;
        public MaterialCost(){}
        public MaterialCost(int b,int t){B=b;T=t;}
        public MaterialCost Clone()=>new MaterialCost(B,T);
        public void Validate(){if(B<0||T<0||B>1000000||T>1000000)throw new InvalidDataException("Ungültiger Materialbestand.");}
        public override string ToString()=>B+" Baumaterial / "+T+" Signaltechnik";
    }
    public sealed class MaterialDepot {
        public string Id=Guid.NewGuid().ToString("N"),Name="Materiallager";
        public MarkerAnchor Anchor;
        public MaterialCost Stock=new MaterialCost();
    }
    public sealed class BuiltMaterial {
        public MaterialCost Budget=new MaterialCost(),Paid=new MaterialCost();
        public string Depot;
    }
    public sealed class MaterialLedger {
        public bool Enabled;
        public double Factor=1;
        public List<MaterialDepot> Depots=new List<MaterialDepot>();
        public Dictionary<string,BuiltMaterial> Built=new Dictionary<string,BuiltMaterial>();
        public List<string> ConnectedPoints=new List<string>(),Receipts=new List<string>();
        public void Validate() {
            if(double.IsNaN(Factor)||double.IsInfinity(Factor)||Factor<.25||Factor>4||Depots==null||Depots.Count>16||Built==null||Built.Count>MarkerRepository.Maximum||ConnectedPoints==null||ConnectedPoints.Count>4096||ConnectedPoints.Any(s=>string.IsNullOrWhiteSpace(s)||s.Length>128)||ConnectedPoints.Distinct().Count()!=ConnectedPoints.Count||Receipts==null||Receipts.Count>64||Receipts.Any(s=>s==null||s.Length!=32))throw new InvalidDataException("Materialverwaltung ungültig.");
            foreach(var depot in Depots){Guid id;if(depot==null||!Guid.TryParseExact(depot.Id,"N",out id)||string.IsNullOrWhiteSpace(depot.Name)||depot.Name.Length>48||depot.Anchor==null||depot.Stock==null)throw new InvalidDataException("Materiallager ungültig.");depot.Anchor.Validate();depot.Stock.Validate();}
            if(Depots.Select(d=>d.Id).Distinct().Count()!=Depots.Count)throw new InvalidDataException("Materiallager-ID doppelt.");
            foreach(var item in Built){Guid id;if(!Guid.TryParseExact(item.Key,"N",out id)||item.Value==null||item.Value.Budget==null||item.Value.Paid==null)throw new InvalidDataException("Bauabrechnung ungültig.");item.Value.Budget.Validate();item.Value.Paid.Validate();}
        }
    }
    public sealed class InstallationDocument {
        public int Protocol=1;
        public string WorldKey,Map;
        public MarkerDocument Objects;
        public MaterialLedger Economy=new MaterialLedger();
        public ServerPlacement Placement=new ServerPlacement();
        public Dictionary<string,string> TrainNumbers=new Dictionary<string,string>();
        public Dictionary<string,string> SignalNumberFields=new Dictionary<string,string>();
        public List<RetiredSignal> Retired=new List<RetiredSignal>();
        public InstallationDocument Clone()=>JsonConvert.DeserializeObject<InstallationDocument>(JsonConvert.SerializeObject(this));
        public void Validate() {
            if(Retired==null||Retired.Count>512||Retired.Any(r=>r==null)||Retired.Select(r=>r.Id).Distinct().Count()!=Retired.Count)throw new InvalidDataException("Ungültigkeitsmarkierungen ungültig.");foreach(var retired in Retired)retired.Validate();
            if(Protocol!=1||WorldKey==null||WorldKey.Length!=64||WorldKey.Any(c=>!Uri.IsHexDigit(c))||string.IsNullOrWhiteSpace(Map)||Objects==null||Objects.WorldKey!=WorldKey||Objects.Map!=Map||Economy==null||Placement==null)throw new InvalidDataException("Gemeinsamer Anlagenbestand ungültig.");
            MarkerRepository.ValidateDocument(Objects,WorldKey,Map);Economy.Validate();if(Economy.Built.Keys.Any(id=>!Objects.Markers.Any(m=>m.Id==id)))throw new InvalidDataException("Bauabrechnung ohne Anlage.");
            Placement.Validate();
            if(TrainNumbers==null||TrainNumbers.Count>2048||TrainNumbers.Any(p=>string.IsNullOrWhiteSpace(p.Key)||p.Key.Length>128||p.Value==null||p.Value.Length<5||p.Value.Length>6||p.Value.Any(c=>c<'0'||c>'9')))throw new InvalidDataException("Zugnummernzuordnung ungültig.");
            if(SignalNumberFields==null||SignalNumberFields.Count>2048||SignalNumberFields.Any(p=>
                !Guid.TryParseExact(p.Key,"N",out _)||!EnusDE.Signaltechnik.TrainNumbers.TryDisplaySlots(p.Value,out _)))
                throw new InvalidDataException("Zugnummernfelder ungültig.");
        }
    }
    public static class MaterialEconomy {
        public const string BuildingLoad="enusde/signal-building",TechnologyLoad="enusde/signal-technology";
        public static MaterialCost Cost(MarkerRecord marker,double factor=1) {
            int b=1,t=0;
            if(marker.Kind==MarkerRecord.Main){b=3;var mast=MastConfiguration.Effective(marker);t=0;foreach(var face in new[]{mast.Front,mast.Back})if(face.Enabled){t+=t==0?2:1;if(face.DistantHead)t++;if(face.Sh1)t++;}}
            else if(marker.Kind==MarkerRecord.Distant){b=2;t=1;}else if(marker.Kind==MarkerRecord.Shunt||marker.IsCircuitBox)t=1;
            return new MaterialCost((int)Math.Ceiling(b*factor),(int)Math.Ceiling(t*factor));
        }
        public static MaterialCost Missing(InstallationDocument doc,MarkerRecord marker) {
            BuiltMaterial built;doc.Economy.Built.TryGetValue(marker.Id,out built);var cost=Cost(marker);var budget=built?.Budget??new MaterialCost();
            return new MaterialCost((int)Math.Ceiling(Math.Max(0,cost.B-budget.B)*doc.Economy.Factor),(int)Math.Ceiling(Math.Max(0,cost.T-budget.T)*doc.Economy.Factor));
        }
        public static InstallationDocument Markers(InstallationDocument before,MarkerDocument next) {
            var doc=before.Clone();MarkerRepository.ValidateDocument(next,doc.WorldKey,doc.Map);doc.Objects=JsonConvert.DeserializeObject<MarkerDocument>(JsonConvert.SerializeObject(next));
            if(doc.SignalNumberFields.Count>0) {
                var signalIds=new HashSet<string>(SignalCatalogue.Expand(doc.Objects.Markers,true).Where(r=>r.Kind==MarkerRecord.Main).Select(r=>r.Id));
                foreach(string id in doc.SignalNumberFields.Keys.Where(id=>!signalIds.Contains(id)).ToArray())doc.SignalNumberFields.Remove(id);
            }
            foreach(var removed in before.Objects.Markers.Where(m=>!next.Markers.Any(n=>n.Id==m.Id))){BuiltMaterial paid;if(doc.Economy.Built.TryGetValue(removed.Id,out paid)){var depot=doc.Economy.Depots.FirstOrDefault(d=>d.Id==paid.Depot);if(depot!=null){depot.Stock.B=checked(depot.Stock.B+paid.Paid.B/2);depot.Stock.T=checked(depot.Stock.T+paid.Paid.T/2);}doc.Economy.Built.Remove(removed.Id);}}
            foreach(var marker in doc.Objects.Markers){var old=before.Objects.Markers.FirstOrDefault(m=>m.Id==marker.Id);if(old!=null&&old.Kind!=marker.Kind)throw new InvalidOperationException("Objekttyp nur über Rückbau und Neubau ändern.");BuiltMaterial built;if(!doc.Economy.Built.TryGetValue(marker.Id,out built))doc.Economy.Built[marker.Id]=built=new BuiltMaterial();
                if(!doc.Economy.Enabled){marker.Planned=old?.Planned??false;if(!marker.Planned){var cost=Cost(marker);built.Budget.B=Math.Max(built.Budget.B,cost.B);built.Budget.T=Math.Max(built.Budget.T,cost.T);}}else {var missing=Missing(doc,marker);marker.Planned=missing.B>0||missing.T>0;}
                if(old!=null&&old.Planned!=marker.Planned)marker.VisualRevision++;
            }
            doc.Validate();return doc;
        }
        public static void Debit(MaterialDepot depot,MaterialCost cost){cost.Validate();if(depot==null||depot.Stock.B<cost.B||depot.Stock.T<cost.T)throw new InvalidOperationException("Material fehlt: benötigt "+cost);depot.Stock.B-=cost.B;depot.Stock.T-=cost.T;}
        public static InstallationDocument Complete(InstallationDocument before,string id,string depotId)=>Complete(before,id,depotId,false);
        internal static InstallationDocument Complete(InstallationDocument before,string id,string depotId,bool sandboxFree) {
            var doc=before.Clone();var marker=doc.Objects.Markers.FirstOrDefault(m=>m.Id==id)??throw new InvalidOperationException("Bauauftrag fehlt.");if(!marker.Planned)return doc;
            var depot=doc.Economy.Depots.FirstOrDefault(d=>d.Id==depotId);
            var payment=doc.Economy.Built[id];
            if(!sandboxFree) {
                var cost=doc.Economy.Enabled?Missing(doc,marker):new MaterialCost();
                if(doc.Economy.Enabled)Debit(depot,cost);
                payment.Paid.B=checked(payment.Paid.B+cost.B);
                payment.Paid.T=checked(payment.Paid.T+cost.T);
                payment.Depot=depot?.Id;
            }
            var baseCost=Cost(marker);payment.Budget.B=Math.Max(payment.Budget.B,baseCost.B);payment.Budget.T=Math.Max(payment.Budget.T,baseCost.T);marker.Planned=false;marker.VisualRevision++;doc.Validate();return doc;
        }
        public static InstallationDocument Connect(InstallationDocument before,IEnumerable<string> points,Func<string,string> depotFor) {
            var doc=before.Clone();foreach(string id in points.Distinct().Where(id=>!doc.Economy.ConnectedPoints.Contains(id))){if(doc.Economy.Enabled){var depot=doc.Economy.Depots.FirstOrDefault(d=>d.Id==depotFor(id));Debit(depot,new MaterialCost((int)Math.Ceiling(doc.Economy.Factor),(int)Math.Ceiling(2*doc.Economy.Factor)));}doc.Economy.ConnectedPoints.Add(id);}doc.Validate();return doc;
        }
        public static bool Receipt(MaterialLedger ledger,string token) {Guid id;if(!Guid.TryParseExact(token,"N",out id))throw new InvalidOperationException("Vorgangskennung ungültig.");if(ledger.Receipts.Contains(token))return false;ledger.Receipts.Add(token);if(ledger.Receipts.Count>64)ledger.Receipts.RemoveAt(0);return true;}
    }
}
