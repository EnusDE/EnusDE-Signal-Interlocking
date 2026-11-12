using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class RouteChainPlan {
        public List<RouteDefinition> Routes=new List<RouteDefinition>();
        public string EndReason;
    }
    public static class RouteChains {
        public static RouteChainPlan Follow(IEnumerable<RouteDefinition> definitions,RouteDefinition first) {
            var all=definitions.ToArray();var plan=new RouteChainPlan();var seen=new HashSet<string>();var current=first;
            while(current!=null){if(plan.Routes.Count>=16){plan.EndReason="Grenze von 16 Teilfahrstraßen erreicht.";break;}if(!seen.Add(current.Entry)){plan.EndReason="Schleife: Fortsetzung muss der Fahrdienstleiter wählen.";break;}plan.Routes.Add(current);if(current.AuxiliaryExit){plan.EndReason="Hilfsziel erreicht.";break;}var choices=all.Where(r=>r.Entry==current.Exit&&r.Shunting==first.Shunting).ToArray();if(choices.Length!=1){plan.EndReason=choices.Length==0?"Keine weitere gespeicherte Fahrstraße ab dem Zielsignal.":"Mehrere Fortsetzungen: Fahrdienstleiter muss den Weg wählen.";break;}if(seen.Contains(choices[0].Entry)){plan.EndReason="Schleife: keine automatische Wiederholung.";break;}current=choices[0];}
            return plan;
        }
        public static RouteChainPlan ToTarget(IEnumerable<RouteDefinition> definitions,string start,string end,bool shunting,bool reduced,Func<string,string> signalName=null) {
            var all=definitions.ToArray();var results=new List<List<RouteDefinition>>();int work=0;
            Action<string,List<RouteDefinition>,HashSet<string>> visit=null;visit=(at,path,seen)=>{if(results.Count>1||++work>4096||path.Count>=16)return;foreach(var r in all.Where(r=>r.Entry==at&&r.Shunting==shunting&&(path.Count>0||r.Reduced==reduced))){if(seen.Contains(r.Exit))continue;var next=path.Concat(new[]{r}).ToList();if(r.Exit==end){results.Add(next);if(results.Count>1)return;}else if(!r.AuxiliaryExit){var visited=new HashSet<string>(seen){r.Exit};visit(r.Exit,next,visited);}}};visit(start,new List<RouteDefinition>(),new HashSet<string>{start});
            if(work>4096)throw new InvalidOperationException("Zu viele mögliche Wege. Eine Teilfahrstraße aus der Liste wählen.");
            if(results.Count>1)throw new InvalidOperationException("Mehrere Fahrstraßenfolgen möglich. Konkrete Teilfahrstraße auswählen.");
            if(results.Count==0) {
                var name=signalName??(id=>id);
                string kind=shunting?"Rangierfahrt":reduced?"Langsamfahrt":"Zugfahrt";
                string pair=name(start)+" → "+name(end);
                var direct=all.Where(r=>r.Entry==start&&r.Exit==end).Select(r=>r.Shunting?"Rangierfahrt":r.Reduced?"Langsamfahrt":"Zugfahrt").Distinct().ToArray();
                if(direct.Length>0)throw new InvalidOperationException(pair+": Für "+kind+" fehlt die Definition. Gespeichert als "+string.Join(" / ",direct)+".");
                if(!all.Any(r=>r.Entry==start&&r.Shunting==shunting&&r.Reduced==reduced))
                    throw new InvalidOperationException(pair+": Keine "+kind+" ab "+name(start)+" gespeichert.");
                if(!all.Any(r=>r.Exit==end&&r.Shunting==shunting))
                    throw new InvalidOperationException(pair+": Keine passende Teilfahrstraße endet an "+name(end)+".");
                throw new InvalidOperationException(pair+": Gespeicherte Teilfahrstraßen ergeben keinen durchgehenden Weg für "+kind+".");
            }
            return new RouteChainPlan{Routes=results[0],EndReason="Gewähltes Zielsignal erreicht."};
        }
    }
    public sealed partial class InterlockingEngine {
        void RefreshChains(){foreach(var group in active.Values.Where(r=>r.ChainId!=null).GroupBy(r=>r.ChainId)){bool ready=group.All(r=>r.Stage==RouteStage.Set||r.Stage==RouteStage.Running);foreach(var run in group)run.ChainReady=ready;}}
        public void RequestChain(IEnumerable<RouteDefinition> definitions,IInterlockingWorld world,double wait=15) {
            var list=definitions.ToArray();if(list.Length<1||list.Length>16||list.Select(r=>r.Id).Distinct().Count()!=list.Length||active.Count+list.Length>16)throw new InvalidOperationException("Fahrstraßenfolge ungültig oder zu umfangreich.");
            for(int i=1;i<list.Length;i++)if(list[i-1].Exit!=list[i].Entry||list[i].Shunting!=list[0].Shunting)throw new InvalidOperationException("Teilfahrstraßen folgen nicht durchgehend in derselben Fahrart.");
            var old=active.Values.Select(r=>new{run=r,r.Stage,r.Reason,r.ChainReady}).ToArray();string group=list.Length>1?Guid.NewGuid().ToString("N"):null;
            try{foreach(var definition in list){Request(definition,world,wait);var run=active[definition.Id];run.ChainId=group;run.ChainReady=group==null;}RefreshChains();}
            catch{foreach(var r in list)if(!old.Any(o=>o.run.Route.Id==r.Id))active.Remove(r.Id);foreach(var o in old){o.run.Stage=o.Stage;o.run.Reason=o.Reason;o.run.ChainReady=o.ChainReady;active[o.run.Route.Id]=o.run;}throw;}
        }
        public void CancelChain(string id,IInterlockingWorld world) {
            RouteRun selected;if(!active.TryGetValue(id,out selected))return;var members=selected.ChainId==null?new[]{selected}:active.Values.Where(r=>r.ChainId==selected.ChainId).ToArray();
            foreach(var run in members){run.Stage=RouteStage.Cancelled;run.ChainReady=false;run.Reason="Fahrstraßenfolge Halt; Verschlüsse bis zur Freiprüfung behalten.";}
            var failures=new List<string>();foreach(var run in members)try{Cancel(run.Route.Id,world);}catch(Exception e){failures.Add(run.Route.Code+": "+e.Message);}if(failures.Count>0)throw new InvalidOperationException(string.Join("\n",failures));
        }
    }
}
