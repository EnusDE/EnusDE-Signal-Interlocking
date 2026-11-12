using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using EnusDE.Signaltechnik;

public static class NetworkTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    public static void Run(string mapPath) {
        var input=new NetworkInput{World="network"};input.Nodes.AddRange(new[]{new PlanNode{Id="a",X=0,Y=0},new PlanNode{Id="b",X=1,Y=0},new PlanNode{Id="w",X=2,Y=0,Point=true,Common="bw",Normal="wc"},new PlanNode{Id="c",X=3,Y=0},new PlanNode{Id="d",X=3,Y=1}});input.Edges.AddRange(new[]{new PlanEdge{Id="ab",A="a",B="b",Length=100},new PlanEdge{Id="bw",A="b",B="w",Length=100},new PlanEdge{Id="wc",A="w",B="c",Length=100},new PlanEdge{Id="wd",A="w",B="d",Length=100}});
        var result=NetworkPlan.Build(input);Check(result.Chains==3&&result.Blocks.Count==4&&result.Drawn==3&&result.Missing.Count==0,"automatic plan joins degree-two track, recognizes native points and draws all toy-network connections");
        input.Cuts.Add(new PlanCut{Segment="ab",Meters=40,X=.4,Y=0,Signal="S"});result=NetworkPlan.Build(input);Check(result.Chains==4&&result.Blocks.Values.Where(b=>b.Point==null).Sum(b=>b.Path.Sum((DirectedRouteSection p)=>p.Section.To-p.Section.From))==400&&result.Panel.Tiles.Any(t=>t.Kind==PanelTileKind.Signal&&t.Reference=="S"),"main/shunt signal splits actual track into gapless occupancy groups and creates a bound button");
        Check(result.Blocks["p:w"].Guards.Count==3&&result.Blocks["p:w"].Guards.All(s=>s.To-s.From==25),"point occupancy includes all three physical protection legs");
        var catalog=NetworkPlan.Build(input,draw:false);Check(catalog.Blocks.Keys.Order().SequenceEqual(result.Blocks.Keys.Order()),"catalog-only refresh preserves stable section identifiers independently of layout");
        if(string.IsNullOrEmpty(mapPath))return;var map=JObject.Parse(File.ReadAllText(mapPath));var nodes=(JObject)map["graph"]["nodes"];var edges=(JObject)map["graph"]["segments"];input=new NetworkInput{World="standard-map"};foreach(var p in nodes.Properties()){var position=(JArray)p.Value["position"];input.Nodes.Add(new PlanNode{Id=p.Name,X=(double)position[0],Y=(double)position[2]});}foreach(var p in edges.Properties()){string a=(string)p.Value["a"],b=(string)p.Value["b"];if(a==b)continue;var na=input.Nodes.First(n=>n.Id==a);var nb=input.Nodes.First(n=>n.Id==b);input.Edges.Add(new PlanEdge{Id=p.Name,A=a,B=b,Length=Math.Max(.1,Math.Sqrt(Math.Pow(na.X-nb.X,2)+Math.Pow(na.Y-nb.Y,2)))});}foreach(var n in input.Nodes){var connected=input.Edges.Where(e=>e.A==n.Id||e.B==n.Id).ToArray();if(connected.Length==3){n.Point=true;n.Common=connected[0].Id;n.Normal=connected[1].Id;}}
        var watch=System.Diagnostics.Stopwatch.StartNew();result=NetworkPlan.Build(input);watch.Stop();System.Console.WriteLine("NETWORK STANDARD: "+result.Segments+" segments, "+result.Chains+" chains, "+result.Drawn+" drawn, "+result.Missing.Count+" missing, "+result.Panel.Tiles.Count+" tiles, "+watch.Elapsed.TotalSeconds.ToString("0.00")+" seconds");
        Check(result.Blocks.Values.Where(b=>b.Point==null).Sum(b=>b.Path.Count)==input.Edges.Count,"real standard map: every loaded native segment appears in exactly one automatic block path");System.Console.WriteLine(string.Join(",",result.Missing.GroupBy(s=>s.Substring(0,s.IndexOf(':'))).Select(g=>g.Key+"="+g.Count())));Check(result.Missing.Count==0,"real standard map: all compressed network connections are drawn without missing chains");
    }
}
