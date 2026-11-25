using System;
using System.Collections.Generic;
using System.Linq;
namespace EnusDE.Signaltechnik {
    public enum PanelRouteKind { None,Train,Shunt }
    public enum PanelRailInk { Free,Unknown,Occupied,Train,Shunt }
    public static class PanelRouteStyle {
        public static PanelRouteKind Kind(RouteRun run)=>run!=null&&run.ChainReady&&(run.Stage==RouteStage.Set||run.Stage==RouteStage.Running)?run.Route.Shunting?PanelRouteKind.Shunt:PanelRouteKind.Train:PanelRouteKind.None;
        public static PanelRouteKind Merge(PanelRouteKind a,PanelRouteKind b)=>a==PanelRouteKind.Train||b==PanelRouteKind.Train?PanelRouteKind.Train:a==PanelRouteKind.Shunt||b==PanelRouteKind.Shunt?PanelRouteKind.Shunt:PanelRouteKind.None;
        public static PanelRailInk Resolve(TrackOccupancy occupancy,PanelRouteKind kind)=>occupancy==TrackOccupancy.Occupied?PanelRailInk.Occupied:occupancy!=TrackOccupancy.Clear?PanelRailInk.Unknown:kind==PanelRouteKind.Train?PanelRailInk.Train:kind==PanelRouteKind.Shunt?PanelRailInk.Shunt:PanelRailInk.Free;
        public static PanelRouteKind OnSection(TrackSection section,IEnumerable<RouteRun> runs) {
            if(section==null)return PanelRouteKind.None;var kind=PanelRouteKind.None;
            foreach(var run in runs)if(Kind(run)!=PanelRouteKind.None&&run.HeldSections.Any(s=>s.Overlaps(section)&&(run.Route.Path==null||run.Route.Path.Any(p=>p.Section.Overlaps(section)&&p.Section.Overlaps(s)))))kind=Merge(kind,Kind(run));
            return kind;
        }
    }
}
