using System;
using System.Linq;
using System.Collections.Generic;
using Track;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string worldGfmA,worldGfmB,worldGfmPoint,worldGfmTarget,worldGfmDesk,worldGfmScope,worldGfmSignature;
        int worldGfmArm,worldGfmChoice;
        readonly List<TrackBlock> worldGfmPlans=new List<TrackBlock>();
        void OpenWorldGfmSetup(){RequirePanelEdit();worldGfmDesk=deskId;worldGfmScope=scope;worldGfmPlans.Clear();WorkspacePage(13);}
        void ResumeWorldGfmSetup() {
            if(scope!=worldGfmScope||!desks.Desks.Any(d=>d.Id==worldGfmDesk))throw new InvalidOperationException(WindowText("Interlocking changed. Reopen GFM setup.","Stellwerk gewechselt. GFM-Einrichtung erneut öffnen."));
            ShowWorkspace(true,4,false);EnterEditorDesk(worldGfmDesk);WorkspacePage(13);
        }
        void SelectWorldGfm(Action<string> accept)=>BeginWorldSignalPick(id=>{worldGfmPlans.Clear();accept(id);RefreshWorkspaceFields();},ResumeWorldGfmSetup,false,true);
        void BuildWorldGfmSetup(UIPanelBuilder c) {
            c.AddSection(WindowText("GFM · from boundary to boundary","GFM · von Grenze bis Grenze"));
            c.AddLabel(WindowText("Select real signal detectors or point-arm boxes in the world. Save the physical section independently of the schematic; unambiguous diagram links are added where possible. Missing diagram links do not delete the section.","Reale Signal-GFM oder Weichenast-Kästen in der Welt auswählen. Den physischen Abschnitt unabhängig vom Gleisbild speichern; eindeutige Gleisbildverbindungen werden nach Möglichkeit ergänzt. Fehlende Zeichnungszuordnungen löschen den Abschnitt nicht."));
            c.AddButton(WindowText("Select start and end consecutively in world…","Von und bis nacheinander in der Welt auswählen …"),()=>Act(()=>{worldGfmB=null;SelectWorldGfm(a=>{worldGfmA=a;SelectWorldGfm(b=>worldGfmB=b);});}));
            c.AddField(WindowText("From","Von"),worldGfmA==null?"—":BoundaryCaption(worldGfmA));
            c.AddButton(WindowText("Select start boundary in world…","Anfangsgrenze in der Welt auswählen …"),()=>Act(()=>SelectWorldGfm(id=>worldGfmA=id)));
            c.AddField(WindowText("To","Bis"),worldGfmB==null?"—":BoundaryCaption(worldGfmB));
            c.AddButton(WindowText("Select end boundary in world…","Endgrenze in der Welt auswählen …"),()=>Act(()=>SelectWorldGfm(id=>worldGfmB=id)));
            c.AddButton(WindowText("Find physical section","Physischen Abschnitt suchen"),()=>Act(()=>{
                RequirePanelEdit();var a=CircuitEndpoint(worldGfmA);var b=CircuitEndpoint(worldGfmB);
                if(!CircuitIdentity.Enabled(a)||!CircuitIdentity.Enabled(b)||CanonicalBoundaryId(worldGfmA)==CanonicalBoundaryId(worldGfmB))throw new InvalidOperationException(WindowText("Choose two different active boundaries. For a shared A/B box, use the point-arm link below.","Zwei verschiedene aktive Grenzen wählen. Für einen gemeinsamen A/B-Kasten die Weichenast-Zuordnung unten verwenden."));
                worldGfmPlans.Clear();worldGfmChoice=0;var input=CaptureNetwork();var detected=NetworkRecognition.PreserveCircuitIds(desks,DetectionNetwork.Build(input),CanonicalBoundaryId);
                string pair=TrackCircuit.Pair(CanonicalBoundaryId(worldGfmA),CanonicalBoundaryId(worldGfmB));
                foreach(var block in detected.Blocks.Values.Where(v=>v.IsCircuit&&TrackCircuit.Pair(CanonicalBoundaryId(v.BoundaryA),CanonicalBoundaryId(v.BoundaryB))==pair))worldGfmPlans.Add(block.Clone());
                if(worldGfmPlans.Count==0) {
                    var paths=TrackCircuit.Find(input,a,b,out bool limited);if(limited)throw new InvalidOperationException(WindowText("Section search limit reached. Use closer boundaries.","Suchgrenze erreicht. Näher beieinander liegende Grenzen wählen."));
                    foreach(var path in paths.Where(p=>p.Points.Count==0))worldGfmPlans.Add(new TrackBlock{Id="c:"+Guid.NewGuid().ToString("N"),Name=(BoundaryCaption(worldGfmA)+" ↔ "+BoundaryCaption(worldGfmB)),BoundaryA=worldGfmA,BoundaryB=worldGfmB,Path=path.Sections});
                }
                worldGfmSignature=NetworkRecognition.Signature(input);status=worldGfmPlans.Count==0?WindowText("No complete section found. Check direction, intermediate boundaries and point arms.","Keinen vollständigen Abschnitt gefunden. Richtung, dazwischenliegende Grenzen und Weichenäste prüfen."):WindowText("Review the section, then save it.","Abschnitt prüfen und anschließend speichern.");RefreshWorkspaceFields();
            }));
            if(worldGfmPlans.Count>0) {
                c.AddSearchChoice(WindowText("Physical section","Physischer Abschnitt"),worldGfmPlans.Select(p=>p.Name+" · "+SignUnits.Number(p.Path.Sum(s=>s.Section.To-s.Section.From))+" m").ToList(),worldGfmChoice,i=>worldGfmChoice=i);
                var choice=worldGfmPlans[worldGfmChoice];
                c.AddButton(WindowText("Save boundary-to-boundary section","Abschnitt von Grenze bis Grenze speichern"),()=>Act(()=>{
                    RequirePanelEdit();RequireIdleAdministration();var input=CaptureNetwork();
                    if(worldGfmDesk!=deskId||worldGfmScope!=scope||!worldGfmPlans.Contains(choice)||NetworkRecognition.Signature(input)!=worldGfmSignature)throw new InvalidOperationException(WindowText("Section proposal changed. Search again.","Abschnittsvorschlag geändert. Erneut suchen."));
                    string error=CircuitPathValidation.Error(input,choice,CircuitEndpoint(choice.BoundaryA),CircuitEndpoint(choice.BoundaryB));if(error!=null)throw new InvalidOperationException(error);
                    Entry.Shared.CheckpointInstallation();SaveAutomation(d=>{d.Blocks[choice.Id]=choice.Clone();var net=new NetworkResult();net.Blocks[choice.Id]=choice.Clone();var linked=ExistingDiagramDetection.Build(d,deskId,net,DiagramBoundary,SharedPointCircuits().PointSignals,input);d.Blocks=linked.Library.Blocks;d.Desks=linked.Library.Desks;});
                    worldGfmPlans.Clear();panelView?.ReplaceDocument(panel);panelReadTime=-100;nextPanelRead=0;status=WindowText("Physical GFM section saved. Use GFM inspection to check schematic assignment.","Physischer GFM-Abschnitt gespeichert. Gleisbildzuordnung mit der GFM-Prüfung kontrollieren.");RefreshWorkspaceFields();
                }));
            }
            c.AddSection(WindowText("Assign a signal / GFM to a point arm","Signal / GFM einem Weichenast zuordnen"));
            c.AddLabel(WindowText("Choose the actual common, normal or reverse arm. The selected detector must lie on that native track segment. The point's monitored arm extends to it; an A signal detector becomes the shared A/B boundary. C boundaries cannot be combined this way.","Realen Stamm-, Grund- oder Abzweigast wählen. Der gewählte Melder muss auf dessen realem Gleissegment liegen. Der überwachte Weichenast reicht dann bis dorthin; ein A-Melder am Signal wird zur gemeinsamen A/B-Grenze. C-Grenzen lassen sich so nicht zusammenlegen."));
            c.AddButton(WindowText("Select point in world…","Weiche in der Welt auswählen …"),()=>Act(()=>BeginWorldPointSelection(id=>{worldGfmPoint=id;RefreshWorkspaceFields();},ResumeWorldGfmSetup)));
            var points=desks.Points.Keys.Concat(worldGfmPoint==null?Array.Empty<string>():new[]{worldGfmPoint}).Distinct().OrderBy(id=>PointAssignments.Caption(desks,id)).ToArray();
            if(points.Length>0) {
                if(!points.Contains(worldGfmPoint))worldGfmPoint=points[0];
                c.AddSearchChoice(WindowText("Point","Weiche"),points.Select(id=>PointAssignments.Caption(desks,id)).ToList(),Array.IndexOf(points,worldGfmPoint),i=>{worldGfmPoint=points[i];RefreshWorkspaceFields();});
                c.AddSearchChoice(WindowText("Native arm","Realer Weichenast"),new List<string>{WindowText("Common / tip","Stammgleis / Spitze"),WindowText("Normal arm","Grundast"),WindowText("Reverse arm","Abzweigast")},worldGfmArm,i=>{worldGfmArm=i;RefreshWorkspaceFields();});
                if(desks.Points.TryGetValue(worldGfmPoint,out var existing)&&existing.BoundaryLinks.TryGetValue(worldGfmArm,out var linked))c.AddLabel(WindowText("Current boundary: ","Aktuelle Grenze: ")+BoundaryCaption(linked));
                c.AddField(WindowText("Selected signal / GFM","Gewähltes Signal / GFM"),worldGfmTarget==null?"—":BoundaryCaption(worldGfmTarget));
                c.AddButton(WindowText("Select signal detector / GFM in world…","Signal-GFM / GFM in der Welt auswählen …"),()=>Act(()=>SelectWorldGfm(id=>worldGfmTarget=id)));
                c.AddButton(WindowText("Apply shared point boundary","Gemeinsame Weichengrenze übernehmen"),()=>Act(()=>SaveWorldPointBoundary(false)));
                c.AddButton(WindowText("Remove this manual point-boundary link","Diese manuelle Weichen-Grenzverknüpfung lösen"),()=>Act(()=>SaveWorldPointBoundary(true)));
            }
            c.AddButton(WindowText("Back to GFM detection","Zur GFM-Erkennung"),OpenExistingDetection);
        }
        void SaveWorldPointBoundary(bool remove) {
            RequirePanelEdit();RequireIdleAdministration();if(scope!=worldGfmScope||deskId!=worldGfmDesk)throw new InvalidOperationException(WindowText("Interlocking changed. Reopen GFM setup.","Stellwerk gewechselt. GFM-Einrichtung erneut öffnen."));
            if(remove){Entry.Shared.CheckpointInstallation();SaveAutomation(d=>{if(d.Points.TryGetValue(worldGfmPoint,out var point))point.BoundaryLinks.Remove(worldGfmArm);});}
            else {
                string target=CanonicalBoundaryId(worldGfmTarget);if(target==null||TrackCircuit.TryPointId(target,out _,out _))throw new InvalidOperationException(WindowText("Choose a physical signal detector or B box.","Einen physischen Signalmelder oder B-Kasten wählen."));
                var graph=Graph.Shared;var node=graph.GetNode(worldGfmPoint);if(node==null||!graph.DecodeSwitchAt(node,out var common,out var normal,out var branch))throw new InvalidOperationException(WindowText("Point geometry unavailable.","Weichengeometrie nicht verfügbar."));
                var block=desks.Blocks.TryGetValue("p:"+worldGfmPoint,out var guard)?guard:NetworkPlan.Build(CaptureNetwork(),draw:false).Blocks["p:"+worldGfmPoint];
                var links=desks.Points.TryGetValue(worldGfmPoint,out var current)?new Dictionary<int,string>(current.BoundaryLinks):new Dictionary<int,string>();links[worldGfmArm]=target;
                var edges=new[]{common,normal,branch};PointBoundaryBinding.Apply(block,edges.Select(e=>e.id).ToArray(),edges.Select(e=>e.a==node).ToArray(),edges.Select(e=>(double)e.GetLength()).ToArray(),links,id=>physicalBoundaries.TryGetValue(id,out var end)?end:null);
                Entry.Shared.CheckpointInstallation();SaveAutomation(d=>{PointAssignments.NameDiscovered(d,new[]{worldGfmPoint});d.Points[worldGfmPoint].ShowCircuit=true;d.Points[worldGfmPoint].BoundaryLinks=links;d.Blocks[block.Id]=block.Clone();string raw=TrackCircuit.PointId(worldGfmPoint,worldGfmArm);if(d.PointBoxes.TryGetValue(raw,out var box))box.Variant=2;});
            }
            worldGfmPlans.Clear();panelReadTime=-100;nextPanelRead=0;status=WindowText("Point boundary updated. Detect adjoining GFM sections again.","Weichengrenze aktualisiert. Angrenzende GFM-Abschnitte erneut erkennen.");RefreshWorkspaceFields();
        }
    }
}
