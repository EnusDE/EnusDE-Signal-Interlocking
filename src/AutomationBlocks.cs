using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Track;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly DirectionBlockAuthority directionBlocks=new DirectionBlockAuthority();
        string blockSignature,blockError,blockCatalogError,nativeBlockTopology;double nextBlockTopology;
        double nextBlockScan;long blockScanConfiguration=-1,blockScanInstallation=-1;
        AutomationConfiguration Automation=>desks?.Automation;
        static string BoundaryKey(MarkerAnchor a)=>a==null?null:a.Segment+"@"+Math.Round(a.Meters,2).ToString("0.00",CultureInfo.InvariantCulture);
        void StartAutomation() {
            lineDirections.TargetBinding=RouteLineTarget;
            stationCapacity.Library=()=>desks;stationCapacity.Active=()=>engine.Active;stationCapacity.Time=()=>UnityEngine.Time.realtimeSinceStartup;stationCapacity.Occupancy=b=>CircuitUsable(b)?world.Block(b,out _):TrackOccupancy.Unknown;
            engine.Admission=new CombinedRouteAdmission(new OperatingRouteAdmission(RequireOperatingRoutes),directionBlocks,lineDirections,stationCapacity);lineDirections.Occupancy=s=>world.Occupancy(s);lineDirections.AutomaticAllowed=l=>Automation!=null&&!OperatingIndex().DeskDisabled(l.StationA)&&!OperatingIndex().DeskDisabled(l.StationB)&&!Automation.PausedStations.Contains(l.StationA)&&!Automation.PausedStations.Contains(l.StationB);lineDirections.AdditionalHold=s=>directionBlocks.Leases.Any(l=>l.Axis.Any(a=>s.Axis.Any(b=>a.Section.Overlaps(b.Section))));directionBlocks.Occupancy=s=>world.Occupancy(s);
            directionBlocks.CheckStations=CheckAutomationStations;
            directionBlocks.ProtectRoute=NeedsDirectionCoordination;
            automationHooks=new AutomationHooks();
            try{automationHooks.Start(ProtectAiPoint,BuildCarAutomation);}catch(Exception e){automationHookError=e.Message;log?.Warning("Automatik-Anbindung: "+e.Message);}
            try{contactScene=new AutomationContactScene(transform,OpenAutomationContact);}catch(Exception e){log?.Warning("Kontaktmodell nicht verfügbar; Kontaktfunktion bleibt aktiv: "+e.Message);}
        }
        void SaveAutomation(Action<DeskLibrary> update) {
            RequireEdit();if(desks==null||deskStore==null)throw new InvalidOperationException("Stellwerke werden noch geladen.");
            var next=desks.Clone();update(next);next.Validate(scope);
            if(next.Automation.OnDemandRoutes!=OnDemandRoutes||next.Automation.AllowUnmonitoredManualRoutes!=Automation.AllowUnmonitoredManualRoutes)RouteRequestPolicy.RequireModeChange(engine.Active.Count>0,directionBlocks.Leases.Count>0,lineDirections.HasHolds,
                routeWork.Count>0||automaticRequests.Any(r=>r.Stage==AutomaticRequestStage.Waiting||r.Stage==AutomaticRequestStage.Submitted));
            deskStore.Save(next);desks=next;panel=DeskPanel(CurrentDesk);
            if(!Automation.AutomaticGfmCatalog)PauseGfmBackground();
            RefreshDirectionBlocks();RefreshLineDirections();nativeDirty=true;
        }
        void RefreshDirectionBlocks(bool immediate=false) {
            directionBlocks.Enabled=Automation?.BlockEnabled==true;
            if(!directionBlocks.Enabled)return;
            double now=UnityEngine.Time.realtimeSinceStartup;
            if(!immediate&&now<nextBlockScan&&blockScanConfiguration==Entry.Shared.ConfigurationVersion&&blockScanInstallation==Entry.Shared.InstallationVersion)return;
            nextBlockScan=now+2;blockScanConfiguration=Entry.Shared.ConfigurationVersion;blockScanInstallation=Entry.Shared.InstallationVersion;
            if(immediate||now>=nextBlockTopology) {
                nextBlockTopology=now+2;
                nativeBlockTopology=SharedDocuments.Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("|",Graph.Shared.AllSegments.OrderBy(s=>s.id,StringComparer.Ordinal)
                    .Select(s=>s.id+":"+s.a?.id+":"+s.b?.id+":"+s.GetLength().ToString("R",CultureInfo.InvariantCulture)+":"+s.Available+":"+s.GroupEnabled+":"+(s.turntable!=null)))));
            }
            var boundaries=Automation.Signals.Where(p=>p.Value.Duty==SignalDuty.Entry||p.Value.Duty==SignalDuty.Exit)
                .Select(p=>Object(p.Key)?.Anchor).Where(a=>a!=null).ToArray();
            var network=CaptureNetwork();string signature=DirectionBlockTopology.Signature(network,desks.Blocks.Values,boundaries);
            if(signature==blockSignature){blockError=blockCatalogError;return;}
            var links=new List<DirectionBlockLink>();
            foreach(var circuit in desks.Blocks.Values.Where(b=>b.IsCircuit)) {
                var a=CircuitEndpoint(circuit.BoundaryA);var b=CircuitEndpoint(circuit.BoundaryB);
                var link=new DirectionBlockLink{Id=circuit.Id,A=BoundaryKey(a?.Anchor)??"missing:"+circuit.BoundaryA,B=BoundaryKey(b?.Anchor)??"missing:"+circuit.BoundaryB,
                    Path=circuit.Path.Select(p=>new DirectedRouteSection{Section=p.Section.Clone(),Reverse=p.Reverse}).ToList()};
                try {link.Error=CircuitPathValidation.Error(network,circuit,a,b,id=>PointAssignments.Caption(desks,id));}
                catch(Exception e){link.Error=LocalText.Select("GFM network cannot be read: ","GFM-Netz nicht lesbar: ")+e.Message;}
                if(link.Error!=null)link.Error=DescribeGfm(circuit,link.Error);
                links.AddRange(DirectionBlockBuilder.Split(link,boundaries));
            }
            var stops=new HashSet<string>(boundaries.Select(BoundaryKey));
            foreach(var id in desks.Blocks.Values.Where(b=>b.IsCircuit).SelectMany(b=>new[]{b.BoundaryA,b.BoundaryB}))
                if(TrackCircuit.TryPointId(id,out _,out _)||SharedPointCircuits().Signals.ContainsKey(id)) {var endpoint=CircuitEndpoint(id);if(endpoint!=null)stops.Add(BoundaryKey(endpoint.Anchor));}
            var proposed=DirectionBlockBuilder.Build(links,stops);
            if(!DirectionCatalogChanges.CanReplace(directionBlocks.Blocks,proposed,directionBlocks.Leases)){blockError=LocalText.Select("Changed GFM/station boundaries affect retained block directions: ","Geänderte GFM-/Bahnhofsgrenzen betreffen gehaltene Blockrichtungen: ")+string.Join("; ",directionBlocks.Leases.Select(l=>l.Id+" · "+l.ClearChecks+"/3 · "+l.Reason));return;}
            directionBlocks.Blocks=proposed;blockSignature=signature;blockError=blockCatalogError=null;
            if(directionBlocks.Blocks.Count>512){blockError=blockCatalogError="Zu viele Blockketten (höchstens 512 im ersten Testpaket).";}
        }
        DeskDefinition SignalStation(string id)=>StationAutomationPolicy.Owner(desks,id);
        bool NeedsDirectionCoordination(RouteDefinition route) {
            if(desks.FreeLines.Any(l=>l.Enabled&&l.Forward.Concat(l.Reverse).Any(p=>route.Path?.Any(r=>r.Section.Overlaps(p.Section))==true)))return true;
            return !StationAutomationPolicy.LocalMovement(desks,route,Automation.Signals.Where(p=>p.Value.Duty==SignalDuty.Entry||p.Value.Duty==SignalDuty.Exit).Select(p=>Object(p.Key)?.Anchor).Where(a=>a!=null));
        }
        string CheckAutomationStations(IReadOnlyList<RouteDefinition> plan) {
            RefreshDirectionBlocks(true);RefreshLineDirections();var external=plan.Where(NeedsDirectionCoordination).ToArray();if(directionBlocks.Enabled&&external.Length>0&&blockError!=null)return blockError;
            if(desks==null)return null;
            if(directionBlocks.Enabled&&!Automation.AllowUnmonitoredManualRoutes&&plan.Any(r=>!r.Shunting))
                RequireMonitoredPlan(new RouteChainPlan{Routes=plan.Where(r=>!r.Shunting).ToList()});
            string detection=StationRouteProtection.CircuitError(plan,desks.Blocks.Values,CircuitUsable,b=>world.Block(b,out _),b=>BoundaryCaption(b.BoundaryA)+" ↔ "+BoundaryCaption(b.BoundaryB),world.Occupancy);
            if(detection!=null)return detection;
            return StationAutomationPolicy.Admission(desks,plan);
        }
        void RequireManualStation(string source) {
            if(desks?.Desks.Any(d=>!d.IsMaster&&d.AutomaticOnly)==true&&SignalStation(source)?.AutomaticOnly==true&&!Automation.PausedStations.Contains(SignalStation(source).Id))throw new InvalidOperationException("Dieses Stellwerk ist auf Automatikbetrieb eingestellt. Im Editor umstellen, bevor dort manuell angefordert wird.");
        }
        void RequireMonitoredPlan(RouteChainPlan plan) {
            if(!Automation.BlockEnabled||blockError!=null)throw new InvalidOperationException(blockError??"Blockabstimmung ist ausgeschaltet.");
            if(plan.Routes.Any(r=>r.Path==null||r.Path.Count==0))throw new InvalidOperationException("Gerichteten Fahrweg zuerst aktualisieren.");
            var monitored=desks.Blocks.Values.Where(b=>b.IsCircuit&&CircuitUsable(b)).SelectMany(b=>b.Path.Select(p=>p.Section))
                .Concat(desks.Blocks.Values.Where(b=>b.Point!=null&&PointCircuitVisibility.Enabled(desks,b.Point)).SelectMany(UsablePointGuards));
            var gaps=GfmCoverage.Gaps(plan.Routes.SelectMany(r=>r.Path).Select(p=>p.Section),monitored,.05);
            if(gaps.Count>0)
                throw new InvalidOperationException(LocalText.Select("Missing track detection on ","Gleisfreimeldung fehlt auf ")+
                    string.Join(" · ",gaps.Take(8).Select(g=>g.Segment+" "+SignUnits.Number(g.From)+"–"+SignUnits.Number(g.To)+" m")));
        }
        string ManualCoverageNotice(IEnumerable<RouteDefinition> plan) {
            if(!Automation.AllowUnmonitoredManualRoutes)return "";
            try {
            var monitored=desks.Blocks.Values.Where(b=>b.IsCircuit&&CircuitUsable(b)).SelectMany(b=>b.Path.Select(p=>p.Section))
                .Concat(desks.Blocks.Values.Where(b=>b.Point!=null&&PointCircuitVisibility.Enabled(desks,b.Point)).SelectMany(UsablePointGuards));
            var gaps=GfmCoverage.Gaps(plan.SelectMany(r=>r.Path).Select(p=>p.Section),monitored);
            if(gaps.Count==0)return "";
            Entry.RecordSignalEvent("manual_unmonitored_route",new{routes=plan.Select(r=>r.Id).ToArray(),gaps});
            return LocalText.Select(" · Unmonitored areas: dispatcher checks the route. No GFM clear indication."," · Unüberwachte Bereiche: Fahrdienstleiter prüft den Fahrweg. Keine GFM-Freimeldung.");
            }catch(Exception e){Entry.RecordSignalEvent("manual_gfm_diagnostic_failed",new{error=e.Message});return LocalText.Select(" · GFM coverage could not be fully checked; dispatcher inspection required."," · GFM-Abdeckung nicht vollständig prüfbar; Prüfung durch den Fahrdienstleiter erforderlich.");}
        }
        bool ProtectAiPoint(string node) {
            if(stopped||Game.State.StateManager.IsUnloading||scope!=objects.WorldKey||!Game.State.StateManager.IsHost)return false;
            if(OperatingReady&&OperatingIndex().PointReleased(node))return false;
            return WaypointSwitchPolicy.Protect(ElectricStand(node),Automation?.BlockEnabled==true,Automation?.ProtectWaypointSwitches==true,
                ()=>OperationalRoutes.Any(r=>r.Switches.Any(p=>p.Node==node))||desks.Desks.Any(d=>d.Panel.Tiles.Any(t=>t.Kind==PanelTileKind.Switch&&t.Reference==node)));
        }
    }
}
