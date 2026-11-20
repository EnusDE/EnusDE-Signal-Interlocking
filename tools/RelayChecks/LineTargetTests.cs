using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class LineTargetTests {
    static string Id()=>Guid.NewGuid().ToString("N");
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static DirectedRouteSection P(double a,double b,bool reverse=false)=>new DirectedRouteSection{Section=new TrackSection{Segment="S",From=a,To=b},Reverse=reverse};
    static MarkerRecord M(string name,double meters,bool reverse=false)=>new MarkerRecord{Id=Id(),Name=name,Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="S",Meters=meters},FacingReverse=reverse,AllowHp1=true,AllowHp2=true};
    static RouteDefinition R(RouteProposal p)=>new RouteDefinition{Code="R-"+Id().Substring(0,6),Entry=p.Entry,Exit=p.Exit,Path=p.Path.Select(s=>s.Clone()).ToList(),Sections=p.Path.Select(s=>s.Section.Clone()).ToList(),ViaSignals=new List<RouteViaSignal>(),Approach=new List<TrackSection>{new TrackSection{Segment="approach",From=0,To=20}}};
    sealed class World:IInterlockingWorld,ISectionalWorld {
        public bool PathValid(RouteDefinition route)=>true;
        public TrackOccupancy Occupancy(TrackSection section)=>TrackOccupancy.Clear;
        public bool SwitchPosition(string id,out bool thrown){thrown=false;return true;}
        public bool SetSwitch(string id,bool thrown)=>true;
        public bool CaptureTrain(RouteDefinition route,out string[] cars){cars=new[]{"train"};return true;}
        public bool TrainProgress(RouteDefinition route,string[] cars,out double head,out double tail){head=-20;tail=-100;return true;}
    }
    public static void Run() {
        string key=new string('d',64);var lib=new DeskLibrary{WorldKey=key};
        DeskDefinition Desk(string code,bool master=false)=>new DeskDefinition{Code=code,Name=code,Global=master,Panel=new PanelDocument{WorldKey=key}};
        var a=Desk("WHB");var b=Desk("ELA");var c=Desk("BLOCK");var master=Desk("ESTW",true);lib.Desks.AddRange(new[]{master,a,b,c});
        var source=M("HS-006",50);var arrival=M("HS-058",2900);var opposite=M("HS-B",2950,true);var arrivalA=M("HS-A",100,true);
        var forward=new[]{M("BS-1A",990),M("BS-2A",1990)};var backward=new[]{M("BS-1B",1010,true),M("BS-2B",2010,true)};
        void Bind(DeskDefinition desk,MarkerRecord signal,int x){desk.Panel.Put(new PanelTile{X=x,Y=2,Kind=PanelTileKind.Signal,Reference=signal.Id});}
        Bind(a,source,1);Bind(a,arrivalA,2);Bind(b,arrival,1);Bind(b,opposite,2);
        for(int i=0;i<2;i++){Bind(c,forward[i],i*2+1);Bind(c,backward[i],i*2+2);}
        var target=new AuxiliaryTarget{Name="Nach Ela",Anchor=new MarkerAnchor{Segment="S",Meters=80},Train=true};a.Panel.Put(new PanelTile{X=4,Y=2,Kind=PanelTileKind.AuxiliaryTarget,Target=target});
        var line=new LineTrackDefinition{Code="STR-001",Name="WHB-ELA",StationA=a.Id,StationB=b.Id,Enabled=true,NormalDirection=-1,ForwardStart=source.Id,ForwardEnd=arrival.Id,ReverseStart=opposite.Id,ReverseEnd=arrivalA.Id,Forward=new List<DirectedRouteSection>{P(100,2900)},Reverse=new List<DirectedRouteSection>{P(100,2900,true)},Topology=new string('a',64),BlockOwner=c.Id};
        foreach(string id in LineSignalGroups.All(line))line.Anchors[id]="fixture";
        string boundaryA=Id(),boundaryB=Id();
        for(int i=0;i<2;i++)line.BlockUnits.Add(new BlockSignalUnit{Boundary=i==0?boundaryA:boundaryB,Forward=forward[i].Id,Reverse=backward[i].Id,Distance=(i+1)*1000});
        var ends=new[]{Id(),boundaryA,boundaryB,Id()};var distances=new[]{100d,1000,2000,2900};
        for(int i=0;i<3;i++){string id="c:"+Id();line.BlockSections.Add(id);lib.Blocks[id]=new TrackBlock{Id=id,Name="Block "+i,BoundaryA=ends[i],BoundaryB=ends[i+1],Path=new List<DirectedRouteSection>{P(distances[i],distances[i+1])}};}
        lib.FreeLines.Add(line);a.Panel.Put(new PanelTile{X=4,Y=3,Kind=PanelTileKind.LinePermission,Reference=line.Id,PermissionAuxiliary=target.Id});lib.Validate(key);
        var binding=LineTargetBindings.Index(lib)[target.Id];
        Check(binding.Error==null&&binding.Direction==1&&binding.Station==a.Id&&binding.Arrival==arrival.Id&&target.Function==AuxiliaryFunction.RouteDestination&&target.Endpoint().Anchor.Meters==80,"linking a normal auxiliary to its permission module retains its function and resolves the neighbour's real arrival signal");
        var network=new NetworkInput{World=key,Nodes=new List<PlanNode>{new PlanNode{Id="n0"},new PlanNode{Id="n1",X=3000}},Edges=new List<PlanEdge>{new PlanEdge{Id="S",A="n0",B="n1",Length=3000}}};
        var signals=new[]{source,arrival,opposite,arrivalA}.Concat(forward).Concat(backward).ToArray();
        var proposals=AutomaticRoutePlanner.Find(network,signals,source,arrival,RouteKind.Train);
        Check(proposals.Count==1&&proposals[0].Count==3&&proposals[0].Select(p=>p.Entry).SequenceEqual(new[]{source.Id,forward[0].Id,forward[1].Id}),"a bound destination plans through every intervening block signal instead of stopping at the local drawn auxiliary");
        var plan=new RouteChainPlan{Routes=proposals[0].Select(R).ToList()};LineTargetBindings.Tag(plan,binding);LineTargetBindings.RequirePlan(lib,plan.Routes);
        Check(StationAutomationPolicy.CanControl(lib,a.Id,plan.Routes)&&!StationAutomationPolicy.CanControl(lib,b.Id,plan.Routes)&&!lib.Automation.Enabled,"a local bound request may operate only its assigned block chain without requiring global contact automation or control of neighbour station signals");
        var untagged=new RouteChainPlan{Routes=plan.Routes.Select(r=>r.Clone()).ToList()};foreach(var r in untagged.Routes){r.LineTarget=null;r.LineTargetSignature=null;}
        Check(!StationAutomationPolicy.CanControl(lib,a.Id,untagged.Routes),"ordinary unbound manual requests retain the existing foreign-signal ownership guard");
        var authority=new LineDirectionAuthority{BlockEnabled=true,Occupancy=_=>TrackOccupancy.Clear,TargetBinding=r=>LineTargetBindings.Index(lib).TryGetValue(r.LineTarget??r.Exit,out var info)?info:null};authority.Configure(lib.FreeLines,_=>null);
        for(int i=1;i<=3;i++)authority.Observe(Array.Empty<RouteRun>(),i);
        var local=R(new RouteProposal{Entry=source.Id,Exit=target.Id,Path=new List<DirectedRouteSection>{P(50,80)}});var localPlan=new RouteChainPlan{Routes=new List<RouteDefinition>{local}};LineTargetBindings.Tag(localPlan,binding);
        var engine=new InterlockingEngine{Admission=authority};var world=new World();
        Check(Reject(()=>engine.Request(localPlan.Routes[0],world))&&engine.Active.Count==0&&!authority.Get(line.Id).Held,"wrong-end permission prevents a green signal even when the old local auxiliary path never reaches the shared line axis");
        Check(Reject(()=>LineTargetBindings.RequirePlan(lib,new[]{local})),"a saved route ending at the former local helper is rejected until recalculated through the block chain");
        var raceLine=line.Clone();raceLine.NormalDirection=1;var race=new LineDirectionAuthority{BlockEnabled=true,Occupancy=_=>TrackOccupancy.Clear,TargetBinding=authority.TargetBinding};race.Configure(new[]{raceLine},_=>null);
        for(int i=1;i<=3;i++)race.Observe(Array.Empty<RouteRun>(),i);race.RequirePermission(line.Id,1);
        race.RequestPermission(line.Id,b.Id,race.Get(line.Id).Revision);race.GivePermission(line.Id,a.Id,race.Get(line.Id).Revision,Array.Empty<RouteRun>());
        var racedEngine=new InterlockingEngine{Admission=race};
        Check(Reject(()=>racedEngine.RequestChain(plan.Routes,world))&&racedEngine.Active.Count==0&&!race.Get(line.Id).Held,"permission given away after preparation is checked again at chain admission and leaves every signal without authority");
        authority.RequestPermission(line.Id,a.Id,authority.Get(line.Id).Revision);authority.GivePermission(line.Id,b.Id,authority.Get(line.Id).Revision,Array.Empty<RouteRun>());
        authority.RequirePermission(line.Id,1);engine.RequestChain(plan.Routes,world);for(int i=0;i<20;i++)engine.Tick(world);
        Check(engine.ForSignal(source.Id)?.Permitted==true&&forward.All(s=>engine.ForSignal(s.Id)?.Permitted==true)&&backward.All(s=>engine.ForSignal(s.Id)?.Permitted!=true)&&engine.ForSignal(arrival.Id)==null,"with the correct permission a single request clears the exit and both forward block signals while opposing signals and neighbour entry remain untouched");
        var changed=lib.Clone();changed.FreeLines[0].ForwardEnd=opposite.Id;
        Check(Reject(()=>LineTargetBindings.RequirePlan(changed,plan.Routes)),"changing the line destination invalidates prepared or saved binding signatures instead of reusing an old chain");
        var partial=new RouteChainPlan{Routes=plan.Routes.Take(1).Select(r=>r.Clone()).ToList()};LineTargetBindings.RequirePlan(lib,partial.Routes);
        Check(partial.Routes[0].LineTarget==target.Id&&partial.Routes[0].LineTargetSignature==LineTargetBindings.Signature(binding),"a waypoint-limited prefix retains the explicit permission binding for final admission");
        var disabled=line.Clone();disabled.Enabled=false;var inactive=new LineDirectionAuthority{BlockEnabled=true,Occupancy=_=>TrackOccupancy.Clear,TargetBinding=authority.TargetBinding};inactive.Configure(new[]{disabled},_=>null);
        Check(Reject(()=>inactive.Reserve(localPlan.Routes)),"an inactive bound line cannot be bypassed by a local destination route before the physical axis");
        var unlinked=lib.Clone();unlinked.Desks.Single(d=>d.Id==a.Id).Panel.At(4,3).PermissionAuxiliary=null;
        var changes=LineTargetBindings.ChangedTargets(lib,unlinked,out var affected);
        Check(changes.SequenceEqual(new[]{target.Id})&&affected.SequenceEqual(new[]{line.Id})&&Reject(()=>LineTargetBindings.RequirePlan(unlinked,partial.Routes)),"unlinking a destination identifies its guarded line/target dependencies and invalidates an outstanding partial request");
        var round=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(lib));round.Validate(key);var routeRound=JsonConvert.DeserializeObject<RouteDefinition>(JsonConvert.SerializeObject(plan.Routes[0]));
        Check(LineTargetBindings.Index(round)[target.Id].Direction==1&&routeRound.LineTargetSignature==plan.Routes[0].LineTargetSignature,"permission module links and route binding signatures survive save and multiplayer configuration serialization");
        var ambiguous=lib.Clone();ambiguous.Desks.Single(d=>d.Id==b.Id).Panel.Put(new PanelTile{X=4,Y=2,Kind=PanelTileKind.AuxiliaryTarget,Target=target.Clone()});ambiguous.Validate(key);
        Check(LineTargetBindings.Index(ambiguous)[target.Id].Error!=null&&Reject(()=>LineTargetBindings.RequirePlan(ambiguous,plan.Routes)),"an old auxiliary duplicated across local boards remains inspectable but cannot authorize a bound departure with ambiguous ownership");
    }
}
