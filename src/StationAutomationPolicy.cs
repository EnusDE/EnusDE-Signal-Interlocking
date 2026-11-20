using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class StationAutomationPolicy {
        public static bool CanControl(DeskLibrary library,string desk,IEnumerable<RouteDefinition> routes)=>library.Desks.Any(d=>d.Id==desk&&d.IsMaster)||routes.All(r=>Owner(library,r.Entry)?.Id==desk)||LineTargetBindings.CanControlBlocks(library,desk,routes.ToArray());
        public static void RequireControl(DeskLibrary library,string desk,IEnumerable<RouteDefinition> routes) {
            if(!CanControl(library,desk,routes))throw new InvalidOperationException(LocalText.Select("The route would operate a neighbour's signal. Request it at the owning interlocking or the master; a local route may end at the neighbour's signal.","Der Fahrweg würde ein Nachbarsignal bedienen. Am zuständigen Stellwerk oder im ESTW anfordern; ein örtlicher Fahrweg darf am Nachbarsignal enden."));
        }
        public static DeskDefinition Owner(DeskLibrary library,string signal) {
            var matches=library.Desks.Where(d=>!d.IsMaster&&d.Panel.Tiles.Any(t=>!t.ReadOnlySignal&&(t.Kind==PanelTileKind.Signal&&(t.Reference==signal||t.AlternateSignal==signal)||t.Target?.Id==signal))).ToArray();
            if(matches.Length>1)throw new InvalidOperationException("Signal / Hilfsziel mehreren Ortsstellwerken zugeordnet. Zuordnung klären.");
            return matches.FirstOrDefault();
        }
        public static string Admission(DeskLibrary library,IReadOnlyList<RouteDefinition> plan) {
            if(!library.Desks.Any(d=>!d.IsMaster&&d.Acceptance!=StationAcceptance.Through))return null;
            foreach(var route in plan) {
                var station=Owner(library,route.Exit);if(station==null||station.Id==Owner(library,route.Entry)?.Id)continue;
                if(station.Acceptance==StationAcceptance.Closed)return station.Code+": nimmt derzeit keine Fahrten an.";
                if(station.Acceptance==StationAcceptance.DestinationOnly&&Owner(library,plan.Last().Exit)?.Id!=station.Id)return station.Code+": nur Zielankunft, keine Durchfahrt freigegeben.";
            }
            return null;
        }
        public static bool LocalShunt(DeskLibrary library,RouteDefinition route,IEnumerable<MarkerAnchor> boundaries) {
            return route.Shunting&&LocalMovement(library,route,boundaries);
        }
        public static bool LocalMovement(DeskLibrary library,RouteDefinition route,IEnumerable<MarkerAnchor> boundaries) {
            if(route.Path==null)return false;
            var owner=Owner(library,route.Entry);if(owner==null||Owner(library,route.Exit)?.Id!=owner.Id)return false;
            double length=route.Path.Sum(p=>p.Section.To-p.Section.From),before=0;
            foreach(var piece in route.Path) {
                foreach(var boundary in boundaries.Where(a=>a.Segment==piece.Section.Segment)) {
                    double progress=before+(piece.Reverse?piece.Section.To-boundary.Meters:boundary.Meters-piece.Section.From);
                    if(boundary.Meters>=piece.Section.From-.01&&boundary.Meters<=piece.Section.To+.01&&progress>.05&&progress<length-.05)return false;
                }
                before+=piece.Section.To-piece.Section.From;
            }
            return true;
        }
    }
}
