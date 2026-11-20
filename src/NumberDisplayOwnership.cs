using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class NumberDisplayReading {public string Field,Number;}
    // A consist can straddle several circuits. Detector overlap does not create new number owners.
    public sealed class NumberDisplayOwnership {
        Dictionary<string,string> owners=new Dictionary<string,string>(StringComparer.Ordinal);
        static string Identity(string number)=>TrainNumbers.TryDisplaySlots(number,out _)?number.Substring(number.Length-5):null;
        public void Refresh(IEnumerable<NumberDisplayReading> readings,IEnumerable<string> registered) {
            var reserved=new HashSet<string>(registered.Select(Identity).Where(n=>n!=null),StringComparer.Ordinal);
            var next=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var group in readings.Where(r=>r.Field!=null&&Identity(r.Number)!=null).GroupBy(r=>Identity(r.Number))) {
                if(reserved.Contains(group.Key))continue;
                owners.TryGetValue(group.Key,out var old);
                next[group.Key]=group.Any(r=>r.Field==old)?old:group.Select(r=>r.Field).OrderBy(k=>k,StringComparer.Ordinal).First();
            }
            owners=next;
        }
        public string Caption(string field,string number) {
            string identity=Identity(number);
            if(identity==null)return number;
            return owners.TryGetValue(identity,out var owner)&&owner==field?number:"-----";
        }
    }
    public sealed partial class SignalRuntime {
        readonly NumberDisplayOwnership numberDisplayOwners=new NumberDisplayOwnership();
        NumberMirrorSource[] numberDisplayInputs=Array.Empty<NumberMirrorSource>();
        bool numberDisplayOverflow;
        NumberMirrorSource[] NumberDisplayInputs() {
            var fields=(desks?.Desks??new List<DeskDefinition>()).SelectMany(d=>d.Panel.Tiles
                .Where(t=>t.Kind==PanelTileKind.TrainNumber&&!NumberMirrors.IsMirror(t)).Select(t=>new NumberMirrorSource{Desk=d,Tile=t})).Take(513).ToArray();
            numberDisplayOverflow=fields.Length>512;
            return numberDisplayInputs=numberDisplayOverflow?Array.Empty<NumberMirrorSource>():fields;
        }
        static string NumberDisplayKey(NumberMirrorSource source)=>source==null?null:source.Desk.Id+":"+source.Tile.X+":"+source.Tile.Y;
        string NumberDisplayKey(PanelTile tile) {
            var direct=numberDisplayInputs.FirstOrDefault(s=>ReferenceEquals(s.Tile,tile));if(direct!=null)return NumberDisplayKey(direct);
            if(CurrentDesk==null)return null;
            var module=CurrentDesk.IsMaster?MasterDiagram.At(desks,CurrentDesk.Id,tile.X,tile.Y):null;
            string owner=module?.Desk??CurrentDesk.Id;
            return owner+":"+(tile.X-(module?.X??0))+":"+(tile.Y-(module?.Y??0));
        }
        string CarNumberCaption(PanelTile tile) {
            string key=tile.Section!=null?tile.Section.Key:TileBlocks(tile).Select(id=>"block:"+id).FirstOrDefault();
            if(key==null)return "-----";
            blockCars.TryGetValue(key,out var cars);
            TrackOccupancy state=TrackOccupancy.Unknown;
            if(panelReadings.TryGetValue(key,out var reading))state=reading;
            return TrainNumbers.Display(state,cars,Entry.Shared?.NumberRegistry()??new Dictionary<string,string>());
        }
        void ElectNumberDisplays() {
            var reserved=Entry.Shared?.NumberFields()?.Values??Enumerable.Empty<string>();
            numberDisplayOwners.Refresh(numberDisplayInputs.Select(s=>new NumberDisplayReading{Field=NumberDisplayKey(s),Number=CarNumberCaption(s.Tile)}),reserved);
        }
    }
}
