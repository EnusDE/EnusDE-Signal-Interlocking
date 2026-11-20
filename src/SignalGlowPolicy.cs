using System;
namespace EnusDE.Signaltechnik {
    public sealed class SignalGlowOptions {
        public bool Enabled=true,Halo=true;
        public double Radiance=6,HaloStrength=.75;
        public SignalGlowOptions Normalize(){Radiance=Bound(Radiance,1,24,6);HaloStrength=Bound(HaloStrength,.1,2,.75);return this;}
        static double Bound(double value,double min,double max,double fallback)=>double.IsNaN(value)||double.IsInfinity(value)?fallback:Math.Max(min,Math.Min(max,value));
        public static double HaloAlpha(double x,double y){double r=Math.Sqrt(x*x+y*y);return r>=1?0:Math.Pow(1-r,2.5)*.45;}
        public static bool ShowHalo(bool enabled,bool halo,bool preview,bool cameraKnown,double squaredDistance,double facingDot=1)=>enabled&&halo&&!preview&&cameraKnown&&facingDot>0&&!double.IsNaN(squaredDistance)&&squaredDistance>=0&&squaredDistance<=250*250;
    }
    public enum SignalLensColor { Off,Red,Green,Yellow,White }
    public static class SignalLensPlan {
        public static SignalLensColor[] For(string kind,LocalSignalAspect aspect) {
            if(kind!=MarkerRecord.Main&&kind!=MarkerRecord.Shunt&&kind!=MarkerRecord.Distant)throw new ArgumentException("Unbekannter Signalkopf.");
            if(!Enum.IsDefined(typeof(LocalSignalAspect),aspect))aspect=LocalSignalAspect.Stop;
            var lamps=new SignalLensColor[kind==MarkerRecord.Main?3:4];if(aspect==LocalSignalAspect.Dark||aspect==LocalSignalAspect.Invalid||aspect==LocalSignalAspect.Kennlicht)return lamps;
            if(kind==MarkerRecord.Main){if(aspect==LocalSignalAspect.Stop||aspect==LocalSignalAspect.Shunt)lamps[2]=SignalLensColor.Red;else{lamps[0]=SignalLensColor.Green;if(aspect==LocalSignalAspect.Slow)lamps[1]=SignalLensColor.Yellow;}}
            else if(kind==MarkerRecord.Shunt){if(aspect==LocalSignalAspect.Shunt){lamps[2]=lamps[3]=SignalLensColor.White;}else lamps[0]=lamps[1]=SignalLensColor.Red;}
            else if(kind==MarkerRecord.Distant){if(aspect==LocalSignalAspect.Clear)lamps[2]=lamps[3]=SignalLensColor.Green;else if(aspect==LocalSignalAspect.Slow){lamps[0]=SignalLensColor.Yellow;lamps[3]=SignalLensColor.Green;}else lamps[0]=lamps[1]=SignalLensColor.Yellow;}
            else throw new ArgumentException("Unbekannter Signalkopf.");return lamps;
        }
    }
}
