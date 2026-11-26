using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        MarkerRecord[] circuitValidationBoundaries=new MarkerRecord[0];
        long circuitValidationInstall=-1,circuitValidationConfiguration=-1;
        NetworkInput circuitValidationNetwork;
        double nextCircuitValidation;
        internal bool CircuitUsable(TrackBlock block) {
            var shared=Entry.Shared;if(shared==null)return false;
            if(circuitValidationInstall!=shared.InstallationVersion||circuitValidationConfiguration!=shared.ConfigurationVersion||UnityEngine.Time.realtimeSinceStartup>=nextCircuitValidation) {
                circuitValidationInstall=shared.InstallationVersion;circuitValidationConfiguration=shared.ConfigurationVersion;
                circuitValidationBoundaries=physicalBoundaries.Values.Where(CircuitIdentity.Enabled)
                    .Concat(desks.Points.Where(p=>p.Value.ShowCircuit).SelectMany(p=>SafePointBoundaries(p.Key))).Where(b=>b!=null).ToArray();
                circuitValidationNetwork=CaptureNetwork();nextCircuitValidation=UnityEngine.Time.realtimeSinceStartup+2;
            }
            var a=CircuitEndpoint(block.BoundaryA);var b=CircuitEndpoint(block.BoundaryB);
            return CircuitPathValidation.Error(circuitValidationNetwork,block,a,b)==null&&
                TrackCircuit.InteriorClear(block,a,b,circuitValidationBoundaries,circuitValidationNetwork);
        }
        bool CircuitEndsMatch(TrackBlock block,MarkerRecord a,MarkerRecord b) {
            if(a?.Anchor==null||b?.Anchor==null||block.Path.Count==0)return false;
            bool adjacent=a.Anchor.Segment!=block.Path[0].Section.Segment||b.Anchor.Segment!=block.Path.Last().Section.Segment;
            return TrackCircuit.EndsMatch(block,a,b,adjacent?CaptureNetwork():null);
        }
    }
}
