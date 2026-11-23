using System;
using System.Linq;
using System.Collections.Generic;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        OperatingStateIndex operatingIndex;
        DeskLibrary operatingLibrary;object operatingSavedRoutes;string operatingActive;
        bool operatingNativeFault;string operatingNativeError;double nextOperatingNativeRetry;
        bool OperatingDataReady=>!stopped&&!backupSuspended&&!loadFailed&&!StateManager.IsUnloading&&desks!=null&&Entry.Shared?.ConfigurationReady==true&&scope==objects.WorldKey;
        bool OperatingReady=>OperatingDataReady&&!operatingNativeFault;
        OperatingStateIndex OperatingIndex() {
            string active=string.Join("|",engine.Active.Select(r=>r.Route.Id).OrderBy(id=>id));
            if(operatingIndex==null||!ReferenceEquals(desks,operatingLibrary)||!ReferenceEquals(routes,operatingSavedRoutes)||active!=operatingActive) {
                operatingIndex=new OperatingStateIndex(desks,OperationalRoutes);operatingLibrary=desks;operatingSavedRoutes=routes;operatingActive=active;
            }
            return operatingIndex;
        }
        internal SignalOperatingState OperatingState(string id) {
            if(!OperatingReady||Object(id) is not MarkerRecord row)return SignalOperatingState.Unknown;
            return OperatingIndex().Signal(id,row.Kind==MarkerRecord.Distant&&Object(row.LinkedSignal)?.Kind==MarkerRecord.Main?row.LinkedSignal:null);
        }
        internal bool IgnoreOwnSignal(string id){if(lampTest.Holds(id))return false;var state=OperatingState(id);return state==SignalOperatingState.Unbound||state==SignalOperatingState.OutOfService;}
        internal IEnumerable<MarkerRecord> OperationalSignalObjects=>SignalObjects.Where(s=>OperatingState(s.Id)==SignalOperatingState.Active);
        void RequireOperatingRoutes(IReadOnlyList<RouteDefinition> plan) {
            if(plan.Any(lampTest.Blocks))throw new InvalidOperationException(LocalText.Select("End the affected signal lamp test before setting this route.","Vor dieser Fahrstraße den betroffenen Signal-Lampentest beenden."));
            if(!OperatingReady)throw new InvalidOperationException(WindowText("Operating state is not ready.","Betriebszustand noch nicht verfügbar."));
            LineTargetBindings.RequirePlan(desks,plan);
            OperatingIndex().RequireRoute(plan,id=>AuxiliaryEndpoint(id)!=null?SignalOperatingState.Active:OperatingState(id));
        }
        void ChangeOperatingState(Action<DeskLibrary> update) {
            RequireEdit();var next=desks.Clone();update(next);next.Validate(scope);
            var before=OperatingIndex();var after=new OperatingStateIndex(next,OperationalRoutes);
            var changedSignals=new HashSet<string>(inventory.Values.Where(s=>s.IsSignal&&before.Signal(s.Id,s.Kind==MarkerRecord.Distant?s.LinkedSignal:null)!=after.Signal(s.Id,s.Kind==MarkerRecord.Distant?s.LinkedSignal:null)).Select(s=>s.Id));
            var changedPoints=new HashSet<string>(desks.Points.Keys.Where(id=>before.PointReleased(id)!=after.PointReleased(id)));
            var sections=new List<TrackSection>();
            foreach(var block in desks.Blocks.Values)if(block.Point!=null&&changedPoints.Contains(block.Point)||block.IsCircuit&&new[]{block.BoundaryA,block.BoundaryB}.Any(id=>changedSignals.Contains(id)||CircuitEndpoint(id) is MarkerRecord boundary&&changedSignals.Contains(boundary.PhysicalId??boundary.Id)))sections.AddRange(block.Sections);
            foreach(var desk in desks.Desks.Where(d=>!d.IsMaster&&before.DeskDisabled(d.Id)!=after.DeskDisabled(d.Id)))foreach(string id in desk.Panel.Tiles.SelectMany(t=>new[]{t.BlockId,t.SecondBlock}.Concat(t.CircuitRails?.Values??Enumerable.Empty<string>())).Distinct())if(desks.Blocks.TryGetValue(id,out var block))sections.AddRange(block.Sections);
            bool Overlap(IEnumerable<TrackSection> other)=>OperatingTransitionPolicy.Overlap(other,sections);
            if(engine.Active.Any(run=>OperatingTransitionPolicy.Conflicts(run,changedSignals,changedPoints,sections))||
                directionBlocks.Leases.Any(l=>Overlap(l.Axis.Select(p=>p.Section)))||lineDirections.States.Any(l=>l.Held&&(l.Definition.Anchors.Keys.Any(changedSignals.Contains)||Overlap(l.Axis.Select(p=>p.Section)))))
                throw new InvalidOperationException(WindowText("Release the affected routes and direction locks before changing operating state. Other interlockings may remain in service.","Vor dem Betriebswechsel betroffene Fahrstraßen und Richtungsbindungen auflösen. Andere Stellwerke dürfen in Betrieb bleiben."));
            Entry.Shared.CheckpointInstallation();SaveAutomation(update);
            foreach(var request in automaticRequests.Where(r=>r.Stage==AutomaticRequestStage.Waiting&&(changedSignals.Contains(r.Signal)||changedSignals.Contains(r.Target)))){request.Stage=AutomaticRequestStage.Cancelled;request.Reason=WindowText("Operating state changed; request withdrawn.","Betriebszustand geändert; Anforderung zurückgenommen.");}
            contactLatches.Clear();aheadCache.Clear();panelButtons.Clear();ClearAutomaticRouteChoices();RefreshProxies();nativeDirty=true;PublishNativeAspects();panelView?.Refresh();
            Entry.RecordSignalEvent("operating_state_changed",new{signals=changedSignals.ToArray(),points=changedPoints.ToArray(),desks=desks.Desks.Where(d=>d.OutOfService).Select(d=>d.Id).ToArray()});
        }
        static string OperatingCaption(SignalOperatingState state)=>state==SignalOperatingState.Unbound?LocalText.Select("Not connected · invalidity cross","Nicht eingebunden · Ungültigkeitskreuz"):
            state==SignalOperatingState.OutOfService?LocalText.Select("Out of service · identification light","Betrieblich abgeschaltet · Kennlicht"):
            state==SignalOperatingState.Active?LocalText.Select("In service","In Betrieb"):LocalText.Select("State unavailable · Stop","Zustand unbekannt · Halt");
        internal void BuildSignalOperatingEditor(UIPanelBuilder c,string id,Action verify=null) {
            if(Object(id)?.IsSignal!=true)return;
            var initial=OperatingState(id);string selectedScope=scope;bool own=Automation.Signals.TryGetValue(id,out var rule)&&rule.OutOfService;
            c.AddSection(WindowText("Signal operating state","Betriebszustand des Signals"));
            c.AddField(WindowText("Operating state","Betriebszustand"),()=>OperatingCaption(OperatingState(id)),UIPanelBuilder.Frequency.Periodic);
            if(initial==SignalOperatingState.Unbound){c.AddLabel(WindowText("Link this signal to its diagram first. Until then its normal lights are off and its cross marks it as invalid; native AI ignores it.","Dieses Signal zuerst im Gleisbild einbinden. Bis dahin sind seine normalen Lampen aus; das Kreuz kennzeichnet es als ungültig und die native KI berücksichtigt es nicht."));return;}
            c.AddButton(own?WindowText("Remove individual shutdown","Einzelabschaltung aufheben"):WindowText("Take signal out of service","Signal betrieblich abschalten"),()=>Act(()=>{
                RequireEdit();verify?.Invoke();if(scope!=selectedScope||OperatingState(id)!=initial||(Automation.Signals.TryGetValue(id,out var current)&&current.OutOfService)!=own)throw new InvalidOperationException(WindowText("Signal state changed. Reopen its properties.","Signalzustand geändert. Eigenschaften erneut öffnen."));
                ChangeOperatingState(d=>{if(!d.Automation.Signals.TryGetValue(id,out var value))d.Automation.Signals[id]=value=new AutomaticSignal();value.OutOfService=!own;});
                status=OperatingCaption(OperatingState(id));c.SetNeedsRebuild();
            }));
            c.AddLabel(WindowText("An individual shutdown does not release points for AI. An interlocking shutdown takes precedence over the individual setting.","Eine Einzelabschaltung gibt Weichen nicht für die KI frei. Die Abschaltung des Stellwerks hat Vorrang vor der Einzeleinstellung."));
            c.AddField(WindowText("Result","Ergebnis"),()=>workspaceActionError??status,UIPanelBuilder.Frequency.Periodic);
        }
        void BuildDeskOperatingEditor(UIPanelBuilder c,string id) {
            var desk=desks.Desks.Single(d=>d.Id==id);string selectedScope=scope;bool original=desk.OutOfService;
            c.AddSection(WindowText("Interlocking operating state","Betriebszustand des Stellwerks"));
            c.AddField(WindowText("Operating state","Betriebszustand"),()=>OperatingIndex().DeskDisabled(id)?OperatingCaption(SignalOperatingState.OutOfService):OperatingCaption(SignalOperatingState.Active),UIPanelBuilder.Frequency.Periodic);
            c.AddLabel(WindowText("Out of service: connected signals show an identification light and are ignored by native AI. Points used exclusively by the disabled area return to native AI control; native CTC and occupancy rules remain. A master includes its linked local interlockings.","Außer Betrieb: eingebundene Signale zeigen Kennlicht und werden von der nativen KI ignoriert. Ausschließlich zum abgeschalteten Bereich gehörende Weichen sind für die native KI wieder verfügbar; native CTC- und Belegungsregeln bleiben bestehen. Ein ESTW umfasst seine verknüpften Ortsstellwerke."));
            if(!original&&OperatingIndex().DeskDisabled(id))c.AddLabel(WindowText("Disabled by a linked master. Restore that master first to return this area to service.","Durch ein verknüpftes ESTW abgeschaltet. Zuerst dieses ESTW wieder in Betrieb nehmen."));
            c.AddButton(original?WindowText("Return interlocking to service","Stellwerk wieder in Betrieb nehmen"):WindowText("Take interlocking out of service","Stellwerk außer Betrieb setzen"),()=>Act(()=>{
                RequireEdit();if(scope!=selectedScope||desks.Desks.Single(d=>d.Id==id).OutOfService!=original)throw new InvalidOperationException(WindowText("Interlocking state changed. Reopen this menu.","Stellwerkszustand geändert. Menü erneut öffnen."));
                ChangeOperatingState(d=>d.Desks.Single(item=>item.Id==id).OutOfService=!original);status=WindowText("Interlocking operating state applied.","Stellwerkszustand übernommen.");c.SetNeedsRebuild();
            }));
            c.AddField(WindowText("Result","Ergebnis"),()=>workspaceActionError??status,UIPanelBuilder.Frequency.Periodic);
        }
    }
}
