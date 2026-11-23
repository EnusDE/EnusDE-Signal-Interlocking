using System;
namespace EnusDE.Runenwacht {
    // Visual gain only: never selects a colour or operating aspect. No far-distance cutoff.
    public readonly struct CrystalEmission {
        public readonly float Body,Edge,Vein;
        public CrystalEmission(float body,float edge,float vein){Body=body;Edge=edge;Vein=vein;}
    }
    public static class CrystalAppearance {
        public const float NearMeters=12,FarMeters=80;
        public static CrystalEmission For(float distance,float level,float strength,CrystalChannel channel) {
            var light=For(distance,level,strength);if(channel!=CrystalChannel.White)return light;
            if(float.IsNaN(distance)||float.IsInfinity(distance)||distance<0)distance=0;
            float t=Math.Max(0,Math.Min(1,(distance-NearMeters)/(FarMeters-NearMeters)));t=t*t*(3-2*t);
            return new CrystalEmission(light.Body*(1+t),light.Edge*(1+t),light.Vein*(1+t));
        }
        public static CrystalEmission For(float distance,float level,float strength) {
            if(float.IsNaN(level)||float.IsInfinity(level)||float.IsNaN(strength)||float.IsInfinity(strength)||level<=0||strength<=0)return new CrystalEmission(0,0,0);
            level=Math.Min(1,level);strength=Math.Min(4,strength);
            if(float.IsNaN(distance)||float.IsInfinity(distance)||distance<0)distance=0;
            float t=Math.Max(0,Math.Min(1,(distance-NearMeters)/(FarMeters-NearMeters)));t=t*t*(3-2*t);
            float gain=level*strength;
            return new CrystalEmission((.16f+3.84f*t)*gain,(.8f+1.6f*t)*gain,(1.4f+1.6f*t)*gain);
        }
    }
}
