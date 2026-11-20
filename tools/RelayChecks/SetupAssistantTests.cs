using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class SetupAssistantTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static string Id()=>Guid.NewGuid().ToString("N");
    static string Key=new string('b',64);
    static DirectedRouteSection Part(string segment,double from,double to,bool reverse=false)=>new DirectedRouteSection{Section=new TrackSection{Segment=segment,From=from,To=to},Reverse=reverse};
    static DeskLibrary Library() =>new DeskLibrary{WorldKey=Key,Desks=new List<DeskDefinition>{new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey=Key}},new DeskDefinition{Code="WHB",Name="Whittier",Panel=new PanelDocument{WorldKey=Key}},new DeskDefinition{Code="ELA",Name="Ela",Panel=new PanelDocument{WorldKey=Key}}}};
    public static void Run(){Connections();Assignments();AdjacentPoints();Replacement();Capacity();}
    static void Connections() {
        var library=Library();var line=LineSetupWorkflow.Create(library,library.Desks[1].Id);line.Track=" Gleis 1 ";LineSetupWorkflow.Put(library,line);
        var duplicate=line.Clone();duplicate.Id=Id();duplicate.Code="STR-002";duplicate.Track="gleis 1";duplicate.StationA=line.StationB;duplicate.StationB=line.StationA;
        Check(LineSetupWorkflow.Connections(library,duplicate.StationA,duplicate.StationB,duplicate.Track).Single().Id==line.Id&&Reject(()=>LineSetupWorkflow.Put(library,duplicate)),"reversed neighbour order, case and spaces cannot create a duplicate line connection");
        line.Forward=new List<DirectedRouteSection>{Part("same physical track",0,100)};library.FreeLines[0]=line.Clone();duplicate.Track="Different label";duplicate.Forward=line.Forward.Select(p=>p.Clone()).ToList();
        Check(Reject(()=>LineSetupWorkflow.Put(library,duplicate)),"different labels cannot duplicate the same physical track even while its line definitions are inactive");duplicate.Forward.Clear();line.Forward.Clear();library.FreeLines[0]=line.Clone();
        duplicate.Track="2";LineSetupWorkflow.Put(library,duplicate);line.Name="Changed";LineSetupWorkflow.Put(library,line);
        Check(library.FreeLines.Count==2&&library.FreeLines.Single(l=>l.Id==line.Id).Name=="Changed","editing retains a line identity while a separate parallel track can be configured independently");
    }
    static void Assignments() {
        var library=Library();var signal=new MarkerRecord{Id=Id(),Name="HS-1",Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="S",Meters=5}};
        var distant=new MarkerRecord{Id=Id(),Name="VS-1",Kind=MarkerRecord.Distant,Anchor=new MarkerAnchor{Segment="S",Meters=3}};
        var network=new NetworkInput{Nodes=new List<PlanNode>{new PlanNode{Id="W",Point=true}}};
        var sources=SetupAssignments.Pending(library,new[]{signal,distant},network);string before=JsonConvert.SerializeObject(library);
        var next=SetupAssignments.Assign(library,sources.Single(s=>s.Id==signal.Id),library.Desks[1].Id,2,2,signal);
        Check(JsonConvert.SerializeObject(library)==before&&next.Desks[1].Panel.At(2,2).Reference==signal.Id,"assignment prepares a separate document and binds the chosen real signal to the exact selected cell");
        Check(Reject(()=>SetupAssignments.Assign(next,sources.Single(s=>s.Id==signal.Id),library.Desks[2].Id,3,3,signal)),"a second local desk cannot acquire the same controlling signal through the assistant");
        var point=sources.Single(s=>s.Point);next.Desks[2].Panel.Put(new PanelTile{X=3,Y=3,Kind=PanelTileKind.Switch});next=SetupAssignments.Assign(next,point,library.Desks[2].Id,3,3,null);
        Check(next.Points["W"].Desk==library.Desks[2].Id&&SetupAssignments.Pending(next,new[]{signal,distant},network).Single().Id==distant.Id,"completed point assignments leave the queue, while distant signals remain eligible for setup");
        Check(!SetupAssignments.CanAssign(new PanelTile{Kind=PanelTileKind.TrainNumber,NumberMirror=Id()},false)&&!SetupAssignments.CanAssign(new PanelTile{Kind=PanelTileKind.Straight,CircuitBoundary=Id()},false),"number mirrors and existing detection boundaries cannot be overwritten by a setup assignment");
        var loaded=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(next));Check(SetupAssignments.Pending(loaded,new[]{signal,distant},network).Length==1,"saved assignments resume without replaying already completed world objects");
    }
    static void AdjacentPoints() {
        for(int rotation=0;rotation<4;rotation++)foreach(bool mirror in new[]{false,true}) {
            var doc=new PanelDocument{WorldKey=Key};int mask=PanelConnections.Transform(1,rotation,mirror);int port=Enumerable.Range(0,8).Single(i=>mask==(1<<i));
            var first=new PanelTile{X=4,Y=4,Kind=PanelTileKind.Switch,Reference="W1",Ports=19,Stem=16,Normal=1,Rotation=rotation,Mirror=mirror,BlockId="p:W1"};
            var last=new PanelTile{X=4+PanelConnections.Dx[port],Y=4+PanelConnections.Dy[port],Kind=PanelTileKind.Switch,Reference="W2",Ports=25,Stem=1,Normal=16,Rotation=rotation,Mirror=mirror,BlockId="p:W2"};doc.Put(first);doc.Put(last);first=doc.At(first.X,first.Y);last=doc.At(last.X,last.Y);
            string a=TrackCircuit.PointId("W1",1),b=TrackCircuit.PointId("W2",1);bool Boundary(PanelTile t,string id)=>t.Kind==PanelTileKind.Switch&&TrackCircuit.TryPointId(id,out var node,out _)&&t.Reference==node;
            var block=new TrackBlock{Id="c:"+Id(),BoundaryA=a,BoundaryB=b,Name="Between points",Path=new List<DirectedRouteSection>{Part("S",10,20)}};
            Check(CircuitDiagramAssignment.Assign(doc,first.X,first.Y,block,Boundary)==2&&first.BlockId=="p:W1"&&last.BlockId=="p:W2"&&first.CircuitRails.Values.Single()==block.Id&&last.CircuitRails.Values.Single()==block.Id,"directly adjacent points share their actual connecting rail without an invented middle cell, rotation "+rotation+" mirror "+mirror);
            Check(Reject(()=>CircuitDiagramAssignment.Find(doc,first.X,first.Y,TrackCircuit.PointId("W1",2),b,Boundary)),"adjacent point detection still rejects the wrong physical point arm, rotation "+rotation+" mirror "+mirror);
            var library=Library();library.Desks[1].Panel=doc;library.Points["W1"]=new PointAssignment{Code="W1",Desk=library.Desks[1].Id,ShowCircuit=true};library.Points["W2"]=new PointAssignment{Code="W2",Desk=library.Desks[1].Id,ShowCircuit=true};
            // Point blocks are independently valid and remain untouched by circuit assignment.
            first.BlockId=last.BlockId="";var result=ExistingDiagramDetection.Build(library,library.Desks[1].Id,new NetworkResult{Blocks=new Dictionary<string,TrackBlock>{{block.Id,block}}},Boundary);
            Check(result.Circuits==1&&result.Fields==2,"automatic existing-diagram detection includes endpoint points as seeds, rotation "+rotation+" mirror "+mirror);
        }
    }
    static void Replacement() {
        var library=Library();var objects=new MarkerDocument{WorldKey=Key,Map="map"};
        var repository=new MarkerRepository("unused",Key,"map",()=>objects,d=>objects=d);
        var left=repository.Put(new MarkerAnchor{Segment="line",Meters=0},kind:MarkerRecord.Main);var right=repository.Put(new MarkerAnchor{Segment="line",Meters=3000},kind:MarkerRecord.Main);
        library.Desks[1].Panel.Put(new PanelTile{X=1,Y=6,Kind=PanelTileKind.Signal,Reference=left.Id});library.Desks[2].Panel.Put(new PanelTile{X=1,Y=6,Kind=PanelTileKind.Signal,Reference=right.Id});
        var line=LineSetupWorkflow.Create(library,library.Desks[1].Id);line.ForwardStart=left.Id;line.ForwardEnd=right.Id;line.ReverseStart=MastConfiguration.Effective(right).Back.Id;line.ReverseEnd=MastConfiguration.Effective(left).Back.Id;
        line.Forward=new List<DirectedRouteSection>{Part("line",0,3000)};line.Reverse=new List<DirectedRouteSection>{Part("line",0,3000,true)};
        var initial=LineBlockBuilder.Build(objects,library,line,BlockLayoutPlan.Create(line.Axis,2),library.Desks[1].Id,true,displays:new[]{library.Desks[2].Id});
        var owner=initial.Library.Desks[1].Id;var sources=LineNumberOverview.AutomaticSources(initial.Library,initial.Line);
        initial.Library=LineNumberOverview.Create(initial.Library,line.Id,library.Desks[0].Id,sources);
        var overview=initial.Library.Desks[0].Panel.Areas.Single(a=>a.NumberLine==line.Id);string overviewId=overview.Id;
        string before=JsonConvert.SerializeObject(initial);
        var build=LineBlockReplacement.Build(initial.Objects,initial.Library,initial.Line,BlockLayoutPlan.Create(line.Axis,10),owner,true,null,10,new[]{library.Desks[2].Id},Array.Empty<RouteDefinition>());
        Check(JsonConvert.SerializeObject(initial)==before,"replacement preview never removes existing world signals, contacts, detection or panels before confirmation");
        Check(build.Line.Id==initial.Line.Id&&build.Library.FreeLines.Count==1&&build.Line.BlockSections.Count==10&&build.Line.BlockUnits.Count==9&&build.RemovedSignals==2,"changing two blocks to ten replaces one installation under its original connection identity");
        var oldHeads=initial.Line.BlockUnits.SelectMany(u=>new[]{u.Forward,u.Reverse}).ToHashSet();
        Check(!build.Objects.Markers.Any(m=>oldHeads.Contains(m.Id))&&!build.Library.Automation.Contacts.Any(c=>oldHeads.Contains(c.Signal))&&initial.Line.BlockSections.All(id=>!build.Library.Blocks.ContainsKey(id)),"old block signals, their automatic contacts and obsolete detection sections are absent from the replacement");
        var newOverview=build.Library.Desks[0].Panel.Areas.Single(a=>a.NumberLine==line.Id);
        Check(newOverview.Id==overviewId&&newOverview.Width==10&&new NumberMirrorIndex(build.Library)!=null,"complete compact overviews retain their group identity and mirror all ten new number sources");
        var manual=new RouteDefinition{Code="Manual old block route",Entry=initial.Line.BlockUnits[0].Forward,Exit=right.Id};
        Check(Reject(()=>LineBlockReplacement.Build(initial.Objects,initial.Library,initial.Line,BlockLayoutPlan.Create(line.Axis,3),owner,false,null,10,null,new[]{manual})),"saved routes referring to removed block signals prevent replacement with an actionable external-reference error");
        var extra=initial.Library.Clone();extra.Desks[1].Panel.Put(new PanelTile{X=20,Y=10,Kind=PanelTileKind.TrainNumber,BlockId=initial.Line.BlockSections[0],NumberId=Id()});
        Check(Reject(()=>LineBlockReplacement.Build(initial.Objects,extra,initial.Line,BlockLayoutPlan.Create(line.Axis,3),owner,false,null,10,null,Array.Empty<RouteDefinition>())),"manually drawn external detection fields cannot be silently orphaned during replacement");
        var shrink=LineBlockReplacement.Build(build.Objects,build.Library,build.Line,BlockLayoutPlan.Create(line.Axis,1),owner,false,null,10,Array.Empty<string>(),Array.Empty<RouteDefinition>());
        Check(shrink.Line.BlockUnits.Count==0&&shrink.Line.BlockSections.Count==1&&shrink.Library.Desks[0].Panel.Areas.Single(a=>a.NumberLine==line.Id).Width==1,"replacement also shrinks an installation to a single block and updates its compact overview");
    }
    static void Capacity() {
        var library=Library();var station=library.Desks[2];string entry=Id(),other=Id();
        library.Desks[1].Panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Signal,Reference=other});station.Panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Signal,Reference=entry});
        for(int i=0;i<2;i++) {var block=new TrackBlock{Id="c:"+Id(),Name="Receiving "+i,BoundaryA=Id(),BoundaryB=Id(),Path=new List<DirectedRouteSection>{Part("track"+i,0,100)}};library.Blocks[block.Id]=block;station.ReceivingTracks.Add(new ReceivingTrack{Name="Track "+i,Blocks=new List<string>{block.Id},Arrivals=new List<string>{entry}});}
        station.MaximumReceivingTrains=2;library.Validate(Key);var states=library.Blocks.Keys.ToDictionary(id=>id,id=>TrackOccupancy.Clear);var active=new List<RouteRun>();double now=0;
        var authority=new StationCapacityAuthority{Library=()=>library,Active=()=>active,Occupancy=b=>states[b.Id],Time=()=>now};
        RouteDefinition Request()=>new RouteDefinition{Entry=other,Exit=entry,Path=new List<DirectedRouteSection>{Part("approach",0,100)}};
        var first=Request();using(var ticket=authority.Reserve(new[]{first}))ticket.Commit();var run=new RouteRun{Route=first,Stage=RouteStage.Set};active.Add(run);
        var second=Request();using(var ticket=authority.Reserve(new[]{second}))ticket.Commit();active.Add(new RouteRun{Route=second,Stage=RouteStage.Set});
        Check(Reject(()=>authority.Reserve(new[]{first})),"stable automatic route IDs cannot reuse an older train receiving reservation");
        Check(authority.Reservations==2&&Reject(()=>authority.Reserve(new[]{Request()})),"pending incoming routes reserve different receiving tracks and prevent a third train being accepted");
        active[1].Stage=RouteStage.Cancelled;now++;authority.Observe(now);active.RemoveAt(1);for(int i=0;i<3;i++){now++;authority.Observe(now);}Check(authority.Reservations==1,"cancelled unentered arrival releases its capacity only after three distinct clear checks");
        states[station.ReceivingTracks[0].Blocks[0]]=TrackOccupancy.Occupied;run.Entered=true;now++;authority.Observe(now);active.Clear();
        Check(authority.Reservations==1,"an outgoing or completed route cannot free receiving capacity while its actual detection remains occupied");
        states[station.ReceivingTracks[0].Blocks[0]]=TrackOccupancy.Clear;for(int i=0;i<3;i++){now++;authority.Observe(now);}Check(authority.Reservations==0,"a used receiving track becomes available only after occupancy has ended and separate clear proofs succeed");
        states[station.ReceivingTracks[0].Blocks[0]]=TrackOccupancy.Unknown;states[station.ReceivingTracks[1].Blocks[0]]=TrackOccupancy.Occupied;
        Check(Reject(()=>authority.Reserve(new[]{Request()})),"unknown detection cannot be counted as free receiving capacity");
        foreach(string id in states.Keys.ToArray())states[id]=TrackOccupancy.Clear;
        using(authority.Reserve(new[]{Request()})){}Check(authority.Reservations==0,"a failed or abandoned overall route transaction rolls back its receiving reservation");
        var loaded=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(library));Check(loaded.Desks[2].MaximumReceivingTrains==2&&loaded.Desks[2].ReceivingTracks.Count==2,"receiving-track definitions and configurable limits survive save and shared-configuration persistence");
        var overlap=library.Clone();overlap.Blocks[overlap.Desks[2].ReceivingTracks[1].Blocks[0]].Path=overlap.Blocks[overlap.Desks[2].ReceivingTracks[0].Blocks[0]].Path.Select(p=>p.Clone()).ToList();
        Check(Reject(()=>StationCapacity.Validate(overlap)),"different detector IDs on the same physical track cannot inflate receiving capacity");
        var routeToTrack2=Request();routeToTrack2.Entry=entry;routeToTrack2.Exit=entry;routeToTrack2.Path=new List<DirectedRouteSection>{Part("track1",0,100)};
        var approach=Request();using(var ticket=authority.Reserve(new[]{approach}))ticket.Commit();
        using(var ticket=authority.Reserve(new[]{routeToTrack2}))ticket.Commit();
        Check(!authority.TrackStatus(station.Id,station.ReceivingTracks[0].Id).Contains("reserved")&&authority.TrackStatus(station.Id,station.ReceivingTracks[1].Id).Contains("reserved"),"choosing a later local receiving route transfers its approach reservation onto the actual receiving track");
        var otherWorld=library.Clone();otherWorld.WorldKey=new string('c',64);authority.Library=()=>otherWorld;
        using(authority.Reserve(new[]{Request()})){}Check(authority.Reservations==0,"changing worlds discards only the previous world runtime reservations");
        var clone=library.Clone();clone.Desks[2].ReceivingTracks[0].Blocks.Clear();Check(station.ReceivingTracks[0].Blocks.Count==1,"receiving track edits use deep clones rather than modifying the active library");
    }
}
