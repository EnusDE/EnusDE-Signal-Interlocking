using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class SetupObject {public string Id,Caption;public bool Point;public MarkerAnchor Anchor;}
    public static class SetupAssignments {
        public static SetupObject[] Pending(DeskLibrary library,IEnumerable<MarkerRecord> signals,NetworkInput network) {
            var assignedSignals=library.Desks.Where(d=>!d.IsMaster).SelectMany(d=>d.Panel.Tiles).Where(t=>t.Kind==PanelTileKind.Signal&&!t.ReadOnlySignal).SelectMany(t=>new[]{t.Reference,t.AlternateSignal}).ToHashSet();
            var assignedPoints=library.Desks.Where(d=>!d.IsMaster).SelectMany(d=>d.Panel.Tiles).Where(t=>t.Kind==PanelTileKind.Switch).Select(t=>t.Reference).ToHashSet();
            return signals.Where(s=>s.IsSignal&&!s.Planned&&!assignedSignals.Contains(s.Id)).Select(s=>new SetupObject{Id=s.Id,Caption=s.Name,Anchor=s.Anchor.Clone()})
                .Concat(network.Nodes.Where(n=>n.Point&&!assignedPoints.Contains(n.Id)).Select(n=>new SetupObject{Id=n.Id,Caption=library.Points.TryGetValue(n.Id,out var point)?point.Code:n.Id,Point=true}))
                .OrderBy(s=>s.Point).ThenBy(s=>s.Caption,StringComparer.OrdinalIgnoreCase).ToArray();
        }
        public static bool CanAssign(PanelTile tile,bool point)=>tile==null||
            tile.Target==null&&tile.AlternateSignal==null&&tile.CircuitBoundary==null&&!tile.ReadOnlySignal&&!NumberMirrors.IsMirror(tile)&&
            (tile.Kind==(point?PanelTileKind.Switch:PanelTileKind.Signal)&&string.IsNullOrEmpty(tile.Reference)||
             (tile.Kind==PanelTileKind.Straight||tile.Kind==PanelTileKind.Diagonal||tile.Kind==PanelTileKind.Curve)&&(!point&&PanelConnections.Count(PanelConnections.Ports(tile))==2));
        public static DeskLibrary Assign(DeskLibrary library,SetupObject source,string deskId,int x,int y,MarkerRecord signal) {
            var desk=library.Desks.Single(d=>d.Id==deskId&&!d.IsMaster);
            if(!CanAssign(desk.Panel.At(x,y),source.Point))throw new InvalidOperationException(LocalText.Select("The field is already assigned or has incompatible connections.","Das Feld ist bereits zugeordnet oder hat unpassende Verbindungen."));
            if(library.Desks.Where(d=>!d.IsMaster).SelectMany(d=>d.Panel.Tiles).Any(t=>source.Point?t.Kind==PanelTileKind.Switch&&t.Reference==source.Id:t.Kind==PanelTileKind.Signal&&!t.ReadOnlySignal&&(t.Reference==source.Id||t.AlternateSignal==source.Id)))throw new InvalidOperationException(LocalText.Select("This object has already been assigned. Refresh the assistant.","Dieses Objekt ist bereits zugeordnet. Assistent aktualisieren."));
            if(!source.Point&&(signal==null||!signal.IsSignal||signal.Planned||signal.Id!=source.Id))throw new InvalidOperationException(LocalText.Select("The world signal is no longer available.","Das Weltsignal ist nicht mehr verfügbar."));
            var next=library.Clone();var panel=next.Desks.Single(d=>d.Id==deskId).Panel;
            var tile=panel.At(x,y)?.Clone()??new PanelTile{X=x,Y=y,Ports=source.Point?0:17};
            tile.Kind=source.Point?PanelTileKind.Switch:PanelTileKind.Signal;tile.Reference=source.Id;tile.ReadOnlySignal=false;
            if(new[]{"Signal","Hauptsignal","Main signal","Rangiersignal","Shunting signal","Vorsignal","Distant signal","Weiche ?","Point ?","Weiche","Point"}.Contains(tile.Label,StringComparer.OrdinalIgnoreCase))tile.Label="";
            if(source.Point) {
                PointAssignments.NameDiscovered(next,new[]{source.Id});next.Points[source.Id].Desk=deskId;
                tile.BlockId=next.Blocks.ContainsKey("p:"+source.Id)?"p:"+source.Id:"";
            } else {
                tile.AlternateSignal=signal.Kind==MarkerRecord.Main&&signal.AllowSh1?signal.Id:null;
                if(CircuitIdentity.Enabled(signal))tile.CircuitBoundary=signal.PhysicalId??signal.Id;
            }
            panel.Put(tile);next.Validate(library.WorldKey);return next;
        }
    }
}
