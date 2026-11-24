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

        public static void RebindAutomatic(PanelDocument panel,Dictionary<string,TrackBlock> blocks) {
            var bySegment=blocks.Values.Where(block=>block.Point==null)
                .SelectMany(block=>block.Path.Select(piece=>new{Block=block,Section=piece.Section}))
                .ToLookup(piece=>piece.Section.Segment);
            foreach(var tile in panel.Tiles.Where(t=>t.AutoSegment!=""&&t.Kind!=PanelTileKind.Switch&&t.SecondBlock==""&&
                t.Section==null&&t.CircuitRails?.Count is not >0)) {
                var matching=bySegment[tile.AutoSegment].Where(piece=>piece.Section.From<=tile.AutoMeters&&tile.AutoMeters<=piece.Section.To)
                    .Select(piece=>piece.Block).Distinct().ToArray();
                var circuits=matching.Where(block=>block.IsCircuit).ToArray();
                var candidates=circuits.Length>0?circuits:matching;
                tile.BlockId=candidates.Length==1?candidates[0].Id:"";
            }
        }
    }
}
