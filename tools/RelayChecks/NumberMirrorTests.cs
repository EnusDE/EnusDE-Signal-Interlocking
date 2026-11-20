using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;

public static class NumberMirrorTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static DeskLibrary Fixture() {
        var global=new DeskDefinition{Global=true,Code="ESTW",Name="Global",Panel=new PanelDocument{WorldKey="world"}};
        global.Panel.Put(new PanelTile{Kind=PanelTileKind.TrainNumber,X=3,Y=2,Reference="A",Label="Freie Strecke",Section=new TrackSection{Segment="remote",From=0,To=100}});
        var local=new DeskDefinition{Code="WHB",Name="Bahnhof",Panel=new PanelDocument{WorldKey="world"}};
        return new DeskLibrary{WorldKey="world",Desks=new List<DeskDefinition>{global,local}};
    }
    static DeskLibrary Link(DeskLibrary data,int x=4)=>NumberMirrors.Link(data,data.Desks[1].Id,new PanelTile{Kind=PanelTileKind.TrainNumber,X=x,Y=3,Label="Voranzeige"},data.Desks[0].Id,3,2);
    static MarkerRecord Head(string id)=>new MarkerRecord{Id=id,Name=id,Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="remote",Meters=0}};
    public static void Run() {
        var original=Fixture();original.Validate("world");var linked=Link(original);var source=linked.Desks[0].Panel.At(3,2);var mirror=linked.Desks[1].Panel.At(4,3);
        Check(original.Desks[0].Panel.At(3,2).NumberId==null&&original.Desks[1].Panel.Tiles.Count==0&&source.NumberId!=null&&mirror.NumberMirror==source.NumberId,
            "creating a cross-desk number mirror atomically assigns a stable source ID without mutating the input draft");
        Check(mirror.Reference==""&&mirror.BlockId==""&&mirror.Section==null&&mirror.Number==""&&mirror.NumberId==null&&PanelConnections.Ports(mirror)==0&&!CircuitDiagramAssignment.RailField(mirror),
            "a number mirror owns no signal binding, local detector, duplicate number or track connection");
        var index=new NumberMirrorIndex(linked);string number="100145";
        Check(index.Source(mirror).Desk.Global&&index.Caption(mirror,t=>number)=="100145","a local number field displays the selected global source's live value");
        number="00234";Check(index.Caption(mirror,t=>number)=="00234","a source number change appears on the mirror without recopying configuration or values");
        Check(index.Caption(mirror,t=>number,false)=="?","stale live data never leaves a mirrored train number presented as current");
        Check(index.Inputs(new[]{mirror}).Single()==source&&!linked.Desks[1].Panel.Tiles.Contains(source),
            "reading only a visible local mirror still requests the remote source's detector input while the source board is closed");
        var twice=Link(linked,6);var second=twice.Desks[1].Panel.At(6,3);var first=twice.Desks[1].Panel.At(4,3);var sameIndex=new NumberMirrorIndex(twice);
        Check(second.NumberMirror==first.NumberMirror&&sameIndex.Inputs(new[]{first,second}).Count()==1,
            "several display copies share one source and deduplicate its off-board detection read");
        var bindings=new NumberFieldIndex(twice,new[]{Head("A"),Head("B")});
        Check(bindings.ForSignal("A")=="A"&&bindings.ForField(first)==null&&bindings.ForField(second)==null,
            "display copies never introduce additional train-number forwarding endpoints or ambiguous signal ownership");
        var fields=new Dictionary<string,string>{{"A","00123"}};
        var passage=new RouteRun{Route=new RouteDefinition{Entry="A",Exit="B"},Stage=RouteStage.Running,ChainReady=true,StartPassageVerified=true,TrainCars=new[]{"engine"}};
        Check(TrainNumbers.AdvanceSignalNumbers(fields,new[]{passage})&&fields.Count==1&&fields["B"]=="00123"&&sameIndex.Caption(first,t=>fields.TryGetValue(t.Reference,out var n)?n:"-----")=="-----",
            "normal number forwarding keeps one canonical number and mirrors clear when that source's number moves onward");
        var moved=twice.Clone();new PanelRegion{X=3,Y=2}.Move(moved.Desks[0].Panel,8,5);moved.Validate("world");
        var movedMirror=moved.Desks[1].Panel.At(4,3);var movedIndex=new NumberMirrorIndex(moved);
        Check(movedIndex.Source(movedMirror).Tile.X==8&&movedIndex.Source(movedMirror).Tile.Y==5,
            "moving the source field preserves all mirrored links through its stable ID");
        PanelEditing.Insert(moved.Desks[0].Panel,true,1);moved.Validate("world");
        Check(new NumberMirrorIndex(moved).Source(movedMirror).Tile.Y==6,"inserting a source-board row does not detach remote number copies");
        var changed=moved.Clone();changed.Desks[0].Panel.At(8,6).Reference="B";
        Check(new NumberMirrorIndex(changed).Caption(changed.Desks[1].Panel.At(4,3),t=>t.Reference)=="B","reassigning the source binding updates the mirror instead of retaining its old signal reference");
        var saved=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(changed));saved.Validate("world");
        Check(new NumberMirrorIndex(saved).Source(saved.Desks[1].Panel.At(4,3)).Tile.NumberId==source.NumberId,
            "source IDs and cross-desk mirror links survive normal shared JSON persistence");
        var missing=saved.Clone();missing.Desks[0].Panel.Tiles.Clear();missing.Validate("world");
        Check(new NumberMirrorIndex(missing).Source(missing.Desks[1].Panel.At(4,3))==null&&new NumberMirrorIndex(missing).Caption(missing.Desks[1].Panel.At(4,3),t=>"old value")=="?",
            "deleting a source leaves an explicit unavailable mirror instead of a stale copied number or a guessed replacement");
        var duplicate=linked.Clone();var another=source.Clone();another.X=1;another.Y=1;duplicate.Desks[1].Panel.Put(another);
        Check(Reject(()=>duplicate.Validate("world")),"source identifiers are unique across all local and global panels");
        Check(Reject(()=>NumberMirrors.Link(linked,linked.Desks[0].Id,source,linked.Desks[0].Id,3,2)),"a train-number source cannot mirror itself");
        Check(Reject(()=>NumberMirrors.Link(twice,twice.Desks[0].Id,new PanelTile{Kind=PanelTileKind.TrainNumber,X=9,Y=4},twice.Desks[1].Id,4,3)),
            "a display copy cannot become another copy's source, preventing mirror chains and cycles");
        var alternate=twice.Clone();alternate.Desks[1].Panel.Put(new PanelTile{Kind=PanelTileKind.TrainNumber,X=9,Y=5,Reference="B"});
        Check(Reject(()=>NumberMirrors.Link(alternate,alternate.Desks[0].Id,alternate.Desks[0].Panel.At(3,2),alternate.Desks[1].Id,9,5)),
            "converting a referenced source cannot silently replace it with a copy");
        var invalid=mirror.Clone();invalid.Reference="A";Check(Reject(()=>NumberMirrors.Validate(invalid)),"mirrors cannot acquire writable main-signal ownership");
        invalid=mirror.Clone();invalid.Section=new TrackSection{Segment="local",From=0,To=10};Check(Reject(()=>NumberMirrors.Validate(invalid)),"mirror metadata cannot also bind a different local occupancy section");
        invalid=mirror.Clone();invalid.NumberId=Guid.NewGuid().ToString("N");Check(Reject(()=>NumberMirrors.Validate(invalid)),"a mirror cannot claim its own source identity");
        invalid=mirror.Clone();invalid.NumberMirror="not a source ID";Check(Reject(()=>NumberMirrors.Validate(invalid)),"malformed source links reject without accepting a half-configured mirror");
        var extracted=new PanelRegion{X=0,Y=0,Width=12,Height=8}.Extract(linked.Desks[0].Panel);
        Check(extracted.At(3,2).NumberId==null&&source.NumberId!=null,"copying a region creates independent source identities without corrupting existing mirror references");
        var pasted=new PanelDocument{WorldKey="world"};PanelEditing.Paste(pasted,linked.Desks[0].Panel,1,1);
        Check(pasted.At(4,3).NumberId==null,"pasting an ordinary numbered source does not duplicate its stable ID on another board");
        var displayCopy=NumberMirrors.CopyPanel(twice.Desks[1].Panel);
        Check(displayCopy.Tiles.All(t=>t.NumberMirror==source.NumberId),"copying an explicit display copy preserves its link to the original source");
        var normal=NumberMirrors.Detached(mirror);
        Check(normal.NumberMirror==null&&normal.NumberId==null&&mirror.NumberMirror==source.NumberId,"detaching a mirror creates an empty independent field and never edits the original source");
        var unchanged=JsonConvert.SerializeObject(linked.Desks[1].Panel);
        Check(Reject(()=>PanelEditing.Draw(linked.Desks[1].Panel,3,3,5,3))&&unchanged==JsonConvert.SerializeObject(linked.Desks[1].Panel),
            "drawing through a pure number mirror cannot turn its display into a local physical route connection");
        const string backupWorld="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var layout=linked.Clone();layout.WorldKey=backupWorld;foreach(var desk in layout.Desks)desk.Panel.WorldKey=backupWorld;
        var installation=new InstallationDocument{WorldKey=backupWorld,Map="map",Objects=new MarkerDocument{WorldKey=backupWorld,Map="map"}};
        var backup=InstallationBackupData.Capture(installation,layout,Array.Empty<RouteDefinition>());
        var recovered=backup.RestoreFor("map",new string('b',64));var recoveredMirror=recovered.Desks.Desks[1].Panel.At(4,3);
        Check(new NumberMirrorIndex(recovered.Desks).Source(recoveredMirror).Tile.NumberId==source.NumberId&&backup.Resources().Contains("number-source:"+source.NumberId),
            "external backup and world rebasing retain mirror links and protect removed source fields as recoverable resources");
        var legacy=JsonConvert.DeserializeObject<PanelTile>("{\"Kind\":7,\"X\":1,\"Y\":1}");
        Check(legacy.NumberId==null&&legacy.NumberMirror==null&&!NumberMirrors.IsMirror(legacy),"legacy train-number cells retain their ordinary behavior without migration");
    }
}
