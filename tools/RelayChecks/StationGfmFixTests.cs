using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class StationGfmFixTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static string Id()=>Guid.NewGuid().ToString("N");
    static DirectedRouteSection Piece(string segment,double from,double to,bool reverse=false)=>new DirectedRouteSection{Section=new TrackSection{Segment=segment,From=from,To=to},Reverse=reverse};
    static TrackBlock Circuit(string segment,double from,double to)=>new TrackBlock{Id="c:"+Id(),Name="GFM",BoundaryA=Id(),BoundaryB=Id(),Path=new List<DirectedRouteSection>{Piece(segment,from,to)}};
    public static void Run() {Station();Signatures();PointLinks();GuardGeometry();}
    static void GuardGeometry() {
        var graph=new NetworkInput{World="guard-geometry",Nodes=new List<PlanNode>{new PlanNode{Id="a"},new PlanNode{Id="W",Point=true,Common="stem",Normal="normal"},new PlanNode{Id="b"},new PlanNode{Id="c"}},Edges=new List<PlanEdge>{new PlanEdge{Id="stem",A="a",B="W",Length=100},new PlanEdge{Id="normal",A="W",B="b",Length=100},new PlanEdge{Id="branch",A="W",B="c",Length=100}}};
        graph.Cuts.Add(new PlanCut{Signal=Id(),Segment="stem",Meters=95});
        var point=NetworkPlan.Build(graph,draw:false).Blocks["p:W"];
        Check(point.Guards.Single(g=>g.Segment=="stem").From==75,"adding a signal without GFM does not shorten the actual point guard or shift its boundary");
        graph.Cuts.Add(new PlanCut{Boundary=TrackCircuit.PointId("W",0),BoundaryKind=2,Segment="stem",Meters=91});
        point=NetworkPlan.Build(graph,draw:false).Blocks["p:W"];
        Check(point.Guards.Single(g=>g.Segment=="stem").From==75,"an old generated point boundary cannot perpetuate its own incorrect guard position during rescan");
        graph.Cuts.Add(new PlanCut{Boundary=Id(),BoundaryKind=1,Segment="stem",Meters=85});
        point=NetworkPlan.Build(graph,draw:false).Blocks["p:W"];
        Check(point.Guards.Single(g=>g.Segment=="stem").From==85&&point.Guards.Where(g=>g.Segment!="stem").All(g=>g.To-g.From==25),"an actual nearer physical GFM limits only its own point arm while retaining the other protection legs");
    }
    static void Station() {
        string key=new string('e',64);var local=new DeskDefinition{Code="WHB",Name="Whittier",Panel=new PanelDocument{WorldKey=key}};
        local.Panel.Put(new PanelTile{Kind=PanelTileKind.Signal,X=1,Y=1,Reference="a"});local.Panel.Put(new PanelTile{Kind=PanelTileKind.Signal,X=3,Y=1,Reference="b"});
        var next=new DeskDefinition{Code="ELA",Name="Ela",Panel=new PanelDocument{WorldKey=key}};next.Panel.Put(new PanelTile{Kind=PanelTileKind.Signal,X=1,Y=1,Reference="c"});
        var library=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey=key}},local,next}};
        var route=new RouteDefinition{Entry="a",Exit="b",Path=new List<DirectedRouteSection>{Piece("S",100,200)}};
        Check(StationAutomationPolicy.LocalMovement(library,route,Array.Empty<MarkerAnchor>()),"an ordinary main-signal route within one station is local even though it is not a shunting route");
        var authority=new DirectionBlockAuthority{Enabled=true,ProtectRoute=r=>!StationAutomationPolicy.LocalMovement(library,r,Array.Empty<MarkerAnchor>()),Occupancy=s=>TrackOccupancy.Occupied,
            Blocks=new List<DirectionBlock>{new DirectionBlock{Id="BL-station",Axis=new List<DirectedRouteSection>{Piece("S",0,200)}}}};
        using(var ticket=authority.Reserve(new[]{route}))ticket.Commit();
        Check(authority.Leases.Count==0,"a train waiting behind its station signal cannot block the local onward route through an unrelated aggregate direction chain");
        var external=route.Clone();external.Exit="c";
        Check(!StationAutomationPolicy.LocalMovement(library,external,Array.Empty<MarkerAnchor>())&&Reject(()=>authority.Reserve(new[]{external})),"a route to another station retains direction-chain checks and cannot use the local-station exception");
        Check(!StationAutomationPolicy.LocalMovement(library,route,new[]{new MarkerAnchor{Segment="S",Meters=150}}),"crossing an entry or exit boundary inside a route prevents a false local-station exemption");
        var rear=Circuit("S",0,100);var ahead=Circuit("S",100,200);int reads=0;
        string error=StationRouteProtection.CircuitError(new[]{route},new[]{rear,ahead},_=>true,b=>{reads++;return b==rear?TrackOccupancy.Occupied:TrackOccupancy.Clear;},_=>"section");
        Check(error==null&&reads==1,"local GFM checks read only directly overlapping circuits rather than rejecting occupancy behind the start signal");
        Check(StationRouteProtection.CircuitError(new[]{route},new[]{ahead},_=>true,_=>TrackOccupancy.Occupied,_=>"A ↔ B")?.Contains("A ↔ B")==true,"a genuinely occupied configured GFM on the requested route remains rejected with its boundary pair");
        Check(StationRouteProtection.CircuitError(new[]{route},new[]{ahead},_=>true,_=>TrackOccupancy.Unknown,_=>"A ↔ B")!=null,"unreadable configured detection is not silently reported clear");
        Check(StationRouteProtection.CircuitError(new[]{route},new[]{ahead},_=>false,_=>throw new Exception("must not read invalid circuit"),_=>"invalid")==null,
            "invalid or absent GFM definitions do not masquerade as valid occupied circuits when the manual unmonitored policy is enabled");
        route.Shunting=true;Check(StationRouteProtection.CircuitError(new[]{route},new[]{ahead},_=>true,_=>TrackOccupancy.Occupied,_=>"target")==null,"GFM admission does not remove the existing occupied-target shunting workflow");
        var legacy=JsonConvert.DeserializeObject<AutomationConfiguration>("{}");
        Check(legacy.AllowUnmonitoredManualRoutes&&legacy.AutomaticGfmCatalog,"legacy saves receive explicit manual-unmonitored operation and background-catalog policy defaults");
        legacy.AllowUnmonitoredManualRoutes=false;legacy.AutomaticGfmCatalog=false;var copy=JsonConvert.DeserializeObject<AutomationConfiguration>(JsonConvert.SerializeObject(legacy));
        Check(!copy.AllowUnmonitoredManualRoutes&&!copy.AutomaticGfmCatalog,"host policy choices survive shared configuration serialization");
    }
    static void Signatures() {
        var network=new NetworkInput{Nodes=new List<PlanNode>{new PlanNode{Id="a"},new PlanNode{Id="b",X=100}},Edges=new List<PlanEdge>{new PlanEdge{Id="S",A="a",B="b",Length=100}},Cuts=new List<PlanCut>{new PlanCut{Boundary=Id(),BoundaryKind=1,Segment="S",Meters=10}}};
        var block=Circuit("S",10,90);var stops=new[]{new MarkerAnchor{Segment="S",Meters=90,Lateral=2.3}};
        string signature=DirectionBlockTopology.Signature(network,new[]{block},stops);
        block.Name="renamed";network.Nodes[1].X=150;network.Cuts[0].BoundaryCode="new label";stops[0].Lateral=4;network.Cuts.Add(new PlanCut{Signal=Id(),Segment="S",Meters=55});
        Check(signature==DirectionBlockTopology.Signature(network,new[]{block},stops),"renaming GFM, changing presentation offsets or adding an unrelated non-detector signal cannot invalidate held direction topology");
        network.Cuts[0].Meters=11;
        Check(signature!=DirectionBlockTopology.Signature(network,new[]{block},stops),"actually moving a detector still invalidates block topology");
        network.Cuts[0].Meters=10;network.Cuts[0].BoundaryReverse=true;
        Check(signature!=DirectionBlockTopology.Signature(network,new[]{block},stops),"changing directional detector semantics invalidates the topology even with unchanged positions");
        network.Cuts[0].BoundaryReverse=false;block.Path[0].Section.To=95;
        Check(signature!=DirectionBlockTopology.Signature(network,new[]{block},stops),"changing a physical section path remains a genuine topology change");
    }
    static void PointLinks() {
        var block=new TrackBlock{Id="p:W",Name="W001",Point="W",Guards=new List<TrackSection>{new TrackSection{Segment="stem",From=75,To=100},new TrackSection{Segment="normal",From=0,To=25},new TrackSection{Segment="branch",From=0,To=25}}};
        var signal=new MarkerRecord{Id=Id(),Name="HS-001",Kind=MarkerRecord.Main,CircuitVariant=1,CircuitNumber=8,Anchor=new MarkerAnchor{Segment="stem",Meters=40}};
        var links=new Dictionary<int,string>{{0,signal.Id}};string[] segments={"stem","normal","branch"};bool[] atStart={false,true,true};double[] lengths={100,100,100};
        TrackBlock Apply(MarkerRecord end)=>PointBoundaryBinding.Apply(block,segments,atStart,lengths,links,_=>end);
        var bound=Apply(signal);
        Check(bound.Guards.Single(g=>g.Segment=="stem").From==40&&block.Guards.Single(g=>g.Segment=="stem").From==75&&bound.Guards.Count==3,
            "explicit point-arm binding extends real monitored protection to the selected signal without changing the original guard definition or other arms");
        var point=new MarkerRecord{Id=TrackCircuit.PointId("W",0),Name="GFM-B-W001-S",Kind=MarkerRecord.CircuitB,Anchor=new MarkerAnchor{Segment="stem",Meters=40}};
        var sharing=SharedCircuitBoundary.Resolve(new[]{signal},new[]{point},new Dictionary<string,string>{{point.Id,signal.Id}});
        Check(sharing.PointSignals[point.Id]==signal.Id&&CircuitIdentity.Caption(sharing.PointAlias(point)).StartsWith("GFM-A/B"),"explicit signal/point-arm binding resolves one physical A/B box while retaining both identities");
        var wrong=signal.Clone();wrong.Anchor.Segment="parallel";Check(Reject(()=>Apply(wrong)),"a point arm cannot be linked to a signal on another or parallel native segment");
        var c=signal.Clone();c.Kind=MarkerRecord.CircuitC;c.CircuitVariant=0;Check(Reject(()=>Apply(c)),"a one-sided C detector cannot be converted into a shared A/B point boundary");
        var missing=signal.Clone();missing.CircuitVariant=0;Check(Reject(()=>Apply(missing))&&Reject(()=>Apply(null)),"disabled or removed linked detectors make the point definition unreadable rather than reporting it clear");
        var free=signal.Clone();free.Kind=MarkerRecord.CircuitB;free.CircuitVariant=0;
        var bsharing=SharedCircuitBoundary.Resolve(new[]{free},new[]{point},new Dictionary<string,string>{{point.Id,free.Id}});
        Check(bsharing.PointSignals.Count==1&&CircuitIdentity.Caption(bsharing.PointAlias(point)).StartsWith("GFM-B-")&&SharedCircuitBoundary.Resolve(new[]{free},new[]{point}).PointSignals.Count==0,
            "an explicitly linked B box is reused without fabricating signal-A semantics or changing automatic proximity rules");
        string key=new string('e',64);var master=new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey=key}};
        master.Panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Switch,Reference="W",BlockId="p:W"});var library=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{master}};
        library.Blocks[block.Id]=block;library.Points["W"]=new PointAssignment{Code="W001",Desk=master.Id,ShowCircuit=true,BoundaryLinks=links};library.Validate(key);
        var renamed=PointAssignments.Put(library,"W","W002",master.Id);var clone=library.Clone();clone.Points["W"].BoundaryLinks.Clear();
        Check(renamed.Points["W"].BoundaryLinks.Count==1&&library.Points["W"].BoundaryLinks.Count==1,"point renaming retains boundary links and cloned draft dictionaries cannot mutate the live installation");
        var reset=GfmDetectionRefresh.Clear(library,new[]{master.Id});
        Check(reset.ResetOnly&&!reset.Library.Points["W"].ShowCircuit&&reset.Library.Points["W"].BoundaryLinks.Count==0&&!reset.Library.Automation.AutomaticGfmCatalog&&library.Points["W"].ShowCircuit,
            "clear-only reset disables selected point detection, removes its manual boundary links and pauses automatic recreation while remaining an isolated preview");
    }
}
