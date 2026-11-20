using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class BlockUpdateTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static TrackSection S(string id,double from=0,double to=100)=>new TrackSection{Segment=id,From=from,To=to};
    static DirectedRouteSection P(string id,double from,double to,bool reverse=false)=>new DirectedRouteSection{Section=S(id,from,to),Reverse=reverse};
    sealed class Fixture {
        public DeskLibrary Library;
        public MarkerDocument Objects;
        public LineTrackDefinition Line;
        public string A,B,Master;
    }
    sealed class PassageWorld:IInterlockingWorld,ISectionalWorld,ISignalPassageWorld {
        public MarkerRecord Boundary;public double Head;
        public bool PathValid(RouteDefinition route)=>true;
        public TrackOccupancy Occupancy(TrackSection section)=>TrackOccupancy.Clear;
        public bool SwitchPosition(string id,out bool value){value=false;return true;}
        public bool SetSwitch(string id,bool value)=>true;
        public bool CaptureTrain(RouteDefinition route,out string[] cars){cars=new[]{"assigned-train"};return true;}
        public bool TrainProgress(RouteDefinition route,string[] cars,out double head,out double tail){head=Head;tail=-20;return true;}
        public double EntryPassageOffset(RouteDefinition route,double normal)=>BlockSignalPolicy.Passage(route,Boundary,normal);
    }
    static Fixture Setup(string root,bool middle=false) {
        string key=new string('e',64);var repo=new MarkerRepository(Path.Combine(root,"build","block-update-"+Guid.NewGuid().ToString("N")),key,"map");
        var left=repo.Put(new MarkerAnchor{Segment="S",Meters=0},kind:MarkerRecord.Main);var right=repo.Put(new MarkerAnchor{Segment="S",Meters=3000},kind:MarkerRecord.Main);
        foreach(var marker in new[]{left,right}){var mast=MastConfiguration.Effective(marker);mast.Back.Enabled=true;repo.ConfigureMast(marker.Id,mast);}
        var lib=new DeskLibrary{WorldKey=key};var a=new DeskDefinition{Code="A",Name="A",Panel=new PanelDocument{WorldKey=key}};var b=new DeskDefinition{Code="B",Name="B",Panel=new PanelDocument{WorldKey=key}};
        var master=new DeskDefinition{Code="ESTW",Name="Master",Global=true,Panel=new PanelDocument{WorldKey=key}};lib.Desks.AddRange(new[]{master,a,b});
        foreach(var pair in new[]{(Desk:a,Signal:left),(Desk:b,Signal:right)}) {var mast=MastConfiguration.Effective(pair.Signal);pair.Desk.Panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Signal,Reference=mast.Front.Id});pair.Desk.Panel.Put(new PanelTile{X=1,Y=2,Kind=PanelTileKind.Signal,Reference=mast.Back.Id,Rotation=2});}
        var line=new LineTrackDefinition{Code="STR-001",Name="A-B",StationA=a.Id,StationB=b.Id,ForwardStart=left.Id,ForwardEnd=right.Id,ReverseStart=MastConfiguration.Effective(right).Back.Id,ReverseEnd=MastConfiguration.Effective(left).Back.Id,
            Forward=new List<DirectedRouteSection>{P("S",0,3000)},Reverse=new List<DirectedRouteSection>{P("S",0,3000,true)},Topology=new string('f',64)};
        foreach(string id in LineSignalGroups.All(line))line.Anchors[id]="fixture";lib.FreeLines.Add(line.Clone());
        if(middle){var center=repo.Put(new MarkerAnchor{Segment="S",Meters=1500},kind:MarkerRecord.Main);var mast=MastConfiguration.Effective(center);mast.Back.Enabled=true;repo.ConfigureMast(center.Id,mast);
            a.Panel.Put(new PanelTile{X=4,Y=4,Kind=PanelTileKind.Signal,Reference=center.Id});a.Panel.Put(new PanelTile{X=4,Y=5,Kind=PanelTileKind.Signal,Reference=mast.Back.Id,Rotation=2});}
        return new Fixture{Library=lib,Objects=repo.Snapshot(),Line=line,A=a.Id,B=b.Id,Master=master.Id};
    }
    public static void Run(string root) {
        var f=Setup(root);var plan=BlockLayoutPlan.Create(f.Line.Axis,3);var options=new MarkerOptions{UseGameSpacing=false,Lateral=3.7,Height=.2,Scale=1.2};
        var build=LineBlockBuilder.Build(f.Objects,f.Library,f.Line,plan,f.A,true,options,10,new[]{f.B});var line=build.Line;var owner=build.Library.Desks.Single(d=>d.Id==f.A);var viewer=build.Library.Desks.Single(d=>d.Id==f.B);
        Check(line.BlockUnits.Count==2&&build.Objects.Markers.Count(m=>m.Kind==MarkerRecord.Main)==6&&build.Objects.Markers.Count(m=>m.Kind==MarkerRecord.CircuitB)==2,"block units use two separate one-direction signal masts and one shared physical detector per boundary");
        foreach(var unit in line.BlockUnits) {
            var a=build.Objects.Markers.Single(m=>m.Id==unit.Forward);var b=build.Objects.Markers.Single(m=>m.Id==unit.Reverse);var box=build.Objects.Markers.Single(m=>m.Id==unit.Boundary);
            Check(a.Anchor.Meters<box.Anchor.Meters&&b.Anchor.Meters>box.Anchor.Meters&&a.FacingReverse!=b.FacingReverse&&a.CircuitVariant==0&&b.CircuitVariant==0,"both signal positions straddle their common boundary without creating duplicate detector endpoints");
            Check(a.Anchor.Lateral==3.7&&b.Anchor.Lateral==-3.7&&a.Anchor.Height==.2&&a.Anchor.Scale==1.2&&!a.UseHostPlacement,"automatic block placement preserves resolved game spacing, side, height and scale");
        }
        var area=BlockDiagram.Area(owner.Panel,line.Id);var parts=BlockDiagram.Tiles(owner.Panel,line.Id);
        Check(parts.Where(t=>PanelConnections.Ports(t)!=0).Select(t=>t.Y).Distinct().Count()==1&&owner.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.Signal&&line.BlockUnits.Any(u=>u.Forward==t.Reference||u.Reverse==t.Reference)).Count()==4,"the bidirectional block assembly draws one rail line with independent opposing signal fields");
        Check(BlockDiagram.Tiles(viewer.Panel,line.Id).Where(t=>t.Kind==PanelTileKind.Signal||t.Kind==PanelTileKind.LinePermission).All(t=>t.ReadOnlySignal)&&line.BlockUnits.All(u=>StationAutomationPolicy.Owner(build.Library,u.Forward)?.Id==f.A),"additional diagrams show read-only signals and permission indicators without changing the sole signal owner");
        var index=new NumberMirrorIndex(build.Library);Check(viewer.Panel.Tiles.Where(NumberMirrors.IsMirror).All(t=>index.Source(t)?.Desk.Id==f.A),"repeated block number displays share the original source rather than creating independent numbers");
        Placement();
        Views(build,f,plan,options);
        var drag=new PanelDrag();drag.Begin(owner.Panel,null,area.X+5,area.Y+1);drag.Update(owner.Panel,area.X+6,area.Y+5);
        var ids=parts.Select(t=>t.NumberId).Where(id=>id!=null).ToArray();Check(drag.Source.Width==area.Width&&drag.Source.Height==area.Height&&drag.Valid,"grabbing a single block symbol automatically selects the complete block assembly for dragging");
        drag.Apply(owner.Panel);Check(BlockDiagram.Area(owner.Panel,line.Id).Y==area.Y+4&&ids.All(id=>owner.Panel.Tiles.Any(t=>t.NumberId==id)),"moving the assembly preserves its group marker and every number-source identity");
        var auxiliary=new AuxiliaryTarget{Function=AuxiliaryFunction.LinePermission,PermissionLine=line.Id};
        owner.Panel.Put(new PanelTile{X=23,Y=11,Kind=PanelTileKind.AuxiliaryTarget,Target=auxiliary});
        var module=owner.Panel.Tiles.Single(t=>t.Kind==PanelTileKind.LinePermission&&t.Reference==line.Id);module.PermissionAuxiliary=auxiliary.Id;module.Label="permission";
        var signalField=owner.Panel.Tiles.Single(t=>t.Kind==PanelTileKind.Signal&&t.Reference==line.BlockUnits[0].Forward);signalField.Label="custom label";signalField.LinkedAuxiliary=auxiliary.Id;
        var repeated=LineBlockBuilder.Build(build.Objects,build.Library,line,plan,f.A,true,options,10,new[]{f.B});
        var repeatedPanel=repeated.Library.Desks.Single(d=>d.Id==f.A).Panel;
        Check(repeatedPanel.Tiles.Single(t=>t.Kind==PanelTileKind.LinePermission&&t.Reference==line.Id).PermissionAuxiliary==auxiliary.Id&&repeatedPanel.Tiles.Single(t=>t.Kind==PanelTileKind.Signal&&t.Reference==signalField.Reference).LinkedAuxiliary==auxiliary.Id&&repeatedPanel.Tiles.Any(t=>t.Label=="custom label"),"rebuilding an existing assembly preserves its permission-button link, signal destination link and custom label");
        var oldModule=LineIndicatorTarget.Capture(repeated.Library,f.A,module.X,module.Y);var movedLibrary=repeated.Library.Clone();BlockDiagram.Move(movedLibrary.Desks.Single(d=>d.Id==f.A).Panel,line.Id,0,7);
        var newModule=oldModule.RebindAssembly(repeated.Library,movedLibrary,line);
        Check(newModule.Y==7&&newModule.RequireTarget(movedLibrary,line).Reference==line.Id&&oldModule.Y==module.Y,"the setup source follows a staged assembly move without changing the original button capture");
        var modified=build.Library.Clone();var modifiedPanel=modified.Desks.Single(d=>d.Id==f.A).Panel;var managed=BlockDiagram.Area(modifiedPanel,line.Id);
        modifiedPanel.Put(new PanelTile{X=managed.X+2,Y=managed.Y,Kind=PanelTileKind.Label,Label="manual content"});
        Check(Reject(()=>LineBlockBuilder.Build(build.Objects,modified,line,plan,f.A))&&modifiedPanel.At(managed.X+2,managed.Y).Label=="manual content","manually added assembly fields refuse a rebuild without deleting the user's content");
        var movedBoundary=BlockLayoutPlan.Create(line.Axis,3);movedBoundary.Boundaries[0]+=25;
        Check(Reject(()=>LineBlockBuilder.Build(build.Objects,build.Library,line,movedBoundary,f.A)),"an existing common boundary cannot be silently orphaned by editing the proposal distance");
        var transferred=LineBlockBuilder.Build(build.Objects,build.Library,line,plan,f.B,true,options,10,new[]{f.A});
        Check(transferred.NewSignals==0&&transferred.Line.BlockUnits.All(u=>StationAutomationPolicy.Owner(transferred.Library,u.Forward)?.Id==f.B)&&new NumberMirrorIndex(transferred.Library).Inputs(transferred.Library.Desks.Single(d=>d.Id==f.A).Panel.Tiles).All(t=>ids.Contains(t.NumberId)),"changing the operating interlocking transfers authority and keeps the former panel as a linked read-only view");
        var removedDisplay=LineBlockBuilder.Build(build.Objects,build.Library,line,plan,f.B,true,options,10,Array.Empty<string>());
        Check(oldModule.RebindAssembly(build.Library,removedDisplay.Library,line)==null,"removing the original diagram during control transfer does not leave a stale setup-button coordinate");
        var middle=Setup(root,true);var old=middle.Objects.Markers.Single(m=>m.Anchor.Meters==1500);var oldFace=MastConfiguration.Effective(old).Back.Id;
        var converted=LineBlockBuilder.Build(middle.Objects,middle.Library,middle.Line,BlockLayoutPlan.Create(middle.Line.Axis,2),middle.A);
        Check(converted.Line.BlockUnits.Single().Forward==old.Id&&converted.Line.BlockUnits.Single().Reverse==oldFace&&converted.Objects.Markers.Any(m=>m.Id==oldFace)&&converted.Objects.Markers.Single(m=>m.Id==converted.Line.BlockUnits[0].Boundary).CircuitNumber==old.CircuitNumber,"a legacy two-face block mast splits into separate masts while preserving both logical signal IDs and its detector number");
        var unit0=line.BlockUnits[0];var boundary0=build.Objects.Markers.Single(m=>m.Id==unit0.Boundary);
        var route=new RouteDefinition{Code="passage",Entry=unit0.Forward,Exit="next",Sections=new List<TrackSection>{S("S",990,1900)},Path=new List<DirectedRouteSection>{P("S",990,1900)},ViaSignals=new List<RouteViaSignal>(),Approach=new List<TrackSection>{S("S",900,990)}};
        Check(Math.Abs(BlockSignalPolicy.Passage(route,boundary0,5)-10.1)<.0001,"block signal return waits for the shared boundary beyond the staggered signal rather than any early GFM occupation");
        var passage=new PassageWorld{Boundary=boundary0};var engine=new InterlockingEngine{SignalPassageMeters=5};engine.Request(route,passage);for(int i=0;i<6;i++)engine.Tick(passage);
        passage.Head=6;engine.Tick(passage);Check(engine.ForSignal(route.Entry).Permitted&&!engine.ForSignal(route.Entry).StartPassageVerified,"a train between the staggered signal and common detector does not cause premature signal return");
        passage.Head=11;engine.Tick(passage);Check(!engine.ForSignal(route.Entry).Permitted&&engine.ForSignal(route.Entry).StartPassageVerified,"passing the common detector triggers the owning signal return and verified number-transfer event");
        route.Path=new List<DirectedRouteSection>{P("S",100,900)};Check(Reject(()=>BlockSignalPolicy.Passage(route,boundary0,5)),"a missing or unreachable common detector cannot be used for signal-passage proving");
        ChangesAndDirections(f,line);
    }
    static void Placement() {
        string line=Guid.NewGuid().ToString("N"),number=Guid.NewGuid().ToString("N");
        var panel=new PanelDocument{WorldKey=new string('e',64),Columns=25,Rows=8};
        panel.Areas.Add(new PanelArea{Id=line,Code="BL-TEST",Name="Placement",Kind="Strecke",BlockLine=line,X=0,Y=4,Width=25,Height=2});
        panel.Put(new PanelTile{X=3,Y=5,Kind=PanelTileKind.TrainNumber,NumberId=number,Ports=17});panel.Put(new PanelTile{X=1,Y=0,Kind=PanelTileKind.Label,Label="station"});
        string original=PanelHistory.Hash(panel);var drawing=BlockDiagramPlacement.Display(panel,line);
        Check(drawing.Columns>panel.Columns&&drawing.Rows>panel.Rows&&drawing.Tiles.Any(t=>t.NumberId==number)&&PanelHistory.Hash(panel)==original,"placement drawing provides clickable room beyond the panel while retaining the visible group and leaving the proposal unchanged");
        Check(BlockDiagramPlacement.Issue(panel,line,3,2)==null&&PanelHistory.Hash(panel)==original,"hovering a full-width assembly at a new offset is valid without modifying its current position");
        var moved=panel.Clone();var growth=BlockDiagramPlacement.Move(moved,line,3,2);var area=BlockDiagram.Area(moved,line);
        Check(area.X==3&&area.Y==2&&moved.Columns==28&&growth.Right==3&&moved.Tiles.Single(t=>t.NumberId==number).X==6,"click placement uses the requested top-left exactly and grows a full-width panel to the right");
        BlockDiagramPlacement.Move(moved,line,4,2);area=BlockDiagram.Area(moved,line);
        Check(area.X==4&&area.Y==2&&moved.Tiles.Single(t=>t.NumberId==number).X==7,"a second placement continues from the current candidate instead of resetting to its original location");
        moved.Put(new PanelTile{X=26,Y=6,Kind=PanelTileKind.Label,Label="occupied"});string before=PanelHistory.Hash(moved);
        Check(BlockDiagramPlacement.Issue(moved,line,4,6)!=null&&Reject(()=>BlockDiagramPlacement.Move(moved,line,4,6))&&PanelHistory.Hash(moved)==before,"a colliding placement is rejected atomically without moving fields or growing the proposal");
        var another=new PanelArea{Code="OTHER",Name="Other overview",Kind="Strecke",NumberLine=Guid.NewGuid().ToString("N"),X=25,Y=0,Width=3,Height=1};moved.Areas.Add(another);
        Check(BlockDiagramPlacement.Issue(moved,line,5,0)!=null,"placement also protects the empty space reserved by another number overview");
        var shifted=moved.Clone();growth=BlockDiagramPlacement.Move(shifted,line,-2,-2);area=BlockDiagram.Area(shifted,line);
        Check(area.X==0&&area.Y==0&&growth.Left==2&&growth.Top==2&&shifted.OriginX==2&&shifted.OriginY==2&&shifted.Tiles.Single(t=>t.Label=="station").X==3&&shifted.Tiles.Single(t=>t.NumberId==number).X==3,"left/top placement grows the canvas and preserves relative group positions, source identity and surrounding fields");
        var full=moved.Clone();full.Columns=PanelDocument.MaxColumns;before=PanelHistory.Hash(full);
        Check(BlockDiagramPlacement.Issue(full,line,PanelDocument.MaxColumns-1,2)!=null&&Reject(()=>BlockDiagramPlacement.Move(full,line,PanelDocument.MaxColumns-1,2))&&PanelHistory.Hash(full)==before,"placement respects the hard diagram limit and leaves a rejected candidate untouched");
    }
    static void Views(LineBlockBuild build,Fixture f,BlockLayoutPlan plan,MarkerOptions placement) {
        var library=build.Library;var line=build.Line;var owner=library.Desks.Single(d=>d.Id==f.A);string ownerBefore=PanelHistory.Hash(owner.Panel);string technical=LineConfigurationChanges.Technical(line),automation=JsonConvert.SerializeObject(library.Automation);
        var sources=LineNumberOverview.AutomaticSources(library,line).Select(s=>s.Tile.NumberId).ToArray();
        var remote=library.Desks.Single(d=>d.Id==f.B).Panel;var remoteArea=BlockDiagram.Area(remote,line.Id);var remoteFields=BlockDiagram.Tiles(remote,line.Id);
        Check(remoteArea.Height==2&&remoteFields.Where(NumberMirrors.IsMirror).All(t=>t.InlineNumberMirror&&t.Y==remoteArea.Y+1)&&remoteFields.Where(t=>PanelConnections.Ports(t)!=0).All(t=>t.Y==remoteArea.Y+1),"generated remote number fields sit inside the same rail row instead of below the block assembly");
        Check(remoteFields.Where(NumberMirrors.IsMirror).All(t=>t.NumberId==null&&t.BlockId==""&&t.Section==null&&t.Ports==0),"inline repeated number fields remain pure displays without extra number sources or detector bindings");
        var turned=BlockDiagramViews.Edit(library,line.Id,f.A,new BlockViewOptions{Turn180=true});var turnedDoc=turned.Desks.Single(d=>d.Id==f.A).Panel;var turnedArea=BlockDiagram.Area(turnedDoc,line.Id);
        var sourceSignal=BlockDiagram.Tiles(owner.Panel,line.Id).First(t=>t.Kind==PanelTileKind.Signal);var turnedSignal=BlockDiagram.Tiles(turnedDoc,line.Id).Single(t=>t.Reference==sourceSignal.Reference);
        var sourceArea=BlockDiagram.Area(owner.Panel,line.Id);
        Check(turnedSignal.X-turnedArea.X==sourceArea.Width-1-(sourceSignal.X-sourceArea.X)&&turnedSignal.Y-turnedArea.Y==sourceArea.Height-1-(sourceSignal.Y-sourceArea.Y)&&turnedSignal.Rotation==(sourceSignal.Rotation+2)%4,"180-degree rotation changes both group coordinates and signal direction, independently of reflection");
        var unturned=BlockDiagramViews.Edit(turned,line.Id,f.A,new BlockViewOptions());
        Check(PanelHistory.Hash(unturned.Desks.Single(d=>d.Id==f.A).Panel)==ownerBefore,"turning the operating assembly by 180 degrees and back preserves all original bindings");
        var numbers=BlockDiagramViews.Edit(library,line.Id,f.B,new BlockViewOptions{Detail=BlockViewDetail.NumbersOnly,FirstSection=1,SectionCount=1});var doc=numbers.Desks.Single(d=>d.Id==f.B).Panel;var area=BlockDiagram.Area(doc,line.Id);var parts=BlockDiagram.Tiles(doc,line.Id);
        Check(area.Width==1&&area.Height==1&&parts.Length==1&&NumberMirrors.IsMirror(parts[0])&&!parts[0].InlineNumberMirror&&parts[0].NumberMirror==sources[1],"a remote block assembly can be edited down to just the requested one-number field");
        Check(PanelHistory.Hash(owner.Panel)==ownerBefore&&PanelHistory.Hash(numbers.Desks.Single(d=>d.Id==f.A).Panel)==ownerBefore&&LineConfigurationChanges.Technical(numbers.FreeLines.Single())==technical&&JsonConvert.SerializeObject(numbers.Automation)==automation,"changing a view leaves the source diagram, line safety configuration and automation unchanged");
        Check(line.BlockUnits.All(u=>StationAutomationPolicy.Owner(numbers,u.Forward)?.Id==f.A)&&new NumberMirrorIndex(numbers).Source(parts[0])?.Desk.Id==f.A,"reduced views keep the original number source and sole block signal owner");
        var reversed=BlockDiagramViews.Edit(numbers,line.Id,f.B,new BlockViewOptions{Detail=BlockViewDetail.NumbersOnly,SectionCount=2,Reverse=true});doc=reversed.Desks.Single(d=>d.Id==f.B).Panel;
        Check(BlockDiagram.Tiles(doc,line.Id).OrderBy(t=>t.X).Select(t=>t.NumberMirror).SequenceEqual(sources.Take(2).Reverse()),"horizontal reflection reverses the selected number order without exchanging their sources");
        var track=BlockDiagramViews.Edit(reversed,line.Id,f.B,new BlockViewOptions{Detail=BlockViewDetail.TrackAndNumbers,FirstSection=2,SectionCount=1});parts=BlockDiagram.Tiles(track.Desks.Single(d=>d.Id==f.B).Panel,line.Id);
        Check(parts.All(t=>t.Kind!=PanelTileKind.Signal)&&parts.Any(t=>PanelConnections.Ports(t)!=0)&&parts.Single(NumberMirrors.IsMirror).NumberMirror==sources[2],"track-and-number detail shows a selected section with occupancy rails and no signal symbols");
        var detailed=BlockDiagramViews.Edit(track,line.Id,f.B,new BlockViewOptions{FirstSection=1,SectionCount=1,Reverse=true,Turn180=true});doc=detailed.Desks.Single(d=>d.Id==f.B).Panel;area=BlockDiagram.Area(doc,line.Id);
        Check(area.Width==7&&BlockDiagram.Tiles(doc,line.Id).Any(t=>t.Kind==PanelTileKind.Signal)&&BlockDiagram.Tiles(doc,line.Id).Where(t=>t.Kind==PanelTileKind.Signal||t.Kind==PanelTileKind.LinePermission).All(t=>t.ReadOnlySignal),"a selected section can return to a mirrored complete display without gaining signal control");
        var loaded=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(detailed));loaded.Validate(loaded.WorldKey);
        Check(BlockDiagram.Area(loaded.Desks.Single(d=>d.Id==f.B).Panel,line.Id).BlockView.Reverse&&BlockDiagram.Area(loaded.Desks.Single(d=>d.Id==f.B).Panel,line.Id).BlockView.FirstSection==1,"saved detail, selected range and reflection survive a complete library reload");
        var mirroredOwner=BlockDiagramViews.Edit(library,line.Id,f.A,new BlockViewOptions{Reverse=true});
        Check(line.BlockUnits.All(u=>StationAutomationPolicy.Owner(mirroredOwner,u.Forward)?.Id==f.A)&&LineNumberOverview.AutomaticSources(mirroredOwner,line).Select(s=>s.Tile.NumberId).SequenceEqual(sources),"reflecting the operating assembly retains all signal owners and the physical order of number sources");
        var roundTrip=BlockDiagramViews.Edit(mirroredOwner,line.Id,f.A,new BlockViewOptions());
        Check(PanelHistory.Hash(roundTrip.Desks.Single(d=>d.Id==f.A).Panel)==ownerBefore,"reflecting and restoring the operating diagram preserves its complete field contents and source identities");
        Check(Reject(()=>BlockDiagramViews.Edit(library,line.Id,f.A,new BlockViewOptions{Detail=BlockViewDetail.NumbersOnly}))&&Reject(()=>BlockDiagramViews.Edit(library,line.Id,f.B,new BlockViewOptions{FirstSection=15})),"a reduced owning diagram and a nonexistent requested section are rejected before touching the saved library");
        var custom=library.Clone();doc=custom.Desks.Single(d=>d.Id==f.B).Panel;area=BlockDiagram.Area(doc,line.Id);var signal=doc.Tiles.First(t=>area.Contains(t.X,t.Y)&&t.Kind==PanelTileKind.Signal);signal.Label="Remote label";
        var auxiliary=new AuxiliaryTarget{Function=AuxiliaryFunction.LinePermission,PermissionLine=line.Id};doc.Put(new PanelTile{X=23,Y=11,Kind=PanelTileKind.AuxiliaryTarget,Target=auxiliary});doc.Tiles.First(t=>area.Contains(t.X,t.Y)&&t.Kind==PanelTileKind.LinePermission).PermissionAuxiliary=auxiliary.Id;
        var hidden=BlockDiagramViews.Edit(custom,line.Id,f.B,new BlockViewOptions{Detail=BlockViewDetail.NumbersOnly,Reverse=true});var restored=BlockDiagramViews.Edit(hidden,line.Id,f.B,new BlockViewOptions());doc=restored.Desks.Single(d=>d.Id==f.B).Panel;
        Check(doc.Tiles.Any(t=>t.Kind==PanelTileKind.Signal&&t.Reference==signal.Reference&&t.Label=="Remote label")&&doc.Tiles.Any(t=>t.Kind==PanelTileKind.LinePermission&&t.PermissionAuxiliary==auxiliary.Id),"hiding and restoring full detail retains custom display labels and permission links");
        var indicator=LineIndicatorTarget.Capture(custom,f.B,custom.Desks.Single(d=>d.Id==f.B).Panel.Tiles.First(t=>t.Kind==PanelTileKind.LinePermission).X,area.Y);
        Check(indicator.RebindAssembly(custom,hidden,line)==null,"an indicator capture is cleared when its display is intentionally reduced to numbers only");
        var elsewhere=library.Clone();var other=LineSetupWorkflow.Create(elsewhere,f.B);other.Track="other";LineSetupWorkflow.Put(elsewhere,other);elsewhere.Desks.Single(d=>d.Id==f.B).Panel.Put(new PanelTile{X=23,Y=10,Kind=PanelTileKind.LinePermission,Reference=other.Id});
        var otherIndicator=LineIndicatorTarget.Capture(elsewhere,f.B,23,10);var edited=BlockDiagramViews.Edit(elsewhere,line.Id,f.B,new BlockViewOptions{Detail=BlockViewDetail.NumbersOnly});var retained=otherIndicator.FollowView(elsewhere,edited,line);
        Check(retained.Reference==other.Id&&edited.Desks.Single(d=>d.Id==f.B).Panel.At(23,10).Reference==other.Id,"editing a block view cannot rebind a remembered permission field belonging to a different line");
        BlockDiagramPlacement.Move(edited.Desks.Single(d=>d.Id==f.B).Panel,line.Id,-2,-1);var shiftedCapture=otherIndicator.FollowView(elsewhere,edited,line);
        Check(shiftedCapture.Reference==other.Id&&shiftedCapture.X==25&&shiftedCapture.Y==11&&edited.Desks.Single(d=>d.Id==f.B).Panel.At(25,11).Reference==other.Id,"automatic left/top growth updates a remembered unrelated permission field's coordinates while retaining its own line binding");
        var again=LineBlockBuilder.Build(build.Objects,loaded,line,plan,f.A,true,placement,10,new[]{f.B});var savedView=BlockDiagram.Area(again.Library.Desks.Single(d=>d.Id==f.B).Panel,line.Id).BlockView;
        Check(savedView.Reverse&&savedView.Turn180&&savedView.FirstSection==1&&savedView.SectionCount==1&&again.NewSignals==0,"refreshing an existing block installation retains the edited per-panel view without creating new world signals");
        var replaced=LineBlockReplacement.Build(build.Objects,numbers,line,BlockLayoutPlan.Create(line.Axis,4),f.A,true,placement,10,new[]{f.B},Array.Empty<RouteDefinition>());doc=replaced.Library.Desks.Single(d=>d.Id==f.B).Panel;area=BlockDiagram.Area(doc,line.Id);
        Check(area.BlockView.Detail==BlockViewDetail.NumbersOnly&&area.BlockView.FirstSection==1&&BlockDiagram.Tiles(doc,line.Id).All(t=>new NumberMirrorIndex(replaced.Library).Source(t)!=null),"confirmed replacement of the physical block division retains a reduced view with valid new number sources");
        var moved=numbers.Clone();doc=moved.Desks.Single(d=>d.Id==f.B).Panel;area=BlockDiagram.Area(doc,line.Id);var drag=new PanelDrag();drag.Begin(doc,null,area.X,area.Y);drag.Update(doc,area.X+1,area.Y+2);drag.Apply(doc);
        Check(BlockDiagram.Area(doc,line.Id).BlockView.FirstSection==1&&BlockDiagram.Tiles(doc,line.Id).Single().NumberMirror==sources[1],"dragging a compact view preserves its profile and source link");
        var blocked=library.Clone();doc=blocked.Desks.Single(d=>d.Id==f.B).Panel;area=BlockDiagram.Area(doc,line.Id);doc.Put(new PanelTile{X=area.X+2,Y=area.Y,Kind=PanelTileKind.Label,Label="manual"});
        Check(Reject(()=>BlockDiagramViews.Edit(blocked,line.Id,f.B,new BlockViewOptions{Detail=BlockViewDetail.NumbersOnly}))&&doc.At(area.X+2,area.Y).Label=="manual","view editing refuses to erase manually added fields inside the managed assembly");
    }
    static void ChangesAndDirections(Fixture f,LineTrackDefinition line) {
        var other=f.Line.Clone();other.Id=Guid.NewGuid().ToString("N");other.Code="OTHER";other.Forward=new List<DirectedRouteSection>{P("Z",0,3000)};other.Reverse=new List<DirectedRouteSection>{P("Z",0,3000,true)};other.ForwardStart=Guid.NewGuid().ToString("N");other.ForwardEnd=Guid.NewGuid().ToString("N");other.ReverseStart=Guid.NewGuid().ToString("N");other.ReverseEnd=Guid.NewGuid().ToString("N");
        var route=new RouteRun{Route=new RouteDefinition{Code="unrelated",Entry="other-start",Exit="other-end",Path=new List<DirectedRouteSection>{P("Z",20,50)}}};
        var lease=new DirectionBlockLease{Id="lease",Axis=new List<DirectedRouteSection>{P("Z",0,3000)},Reason="other held"};
        var state=new LineDirectionState{Definition=other,Held=true};
        Check(LineConfigurationChanges.Blockers(new[]{line,other},new[]{line.Id},null,new[]{route},new[]{lease},new[]{state}).Length==0,"unrelated routes, block leases and retained line directions do not block a local line configuration change");
        Check(LineConfigurationChanges.Blockers(new[]{line,other},null,null,new[]{route},new[]{lease},new[]{state}).Length==3,"genuinely global changes still report all three concrete blocker kinds");
        var shown=line.Clone();shown.BlockDisplays.Clear();shown.Name="renamed";
        Check(LineConfigurationChanges.Changed(new[]{line},new[]{shown}).Length==0,"display destinations and readable names do not invalidate a held physical line");
        var authority=new LineDirectionAuthority{BlockEnabled=true,Occupancy=_=>TrackOccupancy.Clear};var active=line.Clone();active.Enabled=true;
        authority.Configure(new[]{active},_=>null);for(int i=1;i<=3;i++)authority.Observe(Array.Empty<RouteRun>(),i);
        var forward=new RouteDefinition{Id=Guid.NewGuid().ToString("N"),Path=new List<DirectedRouteSection>{P("S",100,500)}};
        using(var ticket=authority.Reserve(new[]{forward}))ticket.Commit();
        var reverse=new RouteDefinition{Id=Guid.NewGuid().ToString("N"),Path=new List<DirectedRouteSection>{P("S",2000,2500,true)}};
        Check(Reject(()=>authority.Reserve(new[]{reverse})),"opposing departures on separate free block intervals still conflict with the whole-line permission");
        var disabled=active.Clone();disabled.Enabled=false;var inactive=new LineDirectionAuthority{BlockEnabled=true,Occupancy=_=>TrackOccupancy.Clear};inactive.Configure(new[]{disabled},_=>null);
        Check(Reject(()=>inactive.Reserve(new[]{forward})),"an installed but inactive block line cannot silently bypass direction protection");
        var catalog=new[]{new DirectionBlock{Id="held",Axis=new List<DirectedRouteSection>{P("S",0,100)}}};var hold=new[]{new DirectionBlockLease{Id="held",Axis=new List<DirectedRouteSection>{P("S",0,100)}}};
        Check(DirectionCatalogChanges.CanReplace(catalog,catalog.Append(new DirectionBlock{Id="new",Axis=new List<DirectedRouteSection>{P("Z",0,100)}}),hold),"adding block boundaries on an unrelated track preserves an existing direction lease");
        Check(!DirectionCatalogChanges.CanReplace(catalog,new[]{new DirectionBlock{Id="held",Axis=new List<DirectedRouteSection>{P("S",0,90)}}},hold),"a changed held block axis cannot be silently substituted while its direction lease exists");
    }
}
