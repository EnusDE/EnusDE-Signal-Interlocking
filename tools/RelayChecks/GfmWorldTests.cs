using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using EnusDE.Signaltechnik;
static class GfmWorldTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static TrackBlock Block(string id,string a,string b,string segment,double from,double to)=>new(){Id=id,Name=id,BoundaryA=a,BoundaryB=b,Path=new(){new(){Section=new(){Segment=segment,From=from,To=to}}}};
    static Dictionary<string,int> Pairwise(IReadOnlyCollection<TrackBlock> blocks) {
        var sorted=blocks.OrderBy(b=>b.Id,StringComparer.Ordinal).ToArray();
        var result=new Dictionary<string,int>();
        for(int i=0;i<sorted.Length;i++) {
            var use=new int[GfmWorldPalette.Count];
            for(int j=0;j<i;j++)if(GfmWorldPalette.Adjacent(sorted[i],sorted[j]))use[result[sorted[j].Id]]++;
            result.Add(sorted[i].Id,Enumerable.Range(0,GfmWorldPalette.Count).OrderBy(colour=>use[colour]).ThenBy(colour=>(colour+i)%GfmWorldPalette.Count).First());
        }
        return result;
    }

    static void CheckScheduling() {
        using var overlay=new GfmWorldOverlay();
        overlay.Begin(null,Array.Empty<TrackBlock>(),new Dictionary<string,int>(),default,null,null);
        Check(overlay.Busy&&overlay.Drawn==0,"world overlay begins with pending work instead of drawing in the requesting frame");
        overlay.Step();
        Check(!overlay.Busy&&overlay.Error==null&&overlay.Drawn==0,"the empty world view completes with no Unity objects");
        overlay.Begin(null,new[]{Block("off","A","B","rail",0,15)},new Dictionary<string,int>(),default,null,null);
        overlay.Dispose();
        overlay.Step();
        Check(!overlay.Busy&&overlay.Error==null&&overlay.Drawn==0,"deactivating the world overlay cancels pending geometry before any native track read");
    }

    static void CheckDrawOrder() {
        var type=typeof(GfmWorldOverlay).GetNestedType("Piece",BindingFlags.NonPublic);
        object Piece(long key,bool selected,float distance)=>Activator.CreateInstance(type,key,"section",default(UnityEngine.Vector3),default(UnityEngine.Vector3),false,selected,distance);
        var compare=type.GetMethod("CompareTo");
        int Order(object a,object b)=>(int)compare.Invoke(a,new[]{b});
        object selected=Piece(9,true,400),near=Piece(1,false,25),far=Piece(2,false,200),sameDistance=Piece(3,false,25);
        Check(Order(selected,near)<0&&Order(near,far)<0&&Order(near,sameDistance)<0,
            "world overlay draws the selected pieces first, then the nearest pieces with deterministic ties");
    }

    public static void Run() {
        var a=Block("a","A","B","rail",0,20);var b=Block("b","B","C","rail",20,40);var c=Block("c","D","E","parallel",0,20);
        var first=GfmWorldPalette.Assign(new[]{a,b,c});var reverse=GfmWorldPalette.Assign(new[]{c,b,a});
        Check(first.All(p=>reverse[p.Key]==p.Value),"GFM section colours are deterministic independent of dictionary/input order");
        Check(first[a.Id]!=first[b.Id]&&GfmWorldPalette.Adjacent(a,b)&&!GfmWorldPalette.Adjacent(a,c),"adjacent GFM sections differ in colour while parallel track is not treated as the same physical section");
        var overlap=Block("overlap","F","G","rail",10,30);Check(GfmWorldPalette.Adjacent(a,overlap),"overlapping physical GFM intervals also compete for different identity colours");
        var distant=Block("distant","X","Y","other",50,60);distant.BoundaryA="B";Check(GfmWorldPalette.Adjacent(a,distant),"shared boundary separates adjacent sections across native segment IDs");
        var chain=Enumerable.Range(0,50).Select(i=>Block(i.ToString("D3"),"end"+i,"end"+(i+1),"chain",i*10,(i+1)*10)).ToArray();var colours=GfmWorldPalette.Assign(chain);
        Check(colours.Values.All(v=>v>=0&&v<GfmWorldPalette.Count)&&colours.Values.Distinct().Count()<chain.Length,"finite section identity palette can repeat without assigning operational meanings");
        Check(Enumerable.Range(0,49).All(i=>colours[chain[i].Id]!=colours[chain[i+1].Id]),"long chain of neighbouring sections remains distinguishable despite repeated colours");
        Check(a.Path.Single().Section.From==0&&a.Path.Single().Section.To==20&&a.BoundaryA=="A"&&a.BoundaryB=="B","colour assignment preserves saved boundaries and physical detection intervals");
        Check(GfmWorldPalette.Assign(Array.Empty<TrackBlock>()).Count==0,"empty installations produce no invented GFM coverage");
        var varied=Enumerable.Range(0,200).Select(i=>Block(i.ToString("D3"),i%4==0?null:"A"+(i%31),"B"+(i%41),"track"+(i%9),i%67,i%67+17)).ToArray();
        varied[4].Guards.Add(new TrackSection{Segment=varied[3].Path[0].Section.Segment,From=0,To=90});
        varied[4].Guards.Add(varied[4].Guards[0].Clone());
        var expected=Pairwise(varied);
        Check(GfmWorldPalette.Assign(varied.Reverse()).All(pair=>expected[pair.Key]==pair.Value),
            "indexed world GFM colours agree with pairwise colouring for repeated shared boundaries, guarded overlaps and many independent segments");
        using var cancellation=new CancellationTokenSource();
        cancellation.Cancel();
        bool cancelled=false;
        try{GfmWorldPalette.Assign(varied,cancellation.Token);}catch(OperationCanceledException){cancelled=true;}
        Check(cancelled,"cancelling the background palette stops a stale GFM catalogue calculation");
        CheckScheduling();
        CheckDrawOrder();
    }
}
