using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.State;
using Track;
using Track.Signals;
using Helpers;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using Railroader.Modding;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime : MonoBehaviour {
        const string WindowId="enusde.signaltechnik.interlocking";
        const string EditorWindowId="enusde.signaltechnik.editor";
        readonly InterlockingEngine engine=new InterlockingEngine();
        RouteRun[] clientRuns=new RouteRun[0];
        long clientConfiguration=-1;
        public IReadOnlyCollection<RouteRun> VisibleRuns=>StateManager.IsHost?engine.Active:Entry.Shared?.LiveFresh==true?clientRuns:new RouteRun[0];
        readonly Dictionary<string,RuntimeSignalProxy> proxies=new Dictionary<string,RuntimeSignalProxy>();
        readonly Dictionary<string,string> markerPositions=new Dictionary<string,string>();
        readonly Dictionary<string,float> lastAi=new Dictionary<string,float>();
        readonly Dictionary<string,string> lastStates=new Dictionary<string,string>();
        SignalHooks hooks;
        StockSignalHooks stockHooks;
        SignalWorldBridge world;
        MarkerRuntime objects;
        InterlockingStore store;
        List<RouteDefinition> routes=new List<RouteDefinition>();
        Dictionary<string,MarkerRecord> inventory=new Dictionary<string,MarkerRecord>();
        WindowManager manager;
        CustomWindowDefinition definition;
        IModLogger log;
        string directory,scope,status=LocalText.Get("Stellwerk wird vorbereitet."),entryId,exitId,routeId;
        bool reduced,shunting,aiEnabled,stopped,loadFailed;
        double nextTick,lastRead=-100;
        double publishedRead=-200;
        bool nativeDirty=true,publishedStale,publishedAi;
        long publishedOperatingVersion=-1,publishedObjectVersion=-1;
        public InterlockingEngine Engine=>engine;
        public void Initialize(MarkerRuntime placements,IModLogger logger,string path) {
            objects=placements;log=logger;directory=Path.Combine(path,"Interlocking");world=new SignalWorldBridge(this,transform);
            hooks=new SignalHooks();try{hooks.Start(this);log.Info("Native Signalanbindung bereit; optionale Schild-Tooltips sind getrennt.");Entry.RecordSignalEvent("signal_bridge_health",new{available=true,version=Entry.Version});}catch(Exception e){loadFailed=true;status=LocalText.Get("Native Signalanbindung nicht verfügbar: ")+e.Message;log.Warning(status);Entry.RecordSignalEvent("signal_bridge_health",new{available=false,error=e.Message});}
            try{switchModels=new NativeSwitchModels();switchModels.Start(ElectricStand);}catch(Exception e){switchModels=null;log.Warning("Elektrische Weichenmodelle nicht verfügbar: "+e.Message);}
            StartAutomation();
            ApplySettings(Entry.SettingsSnapshot());
            stockHooks=new StockSignalHooks();stockHooks.Start(message=>log.Warning(message));
        }
        public void CheckSettings(DiagnosticSettings settings){if(settings.Markers.EnableAiSignals&&!settings.Markers.Visible)throw new InvalidOperationException("Für AE-Anbindung müssen die eigenen Signalmodelle angezeigt werden.");if(aiEnabled&&!settings.Markers.EnableAiSignals&&engine.Active.Count>0)throw new InvalidOperationException("Erst alle aktiven Fahrstraßen sicher auflösen, dann AE-Anbindung abschalten.");}
        public void ApplySettings(DiagnosticSettings settings){CheckSettings(settings);aiEnabled=settings.Markers.EnableAiSignals;}
        public MarkerRecord Object(string id){MarkerRecord row;return id!=null&&inventory.TryGetValue(id,out row)?row:null;}
        internal IEnumerable<MarkerRecord> SignalObjects=>inventory.Values;
        public string Code(string id)=>RouteEndpoint(id)?.Name??"Signal fehlt";
        string SignalCaption(string id)=>RouteEndpoint(id)?.DisplayTitle??"Signal fehlt";
        public bool CanModify(string id)=>!engine.UsesSignal(id)&&!ReferencedAuxiliaryLocked(id)&&!lineDirections.States.Any(s=>s.Held&&(s.Definition.Anchors.ContainsKey(id)||s.Definition.BlockUnits.Any(u=>u.Boundary==id||u.Forward==id||u.Reverse==id)))&&
            desks?.Points.Any(p=>p.Value.BoundaryLinks.Values.Contains(Object(id)?.PhysicalId??id)&&engine.Active.Any(r=>r.Route.Switches.Any(s=>s.Node==p.Key&&!r.ReleasedSwitches.Contains(p.Key))))!=true;
        public bool SwitchAllowed(string node,bool thrown)=>!StateManager.IsHost||!engine.Locked(node,thrown);
        public void AiRead(string id,SignalAspect aspect) {
            if(!Entry.WantsSignalEvents)return;
            float now=Time.realtimeSinceStartup;float previous;
            if(!lastAi.TryGetValue(id,out previous)||now-previous>=5){lastAi[id]=now;Entry.RecordSignalEvent("ai_signal_read",new{id=id,code=Code(id),aspect=aspect.ToString(),source="AutoEngineerPlanner.CallSignalIfNeeded",movement_verified=false});}
        }
        public LocalSignalAspect Aspect(string id) {
            if(!StateManager.IsHost)return Entry.Shared?.ClientAspect(id)??LocalSignalAspect.Stop;
            var operation=OperatingState(id);if(operation==SignalOperatingState.Unbound)return LocalSignalAspect.Invalid;if(operation==SignalOperatingState.OutOfService)return LocalSignalAspect.Kennlicht;
            if(engine.FlankHoldsSignal(id))return LocalSignalAspect.Stop;
            var row=Object(id);if(row==null||!aiEnabled||loadFailed||stopped||backupSuspended||!StateManager.IsHost||Time.realtimeSinceStartup-lastRead>1.5)return LocalSignalAspect.Stop;
            if(row.Kind==MarkerRecord.Distant) {
                var target=Object(row.LinkedSignal);if(target==null||target.Kind!=MarkerRecord.Main)return LocalSignalAspect.Stop;
                return Aspect(target.Id);
            }
            if(row.Kind==MarkerRecord.Shunt){var via=engine.ForViaSignal(id);if(via!=null&&via.PermitsVia(id))return LocalSignalAspect.Shunt;}
            var run=engine.ForSignal(id);if(run==null||!run.Permitted)return LocalSignalAspect.Stop;
            if(!SignalCatalogue.Supports(row,run.Route.Shunting,run.Route.Reduced))return LocalSignalAspect.Stop;
            return run.Route.Shunting?LocalSignalAspect.Shunt:run.Route.Reduced?LocalSignalAspect.Slow:LocalSignalAspect.Clear;
        }
        public SignalAspect NativeAspect(string id) {
            if(IgnoreOwnSignal(id))return SignalAspect.Clear;
            return NativeSignalAuthority.For(Aspect(id),engine.ForViaSignal(id),id);
        }
        void Update() {
            if(stopped||backupSuspended||StateManager.IsUnloading){CancelRouteWork();return;}
            ReadSetupFieldFocus();
            ReadEditorDeleteKey();
            ReadPointPickCancel();
            ReadWorldSignalPickCancel();
            if(panelButtons.ExpireSelection()){status=LocalText.Select("Start selection expired. Select the start signal again.","Startauswahl abgelaufen. Startsignal erneut auswählen.");panelView?.Refresh();}
            double now=Time.realtimeSinceStartup;
            if(scope!=null&&scope==objects.WorldKey&&Entry.Shared?.ConfigurationReady==true)try{ReadAutomationContacts(now);}catch(Exception e){automationStatus="Kontaktprüfung: "+e.Message;}
            if(now<nextTick){TickRouteWork();return;}nextTick=now+.5;
            try {
                var shared=Entry.Shared;if(shared==null||!shared.ConfigurationReady)return;
                ReadSignalInventory();
                if(objects.WorldKey!=null&&scope!=objects.WorldKey) {
                    if(scope!=null&&engine.Active.Count>0){engine.HaltAll();lastRead=-100;status=LocalText.Get("Spielstandzuordnung geändert. Vorherige Verschlüsse zuerst sicher auflösen.");return;}
                    if(scope!=null){engine.HaltAll();RemoveProxies();world.Dispose();world=new SignalWorldBridge(this,transform);}
                    scope=objects.WorldKey;store=new InterlockingStore(directory,scope,shared.Routes,shared.PutRoutes);
                    try{routes=store.Load();status=LocalText.Get("Stellwerk bereit. Alle Signale starten mit Halt.");}catch(Exception e){loadFailed=true;status=LocalText.Get("Stellwerksdatei bleibt erhalten: ")+e.Message;}
                    LoadPanel();
                }
                EnsureWindow();RefreshSwitchModels();
                if(!StateManager.IsHost){UpdateClient(shared);ReadPanel();return;}
                RefreshRegisteredSignals();
                UpdateRecognition();UpdateNetwork();
                engine.SignalPassageMeters=Automation?.SignalPassageMeters??5;
                world.BeginRead();engine.Tick(world);lastRead=now;
                TickAutomation(now);
                ReadNumberPassages(shared);
                if(panel!=null&&PanelVisible)BuildRouteCues();
                RefreshActiveInspector();
                RefreshProxies();
                PublishNativeAspects();
                ObserveStates();
                ReadPanel();
                shared.Publish(this);
                TickRouteWork();
            }catch(Exception e){lastRead=-100;engine.HaltAll();status=LocalText.Get("Stellwerk: ")+e.Message;}
        }
        public void PublishNativeAspects() {
            bool stale=stopped||backupSuspended||StateManager.IsUnloading||!StateManager.IsHost||Time.realtimeSinceStartup-lastRead>1.5;
            long config=Entry.Shared?.ConfigurationVersion??-1,objectsVersion=Entry.Shared?.InstallationVersion??-1;
            if(!nativeDirty&&publishedRead==lastRead&&publishedStale==stale&&publishedAi==aiEnabled&&publishedOperatingVersion==config&&publishedObjectVersion==objectsVersion)return;
            if(OperatingDataReady&&(operatingNativeFault||publishedOperatingVersion!=config||publishedObjectVersion!=objectsVersion)) {
                if(operatingNativeFault&&Time.realtimeSinceStartup<nextOperatingNativeRetry)return;
                try {
                    if(physicalBoundaryVersion!=objectsVersion)ReadSignalInventory();
                    operatingNativeFault=false;RefreshProxies();aheadCache.Clear();operatingNativeError=null;
                }catch(Exception error) {
                    operatingNativeFault=true;nextOperatingNativeRetry=Time.realtimeSinceStartup+.5;lastRead=-100;nativeDirty=true;
                    foreach(var proxy in proxies.Values)if(proxy!=null)try{proxy.Publish(SignalAspect.Stop);}catch{}
                    if(operatingNativeError!=error.Message){operatingNativeError=error.Message;log?.Warning("Operating state native sync: "+error.Message);}
                    return;
                }
            }
            foreach(var pair in proxies)if(pair.Value!=null)pair.Value.Publish(IgnoreOwnSignal(pair.Key)?SignalAspect.Clear:stale?SignalAspect.Stop:NativeAspect(pair.Key));
            publishedOperatingVersion=config;publishedObjectVersion=objectsVersion;
            publishedRead=lastRead;publishedStale=stale;publishedAi=aiEnabled;nativeDirty=false;
        }
        void ReadSignalInventory() {
            var markers=objects.Objects;
            IndexGfmObjects(markers);
            physicalBoundaries=markers.Where(r=>CircuitIdentity.Enabled(r)||r.IsSignal&&r.CircuitNumber>0).ToDictionary(r=>r.Id);
            inventory=SignalCatalogue.Expand(markers).ToDictionary(o=>o.Id);physicalBoundaryVersion=Entry.Shared.InstallationVersion;
        }
        void ObserveStates() {
            if(!Entry.WantsSignalEvents){lastStates.Clear();return;}
            var live=new HashSet<string>();
            foreach(var row in inventory.Values.Where(o=>o.IsSignal)) {
                string key="signal:"+row.Id,value=Aspect(row.Id).ToString();live.Add(key);string previous;
                if(!lastStates.TryGetValue(key,out previous)||previous!=value){lastStates[key]=value;Entry.RecordSignalEvent("local_signal_aspect",new{id=row.Id,code=row.Name,aspect=value,linked_signal=row.LinkedSignal,source="local_interlocking",movement_verified=false});}
            }
            foreach(var run in engine.Active) {
                string key="route:"+run.Route.Id,value=run.Stage+":"+run.Reason+":"+run.ReleasedSections.Count+":"+run.ReleasedSwitches.Count+":"+run.PassedSignals.Count+":"+(run.TrainCars==null?0:run.TrainCars.Length)+":"+run.ReleaseStatus+":"+run.OperatorReleaseConfirmed+":"+string.Join("|",run.PointStates.Select(p=>p.Key+"="+p.Value));live.Add(key);string previous;
                if(!lastStates.TryGetValue(key,out previous)||previous!=value){lastStates[key]=value;Entry.RecordSignalEvent("route_state",new{id=run.Route.Id,code=run.Route.Code,entry=run.Route.Entry,exit=run.Route.Exit,stage=run.Stage.ToString(),reason=run.Reason,released_sections=run.ReleasedSections.ToArray(),released_switches=run.ReleasedSwitches.ToArray(),passed_signals=run.PassedSignals.ToArray(),train_cars=run.TrainCars,tail_progress=run.TailProgress,fault_reason=run.FaultReason,point_states=run.PointStates,release_status=run.ReleaseStatus,operator_confirmed=run.OperatorReleaseConfirmed,incorporated_shunt_routes=run.IncorporatedFrom,protection_wait_remaining=engine.RemainingWait(run)});}
            }
            foreach(string key in lastStates.Keys.Where(k=>!live.Contains(k)).ToArray()){if(key.StartsWith("route:")){string id=key.Substring(6);var route=routes.FirstOrDefault(r=>r.Id==id);Entry.RecordSignalEvent("route_state",new{id=id,entry=route?.Entry,exit=route?.Exit,stage="released"});}lastStates.Remove(key);}
        }
        void RefreshProxies() {
            var graph=Graph.Shared;var wanted=new HashSet<string>();
            if(aiEnabled&&!loadFailed&&StateManager.IsHost&&graph!=null)foreach(var row in inventory.Values.Where(o=>o.IsSignal&&o.Kind!=MarkerRecord.Distant)) {
                Location at;try{at=SignalWorldBridge.Location(row);}catch{continue;}
                wanted.Add(row.Id);RuntimeSignalProxy proxy;
                if(!proxies.TryGetValue(row.Id,out proxy)) {
                    var go=new GameObject("EnusDE native signal "+row.Id);go.SetActive(false);go.transform.SetParent(transform,false);
                    proxy=go.AddComponent<RuntimeSignalProxy>();proxy.ObjectId=row.Id;proxy.id="enusde.signaltechnik.signal."+row.Id;proxies[row.Id]=proxy;nativeDirty=true;go.SetActive(true);
                }
                if(IgnoreOwnSignal(row.Id)){proxy.Bypassed=true;proxy.Publish(SignalAspect.Clear);graph.RemoveNamedLocation(proxy.id);markerPositions.Remove(row.Id);continue;}
                if(proxy.Bypassed){proxy.Bypassed=false;proxy.Publish(SignalAspect.Stop);nativeDirty=true;}
                string signature=SignalWorldBridge.AnchorKey(row),previous;
                if(!markerPositions.TryGetValue(row.Id,out previous)||previous!=signature) {
                    graph.RemoveNamedLocation(proxy.id);graph.AddNamedLocation(proxy.id,at,1,proxy);markerPositions[row.Id]=signature;
                }
                proxy.transform.position=at.GetPosition(PositionAccuracy.High).GameToWorld();
            }
            foreach(string id in proxies.Keys.Where(id=>!wanted.Contains(id)).ToArray())RemoveProxy(id);
        }
        void RemoveProxy(string id){var proxy=proxies[id];try{Graph.Shared?.RemoveNamedLocation(proxy.id);}catch(Exception e){log?.Warning("Eigener Signalmarker: "+e.Message);}finally{if(proxy!=null)UnityEngine.Object.Destroy(proxy.gameObject);proxies.Remove(id);markerPositions.Remove(id);lastAi.Remove(id);}}
        void RemoveProxies(){foreach(string id in proxies.Keys.ToArray())Cleanup(()=>RemoveProxy(id));}
        void RequireEdit() {
            if(stopped||backupSuspended||loadFailed||StateManager.IsUnloading||!StateManager.IsHost||store==null)throw new InvalidOperationException("Stellwerk nur im geladenen Host-/Einzelspielerstand und mit lesbaren Daten bearbeitbar.");
            Entry.Shared?.RequireConstruction();
        }
        void CreateRoute() {
            RequirePanelEdit();
            entryId=panelButtons.Start;
            exitId=panelButtons.End;
            var kind=EffectiveRouteKind(Object(entryId));
            var route=world.Design(Object(entryId),RouteEndpoint(exitId),NextRouteCode(entryId,kind,exit:exitId),kind==RouteKind.Slow,kind==RouteKind.Shunt);
            SaveNewRoute(route);
            panelButtons.Clear();
            ClearRouteProposals();
            Rebuild();
        }

        void SaveNewRoute(RouteDefinition route)=>SaveNewRoutes(new[]{route});

        void SaveNewRoutes(IReadOnlyList<RouteDefinition> additions) {
            RequirePanelEdit();
            if(additions.Count==0)throw new InvalidOperationException("Höchstens 128 Fahrstraßen pro Spielstand.");
            var accepted=additions.Select(r=>routes.FirstOrDefault(old=>RouteIdentity.DefinitionKey(old)==RouteIdentity.DefinitionKey(r))??r).ToList();
            var next=routes.Concat(accepted.Where(r=>!routes.Any(old=>old.Id==r.Id))).GroupBy(r=>r.Id).Select(g=>g.First()).ToList();
            if(next.Count>128)throw new InvalidOperationException("Höchstens 128 Fahrstraßen pro Spielstand.");
            if(next.Count!=routes.Count)store.Save(next);
            routes=next;
            routeId=accepted[0].Id;
            status=accepted.Count==1?accepted[0].Code+" verfügbar.":accepted.Count+" Fahrstraßen verfügbar.";
        }
        void SetRoute() {
            RequireDispatcher();var route=routes.FirstOrDefault(r=>r.Id==routeId)??throw new InvalidOperationException("Fahrstraße auswählen.");
            SetRoutePlan(new RouteChainPlan{Routes=new List<RouteDefinition>{route}});
        }
        void CancelRoute() {
            RequireDispatcher();world.BeginRead();engine.CancelChain(routeId,world);status=LocalText.Get("Fahrstraße / Folge sicher aufgelöst.");Entry.RecordSignalEvent("route_cancel",new{id=routeId});
        }
        void DeleteRoute() {
            RequirePanelEdit();if(engine.Active.Any(r=>r.Route.Id==routeId))throw new InvalidOperationException("Aktive Fahrstraße zuerst sicher auflösen.");
            var next=routes.Where(r=>r.Id!=routeId).ToList();store.Save(next);routes=next;routeId=routes.FirstOrDefault()?.Id;status=LocalText.Get("Fahrstraßendefinition entfernt.");Rebuild();
        }
        void Act(Action action){bool fromUi=WorldPointerInput.OverUi();workspaceActionError=null;try{action();}catch(Exception e){workspaceActionError=status=e.Message;if(e is RouteConflictException conflict)OpenBlockingRoutes(conflict);try{log?.Warning("Stellwerkaktion: "+e.Message);Entry.RecordSignalEvent("interlocking_action_failed",new{error=e.Message,entry=entryId,exit=exitId,route=routeId});}catch{}}finally{if(fromUi)worldSelectionInput.Complete();nativeDirty=true;PublishNativeAspects();}}
        CustomWindowDefinition editorDefinition;
        void EnsureWindow() {
            if(manager==null)manager=WindowManager.Shared;
            if(manager==null)return;
            if(definition==null||!ReferenceEquals(manager.GetDefinition(WindowId),definition))
                definition=RegisterWorkspace(WindowId,WindowText("EnusDE · Dispatcher","EnusDE · Fahrdienstleiter"),BuildDispatcherWindow);
            if(StateManager.IsHost&&(editorDefinition==null||!ReferenceEquals(manager.GetDefinition(EditorWindowId),editorDefinition)))
                editorDefinition=RegisterWorkspace(EditorWindowId,WindowText("EnusDE · Interlocking editor","EnusDE · Stellwerkseditor"),BuildEditorWindow);
        }
        CustomWindowDefinition RegisterWorkspace(string id,string title,Action<UIPanelBuilder> builder) {
            var next=new CustomWindowDefinition{Identifier=id,Title=title,BuilderAction=builder,Config=WindowConfig.Resizable(new Vector2Int(1280,850),new Vector2Int(800,550),new Vector2Int(3840,2160),Window.Position.CenterRight)};
            manager.RegisterCustomWindow(next);
            return next;
        }
        void Rebuild(bool rememberScroll=true){if(rememberScroll)RememberPanelScroll();if(manager!=null&&!StateManager.IsUnloading){CustomWindowHandle.Create(ActiveWindowId)?.SetNeedsRebuild();RebuildEditorToolsWindow();}}
        public void Open(bool choose=true)=>ShowWorkspace(false,choose?4:0,choose);
        public void Shutdown() {
            stockHooks?.Dispose();Cleanup(()=>automationHooks?.Dispose());Cleanup(()=>contactScene?.Dispose());Cleanup(RestoreSwitchModels);
            StopNetwork();
            if(stopped)return;stopped=true;CancelRouteWork();engine.HaltAll();lastRead=-100;Cleanup(PublishNativeAspects);Cleanup(RemoveProxies);Cleanup(()=>hooks?.Dispose());Cleanup(()=>world?.Dispose());Cleanup(()=>panelWorld?.Dispose());Cleanup(RemoveWindow);
            manager=null;definition=null;editorDefinition=null;inventory.Clear();lastStates.Clear();lastAi.Clear();routes.Clear();
        }
        void Cleanup(Action action){try{action();}catch(Exception e){log?.Warning("Stellwerk-Bereinigung: "+e.Message);}}
        void RemoveWindow() {
            Cleanup(()=>UnregisterWorkspace(NumberOverviewPlacementId,numberOverviewPlacementDefinition));numberOverviewPlacementDefinition=null;numberOverviewProposal=null;numberOverviewOriginal=null;
            Cleanup(()=>UnregisterWorkspace(BlockPlacementWindowId,blockPlacementDefinition));blockPlacementDefinition=null;blockPlacementBuild=null;
            Cleanup(()=>UnregisterWorkspace(DiagramPickId,diagramPickDefinition));diagramPickDefinition=null;diagramPickAccept=null;diagramPickGeneration++;
            Cleanup(()=>UnregisterWorkspace(RouteChoiceWindowId,routeChoiceDefinition));routeChoiceDefinition=null;
            masterLayoutPopup?.Dismiss();masterLayoutPopup=null;
            ReleaseWorkspaceCanvas();
            Cleanup(()=>UnregisterWorkspace(WindowId,definition));
            Cleanup(()=>UnregisterWorkspace(EditorWindowId,editorDefinition));
            Cleanup(()=>UnregisterWorkspace(CarInfoWindowId,carInfoDefinition));carInfoDefinition=null;informationCar=null;
            Cleanup(()=>UnregisterWorkspace(EditorToolsWindowId,editorToolsDefinition));editorToolsDefinition=null;
            Cleanup(()=>UnregisterWorkspace(RecognitionPreviewWindowId,recognitionPreviewDefinition));recognitionPreviewDefinition=null;
            Cleanup(()=>UnregisterWorkspace(MasterLayoutWindowId,masterLayoutDefinition));masterLayoutDefinition=null;
        }
        void UnregisterWorkspace(string id,CustomWindowDefinition owned) {
            if(manager==null||owned==null||!ReferenceEquals(manager.GetDefinition(id),owned))return;
            try{manager.CloseWindow(id);}catch{}
            var instance=manager.GetCustomWindow(id);owned.BuilderAction=null;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            (typeof(WindowManager).GetField("_customDefinitions",flags)?.GetValue(manager) as IDictionary)?.Remove(id);
            (typeof(WindowManager).GetField("_customWindows",flags)?.GetValue(manager) as IDictionary)?.Remove(id);
            manager.UnregisterActiveWindow(id);
            if(instance!=null)UnityEngine.Object.Destroy(instance.gameObject);
        }
        void OnDestroy(){Shutdown();}
    }
}
