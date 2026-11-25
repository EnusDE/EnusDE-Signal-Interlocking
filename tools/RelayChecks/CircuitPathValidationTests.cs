using System;
using System.Collections.Generic;
using System.Linq;
using EnusDE.Signaltechnik;

static class CircuitPathValidationTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static MarkerRecord End(string segment,double meters)=>new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Kind=MarkerRecord.CircuitB,Anchor=new MarkerAnchor{Segment=segment,Meters=meters}};
    static DirectedRouteSection Piece(string segment,double from,double to,bool reverse=false)=>new DirectedRouteSection{Section=new TrackSection{Segment=segment,From=from,To=to},Reverse=reverse};
    static TrackBlock Block(MarkerRecord a,MarkerRecord b,params DirectedRouteSection[] path)=>new TrackBlock{Id="c:"+Guid.NewGuid().ToString("N"),Name="Test",BoundaryA=a.Id,BoundaryB=b.Id,Path=path.ToList()};
    public static void Run() {
        var graph=new NetworkInput{World="path-validation",Nodes=new List<PlanNode>{new PlanNode{Id="a"},new PlanNode{Id="b"}},Edges=new List<PlanEdge>{new PlanEdge{Id="S",A="a",B="b",Length=500}}};
        var a=End("S",25.123456789);var b=End("S",380.87654321);
        graph.Cuts.AddRange(new[]{new PlanCut{Segment="S",Meters=(float)a.Anchor.Meters,Boundary=a.Id,BoundaryKind=2},new PlanCut{Segment="S",Meters=(float)b.Anchor.Meters,Boundary=b.Id,BoundaryKind=2}});
        var block=NetworkPlan.Build(graph,draw:false).Blocks.Values.Single(p=>p.IsCircuit);
        var from=block.BoundaryA==a.Id?a:b;var to=block.BoundaryB==b.Id?b:a;
        var found=TrackCircuit.Find(graph,from,to,out _).Single();
        Check(!found.Sections.Select(p=>p.Section.Key).SequenceEqual(block.Path.Select(p=>p.Section.Key)),"regression fixture reproduces bytewise GFM mismatch after native float position round trip");
        Check(CircuitPathValidation.Error(graph,block,from,to)==null,"physically identical saved GFM path survives native float/double rounding without a false turnout error");
        var link=new DirectionBlockLink{Id=block.Id,A="a",B="b",Path=block.Path,Error=CircuitPathValidation.Error(graph,block,from,to)};
        var authority=new DirectionBlockAuthority{Enabled=true,Occupancy=s=>TrackOccupancy.Clear,Blocks=DirectionBlockBuilder.Build(new[]{link},Array.Empty<string>())};
        var route=new RouteDefinition{Entry="entry",Exit="exit",Path=block.Path};
        using(var ticket=authority.Reserve(new[]{route}))ticket.Commit();
        Check(authority.Leases.Count==1,"a valid rounded GFM section can reserve direction through the actual block admission path");
        var split=Block(a,b,Piece("S",a.Anchor.Meters,100),Piece("S",100,200),Piece("S",200,b.Anchor.Meters));
        Check(CircuitPathValidation.Error(graph,split,a,b)==null,"contiguous saved fragments of the same physical segment do not become an ambiguous GFM path");
        var inactive=a.Clone();inactive.Kind=MarkerRecord.Main;inactive.CircuitVariant=0;
        Check(CircuitPathValidation.Error(graph,split,inactive,b)!=null,"a leftover signal endpoint without an active GFM cannot masquerade as a valid physical detection boundary");
        var broken=split.Clone();broken.Path[1].Section.From+=.1;
        Check(CircuitPathValidation.Diagnose(graph,broken,a,b).Fault==CircuitPathFault.Interrupted,"a real gap between saved GFM fragments remains rejected");
        broken=split.Clone();broken.Path[1].Reverse=true;
        Check(CircuitPathValidation.Error(graph,broken,a,b)!=null,"a direction reversal inside a saved physical segment remains rejected");
        var moved=b.Clone();moved.Anchor.Meters+=.1;
        Check(CircuitPathValidation.Error(graph,split,a,moved)!=null,"moved GFM endpoints beyond positional tolerance invalidate the saved section");
        graph.Cuts.Add(new PlanCut{Segment="S",Meters=150,Boundary=Guid.NewGuid().ToString("N"),BoundaryKind=3,BoundaryCode="C-limit"});
        Check(CircuitPathValidation.Diagnose(graph,split,a,b).Fault==CircuitPathFault.SkippedBoundary&&CircuitPathValidation.Error(graph,split,a,b)?.Contains("C-limit")==true,"a newly inserted C boundary is reported by identity instead of being mistaken for a turnout");
        graph.Cuts.RemoveAt(2);graph.Edges.Clear();
        Check(CircuitPathValidation.Diagnose(graph,split,a,b).Fault==CircuitPathFault.InvalidInterval,"unavailable native track cannot be accepted as a valid saved GFM path");
        PointSections();LongSection();
    }
    static void PointSections() {
        var graph=new NetworkInput{Nodes=new List<PlanNode>{new PlanNode{Id="a"},new PlanNode{Id="W001",Point=true,Common="stem"},new PlanNode{Id="b"},new PlanNode{Id="c"}},Edges=new List<PlanEdge>{new PlanEdge{Id="stem",A="a",B="W001",Length=100},new PlanEdge{Id="normal",A="W001",B="b",Length=100},new PlanEdge{Id="branch",A="W001",B="c",Length=100}}};
        var a=End("stem",20);var b=End("normal",80);var block=Block(a,b,Piece("stem",20,100),Piece("normal",0,80));
        string error=CircuitPathValidation.Error(graph,block,a,b);
        Check(CircuitPathValidation.Diagnose(graph,block,a,b).Fault==CircuitPathFault.PointCrossing&&error?.Contains("W001")==true,"a circuit crossing an actual point is rejected with the affected point identity");
        var authority=new DirectionBlockAuthority{Enabled=true,Occupancy=s=>TrackOccupancy.Clear,Blocks=DirectionBlockBuilder.Build(new[]{new DirectionBlockLink{Id=block.Id,A="a",B="b",Path=block.Path,Error=error}},Array.Empty<string>())};
        Check(Reject(()=>authority.Reserve(new[]{new RouteDefinition{Path=block.Path}}))&&authority.Leases.Count==0,"actual point-crossing GFM errors still prevent block reservation and leave no direction lease");
        var stem=End("stem",75);var normal=End("normal",25);
        Check(CircuitPathValidation.Error(graph,Block(a,stem,Piece("stem",20,75)),a,stem)==null&&CircuitPathValidation.Error(graph,Block(normal,b,Piece("normal",25,80)),normal,b)==null,
            "sections ending outside the point guard remain valid on both sides without crossing the point");
        graph.Nodes.Single(n=>n.Id=="W001").Point=false;
        Check(CircuitPathValidation.Error(graph,block,a,b)!=null,"a three-way junction with missing point metadata remains ambiguous and cannot be treated as plain track");
        graph.Edges.Single(e=>e.Id=="normal").A="unrelated";
        Check(CircuitPathValidation.Error(graph,block,a,b)!=null,"equal meter endpoints on disconnected segments cannot manufacture a GFM connection");
    }
    static void LongSection() {
        var graph=new NetworkInput();const int count=150;
        for(int i=0;i<=count;i++)graph.Nodes.Add(new PlanNode{Id="n"+i});
        for(int i=0;i<count;i++)graph.Edges.Add(new PlanEdge{Id="s"+i,A="n"+i,B="n"+(i+1),Length=100});
        var a=End("s0",10);var b=End("s149",90);var block=Block(a,b,Enumerable.Range(0,count).Select(i=>Piece("s"+i,i==0?10:0,i==count-1?90:100)).ToArray());
        Check(CircuitPathValidation.Error(graph,block,a,b)==null,"a saved 15-km GFM path validates without the proposal search's 128-edge exploration limit");
        var reversed=Block(b,a,block.Path.AsEnumerable().Reverse().Select(p=>Piece(p.Section.Segment,p.Section.From,p.Section.To,!p.Reverse)).ToArray());
        Check(CircuitPathValidation.Error(graph,reversed,b,a)==null,"long saved GFM paths validate equally in the opposite direction");
    }
}
