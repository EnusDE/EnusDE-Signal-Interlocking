using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class NumberDisplayReading {public string Field,Number;public bool Timetable;}
    // A consist can straddle several circuits. Detector overlap does not create new number owners.
    public sealed class NumberDisplayOwnership {
        Dictionary<string,string> owners=new Dictionary<string,string>(StringComparer.Ordinal);
        HashSet<string> timetable=new HashSet<string>(StringComparer.Ordinal);
        static string Identity(string number)=>TrainNumbers.TryDisplaySlots(number,out _)?number.Substring(number.Length-5):
            string.IsNullOrWhiteSpace(number)||number=="?"||number=="-----"||number=="mehrere"?null:"id:"+number;
        public void Refresh(IEnumerable<NumberDisplayReading> readings,IEnumerable<string> registered) {
            var reserved=new HashSet<string>(registered.Select(Identity).Where(n=>n!=null),StringComparer.Ordinal);
            var next=new Dictionary<string,string>(StringComparer.Ordinal);
            timetable.Clear();
            foreach(var group in readings.Where(r=>r.Field!=null&&Identity(r.Number)!=null).GroupBy(r=>Identity(r.Number))) {
                bool native=group.Any(r=>r.Timetable);
                if(native)timetable.Add(group.Key);
                else if(reserved.Contains(group.Key))continue;
                owners.TryGetValue(group.Key,out var old);
                var eligible=group.Where(r=>!native||r.Timetable).Select(r=>r.Field).ToArray();
                next[group.Key]=eligible.Contains(old)?old:eligible.OrderBy(k=>k,StringComparer.Ordinal).First();
            }
            owners=next;
        }
        public string Caption(string field,string number) {
            string identity=Identity(number);
            if(identity==null)return number;
            return owners.TryGetValue(identity,out var owner)&&owner==field?number:"-----";
        }
        public bool HasTimetableNumber(string number)=>Identity(number) is string key&&timetable.Contains(key);
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
        string NumberSensor(PanelTile tile)=>tile.Section!=null?tile.Section.Key:TileBlocks(tile).Select(id=>"block:"+id).FirstOrDefault();
        string CarNumberCaption(PanelTile tile) {
            string key=NumberSensor(tile);
            if(key==null)return "-----";
            blockCars.TryGetValue(key,out var cars);
            TrackOccupancy state=TrackOccupancy.Unknown;
            if(panelReadings.TryGetValue(key,out var reading))state=reading;
            var scheduled=NativeNumberCaption(tile);
            if(scheduled!=null)return scheduled;
            string assigned=TrainNumbers.Display(state,cars,Entry.Shared?.NumberRegistry()??new Dictionary<string,string>());
            return assigned=="-----"&&state==TrackOccupancy.Occupied?VehicleNumberCaption(tile):assigned;
        }
        readonly Dictionary<string,(string Vehicle,string Timetable)> vehicleCaptions=new Dictionary<string,(string,string)>();
        string VehicleNumberCaption(PanelTile tile) {
            return ReadNumberVehicle(tile).Vehicle;
        }
        string NativeNumberCaption(PanelTile tile)=>ReadNumberVehicle(tile).Timetable;
        (string Vehicle,string Timetable) ReadNumberVehicle(PanelTile tile) {
            string key=NumberSensor(tile);
            if(key==null)return ("-----",null);
            if(vehicleCaptions.TryGetValue(key,out var known))return known;
            string number="-----";
            string scheduled=null;
            if(!panelReadings.TryGetValue(key,out var occupancy)||occupancy==TrackOccupancy.Unknown)number="?";
            else if(occupancy==TrackOccupancy.Occupied) {
                number="?";
                if(blockCars.TryGetValue(key,out var ids)&&ids?.Length>0&&TrainController.Shared!=null&&TrainController.Shared.TryGetCarForId(ids[0],out var representative)) {
                    try {
                        var consist=AutomationNative.Consist(representative);
                        var inConsist=new HashSet<string>(consist.Select(c=>c.id),StringComparer.Ordinal);
                        if(ids.All(inConsist.Contains)) {
                            var engines=consist.Where(c=>c.IsLocomotive).ToArray();
                            var lead=engines.Length==1?engines[0]:Automation==null?null:engines.FirstOrDefault(c=>c.id==AutomationNative.Train(representative,Automation)?.Master);
                            if(lead!=null)number=lead.Ident.ToString();
                            var crew=consist.Select(car=>car.trainCrewId).Where(id=>!string.IsNullOrEmpty(id)).Distinct().Take(2).ToArray();
                            var schedule=Model.Ops.Timetable.TimetableController.Shared;
                            if(crew.Length==1&&schedule!=null&&schedule.TryGetTrainForTrainCrewId(crew[0],out var train))
                                scheduled=TrainNumbers.TimetableNumber(train?.Name);
                        }
                    }catch(InvalidOperationException){}
                }
            }
            return vehicleCaptions[key]=(number,scheduled);
        }
        void ElectNumberDisplays() {
            var reserved=Entry.Shared?.NumberFields()?.Values??Enumerable.Empty<string>();
            numberDisplayOwners.Refresh(numberDisplayInputs.Select(s=>new NumberDisplayReading{Field=NumberDisplayKey(s),Number=CarNumberCaption(s.Tile),Timetable=NativeNumberCaption(s.Tile)!=null}),reserved);
        }
    }
}
