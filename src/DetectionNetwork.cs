using System.Linq;
using System.Collections.Generic;
using System.Threading;
namespace EnusDE.Signaltechnik {
    public static class DetectionNetwork {
        // Signals without their own detector and virtual targets do not interrupt
        // the physical section. Turnout boundaries and one-sided C remain intact.
        public static NetworkResult Build(NetworkInput input)=>Complete(input,NetworkPlan.Build(input,draw:false));
        public static NetworkResult Complete(NetworkInput input,NetworkResult result,CancellationToken cancel=default) {
            var detectorOnly=new NetworkInput{World=input.World,Nodes=input.Nodes,Edges=input.Edges,Cuts=input.Cuts.Where(c=>c.Boundary!=null&&c.BoundaryKind>0).Select(c=>new PlanCut{Segment=c.Segment,Meters=c.Meters,X=c.X,Y=c.Y,Boundary=c.Boundary,BoundaryCode=c.BoundaryCode,BoundaryKind=c.BoundaryKind,BoundaryReverse=c.BoundaryReverse}).ToList()};
            foreach(var block in NetworkPlan.Build(detectorOnly,cancel,draw:false).Blocks.Values.Where(b=>b.IsCircuit))result.Blocks[block.Id]=block;
            return result;
        }
    }
}
