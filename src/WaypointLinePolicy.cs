using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class WaypointLinePolicy {
        public static List<DirectedRouteSection> RequiredPath(IReadOnlyList<LineTrackDefinition> lines,string from,string to,MarkerRecord source,MarkerRecord target,bool shunting) {
            bool usesBlock=lines.SelectMany(line=>line.BlockUnits).Any(unit=>unit.Forward==source.Id||unit.Reverse==source.Id||unit.Forward==target.Id||unit.Reverse==target.Id);
            if(from==to&&!usesBlock)return null;

            var matches=new List<List<DirectedRouteSection>>();
            if(!shunting)foreach(var line in lines.Where(line=>line.Enabled)) {
                Add(line,false);
                if(line.Mode!=LineTrackMode.Directional)Add(line,true);
            }
            if(matches.Count==1)return matches[0];
            if(matches.Count>1)throw new AmbiguousRoutePlanException(LocalText.Select("Several lines match these signals and direction.","Mehrere Strecken passen zu diesen Signalen und dieser Fahrtrichtung."));
            throw new InvalidOperationException(usesBlock?
                LocalText.Select("No active block line links these signals as consecutive stops in this direction.","Keine aktive Blockstrecke verbindet diese Signale als aufeinanderfolgende Ziele in dieser Richtung."):
                LocalText.Select("This waypoint leg needs a configured line between the two interlockings.","Dieser Wegpunktabschnitt benötigt eine eingerichtete Strecke zwischen den beiden Stellwerken."));

            void Add(LineTrackDefinition line,bool reverse) {
                string departureDesk=reverse?line.StationB:line.StationA;
                string arrivalDesk=reverse?line.StationA:line.StationB;
                var departures=LineSignalGroups.Get(line,reverse?LineSignalGroup.BDepartures:LineSignalGroup.ADepartures);
                var arrivals=LineSignalGroups.Get(line,reverse?LineSignalGroup.AArrivals:LineSignalGroup.BArrivals);
                bool departure=from==departureDesk&&departures.Contains(source.Id);
                bool arrival=to==arrivalDesk&&arrivals.Contains(target.Id);
                if(!usesBlock) {
                    var path=reverse?line.Reverse:line.Forward;
                    if(departure&&arrival&&line.BlockUnits.Count==0&&path.Count>0)matches.Add(path);
                    return;
                }

                var order=reverse?line.BlockUnits.OrderByDescending(unit=>unit.Distance):line.BlockUnits.OrderBy(unit=>unit.Distance);
                var blocks=order.Select(unit=>reverse?unit.Reverse:unit.Forward).ToList();
                // Every leg must end at the next configured signal, even when both heads share one desk.
                if(blocks.Count==0)return;
                int start=from==line.BlockOwner?blocks.IndexOf(source.Id):-1;
                if((start>=0)==departure)return;
                int next=start+1;
                if(next<blocks.Count?to!=line.BlockOwner||blocks[next]!=target.Id:!arrival)return;

                var axis=line.Axis;
                if(reverse)axis=axis.AsEnumerable().Reverse().Select(piece=>new DirectedRouteSection{Section=piece.Section,Reverse=!piece.Reverse}).ToList();
                var geometry=new BlockLayoutPlan{Axis=axis};
                double? first=start<0?0:source.Anchor==null?null:geometry.Position(source.Anchor);
                double? last=next==blocks.Count?geometry.Length:target.Anchor==null?null:geometry.Position(target.Anchor);
                if(first.HasValue&&last.HasValue&&last.Value-first.Value>.005)matches.Add(geometry.Slice(first.Value,last.Value));
            }
        }

        public static bool Covers(IReadOnlyList<DirectedRouteSection> expected,IReadOnlyList<DirectedRouteSection> actual)=>
            expected.GroupBy(piece=>piece.Reverse).All(direction=>GfmCoverage.Gaps(direction.Select(piece=>piece.Section),
                actual.Where(piece=>piece.Reverse==direction.Key).Select(piece=>piece.Section)).Count==0);
    }
}
