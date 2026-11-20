using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class SharedCircuitBoundary {
        public readonly Dictionary<string,MarkerRecord> Signals=new Dictionary<string,MarkerRecord>(StringComparer.Ordinal);
        public readonly Dictionary<string,string> PointSignals=new Dictionary<string,string>(StringComparer.Ordinal);
        static bool SamePosition(MarkerAnchor a,MarkerAnchor b) {
            float meters=(float)a.Meters;
            return a.Segment==b.Segment&&!float.IsNaN(meters)&&!float.IsInfinity(meters)&&meters==(float)b.Meters;
        }
        public static SharedCircuitBoundary Resolve(IEnumerable<MarkerRecord> physical,IEnumerable<MarkerRecord> points,IReadOnlyDictionary<string,string> explicitLinks=null) {
            var result=new SharedCircuitBoundary();
            // Sharing is exact, physical and bidirectional. Mere visual proximity never moves a logical boundary.
            var signals=physical.Where(s=>CircuitIdentity.Enabled(s)&&s.Anchor!=null).ToArray();
            foreach(var point in points.Where(p=>p!=null&&p.Kind==MarkerRecord.CircuitB&&!p.Planned&&p.Anchor!=null&&p.Name?.StartsWith("GFM-B-",StringComparison.Ordinal)==true&&TrackCircuit.TryPointId(p.Id,out _,out _))) {
                string linked=null;bool manual=explicitLinks?.TryGetValue(point.Id,out linked)==true;
                var matches=signals.Where(s=>SamePosition(s.Anchor,point.Anchor)&&(!manual||s.Id==linked)).ToArray();
                if(matches.Length!=1||CircuitIdentity.Variant(matches[0])==3||(!manual&&(!matches[0].IsSignal||CircuitIdentity.Variant(matches[0])!=1))||matches[0].CircuitNumber<1)continue;
                var signal=matches[0];result.PointSignals[point.Id]=signal.Id;
                if(!result.Signals.ContainsKey(signal.Id)){var copy=signal.Clone();copy.SharedPointBoundary=copy.IsSignal;result.Signals[copy.Id]=copy;}
            }
            return result;
        }
        public MarkerRecord PointAlias(MarkerRecord point) {
            if(point==null||!PointSignals.TryGetValue(point.Id,out var id))return point;
            var physical=Signals[id];var alias=point.Clone();alias.SharedPointBoundary=physical.IsSignal;alias.PhysicalId=id;alias.CircuitNumber=physical.CircuitNumber;
            alias.Name=CircuitIdentity.Caption(physical);return alias;
        }
    }
    public sealed partial class SignalRuntime {
        SharedCircuitBoundary sharedPointCircuits;
        DeskLibrary sharedCircuitLibrary;
        long physicalBoundaryVersion=-1,sharedCircuitVersion=-2;
        SharedCircuitBoundary SharedPointCircuits() {
            if(sharedPointCircuits!=null&&ReferenceEquals(desks,sharedCircuitLibrary)&&sharedCircuitVersion==physicalBoundaryVersion)return sharedPointCircuits;
            var points=desks==null?Enumerable.Empty<MarkerRecord>():desks.Points.Where(p=>p.Value.ShowCircuit).SelectMany(p=>SafePointBoundaries(p.Key));
            var links=desks?.Points.SelectMany(p=>p.Value.BoundaryLinks.Select(b=>new{Id=TrackCircuit.PointId(p.Key,b.Key),Boundary=b.Value})).ToDictionary(p=>p.Id,p=>p.Boundary);
            sharedPointCircuits=SharedCircuitBoundary.Resolve(physicalBoundaries.Values,points,links);sharedCircuitLibrary=desks;sharedCircuitVersion=physicalBoundaryVersion;
            return sharedPointCircuits;
        }
        internal bool IsSharedSignalCircuit(string id)=>SharedPointCircuits().Signals.ContainsKey(id);
        internal string SharedPointSignal(string id)=>SharedPointCircuits().PointSignals.TryGetValue(id,out var signal)?signal:null;
        internal string[] SharedPointSignals(string point)=>SharedPointCircuits().PointSignals.Where(p=>TrackCircuit.TryPointId(p.Key,out var node,out _)&&node==point).Select(p=>p.Value).Distinct().OrderBy(Code).ToArray();
        internal string[] SharedSignalPoints(string signal)=>SharedPointCircuits().PointSignals.Where(p=>p.Value==signal).Select(p=>{TrackCircuit.TryPointId(p.Key,out var point,out _);return PointAssignments.Caption(desks,point);}).Distinct().OrderBy(s=>s,StringComparer.Ordinal).ToArray();
        string CanonicalBoundaryId(string id)=>BlockBoundary(id)??(id!=null&&SharedPointCircuits().PointSignals.TryGetValue(id,out var signal)?signal:id);
        MarkerRecord EffectivePhysicalBoundary(MarkerRecord marker)=>SharedPointCircuits().Signals.TryGetValue(marker.Id,out var shared)?shared:marker;
    }
}
