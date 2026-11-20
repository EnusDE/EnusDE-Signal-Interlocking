using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly FrameWorkQueue routeWork=new FrameWorkQueue();
        string routeWorkProgress="";
        bool RouteWorkAvailable=>!stopped&&!backupSuspended&&!StateManager.IsUnloading&&StateManager.IsHost&&Entry.Shared?.ConfigurationReady==true&&scope!=null&&scope==objects.WorldKey;
        static string RouteWorkKey(string start,string end,RouteKind kind)=>start+"/"+end+"/"+(int)kind;
        void CancelRouteWork(){routeWork.Clear();preparedAutomaticRoutes.Clear();routeWorkProgress="";}
        void TickRouteWork() {
            if(!RouteWorkAvailable){CancelRouteWork();return;}
            int before=routeWork.Count;routeWork.Advance();
            if(before!=routeWork.Count){RefreshWorkspaceFields();panelView?.Refresh();}
        }
        void BuildRouteWorkControls(UIPanelBuilder c) {
            if(routeWork.Count==0)return;
            c.AddSection(WindowText("Queued route preparation","Fahrstraßen-Vorbereitung"));
            c.AddField(WindowText("Pending requests","Ausstehende Anforderungen"),()=>routeWork.Count.ToString(),UIPanelBuilder.Frequency.Periodic);
            c.AddField(WindowText("Progress","Fortschritt"),()=>routeWorkProgress,UIPanelBuilder.Frequency.Periodic);
            c.AddButton(WindowText("Cancel pending preparations","Ausstehende Vorbereitungen abbrechen"),()=>{foreach(var request in automaticRequests.Where(r=>preparedAutomaticRoutes.ContainsKey(r.Id)).ToArray())WithdrawAutomaticRequest(request,WindowText("Preparation cancelled.","Vorbereitung abgebrochen."));CancelRouteWork();status=WindowText("Pending preparations cancelled. Existing route locks remain supervised.","Ausstehende Vorbereitungen abgebrochen. Bestehende Fahrstraßenverschlüsse bleiben überwacht.");RefreshWorkspaceFields();});
        }
        void QueueDestination(string start,string destination,RouteKind kind) {
            RequireDispatcher();string selectedWorld=scope,selectedDesk=deskId;
            ResolveLineDestination(start,destination,kind);
            string sourceAnchor=SignalWorldBridge.AnchorKey(Object(start)),targetAnchor=SignalWorldBridge.AnchorKey(RouteEndpoint(destination));
            string key=RouteWorkKey(start,destination,kind),label=SignalCaption(start)+" → "+SignalCaption(destination);
            bool current()=>RouteWorkAvailable&&scope==selectedWorld&&deskId==selectedDesk&&!editingWorkspace&&
                Object(start)?.Anchor!=null&&RouteEndpoint(destination)?.Anchor!=null&&SignalWorldBridge.AnchorKey(Object(start))==sourceAnchor&&SignalWorldBridge.AnchorKey(RouteEndpoint(destination))==targetAnchor;
            bool added=routeWork.Enqueue(key,()=>PrepareDestination(start,destination,kind,label).GetEnumerator(),current,error=>{
                RoutePreparationFailed(label,error);Entry.RecordSignalEvent("route_queue_failed",new{start,destination,error=error.Message});
            });
            status=label+" · "+(added?WindowText("Queued for preparation","Zur Vorbereitung eingereiht"):WindowText("Already queued","Bereits eingereiht"));
            if(added)Entry.RecordSignalEvent("route_queued",new{start,destination,kind=kind.ToString(),pending=routeWork.Count});
            RefreshWorkspaceFields();
        }
        void QueueSavedPlan(RouteChainPlan plan) {
            if(plan?.Routes==null||plan.Routes.Count==0)throw new InvalidOperationException(WindowText("Select a route first.","Zuerst eine Fahrstraße wählen."));
            string selectedWorld=scope,selectedDesk=deskId;
            var definitions=plan.Routes.ToArray();string label=definitions[0].Code+" → "+SignalCaption(definitions.Last().Exit);
            string key="saved/"+string.Join("/",definitions.Select(r=>r.Id));
            IEnumerable<bool> Prepare() {
                while(automaticChoices.Count>0){routeWorkProgress=WindowText("Waiting for route choice","Wartet auf Fahrwegauswahl");yield return false;}
                long version=Entry.Shared.ConfigurationVersion;
                var copy=new RouteChainPlan{EndReason=plan.EndReason};
                foreach(var definition in definitions) {
                    if(version!=Entry.Shared.ConfigurationVersion||!world.PathValid(definition))throw new InvalidOperationException(WindowText("Saved route changed. Select it again.","Gespeicherter Fahrweg geändert. Erneut auswählen."));
                    routeWorkProgress=label+" · "+(copy.Routes.Count+1)+" / "+definitions.Length;copy.Routes.Add(definition.Clone());yield return false;
                }
                if(version!=Entry.Shared.ConfigurationVersion)throw new InvalidOperationException(WindowText("Saved route changed. Select it again.","Gespeicherter Fahrweg geändert. Erneut auswählen."));
                ApplyRoutePlan(copy);
            }
            bool added=routeWork.Enqueue(key,()=>Prepare().GetEnumerator(),()=>RouteWorkAvailable&&scope==selectedWorld&&deskId==selectedDesk&&!editingWorkspace,error=>{RoutePreparationFailed(label,error);Entry.RecordSignalEvent("route_queue_failed",new{key,error=error.Message});});
            status=label+" · "+(added?WindowText("Queued for preparation","Zur Vorbereitung eingereiht"):WindowText("Already queued","Bereits eingereiht"));RefreshWorkspaceFields();
        }
        void RoutePreparationFailed(string label,Exception error){workspaceActionError=status=label+": "+LocalText.Get(error.Message);if(error is RouteConflictException conflict)OpenBlockingRoutes(conflict);}
        IEnumerable<bool> PrepareDestination(string start,string destination,RouteKind kind,string label) {
            while(automaticChoices.Count>0){routeWorkProgress=WindowText("Waiting for route choice","Wartet auf Fahrwegauswahl");yield return false;}
            var plans=new List<RouteChainPlan>();
            foreach(bool step in PrepareRoutePlans(start,destination,kind,plans,phase=>routeWorkProgress=label+" · "+phase))yield return step;
            yield return false;
            SelectAutomaticPlans(plans,destination,start);
        }
        // Unity/native reads and protection design stay on the main thread. Only isolated DTOs enter workers.
        IEnumerable<bool> PrepareRoutePlans(string start,string destination,RouteKind kind,List<RouteChainPlan> result,Action<string> progress,bool discoverOnly=false,AutomationContact contact=null) {
            string lineTarget=BoundLineTarget(destination)!=null?destination:null;
            if(lineTarget!=null)destination=ResolveLineDestination(start,destination,kind);
            long config=Entry.Shared.ConfigurationVersion,installation=Entry.Shared.InstallationVersion;bool onDemand=OnDemandRoutes;
            void Current(){if(!RouteWorkAvailable||config!=Entry.Shared.ConfigurationVersion||installation!=Entry.Shared.InstallationVersion||onDemand!=OnDemandRoutes)throw new InvalidOperationException(WindowText("Installation changed during route preparation. Request the route again.","Anlage während der Fahrstraßenvorbereitung geändert. Fahrstraße erneut anfordern."));}
            using(var cancellation=new CancellationTokenSource()) {
                try {
                    if(!onDemand&&!discoverOnly) {
                        progress(WindowText("Checking saved routes","Gespeicherte Fahrwege prüfen"));var valid=new List<RouteDefinition>();
                        foreach(var route in routes.ToArray()){Current();if(world.PathValid(route))valid.Add(route.Clone());yield return true;}
                        var saved=Task.Run(()=>RouteChains.Candidates(valid,start,destination,kind==RouteKind.Shunt,kind==RouteKind.Slow),cancellation.Token);
                        ObserveRouteTask(saved);
                        while(!saved.IsCompleted){Current();yield return false;}
                        var found=saved.GetAwaiter().GetResult();Current();
                        if(found.Count>0){if(lineTarget!=null)foreach(var plan in found)LineTargetBindings.Tag(plan,BoundLineTarget(lineTarget));result.AddRange(found);yield break;}
                    }
                    progress(WindowText("Reading track snapshot","Gleisnetz-Momentaufnahme lesen"));var graph=new NetworkInput();
                    foreach(bool step in world.CaptureRouteGraph(graph)){Current();yield return step;}
                    var signals=new List<MarkerRecord>();
                    foreach(var signal in OperationalSignalObjects){Current();signals.Add(signal.Clone());yield return true;}
                    var source=Object(start)?.Clone();var target=RouteEndpoint(destination)?.Clone();
                    var contactSignals=new List<MarkerRecord>();
                    if(contact!=null)foreach(var signal in SignalObjects){Current();contactSignals.Add(signal.Clone());yield return true;}
                    progress(WindowText("Searching paths","Fahrwege suchen"));
                    string contactError=WindowText("Contact configuration skips a main signal. Correct its start signal.","Kontaktzuordnung überspringt ein Hauptsignal. Startsignal korrigieren.");
                    var search=Task.Run(()=>{
                        if(contact!=null&&!ContactSignalPolicy.IsNextMain(graph,contactSignals,contact,start))throw new InvalidOperationException(contactError);
                        return new {Paths=AutomaticRoutePlanner.Find(graph,signals,source,target,kind,cancellation.Token),Signature=NetworkRecognition.Signature(graph)};
                    },cancellation.Token);
                    ObserveRouteTask(search);
                    while(!search.IsCompleted){Current();yield return false;}
                    var searched=search.GetAwaiter().GetResult();Current();
                    var ends=new HashSet<string>(signals.Where(s=>s.IsSignal).Select(s=>s.Id)){destination};
                    var design=world.PrepareProposalDesign(ends,graph);yield return false;
                    var codes=routes.Select(r=>r.Code).ToList();
                    var cache=new List<Tuple<RouteProposal,RouteDefinition[]>>();InvalidOperationException unavailable=null;
                    int designed=0,total=searched.Paths.Sum(p=>p.Count);
                    foreach(var path in searched.Paths) {
                        var variants=new List<List<RouteDefinition>>{new List<RouteDefinition>()};
                        foreach(var proposal in path) {
                            Current();progress(WindowText("Preparing protection ","Schutzbereiche vorbereiten ")+(++designed)+" / "+total);
                            var cached=cache.FirstOrDefault(p=>p.Item1.SamePath(proposal));var known=cached?.Item2;
                            if(known==null) {
                                known=onDemand?Array.Empty<RouteDefinition>():RouteIdentity.Unique(routes.Where(r=>RouteIdentity.Matches(r,proposal)&&world.PathValid(r)));
                                if(known.Length==0) {
                                    string code=NextRouteCode(proposal.Entry,proposal.Kind,codes,proposal.Exit);codes.Add(code);
                                    try{var calculated=design(proposal,code);known=new[]{onDemand?RouteRequestPolicy.ForRequest(calculated):calculated};}
                                    catch(InvalidOperationException error){unavailable=error;variants.Clear();}
                                }
                                if(variants.Count>0)cache.Add(Tuple.Create(proposal,known));
                            }
                            // A complete native protection calculation is the largest non-preemptible unit.
                            yield return false;Current();if(variants.Count==0)break;
                            var next=new List<List<RouteDefinition>>();
                            foreach(var prefix in variants)foreach(var definition in known) {
                                if(next.Count+result.Count>=32)throw new InvalidOperationException(WindowText("Too many distinct paths. Choose a nearer destination.","Zu viele unterschiedliche Fahrwege. Näheres Ziel wählen."));
                                next.Add(prefix.Concat(new[]{definition}).ToList());yield return true;
                            }
                            variants=next;
                        }
                        result.AddRange(variants.Select(v=>new RouteChainPlan{Routes=v,EndReason=WindowText("Route prepared in the work queue.","Fahrweg in der Warteschlange vorbereitet.")}));
                    }
                    if(result.Count==0&&unavailable!=null)throw new InvalidOperationException(WindowText("No fully verifiable route: ","Kein vollständig prüfbarer Fahrweg: ")+unavailable.Message,unavailable);
                    if(lineTarget!=null)foreach(var plan in result)LineTargetBindings.Tag(plan,BoundLineTarget(lineTarget));
                    // Detect native topology edits even when they did not change a mod configuration version.
                    progress(WindowText("Rechecking track topology","Gleisnetz erneut abgleichen"));var currentGraph=new NetworkInput();
                    foreach(bool step in world.CaptureRouteGraph(currentGraph)){Current();yield return step;}
                    var signature=Task.Run(()=>NetworkRecognition.Signature(currentGraph),cancellation.Token);ObserveRouteTask(signature);
                    while(!signature.IsCompleted){Current();yield return false;}
                    if(signature.GetAwaiter().GetResult()!=searched.Signature)throw new InvalidOperationException(WindowText("Track topology changed during preparation. Request the route again.","Gleisnetz während der Vorbereitung geändert. Fahrstraße erneut anfordern."));
                    foreach(var route in result.SelectMany(p=>p.Routes).Distinct()){Current();if(!world.PathValid(route))throw new InvalidOperationException(WindowText("Prepared route is no longer valid.","Vorbereiteter Fahrweg ist nicht mehr gültig."));yield return false;}
                    Current();
                }finally{cancellation.Cancel();}
            }
        }
        static void ObserveRouteTask(Task task){task.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);}
    }
}
