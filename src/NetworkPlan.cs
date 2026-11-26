using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace EnusDE.Signaltechnik {
    public sealed class PlanNode {public string Id,Common,Normal;public double X,Y;public bool Point,ScopeBoundary;}
    public sealed class PlanEdge {public string Id,A,B;public double Length;}
    public sealed class PlanCut {public string Segment,Signal,PhysicalSignal,Kind,Boundary,BoundaryCode;public int BoundaryKind;public bool Reverse,BoundaryReverse;public double Meters,X,Y;}
    public sealed class NetworkInput {public string World;public List<PlanNode> Nodes=new List<PlanNode>();public List<PlanEdge> Edges=new List<PlanEdge>();public List<PlanCut> Cuts=new List<PlanCut>();}
    public sealed class NetworkResult {public PanelDocument Panel;public Dictionary<string,TrackBlock> Blocks=new Dictionary<string,TrackBlock>();public int Segments,ScopeSegments,Chains,Drawn;public List<string> Missing=new List<string>();}
    public static class NetworkPlan {
        sealed class Piece {public string A,B;public TrackSection S;}
        sealed class Chain {public string A,B;public TrackBlock Block;}
        sealed class Cell {public int X,Y;public Cell(int x,int y){X=x;Y=y;}public int Key=>Y*PanelDocument.MaxColumns+X;}
        public static NetworkResult Build(NetworkInput input,CancellationToken cancel=default,bool draw=true,int attempt=0) {
            if(input.Nodes.Count>20000||input.Edges.Count>24000)throw new InvalidOperationException("Gleisnetz überschreitet die Prototypgrenze.");
            var nodes=input.Nodes.ToDictionary(n=>n.Id);var adjacency=nodes.Keys.ToDictionary(id=>id,id=>new List<Piece>());var pieces=new List<Piece>();
            foreach(var e in input.Edges.OrderBy(e=>e.Id,StringComparer.Ordinal)){cancel.ThrowIfCancellationRequested();if(!nodes.ContainsKey(e.A)||!nodes.ContainsKey(e.B)||e.Length<=.01||double.IsNaN(e.Length))throw new InvalidOperationException("Ungültiges Gleisnetz.");var cuts=input.Cuts.Where(c=>c.Segment==e.Id&&c.Meters>.001&&c.Meters<e.Length-.001).OrderBy(c=>c.Meters).GroupBy(c=>Math.Round(c.Meters,3)).Select(g=>g.First()).ToArray();string last=e.A;double from=0;
                foreach(var cut in cuts){string id="@"+e.Id+":"+cut.Meters.ToString("R",System.Globalization.CultureInfo.InvariantCulture);nodes[id]=new PlanNode{Id=id,X=cut.X,Y=cut.Y};adjacency[id]=new List<Piece>();var p=new Piece{A=last,B=id,S=new TrackSection{Segment=e.Id,From=from,To=cut.Meters}};pieces.Add(p);adjacency[last].Add(p);adjacency[id].Add(p);last=id;from=cut.Meters;}
                var end=new Piece{A=last,B=e.B,S=new TrackSection{Segment=e.Id,From=from,To=e.Length}};pieces.Add(end);adjacency[last].Add(end);adjacency[e.B].Add(end);
            }
            var boundary=new HashSet<string>(adjacency.Where(a=>a.Value.Count!=2||nodes[a.Key].Point||a.Key.StartsWith("@")).Select(a=>a.Key));
            foreach(var cut in input.Cuts){string at=CutNode(input,cut);if(at!=null&&nodes.ContainsKey(at))boundary.Add(at);}
            var visitedNodes=new HashSet<string>();foreach(string start in adjacency.Keys){cancel.ThrowIfCancellationRequested();if(visitedNodes.Contains(start)||adjacency[start].Count==0)continue;var component=new List<string>();var stack=new Stack<string>();stack.Push(start);while(stack.Count>0){string id=stack.Pop();if(!visitedNodes.Add(id))continue;component.Add(id);foreach(var p in adjacency[id])stack.Push(p.A==id?p.B:p.A);}if(!component.Any(boundary.Contains)){boundary.Add(component.OrderBy(id=>id,StringComparer.Ordinal).First());boundary.Add(component.OrderBy(id=>id,StringComparer.Ordinal).Skip(component.Count/2).First());}}
            // A loop returning to one junction needs a second display boundary: two distinct ports.
            foreach(string start in boundary.ToArray())foreach(var first in adjacency[start]){string at=start;var edge=first;var interior=new List<string>();var seen=new HashSet<Piece>();while(seen.Add(edge)){at=edge.A==at?edge.B:edge.A;if(boundary.Contains(at)){if(at==start&&interior.Count>0)boundary.Add(interior[interior.Count/2]);break;}interior.Add(at);edge=adjacency[at].First(p=>!ReferenceEquals(p,edge));}}
            var detectorNodes=input.Cuts.Where(c=>c.Boundary!=null&&c.BoundaryKind>0).GroupBy(c=>CutNode(input,c)).Where(g=>g.Key!=null&&g.Select(c=>c.Boundary).Distinct().Count()==1).ToDictionary(g=>g.Key,g=>g.First());
            var result=new NetworkResult{Panel=new PanelDocument{WorldKey=input.World,Columns=PanelDocument.MaxColumns,Rows=PanelDocument.MaxRows},Segments=input.Edges.Count};var used=new HashSet<Piece>();var chains=new List<Chain>();
            foreach(string start in boundary.OrderBy(id=>id,StringComparer.Ordinal))foreach(var first in adjacency[start]){if(used.Contains(first))continue;string current=start;var p=first;var path=new List<DirectedRouteSection>();while(true){if(!used.Add(p))throw new InvalidOperationException("Mehrdeutige Gleiskette.");bool reverse=p.B==current;path.Add(new DirectedRouteSection{Section=p.S,Reverse=reverse});current=reverse?p.A:p.B;if(boundary.Contains(current))break;p=adjacency[current].First(next=>!ReferenceEquals(next,p));}
                string forward=string.Join("|",path.Select(s=>s.Section.Key+":"+s.Reverse)),backward=string.Join("|",path.AsEnumerable().Reverse().Select(s=>s.Section.Key+":"+!s.Reverse));string hash=SharedDocuments.Hash(Encoding.UTF8.GetBytes(string.CompareOrdinal(forward,backward)<0?forward:backward));var block=new TrackBlock{Id="b:"+hash.Substring(0,24),Name="Abschnitt "+(chains.Count+1),Path=path};
                if(detectorNodes.TryGetValue(start,out var da)&&detectorNodes.TryGetValue(current,out var db)&&da.Boundary!=db.Boundary&&
                    DetectorFaces(da,path[0],true)&&DetectorFaces(db,path.Last(),false)) {
                    block.Id="c:auto-"+hash.Substring(0,24);block.BoundaryA=da.Boundary;block.BoundaryB=db.Boundary;
                    block.Name=(da.BoundaryCode??da.Boundary)+" ↔ "+(db.BoundaryCode??db.Boundary);if(block.Name.Length>80)block.Name=block.Name.Substring(0,80);
                }
                block.Validate();result.Blocks.Add(block.Id,block);chains.Add(new Chain{A=start,B=current,Block=block});}
            if(used.Count!=pieces.Count)throw new InvalidOperationException("Nicht alle Gleissegmente wurden erfasst.");result.Chains=chains.Count;
            bool DetectorFaces(PlanCut cut,DirectedRouteSection piece,bool departing)=>cut.BoundaryKind!=3||
                TrackCircuit.EndDirection(input,piece,cut.Segment,cut.Meters,departing,out bool reverse)&&cut.BoundaryReverse==(departing?reverse:!reverse);
            var physicalDetectors=input.Cuts.Where(c=>c.Boundary!=null&&c.BoundaryKind>0&&!TrackCircuit.TryPointId(c.Boundary,out _,out _)).Select(c=>new MarkerAnchor{Segment=c.Segment,Meters=c.Meters}).ToArray();
            foreach(var n in input.Nodes.Where(n=>n.Point&&adjacency[n.Id].Count>0)){var block=PointGuardGeometry.Build(n.Id,"Weiche "+n.Id,input.Edges,physicalDetectors);result.Blocks.Add(block.Id,block);}
            if(!draw||pieces.Count==0)return result;
            var active=boundary.Where(id=>adjacency[id].Count>0).Select(id=>nodes[id]).OrderBy(n=>n.Id,StringComparer.Ordinal).ToArray();if(active.Length>1800)throw new InvalidOperationException("Zu viele Netzgrenzen für eine Grundtafel.");
            var schematic=SchematicNetwork.Place(active,chains.Select(c=>new SchematicLink{Id=c.Block.Id,A=c.A,B=c.B,SegmentA=EndSegment(c,c.A),SegmentB=EndSegment(c,c.B),Length=c.Block.Path.Sum(p=>p.Section.To-p.Section.From)}),cancel);
            var positions=schematic.Nodes.ToDictionary(p=>p.Key,p=>new Cell(p.Value.X,p.Value.Y));
            var tiles=new Dictionary<int,PanelTile>();var reserved=new HashSet<int>();foreach(var c in positions.Values)for(int x=c.X-2;x<=c.X+2;x++)for(int y=c.Y-2;y<=c.Y+2;y++)reserved.Add(y*PanelDocument.MaxColumns+x);
            var ports=new Dictionary<string,int>();foreach(var n in active){var arms=chains.Where(c=>c.A==n.Id||c.B==n.Id).OrderBy(c=>c.Block.Id,StringComparer.Ordinal).ToArray();var taken=new HashSet<int>();int stem=0,normal=4;if(n.Point){var common=arms.FirstOrDefault(c=>EndSegment(c,n.Id)==n.Common);if(common!=null)stem=schematic.Direction(common.Block.Id,n.Id)>0?0:4;normal=(stem+4)%8;}
                foreach(var c in arms.OrderBy(c=>n.Point&&EndSegment(c,n.Id)==n.Common?0:n.Point&&EndSegment(c,n.Id)==n.Normal?1:2)){var a=positions[n.Id];int target=schematic.Direction(c.Block.Id,n.Id)>0?0:4;if(n.Point){int dy=Math.Sign(schematic.Lanes[c.Block.Id]-a.Y);if(dy==0)dy=-1;target=EndSegment(c,n.Id)==n.Common?stem:EndSegment(c,n.Id)==n.Normal?normal:PanelConnections.Direction(normal==0?1:-1,dy);}int d=Enumerable.Range(0,8).Where(i=>!taken.Contains(i)).OrderBy(i=>Math.Min((i-target+8)%8,(target-i+8)%8)).First();taken.Add(d);ports[n.Id+"|"+c.Block.Id]=d;}
            }
            foreach(var c in chains.OrderBy(c=>attempt==0?Math.Max(Math.Abs(positions[c.A].X-positions[c.B].X),Math.Abs(positions[c.A].Y-positions[c.B].Y)):-Math.Max(Math.Abs(positions[c.A].X-positions[c.B].X),Math.Abs(positions[c.A].Y-positions[c.B].Y))).ThenBy(c=>c.Block.Id,StringComparer.Ordinal)){cancel.ThrowIfCancellationRequested();var a=positions[c.A];var b=positions[c.B];int da=ports[c.A+"|"+c.Block.Id],db=ports[c.B+"|"+c.Block.Id];var sa=new Cell(a.X+2*PanelConnections.Dx[da],a.Y+2*PanelConnections.Dy[da]);var sb=new Cell(b.X+2*PanelConnections.Dx[db],b.Y+2*PanelConnections.Dy[db]);var path=Route(sa,sb,tiles,reserved,cancel,schematic.Lanes[c.Block.Id],schematic.Direction(c.Block.Id,c.A),steps=>result.Missing.Add("limit:"+c.Block.Id));if(path==null){if(!result.Missing.Contains("limit:"+c.Block.Id))result.Missing.Add("path:"+c.Block.Id+":"+sa.X+","+sa.Y+"-"+sb.X+","+sb.Y+"/"+tiles.ContainsKey(sa.Key)+","+tiles.ContainsKey(sb.Key));continue;}path.Insert(0,new Cell(a.X+PanelConnections.Dx[da],a.Y+PanelConnections.Dy[da]));path.Insert(0,a);path.Add(new Cell(b.X+PanelConnections.Dx[db],b.Y+PanelConnections.Dy[db]));path.Add(b);
                bool conflict=false;for(int i=1;i<path.Count-1;i++){PanelTile old;if(!tiles.TryGetValue(path[i].Key,out old))continue;int incoming=PanelConnections.Direction(path[i-1].X-path[i].X,path[i-1].Y-path[i].Y),outgoing=PanelConnections.Direction(path[i+1].X-path[i].X,path[i+1].Y-path[i].Y);int mask=(1<<incoming)|(1<<outgoing);if(old.Kind==PanelTileKind.Switch||old.SecondBlock!=""||(old.Ports&mask)!=0||((incoming+4)%8)!=outgoing){conflict=true;break;}}if(conflict){result.Missing.Add("conflict:"+c.Block.Id);continue;}
                for(int i=0;i<path.Count;i++){var cell=path[i];PanelTile tile;if(!tiles.TryGetValue(cell.Key,out tile))tiles[cell.Key]=tile=new PanelTile{X=cell.X,Y=cell.Y,BlockId=c.Block.Id};else if(tile.BlockId!=c.Block.Id&&i>0&&i<path.Count-1)tile.SecondBlock=c.Block.Id;
                    foreach(int j in new[]{i-1,i+1})if(j>=0&&j<path.Count)tile.Ports|=1<<PanelConnections.Direction(path[j].X-cell.X,path[j].Y-cell.Y);int count=PanelConnections.Count(tile.Ports);tile.Kind=count==3?PanelTileKind.Switch:count==4?PanelTileKind.Cross:PanelTileKind.Straight;var piece=c.Block.Path[Math.Min(c.Block.Path.Count-1,(int)((double)i/path.Count*c.Block.Path.Count))];tile.AutoSegment=piece.Section.Segment;tile.AutoMeters=(piece.Section.From+piece.Section.To)/2;
                }result.Drawn++;
            }
            foreach(var n in active.Where(n=>n.Point)){var cell=positions[n.Id];PanelTile tile;if(!tiles.TryGetValue(cell.Key,out tile))tiles[cell.Key]=tile=new PanelTile{X=cell.X,Y=cell.Y};tile.Kind=PanelTileKind.Switch;tile.Reference=n.Id;tile.BlockId="p:"+n.Id;tile.SecondBlock="";var common=chains.FirstOrDefault(c=>(c.A==n.Id||c.B==n.Id)&&EndSegment(c,n.Id)==n.Common);var normal=chains.FirstOrDefault(c=>(c.A==n.Id||c.B==n.Id)&&EndSegment(c,n.Id)==n.Normal);if(PanelConnections.Count(tile.Ports)!=3)tile.Ports=0;else{if(common!=null)tile.Stem=1<<ports[n.Id+"|"+common.Block.Id];if(normal!=null)tile.Normal=1<<ports[n.Id+"|"+normal.Block.Id];}}
            foreach(var group in input.Cuts.Where(c=>!string.IsNullOrEmpty(c.Signal)).GroupBy(c=>CutNode(input,c))) {
                cancel.ThrowIfCancellationRequested();var cuts=group.GroupBy(c=>c.Signal).Select(g=>g.First()).OrderBy(c=>c.Reverse).ThenBy(c=>c.Signal,StringComparer.Ordinal).ToArray();
                Cell cell;PanelTile tile;
                if(group.Key==null||cuts.Length>2||cuts.Length==2&&(cuts[0].Reverse==cuts[1].Reverse||string.IsNullOrEmpty(cuts[0].PhysicalSignal)||cuts[0].PhysicalSignal!=cuts[1].PhysicalSignal)||!positions.TryGetValue(group.Key,out cell)||!tiles.TryGetValue(cell.Key,out tile)||PanelConnections.Count(tile.Ports)>2||tile.Kind==PanelTileKind.Switch) {
                    result.Missing.AddRange(cuts.Select(c=>"signal:"+c.Signal));continue;
                }
                var arms=chains.Where(c=>c.A==group.Key||c.B==group.Key).ToArray();
                for(int side=0;side<cuts.Length;side++) {
                    var cut=cuts[side];var outgoing=arms.FirstOrDefault(c=>c.A==group.Key?c.Block.Path[0].Reverse==cut.Reverse:c.Block.Path.Last().Reverse!=cut.Reverse);
                    var arm=outgoing??arms.FirstOrDefault();if(arm==null){result.Missing.Add("signal:"+cut.Signal);continue;}
                    int direction=ports[group.Key+"|"+arm.Block.Id];if(outgoing==null)direction=(direction+4)%8;
                    var field=tile;
                    if(side>0) {
                        int key=(cell.Y+PanelConnections.Dy[direction])*PanelDocument.MaxColumns+cell.X+PanelConnections.Dx[direction];
                        if(!tiles.TryGetValue(key,out field)||field.Kind==PanelTileKind.Signal||field.Kind==PanelTileKind.Switch||PanelConnections.Count(field.Ports)!=2){result.Missing.Add("signal:"+cut.Signal);continue;}
                        field.ProjectedMastFace=true;
                    }
                    field.Kind=PanelTileKind.Signal;field.Reference=cut.Signal;field.MainSignalDirection=((direction+1)/2)%4;field.ShuntSignalDirection=field.MainSignalDirection;
                    // Ports stay in diagram coordinates; turning only the head must not rotate the track.
                }
            }
            foreach(var pair in detectorNodes){if(positions.TryGetValue(pair.Key,out var cell)&&tiles.TryGetValue(cell.Key,out var tile)&&tile.Kind!=PanelTileKind.Switch)tile.CircuitBoundary=pair.Value.Boundary;}
            foreach(var n in active.Where(n=>n.ScopeBoundary)){if(positions.TryGetValue(n.Id,out var cell)&&tiles.TryGetValue(cell.Key,out var tile))tile.Label=LocalText.Select("Network boundary","Netzgrenze");}
            result.Panel.Tiles=tiles.Values.OrderBy(t=>t.Y).ThenBy(t=>t.X).ToList();result.Panel.Columns=Math.Max(24,tiles.Values.Max(t=>t.X)+4);result.Panel.Rows=Math.Max(12,tiles.Values.Max(t=>t.Y)+4);result.Panel.Validate(input.World);if(result.Missing.Count>0&&attempt<1){var other=Build(input,cancel,true,attempt+1);if(other.Missing.Count<result.Missing.Count)return other;}return result;
        }
        static string CutNode(NetworkInput input,PlanCut cut) {
            var edge=input.Edges.FirstOrDefault(e=>e.Id==cut.Segment);if(edge==null||double.IsNaN(cut.Meters)||cut.Meters<0||cut.Meters>edge.Length)return null;
            if(cut.Meters<=.001)return edge.A;if(cut.Meters>=edge.Length-.001)return edge.B;
            var canonical=input.Cuts.Where(c=>c.Segment==cut.Segment&&c.Meters>.001&&c.Meters<edge.Length-.001&&Math.Round(c.Meters,3)==Math.Round(cut.Meters,3)).OrderBy(c=>c.Meters).First();
            return "@"+edge.Id+":"+canonical.Meters.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
        }
        static string EndSegment(Chain c,string node)=>node==c.A?c.Block.Path[0].Section.Segment:c.Block.Path[c.Block.Path.Count-1].Section.Segment;
        sealed class Queue {readonly List<Tuple<double,int>> heap=new List<Tuple<double,int>>();public int Count=>heap.Count;public void Push(int state,double f){var t=Tuple.Create(f,state);int i=heap.Count;heap.Add(t);while(i>0){int p=(i-1)/2;if(heap[p].Item1<=f)break;heap[i]=heap[p];i=p;}heap[i]=t;}public int Pop(){int value=heap[0].Item2;var last=heap[heap.Count-1];heap.RemoveAt(heap.Count-1);if(heap.Count>0){int i=0;while(i*2+1<heap.Count){int c=i*2+1;if(c+1<heap.Count&&heap[c+1].Item1<heap[c].Item1)c++;if(heap[c].Item1>=last.Item1)break;heap[i]=heap[c];i=c;}heap[i]=last;}return value;}}
        static List<Cell> LanePath(Cell a,Cell b,int lane,int direction,Dictionary<int,PanelTile> tiles,HashSet<int> reserved,bool diagonal) {
            var path=new List<Cell>{a};
            var left=new Cell(a.X+(diagonal?Math.Abs(lane-a.Y)*direction:0),lane);var right=new Cell(b.X-(diagonal?Math.Abs(lane-b.Y)*direction:0),lane);
            if((right.X-left.X)*direction<0)return null;
            void To(Cell end){while(path.Last().X!=end.X||path.Last().Y!=end.Y){var at=path.Last();path.Add(new Cell(at.X+Math.Sign(end.X-at.X),at.Y+Math.Sign(end.Y-at.Y)));}}
            To(left);To(right);To(b);
            if(path.Select(c=>c.Key).Distinct().Count()!=path.Count)return null;
            for(int i=0;i<path.Count;i++) {
                var cell=path[i];if(cell.X<2||cell.X>=PanelDocument.MaxColumns-4||cell.Y<2||cell.Y>=PanelDocument.MaxRows-4||i>0&&i<path.Count-1&&reserved.Contains(cell.Key))return null;
                if(!tiles.TryGetValue(cell.Key,out var old))continue;
                if(i==0||i==path.Count-1||old.Kind==PanelTileKind.Switch||old.SecondBlock!="")return null;
                int incoming=PanelConnections.Direction(path[i-1].X-cell.X,path[i-1].Y-cell.Y),outgoing=PanelConnections.Direction(path[i+1].X-cell.X,path[i+1].Y-cell.Y);
                if((incoming+4)%8!=outgoing||(old.Ports&((1<<incoming)|(1<<outgoing)))!=0)return null;
            }
            return path;
        }
        static List<Cell> Route(Cell a,Cell b,Dictionary<int,PanelTile> tiles,HashSet<int> reserved,CancellationToken cancel,int lane,int direction,Action<int> exhausted=null) {
            var direct=LanePath(a,b,lane,direction,tiles,reserved,true)??LanePath(a,b,lane,direction,tiles,reserved,false);if(direct!=null)return direct;
            const int width=PanelDocument.MaxColumns,height=PanelDocument.MaxRows;var open=new Queue();var costs=new Dictionary<int,double>();var prev=new Dictionary<int,int>();var closed=new HashSet<int>();int start=a.Key*8;costs[start]=0;open.Push(start,0);int goal=-1,steps=0;
            while(open.Count>0){if((++steps&4095)==0)cancel.ThrowIfCancellationRequested();if(steps>2000000){exhausted?.Invoke(steps);break;}int state=open.Pop();if(!closed.Add(state))continue;int cell=state/8,d=state%8,x=cell%width,y=cell/width;if(cell==b.Key){goal=state;break;}PanelTile current;tiles.TryGetValue(cell,out current);
                for(int dir=0;dir<8;dir++){int nx=x+PanelConnections.Dx[dir],ny=y+PanelConnections.Dy[dir],key=ny*width+nx;if(nx<2||nx>=width-4||ny<2||ny>=height-4||reserved.Contains(key)&&key!=b.Key)continue;if(current!=null&&cell!=a.Key&&(dir!=d||current.SecondBlock!=""))continue;PanelTile target;if(tiles.TryGetValue(key,out target)){if(target.Kind==PanelTileKind.Switch||target.SecondBlock!=""||PanelConnections.Count(target.Ports)!=2||(target.Ports&((1<<dir)|(1<<((dir+4)%8))))!=0)continue;}
                    int next=key*8+dir;double cost=costs[state]+(dir%2==0?1:1.42)+(cell==a.Key||d==dir?0:.8)+(target==null?0:7)+Math.Min(20,Math.Abs(ny-lane))*.04;double old;if(costs.TryGetValue(next,out old)&&old<=cost)continue;costs[next]=cost;prev[next]=state;open.Push(next,cost+1.03*(Math.Max(Math.Abs(nx-b.X),Math.Abs(ny-b.Y))+.42*Math.Min(Math.Abs(nx-b.X),Math.Abs(ny-b.Y))));
                }
            }if(goal<0)return null;var path=new List<Cell>();int at=goal;while(true){int cell=at/8;path.Add(new Cell(cell%width,cell/width));if(at==start)break;at=prev[at];}path.Reverse();var simple=new List<Cell>();foreach(var cell in path){int repeated=simple.FindIndex(c=>c.Key==cell.Key);if(repeated>=0)simple.RemoveRange(repeated+1,simple.Count-repeated-1);else simple.Add(cell);}return simple;
        }
    }
}
