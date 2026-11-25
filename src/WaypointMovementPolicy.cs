using System;
using System.Collections.Generic;
using System.Linq;
namespace EnusDE.Signaltechnik {
    public static class WaypointMovementPolicy {
        // Determine movement from static path/capability only. Never inspect admission failures or occupancy.
        public static MovementRole Choose(MovementRole selection,bool trainSupported,bool shuntSupported,Func<bool> trainTarget,Func<bool> localShuntTarget) {
            if(selection!=MovementRole.Unassigned)return selection;
            if(trainSupported&&trainTarget())return MovementRole.Train;
            return shuntSupported&&localShuntTarget()?MovementRole.Shunt:MovementRole.Train;
        }
        public static bool ProvenLocalAlternative(WaypointApproach approach,MarkerRecord start,MarkerRecord target,bool sameStation,IEnumerable<LineTrackDefinition> lines) {
            if(!sameStation||approach==null||start?.Anchor==null||target?.Anchor==null||
                !approach.TryLocate(start.Anchor.Segment,start.Anchor.Meters,out double from,out bool reverse)||reverse!=start.FacingReverse||
                !approach.TryLocate(target.Anchor.Segment,target.Anchor.Meters,out double to,out bool endReverse)||endReverse!=target.FacingReverse||to<=from)return false;
            var all=lines.ToArray();
            if(all.SelectMany(l=>l.BlockUnits).Any(u=>u.Forward==start.Id||u.Reverse==start.Id||u.Forward==target.Id||u.Reverse==target.Id))return false;
            var lineSections=all.SelectMany(l=>l.Forward.Concat(l.Reverse)).Select(p=>p.Section).ToArray();double distance=0;
            foreach(var piece in approach.Sections) {
                double length=piece.Section.To-piece.Section.From,a=Math.Max(from,distance),b=Math.Min(to,distance+length);
                if(b>a){double lo=a-distance,hi=b-distance;var span=new TrackSection{Segment=piece.Section.Segment,From=piece.Reverse?piece.Section.To-hi:piece.Section.From+lo,To=piece.Reverse?piece.Section.To-lo:piece.Section.From+hi};if(lineSections.Any(s=>s.Overlaps(span)))return false;}
                distance+=length;
            }
            return true;
        }
    }
}
