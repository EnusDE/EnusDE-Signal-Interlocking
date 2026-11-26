using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
static class GfmWorldTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static TrackBlock Block(string id,string a,string b,string segment,double from,double to)=>new(){Id=id,Name=id,BoundaryA=a,BoundaryB=b,Path=new(){new(){Section=new(){Segment=segment,From=from,To=to}}}};
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
    }
}
