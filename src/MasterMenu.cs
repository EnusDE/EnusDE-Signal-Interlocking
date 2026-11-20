using System;
using System.Linq;
using UI.Builder;
using Game.State;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        PanelDocument DeskPanel(DeskDefinition desk)=>desk?.IsMaster==true&&!editingWorkspace?MasterDiagram.Project(desks,desk.Id):desk?.Panel;
        void BuildMasterMenu(UIPanelBuilder c) {
            c.AddSection(WindowText("Master interlocking modules","Übergeordnetes Stellwerk – Module"));
            if(editingWorkspace)c.AddButton(WindowText("Open visual module arrangement","Sichtbare Modultafel bearbeiten"),()=>Act(OpenMasterLayout));
            c.AddLabel(WindowText("Local diagrams remain linked. Edit their track layout locally; move the whole module here. The master diagram also includes its own free-line fields. All views operate the same routes and locks.","Ortsstellwerke bleiben verknüpft. Ihr Gleisbild wird örtlich bearbeitet; hier wird das ganze Modul verschoben. Eigene Streckenfelder des ESTW bleiben erhalten. Alle Ansichten bedienen dieselben Fahrstraßen und Verschlüsse."));
            string masterId=CurrentDesk?.IsMaster==true?CurrentDesk.Id:desks.Desks.Single(d=>d.Global).Id;
            foreach(var local in desks.Desks.Where(d=>!d.IsMaster&&(editingWorkspace||d.ShowInDispatcher)).OrderBy(d=>d.Code)) {
                string id=local.Id,selectedScope=scope;var module=desks.Modules.SingleOrDefault(m=>m.Desk==id&&MasterDiagram.Owner(desks,m)==masterId);
                c.AddSection(local.Code+" · "+local.Name);
                if(editingWorkspace) {
                    if(module==null)c.AddButton(WindowText("Add linked module","Verknüpftes Modul einfügen"),()=>Act(()=>{if(scope!=selectedScope)return;SaveAutomation(d=>d.Modules.Add(MasterDiagram.Suggest(d,id,masterId)));status=WindowText("Module added to the master diagram.","Modul in der ESTW-Tafel eingefügt.");RefreshWorkspaceFields();}));
                    else {
                        string x=module.X.ToString(),y=module.Y.ToString();
                        c.AddField(LocalText.Get("X"),c.AddInputField(x,v=>x=v,"0–2047",4));c.AddField(LocalText.Get("Y"),c.AddInputField(y,v=>y=v,"0–511",3));
                        c.AddButton(WindowText("Move module","Modul verschieben"),()=>Act(()=>{if(scope!=selectedScope)return;if(!int.TryParse(x,out int a)||!int.TryParse(y,out int b))throw new InvalidOperationException(WindowText("Enter integer coordinates.","Ganze Koordinaten eingeben."));SaveAutomation(d=>{var target=d.Modules.Single(m=>m.Desk==id&&MasterDiagram.Owner(desks,m)==masterId);target.X=a;target.Y=b;});RefreshWorkspaceFields();}));
                        c.AddButton(WindowText("Remove module from master view","Modul aus ESTW-Ansicht lösen"),()=>Act(()=>{if(scope!=selectedScope)return;SaveAutomation(d=>d.Modules.RemoveAll(m=>m.Desk==id&&MasterDiagram.Owner(desks,m)==masterId));RefreshWorkspaceFields();}));
                    }
                    c.AddButton(WindowText("Edit local diagram","Örtliches Gleisbild bearbeiten"),()=>Act(()=>EnterEditorDesk(id)));
                } else if(StateManager.IsHost) {
                    bool paused=Automation.PausedStations.Contains(id);
                    c.AddButton(paused?WindowText("Resume automatic operation","Selbststellbetrieb wieder einschalten"):WindowText("Take over: pause automatic operation","Übernehmen: Selbststellbetrieb pausieren"),()=>Act(()=>{
                        RequireDispatcher();if(scope!=selectedScope)throw new InvalidOperationException("Welt gewechselt.");
                        SaveAutomation(d=>{if(d.Automation.PausedStations.Contains(id))d.Automation.PausedStations.Remove(id);else d.Automation.PausedStations.Add(id);});
                        status=WindowText("Only new automatic requests are paused. Existing routes and block locks remain supervised.","Nur neue automatische Anforderungen werden pausiert. Bestehende Fahrstraßen und Blockverschlüsse bleiben überwacht.");RefreshWorkspaceFields();
                    }));
                }
                if(local.ShowInDispatcher||editingWorkspace)c.AddLabel((module==null?WindowText("No master module","Kein ESTW-Modul"):"X "+module.X+" / Y "+module.Y)+" · "+(Automation.PausedStations.Contains(id)?WindowText("automatic operation paused","Selbststellbetrieb pausiert"):WindowText("automatic operation permitted","Selbststellbetrieb zugelassen")));
            }
            c.AddButton(WindowText("Open line / GWB setup and operation","Freie Strecke / GWB einrichten und bedienen"),OpenFreeLines);
            c.AddSection(WindowText("Connections between interlockings","Anbindungen zwischen Stellwerken"));
            bool found=false;
            foreach(var line in desks.FreeLines) {
                var from=desks.Desks.FirstOrDefault(d=>d.Id==line.StationA);var to=desks.Desks.FirstOrDefault(d=>d.Id==line.StationB);
                if(from==null||to==null)continue;found=true;
                c.AddLabel(from.Code+" ↔ "+to.Code+" · "+line.Code+" · "+line.Track+" · "+(line.Enabled?WindowText("active","aktiv"):WindowText("disabled","deaktiviert")));
            }
            if(!found)c.AddLabel(WindowText("Configure an open-line connection between the local interlockings. No saved train-route definitions are required.","Eine Streckenverbindung zwischen den Ortsstellwerken einrichten. Gespeicherte Zugfahrstraßen sind dafür nicht erforderlich."));
            if(!editingWorkspace)c.AddButton(WindowText("Show master diagram","ESTW-Gesamttafel anzeigen"),()=>Act(()=>{SelectDesk(masterId);CloseWorkspaceContext();Mode(0);}));
        }
    }
}
