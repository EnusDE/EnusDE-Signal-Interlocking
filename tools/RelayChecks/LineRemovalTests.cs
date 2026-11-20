using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class LineRemovalTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static string Id()=>Guid.NewGuid().ToString("N");
    static DirectedRouteSection Part(string segment,bool reverse=false)=>new DirectedRouteSection{Section=new TrackSection{Segment=segment,From=0,To=400},Reverse=reverse};
    public static void Run() {
        string key=new string('d',64);var objects=new MarkerDocument{WorldKey=key,Map="map"};var repo=new MarkerRepository("unused",key,"map",()=>objects,d=>objects=d);
        var left=repo.Put(new MarkerAnchor{Segment="S",Meters=0},kind:MarkerRecord.Main);var right=repo.Put(new MarkerAnchor{Segment="S",Meters=400},kind:MarkerRecord.Main);
        var library=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey=key}},new DeskDefinition{Code="WHB",Name="Whittier",Panel=new PanelDocument{WorldKey=key}},new DeskDefinition{Code="ELA",Name="Ela",Panel=new PanelDocument{WorldKey=key}}}};
        library.Desks[1].Panel.Put(new PanelTile{X=1,Y=7,Kind=PanelTileKind.Signal,Reference=left.Id});library.Desks[2].Panel.Put(new PanelTile{X=1,Y=7,Kind=PanelTileKind.Signal,Reference=right.Id});
        var line=LineSetupWorkflow.Create(library,library.Desks[1].Id);line.Mode=LineTrackMode.Gleiswechselbetrieb;line.ForwardStart=left.Id;line.ForwardEnd=right.Id;line.ReverseStart=MastConfiguration.Effective(right).Back.Id;line.ReverseEnd=MastConfiguration.Effective(left).Back.Id;line.Forward.Add(Part("S"));line.Reverse.Add(Part("S",true));
        var built=LineBlockBuilder.Build(objects,library,line,BlockLayoutPlan.Create(line.Axis,2),library.Desks[1].Id,true,displays:new[]{library.Desks[2].Id});library=built.Library;objects=built.Objects;
        library=LineNumberOverview.Create(library,line.Id,library.Desks[0].Id,LineNumberOverview.AutomaticSources(library,built.Line));
        var target=new AuxiliaryTarget{Name="To Ela",Train=true,Anchor=new MarkerAnchor{Segment="S",Meters=400},PermissionLine=line.Id};
        library.Desks[1].Panel.Put(new PanelTile{X=3,Y=6,Kind=PanelTileKind.AuxiliaryTarget,Target=target});
        library.Desks[0].Panel.Put(new PanelTile{X=8,Y=8,Kind=PanelTileKind.AuxiliaryTarget,Target=target.Clone()});
        library.Desks[1].Panel.Put(new PanelTile{X=2,Y=6,Kind=PanelTileKind.LinePermission,Reference=line.Id,PermissionAuxiliary=target.Id,InlinePermission=true,Ports=17,BlockId=built.Line.BlockSections[0]});
        var permissionOnly=new AuxiliaryTarget{Function=AuxiliaryFunction.LinePermission,PermissionLine=line.Id};
        library.Desks[2].Panel.Put(new PanelTile{X=4,Y=6,Kind=PanelTileKind.AuxiliaryTarget,Target=permissionOnly});
        var other=LineSetupWorkflow.Create(library,library.Desks[1].Id);other.Track="2";other.Forward.Add(Part("T"));other.Reverse.Add(Part("T",true));LineSetupWorkflow.Put(library,other);
        library.Desks[2].Panel.Put(new PanelTile{X=5,Y=6,Kind=PanelTileKind.LinePermission,Reference=other.Id});library.Validate(key);
        string before=JsonConvert.SerializeObject(library),world=JsonConvert.SerializeObject(objects);int fields=library.Desks.Sum(d=>d.Panel.Tiles.Count),blocks=library.Blocks.Count;
        var preview=LineRemoval.Preview(library,line.Id);var next=preview.Library;
        Check(JsonConvert.SerializeObject(library)==before&&JsonConvert.SerializeObject(objects)==world,"line-deletion preview changes neither the saved connection nor physical signals and detection");
        Check(next.FreeLines.Count==1&&next.FreeLines[0].Id==other.Id&&preview.Indicators==3,"GWB deletion removes the selected shared connection and clears every generated and manual permission indicator");
        Check(next.Desks[2].Panel.At(5,6).Reference==other.Id,"deleting one GWB track preserves an independent parallel connection");
        var copies=next.Desks.SelectMany(d=>d.Panel.Tiles).Where(t=>t.Target?.Id==target.Id).Select(t=>t.Target).ToArray();
        Check(copies.Length==2&&copies.All(t=>t.Function==AuxiliaryFunction.LinePermission&&t.PermissionLine==""&&t.Anchor.Meters==400)&&copies.All(t=>t.Endpoint()==null),"all copies of a bound auxiliary retain their settings but cannot become an unprotected train destination after line deletion");
        Check(next.Desks[2].Panel.At(4,6).Target.PermissionLine==""&&preview.Auxiliaries==2,"standalone permission-only auxiliary buttons are detached along with route destinations");
        Check(next.Desks.Sum(d=>d.Panel.Tiles.Count)==fields&&next.Blocks.Count==blocks&&next.Desks.All(d=>d.Panel.Areas.All(a=>a.BlockLine!=line.Id&&a.NumberLine!=line.Id&&a.BlockView==null&&a.BlockTemplate==null)),"drawings, train-number sources and GFM remain intact while generated group metadata no longer points at the deleted line");
        Check(new NumberMirrorIndex(next)!=null&&next.Desks[1].Panel.At(2,6).Ports==17&&next.Desks[1].Panel.At(2,6).BlockId==built.Line.BlockSections[0],"number mirrors and inline permission-field detection remain valid after detaching the connection");
        Check(preview.SignalRules==2&&preview.Contacts==2&&next.Automation.Contacts.All(c=>!c.Enabled)&&next.Automation.Signals.Where(p=>built.Line.BlockUnits.Any(u=>u.Forward==p.Key||u.Reverse==p.Key)).All(p=>!p.Value.Enabled),"associated block automation and contacts stop instead of continuing after removal of their permission");
        var unrelated=new RouteRun{Route=new RouteDefinition{Code="Other station",Entry=Id(),Exit=Id(),Path=new List<DirectedRouteSection>{Part("elsewhere")}},Stage=RouteStage.Set};
        LineRemoval.RequireClear(library,line.Id,new[]{unrelated},Array.Empty<DirectionBlockLease>(),Array.Empty<LineDirectionState>(),_=>TrackOccupancy.Clear);
        Check(true,"an unrelated route elsewhere does not prevent deleting a clear GWB connection");
        Check(Reject(()=>LineRemoval.RequireClear(library,line.Id,Array.Empty<RouteRun>(),Array.Empty<DirectionBlockLease>(),Array.Empty<LineDirectionState>(),_=>TrackOccupancy.Occupied)),"an occupied line blocks deletion even without any active route or recorded direction hold");
        Check(Reject(()=>LineRemoval.RequireClear(library,line.Id,Array.Empty<RouteRun>(),Array.Empty<DirectionBlockLease>(),Array.Empty<LineDirectionState>(),_=>TrackOccupancy.Unknown))&&Reject(()=>LineRemoval.RequireClear(library,line.Id,Array.Empty<RouteRun>(),Array.Empty<DirectionBlockLease>(),Array.Empty<LineDirectionState>(),_=>throw new Exception("unreadable"))),"unknown and failing occupancy reads cannot authorize line deletion");
        var active=new RouteRun{Route=new RouteDefinition{Code="Current departure",Entry=left.Id,Exit=right.Id},Stage=RouteStage.Set};
        Check(Reject(()=>LineRemoval.RequireClear(library,line.Id,new[]{active},Array.Empty<DirectionBlockLease>(),Array.Empty<LineDirectionState>(),_=>TrackOccupancy.Clear)),"active departures on the selected line keep deletion blocked after clear track readings");
        Check(Reject(()=>LineRemoval.RequireClear(library,line.Id,Array.Empty<RouteRun>(),Array.Empty<DirectionBlockLease>(),new[]{new LineDirectionState{Definition=built.Line,Held=true,Axis=built.Line.Axis}},_=>TrackOccupancy.Clear)),"retained line-direction authority must be safely released before deletion");
        var roundTrip=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(next));roundTrip.Validate(key);Check(roundTrip.FreeLines.Count==1,"cleaned deletion configuration survives validation, saving and reloading without stale permission references");
        Check(Reject(()=>LineRemoval.Preview(next,line.Id)),"a stale repeated deletion cannot remove a different connection");
        var draft=LineSetupWorkflow.Create(next,next.Desks[1].Id);draft.Track="draft";LineSetupWorkflow.Put(next,draft);
        LineRemoval.RequireClear(next,draft.Id,Array.Empty<RouteRun>(),Array.Empty<DirectionBlockLease>(),Array.Empty<LineDirectionState>(),_=>throw new Exception("no path to read"));
        Check(LineRemoval.Preview(next,draft.Id).Library.FreeLines.Count==1,"incomplete setup drafts can be deleted without completing the guided path or signal steps");
    }
}
