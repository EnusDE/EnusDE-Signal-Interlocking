using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Game.State;
using UI.Builder;
using UI.Windows;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const string WorldSignalRequestWindowId="enusde.signaltechnik.world-signal-request";
        readonly FrameWorkQueue worldSignalSearch=new FrameWorkQueue();
        CustomWindowDefinition worldSignalRequestDefinition;
        WorldSignalRequest worldSignalRequest;

        sealed class WorldSignalRequest {
            public string World,Physical,Signal,Label,Destination,Error,SourceAnchor,SearchError;
            public long Configuration;
            public int Side;
            public Dictionary<string,string> Reachable;
            public RouteKind? Movement;
        }

        internal bool OpenWorldSignalRequest(MarkerRecord physical,int side) {
            if(stopped||StateManager.IsUnloading||physical==null||physical.Planned||physical.Kind!=MarkerRecord.Main&&physical.Kind!=MarkerRecord.Shunt)return false;
            EnsureWindow();if(manager==null)return false;
            string id=WorldSignalPickFace.Resolve(physical,side);
            worldSignalSearch.Clear();
            worldSignalRequest=new WorldSignalRequest {
                World=scope,Physical=physical.Id,Signal=id,Side=side,Label=physical.DisplayTitle,
                Configuration=Entry.Shared?.ConfigurationVersion??-1
            };
            if(worldSignalRequestDefinition==null||!ReferenceEquals(manager.GetDefinition(WorldSignalRequestWindowId),worldSignalRequestDefinition)) {
                worldSignalRequestDefinition=new CustomWindowDefinition {
                    Identifier=WorldSignalRequestWindowId,Title=WindowText("Request a route","Fahrstraße anfordern"),
                    BuilderAction=BuildWorldSignalRequest,
                    Config=WindowConfig.Resizable(new Vector2Int(440,440),new Vector2Int(390,360),new Vector2Int(800,750),Window.Position.CenterRight)
                };
                manager.RegisterCustomWindow(worldSignalRequestDefinition);
            }
            var handle=CustomWindowHandle.Create(WorldSignalRequestWindowId);
            handle?.Show();handle?.SetNeedsRebuild();
            return true;
        }

        void CloseWorldSignalRequest() {
            worldSignalSearch.Clear();
            worldSignalRequest=null;
            if(manager?.GetCustomWindow(WorldSignalRequestWindowId)!=null)manager.CloseWindow(WorldSignalRequestWindowId);
        }

        bool WorldSignalRequestCurrent(WorldSignalRequest request) =>
            ReferenceEquals(worldSignalRequest,request)&&request.World!=null&&scope==request.World&&scope==objects.WorldKey&&
            scope==Entry.Shared?.WorldKey&&request.Configuration==Entry.Shared?.ConfigurationVersion;

        void TickWorldSignalSearch() {
            if(worldSignalSearch.Count==0)return;
            var handle=CustomWindowHandle.Create(WorldSignalRequestWindowId);
            if(handle?.IsOpen()!=true){worldSignalSearch.Clear();worldSignalRequest=null;return;}
            if(worldSignalRequest==null||!WorldSignalRequestCurrent(worldSignalRequest)||!RouteWorkAvailable) {
                worldSignalSearch.Clear();
                handle.SetNeedsRebuild();
                return;
            }
            worldSignalSearch.Advance(.001,256);
        }

        void BeginWorldSignalSearch(WorldSignalRequest request,MarkerRecord source,DeskDefinition owner,RouteKind trainKind,bool train,bool shunt) {
            if(worldSignalSearch.Count>0||request.SearchError!=null)return;
            var trainTargets=train?WorldSignalDestinations(source,owner,trainKind):Enumerable.Empty<MarkerRecord>();
            var shuntTargets=shunt?WorldSignalDestinations(source,owner,RouteKind.Shunt):Enumerable.Empty<MarkerRecord>();
            var targets=trainTargets.Concat(shuntTargets).GroupBy(row=>row.Id).Select(g=>g.First().Clone()).ToArray();
            request.SourceAnchor=SignalWorldBridge.AnchorKey(source);
            if(targets.Length==0){request.Reachable=new Dictionary<string,string>(StringComparer.Ordinal);return;}
            worldSignalSearch.Enqueue(request.Signal,()=>FindWorldSignalTargets(request,source.Clone(),targets).GetEnumerator(),
                ()=>WorldSignalRequestCurrent(request)&&RouteWorkAvailable,error=>{
                    request.SearchError=LocalText.Get(error.Message);
                    CustomWindowHandle.Create(WorldSignalRequestWindowId)?.SetNeedsRebuild();
                });
        }

        IEnumerable<bool> FindWorldSignalTargets(WorldSignalRequest request,MarkerRecord source,MarkerRecord[] targets) {
            var graph=new NetworkInput();
            foreach(bool step in world.CaptureRouteGraph(graph))yield return step;
            using(var cancel=new CancellationTokenSource()) {
                try {
                    var token=cancel.Token;
                    var worker=Task.Run(()=>RouteTargetScope.ReachableDestinations(graph,source,targets,token),token);
                    ObserveRouteTask(worker);
                    while(!worker.IsCompleted)yield return false;
                    var reachable=worker.GetAwaiter().GetResult();
                    request.Reachable=targets.Where(row=>reachable.Contains(row.Id))
                        .ToDictionary(row=>row.Id,SignalWorldBridge.AnchorKey,StringComparer.Ordinal);
                    CustomWindowHandle.Create(WorldSignalRequestWindowId)?.SetNeedsRebuild();
                }finally{cancel.Cancel();}
            }
        }

        string WorldSignalRequestUnavailable(WorldSignalRequest request) {
            if(!StateManager.IsHost)return WindowText("View only. The host can request routes.","Nur Ansicht. Fahrstraßen kann der Host anfordern.");
            if(!WorldSignalRequestCurrent(request)||!RouteWorkAvailable||!OperatingReady||!WorkspaceEditable)
                return WindowText("Signal or interlocking state changed. Click the signal again when the layout is ready.","Signal- oder Stellwerkszustand geändert. Das Signal erneut anklicken, sobald die Anlage bereit ist.");
            if(request.Signal==null)return WindowText("This side has no enabled signal head.","Auf dieser Seite ist kein Signalkopf aktiv.");
            var operating=OperatingState(request.Signal);
            if(operating!=SignalOperatingState.Active)return OperatingCaption(operating);
            if(!aiEnabled)return WindowText("Enable AE signalling in the launcher settings to request routes.","Zum Anfordern von Fahrstraßen die AE-Anbindung in den Einstellungen des Auswahlfensters einschalten.");
            if(Entry.BuildModeActive||editingWorkspace&&PanelVisible)return WindowText("End build mode or close the editor before requesting a route.","Vor dem Anfordern einer Fahrstraße den Baumodus beenden oder den Editor schließen.");
            return null;
        }

        DeskDefinition WorldSignalRequestDesk(string id)=>SignalStation(id)??desks.Desks.Single(d=>d.Global);

        string WorldSignalIndication(WorldSignalRequest request) {
            if(!WorldSignalRequestCurrent(request)||request.Signal==null||!StateManager.IsHost&&Entry.Shared?.LiveFresh!=true)
                return WindowText("Awaiting current signal state","Warte auf aktuellen Signalzustand");
            switch(Aspect(request.Signal)) {
                case LocalSignalAspect.Clear: return "Hp 1";
                case LocalSignalAspect.Slow: return "Hp 2";
                case LocalSignalAspect.Shunt: return "Sh 1";
                case LocalSignalAspect.Invalid: return WindowText("Invalidity cross","Ungültigkeitskreuz");
                case LocalSignalAspect.Kennlicht: return WindowText("Identification light","Kennlicht");
                case LocalSignalAspect.Dark: return WindowText("Dark","Dunkel");
                default: return request.Signal!=null&&Object(request.Signal)?.Kind==MarkerRecord.Shunt?"Sh 0":"Hp 0";
            }
        }

        IEnumerable<MarkerRecord> WorldSignalDestinations(MarkerRecord source,DeskDefinition owner,RouteKind movement) {
            bool shunt=movement==RouteKind.Shunt;
            var tiles=owner.IsMaster?desks.Desks.SelectMany(d=>d.Panel.Tiles):owner.Panel.Tiles;
            var ids=tiles.SelectMany(t=>t.Kind==PanelTileKind.Signal?new[]{t.Reference,t.AlternateSignal,t.Target?.Id}:new[]{t.Target?.Id});
            foreach(string id in ids.Where(id=>!string.IsNullOrEmpty(id)&&id!=source.Id).Distinct(StringComparer.Ordinal)) {
                var target=RouteEndpoint(id);
                if(target?.Anchor==null||target.Planned||!AuxiliaryTarget.Accepts(target,shunt))continue;
                if(AuxiliaryTarget.IsEndpoint(target)) {
                    var binding=BoundLineTarget(id);
                    if(binding!=null&&(shunt||binding.Error!=null||binding.Station!=owner.Id||OperatingState(binding.Arrival)!=SignalOperatingState.Active))continue;
                }else if((!shunt&&target.Kind!=MarkerRecord.Main)||OperatingState(id)!=SignalOperatingState.Active)continue;
                yield return target;
            }
        }

        void BuildWorldSignalRequest(UIPanelBuilder builder) {
            var request=worldSignalRequest;if(request==null)return;
            var source=request.Signal==null?null:Object(request.Signal);
            builder.AddSection(source==null?request.Label:new SignalSearchEntry(source.Id,source.Name,source.DisplayTitle).Caption);
            builder.AddField(WindowText("Signal indication","Signalbild"),()=>WorldSignalIndication(request),UIPanelBuilder.Frequency.Periodic);
            var unavailable=WorldSignalRequestUnavailable(request);
            if(unavailable!=null){builder.AddLabel(unavailable);return;}

            var owner=WorldSignalRequestDesk(source.Id);
            builder.AddLabel(owner.Code+" · "+owner.Name);
            if(!owner.Global&&!owner.ShowInDispatcher){builder.AddLabel(WindowText("The owning interlocking is hidden in Dispatcher.","Das zuständige Stellwerk ist im Fahrdienstleiter ausgeblendet."));return;}
            bool train=SignalCatalogue.Supports(source,false,false),slow=SignalCatalogue.Supports(source,false,true),shunt=SignalCatalogue.Supports(source,true,false);
            if(!train&&!slow&&!shunt){builder.AddLabel(WindowText("This signal cannot clear for a route.","Dieses Signal kann keine Fahrstraße freigeben."));return;}
            var trainKind=train?RouteKind.Train:RouteKind.Slow;
            void SetMovement(RouteKind kind) {
                if(!ReferenceEquals(request,worldSignalRequest))return;
                request.Movement=kind;request.Destination=null;request.Error=null;
                builder.SetNeedsRebuild();
            }
            if(shunt&&(train||slow)) {
                builder.HStack(row=>{
                    row.AddButton(WindowText("Train movement","Zugfahrt"),()=>SetMovement(trainKind));
                    row.AddButton(WindowText("Shunt movement","Rangierfahrt"),()=>SetMovement(RouteKind.Shunt));
                });
                if(!request.Movement.HasValue){builder.AddLabel(WindowText("Choose a movement type.","Fahrart wählen."));return;}
            }
            var movement=request.Movement??(shunt?RouteKind.Shunt:trainKind);
            builder.AddLabel(movement==RouteKind.Shunt?WindowText("Shunt movement (Sh 1)","Rangierfahrt (Sh 1)"):movement==RouteKind.Slow?
                WindowText("Reduced speed (Hp 2)","Langsamfahrt (Hp 2)"):WindowText("Train movement (Hp 1)","Zugfahrt (Hp 1)"));
            if(movement!=RouteKind.Shunt&&train&&slow)builder.AddFieldToggle(WindowText("Reduced speed (Hp 2)","Langsamfahrt (Hp 2)"),
                ()=>movement==RouteKind.Slow,value=>SetMovement(value?RouteKind.Slow:RouteKind.Train),true);

            if(request.Reachable==null)BeginWorldSignalSearch(request,source,owner,trainKind,train||slow,shunt);
            if(request.Reachable==null){
                builder.AddLabel(request.SearchError??WindowText("Checking reachable destinations…","Erreichbare Ziele werden geprüft …"));
                return;
            }
            bool FreshTarget(MarkerRecord row)=>request.Reachable.TryGetValue(row.Id,out string anchor)&&anchor==SignalWorldBridge.AnchorKey(row);
            var candidates=WorldSignalDestinations(source,owner,movement).Where(FreshTarget)
                .OrderBy(row=>row.Name,StringComparer.OrdinalIgnoreCase).ThenBy(row=>row.Id).ToArray();
            if(candidates.Length==0){builder.AddLabel(WindowText("No compatible destination is set up at this interlocking.","Für diese Fahrart ist im Stellwerk kein passendes Ziel eingerichtet."));return;}
            string Caption(MarkerRecord row)=>new SignalSearchEntry(row.Id,row.Name,row.DisplayTitle).Caption;
            if(candidates.Length==1) {
                request.Destination=candidates[0].Id;
                builder.AddField(WindowText("Destination","Ziel"),Caption(candidates[0]));
            } else {
                builder.AddSearchChoice(WindowText("Destination","Ziel"),candidates.Select(Caption).ToList(),
                    Array.FindIndex(candidates,row=>row.Id==request.Destination),index=>{
                        if(ReferenceEquals(request,worldSignalRequest)&&index>=0&&index<candidates.Length) {request.Destination=candidates[index].Id;request.Error=null;}
                    });
            }
            builder.AddButtonLarge(WindowText("Request a route","Fahrstraße anfordern"),()=>SubmitWorldSignalRequest(request,movement));
            builder.AddLabel(()=>request.Error??WorldSignalRequestUnavailable(request)??"",UIPanelBuilder.Frequency.Periodic);
        }

        void SubmitWorldSignalRequest(WorldSignalRequest request,RouteKind movement) {
            if(!ReferenceEquals(worldSignalRequest,request))return;
            try {
                var unavailable=WorldSignalRequestUnavailable(request);
                if(unavailable!=null)throw new InvalidOperationException(unavailable);
                var physical=objects.Objects.FirstOrDefault(row=>row.Id==request.Physical);
                var source=Object(request.Signal);
                if(physical==null||WorldSignalPickFace.Resolve(physical,request.Side)!=source?.Id||
                    !SignalCatalogue.Supports(source,movement==RouteKind.Shunt,movement==RouteKind.Slow)||
                    source.Anchor==null||SignalWorldBridge.AnchorKey(source)!=request.SourceAnchor)
                    throw new InvalidOperationException(WindowText("Signal changed. Click it again.","Signal geändert. Erneut anklicken."));
                var owner=WorldSignalRequestDesk(source.Id);
                if(!owner.Global&&!owner.ShowInDispatcher||request.Destination==null||request.Reachable==null||
                    !request.Reachable.TryGetValue(request.Destination,out string anchor)||
                    !WorldSignalDestinations(source,owner,movement).Any(row=>row.Id==request.Destination&&SignalWorldBridge.AnchorKey(row)==anchor))
                    throw new InvalidOperationException(WindowText("Select an available destination for this movement.","Ein verfügbares Ziel für diese Fahrart auswählen."));
                RequireManualStation(source.Id,movement==RouteKind.Shunt);
                if(automaticChoices.Count>0||deskId!=owner.Id&&routeWork.Count>0)
                    throw new InvalidOperationException(WindowText("Finish the pending route selection in Dispatcher first.","Zuerst die ausstehende Fahrwegauswahl im Fahrdienstleiter abschließen."));
                if(editingWorkspace||!PanelVisible)ShowWorkspace(false,0,false);
                if(deskId!=owner.Id)SelectDesk(owner.Id);
                QueueDestination(source.Id,request.Destination,movement);
                CloseWorldSignalRequest();
            }catch(InvalidOperationException error){request.Error=LocalText.Get(error.Message);}
            finally{worldSelectionInput.Complete();}
        }
    }
}
