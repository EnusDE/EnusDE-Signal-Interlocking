using System;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public enum GfmBoundaryFault { None,Missing,Planned,Disabled,Moved,PointUnavailable,Unavailable }
    public static class GfmFaults {
        public static MarkerAnchor End(TrackBlock block,bool first) {
            var p=first?block.Path.First():block.Path.Last();return new MarkerAnchor{Segment=p.Section.Segment,Meters=first?(p.Reverse?p.Section.To:p.Section.From):(p.Reverse?p.Section.From:p.Section.To)};
        }
        public static GfmBoundaryFault Classify(string id,MarkerRecord raw,MarkerRecord active,MarkerAnchor expected=null) {
            if(raw?.Planned==true)return GfmBoundaryFault.Planned;
            if(active!=null&&CircuitIdentity.Enabled(active))return expected!=null&&(active.Anchor.Segment!=expected.Segment||Math.Abs(active.Anchor.Meters-expected.Meters)>.005)?GfmBoundaryFault.Moved:GfmBoundaryFault.None;
            if(raw!=null&&!CircuitIdentity.Enabled(raw))return GfmBoundaryFault.Disabled;
            if(TrackCircuit.TryPointId(id,out _,out _))return GfmBoundaryFault.PointUnavailable;
            return raw==null?GfmBoundaryFault.Missing:GfmBoundaryFault.Unavailable;
        }
        public static string Reason(GfmBoundaryFault state)=>state==GfmBoundaryFault.Missing?LocalText.Select("object deleted or missing", "Objekt gelöscht oder nicht vorhanden"):
            state==GfmBoundaryFault.Planned?LocalText.Select("construction plan, not completed", "Bauplan, noch nicht fertiggestellt"):
            state==GfmBoundaryFault.Disabled?LocalText.Select("detector disabled", "Melder deaktiviert"):
            state==GfmBoundaryFault.Moved?LocalText.Select("saved and current boundary positions differ", "Gespeicherte und aktuelle Grenzposition weichen ab"):
            state==GfmBoundaryFault.PointUnavailable?LocalText.Select("point detection or its arm link is unavailable", "Weichen-GFM oder deren Astverknüpfung nicht verfügbar"):
            state==GfmBoundaryFault.Unavailable?LocalText.Select("boundary unavailable; inspect its assignment", "Grenze nicht verfügbar; Zuordnung prüfen"):LocalText.Select("active", "aktiv");
        public static string Position(MarkerAnchor at)=>at.Segment+" · "+SignUnits.Number(at.Meters)+" m";
        public static string Caption(string id,MarkerRecord raw,MarkerRecord active) {
            if(active!=null)return CircuitIdentity.Caption(active);
            string name=raw==null?LocalText.Select("Boundary ","Grenze ")+(id??"—").Substring(0,Math.Min(8,(id??"—").Length)):CircuitIdentity.Caption(raw);
            return name+" ("+Reason(Classify(id,raw,active))+")";
        }
    }
}
