using System;
using System.Collections.Generic;
using System.Linq;
namespace EnusDE.Signaltechnik {
    public static class GfmWorldPalette {
        public const int Count=8;
        public static bool Adjacent(TrackBlock a,TrackBlock b)=>
            new[]{a.BoundaryA,a.BoundaryB}.Where(x=>!string.IsNullOrEmpty(x)).Intersect(new[]{b.BoundaryA,b.BoundaryB}).Any()||
            a.Sections.Any(x=>b.Sections.Any(y=>x.Segment==y.Segment&&Math.Max(x.From,y.From)<=Math.Min(x.To,y.To)+.01));
        public static Dictionary<string,int> Assign(IEnumerable<TrackBlock> blocks) {
            var ordered=blocks.OrderBy(b=>b.Id,StringComparer.Ordinal).ToArray();var result=new Dictionary<string,int>();
            for(int i=0;i<ordered.Length;i++) {
                var use=new int[Count];for(int j=0;j<i;j++)if(Adjacent(ordered[i],ordered[j]))use[result[ordered[j].Id]]++;
                result[ordered[i].Id]=Enumerable.Range(0,Count).OrderBy(c=>use[c]).ThenBy(c=>(c+i)%Count).First();
            }
            return result;
        }
    }
}
