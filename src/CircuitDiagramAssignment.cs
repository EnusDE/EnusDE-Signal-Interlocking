using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    // Diagram assignment only: this does not create a route or alter physical occupancy.
    public static class CircuitDiagramAssignment {
        public static bool RailField(PanelTile tile)=>tile!=null&&!NumberMirrors.IsMirror(tile)&&(tile.Kind==PanelTileKind.Straight||
            tile.Kind==PanelTileKind.Diagonal||tile.Kind==PanelTileKind.Curve||tile.Kind==PanelTileKind.Cross||tile.Kind==PanelTileKind.TrainNumber||tile.Kind==PanelTileKind.LinePermission&&tile.InlinePermission||tile.Kind==PanelTileKind.AuxiliaryTarget&&tile.Target!=null);
        public static bool NextPort(PanelTile tile,int back,out int next) {
            next=-1;int mask=Ports(tile);if(tile.CircuitBoundary!=null||(mask&(1<<back))==0)return false;
            if(tile.Kind==PanelTileKind.Cross&&PanelConnections.Count(mask)==4){next=(back+4)%8;return (mask&(1<<next))!=0;}
            if(!RailField(tile)&&!(tile.Kind==PanelTileKind.Signal&&!string.IsNullOrEmpty(tile.Reference))||PanelConnections.Count(mask)!=2)return false;
            next=Enumerable.Range(0,8).Single(p=>p!=back&&(mask&(1<<p))!=0);return true;
        }
        public static int? PointArm(PanelTile tile,int worldPort) {
            if(tile.Kind!=PanelTileKind.Switch)return null;
            int stem,normal,branch;
            if(tile.Ports==0){stem=16;normal=1;branch=2;}else PanelConnections.Arms(tile,out stem,out normal,out branch);
            var arms=new[]{stem,normal,branch};
            for(int i=0;i<3;i++)if((PanelConnections.Transform(arms[i],tile.Rotation,tile.Mirror)&(1<<worldPort))!=0)return i;
            return null;
        }
        static bool MatchesPort(PanelTile tile,string id,int port,Func<string,string> canonical) {
            if(tile.Kind!=PanelTileKind.Switch)return true;
            int? actual=PointArm(tile,port);if(!actual.HasValue)return false;
            string raw=TrackCircuit.PointId(tile.Reference,actual.Value);
            return canonical==null?raw==id:canonical(raw)==canonical(id);
        }
        static int Ports(PanelTile tile)=>tile.Kind==PanelTileKind.TrainNumber&&!NumberMirrors.IsMirror(tile)&&tile.Ports==0?
            PanelConnections.Transform(17,tile.Rotation,tile.Mirror):PanelConnections.Ports(tile);
        public static PanelTile[] Find(PanelDocument document,int x,int y,string first,string last,Func<PanelTile,string,bool> boundary,Func<string,string> canonical=null) {
            var tiles=document.Tiles.ToDictionary(t=>(t.X,t.Y));
            if(!tiles.TryGetValue((x,y),out var seed)||!RailField(seed)&&seed.Kind!=PanelTileKind.Signal&&!(seed.Kind==PanelTileKind.Switch&&(boundary(seed,first)||boundary(seed,last))))throw new InvalidOperationException("Ein Gleis- oder Zugnummernfeld innerhalb des Abschnitts wählen.");
            var paths=Paths(document,first,last,boundary,canonical).Where(p=>p.Contains(seed)).ToArray();
            if(paths.Length!=1)throw new InvalidOperationException(paths.Length==0?
                "Kein eindeutiger durchgehender Gleisbildabschnitt über dieses Feld zwischen beiden Grenzen. Verbindungen und Grenzmarken prüfen oder Felder ausdrücklich markieren.":
                "Mehrere Gleisbildwege enthalten das gewählte Feld. Felder ausdrücklich markieren.");
            return paths[0];
        }
        public static List<PanelTile[]> Paths(PanelDocument document,string first,string last,Func<PanelTile,string,bool> boundary,Func<string,string> canonical=null,bool checkArms=true) {
            if(first==last||string.IsNullOrEmpty(first)||string.IsNullOrEmpty(last)||canonical!=null&&canonical(first)==canonical(last))throw new InvalidOperationException("Zwei unterschiedliche Abschnittsgrenzen wählen.");
            var tiles=document.Tiles.ToDictionary(t=>(t.X,t.Y));
            var paths=new List<PanelTile[]>();int work=0;
            foreach(var start in document.Tiles.Where(t=>boundary(t,first))) {
                int ports=Ports(start);
                for(int port=0;port<8;port++) {
                    if((ports&(1<<port))==0||checkArms&&!MatchesPort(start,first,port,canonical))continue;
                    var path=new List<PanelTile>{start};var seen=new HashSet<(int,int)>{(start.X,start.Y)};
                    var previous=start;int direction=port;
                    while(++work<=8192&&path.Count<4096) {
                        if(!tiles.TryGetValue((previous.X+PanelConnections.Dx[direction],previous.Y+PanelConnections.Dy[direction]),out var tile)||
                            !seen.Add((tile.X,tile.Y)))break;
                        int back=(direction+4)%8,mask=Ports(tile);
                        if((mask&(1<<back))==0)break;
                        path.Add(tile);
                        if(boundary(tile,last)&&(!checkArms||MatchesPort(tile,last,back,canonical))) {
                            paths.Add(path.ToArray());
                            break;
                        }
                        // Crossings keep their opposite ports; no invented turnout or turn.
                        if(!NextPort(tile,back,out direction))break;previous=tile;
                    }
                }
            }
            if(work>8192)throw new InvalidOperationException("Abschnittszeichnung zu umfangreich. Kleineren Bereich ausdrücklich markieren.");
            return paths.GroupBy(p=>string.Join("/",p.Select(t=>t.X+","+t.Y))).Select(g=>g.First()).ToList();
        }
        public static int Assign(PanelDocument document,int x,int y,TrackBlock block,Func<PanelTile,string,bool> boundary,Func<string,string> canonical=null) {
            var path=Find(document,x,y,block.BoundaryA,block.BoundaryB,boundary,canonical);
            for(int i=0;i<path.Length;i++) {
                var tile=path[i];
                if(i==0||i==path.Length-1) {
                    var next=path[i==0?1:i-1];
                    int world=PanelConnections.Direction(next.X-tile.X,next.Y-tile.Y);
                    int mask=PanelConnections.Transform(1<<world,tile.Rotation,tile.Mirror,true);
                    int local=Enumerable.Range(0,8).Single(p=>mask==(1<<p));
                    if(tile.CircuitRails==null)tile.CircuitRails=new Dictionary<int,string>();
                    tile.CircuitRails[local]=block.Id;
                    if(tile.Kind!=PanelTileKind.Switch){tile.Section=null;tile.BlockId=tile.SecondBlock=tile.AutoSegment="";}
                } else if(tile.Kind==PanelTileKind.Cross||tile.Kind==PanelTileKind.Signal) {
                    if(tile.CircuitRails==null)tile.CircuitRails=new Dictionary<int,string>();
                    foreach(var neighbour in new[]{path[i-1],path[i+1]}) {
                        int world=PanelConnections.Direction(neighbour.X-tile.X,neighbour.Y-tile.Y);
                        int localMask=PanelConnections.Transform(1<<world,tile.Rotation,tile.Mirror,true);
                        int local=Enumerable.Range(0,8).Single(p=>localMask==(1<<p));tile.CircuitRails[local]=block.Id;
                    }
                    tile.Section=null;tile.AutoSegment="";tile.BlockId=tile.SecondBlock="";
                } else {
                    tile.Section=null;tile.AutoSegment="";tile.BlockId=block.Id;tile.SecondBlock="";tile.CircuitRails=null;
                }
            }
            return path.Length;
        }
    }
}
