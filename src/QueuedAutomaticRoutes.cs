using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        sealed class AutomaticPreparation {
            public string Target,Key,Rule,Progress;
            public long Configuration,Installation;
            public bool Ready;
            public Exception Error;
            public List<RouteChainPlan> Plans=new List<RouteChainPlan>();
        }
        readonly Dictionary<string,AutomaticPreparation> preparedAutomaticRoutes=new Dictionary<string,AutomaticPreparation>();
        bool TryPreparedAutomaticRoutes(AutomaticRequest request,AutomationContact contact,AutomaticSignal rule,string target,out List<RouteChainPlan> plans) {
            plans=null;
            if(preparedAutomaticRoutes.TryGetValue(request.Id,out var old)) {
                if(old.Rule!=JsonConvert.SerializeObject(rule)||old.Target!=target||!old.Ready&&!routeWork.Contains(old.Key)||old.Ready&&(old.Configuration!=Entry.Shared.ConfigurationVersion||old.Installation!=Entry.Shared.InstallationVersion)) {
                    routeWork.Cancel(old.Key);preparedAutomaticRoutes.Remove(request.Id);
                } else {
                    if(!old.Ready){request.Reason=old.Progress;return false;}
                    preparedAutomaticRoutes.Remove(request.Id);
                    if(old.Error!=null)throw old.Error;
                    plans=old.Plans;return true;
                }
            }
            string selectedWorld=scope;
            var prepared=new AutomaticPreparation{Target=target,Key="contact/"+request.Id,Rule=JsonConvert.SerializeObject(rule),Progress=WindowText("Queued route preparation","Fahrstraßenvorbereitung eingereiht")};
            var snapshot=new AutomationContact{Anchor=contact.Anchor.Clone(),Reverse=contact.Reverse};
            IEnumerable<bool> Prepare() {
                foreach(bool step in PrepareRoutePlans(request.Signal,target,rule.Reduced?RouteKind.Slow:RouteKind.Train,prepared.Plans,phase=>{
                    prepared.Progress=phase;request.Reason=phase;routeWorkProgress=Code(request.Signal)+" → "+Code(target)+" · "+phase;
                },true,snapshot))yield return step;
                prepared.Configuration=Entry.Shared.ConfigurationVersion;prepared.Installation=Entry.Shared.InstallationVersion;prepared.Ready=true;
            }
            routeWork.Enqueue(prepared.Key,()=>Prepare().GetEnumerator(),()=>RouteWorkAvailable&&scope==selectedWorld&&request.Stage==AutomaticRequestStage.Waiting&&Automation.Enabled&&Automation.Signals.TryGetValue(request.Signal,out var current)&&current.Enabled&&JsonConvert.SerializeObject(current)==prepared.Rule,error=>{
                prepared.Error=error;prepared.Configuration=Entry.Shared.ConfigurationVersion;prepared.Installation=Entry.Shared.InstallationVersion;prepared.Ready=true;
            });
            preparedAutomaticRoutes[request.Id]=prepared;request.Reason=prepared.Progress;return false;
        }
    }
}
