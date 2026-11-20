using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

public static class SchematicTests {
    static void Check(bool value,string text){if(!value)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    public static NetworkInput Station() {
        var graph=new NetworkInput{World="curved-station"};
        var ids=new[]{"a","w1","w2","w3","w4","z","side","end"};
        for(int i=0;i<ids.Length;i++)graph.Nodes.Add(new PlanNode{Id=ids[i],X=Math.Sin(i*2)*900,Y=Math.Cos(i)*600});
        void Edge(string id,string a,string b)=>graph.Edges.Add(new PlanEdge{Id=id,A=a,B=b,Length=100});
        Edge("m01","a","w1");Edge("m12","w1","w2");Edge("m23","w2","w3");Edge("m34","w3","w4");Edge("m4z","w4","z");
        Edge("loop","w1","w3");Edge("siding","w2","side");Edge("yard","w4","end");
        void Point(string id,string common,string normal){var n=graph.Nodes.Single(n=>n.Id==id);n.Point=true;n.Common=common;n.Normal=normal;}
        Point("w1","m01","m12");Point("w2","m12","m23");Point("w3","m34","m23");Point("w4","m34","m4z");
        graph.Cuts.Add(new PlanCut{Segment="m23",Meters=40,Signal="main",X=-100,Y=200});
        graph.Cuts.Add(new PlanCut{Segment="loop",Meters=50,Signal="loop-signal",X=700,Y=-900});
        return graph;
    }
    public static void Export(PanelDocument panel,string file,string title) {
        var svg=new StringBuilder();int width=panel.Columns*18+40,height=panel.Rows*16+60;
        svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\"><rect width=\"100%\" height=\"100%\" fill=\"#07191e\"/><text x=\"16\" y=\"23\" fill=\"#d9e8e7\" font-family=\"sans-serif\" font-size=\"14\">{System.Security.SecurityElement.Escape(title)}</text><g transform=\"translate(20 40)\" stroke=\"#b7a457\" stroke-width=\"2\">");
        foreach(var tile in panel.Tiles)for(int d=0;d<8;d++)if((tile.Ports&(1<<d))!=0)svg.Append($"<path d=\"M{tile.X*18} {tile.Y*16}l{PanelConnections.Dx[d]*9} {PanelConnections.Dy[d]*8}\"/>");
        svg.Append("</g><g transform=\"translate(20 40)\" font-family=\"sans-serif\" font-size=\"10\" fill=\"#e0efeb\">");
        foreach(var tile in panel.Tiles.Where(t=>t.Kind==PanelTileKind.Switch||t.Kind==PanelTileKind.Signal)) {
            string name=tile.Kind==PanelTileKind.Switch?tile.Reference:tile.Reference=="main"?"MAIN":tile.Reference=="loop-signal"?"LOOP":tile.Reference;
            svg.Append($"<circle cx=\"{tile.X*18}\" cy=\"{tile.Y*16}\" r=\"3\" fill=\"{(tile.Kind==PanelTileKind.Switch?"#78b7bf":"#df6d61")}\"/><text x=\"{tile.X*18+4}\" y=\"{tile.Y*16-7}\">{System.Security.SecurityElement.Escape(name)}</text>");
        }
        svg.Append("</g></svg>");Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllText(file,svg.ToString());
    }
    public static void Run(string root) {
        var graph=Station();var result=NetworkPlan.Build(graph);var points=result.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.Switch).OrderBy(t=>t.Reference).ToArray();
        Export(result.Panel,Path.Combine(root,"build/checks/schematic-station.svg"),"Generator preview - schematic station (not a game screenshot)");
        Check(result.Missing.Count==0&&result.Drawn==result.Chains,"schematic station draws every actual compressed connection without missing or invented links");
        Check(points.Length==4&&points.Select(t=>t.Y).Distinct().Count()==1&&points.Zip(points.Skip(1),(a,b)=>Math.Sign(b.X-a.X)).Distinct().Count()==1,"successive normal main-line switches share one horizontal row in their true topological order despite strongly curved world coordinates");
        int mainY=points[0].Y;
        Check(result.Panel.Tiles.Single(t=>t.Reference=="main").Y==mainY&&result.Panel.Tiles.Single(t=>t.Reference=="loop-signal").Y!=mainY,"main-line signals remain on the central strand and the passing-loop signal occupies a separate parallel lane");
        Check(points.All(t=>PanelConnections.Count(t.Ports)==3&&(((t.Stem<<4)|(t.Stem>>4))&255)==t.Normal),"schematic switch symbols preserve three ports and opposite common and normal arms");
        Check(result.Panel.Columns<120&&result.Panel.Rows<40&&result.Panel.Tiles.Count<600,"a small station produces a compact readable diagram rather than stretching across a 512 by 320 world-coordinate canvas");
        var moved=JsonConvert.DeserializeObject<NetworkInput>(JsonConvert.SerializeObject(graph));
        foreach(var n in moved.Nodes){n.X=n.X*500+100000;n.Y=-n.Y*7;}
        foreach(var c in moved.Cuts){c.X=c.X*11-30000;c.Y=c.Y*800;}
        moved.Nodes.Reverse();moved.Edges.Reverse();moved.Cuts.Reverse();var again=NetworkPlan.Build(moved);
        Check(JsonConvert.SerializeObject(again.Panel)==JsonConvert.SerializeObject(result.Panel),"world curvature, scaling, translation and input enumeration do not arbitrarily rearrange the same schematic railway");
        var catalog=NetworkPlan.Build(graph,draw:false);
        Check(catalog.Blocks.Keys.Order().SequenceEqual(result.Blocks.Keys.Order())&&catalog.Blocks.All(p=>JsonConvert.SerializeObject(p.Value)==JsonConvert.SerializeObject(result.Blocks[p.Key])),"schematic drawing changes no physical block identities, protection paths or point guards");
        graph.Nodes.Add(new PlanNode{Id="island-a"});graph.Nodes.Add(new PlanNode{Id="island-b"});graph.Edges.Add(new PlanEdge{Id="island",A="island-a",B="island-b",Length=20});
        var disconnected=NetworkPlan.Build(graph);
        Check(disconnected.Missing.Count==0&&disconnected.Chains==result.Chains+1,"disconnected physical track components occupy separate lanes without a fabricated joining track");
        var cycle=new NetworkInput{World="cycle",Nodes=new List<PlanNode>{new PlanNode{Id="a"},new PlanNode{Id="b"},new PlanNode{Id="c"}},Edges=new List<PlanEdge>{new PlanEdge{Id="ab",A="a",B="b",Length=100},new PlanEdge{Id="bc",A="b",B="c",Length=100},new PlanEdge{Id="ca",A="c",B="a",Length=100}}};
        var loop=NetworkPlan.Build(cycle);Check(loop.Drawn==loop.Chains&&loop.Missing.Count==0,"a closed track loop remains a loop rather than duplicating or dropping its return connection");
        var longLine=new NetworkInput{World="scope"};
        for(int i=0;i<=6;i++)longLine.Nodes.Add(new PlanNode{Id="n"+i,X=i*1000});
        for(int i=0;i<6;i++)longLine.Edges.Add(new PlanEdge{Id="e"+i,A="n"+i,B="n"+(i+1),Length=1000});
        longLine.Cuts.Add(new PlanCut{Signal="first",Segment="e0",Meters=50,X=50});
        string original=JsonConvert.SerializeObject(longLine);var scoped=NetworkScope.DrawNearby(longLine,100);
        var full=NetworkPlan.Build(longLine,draw:false);
        Check(scoped.ScopeSegments==1&&scoped.Segments==6&&scoped.Blocks.Keys.Order().SequenceEqual(full.Blocks.Keys.Order()),"near-signal drawing limits the sketch while retaining the complete physical section catalogue");
        Check(scoped.Panel.Tiles.Any(t=>t.Label=="Network boundary"||t.Label=="Netzgrenze")&&scoped.Panel.Tiles.All(t=>t.BlockId==""||full.Blocks.ContainsKey(t.BlockId))&&JsonConvert.SerializeObject(longLine)==original,"scope end caps are visibly marked and refer to complete physical blocks without changing captured track data");
        longLine.Nodes[1].Point=true;longLine.Nodes[1].Common="e0";longLine.Nodes[1].Normal="e1";longLine.Nodes.Add(new PlanNode{Id="spur"});longLine.Edges.Add(new PlanEdge{Id="spur",A="n1",B="spur",Length=100});
        var cap=NetworkScope.AroundSignals(longLine,100);
        Check(cap.Nodes.Where(n=>n.ScopeBoundary).All(n=>!n.Point)&&cap.Edges.Count==1,"a drawing boundary beyond the selected radius does not invent a partial turnout or connect unrelated branches");
    }
}
