using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace EnusDE.Signaltechnik {
    public sealed class SchematicLink {
        public string Id,A,B,SegmentA,SegmentB;
        public double Length;
        public string Other(string node)=>node==A?B:A;
        public string SegmentAt(string node)=>node==A?SegmentA:SegmentB;
    }
    public sealed class SchematicCell {
        public int X,Y;
        public SchematicCell(int x,int y){X=x;Y=y;}
    }
    public sealed class SchematicPlacement {
        public readonly Dictionary<string,SchematicCell> Nodes=new Dictionary<string,SchematicCell>();
        public readonly Dictionary<string,int> Lanes=new Dictionary<string,int>();
        public readonly Dictionary<string,string> Forward=new Dictionary<string,string>();
        public int Direction(string link,string node)=>Forward[link]==node?1:-1;
    }
    // World curves and distances do not define a control-panel drawing. Join the
    // common/normal switch legs into continuous strands, then lay those strands
    // out on horizontal lanes. Every physical chain retains its identity.
    public static class SchematicNetwork {
        sealed class Strand {
            public readonly List<string> Nodes=new List<string>();
            public readonly List<SchematicLink> Links=new List<SchematicLink>();
            public string Id=>Links.Min(l=>l.Id);
            public double Length=>Links.Sum(l=>l.Length);
            public int Row;
        }
        sealed class Constraint {public SchematicLink Link;public string From,To;}
        public static SchematicPlacement Place(IEnumerable<PlanNode> source,IEnumerable<SchematicLink> connections,CancellationToken cancel=default) {
            var nodes=source.ToDictionary(n=>n.Id);var links=connections.OrderBy(l=>l.Id,StringComparer.Ordinal).ToArray();
            var arms=links.SelectMany(l=>new[]{(node:l.A,link:l),(node:l.B,link:l)}).GroupBy(p=>p.node).ToDictionary(g=>g.Key,g=>g.Select(p=>p.link).ToArray());
            var pairs=new Dictionary<string,string>();string Key(string node,string link)=>node+"|"+link;
            void Pair(string node,SchematicLink a,SchematicLink b){pairs[Key(node,a.Id)]=b.Id;pairs[Key(node,b.Id)]=a.Id;}
            foreach(var item in arms) {
                cancel.ThrowIfCancellationRequested();var n=nodes[item.Key];var at=item.Value;
                if(n.Point) {
                    var common=at.SingleOrDefault(l=>l.SegmentAt(n.Id)==n.Common);var normal=at.SingleOrDefault(l=>l.SegmentAt(n.Id)==n.Normal);
                    if(common!=null&&normal!=null&&common!=normal)Pair(n.Id,common,normal);
                } else if(at.Length==2)Pair(n.Id,at[0],at[1]);
            }
            var byId=links.ToDictionary(l=>l.Id);var used=new HashSet<string>();var strands=new List<Strand>();
            void Walk(SchematicLink first,string start) {
                if(used.Contains(first.Id))return;var strand=new Strand();strand.Nodes.Add(start);var edge=first;string at=start;
                while(used.Add(edge.Id)) {
                    cancel.ThrowIfCancellationRequested();strand.Links.Add(edge);at=edge.Other(at);strand.Nodes.Add(at);
                    if(!pairs.TryGetValue(Key(at,edge.Id),out var next)||used.Contains(next))break;edge=byId[next];
                }
                if(strand.Nodes[0]==strand.Nodes.Last()&&strand.Links.Count>1) {
                    int split=strand.Links.Count/2;var a=new Strand();a.Nodes.AddRange(strand.Nodes.Take(split+1));a.Links.AddRange(strand.Links.Take(split));
                    var b=new Strand();b.Nodes.AddRange(strand.Nodes.Skip(split));b.Links.AddRange(strand.Links.Skip(split));strands.Add(a);strands.Add(b);
                } else strands.Add(strand);
            }
            foreach(var link in links)foreach(string end in new[]{link.A,link.B}.OrderBy(id=>id,StringComparer.Ordinal))if(!pairs.ContainsKey(Key(end,link.Id)))Walk(link,end);
            foreach(var link in links)Walk(link,string.CompareOrdinal(link.A,link.B)<=0?link.A:link.B);
            if(strands.Count==0)return new SchematicPlacement();
            var linkStrand=strands.SelectMany(s=>s.Links.Select(l=>(l.Id,strand:s))).ToDictionary(p=>p.Id,p=>p.strand);
            var ordered=new List<Strand>();var estimate=new Dictionary<string,int>();var remaining=strands.ToList();
            while(remaining.Count>0) {
                cancel.ThrowIfCancellationRequested();
                var reachable=remaining.Where(s=>s.Nodes.Any(estimate.ContainsKey)).ToArray();
                var strand=(reachable.Length>0?reachable:remaining.ToArray()).OrderByDescending(s=>s.Length).ThenByDescending(s=>s.Links.Count).ThenBy(s=>s.Id,StringComparer.Ordinal).First();
                var known=strand.Nodes.Select((id,i)=>(id,i)).Where(p=>estimate.ContainsKey(p.id)).ToArray();
                bool reverse=known.Length>1?estimate[known[0].id]>estimate[known.Last().id]:false;
                if(known.Length==0)reverse=string.CompareOrdinal(strand.Nodes[0],strand.Nodes.Last())>0;
                if(known.Length==1) {
                    var at=known[0];var point=nodes[at.id];
                    if(point.Point) {
                        var common=arms[at.id].FirstOrDefault(l=>l.SegmentAt(at.id)==point.Common);var normal=arms[at.id].FirstOrDefault(l=>l.SegmentAt(at.id)==point.Normal);
                        var branch=arms[at.id].FirstOrDefault(l=>l!=common&&l!=normal);
                        SchematicLink ours=null,theirs=null;
                        if(normal!=null&&common!=null&&linkStrand[normal.Id]==strand&&linkStrand[common.Id]==strand){ours=normal;theirs=branch;}
                        else if(branch!=null&&linkStrand[branch.Id]==strand){ours=branch;theirs=normal;}
                        if(ours!=null&&theirs!=null&&estimate.TryGetValue(theirs.Other(at.id),out var other)) {
                            int index=strand.Links.IndexOf(ours);int step=strand.Nodes[index]==at.id?1:-1;
                            if(other!=estimate[at.id])reverse=Math.Sign(other-estimate[at.id])*step<0;
                        }
                    }
                }
                if(reverse){strand.Nodes.Reverse();strand.Links.Reverse();}
                var anchors=strand.Nodes.Select((id,i)=>(id,i)).Where(p=>estimate.ContainsKey(p.id)).ToArray();
                int origin=anchors.Length==0?0:estimate[anchors[0].id]-anchors[0].i*8;
                for(int i=0;i<strand.Nodes.Count;i++)if(!estimate.ContainsKey(strand.Nodes[i]))estimate[strand.Nodes[i]]=origin+i*8;
                remaining.Remove(strand);ordered.Add(strand);
            }
            var owners=new Dictionary<string,Strand>();
            foreach(var item in arms) {
                var n=nodes[item.Key];var common=item.Value.FirstOrDefault(l=>l.SegmentAt(n.Id)==n.Common);var normal=item.Value.FirstOrDefault(l=>l.SegmentAt(n.Id)==n.Normal);
                owners[n.Id]=n.Point&&common!=null&&normal!=null&&linkStrand[common.Id]==linkStrand[normal.Id]?linkStrand[common.Id]:ordered.First(s=>s.Nodes.Contains(n.Id));
            }
            var forward=new Dictionary<string,string>();var constraints=new List<Constraint>();var outgoing=arms.Keys.ToDictionary(id=>id,id=>new List<string>());var returns=new HashSet<string>();
            bool Reaches(string from,string to) {
                var queue=new Stack<string>();var seen=new HashSet<string>();queue.Push(from);
                while(queue.Count>0){var at=queue.Pop();if(at==to)return true;if(!seen.Add(at))continue;foreach(string next in outgoing[at])queue.Push(next);}return false;
            }
            foreach(var strand in ordered)for(int i=0;i<strand.Links.Count;i++) {
                cancel.ThrowIfCancellationRequested();var link=strand.Links[i];string a=strand.Nodes[i],b=strand.Nodes[i+1];forward[link.Id]=a;
                if(Reaches(b,a)){returns.Add(link.Id);continue;}
                outgoing[a].Add(b);constraints.Add(new Constraint{Link=link,From=a,To=b});
            }
            var lanes=new Dictionary<string,int>();
            void Rows(Dictionary<string,int> columns) {
                var occupied=new List<(int row,int from,int to)>();
                int Choose(int from,int to) {
                    for(int r=0;r<=75;r++)foreach(int row in r==0?new[]{0}:new[]{-r,r})if(!occupied.Any(o=>o.row==row&&o.from<=to+2&&from<=o.to+2)){occupied.Add((row,from,to));return row;}
                    throw new InvalidOperationException(LocalText.Select("Too many parallel schematic lanes. Generate a smaller area or edit the diagram manually.","Zu viele parallele Gleisbildzeilen. Einen kleineren Bereich erzeugen oder die Tafel manuell bearbeiten."));
                }
                foreach(var strand in ordered){int from=strand.Nodes.Min(id=>columns[id]),to=strand.Nodes.Max(id=>columns[id]);strand.Row=Choose(from,to);foreach(var link in strand.Links)lanes[link.Id]=strand.Row;}
                foreach(var link in links.Where(l=>returns.Contains(l.Id)))lanes[link.Id]=Choose(Math.Min(columns[link.A],columns[link.B]),Math.Max(columns[link.A],columns[link.B]));
            }
            Dictionary<string,int> Columns() {
                var rank=arms.Keys.ToDictionary(id=>id,id=>0);var degree=arms.Keys.ToDictionary(id=>id,id=>0);var next=constraints.GroupBy(c=>c.From).ToDictionary(g=>g.Key,g=>g.ToArray());
                foreach(var edge in constraints)degree[edge.To]++;
                var queue=new SortedSet<string>(degree.Where(p=>p.Value==0).Select(p=>p.Key),StringComparer.Ordinal);
                while(queue.Count>0) {
                    cancel.ThrowIfCancellationRequested();string at=queue.Min;queue.Remove(at);
                    if(!next.TryGetValue(at,out var following))continue;
                    foreach(var edge in following) {
                        rank[edge.To]=Math.Max(rank[edge.To],rank[at]+8);if(--degree[edge.To]==0)queue.Add(edge.To);
                    }
                }
                return rank;
            }
            var columns=estimate;
            for(int pass=0;pass<8;pass++) {
                Rows(columns);var next=Columns();bool same=next.All(p=>columns[p.Key]==p.Value);columns=next;if(same)break;
            }
            // A final lane allocation uses actual, rather than estimated, column spans.
            Rows(columns);columns=Columns();
            int minRow=lanes.Values.Concat(owners.Values.Select(s=>s.Row)).Min(),maxRow=lanes.Values.Concat(owners.Values.Select(s=>s.Row)).Max();
            if(columns.Values.Max()+12>PanelDocument.MaxColumns||(maxRow-minRow)*6+12>PanelDocument.MaxRows)throw new InvalidOperationException(LocalText.Select("The schematic network exceeds the diagram size. Keep your existing drawing and use section detection, or draw a smaller network manually.","Das schematische Netz überschreitet die Tafelgröße. Vorhandene Zeichnung behalten und Abschnitte erkennen oder ein kleineres Netz manuell zeichnen.")+" "+(columns.Values.Max()+12)+" × "+((maxRow-minRow)*6+12));
            var result=new SchematicPlacement();
            foreach(var item in columns)result.Nodes[item.Key]=new SchematicCell(item.Value+6,(owners[item.Key].Row-minRow)*6+6);
            if(result.Nodes.Values.Select(c=>c.X+":"+c.Y).Distinct().Count()!=result.Nodes.Count)throw new InvalidOperationException(LocalText.Select("Schematic node positions overlap. The existing drawing is retained.","Schematische Knotenpositionen überlappen. Die bisherige Zeichnung bleibt erhalten."));
            foreach(var link in links){result.Lanes[link.Id]=(lanes[link.Id]-minRow)*6+6;result.Forward[link.Id]=forward[link.Id];}
            return result;
        }
    }
}
