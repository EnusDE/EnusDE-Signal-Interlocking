using System;
using System.Collections.Generic;
using System.Linq;
using EnusDE.Signaltechnik;

static class WaypointRouteTests {
    static MarkerAnchor At(string segment,double meters)=>new MarkerAnchor{Segment=segment,Meters=meters};
    static DirectedRouteSection Span(string segment,double from,double to,bool reverse=false)=>
        new DirectedRouteSection{Section=new TrackSection{Segment=segment,From=from,To=to},Reverse=reverse};
    static MarkerRecord Signal(string id,string segment,double meters,string kind=MarkerRecord.Main,bool reverse=false)=>
        new MarkerRecord{Id=id,Anchor=At(segment,meters),Kind=kind,FacingReverse=reverse,AllowSh1=true};
    static RouteChainPlan Leg(MarkerRecord start,MarkerRecord end,params DirectedRouteSection[] path)=>new RouteChainPlan {
        Routes=new List<RouteDefinition>{new RouteDefinition{Entry=start.Id,Exit=end.Id,Path=new List<DirectedRouteSection>(path)}}
    };
    static void Check(bool value,string message){if(!value)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}

    static void BlockLine() {
        var departure=Signal("HS-007","line",0);var first=Signal("HS-014","line",290);
        var middle=Signal("HS-017","line",590);var last=Signal("HS-020","line",890);var arrival=Signal("HS-009","line",1200);
        var reverseLast=Signal("HS-015","line",310,reverse:true);var reverseArrival=Signal("HS-012","line",0,reverse:true);
        var line=new LineTrackDefinition{Enabled=true,StationA="Hemingway",StationB="Alarka",BlockOwner="Hemingway",
            ForwardStart=departure.Id,ForwardEnd=arrival.Id,ReverseStart="HS-010",ReverseEnd=reverseArrival.Id,
            Forward=new(){Span("line",0,1200)},Reverse=new(){Span("line",0,1200,true)},BlockUnits=new(){
                new BlockSignalUnit{Forward=middle.Id,Reverse="HS-018",Distance=600},new BlockSignalUnit{Forward=last.Id,Reverse="HS-021",Distance=900},
                new BlockSignalUnit{Forward=first.Id,Reverse=reverseLast.Id,Distance=300}}};
        var lines=new[]{line};
        List<DirectedRouteSection> Required(MarkerRecord start,MarkerRecord end,string from="Hemingway",string to="Alarka",bool shunt=false)=>
            WaypointLinePolicy.RequiredPath(lines,from,to,start,end,shunt);
        bool Refused(MarkerRecord start,MarkerRecord end,string from="Hemingway",string to="Alarka",bool shunt=false) {
            try{Required(start,end,from,to,shunt);return false;}catch(InvalidOperationException){return true;}
        }
        var final=Required(last,arrival);
        Check(final.Single().Section.From==890&&final[0].Section.To==1200&&WaypointLinePolicy.Covers(final,new[]{Span("line",890,900),Span("line",900,1200)}),
            "the final HS-020 to HS-009 waypoint leg proves only the correctly directed remaining line, including the staggered signal approach to its detector");
        Check(!WaypointLinePolicy.Covers(final,new[]{Span("line",890,900),Span("line",905,1200)})&&
            !WaypointLinePolicy.Covers(final,new[]{Span("line",890,1200,true)}),"a gap or reversed remaining section fails the waypoint block proof");
        var reverse=Required(reverseLast,reverseArrival,to:"Hemingway");
        Check(reverse.Single().Reverse&&reverse[0].Section.From==0&&reverse[0].Section.To==310,
            "the remaining stretch to the arrival is clipped and directed correctly in reverse even under the same signal owner");
        Check(Required(departure,first,to:"Hemingway").Single().Section.To==290&&Required(first,middle,to:"Hemingway").Single().Section.From==290,
            "departure and intermediate legs use their own ordered slices even when the block unit list is unordered");
        Check(Refused(first,last,to:"Hemingway")&&Refused(last,reverseArrival,to:"Hemingway")&&Refused(last,arrival,shunt:true),
            "a waypoint cannot skip a block signal, target the wrong direction or shunt over a free-line block boundary");
        line.BlockOwner="Alarka";
        Check(Required(departure,first).Single().Section.To==290&&Required(last,arrival,from:"Alarka").Single().Section.From==890,
            "block control at the arrival interlocking does not change the physical line legs");
        line.Enabled=false;
        Check(Refused(last,arrival,from:"Alarka")&&Refused(departure,first),"a disabled block line cannot be used even between two signals on the same interlocking");
        line.Enabled=true;line.BlockUnits.Clear();
        Check(Required(departure,arrival).Single().Section.From==0&&Required(departure,arrival).Single().Section.To==1200,
            "an unpartitioned line still requires the complete configured departure to arrival path");
    }

    public static void Run() {
        BlockLine();
        var approach=WaypointApproach.Create(new[]{Span("a",0,60),Span("b",0,45)},At("a",0),false,At("b",45));
        var start=Signal("start","a",15);
        var first=Signal("first","a",40);
        var platformEnd=Signal("end","b",65);
        var reverse=Signal("reverse","a",25,reverse:true);
        var shunting=Signal("shunting","a",32,MarkerRecord.Shunt);
        var otherBranch=Signal("other","c",80);
        var earlyAuxiliary=Signal("early","a",36,MarkerRecord.Auxiliary);
        var stopAuxiliary=Signal("stop","b",45,MarkerRecord.Auxiliary);
        var physical=new[]{first,platformEnd,reverse,shunting,otherBranch};
        Check(approach.NextSignal(physical)==shunting,"a standalone shunt signal remains a governing start ahead of a later main signal");
        var running=new RouteRun{Stage=RouteStage.Running,Entered=true,HeadKnown=true,TrainCars=new[]{"wagon","engine"},
            Route=new RouteDefinition{ViaSignals=new List<RouteViaSignal>{new RouteViaSignal{Id=shunting.Id}}}};
        Check(WaypointRoutePolicy.HasViaAuthority(running,shunting.Id,new[]{"engine","wagon"},LocalSignalAspect.Shunt)&&
            !WaypointRoutePolicy.HasViaAuthority(running,shunting.Id,new[]{"engine","other"},LocalSignalAspect.Shunt)&&
            !WaypointRoutePolicy.HasViaAuthority(running,shunting.Id,new[]{"engine","wagon"},LocalSignalAspect.Stop),
            "only a proved via release for this exact consist covers a standalone shunt signal");
        running.PassedSignals.Add(shunting.Id);
        Check(!WaypointRoutePolicy.HasViaAuthority(running,shunting.Id,new[]{"engine","wagon"},LocalSignalAspect.Shunt),
            "a released via signal cannot be passed again by the waypoint automaton");
        running.PassedSignals.Clear();running.Entered=false;running.Stage=RouteStage.Set;
        running.TrainCars=null;running.ExpectedWaypointCars=new[]{"engine","wagon"};
        Check(WaypointRoutePolicy.HasViaAuthority(running,shunting.Id,new[]{"wagon","engine"},LocalSignalAspect.Shunt)&&
            !WaypointRoutePolicy.HasViaAuthority(running,shunting.Id,new[]{"foreign","wagon"},LocalSignalAspect.Shunt),
            "an already-cleared via remains covered for its verified consist before entry detection catches up");
        running.Stage=RouteStage.Preparing;
        Check(!WaypointRoutePolicy.HasViaAuthority(running,shunting.Id,new[]{"engine","wagon"},LocalSignalAspect.Shunt),
            "an unconfirmed preparation grants no permission to skip an intermediate signal");
        Check(WaypointRoutePolicy.Destination(approach,start,physical,new[]{earlyAuxiliary,stopAuxiliary},false)==first,
            "waypoint train leg ends at the first same-direction main signal before the stop and does not choose an early auxiliary");
        Check(WaypointRoutePolicy.Destination(approach,start,physical,Array.Empty<MarkerRecord>(),true)==shunting,
            "waypoint shunt leg respects the next same-direction shunt signal");
        Check(WaypointRoutePolicy.Destination(approach,start,new[]{platformEnd,otherBranch},new[]{earlyAuxiliary,stopAuxiliary},false)==stopAuxiliary,
            "waypoint leg can use an auxiliary at the planned stop instead of reserving farther to the platform end");
        Check(WaypointRoutePolicy.Destination(approach,start,new[]{platformEnd,otherBranch},Array.Empty<MarkerRecord>(),false)==platformEnd&&
            WaypointRoutePolicy.Destination(approach,start,new[]{otherBranch},Array.Empty<MarkerRecord>(),false)==null,
            "a waypoint inside a segment can reserve to its protecting signal, but cannot select a destination on an unproven next branch");
        typeof(WaypointApproach).GetMethod("SetContinuation",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Invoke(approach,new object[]{new[]{Span("b",45,100),Span("next",0,30)}});
        var afterStop=Signal("next-main","next",20);
        var beyondStop=Leg(start,afterStop,Span("a",15,60),Span("b",0,100),Span("next",0,20));
        Check(WaypointRoutePolicy.Destination(approach,start,new[]{afterStop,otherBranch},Array.Empty<MarkerRecord>(),false)==afterStop&&
            WaypointRoutePolicy.Matches(approach,start,afterStop,RouteKind.Train,beyondStop)&&
            WaypointRoutePolicy.Destination(approach,start,new[]{Signal("past-proof","next",35)},Array.Empty<MarkerRecord>(),false)==null,
            "an exact path can reach the next signal beyond the waypoint across a proven segment boundary only");
        var tied=Signal("tied","a",40);
        bool ambiguous=false;
        try{WaypointRoutePolicy.Destination(approach,start,new[]{first,tied},Array.Empty<MarkerRecord>(),false);}
        catch(AmbiguousRoutePlanException){ambiguous=true;}
        Check(ambiguous,"coincident distinct governing destination signals remain ambiguous");

        var good=Leg(start,platformEnd,Span("a",15,60),Span("b",0,27),Span("b",27,65));
        Check(WaypointRoutePolicy.Matches(approach,start,platformEnd,RouteKind.Train,good),
            "one fully directed waypoint leg tolerates legitimate splits at protection boundaries");
        Check(!WaypointRoutePolicy.Matches(approach,start,platformEnd,RouteKind.Train,Leg(start,platformEnd,Span("a",15,60),Span("c",0,50),Span("b",0,65)))&&
            !WaypointRoutePolicy.Matches(approach,start,platformEnd,RouteKind.Train,Leg(start,platformEnd,Span("a",15,59),Span("b",0,65)))&&
            !WaypointRoutePolicy.Matches(approach,start,platformEnd,RouteKind.Train,Leg(start,platformEnd,Span("a",15,60),Span("b",0,65,true))),
            "a differing branch, gap or reversed span cannot pass the current-waypoint recheck");
        good.Routes.Add(new RouteDefinition());
        Check(!WaypointRoutePolicy.Matches(approach,start,platformEnd,RouteKind.Train,good),"waypoint preparation cannot admit a multi-leg route chain");

        var backward=WaypointApproach.Create(new[]{Span("r",70,120,true)},At("r",120),true,At("r",70));
        var backStart=Signal("back","r",100,reverse:true);
        var backEnd=Signal("back-end","r",60,reverse:true);
        Check(WaypointRoutePolicy.Destination(backward,backStart,new[]{backEnd},Array.Empty<MarkerRecord>(),false)==backEnd&&
            WaypointRoutePolicy.Matches(backward,backStart,backEnd,RouteKind.Train,Leg(backStart,backEnd,Span("r",60,100,true))),
            "the final-segment reservation and proof work in reverse track direction");
    }
}
