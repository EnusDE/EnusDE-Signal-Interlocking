using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class BlockLayoutPlan {
        public List<DirectedRouteSection> Axis;
        public List<double> Boundaries=new List<double>();
        public double Length=>Axis.Sum(p=>p.Section.To-p.Section.From);
        public int BlockCount=>Boundaries.Count+1;
        public static BlockLayoutPlan Create(IEnumerable<DirectedRouteSection> axis,int blocks,double? spacing=null) {
            var plan=new BlockLayoutPlan{Axis=LineTrackGeometry.Normalize(axis)};
            if(spacing.HasValue){if(!Finite(spacing.Value)||spacing.Value<10)throw new InvalidOperationException(LocalText.Select("Enter a spacing of at least 10 m.","Einen Abstand von mindestens 10 m eingeben."));blocks=(int)Math.Ceiling(plan.Length/spacing.Value);}
            if(blocks<1||blocks>16||plan.Length<10*blocks)throw new InvalidOperationException(LocalText.Select("Choose 1–16 blocks, each at least 10 m long.","1–16 Blöcke mit jeweils mindestens 10 m Länge wählen."));
            for(int i=1;i<blocks;i++)plan.Boundaries.Add(plan.Length*i/blocks);return plan;
        }
        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        public void Validate() {
            if(Axis==null||Axis.Count==0||Boundaries.Count>15||!Finite(Length))throw new InvalidOperationException(LocalText.Select("Detect the continuous line path first.","Zuerst den durchgehenden Streckenverlauf erkennen."));
            double last=0;
            foreach(double at in Boundaries.Append(Length)){if(!Finite(at)||at-last<10-.001)throw new InvalidOperationException(LocalText.Select("Block boundaries must stay ordered and at least 10 m apart.","Blockgrenzen müssen in Reihenfolge und mindestens 10 m auseinander liegen."));last=at;}
        }
        public MarkerAnchor At(double distance) {
            if(!Finite(distance)||distance<0||distance>Length)throw new ArgumentOutOfRangeException(nameof(distance));
            double before=0;foreach(var piece in Axis){double size=piece.Section.To-piece.Section.From;if(distance<=before+size+.001)return new MarkerAnchor{Segment=piece.Section.Segment,Meters=piece.Reverse?piece.Section.To-(distance-before):piece.Section.From+distance-before};before+=size;}
            throw new InvalidOperationException(LocalText.Select("Block position is outside the line.","Blockposition liegt außerhalb der Strecke."));
        }
        public bool ReverseAt(double distance) {double before=0;foreach(var p in Axis){before+=p.Section.To-p.Section.From;if(distance<=before+.001)return p.Reverse;}throw new ArgumentOutOfRangeException(nameof(distance));}
        public List<DirectedRouteSection> Slice(double from,double to) {
            var result=new List<DirectedRouteSection>();double before=0;
            foreach(var p in Axis) {
                double size=p.Section.To-p.Section.From,lo=Math.Max(from,before)-before,hi=Math.Min(to,before+size)-before;
                if(hi>lo+.001)result.Add(new DirectedRouteSection{Reverse=p.Reverse,Section=new TrackSection{Segment=p.Section.Segment,From=p.Reverse?p.Section.To-hi:p.Section.From+lo,To=p.Reverse?p.Section.To-lo:p.Section.From+hi}});
                before+=size;
            }
            return result;
        }
        public double? Position(MarkerAnchor anchor) {
            double before=0;foreach(var p in Axis){if(p.Section.Segment==anchor.Segment&&anchor.Meters>=p.Section.From-.001&&anchor.Meters<=p.Section.To+.001)return before+(p.Reverse?p.Section.To-anchor.Meters:anchor.Meters-p.Section.From);before+=p.Section.To-p.Section.From;}return null;
        }
        public void IncludeExisting(IEnumerable<MarkerRecord> markers) {
            var existing=markers.Where(m=>m.Kind==MarkerRecord.Main).Select(m=>Position(m.Anchor)).Where(d=>d>0.01&&d<Length-.01).Select(d=>d.Value).Distinct().OrderBy(d=>d).ToList();
            if(existing.Count>Boundaries.Count)throw new InvalidOperationException(LocalText.Select("More existing signal boundaries than requested. Increase the block count or use existing signals.","Mehr vorhandene Signalgrenzen als angefordert. Blockanzahl erhöhen oder vorhandene Signale verwenden."));
            foreach(double at in existing) {if(Boundaries.Count>0){double nearest=Boundaries.OrderBy(d=>Math.Abs(d-at)).First();Boundaries.Remove(nearest);}}
            Boundaries.AddRange(existing);Boundaries=Boundaries.OrderBy(d=>d).ToList();Validate();
        }
    }
}
