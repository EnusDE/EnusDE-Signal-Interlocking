using System;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class PointCircuitBoxes {
        public static string Side(int arm,double normalRight,double branchRight) {
            if(arm<0||arm>2||double.IsNaN(normalRight)||double.IsInfinity(normalRight)||
                double.IsNaN(branchRight)||double.IsInfinity(branchRight)||Math.Abs(normalRight-branchRight)<.001)
                throw new ArgumentException("Linken und rechten Weichenast nicht eindeutig erkannt.");
            return arm==0?"S":(arm==1?normalRight<branchRight:branchRight<normalRight)?"L":"R";
        }
        public static MarkerRecord[] Create(string point,string code,MarkerAnchor[] anchors,double normalRight,double branchRight) {
            if(anchors==null||anchors.Length!=3||anchors.Any(a=>a==null))return Array.Empty<MarkerRecord>();
            if(string.IsNullOrWhiteSpace(code)||code=="Weiche")return Array.Empty<MarkerRecord>();
            // An ambiguous geometry produces no guessed L/R labels.
            try{Side(0,normalRight,branchRight);}catch(ArgumentException){return Array.Empty<MarkerRecord>();}
            return Enumerable.Range(0,3).Select(arm=>{
                string side=Side(arm,normalRight,branchRight);
                var anchor=anchors[arm].Clone();anchor.Height=0;anchor.GroundSupport=true;
                return new MarkerRecord{Id=TrackCircuit.PointId(point,arm),Kind=MarkerRecord.CircuitB,
                    Name="GFM-B-"+code+"-"+side,DisplayName=code+" · "+(side=="S"?"Spitzenseite / Stammgleis":side=="L"?"linker Ast":"rechter Ast"),Anchor=anchor};
            }).ToArray();
        }
    }
}
