using System;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public interface ISectionCaptionWorld {string SectionCaption(RouteDefinition route,TrackSection section);}
    public sealed partial class SignalWorldBridge : ISectionCaptionWorld {
        public string SectionCaption(RouteDefinition route,TrackSection section)=>owner.SectionCaption(route,section)+(vehicleOccupancy?.GlobalUnknown==true?
            LocalText.Select(" · Unreadable vehicle positions: "," · Nicht lesbare Fahrzeugpositionen: ")+string.Join(", ",vehicleOccupancy.UnresolvedVehicles.Take(3)):"");
    }
    public sealed partial class SignalRuntime {
        internal string SectionCaption(RouteDefinition route,TrackSection section) {
            var point=route.Switches.FirstOrDefault(p=>p.Protection?.Any(s=>s.Overlaps(section))==true);
            var circuit=desks?.Blocks.Values.Where(b=>b.IsCircuit&&b.Path.Any(p=>p.Section.Overlaps(section))).OrderBy(b=>b.Path.Sum(p=>p.Section.To-p.Section.From)).FirstOrDefault();
            string context=Code(route.Entry)+" → "+Code(route.Exit);
            if(point!=null)context+=" · "+PointAssignments.Caption(desks,point.Node);
            if(circuit!=null)context+=" · "+BoundaryCaption(circuit.BoundaryA)+" ↔ "+BoundaryCaption(circuit.BoundaryB);
            return context+" · "+LocalText.Select("track ","Gleis ")+section.Segment+", "+SignUnits.Number(section.From)+"–"+SignUnits.Number(section.To)+" m";
        }
    }
}
