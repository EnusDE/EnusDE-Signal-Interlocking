using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EnusDE.Signaltechnik;

public static class SharedFeatureTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action f){try{f();return false;}catch{return true;}}
    sealed class Properties:ISharedProperties {public Dictionary<string,string> Data=new Dictionary<string,string>();public int Fail=-1,Writes;public string Read(string key)=>Data.TryGetValue(key,out var value)?value:null;public void Write(string key,string value){if(++Writes==Fail)throw new IOException("fixture interrupted");if(value==null)Data.Remove(key);else Data[key]=value;}}
    static MarkerRecord Marker(string kind,string name="A")=>new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Kind=kind,Name=name,Anchor=new MarkerAnchor{Segment="S",Meters=30}};
    static InstallationDocument Installation(){string world=new string('a',64);return new InstallationDocument{WorldKey=world,Map="map",Objects=new MarkerDocument{WorldKey=world,Map="map"}};}
    public static void Run(string root) {
        var p=new Properties();var documents=new SharedDocuments(p);string unicode=string.Concat(Enumerable.Repeat("Ä漢字🚂",20000));documents.Write("x",unicode,1024*1024);Check(documents.Read<string>("x",1024*1024)==unicode,"shared compressed documents round-trip large Unicode including surrogate pairs");
        string baseline=documents.Header("x");p.Fail=p.Writes+2;Check(Reject(()=>documents.Write("x","new",1024*1024))&&documents.Header("x")==baseline&&documents.Read<string>("x",1024*1024)==unicode,"interrupted inactive-bank write leaves the last committed snapshot readable");p.Fail=-1;
        var random=new Random(7);string noise=new string(Enumerable.Range(0,50000).Select(_=>(char)random.Next(32,126)).ToArray());documents.Write("x",noise,1024*1024);documents.Write("x","tiny",1024*1024);documents.Write("x","small",1024*1024);documents.Write("x","short",1024*1024);Check(p.Data.Count<=5&&documents.Read<string>("x",1024*1024)=="short","repeated large-to-small edits prune obsolete chunks in both banks");
        var head=JsonConvert.DeserializeObject<SharedDocumentHeader>(documents.Header("x"));string key="x."+head.Slot+".0",old=p.Data[key];p.Data[key]=(old[0]=='A'?"B":"A")+old.Substring(1);Check(Reject(()=>documents.Read<string>("x",1024*1024)),"corrupt shared chunk cannot become a valid installation");p.Data.Remove(key);Check(Reject(()=>documents.Read<string>("x",1024*1024)),"incomplete late-join snapshot remains unavailable");p.Data[key]=old;Check(Reject(()=>documents.Read<string>("x",2)),"declared and expanded size caps reject oversized received state");
        var lease=new LiveLease();string epoch=Guid.NewGuid().ToString("N");Check(lease.Accept(epoch,1,10)&&!lease.Fresh(10)&&lease.Accept(epoch,2,11)&&lease.Fresh(12),"stored operational snapshot cannot authorize until a second live host heartbeat arrives");Check(!lease.Accept(epoch,2,13)&&!lease.Fresh(17)&&lease.Accept(epoch,3,18)&&!lease.Fresh(18)&&lease.Accept(epoch,4,19)&&lease.Fresh(19),"replayed frames and stale host lease stay safe and require renewed heartbeat confirmation");Check(lease.Accept(Guid.NewGuid().ToString("N"),1,20)&&!lease.Fresh(20),"new host epoch never inherits an old live authority");
        var install=Installation();install.Economy.Enabled=true;var marker=Marker(MarkerRecord.Main);var next=JsonConvert.DeserializeObject<MarkerDocument>(JsonConvert.SerializeObject(install.Objects));next.Markers.Add(marker);install=MaterialEconomy.Markers(install,next);Check(install.Objects.Markers[0].Planned&&SignalCatalogue.Expand(install.Objects.Markers).Length==0,"new economic construction stays blue and absent from active signal authority until supplied");
        var depot=new MaterialDepot{Anchor=marker.Anchor.Clone(),Stock=new MaterialCost(20,20)};install.Economy.Depots.Add(depot);var cost=MaterialEconomy.Missing(install,marker);string before=JsonConvert.SerializeObject(install);var failed=install.Clone();failed.Economy.Depots[0].Stock=new MaterialCost();Check(Reject(()=>MaterialEconomy.Complete(failed,marker.Id,depot.Id))&&JsonConvert.SerializeObject(install)==before,"missing materials cannot partially complete a signal or mutate the committed ledger");
        var freeComplete=typeof(MaterialEconomy).GetMethod("Complete",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
        var free=(InstallationDocument)freeComplete.Invoke(null,new object[]{install,marker.Id,null,true});
        Check(free.Economy.Enabled&&free.Objects.Markers[0].Id==marker.Id&&!free.Objects.Markers[0].Planned&&
            free.Economy.Depots[0].Stock.B==20&&free.Economy.Depots[0].Stock.T==20&&free.Economy.Built[marker.Id].Paid.B==0&&free.Economy.Built[marker.Id].Paid.T==0&&
            SignalCatalogue.Expand(free.Objects.Markers).Length==1&&JsonConvert.SerializeObject(install)==before,
            "one sandbox completion preserves host economy and depot stock and cannot refund materials never paid");
        Check(TrainNumbers.TryDisplaySlots(TrainNumbers.Format("1","00145"),out var prefixed)&&prefixed=="1  145"&&
            TrainNumbers.TryDisplaySlots("01005",out var internalZeros)&&internalZeros=="  1005"&&
            TrainNumbers.TryDisplaySlots("00000",out var zero)&&zero=="     0"&&
            !TrainNumbers.TryDisplaySlots("-----",out _)&&!TrainNumbers.TryDisplaySlots("mehrere",out _),
            "only leading body zeros are hidden in six fixed display slots while internal zeros, prefix and status meanings survive");
        RouteRun Passage(string a,string b,string car,bool shunt=false)=>new RouteRun{
            Route=new RouteDefinition{Entry=a,Exit=b,Shunting=shunt},Stage=RouteStage.Running,ChainReady=true,
            StartPassageVerified=true,TrainCars=new[]{car}
        };
        var numberFields=new Dictionary<string,string>{{"A","100145"}};
        var firstPass=Passage("A","B","engine");
        firstPass.Stage=RouteStage.Cancelled;
        Check(!TrainNumbers.AdvanceSignalNumbers(numberFields,new[]{firstPass,Passage("A","B","engine",true)})&&numberFields["A"]=="100145",
            "returning a route to stop manually and traversing a shunt route do not move a train number");
        firstPass.Stage=RouteStage.Running;
        Check(TrainNumbers.AdvanceSignalNumbers(numberFields,new[]{Passage("B","C","engine"),firstPass})&&numberFields.Count==1&&numberFields["C"]=="100145",
            "two verified passages for the same train can forward the full stored number along a chain even if observed in reverse order");
        numberFields=new Dictionary<string,string>{{"A","100145"},{"C","00234"}};
        Check(TrainNumbers.AdvanceSignalNumbers(numberFields,new[]{firstPass,Passage("B","D","other"),Passage("B","C","engine")})&&
            numberFields.Count==2&&numberFields["B"]=="100145"&&numberFields["C"]=="00234",
            "train number movement never replaces a different destination number or follows a different train from a field assigned in the same tick");
        var complete=MaterialEconomy.Complete(install,marker.Id,depot.Id);Check(!complete.Objects.Markers[0].Planned&&complete.Economy.Depots[0].Stock.B==20-cost.B&&complete.Economy.Depots[0].Stock.T==20-cost.T&&SignalCatalogue.Expand(complete.Objects.Markers).Length==1,"completion debits exact stock and only then exposes the built signal");
        var twice=MaterialEconomy.Complete(complete,marker.Id,depot.Id);Check(JsonConvert.SerializeObject(twice)==JsonConvert.SerializeObject(complete),"repeated completion cannot consume materials twice");var removed=JsonConvert.DeserializeObject<MarkerDocument>(JsonConvert.SerializeObject(complete.Objects));removed.Markers.Clear();var refund=MaterialEconomy.Markers(complete,removed);Check(refund.Economy.Depots[0].Stock.B==20-cost.B+cost.B/2&&refund.Economy.Depots[0].Stock.T==20-cost.T+cost.T/2&&!refund.Economy.Built.ContainsKey(marker.Id),"demolition returns only half of actually paid components to its original depot");
        var sandbox=Installation();next=sandbox.Objects;next.Markers.Add(Marker(MarkerRecord.Shunt));sandbox=MaterialEconomy.Markers(sandbox,next);sandbox.Economy.Enabled=true;var grandfather=MaterialEconomy.Markers(sandbox,sandbox.Objects);Check(!grandfather.Objects.Markers[0].Planned&&MaterialEconomy.Missing(grandfather,grandfather.Objects.Markers[0]).T==0,"existing free or migrated installations are grandfathered when economy is enabled");
        install.Economy.Factor=2;Check(MaterialEconomy.Missing(install,marker).B==cost.B*2,"material factor scales unpaid requirements without rewriting paid budget");var planned=install.Clone();planned.Economy.Enabled=false;planned=MaterialEconomy.Markers(planned,planned.Objects);Check(planned.Objects.Markers[0].Planned,"changing to sandbox does not silently enable an unfinished worksite");
        complete.Economy.Depots[0].Stock=new MaterialCost(10,10);var wired=MaterialEconomy.Connect(complete,new[]{"W1","W1"},id=>depot.Id);var again=MaterialEconomy.Connect(wired,new[]{"W1"},id=>depot.Id);Check(wired.Economy.Depots[0].Stock.B==9&&wired.Economy.Depots[0].Stock.T==8&&JsonConvert.SerializeObject(wired)==JsonConvert.SerializeObject(again),"physical point connection is paid once, independent of table copies or repeated route saving");
        var ledger=new MaterialLedger();string token=Guid.NewGuid().ToString("N");Check(MaterialEconomy.Receipt(ledger,token)&&!MaterialEconomy.Receipt(ledger,token),"cargo operation receipts suppress repeated native load transactions");for(int i=0;i<100;i++)MaterialEconomy.Receipt(ledger,Guid.NewGuid().ToString("N"));Check(ledger.Receipts.Count==64,"receipt cache stays bounded during long games");
        var duplicate=install.Objects.Markers[0].Clone();duplicate.Id=Guid.NewGuid().ToString("N");duplicate.Mast=null;Check(Reject(()=>SignalCatalogue.Validate(new[]{install.Objects.Markers[0],duplicate})),"planned mast face codes cannot collide before construction completion");
        Check(PlacementDirection.Reverse(0,1,0,-1)&&!PlacementDirection.Reverse(0,1,0,1)&&PlacementDirection.Reverse(0,1,1,0,true),"placement follows viewing travel direction and retains the last direction for perpendicular or vertical views");Check(PlacementDirection.Side(2.3,true,false)==2.3&&PlacementDirection.Side(2.3,true,true)==-2.3&&PlacementDirection.Side(-2.3,false,false)==-2.3,"default physical side is relative to travel in both directions for every construction kind");
        var placement=Installation();placement.Placement.RightSide=false;placement.Placement.FollowView=true;var state=JsonConvert.DeserializeObject<InstallationDocument>(JsonConvert.SerializeObject(placement));Check(!state.Placement.RightSide&&state.Placement.FollowView,"server placement policy persists in shared installation state instead of local preferences alone");
        Check(TrainNumbers.Format("2","00123")=="200123"&&TrainNumbers.Format("","00001")=="00001"&&Reject(()=>TrainNumbers.Format("11","12345"))&&Reject(()=>TrainNumbers.Format("","1234")),"train identifiers preserve five digits and optional distinct one-digit prefix");var registry=new Dictionary<string,string>{{"loco","00123"},{"car","00456"}};Check(TrainNumbers.Display(TrackOccupancy.Occupied,new[]{"loco"},registry)=="00123"&&TrainNumbers.Display(TrackOccupancy.Occupied,new[]{"loco","car"},registry)=="mehrere"&&TrainNumbers.Display(TrackOccupancy.Clear,new[]{"loco"},registry)=="-----"&&TrainNumbers.Display(TrackOccupancy.Unknown,null,registry)=="?","bound train-number display depends on actual known occupancy and exposes mixed or unknown identity");
        Check(PanelTrackPolicy.Resolve(TrackOccupancy.Occupied,true,true)==PanelTrackCue.Occupied&&PanelTrackPolicy.Resolve(TrackOccupancy.Clear,true,false)==PanelTrackCue.Route&&PanelTrackPolicy.Resolve(TrackOccupancy.Clear,false,true)==PanelTrackCue.Setting&&PanelTrackPolicy.Resolve(TrackOccupancy.Unknown,true,false)==PanelTrackCue.Unknown,"occupied red overrides valid green route and flashing preparation; unknown never appears clear");
        Check(Reject(()=>new TrackBlock{Id="b",Name="broken",Path=null}.Validate()),"malformed received block catalog rejects before dereferencing path entries");
        var definitions=JObject.Parse(File.ReadAllText(Path.Combine(root,"mod","EnusDE-Signaltechnik","Definitions.json")));var loads=(JArray)definitions["objects"];Check(loads.Count==2&&loads.Select(l=>(string)l["identifier"]).SequenceEqual(new[]{MaterialEconomy.BuildingLoad,MaterialEconomy.TechnologyLoad})&&loads.All(l=>(string)l["definition"]["units"]=="Quantity"&&(bool)l["definition"]["importable"]),"shipped freight definitions use matching unique identifiers and native whole-unit importable loads");
        foreach(var row in loads){var definition=row["definition"].ToObject<Model.Definition.Data.LoadDefinition>();var load=Model.Ops.Definition.Load.FromDefinition((string)row["identifier"],definition);Check(load.units.ToString()=="Quantity"&&load.Pounds(2)==2*load.unitWeightInPounds&&load.carTypeFilter.Matches(load.id==MaterialEconomy.BuildingLoad?"FM":"XM")&&!load.carTypeFilter.Matches(load.id==MaterialEconomy.BuildingLoad?"XM":"FM"),"installed native Load.FromDefinition accepts "+load.id+" with correct mass and wagon-type filter");}
        Check(Reject(()=>new RetiredSignal{Id="enusde.signaltechnik.signal.own",Offset=new float[3],Normal=new float[]{0,0,1}}.Validate())&&Reject(()=>new RetiredSignal{Id="stock",Offset=new float[]{float.NaN,0,0},Normal=new float[]{0,0,1}}.Validate()),"invalidity-cross data excludes own proxies and invalid placements before replication");
        var retired=Installation();retired.Retired.Add(new RetiredSignal{Id="native-stock-A",IgnoreAi=true,Offset=new float[]{0,4,0},Normal=new float[]{0,0,1}});retired.Validate();var cross=JsonConvert.DeserializeObject<InstallationDocument>(JsonConvert.SerializeObject(retired));Check(cross.Retired.Single().Id=="native-stock-A"&&cross.Retired.Single().IgnoreAi&&cross.Retired.Single().Offset[1]==4,"explicit stock-signal invalidity and optical placement persist for late join and reload");
        StockInvalidityTests.Run();Editor(root);Chains();IndependentRoutes(root);NumberDirections();
    }
    static void Editor(string root) {
        var lookup=new PanelLookup();var indexed=new PanelDocument{WorldKey="index"};indexed.Put(new PanelTile{X=3,Y=2,Label="old"});Check(lookup.Read(indexed,3,2).Label=="old"&&lookup.Read(indexed,1,1)==null,"large-board display resolves existing and empty cells through a reusable coordinate index");indexed.Put(new PanelTile{X=3,Y=2,Label="new"});Check(lookup.Read(indexed,3,2).Label=="new","editor commit invalidates display lookup even when field count stays unchanged");
        string path=Path.Combine(root,"build","history-test-"+Guid.NewGuid().ToString("N"));var doc=new PanelDocument{WorldKey="history"};var history=new PanelHistory(path,doc);for(int i=0;i<12;i++){var after=doc.Clone();after.Put(new PanelTile{X=i,Y=1,Label=""+i});history.Record(doc,after);doc=after;}Check(history.UndoCount==10&&Directory.GetFiles(path,"*.gz").Length==10,"editor stores at most ten compressed steps on disk instead of retaining ten full boards in RAM");
        for(int i=0;i<10;i++){doc=history.Target(doc,false);history.Accept(doc,false);}Check(doc.Tiles.Count==2&&history.RedoCount==10&&Reject(()=>history.Target(doc,false)),"ten sequential undos restore exact data while older changes stay committed");history=new PanelHistory(path,doc);for(int i=0;i<10;i++){doc=history.Target(doc,true);history.Accept(doc,true);}Check(doc.Tiles.Count==12&&history.UndoCount==10,"redo survives reopening and reconstructs the exact board");doc=history.Target(doc,false);history.Accept(doc,false);var edited=doc.Clone();PanelEditing.Insert(edited,true,1);history.Record(doc,edited);doc=edited;Check(history.RedoCount==0&&doc.Tiles.All(t=>t.Y==2),"editing after undo drops the obsolete redo branch and tracks middle-row insertion");var deletion=new PanelDeletion(doc,new PanelRegion{X=0,Y=0,Width=24,Height=12});var cleared=doc.Clone();deletion.Apply(cleared,true);history.Record(doc,cleared);doc=history.Target(cleared,false);history.Accept(doc,false);Check(doc.Tiles.Count==11&&doc.Tiles.All(t=>t.Y==2),"confirmed delete-all can itself be undone with original bindings and positions intact");
        var global=new DeskDefinition{Global=true,Code="ESTW",Name="Global",Panel=new PanelDocument{WorldKey="history"}};var local=new DeskDefinition{Code="A",Name="Station A",Panel=new PanelDocument{WorldKey="history"}};global.Panel.Put(new PanelTile{X=3,Y=3,Kind=PanelTileKind.Switch,Reference="native1"});local.Panel.Put(new PanelTile{X=8,Y=2,Kind=PanelTileKind.Switch,Reference="native1"});var library=new DeskLibrary{WorldKey="history",Desks=new List<DeskDefinition>{global,local}};library=PointAssignments.Put(library,"native1","W12",local.Id);PointAssignments.Boundaries(library);Check(library.Desks.Single(d=>d.Global).Panel.Areas.Single().Desk==local.Id&&PointAssignments.Caption(library,"native1")=="W12","one shared point assignment labels different local/global geometries and outlines the local territory in ESTW");library=PointAssignments.Put(library,"native1","W99",local.Id);Check(library.Desks.All(d=>PointAssignments.Caption(library,d.Panel.Tiles[0].Reference)=="W99"),"renaming the shared point updates every board without duplicating physical control");Check(Reject(()=>PointAssignments.Put(library,"native2","w99",local.Id)),"duplicate point code is rejected centrally across all desks");PointAssignments.NameDiscovered(library,new[]{"native1","native2","native3"});string registryBefore=JsonConvert.SerializeObject(library.Points);PointAssignments.NameDiscovered(library,new[]{"native3","native2","native1"});Check(library.Points["native1"].Code=="W99"&&library.Points["native1"].Desk==local.Id&&library.Points["native2"].Code=="W001"&&registryBefore==JsonConvert.SerializeObject(library.Points),"automatic point naming is stable and never overwrites a user label or local ownership");
    }
    sealed class World:IInterlockingWorld {public bool Valid=true;public bool PathValid(RouteDefinition r)=>Valid;public TrackOccupancy Occupancy(TrackSection s)=>s.Segment=="bad"?TrackOccupancy.Unknown:TrackOccupancy.Clear;public bool SwitchPosition(string id,out bool value){value=false;return true;}public bool SetSwitch(string id,bool value)=>true;}
    static RouteDefinition Route(string a,string b,string segment)=>new RouteDefinition{Code=a+b,Entry=a,Exit=b,Shunting=true,Sections=new List<TrackSection>{new TrackSection{Segment=segment,From=0,To=100}},Approach=new List<TrackSection>{new TrackSection{Segment="approach",From=0,To=100}},Switches=new List<RouteSwitch>()};
    static void NumberDirections() {
        MarkerRecord Head(string id,double meters,bool reverse)=>new MarkerRecord{Id=id,Name=id,Kind=MarkerRecord.Main,FacingReverse=reverse,
            Anchor=new MarkerAnchor{Segment="segment",Meters=meters}};
        var panel=new PanelDocument{WorldKey="fixture"};
        panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.TrainNumber,Section=new TrackSection{Segment="segment",From=10,To=25}});
        panel.Put(new PanelTile{X=2,Y=1,Kind=PanelTileKind.TrainNumber,Reference="C",AlternateSignal="D"});
        var library=new DeskLibrary{WorldKey="fixture",Desks=new List<DeskDefinition>{new DeskDefinition{Panel=panel}}};
        var stock=new[]{Head("A",10,false),Head("B",25,true),Head("C",60,false),Head("D",70,true)};
        var index=new NumberFieldIndex(library,stock);
        Check(index.ForField(panel.At(1,1))=="A"&&index.ForSignal("B")=="A"&&index.ForSignal("D")=="C",
            "a real section derives a common field for opposite directions, and manual aliases share a common number too");
        RouteRun Passed(string entry,string exit)=>new RouteRun{Route=new RouteDefinition{Entry=entry,Exit=exit},
            Stage=RouteStage.Running,ChainReady=true,StartPassageVerified=true,TrainCars=new[]{"same"}};
        var numbers=new Dictionary<string,string>{{"A","100145"}};
        Check(TrainNumbers.AdvanceSignalNumbers(numbers,new[]{Passed("B","C")},index.ForSignal)&&numbers["C"]=="100145"&&
            TrainNumbers.AdvanceSignalNumbers(numbers,new[]{Passed("D","A")},index.ForSignal)&&numbers["A"]=="100145"&&numbers.Count==1,
            "one train number moves forward and backward through the actual active route endpoints");
        var ambiguous=new NumberFieldIndex(library,stock.Append(Head("E",17,false)));
        Check(ambiguous.ForField(panel.At(1,1))==null&&ambiguous.ForSignal("B")==null,
            "two possible main signals in one direction are not automatically treated as one number field");
        Check(Head("HS-006",10,false).DisplayTitle=="HS-006"&&SignalLabel.Normalize("  ")==null&&
            SignalLabel.Normalize(" V P1 ")=="V P1"&&Reject(()=>SignalLabel.Validate("Tafel",3)),
            "custom display labels preserve their code fallback and accept only bounded physical scale");
    }

    static void IndependentRoutes(string root) {
        var first=Route("A","B","west");
        var second=Route("C","D","east");
        var overlap=Route("E","F","west");
        first.Code="WHB-RF-001";
        second.Code="WHB-RF-002";
        overlap.Code="WHB-RF-003";
        var store=new InterlockingStore(Path.Combine(root,"build","route-batch-"+Guid.NewGuid().ToString("N")),"fixture");
        var warning="";
        try{RouteChains.ToTarget(new[]{first},"A","B",false,false);}catch(InvalidOperationException e){warning=e.Message;}
        Check(warning.Contains("A → B")&&warning.Contains("Zugfahrt")&&warning.Contains("Gespeichert als Rangierfahrt"),
            "start-destination feedback identifies when a definition exists only for a different movement type");
        store.Save(new[]{first,second,overlap});
        var definitions=store.Load();
        Check(definitions.Count==3&&definitions.Select(r=>r.Code).SequenceEqual(new[]{first.Code,second.Code,overlap.Code}),
            "one save retains multiple independently selectable route definitions and their names");
        var engine=new InterlockingEngine();
        var world=new World();
        engine.Request(definitions[0],world);
        engine.Request(definitions[1],world);
        for(int i=0;i<8;i++)engine.Tick(world);
        Check(engine.Active.Count==2&&engine.Active.All(run=>run.Permitted)&&
            Reject(()=>engine.Request(definitions[2],world))&&engine.Active.Count==2,
            "two independently requested nonconflicting routes can be active together while a conflicting third is rejected");
    }
    static void Chains(){var ab=Route("A","B","s1");var bc=Route("B","C","s2");var all=new[]{ab,bc};var chain=RouteChains.Follow(all,ab);Check(chain.Routes.Count==2&&chain.EndReason.Contains("Keine weitere"),"automatic continuation stops at the first signal without a saved follow-up route");Check(RouteChains.Follow(new[]{ab,bc,Route("B","D","s3")},ab).Routes.Count==1,"ambiguous shunt continuation stops for dispatcher selection");Check(RouteChains.Follow(new[]{ab,bc,Route("C","A","s3")},ab).Routes.Count==3,"cyclic route definitions never repeat automatically");Check(RouteChains.ToTarget(all,"A","C",true,false).Routes.Count==2&&Reject(()=>RouteChains.ToTarget(all,"A","Z",true,false)),"start and target resolve only a fully saved unique chain");var world=new World();var engine=new InterlockingEngine();engine.RequestChain(all,world);Check(engine.Active.Count==2&&engine.Active.All(r=>!r.Permitted),"all chain signals stay at stop while any part is still preparing");for(int i=0;i<8;i++)engine.Tick(world);Check(engine.Active.All(r=>r.Permitted&&r.ChainReady),"chain authorizes only after every constituent route passes the normal setting checks");engine.CancelChain(ab.Id,world);Check(engine.Active.Count==0,"canceling a route chain withdraws all parts before releasing known-clear resources");var bad=Route("B","C","bad");Check(Reject(()=>engine.RequestChain(new[]{ab,bad},world))&&engine.Active.Count==0,"failed later route leaves no partial new chain authority");}
}
