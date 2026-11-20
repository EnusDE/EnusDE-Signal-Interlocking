using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EnusDE.Signaltechnik {
    public static class RouteIdentity {
        public static string DefinitionKey(RouteDefinition route) {
            if(route.Path==null)return "legacy:"+route.Id;
            var json=JObject.FromObject(route);json.Remove("Id");json.Remove("Code");
            return json.ToString(Newtonsoft.Json.Formatting.None);
        }
        public static RouteDefinition[] Unique(IEnumerable<RouteDefinition> routes) {
            var result=new List<RouteDefinition>();
            var ordered=routes.OrderBy(r=>r.Code,StringComparer.Ordinal).ThenBy(r=>r.Id,StringComparer.Ordinal);
            foreach(var group in ordered.GroupBy(r=>r.Entry+"|"+r.Exit+"|"+r.Shunting+"|"+r.Reduced+"|"+PathKey(r.Path),StringComparer.Ordinal)) {
                if(group.Count()==1)result.Add(group.First());
                else result.AddRange(group.GroupBy(DefinitionKey,StringComparer.Ordinal).Select(g=>g.First()));
            }
            return result.ToArray();
        }
        public static string PathKey(IEnumerable<DirectedRouteSection> path) {
            if(path==null)return "";
            var merged=new List<DirectedRouteSection>();
            foreach(var piece in path) {
                var last=merged.LastOrDefault();
                if(last!=null&&last.Section.Segment==piece.Section.Segment&&last.Reverse==piece.Reverse&&
                    Math.Abs((last.Reverse?last.Section.From:last.Section.To)-(piece.Reverse?piece.Section.To:piece.Section.From))<.00001) {
                    last.Section.From=Math.Min(last.Section.From,piece.Section.From);last.Section.To=Math.Max(last.Section.To,piece.Section.To);
                }else merged.Add(piece.Clone());
            }
            return string.Join("|",merged.Select(p=>p.Section.Key+":"+p.Reverse));
        }
        public static bool Matches(RouteDefinition route,RouteProposal proposal)=>route.Entry==proposal.Entry&&route.Exit==proposal.Exit&&
            route.Shunting==(proposal.Kind==RouteKind.Shunt)&&route.Reduced==(proposal.Kind==RouteKind.Slow)&&route.Path!=null&&
            PathKey(route.Path)==PathKey(proposal.Path)&&
            route.Switches.OrderBy(s=>s.Node,StringComparer.Ordinal).Select(s=>s.Node+":"+s.Thrown)
                .SequenceEqual(proposal.Switches.OrderBy(s=>s.Node,StringComparer.Ordinal).Select(s=>s.Node+":"+s.Thrown));
    }
    public sealed class MissingRoutePlanException:InvalidOperationException {public MissingRoutePlanException(string message):base(message){}}
    public sealed class AmbiguousRoutePlanException:InvalidOperationException {public AmbiguousRoutePlanException(string message):base(message){}}
    public static class RouteMovement {
        public static RouteKind Default(MarkerRecord signal,bool shunting=false) {
            if(signal?.Kind==MarkerRecord.Shunt||shunting)return RouteKind.Shunt;
            return SignalCatalogue.Supports(signal,false,false)?RouteKind.Train:RouteKind.Slow;
        }
    }
}
