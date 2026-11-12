using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class AuxiliaryTarget {
        public string Id=Guid.NewGuid().ToString("N");
        public string Name="";
        public MarkerAnchor Anchor;
        public bool Reverse,Train=true,Shunt;
        public AuxiliaryTarget Clone(){var copy=(AuxiliaryTarget)MemberwiseClone();copy.Anchor=Anchor?.Clone();return copy;}
        [Newtonsoft.Json.JsonIgnore]
        public string Caption=>string.IsNullOrWhiteSpace(Name)?"HZ-"+Id.Substring(0,6).ToUpperInvariant():Name;
        public void Validate() {
            if(!Guid.TryParseExact(Id,"N",out _)||Name==null||Name.Length>48||Name.Any(char.IsControl)||
                Anchor==null||!Train&&!Shunt)throw new InvalidDataException("Hilfsziel benötigt Gleisposition und mindestens eine Fahrart.");
            Anchor.Validate();
        }
        public bool SamePath(AuxiliaryTarget b)=>b!=null&&Id==b.Id&&Train==b.Train&&Shunt==b.Shunt&&Reverse==b.Reverse&&
            Anchor?.Segment==b.Anchor?.Segment&&Anchor?.Meters==b.Anchor?.Meters;
        public bool SameContent(AuxiliaryTarget b)=>SamePath(b)&&Name==b.Name;
        public MarkerRecord Endpoint()=>new MarkerRecord{Id=Id,Name=Caption,Kind=MarkerRecord.Auxiliary,Anchor=Anchor,
            FacingReverse=Reverse,AllowHp1=Train,AllowHp2=Train,AllowSh1=Shunt};

        public static bool IsEndpoint(MarkerRecord row)=>row!=null&&row.Kind==MarkerRecord.Auxiliary;
        public static bool Accepts(MarkerRecord row,bool shunting)=>row!=null&&(IsEndpoint(row)?
            shunting?row.AllowSh1:row.AllowHp1:row.Kind==MarkerRecord.Main||row.Kind==MarkerRecord.Shunt);

        public static Dictionary<string,AuxiliaryTarget> Index(IEnumerable<PanelTile> tiles) {
            var result=new Dictionary<string,AuxiliaryTarget>(StringComparer.Ordinal);
            foreach(var target in tiles.Select(t=>t.Target).Where(t=>t!=null)) {
                if(result.TryGetValue(target.Id,out var previous)&&!target.SameContent(previous))
                    throw new InvalidDataException("Mehrfach dargestelltes Hilfsziel hat unterschiedliche Einstellungen: "+target.Caption);
                result[target.Id]=target;
            }
            return result;
        }

        public static bool HasBlockingSignal(IEnumerable<DirectedRouteSection> path,IEnumerable<MarkerRecord> signals,
            string start,bool shunting) {
            var pieces=path.ToArray();
            if(pieces.Length==0)return false;
            double total=pieces.Sum(p=>p.Section.To-p.Section.From),traversed=0;
            var bySegment=signals.Where(s=>s.Id!=start&&s.Anchor!=null&&
                (s.Kind==MarkerRecord.Main||shunting&&s.Kind==MarkerRecord.Shunt)).ToLookup(s=>s.Anchor.Segment);
            foreach(var piece in pieces) {
                foreach(var signal in bySegment[piece.Section.Segment]) {
                    if(signal.FacingReverse!=piece.Reverse||
                        signal.Anchor.Meters<piece.Section.From-.001||signal.Anchor.Meters>piece.Section.To+.001)continue;
                    double position=traversed+(piece.Reverse?piece.Section.To-signal.Anchor.Meters:signal.Anchor.Meters-piece.Section.From);
                    if(position>.01&&position<=total+.01)return true;
                }
                traversed+=piece.Section.To-piece.Section.From;
            }
            return false;
        }
    }

    public static class SignalCellLayout {
        public static bool ShuntHalf(PanelTile tile,bool right)=>tile.AlternateSignal!=null&&right!=tile.SwapSignalHalves;
        public static string SignalAt(PanelTile tile,bool right)=>ShuntHalf(tile,right)?tile.AlternateSignal:tile.Reference;
        public static int TargetDirection(PanelTile tile)=>(tile.Rotation+(tile.Target?.Reverse==true?2:0))%4;
        public static int MainDirection(PanelTile tile)=>tile.MainSignalDirection??tile.Rotation;
        public static int ShuntDirection(PanelTile tile)=>tile.ShuntSignalDirection??tile.Rotation;
    }
}
