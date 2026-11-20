using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class GfmCoverage {
        public static List<TrackSection> Gaps(IEnumerable<TrackSection> path,IEnumerable<TrackSection> coverage) {
            var spans=coverage.ToLookup(s=>s.Segment,StringComparer.Ordinal);var gaps=new List<TrackSection>();
            foreach(var piece in path) {
                double at=piece.From;
                foreach(var span in spans[piece.Segment].Where(s=>s.To>piece.From&&s.From<piece.To).OrderBy(s=>s.From)) {
                    if(span.From>at+.005)gaps.Add(new TrackSection{Segment=piece.Segment,From=at,To=Math.Min(span.From,piece.To)});
                    at=Math.Max(at,Math.Min(span.To,piece.To));if(at>=piece.To-.005)break;
                }
                if(at<piece.To-.005)gaps.Add(new TrackSection{Segment=piece.Segment,From=at,To=piece.To});
            }
            return gaps.GroupBy(s=>s.Key).Select(g=>g.First()).ToList();
        }
        public static int Ports(PanelTile tile,ISet<string> blocks) {
            int ports=0;
            if(blocks.Contains(tile.BlockId)||blocks.Contains(tile.SecondBlock))ports=tile.Kind==PanelTileKind.TrainNumber&&tile.Ports==0&&!NumberMirrors.IsMirror(tile)?PanelConnections.Transform(17,tile.Rotation,tile.Mirror):PanelConnections.Ports(tile);
            if(tile.Kind==PanelTileKind.Switch&&blocks.Contains("p:"+tile.Reference))ports=PanelConnections.Ports(tile);
            if(tile.CircuitRails!=null)foreach(var pair in tile.CircuitRails)if(blocks.Contains(pair.Value))ports|=PanelConnections.Transform(1<<pair.Key,tile.Rotation,tile.Mirror);
            return ports;
        }
    }
}
