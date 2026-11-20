using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class GfmRepairTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action run){try{run();return false;}catch(InvalidOperationException){return true;}}
    static string Id()=>Guid.NewGuid().ToString("N");
    static DirectedRouteSection P(double a,double b,string segment="S")=>new DirectedRouteSection{Section=new TrackSection{Segment=segment,From=a,To=b}};
    static TrackBlock B(string id,string a,string b,double from,double to)=>new TrackBlock{Id=id,Name=id,BoundaryA=a,BoundaryB=b,Path=new List<DirectedRouteSection>{P(from,to)}};
    public static void Run() {
        string a=Id(),gone=Id(),c=Id(),key=new string('c',64);
        var start=new MarkerRecord{Id=a,Name="HS-018",Kind=MarkerRecord.Main,CircuitVariant=1,CircuitNumber=50,Anchor=new MarkerAnchor{Segment="S",Meters=0}};
        var end=new MarkerRecord{Id=c,Name="HS-058",Kind=MarkerRecord.Main,CircuitVariant=1,CircuitNumber=51,Anchor=new MarkerAnchor{Segment="S",Meters=200}};
        var raw=new Dictionary<string,MarkerRecord>{{a,start},{c,end}};
        MarkerRecord Raw(string id)=>raw.TryGetValue(id,out var m)?m:null;
        MarkerRecord Active(string id)=>CircuitIdentity.Enabled(Raw(id))?Raw(id):null;
        GfmBoundaryFault Fault(string id)=>GfmFaults.Classify(id,Raw(id),Active(id));
        Check(Fault(gone)==GfmBoundaryFault.Missing,"a deleted GFM boundary is distinguished from occupancy and an active detector");
        var plan=end.Clone();plan.Planned=true;Check(GfmFaults.Classify(c,plan,null)==GfmBoundaryFault.Planned&&GfmFaults.Caption(c,plan,null).Contains("051"),"an unfinished detector keeps its human GFM code and is classified as a construction plan");
        var disabled=end.Clone();disabled.CircuitVariant=0;Check(GfmFaults.Classify(c,disabled,null)==GfmBoundaryFault.Disabled,"a detector switched off on an existing signal is not mislabeled as a deleted object");
        Check(GfmFaults.Classify(c,end,end,new MarkerAnchor{Segment="S",Meters=195})==GfmBoundaryFault.Moved&&GfmFaults.Classify("w:W:0",null,null)==GfmBoundaryFault.PointUnavailable,"moved boundaries and unavailable point arms receive distinct repair diagnoses");
        var network=new NetworkInput{World=key,Nodes=new List<PlanNode>{new PlanNode{Id="n0"},new PlanNode{Id="n1",X=200}},Edges=new List<PlanEdge>{new PlanEdge{Id="S",A="n0",B="n1",Length=200}},Cuts=new List<PlanCut>{new PlanCut{Boundary=a,BoundaryKind=1,Segment="S",Meters=0},new PlanCut{Boundary=c,BoundaryKind=1,Segment="S",Meters=200}}};
        var left=B("old-left",a,gone,0,100);var right=B("old-right",gone,c,100,200);
        bool Valid(TrackBlock b)=>b.Id=="other"||CircuitPathValidation.Error(network,b,Active(b.BoundaryA),Active(b.BoundaryB))==null;
        var detected=DetectionNetwork.Build(network);var candidates=GfmSectionRepair.Candidates(detected.Blocks.Values.Concat(new[]{left,right}),left,Valid);
        Check(candidates.Length==1&&candidates[0].Path.Sum(p=>p.Section.To-p.Section.From)==200&&TrackCircuit.Pair(candidates[0].BoundaryA,candidates[0].BoundaryB)==TrackCircuit.Pair(a,c),"after deleting an intermediate boundary recognition offers the complete section up to the next actual detector");
        var lib=new DeskLibrary{WorldKey=key};var master=new DeskDefinition{Code="ESTW",Name="Master",Global=true,Panel=new PanelDocument{WorldKey=key}};
        var whb=new DeskDefinition{Code="WHB",Name="Whittier",Panel=new PanelDocument{WorldKey=key}};var ela=new DeskDefinition{Code="ELA",Name="Ela",Panel=new PanelDocument{WorldKey=key}};lib.Desks.AddRange(new[]{master,whb,ela});
        lib.Blocks[left.Id]=left;lib.Blocks[right.Id]=right;var other=B("other",Id(),Id(),0,100);other.Path[0].Section.Segment="Z";lib.Blocks[other.Id]=other;
        string numberId=Id();whb.Panel.Put(new PanelTile{X=2,Y=2,Kind=PanelTileKind.TrainNumber,NumberId=numberId,Number="00001",Ports=17,BlockId=left.Id,AutoSegment="S",AutoMeters=50});
        whb.Panel.Put(new PanelTile{X=3,Y=2,Kind=PanelTileKind.Straight,Ports=17,CircuitBoundary=gone,CircuitRails=new Dictionary<int,string>{{4,left.Id},{0,right.Id}}});
        ela.Panel.Put(new PanelTile{X=2,Y=2,Kind=PanelTileKind.Straight,Ports=17,BlockId=right.Id});ela.Panel.Put(new PanelTile{X=2,Y=3,Kind=PanelTileKind.TrainNumber,NumberMirror=numberId});lib.Validate(key);
        string before=JsonConvert.SerializeObject(lib);var repair=GfmSectionRepair.Build(lib,new[]{left.Id},candidates[0],Valid,Fault);
        Check(repair.Removed.Length==2&&repair.Fields==3&&repair.Desks.Length==2&&repair.Boundaries==1&&JsonConvert.SerializeObject(lib)==before,"one replacement preview collects both stale halves and both interlockings without modifying the saved installation");
        var repairedNumber=repair.Library.Desks.Single(d=>d.Id==whb.Id).Panel.At(2,2);
        Check(repairedNumber.NumberId==numberId&&repairedNumber.Number=="00001"&&new NumberMirrorIndex(repair.Library).Source(repair.Library.Desks.Single(d=>d.Id==ela.Id).Panel.At(2,3)).Tile.NumberId==numberId&&repair.Library.Blocks.ContainsKey(other.Id),"repair retains number sources, mirrors and unrelated sections while replacing only affected GFM references");
        Check(!repair.Library.Blocks.ContainsKey(left.Id)&&!repair.Library.Blocks.ContainsKey(right.Id)&&repair.Library.Desks.SelectMany(d=>d.Panel.Tiles).All(t=>!GfmDetectionRefresh.References(t).Any(id=>id==left.Id||id==right.Id)),"confirmed replacement data contains no dangling reference to either stale half");
        var removal=GfmSectionRepair.Build(lib,new[]{left.Id,right.Id},null,Valid,Fault);
        Check(!CircuitDisplayPolicy.Monitored(removal.Library,removal.Library.Desks.Single(d=>d.Id==whb.Id).Panel.At(2,2).BlockId)&&GfmCoverage.Gaps(new[]{P(0,200).Section},removal.Library.Blocks.Values.SelectMany(b=>b.Sections)).Count==1,"cleanup without replacement leaves an explicit coverage gap rather than manufacturing a clear-track report");
        var off=disabled.Clone();off.Id=gone;off.Anchor.Meters=100;raw[gone]=off;
        var retiredDisabled=GfmSectionRepair.Build(lib,new[]{left.Id},null,Valid,Fault);
        Check(retiredDisabled.Library.Desks.Single(d=>d.Id==whb.Id).Panel.At(3,2).CircuitBoundary==gone&&retiredDisabled.Library.Blocks.ContainsKey(right.Id),"removing one stale assignment does not erase a disabled but still existing shared boundary mark");
        raw[gone]=new MarkerRecord{Id=gone,Name="planned",Kind=MarkerRecord.CircuitB,Planned=true,Anchor=new MarkerAnchor{Segment="S",Meters=100}};
        Check(Reject(()=>GfmSectionRepair.Build(lib,new[]{left.Id},null,Valid,Fault)),"cleanup cannot mistake an unfinished construction plan for a deleted boundary");raw.Remove(gone);
        Check(Reject(()=>GfmSectionRepair.Build(lib,new[]{other.Id},null,Valid,Fault)),"a valid live section cannot be removed by the invalid-section cleanup");
        var protectedLib=lib.Clone();protectedLib.FreeLines.Add(new LineTrackDefinition{Code="STR-001",Name="A-B",StationA=whb.Id,StationB=ela.Id,BlockOwner=whb.Id,BlockUnits=new List<BlockSignalUnit>{new BlockSignalUnit{Boundary=gone,Forward=Id(),Reverse=Id(),Distance=100}},BlockSections=new List<string>{left.Id,right.Id}});protectedLib.Validate(key);
        Check(Reject(()=>GfmSectionRepair.Build(protectedLib,new[]{left.Id},null,Valid,Fault)),"cleanup refuses to orphan a constructed block assembly's internal section metadata");
        var run=new RouteRun{Route=new RouteDefinition{Code="near",Sections=new List<TrackSection>{P(20,40).Section}}};var far=new RouteRun{Route=new RouteDefinition{Code="far",Sections=new List<TrackSection>{P(20,40,"Z").Section}}};
        var lease=new DirectionBlockLease{Id="BL-near",Axis=new List<DirectedRouteSection>{P(20,40)}};var farLease=new DirectionBlockLease{Id="BL-far",Axis=new List<DirectedRouteSection>{P(20,40,"Z")}};
        var state=new LineDirectionState{Held=true,Definition=new LineTrackDefinition{Code="held"},Axis=new List<DirectedRouteSection>{P(30,50)}};
        Check(GfmSectionRepair.Blockers(repair,new[]{run,far},new[]{lease,farLease},new[]{state}).Length==3&&GfmSectionRepair.Blockers(repair,new[]{far},new[]{farLease},Array.Empty<LineDirectionState>()).Length==0,"GFM repair protects overlapping routes and direction holds while allowing unrelated running areas");
        var cuts=network.Cuts.ToList();network.Cuts.Add(new PlanCut{Boundary=Id(),BoundaryKind=1,Segment="S",Meters=125});
        Check(GfmSectionRepair.Candidates(new[]{candidates[0]},left,Valid).Length==0,"replacement cannot jump over another active detector introduced after the old boundary was removed");
        ShortPointSection(key);
    }
    static void ShortPointSection(string key) {
        string a=TrackCircuit.PointId("W198",2),b=TrackCircuit.PointId("W212",2);double length=57.29;
        var first=new MarkerRecord{Id=a,Name="GFM-B-W198-R",Kind=MarkerRecord.CircuitB,Anchor=new MarkerAnchor{Segment="Sz5hy",Meters=25}};
        var last=new MarkerRecord{Id=b,Name="GFM-B-W212-R",Kind=MarkerRecord.CircuitB,Anchor=new MarkerAnchor{Segment="Sz5hy",Meters=length-25}};
        var input=new NetworkInput{World=key,Nodes=new List<PlanNode>{new PlanNode{Id="n0"},new PlanNode{Id="n1",X=length}},Edges=new List<PlanEdge>{new PlanEdge{Id="Sz5hy",A="n0",B="n1",Length=length}},Cuts=new List<PlanCut>{new PlanCut{Boundary=a,BoundaryKind=2,Segment="Sz5hy",Meters=25},new PlanCut{Boundary=b,BoundaryKind=2,Segment="Sz5hy",Meters=length-25}}};
        var old=new TrackBlock{Id="old-between-points",Name="W198-W212",BoundaryA=a,BoundaryB=b,Path=new List<DirectedRouteSection>{P(0,length,"Sz5hy")}};
        MarkerRecord End(string id)=>id==a?first:id==b?last:null;
        bool Valid(TrackBlock block)=>CircuitPathValidation.Error(input,block,End(block.BoundaryA),End(block.BoundaryB))==null;
        var current=DetectionNetwork.Build(input).Blocks.Values.Where(c=>c.IsCircuit).ToArray();var choices=GfmSectionRepair.Candidates(current,old,Valid);
        Check(choices.Length==1&&Math.Abs(choices[0].Sections.Sum(s=>s.To-s.From)-7.29)<.001,"a shorter 7.29-m current section between two point boundaries is offered for an obsolete 57.29-m saved path with the same endpoints");
        var lib=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey=key}}}};lib.Blocks[old.Id]=old;lib.Desks[0].Panel.Put(new PanelTile{X=2,Y=2,Kind=PanelTileKind.Straight,Ports=17,BlockId=old.Id});
        var fixedPlan=GfmSectionRepair.Build(lib,new[]{old.Id},choices[0],Valid,_=>GfmBoundaryFault.None);
        Check(fixedPlan.Affected.Any(s=>s.From==0&&s.To==length)&&Valid(fixedPlan.Library.Blocks[choices[0].Id])&&!fixedPlan.Library.Blocks.ContainsKey(old.Id),"same-boundary recalculation replaces the stale wider path while guarding the full old and new intervals during confirmation");
    }
}
