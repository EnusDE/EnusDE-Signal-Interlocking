using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class CircuitPath {
        public readonly List<DirectedRouteSection> Sections=new List<DirectedRouteSection>();
        public readonly List<string> Points=new List<string>();
        public double Length=>Sections.Sum(p=>p.Section.To-p.Section.From);
    }

    public static class TrackCircuit {
        public static readonly string[] PointArms={"Stammgleis","gerader Ast","Abzweig"};
        public static string PointId(string point,int arm)=>"w:"+point+":"+arm;
        public static bool TryPointId(string id,out string point,out int arm) {
            point=null;arm=-1;
            if(id==null||id.Length<5||id.Length>132||!id.StartsWith("w:",StringComparison.Ordinal)||id[id.Length-2]!=':')return false;
            arm=id[id.Length-1]-'0';
            point=id.Substring(2,id.Length-4);
            return arm>=0&&arm<3&&point.Length>0&&!point.Any(char.IsControl);
        }
        public static bool ValidId(string id)=>Guid.TryParseExact(id,"N",out _)||TryPointId(id,out _,out _);
        public static string Pair(string a,string b)=>string.CompareOrdinal(a,b)<=0?a+"|"+b:b+"|"+a;
        public static bool IsBoundary(MarkerRecord marker)=>marker!=null&&!marker.Planned&&
            (marker.Kind==MarkerRecord.Main||marker.Kind==MarkerRecord.Shunt||marker.IsCircuitBox||marker.IsSignal&&CircuitIdentity.Enabled(marker));

        public static List<CircuitPath> Find(NetworkInput graph,MarkerAnchor from,MarkerAnchor to,out bool limited) {
            limited=false;
            var edges=graph.Edges.ToDictionary(e=>e.Id,StringComparer.Ordinal);
            var paths=new List<CircuitPath>();
            if(from==null||to==null||!edges.TryGetValue(from.Segment,out var first)||
                !edges.TryGetValue(to.Segment,out var last)||from.Meters>first.Length||to.Meters>last.Length||
                first==last&&Math.Abs(from.Meters-to.Meters)<.01)return paths;
            var nodes=graph.Nodes.ToDictionary(n=>n.Id,StringComparer.Ordinal);
            var joins=graph.Edges.SelectMany(e=>new[]{(node:e.A,edge:e),(node:e.B,edge:e)})
                .ToLookup(x=>x.node,x=>x.edge,StringComparer.Ordinal);
            var current=new CircuitPath();
            var used=new HashSet<string>(StringComparer.Ordinal);
            int work=0;
            void Walk(PlanEdge edge,double start,bool reverse) {
                if(++work>1024||paths.Count>=8||used.Count>=128||!used.Add(edge.Id))return;
                bool atTarget=edge==last&&(reverse?to.Meters<=start:to.Meters>=start);
                double end=atTarget?to.Meters:reverse?0:edge.Length;
                bool added=Math.Abs(start-end)>.001;
                if(added)current.Sections.Add(new DirectedRouteSection{
                    Reverse=reverse,Section=new TrackSection{Segment=edge.Id,From=Math.Min(start,end),To=Math.Max(start,end)}
                });
                if(atTarget) {
                    if(current.Sections.Count>0&&current.Length>=.01&&
                        EndDirection(graph,current.Sections[0],from.Segment,from.Meters,true,out _)&&
                        EndDirection(graph,current.Sections.Last(),to.Segment,to.Meters,false,out _)) {
                        var candidate=new CircuitPath();
                        candidate.Sections.AddRange(current.Sections);
                        candidate.Points.AddRange(current.Points);
                        paths.Add(candidate);
                    }
                } else {
                    string id=reverse?edge.A:edge.B;
                    if(nodes.TryGetValue(id,out var node)) {
                        var links=joins[id];
                        if(node.Point)current.Points.Add(id);
                        foreach(var next in links.Where(e=>e!=edge).OrderBy(e=>e.Id,StringComparer.Ordinal)) {
                            if(node.Point?links.Count()!=3||(edge.Id!=node.Common&&next.Id!=node.Common):links.Count()!=2)continue;
                            Walk(next,next.A==id?0:next.Length,next.A!=id);
                        }
                        if(node.Point)current.Points.RemoveAt(current.Points.Count-1);
                    }
                }
                if(added)current.Sections.RemoveAt(current.Sections.Count-1);
                used.Remove(edge.Id);
            }
            Walk(first,from.Meters,false);
            bool forwardLimited=work>1024;
            work=0;
            Walk(first,from.Meters,true);
            limited=forwardLimited||work>1024||paths.Count>=8;
            return paths.OrderBy(p=>p.Length).ToList();
        }

        public static List<CircuitPath> Find(NetworkInput graph,MarkerRecord from,MarkerRecord to,out bool limited) {
            var paths=Find(graph,from?.Anchor,to?.Anchor,out limited);
            return paths.Where(p=>MatchesBoundary(graph,p.Sections[0],from,true)&&MatchesBoundary(graph,p.Sections.Last(),to,false)&&
                !graph.Cuts.Any(c=>c.Boundary!=null&&p.Sections.Any(s=>Contains(s.Section,c.Segment,c.Meters))&&
                    !SameLocation(graph,c.Segment,c.Meters,from.Anchor)&&!SameLocation(graph,c.Segment,c.Meters,to.Anchor))).ToList();
        }
        public static bool InteriorClear(TrackBlock block,MarkerRecord from,MarkerRecord to,IEnumerable<MarkerRecord> boundaries)=>
            InteriorClear(block,from,to,boundaries,null);
        public static bool InteriorClear(TrackBlock block,MarkerRecord from,MarkerRecord to,IEnumerable<MarkerRecord> boundaries,NetworkInput network) {
            return !boundaries.Where(CircuitIdentity.Enabled).Any(c=>block.Path.Any(p=>Contains(p.Section,c.Anchor.Segment,c.Anchor.Meters))&&
                !SameLocation(network,c.Anchor.Segment,c.Anchor.Meters,from?.Anchor)&&!SameLocation(network,c.Anchor.Segment,c.Anchor.Meters,to?.Anchor));
        }

        public static bool EndsMatch(TrackBlock block,MarkerRecord a,MarkerRecord b)=>EndsMatch(block,a,b,null);
        public static bool EndsMatch(TrackBlock block,MarkerRecord a,MarkerRecord b,NetworkInput network) =>
            IsBoundary(a)&&IsBoundary(b)&&block.Path.Count>0&&MatchesBoundary(network,block.Path[0],a,true)&&MatchesBoundary(network,block.Path.Last(),b,false);

        static bool MatchesBoundary(NetworkInput network,DirectedRouteSection piece,MarkerRecord marker,bool departing)=>
            marker?.Anchor is MarkerAnchor anchor&&EndDirection(network,piece,anchor.Segment,anchor.Meters,departing,out bool reverse)&&
                CircuitIdentity.Accepts(marker,reverse,departing);

        internal static bool EndDirection(NetworkInput network,DirectedRouteSection piece,string segment,double meters,bool departing,out bool reverse) {
            var span=piece.Section;
            double at=piece.Reverse==departing?span.To:span.From;
            reverse=piece.Reverse;
            if(span.Segment==segment)return Math.Abs(at-meters)<.005;
            if(!AtPlainJoin(network,span.Segment,at,segment,meters,out bool detectorAtB))return false;
            // A detector on the adjacent rail uses that rail's coordinates and direction.
            reverse=detectorAtB!=departing;
            return true;
        }

        internal static bool SameLocation(NetworkInput network,string segment,double meters,MarkerAnchor other)=>
            other!=null&&(segment==other.Segment?Math.Abs(meters-other.Meters)<.005:
                AtPlainJoin(network,segment,meters,other.Segment,other.Meters,out _));

        internal static bool Contains(TrackSection span,string segment,double meters)=>
            span.Segment==segment&&meters>=span.From-.005&&meters<=span.To+.005;

        static bool AtPlainJoin(NetworkInput network,string segment,double meters,string other,double otherMeters,out bool otherAtB) {
            otherAtB=false;
            if(network==null)return false;
            var edge=network.Edges.FirstOrDefault(e=>e.Id==segment);
            var next=network.Edges.FirstOrDefault(e=>e.Id==other);
            if(edge==null||next==null)return false;
            string node=Endpoint(edge,meters);
            if(node==null||node!=Endpoint(next,otherMeters)||!network.Nodes.Any(n=>n.Id==node&&!n.Point)||
                network.Edges.Count(e=>e.A==node||e.B==node)!=2)return false;
            otherAtB=node==next.B;
            return true;
        }

        static string Endpoint(PlanEdge edge,double meters)=>Math.Abs(meters)<.005?edge.A:Math.Abs(meters-edge.Length)<.005?edge.B:null;
    }
}
