using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class GfmRepairPlan {
        public DeskLibrary Library;
        public TrackBlock Replacement;
        public string[] Removed;
        public int Fields,Boundaries;
        public string[] Desks;
        public List<TrackSection> Affected;
    }
    public static class GfmSectionRepair {
        static bool Covers(TrackBlock outer,TrackBlock inner)=>GfmCoverage.Gaps(inner.Sections,outer.Sections).Count==0;
        static bool SameEnds(TrackBlock a,TrackBlock b)=>TrackCircuit.Pair(a.BoundaryA,a.BoundaryB)==TrackCircuit.Pair(b.BoundaryA,b.BoundaryB);
        static string[] Segments(TrackBlock b)=>b.Path.Where((p,i)=>i==0||p.Section.Segment!=b.Path[i-1].Section.Segment).Select(p=>p.Section.Segment).ToArray();
        static bool SameCorridor(TrackBlock a,TrackBlock b){var x=Segments(a);var y=Segments(b);return SameEnds(a,b)&&(x.SequenceEqual(y)||x.SequenceEqual(y.Reverse()));}
        public static TrackBlock[] Candidates(IEnumerable<TrackBlock> catalogue,TrackBlock old,Func<TrackBlock,bool> valid)=>catalogue.Where(b=>b.IsCircuit&&b.Id!=old.Id&&valid(b)&&(Covers(b,old)||SameCorridor(b,old)))
            .GroupBy(b=>TrackCircuit.Pair(b.BoundaryA,b.BoundaryB)+"|"+string.Join(";",b.Sections.Select(s=>s.Key).OrderBy(s=>s,StringComparer.Ordinal))).Select(g=>g.First())
            .OrderBy(b=>b.Sections.Sum(s=>s.To-s.From)).ThenBy(b=>b.Id,StringComparer.Ordinal).ToArray();
        public static GfmRepairPlan Build(DeskLibrary library,IEnumerable<string> ids,TrackBlock replacement,Func<TrackBlock,bool> valid,Func<string,GfmBoundaryFault> fault) {
            var remove=new HashSet<string>(ids,StringComparer.Ordinal);
            if(remove.Count==0||remove.Any(id=>!library.Blocks.TryGetValue(id,out var b)||!b.IsCircuit||valid(b)))throw new InvalidOperationException(LocalText.Select("Choose saved invalid GFM sections first.","Zuerst gespeicherte ungültige GFM-Abschnitte auswählen."));
            if(remove.Any(id=>new[]{library.Blocks[id].BoundaryA,library.Blocks[id].BoundaryB}.Any(end=>fault(end)==GfmBoundaryFault.Planned)))throw new InvalidOperationException(LocalText.Select("This section uses an unfinished construction plan. Complete construction instead of deleting its assignment.","Dieser Abschnitt verwendet einen unfertigen Bauplan. Bau fertigstellen statt dessen Zuordnung zu löschen."));
            if(replacement!=null) {
                replacement.Validate();
                if(!replacement.IsCircuit||!valid(replacement)||remove.Contains(replacement.Id)||remove.Any(id=>!Covers(replacement,library.Blocks[id])&&!SameCorridor(replacement,library.Blocks[id])))throw new InvalidOperationException(LocalText.Select("The replacement must cover the old path or be the current valid path between the same boundaries on the same track corridor.","Der Ersatz muss den alten Verlauf abdecken oder der aktuelle gültige Weg zwischen denselben Grenzen auf demselben Gleisverlauf sein."));
                foreach(var b in library.Blocks.Values.Where(b=>b.IsCircuit&&!valid(b)&&SameCorridor(replacement,b)&&new[]{b.BoundaryA,b.BoundaryB}.All(end=>fault(end)!=GfmBoundaryFault.Planned)))remove.Add(b.Id);
                foreach(var b in library.Blocks.Values.Where(b=>b.IsCircuit&&!valid(b)&&Covers(replacement,b)&&new[]{b.BoundaryA,b.BoundaryB}.Any(end=>fault(end)==GfmBoundaryFault.Missing)&&new[]{b.BoundaryA,b.BoundaryB}.All(end=>fault(end)!=GfmBoundaryFault.Planned)))remove.Add(b.Id);
            }
            var managed=library.FreeLines.Where(l=>l.BlockSections.Any(remove.Contains)).Select(l=>l.Code).ToArray();
            if(managed.Length>0)throw new InvalidOperationException(LocalText.Select("These sections belong to a constructed block assembly: ","Diese Abschnitte gehören zu einem gebauten Blockverbund: ")+string.Join(", ",managed)+LocalText.Select(". Restore its shared detector first; removing it here would break the assembly.",". Zuerst dessen gemeinsamen Melder instand setzen; Entfernen an dieser Stelle würde den Verbund beschädigen."));
            var next=library.Clone();var dead=new HashSet<string>(remove.SelectMany(id=>new[]{library.Blocks[id].BoundaryA,library.Blocks[id].BoundaryB}).Where(id=>fault(id)==GfmBoundaryFault.Missing));
            var result=new GfmRepairPlan{Library=next,Removed=remove.OrderBy(id=>id,StringComparer.Ordinal).ToArray(),Replacement=replacement?.Clone(),Affected=remove.SelectMany(id=>library.Blocks[id].Sections).Concat(replacement?.Sections??Enumerable.Empty<TrackSection>()).GroupBy(s=>s.Key).Select(g=>g.First().Clone()).ToList()};
            var changed=new HashSet<string>();
            foreach(var desk in next.Desks)foreach(var tile in desk.Panel.Tiles) {
                bool touched=false;
                if(remove.Contains(tile.BlockId)){tile.BlockId=replacement?.Id??"";touched=true;}
                if(remove.Contains(tile.SecondBlock)){tile.SecondBlock=replacement?.Id??"";touched=true;}
                if(tile.CircuitRails!=null)foreach(int port in tile.CircuitRails.Where(p=>remove.Contains(p.Value)).Select(p=>p.Key).ToArray()){if(replacement==null)tile.CircuitRails.Remove(port);else tile.CircuitRails[port]=replacement.Id;touched=true;}
                if(tile.CircuitRails?.Count==0)tile.CircuitRails=null;
                if(tile.CircuitBoundary!=null&&dead.Contains(tile.CircuitBoundary)){tile.CircuitBoundary=null;result.Boundaries++;touched=true;}
                if(touched){result.Fields++;changed.Add(desk.Code);}
            }
            foreach(string id in remove)next.Blocks.Remove(id);
            if(replacement!=null)next.Blocks[replacement.Id]=replacement.Clone();
            result.Desks=changed.OrderBy(s=>s,StringComparer.Ordinal).ToArray();next.Validate(library.WorldKey);return result;
        }
        public static string[] Blockers(GfmRepairPlan plan,IEnumerable<RouteRun> runs,IEnumerable<DirectionBlockLease> leases,IEnumerable<LineDirectionState> states) {
            bool Touches(IEnumerable<TrackSection> sections)=>sections.Any(s=>plan.Affected.Any(s.Overlaps));var reasons=new List<string>();
            foreach(var r in runs.Where(r=>Touches(r.HeldSections.Concat(r.Route.Approach).Concat(r.Route.Path?.Select(p=>p.Section)??Enumerable.Empty<TrackSection>()))))reasons.Add(LocalText.Select("Route ","Fahrstraße ")+r.Route.Code+" · "+RouteConflictException.State(r.Stage));
            foreach(var l in leases.Where(l=>Touches(l.Axis.Select(p=>p.Section))))reasons.Add(LocalText.Select("Block direction ","Blockrichtung ")+l.Id+" · "+l.ClearChecks+"/3 · "+l.Reason);
            foreach(var s in states.Where(s=>s.Held&&Touches(s.Axis.Select(p=>p.Section))))reasons.Add(s.Definition.Code+" · "+LocalText.Select("retained direction", "gehaltene Richtung"));
            return reasons.ToArray();
        }
    }
}
