using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly List<RouteChainPlan> automaticChoices=new List<RouteChainPlan>();
        string automaticWorld,automaticDestination;
        void ClearAutomaticRouteChoices(){automaticChoices.Clear();automaticWorld=automaticDestination=null;CloseRouteChoiceWindow();}
        List<RouteChainPlan> DiscoverAutomaticPlans(string startId,string targetId)=>DiscoverAutomaticPlans(startId,targetId,shunting?RouteKind.Shunt:reduced?RouteKind.Slow:RouteKind.Train);
        List<RouteChainPlan> DiscoverAutomaticPlans(string startId,string targetId,RouteKind kind) {
            var source=Object(startId);var target=RouteEndpoint(targetId);
            var paths=AutomaticRoutePlanner.Find(world.RoutePlanningGraph(),OperationalSignalObjects,source,target,kind);
            var destinations=new HashSet<string>(OperationalSignalObjects.Where(s=>s.IsSignal).Select(s=>s.Id)){targetId};
            var design=world.PrepareProposalDesign(destinations);
            var codes=routes.Select(r=>r.Code).ToList();var plans=new List<RouteChainPlan>();
            InvalidOperationException unavailable=null;
            foreach(var path in paths) {
                var variants=new List<List<RouteDefinition>>{new List<RouteDefinition>()};
                foreach(var proposal in path) {
                    var known=OnDemandRoutes?Array.Empty<RouteDefinition>():RouteIdentity.Unique(routes.Where(r=>RouteIdentity.Matches(r,proposal)&&world.PathValid(r)));
                    if(known.Length==0) {
                        string code=NextRouteCode(proposal.Entry,proposal.Kind,codes,proposal.Exit);codes.Add(code);
                        try{var calculated=design(proposal,code);known=new[]{OnDemandRoutes?RouteRequestPolicy.ForRequest(calculated):calculated};}
                        catch(InvalidOperationException error){unavailable=error;variants.Clear();break;}
                    }
                    var next=new List<List<RouteDefinition>>();
                    foreach(var prefix in variants)foreach(var definition in known) {
                        if(next.Count+plans.Count>=32)throw new InvalidOperationException("Zu viele unterschiedliche Fahrwege. Näheres Ziel wählen.");
                        next.Add(prefix.Concat(new[]{definition}).ToList());
                    }
                    variants=next;
                }
                plans.AddRange(variants.Select(v=>new RouteChainPlan{Routes=v,EndReason="Fahrweg aus Start und Ziel automatisch ermittelt."}));
            }
            if(plans.Count==0&&unavailable!=null)throw new InvalidOperationException("Kein vollständig prüfbarer Fahrweg: "+unavailable.Message,unavailable);
            return plans;
        }
        void SelectAutomaticPlans(List<RouteChainPlan> plans,string destination,string start=null) {
            RequireDispatcher();
            if(plans.Count==0)throw new InvalidOperationException(SignalCaption(start??panelButtons.Start)+" → "+SignalCaption(destination)+": Kein zulässiger Fahrweg für diese Fahrart. Gleisposition, Anfahrtrichtung und Signalbegriffe am Start und an Zwischensignalen prüfen.");
            plans=AutomaticRoutePlanner.PreferCurrent(plans,id=>world.SwitchPosition(id,out bool thrown)?(bool?)thrown:null);
            if(plans.Count==1){CommitAutomaticPlan(plans[0]);return;}
            ClearAutomaticRouteChoices();automaticWorld=scope;automaticDestination=destination;automaticChoices.AddRange(plans.OrderBy(RouteChoiceLabels.Length));
            status=LocalText.Get("Mehrere unterschiedliche Fahrwege. Gewünschten Weg in der Auswahl wählen.");
            CloseWorkspaceContext();OpenRouteChoiceWindow();
        }
        void CommitAutomaticPlan(RouteChainPlan plan) {
            RequireDispatcher();
            AdmitAutomaticPlan(plan,deskId);
            ClearAutomaticRouteChoices();
        }
        void AdmitAutomaticPlan(RouteChainPlan plan,string operatingDesk) {
            if(!aiEnabled)throw new InvalidOperationException("AE-Anbindung unter Auswahlfenster → Einstellungen einschalten.");
            if(plan.Routes.Count>1&&plan.Routes[0].LineTarget!=null) {
                var binding=RouteLineTarget(plan.Routes[0]);
                LineTargetBindings.RequirePlan(desks,plan.Routes);
                world.BeginRead();
                plan=LineTargetBindings.ClearPrefix(plan,binding,route=>{
                    string reason=RouteCircuitError(new[]{route},out var state);
                    return (state,reason);
                });
            }
            foreach(var route in plan.Routes)RequireOperatorRoute(route);
            PrepareRequestedPlan(plan);
            AdmitRoutePlan(plan,operatingDesk);
        }
        bool BuildAutomaticRouteChoices(UIPanelBuilder c) {
            if(automaticChoices.Count==0)return false;
            if(automaticWorld!=scope){ClearAutomaticRouteChoices();return false;}
            c.AddButton(LocalText.Select("Open route choice","Fahrwegauswahl öffnen"),OpenRouteChoiceWindow);
            return true;
        }
    }
}
