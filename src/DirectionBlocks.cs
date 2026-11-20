using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class DirectionBlockLink {
        public string Id,A,B,Error;
        public List<DirectedRouteSection> Path=new List<DirectedRouteSection>();
    }
    public sealed class DirectionBlock {
        public string Id,Error;
        public List<DirectedRouteSection> Axis=new List<DirectedRouteSection>();
        public List<string> Members=new List<string>();
    }
    public static class DirectionBlockBuilder {
        static int End(DirectionBlockLink link,string endpoint)=>link.A==endpoint?-1:1;
        static int Relation(DirectionBlockLink a,DirectionBlockLink b,HashSet<string> stops) {
            foreach(var x in a.Path)foreach(var y in b.Path)if(x.Section.Overlaps(y.Section))return x.Reverse==y.Reverse?1:-1;
            foreach(string endpoint in new[]{a.A,a.B})if(!stops.Contains(endpoint)&&(b.A==endpoint||b.B==endpoint))return -End(a,endpoint)*End(b,endpoint);
            return 0;
        }
        public static List<DirectionBlockLink> Split(DirectionBlockLink link,IEnumerable<MarkerAnchor> boundaries) {
            var result=new List<DirectionBlockLink>();var current=new DirectionBlockLink{Id=link.Id+":0",A=link.A,Error=link.Error};
            foreach(var piece in link.Path) {
                double start=piece.Reverse?piece.Section.To:piece.Section.From,end=piece.Reverse?piece.Section.From:piece.Section.To;
                var cuts=boundaries.Where(a=>a.Segment==piece.Section.Segment&&a.Meters>piece.Section.From+.005&&a.Meters<piece.Section.To-.005)
                    .GroupBy(a=>Math.Round(a.Meters,2)).Select(g=>g.First()).OrderBy(a=>piece.Reverse?-a.Meters:a.Meters).ToArray();
                foreach(var cut in cuts) {
                    current.Path.Add(new DirectedRouteSection{Reverse=piece.Reverse,Section=new TrackSection{Segment=piece.Section.Segment,From=Math.Min(start,cut.Meters),To=Math.Max(start,cut.Meters)}});
                    current.B=cut.Segment+"@"+Math.Round(cut.Meters,2).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);result.Add(current);
                    current=new DirectionBlockLink{Id=link.Id+":"+result.Count,A=current.B,Error=link.Error};start=cut.Meters;
                }
                current.Path.Add(new DirectedRouteSection{Reverse=piece.Reverse,Section=new TrackSection{Segment=piece.Section.Segment,From=Math.Min(start,end),To=Math.Max(start,end)}});
            }
            current.B=link.B;result.Add(current);return result;
        }
        public static List<DirectionBlock> Build(IEnumerable<DirectionBlockLink> source,IEnumerable<string> stationBoundaries) {
            var links=source.OrderBy(l=>l.Id,StringComparer.Ordinal).ToArray();var stops=new HashSet<string>(stationBoundaries,StringComparer.Ordinal);
            var endpoints=links.SelectMany((link,i)=>new[]{(key:link.A,index:i),(key:link.B,index:i)}).ToLookup(p=>p.key,p=>p.index,StringComparer.Ordinal);
            var segments=links.SelectMany((link,i)=>link.Path.Select(p=>(key:p.Section.Segment,index:i))).ToLookup(p=>p.key,p=>p.index,StringComparer.Ordinal);
            var visited=new HashSet<int>();var result=new List<DirectionBlock>();
            for(int root=0;root<links.Length;root++) {
                if(visited.Contains(root))continue;var signs=new Dictionary<int,int>{{root,1}};var queue=new Queue<int>();queue.Enqueue(root);
                var block=new DirectionBlock();
                while(queue.Count>0) {
                    int i=queue.Dequeue();if(!visited.Add(i))continue;var link=links[i];block.Members.Add(link.Id);if(link.Error!=null)block.Error=link.Error;
                    foreach(var piece in link.Path)block.Axis.Add(new DirectedRouteSection{Section=piece.Section.Clone(),Reverse=piece.Reverse^(signs[i]<0)});
                    var neighbors=new[]{link.A,link.B}.Where(e=>!stops.Contains(e)).SelectMany(e=>endpoints[e]).Concat(link.Path.SelectMany(p=>segments[p.Section.Segment])).Distinct();
                    foreach(int j in neighbors)if(i!=j) {
                        int relation=Relation(link,links[j],stops);if(relation==0)continue;int sign=signs[i]*relation;
                        if(signs.TryGetValue(j,out int known)){if(known!=sign)block.Error="Widersprüchliche Blockrichtung / Schleife.";}
                        else{signs[j]=sign;queue.Enqueue(j);}
                    }
                }
                block.Axis=block.Axis.GroupBy(p=>p.Section.Key+":"+p.Reverse).Select(g=>g.First()).ToList();
                foreach(var segment in block.Axis.GroupBy(p=>p.Section.Segment)) {
                    double normal=-1,reverse=-1;
                    foreach(var piece in segment.OrderBy(p=>p.Section.From)) {
                        if(piece.Section.From<(piece.Reverse?normal:reverse)-.001)block.Error="Blockrichtung nicht eindeutig.";
                        if(piece.Reverse)reverse=Math.Max(reverse,piece.Section.To);else normal=Math.Max(normal,piece.Section.To);
                    }
                }
                block.Id="BL-"+SharedDocuments.Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("|",block.Axis.Select(p=>p.Section.Key).Distinct().OrderBy(s=>s,StringComparer.Ordinal)))).Substring(0,10).ToUpperInvariant();
                result.Add(block);
            }
            return result;
        }
        public static int Direction(IEnumerable<DirectedRouteSection> path,IEnumerable<DirectedRouteSection> axis) {
            int found=0;foreach(var piece in path)foreach(var section in axis)if(piece.Section.Overlaps(section.Section)) {
                int direction=piece.Reverse==section.Reverse?1:-1;if(found!=0&&found!=direction)throw new InvalidOperationException("Fahrweg wechselt innerhalb einer Blockkette die Richtung.");found=direction;
            }return found;
        }
        public static bool Covers(IEnumerable<DirectedRouteSection> path,IEnumerable<DirectionBlock> blocks) {
            var all=blocks.SelectMany(b=>b.Axis).Select(p=>p.Section).ToArray();
            foreach(var piece in path) {
                double at=piece.Section.From;
                foreach(var span in all.Where(s=>s.Segment==piece.Section.Segment&&s.To>at).OrderBy(s=>s.From)) {
                    if(span.From>at+.05)break;at=Math.Max(at,span.To);if(at>=piece.Section.To-.05)break;
                }
                if(at<piece.Section.To-.05)return false;
            }return true;
        }
    }
    public interface IRouteAdmissionTicket:IDisposable {void Commit();}
    public interface IRouteAdmission {IRouteAdmissionTicket Reserve(IReadOnlyList<RouteDefinition> routes);}
    public sealed class DirectionBlockLease {
        public string Id,Reason;
        public int Direction,ClearChecks;
        public double LastRead=double.NegativeInfinity;
        public List<DirectedRouteSection> Axis=new List<DirectedRouteSection>();
        public HashSet<string> Routes=new HashSet<string>(StringComparer.Ordinal);
    }
    public sealed class DirectionBlockAuthority : IRouteAdmission {
        sealed class Ticket:IRouteAdmissionTicket {
            readonly DirectionBlockAuthority owner;readonly List<DirectionBlockLease> before;
            bool committed;
            public Ticket(DirectionBlockAuthority owner,List<DirectionBlockLease> before){this.owner=owner;this.before=before;}
            public void Commit(){committed=true;}
            public void Dispose(){if(!committed){owner.leases.Clear();owner.leases.AddRange(before);}}
        }
        readonly List<DirectionBlockLease> leases=new List<DirectionBlockLease>();
        public IReadOnlyList<DirectionBlockLease> Leases=>leases;
        public List<DirectionBlock> Blocks=new List<DirectionBlock>();
        public bool Enabled;
        public Func<TrackSection,TrackOccupancy> Occupancy;
        public Func<IReadOnlyList<RouteDefinition>,string> CheckStations;
        public Func<RouteDefinition,bool> ProtectRoute;
        TrackOccupancy Read(TrackSection section){try{return Occupancy?.Invoke(section)??TrackOccupancy.Unknown;}catch{return TrackOccupancy.Unknown;}}
        static DirectionBlockLease Clone(DirectionBlockLease l)=>new DirectionBlockLease{Id=l.Id,Direction=l.Direction,Reason=l.Reason,ClearChecks=l.ClearChecks,LastRead=l.LastRead,
            Axis=l.Axis.Select(p=>new DirectedRouteSection{Section=p.Section.Clone(),Reverse=p.Reverse}).ToList(),Routes=new HashSet<string>(l.Routes,StringComparer.Ordinal)};
        public IRouteAdmissionTicket Reserve(IReadOnlyList<RouteDefinition> routes) {
            var ticket=new Ticket(this,leases.Select(Clone).ToList());
            try {
                string station=CheckStations?.Invoke(routes);if(station!=null)throw new InvalidOperationException(station);
                if(!Enabled)return ticket;
                var protectedRoutes=routes.Where(r=>ProtectRoute?.Invoke(r)!=false).ToArray();
                if(protectedRoutes.Any(r=>r.Path==null||r.Path.Count==0))throw new InvalidOperationException("Blockabstimmung benötigt einen gerichteten Fahrweg. Fahrstraße aktualisieren.");
                var path=protectedRoutes.SelectMany(r=>r.Path).ToArray();
                foreach(var block in Blocks) {
                    int direction=DirectionBlockBuilder.Direction(path,block.Axis);if(direction==0)continue;
                    if(block.Error!=null)throw new InvalidOperationException(block.Id+": "+block.Error);
                    var overlaps=leases.Where(l=>l.Axis.Any(a=>block.Axis.Any(b=>a.Section.Overlaps(b.Section)))).ToArray();
                    foreach(var held in overlaps) {
                        int relative=DirectionBlockBuilder.Direction(block.Axis,held.Axis);
                        if(relative==0||relative*direction!=held.Direction)throw new InvalidOperationException(block.Id+": Gegenrichtung durch "+held.Id+" gebunden ("+string.Join(", ",held.Routes)+").");
                    }
                    if(overlaps.Length==0&&block.Axis.Any(p=>Read(p.Section)!=TrackOccupancy.Clear))
                        throw new InvalidOperationException(block.Id+": Blockkette belegt oder nicht vollständig lesbar; keine neue Richtung freigegeben.");
                    var lease=leases.FirstOrDefault(l=>l.Id==block.Id&&l.Direction==direction);
                    if(lease==null){lease=new DirectionBlockLease{Id=block.Id,Direction=direction,Axis=block.Axis.Select(p=>new DirectedRouteSection{Section=p.Section.Clone(),Reverse=p.Reverse}).ToList()};leases.Add(lease);}
                    lease.Routes.UnionWith(routes.Select(r=>r.Id));lease.ClearChecks=0;lease.Reason=LocalText.Get("Richtung gebunden; Fahrstraßen und Zugschluss abwarten.");
                }
                return ticket;
            } catch{ticket.Dispose();throw;}
        }
        public void Observe(IEnumerable<RouteRun> active,double now) {
            var ids=new HashSet<string>(active.Select(r=>r.Route.Id),StringComparer.Ordinal);
            foreach(var lease in leases.ToArray()) {
                if(now<=lease.LastRead)continue;lease.LastRead=now;
                bool hasRoutes=lease.Routes.Any(ids.Contains);bool clear=lease.Axis.All(p=>Read(p.Section)==TrackOccupancy.Clear);
                lease.ClearChecks=!hasRoutes&&clear?lease.ClearChecks+1:0;
                lease.Reason=hasRoutes?"Fahrstraße / Restverschluss hält die Richtung.":clear?"Vollständige Freiprüfung "+lease.ClearChecks+"/3":"Zugschluss / Belegung noch nicht vollständig frei.";
                if(lease.ClearChecks>=3)leases.Remove(lease);
            }
        }
    }
}
