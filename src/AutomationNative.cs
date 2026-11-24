using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Model;
using Model.AI;
using Track;
using Track.Search;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed class AutomationTrain {
        public string Master,Consist;
        public Car[] Cars;
    }
    public static class AutomationNative {
        static readonly FieldInfo wheels=typeof(RollingStock.Wheelset).GetField("wheels",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        static readonly FieldInfo orders=typeof(AutoEngineerPlanner).GetField("_orders",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly FieldInfo route=typeof(AutoEngineerPlanner).GetField("_route",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly FieldInfo routeTarget=typeof(AutoEngineerPlanner).GetField("_routeTargetLocation",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly FieldInfo startIndex=typeof(AutoEngineerPlanner).GetField("_startStepIndex",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly FieldInfo plannedWaypoint=typeof(AutoEngineerPlanner).GetField("_lastRouteWaypoint",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly MethodInfo routeStart=typeof(AutoEngineerPlanner).GetMethod("RouteStartLocation",BindingFlags.Static|BindingFlags.NonPublic,
            null,new[]{typeof(BaseLocomotive),typeof(bool),typeof(ConsistEnd).MakeByRefType()},null);
        public static Car[] Consist(Car selected) {
            if(selected==null)return new Car[0];var queue=new Queue<Car>();var result=new Dictionary<string,Car>(StringComparer.Ordinal);queue.Enqueue(selected);
            while(queue.Count>0) {
                var car=queue.Dequeue();if(car==null||result.ContainsKey(car.id))continue;
                if(result.Count>=256||car.IsDerailed||car.IsInBardo)throw new InvalidOperationException("Verband nicht vollständig lesbar.");
                result[car.id]=car;foreach(var end in new[]{Car.LogicalEnd.A,Car.LogicalEnd.B}){var next=car.CoupledTo(end);if(next!=null)queue.Enqueue(next);}
            }
            return result.Values.OrderBy(c=>c.id,StringComparer.Ordinal).ToArray();
        }
        public static string Signature(IEnumerable<Car> cars)=>SharedDocuments.Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("|",cars.Select(c=>c.id+":"+(c.CoupledTo(Car.LogicalEnd.A)?.id??"")+":"+(c.CoupledTo(Car.LogicalEnd.B)?.id??"")).OrderBy(s=>s,StringComparer.Ordinal))));
        public static AutomationTrain Train(Car car,AutomationConfiguration config) {
            var cars=Consist(car);if(cars.Length==0)return null;string signature=Signature(cars);
            var masters=cars.Where(c=>c.IsLocomotive&&config.Role(c.id,signature)==MovementRole.Train).ToArray();
            return masters.Length==1?new AutomationTrain{Master=masters[0].id,Consist=signature,Cars=cars}:null;
        }
        public static AutomationIntent Intent(Car locomotive,bool allowPausedWaypoint=false) {
            var planner=(locomotive as BaseLocomotive)?.AutoEngineerPlanner;
            if(planner==null)return new AutomationIntent{Mode=AutomationDrivingMode.Manual,Signature="manual"};
            try {
                var current=(Orders)orders.GetValue(planner);var intent=new AutomationIntent{Mode=(AutomationDrivingMode)(int)current.Mode};
                intent.Signature=OrderSignature(current);
                if(current.Mode!=Game.Messages.AutoEngineerMode.Off&&!current.Enabled&&!(allowPausedWaypoint&&current.Mode==Game.Messages.AutoEngineerMode.Waypoint)){intent.Ready=false;intent.Reason=LocalText.Get("KI-Fahrauftrag pausiert / ohne Fahrfreigabe.");return intent;}
                if(current.Mode!=Game.Messages.AutoEngineerMode.Waypoint)return intent;
                if(!current.Waypoint.HasValue||current.Waypoint.Value.WantsCouple){intent.Ready=false;intent.Reason=LocalText.Get("Kein fester Wegpunkt oder Kuppelauftrag: keine Zugautomatik.");return intent;}
                var target=(Location?)routeTarget.GetValue(planner);var steps=route.GetValue(planner) as List<Track.Search.RouteSearch.Step>;
                if(!target.HasValue||!target.Value.IsValid||steps==null||steps.Count==0){intent.Ready=false;intent.Reason=LocalText.Get("Wegpunkt-Gleisweg wird noch ermittelt.");return intent;}
                var canonical=target.Value.WithEnd(TrackSegment.End.A);intent.Target=new MarkerAnchor{Segment=canonical.segment.id,Meters=canonical.distance};
                int start=Math.Max(0,(int)startIndex.GetValue(planner)-1);
                foreach(var step in steps.Skip(start))if(step.Location.IsValid&&(intent.Segments.Count==0||intent.Segments.Last()!=step.Location.segment.id))intent.Segments.Add(step.Location.segment.id);
                if(intent.Segments.Count==0||intent.Segments.Last()!=intent.Target.Segment)intent.Segments.Add(intent.Target.Segment);
                return intent;
            } catch(Exception e){return new AutomationIntent{Ready=false,Reason="KI-Auftrag nicht lesbar: "+e.Message};}
        }
        static string OrderSignature(Orders current) {
            if(!current.Waypoint.HasValue)return current.Mode+"|"+current.Forward;
            var waypoint=current.Waypoint.Value;
            // A plain waypoint's exact native location identifies the order across replans.
            string target=waypoint.WantsCouple||waypoint.IsPosed?"special|"+waypoint.GetHashCode():waypoint.LocationString;
            return current.Mode+"|"+current.Forward+"|"+target;
        }

        public static bool TryWaypointApproach(Car locomotive,AutomationIntent intent,out WaypointApproach approach,bool allowPausedWaypoint=false) {
            approach=null;
            if(!(locomotive is BaseLocomotive engine)||engine.IsInBardo||engine.IsDerailed||intent==null||!intent.Ready||intent.Mode!=AutomationDrivingMode.Waypoint||intent.Target==null)return false;
            var planner=engine.AutoEngineerPlanner;
            if(planner==null||routeStart==null||plannedWaypoint==null)return false;
            try {
                var current=(Orders)orders.GetValue(planner);
                if((!current.Enabled&&!allowPausedWaypoint)||current.Mode!=Game.Messages.AutoEngineerMode.Waypoint||!current.Waypoint.HasValue||current.Waypoint.Value.WantsCouple||current.Waypoint.Value.IsPosed)return false;
                if(intent.Signature!=OrderSignature(current)||!current.Waypoint.Value.Equals((OrderWaypoint)plannedWaypoint.GetValue(planner)))return false;
                if(!engine.IsStopped(0)&&(engine.velocity>=0)!=current.Forward)return false;
                var steps=route.GetValue(planner) as List<RouteSearch.Step>;
                if(steps==null||steps.Count<2||steps.Count>10001)return false;
                var arguments=new object[]{engine,current.Forward,ConsistEnd.A};
                // This is the game's own route-search origin, which follows the outermost
                // wheel bound of the currently leading consist end rather than the locomotive.
                var head=(Location)routeStart.Invoke(null,arguments);
                var leadingEnd=(ConsistEnd)arguments[2];
                if(!head.IsValid||!TryWaypointSections(steps,leadingEnd,out var sections))return false;
                var canonical=head.WithEnd(TrackSegment.End.A);
                approach=WaypointApproach.Create(sections,new MarkerAnchor{Segment=canonical.segment.id,Meters=canonical.distance},!head.EndIsA,intent.Target);
                return approach!=null;
            } catch(Exception) {
                return false;
            }
        }

        static bool TryWaypointSections(IReadOnlyList<RouteSearch.Step> steps,ConsistEnd leadingEnd,out List<DirectedRouteSection> sections) {
            sections=new List<DirectedRouteSection>();
            var initial=steps[0];
            var previous=initial.Location;
            if(!UsableWaypointLocation(previous)||initial.LeadingEnd!=leadingEnd)return false;
            double entry=previous.WithEnd(TrackSegment.End.A).distance;
            for(int i=1;i<steps.Count;i++) {
                var step=steps[i];
                var next=step.Location;
                if(step.LeadingEnd!=leadingEnd||step.Direction!=initial.Direction||!UsableWaypointLocation(next))return false;
                if(next.segment==previous.segment) {
                    if(next.end!=previous.end||next.distance<previous.distance-.001)return false;
                } else {
                    // The native search stores each traversal from its departure node.
                    // Require a shared real endpoint and a completed previous traversal.
                    var arrival=previous.EndIsA?previous.segment.b:previous.segment.a;
                    var departure=next.EndIsA?next.segment.a:next.segment.b;
                    if(arrival==null||arrival!=departure||Math.Abs(previous.DistanceUntilEnd())>.001)return false;
                    AddWaypointSection(sections,previous,entry);
                    entry=next.EndIsA?0:next.segment.GetLength();
                }
                previous=next;
            }
            AddWaypointSection(sections,previous,entry);
            return sections.Count>0;
        }

        static bool UsableWaypointLocation(Location location)=>location.IsValid&&location.segment.Available&&location.segment.GroupEnabled&&location.segment.turntable==null;

        static void AddWaypointSection(List<DirectedRouteSection> sections,Location exit,double entry) {
            double end=exit.WithEnd(TrackSegment.End.A).distance;
            double from=Math.Max(0,Math.Min(entry,end)),to=Math.Max(entry,end);
            if(to-from<=.001)return;
            sections.Add(new DirectedRouteSection{Reverse=!exit.EndIsA,Section=new TrackSection{Segment=exit.segment.id,From=from,To=to}});
        }

        public static double? Distance(MarkerAnchor from,bool reverse,Location to,double limit=300) {
            var graph=Graph.Shared;if(graph==null||!to.IsValid)return null;var target=to.WithEnd(TrackSegment.End.A);
            PlanEdge Edge(TrackSegment segment)=>segment==null||!segment.Available||!segment.GroupEnabled||segment.turntable!=null?null:
                new PlanEdge{Id=segment.id,A=segment.a.id,B=segment.b.id,Length=segment.GetLength()};
            PlanNode Node(string id) {
                var junction=graph.GetNode(id);if(junction==null)return null;bool point=graph.IsSwitch(junction);string common=null;
                if(point&&graph.DecodeSwitchAt(junction,out var stem,out _,out _))common=stem.id;
                return new PlanNode{Id=id,Point=point,Common=common};
            }
            return ContactDistance.Find(from,reverse,new MarkerAnchor{Segment=target.segment.id,Meters=target.distance},
                id=>Edge(graph.GetSegment(id)),Node,id=>graph.SegmentsConnectedTo(graph.GetNode(id)).Select(Edge),limit);
        }
        public static IEnumerable<KeyValuePair<string,Location>> Axles(Car car) {
            var graph=Graph.Shared;if(graph==null||car==null)yield break;
            foreach(var wheelset in car.GetComponentsInChildren<RollingStock.Wheelset>(true)) {
                var transforms=wheelset==null?null:wheels?.GetValue(wheelset) as IEnumerable<Transform>;if(transforms==null)continue;
                foreach(var wheel in transforms)if(wheel!=null&&graph.TryGetLocationFromWorldPoint(wheel.position,2,out var at)&&at.IsValid)
                    yield return new KeyValuePair<string,Location>(car.id+"/axle/"+wheel.GetInstanceID(),at);
            }
        }
    }
}
