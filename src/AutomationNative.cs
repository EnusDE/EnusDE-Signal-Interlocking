using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Model;
using Model.AI;
using Track;
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
        public static AutomationIntent Intent(Car locomotive) {
            var planner=(locomotive as BaseLocomotive)?.AutoEngineerPlanner;
            if(planner==null)return new AutomationIntent{Mode=AutomationDrivingMode.Manual,Signature="manual"};
            try {
                var current=(Orders)orders.GetValue(planner);var intent=new AutomationIntent{Mode=(AutomationDrivingMode)(int)current.Mode};
                intent.Signature=current.Mode+"|"+current.Forward+"|"+(current.Waypoint.HasValue?current.Waypoint.Value.GetHashCode().ToString():"");
                if(current.Mode!=Game.Messages.AutoEngineerMode.Off&&!current.Enabled){intent.Ready=false;intent.Reason=LocalText.Get("KI-Fahrauftrag pausiert / ohne Fahrfreigabe.");return intent;}
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
