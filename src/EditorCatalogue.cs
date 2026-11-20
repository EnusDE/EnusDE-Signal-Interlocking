using System;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        DeskRemoval pendingDeskRemoval;
        long pendingDeskVersion;
        string catalogueCode,catalogueName;
        bool catalogueMaster;

        void RequireEditorCatalogue() {
            RequireEdit();
            if(!editingWorkspace||!PanelVisible||panelPage!=4||desks==null)
                throw new InvalidOperationException(WindowText("Open the interlocking selection in the editor.","Stellwerksauswahl im Editor öffnen."));
        }

        void BuildEditorCatalogue(UIPanelBuilder c) {
            if(!WorkspaceEditable)return;
            c.AddLabel(WindowText("Open a diagram to edit it, or use Manage for its name and deletion.","Tafel zum Bearbeiten öffnen oder über Verwalten umbenennen und löschen."));
            if(HasPendingSignal)c.AddLabel(LocalText.Get("Signal ")+Code(pendingSignalId)+" vorgemerkt. Zielstellwerk auswählen.");
            if(assigningPoint)c.AddLabel(WindowText("Switch selected. Choose its target interlocking.","Weiche vorgemerkt. Zielstellwerk auswählen."));
            foreach(var desk in desks.Desks.OrderByDescending(d=>d.IsMaster).ThenBy(d=>d.Code,StringComparer.OrdinalIgnoreCase)) {
                var id=desk.Id;
                c.HStack(row=>{
                    row.AddButton((desk.IsMaster?"ESTW · ":"")+desk.Code+" · "+desk.Name,()=>Act(()=>EnterEditorDesk(id)));
                    row.AddButton(WindowText("Manage","Verwalten"),()=>Act(()=>OpenCatalogueForm(id)));
                    if(desk.IsMaster)row.AddButton(WindowText("Modules","Module"),()=>Act(()=>{EnterEditorDesk(id);OpenMasterLayout();}));
                    else row.AddButton(WindowText("Lines / blocks","Strecken / Block"),()=>Act(()=>{EnterEditorDesk(id);OpenFreeLines();}));
                });
            }
            c.AddButton(WindowText("New local interlocking","Neues örtliches Stellwerk"),()=>Act(()=>OpenCatalogueForm("")));
            c.AddButton(WindowText("New master interlocking","Neues übergeordnetes Stellwerk"),()=>Act(()=>OpenCatalogueForm("",true)));
            c.AddButton(WindowText("Automatic recognition…","Automatische Erkennung …"),()=>Act(OpenRecognition));
        }

        void OpenCatalogueForm(string id,bool master=false) {
            RequireEditorCatalogue();
            var desk=desks.Desks.SingleOrDefault(d=>d.Id==id);
            if(id!=""&&desk==null)return;
            editorCatalogueForm=id;
            catalogueMaster=desk?.IsMaster??master;
            catalogueCode=desk?.Code??"";
            catalogueName=desk?.Name??"";
            OpenEditorToolsWindow();
        }

        void BuildCatalogueForm(UIPanelBuilder c,string id) {
            var desk=desks.Desks.SingleOrDefault(d=>d.Id==id);
            if(id!=""&&desk==null)return;
            c.AddSection(id==""?(catalogueMaster?WindowText("New master interlocking","Neues übergeordnetes Stellwerk"):WindowText("New local interlocking","Neues örtliches Stellwerk")):desk.Code+" · "+desk.Name);
            c.AddField(WindowText("Code","Kennung"),c.AddInputField(catalogueCode,v=>{if(editorCatalogueForm==id)catalogueCode=v;},WindowText("Automatic if blank","Leer: aus dem Namen"),24));
            if(id=="")c.AddField(WindowText("Generated code","Vorgeschlagenes Kürzel"),()=>RouteNaming.DeskCode(catalogueName,desks.Desks.Select(d=>d.Code)),UIPanelBuilder.Frequency.Periodic);
            c.AddField(WindowText("Name","Name"),c.AddInputField(catalogueName,v=>{if(editorCatalogueForm==id)catalogueName=v;},"",64));
            c.HStack(row=>{
                row.AddButton(id==""?WindowText("Create and open","Anlegen und öffnen"):WindowText("Save name","Name speichern"),()=>Act(()=>{
                    RequireEditorCatalogue();
                    if(editorCatalogueForm!=id)return;
                    if(id=="") {
                        desks=catalogueMaster?deskStore.CreateMaster(desks,catalogueCode,catalogueName):deskStore.CreateLocal(desks,catalogueCode,catalogueName,new PanelDocument{WorldKey=scope});
                        editorCatalogueForm=null;
                        CloseWorkspaceContext();
                        EnterEditorDesk(desks.Desks.Last().Id);
                    } else {
                        desks=deskStore.Rename(desks,id,catalogueCode,catalogueName);
                        panel=CurrentDesk.Panel;
                        editorCatalogueForm=null;
                        CloseWorkspaceContext();Rebuild(false);
                    }
                }));
                row.AddButton(WindowText("Back","Zurück"),()=>{if(editorCatalogueForm==id){editorCatalogueForm=null;CloseWorkspaceContext();}});
            });
            if(desk!=null)c.AddButton(WindowText("Remove interlocking…","Stellwerk entfernen …"),()=>Act(()=>{
                RequireEditorCatalogue();
                if(editorCatalogueForm!=id)return;
                pendingDeskRemoval=new DeskRemoval(desks,id);
                pendingDeskVersion=Entry.Shared.ConfigurationVersion;
                RefreshWorkspaceFields();
            }));
            if(desk!=null)c.AddButton(WindowText("Open diagram","Gleisbild bearbeiten"),()=>Act(()=>{editorCatalogueForm=null;CloseWorkspaceContext();EnterEditorDesk(id);}));
            if(desk!=null)BuildDeskOperatingEditor(c,id);
        }

        void BuildDeskRemoval(UIPanelBuilder c,DeskRemoval request) {
            c.AddSection(request.Name);
            if(desks.Desks.Single(d=>d.Id==request.Id).Global)c.AddLabel(WindowText("The shared installation is retained. Another master view will become the default, or an empty replacement will be created.","Die gemeinsame Anlage bleibt erhalten. Eine andere ESTW-Ansicht wird Standard; andernfalls entsteht eine leere Ersatzansicht."));
            int related=routes.Count(request.RelatesTo);
            if(request.LinkedLines>0)c.AddLabel(WindowText($"This also removes {request.LinkedLines} connected open-line definitions and clears their permission buttons. Placed world objects remain.",$"Dabei werden auch {request.LinkedLines} angebundene Streckendefinitionen entfernt und ihre Erlaubnisfelder gelöst. Platzierte Weltobjekte bleiben erhalten."));
            c.AddLabel(WindowText(
                $"Remove this diagram with {request.Fields} fields and {request.LinkedAreas} linked areas? {request.OwnedPoints} switch assignments will return to the ESTW. The {related} related route definitions and the placed signals remain.",
                $"Tafel mit {request.Fields} Feldern und {request.LinkedAreas} verknüpften Bereichen entfernen? {request.OwnedPoints} Weichenzuordnungen gehen an das ESTW zurück. Die {related} betroffenen Fahrstraßendefinitionen und die platzierten Signale bleiben erhalten."));
            c.HStack(row=>{
                if(engine.Active.Any(run=>request.RelatesTo(run.Route)))
                    row.AddLabel(WindowText("Release affected routes first.","Zuerst betroffene Fahrstraßen und Restverschlüsse auflösen."));
                else row.AddButton(WindowText("Remove diagram","Tafel entfernen"),()=>Act(()=>{
                    RequireEditorCatalogue();
                    RequireIdleAdministration();Entry.Shared.CheckpointInstallation();
                    if(!ReferenceEquals(pendingDeskRemoval,request))return;
                    if(pendingDeskVersion!=Entry.Shared.ConfigurationVersion) {
                        pendingDeskRemoval=null;RefreshWorkspaceFields();
                        throw new InvalidOperationException(WindowText("The configuration changed. Review the removal again.","Die Anlage wurde geändert. Entfernung erneut prüfen."));
                    }
                    CheckTargetEdit(desks.Desks.Single(d=>d.Id==request.Id).Panel,new PanelDocument(),
                        desks.Desks.Where(d=>d.Id!=request.Id).SelectMany(d=>d.Panel.Tiles));
                    var next=request.Apply(desks,engine.Active.Select(run=>run.Route));
                    deskStore.Save(next);
                    desks=next;
                    if(deskId==request.Id)SelectDesk(desks.Desks.Single(d=>d.Global).Id,false);
                    else panel=CurrentDesk.Panel;
                    panelPage=4;
                    pendingDeskRemoval=null;editorCatalogueForm=null;
                    CloseWorkspaceContext();Rebuild(false);
                }));
                row.AddButton(WindowText("Cancel","Abbrechen"),()=>{if(ReferenceEquals(pendingDeskRemoval,request)){pendingDeskRemoval=null;RefreshWorkspaceFields();}});
            });
        }
    }
}
