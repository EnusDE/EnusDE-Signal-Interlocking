using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public static class LineConfigurationChanges {
        public static string Technical(LineTrackDefinition l)=>JsonConvert.SerializeObject(new{l.StationA,l.StationB,l.Enabled,l.Mode,l.NormalDirection,l.Automatic,l.RequireRequest,l.ForwardStart,l.ForwardEnd,l.ReverseStart,l.ReverseEnd,l.ADepartures,l.BArrivals,l.BDepartures,l.AArrivals,l.Topology,l.Anchors,l.Forward,l.Reverse,l.BlockOwner,l.BlockUnits,l.BlockSections});
        public static string[] Changed(IEnumerable<LineTrackDefinition> before,IEnumerable<LineTrackDefinition> after) {
            var a=before.ToDictionary(l=>l.Id);var b=after.ToDictionary(l=>l.Id);
            return a.Keys.Union(b.Keys).Where(id=>!a.ContainsKey(id)||!b.ContainsKey(id)||Technical(a[id])!=Technical(b[id])).ToArray();
        }
        public static bool Touches(RouteDefinition route,LineTrackDefinition line) {
            var ids=new HashSet<string>(LineSignalGroups.All(line).Concat(line.BlockUnits.SelectMany(u=>new[]{u.Forward,u.Reverse})).Where(id=>id!=""));
            if(ids.Contains(route.Entry)||ids.Contains(route.Exit)||route.ViaSignals?.Any(s=>ids.Contains(s.Id))==true)return true;
            if(route.Path==null)return false;
            try{return route.Path.Any(p=>line.Axis.Any(a=>p.Section.Overlaps(a.Section)));}catch{return route.Path.Any(p=>line.Forward.Concat(line.Reverse).Any(a=>p.Section.Overlaps(a.Section)));}
        }
        public static string[] Blockers(IEnumerable<LineTrackDefinition> definitions,IEnumerable<string> affected,IEnumerable<string> targets,IEnumerable<RouteRun> runs,IEnumerable<DirectionBlockLease> leases,IEnumerable<LineDirectionState> states,Func<TrackSection,TrackOccupancy> read=null,Func<TrackSection,string> caption=null) {
            bool global=affected==null;var ids=new HashSet<string>(affected??Enumerable.Empty<string>());var targetIds=new HashSet<string>(targets??Enumerable.Empty<string>());
            var lines=definitions.Where(l=>global||ids.Contains(l.Id)).ToArray();var result=new List<string>();
            string Unclear(IEnumerable<DirectedRouteSection> path) {
                if(read==null)return "";
                foreach(var part in path){TrackOccupancy state;try{state=read(part.Section);}catch{state=TrackOccupancy.Unknown;}if(state!=TrackOccupancy.Clear)return " · "+(caption?.Invoke(part.Section)??part.Section.Key)+" · "+(state==TrackOccupancy.Unknown?LocalText.Select("unreadable", "nicht lesbar"):LocalText.Select("occupied", "belegt"));}
                return "";
            }
            foreach(var run in runs)if(global||targetIds.Contains(run.Route.Entry)||targetIds.Contains(run.Route.Exit)||run.Route.LineTarget!=null&&targetIds.Contains(run.Route.LineTarget)||lines.Any(l=>Touches(run.Route,l)))
                result.Add(LocalText.Select("Route ","Fahrstraße ")+run.Route.Code+" · "+RouteConflictException.State(run.Stage)+" · "+run.Reason);
            foreach(var lease in leases)if(global||lines.Any(l=>l.Forward.Concat(l.Reverse).Any(a=>lease.Axis.Any(b=>a.Section.Overlaps(b.Section))))) {
                var names=lines.Where(l=>l.Forward.Concat(l.Reverse).Any(a=>lease.Axis.Any(b=>a.Section.Overlaps(b.Section)))).Select(l=>l.Code).ToArray();
                result.Add(LocalText.Select("Block direction ","Blockrichtung ")+(names.Length>0?string.Join(" / ",names):lease.Id)+" · "+lease.ClearChecks+"/3 · "+lease.Reason+Unclear(lease.Axis));
            }
            foreach(var state in states.Where(s=>s.Held))if(global||ids.Contains(state.Definition.Id)||targetIds.Any(id=>state.Definition.Anchors.ContainsKey(id)))
                result.Add(state.Definition.Code+" · "+LocalText.Select("retained direction", "gehaltene Richtung")+" · "+state.ClearChecks+"/3 · "+(state.Error??(state.Routes.Count>0?LocalText.Select("route, train tail or clear proving still outstanding", "Fahrstraße, Zugschluss oder Freiprüfung noch offen"):LocalText.Select("complete clear proving still outstanding", "vollständige Freiprüfung noch offen")))+Unclear(state.Axis??new List<DirectedRouteSection>()));
            return result.Distinct().ToArray();
        }
    }
}
