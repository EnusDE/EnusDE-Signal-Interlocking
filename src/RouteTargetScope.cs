using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace EnusDE.Signaltechnik {
    public static class RouteTargetScope {
        public static string Key(string segment,bool reverse)=>(reverse?"r:":"f:")+segment;
        // Reverse reachability only prunes branches that cannot lead to the target.
        // RouteDiscovery still checks signal boundaries, path length and search limits.
        public static HashSet<string> Build(NetworkInput graph,MarkerRecord target)=>Walk(graph,target,true,out _);

        public static HashSet<string> ReachableDestinations(NetworkInput graph,MarkerRecord source,IEnumerable<MarkerRecord> destinations,CancellationToken cancel=default) {
            var reachable=Walk(graph,source,false,out bool returnsToStart,cancel);
            if(reachable.Count==0)return reachable;
            var lengths=graph.Edges.ToDictionary(e=>e.Id,e=>e.Length,StringComparer.Ordinal);
            var result=new HashSet<string>(StringComparer.Ordinal);
            foreach(var destination in destinations) {
                cancel.ThrowIfCancellationRequested();
                var anchor=destination?.Anchor;
                if(anchor==null||destination.Id==source.Id||!lengths.TryGetValue(anchor.Segment,out double length)||
                    !(anchor.Meters>=0&&anchor.Meters<=length)||!reachable.Contains(Key(anchor.Segment,destination.FacingReverse)))continue;
                if(anchor.Segment!=source.Anchor.Segment||destination.FacingReverse!=source.FacingReverse||returnsToStart||
                    (source.FacingReverse?source.Anchor.Meters-anchor.Meters:anchor.Meters-source.Anchor.Meters)>.001)
                    result.Add(destination.Id);
            }
            return result;
        }

        static HashSet<string> Walk(NetworkInput graph,MarkerRecord marker,bool backward,out bool returnsToStart,CancellationToken cancel=default) {
            returnsToStart=false;
            var result=new HashSet<string>(StringComparer.Ordinal);
            if(graph==null||marker?.Anchor==null)return result;
            var edges=graph.Edges.ToDictionary(e=>e.Id,StringComparer.Ordinal);
            var nodes=graph.Nodes.ToDictionary(n=>n.Id,StringComparer.Ordinal);
            if(!edges.TryGetValue(marker.Anchor.Segment,out var start)||!(marker.Anchor.Meters>=0&&marker.Anchor.Meters<=start.Length))return result;
            var connections=graph.Edges.SelectMany(e=>new[]{(node:e.A,edge:e),(node:e.B,edge:e)})
                .GroupBy(e=>e.node,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.Select(e=>e.edge).ToArray(),StringComparer.Ordinal);
            var queue=new Queue<(PlanEdge Edge,bool Reverse)>();
            string startKey=Key(start.Id,marker.FacingReverse);
            result.Add(startKey);queue.Enqueue((start,marker.FacingReverse));
            while(queue.Count>0) {
                cancel.ThrowIfCancellationRequested();
                var current=queue.Dequeue();string from=current.Reverse==backward?current.Edge.B:current.Edge.A;
                if(!nodes.TryGetValue(from,out var node)||!connections.TryGetValue(from,out var joins))continue;
                bool point=node.Point&&joins.Length==3&&joins.Any(e=>e.Id==node.Common)&&joins.Any(e=>e.Id==node.Normal);
                if(!point&&(node.Point||joins.Length!=2))continue;
                foreach(var edge in joins.Where(e=>e.Id!=current.Edge.Id&&(!point||e.Id==node.Common||current.Edge.Id==node.Common))) {
                    bool reverse=(edge.A==from)==backward;
                    string key=Key(edge.Id,reverse);
                    if(result.Add(key))queue.Enqueue((edge,reverse));
                    else if(key==startKey)returnsToStart=true;
                }
            }
            return result;
        }
    }
}
