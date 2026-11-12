using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public static class SignalLabel {
        public static string Normalize(string text) {
            var value=(text??"").Trim();
            if(value=="")return null;
            Validate(value,null);
            return value;
        }
        public static void Validate(string name,double? size) {
            if(name!=null&&(name.Length>48||name.Any(c=>char.IsControl(c)||c=='<'||c=='>')))
                throw new InvalidOperationException("Anzeigename: höchstens 48 Zeichen, ohne Steuerzeichen oder spitze Klammern.");
            if(size.HasValue&&!(size>=.6&&size<=2))
                throw new InvalidOperationException("Beschriftungsgröße muss zwischen 0,6 und 2 liegen.");
        }
    }

    public sealed class SignalFace {
        public string Id,Code;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string DisplayName;
        public bool Enabled=true,Hp1=true,Hp2=true,Sh1=false;
        public bool DistantHead;
        public string DistantTarget;
        public SignalFace Clone()=>(SignalFace)MemberwiseClone();
        public bool Supports(bool shunting,bool reduced)=>Enabled&&(shunting?Sh1:reduced?Hp2:Hp1);
        public void Validate(){SignalLabel.Validate(DisplayName,null);Guid id;if(!Guid.TryParseExact(Id,"N",out id)||string.IsNullOrWhiteSpace(Code)||Code.Length>48||Code.Any(c=>char.IsControl(c)||c=='<'||c=='>'))throw new InvalidOperationException("Richtung benötigt feste ID und eine gültige Kennung (1–48 Zeichen).");}
    }
    public sealed class MastConfiguration {
        public SignalFace Front,Back;
        public MastConfiguration Clone()=>new MastConfiguration{Front=Front?.Clone(),Back=Back?.Clone()};
        public static MastConfiguration Legacy(MarkerRecord row) {
            var bytes=Guid.ParseExact(row.Id,"N").ToByteArray();bytes[0]^=128;
            return new MastConfiguration{Front=new SignalFace{Id=row.Id,Code=row.Name},Back=new SignalFace{Id=new Guid(bytes).ToString("N"),Code=row.Name.Substring(0,Math.Min(46,row.Name.Length))+"-R",Enabled=false}};
        }
        public static MastConfiguration Effective(MarkerRecord row)=>(row.Mast??Legacy(row)).Clone();
        public void Validate(string ownerId) {
            if(Front==null||Back==null)throw new InvalidOperationException("Mastkonfiguration ist unvollständig.");
            Front.Validate();Back.Validate();
            if(Front.Id!=ownerId||Front.Id==Back.Id||Front.Enabled&&Back.Enabled&&string.Equals(Front.Code,Back.Code,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Richtungs-IDs und Kennungen müssen getrennt bleiben.");
        }
    }
    public static class SignalCatalogue {
        public static IEnumerable<string> Ids(MarkerRecord row) {
            if(row.Kind==MarkerRecord.Main){var mast=MastConfiguration.Effective(row);yield return mast.Front.Id;yield return mast.Back.Id;}
            else if(row.IsSignal)yield return row.Id;
        }
        public static MarkerRecord[] Expand(IEnumerable<MarkerRecord> objects,bool includePlanned=false) {
            var result=new List<MarkerRecord>();
            foreach(var row in objects) {
                if(row.Planned&&!includePlanned)continue;
                if(row.Kind!=MarkerRecord.Main||row.PhysicalId!=null){result.Add(row.Clone());continue;}
                var mast=MastConfiguration.Effective(row);
                for(int side=0;side<2;side++) {
                    var face=side==0?mast.Front:mast.Back;if(!face.Enabled)continue;
                    var signal=row.Clone();signal.Id=face.Id;signal.Name=face.Code;signal.MastName=row.Name;signal.DisplayName=face.DisplayName;signal.FacingReverse=row.FacingReverse^(side==1);signal.LinkedSignal=face.DistantTarget;signal.Mast=null;signal.PhysicalId=row.Id;signal.AllowHp1=face.Hp1;signal.AllowHp2=face.Hp2;signal.AllowSh1=face.Sh1;
                    result.Add(signal);
                }
            }
            return result.ToArray();
        }
        public static bool Supports(MarkerRecord signal,bool shunting,bool reduced)=>signal!=null&&(signal.Kind==MarkerRecord.Shunt?shunting:signal.Kind==MarkerRecord.Main&&(shunting?signal.AllowSh1:reduced?signal.AllowHp2:signal.AllowHp1));
        public static void Validate(IEnumerable<MarkerRecord> objects) {
            var rows=objects.ToArray();var ids=new HashSet<string>(StringComparer.Ordinal);var codes=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var row in rows) {
                if(row.Kind==MarkerRecord.Main)MastConfiguration.Effective(row).Validate(row.Id);
                if(!ids.Add(row.Id))throw new InvalidOperationException("Doppelte interne Objekt-ID.");
                if(row.Kind==MarkerRecord.Main&&!ids.Add(MastConfiguration.Effective(row).Back.Id))throw new InvalidOperationException("Doppelte interne Signal-ID.");
            }
            foreach(var row in Expand(rows,true).Where(r=>r.IsSignal))if(!codes.Add(row.Name))throw new InvalidOperationException("Signalkennung bereits vergeben: "+row.Name);
        }
    }
}
