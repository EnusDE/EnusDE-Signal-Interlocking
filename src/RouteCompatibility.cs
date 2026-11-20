using System;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class RouteCompatibility {
        // Monitoring the same empty corridor is compatible. Occupying any part of it
        // with an actual route remains exclusive. Legacy routes keep their old limits.
        public static bool SectionConflict(RouteDefinition a,TrackSection x,RouteDefinition b,TrackSection y) {
            if(!x.Overlaps(y))return false;
            if(a.Path==null||b.Path==null)return true;
            var common=new TrackSection{Segment=x.Segment,From=Math.Max(x.From,y.From),To=Math.Min(x.To,y.To)};
            return a.Path.Any(p=>p.Section.Overlaps(common))||b.Path.Any(p=>p.Section.Overlaps(common));
        }
        public static bool SwitchConflict(RouteSwitch a,RouteSwitch b)=>a.Node==b.Node&&(a.Thrown!=b.Thrown||a.FlankOf==null&&b.FlankOf==null);
    }
}
