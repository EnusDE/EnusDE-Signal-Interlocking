using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
namespace EnusDE.Signaltechnik {
    public static class GfmWorldPalette {
        public const int Count=8;
        public static bool Adjacent(TrackBlock a,TrackBlock b)=>
            new[]{a.BoundaryA,a.BoundaryB}.Where(x=>!string.IsNullOrEmpty(x)).Intersect(new[]{b.BoundaryA,b.BoundaryB}).Any()||
            a.Sections.Any(x=>b.Sections.Any(y=>x.Segment==y.Segment&&Math.Max(x.From,y.From)<=Math.Min(x.To,y.To)+.01));
        public static Dictionary<string,int> Assign(IEnumerable<TrackBlock> blocks,CancellationToken cancel=default) {
            cancel.ThrowIfCancellationRequested();
            var ordered=blocks.OrderBy(b=>b.Id,StringComparer.Ordinal).ToArray();
            var result=new Dictionary<string,int>(ordered.Length,StringComparer.Ordinal);
            var boundaries=new Dictionary<string,List<int>>(StringComparer.Ordinal);
            var segments=new Dictionary<string,List<(TrackSection Span,int Index)>>(StringComparer.Ordinal);
            var colours=new int[ordered.Length];var use=new int[Count];var neighbours=new HashSet<int>();
            for(int i=0;i<ordered.Length;i++) {
                cancel.ThrowIfCancellationRequested();
                var block=ordered[i];neighbours.Clear();Array.Clear(use,0,use.Length);
                AddBoundary(block.BoundaryA,i);AddBoundary(block.BoundaryB,i);
                foreach(var span in block.Sections) {
                    cancel.ThrowIfCancellationRequested();
                    if(!segments.TryGetValue(span.Segment,out var previous))segments.Add(span.Segment,previous=new List<(TrackSection,int)>());
                    foreach(var (other,index) in previous)
                        if(index<i&&Math.Max(span.From,other.From)<=Math.Min(span.To,other.To)+.01)neighbours.Add(index);
                    previous.Add((span,i));
                }
                foreach(int index in neighbours)use[colours[index]]++;
                int first=(Count-i%Count)%Count,best=first;
                for(int step=1;step<Count;step++){
                    int colour=(first+step)%Count;
                    if(use[colour]<use[best])best=colour;
                }
                colours[i]=best;result.Add(block.Id,best);
            }
            return result;

            void AddBoundary(string boundary,int i) {
                if(string.IsNullOrEmpty(boundary))return;
                if(!boundaries.TryGetValue(boundary,out var previous))boundaries.Add(boundary,previous=new List<int>());
                foreach(int index in previous)if(index<i)neighbours.Add(index);
                previous.Add(i);
            }
        }
    }
}
