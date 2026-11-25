using System.Collections.Generic;
using System.Linq;
using Track;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        static readonly Color unknownRail=new Color(.65f,.5f,.18f);
        static readonly Color freeRail=new Color(.5f,.55f,.55f);

        TrackOccupancy ReadPanelKey(string key)=>Time.realtimeSinceStartup-panelReadTime<5&&
            panelReadings.TryGetValue(key,out var read)?read:TrackOccupancy.Unknown;

        bool DisplayMonitored(string id)=>CircuitDisplayPolicy.Monitored(desks,id);
        static Color RouteRailColor(TrackOccupancy occupancy,PanelRouteKind kind) {
            switch(PanelRouteStyle.Resolve(occupancy,kind)){case PanelRailInk.Occupied:return Color.red;case PanelRailInk.Unknown:return unknownRail;case PanelRailInk.Train:return Color.green;case PanelRailInk.Shunt:return Color.white;default:return freeRail;}
        }
        Color RailColor(string blockId) {
            if(!DisplayMonitored(blockId))return unknownRail;
            var state=ReadPanelKey("block:"+blockId);
            routeBlockKinds.TryGetValue(blockId,out var kind);return RouteRailColor(state,kind);
        }

        PanelTile Neighbour(PanelTile tile,int port) {
            var other=panelLookup.Read(panel,tile.X+PanelConnections.Dx[port],tile.Y+PanelConnections.Dy[port]);
            return other!=null&&(PanelConnections.Ports(other)&(1<<((port+4)%8)))!=0?other:null;
        }

        IEnumerable<PanelTile> OccupancyNeighbours(PanelTile tile) {
            if(tile.Kind!=PanelTileKind.Signal&&tile.Kind!=PanelTileKind.TrainNumber)yield break;
            int mask=RailPorts(tile);
            for(int i=0;i<8;i++)
                if((mask&(1<<i))!=0&&Neighbour(tile,i) is PanelTile other&&other.Kind!=PanelTileKind.Signal&&
                    other.Kind!=PanelTileKind.Switch&&other.Kind!=PanelTileKind.Cross)yield return other;
        }

        static int RailPorts(PanelTile tile) {
            if(NumberMirrors.IsMirror(tile))return 0;
            int mask=PanelConnections.Ports(tile);
            if(mask==0&&tile.Kind==PanelTileKind.TrainNumber)
                mask=PanelConnections.Transform(17,tile.Rotation,tile.Mirror);
            return mask;
        }

        void ReadSymbolRail(PanelTile tile,ref PanelVisual view) {
            int ports=RailPorts(tile);
            if(tile.Kind==PanelTileKind.Signal||tile.Kind==PanelTileKind.TrainNumber||tile.CircuitRails!=null&&CircuitDiagramAssignment.RailField(tile)) {
                view.RailPorts=ports;
                view.PortColors=new Dictionary<int,Color>();
                if(tile.Kind==PanelTileKind.Signal) {
                    string middle=BetweenSignals(tile);
                    if(!string.IsNullOrEmpty(middle)) {
                        view.HasMiddleRail=true;
                        view.MiddleRail=RailColor(middle);
                    }
                }
                for(int i=0;i<8;i++) {
                    if((ports&(1<<i))==0)continue;
                    if(tile.CircuitRails!=null&&tile.CircuitRails.TryGetValue(LocalPort(tile,i),out var linked)) {
                        view.PortColors[i]=RailColor(linked);
                        continue;
                    }
                    var next=Neighbour(tile,i);
                    if(tile.Kind==PanelTileKind.TrainNumber&&(tile.Section!=null||tile.BlockId!="")) {
                        view.PortColors[i]=view.Rail;
                        continue;
                    }
                    if(next==null||next.Kind==PanelTileKind.Switch||next.Kind==PanelTileKind.Signal||
                        next.Kind==PanelTileKind.Cross||next.Section==null&&next.BlockId=="") {
                        view.PortColors[i]=unknownRail;
                        continue;
                    }
                    if(next.Section!=null)
                        view.PortColors[i]=RouteRailColor(ReadPanelKey(next.Section.Key),TileRouteKind(next));
                    else view.PortColors[i]=RailColor(next.BlockId);
                }
            } else if(tile.Kind==PanelTileKind.Switch&&tile.Reference!="") {
                if(!PointCircuitVisibility.Enabled(desks,tile.Reference)) {
                    view.RailPorts=ports;view.PortColors=new Dictionary<int,Color>();view.Unknown=true;view.Rail=unknownRail;
                    for(int i=0;i<8;i++)if((ports&(1<<i))!=0)view.PortColors[i]=unknownRail;
                    return;
                }
                var graph=Graph.Shared;
                var node=graph?.GetNode(tile.Reference);
                if(node==null||!desks.Blocks.TryGetValue("p:"+tile.Reference,out var block)||
                    !graph.DecodeSwitchAt(node,out var common,out var normal,out var branch))return;
                try{block=ReadPointDefinition(block);}catch{view.RailPorts=ports;view.PortColors=new Dictionary<int,Color>();view.Unknown=true;for(int p=0;p<8;p++)if((ports&(1<<p))!=0)view.PortColors[p]=unknownRail;return;}
                if(tile.Ports==0) {
                    view.RailPorts=ports;
                } else view.RailPorts=PanelConnections.Ports(tile);
                view.PortColors=new Dictionary<int,Color>();
                int stem,straight,diverging;
                if(tile.Ports==0){stem=16;straight=1;diverging=2;}
                else PanelConnections.Arms(tile,out stem,out straight,out diverging);
                var masks=new[]{stem,straight,diverging};
                var tracks=new[]{common,normal,branch};
                for(int i=0;i<3;i++) {
                    int worldPort=PanelConnections.Transform(masks[i],tile.Rotation,tile.Mirror);
                    var guard=block.Guards.FirstOrDefault(g=>g.Segment==tracks[i]?.id);
                    var occupied=guard!=null?ReadPanelKey(guard.Key):TrackOccupancy.Unknown;
                    bool active=i==0||view.PointKnown&&(view.Thrown?i==2:i==1);
                    Color color=occupied==TrackOccupancy.Occupied?Color.red:!active?new Color(.18f,.2f,.21f):
                        occupied==TrackOccupancy.Unknown?unknownRail:RouteRailColor(occupied,TileRouteKind(tile));
                    for(int port=0;port<8;port++)if((worldPort&(1<<port))!=0) {
                    view.PortColors[port]=color;
                    }
                }
            }

            view.Terminal=TerminalField(tile);
            string boundary=tile.CircuitBoundary;
            if(boundary==null&&tile.Kind==PanelTileKind.Signal&&!tile.ProjectedMastFace) {
                var signal=Object(tile.Reference);
                var shunt=Object(tile.AlternateSignal);
                if(CircuitIdentity.Enabled(signal))boundary=signal.PhysicalId??signal.Id;
                if(CircuitIdentity.Enabled(shunt)&&(shunt.PhysicalId??shunt.Id)!=boundary)
                    view.OtherBoundary=BoundaryCaption(shunt.PhysicalId??shunt.Id);
            }
            if(boundary!=null&&(editingWorkspace||CircuitEndpoint(boundary) is MarkerRecord end&&CircuitIdentity.Variant(end)==3))view.Boundary=BoundaryCaption(boundary);
            if(!editingWorkspace)view.OtherBoundary=null;
        }
    }
}
