using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EnusDE.Signaltechnik;

static class FlexibleSetupTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static string Id()=>Guid.NewGuid().ToString("N");
    static DirectedRouteSection Part(bool reverse=false)=>new DirectedRouteSection{Section=new TrackSection{Segment="S",From=0,To=100},Reverse=reverse};
    static RouteChainPlan Plan(int length,string via,string point)=>new RouteChainPlan{Routes=new List<RouteDefinition>{new RouteDefinition{Code="AUTO-internal-do-not-show",Entry="A",Exit="B",Path=new List<DirectedRouteSection>{new DirectedRouteSection{Section=new TrackSection{Segment="S",From=0,To=length}}},ViaSignals=new List<RouteViaSignal>{new RouteViaSignal{Id=via,Distance=10}},Switches=new List<RouteSwitch>{new RouteSwitch{Node=point},new RouteSwitch{Node="hidden-flank",FlankOf=point}}}}};
    public static void Run() {
        var shortPlan=Plan(100,"HS-1","W1");var longPlan=Plan(200,"HS-2","W2");
        var names=RouteChoiceLabels.Captions(new[]{shortPlan,longPlan},s=>s,s=>s);
        Check(names[0].Contains("Short")&&names[1].Contains("Long")&&!names.Any(s=>s.Contains("AUTO")),"two unequal routes receive short/long captions without exposing internal request IDs");
        var equal=Plan(100,"HS-2","W2");names=RouteChoiceLabels.Captions(new[]{shortPlan,equal},s=>s,s=>s);
        Check(names.Distinct().Count()==2&&names.All(s=>s.Contains("via"))&&!names.Any(s=>s.Contains("Long")||s.Contains("Short")),"equal-length alternatives are distinguished by their signal or point rather than incorrectly labelled short/long");
        names=RouteChoiceLabels.Captions(new[]{shortPlan,longPlan,Plan(300,"HS-3","W3")},s=>s,s=>s);
        Check(names.Distinct().Count()==3&&names[0].Contains("HS-1")&&names[1].Contains("HS-2")&&names[2].Contains("HS-3")&&!names.Any(s=>s.Contains("hidden-flank")),"three alternatives identify their distinguishing signals and omit protection-only flank settings");
        Groups();Neighbour();
        Check(!SignalCommunication.Request("physical-signal",0),"the communication seam has no effect and reports unavailable until a handler is installed");
        string received=null;int direction=-1;Action<string,int> handler=(s,d)=>{received=s;direction=d;};SignalCommunication.Requested+=handler;
        try{Check(SignalCommunication.Request("physical-signal",1)&&received=="physical-signal"&&direction==1,"the hidden communication seam forwards the stable physical ID and mast side without changing signal authority");}finally{SignalCommunication.Requested-=handler;}
    }
    static void Groups() {
        var line=new LineTrackDefinition{Code="STR1",Name="Whittier – Ela",StationA=Id(),StationB=Id(),Enabled=true,ForwardStart=Id(),ForwardEnd=Id(),ReverseStart=Id(),ReverseEnd=Id(),Forward=new List<DirectedRouteSection>{Part()},Reverse=new List<DirectedRouteSection>{Part(true)},Topology=new string('a',64)};
        foreach(string id in LineSignalGroups.All(line))line.Anchors[id]="anchor";line.Validate();
        var legacy=JObject.FromObject(line);foreach(string key in new[]{"ADepartures","BArrivals","BDepartures","AArrivals"})legacy.Remove(key);var loaded=legacy.ToObject<LineTrackDefinition>();loaded.Validate();
        Check(LineSignalGroups.Get(loaded,LineSignalGroup.ADepartures).Single()==line.ForwardStart&&LineSignalGroups.All(loaded).Count()==4,"existing saved four-slot connections remain valid without requiring a migration");
        var starts=new[]{line.ForwardStart,Id(),Id(),Id(),Id()};LineSignalGroups.Put(line,LineSignalGroup.ADepartures,starts);foreach(string id in starts)line.Anchors[id]="anchor";line.Validate();
        Check(LineSignalGroups.Get(line,LineSignalGroup.ADepartures).Count==5&&LineSignalGroups.Get(line,LineSignalGroup.BArrivals).Count==1,"five departure signals can share one arrival target on the same line definition");
        var clone=line.Clone();clone.ADepartures.RemoveAt(0);Check(line.ADepartures.Count==5&&JsonConvert.DeserializeObject<LineTrackDefinition>(JsonConvert.SerializeObject(line)).ADepartures.Count==5,"line signal groups clone independently and survive shared JSON persistence");
        var missing=line.Clone();missing.Anchors.Remove(starts[4]);Check(Reject(missing.Validate),"enabling a line requires captured anchors for every configured departure, including additional signals");
        var library=new DeskLibrary();library.FreeLines.Add(line);Check(Reject(()=>SignalRolePolicy.RequireLineCompatibility(library,starts[4],SignalDuty.Entry)),"additional departures keep their exit role while their line is active");
        SignalRolePolicy.RequireLineCompatibility(library,starts[4],SignalDuty.Exit);
        var target=new AuxiliaryTarget{Name="Arrival",Train=true,Anchor=new MarkerAnchor{Segment="S",Meters=100}};
        var endpoint=target.Endpoint();
        Check(LineSignalGroups.AcceptsEndpoint(endpoint,false)&&!LineSignalGroups.AcceptsEndpoint(endpoint,true),"a configured train target can replace an arrival main signal but never impersonates a departure signal");
        var oneWay=line.Clone();oneWay.Mode=LineTrackMode.Directional;oneWay.Reverse.Clear();oneWay.Validate();Check(LineSignalGroups.Used(oneWay).Count()==2,"one-way line setup requires only its applicable departure and arrival groups");
    }
    static void Neighbour() {
        string key=new string('f',64),id=Id();var master=new DeskDefinition{Code="ESTW",Name="Master",Global=true,Panel=new PanelDocument{WorldKey=key}};var a=new DeskDefinition{Code="A",Name="A",Panel=new PanelDocument{WorldKey=key}};var b=new DeskDefinition{Code="B",Name="B",Panel=new PanelDocument{WorldKey=key}};
        a.Panel.Put(new PanelTile{Kind=PanelTileKind.Signal,Reference=id,X=1,Y=1});b.Panel.Put(new PanelTile{Kind=PanelTileKind.Signal,Reference=id,ReadOnlySignal=true,X=2,Y=1});var library=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{master,a,b}};
        Check(StationAutomationPolicy.Owner(library,id)?.Id==a.Id,"a neighbour display cannot take ownership from the actual controlling interlocking");
        var move=new RouteDefinition{Entry=id,Exit="other"};Check(!StationAutomationPolicy.CanControl(library,b.Id,new[]{move})&&StationAutomationPolicy.CanControl(library,master.Id,new[]{move}),"local neighbour displays cannot operate the owning interlocking's routes, while central master control remains available");
        b.OutOfService=true;Check(new OperatingStateIndex(library,Array.Empty<RouteDefinition>()).Signal(id)==SignalOperatingState.Active,"shutting down a display-only neighbour panel cannot take the actual neighbour's signal out of service");
        var displayOnly=library.Clone();displayOnly.Desks.Single(s=>s.Id==a.Id).Panel.Tiles.Clear();Check(new OperatingStateIndex(displayOnly,Array.Empty<RouteDefinition>()).Signal(id)==SignalOperatingState.Unbound,"a remote display alone cannot commission an otherwise unbound physical signal");
        var copied=library.Clone();Check(copied.Desks.Single(s=>s.Id==b.Id).Panel.At(2,1).ReadOnlySignal,"read-only neighbour status survives shared panel cloning");
    }
}
