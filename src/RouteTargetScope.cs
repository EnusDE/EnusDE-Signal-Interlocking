using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class RouteTargetScope {
        public static string Key(string segment,bool reverse)=>(reverse?"r:":"f:")+segment;
        // Reverse reachability only prunes branches that cannot lead to the target.
        // RouteDiscovery still checks signal boundaries, path length and search limits.
        public static HashSet<string> Build(NetworkInput graph,MarkerRecord target) {
            var result=new HashSet<string>(StringComparer.Ordinal);
            if(graph==null||target?.Anchor==null)return result;
            var edges=graph.Edges.ToDictionary(e=>e.Id,StringComparer.Ordinal);
            var nodes=graph.Nodes.ToDictionary(n=>n.Id,StringComparer.Ordinal);
            if(!edges.TryGetValue(target.Anchor.Segment,out var end)||!(target.Anchor.Meters>=0&&target.Anchor.Meters<=end.Length))return result;
            var connections=graph.Edges.SelectMany(e=>new[]{(node:e.A,edge:e),(node:e.B,edge:e)})
                .GroupBy(e=>e.node,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.Select(e=>e.edge).ToArray(),StringComparer.Ordinal);
            var queue=new Queue<(PlanEdge Edge,bool Reverse)>();
            result.Add(Key(end.Id,target.FacingReverse));queue.Enqueue((end,target.FacingReverse));
            while(queue.Count>0) {
                var current=queue.Dequeue();string from=current.Reverse?current.Edge.B:current.Edge.A;
                if(!nodes.TryGetValue(from,out var node)||!connections.TryGetValue(from,out var joins))continue;
                bool point=node.Point&&joins.Length==3&&joins.Any(e=>e.Id==node.Common)&&joins.Any(e=>e.Id==node.Normal);
                if(!point&&(node.Point||joins.Length!=2))continue;
                foreach(var previous in joins.Where(e=>e.Id!=current.Edge.Id&&(!point||e.Id==node.Common||current.Edge.Id==node.Common))) {
                    bool reverse=previous.A==from;
                    if(result.Add(Key(previous.Id,reverse)))queue.Enqueue((previous,reverse));
                }
            }
            return result;
        }
    }
}
