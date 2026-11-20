using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class PointGuardGeometry {
        public static TrackBlock Build(string point,string name,IEnumerable<PlanEdge> edges,IEnumerable<MarkerAnchor> physicalBoundaries) {
            var bounds=physicalBoundaries.ToLookup(a=>a.Segment,StringComparer.Ordinal);
            var block=new TrackBlock{Id="p:"+point,Name=name,Point=point};
            foreach(var edge in edges.Where(e=>e.A==point||e.B==point)) {
                bool atStart=edge.A==point;double length=Math.Min(25,edge.Length);
                foreach(var end in bounds[edge.Id]){double distance=atStart?end.Meters:edge.Length-end.Meters;if(distance>.001&&distance<length)length=distance;}
                block.Guards.Add(new TrackSection{Segment=edge.Id,From=atStart?0:edge.Length-length,To=atStart?length:edge.Length});
            }
            block.Validate();return block;
        }
    }
}
