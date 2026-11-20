using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly Dictionary<string,MarkerRecord> rawGfmObjects=new Dictionary<string,MarkerRecord>();
        GfmRepairPlan gfmRepair;
        TrackBlock[] gfmReplacements;
        string gfmRepairSelection;
        int gfmReplacementChoice;
        bool gfmShowAllFaults=true;
        void IndexGfmObjects(IEnumerable<MarkerRecord> markers) {
            rawGfmObjects.Clear();foreach(var row in markers){rawGfmObjects[row.Id]=row;if(row.Kind==MarkerRecord.Main)foreach(string id in SignalCatalogue.Ids(row))rawGfmObjects[id]=row;}
        }
        MarkerRecord RawGfm(string id)=>id!=null&&rawGfmObjects.TryGetValue(id,out var row)?row:null;
        GfmBoundaryFault GfmFault(string id)=>GfmFaults.Classify(id,RawGfm(id),CircuitEndpoint(id));
        string DescribeGfm(TrackBlock block,string fallback) {
            var issues=new List<string>();
            foreach(bool first in new[]{true,false}) {
                string id=first?block.BoundaryA:block.BoundaryB;var expected=GfmFaults.End(block,first);
                var state=GfmFaults.Classify(id,RawGfm(id),CircuitEndpoint(id),expected);
                if(state!=GfmBoundaryFault.None){var current=CircuitEndpoint(id);issues.Add(BoundaryCaption(id)+" · "+GfmFaults.Reason(state)+" · "+WindowText("saved position: ","gespeicherte Position: ")+GfmFaults.Position(expected)+(current?.Anchor==null?"":" · "+WindowText("current position: ","aktuelle Position: ")+GfmFaults.Position(current.Anchor)));}
            }
            return block.Name+" · "+(issues.Count>0?string.Join("; ",issues):fallback);
        }
        bool GfmRepairValid(TrackBlock b)=>b.IsCircuit&&!gfmInspectionErrors.ContainsKey(b.Id);
        bool GfmCanRetire(TrackBlock b)=>b.IsCircuit&&desks.Blocks.ContainsKey(b.Id)&&gfmInspectionErrors.ContainsKey(b.Id)&&
            new[]{b.BoundaryA,b.BoundaryB}.All(id=>GfmFault(id)!=GfmBoundaryFault.Planned)&&!desks.FreeLines.Any(l=>l.BlockSections.Contains(b.Id));
        void ResetGfmRepair(){gfmRepair=null;gfmReplacements=null;gfmRepairSelection=null;}
        void BuildGfmFaultOverview(UIPanelBuilder c) {
            var faults=desks.Blocks.Values.Where(b=>b.IsCircuit&&gfmInspectionErrors.ContainsKey(b.Id)).OrderBy(b=>b.Name,StringComparer.Ordinal).ToArray();
            c.AddSection(WindowText("All saved GFM faults","Alle gespeicherten GFM-Fehler"));
            c.AddLabel(faults.Length+WindowText(" invalid saved sections. Invalid detection is not used as clear-track proof."," ungültige gespeicherte Abschnitte. Ungültige Freimeldung zählt nicht als Freinachweis."));
            c.AddFieldToggle(WindowText("Include faults from every interlocking in the section list","Fehler aller Stellwerke in der Abschnittsliste anzeigen"),()=>gfmShowAllFaults,v=>{gfmShowAllFaults=v;ResetGfmRepair();RefreshWorkspaceFields();},true);
            foreach(var block in faults.Take(128)) {
                var saved=block;var places=desks.Desks.Where(d=>d.Panel.Tiles.Any(t=>GfmDetectionRefresh.References(t).Contains(block.Id))).Select(d=>d.Code).ToArray();
                c.AddLabel((places.Length==0?WindowText("No diagram assignment", "Ohne Gleisbildzuordnung"):string.Join(" / ",places))+" · "+DescribeGfm(block,gfmInspectionErrors[block.Id]));
                c.AddButton(WindowText("Inspect this fault: ","Diesen Fehler prüfen: ")+block.Name,()=>Act(()=>{RequireFreshGfmInspection();gfmShowAllFaults=true;gfmInspectionChoice=saved.Id;ResetGfmRepair();HighlightGfm(new[]{saved.Id});RefreshWorkspaceFields();}));
            }
            if(faults.Length>128)c.AddLabel(WindowText("Further faults remain selectable in the section list below.","Weitere Fehler sind in der Abschnittsliste unten auswählbar."));
            var removable=faults.Where(b=>GfmCanRetire(b)&&new[]{b.BoundaryA,b.BoundaryB}.Any(id=>GfmFault(id)==GfmBoundaryFault.Missing)).ToArray();
            if(removable.Length>0)c.AddButton(WindowText("Preview cleanup of all sections with deleted boundaries","Bereinigung aller Abschnitte mit gelöschten Grenzen vorschauen"),()=>Act(()=>{
                RequireFreshGfmInspection();gfmRepair=GfmSectionRepair.Build(desks,removable.Select(b=>b.Id),null,GfmRepairValid,GfmFault);gfmRepairSelection=null;RefreshWorkspaceFields();
            }));
        }
        void BuildGfmRepair(UIPanelBuilder c,TrackBlock block) {
            if(!GfmCanRetire(block)) {
                if(desks.FreeLines.Any(l=>l.BlockSections.Contains(block.Id))&&gfmInspectionErrors.ContainsKey(block.Id))c.AddLabel(WindowText("This section belongs to a constructed block assembly. Inspect its boundary origins: complete unfinished plans, re-enable disabled detectors or restore the shared boundary before changing the assembly.","Dieser Abschnitt gehört zu einem gebauten Blockverbund. Herkunft der Grenzen prüfen: unfertige Baupläne fertigstellen, deaktivierte Melder aktivieren oder die gemeinsame Grenze vor einem Verbundumbau wiederherstellen."));
                return;
            }
            c.AddSection(WindowText("Repair this invalid section · preview first","Diesen ungültigen Abschnitt reparieren · zuerst Vorschau"));
            c.AddButton(WindowText("Preview removal of this stale assignment","Entfernen dieser veralteten Zuordnung vorschauen"),()=>Act(()=>{RequireFreshGfmInspection();gfmRepair=GfmSectionRepair.Build(desks,new[]{block.Id},null,GfmRepairValid,GfmFault);gfmRepairSelection=block.Id;RefreshWorkspaceFields();}));
            c.AddButton(WindowText("Re-detect this boundary pair / find complete replacement","Dieses Grenzpaar neu erkennen / vollständigen Ersatz suchen"),()=>Act(()=>{
                RequireFreshGfmInspection();gfmRepair=null;gfmRepairSelection=block.Id;gfmReplacementChoice=0;gfmReplacements=GfmSectionRepair.Candidates(gfmInspectionCatalogue.Values,block,GfmRepairValid);RefreshWorkspaceFields();
            }));
            if(gfmRepairSelection!=block.Id||gfmReplacements==null)return;
            if(gfmReplacements.Length==0){c.AddLabel(WindowText("No valid complete replacement found. Configure actual boundaries or point detection, then refresh this inspection. Signal positions alone do not define GFM boundaries.","Keinen gültigen vollständigen Ersatz gefunden. Reale Grenzen oder Weichen-GFM einrichten, dann die Prüfung aktualisieren. Signalstandorte allein bilden keine GFM-Grenzen."));return;}
            c.AddSearchChoice(WindowText("Complete replacement section","Vollständiger Ersatzabschnitt"),gfmReplacements.Select(b=>GfmPair(b)+" · "+SignUnits.Number(b.Sections.Sum(s=>s.To-s.From))+" m").ToList(),gfmReplacementChoice,i=>{gfmReplacementChoice=i;gfmRepair=null;RefreshWorkspaceFields();});
            c.AddButton(WindowText("Preview replacement and related stale entries","Ersatz und zugehörige Alt-Einträge vorschauen"),()=>Act(()=>{RequireFreshGfmInspection();gfmRepair=GfmSectionRepair.Build(desks,new[]{block.Id},gfmReplacements[gfmReplacementChoice],GfmRepairValid,GfmFault);RefreshWorkspaceFields();}));
        }
        void BuildGfmRepairPreview(UIPanelBuilder c) {
            var plan=gfmRepair;if(plan==null)return;
            c.AddSection(WindowText("Pending GFM repair","Vorgemerkte GFM-Reparatur"));
            c.AddLabel(WindowText("Old sections / changed fields / detached obsolete marks: ","Alt-Abschnitte / geänderte Felder / gelöste veraltete Grenzmarken: ")+plan.Removed.Length+" / "+plan.Fields+" / "+plan.Boundaries);
            c.AddLabel(WindowText("Affected interlockings: ","Betroffene Stellwerke: ")+string.Join(" / ",plan.Desks));
            foreach(string id in plan.Removed)c.AddLabel(DescribeGfm(desks.Blocks[id],gfmInspectionErrors[id]));
            c.AddLabel(plan.Replacement==null?WindowText("After removal these assignments are unmonitored. Define valid coverage before using protected block operation.","Nach dem Entfernen sind diese Zuordnungen unüberwacht. Vor gesichertem Blockbetrieb gültige Abdeckung einrichten."):
                WindowText("Replacement: ","Ersatz: ")+GfmPair(plan.Replacement)+" · "+SignUnits.Number(plan.Replacement.Sections.Sum(s=>s.To-s.From))+" m");
            c.AddLabel(WindowText("Drawing, world objects and train-number identities are retained. Applying requires the affected track clear and no related route or direction holds; these are checked again on confirmation.","Zeichnung, Weltobjekte und Zugnummernkennungen bleiben erhalten. Übernehmen erfordert freie betroffene Gleise und keine zugehörigen Fahrstraßen-/Richtungsbindungen; dies wird bei Bestätigung erneut geprüft."));
            c.AddButton(WindowText("Cancel repair preview","Reparaturvorschau verwerfen"),()=>{gfmRepair=null;RefreshWorkspaceFields();});
            c.AddButton(WindowText("Apply this GFM repair","Diese GFM-Reparatur übernehmen"),()=>Act(()=>{
                RequireFreshGfmInspection();if(!ReferenceEquals(plan,gfmRepair))throw new InvalidOperationException(WindowText("Repair preview changed. Preview it again.","Reparaturvorschau geändert. Erneut erstellen."));
                var blockers=GfmSectionRepair.Blockers(plan,engine.Active,directionBlocks.Leases,lineDirections.States);
                if(blockers.Length>0)throw new InvalidOperationException(WindowText("Repair blocked by:\n","Reparatur blockiert durch:\n")+string.Join("\n",blockers));
                if(routeWork.Count>0||automaticRequests.Any(r=>r.Stage==AutomaticRequestStage.Waiting||r.Stage==AutomaticRequestStage.Submitted))throw new InvalidOperationException(WindowText("Withdraw pending route and automatic requests before applying this repair.","Wartende Fahrstraßen- und Automatikanforderungen vor dieser Reparatur zurücknehmen."));
                world.BeginRead();foreach(var section in plan.Affected)if(world.Occupancy(section)!=TrackOccupancy.Clear)throw new InvalidOperationException(WindowText("Affected track is occupied or unreadable: ","Betroffenes Gleis ist belegt oder nicht lesbar: ")+section.Segment+" · "+SignUnits.Number(section.From)+"–"+SignUnits.Number(section.To)+" m");
                Entry.Shared.CheckpointInstallation();deskStore.Save(plan.Library);desks=Entry.Shared.Desks();panel=DeskPanel(CurrentDesk);panelReadTime=-100;nextPanelRead=0;RefreshDirectionBlocks(true);RefreshLineDirections();ReadGfmInspection();gfmInspectionChoice=plan.Replacement?.Id;
                panelView?.ReplaceDocument(panel);nativeDirty=true;status=WindowText("GFM repair saved. Review the remaining fault overview and coverage before operation.","GFM-Reparatur gespeichert. Verbleibende Fehlerübersicht und Abdeckung vor dem Betrieb prüfen.");RefreshWorkspaceFields();
            }));
        }
        void OpenGfmFaultsFromDispatcher() {
            string target=deskId;ShowWorkspace(true,4,false);EnterEditorDesk(target);gfmShowAllFaults=true;OpenGfmInspection();
        }
    }
}
