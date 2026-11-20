using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

public static class CircuitRoleTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static MarkerRecord Boundary(double meters,int role,bool reverse=false)=>new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name="boundary",Kind=role==1?MarkerRecord.Main:role==3?MarkerRecord.CircuitC:MarkerRecord.CircuitB,CircuitVariant=role==1?1:0,Anchor=new MarkerAnchor{Segment="line",Meters=meters},FacingReverse=reverse};
    static NetworkInput Graph()=>new NetworkInput{World="roles",Nodes=new List<PlanNode>{new PlanNode{Id="a"},new PlanNode{Id="z",X=100}},Edges=new List<PlanEdge>{new PlanEdge{Id="line",A="a",B="z",Length=100}}};
    static PlanCut Cut(MarkerRecord row)=>new PlanCut{Segment=row.Anchor.Segment,Meters=row.Anchor.Meters,X=row.Anchor.Meters,Boundary=row.Id,BoundaryCode=row.Name,BoundaryKind=CircuitIdentity.Variant(row),BoundaryReverse=CircuitIdentity.Reverse(row)};
    public static void Run(string root) {
        var a=Boundary(10,1);var c=Boundary(40,3);var b=Boundary(90,2);var graph=Graph();
        Check(TrackCircuit.Find(graph,a,c,out _).Count==0&&TrackCircuit.Find(graph,c,b,out _).Count==1,"C accepts a section on its arrow side and refuses closing a section against its back");
        c.CircuitReverse=true;
        Check(TrackCircuit.Find(graph,a,c,out _).Count==1&&TrackCircuit.Find(graph,c,b,out _).Count==0,"reversing C changes the permitted monitored side without moving the boundary or changing its ID");
        c.CircuitReverse=false;graph.Cuts.AddRange(new[]{Cut(a),Cut(c),Cut(b)});
        Check(TrackCircuit.Find(graph,a,b,out _).Count==0,"a manually selected long detection section cannot skip an intervening C boundary");
        var result=NetworkPlan.Build(graph);var detected=result.Blocks.Values.Single(x=>x.IsCircuit);
        Check(detected.Path.Single().Section.From==40&&detected.Path.Single().Section.To==90&&new[]{detected.BoundaryA,detected.BoundaryB}.Order().SequenceEqual(new[]{c.Id,b.Id}.Order()),"automatic recognition creates a real detection section only between configured boundaries on the monitored side of C");
        Check(result.Blocks.Values.Where(x=>!x.IsCircuit&&x.Point==null).Any(x=>x.Path.Any(p=>p.Section.From==10&&p.Section.To==40)),"the back of C remains unmonitored rather than being reported free by the new diagram");
        var broad=new TrackBlock{Id="test",Name="too broad",BoundaryA=a.Id,BoundaryB=b.Id,Path=TrackCircuit.Find(Graph(),a.Anchor,b.Anchor,out _).Single().Sections};
        Check(!TrackCircuit.InteriorClear(broad,a,b,new[]{a,c,b}),"a pre-existing section spanning a newly inserted boundary becomes invalid for live detection");
        var library=new DeskLibrary{WorldKey="roles",Desks=new List<DeskDefinition>{new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey="roles"}}}};
        var manual=detected.Clone();manual.Id="c:"+Guid.NewGuid().ToString("N");manual.Name="Existing section";library.Blocks[manual.Id]=manual;
        var preserved=NetworkRecognition.PreserveCircuitIds(library,result);
        Check(preserved.Blocks.ContainsKey(manual.Id)&&preserved.Blocks[manual.Id].Name=="Existing section"&&preserved.Blocks.Values.Count(x=>x.IsCircuit)==1&&result.Blocks.ContainsKey(detected.Id),"recognition reuses an existing matching detection ID and name without duplicating its block or mutating the preview");
        var point=Boundary(25,2);point.Id=TrackCircuit.PointId("W1",0);point.Name="GFM-B-W001-S";
        var placement=new PointBoxPlacement{Variant=3,DirectionFlip=true,Yaw=90};var once=placement.Appearance(point);var twice=placement.Appearance(once);
        Check(once.Id==point.Id&&once.Kind==MarkerRecord.CircuitC&&once.Name=="GFM-C-W001-S"&&twice.FacingReverse==once.FacingReverse&&twice.CircuitReverse==once.CircuitReverse&&CircuitIdentity.FacingDegrees(once)==180,"C on a point arm retains its exact boundary ID, follows its logical arrow and is not reversed twice by repeated appearance application");
        var clone=JsonConvert.DeserializeObject<MarkerRecord>(JsonConvert.SerializeObject(once));
        Check(clone.Clone().CircuitReverse&&CircuitIdentity.Reverse(clone),"C monitoring direction survives replication and marker cloning");
        var mesh=MarkerMeshData.CreateCircuitBoxC();
        Check(mesh.Vertices.Count>MarkerMeshData.CreateCircuitBox(true).Vertices.Count&&mesh.Triangles.All(t=>t.Count%3==0&&t.All(i=>i>=0&&i<mesh.Vertices.Count))&&mesh.Vertices.All(v=>v.All(x=>!float.IsNaN(x)&&!float.IsInfinity(x))),"C has bounded valid procedural geometry with its own top arrow");
        int tip=mesh.Vertices.Count-3;var tipTriangles=mesh.Triangles[1].Chunk(3).Where(t=>t.Any(i=>i>=tip)).ToArray();
        var aTip=mesh.Vertices[tipTriangles[0][0]];var bTip=mesh.Vertices[tipTriangles[0][1]];var cTip=mesh.Vertices[tipTriangles[0][2]];
        Check(tipTriangles.Length==1&&(bTip[2]-aTip[2])*(cTip[0]-aTip[0])-(bTip[0]-aTip[0])*(cTip[2]-aTip[2])>0,"the painted C arrow has one upward-facing top triangle, so lighting normals cannot cancel against a coincident reverse face");
        File.WriteAllText(Path.Combine(root,"build/checks/circuit-model-preview.json"),JsonConvert.SerializeObject(new{A=MarkerMeshData.CreateCircuitBox(false),B=MarkerMeshData.CreateCircuitBox(true),C=mesh,Mast=MarkerMeshData.CreateSignal(false,false)}));
        bool separated=true;
        for(int i=-50;i<=50;i++)for(int j=1;j<=6;j++) {
            double lateral=i/10d,scale=j/2d;var box=CircuitPlacement.AttachedLateral(lateral,scale);
            if(box.HasValue)separated&=Math.Abs(box.Value)<=5&&Math.Abs(box.Value-lateral)>=.8*scale+.15-.00001&&Math.Abs(box.Value)>=1.1+.27*scale-.00001;
        }
        Check(separated&&CircuitPlacement.AttachedLateral(2.3,1)>3&&CircuitPlacement.AttachedLateral(-2.3,1)< -3,"automatic attached-box placement keeps mast clearance and track clearance across supported sides and model scales");
        string directory=Path.Combine(root,"build","signal-code-tests-"+Guid.NewGuid().ToString("N"));string key=MarkerRepository.Key(directory,"map");var repo=new MarkerRepository(directory,key,"map");
        var shunt=repo.Put(new MarkerAnchor{Segment="line",Meters=10},kind:MarkerRecord.Shunt);int number=shunt.CircuitNumber;repo.PromoteShunt(shunt.Id);var upgraded=repo.Records.Single();
        Check(shunt.CircuitVariant==1&&number>0&&upgraded.Id==shunt.Id&&upgraded.Name.StartsWith("HS-")&&upgraded.PreviousCode==shunt.Name&&upgraded.CircuitNumber==number,"new signals receive A by default and a shunt upgrade changes only its technical prefix while retaining signal and detection identity");
        var face=MastConfiguration.Effective(upgraded);face.Front.DisplayName="Platform 1";repo.ConfigureMast(upgraded.Id,face,upgraded.Name);
        Check(Reject(()=>repo.Rename(upgraded.Id,"HS-999"))&&Reject(()=>repo.ConfigureDetails(upgraded.Id,"HS-999",null,false,null)),"technical signal prefix and number are protected in both rename and general-configuration APIs");
        face.Front.Code="HS-999";
        Check(Reject(()=>repo.ConfigureMast(upgraded.Id,face,upgraded.Name))&&SignalCatalogue.Expand(repo.Records).Single().DisplayName=="Platform 1","mast configuration cannot bypass automatic-code protection while editable display names remain supported");
    }
}
