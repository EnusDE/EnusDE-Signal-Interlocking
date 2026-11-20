using System;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        ExistingDetectionResult existingDetectionProposal;
        string existingDetectionDesk,existingDetectionScope,existingDetectionNetwork;
        long existingDetectionVersion,existingDetectionInstallation;
        bool existingDetectionAll;
        void OpenExistingDetection(){RequirePanelEdit();existingDetectionProposal=null;existingDetectionDesk=deskId;WorkspacePage(11);}
        void BuildExistingDetection(UIPanelBuilder c) {
            c.AddSection(WindowText("Detect GFM for this diagram","GFM für dieses Gleisbild erkennen"));
            c.AddLabel(WindowText("Keeps your drawing. Reads existing signal/box boundaries and enabled point detection, then proposes complete sections between assigned fields. Unassigned symbols and ambiguous paths are reported rather than guessed.","Deine Zeichnung bleibt erhalten. Liest vorhandene Signal-/Kastengrenzen und aktivierte Weichen-GFM und schlägt vollständige Abschnitte zwischen zugeordneten Feldern vor. Ungebundene Symbole und mehrdeutige Wege werden nicht geraten."));
            c.AddButton(WindowText("Inspect sections and coverage gaps…","Abschnitte und Überwachungslücken prüfen …"),()=>Act(()=>OpenGfmInspection()));
            c.AddButton(WindowText("Set up GFM from world objects…","GFM über Weltobjekte einrichten …"),()=>Act(()=>OpenWorldGfmSetup()));
            c.AddFieldToggle(WindowText("Include all interlockings","Alle Stellwerke einbeziehen"),()=>existingDetectionAll,v=>{existingDetectionAll=v;existingDetectionProposal=null;RefreshWorkspaceFields();},true);
            c.AddLabel(WindowText("Full rescan replaces section assignments only after you apply the preview. World objects, boundary marks, signal/point links and the drawing remain. Unresolved areas stay unmonitored. A backup is created before applying.","Der vollständige Neuscan ersetzt Abschnittszuordnungen erst beim Übernehmen der Vorschau. Weltobjekte, Grenzmarken, Signal-/Weichenverknüpfungen und Zeichnung bleiben erhalten. Ungeklärte Bereiche bleiben unüberwacht. Vor dem Übernehmen wird eine Sicherung erstellt."));
            void Search(bool reset)=>Act(()=>{
                RequirePanelEdit();var input=CaptureNetwork();var next=desks.Clone();
                var ids=existingDetectionAll?next.Desks.Select(d=>d.Id).ToArray():new[]{deskId}.Concat(CurrentDesk.IsMaster?next.Modules.Where(m=>MasterDiagram.Owner(next,m)==deskId).Select(m=>m.Desk):Enumerable.Empty<string>()).Distinct().ToArray();
                foreach(var target in next.Desks.Where(d=>ids.Contains(d.Id)))target.Panel=DiagramRegistration.Inspect(target.Panel,SignalObjects,true).Panel;
                var network=NetworkRecognition.PreserveCircuitIds(next,DetectionNetwork.Build(input),CanonicalBoundaryId);
                existingDetectionProposal=GfmDetectionRefresh.Build(next,ids,network,DiagramBoundary,SharedPointCircuits().PointSignals,input,reset);
                existingDetectionInstallation=Entry.Shared.InstallationVersion;
                existingDetectionDesk=deskId;existingDetectionScope=scope;existingDetectionVersion=Entry.Shared.ConfigurationVersion;existingDetectionNetwork=NetworkRecognition.Signature(input);RefreshWorkspaceFields();
            });
            c.AddButton(WindowText("Find GFM sections in existing diagram","GFM-Abschnitte im vorhandenen Gleisbild suchen"),()=>Search(false));
            c.AddButton(WindowText("Rebuild GFM assignments · preview","GFM-Zuordnungen vollständig neu erkennen · Vorschau"),()=>Search(true));
            c.AddButton(WindowText("Clear GFM assignments only · preview","Nur GFM-Zuordnungen aufheben · Vorschau"),()=>Act(()=>{
                RequirePanelEdit();var ids=existingDetectionAll?desks.Desks.Select(d=>d.Id).ToArray():new[]{deskId};
                existingDetectionProposal=GfmDetectionRefresh.Clear(desks,ids);existingDetectionDesk=deskId;existingDetectionScope=scope;
                existingDetectionVersion=Entry.Shared.ConfigurationVersion;existingDetectionInstallation=Entry.Shared.InstallationVersion;existingDetectionNetwork=null;RefreshWorkspaceFields();
            }));
            c.AddLabel(WindowText("Clear only also disables point detection and removes manual point-boundary links in the selected scope. Physical objects remain. Background GFM catalog updates are paused so cleared entries are not immediately recreated.","Nur aufheben deaktiviert auch die Weichen-GFM und entfernt manuelle Weichen-Grenzverknüpfungen im gewählten Umfang. Weltobjekte bleiben erhalten. Die GFM-Hintergrunderkennung wird pausiert, damit aufgehobene Einträge nicht sofort neu entstehen."));
            var proposal=existingDetectionProposal;if(proposal==null||existingDetectionDesk!=deskId)return;
            c.AddLabel(WindowText("Complete sections / points / assigned fields: ","Vollständige Abschnitte / Weichen / zugeordnete Felder: ")+proposal.Circuits+" / "+proposal.Points+" / "+proposal.Fields);
            c.AddLabel(WindowText("Shared A/B point links: ","Gemeinsame A/B-Weichenanschlüsse: ")+proposal.Shared);
            c.AddLabel(WindowText("Corrected point-arm mappings: ","Korrigierte Weichenast-Zuordnungen: ")+proposal.Calibrated);
            if(proposal.Rebuilt)c.AddLabel(WindowText("Replaced old sections / cleared fields: ","Ersetzte alte Abschnitte / zurückgesetzte Felder: ")+proposal.Removed+" / "+proposal.ResetFields);
            int unbound=panel.Tiles.Count(t=>t.Kind==PanelTileKind.Signal&&Object(t.Reference)?.IsSignal!=true);
            int missing=panel.Tiles.Count(t=>t.Kind==PanelTileKind.Signal&&Object(t.Reference)?.IsSignal==true&&!CircuitIdentity.Enabled(Object(t.Reference)));
            int points=panel.Tiles.Count(t=>t.Kind==PanelTileKind.Switch&&!PointCircuitVisibility.Enabled(desks,t.Reference));
            c.AddLabel(WindowText("Unassigned signals / signals without GFM / points without GFM: ","Ungebundene Signale / Signale ohne GFM / Weichen ohne GFM: ")+unbound+" / "+missing+" / "+points);
            c.AddLabel(WindowText("Unresolved connections: ","Ungeklärte Verbindungen: ")+proposal.Unresolved.Count);
            foreach(string item in proposal.Unresolved.Take(64))c.AddLabel(WindowText("Unresolved diagram path: ","Nicht eindeutiger Gleisbildweg: ")+item);
            foreach(var issue in proposal.Issues.Take(64)){var selectedIssue=issue;c.AddButton(WindowText("Inspect: ","Prüfen: ")+issue.Message,()=>Act(()=>OpenGfmIssue(selectedIssue)));}
            if(proposal.Circuits+proposal.Points==0&&!proposal.Rebuilt)return;
            c.AddButton(proposal.ResetOnly?WindowText("Clear the previewed GFM assignments","GFM-Zuordnungen laut Vorschau aufheben"):proposal.Rebuilt?WindowText("Replace GFM assignments with this preview","GFM-Zuordnungen durch diese Vorschau ersetzen"):WindowText("Apply detected GFM without redrawing","Erkannte GFM ohne Neuzeichnen übernehmen"),()=>Act(()=>{
                RequirePanelEdit();RequireIdleAdministration();
                if(!ReferenceEquals(proposal,existingDetectionProposal)||deskId!=existingDetectionDesk||scope!=existingDetectionScope||Entry.Shared.ConfigurationVersion!=existingDetectionVersion||Entry.Shared.InstallationVersion!=existingDetectionInstallation||existingDetectionNetwork!=null&&NetworkRecognition.Signature(CaptureNetwork())!=existingDetectionNetwork)
                    throw new InvalidOperationException(WindowText("Detection is outdated. Search again.","Erkennung ist veraltet. Erneut suchen."));
                Entry.Shared.CheckpointInstallation();deskStore.Save(proposal.Library);desks=proposal.Library;panel=DeskPanel(CurrentDesk);existingDetectionProposal=null;
                if(proposal.ResetOnly)PauseGfmBackground();
                RefreshDirectionBlocks();RefreshLineDirections();nativeDirty=true;
                panelView?.ReplaceDocument(panel);panelReadTime=-100;nextPanelRead=0;status=proposal.ResetOnly?WindowText("GFM assignments cleared; background catalog paused. Configure sections manually or request a new scan.","GFM-Zuordnungen aufgehoben; Hintergrundkatalog pausiert. Abschnitte manuell einrichten oder neuen Scan anfordern."):WindowText("GFM assigned to the existing diagram. Missing boundaries still require configuration.","GFM im vorhandenen Gleisbild zugeordnet. Fehlende Grenzen weiterhin einrichten.");RefreshWorkspaceFields();
            }));
        }
    }
}
