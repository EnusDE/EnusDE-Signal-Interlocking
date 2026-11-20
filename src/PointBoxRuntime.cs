using System;
using System.Linq;
using Track;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        internal MarkerRecord PointBoxDefault(string id) {
            if(!TrackCircuit.TryPointId(id,out var point,out int arm))return null;
            var row=PointBoundaries(point).ElementAtOrDefault(arm);
            return row!=null&&row.Name.StartsWith("GFM-",StringComparison.Ordinal)?row:null;
        }
        internal PointBoxPlacement PointBoxPosition(string id)=>desks?.PointBoxes.TryGetValue(id,out var value)==true?value.Clone():null;
        internal void PutPointBoxPosition(string id,PointBoxPlacement placement) {
            RequireEdit();var boundary=PointBoxDefault(id)??throw new InvalidOperationException("Weichengrenze nicht mehr verfügbar.");
            var old=PointBoxPosition(id)??PointBoxPlacement.Default(boundary);var proposed=placement??PointBoxPlacement.Default(boundary);
            if((old.Variant==3)!=(proposed.Variant==3)||(old.Variant==3||proposed.Variant==3)&&old.DirectionFlip!=proposed.DirectionFlip)RequireIdleAdministration();
            if(placement!=null) {
                placement.Validate();var row=placement.Apply(boundary);
                var segment=Graph.Shared?.GetSegment(row.Anchor.Segment);
                if(segment==null||row.Anchor.Meters<0||row.Anchor.Meters>segment.GetLength())
                    throw new InvalidOperationException("Der Längsversatz muss innerhalb des Grenzsegments bleiben.");
            }
            var next=desks.Clone();
            if(placement==null)next.PointBoxes.Remove(id);else next.PointBoxes[id]=placement.Clone();
            deskStore.Save(next);desks=next;panel=CurrentDesk.Panel;
        }
    }
}
