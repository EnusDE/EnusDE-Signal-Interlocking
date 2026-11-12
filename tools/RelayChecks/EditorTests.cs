using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;

public static class EditorTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static PanelDocument Blank()=>new PanelDocument{WorldKey="editor",Columns=24,Rows=12};
    static string Data(PanelDocument doc)=>JsonConvert.SerializeObject(doc);
    static bool Linked(PanelDocument doc){foreach(var t in doc.Tiles)foreach(int i in Enumerable.Range(0,8))if((PanelConnections.Ports(t)&(1<<i))!=0){var neighbor=doc.At(t.X+PanelConnections.Dx[i],t.Y+PanelConnections.Dy[i]);if(neighbor!=null&&(PanelConnections.Ports(neighbor)&(1<<((i+4)%8)))==0)return false;}return true;}
    public static void Run(string root) {
        TestRouteProposals();
        TestAuxiliaryTargets();
        TestCircuitAndDispatcherChanges(root);
        TestCircuitIdentities();
        var split=Blank();
        var pending=PanelEditing.PreviewField(split,1,1,PanelTileKind.Signal);
        pending.Reference="preselected";
        Check(split.At(1,1)==null&&pending.Reference=="preselected","world-picked signal stages a drawing preview without writing the board");
        split.Put(new PanelTile{X=3,Y=2,Kind=PanelTileKind.Signal,Reference="main",AlternateSignal="shunt",Ports=17});
        var restored=JsonConvert.DeserializeObject<PanelDocument>(Data(split));
        Check(restored.At(3,2).SameContent(split.At(3,2))&&restored.At(3,2).AlternateSignal=="shunt"&&
            PanelConnections.Ports(restored.At(3,2))==17,"split field round-trips both stable signal IDs without changing the track connections");
        Check(!Data(Blank()).Contains("AlternateSignal")&&!JsonConvert.SerializeObject(new PanelTile()).Contains("AlternateSignal"),
            "ordinary legacy cells keep their old serialization when they have no second binding");
        var doc=Blank();PanelEditing.Draw(doc,1,5,10,5);Check(doc.Tiles.Count==10&&Linked(doc)&&doc.Tiles.All(t=>t.Reference==""&&t.Section==null),"one drawing gesture creates a continuous track chain without inventing physical IDs or occupancy");
        PanelEditing.Draw(doc,5,5,8,2);Check(doc.At(5,5).Kind==PanelTileKind.Switch&&PanelConnections.Count(doc.At(5,5).Ports)==3&&Linked(doc),"drawing a branch adapts the junction automatically and joins diagonal neighbors continuously");
        int stem,normal,branch;PanelConnections.Arms(doc.At(5,5),out stem,out normal,out branch);Check(stem==(1<<4)&&normal==(1<<0)&&branch==(1<<1),"automatic turnout uses the straight continuation as normal branch and the diagonal as reverse branch");
        var arms=doc.At(5,5);Check(Enumerable.Range(0,3).Count(i=>PanelConnections.LineActive(arms,i,false))==2&&Enumerable.Range(0,3).Count(i=>PanelConnections.LineActive(arms,i,true))==2&&PanelConnections.LineActive(arms,0,false)!=PanelConnections.LineActive(arms,0,true),"live turnout depiction selects the stem plus exactly the matching normal or reverse branch");
        Check(ReferenceEquals(PanelConnections.Lines(arms),PanelConnections.Lines(arms.Clone())),"repeated rendering of equal custom glyphs reuses bounded geometry instead of allocating rail shapes per frame");
        var point=doc.At(5,5);point.Stem=stem;point.Normal=branch;PanelEditing.Draw(doc,1,5,10,5);Check(doc.At(5,5).Stem==stem&&doc.At(5,5).Normal==branch,"redrawing existing rails retains the user's configured turnout branch mapping");
        doc=Blank();PanelEditing.Draw(doc,1,5,9,5);PanelEditing.Draw(doc,4,2,4,9);Check(doc.At(4,5).Kind==PanelTileKind.Cross&&PanelConnections.Count(doc.At(4,5).Ports)==4&&Linked(doc),"crossing a drawn line generates a continuous four-arm crossing symbol");
        doc=Blank();PanelEditing.Draw(doc,1,1,9,4);Check(doc.Tiles.Any(t=>t.Kind==PanelTileKind.Curve)&&Linked(doc),"unequal horizontal and vertical distances create joined diagonal and straight sections with an automatic bend");
        doc=Blank();doc.Put(new PanelTile{X=4,Y=3,Kind=PanelTileKind.Label,Label="Bahnhof"});string original=Data(doc);Check(Reject(()=>PanelEditing.Draw(doc,1,3,8,3))&&Data(doc)==original,"a label in the drawing path rejects the complete gesture without partial rail edits");
        doc=Blank();doc.Put(new PanelTile{X=4,Y=5,Kind=PanelTileKind.Signal,Reference="A"});original=Data(doc);Check(Reject(()=>PanelEditing.Draw(doc,4,2,4,8))&&Data(doc)==original,"a signal tile cannot silently become a junction or lose its physical binding");
        doc=Blank();doc.Put(new PanelTile{X=4,Y=5,Kind=PanelTileKind.Switch,Reference="W1",Ports=(1<<0)|(1<<4)|(1<<1)});original=Data(doc);Check(Reject(()=>PanelEditing.Draw(doc,4,5,4,9))&&Data(doc)==original,"a bound turnout cannot silently be converted into an unrelated four-arm crossing");
        doc=Blank();doc.Put(new PanelTile{X=4,Y=5,Section=new TrackSection{Segment="S1",From=10,To=30}});original=Data(doc);Check(Reject(()=>PanelEditing.Draw(doc,4,5,6,3))&&Data(doc)==original,"drawing a turnout over an assigned track section rejects rather than discarding occupancy metadata");
        bool transforms=true;for(int mask=1;mask<256;mask++)for(int rotation=0;rotation<4;rotation++)foreach(bool mirror in new[]{false,true})transforms&=PanelConnections.Transform(PanelConnections.Transform(mask,rotation,mirror),rotation,mirror,true)==mask;Check(transforms,"all eight-direction connection masks round-trip through every rotation and mirror");
        var diagonal=new PanelTile{Kind=PanelTileKind.Curve,Ports=(1<<0)|(1<<3),Rotation=1,Mirror=true};bool endpoints=true;foreach(var line in PanelConnections.Lines(diagonal)){var p=PanelGeometry.World(line.B,diagonal.Rotation,diagonal.Mirror,0,0);int dir=PanelConnections.Direction((int)Math.Sign(p.X),-(int)Math.Sign(p.Y));endpoints&=(PanelConnections.Ports(diagonal)&(1<<dir))!=0;}Check(endpoints,"rendered custom connections use the same rotated and mirrored grid ports as the drawing model");
        doc=Blank();doc.Put(new PanelTile{X=2,Y=2,Kind=PanelTileKind.Signal,Reference="A",Label="P1"});doc.Put(new PanelTile{X=4,Y=5,Kind=PanelTileKind.Switch,Reference="W1",Ports=19});doc.Put(new PanelTile{X=1,Y=8,Section=new TrackSection{Segment="S1",From=0,To=25}});PanelEditing.Insert(doc,true,4);Check(doc.Rows==13&&doc.At(2,2).Reference=="A"&&doc.At(4,6).Reference=="W1"&&doc.At(1,9).Section.To==25&&doc.Tiles.All(t=>t.Y!=4),"inserting a middle row shifts only subsequent rows and preserves signal, turnout and occupancy bindings");
        PanelEditing.Insert(doc,false,3);Check(doc.Columns==25&&doc.At(5,6).Reference=="W1"&&doc.At(2,2).Label=="P1"&&doc.Tiles.All(t=>t.X!=3),"inserting a middle column preserves earlier fields and moves subsequent columns intact");
        doc.Rows=320;original=Data(doc);Check(Reject(()=>PanelEditing.Insert(doc,true,50))&&Data(doc)==original,"row insertion at the canvas limit rejects transactionally without clipping the last row");doc.Columns=512;original=Data(doc);Check(Reject(()=>PanelEditing.Insert(doc,false,70))&&Data(doc)==original,"column insertion at the canvas limit cannot truncate the existing diagram");
        var local=Blank();local.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Signal,Reference="A",Label="Einfahrt"});var global=new PanelDocument{WorldKey="editor",Columns=96,Rows=48};original=Data(local);PanelEditing.Paste(global,local,20,10);Check(global.At(21,11).Reference=="A"&&Data(local)==original,"a locally built panel can be merged into the global ESTW without changing the local geometry or its stable signal ID");
        string before=Data(global);Check(Reject(()=>PanelEditing.Paste(global,local,20,10))&&Data(global)==before,"merging onto existing ESTW fields rejects the entire paste instead of overwriting them");
        var foreign=local.Clone();foreign.WorldKey="other";Check(Reject(()=>PanelEditing.Paste(global,foreign,30,10))&&Data(global)==before,"clipboard content from another railroad cannot be pasted into the current ESTW");
        doc=Blank();PanelEditing.Draw(doc,1,1,5,1);PanelEditing.Draw(doc,1,3,5,3);var deletion=new PanelDeletion(doc,new PanelRegion{X=0,Y=0,Width=24,Height=12});original=Data(doc);Check(deletion.Count==10&&Reject(()=>deletion.Apply(doc,false))&&Data(doc)==original,"delete-all requires an explicit confirmation and rejection leaves every drawing field intact");
        var subset=new PanelDeletion(doc,new PanelRegion{X=0,Y=0,Width=24,Height=2});subset.Apply(doc,true);Check(doc.Tiles.Count==5&&doc.Tiles.All(t=>t.Y==3),"confirmed area deletion removes only the selected drawing cells");
        doc=Blank();doc.Put(new PanelTile{X=1,Y=1});deletion=new PanelDeletion(doc,new PanelRegion{X=0,Y=0,Width=24,Height=12});doc.Put(new PanelTile{X=2,Y=2});original=Data(doc);Check(Reject(()=>deletion.Apply(doc,true))&&Data(doc)==original,"a stale delete confirmation cannot erase diagram changes made since the request");
        deletion=new PanelDeletion(doc,new PanelRegion{X=0,Y=0,Width=24,Height=12});deletion.Apply(doc,true);Check(doc.Tiles.Count==0&&doc.Columns==24&&doc.Rows==12,"confirmed delete-all clears the chosen board drawing while retaining its canvas dimensions");
        var window=PanelWindow.Visible(160,100,72,48,144,864,-480,-96);Check(window.Left==2&&window.Right==12&&window.Top==2&&window.Bottom==10&&window.Contains(6,7)&&!window.Contains(1,7),"viewport culling computes the visible portion of a large top-left-origin canvas");
        window=PanelWindow.Visible(160,100,72*.08f,48*.08f,-500,10000,-10000,500);Check(window.Left==0&&window.Top==0&&window.Right==159&&window.Bottom==99,"overview zoom clamps the drawing viewport to all valid cells without allocating empty-cell controls");
        int cx,cy;bool picking=true;foreach(float zoom in new[]{.005f,.08f,.5f,1f,2f})picking&=PanelPicking.Cell(160,100,zoom,72*zoom*15.5f,-48*zoom*23.5f,out cx,out cy)&&cx==15&&cy==23;Check(picking,"direct cell picking addresses the same signal or turnout at overview, half, normal and double zoom");
        Check(!PanelPicking.Cell(160,100,1,-1,-40,out cx,out cy)&&!PanelPicking.Cell(160,100,1,10,1,out cx,out cy)&&!PanelPicking.Cell(160,100,1,160*72,-40,out cx,out cy)&&!PanelPicking.Cell(160,100,1,float.NaN,0,out cx,out cy),"context-menu hit testing rejects outside-canvas and invalid pointer coordinates");
        Check(!PanelPicking.RightHalf(5,1,5*72+35.9f)&&PanelPicking.RightHalf(5,1,5*72+36)&&
            PanelPicking.RightHalf(5,.5f,5*36+18),"left and right signal buttons share one rectangular cell with an exact half-cell click boundary");
        Check(Reject(()=>PanelConnections.Validate(new PanelTile{Kind=PanelTileKind.Switch,Ports=17}))&&Reject(()=>PanelConnections.Validate(new PanelTile{Kind=PanelTileKind.Signal,Ports=19})),"invalid custom turnout or signal connections fail before being saved");
        var big=new PanelDocument{WorldKey="editor",Columns=160,Rows=100};for(int i=0;i<8192;i++)big.Tiles.Add(new PanelTile{X=i%160,Y=i/160,Ports=17});big.Validate("editor");string directory=Path.Combine(root,"build","editor-tests-"+Guid.NewGuid().ToString("N"));var store=new DeskStore(directory,"editor");var library=store.Load(big);Check(store.Load(null).Desks[0].Panel.Tiles.Count==8192,"the expanded canvas and full 8192-tile model persist and reload through the real desk store");
        int maxVerticesPerTile=4+4+8+4+11+20;Check(maxVerticesPerTile*PanelView.ChunkSize<65535&&((65537+PanelView.ChunkSize-1)/PanelView.ChunkSize)==65,"bounded mesh chunks stay below the native UI vertex limit for every supported tile and need at most 65 layers including a new draft");
        var type=typeof(PanelView);Check(type.GetInterfaces().Any(t=>t.FullName=="UnityEngine.EventSystems.IPointerClickHandler")&&type.GetInterfaces().Any(t=>t.FullName=="UnityEngine.EventSystems.IScrollHandler"),"compiled native grid provides direct right-click and mouse-wheel input interfaces");
        doc=Blank();
        foreach(int x in new[]{2,3,5})PanelEditing.PlaceField(doc,x,2,PanelTileKind.Label);
        original=Data(doc);
        Check(doc.Tiles.Count==3&&doc.Tiles.All(t=>t.Kind==PanelTileKind.Label)
            &&Reject(()=>PanelEditing.PlaceField(doc,3,2,PanelTileKind.Signal))&&Data(doc)==original,
            "repeated placement uses the same symbol and rejects overwriting an existing object");
        doc=Blank();PanelEditing.Draw(doc,2,5,8,5);
        int connections=PanelConnections.Ports(doc.At(5,5));
        doc.At(5,5).BlockId="block";
        PanelEditing.PlaceField(doc,5,5,PanelTileKind.Signal);
        Check(doc.At(5,5).Kind==PanelTileKind.Signal&&PanelConnections.Ports(doc.At(5,5))==connections&&doc.At(5,5).BlockId=="block"&&Linked(doc),
            "placing a signal in drawn track preserves both connections and the block binding");
        doc.At(5,5).Reference="A";
        deletion=new PanelDeletion(doc,new PanelRegion{X=5,Y=5},true);
        deletion.Apply(doc,true);
        Check(deletion.KeepsTrack&&doc.At(5,5).Kind==PanelTileKind.Straight&&doc.At(5,5).Reference==""&&doc.At(5,5).BlockId=="block"&&Linked(doc),
            "removing a connected signal glyph leaves its continuous track and block on the board");
        doc=Blank();PanelEditing.Draw(doc,2,2,6,6);
        var previous=doc.At(4,4);
        previous.Section=new TrackSection{Segment="real-track",From=2,To=16};
        previous.BlockId="existing-block";
        int diagonalPorts=PanelConnections.Ports(previous);
        PanelEditing.PlaceField(doc,4,4,PanelTileKind.TrainNumber);
        var number=doc.At(4,4);
        Check(number.Kind==PanelTileKind.TrainNumber&&number.Section?.Segment=="real-track"&&number.BlockId=="existing-block"
            &&PanelConnections.Ports(number)==diagonalPorts&&Linked(doc),"a train-number display preserves the original diagonal, attached section and track connections");
        number.Number="00145";number.Prefix="1";
        deletion=new PanelDeletion(doc,new PanelRegion{X=4,Y=4},true);deletion.Apply(doc,true);
        Check(deletion.KeepsTrack&&doc.At(4,4).Number==""&&doc.At(4,4).Prefix==""&&doc.At(4,4).Section?.Segment=="real-track"
            &&PanelConnections.Ports(doc.At(4,4))==diagonalPorts&&Linked(doc),"deleting a number display preserves the real section and only removes the display");
        doc=Blank();PanelEditing.PlaceField(doc,5,5,PanelTileKind.TrainNumber);
        PanelEditing.Draw(doc,2,5,8,5);
        string afterNumber=Data(doc);
        Check(doc.At(5,5).Kind==PanelTileKind.TrainNumber&&Linked(doc)&&Reject(()=>PanelEditing.Draw(doc,5,5,5,8))&&afterNumber==Data(doc),
            "drawing through a train-number field connects it without allowing an ambiguous third leg");
        var choices=new[]{new SignalSearchEntry("tech-one","HS-006","Whittier P1"),new SignalSearchEntry("tech-two","P1","Bridge"),new SignalSearchEntry("tech-three","RS-006","Depot")};
        Check(SignalSearch.Find(choices,"p1").Select(x=>x.Id).SequenceEqual(new[]{"tech-two","tech-one"})&&
            SignalSearch.Find(choices,"006 whittier").Single().Id=="tech-one"&&SignalSearch.Find(choices,"tech-three").Single().Id=="tech-three"&&
            SignalSearch.Find(choices,"missing").Length==0&&SignalSearch.Find(choices,"006",1).Length==1,
            "search accepts partial code, mast name, stable ID and multiple words, with exact code first and bounded results");
        var many=Enumerable.Range(1,73).Select(i=>new SignalSearchEntry("id"+i,"HS-"+i.ToString("D3"),""));
        Check(SignalSearch.Find(many,"",int.MaxValue).Length==73&&SignalSearch.Find(many,"073",int.MaxValue).Single().Code=="HS-073",
            "the compact scrolling picker can reach and filter all candidates beyond the old first twelve");
    }

    static void TestRouteProposals() {
        var graph=new NetworkInput{
            Nodes=new List<PlanNode>{new PlanNode{Id="a"},new PlanNode{Id="p",Point=true,Common="stem",Normal="main"},
                new PlanNode{Id="b"},new PlanNode{Id="c"}},
            Edges=new List<PlanEdge>{new PlanEdge{Id="stem",A="a",B="p",Length=50},new PlanEdge{Id="main",A="p",B="b",Length=100},new PlanEdge{Id="loop",A="c",B="p",Length=75}}
        };
        MarkerRecord Signal(string id,string edge,double meters,string kind=MarkerRecord.Main,bool reverse=false)=>new MarkerRecord{
            Id=id,Name=id,Kind=kind,Anchor=new MarkerAnchor{Segment=edge,Meters=meters},FacingReverse=reverse,AllowHp1=true,AllowHp2=true,AllowSh1=true
        };
        var start=Signal("A","stem",20);
        var main=Signal("B","main",50);
        var branch=Signal("C","loop",30,reverse:true);
        var intermediate=Signal("R","main",10,MarkerRecord.Shunt);
        var other=Signal("opposite","main",5,reverse:true);
        var distant=Signal("V","main",2,MarkerRecord.Distant);
        var after=Signal("D","main",80);
        var signals=new[]{start,main,branch,intermediate,other,distant,after};
        var targets=new HashSet<string>(signals.Select(s=>s.Id));
        var discover=new RouteDiscovery(graph,signals);
        var train=discover.Find(start,RouteKind.Train,targets);
        Check(train.Select(p=>p.Exit).SequenceEqual(new[]{"C","B"})&&train[0].Switches.Single().Thrown&&!train[1].Switches.Single().Thrown&&
            train[0].Path[1].Reverse&&train.All(p=>p.Switches[0].ReleaseAt==30),
            "automatic train route discovery follows both lawful turnout legs with the correct required position and direction");
        Check(discover.Find(start,RouteKind.Shunt,targets).Any(p=>p.Exit=="R")&&
            discover.Find(start,RouteKind.Shunt,targets).All(p=>p.Exit!="B")&&
            discover.Find(start,RouteKind.Slow,targets).All(p=>p.Kind==RouteKind.Slow),
            "shunting stops at the next shunt signal and slow routes retain the selected aspect type");
        Check(discover.Find(start,RouteKind.Train,new HashSet<string>{"D"}).Count==0&&
            discover.Find(start,RouteKind.Train,targets,1).Count==0&&
            discover.Find(Signal("return","loop",15),RouteKind.Train,new HashSet<string>{"B"}).Count==0,
            "search never skips an intermediate main signal, traverses branch-to-branch or keeps incomplete limited search results");
        var old=train[0];
        branch.Anchor.Meters=29;
        Check(!old.SamePath(discover.Find(start,RouteKind.Train,targets).Single(p=>p.Exit=="C")),"moved target invalidates an already proposed path");
        Check(RouteNaming.DeskCode("Whittier Bahnhof",Array.Empty<string>())=="WB"&&
            RouteNaming.DeskCode("Köln",new[]{"KOL","KOL2"})=="KOL3"&&
            RouteNaming.Next("WHB",RouteKind.Train,new[]{"F-001","WHB-ZF-003","WHB-RF-011"})=="WHB-ZF-004"&&
            RouteNaming.Next("WHB",RouteKind.Slow,new[]{"WHB-ZF-001"})=="WHB-LF-001"&&RouteNaming.KindCode(RouteKind.Shunt)=="RF",
            "generated station codes are collision-free and route numbering advances within the selected station and type");
    }

    static void TestCircuitAndDispatcherChanges(string root) {
        var tile=new PanelTile{Kind=PanelTileKind.Signal,Reference="shared",AlternateSignal="shared"};
        var buttons=new PanelButtons();
        buttons.Press("shared",_=>true,true);
        Check(buttons.StartHalf(tile,true)&&!buttons.StartHalf(tile,false),"one stable signal ID keeps its shunting half independently selected");
        tile.SwapSignalHalves=true;
        Check(buttons.StartHalf(tile,false)&&!buttons.StartHalf(tile,true),"swapping a split field moves the selected function with its symbol");
        buttons.Clear();
        buttons.Press("shared",_=>true);
        Check(!buttons.StartHalf(tile,false)&&buttons.StartHalf(tile,true),"train route selection uses the opposite half of the same signal");
        var aux=new PanelTile{Kind=PanelTileKind.AuxiliaryTarget,Rotation=3,Target=new AuxiliaryTarget{Reverse=true}};
        Check(SignalCellLayout.TargetDirection(aux)==1,"auxiliary direction arrow combines the diagram rotation and actual travel direction");

        var numbers=new Dictionary<string,string>{{"ESTW","100145"},{"WHB","00145"},{"Sylva","00200"},{"new","200145"}};
        var old=TrainNumbers.RemoveNumber(numbers,"200145",new HashSet<string>{"new"});
        Check(old.Length==2&&!numbers.ContainsKey("ESTW")&&!numbers.ContainsKey("WHB")&&
            numbers["new"]=="200145"&&numbers["Sylva"]=="00200","one five-digit train number is transferred across desks even with another optional prefix");
        Check(TrainNumbers.RemoveNumber(numbers,"",new HashSet<string>()).Length==0&&numbers.Count==2,
            "clearing a number field never transfers another assignment");
        Check(MarkerRecord.Caption("A Ost","Mast A","HS-01")=="A Ost"&&
            MarkerRecord.Caption(null,"Mast A","HS-01")=="Mast A"&&MarkerRecord.Caption(null,null,"HS-01")=="HS-01",
            "world and diagram caption fallbacks prioritize display name, mast name, then stable signal code");

        var graph=new NetworkInput();
        graph.Nodes.AddRange(new[]{new PlanNode{Id="a"},new PlanNode{Id="j",Point=true,Common="A",Normal="B"},
            new PlanNode{Id="b"},new PlanNode{Id="c"}});
        graph.Edges.AddRange(new[]{new PlanEdge{Id="A",A="a",B="j",Length=100},new PlanEdge{Id="B",A="j",B="b",Length=80},
            new PlanEdge{Id="C",A="j",B="c",Length=80}});
        var a=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Kind=MarkerRecord.CircuitA,Anchor=new MarkerAnchor{Segment="A",Meters=70}};
        var b=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Kind=MarkerRecord.Shunt,Anchor=new MarkerAnchor{Segment="C",Meters=15}};
        var path=TrackCircuit.Find(graph,a.Anchor,b.Anchor,out bool limit).Single();
        Check(!limit&&path.Length==45&&path.Points.SequenceEqual(new[]{"j"})&&
            path.Sections[0].Section.From==70&&path.Sections[1].Section.To==15,
            "a circuit from a box to a shunting signal uses exact anchors across the selected turnout leg");
        var reverse=TrackCircuit.Find(graph,b.Anchor,a.Anchor,out _).Single();
        Check(reverse.Length==45&&reverse.Sections.All(p=>p.Reverse),
            "a circuit follows the same connected segments in the reverse direction");
        Check(TrackCircuit.Find(graph,new MarkerAnchor{Segment="B",Meters=10},b.Anchor,out _).Count==0,
            "circuit planner rejects a nonexistent direct path between the two branch ends of a turnout");
        var block=new TrackBlock{Id="c:demo",Name="Test circuit",BoundaryA=a.Id,BoundaryB=b.Id,Path=path.Sections};
        var restored=JsonConvert.DeserializeObject<TrackBlock>(JsonConvert.SerializeObject(block));
        restored.Validate();
        Check(TrackCircuit.EndsMatch(restored,a,b)&&TrackCircuit.IsBoundary(a)&&!TrackCircuit.IsBoundary(new MarkerRecord{Kind=MarkerRecord.Distant}),
            "circuit persists its boundary identities and accepts stopping signals and boxes");
        b.Anchor.Meters+=1;
        Check(!TrackCircuit.EndsMatch(restored,a,b),"a moved boundary invalidates the previous occupancy section until rebound");

        var sectionDocument=Blank();
        var store=new DeskStore(Path.Combine(root,"build","circuit-"+Guid.NewGuid().ToString("N")),sectionDocument.WorldKey);
        var library=store.Load(sectionDocument);
        var active=library.Desks.Single().Id;
        sectionDocument.Put(new PanelTile{Kind=PanelTileKind.Straight,X=2,Y=2,BlockId=block.Id});
        store.PutPanel(library,active,sectionDocument,block);
        var loaded=store.Load(null);
        Check(loaded.Desks.Single().Panel.At(2,2).BlockId==block.Id&&loaded.Blocks[block.Id].BoundaryB==b.Id,
            "a section and its linked board field are stored together in one desk commit");
    }

    static void TestCircuitIdentities() {
        var oldA=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name="A-022",Kind=MarkerRecord.CircuitA};
        var oldB=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name="B-023",Kind=MarkerRecord.CircuitB};
        var signal=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name="RS-004",Kind=MarkerRecord.Shunt,CircuitVariant=1};
        var doc=new MarkerDocument{Markers=new List<MarkerRecord>{oldA,oldB,signal}};
        CircuitIdentity.Fill(doc);
        var ids=new[]{oldA.Id,oldB.Id,signal.Id};
        Check(CircuitIdentity.Caption(oldA)=="GFM-A-001"&&CircuitIdentity.Caption(oldB)=="GFM-B-002"&&
            CircuitIdentity.Caption(signal)=="GFM-A-003"&&ids.SequenceEqual(doc.Markers.Select(m=>m.Id)),
            "legacy A and B boxes and signal attachments share one numbering stream without changing world anchor identities");
        signal.CircuitVariant=0;
        CircuitIdentity.Fill(doc);
        Check(!CircuitIdentity.Enabled(signal)&&signal.CircuitNumber==3&&doc.NextCircuitNumber==4,
            "removing a signal attachment keeps its identity reserved");
        signal.CircuitVariant=2;
        var restored=JsonConvert.DeserializeObject<MarkerDocument>(JsonConvert.SerializeObject(doc));
        CircuitIdentity.Validate(restored);
        Check(CircuitIdentity.Caption(restored.Markers.Last())=="GFM-B-003"&&restored.NextCircuitNumber==4,
            "a reattached B box has the original entity number after replication");
        restored.Markers[1].CircuitNumber=1;
        Check(Reject(()=>CircuitIdentity.Validate(restored)),"identical GFM entity numbers are rejected across A and B variants");

        string port=TrackCircuit.PointId("switch-1",2);
        Check(TrackCircuit.ValidId(port)&&TrackCircuit.TryPointId(port,out var point,out int arm)&&point=="switch-1"&&arm==2&&
            !TrackCircuit.ValidId(TrackCircuit.PointId("switch-1",3)),"turnout boundary ID identifies its exact stable arm");
        var board=Blank();
        board.Put(new PanelTile{Kind=PanelTileKind.Straight,Ports=1,X=1,Y=1,CircuitBoundary=oldA.Id});
        var loaded=JsonConvert.DeserializeObject<PanelDocument>(Data(board));
        loaded.Validate(board.WorldKey);
        Check(loaded.At(1,1).SameContent(board.At(1,1))&&PanelConnections.Count(PanelConnections.Ports(loaded.At(1,1)))==1,
            "an independently drawn stop-end boundary and its track connection survive editor round-trip");
        var half=new PanelTile{Kind=PanelTileKind.Signal,X=2,Y=1,CircuitRails=new Dictionary<int,string>{{0,"right"},{4,"left"}}};
        var copy=half.Clone();
        copy.CircuitRails[0]="other";
        Check(!half.SameContent(copy)&&half.CircuitRails[0]=="right"&&half.CircuitRails[4]=="left",
            "two independently bound signal-side rails clone without losing their port associations");
        var stock=StockInvalidity.Request("native-head",false);
        stock.Darken=true;
        var received=JsonConvert.DeserializeObject<RetiredSignal>(JsonConvert.SerializeObject(stock));
        Check(received.Darken&&received.Visual&&!received.IgnoreAi&&received.Clone().Darken,
            "darkened stock lenses are optically requested independently of AI state and shared in save data");
        var foot=MarkerMeshData.CreateFoundation();
        Check(foot.Vertices.Min(v=>v[1])<0&&foot.Vertices.Max(v=>v[1])>.1f,"the mast foundation spans the local ground plane");
    }

    static void TestAuxiliaryTargets() {
        var target=new AuxiliaryTarget{Name="Ausziehgleis",Anchor=new MarkerAnchor{Segment="S",Meters=78},Train=true,Shunt=true};
        var document=Blank();
        PanelEditing.Draw(document,1,3,6,3);
        var field=PanelEditing.PreviewField(document,3,3,PanelTileKind.AuxiliaryTarget);
        field.Target=target;
        document.Put(field);
        var restored=JsonConvert.DeserializeObject<PanelDocument>(Data(document));
        Check(restored.At(3,3).Target.SameContent(target)&&Linked(restored)&&
            restored.At(3,3).SameContent(document.At(3,3)),"a split-purpose auxiliary target preserves its stable ID, anchor and two track connections on save and reload");
        var clone=document.Clone();
        clone.At(3,3).Target.Anchor.Meters=80;
        Check(document.At(3,3).Target.Anchor.Meters==78&&
            Reject(()=>AuxiliaryTarget.Index(document.Tiles.Concat(clone.Tiles)))&&
            AuxiliaryTarget.Index(document.Tiles.Concat(restored.Tiles)).Count==1,
            "mirrored target copies agree on one definition and divergent geometry is rejected");
        var endpoint=target.Endpoint();
        Check(!endpoint.IsSignal&&!SignalCatalogue.Supports(endpoint,false,false)&&
            AuxiliaryTarget.Accepts(endpoint,false)&&AuxiliaryTarget.Accepts(endpoint,true)&&
            RouteNaming.Next("WHB",RouteKind.Train,Array.Empty<string>(),true)=="WHB-HZF-001"&&
            RouteNaming.Next("WHB",RouteKind.Shunt,Array.Empty<string>(),true)=="WHB-HRF-001",
            "auxiliary destination is target-only, supports both configured movements and has independent numbering");
        var start=new MarkerRecord{Id="A",Name="A",Kind=MarkerRecord.Main,AllowSh1=true,Anchor=new MarkerAnchor{Segment="S",Meters=10}};
        var net=new NetworkInput{Nodes=new List<PlanNode>{new PlanNode{Id="n"},new PlanNode{Id="s"}},
            Edges=new List<PlanEdge>{new PlanEdge{Id="S",A="n",B="s",Length=100}}};
        var ids=new HashSet<string>{target.Id};
        var search=new RouteDiscovery(net,new[]{start},endpoint);
        var proposal=search.Find(start,RouteKind.Train,ids).Single();
        Check(proposal.Exit==target.Id&&proposal.Length==68&&search.Find(start,RouteKind.Shunt,ids).Single().Exit==target.Id&&
            new RouteDiscovery(net,new[]{start}).Find(start,RouteKind.Train,ids).Count==0,
            "both selected movement types reach the actual virtual endpoint and ordinary discovery does not insert one");
        target.Reverse=true;
        Check(new RouteDiscovery(net,new[]{start},target.Endpoint()).Find(start,RouteKind.Train,ids).Count==0,
            "an auxiliary target in the opposite direction cannot terminate a route");
        var intermediate=new MarkerRecord{Id="middle",Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="S",Meters=40}};
        target.Reverse=false;
        Check(AuxiliaryTarget.HasBlockingSignal(proposal.Path,new[]{start,intermediate},"A",false)&&
            new RouteDiscovery(net,new[]{start,intermediate},target.Endpoint()).Find(start,RouteKind.Train,ids).Count==0,
            "an intervening same-direction main signal is not skipped by a virtual destination");
        intermediate.Kind=MarkerRecord.Shunt;
        Check(!AuxiliaryTarget.HasBlockingSignal(proposal.Path,new[]{intermediate},"A",false)&&
            AuxiliaryTarget.HasBlockingSignal(proposal.Path,new[]{intermediate},"A",true),
            "a shunt signal remains a shunting boundary and can retain ordinary via-signal behavior for a train route");
        target.Shunt=false;
        Check(!AuxiliaryTarget.Accepts(target.Endpoint(),true)&&
            new RouteDiscovery(net,new[]{start},target.Endpoint()).Find(start,RouteKind.Shunt,ids).Count==0,
            "disabled auxiliary movement cannot create a proposal or pass destination validation");
        var first=new RouteDefinition{Entry="A",Exit=target.Id,AuxiliaryExit=true};
        var impossible=new RouteDefinition{Entry=target.Id,Exit="B"};
        Check(RouteChains.Follow(new[]{first,impossible},first).Routes.Count==1&&
            Reject(()=>RouteChains.ToTarget(new[]{first,impossible},"A","B",false,false)),
            "chain resolution ends at auxiliary destinations even if given an invalid continuation");
        var split=new PanelTile{Kind=PanelTileKind.Signal,Reference="main",AlternateSignal="shunt",Rotation=1,Ports=17,
            MainSignalDirection=2,ShuntSignalDirection=0};
        int ports=PanelConnections.Ports(split);
        Check(SignalCellLayout.SignalAt(split,false)=="main"&&SignalCellLayout.SignalAt(split,true)=="shunt",
            "default split click surfaces match main and shunt button roles");
        split.SwapSignalHalves=true;
        Check(SignalCellLayout.SignalAt(split,false)=="shunt"&&SignalCellLayout.ShuntHalf(split,false)&&
            SignalCellLayout.SignalAt(split,true)=="main"&&SignalCellLayout.MainDirection(split)==2&&
            SignalCellLayout.ShuntDirection(split)==0&&PanelConnections.Ports(split)==ports,
            "swapping physical halves moves their click roles and preserves both independent directions and track topology");
    }
}
