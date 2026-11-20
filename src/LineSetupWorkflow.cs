using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class LineIndicatorTarget {
        public string Desk,Reference;
        public int X,Y;
        public static LineIndicatorTarget Capture(DeskLibrary library,string desk,int x,int y) {
            var tile=library.Desks.FirstOrDefault(d=>d.Id==desk)?.Panel.At(x,y);
            if(tile?.Kind!=PanelTileKind.LinePermission)throw new InvalidOperationException(LocalText.Select("Select a line permission field first.","Zuerst ein Streckenerlaubnisfeld auswählen."));
            return new LineIndicatorTarget{Desk=desk,X=x,Y=y,Reference=tile.Reference};
        }
        public PanelTile RequireTarget(DeskLibrary library,LineTrackDefinition line) {
            var owner=library.Desks.FirstOrDefault(d=>d.Id==Desk);var tile=owner?.Panel.At(X,Y);
            if(tile?.Kind!=PanelTileKind.LinePermission||tile.Reference!=Reference)throw new InvalidOperationException(LocalText.Select("The destination field changed. Reopen line setup from that field.","Das Zielfeld wurde geändert. Streckeneinrichtung von diesem Feld erneut öffnen."));
            if(!owner.IsMaster&&owner.Id!=line.StationA&&owner.Id!=line.StationB&&owner.Id!=line.BlockOwner&&!line.BlockDisplays.Contains(owner.Id))throw new InvalidOperationException(LocalText.Select("This local interlocking is not a neighbour of the line.","Dieses örtliche Stellwerk ist kein Nachbar der Strecke."));
            return tile;
        }
        public void Bind(DeskLibrary library,LineTrackDefinition line) {
            var tile=RequireTarget(library,line);
            if(!library.FreeLines.Any(l=>l.Id==line.Id))throw new InvalidOperationException(LocalText.Select("Save the line before linking its indicator.","Die Strecke vor dem Verknüpfen des Melders speichern."));
            tile.Reference=line.Id;
        }
        public LineIndicatorTarget FollowView(DeskLibrary before,DeskLibrary after,LineTrackDefinition line) {
            var source=before.Desks.FirstOrDefault(d=>d.Id==Desk);var area=source==null?null:BlockDiagram.Area(source.Panel,line.Id);
            if(area?.Contains(X,Y)==true)return RebindAssembly(before,after,line);
            var destination=after.Desks.FirstOrDefault(d=>d.Id==Desk)?.Panel;
            return new LineIndicatorTarget{Desk=Desk,X=X+(destination?.OriginX??0)-(source?.Panel.OriginX??0),Y=Y+(destination?.OriginY??0)-(source?.Panel.OriginY??0),Reference=Reference};
        }
        public LineIndicatorTarget RebindAssembly(DeskLibrary before,DeskLibrary after,LineTrackDefinition line) {
            var original=before.Desks.FirstOrDefault(d=>d.Id==Desk);var oldArea=original==null?null:BlockDiagram.Area(original.Panel,line.Id);
            if(oldArea?.Contains(X,Y)==true) {
                var destination=after.Desks.FirstOrDefault(d=>d.Id==Desk);var area=destination==null?null:BlockDiagram.Area(destination.Panel,line.Id);
                if(area==null)return null;
                var module=destination.Panel.Tiles.FirstOrDefault(t=>area.Contains(t.X,t.Y)&&t.Kind==PanelTileKind.LinePermission&&t.Reference==line.Id);
                if(module==null)return null;
                return Capture(after,Desk,module.X,module.Y);
            }
            var copy=new LineIndicatorTarget{Desk=Desk,X=X,Y=Y,Reference=Reference};copy.Bind(after,line);copy.Reference=line.Id;return copy;
        }
    }
    public static class LineSetupWorkflow {
        public static LineTrackDefinition Create(DeskLibrary library,string preferredStation) {
            var stations=library.Desks.Where(d=>!d.IsMaster).OrderBy(d=>d.Code,StringComparer.OrdinalIgnoreCase).ToArray();
            if(stations.Length<2)throw new InvalidOperationException(LocalText.Select("Create two local interlockings first.","Zuerst zwei örtliche Stellwerke anlegen."));
            var a=stations.FirstOrDefault(d=>d.Id==preferredStation)??stations[0];var b=stations.First(d=>d.Id!=a.Id);
            var used=new HashSet<string>(library.FreeLines.Select(l=>l.Code),StringComparer.OrdinalIgnoreCase);int number=1;while(used.Contains("STR-"+number.ToString("D3")))number++;
            return new LineTrackDefinition{Code="STR-"+number.ToString("D3"),Name=a.Code+" – "+b.Code,StationA=a.Id,StationB=b.Id};
        }
        public static IEnumerable<LineTrackDefinition> Visible(DeskLibrary library,string desk) {
            var owner=library.Desks.FirstOrDefault(d=>d.Id==desk);bool master=owner?.IsMaster==true;
            var overviews=new HashSet<string>(owner?.Panel.Areas.Where(a=>a.NumberLine!=null).Select(a=>a.NumberLine)??Enumerable.Empty<string>());
            return library.FreeLines.Where(l=>master||l.StationA==desk||l.StationB==desk||l.BlockOwner==desk||l.BlockDisplays.Contains(desk)||overviews.Contains(l.Id)).OrderBy(l=>l.Code,StringComparer.OrdinalIgnoreCase);
        }
        public static (int Forward,int Reverse) MatchDirections(IReadOnlyList<List<DirectedRouteSection>> forward,IReadOnlyList<List<DirectedRouteSection>> reverse) {
            for(int a=0;a<forward.Count;a++)for(int b=0;b<reverse.Count;b++)try{_ = LineTrackGeometry.Axis(forward[a],reverse[b],LineTrackMode.SingleTrack);return (a,b);}catch(System.IO.InvalidDataException){}
            throw new InvalidOperationException(LocalText.Select("No matching opposite path on the same physical track. Check both entry/departure signals and their directions.","Kein passender Gegenfahrweg auf demselben Gleis. Ein-/Ausfahrsignale und ihre Richtungen prüfen."));
        }
        public static TrackBlock[] DetectionCandidates(DeskLibrary library,LineTrackDefinition line,NetworkResult result) {
            var paths=line.Forward.Concat(line.Reverse).ToArray();
            return result.Blocks.Values.Where(b=>b.IsCircuit&&TrackCircuit.ValidId(b.BoundaryA)&&TrackCircuit.ValidId(b.BoundaryB)&&b.BoundaryA!=b.BoundaryB&&b.Path.Any(p=>paths.Any(q=>p.Section.Overlaps(q.Section)))&&
                DirectionBlockBuilder.Covers(b.Path,new[]{new DirectionBlock{Axis=paths.ToList()}})||
                b.Point!=null&&PointCircuitVisibility.Enabled(library,b.Point)&&b.Guards.Any(p=>paths.Any(q=>p.Overlaps(q.Section))))
                .OrderBy(b=>b.Id,StringComparer.Ordinal).Select(b=>b.Clone()).ToArray();
        }
        public static LineTrackDefinition[] Connections(DeskLibrary library,string first,string second,string track) {
            string key=(track??"").Trim();
            return library.FreeLines.Where(l=>((l.StationA==first&&l.StationB==second)||(l.StationA==second&&l.StationB==first))&&string.Equals((l.Track??"").Trim(),key,StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        public static void RequireUnique(DeskLibrary library,LineTrackDefinition line) {
            var duplicate=Connections(library,line.StationA,line.StationB,line.Track).FirstOrDefault(l=>l.Id!=line.Id);
            if(duplicate==null&&line.Forward.Count>0)duplicate=library.FreeLines.FirstOrDefault(l=>l.Id!=line.Id&&l.Forward.Any(a=>line.Forward.Any(b=>a.Section.Overlaps(b.Section))));
            if(duplicate!=null)throw new InvalidOperationException(LocalText.Select("This connection already exists. Edit it instead of creating a copy: ","Diese Verbindung ist bereits vorhanden. Statt einer Kopie den vorhandenen Eintrag bearbeiten: ")+duplicate.Code);
        }
        public static void Put(DeskLibrary library,LineTrackDefinition line,LineIndicatorTarget target=null) {
            line.Validate();RequireUnique(library,line);target?.RequireTarget(library,line);var copy=line.Clone();int at=library.FreeLines.FindIndex(l=>l.Id==line.Id);
            if(at<0)library.FreeLines.Add(copy);else library.FreeLines[at]=copy;
            target?.Bind(library,copy);
        }
    }
}
