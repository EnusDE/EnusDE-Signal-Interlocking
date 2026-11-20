using System;
using System.Linq;
using System.Collections.Generic;
namespace EnusDE.Signaltechnik {
    public enum LineSignalGroup { ADepartures,BArrivals,BDepartures,AArrivals }
    public static class LineSignalGroups {
        public static List<string> Get(LineTrackDefinition line,LineSignalGroup group) {
            var extra=group==LineSignalGroup.ADepartures?line.ADepartures:group==LineSignalGroup.BArrivals?line.BArrivals:group==LineSignalGroup.BDepartures?line.BDepartures:line.AArrivals;
            string first=group==LineSignalGroup.ADepartures?line.ForwardStart:group==LineSignalGroup.BArrivals?line.ForwardEnd:group==LineSignalGroup.BDepartures?line.ReverseStart:line.ReverseEnd;
            return new[]{first}.Concat(extra??Enumerable.Empty<string>()).Where(id=>!string.IsNullOrEmpty(id)).Distinct().ToList();
        }
        public static void Put(LineTrackDefinition line,LineSignalGroup group,IEnumerable<string> ids) {
            var values=ids.Where(id=>!string.IsNullOrEmpty(id)).Distinct().ToList();string first=values.FirstOrDefault()??"";
            if(group==LineSignalGroup.ADepartures){line.ForwardStart=first;line.ADepartures=values;}
            if(group==LineSignalGroup.BArrivals){line.ForwardEnd=first;line.BArrivals=values;}
            if(group==LineSignalGroup.BDepartures){line.ReverseStart=first;line.BDepartures=values;}
            if(group==LineSignalGroup.AArrivals){line.ReverseEnd=first;line.AArrivals=values;}
        }
        public static IEnumerable<LineSignalGroup> Used(LineTrackDefinition line)=>line.Mode==LineTrackMode.Directional?new[]{LineSignalGroup.ADepartures,LineSignalGroup.BArrivals}:Enum.GetValues(typeof(LineSignalGroup)).Cast<LineSignalGroup>();
        public static IEnumerable<string> All(LineTrackDefinition line)=>Used(line).SelectMany(g=>Get(line,g)).Distinct();
        public static string Station(LineTrackDefinition line,LineSignalGroup group)=>group==LineSignalGroup.ADepartures||group==LineSignalGroup.AArrivals?line.StationA:line.StationB;
        public static bool Departure(LineSignalGroup group)=>group==LineSignalGroup.ADepartures||group==LineSignalGroup.BDepartures;
        public static bool AcceptsEndpoint(MarkerRecord signal,bool departure)=>signal!=null&&!signal.Planned&&(signal.Kind==MarkerRecord.Main||!departure&&AuxiliaryTarget.IsEndpoint(signal)&&AuxiliaryTarget.Accepts(signal,false));
    }
}
