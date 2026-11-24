using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class WaypointRoutePolicy {
        const double PositionTolerance=.02;
        const double TerminalExtension=300;
        const double MaximumLeg=10000;

        public static bool ClearApproach(WaypointApproach approach,MarkerRecord start,IVehicleOccupancyWorld world,string[] expected) {
            if(world==null||expected==null||expected.Length==0||!approach.TryLocateAhead(start,out double remaining,6000))return false;
            foreach(var piece in approach.Sections) {
                if(remaining<=PositionTolerance)return true;
                var span=piece.Section;
                double length=Math.Min(remaining,span.To-span.From);
                var section=new TrackSection{Segment=span.Segment,From=piece.Reverse?span.To-length:span.From,To=piece.Reverse?span.To:span.From+length};
                var occupancy=world.SectionWithCars(section,out var cars);
                if(occupancy!=TrackOccupancy.Clear&&(occupancy!=TrackOccupancy.Occupied||cars==null||cars.Length==0||cars.Any(id=>!expected.Contains(id))))return false;
                remaining-=length;
            }
            return remaining<=PositionTolerance;
        }

        public static bool HasViaAuthority(RouteRun run,string signal,string[] cars,LocalSignalAspect aspect) {
            return aspect==LocalSignalAspect.Shunt&&run.Entered&&run.PermitsVia(signal)&&
                run.TrainCars!=null&&cars.Length>0&&run.TrainCars.Length==cars.Length&&
                new HashSet<string>(run.TrainCars,StringComparer.Ordinal).SetEquals(cars);
        }

        // A stop inside a segment can reserve to its following signal or auxiliary endpoint.
        // Beyond the way point, only the final segment has a proven travel direction.
        static bool Project(WaypointApproach approach,string segment,double meters,out double distance,out bool reverse) {
            if(approach.TryLocate(segment,meters,out distance,out reverse))return true;
            var terminal=approach.Sections[approach.Sections.Count-1];
            if(terminal.Section.Segment!=segment)return false;
            double extra=terminal.Reverse?terminal.Section.From-meters:meters-terminal.Section.To;
            if(!(extra>0&&extra<=TerminalExtension))return false;
            distance=approach.Distance+extra;
            reverse=terminal.Reverse;
            return true;
        }

        public static MarkerRecord Destination(WaypointApproach approach,MarkerRecord start,IEnumerable<MarkerRecord> signals,IEnumerable<MarkerRecord> auxiliaries,bool shunting,Func<MarkerRecord,bool> physicalInService=null) {
            if(approach==null||start?.Anchor==null||!approach.TryLocate(start.Anchor.Segment,start.Anchor.Meters,out double from,out bool reverse)||reverse!=start.FacingReverse||from>=approach.Distance-PositionTolerance)return null;
            var candidates=new List<(MarkerRecord Signal,double Distance,bool Auxiliary)>();
            foreach(var endpoint in signals.Concat(auxiliaries)) {
                if(endpoint?.Anchor==null||endpoint.Planned||endpoint.Id==start.Id)continue;
                bool auxiliary=AuxiliaryTarget.IsEndpoint(endpoint);
                if(auxiliary?!AuxiliaryTarget.Accepts(endpoint,shunting):endpoint.Kind!=MarkerRecord.Main&&!(shunting&&endpoint.Kind==MarkerRecord.Shunt))continue;
                if(!Project(approach,endpoint.Anchor.Segment,endpoint.Anchor.Meters,out double at,out bool direction)||direction!=endpoint.FacingReverse||at<=from+PositionTolerance||at-from>MaximumLeg)continue;
                // A local auxiliary before the native stop would leave the rest of the leg unprotected.
                if(auxiliary&&at<approach.Distance-PositionTolerance)continue;
                if(!auxiliary&&physicalInService!=null&&!physicalInService(endpoint))continue;
                candidates.Add((endpoint,at,auxiliary));
            }
            if(candidates.Count==0)return null;
            double closest=candidates.Min(endpoint=>endpoint.Distance);
            var tied=candidates.Where(endpoint=>endpoint.Distance-closest<=PositionTolerance).ToArray();
            var physical=tied.Where(endpoint=>!endpoint.Auxiliary).ToArray();
            var nearest=physical.Length>0?physical:tied;
            if(nearest.Length!=1)throw new AmbiguousRoutePlanException(LocalText.Select("Several possible route destinations at the same position.","Mehrere mögliche Fahrstraßenziele an derselben Stelle."));
            return nearest[0].Signal;
        }

        public static bool Matches(WaypointApproach approach,MarkerRecord start,MarkerRecord target,RouteKind kind,RouteChainPlan plan) {
            if(approach==null||start?.Anchor==null||target?.Anchor==null||plan?.Routes?.Count!=1)return false;
            var route=plan.Routes[0];
            if(route==null||route.Entry!=start.Id||route.Exit!=target.Id||route.Shunting!=(kind==RouteKind.Shunt)||route.Reduced!=(kind==RouteKind.Slow)||route.LineTarget!=null||route.Path?.Count is not >0)return false;
            if(!approach.TryLocate(start.Anchor.Segment,start.Anchor.Meters,out double progress,out bool startReverse)||startReverse!=start.FacingReverse||progress>=approach.Distance-PositionTolerance)return false;
            if(!Project(approach,target.Anchor.Segment,target.Anchor.Meters,out double end,out bool endReverse)||endReverse!=target.FacingReverse)return false;
            foreach(var piece in route.Path) {
                var span=piece?.Section;
                if(span==null||!(span.To>span.From))return false;
                double from=piece.Reverse?span.To:span.From;
                double to=piece.Reverse?span.From:span.To;
                if(!Project(approach,span.Segment,from,out double begin,out bool beginReverse)||
                    !Project(approach,span.Segment,to,out double finish,out bool finishReverse)||
                    piece.Reverse!=beginReverse||piece.Reverse!=finishReverse||Math.Abs(begin-progress)>PositionTolerance||
                    Math.Abs((finish-begin)-(span.To-span.From))>PositionTolerance||finish<=begin)return false;
                progress=finish;
            }
            return Math.Abs(progress-end)<=PositionTolerance;
        }
    }
}
