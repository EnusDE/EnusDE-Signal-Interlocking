using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Track;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        Task<NetworkResult> recognitionTask;
        CancellationTokenSource recognitionCancel;
        NetworkResult recognitionProposal;
        string recognitionTarget,recognitionSignature,recognitionWorld,recognitionMessage="";
        string[] recognitionMissingBoxes=new string[0];
        long recognitionVersion;
        bool recognitionDrawing;
        bool recognitionNearby=true;
        string recognitionRadius="1000";
        void OpenRecognition() {
            RequireEdit();if(!editingWorkspace)ShowWorkspace(true,4,false);
            registrationDesk=CurrentDesk?.Id;
            if(CurrentDesk?.IsMaster!=true)SelectDesk(desks.Desks.Single(d=>d.Global).Id,false);
            recognitionTarget=CurrentDesk.Id;WorkspacePage(10);
        }
        NetworkInput CaptureRecognitionNetwork() {
            var input=CaptureNetwork();
            foreach(var row in inventory.Values.Where(s=>s.Kind==MarkerRecord.Distant)) {
                var at=SignalWorldBridge.Location(row).WithEnd(TrackSegment.End.A);var position=at.GetPosition(PositionAccuracy.High);
                input.Cuts.Add(new PlanCut{Segment=at.segment.id,Meters=at.distance,Signal=row.Id,X=position.x,Y=position.z,Kind=row.Kind,Reverse=row.FacingReverse});
            }
            return input;
        }
        void StartRecognition(bool draw) {
            RequirePanelEdit();StopNetwork();CancelRecognition();
            if(!desks.Desks.Any(d=>d.Id==recognitionTarget&&d.IsMaster))throw new InvalidOperationException(WindowText("Select a master interlocking.","Ein übergeordnetes Stellwerk auswählen."));
            var input=CaptureRecognitionNetwork();recognitionSignature=NetworkRecognition.Signature(input);recognitionWorld=scope;
            recognitionMissingBoxes=input.Cuts.Where(c=>c.Signal!=null&&!input.Cuts.Any(b=>b.Boundary!=null&&b.Segment==c.Segment&&Math.Abs(b.Meters-c.Meters)<.001)).Select(c=>c.Signal).Distinct().ToArray();
            recognitionVersion=Entry.Shared.ConfigurationVersion;recognitionDrawing=draw;
            recognitionCancel=new CancellationTokenSource();var token=recognitionCancel.Token;
            recognitionMessage=WindowText("Reading track network… Existing diagrams remain unchanged.","Gleisnetz wird erkannt … Vorhandene Tafeln bleiben unverändert.");
            bool nearby=recognitionNearby;double radius=1000;
            if(draw&&nearby&&(!double.TryParse(recognitionRadius,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out radius)||radius<100||radius>10000))throw new InvalidOperationException(WindowText("Drawing radius must be 100–10000 metres.","Zeichenradius muss 100–10000 Meter betragen."));
            recognitionTask=Task.Run(()=>DetectionNetwork.Complete(input,draw&&nearby?NetworkScope.DrawNearby(input,radius,token):NetworkPlan.Build(input,token,draw),token),token);RefreshWorkspaceFields();
        }
        void CancelRecognition() {
            recognitionCancel?.Cancel();recognitionCancel?.Dispose();recognitionCancel=null;recognitionTask=null;recognitionProposal=null;
            if(manager?.GetCustomWindow(RecognitionPreviewWindowId)!=null)manager.CloseWindow(RecognitionPreviewWindowId);
        }
        void UpdateRecognition() {
            if(recognitionTask==null||!recognitionTask.IsCompleted)return;
            var task=recognitionTask;recognitionTask=null;recognitionCancel?.Dispose();recognitionCancel=null;
            try {
                var result=task.GetAwaiter().GetResult();
                if(scope!=recognitionWorld||Entry.Shared.ConfigurationVersion!=recognitionVersion||NetworkRecognition.Signature(CaptureRecognitionNetwork())!=recognitionSignature)
                    throw new InvalidOperationException(WindowText("The installation changed during recognition. Run it again.","Die Anlage wurde während der Erkennung geändert. Erneut erkennen lassen."));
                recognitionProposal=result;
                int signals=result.Panel.Tiles.Count(t=>t.Kind==PanelTileKind.Signal),points=result.Blocks.Values.Where(b=>b.Point!=null).Select(b=>b.Point).Distinct().Count();
                recognitionMessage=WindowText("Recognition ready: ","Erkennung bereit: ")+result.Segments+WindowText(" track segments, "," Gleissegmente, ")+points+WindowText(" points, "," Weichen, ")+result.Blocks.Count+WindowText(" sections. Drawn signals: "," Abschnitte. Gezeichnete Signale: ")+signals;
                if(result.ScopeSegments>0)recognitionMessage+=" · "+result.ScopeSegments+WindowText(" segments in drawing scope"," Segmente im Zeichenbereich");
            }catch(OperationCanceledException){recognitionMessage=WindowText("Recognition cancelled.","Erkennung abgebrochen.");}
            catch(Exception e){recognitionMessage=e.Message;}
            RefreshWorkspaceFields();
        }
        void BuildRecognition(UIPanelBuilder c) {
            BuildDiagramRegistration(c);
            c.AddSection(WindowText("Network catalogue and new diagram proposal","Netzkatalog und neuer Gleisbildvorschlag"));
            var masters=desks.Desks.Where(d=>d.IsMaster).ToArray();
            if(!masters.Any(d=>d.Id==recognitionTarget))recognitionTarget=CurrentDesk?.IsMaster==true?CurrentDesk.Id:masters[0].Id;
            c.AddSearchChoice(WindowText("Target master interlocking","Ziel-ESTW"),masters.Select(d=>d.Code+" · "+d.Name).ToList(),Array.FindIndex(masters,d=>d.Id==recognitionTarget),i=>{CancelRecognition();recognitionTarget=masters[i].Id;RefreshWorkspaceFields();});
            c.AddLabel(WindowText("Works with your existing save. Reading the network grants no movement authority and does not enable detection where none is configured.","Funktioniert im bestehenden Spielstand. Die Erkennung gibt keine Fahrt frei und aktiviert keine fehlende Gleisfreimeldung."));
            c.AddField(WindowText("Recognition status","Erkennungsstatus"),()=>recognitionMessage,UIPanelBuilder.Frequency.Periodic);
            if(recognitionTask!=null){c.AddButton(WindowText("Cancel recognition","Erkennung abbrechen"),()=>{CancelRecognition();recognitionMessage=WindowText("Recognition cancelled.","Erkennung abgebrochen.");RefreshWorkspaceFields();});return;}
            c.AddButton(WindowText("Read network / update sections","Netz erfassen / Abschnitte aktualisieren"),()=>Act(()=>StartRecognition(false)));
            c.AddFieldToggle(WindowText("Draw near placed signals","In der Nähe platzierter Signale zeichnen"),()=>recognitionNearby,v=>{recognitionNearby=v;recognitionProposal=null;},true);
            c.AddField(WindowText("Drawing radius along track (m)","Zeichenradius entlang der Gleise (m)"),c.AddInputField(recognitionRadius,v=>{if(v!=recognitionRadius){recognitionRadius=v;recognitionProposal=null;}},"1000",6));
            c.AddLabel(WindowText("Drawing scope changes the sketch only. Detection data still covers the captured network. The entire network can be drawn by disabling the scope; large maps need substantial space.","Der Zeichenbereich begrenzt nur die Skizze. Die erfassten Anlagendaten bleiben vollständig. Ohne Begrenzung wird das ganze Netz gezeichnet; große Karten brauchen dafür viel Platz."));
            c.AddButton(WindowText("Generate a new diagram proposal","Neues Gleisbild vorschlagen"),()=>Act(()=>StartRecognition(true)));
            var proposal=recognitionProposal;if(proposal==null)return;
            if(recognitionMissingBoxes.Length>0)c.AddLabel(WindowText("Signals without an active detection box: ","Signale ohne aktiven GFM-Kasten: ")+recognitionMissingBoxes.Length+" · "+string.Join(", ",recognitionMissingBoxes.Take(12).Select(Code)));
            var pointsWithoutDetection=proposal.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.Switch&&!PointCircuitVisibility.Enabled(desks,t.Reference)).Select(t=>t.Reference).Distinct().ToArray();
            if(pointsWithoutDetection.Length>0)c.AddLabel(WindowText("Drawn points without active detection: ","Gezeichnete Weichen ohne aktive GFM: ")+pointsWithoutDetection.Length+" · "+string.Join(", ",pointsWithoutDetection.Take(12).Select(id=>PointAssignments.Caption(desks,id))));
            string target=recognitionTarget;
            if(recognitionDrawing) {
                c.AddLabel(WindowText("The previous diagram and linked modules are kept in an archive view when applied. Physical signals, local diagrams and their IDs are retained.","Beim Übernehmen bleiben die bisherige Zeichnung und verknüpfte Module in einer Archivansicht erhalten. Weltobjekte, örtliche Tafeln und Kennungen bleiben bestehen."));
                c.AddButton(WindowText("Show read-only diagram preview","Schreibgeschützte Gleisbildvorschau öffnen"),()=>Act(OpenRecognitionPreview));
                if(proposal.Missing.Count>0)c.AddLabel(WindowText("Incomplete proposal; inspect missing items before applying: ","Unvollständiger Vorschlag; fehlende Elemente vor Übernahme prüfen: ")+string.Join(", ",proposal.Missing.Take(12)));
            }
            c.AddButton(recognitionDrawing?WindowText("Apply diagram proposal","Gleisbildvorschlag übernehmen"):WindowText("Apply detected sections","Erkannte Abschnitte übernehmen"),()=>Act(()=>{
                RequirePanelEdit();RequireIdleAdministration();
                if(!ReferenceEquals(proposal,recognitionProposal)||target!=recognitionTarget||scope!=recognitionWorld||Entry.Shared.ConfigurationVersion!=recognitionVersion||NetworkRecognition.Signature(CaptureRecognitionNetwork())!=recognitionSignature)
                    throw new InvalidOperationException(WindowText("Recognition is outdated. Run it again before applying.","Erkennung ist veraltet. Vor dem Übernehmen erneut ausführen."));
                if(recognitionDrawing&&(proposal.Drawn!=proposal.Chains||proposal.Missing.Count>0))throw new InvalidOperationException(WindowText("Not all track connections or signals could be drawn. The previous diagram is retained.","Nicht alle Gleisverbindungen oder Signale konnten gezeichnet werden. Die bisherige Tafel bleibt erhalten."));
                var next=NetworkRecognition.Apply(desks,target,NetworkRecognition.PreserveCircuitIds(desks,proposal,CanonicalBoundaryId),recognitionDrawing);
                Entry.Shared.CheckpointInstallation();deskStore.Save(next);desks=next;
                SelectDesk(target,false);panelPage=10;recognitionTarget=target;
                networkSignature=NetworkSignature(CaptureNetwork());recognitionProposal=null;recognitionMessage=WindowText("Recognition applied. Previous configuration remains externally backed up.","Erkennung übernommen. Der vorherige Stand bleibt extern gesichert.");
                panelReadTime=-100;nextPanelRead=0;Rebuild();
            }));
        }
        void RequireIdleAdministration() {
            if(routeWork.Count>0||engine.Active.Count>0||directionBlocks.Leases.Count>0||lineDirections.HasHolds||automaticRequests.Any(r=>r.Stage==AutomaticRequestStage.Waiting||r.Stage==AutomaticRequestStage.Submitted))
                throw new InvalidOperationException(WindowText("Release all routes and direction locks and withdraw pending automatic requests before changing installation diagrams.","Vor Änderungen an den Anlagentafeln alle Fahrstraßen und Richtungsbindungen vollständig auflösen und wartende Automatikanforderungen zurücknehmen."));
        }
    }
}
