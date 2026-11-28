using System;
using System.Linq;
using System.Collections.Generic;
using UI;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        RectTransform masterLayoutRoot;
        PanelView masterLayoutView;ScrollRect masterLayoutScroll;
        PopupMenu masterLayoutPopup;
        string masterLayoutDesk,masterLayoutModule,masterLayoutMessage;
        bool masterLayoutFit=true;float masterLayoutZoom=1;Vector2 masterLayoutPosition=new Vector2(0,1);
        void OpenMasterLayout() {
            RequireEdit();if(!editingWorkspace)throw new InvalidOperationException(WindowText("Open the editor first.","Zuerst den Editor öffnen."));
            masterLayoutDesk=CurrentDesk?.IsMaster==true?CurrentDesk.Id:desks.Desks.Single(d=>d.Global).Id;masterLayoutFit=true;masterLayoutMessage=null;
            Mode(16);
        }
        void ReleaseMasterLayoutSurface() {
            masterLayoutPopup?.Dismiss();masterLayoutPopup=null;
            if(masterLayoutRoot!=null){masterLayoutRoot.gameObject.SetActive(false);Destroy(masterLayoutRoot.gameObject);}
            masterLayoutRoot=null;masterLayoutView=null;masterLayoutScroll=null;
        }
        void BuildMasterLayout(UIPanelBuilder builder) {
            ReleaseWorkspaceCanvas();
            var master=desks?.Desks.FirstOrDefault(d=>d.IsMaster&&d.Id==masterLayoutDesk);
            if(master==null||!editingWorkspace){builder.AddLabel(WindowText("Reopen the module view from the editor.","Modultafel erneut aus dem Editor öffnen."));return;}
            string target=master.Id,worldScope=scope;long version=Entry.Shared.ConfigurationVersion;
            var window=builder.VStack(_=>{}).GetComponentInParent<Window>();
            var root=masterLayoutRoot=WorkspaceSurface.Rect(window.transform,"Master module layout");CoverGameViewport(window,root);root.SetAsLastSibling();
            bool Alive()=>editingWorkspace&&scope==worldScope&&root==masterLayoutRoot&&root.gameObject.activeInHierarchy&&target==masterLayoutDesk;
            void Check(){RequireEdit();if(!Alive()||Entry.Shared.ConfigurationVersion!=version)throw new InvalidOperationException(WindowText("The module layout changed. Refresh this view before editing.","Modulanordnung wurde geändert. Vor dem Bearbeiten die Ansicht aktualisieren."));}
            void Refresh(){if(masterLayoutView!=null)masterLayoutZoom=masterLayoutView.Zoom;if(masterLayoutScroll!=null)masterLayoutPosition=masterLayoutScroll.normalizedPosition;CustomWindowHandle.Create(WindowId)?.SetNeedsRebuild();}
            void Edit(string id)=>Act(()=>{Check();OpenEditorCatalogue();EnterEditorDesk(id);});
            void Unlink(string id)=>Act(()=>{Check();SaveAutomation(d=>d.Modules.RemoveAll(m=>m.Desk==id&&MasterDiagram.Owner(d,m)==target));masterLayoutMessage=WindowText("Module unlinked; its local diagram is retained.","Modul gelöst; das örtliche Gleisbild bleibt erhalten.");Refresh();});
            var surface=root.gameObject.AddComponent<MasterLayoutSurface>();
            surface.Initialize(root,master.Code+" · "+master.Name,WindowText("Drag modules to move them. Right-click a module to edit it. Drag empty space to pan.","Module zum Verschieben ziehen. Rechtsklick auf ein Modul zum Bearbeiten. Leere Fläche ziehen verschiebt die Ansicht."));
            BuildWorkspaceCommands(surface.Header,true);
            surface.Notice=()=>workspaceActionError??masterLayoutMessage??WindowText("Select or insert a local interlocking.","Örtliches Stellwerk auswählen oder einfügen.");
            var controls=builder.VScrollView(c=>{
                c.AddSection(WindowText("Local interlocking","Örtliches Stellwerk"));
                var locals=desks.Desks.Where(d=>!d.IsMaster).OrderBy(d=>d.Code).ToArray();
                if(locals.Length==0){c.AddLabel(WindowText("Create a local interlocking first.","Zuerst ein örtliches Stellwerk anlegen."));return;}
                int index=Math.Max(0,Array.FindIndex(locals,d=>d.Id==masterLayoutModule));masterLayoutModule=locals[index].Id;string local=masterLayoutModule;
                c.AddSearchChoice(WindowText("Selection","Auswahl"),locals.Select(d=>d.Code+" · "+d.Name).ToList(),index,i=>{if(!Alive())return;masterLayoutModule=locals[i].Id;Refresh();});
                bool linked=desks.Modules.Any(m=>m.Desk==local&&MasterDiagram.Owner(desks,m)==target);
                if(!linked)c.AddButton(WindowText("Insert module","Modul einfügen"),()=>Act(()=>{Check();SaveAutomation(d=>d.Modules.Add(MasterDiagram.Suggest(d,local,target)));masterLayoutFit=true;Refresh();}));
                else c.AddLabel(WindowText("Linked · drag in diagram to move","Eingebunden · im Gleisbild ziehen"));
                c.AddButton(WindowText("Edit local diagram","Örtliches Gleisbild bearbeiten"),()=>Edit(local));
                if(linked)c.AddButton(WindowText("Unlink module","Modul lösen"),()=>Unlink(local));
                c.AddButton(WindowText("Refresh module view","Modultafel aktualisieren"),Refresh);
                c.AddButton(WindowText("Back to the editor diagram","Zurück zur Editortafel"),()=>WorkspacePage(8));
            });
            controls.SetParent(surface.Sidebar,false);WorkspaceSurface.Fill(controls,8,8,8,8);(controls.GetComponent<LayoutElement>()??controls.gameObject.AddComponent<LayoutElement>()).ignoreLayout=true;surface.ThemeSoon();
            var document=MasterDiagram.Project(desks,target,true);surface.Document=document;
            var viewport=WorkspaceSurface.Rect(surface.Frame,"Module viewport");WorkspaceSurface.Fill(viewport,0,0,12,12);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=masterLayoutScroll=surface.Frame.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=scroll.vertical=true;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var drawing=WorkspaceSurface.Rect(viewport,"Linked module diagram");var view=masterLayoutView=drawing.gameObject.AddComponent<PanelView>();
            DeskModule Hit(int x,int y)=>MasterDiagram.At(desks,target,x,y);
            PanelRegion Selection(){var module=desks.Modules.FirstOrDefault(m=>m.Desk==masterLayoutModule&&MasterDiagram.Owner(desks,m)==target);if(module==null)return null;var doc=desks.Desks.Single(d=>d.Id==module.Desk).Panel;return new PanelRegion{X=module.X,Y=module.Y,Width=doc.Columns,Height=doc.Rows};}
            PanelVisual Read(int x,int y){var tile=document.At(x,y);var signal=tile==null?null:Object(tile.Reference);return new PanelVisual{Rail=Color.gray,Lamp=Color.red,Unknown=true,Caption=tile?.Kind==PanelTileKind.Signal?signal?.DisplayTitle??tile.Reference:tile?.Kind==PanelTileKind.Switch?PointAssignments.Caption(desks,tile.Reference):tile?.Label??"",ShuntOnly=signal?.Kind==MarkerRecord.Shunt,Distant=signal?.Kind==MarkerRecord.Distant};}
            void Context(int x,int y,bool half) {
                var module=Hit(x,y);if(module==null||!Alive())return;masterLayoutModule=module.Desk;view.Refresh();masterLayoutPopup?.Dismiss();
                var canvas=root.GetComponentInParent<Canvas>()?.rootCanvas;if(canvas==null)return;string id=module.Desk;
                masterLayoutPopup=PopupMenu.PresentAt(canvas,Mouse.current?.position.ReadValue()??new Vector2(Screen.width/2f,Screen.height/2f),new List<MenuItem>{
                    MenuItem.Action(WindowText("Edit local diagram","Örtliches Gleisbild bearbeiten"),()=>Edit(id)),MenuItem.Action(WindowText("Unlink module","Modul lösen"),()=>Unlink(id))},"enusde.master.module");
            }
            view.Initialize(document,Read,(x,y,half)=>{var module=Hit(x,y);if(module!=null&&Alive()){masterLayoutModule=module.Desk;Refresh();}},Alive,Context,v=>view.SetZoom(Mathf.Clamp(v,.025f,2)),()=>null,Selection,true);
            MasterLinkGraphic.Attach(view,desks,target,true);
            scroll.content=drawing;view.SetZoom(masterLayoutZoom);view.AttachScroll(scroll);scroll.horizontalScrollbar=WorkspaceBar(surface.Frame,false);scroll.verticalScrollbar=WorkspaceBar(surface.Frame,true);
            surface.View=view;surface.Scroll=scroll;surface.RestoreView=()=>{if(Alive())scroll.normalizedPosition=masterLayoutPosition;};surface.SetInitialView(masterLayoutFit);masterLayoutFit=false;
            var drag=drawing.gameObject.AddComponent<MasterModuleDrag>();drag.View=view;drag.Scroll=scroll;drag.Find=Hit;drag.Alive=()=>Alive()&&Entry.Shared.ConfigurationVersion==version;drag.Local=id=>desks.Desks.FirstOrDefault(d=>d.Id==id)?.Panel;
            drag.Commit=(id,x,y)=>Act(()=>{Check();SaveAutomation(d=>MasterDiagram.Move(d,target,id,x,y));masterLayoutModule=id;masterLayoutMessage=WindowText("Module position applied.","Modulposition übernommen.");Refresh();});
        }
    }
}
