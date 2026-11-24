using System;
using System.Reflection;
using EnusDE.Signaltechnik;
using Model;
using Model.AI;
using Track;
using Track.Search;

static class WaypointApproachTests {
    static void Check(bool result,string text){if(!result)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static MarkerAnchor At(string segment,double meters)=>new MarkerAnchor{Segment=segment,Meters=meters};
    static MarkerRecord Signal(string id,string segment,double meters,bool reverse=false)=>new MarkerRecord{Id=id,Anchor=At(segment,meters),FacingReverse=reverse};
    static DirectedRouteSection Piece(string segment,double from,double to,bool reverse=false)=>new DirectedRouteSection{Section=new TrackSection{Segment=segment,From=from,To=to},Reverse=reverse};

    public static void Run() {
        var plan=new[]{Piece("entry",10,100),Piece("platform",0,120),Piece("departure",30,170,true)};
        var waypoint=At("departure",30);
        var approach=WaypointApproach.Create(plan,At("entry",60),false,waypoint);
        Check(approach!=null&&approach.Distance==300&&approach.TryLocate("departure",130,out double progress,out bool reverse)&&progress==200&&reverse,
            "waypoint scope clips to the leading wheel position and accumulates signed segment distances including reversed track coordinates");
        var near=Signal("first","entry",82);
        var far=Signal("second","platform",40);
        var unrelated=new[]{Signal("behind","entry",50),Signal("opposite","entry",70,true),Signal("neighbor","parallel",70),far,near};
        Check(approach.NextSignal(unrelated)==near&&approach.TryLocateAhead(near,out double toFirst)&&toFirst==22,
            "waypoint automation selects the nearest governing face and excludes signals already passed, facing away, or on neighboring track");
        var after=WaypointApproach.Create(plan,At("entry",85),false,waypoint);
        Check(after.NextSignal(unrelated)==far&&after.NextSignal(unrelated,10)==null,
            "waypoint automation can proceed to the next face only after passing the first and coming within approach distance");
        var standing=WaypointApproach.Create(plan,At("entry",82),false,waypoint);
        Check(standing.TryLocateAhead(near,out double atStop)&&atStop==0,
            "a stationary train exactly at the governing signal can still request its route");
        var approachReverse=WaypointApproach.Create(plan,At("departure",100),true,waypoint);
        var aheadReverse=Signal("reverse","departure",60,true);
        Check(approachReverse.NextSignal(new[]{Signal("past","departure",110,true),Signal("waypoint","departure",30,true),Signal("beyond","departure",25,true),aheadReverse})==aheadReverse,
            "reverse-direction approach stops searching at the exact waypoint and treats traveled rail as behind the train");
        Check(WaypointApproach.Create(plan,At("entry",60),true,waypoint)==null&&WaypointApproach.Create(plan,At("parallel",60),false,waypoint)==null&&
            WaypointApproach.Create(plan,At("entry",60),false,At("departure",29))==null&&WaypointApproach.Create(plan,At("departure",30),true,waypoint)==null,
            "off-path or wrong-direction leading positions, incomplete native routes and an already reached waypoint grant no approach");
        var repeated=new[]{Piece("entry",10,100),Piece("platform",0,120),Piece("entry",0,10)};
        Check(WaypointApproach.Create(repeated,At("entry",60),false,At("entry",10))==null,
            "repeated segment loops are refused instead of choosing one occurrence of the signal");
        bool ambiguous=false;
        try{approach.NextSignal(new[]{near,Signal("same-position","entry",82),far});}catch(InvalidOperationException){ambiguous=true;}
        Check(ambiguous,"two coincident governing faces stop the request instead of skipping to a farther signal");

        var native=typeof(AutoEngineerPlanner);
        const BindingFlags privateInstance=BindingFlags.NonPublic|BindingFlags.Instance;
        var start=native.GetMethod("RouteStartLocation",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(BaseLocomotive),typeof(bool),typeof(ConsistEnd).MakeByRefType()},null);
        Check(start?.ReturnType==typeof(Location)&&native.GetField("_lastRouteWaypoint",privateInstance)?.FieldType==typeof(OrderWaypoint)&&
            typeof(RouteSearch.Step).GetField("Location")?.FieldType==typeof(Location)&&typeof(RouteSearch.Step).GetField("LeadingEnd")?.FieldType==typeof(ConsistEnd),
            "installed game exposes the native leading-end route origin, planned waypoint freshness and directed route step contracts");
    }
}
