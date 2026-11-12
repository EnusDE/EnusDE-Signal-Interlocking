using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class FlankSignal {public string Id,Anchor;}
    public sealed class RouteFlank {
        public string Point;
        public bool MonitorOnly;
        public List<TrackSection> Sections=new List<TrackSection>();
        public List<FlankSignal> Signals=new List<FlankSignal>();
    }
    public interface IFlankOccupancyWorld {TrackOccupancy FlankOccupancy(TrackSection section);}
    public static class FlankProtection {
        public static void RequireCalculated(RouteDefinition route){if(route.Shunting){if(route.Flanks?.Count>0||route.Switches.Any(p=>p.FlankOf!=null))throw new InvalidOperationException("Alte Rangierfahrstraße: Fahrweg einmal aktualisieren, um frühere Flankenbereiche zu entfernen.");return;}if(route.Flanks==null&&route.Switches.Count>0)throw new InvalidOperationException("Fahrweg/Flankenschutz für "+route.Code+" einmal aktualisieren. Alte Definition besitzt noch keinen berechneten Flankenschutz.");}
        public static void Validate(RouteDefinition route){if(route.Shunting||route.Flanks==null)return;if(route.Flanks.Count>64)throw new InvalidDataException("Zu viele Flankenschutzgruppen.");if(route.Switches.Any(p=>p.FlankOf==null&&!route.Flanks.Any(f=>f!=null&&f.Point==p.Node)))throw new InvalidDataException("Weiche ohne berechnete Flankenschutzgruppe.");foreach(var f in route.Flanks){if(f==null||!route.Switches.Any(p=>p.Node==f.Point&&p.FlankOf==null)||f.Sections==null||f.Sections.Count>64||f.Signals==null||f.Signals.Count>32)throw new InvalidDataException("Flankenschutzgruppe ungültig.");foreach(var s in f.Sections){s.Validate();if(!route.Sections.Any(p=>p.Key==s.Key))throw new InvalidDataException("Flankenbereich bei Weiche "+f.Point+": Abschnitt "+s.Key+" ist intern keiner Festlegung zugeordnet. Fahrweg neu berechnen.");}foreach(var signal in f.Signals)if(signal==null||string.IsNullOrWhiteSpace(signal.Id)||string.IsNullOrWhiteSpace(signal.Anchor)||signal.Id==route.Entry||signal.Id==route.Exit||route.ViaSignals?.Any(s=>s.Id==signal.Id)==true)throw new InvalidDataException("Flankensignal kollidiert mit Fahrtsignal.");}}
        public static void Plan(RouteDefinition route,NetworkInput graph,IEnumerable<MarkerRecord> signals) {
            if(route.Shunting){route.Flanks=new List<RouteFlank>();return;}var nodes=graph.Nodes.ToDictionary(n=>n.Id);var edges=graph.Edges.ToDictionary(e=>e.Id);var adjacency=graph.Nodes.ToDictionary(n=>n.Id,n=>graph.Edges.Where(e=>e.A==n.Id||e.B==n.Id).ToArray());var pathIds=new HashSet<string>(route.Path.Select(p=>p.Section.Segment));var main=route.Switches.ToArray();route.Flanks=new List<RouteFlank>();var controlled=new HashSet<string>(new[]{route.Entry,route.Exit}.Concat(route.ViaSignals.Select(s=>s.Id)));
            foreach(var point in main){PlanNode node;if(!nodes.TryGetValue(point.Node,out node)||!node.Point)throw new InvalidOperationException("Flankenweiche nicht im geladenen Netz.");var unused=adjacency[node.Id].Where(e=>e.Id!=node.Common&&e.Id!=(point.Thrown?adjacency[node.Id].First(e=>e.Id!=node.Common&&e.Id!=node.Normal).Id:node.Normal)).SingleOrDefault();if(unused==null)throw new InvalidOperationException("Unbenutzter Weichenast nicht eindeutig.");
                var flank=new RouteFlank{Point=point.Node};var pending=new Queue<Tuple<string,string,double>>();pending.Enqueue(Tuple.Create(node.Id,unused.Id,150d));var seen=new HashSet<string>();int work=0;
                while(pending.Count>0){if(++work>128)throw new InvalidOperationException("Flankenbereich zu verzweigt; Schutz explizit aufteilen.");var request=pending.Dequeue();string from=request.Item1;var edge=edges[request.Item2];if(!seen.Add(from+"|"+edge.Id)){flank.MonitorOnly=true;continue;}if(pathIds.Contains(edge.Id)){flank.MonitorOnly=true;continue;}bool a=edge.A==from;double take=Math.Min(edge.Length,request.Item3);var own=signals.Where(s=>s.IsSignal&&s.Kind!=MarkerRecord.Distant&&!s.Planned&&!controlled.Contains(s.Id)&&s.Anchor.Segment==edge.Id&&s.FacingReverse==a).Select(s=>new{s,at=a?s.Anchor.Meters:edge.Length-s.Anchor.Meters}).Where(x=>x.at>.1&&x.at<=take).OrderBy(x=>x.at).FirstOrDefault();if(own!=null)take=own.at;
                    if(take>.001){var section=new TrackSection{Segment=edge.Id,From=a?0:edge.Length-take,To=a?take:edge.Length};flank.Sections.Add(section);route.Sections.Add(section);point.Protection.Add(section);}
                    if(own!=null){flank.Signals.Add(new FlankSignal{Id=own.s.Id,Anchor=RouteSignals.Anchor(own.s)});continue;}
                    if(take>=request.Item3-.001){flank.MonitorOnly=true;continue;}string far=a?edge.B:edge.A;var next=nodes[far];if(next.Point&&edge.Id!=next.Common){bool away=edge.Id==next.Normal;var existing=route.Switches.FirstOrDefault(p=>p.Node==far);if(existing!=null){if(existing.Thrown!=away)throw new InvalidOperationException("Flankenschutz verlangt widersprechende Weichenlage: "+far);continue;}var guard=new RouteSwitch{Node=far,Thrown=away,FlankOf=point.Node,ReleaseAt=point.ReleaseAt,Protection=new List<TrackSection>()};foreach(var leg in adjacency[far]){var section=new TrackSection{Segment=leg.Id,From=leg.A==far?0:Math.Max(0,leg.Length-25),To=leg.A==far?Math.Min(25,leg.Length):leg.Length};guard.Protection.Add(section);route.Sections.Add(section);flank.Sections.Add(section);}route.Switches.Add(guard);continue;}
                    var onward=adjacency[far].Where(e=>e.Id!=edge.Id).ToArray();if(onward.Length==0){flank.MonitorOnly=true;continue;}foreach(var e in onward)pending.Enqueue(Tuple.Create(far,e.Id,request.Item3-take));
                }
                flank.Sections=flank.Sections.GroupBy(s=>s.Key).Select(g=>g.First()).ToList();flank.Signals=flank.Signals.GroupBy(s=>s.Id).Select(g=>g.First()).ToList();route.Flanks.Add(flank);
            }
            route.Sections=route.Sections.GroupBy(s=>s.Key).Select(g=>g.First()).ToList();
        }
    }
    public sealed partial class InterlockingEngine {
        static IEnumerable<FlankSignal> HeldFlankSignals(RouteDefinition route,HashSet<string> released=null)=>(route.Shunting?new List<RouteFlank>():route.Flanks??new List<RouteFlank>()).Where(f=>released==null||!released.Contains(f.Point)).SelectMany(f=>f.Signals);
        public bool FlankHoldsSignal(string id)=>active.Values.Any(r=>HeldFlankSignals(r.Route,r.ReleasedSwitches).Any(s=>s.Id==id));
        static void CheckFlanks(RouteRun run,IInterlockingWorld world){if(run.Route.Shunting||run.Route.Flanks==null)return;foreach(var f in run.Route.Flanks.Where(f=>!run.ReleasedSwitches.Contains(f.Point)))foreach(var s in f.Sections){var state=world is IFlankOccupancyWorld reader?reader.FlankOccupancy(s):world.Occupancy(s);if(state!=TrackOccupancy.Clear)throw new InvalidOperationException("Flankenschutz "+f.Point+" / "+s.Key+": "+(state==TrackOccupancy.Occupied?"seitlicher Bereich belegt":"Rückmeldung unbekannt"));}}
    }
}
