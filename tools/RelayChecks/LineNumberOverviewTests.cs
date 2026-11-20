using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class LineNumberOverviewTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static string Json(object value)=>JsonConvert.SerializeObject(value);
    static DeskDefinition Desk(string code,bool global=false)=>new DeskDefinition{Code=code,Name=code,Global=global,Panel=new PanelDocument{WorldKey="overview"}};
    public static void Run() {
        var master=Desk("ESTW",true);var a=Desk("ELA");var b=Desk("WHB");var vita=Desk("VITA");
        var library=new DeskLibrary{WorldKey="overview",Desks=new List<DeskDefinition>{master,a,b,vita}};
        var bounds=Enumerable.Range(0,4).Select(_=>Guid.NewGuid().ToString("N")).ToArray();
        var line=new LineTrackDefinition{Code="STR-001",Name="ELA – WHB",StationA=a.Id,StationB=b.Id,BlockOwner=a.Id};
        for(int i=0;i<3;i++) {
            var block=new TrackBlock{Id="block-"+i,Name="Block "+i,BoundaryA=bounds[i],BoundaryB=bounds[i+1],Path=new List<DirectedRouteSection>{new DirectedRouteSection{Section=new TrackSection{Segment="track",From=i*100,To=(i+1)*100}}}};
            library.Blocks.Add(block.Id,block);line.BlockSections.Add(block.Id);
            if(i<2)line.BlockUnits.Add(new BlockSignalUnit{Boundary=bounds[i+1],Forward=Guid.NewGuid().ToString("N"),Reverse=Guid.NewGuid().ToString("N"),Distance=(i+1)*100});
        }
        library.FreeLines.Add(line);BlockDiagram.Rebuild(library,line,line.BlockSections.Select(id=>library.Blocks[id]).ToArray());library.Validate("overview");
        var sources=LineNumberOverview.AutomaticSources(library,line);var reverse=LineNumberOverview.AutomaticSources(library,line,true);
        Check(sources.Select(s=>s.Tile.BlockId).SequenceEqual(line.BlockSections)&&reverse.Select(s=>s.Tile.BlockId).SequenceEqual(line.BlockSections.AsEnumerable().Reverse()),"number overview obtains physical block order from the line and reverses it without guessing diagram coordinates");
        string before=Json(library);var overview=LineNumberOverview.Create(library,line.Id,vita.Id,reverse);var target=overview.Desks.Single(d=>d.Id==vita.Id);var area=target.Panel.Areas.Single(g=>g.NumberLine==line.Id);
        var mirrors=target.Panel.Tiles.OrderBy(t=>t.X).ToArray();var index=new NumberMirrorIndex(overview);
        Check(Json(library)==before&&area.Width==3&&area.Height==1&&mirrors.Length==3&&target.Panel.Tiles.All(NumberMirrors.IsMirror),"compact three-block overview is created as one row of mirrors without changing the input configuration");
        Check(mirrors.Select(t=>index.Source(t).Tile.BlockId).SequenceEqual(line.BlockSections.AsEnumerable().Reverse())&&mirrors.All(t=>t.NumberId==null&&t.BlockId==""&&t.Section==null&&PanelConnections.Ports(t)==0),"compact fields mirror canonical sources and own no block, detector, rail connection or independent number");
        Check(Json(overview.Blocks)==Json(library.Blocks)&&Json(overview.FreeLines)==Json(library.FreeLines)&&Json(overview.Automation)==Json(library.Automation)&&overview.Desks.Single(d=>d.Id==b.Id).Panel.Tiles.Count==0,"number-only view creates no copied block installation, permission, signal, automation change or compulsory neighbour diagram");
        Check(LineSetupWorkflow.Visible(overview,vita.Id).Single().Id==line.Id&&LineConfigurationChanges.Changed(library.FreeLines,overview.FreeLines).Length==0,"an explicitly selected remote viewing desk can find its line without becoming a neighbouring operator");
        Check(index.Inputs(mirrors).Count()==3&&index.Caption(mirrors[0],t=>t.BlockId)=="block-2"&&index.Caption(mirrors[0],t=>"12345",false)=="?","visible compact mirrors request remote detector inputs and reject stale values");
        var drag=new PanelDrag();drag.Begin(target.Panel,null,area.X+1,area.Y);Check(drag.Source.Width==3&&drag.Source.Height==1,"grabbing a compact field selects the complete number strip for dragging");
        drag.Update(target.Panel,area.X+5,area.Y+4);drag.Apply(target.Panel);overview.Validate("overview");
        var moved=target.Panel.Areas.Single(g=>g.Id==area.Id);Check(moved.X==area.X+4&&moved.Y==area.Y+4&&target.Panel.Tiles.All(t=>index.Source(t)!=null),"moving the compact group preserves stable source references and moves its area metadata");
        target.Panel.Put(new PanelTile{Kind=PanelTileKind.Label,X=12,Y=8,Label="occupied"});
        Check(!LineNumberOverview.CanPlace(target.Panel,area.Id,12,8)&&!LineNumberOverview.CanPlace(target.Panel,area.Id,-1,3)&&!LineNumberOverview.CanPlace(target.Panel,area.Id,target.Panel.Columns-1,3),"number overview refuses occupied destinations, negative positions and off-panel footprints");
        var ownerPanel=overview.Desks.Single(d=>d.Id==a.Id).Panel;var full=BlockDiagram.Area(ownerPanel,line.Id);
        var onOwner=LineNumberOverview.Create(overview,line.Id,a.Id,sources.Take(1));var ownerGroup=onOwner.Desks.Single(d=>d.Id==a.Id).Panel.Areas.Single(g=>g.NumberLine==line.Id);
        Check(!LineNumberOverview.CanPlace(onOwner,a.Id,ownerGroup.Id,full.X+2,full.Y),"compact placement also reserves empty cells inside a generated block assembly");
        var replaced=LineNumberOverview.Create(overview,line.Id,vita.Id,LineNumberOverview.AutomaticSources(overview,line).Take(1),area.Id);var replacement=replaced.Desks.Single(d=>d.Id==vita.Id).Panel.Areas.Single(g=>g.NumberLine==line.Id);
        Check(replacement.Id==area.Id&&replacement.X==moved.X&&replacement.Y==moved.Y&&replacement.Width==1&&replaced.Desks.SelectMany(d=>d.Panel.Tiles).Count(t=>t.NumberId!=null)==3,"reconfiguring three fields to one keeps the group position and canonical source count");
        var two=LineNumberOverview.Create(replaced,line.Id,vita.Id,LineNumberOverview.AutomaticSources(replaced,line).Skip(1).Take(1));
        Check(two.Desks.Single(d=>d.Id==vita.Id).Panel.Areas.Count(g=>g.NumberLine==line.Id)==2,"a desk may have several independently placed compact views of the same physical line");
        var removed=LineNumberOverview.Delete(replaced,vita.Id,area.Id);
        Check(!removed.Desks.Single(d=>d.Id==vita.Id).Panel.Tiles.Any(NumberMirrors.IsMirror)&&removed.Desks.SelectMany(d=>d.Panel.Tiles).Count(t=>t.NumberId!=null)==3&&Json(removed.Blocks)==Json(library.Blocks),"removing a compact overview leaves all source numbers and physical blocks intact");
        var saved=JsonConvert.DeserializeObject<DeskLibrary>(Json(replaced));saved.Validate("overview");var savedMirror=saved.Desks.Single(d=>d.Id==vita.Id).Panel.Tiles.Single(NumberMirrors.IsMirror);
        Check(new NumberMirrorIndex(saved).Source(savedMirror)!=null&&saved.Desks.Single(d=>d.Id==vita.Id).Panel.Areas.Single(g=>g.Id==area.Id).NumberLine==line.Id,"compact group metadata and mirror sources survive shared JSON persistence");
        var lost=saved.Clone();var source=new NumberMirrorIndex(lost).Source(savedMirror);source.Desk.Panel.Tiles.Remove(source.Tile);lost.Validate("overview");
        Check(new NumberMirrorIndex(lost).Caption(savedMirror,t=>"old value")=="?","deleting a compact overview's original source produces an explicit unknown rather than a stale number");
        Check(Reject(()=>LineNumberOverview.Create(library,line.Id,vita.Id,new[]{sources[0],sources[0]}))&&Reject(()=>LineNumberOverview.Create(library,line.Id,vita.Id,Array.Empty<NumberMirrorSource>()))&&Reject(()=>LineNumberOverview.Create(library,line.Id,vita.Id,Enumerable.Repeat(sources[0],17))),"overview rejects duplicate sources, empty selection and excessive field count atomically");
        var changed=library.Clone();changed.Desks.Single(d=>d.Id==a.Id).Panel.At(sources[0].Tile.X,sources[0].Tile.Y).Label="changed";string changedBefore=Json(changed);
        Check(Reject(()=>LineNumberOverview.Create(changed,line.Id,vita.Id,sources))&&Json(changed)==changedBefore,"a changed source invalidates a captured overview proposal without partial source-ID or layout edits");
        var foreign=replaced.Clone();var foreignPanel=foreign.Desks.Single(d=>d.Id==vita.Id).Panel;foreignPanel.Put(new PanelTile{Kind=PanelTileKind.Label,X=replacement.X,Y=replacement.Y,Label="manual"});
        Check(Reject(()=>LineNumberOverview.Delete(foreign,vita.Id,area.Id)),"removing an overview refuses to erase manually substituted fields");
        var rebuilt=overview.Clone();BlockDiagram.Rebuild(rebuilt,line,line.BlockSections.Select(id=>rebuilt.Blocks[id]).ToArray());rebuilt.Validate("overview");
        Check(rebuilt.Desks.Single(d=>d.Id==vita.Id).Panel.Tiles.Where(NumberMirrors.IsMirror).All(t=>new NumberMirrorIndex(rebuilt).Source(t)!=null),"rebuilding the full operating diagram preserves compact views and all canonical source identities");
        var transferred=rebuilt.FreeLines.Single();transferred.BlockOwner=b.Id;BlockDiagram.Rebuild(rebuilt,transferred,transferred.BlockSections.Select(id=>rebuilt.Blocks[id]).ToArray());rebuilt.Validate("overview");
        Check(rebuilt.Desks.Single(d=>d.Id==vita.Id).Panel.Tiles.Where(NumberMirrors.IsMirror).All(t=>new NumberMirrorIndex(rebuilt).Source(t).Desk.Id==b.Id),"compact views follow a block operator transfer without copying or replacing their source links");
        var manual=library.Clone();var manualLine=LineSetupWorkflow.Create(manual,a.Id);manual.FreeLines.Add(manualLine);
        var manualView=LineNumberOverview.Create(manual,manualLine.Id,b.Id,LineNumberOverview.Sources(manual).Take(2));manualView.Validate("overview");
        Check(manualView.FreeLines.Single(l=>l.Id==manualLine.Id).BlockOwner==""&&manualView.Desks.Single(d=>d.Id==b.Id).Panel.Tiles.Count==2,"manual source selection works without requiring a generated block-owner diagram for that line");
        var modules=library.Clone();modules.Modules.Add(new DeskModule{Desk=vita.Id,Master=master.Id,X=0,Y=0});
        var masterView=LineNumberOverview.Create(modules,line.Id,master.Id,LineNumberOverview.AutomaticSources(modules,line));var masterArea=masterView.Desks.Single(d=>d.Id==master.Id).Panel.Areas.Single(g=>g.NumberLine==line.Id);
        Check(masterArea.Y>=vita.Panel.Rows&&!LineNumberOverview.CanPlace(masterView,master.Id,masterArea.Id,2,2)&&Reject(()=>LineNumberOverview.Move(masterView,master.Id,masterArea.Id,2,2)),"master placement excludes complete local-module rectangles including their blank cells");
    }
}
