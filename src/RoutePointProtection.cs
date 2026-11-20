using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace EnusDE.Signaltechnik {
    public static class RoutePointProtection {
        public static List<TrackSection> Read(string point,Func<string,IEnumerable<TrackSection>> guards) {
            var result=guards(point)?.Select(s=>s.Clone()).ToList();
            if(result==null||result.Count!=3||result.Select(s=>s.Segment).Distinct().Count()!=3)throw new InvalidOperationException(LocalText.Select("Point protection is incomplete: ","Weichenschutz unvollständig: ")+point);
            foreach(var section in result)section.Validate();return result;
        }
        public static void Bind(RouteDefinition route,Func<string,IEnumerable<TrackSection>> guards) {
            foreach(var point in route.Switches) {
                point.Protection=Read(point.Node,guards);
                route.Sections.AddRange(point.Protection.Select(s=>s.Clone()));
            }
        }
        public static string Signature(RouteDefinition route,Func<string,IEnumerable<TrackSection>> guards)=>SharedDocuments.Hash(Encoding.UTF8.GetBytes(string.Join("\n",route.Switches.OrderBy(p=>p.Node,StringComparer.Ordinal)
            .Select(p=>p.Node+"|"+string.Join("|",Read(p.Node,guards).Select(s=>s.Key).OrderBy(k=>k,StringComparer.Ordinal))))));
        public static bool Current(RouteDefinition route,Func<string,IEnumerable<TrackSection>> guards)=>route.Switches.Count==0||
            !string.IsNullOrEmpty(route.PointGuardSignature)&&route.PointGuardSignature==Signature(route,guards);
        public static void RequireCalculated(RouteDefinition route) {
            if(route.Switches.Count>0&&string.IsNullOrEmpty(route.PointGuardSignature))throw new InvalidOperationException(LocalText.Select("Recalculate the saved route with current physical point boundaries: ","Gespeicherten Fahrweg mit aktuellen physischen Weichengrenzen neu berechnen: ")+route.Code);
        }
    }
    public sealed partial class SignalRuntime {
        internal IEnumerable<TrackSection> RoutePointGuards(string point)=>ReadPointDefinition(new TrackBlock{Point=point,Id="p:"+point,Name=PointAssignments.Caption(desks,point)}).Guards;
    }
}
