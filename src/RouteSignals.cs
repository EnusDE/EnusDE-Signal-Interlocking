using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class DirectedRouteSection {public TrackSection Section;public bool Reverse;public DirectedRouteSection Clone(){var copy=(DirectedRouteSection)MemberwiseClone();copy.Section=Section?.Clone();return copy;}}
    public sealed class RouteViaSignal {
        public string Id,Anchor;
        public double Distance;
        public List<TrackSection> Downstream=new List<TrackSection>();
    }
    public static class RouteSignals {
        // Path consists only of actually traversed, directed segments, never flank guards.
        public static List<RouteViaSignal> Bind(IEnumerable<DirectedRouteSection> path,IEnumerable<MarkerRecord> signals,string entry,string exit) {
            var pieces=path.ToArray();var result=new List<RouteViaSignal>();
            foreach(var signal in signals.Where(s=>s.Id!=entry&&s.Id!=exit&&(s.Kind==MarkerRecord.Main||s.Kind==MarkerRecord.Shunt))) {
                var matches=pieces.Select((p,i)=>new{p,i}).Where(t=>t.p.Section.Segment==signal.Anchor.Segment&&t.p.Reverse==signal.FacingReverse&&(t.p.Reverse?signal.Anchor.Meters>t.p.Section.From+.001&&signal.Anchor.Meters<=t.p.Section.To+.001:signal.Anchor.Meters>=t.p.Section.From-.001&&signal.Anchor.Meters<t.p.Section.To-.001)).ToArray();
                if(matches.Length==0)continue;
                if(matches.Length!=1)throw new InvalidOperationException("Signal im Fahrweg mehrfach durchfahren: "+signal.Name);
                if(signal.Kind==MarkerRecord.Main)throw new InvalidOperationException("Weiteres Hauptsignal "+signal.Name+" im Fahrweg. Fahrstraße dort teilen.");
                var match=matches[0];var section=match.p.Section;
                var rest=new List<TrackSection>();double from=match.p.Reverse?section.From:signal.Anchor.Meters,to=match.p.Reverse?signal.Anchor.Meters:section.To;
                if(to-from>.001)rest.Add(new TrackSection{Segment=section.Segment,From=from,To=to});
                rest.AddRange(pieces.Skip(match.i+1).Select(p=>new TrackSection{Segment=p.Section.Segment,From=p.Section.From,To=p.Section.To}));
                if(rest.Count==0)throw new InvalidOperationException("Rangiersignal zu dicht am Fahrstraßenziel: "+signal.Name);
                double distance=pieces.Take(match.i).Sum(p=>p.Section.To-p.Section.From)+(match.p.Reverse?section.To-signal.Anchor.Meters:signal.Anchor.Meters-section.From);
                result.Add(new RouteViaSignal{Id=signal.Id,Anchor=Anchor(signal),Distance=distance,Downstream=rest});
            }
            return result.OrderBy(r=>r.Id,StringComparer.Ordinal).ToList();
        }
        public static string Anchor(MarkerRecord row)=>row.Anchor.Segment+":"+row.Anchor.Meters.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+row.FacingReverse;
        public static bool TargetContains(RouteDefinition route,TrackSection section)=>route.Shunting&&route.OccupiedTarget!=null&&route.OccupiedTarget.Any(t=>t.Segment==section.Segment&&t.From<=section.From&&t.To>=section.To);
        public static double TargetStart(RouteDefinition route) {
            double distance=0;foreach(var piece in route.Path){if(TargetContains(route,piece.Section))return distance;distance+=piece.Section.To-piece.Section.From;}return double.PositiveInfinity;
        }
        public static bool Position(RouteDefinition route,string segment,double meters,out double progress,bool includeContinuation=true) {
            progress=0;double distance=0;int matches=0;double found=0;
            foreach(var piece in route.Path) {
                if(piece.Section.Segment==segment&&meters>=piece.Section.From-.001&&meters<=piece.Section.To+.001){double value=distance+(piece.Reverse?piece.Section.To-meters:meters-piece.Section.From);if(matches>0&&Math.Abs(found-value)>.01)return false;found=value;matches++;}
                distance+=piece.Section.To-piece.Section.From;
            }
            if(matches>0){progress=found;return true;}
            distance=0;if(route.EntryApproach!=null)foreach(var piece in route.EntryApproach){if(piece.Section.Segment==segment&&meters>=piece.Section.From-.001&&meters<=piece.Section.To+.001){progress= -distance-(piece.Reverse?piece.Section.To-meters:meters-piece.Section.From);return true;}distance+=piece.Section.To-piece.Section.From;}
            if(!includeContinuation)return false;
            distance=route.Path.Sum(p=>p.Section.To-p.Section.From);matches=0;found=0;
            if(route.ExitContinuation!=null)foreach(var piece in route.ExitContinuation){if(piece.Section.Segment==segment&&meters>=piece.Section.From-.001&&meters<=piece.Section.To+.001){double value=distance+(piece.Reverse?piece.Section.To-meters:meters-piece.Section.From);if(matches>0&&Math.Abs(value-found)>.01)return false;found=value;matches++;}distance+=piece.Section.To-piece.Section.From;}
            if(matches>0){progress=found;return true;}return false;
        }
        public static void SplitProtection(RouteDefinition route) {
            // Check the original ownership before rebinding; never discard an uncovered guard.
            FlankProtection.Validate(route);
            var split=new List<TrackSection>();
            foreach(var group in route.Sections.GroupBy(s=>s.Segment)) {
                var cuts=group.SelectMany(s=>new[]{s.From,s.To}).Distinct().OrderBy(v=>v).ToArray();
                for(int i=1;i<cuts.Length;i++)if(cuts[i]-cuts[i-1]>.001&&group.Any(s=>s.From<=cuts[i-1]&&s.To>=cuts[i]))split.Add(new TrackSection{Segment=group.Key,From=cuts[i-1],To=cuts[i]});
            }
            var path=new List<DirectedRouteSection>();
            foreach(var piece in route.Path) {
                var parts=split.Where(s=>s.Segment==piece.Section.Segment&&s.From>=piece.Section.From&&s.To<=piece.Section.To);parts=piece.Reverse?parts.OrderByDescending(s=>s.To):parts.OrderBy(s=>s.From);
                path.AddRange(parts.Select(s=>new DirectedRouteSection{Section=s,Reverse=piece.Reverse}));
            }
            route.Sections=split;route.Path=path;
            if(route.Flanks!=null)foreach(var flank in route.Flanks){var original=flank.Sections;flank.Sections=split.Where(s=>original.Any(g=>g.Segment==s.Segment&&g.From<=s.From&&g.To>=s.To)).ToList();}
            // The occupied target starts after every protected point area, never on a point.
            double distance=0,lastGuard=0;
            foreach(var piece in path){distance+=piece.Section.To-piece.Section.From;if(route.Switches.Any(s=>s.Protection!=null&&s.Protection.Any(p=>p.Overlaps(piece.Section))))lastGuard=distance;}
            route.OccupiedTarget=new List<TrackSection>();distance=0;
            if(route.Shunting)foreach(var piece in path){if(distance>=lastGuard-.001)route.OccupiedTarget.Add(piece.Section);distance+=piece.Section.To-piece.Section.From;}
        }
        public static void Validate(RouteDefinition route) {
            if(route.Path==null){if(route.ViaSignals!=null&&route.ViaSignals.Count>0)throw new InvalidDataException("Rangiersignal ohne gerichteten Fahrweg.");return;}
            if(route.Path.Count==0||route.Path.Count>128||route.Path.Any(p=>p==null||p.Section==null)||route.ViaSignals==null||route.ViaSignals.Count>128)throw new InvalidDataException("Ungültiger gerichteter Fahrweg.");
            foreach(var piece in route.Path){piece.Section.Validate();if(!route.Sections.Any(s=>s.Segment==piece.Section.Segment&&s.From<=piece.Section.From&&s.To>=piece.Section.To))throw new InvalidDataException("Fahrwegabschnitt ohne Belegungsschutz.");}
            foreach(var point in route.Switches)if(!point.ReleaseAt.HasValue||double.IsNaN(point.ReleaseAt.Value)||point.ReleaseAt.Value<0||point.Protection==null||point.Protection.Count==0||point.Protection.Count>64)throw new InvalidDataException("Weichenfreimeldebereich fehlt.");
            if(route.OccupiedTarget!=null)foreach(var target in route.OccupiedTarget){target.Validate();if(!route.Shunting||!route.Path.Any(p=>p.Section.Key==target.Key)||route.Switches.Any(s=>s.Protection.Any(p=>p.Overlaps(target))))throw new InvalidDataException("Besetztes Ziel darf keinen Weichenschutzbereich enthalten.");}
            if(route.ViaSignals.Select(s=>s?.Id).Distinct().Count()!=route.ViaSignals.Count)throw new InvalidDataException("Doppeltes Rangiersignal.");
            foreach(var signal in route.ViaSignals){if(signal==null||string.IsNullOrWhiteSpace(signal.Id)||string.IsNullOrWhiteSpace(signal.Anchor)||signal.Id==route.Entry||signal.Id==route.Exit||signal.Downstream==null||signal.Downstream.Count==0||signal.Downstream.Count>128)throw new InvalidDataException("Ungültiges Rangiersignal im Fahrweg.");foreach(var section in signal.Downstream){section.Validate();if(!route.Path.Any(p=>p.Section.Segment==section.Segment&&p.Section.From<=section.From&&p.Section.To>=section.To))throw new InvalidDataException("Rangiersignal zeigt außerhalb des Fahrwegs.");}}
        }
        public static bool Same(IEnumerable<RouteViaSignal> a,IEnumerable<RouteViaSignal> b) {
            var x=a.OrderBy(s=>s.Id,StringComparer.Ordinal).ToArray();var y=b.OrderBy(s=>s.Id,StringComparer.Ordinal).ToArray();
            return x.Length==y.Length&&x.Zip(y,(p,q)=>p.Id==q.Id&&p.Anchor==q.Anchor&&p.Distance==q.Distance&&p.Downstream.Select(s=>s.Key).SequenceEqual(q.Downstream.Select(s=>s.Key))).All(v=>v);
        }
    }
}
