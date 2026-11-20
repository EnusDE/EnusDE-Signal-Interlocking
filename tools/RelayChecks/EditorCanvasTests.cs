using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;

static class EditorCanvasTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static string Json(object value)=>JsonConvert.SerializeObject(value);
    static PanelDocument Blank()=>new PanelDocument{WorldKey="canvas",Columns=24,Rows=12};
    static DeskDefinition Desk(string code,bool global=false)=>new DeskDefinition{Code=code,Name=code,Global=global,Panel=Blank()};
    public static void Run(string root) {
        Check(EditorCanvasGeometry.ColumnName(0)=="A"&&EditorCanvasGeometry.ColumnName(25)=="Z"&&EditorCanvasGeometry.ColumnName(26)=="AA"&&EditorCanvasGeometry.ColumnName(701)=="ZZ"&&EditorCanvasGeometry.ColumnName(702)=="AAA"&&EditorCanvasGeometry.ColumnName(2047)=="BZT","Excel-style column captions span A through BZT at the supported canvas limit");
        var axis=new EditorAxisSelection();var r=axis.Select(true,4,false,24,12);r=axis.Select(true,1,true,24,12);
        Check(r.X==0&&r.Y==1&&r.Width==24&&r.Height==4,"Shift-click selects complete contiguous rows in either direction from the fixed anchor");
        r=axis.Select(false,7,true,24,12);r=axis.Select(false,10,true,24,12);
        Check(r.X==7&&r.Y==0&&r.Width==4&&r.Height==12,"changing header axis starts a new column anchor and selects full-height columns");
        axis.Clear();r=axis.Select(false,2,true,24,12);Check(r.Width==1&&r.X==2,"a cleared header selection cannot reuse a stale Shift anchor");
        var doc=Blank();var sourceId=Guid.NewGuid().ToString("N");var targetId=Guid.NewGuid().ToString("N");
        doc.Put(new PanelTile{X=0,Y=0,Kind=PanelTileKind.TrainNumber,NumberId=sourceId,Reference="main",BlockId="detector",Ports=17,Number="12345"});
        doc.Put(new PanelTile{X=4,Y=3,Kind=PanelTileKind.TrainNumber,NumberMirror=sourceId});
        doc.Put(new PanelTile{X=6,Y=5,Kind=PanelTileKind.AuxiliaryTarget,Target=new AuxiliaryTarget{Id=targetId,Name="Exit",Anchor=new MarkerAnchor{Segment="real",Meters=15},Train=true}});
        string line=Guid.NewGuid().ToString("N");doc.Areas.Add(new PanelArea{Code="ZN",Name="Numbers",Kind="Strecke",NumberLine=line,X=4,Y=3,Width=1,Height=1});
        var original=doc.Clone();var growth=CanvasGrowth.For(doc,new PanelRegion{X=-2,Y=-1,Width=2,Height=2});growth.Apply(doc);
        Check(doc.Columns==28&&doc.Rows==15&&growth.Left==4&&growth.Top==3&&doc.OriginX==4&&doc.OriginY==3&&doc.At(4,3).Reference=="main"&&doc.At(4,3).NumberId==sourceId&&doc.At(4,3).Number=="12345","top/left growth adds a working margin and shifts complete fields without changing number, signal or detector identities");
        Check(doc.At(8,6).NumberMirror==sourceId&&doc.At(10,8).Target.Id==targetId&&doc.At(10,8).Target.Anchor.Meters==15&&doc.Areas.Single().X==8&&doc.Areas.Single().Y==6,"automatic prefix growth preserves mirror links, auxiliary world anchors and group boundaries");
        var right=CanvasGrowth.For(doc,new PanelRegion{X=doc.Columns,Y=doc.Rows});int cols=doc.Columns,rows=doc.Rows;right.Apply(doc);
        Check(right.Left==0&&right.Top==0&&doc.Columns==cols+3&&doc.Rows==rows+3&&doc.At(4,3).NumberId==sourceId,"right/bottom expansion appends cells without moving existing content");
        var roundtrip=JsonConvert.DeserializeObject<PanelDocument>(Json(doc));roundtrip.Validate("canvas");
        Check(roundtrip.OriginX==doc.OriginX&&roundtrip.OriginY==doc.OriginY,"prefix offsets persist for save/reload and reversible master-module translation");
        int x,y;bool picks=true;foreach(float zoom in new[]{.025f,.08f,.5f,1,2})picks&=EditorCanvasGeometry.Pick(doc,zoom,-72*zoom*1.5f,48*zoom*.5f,out x,out y)&&x==-2&&y==-1;
        Check(picks&&!EditorCanvasGeometry.Pick(doc,1,-4*72,0,out x,out y)&&!EditorCanvasGeometry.Pick(doc,1,float.NaN,0,out x,out y),"the editor virtual border accepts negative cells at every zoom and rejects invalid or distant input");
        Check(!PanelPicking.Cell(doc.Columns,doc.Rows,1,-1,-1,out x,out y),"ordinary dispatcher hit-testing still rejects positions outside its saved diagram");
        var full=Blank();full.Columns=PanelDocument.MaxColumns;full.Rows=PanelDocument.MaxRows;string before=Json(full);
        var inside=CanvasGrowth.For(full,new PanelRegion{X=0,Y=0});inside.Apply(full);
        Check(!inside.Changed&&Json(full)==before&&Reject(()=>CanvasGrowth.For(full,new PanelRegion{X=-1,Y=0}))&&Json(full)==before,"full-size diagrams can edit existing edge cells but reject actual overflow without partially growing");
        var insert=Blank();insert.Put(new PanelTile{X=3,Y=3,Reference="signal",Kind=PanelTileKind.Signal,Ports=17});insert.Areas.Add(new PanelArea{Code="AREA",Name="Area",X=1,Y=1,Width=5,Height=5});EditorCanvasGeometry.Insert(insert,true,3,2);EditorCanvasGeometry.Insert(insert,false,0,2);
        Check(insert.At(5,5).Reference=="signal"&&insert.Areas.Single().Height==7&&insert.Areas.Single().X==3&&insert.OriginX==2&&insert.OriginY==0,"multi-row insertion expands intersected areas, shifts whole fields and records only actual prefix insertion");
        string insertion=Json(insert);Check(Reject(()=>EditorCanvasGeometry.Insert(insert,false,0,2048))&&Json(insert)==insertion,"multi-column insertion rejects limits atomically");
        var move=Blank();move.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.TrainNumber,NumberMirror=sourceId});move.Put(new PanelTile{X=2,Y=1,Kind=PanelTileKind.TrainNumber,NumberMirror=sourceId});move.Areas.Add(new PanelArea{Code="ZN",Name="Strip",Kind="Strecke",NumberLine=line,X=1,Y=1,Width=2,Height=1});
        var drag=new PanelDrag();drag.Begin(move,null,2,1,true);drag.Update(move,0,0);var pad=drag.Growth(move);drag.Apply(move);
        Check(move.Columns==27&&move.Rows==14&&move.Tiles.All(t=>t.Y==2&&t.NumberMirror==sourceId)&&move.Areas.Single().X==2&&move.Areas.Single().Y==2,"dragging a compact group beyond the top/left edge grows and moves the complete linked strip");
        drag.Begin(move,null,2,2,true);drag.Update(move,3,3);drag.Invalidate();string unchanged=Json(move);
        Check(!drag.Valid&&Reject(()=>drag.Apply(move))&&Json(move)==unchanged,"releasing a drag outside the viewport cannot accidentally commit a far-negative destination");
        var collision=Blank();collision.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Label,Label="one"});collision.Put(new PanelTile{X=2,Y=1,Kind=PanelTileKind.Label,Label="two"});drag.Begin(collision,null,1,1,true);drag.Update(collision,2,1);
        Check(!drag.Valid&&Reject(()=>drag.Apply(collision))&&collision.Columns==24,"automatic growth does not override occupied move destinations");
        for(double oldZoom=.025;oldZoom<=2;oldZoom*=2)foreach(double newZoom in new[]{.025,.08,.5,1,2})foreach(double pointer in new[]{0,137,750}) {
            double logical=(190+pointer)/oldZoom,offset=EditorCanvasGeometry.ZoomScroll(190,pointer,oldZoom,newZoom);
            if(Math.Abs((offset+pointer)/newZoom-logical)>.000001)throw new Exception("mouse zoom drift");
        }
        Check(true,"mouse-centred zoom preserves the logical point under the cursor across all supported zoom ranges");
        var grouped=Blank();grouped.Put(new PanelTile{X=3,Y=3,Kind=PanelTileKind.Label,Label="group"});grouped.Put(new PanelTile{X=18,Y=3,Kind=PanelTileKind.Label,Label="other"});
        grouped.Areas.Add(new PanelArea{Code="BLOCK",Name="Block group",Kind="Strecke",BlockLine=line,X=2,Y=2,Width=6,Height=3});
        var rowSelection=new PanelRegion{X=0,Y=3,Width=24,Height=1};drag.Begin(grouped,rowSelection,18,3,true);
        Check(drag.Source.X==0&&drag.Source.Width==24&&drag.Source.Y==2&&drag.Source.Height==3&&rowSelection.Height==1,"whole-row dragging expands around touched managed groups while retaining every selected field and the original selection");
        drag.Update(grouped,18,5);var rowGrowth=drag.Growth(grouped);drag.Apply(grouped);
        Check(grouped.At(18+rowGrowth.Left,5+rowGrowth.Top).Label=="other"&&grouped.Areas.Single().Y==4+rowGrowth.Top&&grouped.At(3+rowGrowth.Left,5+rowGrowth.Top).Label=="group","moving a selected row never splits its block assembly or drops other fields from the selection");
        axis.Select(true,11,false,24,12);var shorter=axis.Select(true,2,true,24,6);
        Check(shorter.Y==2&&shorter.Height==1,"a header anchor outside a resized panel is reset before Shift range selection");
        var metadata=Blank();var altered=metadata.Clone();altered.OriginX=1;
        Check(PanelHistory.Hash(metadata)!=PanelHistory.Hash(altered),"history and stale-preview hashes include the canvas prefix offsets");
        MasterPersistence(root);
    }
    static void MasterPersistence(string root) {
        var master=Desk("MASTER",true);var local=Desk("LOCAL");master.Panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Label,Label="master"});
        var lib=new DeskLibrary{WorldKey="canvas",Desks=new List<DeskDefinition>{master,local},Modules=new List<DeskModule>{new DeskModule{Desk=local.Id,Master=master.Id,X=26,Y=0}}};lib.Validate("canvas");
        var store=new DeskStore(Path.Combine(root,"build","canvas-tests-"+Guid.NewGuid().ToString("N")),"canvas");var before=master.Panel.Clone();var changed=before.Clone();CanvasGrowth.For(changed,new PanelRegion{X=-1,Y=-1}).Apply(changed);var saved=store.PutPanel(lib,master.Id,changed);
        Check(saved.Modules.Single().X==29&&saved.Modules.Single().Y==3&&lib.Modules.Single().X==26,"master prefix growth translates its real module references exactly once without changing the input library");
        var historyPath=Path.Combine(root,"build","canvas-history-"+Guid.NewGuid().ToString("N"));var history=new PanelHistory(historyPath,before);history.Record(before,changed);
        var undoTarget=history.Target(saved.Desks.Single(d=>d.Id==master.Id).Panel,false);var undo=store.PutPanel(saved,master.Id,undoTarget);history.Accept(undoTarget,false);
        var redoTarget=history.Target(undo.Desks.Single(d=>d.Id==master.Id).Panel,true);var redo=store.PutPanel(undo,master.Id,redoTarget);history.Accept(redoTarget,true);
        Check(new PanelHistory(historyPath,redoTarget).UndoCount==1&&undoTarget.OriginX==0&&redoTarget.OriginX==3,"compressed editor-history deltas preserve prefix offsets and reopen after actual undo/redo");
        Check(undo.Modules.Single().X==26&&undo.Modules.Single().Y==0&&redo.Modules.Single().X==29&&redo.Modules.Single().Y==3&&store.Load(null).Desks.Single(d=>d.Id==master.Id).Panel.OriginX==3,"undo/redo and real desk-store persistence restore matching canvas and master-module positions");
        var other=Desk("SECOND");lib.Desks.Add(other);lib.Modules.Add(new DeskModule{Desk=other.Id,Master=master.Id,X=50,Y=0});lib.Validate("canvas");var expanded=local.Panel.Clone();expanded.Columns=28;string original=Json(lib);
        Check(Reject(()=>store.PutPanel(lib,local.Id,expanded))&&Json(lib)==original,"local expansion that collides with another master module refuses without changing any shared configuration");
        var legacy=JsonConvert.DeserializeObject<PanelDocument>("{\"WorldKey\":\"canvas\",\"Columns\":24,\"Rows\":12}");legacy.Validate("canvas");Check(legacy.OriginX==0&&legacy.OriginY==0,"older saved diagrams load with zero prefix offsets and need no migration");
    }
}
