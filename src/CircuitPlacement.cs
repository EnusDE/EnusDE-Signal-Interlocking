using System;

namespace EnusDE.Signaltechnik {
    public static class CircuitPlacement {
        public static double? AttachedLateral(double signal,double scale) {
            double side=signal<0?-1:1,gap=.8*scale+.15,railClearance=1.1+.27*scale;
            double outside=side*Math.Max(railClearance,Math.Abs(signal)+gap);
            if(Math.Abs(outside)<=5)return outside;
            double inside=signal-side*gap;
            return Math.Sign(inside)==side&&Math.Abs(inside)>=railClearance?(double?)inside:null;
        }
    }
}
