using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Game.State;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly LineDirectionAuthority lineDirections=new LineDirectionAuthority();
        string lineValidationSignature;
        void RefreshLineDirections() {
            if(desks==null)return;lineDirections.BlockEnabled=Automation.BlockEnabled;
            string signature=scope+"|"+Entry.Shared?.ConfigurationVersion+"|"+Entry.Shared?.InstallationVersion+"|"+nativeBlockTopology+"|"+blockSignature;
            if(signature==lineValidationSignature&&!lineDirections.NeedsRefresh)return;
            lineDirections.Configure(desks.FreeLines,LineConfigurationError);lineValidationSignature=signature;
        }
        IEnumerable<DirectedRouteSection> MonitoredLineSections()=>desks.Blocks.Values.Where(b=>b.IsCircuit&&CircuitUsable(b))
            .SelectMany(b=>b.Path).Concat(desks.Blocks.Values.Where(b=>b.Point!=null&&PointCircuitVisibility.Enabled(desks,b.Point)).SelectMany(b=>UsablePointGuards(b).Select(s=>new DirectedRouteSection{Section=s})));
        string LineConfigurationError(LineTrackDefinition line) {
            try {
                line.Validate();
                var unitFaults=new List<string>();
                foreach(var unit in line.BlockUnits) {
                    var boundary=Object(unit.Boundary);var forward=Object(unit.Forward);var reverse=unit.Reverse==""?null:Object(unit.Reverse);
                    if(!CircuitIdentity.Enabled(boundary)||forward?.Kind!=MarkerRecord.Main||unit.Reverse!=""&&reverse?.Kind!=MarkerRecord.Main||StationAutomationPolicy.Owner(desks,unit.Forward)?.Id!=line.BlockOwner||reverse!=null&&StationAutomationPolicy.Owner(desks,unit.Reverse)?.Id!=line.BlockOwner)
                        {var affected=new[]{unit.Boundary,unit.Forward,unit.Reverse}.Where(id=>id!="").Select(id=>RawGfm(id)?.Planned==true?BoundaryCaption(id)+" · "+GfmFaults.Reason(GfmBoundaryFault.Planned):Object(id)==null?BoundaryCaption(id):null).Where(s=>s!=null).ToArray();unitFaults.Add(affected.Length>0?string.Join("; ",affected):BoundaryCaption(unit.Boundary)+" · "+WindowText("Check block detector and signal ownership.","Blockmelder und Signalzuständigkeit prüfen."));continue;}
                    var geometry=new BlockLayoutPlan{Axis=line.Axis};var a=geometry.Position(forward.Anchor);var b=reverse==null?(double?)null:geometry.Position(reverse.Anchor);var c=geometry.Position(boundary.Anchor);
                    if(!a.HasValue||!c.HasValue||a>=c||forward.FacingReverse!=geometry.ReverseAt(a.Value)||reverse!=null&&(!b.HasValue||b<=c||reverse.FacingReverse==geometry.ReverseAt(b.Value)))unitFaults.Add(BoundaryCaption(unit.Boundary)+" · "+WindowText("Check the stagger and facing of both block signals around their shared detector.","Versatz und Fahrtrichtung beider Blocksignale um den gemeinsamen Melder prüfen."));
                }
                if(unitFaults.Count>0)return unitFaults.Count+WindowText(" block assembly faults: "," Fehler im Blockverbund: ")+string.Join("; ",unitFaults.Take(6));
                foreach(var group in LineSignalGroups.Used(line))foreach(string id in LineSignalGroups.Get(line,group))CheckLineEndpoint(line,id,LineSignalGroups.Station(line,group),LineSignalGroups.Departure(group));
                if(SignalWorldBridge.TrackFingerprint(line.Forward.Concat(line.Reverse).Select(p=>p.Section))!=line.Topology)return WindowText("Track topology changed; detect the line again.","Gleisnetz geändert. Strecke erneut erkennen.");
                var gaps=GfmCoverage.Gaps(line.Forward.Concat(line.Reverse).Select(p=>p.Section),MonitoredLineSections().Select(p=>p.Section));
                if(gaps.Count>0)return gaps.Count+WindowText(" detection gaps; configure sections / point detection at: "," GFM-Lücken; Abschnitte / Weichen-GFM einrichten bei: ")+string.Join("; ",gaps.Take(8).Select(g=>g.Segment+" · "+SignUnits.Number(g.From)+"–"+SignUnits.Number(g.To)+" m"));
                var invalid=directionBlocks.Blocks.Where(b=>b.Error!=null&&b.Axis.Any(a=>line.Forward.Concat(line.Reverse).Any(p=>p.Section.Overlaps(a.Section)))).ToArray();
                if(invalid.Length>0)return invalid.Length+WindowText(" block-chain faults: "," Fehler in Blockketten: ")+string.Join("; ",invalid.Take(6).Select(b=>b.Error));
                return null;
            }catch(Exception e){return e.Message;}
        }
        void CheckLineEndpoint(LineTrackDefinition line,string id,string station,bool departure) {
            var signal=RouteEndpoint(id);
            if(!LineSignalGroups.AcceptsEndpoint(signal,departure)||StationAutomationPolicy.Owner(desks,id)?.Id!=station||!line.Anchors.TryGetValue(id,out string anchor)||SignalWorldBridge.AnchorKey(signal)!=anchor)
                throw new InvalidOperationException(WindowText("Line signal missing, moved or assigned to another interlocking. Detect the line again.","Streckensignal fehlt, wurde versetzt oder anders zugeordnet. Strecke erneut erkennen."));
        }
        internal void CheckLineConfigurationChange(IEnumerable<string> affected=null,IEnumerable<LineTrackDefinition> proposed=null,IEnumerable<string> targets=null) {
            var definitions=(desks?.FreeLines??new List<LineTrackDefinition>()).Concat(proposed??Enumerable.Empty<LineTrackDefinition>()).ToArray();
            string Caption(TrackSection section){var names=desks.Blocks.Values.Where(b=>b.Path.Any(p=>p.Section.Overlaps(section))).Select(b=>b.Name).Distinct().Take(2).ToArray();return (names.Length==0?section.Segment:string.Join(" / ",names))+" · "+SignUnits.Number(section.From)+"–"+SignUnits.Number(section.To)+" m";}
            var blockers=LineConfigurationChanges.Blockers(definitions,affected,targets,engine.Active,directionBlocks.Leases,lineDirections.States,world.Occupancy,Caption);
            configurationBlockers=blockers;
            if(blockers.Length>0)throw new InvalidOperationException(WindowText("Change blocked by:\n","Änderung blockiert durch:\n")+string.Join("\n",blockers));
        }
        string[] configurationBlockers;
        void BuildConfigurationBlockers(UI.Builder.UIPanelBuilder c) {
            if(configurationBlockers==null||configurationBlockers.Length==0)return;
            c.AddSection(WindowText("Last change blockers","Zuletzt gemeldete Änderungssperren"));
            foreach(string reason in configurationBlockers.Take(20))c.AddLabel(reason);
            c.AddLabel(WindowText("The list describes the last save attempt. Retry saving after the affected route or clear proving has completed.","Die Liste beschreibt den letzten Speicherversuch. Nach Auflösung der betroffenen Fahrstraße oder abgeschlossener Freiprüfung erneut speichern."));
        }
        AutomationStatus[] LineDirectionFrame() {
            return lineDirections.States.Select(s=>new AutomationStatus{Id=s.Definition.Id,Direction=s.Direction,State=!s.Definition.Enabled||!lineDirections.BlockEnabled?"disabled":s.Error!=null?"fault":s.Held?"held":s.ClearChecks<3?"checking":"clear",
                Count=s.ClearChecks,Balance=lineDirections.IsAutomatic(s)?1:0,RequestedDirection=s.RequestedDirection,Revision=s.Revision,Reason=AutomationText(s.Error??LineHoldReason(s),512)}).ToArray();
        }
        string LineHoldReason(LineDirectionState state) {
            string request=state.RequestedDirection==0?"":WindowText("Permission requested by ","Erlaubnis angefragt von ")+LineStationCode(state.RequestedDirection==1?state.Definition.StationA:state.Definition.StationB)+" · ";
            if(!state.Held)return request+(state.ClearChecks<3?WindowText("Whole-line clear proving required for transfer.","Vollständige Freiprüfung für die Abgabe erforderlich."):lineDirections.AutomaticAllowed?.Invoke(state.Definition)==false?WindowText("Automatic permission paused by central takeover.","Automatischer Erlaubniswechsel durch zentrale Übernahme pausiert."):WindowText("Permission retained; route and block checks remain separate.","Erlaubnis bleibt erhalten; Fahrstraßen- und Blockprüfung bleiben getrennt."));
            var ids=state.Routes.Concat(directionBlocks.Leases.Where(l=>l.Axis.Any(a=>state.Axis.Any(b=>a.Section.Overlaps(b.Section)))).SelectMany(l=>l.Routes)).ToHashSet();
            var active=engine.Active.Where(r=>ids.Contains(r.Route.Id)).Select(r=>r.Route.Code).Distinct().ToArray();
            return request+(active.Length>0?WindowText("Held by: ","Gebunden durch: ")+string.Join(", ",active):WindowText("Train tail / complete clear proving ","Zugschluss / vollständige Freiprüfung ")+state.ClearChecks+"/3");
        }
        AutomationStatus LineLive(string id)=>StateManager.IsHost?LineDirectionFrame().FirstOrDefault(s=>s.Id==id):Entry.Shared?.LiveFresh==true?Entry.Shared.Live?.Automation?.Lines?.FirstOrDefault(s=>s.Id==id):null;
        string LineStationCode(string id)=>desks?.Desks.FirstOrDefault(d=>d.Id==id)?.Code??"?";
        string LineDirectionCaption(LineTrackDefinition line,AutomationStatus live) {
            if(live==null)return WindowText("Unknown / no current host data","Unbekannt / keine aktuellen Hostdaten");
            string arrow=live.Direction==1?LineStationCode(line.StationA)+" → "+LineStationCode(line.StationB):live.Direction==-1?LineStationCode(line.StationB)+" → "+LineStationCode(line.StationA):WindowText("No permission","Keine Erlaubnis");
            string state=live.State=="held"?WindowText("held","gebunden"):live.State=="fault"?WindowText("fault","Störung"):live.State=="disabled"?WindowText("disabled","deaktiviert"):live.State=="checking"?WindowText("checking clear","Freiprüfung"):WindowText("clear","frei");
            return arrow+" · "+state+" · "+(live.Balance==1?WindowText("automatic","automatisch"):WindowText("manual","manuell"));
        }
        void OperateLinePermission(string id,string station,long revision,string operation) {
            RequireDispatcher();RefreshLineDirections();world.BeginRead();
            var line=desks.FreeLines.FirstOrDefault(l=>l.Id==id)??throw new InvalidOperationException(WindowText("Line missing","Strecke fehlt."));
            if(CurrentDesk?.IsMaster!=true&&deskId!=station||station!=line.StationA&&station!=line.StationB)throw new InvalidOperationException(WindowText("Operate from the selected neighbouring interlocking.","An der ausgewählten Nachbarbetriebsstelle bedienen."));
            if(SessionRequiresExplicitScope&&!LineSignalGroups.Get(line,station==line.StationA?LineSignalGroup.ADepartures:LineSignalGroup.BDepartures)
                .Any(signal=>SessionAutomaticHeld(signal,false)))
                throw new InvalidOperationException(WindowText("Take over train movements at this line end first.","Übernimm zuerst die Zugfahrten an diesem Streckenende."));
            if(operation=="request")lineDirections.RequestPermission(id,station,revision);
            else if(operation=="give")lineDirections.GivePermission(id,station,revision,engine.Active);
            else if(operation=="cancel")lineDirections.CancelRequest(id,station,revision);
            else if(operation=="pause")lineDirections.PauseAutomatic(id,station,revision);
            else if(operation=="resume")lineDirections.ResumeAutomatic(id,station,revision);
            else throw new InvalidOperationException(WindowText("Unknown permission operation.","Unbekannte Erlaubnisbedienung."));
            Entry.RecordSignalEvent("line_permission",new{id,station,operation,revision});status=WindowText("Line permission updated. Existing route locks stay supervised.","Streckenerlaubnis aktualisiert. Bestehende Fahrstraßenverschlüsse bleiben überwacht.");
            RefreshWorkspaceFields();panelView?.Refresh();
        }
    }
}
