using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;

static class SharedCircuitTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static MarkerRecord Signal()=>new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name="RS-009",Kind=MarkerRecord.Shunt,CircuitVariant=1,CircuitNumber=9,Anchor=new MarkerAnchor{Segment="line",Meters=25}};
    static MarkerRecord Point(string node="W1",int arm=0)=>new MarkerRecord{Id=TrackCircuit.PointId(node,arm),Name="GFM-B-W001-S",Kind=MarkerRecord.CircuitB,Anchor=new MarkerAnchor{Segment="line",Meters=25}};
    static PlanCut Cut(MarkerRecord row)=>new PlanCut{Segment=row.Anchor.Segment,Meters=row.Anchor.Meters,Signal=row.IsSignal?row.Id:null,Kind=row.Kind,Boundary=row.Id,BoundaryCode=CircuitIdentity.Caption(row),BoundaryKind=CircuitIdentity.Variant(row)};
    public static void Run() {
        var signal=Signal();var point=Point();var sharing=SharedCircuitBoundary.Resolve(new[]{signal},new[]{point});var effective=sharing.Signals[signal.Id];
        Check(sharing.PointSignals[point.Id]==signal.Id&&effective.SharedPointBoundary&&CircuitIdentity.Variant(effective)==2&&CircuitIdentity.Caption(effective)=="GFM-A/B-009","an exact shared signal/point boundary uses one B-shaped housing with the combined A/B identity");
        Check(!signal.SharedPointBoundary&&signal.CircuitVariant==1&&signal.Name=="RS-009"&&point.CircuitNumber==0&&point.Name=="GFM-B-W001-S","sharing leaves persistent signal code, original A configuration and point identity untouched");
        var alias=sharing.PointAlias(point);
        Check(alias.Id==point.Id&&alias.PhysicalId==signal.Id&&alias.Anchor.Meters==point.Anchor.Meters&&CircuitIdentity.Caption(alias)=="GFM-A/B-009"&&point.PhysicalId==null,"existing point references resolve to the combined box without replacing their stable point-arm IDs");
        var otherPoint=Point("W2",1);var multiple=SharedCircuitBoundary.Resolve(new[]{signal},new[]{otherPoint,point});
        Check(multiple.Signals.Count==1&&multiple.PointSignals.Count==2&&multiple.PointSignals.Values.All(id=>id==signal.Id)&&CircuitIdentity.Caption(multiple.Signals[signal.Id])=="GFM-A/B-009","multiple point arms can share the same physical box while its A/B label is listed once");
        var near=point.Clone();near.Anchor.Meters+=.01;
        Check(SharedCircuitBoundary.Resolve(new[]{signal},new[]{near}).PointSignals.Count==0,"mere proximity never snaps a point boundary to a different logical signal position");
        var nativeSignal=Signal();nativeSignal.Anchor.Meters=25.123456789;var nativePoint=Point();nativePoint.Anchor.Meters=(float)nativeSignal.Anchor.Meters;
        Check(SharedCircuitBoundary.Resolve(new[]{nativeSignal},new[]{nativePoint}).PointSignals.Count==1,"a native Location float round trip retains the same logical boundary without requiring identical stored double bits");
        near=point.Clone();near.Anchor.Segment="parallel";
        Check(SharedCircuitBoundary.Resolve(new[]{signal},new[]{near}).PointSignals.Count==0,"equal distances on parallel segments cannot share a detection boundary");
        var singleC=point.Clone();singleC.Kind=MarkerRecord.CircuitC;
        var signalC=signal.Clone();signalC.CircuitVariant=3;
        Check(SharedCircuitBoundary.Resolve(new[]{signal},new[]{singleC}).PointSignals.Count==0&&SharedCircuitBoundary.Resolve(new[]{signalC},new[]{point}).PointSignals.Count==0,"C boundaries never lose their one-sided monitoring rules through automatic A/B sharing");
        var duplicate=Signal();var free=Point();free.Id=Guid.NewGuid().ToString("N");
        Check(SharedCircuitBoundary.Resolve(new[]{signal,duplicate},new[]{point}).Signals.Count==0&&SharedCircuitBoundary.Resolve(new[]{signal,free},new[]{point}).Signals.Count==0,"multiple physical boundaries at the same cut remain ambiguous instead of being arbitrarily merged");
        var disabled=signal.Clone();disabled.CircuitVariant=0;var fallback=point.Clone();fallback.Name="W001";
        Check(SharedCircuitBoundary.Resolve(new[]{disabled},new[]{point}).Signals.Count==0&&SharedCircuitBoundary.Resolve(new[]{signal},new[]{fallback}).Signals.Count==0,"inactive signal detection and unresolved point geometry cannot create a shared box");
        var unshared=SharedCircuitBoundary.Resolve(new[]{signal},Array.Empty<MarkerRecord>());
        Check(unshared.Signals.Count==0&&unshared.PointAlias(point)==point&&CircuitIdentity.Caption(signal)=="GFM-A-009","removing point monitoring restores ordinary A presentation without a migration or new number");
        var serialized=JsonConvert.SerializeObject(effective);var restored=JsonConvert.DeserializeObject<MarkerRecord>(serialized);
        Check(effective.Clone().SharedPointBoundary&&!serialized.Contains("SharedPointBoundary")&&!restored.SharedPointBoundary&&restored.Id==signal.Id&&restored.CircuitNumber==9,"the computed A/B presentation survives local cloning but never replaces persistent marker data during serialization");
        var end=Point();end.Id=Guid.NewGuid().ToString("N");end.Name="GFM-B-020";end.CircuitNumber=20;end.Anchor.Meters=90;
        var input=new NetworkInput{World="shared",Nodes=new List<PlanNode>{new PlanNode{Id="a"},new PlanNode{Id="z",X=100}},Edges=new List<PlanEdge>{new PlanEdge{Id="line",A="a",B="z",Length=100}},Cuts=new List<PlanCut>{Cut(signal),Cut(point),Cut(end)}};
        Check(!NetworkPlan.Build(input,draw:false).Blocks.Values.Any(b=>b.IsCircuit),"regression fixture exposes the former loss of a detection section when A and point-B occupy one cut");
        input.Cuts=new List<PlanCut>{Cut(effective),Cut(end)};var result=NetworkPlan.Build(input);var block=result.Blocks.Values.Single(b=>b.IsCircuit);
        Check(TrackCircuit.Pair(block.BoundaryA,block.BoundaryB)==TrackCircuit.Pair(signal.Id,end.Id)&&TrackCircuit.EndsMatch(block,block.BoundaryA==signal.Id?alias:end,block.BoundaryB==signal.Id?alias:end),"one canonical A/B cut produces a monitored section while old point endpoints still resolve to the same real limits");
        var old=block.Clone();old.Id="c:"+Guid.NewGuid().ToString("N");old.Name="Existing point section";if(old.BoundaryA==signal.Id)old.BoundaryA=point.Id;else old.BoundaryB=point.Id;
        var library=new DeskLibrary{WorldKey="shared"};library.Blocks[old.Id]=old;
        var preserved=NetworkRecognition.PreserveCircuitIds(library,result,id=>sharing.PointSignals.TryGetValue(id,out var canonical)?canonical:id);
        Check(preserved.Blocks.ContainsKey(old.Id)&&preserved.Blocks[old.Id].Name==old.Name&&preserved.Blocks.Values.Count(b=>b.IsCircuit)==1&&old.BoundaryA==library.Blocks[old.Id].BoundaryA,"canonical A/B recognition reuses existing point-section IDs and names instead of creating a second monitored section");
    }
}
