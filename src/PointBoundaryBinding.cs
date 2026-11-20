using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class PointBoundaryBinding {
        public static TrackBlock Apply(TrackBlock source,string[] segments,bool[] atStart,double[] lengths,IReadOnlyDictionary<int,string> links,Func<string,MarkerRecord> lookup) {
            var result=source.Clone();
            foreach(var pair in links) {
                int arm=pair.Key;var end=lookup(pair.Value);
                if(arm<0||arm>2||end?.Anchor==null||!CircuitIdentity.Enabled(end)||CircuitIdentity.Variant(end)==3||end.Anchor.Segment!=segments[arm]||
                    end.Anchor.Meters<0||end.Anchor.Meters>lengths[arm]||(atStart[arm]?end.Anchor.Meters:lengths[arm]-end.Anchor.Meters)<.01)
                    throw new InvalidOperationException(LocalText.Select("The selected boundary must be active and on the selected native point arm (A or B, not C).", "Die gewählte Grenze muss aktiv auf dem gewählten realen Weichenast liegen (A oder B, kein C)."));
                result.Guards.RemoveAll(g=>g.Segment==segments[arm]);
                result.Guards.Add(new TrackSection{Segment=segments[arm],From=atStart[arm]?0:end.Anchor.Meters,To=atStart[arm]?end.Anchor.Meters:lengths[arm]});
            }
            result.Validate();return result;
        }
    }
    public sealed partial class SignalRuntime {
        internal TrackBlock ReadPointDefinition(TrackBlock block) {
            if(block?.Point==null)return block;
            var graph=Track.Graph.Shared;var node=graph?.GetNode(block.Point);
            if(node==null||!graph.DecodeSwitchAt(node,out var common,out var normal,out var branch))throw new InvalidOperationException(LocalText.Select("Point geometry unavailable.","Weichengeometrie nicht verfügbar."));
            var edges=new[]{common,normal,branch};
            block=PointGuardGeometry.Build(block.Point,block.Name,edges.Select(e=>new PlanEdge{Id=e.id,A=e.a.id,B=e.b.id,Length=e.GetLength()}),physicalBoundaries.Values.Where(CircuitIdentity.Enabled).Select(b=>b.Anchor));
            if(!desks.Points.TryGetValue(block.Point,out var settings)||settings.BoundaryLinks.Count==0)return block;
            return PointBoundaryBinding.Apply(block,edges.Select(e=>e.id).ToArray(),edges.Select(e=>e.a==node).ToArray(),edges.Select(e=>(double)e.GetLength()).ToArray(),settings.BoundaryLinks,id=>physicalBoundaries.TryGetValue(id,out var end)?end:null);
        }
    }
}
