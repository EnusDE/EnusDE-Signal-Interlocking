using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        sealed class WaypointPreparation {
            public string Key,Intent,Source,Destination,Target;
            public RouteKind Kind;
            public long Configuration,Installation;
            public bool Ready;
            public Exception Error;
            public readonly List<RouteChainPlan> Plans=new List<RouteChainPlan>();
        }

        readonly Dictionary<string,WaypointPreparation> waypointPreparations=new Dictionary<string,WaypointPreparation>();

        MarkerRecord WaypointDestination(AutomaticRequest request,MovementRole role,WaypointApproach approach,out MarkerRecord source,out RouteKind kind) {
            source=Object(request.Signal);
            bool shunting=role==MovementRole.Shunt;
            kind=RouteMovement.Default(source,shunting);
            if(source==null||role==MovementRole.Unassigned||OperatingState(request.Signal)!=SignalOperatingState.Active||!SignalCatalogue.Supports(source,shunting,kind==RouteKind.Slow))
                throw new InvalidOperationException(LocalText.Select("The next signal cannot authorize this movement.","Das nächste Signal kann diese Fahrart nicht freigeben."));
            var station=SignalStation(request.Signal);
            if(station==null)throw new InvalidOperationException(LocalText.Select("Assign the start signal to a local interlocking first.","Zuerst das Startsignal einem örtlichen Stellwerk zuordnen."));
            var auxiliary=station.Panel.Tiles.Where(t=>!t.ReadOnlySignal&&t.Target?.Function==AuxiliaryFunction.RouteDestination)
                .Select(t=>t.Target.Id).Distinct().Where(id=>BoundLineTarget(id)==null).Select(AuxiliaryEndpoint).Where(s=>s!=null);
            var target=WaypointRoutePolicy.Destination(approach,source,SignalObjects,auxiliary,shunting,s=>!IgnoreOwnSignal(s.Id));
            if(target==null)throw new InvalidOperationException(LocalText.Select("No reachable signal or auxiliary destination for this waypoint leg.","Kein erreichbares Signal oder Hilfsziel für diesen Wegpunktabschnitt."));
            if(SignalStation(target.Id)?.Id!=station.Id)throw new InvalidOperationException(LocalText.Select("This waypoint leg ends outside the assigned local interlocking.","Dieser Wegpunktabschnitt endet außerhalb des zuständigen örtlichen Stellwerks."));
            if(!AuxiliaryTarget.IsEndpoint(target)&&OperatingState(target.Id)!=SignalOperatingState.Active)
                throw new InvalidOperationException(LocalText.Select("The next destination signal is unavailable.","Das nächste Zielsignal ist nicht verfügbar."));
            return target;
        }

        bool TryPreparedWaypointRoutes(AutomaticRequest request,AutomationIntent intent,MovementRole role,WaypointApproach approach,out List<RouteChainPlan> plans) {
            plans=null;
            MarkerRecord source,target;
            RouteKind kind;
            try{target=WaypointDestination(request,role,approach,out source,out kind);}
            catch{CancelWaypointPreparation(request.Id);throw;}
            string sourceAnchor=SignalWorldBridge.AnchorKey(source),destinationAnchor=SignalWorldBridge.AnchorKey(target);
            var shared=Entry.Shared;
            if(waypointPreparations.TryGetValue(request.Id,out var previous)) {
                if(previous.Intent==intent.Signature&&previous.Kind==kind&&previous.Target==target.Id&&previous.Source==sourceAnchor&&previous.Destination==destinationAnchor&&
                    previous.Configuration==shared.ConfigurationVersion&&previous.Installation==shared.InstallationVersion&&(previous.Ready||routeWork.Contains(previous.Key))) {
                    if(!previous.Ready)return false;
                    if(previous.Error!=null) {
                        waypointPreparations.Remove(request.Id);
                        throw previous.Error;
                    }
                    plans=previous.Plans.Where(plan=>WaypointRoutePolicy.Matches(approach,source,target,kind,plan)).ToList();
                    if(plans.Count==0)throw new InvalidOperationException(LocalText.Select("No route matches the directed waypoint path.","Keine Fahrstraße passt zum gerichteten Wegpunktfahrweg."));
                    return true;
                }
                CancelWaypointPreparation(request.Id);
            }
            request.Target=target.Id;
            var prepared=new WaypointPreparation {
                Key="waypoint/"+request.Id,Intent=intent.Signature,Kind=kind,Target=target.Id,Source=sourceAnchor,Destination=destinationAnchor,
                Configuration=shared.ConfigurationVersion,Installation=shared.InstallationVersion
            };
            string selectedWorld=scope;
            IEnumerable<bool> Prepare() {
                foreach(bool step in PrepareRoutePlans(request.Signal,target.Id,kind,prepared.Plans,phase=>{
                    request.Reason=phase;
                    routeWorkProgress=Code(request.Signal)+" → "+Code(target.Id)+" · "+phase;
                },true))yield return step;
                prepared.Ready=true;
            }
            bool IsCurrent()=>RouteWorkAvailable&&scope==selectedWorld&&request.Stage==AutomaticRequestStage.Waiting&&
                Entry.Shared.ConfigurationVersion==prepared.Configuration&&Entry.Shared.InstallationVersion==prepared.Installation;
            if(!routeWork.Enqueue(prepared.Key,()=>Prepare().GetEnumerator(),IsCurrent,error=>{prepared.Error=error;prepared.Ready=true;}))
                throw new InvalidOperationException(LocalText.Select("Waypoint route is already being prepared.","Die Wegpunktfahrstraße wird bereits vorbereitet."));
            waypointPreparations.Add(request.Id,prepared);
            request.Reason=LocalText.Select("Preparing waypoint route.","Wegpunktfahrstraße wird vorbereitet.");
            return false;
        }

        void RequireWaypointPlan(AutomaticRequest request,MovementRole role,WaypointApproach approach,RouteChainPlan plan) {
            var target=WaypointDestination(request,role,approach,out var source,out var kind);
            if(!WaypointRoutePolicy.Matches(approach,source,target,kind,plan))
                throw new InvalidOperationException(LocalText.Select("The route no longer matches the current waypoint path.","Die Fahrstraße passt nicht mehr zum aktuellen Wegpunktfahrweg."));
            if(role==MovementRole.Shunt&&NeedsDirectionCoordination(plan.Routes[0]))
                throw new InvalidOperationException(LocalText.Select("A waypoint shunt must stay within its local interlocking and station boundaries.","Eine Wegpunkt-Rangierfahrt muss innerhalb ihres örtlichen Stellwerks und der Bahnhofsgrenzen bleiben."));
        }

        void CancelWaypointPreparation(string id) {
            if(!waypointPreparations.TryGetValue(id,out var preparation))return;
            routeWork.Cancel(preparation.Key);
            waypointPreparations.Remove(id);
        }

        void ClearWaypointPreparations() {
            foreach(string id in waypointPreparations.Keys.ToArray())CancelWaypointPreparation(id);
        }
    }
}
