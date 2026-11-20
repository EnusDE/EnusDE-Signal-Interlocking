using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    // Validate the saved physical path, not its serialization or a new map-wide search.
    public static class CircuitPathValidation {
        const double tolerance=.005;
        static bool Near(double a,double b)=>Math.Abs(a-b)<tolerance;
        public static string Error(NetworkInput network,TrackBlock block,MarkerRecord from,MarkerRecord to,Func<string,string> pointCaption=null) {
            if(!TrackCircuit.EndsMatch(block,from,to))return LocalText.Select("GFM boundary missing, disabled or moved.","GFM-Grenze fehlt, ist deaktiviert oder wurde versetzt.");
            if(!CircuitIdentity.Enabled(from)||!CircuitIdentity.Enabled(to))return LocalText.Select("Stored section ends at a signal without an active detector. Enable its GFM or select an actual detection boundary.","Gespeicherter Abschnitt endet an einem Signal ohne aktiven Melder. Dessen GFM aktivieren oder eine tatsächliche Meldegrenze wählen.");
            var edges=network.Edges.ToDictionary(e=>e.Id,StringComparer.Ordinal);
            var nodes=network.Nodes.ToDictionary(n=>n.Id,StringComparer.Ordinal);
            var joins=network.Edges.SelectMany(e=>new[]{e.A,e.B}).ToLookup(n=>n,StringComparer.Ordinal);
            var visited=new HashSet<string>(StringComparer.Ordinal);
            PlanEdge previous=null;double previousEnd=0;bool previousReverse=false;
            foreach(var piece in block.Path) {
                var section=piece.Section;
                if(!edges.TryGetValue(section.Segment,out var edge)||double.IsNaN(section.From)||double.IsNaN(section.To)||
                    section.From<0||section.To>edge.Length+tolerance||section.To<=section.From)
                    return LocalText.Select("GFM track is unavailable or its saved interval is invalid: ","GFM-Gleis nicht verfügbar oder gespeicherter Meterbereich ungültig: ")+section.Segment;
                double start=piece.Reverse?section.To:section.From,end=piece.Reverse?section.From:section.To;
                if(previous!=null) {
                    if(previous.Id==edge.Id) {
                        if(previousReverse!=piece.Reverse||!Near(previousEnd,start))return Interrupted(edge.Id);
                    } else {
                        string exit=previousReverse?previous.A:previous.B,entry=piece.Reverse?edge.B:edge.A;
                        if(!Near(previousEnd,previousReverse?0:previous.Length)||!Near(start,piece.Reverse?edge.Length:0)||exit!=entry)
                            return Interrupted(previous.Id+" / "+edge.Id);
                        if(!nodes.TryGetValue(exit,out var node))return Interrupted(exit);
                        if(node.Point)return LocalText.Select("GFM section crosses point ","GFM-Abschnitt führt über Weiche ")+(pointCaption?.Invoke(exit)??exit)+
                            LocalText.Select(". Enable its detection boundaries and detect the adjoining sections again.",". Weichen-GFM aktivieren und angrenzende Abschnitte erneut erkennen.");
                        if(joins[exit].Count()!=2)return LocalText.Select("GFM connection is ambiguous at node: ","GFM-Verbindung am Knoten nicht eindeutig: ")+exit;
                        if(visited.Contains(edge.Id))return Interrupted(edge.Id);
                    }
                }
                visited.Add(edge.Id);previous=edge;previousEnd=end;previousReverse=piece.Reverse;
            }
            bool AtEnd(PlanCut cut,MarkerAnchor end)=>end!=null&&cut.Segment==end.Segment&&Near(cut.Meters,end.Meters);
            var interior=network.Cuts.FirstOrDefault(c=>c.Boundary!=null&&!AtEnd(c,from.Anchor)&&!AtEnd(c,to.Anchor)&&
                block.Path.Any(p=>p.Section.Segment==c.Segment&&c.Meters>=p.Section.From-tolerance&&c.Meters<=p.Section.To+tolerance));
            if(interior!=null)return LocalText.Select("GFM section skips boundary ","GFM-Abschnitt überspringt Grenze ")+(interior.BoundaryCode??interior.Boundary)+
                LocalText.Select(". Detect the sections again.",". Abschnitte erneut erkennen.");
            return null;
        }
        static string Interrupted(string id)=>LocalText.Select("Saved GFM path is interrupted or its direction is inconsistent: ","Gespeicherter GFM-Weg unterbrochen oder Richtung widersprüchlich: ")+id;
    }
}
