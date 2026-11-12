using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

public static class MarkerTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    public static void Run(string root) {
        string directory=Path.Combine(root,"build","marker-tests-"+Guid.NewGuid().ToString("N"));
        string world=MarkerRepository.Key(Path.Combine(directory,"a.railroad"),"map/a");
        string other=MarkerRepository.Key(Path.Combine(directory,"b.railroad"),"map/a");
        Check(world.Length==64&&world!=other&&world!=MarkerRepository.Key(Path.Combine(directory,"a.railroad"),"map/b"),"marker storage is isolated by railroad path and map, not only by map name");
        var repository=new MarkerRepository(directory,world,"map/a");
        var anchor=new MarkerAnchor{Segment="track/curve-Ä",Meters=42.5,Lateral=1.8};
        var placed=repository.Put(anchor);
        Check(placed.Name=="GZ-001"&&placed.Id.Length==32&&File.Exists(repository.FilePath),"placing a marker assigns a stable ID and atomically persists its track anchor");
        anchor.Meters=99;Check(repository.Records[0].Anchor.Meters==42.5,"stored anchor does not alias the caller's mutable preview");
        var reloaded=new MarkerRepository(directory,world,"map/a");
        Check(reloaded.Records.Single().Anchor.Segment=="track/curve-Ä"&&reloaded.Records.Single().Anchor.Lateral==1.8,"marker reload preserves Unicode segment IDs, along-track distance and signed lateral offset");
        var edited=reloaded.Records.Single();edited.Anchor.Meters=44;
        var changed=reloaded.Put(edited.Anchor,edited.Id);
        Check(changed.Id==placed.Id&&changed.Name==placed.Name&&reloaded.Records.Count==1&&File.Exists(repository.FilePath+".bak"),"moving a marker preserves identity and a previous-file backup without duplicating it");
        var session=new MarkerEditSession();session.Edit(changed);session.Candidate.Meters=45;session.Cancel();
        Check(reloaded.Records.Single().Anchor.Meters==44&&session.Candidate==null,"cancelled preview edits never move the persisted marker");
        session.Start(new MarkerOptions());session.Offer(new MarkerAnchor{Segment="track/new",Meters=5});session.Freeze();session.Offer(new MarkerAnchor{Segment="wrong",Meters=1});
        Check(session.Candidate.Segment=="track/new"&&!session.Aiming,"freezing a valid preview prevents camera motion from replacing its anchor");
        Check(Reject(()=>session.Commit(reloaded,a=>false))&&session.Candidate!=null&&reloaded.Records.Count==1,"stale or removed track refuses placement and retains the editable preview");
        var committed=session.Commit(reloaded,a=>true);
        Check(committed.Name=="GZ-002"&&session.Candidate==null&&reloaded.Records.Count==2,"confirmed preview is stored exactly once and is then cleared");
        reloaded.Remove(changed.Id);Check(new MarkerRepository(directory,world,"map/a").Records.Single().Id==committed.Id,"removal deletes only the chosen local marker");
        var emptyWorld=new MarkerRepository(directory,other,"map/a");Check(emptyWorld.Records.Count==0,"another railroad on the same map does not inherit local markers");
        Check(Reject(()=>new MarkerAnchor{Segment="s",Meters=double.NaN}.Validate())&&Reject(()=>new MarkerAnchor{Segment="s",Meters=-1}.Validate())&&Reject(()=>new MarkerAnchor{Segment="s",Lateral=6}.Validate()),"nonfinite, negative and out-of-bounds marker parameters are rejected");
        File.WriteAllText(emptyWorld.FilePath,"{bad");string invalid=File.ReadAllText(emptyWorld.FilePath);
        Check(Reject(()=>new MarkerRepository(directory,other,"map/a"))&&File.ReadAllText(emptyWorld.FilePath)==invalid,"corrupt marker data is preserved and cannot silently turn into an empty editable book");
        string blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"file");var blockedRepo=new MarkerRepository(blocked,world,"map/a");
        session.Start(new MarkerOptions());session.Offer(new MarkerAnchor{Segment="s",Meters=1});
        Check(Reject(()=>session.Commit(blockedRepo,a=>true))&&blockedRepo.Records.Count==0&&session.Candidate!=null,"failed persistence neither commits the marker in memory nor discards the preview");
        var mesh=MarkerMeshData.Create();
        Check(mesh.Vertices.Count<300&&mesh.Triangles.All(t=>t.Count>0&&t.Count%3==0&&t.All(i=>i>=0&&i<mesh.Vertices.Count))&&mesh.Vertices.All(v=>v.Length==3&&v.All(f=>!float.IsNaN(f)&&!float.IsInfinity(f))),"original procedural marker mesh has bounded finite geometry and valid white/red/base submeshes");
        foreach(var board in new[]{MarkerMeshData.CreateSpeedBoard(),MarkerMeshData.CreateSignal(false,false),MarkerMeshData.CreateSignal(false,true),MarkerMeshData.CreateSignal(true,false)})
            if(board.Vertices.Count>100||board.Triangles.Any(t=>t.Count%3!=0||t.Any(i=>i<0||i>=board.Vertices.Count)))throw new Exception("Invalid signal mesh");
        Check(true,"all original speed/main/distant/shunt model meshes have bounded valid triangle indices");
        var main=reloaded.Put(new MarkerAnchor{Segment="s",Meters=10},null,MarkerRecord.Main);
        var distant=reloaded.Put(new MarkerAnchor{Segment="s",Meters=1},null,MarkerRecord.Distant);
        reloaded.ConfigureSignal(distant.Id,true,main.Id);reloaded.Rename(main.Id,"A 1");
        var signalReload=new MarkerRepository(directory,world,"map/a");
        Check(signalReload.Records.Single(r=>r.Id==distant.Id).LinkedSignal==main.Id&&signalReload.Records.Single(r=>r.Id==distant.Id).FacingReverse&&signalReload.Records.Single(r=>r.Id==main.Id).Name=="A 1","renaming a signal preserves its fixed ID and distant-signal linkage and direction across reload");
        Check(Reject(()=>reloaded.Rename(distant.Id,"a 1")),"human-readable signal codes remain unique regardless of case");
        var speed=reloaded.Put(new MarkerAnchor{Segment="s",Meters=20},null,MarkerRecord.Speed,SignUnits.Input("40",true));
        session.Edit(speed);session.Candidate.Meters=21;var movedSpeed=session.Commit(reloaded,a=>true);
        Check(movedSpeed.Kind==MarkerRecord.Speed&&Math.Abs(movedSpeed.SpeedMph*SignUnits.MphToKmh-40)<1e-10,"moving a custom speed board keeps its object kind and canonical speed");
        var doc=new MarkerDocument{WorldKey=world,Map="map/a",NextNumber=129};
        for(int i=0;i<MarkerRepository.Maximum;i++)doc.Markers.Add(new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name="GZ-"+i,Anchor=new MarkerAnchor{Segment="s",Meters=i}});
        string limitRoot=Path.Combine(directory,"limit");Directory.CreateDirectory(limitRoot);File.WriteAllText(Path.Combine(limitRoot,world+".json"),JsonConvert.SerializeObject(doc));
        var full=new MarkerRepository(limitRoot,world,"map/a");Check(Reject(()=>full.Put(new MarkerAnchor{Segment="s",Meters=1}))&&full.Records.Count==128,"per-world marker limit prevents unbounded scene growth");
    }
}
