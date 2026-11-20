using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

public static class RecognitionContactTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static PanelDocument Panel()=>new PanelDocument{WorldKey="world",Columns=12,Rows=8};
    static DeskLibrary Library()=>new DeskLibrary{WorldKey="world",Desks=new List<DeskDefinition>{
        new DeskDefinition{Global=true,Code="ESTW",Name="Original master",Panel=Panel()},
        new DeskDefinition{Code="A",Name="Station A",Panel=Panel()}}};
    static NetworkInput Graph()=>new NetworkInput{World="world",Nodes=new List<PlanNode>{new PlanNode{Id="a",X=0,Y=0},new PlanNode{Id="b",X=100,Y=0}},Edges=new List<PlanEdge>{new PlanEdge{Id="ab",A="a",B="b",Length=100}}};
    static MarkerRecord MainSignal(string id,double meters,bool reverse=false)=>new MarkerRecord{Id=id,Name=id,Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="ab",Meters=meters},FacingReverse=reverse,AllowHp1=true,AllowHp2=true};
    public static void Run() {
        var library=Library();string root=library.Desks[0].Id,local=library.Desks[1].Id;
        var legacy=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(library).Replace("\"MasterView\":false,",""));legacy.Validate("world");
        Check(legacy.Desks.Single(d=>d.Global).IsMaster&&!legacy.Desks[1].IsMaster,"existing libraries need no new save or MasterView field to retain their original master and local station");
        DeskLibrary written=null;var store=new DeskStore("unused","world",()=>library,d=>written=d);
        var next=store.CreateMaster(library,"","Second overview");string second=next.Desks.Last().Id;
        Check(next.Desks.Count==3&&next.Desks.Last().IsMaster&&!next.Desks.Last().Global&&written!=null&&library.Desks.Count==2,"a second master view is created and saved without replacing the existing master or modifying its source library");
        next.Desks[1].Panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Signal,Reference="signal-a"});
        next.Modules.Add(new DeskModule{Desk=local,X=1,Y=1});next.Modules.Add(new DeskModule{Desk=local,Master=second,X=8,Y=4});next.Validate("world");
        Check(MasterDiagram.Project(next).At(2,2)?.Reference=="signal-a"&&MasterDiagram.Project(next,second).At(9,5)?.Reference=="signal-a"&&MasterDiagram.Project(next,second).At(2,2)==null,"each master has its own module positions while both refer to the same physical signal");
        var duplicate=next.Clone();duplicate.Modules.Add(duplicate.Modules[1].Clone());
        Check(Reject(()=>duplicate.Validate("world")),"the same local module cannot be inserted twice in one master view");
        next.Points["point"]=new PointAssignment{Desk=root,Code="W1",ElectricStand=true};
        var removed=new DeskRemoval(next,root).Apply(next,Array.Empty<RouteDefinition>());
        Check(removed.Desks.Single(d=>d.Global).Id==second&&removed.Points["point"].Desk==second&&removed.Modules.Single().Master==second,"deleting the original master promotes an existing overview and preserves that overview's modules and electric point assignment");
        removed=new DeskRemoval(library,root).Apply(library,Array.Empty<RouteDefinition>());
        Check(removed.Desks.Count==2&&removed.Desks.Single(d=>d.Global).Id!=root&&removed.Desks.Any(d=>d.Id==local),"deleting the only master creates an empty replacement without removing the local station");
        var graph=Graph();graph.Cuts.Add(new PlanCut{Signal="signal-a",Segment="ab",Meters=40,X=40});
        string signature=NetworkRecognition.Signature(graph);graph.Nodes.Reverse();
        Check(signature==NetworkRecognition.Signature(graph),"recognition signatures ignore node enumeration order");
        graph.Cuts[0].Reverse=true;Check(signature!=NetworkRecognition.Signature(graph),"changing signal direction invalidates a recognition preview");
        graph.Cuts[0].Reverse=false;graph.Nodes[0].Normal="changed";Check(signature!=NetworkRecognition.Signature(graph),"changing point topology invalidates recognition even when track lengths remain identical");
        var proposal=NetworkPlan.Build(Graph());
        next=library.Clone();next.Desks[0].Panel.Put(new PanelTile{X=0,Y=0,Kind=PanelTileKind.Label,Label="Keep old drawing"});next.Modules.Add(new DeskModule{Desk=local,X=2,Y=2});
        var refreshed=NetworkRecognition.Apply(next,root,proposal,false);
        Check(refreshed.Desks[0].Panel.At(0,0)?.Label=="Keep old drawing"&&refreshed.Modules.Count==1&&next.Blocks.Count==0&&refreshed.Blocks.Count>0,"section-only recognition keeps existing geometry and modules and does not mutate the unaccepted source");
        var accepted=NetworkRecognition.Apply(next,root,proposal,true);var archive=accepted.Desks.Single(d=>d.MasterView);
        Check(archive.Panel.At(0,0)?.Label=="Keep old drawing"&&!archive.ShowInDispatcher&&accepted.Modules.Single().Master==archive.Id&&accepted.Desks.Any(d=>d.Id==local),"accepting a new diagram keeps the previous drawing and linked modules in a hidden master archive with unchanged local IDs");
        proposal.Missing.Add("signal:missing");
        Check(Reject(()=>NetworkRecognition.Apply(next,root,proposal,true))&&next.Desks[0].Panel.At(0,0)?.Label=="Keep old drawing","an incomplete recognition proposal cannot replace a working diagram");
        proposal.Missing.Clear();proposal.Panel.WorldKey="different";Check(Reject(()=>NetworkRecognition.Apply(next,root,proposal,true)),"a recognition proposal from another world is rejected");
        graph=Graph();graph.Cuts.Add(new PlanCut{Signal="middle",Segment="ab",Meters=40,X=40,Reverse=true});graph.Cuts.Add(new PlanCut{Signal="end",Segment="ab",Meters=100,X=100});
        proposal=NetworkPlan.Build(graph);
        Check(proposal.Missing.Count==0&&proposal.Panel.Tiles.Count(t=>t.Kind==PanelTileKind.Signal)==2&&proposal.Panel.Tiles.Single(t=>t.Reference=="middle").MainSignalDirection==2&&proposal.Panel.Tiles.Single(t=>t.Reference=="end").MainSignalDirection==0,"recognition includes signals at segment ends and draws signal directions from physical facing");
        graph.Cuts.Add(new PlanCut{Signal="duplicate",Segment="ab",Meters=40.0001,X=40});proposal=NetworkPlan.Build(graph);
        Check(proposal.Missing.Contains("signal:middle")&&proposal.Missing.Contains("signal:duplicate")&&!proposal.Panel.Tiles.Any(t=>t.Reference=="duplicate"||t.Reference=="middle"),"near-coincident signals are reported for review instead of silently overwriting a signal binding");
        Check(Reject(()=>NetworkPlan.Build(Graph(),new CancellationToken(true))),"recognition honors cancellation before replacing any diagram");
        var mains=new[]{MainSignal("first",20),MainSignal("second",60),MainSignal("third",90)};var contact=new AutomationContact{Anchor=new MarkerAnchor{Segment="ab",Meters=5}};
        Check(ContactSignalPolicy.IsNextMain(Graph(),mains,contact,"first")&&!ContactSignalPolicy.IsNextMain(Graph(),mains,contact,"second"),"a contact can request its first main signal but cannot bypass it for a later one");
        var shunt=MainSignal("shunt",10);shunt.Kind=MarkerRecord.Shunt;
        Check(ContactSignalPolicy.IsNextMain(Graph(),mains.Append(shunt),contact,"first"),"a shunting signal before the first main does not prevent contact train requests");
        contact.Reverse=true;
        Check(!ContactSignalPolicy.IsNextMain(Graph(),mains,contact,"first"),"contact requests respect physical facing rather than selecting a nearby signal behind them");
        contact.Reverse=false;var paths=AutomaticRoutePlanner.Find(Graph(),mains,mains[0],mains[2],RouteKind.Train).Single();
        Check(paths.Count==2&&paths[0].Entry=="first"&&paths[0].Exit=="second"&&paths[1].Entry=="second"&&paths[1].Exit=="third","one contact through-request includes every intermediate main signal as a separate secured subroute");
        var plan=new RouteChainPlan{Routes=paths.Select(p=>new RouteDefinition{Entry=p.Entry,Exit=p.Exit,Path=p.Path}).ToList()};
        var intent=new AutomationIntent{Mode=AutomationDrivingMode.Waypoint,Target=new MarkerAnchor{Segment="ab",Meters=60},Segments=new List<string>{"ab"}};
        var rule=new AutomaticSignal{Primary="third"};
        Check(AutomationPlanPolicy.Limit(plan,rule,intent).Routes.Count==1,"a waypoint ending exactly at the next main signal keeps that main signal at Stop");
        intent.Target.Meters=65;Check(AutomationPlanPolicy.Limit(plan,rule,intent).Routes.Count==2,"only a waypoint continuing beyond the next main permits its onward subroute to be requested");
        contact.ShowCounter=true;var round=JsonConvert.DeserializeObject<AutomationContact>(JsonConvert.SerializeObject(contact));
        Check(round.ShowCounter&&round.Id==contact.Id&&!JsonConvert.DeserializeObject<AutomationContact>("{}").ShowCounter,"the optional axle-test display persists without changing contact identity or enabling displays on existing contacts");
        var game=typeof(Model.Car).Assembly;var flags=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        var company=game.GetType("UI.CompanyWindow.CompanyWindow",true);var builder=game.GetType("UI.CompanyWindow.SettingsPanelBuilder",true);
        Check(builder.GetMethod("BuildMods",flags)?.IsStatic==true&&builder.GetProperty("ShouldShowMods",flags)?.GetGetMethod(true)!=null&&company.GetMethod("Show",flags)!=null&&company.GetField("_selectedSettingsItem",flags)!=null&&company.GetField("_selectedTabState",flags)!=null,"Company settings hooks and navigation resolve against the installed game, including private Show and state fields");
    }
}
