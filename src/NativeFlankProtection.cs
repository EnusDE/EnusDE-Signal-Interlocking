using System;
using System.Linq;
using Track;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalWorldBridge : IFlankOccupancyWorld {
        readonly System.Collections.Generic.Dictionary<string,TrackOccupancy> flankReadings=new System.Collections.Generic.Dictionary<string,TrackOccupancy>();
        public TrackOccupancy FlankOccupancy(TrackSection section){TrackOccupancy state;if(flankReadings.TryGetValue(section.Key,out state))return state;state=ReadFlankOccupancy(section);flankReadings[section.Key]=state;return state;}
        static NetworkInput FlankGraph(){var graph=Graph.Shared;var input=new NetworkInput();foreach(var node in graph.Nodes){TrackSegment common=null,normal=null,reverse;bool point=graph.IsSwitch(node);if(point&&!graph.DecodeSwitchAt(node,out common,out normal,out reverse))throw new InvalidOperationException("Weichenäste nicht lesbar.");input.Nodes.Add(new PlanNode{Id=node.id,Point=point,Common=common?.id,Normal=normal?.id});}foreach(var edge in graph.AllSegments.Where(s=>s!=null&&s.turntable==null&&s.Available&&s.GroupEnabled))input.Edges.Add(new PlanEdge{Id=edge.id,A=edge.a.id,B=edge.b.id,Length=edge.GetLength()});return input;}
        TrackOccupancy ReadFlankOccupancy(TrackSection section){var state=Occupancy(section);if(state!=TrackOccupancy.Clear)return state;var trains=TrainController.Shared;if(trains==null)return TrackOccupancy.Unknown;int checkedCars=0;foreach(var car in trains.Cars){if(car==null||car.IsInBardo)continue;if(++checkedCars>4096)return TrackOccupancy.Unknown;var a=car.LocationA;var b=car.LocationB;if(!a.IsValid||!b.IsValid)continue;a=a.WithEnd(TrackSegment.End.A);b=b.WithEnd(TrackSegment.End.A);if(a.segment.id==section.Segment&&a.distance>=section.From&&a.distance<=section.To||b.segment.id==section.Segment&&b.distance>=section.From&&b.distance<=section.To)return TrackOccupancy.Occupied;if(a.segment.id==section.Segment&&b.segment.id==section.Segment&&Math.Max(a.distance,b.distance)>=section.From&&Math.Min(a.distance,b.distance)<=section.To)return TrackOccupancy.Occupied;}return TrackOccupancy.Clear;}
        bool FlankBindingsValid(RouteDefinition route){if(route.Flanks==null)return true;foreach(var signal in route.Flanks.SelectMany(f=>f.Signals)){var actual=owner.Object(signal.Id);if(actual==null||!actual.IsSignal||RouteSignals.Anchor(actual)!=signal.Anchor)return false;}return true;}
    }
}
