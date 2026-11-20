using System;
using System.Linq;
using Track;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalWorldBridge : IFlankOccupancyWorld {
        readonly System.Collections.Generic.Dictionary<string,TrackOccupancy> flankReadings=new System.Collections.Generic.Dictionary<string,TrackOccupancy>();
        public TrackOccupancy FlankOccupancy(TrackSection section){TrackOccupancy state;if(flankReadings.TryGetValue(section.Key,out state))return state;state=ReadFlankOccupancy(section);flankReadings[section.Key]=state;return state;}
        static NetworkInput FlankGraph(){var graph=Graph.Shared;var input=new NetworkInput();foreach(var node in graph.Nodes){TrackSegment common=null,normal=null,reverse;bool point=graph.IsSwitch(node);if(point&&!graph.DecodeSwitchAt(node,out common,out normal,out reverse))throw new InvalidOperationException("Weichenäste nicht lesbar.");input.Nodes.Add(new PlanNode{Id=node.id,Point=point,Common=common?.id,Normal=normal?.id});}foreach(var edge in graph.AllSegments.Where(s=>s!=null&&s.turntable==null&&s.Available&&s.GroupEnabled))input.Edges.Add(new PlanEdge{Id=edge.id,A=edge.a.id,B=edge.b.id,Length=edge.GetLength()});return input;}
        internal System.Collections.Generic.IEnumerable<bool> CaptureRouteGraph(NetworkInput input) {
            var graph=Graph.Shared??throw new InvalidOperationException(LocalText.Select("Track network unavailable.","Gleisnetz nicht verfügbar."));
            foreach(var node in graph.Nodes) {
                TrackSegment common=null,normal=null,reverse;bool point=graph.IsSwitch(node);
                if(point&&!graph.DecodeSwitchAt(node,out common,out normal,out reverse))throw new InvalidOperationException(LocalText.Select("Point branches cannot be read.","Weichenäste nicht lesbar."));
                input.Nodes.Add(new PlanNode{Id=node.id,Point=point,Common=common?.id,Normal=normal?.id});yield return true;
            }
            foreach(var edge in graph.AllSegments) {
                if(edge!=null&&edge.turntable==null&&edge.Available&&edge.GroupEnabled)input.Edges.Add(new PlanEdge{Id=edge.id,A=edge.a.id,B=edge.b.id,Length=edge.GetLength()});
                yield return true;
            }
        }
        TrackOccupancy ReadFlankOccupancy(TrackSection section)=>Occupancy(section);
        bool FlankBindingsValid(RouteDefinition route){if(route.Flanks==null)return true;foreach(var signal in route.Flanks.SelectMany(f=>f.Signals)){var actual=owner.Object(signal.Id);if(actual==null||!actual.IsSignal||owner.OperatingState(signal.Id)!=SignalOperatingState.Active||RouteSignals.Anchor(actual)!=signal.Anchor)return false;}return true;}
    }
}
