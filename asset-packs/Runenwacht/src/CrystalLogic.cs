using System;
using EnusDE.Signaltechnik;
namespace EnusDE.Runenwacht {
    // Each physical channel has one immutable colour. No aspect can recolour a crystal.
    public enum CrystalChannel { Red, Amber, Green, Violet, White }
    public static class CrystalLogic {
        public static CrystalChannel? Channel(string kind,LocalSignalAspect aspect) {
            if(aspect==LocalSignalAspect.Dark)return null;
            if(aspect==LocalSignalAspect.Invalid)return CrystalChannel.Red;
            if(aspect==LocalSignalAspect.Kennlicht)return CrystalChannel.White;
            if(kind==MarkerRecord.Shunt)return aspect==LocalSignalAspect.Shunt?CrystalChannel.Violet:CrystalChannel.Red;
            if(kind==MarkerRecord.Distant)return aspect==LocalSignalAspect.Clear?CrystalChannel.Green:aspect==LocalSignalAspect.Slow?CrystalChannel.Amber:CrystalChannel.Red;
            return aspect==LocalSignalAspect.Clear?CrystalChannel.Green:aspect==LocalSignalAspect.Slow?CrystalChannel.Amber:CrystalChannel.Red;
        }
    }
    // Break-before-make: a permissive crystal never overlaps a conflicting old indication.
    // New restrictive red is immediate, old permissive channels extinguish immediately.
    public sealed class CrystalTransition {
        readonly float[] levels=new float[5]; CrystalChannel? target;
        public const float FadeSeconds=.24f;
        public float Level(CrystalChannel c)=>levels[(int)c];
        public void Set(CrystalChannel? next) {
            if(next==target)return;target=next;
            for(int i=0;i<levels.Length;i++)if(!next.HasValue||i!=(int)next.Value&&i!=(int)CrystalChannel.Red)levels[i]=0;
            if(next==CrystalChannel.Red)levels[(int)CrystalChannel.Red]=1;
        }
        public void Advance(float seconds) {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return;
            if(target!=CrystalChannel.Red&&levels[(int)CrystalChannel.Red]>0){levels[(int)CrystalChannel.Red]=Math.Max(0,levels[(int)CrystalChannel.Red]-Math.Min(seconds,1)/.12f);return;}
            if(target.HasValue){int i=(int)target.Value;levels[i]=Math.Min(1,levels[i]+Math.Min(seconds,1)/FadeSeconds);}
        }
    }
}
