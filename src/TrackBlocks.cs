using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class TrackBlock {
        public string Id,Name,Point;
        [Newtonsoft.Json.JsonProperty(NullValueHandling=Newtonsoft.Json.NullValueHandling.Ignore)]
        public string BoundaryA,BoundaryB;
        [Newtonsoft.Json.JsonIgnore] public bool IsCircuit=>BoundaryA!=null||BoundaryB!=null;
        public List<DirectedRouteSection> Path=new List<DirectedRouteSection>();
        public List<TrackSection> Guards=new List<TrackSection>();
        public TrackBlock Clone(){var copy=(TrackBlock)MemberwiseClone();copy.Path=Path?.Select(p=>p?.Clone()).ToList();copy.Guards=Guards?.Select(s=>s?.Clone()).ToList();return copy;}
        public IEnumerable<TrackSection> Sections=>Path.Select(p=>p.Section).Concat(Guards);
        public void Validate(){if(string.IsNullOrWhiteSpace(Id)||Id.Length>160||string.IsNullOrWhiteSpace(Name)||Name.Length>80||Path==null||Path.Count>2048||Guards==null||Guards.Count>4||Path.Count+Guards.Count==0||Point?.Length>128)throw new InvalidDataException("Belegungsabschnitt ungültig.");if(Path.Any(p=>p==null||p.Section==null)||Guards.Any(s=>s==null))throw new InvalidDataException("Abschnitt ohne Gleisweg.");foreach(var s in Sections)s.Validate();
            if(IsCircuit&&(Point!=null||Guards.Count!=0||Path.Count==0||!TrackCircuit.ValidId(BoundaryA)||
                !TrackCircuit.ValidId(BoundaryB)||BoundaryA==BoundaryB))
                throw new InvalidDataException("Ungültige Grenzen für die Gleisfreimeldung.");
        }
    }
    public static class TrainNumbers {
        public static string Format(string prefix,string number){prefix=(prefix??"").Trim();number=(number??"").Trim();if(number==""&&prefix=="")return "";if(number.Length!=5||prefix.Length>1||number.Any(c=>c<'0'||c>'9')||prefix.Any(c=>c<'0'||c>'9'))throw new InvalidOperationException("Zugnummer fünfstellig, Kennziffer optional einstellig (nur 0–9).");return prefix+number;}
        public static bool TryDisplaySlots(string value,out string slots) {
            slots=value;
            if(value==null||(value.Length!=5&&value.Length!=6)||value.Any(c=>c<'0'||c>'9'))return false;
            int start=value.Length-5,first=start;
            while(first<value.Length-1&&value[first]=='0')first++;
            slots=(start==0?" ":value.Substring(0,1))+new string(' ',first-start)+value.Substring(first);
            return true;
        }
        static bool SameNumber(string a,string b)=>a!=null&&b!=null&&a.Length>=5&&b.Length>=5&&
            string.CompareOrdinal(a,a.Length-5,b,b.Length-5,5)==0;

        public static string[] RemoveNumber(IDictionary<string,string> registry,string value,ISet<string> except) {
            if(string.IsNullOrEmpty(value))return Array.Empty<string>();
            var old=registry.Where(p=>!except.Contains(p.Key)&&SameNumber(p.Value,value)).Select(p=>p.Key).ToArray();
            foreach(string key in old)registry.Remove(key);
            return old;
        }

        public static bool IsNumberPassage(RouteRun run)=>run.StartPassageVerified&&run.ChainReady&&
            run.Stage==RouteStage.Running&&run.ReleaseSelection==null&&!run.Route.Shunting&&!run.NumberPassageHandled&&run.TrainCars?.Length>0;

        public static bool MoveSignalNumber(IDictionary<string,string> numbers,string start,string end) {
            if(start==end||!numbers.TryGetValue(start,out string number))return false;
            if(numbers.TryGetValue(end,out string existing)&&existing!=number)return false;
            numbers[end]=number;
            numbers.Remove(start);
            return true;
        }

        public static bool AdvanceSignalNumbers(IDictionary<string,string> numbers,IEnumerable<RouteRun> passages,Func<string,string> fieldFor=null) {
            fieldFor=fieldFor??(id=>id);
            var pending=passages.Where(IsNumberPassage).ToList();
            var movedBy=new Dictionary<string,HashSet<string>>(StringComparer.Ordinal);
            bool changed=false,advanced;
            do {
                advanced=false;
                for(int i=0;i<pending.Count;i++) {
                    var run=pending[i];
                    string start=fieldFor(run.Route.Entry),end=fieldFor(run.Route.Exit);
                    if(string.IsNullOrEmpty(start)||string.IsNullOrEmpty(end))continue;
                    if(movedBy.TryGetValue(start,out var previous)&&!previous.SetEquals(run.TrainCars))continue;
                    if(!MoveSignalNumber(numbers,start,end))continue;
                    movedBy[end]=new HashSet<string>(run.TrainCars,StringComparer.Ordinal);
                    movedBy.Remove(start);
                    pending.RemoveAt(i--);
                    advanced=changed=true;
                }
            } while(advanced&&pending.Count>0);
            return changed;
        }

        public static string Display(TrackOccupancy occupancy,string[] cars,IReadOnlyDictionary<string,string> registry) {
            if(occupancy==TrackOccupancy.Unknown||cars==null)return "?";if(occupancy==TrackOccupancy.Clear)return "-----";var numbers=cars.Where(id=>registry.ContainsKey(id)).Select(id=>registry[id]).Distinct().ToArray();return numbers.Length==0?"-----":numbers.Length==1?numbers[0]:"mehrere";
        }
    }
    public enum PanelTrackCue { Idle,Unknown,Setting,Route,Occupied }
    public static class PanelTrackPolicy {
        public static PanelTrackCue Resolve(TrackOccupancy occupancy,bool route,bool setting)=>occupancy==TrackOccupancy.Occupied?PanelTrackCue.Occupied:occupancy==TrackOccupancy.Unknown?PanelTrackCue.Unknown:setting?PanelTrackCue.Setting:route?PanelTrackCue.Route:PanelTrackCue.Idle;
    }
}
