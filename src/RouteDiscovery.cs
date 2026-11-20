using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace EnusDE.Signaltechnik {
    public enum RouteKind { Train,Slow,Shunt }

    public static class RouteNaming {
        public static string KindCode(RouteKind kind,bool auxiliary=false)=>(auxiliary?"H":"")+(kind==RouteKind.Shunt?"RF":kind==RouteKind.Slow?"LF":"ZF");
        public static string Prefix(string desk,RouteKind kind,bool auxiliary=false)=>(string.IsNullOrWhiteSpace(desk)?"ESTW":desk.Trim())+"-"+KindCode(kind,auxiliary)+"-";

        public static string Next(string desk,RouteKind kind,IEnumerable<string> existing,bool auxiliary=false) {
            string prefix=Prefix(desk,kind,auxiliary);
            int last=0;
            foreach(string code in existing) {
                if(code!=null&&code.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&
                    int.TryParse(code.Substring(prefix.Length),NumberStyles.None,CultureInfo.InvariantCulture,out int n)&&n>last&&n<=999999)
                    last=n;
            }
            if(last>=999999)throw new InvalidOperationException("Keine weitere Nummer für diese Fahrstraßenart verfügbar.");
            return prefix+(last+1).ToString("D3",CultureInfo.InvariantCulture);
        }

        public static string DeskCode(string name,IEnumerable<string> used) {
            var normalized=(name??"").Normalize(NormalizationForm.FormD);
            var words=new List<string>();
            var word=new StringBuilder();
            foreach(char c in normalized) {
                if(CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark)continue;
                if(char.IsLetterOrDigit(c)){word.Append(char.ToUpperInvariant(c));continue;}
                if(word.Length>0){words.Add(word.ToString());word.Clear();}
            }
            if(word.Length>0)words.Add(word.ToString());
            string basis=words.Count==0?"ORT":words.Count==1?words[0]:string.Concat(words.Select(w=>w[0]));
            if(basis.Length==1&&words.Count>0&&words[0].Length>1)basis=words[0];
            basis=basis.Substring(0,Math.Min(words.Count>1?4:3,basis.Length));
            var codes=new HashSet<string>(used,StringComparer.OrdinalIgnoreCase);
            if(!codes.Contains(basis))return basis;
            for(int i=2;;i++)if(!codes.Contains(basis+i))return basis+i;
        }
    }

    public sealed class RouteProposal {
        public string Entry, Exit;
        public RouteKind Kind;
        public List<DirectedRouteSection> Path;
        public List<RouteSwitch> Switches;
        public double Length=>Path.Sum(p=>p.Section.To-p.Section.From);
        public bool SamePath(RouteProposal other)=>other!=null&&Entry==other.Entry&&Exit==other.Exit&&Kind==other.Kind&&
            Path.Select(p=>p.Section.Key+":"+p.Reverse).SequenceEqual(other.Path.Select(p=>p.Section.Key+":"+p.Reverse))&&
            Switches.Select(p=>p.Node+":"+p.Thrown).SequenceEqual(other.Switches.Select(p=>p.Node+":"+p.Thrown));
    }

    public sealed class RouteSearchReport {
        public bool StartUnavailable, LimitReached;
        public readonly HashSet<string> Ambiguous=new HashSet<string>(StringComparer.Ordinal);
    }

    public sealed class RouteDiscovery {
        const double MaximumDistance=10000;
        readonly Dictionary<string,PlanEdge> edges;
        readonly Dictionary<string,PlanNode> nodes;
        readonly Dictionary<string,PlanEdge[]> connections;
        readonly Dictionary<string,MarkerRecord[]> signals;

        public RouteDiscovery(NetworkInput graph,IEnumerable<MarkerRecord> available,MarkerRecord auxiliary=null) {
            edges=graph.Edges.ToDictionary(edge=>edge.Id,StringComparer.Ordinal);
            nodes=graph.Nodes.ToDictionary(node=>node.Id,StringComparer.Ordinal);
            connections=graph.Edges.SelectMany(e=>new[]{(node:e.A,edge:e),(node:e.B,edge:e)})
                .GroupBy(e=>e.node,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.Select(e=>e.edge).OrderBy(e=>e.Id,StringComparer.Ordinal).ToArray(),StringComparer.Ordinal);
            signals=(AuxiliaryTarget.IsEndpoint(auxiliary)?available.Append(auxiliary):available)
                .Where(s=>(s.IsSignal&&s.Kind!=MarkerRecord.Distant||AuxiliaryTarget.IsEndpoint(s))&&s.Anchor!=null&&edges.ContainsKey(s.Anchor.Segment))
                .GroupBy(s=>s.Anchor.Segment,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.ToArray(),StringComparer.Ordinal);
        }

        public List<RouteProposal> Find(MarkerRecord entry,RouteKind kind,ISet<string> destinations,int workLimit=1024,RouteSearchReport report=null,bool probeThroughSignals=false,bool includeAlternatives=false,ISet<string> allowedDirections=null,CancellationToken cancellation=default) {
            cancellation.ThrowIfCancellationRequested();
            var result=new List<RouteProposal>();
            bool shunt=kind==RouteKind.Shunt;
            if(!SignalCatalogue.Supports(entry,shunt,kind==RouteKind.Slow)||entry.Anchor==null||
                !edges.TryGetValue(entry.Anchor.Segment,out var start)||!(entry.Anchor.Meters>=0&&entry.Anchor.Meters<=start.Length)){
                if(report!=null)report.StartUnavailable=true;
                return result;
            }
            var path=new List<DirectedRouteSection>();
            var switches=new List<RouteSwitch>();
            var used=new HashSet<string>(StringComparer.Ordinal);
            int work=0;
            void Visit(PlanEdge edge,double from,bool reverse,double distance) {
                cancellation.ThrowIfCancellationRequested();
                if(allowedDirections!=null&&!allowedDirections.Contains(RouteTargetScope.Key(edge.Id,reverse)))return;
                if(++work>workLimit||distance>MaximumDistance||path.Count>=128||!used.Add(edge.Id))return;
                double limit=reverse?0:edge.Length;
                MarkerRecord[] next=Array.Empty<MarkerRecord>();
                if(signals.TryGetValue(edge.Id,out var group)) {
                    var forward=group.Where(s=>s.Id!=entry.Id&&s.FacingReverse==reverse&&
                        (AuxiliaryTarget.IsEndpoint(s)?destinations.Contains(s.Id)&&AuxiliaryTarget.Accepts(s,shunt):probeThroughSignals?destinations.Contains(s.Id):shunt||s.Kind==MarkerRecord.Main)&&s.Anchor.Meters>=0&&s.Anchor.Meters<=edge.Length&&
                        (reverse?from-s.Anchor.Meters:s.Anchor.Meters-from)>(edge==start?.001:-.0001))
                        .OrderBy(s=>reverse?-s.Anchor.Meters:s.Anchor.Meters).ThenBy(s=>s.Id,StringComparer.Ordinal).ToArray();
                    if(forward.Length>0){limit=forward[0].Anchor.Meters;next=forward.TakeWhile(s=>Math.Abs(s.Anchor.Meters-limit)<.001).ToArray();}
                }
                double length=Math.Abs(limit-from);
                bool added=length>.001;
                if(added)path.Add(new DirectedRouteSection{Section=new TrackSection{Segment=edge.Id,From=Math.Min(from,limit),To=Math.Max(from,limit)},Reverse=reverse});
                if(distance+length<=MaximumDistance) {
                    if(next.Length>0) {
                        if(next.Length>1&&report!=null)
                            report.Ambiguous.UnionWith(next.Where(s=>destinations.Contains(s.Id)).Select(s=>s.Id));
                        if(next.Length==1&&distance+length>=1&&destinations.Contains(next[0].Id))
                            result.Add(new RouteProposal{Entry=entry.Id,Exit=next[0].Id,Kind=kind,Path=path.ToList(),Switches=switches.ToList()});
                    } else {
                        string nodeId=reverse?edge.A:edge.B;
                        if(nodes.TryGetValue(nodeId,out var node)&&connections.TryGetValue(nodeId,out var joins)) {
                            if(!node.Point&&joins.Length==2) {
                                var joined=joins.FirstOrDefault(e=>e.Id!=edge.Id);
                                if(joined!=null)Visit(joined,joined.A==nodeId?0:joined.Length,joined.A!=nodeId,distance+length);
                            } else if(node.Point&&joins.Length==3&&joins.Any(e=>e.Id==node.Common)&&joins.Any(e=>e.Id==node.Normal)) {
                                foreach(var joined in joins.Where(e=>e.Id!=edge.Id&&(edge.Id==node.Common||e.Id==node.Common))) {
                                    if(switches.Any(s=>s.Node==nodeId))continue;
                                    bool thrown=(edge.Id==node.Common?joined.Id:edge.Id)!=node.Normal;
                                    switches.Add(new RouteSwitch{Node=nodeId,Thrown=thrown,ReleaseAt=distance+length});
                                    Visit(joined,joined.A==nodeId?0:joined.Length,joined.A!=nodeId,distance+length);
                                    switches.RemoveAt(switches.Count-1);
                                }
                            }
                        }
                    }
                }
                if(added)path.RemoveAt(path.Count-1);
                used.Remove(edge.Id);
            }
            Visit(start,entry.Anchor.Meters,entry.FacingReverse,0);
            if(work>workLimit) {
                if(report!=null)report.LimitReached=true;
                return new List<RouteProposal>();
            }
            if(report!=null)report.Ambiguous.UnionWith(result.GroupBy(p=>p.Exit).Where(g=>g.Count()>1).Select(g=>g.Key));
            if(includeAlternatives)return result.OrderBy(p=>p.Length).ThenBy(p=>p.Exit,StringComparer.Ordinal).ToList();
            return result.GroupBy(p=>p.Exit,StringComparer.Ordinal).Where(g=>g.Count()==1)
                .Select(g=>g.Single()).OrderBy(p=>p.Length).ThenBy(p=>p.Exit,StringComparer.Ordinal).ToList();
        }
    }
}
