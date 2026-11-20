using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class CircuitIdentity {
        public static bool Reverse(MarkerRecord row)=>row.FacingReverse^row.CircuitReverse;
        public static double FacingDegrees(MarkerRecord row)=>((Variant(row)==3?Reverse(row):row.FacingReverse)?180:0)+(Variant(row)==3?0:row.CircuitYaw);
        public static int Variant(MarkerRecord row)=>row.IsCircuitBox?(row.Kind==MarkerRecord.CircuitC?3:2):row.SharedPointBoundary&&row.CircuitVariant==1?2:row.CircuitVariant==2?1:row.CircuitVariant;
        public static bool Accepts(MarkerRecord row,bool direction,bool departing)=>Variant(row)!=3||Reverse(row)==(departing?direction:!direction);

        public static string Code(int variant,int number)=>variant<1||number<1?null:
            "GFM-"+(variant==1?"A":variant==2?"B":"C")+"-"+number.ToString("D3",CultureInfo.InvariantCulture);

        public static bool Enabled(MarkerRecord row)=>row!=null&&!row.Planned&&Variant(row)>0;
        public static string Caption(MarkerRecord row)=>row.SharedPointBoundary&&row.CircuitNumber>0?"GFM-A/B-"+row.CircuitNumber.ToString("D3",CultureInfo.InvariantCulture):Code(Variant(row),row.CircuitNumber)??row.Name;

        public static void Fill(MarkerDocument document) {
            if(document?.Markers==null)return;
            foreach(var row in document.Markers.Where(m=>m!=null)) {
                if(row.Kind==MarkerRecord.CircuitA)row.Kind=MarkerRecord.CircuitB;
                if(row.IsSignal&&row.CircuitVariant==2)row.CircuitVariant=1;
            }
            foreach(var row in document.Markers.Where(m=>m!=null&&m.CircuitNumber==0&&
                (m.IsCircuitBox||m.CircuitVariant>0)))
                row.CircuitNumber=document.NextCircuitNumber++;
        }

        public static void Validate(MarkerDocument doc) {
            if(doc.NextCircuitNumber<1||doc.NextCircuitNumber>1000000000)
                throw new InvalidDataException("Ungültiger GFM-Nummernzähler.");
            var used=new HashSet<int>();
            foreach(var row in doc.Markers) {
                if(row.CircuitVariant<0||row.CircuitVariant>3||row.CircuitNumber<0||
                    row.CircuitNumber>=doc.NextCircuitNumber||row.CircuitNumber>0&&!used.Add(row.CircuitNumber)||
                    row.CircuitVariant>0&&!row.IsSignal||row.CircuitNumber>0&&!row.IsSignal&&!row.IsCircuitBox||
                    double.IsNaN(row.CircuitOffset)||double.IsInfinity(row.CircuitOffset)||Math.Abs(row.CircuitOffset)>2||double.IsNaN(row.CircuitYaw)||double.IsInfinity(row.CircuitYaw)||Math.Abs(row.CircuitYaw)>180)
                    throw new InvalidDataException("Ungültige oder doppelte Gleisfreimeldekennung.");
            }
        }
    }
}
