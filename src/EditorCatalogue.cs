using System;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        DeskRemoval pendingDeskRemoval;
        long pendingDeskVersion;
        string catalogueCode,catalogueName;

        void RequireEditorCatalogue() {
            RequireEdit();
            if(!editingWorkspace||!PanelVisible||panelPage!=4||desks==null)
                throw new InvalidOperationException(WindowText("Open the interlocking selection in the editor.","Stellwerksauswahl im Editor öffnen."));
        }

        void BuildEditorCatalogue(UIPanelBuilder c) {
            if(!WorkspaceEditable)return;
            if(pendingDeskRemoval!=null){BuildDeskRemoval(c,pendingDeskRemoval);return;}
            if(editorCatalogueForm!=null){BuildCatalogueForm(c,editorCatalogueForm);return;}
            if(HasPendingSignal)c.AddLabel("Signal "+Code(pendingSignalId)+" vorgemerkt. Zielstellwerk auswählen.");
            if(assigningPoint)c.AddLabel(WindowText("Switch selected. Choose its target interlocking.","Weiche vorgemerkt. Zielstellwerk auswählen."));
            foreach(var desk in desks.Desks.OrderByDescending(d=>d.Global).ThenBy(d=>d.Code,StringComparer.OrdinalIgnoreCase)) {
                var id=desk.Id;
                c.HStack(row=>{
                    row.AddButton((desk.Global?"ESTW · ":"")+desk.Code+" · "+desk.Name,()=>Act(()=>EnterEditorDesk(id)));
                    row.AddButton(WindowText("Manage","Verwalten"),()=>Act(()=>OpenCatalogueForm(id)));
                });
            }
            c.AddButton(WindowText("New local interlocking","Neues örtliches Stellwerk"),()=>Act(()=>OpenCatalogueForm("")));
        }

        void OpenCatalogueForm(string id) {
            RequireEditorCatalogue();
            var desk=desks.Desks.SingleOrDefault(d=>d.Id==id);
            if(id!=""&&desk==null)return;
            editorCatalogueForm=id;
            catalogueCode=desk?.Code??"";
            catalogueName=desk?.Name??"";
            RefreshWorkspaceFields();
        }

        void BuildCatalogueForm(UIPanelBuilder c,string id) {
            var desk=desks.Desks.SingleOrDefault(d=>d.Id==id);
            if(id!=""&&desk==null)return;
            c.AddSection(id==""?WindowText("New local interlocking","Neues örtliches Stellwerk"):desk.Code+" · "+desk.Name);
            c.AddField(WindowText("Code","Kennung"),c.AddInputField(catalogueCode,v=>{if(editorCatalogueForm==id)catalogueCode=v;},WindowText("Automatic if blank","Leer: aus dem Namen"),24));
            if(id=="")c.AddField(WindowText("Generated code","Vorgeschlagenes Kürzel"),()=>RouteNaming.DeskCode(catalogueName,desks.Desks.Select(d=>d.Code)),UIPanelBuilder.Frequency.Periodic);
            c.AddField(WindowText("Name","Name"),c.AddInputField(catalogueName,v=>{if(editorCatalogueForm==id)catalogueName=v;},"",64));
            c.HStack(row=>{
                row.AddButton(id==""?WindowText("Create and open","Anlegen und öffnen"):WindowText("Save name","Name speichern"),()=>Act(()=>{
                    RequireEditorCatalogue();
                    if(editorCatalogueForm!=id)return;
                    if(id=="") {
                        desks=deskStore.CreateLocal(desks,catalogueCode,catalogueName,new PanelDocument{WorldKey=scope});
                        editorCatalogueForm=null;
                        EnterEditorDesk(desks.Desks.Last().Id);
                    } else {
                        desks=deskStore.Rename(desks,id,catalogueCode,catalogueName);
                        panel=CurrentDesk.Panel;
                        editorCatalogueForm=null;
                        RefreshWorkspaceFields();
                    }
                }));
                row.AddButton(WindowText("Back","Zurück"),()=>{if(editorCatalogueForm==id){editorCatalogueForm=null;RefreshWorkspaceFields();}});
            });
            if(desk!=null&&!desk.Global)c.AddButton(WindowText("Remove interlocking…","Stellwerk entfernen …"),()=>Act(()=>{
                RequireEditorCatalogue();
                if(editorCatalogueForm!=id)return;
                pendingDeskRemoval=new DeskRemoval(desks,id);
                pendingDeskVersion=Entry.Shared.ConfigurationVersion;
                RefreshWorkspaceFields();
            }));
        }

        void BuildDeskRemoval(UIPanelBuilder c,DeskRemoval request) {
            c.AddSection(request.Name);
            int related=routes.Count(request.RelatesTo);
            c.AddLabel(WindowText(
                $"Remove this diagram with {request.Fields} fields and {request.LinkedAreas} linked areas? {request.OwnedPoints} switch assignments will return to the ESTW. The {related} related route definitions and the placed signals remain.",
                $"Tafel mit {request.Fields} Feldern und {request.LinkedAreas} verknüpften Bereichen entfernen? {request.OwnedPoints} Weichenzuordnungen gehen an das ESTW zurück. Die {related} betroffenen Fahrstraßendefinitionen und die platzierten Signale bleiben erhalten."));
            c.HStack(row=>{
                if(engine.Active.Any(run=>request.RelatesTo(run.Route)))
                    row.AddLabel(WindowText("Release affected routes first.","Zuerst betroffene Fahrstraßen und Restverschlüsse auflösen."));
                else row.AddButton(WindowText("Remove diagram","Tafel entfernen"),()=>Act(()=>{
                    RequireEditorCatalogue();
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
                    RefreshWorkspaceFields();
                }));
                row.AddButton(WindowText("Cancel","Abbrechen"),()=>{if(ReferenceEquals(pendingDeskRemoval,request)){pendingDeskRemoval=null;RefreshWorkspaceFields();}});
            });
        }
    }
}
