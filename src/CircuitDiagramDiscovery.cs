using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public readonly struct CircuitBoundaryPair {
        public readonly string A,B;
        public CircuitBoundaryPair(string a,string b){A=a;B=b;}
    }
    public static class CircuitDiagramDiscovery {
        static int Ports(PanelTile t)=>t.Kind==PanelTileKind.TrainNumber&&t.Ports==0?PanelConnections.Transform(17,t.Rotation,t.Mirror):PanelConnections.Ports(t);
        public static CircuitBoundaryPair[] Find(PanelDocument doc,int x,int y,Func<PanelTile,int?,string> boundary) {
            var cells=doc.Tiles.ToDictionary(t=>(t.X,t.Y));
            if(!cells.TryGetValue((x,y),out var seed)||!CircuitDiagramAssignment.RailField(seed)&&seed.Kind!=PanelTileKind.Signal)return Array.Empty<CircuitBoundaryPair>();
            string own=boundary(seed,null);var ends=new List<string>();int ports=Ports(seed);
            for(int port=0;port<8;port++) {
                if((ports&(1<<port))==0)continue;
                var previous=seed;int direction=port;var seen=new HashSet<(int,int)>{(x,y)};string end=null;
                while(seen.Count<512) {
                    if(!cells.TryGetValue((previous.X+PanelConnections.Dx[direction],previous.Y+PanelConnections.Dy[direction]),out var tile)||!seen.Add((tile.X,tile.Y)))break;
                    int back=(direction+4)%8,mask=Ports(tile);if((mask&(1<<back))==0)break;
                    end=boundary(tile,back);if(end!=null)break;
                    if(!CircuitDiagramAssignment.NextPort(tile,back,out direction))break;previous=tile;
                }
                ends.Add(end);
            }
            if(own!=null)return ends.Where(e=>e!=null&&e!=own).Distinct().Select(e=>new CircuitBoundaryPair(own,e)).ToArray();
            return ends.Count==2&&ends.All(e=>e!=null)&&ends[0]!=ends[1]?new[]{new CircuitBoundaryPair(ends[0],ends[1])}:Array.Empty<CircuitBoundaryPair>();
        }
    }
}
