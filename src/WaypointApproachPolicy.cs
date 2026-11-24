using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    /// <summary>A unique directed rail path from the current leading end to the native waypoint.</summary>
    public sealed class WaypointApproach {
        const double PositionTolerance=.001;
        const double EndClearance=.1;
        const double CoincidentSignals=.05;
        readonly Dictionary<string,(DirectedRouteSection piece,double offset)> sectionsBySegment;

        public IReadOnlyList<DirectedRouteSection> Sections {get;}
        public double Distance {get;}
        public int Phase {get;internal set;}
        public bool EndsAtTurnback {get;internal set;}

        WaypointApproach(List<DirectedRouteSection> sections) {
            Sections=sections.AsReadOnly();
            sectionsBySegment=new Dictionary<string,(DirectedRouteSection,double)>(StringComparer.Ordinal);
            double offset=0;
            foreach(var piece in sections) {
                sectionsBySegment.Add(piece.Section.Segment,(piece,offset));
                offset+=piece.Section.To-piece.Section.From;
            }
            Distance=offset;
        }

        public static WaypointApproach Create(IReadOnlyList<DirectedRouteSection> plan,MarkerAnchor head,bool headReverse,MarkerAnchor waypoint) {
            if(plan==null||plan.Count==0||head==null||waypoint==null)return null;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            var sections=new List<DirectedRouteSection>();
            bool foundHead=false;
            foreach(var piece in plan) {
                var span=piece?.Section;
                if(span==null||string.IsNullOrEmpty(span.Segment)||!Finite(span.From)||!Finite(span.To)||span.From<0||span.To<=span.From||!seen.Add(span.Segment))return null;
                if(!foundHead) {
                    if(span.Segment!=head.Segment)continue;
                    if(piece.Reverse!=headReverse||!Contains(span,head.Meters))return null;
                    foundHead=true;
                }
                double from=span.From,to=span.To;
                if(span.Segment==head.Segment) {
                    var position=Math.Max(from,Math.Min(to,head.Meters));
                    if(piece.Reverse)to=position;
                    else from=position;
                }
                if(span.Segment==waypoint.Segment) {
                    // The native route itself must end at this waypoint. A truncated
                    // search, or an earlier occurrence of the target segment, is ambiguous.
                    double end=piece.Reverse?span.From:span.To;
                    if(!Finite(waypoint.Meters)||Math.Abs(end-waypoint.Meters)>PositionTolerance||!ReferenceEquals(plan[plan.Count-1],piece))return null;
                }
                if(to-from>PositionTolerance)sections.Add(new DirectedRouteSection{Reverse=piece.Reverse,Section=new TrackSection{Segment=span.Segment,From=from,To=to}});
            }
            return foundHead&&sections.Count>0&&sections[sections.Count-1].Section.Segment==waypoint.Segment?new WaypointApproach(sections):null;
        }

        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        static bool Contains(TrackSection span,double meters)=>Finite(meters)&&meters>=span.From-PositionTolerance&&meters<=span.To+PositionTolerance;

        public bool TryLocate(string segment,double meters,out double distance,out bool reverse) {
            distance=0;
            reverse=false;
            if(segment==null||!sectionsBySegment.TryGetValue(segment,out var entry)||!Contains(entry.piece.Section,meters))return false;
            var span=entry.piece.Section;
            reverse=entry.piece.Reverse;
            distance=entry.offset+(reverse?span.To-meters:meters-span.From);
            return true;
        }

        public bool TryLocateAhead(MarkerRecord signal,out double distance,double limit=300) {
            return LocateAhead(signal,out distance,limit,false);
        }

        bool LocateAhead(MarkerRecord signal,out double distance,double limit,bool includeOpposite) {
            distance=0;
            if(signal?.Anchor==null||!TryLocate(signal.Anchor.Segment,signal.Anchor.Meters,out double progress,out bool reverse))return false;
            if((!includeOpposite&&reverse!=signal.FacingReverse)||progress<0||progress>limit||progress>=Distance-EndClearance)return false;
            distance=progress;
            return true;
        }

        public MarkerRecord[] SignalsAhead(IEnumerable<MarkerRecord> signals,double limit=300,bool includeOpposite=false)=>signals
            .Select(signal=>(Signal:signal,Distance:LocateAhead(signal,out double progress,limit,includeOpposite)?progress:double.PositiveInfinity))
            .Where(s=>!double.IsPositiveInfinity(s.Distance)).OrderBy(s=>s.Distance).Select(s=>s.Signal).ToArray();

        public MarkerRecord NextSignal(IEnumerable<MarkerRecord> governingSignals,double limit=300) {
            var matches=new List<(MarkerRecord signal,double distance)>();
            foreach(var signal in governingSignals)if(TryLocateAhead(signal,out double distance,limit))matches.Add((signal,distance));
            var nearest=matches.OrderBy(match=>match.distance).Take(2).ToArray();
            if(nearest.Length==0)return null;
            if(nearest.Length>1&&nearest[1].distance-nearest[0].distance<CoincidentSignals)
                throw new InvalidOperationException(LocalText.Select("Two governing signals share this position.","Zwei zuständige Signale liegen an dieser Stelle."));
            return nearest[0].signal;
        }
    }
}
