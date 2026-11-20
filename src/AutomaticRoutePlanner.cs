using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace EnusDE.Signaltechnik {
    public static class AutomaticRoutePlanner {
        public static List<List<RouteProposal>> Find(NetworkInput graph,IEnumerable<MarkerRecord> signals,MarkerRecord start,MarkerRecord target,RouteKind kind,CancellationToken cancellation=default) {
            cancellation.ThrowIfCancellationRequested();
            if(start==null||target==null||start.Id==target.Id||AuxiliaryTarget.IsEndpoint(start)||
                !SignalCatalogue.Supports(start,kind==RouteKind.Shunt,kind==RouteKind.Slow)||!AuxiliaryTarget.Accepts(target,kind==RouteKind.Shunt))
                throw new InvalidOperationException("Startsignal oder Ziel unterstützt die gewählte Fahrart nicht.");
            var markers=signals.Where(s=>s!=null&&s.IsSignal&&s.Kind!=MarkerRecord.Distant).GroupBy(s=>s.Id).Select(g=>g.First()).ToDictionary(s=>s.Id);
            var ends=new HashSet<string>(markers.Keys){target.Id};
            var scanner=new RouteDiscovery(graph,markers.Values,AuxiliaryTarget.IsEndpoint(target)?target:null);
            var allowed=RouteTargetScope.Build(graph,target);
            var plans=new List<List<RouteProposal>>();int work=0;
            void Visit(MarkerRecord at,RouteKind movement,List<RouteProposal> path,HashSet<string> seen) {
                cancellation.ThrowIfCancellationRequested();
                if(++work>512||plans.Count>=32)throw new InvalidOperationException("Fahrwegsuche zu umfangreich. Näheres Ziel wählen.");
                if(path.Count>=16)return;
                var report=new RouteSearchReport();
                var options=scanner.Find(at,movement,ends,report:report,includeAlternatives:true,allowedDirections:allowed,cancellation:cancellation);
                if(report.LimitReached)throw new InvalidOperationException("Fahrwegsuche begrenzt. Näheres Ziel wählen.");
                foreach(var option in options) {
                    if(seen.Contains(option.Exit))continue;
                    var next=path.Concat(new[]{option}).ToList();
                    if(option.Exit==target.Id){if(plans.Count>=32)throw new InvalidOperationException("Mehr als 32 Fahrwege. Näheres Ziel wählen.");plans.Add(next);}
                    else if(markers.TryGetValue(option.Exit,out var following)) {
                        var nextKind=kind==RouteKind.Shunt?RouteKind.Shunt:RouteMovement.Default(following);
                        if(SignalCatalogue.Supports(following,nextKind==RouteKind.Shunt,nextKind==RouteKind.Slow))
                            Visit(following,nextKind,next,new HashSet<string>(seen){option.Exit});
                    }
                }
            }
            Visit(start,kind,new List<RouteProposal>(),new HashSet<string>{start.Id});return plans;
        }
        public static List<RouteChainPlan> PreferCurrent(List<RouteChainPlan> plans,Func<string,bool?> position) {
            if(plans.Count<=1)return plans;
            var matching=plans.Where(p=>p.Routes.SelectMany(r=>r.Switches).All(s=>position(s.Node)==s.Thrown)).ToList();
            return matching.Count==1?matching:plans;
        }
    }
}
