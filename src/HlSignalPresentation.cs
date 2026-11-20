using System;

namespace EnusDE.Signaltechnik {
    public enum HlSpeed { Stop=0, Forty=40, Sixty=60, Hundred=100, Line= int.MaxValue }

    // Ril 301.0103: upper light announces the next signal; lower yellow and strips
    // describe the speed from this signal. Original geometry, no copied DB artwork.
    public sealed class HlLampPlan {
        public string Code;
        public readonly SignalLensColor[] Lamps=new SignalLensColor[8];
        public int FlashMask;
        public SignalLensColor[] At(double seconds) {
            var result=(SignalLensColor[])Lamps.Clone();
            bool on=!double.IsNaN(seconds)&&!double.IsInfinity(seconds)&&seconds>=0&&seconds%1<.5;
            if(!on)for(int i=0;i<result.Length;i++)if((FlashMask&(1<<i))!=0)result[i]=SignalLensColor.Off;
            return result;
        }
        public static HlLampPlan Stop() {var plan=new HlLampPlan{Code="Hp 0"};plan.Lamps[2]=SignalLensColor.Red;return plan;}
        public static HlLampPlan For(HlSpeed current,HlSpeed next) {
            if(current==HlSpeed.Stop)return Stop();
            if(current!=HlSpeed.Line&&current!=HlSpeed.Forty&&current!=HlSpeed.Sixty&&current!=HlSpeed.Hundred||
                next!=HlSpeed.Stop&&next!=HlSpeed.Line&&next!=HlSpeed.Forty&&next!=HlSpeed.Sixty&&next!=HlSpeed.Hundred)return Stop();
            int column=next==HlSpeed.Line?0:next==HlSpeed.Hundred?1:next==HlSpeed.Stop?3:2;
            string[] codes=current==HlSpeed.Line?new[]{"Hl 1","Hl 4","Hl 7","Hl 10"}:
                current==HlSpeed.Hundred?new[]{"Hl 2","Hl 5","Hl 8","Hl 11"}:
                current==HlSpeed.Sixty?new[]{"Hl 3b","Hl 6b","Hl 9b","Hl 12b"}:new[]{"Hl 3a","Hl 6a","Hl 9a","Hl 12a"};
            var plan=new HlLampPlan{Code=codes[column]};
            int upper=column<2?0:1;plan.Lamps[upper]=column<2?SignalLensColor.Green:SignalLensColor.Yellow;
            if(column==1||column==2)plan.FlashMask=1<<upper;
            if(current!=HlSpeed.Line)plan.Lamps[3]=SignalLensColor.Yellow;
            if(current==HlSpeed.Sixty)plan.Lamps[6]=SignalLensColor.Yellow;
            if(current==HlSpeed.Hundred)plan.Lamps[7]=SignalLensColor.Green;
            return plan;
        }
        public static HlSpeed Announced(LocalSignalAspect aspect)=>aspect==LocalSignalAspect.Clear?HlSpeed.Line:aspect==LocalSignalAspect.Slow?HlSpeed.Forty:HlSpeed.Stop;
        public static HlLampPlan Runtime(string kind,LocalSignalAspect authority,LocalSignalAspect announced,bool combined) {
            if(!Enum.IsDefined(typeof(LocalSignalAspect),authority))return kind==MarkerRecord.Distant?For(HlSpeed.Line,HlSpeed.Stop):Stop();
            if(authority==LocalSignalAspect.Dark||authority==LocalSignalAspect.Invalid||authority==LocalSignalAspect.Kennlicht)return new HlLampPlan{Code=authority.ToString()};
            if(kind==MarkerRecord.Distant)return For(HlSpeed.Line,Announced(authority));
            if(authority==LocalSignalAspect.Shunt){var plan=Stop();plan.Code="Hp 0 + Ra 12";plan.Lamps[4]=plan.Lamps[5]=SignalLensColor.White;return plan;}
            if(authority!=LocalSignalAspect.Clear&&authority!=LocalSignalAspect.Slow)return Stop();
            return For(authority==LocalSignalAspect.Slow?HlSpeed.Forty:HlSpeed.Line,combined?Announced(announced):HlSpeed.Line);
        }
    }
}
