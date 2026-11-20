using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace EnusDE.Signaltechnik {
    public static class NetworkScope {
        public static NetworkInput AroundSignals(NetworkInput input,double distance,CancellationToken cancel=default) {
            if(double.IsNaN(distance)||double.IsInfinity(distance)||distance<100||distance>10000)throw new InvalidOperationException(LocalText.Select("Drawing radius must be 100–10000 metres.","Zeichenradius muss 100–10000 Meter betragen."));
            var edges=input.Edges.ToDictionary(e=>e.Id);var nodes=input.Nodes.ToDictionary(n=>n.Id);
            var seeds=input.Cuts.Where(c=>c.Signal!=null&&edges.ContainsKey(c.Segment)).ToArray();
            if(seeds.Length==0)throw new InvalidOperationException(LocalText.Select("Place at least one signal, or select the entire track network for drawing.","Mindestens ein Signal platzieren oder das gesamte Gleisnetz zum Zeichnen wählen."));
            var joins=input.Edges.SelectMany(e=>new[]{(node:e.A,edge:e),(node:e.B,edge:e)}).ToLookup(p=>p.node,p=>p.edge);
            var best=new Dictionary<string,double>();var queue=new SortedSet<Tuple<double,string>>();
            void Visit(string node,double cost){if(best.TryGetValue(node,out double old)&&old<=cost)return;best[node]=cost;queue.Add(Tuple.Create(cost,node));}
            foreach(var seed in seeds){var e=edges[seed.Segment];Visit(e.A,seed.Meters);Visit(e.B,e.Length-seed.Meters);}
            while(queue.Count>0) {
                cancel.ThrowIfCancellationRequested();var at=queue.Min;queue.Remove(at);if(at.Item1>distance||best[at.Item2]!=at.Item1)continue;
                foreach(var edge in joins[at.Item2])Visit(edge.A==at.Item2?edge.B:edge.A,at.Item1+edge.Length);
            }
            var seedEdges=new HashSet<string>(seeds.Select(c=>c.Segment));
            bool Near(string id)=>best.TryGetValue(id,out double d)&&d<=distance;
            var included=input.Edges.Where(e=>seedEdges.Contains(e.Id)||Near(e.A)||Near(e.B)).OrderBy(e=>e.Id,StringComparer.Ordinal).ToArray();
            var count=included.SelectMany(e=>new[]{e.A,e.B}).GroupBy(id=>id).ToDictionary(g=>g.Key,g=>g.Count());
            var result=new NetworkInput{World=input.World};
            string End(string id,PlanEdge edge) {
                var n=nodes[id];bool cap=!Near(id)||n.Point&&count[id]!=3;
                string key=cap?"scope:"+id+":"+edge.Id:id;
                if(!result.Nodes.Any(p=>p.Id==key))result.Nodes.Add(new PlanNode{Id=key,X=n.X,Y=n.Y,Point=!cap&&n.Point,Common=n.Common,Normal=n.Normal,ScopeBoundary=cap});
                return key;
            }
            foreach(var edge in included)result.Edges.Add(new PlanEdge{Id=edge.Id,A=End(edge.A,edge),B=End(edge.B,edge),Length=edge.Length});
            var kept=new HashSet<string>(included.Select(e=>e.Id));result.Cuts=input.Cuts.Where(c=>kept.Contains(c.Segment)).ToList();
            return result;
        }
        public static NetworkResult DrawNearby(NetworkInput input,double distance,CancellationToken cancel=default) {
            var catalogue=NetworkPlan.Build(input,cancel,false);var scope=AroundSignals(input,distance,cancel);var drawing=NetworkPlan.Build(scope,cancel,true);
            var remap=new Dictionary<string,string>();
            foreach(var block in drawing.Blocks.Values) {
                if(catalogue.Blocks.ContainsKey(block.Id)){remap[block.Id]=block.Id;continue;}
                var match=catalogue.Blocks.Values.Where(b=>b.Point==null&&block.Point==null&&block.Path.All(p=>b.Path.Any(q=>q.Section.Segment==p.Section.Segment&&q.Section.From<=p.Section.From+.001&&q.Section.To>=p.Section.To-.001))).ToArray();
                if(match.Length!=1)throw new InvalidOperationException(LocalText.Select("The drawing boundary cannot be matched to a unique physical section. Keep the existing diagram.","Die Zeichengrenze lässt sich keinem eindeutigen realen Abschnitt zuordnen. Vorhandene Tafel beibehalten."));
                remap[block.Id]=match[0].Id;
            }
            foreach(var tile in drawing.Panel.Tiles){if(remap.TryGetValue(tile.BlockId,out var main))tile.BlockId=main;if(remap.TryGetValue(tile.SecondBlock,out var other))tile.SecondBlock=other;}
            drawing.Blocks=catalogue.Blocks;drawing.ScopeSegments=scope.Edges.Count;drawing.Segments=input.Edges.Count;return drawing;
        }
    }
}
