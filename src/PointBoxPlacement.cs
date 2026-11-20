using System;
using System.IO;

namespace EnusDE.Signaltechnik {
    public sealed class PointBoxPlacement {
        public double Along,Lateral,Height,Yaw;
        public int Variant=2;
        public bool FixedHeight,GroundSupport=true,DirectionFlip;
        public PointBoxPlacement Clone()=>(PointBoxPlacement)MemberwiseClone();
        public void Validate() {
            if(!Finite(Along)||Math.Abs(Along)>10||!Finite(Lateral)||Math.Abs(Lateral)>5||
                !Finite(Height)||Height< -1||Height>2||!Finite(Yaw)||Math.Abs(Yaw)>180||Variant<1||Variant>3)throw new InvalidDataException(LocalText.Select("Invalid point boundary position or type (B/C).","Ungültige Weichengrenzposition oder Bauart (B/C)."));
        }
        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        public MarkerRecord Apply(MarkerRecord boundary) {
            Validate();var row=Appearance(boundary);
            row.CircuitAnchor=boundary.Anchor.Clone();
            row.Anchor.Meters+=Along;row.Anchor.Lateral=Lateral;
            row.Anchor.Height=Height;
            row.Anchor.FixedHeight=FixedHeight;row.Anchor.GroundSupport=GroundSupport;
            return row;
        }
        public MarkerRecord Appearance(MarkerRecord boundary) {
            Validate();var row=boundary.Clone();row.Kind=Variant==3?MarkerRecord.CircuitC:MarkerRecord.CircuitB;row.CircuitReverse=DirectionFlip;row.CircuitYaw=Yaw;row.CircuitSupport=GroundSupport;row.VisualRevision=Variant;
            if(row.Name.StartsWith("GFM-A-",StringComparison.Ordinal)||row.Name.StartsWith("GFM-B-",StringComparison.Ordinal)||row.Name.StartsWith("GFM-C-",StringComparison.Ordinal))
                row.Name="GFM-"+(Variant==3?"C":"B")+row.Name.Substring(5);
            return row;
        }
        public static PointBoxPlacement Default(MarkerRecord boundary)=>new PointBoxPlacement{Lateral=boundary.Anchor.Lateral};
    }
}
