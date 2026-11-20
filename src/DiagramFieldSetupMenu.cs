using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Track;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        DiagramFieldTarget setupDiagramTarget;
        string setupDiagramDesk;
        int setupNewSignalKind;
        bool setupFocusPending,setupConstructionExpanded;
        readonly HashSet<(int,int)> setupSkippedFields=new HashSet<(int,int)>();
        bool DiagramSetupVisible=>editingWorkspace&&panelPage==15&&(setupTask==1||setupTask==3)&&setupScope==scope;
        void StartDiagramFieldSetup() {
            RequirePanelEdit();setupScope=scope;setupTask=1;setupDiagramTarget=null;setupConstructionExpanded=false;setupSkippedFields.Clear();setupDiagramDesk=CurrentDesk?.IsMaster==false?deskId:null;
            if(setupDiagramDesk!=null)ChooseNextSetupField(true);WorkspacePage(15);
        }
        void SelectDiagramSetupField(string desk,int x,int y) {
            RequirePanelEdit();var target=DiagramFieldTarget.Capture(desks,desk,x,y);
            if(deskId!=desk){SelectDesk(desk,false);WorkspacePage(15);}
            setupScope=scope;setupDiagramDesk=desk;setupDiagramTarget=target;setupConstructionExpanded=false;setupTask=1;selectedX=x;selectedY=y;region=null;tileDraft=null;
            setupFocusPending=true;status=WindowText("Green field selected. Now choose its real signal or point in the world.","Grünes Feld gewählt. Jetzt das zugehörige reale Signal oder die Weiche in der Welt auswählen.");
            panelView?.Refresh();RefreshWorkspaceFields();
        }
        void ChooseNextSetupField(bool preferSelected=false) {
            setupConstructionExpanded=false;setupDiagramTarget=null;
            var document=desks.Desks.FirstOrDefault(d=>d.Id==setupDiagramDesk&&!d.IsMaster)?.Panel;if(document==null)return;
            var fields=DiagramFieldSetup.Pending(document).Where(t=>!setupSkippedFields.Contains((t.X,t.Y))).ToArray();
            var chosen=preferSelected?fields.FirstOrDefault(t=>t.X==selectedX&&t.Y==selectedY)??fields.FirstOrDefault():fields.FirstOrDefault();
            if(chosen!=null)SelectDiagramSetupField(setupDiagramDesk,chosen.X,chosen.Y);
            else {setupFocusPending=false;panelView?.Refresh();RefreshWorkspaceFields();}
        }
        void ResumeDiagramFieldSetup(DiagramFieldTarget target) {
            if((setupTask!=1&&setupTask!=3)||scope!=target.World||!ReferenceEquals(target,setupDiagramTarget)||!desks.Desks.Exists(d=>d.Id==target.Desk&&!d.IsMaster))return;
            // ShowWorkspace enters the catalogue for editor callers; restore the local desk and assistant explicitly.
            ShowWorkspace(true,4,false);EnterEditorDesk(target.Desk);setupTask=1;WorkspacePage(15);selectedX=target.X;selectedY=target.Y;setupFocusPending=true;panelView?.Refresh();
        }
        void RequireDiagramTarget(DiagramFieldTarget target) {
            RequirePanelEdit();if(!DiagramSetupVisible||deskId!=target.Desk||scope!=target.World||!ReferenceEquals(target,setupDiagramTarget))throw new InvalidOperationException(WindowText("The selected setup field changed. Click it again.","Das gewählte Einrichtungsfeld wurde geändert. Erneut anklicken."));
            target.RequireCurrent(desks);
        }
        string SetupPointSignature(string id) {
            var graph=Graph.Shared;var node=graph?.GetNode(id);
            if(node==null||!graph.IsSwitch(node)||!world.SwitchPosition(id,out _))throw new InvalidOperationException(WindowText("The selected point is no longer available.","Die gewählte Weiche ist nicht mehr verfügbar."));
            graph.DecodeSwitchAt(node,out var common,out var normal,out var reverse);
            return JsonConvert.SerializeObject(new{id,x=node.Position.x,y=node.Position.y,z=node.Position.z,common=common?.id,normal=normal?.id,reverse=reverse?.id});
        }
        void AssignDiagramWorldObject(DiagramFieldTarget target,string id) {
            RequireDiagramTarget(target);RequireIdleAdministration();ReadSignalInventory();SetupObject source;MarkerRecord signal=null;
            if(target.Point) {
                SetupPointSignature(id);source=new SetupObject{Id=id,Point=true,Caption=desks.Points.TryGetValue(id,out var point)?point.Code:WindowText("Selected point","Gewählte Weiche")};
            } else {
                signal=Object(id);if(signal?.IsSignal!=true||signal.Planned)throw new InvalidOperationException(WindowText("Choose a completed world signal.","Ein fertig gebautes Weltsignal wählen."));
                source=new SetupObject{Id=id,Caption=signal.Name,Anchor=signal.Anchor.Clone()};
            }
            var next=DiagramFieldSetup.Assign(desks,target,source,signal);Entry.Shared.CheckpointInstallation();deskStore.Save(next);desks=next;panel=DeskPanel(CurrentDesk);panelView?.ReplaceDocument(panel);nativeDirty=true;
            setupSkippedFields.Remove((target.X,target.Y));ChooseNextSetupField();
            status=source.Caption+" → "+target.Cell+WindowText(" assigned."," zugeordnet.");
        }
        bool PressDiagramSetupField(int x,int y) {
            if(!DiagramSetupVisible)return false;
            if(!DiagramFieldSetup.Eligible(panel?.At(x,y))){status=WindowText("Click a green, unassigned signal or point field in this diagram.","Ein grünes, noch nicht zugeordnetes Signal- oder Weichenfeld dieser Tafel anklicken.");RefreshWorkspaceFields();return true;}
            SelectDiagramSetupField(deskId,x,y);return true;
        }
        void BuildDiagramFieldSetup(UIPanelBuilder c) {
            if(setupDiagramDesk==null||deskId!=setupDiagramDesk||CurrentDesk?.IsMaster!=false) {
                c.AddLabel(WindowText("Choose the local interlocking whose drawing you want to configure.","Das örtliche Stellwerk wählen, dessen Zeichnung eingerichtet werden soll."));
                foreach(var desk in desks.Desks.Where(d=>!d.IsMaster).OrderBy(d=>d.Code)){string id=desk.Id;c.AddButton(desk.Code+" · "+desk.Name,()=>Act(()=>{SelectDesk(id,false);setupDiagramDesk=id;setupSkippedFields.Clear();ChooseNextSetupField();WorkspacePage(15);}));}return;
            }
            var fields=DiagramFieldSetup.Pending(panel);var target=setupDiagramTarget;
            c.AddLabel(WindowText("Open fields in this diagram: ","Offene Felder in dieser Tafel: ")+fields.Length);
            if(target==null) {c.AddButton(WindowText("Recheck open fields","Offene Felder neu prüfen"),()=>Act(()=>{setupSkippedFields.Clear();ChooseNextSetupField(true);}));c.AddLabel(fields.Length==0?WindowText("All signal and point fields in this diagram are assigned. Inspect GFM before operation.","Alle Signal- und Weichenfelder dieser Tafel sind zugeordnet. Vor dem Betrieb GFM prüfen."):WindowText("Remaining fields were skipped. Click a green field or recheck the open fields.","Verbleibende Felder wurden übersprungen. Ein grünes Feld anklicken oder offene Felder neu prüfen."));return;}
            c.AddSection(WindowText("Current green field","Aktuelles grünes Feld")+" · "+target.Cell+" · "+(target.Point?WindowText("Point","Weiche"):WindowText("Signal","Signal")));
            if(!string.IsNullOrWhiteSpace(target.Snapshot?.Label))c.AddLabel(target.Snapshot.Label);
            c.AddLabel(WindowText("Select in the world, then click the matching object. The click saves this field immediately and marks the next one.","In der Welt auswählen und das passende Objekt anklicken. Der Klick speichert dieses Feld direkt und markiert das nächste."));
            if(!target.Current(desks)){c.AddLabel(WindowText("The drawing changed. Click the target field again.","Die Zeichnung wurde geändert. Das Zielfeld erneut anklicken."));return;}
            c.AddButton(target.Point?WindowText("Select this point in the world…","Diese Weiche in der Welt auswählen …"):WindowText("Select this signal in the world…","Dieses Signal in der Welt auswählen …"),()=>Act(()=>{
                RequireDiagramTarget(target);RequireIdleAdministration();setupConstructionExpanded=false;
                if(target.Point)BeginWorldPointSelection(id=>AssignDiagramWorldObject(target,id),()=>ResumeDiagramFieldSetup(target));
                else BeginWorldSignalPick(id=>AssignDiagramWorldObject(target,id),()=>ResumeDiagramFieldSetup(target),false);
            }));
            c.AddButton(WindowText("Skip this field for now","Dieses Feld vorerst überspringen"),()=>{if(!ReferenceEquals(target,setupDiagramTarget))return;setupSkippedFields.Add((target.X,target.Y));ChooseNextSetupField();});
            if(!target.Point)c.AddButton(WindowText("Signal missing? Place a new one…","Signal fehlt? Neues platzieren …"),()=>{setupConstructionExpanded=!setupConstructionExpanded;RefreshWorkspaceFields();});
            if(!target.Point&&setupConstructionExpanded) {
                var kinds=new[]{MarkerRecord.Main,MarkerRecord.Shunt,MarkerRecord.Distant};
                c.AddSearchChoice(WindowText("If the signal is missing: new signal type","Falls das Signal fehlt: neuer Signaltyp"),new[]{WindowText("Main signal","Hauptsignal"),WindowText("Shunting signal","Rangiersignal"),WindowText("Distant signal","Vorsignal")}.ToList(),setupNewSignalKind,i=>setupNewSignalKind=i);
                c.AddButton(WindowText("Place a new world signal for this field…","Neues Weltsignal für dieses Feld platzieren …"),()=>Act(()=>{
                    RequireDiagramTarget(target);setupConstructionExpanded=false;objects.BeginSetupPlacement(kinds[setupNewSignalKind],row=>Act(()=>{ResumeDiagramFieldSetup(target);AssignDiagramWorldObject(target,row.Id);}));CloseWorkspaceMode();
                }));
            }
            c.AddButton(WindowText("Show green field","Grünes Feld zeigen"),()=>{setupFocusPending=true;});
            c.AddButton(WindowText("Recheck open fields","Offene Felder neu prüfen"),()=>Act(()=>{setupSkippedFields.Clear();ChooseNextSetupField(true);}));
        }
        void ReadSetupFieldFocus() {
            if(!setupFocusPending||!DiagramSetupVisible||setupDiagramTarget==null||deskId!=setupDiagramTarget.Desk||!PanelVisible||panelView==null)return;
            if(panelView.RevealCell(setupDiagramTarget.X,setupDiagramTarget.Y))setupFocusPending=false;
        }
    }
}
