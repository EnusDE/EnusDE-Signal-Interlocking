using System;

namespace EnusDE.Signaltechnik {
    public static class MarkerGroundPlacement {
        public static double Level(double railLevel,double correction,double? terrain,bool fixedHeight) {
            if(fixedHeight||!terrain.HasValue||double.IsNaN(terrain.Value)||double.IsInfinity(terrain.Value)||Math.Abs(terrain.Value-railLevel)>1.35)
                return railLevel+correction;
            return terrain.Value+correction;
        }
        public static double SupportDepth(double top,double? terrain) {
            if(!terrain.HasValue||double.IsNaN(terrain.Value)||double.IsInfinity(terrain.Value))return 0;
            double depth=top-terrain.Value+.08;
            return depth>.08&&depth<=5?depth:0;
        }
    }
}
