using System;
using System.Linq;
using Newtonsoft.Json;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        TrackBlock[] lineDetectionProposal;
        string lineDetectionWorld,lineDetectionNetwork,lineDetectionShape;
        long lineDetectionVersion;
        void ClearLineDetectionProposal(){lineDetectionProposal=null;lineDetectionWorld=lineDetectionNetwork=lineDetectionShape=null;}
        static string LineDetectionShape(LineTrackDefinition line)=>JsonConvert.SerializeObject(new{line.Id,line.StationA,line.StationB,line.Mode,line.ForwardStart,line.ForwardEnd,line.ReverseStart,line.ReverseEnd,line.ADepartures,line.BArrivals,line.BDepartures,line.AArrivals,line.Topology,line.Forward,line.Reverse});
        void BuildLineDetectionProposal(UIPanelBuilder c,LineTrackDefinition draft,Func<bool> current) {
            c.AddLabel(WindowText("Detection proposals use existing GFM boundaries and enabled point detection. Your track diagram is retained. Apply proposals explicitly, then recheck coverage before enabling the line.","GFM-Vorschläge verwenden vorhandene Grenzen und aktivierte Weichen-GFM. Deine Gleiszeichnung bleibt erhalten. Vorschläge ausdrücklich übernehmen und vor Aktivierung die Abdeckung erneut prüfen."));
            c.AddButton(WindowText("Find detection sections for this line","GFM-Abschnitte für diese Strecke vorschlagen"),()=>Act(()=>{
                if(!current())return;RequirePanelEdit();CheckLineConfigurationChange(new[]{draft.Id},new[]{draft});
                var input=CaptureNetwork();var detected=NetworkRecognition.PreserveCircuitIds(desks,DetectionNetwork.Build(input),CanonicalBoundaryId);
                lineDetectionProposal=LineSetupWorkflow.DetectionCandidates(desks,draft,detected);
                lineDetectionWorld=scope;lineDetectionVersion=Entry.Shared.ConfigurationVersion;lineDetectionNetwork=NetworkRecognition.Signature(input);lineDetectionShape=LineDetectionShape(draft);
                RefreshWorkspaceFields();
            }));
            if(lineDetectionProposal==null)return;
            c.AddField(WindowText("Detection proposals","GFM-Vorschläge"),lineDetectionProposal.Length.ToString());
            if(lineDetectionProposal.Length==0){c.AddLabel(WindowText("No complete boundary pairs found. Enable signal boxes and any required point detection, then detect again.","Keine vollständigen Grenzpaare gefunden. Signalkästen und erforderliche Weichen-GFM aktivieren, dann erneut erkennen."));return;}
            foreach(var block in lineDetectionProposal.Take(12))c.AddLabel(block.Point!=null?PointAssignments.Caption(desks,block.Point):
                (CircuitEndpoint(block.BoundaryA)?.Name??block.BoundaryA)+" ↔ "+(CircuitEndpoint(block.BoundaryB)?.Name??block.BoundaryB));
            var proposal=lineDetectionProposal;
            c.AddButton(WindowText("Apply line detection proposals","GFM-Vorschläge für die Strecke übernehmen"),()=>Act(()=>{
                if(!current())return;RequirePanelEdit();CheckLineConfigurationChange(new[]{draft.Id},new[]{draft});
                if(!ReferenceEquals(proposal,lineDetectionProposal)||scope!=lineDetectionWorld||Entry.Shared.ConfigurationVersion!=lineDetectionVersion||LineDetectionShape(draft)!=lineDetectionShape||NetworkRecognition.Signature(CaptureNetwork())!=lineDetectionNetwork)
                    throw new InvalidOperationException(WindowText("Line detection proposal is outdated. Detect it again.","GFM-Vorschlag der Strecke ist veraltet. Erneut erkennen."));
                SaveAutomation(d=>{foreach(var block in proposal)d.Blocks[block.Id]=block.Clone();});
                ClearLineDetectionProposal();status=WindowText("Line detection sections saved. Review coverage before enabling protection.","GFM-Abschnitte der Strecke gespeichert. Vor Aktivierung der Sicherung die Abdeckung prüfen.");RefreshWorkspaceFields();
            }));
        }
    }
}
