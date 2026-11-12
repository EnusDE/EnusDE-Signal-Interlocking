using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class CircuitIdentity {
        public static int Variant(MarkerRecord row)=>row.Kind==MarkerRecord.CircuitA?1:
            row.Kind==MarkerRecord.CircuitB?2:row.CircuitVariant;

        public static string Code(int variant,int number)=>variant<1||number<1?null:
            "GFM-"+(variant==1?"A":"B")+"-"+number.ToString("D3",CultureInfo.InvariantCulture);

        public static bool Enabled(MarkerRecord row)=>row!=null&&!row.Planned&&Variant(row)>0;
        public static string Caption(MarkerRecord row)=>Code(Variant(row),row.CircuitNumber)??row.Name;

        public static void Fill(MarkerDocument document) {
            if(document?.Markers==null)return;
            foreach(var row in document.Markers.Where(m=>m!=null&&m.CircuitNumber==0&&
                (m.IsCircuitBox||m.CircuitVariant>0)))
                row.CircuitNumber=document.NextCircuitNumber++;
        }

        public static void Validate(MarkerDocument doc) {
            if(doc.NextCircuitNumber<1||doc.NextCircuitNumber>1000000000)
                throw new InvalidDataException("Ungültiger GFM-Nummernzähler.");
            var used=new HashSet<int>();
            foreach(var row in doc.Markers) {
                if(row.CircuitVariant<0||row.CircuitVariant>2||row.CircuitNumber<0||
                    row.CircuitNumber>=doc.NextCircuitNumber||row.CircuitNumber>0&&!used.Add(row.CircuitNumber)||
                    row.CircuitVariant>0&&!row.IsSignal||row.CircuitNumber>0&&!row.IsSignal&&!row.IsCircuitBox||
                    double.IsNaN(row.CircuitOffset)||double.IsInfinity(row.CircuitOffset)||Math.Abs(row.CircuitOffset)>2)
                    throw new InvalidDataException("Ungültige oder doppelte Gleisfreimeldekennung.");
            }
        }
    }
}
