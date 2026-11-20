using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class PracticalSetupTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static TrackSection S(string id,double from=0,double to=25)=>new TrackSection{Segment=id,From=from,To=to};
    sealed class World:IInterlockingWorld {
        public Dictionary<string,bool> Points=new Dictionary<string,bool>{{"W1",false},{"W2",false},{"Q",true}};
        public bool PathValid(RouteDefinition route)=>true;
        public TrackOccupancy Occupancy(TrackSection section)=>TrackOccupancy.Clear;
        public bool SwitchPosition(string id,out bool value)=>Points.TryGetValue(id,out value);
        public bool SetSwitch(string id,bool value){Points[id]=value;return true;}
    }
    static RouteDefinition Protected(string name,string track,string point) {
        var route=new RouteDefinition{Code=name,Entry=name+"-in",Exit=name+"-out",Path=new List<DirectedRouteSection>{new DirectedRouteSection{Section=S(track)}},ViaSignals=new List<RouteViaSignal>(),
            Sections=new List<TrackSection>{S(track),S("monitored")},Approach=new List<TrackSection>{S(name+"-approach")}};
        route.Switches.Add(new RouteSwitch{Node=point,ReleaseAt=20,Protection=new List<TrackSection>{S(track),S("monitored"),S(point+"-other")}});
        route.Switches.Add(new RouteSwitch{Node="Q",Thrown=true,FlankOf=point,ReleaseAt=20,Protection=new List<TrackSection>{S("Q1"),S("Q2"),S("Q3")}});
        route.Flanks=new List<RouteFlank>{new RouteFlank{Point=point,Sections=new List<TrackSection>{S("monitored")}}};return route;
    }
    public static void Run(string root) {
        var world=new World();var engine=new InterlockingEngine();var a=Protected("A","track-A","W1");var b=Protected("B","track-B","W2");
        engine.Request(a,world);engine.Request(b,world);for(int i=0;i<8;i++)engine.Tick(world);
        Check(engine.Active.Count==2&&engine.Active.All(r=>r.Permitted),"parallel routes share an unchanged protective point and empty monitoring corridor while both reach proceed");
        Check(engine.Locked("Q",false)&&!engine.Locked("Q",true),"shared protective position remains locked against the opposite setting");
        var opposite=Protected("C","track-C","W2");opposite.Switches.Last().Thrown=false;
        Check(Reject(()=>engine.Request(opposite,world)),"an opposite protective point demand still rejects a parallel request");
        var intrusion=Protected("D","monitored","W2");Check(Reject(()=>engine.Request(intrusion,world)),"a route traversing a jointly monitored flank corridor remains conflicting");
        var legacy=new RouteDefinition{Code="old",Entry="old-in",Exit="old-out",Sections=new List<TrackSection>{S("monitored")},Approach=new List<TrackSection>{S("legacy-approach")}};
        Check(Reject(()=>engine.Request(legacy,world)),"a legacy route without a directed footprint keeps conservative overlap conflicts");
        var signal=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="S",Meters=100},FacingReverse=true};
        var target=new AuxiliaryTarget();AuxiliarySetup.Link(target,signal);target.Validate();
        var copy=JsonConvert.DeserializeObject<AuxiliaryTarget>(JsonConvert.SerializeObject(target));signal.Anchor.Meters=120;signal.Name="Renamed";
        Check(copy.ReferenceSignal==signal.Id&&copy.Endpoint(id=>id==signal.Id?signal:null).Anchor.Meters==120&&!copy.Endpoint(id=>signal).FacingReverse,"saved auxiliary references follow a moved/renamed entry face and use its opposite approach");
        Check(copy.Endpoint(id=>null)==null,"a deleted reference cannot silently reuse the obsolete auxiliary coordinates");
        var panel=new PanelDocument{WorldKey=new string('a',64),Columns=16,Rows=8};PanelEditing.Draw(panel,1,2,8,2);
        var field=new PanelTile{X=2,Y=2,Kind=PanelTileKind.TrainNumber,Ports=17,NumberId=Guid.NewGuid().ToString("N")};panel.Put(field);
        string before=JsonConvert.SerializeObject(panel);var drag=new PanelDrag();drag.Begin(panel,new PanelRegion{X=1,Y=2,Width=3,Height=1},2,2);drag.Update(panel,3,3);
        Check(drag.Valid&&drag.X==2&&drag.Y==3&&JsonConvert.SerializeObject(panel)==before,"drag preview keeps grab offset and leaves all saved fields untouched");
        drag.Apply(panel);Check(panel.At(3,3).NumberId==field.NumberId,"drag commit preserves original train-number source identity");
        drag=new PanelDrag();drag.Begin(panel,null,3,3);drag.Update(panel,4,2);before=JsonConvert.SerializeObject(panel);
        Check(!drag.Valid&&Reject(()=>drag.Apply(panel))&&JsonConvert.SerializeObject(panel)==before,"occupied drag destinations are rejected without changing bindings");
        drag=new PanelDrag();drag.Begin(panel,null,3,3);drag.Update(panel,10,4);panel.Put(new PanelTile{X=14,Y=7});
        Check(Reject(()=>drag.Apply(panel)),"a diagram changed during dragging cannot accept a stale drop");
        engine.RequestRelease(a.Id,"all",0);engine.Tick(world);
        Check(engine.Active.All(r=>r.Route.Id!=a.Id)&&engine.Active.Count==1&&engine.Locked("Q",false),"releasing one parallel route preserves the other route protective point lock");
        BlockConstruction(root);
    }
    static void BlockConstruction(string root) {
        string key=new string('b',64);var repository=new MarkerRepository(Path.Combine(root,"build","block-layout-test-"+Guid.NewGuid().ToString("N")),key,"map");
        var left=repository.Put(new MarkerAnchor{Segment="line",Meters=0},kind:MarkerRecord.Main);var right=repository.Put(new MarkerAnchor{Segment="line",Meters=3000},kind:MarkerRecord.Main);
        var library=new DeskLibrary{WorldKey=key};var master=new DeskDefinition{Code="ESTW",Name="Master",Global=true,Panel=new PanelDocument{WorldKey=key}};
        var a=new DeskDefinition{Code="A",Name="A",Panel=new PanelDocument{WorldKey=key}};var b=new DeskDefinition{Code="B",Name="B",Panel=new PanelDocument{WorldKey=key}};library.Desks.AddRange(new[]{master,a,b});
        a.Panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Signal,Reference=left.Id});b.Panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Signal,Reference=right.Id});
        var line=new LineTrackDefinition{Code="STR-001",Name="A-B",StationA=a.Id,StationB=b.Id,ForwardStart=left.Id,ForwardEnd=right.Id,
            Forward=new List<DirectedRouteSection>{new DirectedRouteSection{Section=S("line",0,3000)}},Reverse=new List<DirectedRouteSection>{new DirectedRouteSection{Section=S("line",0,3000),Reverse=true}}};
        var plan=BlockLayoutPlan.Create(line.Axis,3);Check(plan.Boundaries.SequenceEqual(new[]{1000d,2000d}),"three kilometres divided into three blocks needs two intermediate boundaries");
        var objects=repository.Snapshot();string before=JsonConvert.SerializeObject(objects),oldLibrary=JsonConvert.SerializeObject(library);
        var build=LineBlockBuilder.Build(objects,library,line,plan,a.Id);
        Check(build.NewSignals==4&&build.Library.Blocks.Count==3&&build.Library.Blocks.Values.Sum(v=>v.Path.Sum(p=>p.Section.To-p.Section.From))==3000,"block preview constructs four staggered signal masts and three complete physical detection sections");
        Check(JsonConvert.SerializeObject(objects)==before&&JsonConvert.SerializeObject(library)==oldLibrary,"block planning changes neither installation objects nor existing panels before confirmation");
        Check(build.Objects.Markers.Count(m=>m.Kind==MarkerRecord.CircuitB)==2&&build.Line.BlockUnits.All(u=>build.Objects.Markers.Any(m=>m.Id==u.Boundary&&CircuitIdentity.Enabled(m))),"both running directions share each new physical GFM boundary");
        line.ReverseEnd=MastConfiguration.Effective(left).Back.Id;line.ReverseStart=MastConfiguration.Effective(right).Back.Id;
        var automated=LineBlockBuilder.Build(objects,library,line,plan,a.Id,true);
        Check(automated.NewContacts==4&&!automated.Library.Automation.Enabled&&automated.Library.Automation.Contacts.All(c=>c.Enabled&&c.AutomaticSignal)&&automated.Library.Automation.Contacts.Select(c=>c.Signal).Distinct().Count()==4,"optional block contacts cover both directions without implicitly enabling global automation");
        Check(automated.Library.Automation.Contacts.All(c=>automated.Library.Automation.Signals[c.Signal].Primary!=""&&c.Anchor.Meters>0&&c.Anchor.Meters<3000),"every optional block contact receives an explicit next-signal destination and an approach position inside the line");
        var repeated=LineBlockBuilder.Build(automated.Objects,automated.Library,automated.Line,plan,a.Id,true);
        Check(repeated.NewSignals==0&&repeated.NewContacts==0&&repeated.Library.Blocks.Count==3&&repeated.Library.Desks.Single(d=>d.Id==a.Id).Panel.Tiles.Count==automated.Library.Desks.Single(d=>d.Id==a.Id).Panel.Tiles.Count,"repeating block setup reuses section IDs, signals, contacts and number fields rather than duplicating the installation");
        var extended=BlockLayoutPlan.Create(line.Axis,3);var oldSignal=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="line",Meters=1200}};extended.IncludeExisting(new[]{oldSignal});
        Check(extended.Boundaries.Contains(1200),"existing signal positions replace a proposal rather than spawning nearby duplicate boundaries");
        var reverse=BlockLayoutPlan.Create(new[]{new DirectedRouteSection{Section=S("R",100,3100),Reverse=true}},3);
        Check(reverse.At(1000).Meters==2100&&reverse.Slice(1000,2000).Single().Section.From==1100&&reverse.Slice(1000,2000).Single().Reverse,"block spacing and slicing follow track direction instead of drawing coordinates");
        Check(Reject(()=>BlockLayoutPlan.Create(line.Axis,0))&&Reject(()=>BlockLayoutPlan.Create(line.Axis,1,double.NaN)),"invalid counts and non-finite spacing cannot produce a block construction proposal");
    }
}
