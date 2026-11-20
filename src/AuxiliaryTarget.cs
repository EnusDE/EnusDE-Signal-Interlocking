using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public enum AuxiliaryFunction { RouteDestination,LinePermission }
    public sealed class AuxiliaryTarget {
        public string Id=Guid.NewGuid().ToString("N");
        public string Name="";
        public MarkerAnchor Anchor;
        public bool Reverse,Train=true,Shunt;
        public AuxiliaryFunction Function;
        public string PermissionLine="";
        public string ReferenceSignal="";
        public AuxiliaryTarget Clone(){var copy=(AuxiliaryTarget)MemberwiseClone();copy.Anchor=Anchor?.Clone();return copy;}
        [Newtonsoft.Json.JsonIgnore]
        public string Caption=>string.IsNullOrWhiteSpace(Name)?"HZ-"+Id.Substring(0,6).ToUpperInvariant():Name;
        public void Validate() {
            if(!Guid.TryParseExact(Id,"N",out _)||Name==null||Name.Length>48||Name.Any(char.IsControl)||
                !Enum.IsDefined(typeof(AuxiliaryFunction),Function)||PermissionLine==null||PermissionLine!=""&&!Guid.TryParseExact(PermissionLine,"N",out _)||ReferenceSignal==null||ReferenceSignal!=""&&!Guid.TryParseExact(ReferenceSignal,"N",out _)||Function==AuxiliaryFunction.RouteDestination&&(Anchor==null||!Train&&!Shunt))throw new InvalidDataException(LocalText.Select("A route destination needs a track position and movement type; check the auxiliary function and line binding.","Ein Fahrstraßenziel benötigt Gleisposition und Fahrart; Hilfstastenfunktion und Streckenzuordnung prüfen."));
            Anchor?.Validate();
        }
        public bool SamePath(AuxiliaryTarget b)=>b!=null&&Id==b.Id&&Function==b.Function&&PermissionLine==b.PermissionLine&&ReferenceSignal==b.ReferenceSignal&&Train==b.Train&&Shunt==b.Shunt&&Reverse==b.Reverse&&
            Anchor?.Segment==b.Anchor?.Segment&&Anchor?.Meters==b.Anchor?.Meters;
        public bool SameContent(AuxiliaryTarget b)=>SamePath(b)&&Name==b.Name;
        public MarkerRecord Endpoint(Func<string,MarkerRecord> resolve=null) {
            if(Function!=AuxiliaryFunction.RouteDestination)return null;
            var reference=ReferenceSignal!=""&&resolve!=null?resolve(ReferenceSignal):null;
            if(ReferenceSignal!=""&&resolve!=null&&(reference?.Kind!=MarkerRecord.Main||reference.Planned))return null;
            return new MarkerRecord{Id=Id,Name=Caption,Kind=MarkerRecord.Auxiliary,Anchor=reference?.Anchor.Clone()??Anchor,
                FacingReverse=reference==null?Reverse:!reference.FacingReverse,AllowHp1=Train,AllowHp2=Train,AllowSh1=Shunt};
        }

        public void SelectDestination(PanelButtons buttons,bool shunting,Func<string,bool> valid) {
            if(buttons.Start==null)throw new InvalidOperationException("Zuerst das Startsignal wählen. Die Hilfstaste ist ausschließlich ein Ziel.");
            if(!Accepts(Endpoint(),shunting))throw new InvalidOperationException("Hilfsziel unterstützt die gewählte Fahrart nicht.");
            if(buttons.Start==Id||!valid(buttons.Start)||!valid(Id))throw new InvalidOperationException("Startsignal oder Hilfsziel ist nicht mehr verfügbar.");
            buttons.ClearEnd();
            buttons.Press(Id,valid);
        }

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
        public static AuxiliaryTarget[] LinkedForSignal(IEnumerable<PanelTile> tiles,string signal) {
            if(string.IsNullOrEmpty(signal))return Array.Empty<AuxiliaryTarget>();
            var fields=tiles.ToArray();var index=Index(fields);
            return fields.Where(t=>t.Kind==PanelTileKind.Signal&&(t.Reference==signal||t.AlternateSignal==signal)&&t.LinkedAuxiliary!=null)
                .Select(t=>t.LinkedAuxiliary).Distinct().Where(index.ContainsKey).Select(id=>index[id]).OrderBy(t=>t.Caption,StringComparer.Ordinal).ToArray();
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
        public static bool HasAuxiliaryHalf(PanelTile tile)=>tile?.Kind==PanelTileKind.Signal&&tile.Target!=null;
        public static bool AuxiliaryAt(PanelTile tile,bool second)=>HasAuxiliaryHalf(tile)&&second==(MainDirection(tile)>=2);
        public static bool ShuntHalf(PanelTile tile,bool right)=>HasAuxiliaryHalf(tile)?!AuxiliaryAt(tile,right):tile.AlternateSignal!=null&&right!=tile.SwapSignalHalves;
        public static string SignalAt(PanelTile tile,bool right)=>AuxiliaryAt(tile,right)?null:ShuntHalf(tile,right)&&tile.AlternateSignal!=null?tile.AlternateSignal:tile.Reference;
        public static void RotateAuxiliaryField(PanelTile tile) {
            tile.Rotation=(tile.Rotation+1)%4;
            if(tile.MainSignalDirection.HasValue)tile.MainSignalDirection=(tile.MainSignalDirection.Value+1)%4;
        }
        public static int AuxiliaryDirection(PanelTile tile)=>(MainDirection(tile)+2)%4;
        public static PanelPoint HalfPoint(PanelTile tile,bool target) {
            int direction=MainDirection(tile);float sign=target?-1:1;
            return direction==0?new PanelPoint(18*sign,0):direction==1?new PanelPoint(0,-12*sign):
                direction==2?new PanelPoint(-18*sign,0):new PanelPoint(0,12*sign);
        }
        public static int TargetDirection(PanelTile tile)=>tile.Rotation%4;
        public static int HeadSideDirection(PanelTile tile)=>MainDirection(tile);
        public static int MainDirection(PanelTile tile)=>tile.MainSignalDirection??tile.Rotation;
        public static int ShuntDirection(PanelTile tile)=>tile.ShuntSignalDirection??tile.Rotation;
    }
}
