using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
static class RouteStyleMovementTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static DirectedRouteSection Span(string segment,double a,double b,bool reverse=false)=>new(){Section=new(){Segment=segment,From=a,To=b},Reverse=reverse};
    static MarkerRecord Signal(string id,string segment,double at,bool reverse=false)=>new(){Id=id,Kind=MarkerRecord.Main,Anchor=new(){Segment=segment,Meters=at},FacingReverse=reverse};
    public static void Run() {
        var span=Span("track",0,100);var shunt=new RouteRun{Stage=RouteStage.Set,ChainReady=true,Route=new(){Shunting=true,Sections=new(){span.Section},Path=new(){span}}};
        var train=new RouteRun{Stage=RouteStage.Set,ChainReady=true,Route=new(){Shunting=false,Reduced=true,Sections=new(){span.Section},Path=new(){span}}};
        Check(PanelRouteStyle.Kind(shunt)==PanelRouteKind.Shunt&&PanelRouteStyle.Kind(train)==PanelRouteKind.Train,"route display distinguishes shunting from train/reduced train using actual admitted route metadata");
        Check(PanelRouteStyle.Resolve(TrackOccupancy.Clear,PanelRouteKind.Shunt)==PanelRailInk.Shunt&&PanelRouteStyle.Resolve(TrackOccupancy.Clear,PanelRouteKind.Train)==PanelRailInk.Train,"clear held rails select white-shunting versus green-train display roles");
        foreach(var kind in new[]{PanelRouteKind.None,PanelRouteKind.Train,PanelRouteKind.Shunt}) {
            Check(PanelRouteStyle.Resolve(TrackOccupancy.Occupied,kind)==PanelRailInk.Occupied,"occupied red wins over every route display type · "+kind);
            Check(PanelRouteStyle.Resolve(TrackOccupancy.Unknown,kind)==PanelRailInk.Unknown,"unknown detection cannot become white/green through route membership · "+kind);
        }
        foreach(var stage in new[]{RouteStage.Preparing,RouteStage.WaitingProtection,RouteStage.Fault,RouteStage.Cancelled}){shunt.Stage=stage;Check(PanelRouteStyle.Kind(shunt)==PanelRouteKind.None,"unadmitted, retained or cancelled route does not receive an admitted white colour · "+stage);}
        shunt.Stage=RouteStage.Running;shunt.ChainReady=false;Check(PanelRouteStyle.Kind(shunt)==PanelRouteKind.None,"a running route without ready chain cannot appear as an admitted route");shunt.ChainReady=true;
        var part=new TrackSection{Segment="track",From=10,To=30};
        Check(PanelRouteStyle.OnSection(part,new[]{shunt})==PanelRouteKind.Shunt&&PanelRouteStyle.OnSection(new TrackSection{Segment="parallel",From=10,To=30},new[]{shunt})==PanelRouteKind.None,"section route colouring follows held physical intervals instead of neighbouring drawn track");
        shunt.ReleasedSections.Add(span.Section.Key);Check(PanelRouteStyle.OnSection(part,new[]{shunt})==PanelRouteKind.None,"released route sections lose their white indication");shunt.ReleasedSections.Clear();
        Check(PanelRouteStyle.OnSection(part,new[]{shunt,train})==PanelRouteKind.Train&&PanelRouteStyle.OnSection(part,new[]{train,shunt})==PanelRouteKind.Train,"combined display membership has a deterministic order-independent train priority");
        Check(PanelRouteStyle.Resolve(TrackOccupancy.Clear,PanelRouteKind.None)==PanelRailInk.Free,"free unreserved track retains its idle colour role");
        int queried=0;bool Forbidden(){queried++;throw new InvalidOperationException("must not query alternate");}
        Check(WaypointMovementPolicy.Choose(MovementRole.Train,true,true,Forbidden,Forbidden)==MovementRole.Train&&WaypointMovementPolicy.Choose(MovementRole.Shunt,true,true,Forbidden,Forbidden)==MovementRole.Shunt&&queried==0,"explicit train and shunting selections are never automatically changed");
        Check(WaypointMovementPolicy.Choose(MovementRole.Unassigned,true,true,()=>true,Forbidden)==MovementRole.Train&&queried==0,"automatic movement prefers an available train destination without even probing shunting");
        Check(WaypointMovementPolicy.Choose(MovementRole.Unassigned,true,true,()=>false,()=>true)==MovementRole.Shunt,"automatic movement can use a proven local shunting alternative when no train destination exists");
        Check(WaypointMovementPolicy.Choose(MovementRole.Unassigned,true,false,()=>false,Forbidden)==MovementRole.Train&&WaypointMovementPolicy.Choose(MovementRole.Unassigned,true,true,()=>false,()=>false)==MovementRole.Train,"missing shunting capability or local proof never manufactures a shunting permission");
        bool fault=false;try{WaypointMovementPolicy.Choose(MovementRole.Unassigned,true,true,Forbidden,()=>{throw new Exception("unexpected fallback");});}catch(InvalidOperationException){fault=true;}
        Check(fault,"a failed or ambiguous train-target query is propagated instead of triggering a shunting fallback");
        var path=new[]{Span("local",0,100)};var start=Signal("start","local",10);var target=Signal("target","local",80);
        var approach=WaypointApproach.Create(path,new(){Segment="local",Meters=0},false,new(){Segment="local",Meters=100});
        Check(WaypointMovementPolicy.ProvenLocalAlternative(approach,start,target,true,Array.Empty<LineTrackDefinition>()),"same-station shunting fallback is admitted only within the proven current waypoint path");
        Check(!WaypointMovementPolicy.ProvenLocalAlternative(approach,start,target,false,Array.Empty<LineTrackDefinition>())&&!WaypointMovementPolicy.ProvenLocalAlternative(approach,start,Signal("outside","parallel",80),true,Array.Empty<LineTrackDefinition>())&&!WaypointMovementPolicy.ProvenLocalAlternative(approach,start,Signal("opposite","local",80,true),true,Array.Empty<LineTrackDefinition>()),"different stations, unrelated track and opposite direction forbid automatic shunting fallback");
        var line=new LineTrackDefinition{Enabled=false,Forward=new(){Span("local",40,60)}};
        Check(!WaypointMovementPolicy.ProvenLocalAlternative(approach,start,target,true,new[]{line}),"even a disabled configured free line cannot be crossed by local automatic shunting fallback");
        line.Forward=new(){Span("local",0,5)};Check(WaypointMovementPolicy.ProvenLocalAlternative(approach,start,target,true,new[]{line}),"a line entirely behind the start does not incorrectly disqualify a local shunting leg");
        line.Forward.Clear();line.BlockUnits.Add(new BlockSignalUnit{Forward=target.Id});Check(!WaypointMovementPolicy.ProvenLocalAlternative(approach,start,target,true,new[]{line}),"block signals remain excluded from the local shunting alternative");
        var reversePath=new[]{Span("local",0,100,true)};var reverse=WaypointApproach.Create(reversePath,new(){Segment="local",Meters=100},true,new(){Segment="local",Meters=0});
        Check(WaypointMovementPolicy.ProvenLocalAlternative(reverse,Signal("back-start","local",90,true),Signal("back-end","local",20,true),true,Array.Empty<LineTrackDefinition>()),"local shunting fallback proves reversed track coordinates as well as the forward direction");
    }
}
