using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnusDE.Signaltechnik;

static class RouteQueueTests {
    static void Check(bool value,string name){if(!value)throw new Exception("FAIL: "+name);System.Console.WriteLine("PASS: "+name);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static IEnumerable<bool> RunSteps(List<string> events,string name) {events.Add(name+"/prepare");yield return false;events.Add(name+"/commit");}
    sealed class World:IInterlockingWorld {
        public string Occupied;
        public bool PathValid(RouteDefinition r)=>true;
        public TrackOccupancy Occupancy(TrackSection s)=>s.Segment==Occupied?TrackOccupancy.Occupied:TrackOccupancy.Clear;
        public bool SwitchPosition(string id,out bool thrown){thrown=false;return true;}
        public bool SetSwitch(string id,bool thrown)=>throw new Exception("preparation must not move points");
    }
    static RouteDefinition Route(string entry,string exit,string segment)=>new RouteDefinition{Code=entry+exit,Entry=entry,Exit=exit,Sections=new(){new TrackSection{Segment=segment,From=0,To=10}},Approach=new(){new TrackSection{Segment="approach/"+entry,From=0,To=10}}};
    public static void Run() {
        var events=new List<string>();var queue=new FrameWorkQueue();int errors=0;
        queue.Enqueue("A",()=>RunSteps(events,"A").GetEnumerator(),()=>true,_=>errors++);
        queue.Enqueue("B",()=>RunSteps(events,"B").GetEnumerator(),()=>true,_=>errors++);
        Check(events.Count==0&&queue.Count==2,"queuing routes performs no eager search, switch movement or admission");
        Check(!queue.Enqueue("A",()=>throw new Exception("duplicate executed"),()=>true,_=>{}),"duplicate queued endpoints do not launch another preparation");
        queue.Advance();Check(events.SequenceEqual(new[]{"A/prepare"})&&queue.Count==2,"a waiting/yielding first request preserves FIFO and leaves later requests untouched");
        queue.Advance();Check(events.SequenceEqual(new[]{"A/prepare","A/commit"})&&queue.Count==1,"finishing one request does not start the next in the same frame");
        queue.Advance();queue.Advance();Check(events.Last()=="B/commit"&&queue.Count==0&&errors==0,"later requests resume in order on subsequent frames");
        double clock=0;int iterations=0;queue=new FrameWorkQueue(()=>clock);
        IEnumerable<bool> Timed(){while(iterations<10){iterations++;clock+=.001;yield return true;}}
        queue.Enqueue("timed",()=>Timed().GetEnumerator(),()=>true,_=>errors++);queue.Advance(.002,512);
        Check(iterations==2&&queue.Count==1,"elapsed-time budget stops a batch before processing all pending steps");
        queue.Advance(100,3);Check(iterations==5,"step-count cap also bounds preparation when the time budget is generous");
        queue.Clear();bool disposed=false,valid=true;int committed=0;
        IEnumerable<bool> Cancellable(){try{yield return false;committed++;}finally{disposed=true;}}
        queue.Enqueue("stale",()=>Cancellable().GetEnumerator(),()=>valid,e=>{if(e is OperationCanceledException)errors++;});queue.Advance();valid=false;queue.Advance();
        Check(disposed&&committed==0&&queue.Count==0&&errors==1,"changed world/context cancels a started request and disposes its resources before admission");
        disposed=false;queue.Enqueue("cancel",()=>Cancellable().GetEnumerator(),()=>true,_=>errors++);queue.Advance();queue.Clear();
        Check(disposed&&committed==0&&queue.Count==0,"STOP/cancel disposal prevents a deferred route from being submitted later");
        IEnumerable<bool> Broken(){yield return false;throw new InvalidOperationException("bad topology");}
        queue.Enqueue("bad",()=>Broken().GetEnumerator(),()=>true,_=>errors++);queue.Enqueue("good",()=>RunSteps(events,"good").GetEnumerator(),()=>true,_=>errors++);
        queue.Advance();queue.Advance();queue.Advance();queue.Advance();Check(queue.Count==0&&errors==2&&events.Last()=="good/commit","failed preparation is removed and does not stall independent queued requests");
        for(int i=0;i<32;i++)queue.Enqueue(i.ToString(),()=>Cancellable().GetEnumerator(),()=>true,_=>{});
        Check(Reject(()=>queue.Enqueue("overflow",()=>Cancellable().GetEnumerator(),()=>true,_=>{}))&&queue.Count==32,"bounded queue rejects flooding instead of allocating unlimited route jobs");queue.Clear();
        var world=new World();var engine=new InterlockingEngine();var existing=Route("old","end","held");engine.Request(existing,world);
        var first=Route("A","B","first");var second=Route("B","C","second");bool rejected=false;
        IEnumerable<bool> Chain(){yield return false;yield return false;engine.RequestChain(new[]{first,second},world);}
        queue.Enqueue("chain",()=>Chain().GetEnumerator(),()=>true,_=>rejected=true);queue.Advance();
        Check(engine.Active.Count==1&&engine.Active.Single().Route.Id==existing.Id,"preparing part of a multi-signal request creates no partial engine reservations");
        queue.Advance();world.Occupied="second";queue.Advance();
        Check(rejected&&engine.Active.Count==1&&engine.Active.Single().Route.Id==existing.Id&&engine.LastRequest?.Accepted==false,"new occupation during queueing is checked at atomic admission and rolls back the entire new chain while retaining old locks");
        queue.Enqueue("cancelled",()=>Chain().GetEnumerator(),()=>true,_=>{});queue.Advance();queue.Clear();
        Check(engine.Active.Single().Route.Id==existing.Id,"cancelling preparation does not release an existing route or retained lock");
        var graph=new NetworkInput{Nodes=new(){new PlanNode{Id="left"},new PlanNode{Id="right"}},Edges=new(){new PlanEdge{Id="line",A="left",B="right",Length=1000}}};
        var signals=Enumerable.Range(0,12).Select(i=>new MarkerRecord{Id="S"+i,Name="S"+i,Kind=MarkerRecord.Main,AllowHp1=true,Anchor=new MarkerAnchor{Segment="line",Meters=10+i*80}}).ToArray();
        var expected=AutomaticRoutePlanner.Find(graph,signals,signals[0],signals[11],RouteKind.Train);
        var actual=Task.Run(()=>AutomaticRoutePlanner.Find(graph,signals.Select(s=>s.Clone()).ToArray(),signals[0].Clone(),signals[11].Clone(),RouteKind.Train)).GetAwaiter().GetResult();
        Check(expected.Single().Count==11&&actual.Single().Select(p=>p.Entry+"/"+p.Exit).SequenceEqual(expected.Single().Select(p=>p.Entry+"/"+p.Exit)),"isolated background search preserves every intermediate main signal in an eleven-part train route");
        using(var cancellation=new CancellationTokenSource()) {
            cancellation.Cancel();
            Check(Reject(()=>AutomaticRoutePlanner.Find(graph,signals,signals[0],signals[11],RouteKind.Train,cancellation.Token)),"cancelled background route searches stop before producing a route");
            Check(Reject(()=>new RouteDiscovery(graph,signals).Find(signals[0],RouteKind.Train,new HashSet<string>{signals[11].Id},cancellation:cancellation.Token)),"cancellation also reaches the individual track-walk search");
        }
    }
}
