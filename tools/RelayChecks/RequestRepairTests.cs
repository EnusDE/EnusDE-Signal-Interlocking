using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using Newtonsoft.Json.Linq;

static class RequestRepairTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    sealed class World:IInterlockingWorld,IRequestDiagnosticWorld {
        public int Reads;public TrackOccupancy State=TrackOccupancy.Unknown;
        readonly Dictionary<string,TrackOccupancy> cache=new();
        public bool PathValid(RouteDefinition route)=>true;
        public TrackOccupancy Occupancy(TrackSection section){Reads++;return cache[section.Key]=State;}
        public bool CachedRequestOccupancy(TrackSection section,out TrackOccupancy state)=>cache.TryGetValue(section.Key,out state);
        public bool RequestPointPosition(string id,out bool value){value=false;return false;}
        public bool SwitchPosition(string id,out bool value){value=false;return true;}
        public bool SetSwitch(string id,bool value)=>true;
    }
    static RouteDefinition Route()=>new(){Code="TEST",Entry="A",Exit="B",Sections=new(){new TrackSection{Segment="native-segment",From=35.61,To=53.46}},Approach=new(){new TrackSection{Segment="behind",From=0,To=10}}};
    static RouteRun Passed(string a,string b)=>new(){Route=new RouteDefinition{Entry=a,Exit=b},StartPassageVerified=true,Stage=RouteStage.Running,TrainCars=new[]{"engine"}};
    public static void Run(string root) {
        var engine=new InterlockingEngine();var world=new World();var route=Route();string error="";
        try{engine.Request(route,world);}catch(InvalidOperationException e){error=e.Message;}
        Check(error.Contains("native-segment:35.61:53.46")&&(error.Contains("unbekannt")||error.Contains("unknown"))&&engine.Active.Count==0,
            "unreadable admission identifies the exact native segment and meter interval without pretending it is occupied");
        Check(engine.LastRequest.Error==error&&!engine.LastRequest.Accepted&&engine.LastRequest.Sections.Single().Sampled&&world.Reads==1,
            "failed request diagnostics keep the actual sampled result without making additional occupancy queries");
        route.Sections[0].From=40;Check(engine.LastRequest.Definitions[0].Sections[0].From==35.61,"last attempted route is an immutable copy of the failed request");
        world.State=TrackOccupancy.Clear;engine.RequestChain(new[]{route},world);
        Check(engine.LastRequest.Accepted&&engine.Active.Count==1,"successful chain requests replace the last attempt record and preserve ordinary admission");
        Check(new InterlockingEngine().Active.Count==0,"a fresh session engine restores no previous route locks or permissive authorities");

        var fields=new Dictionary<string,string>{{"A","00123"},{"B","00456"}};var passage=Passed("A","B");passage.NumberPassageNumber="00123";var done=new HashSet<string>();
        Check(!TrainNumbers.AdvanceSignalNumbers(fields,new[]{passage},completed:done)&&done.Count==0&&fields["A"]=="00123",
            "a number blocked by another destination number stays pending rather than being marked handled");
        fields.Remove("B");fields["old-duplicate"]="900123";
        Check(TrainNumbers.DisplayFieldNumber(fields,"A")=="?"&&TrainNumbers.DisplayFieldNumber(fields,"old-duplicate")=="?","pre-existing duplicate registered numbers are shown as an ownership conflict rather than two train identities");
        Check(TrainNumbers.AdvanceSignalNumbers(fields,new[]{passage},completed:done)&&done.Contains(passage.Route.Id)&&fields.Count==1&&fields["B"]=="00123",
            "retry advances the number after the destination clears and removes stale duplicates even with a different prefix");
        fields=new(){{"A","00789"}};done.Clear();
        Check(!TrainNumbers.AdvanceSignalNumbers(fields,new[]{passage},completed:done)&&fields["A"]=="00789"&&done.Count==0,
            "a delayed previous passage cannot steal a different train's newly assigned number");
        var same=Passed("A","A");done.Clear();TrainNumbers.AdvanceSignalNumbers(fields,new[]{same},completed:done);
        Check(done.Contains(same.Route.Id)&&fields.Count==1,"verified movement inside one logical number field completes without deleting its number");
        var earlier=Passed("A","B");var later=Passed("B","C");fields=new(){{"A","00123"}};done.Clear();
        Check(TrainNumbers.AdvanceSignalNumbers(fields,new[]{later,earlier},completed:done)&&fields.Count==1&&fields["C"]=="00123"&&done.SetEquals(new[]{earlier.Route.Id,later.Route.Id}),
            "a reversed batch of verified passages advances through an initially empty middle field before completing either movement");
        fields.Clear();var empty=Passed("A","B");done.Clear();
        Check(!TrainNumbers.AdvanceSignalNumbers(fields,new[]{empty},completed:done)&&done.Contains(empty.Route.Id),"a truly empty source completes without later inheriting a following train's number");

        var first=new PanelTile{Kind=PanelTileKind.TrainNumber,Reference="A"};var second=first.Clone();second.X=1;
        var library=new DeskLibrary{Desks=new(){new DeskDefinition{Panel=new PanelDocument{Tiles=new(){first,second}}}}};
        var bindings=new NumberFieldIndex(library,Array.Empty<MarkerRecord>());
        Check(bindings.ForSignal("A")==null&&bindings.ConflictingField(first)&&bindings.ConflictingField(second),
            "two independent fields bound to the same signal are reported as ambiguous instead of showing duplicate owners");
        var display=new NumberDisplayOwnership();var readings=new[]{new NumberDisplayReading{Field="west",Number="00123"},new NumberDisplayReading{Field="east",Number="100123"}};
        display.Refresh(readings,Array.Empty<string>());string owner=display.Caption("east","100123")!="-----"?"east":"west";
        Check(readings.Count(r=>display.Caption(r.Field,r.Number)!="-----")==1,"a train straddling two occupied sections produces one ordinary number display");
        display.Refresh(readings.Reverse(),Array.Empty<string>());Check(display.Caption(owner,"00123")!="-----","number display ownership stays stable when detector enumeration order changes");
        display.Refresh(readings,new[]{"900123"});Check(readings.All(r=>display.Caption(r.Field,r.Number)=="-----"),"an explicitly registered field owns its number and suppresses additional car-derived copies");
        display.Refresh(new[]{readings[0]},Array.Empty<string>());Check(display.Caption("west","00123")=="00123","display ownership transfers after the previous detector no longer contains the train");
        display.Refresh(new[]{new NumberDisplayReading{Field="line",Number="00123",Timetable=true},readings[0]},new[]{"900123"});
        Check(display.Caption("line","00123")=="00123"&&display.Caption("west","00123")=="-----"&&display.HasTimetableNumber("900123"),
            "observed timetable identity takes precedence over stale ordinary display copies");
        Check(TrainNumbers.TimetableNumber("145")=="00145"&&TrainNumbers.TimetableNumber("P-12")=="P-12","native timetable symbols keep their identity and short digits use the established width");
        var crossing=Passed("A","B");fields=new(){{"A","00123"}};var registry=new Dictionary<string,string>();
        Check(TrainNumbers.MoveFieldNumberToTrain(fields,registry,"A",crossing)&&fields.Count==0&&crossing.TrainCars.All(id=>registry[id]=="00123"),
            "a proven departure moves the field number to the train rather than leaving it at the origin");
        fields["A"]="00124";
        Check(!TrainNumbers.MoveFieldNumberToTrain(fields,registry,"A",crossing)&&fields["A"]=="00124",
            "a field-to-train handover preserves conflicting identities for review");
        var workingDesk=new DeskDefinition{Code="WHB",Panel=new PanelDocument{Tiles=new(){
            new PanelTile{X=0,Kind=PanelTileKind.Signal,Reference="local",AlternateSignal="yard"},
            new PanelTile{X=1,Kind=PanelTileKind.Switch,Reference="point"},
            new PanelTile{X=2,Kind=PanelTileKind.Signal,Reference="neighbour",ReadOnlySignal=true},
            new PanelTile{X=4,Kind=PanelTileKind.Signal,Reference="outside"}
        }}};
        var area=new PanelArea{Width=3,Height=2};
        var yardOnly=OperatorScope.From(workingDesk,area,OperatorDuty.Shunt);
        var allMoves=OperatorScope.From(workingDesk,area,OperatorDuty.Manual);
        Check(yardOnly.Accepts("yard",true)&&!yardOnly.Accepts("local",false)&&yardOnly.AcceptsPoint("point"),"shunting takeover leaves train movements under automation");
        Check(allMoves.Accepts("local",false)&&!allMoves.Accepts("outside",false)&&!allMoves.Accepts("neighbour",false),"full manual takeover obeys named-area and neighbour boundaries");
        Check(allMoves.AcceptsRoute(new RouteDefinition{Entry="local",Exit="neighbour"})&&!allMoves.AcceptsRoute(new RouteDefinition{Entry="local",Exit="outside"}),
            "a named takeover accepts a displayed boundary destination and refuses a route ending beyond the area");

        var target=new AuxiliaryTarget{Name="Turnback",Shunt=true,Train=false,Anchor=new MarkerAnchor{Segment="s",Meters=50}};
        var linked=new[]{new PanelTile{Kind=PanelTileKind.Signal,Reference="main",AlternateSignal="shunt",LinkedAuxiliary=target.Id},new PanelTile{Kind=PanelTileKind.AuxiliaryTarget,Target=target}};
        Check(AuxiliaryTarget.LinkedForSignal(linked,"shunt").Single().Id==target.Id&&AuxiliaryTarget.LinkedForSignal(linked,"main").Length==1&&AuxiliaryTarget.LinkedForSignal(linked,"missing").Length==0,
            "linked auxiliary destinations resolve for either signal half without depending on which cell is currently selected");
        var drawing=new PanelDocument{Tiles=new(){new PanelTile{X=1,Kind=PanelTileKind.Signal,Reference="045"},new PanelTile{X=2,Kind=PanelTileKind.Signal,Reference="050"}}};
        var local=new TrackBlock{Id="local",Name="local",BoundaryA="045",BoundaryB="050",Path=new(){new DirectedRouteSection{Section=new TrackSection{Segment="local",From=0,To=50}}}};
        var outside=local.Clone();outside.Id="outside";outside.BoundaryA="051";outside.Path[0].Section.To=2773.74;
        bool Boundary(PanelTile t,string id)=>t.Reference==id;
        Check(GfmInspectionOrder.Sort(new[]{outside,local},drawing,null,Boundary).First().Id=="local","inspection offers sections with both ends in the diagram before a distant section touching only one visible boundary");
        drawing.Tiles[1].BlockId="outside";
        Check(GfmInspectionOrder.Sort(new[]{outside,local},drawing,drawing.Tiles[1],Boundary).First().Id=="outside","an explicitly selected field's actual assignment remains inspectable even when its far boundary is not drawn");

        string folder=Path.Combine(root,"build","checks","diagnostic-export-"+Guid.NewGuid().ToString("N"));
        try {
            var data=new Dictionary<string,object>{{"manifest.json",new{version="fixture"}},{"last-request.json",engine.LastRequest}};
            string one=DiagnosticArchive.Write(folder,data),two=DiagnosticArchive.Write(folder,data);
            using(var file=File.OpenRead(one))using(var zip=new ZipArchive(file,ZipArchiveMode.Read)) {
                using var reader=new StreamReader(zip.GetEntry("manifest.json").Open());
                Check(JObject.Parse(reader.ReadToEnd())["version"].Value<string>()=="fixture"&&zip.GetEntry("last-request.json")!=null&&one!=two&&File.Exists(two),
                    "diagnostic export creates readable, separate ZIP snapshots and preserves earlier archives");
            }
            bool rejected=false;try{DiagnosticArchive.Write(folder,new Dictionary<string,object>{{"../outside.json",new{}}});}catch(InvalidDataException){rejected=true;}
            Check(rejected&&Directory.GetFiles(folder).Length==2&&!Directory.GetFiles(folder,"*.tmp").Any(),"invalid export entry names cannot escape the archive or leave a partial snapshot");
        }finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }
}
