using System;
using System.Linq;
using System.Collections.Generic;
using UI;
using UI.Builder;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        PopupMenu editorPopup;
        long editorContextEpoch;
        bool editorDetailsOpen;
        EditorFieldCategory editorFieldCategory;
        void DismissEditorMenu(){editorContextEpoch++;var menu=editorPopup;editorPopup=null;menu?.Dismiss();}
        void ShowEditorCategories(int x,int y,RectTransform anchor=null) {
            RequirePanelEdit();DismissEditorMenu();long epoch=editorContextEpoch;string world=scope,desk=deskId;
            var field=x>=0&&y>=0?panel.At(x,y)?.Clone():null;
            bool Current()=>epoch==editorContextEpoch&&editingWorkspace&&PanelVisible&&scope==world&&deskId==desk&&!stopped&&WorkspaceEditable;
            Action Guard(Action action,bool fieldAction=false)=>()=>Act(()=>{
                if(!Current())return;
                if(fieldAction&&!EditorContextPolicy.SameTarget(panel,x,y,field))throw new InvalidOperationException(WindowText("The field changed. Open its menu again.","Das Feld wurde geändert. Menü erneut öffnen."));
                action();
            });
            var items=new List<MenuItem>();
            bool general=anchor!=null&&x<0;
            var blockArea=panel.Areas.FirstOrDefault(a=>a.BlockLine!=null&&a.Contains(x,y));
            if(blockArea!=null)items.Add(MenuItem.Action(WindowText("Edit block assembly view: ","Ansicht des Blockverbunds bearbeiten: ")+blockArea.Code,Guard(()=>OpenBlockViewEditor(blockArea.BlockLine,desk))));
            var categories=EditorContextPolicy.FieldCategories(field);
            if(categories.Length>0) {
                var fields=categories.Select(category=>MenuItem.Action(FieldCategoryCaption(category),Guard(()=>OpenEditorField(x,y,category),true))).ToList();
                fields.Add(MenuItem.Action(WindowText("Rotate field 90°","Feld um 90° drehen"),Guard(()=>QuickFieldTransform(x,y),true)));
                items.AddRange(fields);
                if(field.Kind!=PanelTileKind.Switch&&PanelConnections.Count(RailPorts(field))==2)items.Add(MenuItem.Action(WindowText("Select GFM in world…","GFM in der Welt auswählen …"),Guard(()=>BeginPanelBoundaryPick(x,y),true)));
                if(DiagramFieldSetup.Eligible(field))items.Add(MenuItem.Action(WindowText("Assign this field with assistant…","Dieses Feld mit Assistent zuordnen …"),Guard(()=>OpenSetupForSignalField(deskId,x,y),true)));
                if(field.Kind==PanelTileKind.Signal)items.Insert(0,MenuItem.Action(WindowText("Assign signal in world…","Signal in der Welt zuordnen …"),Guard(()=>BeginPanelSignalPick(x,y),true)));
                if(field.Kind==PanelTileKind.Signal)items.Add(MenuItem.Action(WindowText("Assign neighbour signal for display…","Nachbarsignal zur Anzeige zuordnen …"),Guard(()=>BeginPanelSignalPick(x,y,true),true)));
                if(field.Kind==PanelTileKind.LinePermission)items.Insert(0,MenuItem.Action(WindowText("Set up block / line connection","Block-/Streckenverbindung einrichten"),Guard(()=>OpenLineSetupForField(x,y),true)));
                if(field.Kind==PanelTileKind.Switch)
                    items.Insert(0,MenuItem.Action(WindowText("Assign in world…","In der Welt zuordnen …"),Guard(()=>BeginPointPick(x,y),true)));
                if(field.Kind==PanelTileKind.Switch&&assigningPoint)
                    items.Insert(0,MenuItem.Action(WindowText("Assign selected world point","Vorgemerkte Weltweiche zuordnen"),Guard(()=>AcceptPoint(x,y),true)));
                if(field.Kind==PanelTileKind.Switch&&!string.IsNullOrEmpty(field.Reference)&&desks.Points.TryGetValue(field.Reference,out var point)) {
                    bool enable=!point.ShowCircuit;
                    items.Insert(0,MenuItem.Action(enable?WindowText("Enable point detection boundaries","Weichen-GFM-Grenzen aktivieren"):WindowText("Disable point detection","Weichen-GFM deaktivieren"),Guard(()=>{
                        ConfigurePointCircuit(field.Reference,enable,enable);panelView?.Refresh();
                        status=enable?WindowText("Point detection enabled; missing boundaries are being detected.","Weichen-GFM aktiviert; fehlende Grenzen werden erkannt."):WindowText("Point detection disabled; this point is unmonitored.","Weichen-GFM deaktiviert; diese Weiche ist unüberwacht.");
                    },true)));
                }
            }
            // Field menus contain only actions for that field (or its selection).
            // Drawing tools are available on empty cells and in the general menu.
            if(field==null||general) {
            var add=new List<MenuItem>();
            var kinds=new[]{PanelTileKind.Signal,PanelTileKind.Switch,PanelTileKind.TrainNumber,PanelTileKind.AuxiliaryTarget,PanelTileKind.LinePermission,PanelTileKind.Label};
            foreach(var kind in kinds){var selected=kind;string caption=kind==PanelTileKind.LinePermission?WindowText("Line permission","Streckenerlaubnis"):LocalText.Get(ToolNames[(int)kind]);add.Add(MenuItem.Action(caption,Guard(()=>{panelPage=1;EditorMode(3);ChooseSymbol(selected);CloseWorkspaceContext();})));}
            items.Add(MenuItem.Submenu(WindowText("Draw and place","Zeichnen und platzieren"),new List<MenuItem>{
                MenuItem.Action(WindowText("Draw track","Gleis zeichnen"),Guard(()=>{SelectEditorTool(0);CloseWorkspaceContext();})),
                MenuItem.Submenu(WindowText("Add fields","Felder setzen"),add),
                MenuItem.Action(WindowText("Select / finish drawing","Auswählen / Zeichnen beenden"),Guard(()=>{SelectEditorTool(1);CloseWorkspaceContext();}))
            }));
            }
            if(general||region!=null) {
            var regions=new List<MenuItem>{
                MenuItem.Action(WindowText("Selection tools","Auswahl bearbeiten"),Guard(()=>SelectEditorTool(2))),
                MenuItem.Action(WindowText("Select all","Alles markieren"),Guard(()=>{panelPage=1;SelectAll();}))
            };
            if(region!=null)regions.Add(MenuItem.Action(WindowText("Delete selected region…","Markierten Bereich löschen…"),Guard(()=>{panelPage=1;AskDelete();})));
            items.Add(MenuItem.Submenu(WindowText("Regions","Bereiche"),regions));
            }
            if(general) {
            items.Insert(0,MenuItem.Action(WindowText("Choose interlocking","Stellwerk wählen"),Guard(OpenEditorCatalogue)));
            items.Insert(1,MenuItem.Action(WindowText("Set up and check","Einrichten und prüfen"),Guard(OpenSetupAssistant)));
            items.Add(MenuItem.Submenu(WindowText("Signals and routes","Signale und Fahrstraßen"),new List<MenuItem>{
                MenuItem.Action(WindowText("World links","Spielwelt-Zuordnungen"),Guard(()=>WorkspacePage(3))),
                MenuItem.Action(WindowText("Saved route planning","Gespeicherte Fahrstraßen planen"),Guard(()=>WorkspacePage(2))),
                MenuItem.Action(WindowText("Automatic recognition…","Automatische Erkennung …"),Guard(OpenRecognition)),
                MenuItem.Action(WindowText("Operating settings…","Betriebseinstellungen …"),Guard(Entry.OpenAutomationSettings))
            }));
            items.Add(MenuItem.Submenu(WindowText("Track detection","Gleisfreimeldung"),new List<MenuItem>{
                MenuItem.Action(WindowText("Detect GFM in this diagram…","GFM in diesem Gleisbild erkennen …"),Guard(OpenExistingDetection)),
                MenuItem.Action(WindowText("Section manager / GFM inspection…","Abschnittmanager / GFM-Prüfung …"),Guard(OpenGfmInspection)),
                MenuItem.Action(WindowText("Set up GFM from world objects…","GFM über Weltobjekte einrichten …"),Guard(OpenWorldGfmSetup))
            }));
            items.Add(MenuItem.Submenu(WindowText("Interlocking and line","Stellwerk und Strecke"),new List<MenuItem>{
                MenuItem.Action(WindowText("Manage this interlocking…","Dieses Stellwerk verwalten …"),Guard(()=>{string id=deskId;OpenEditorCatalogue();OpenCatalogueForm(id);} )),
                MenuItem.Action(WindowText("Open line / GWB","Freie Strecke / GWB"),Guard(OpenFreeLines)),
                MenuItem.Action(WindowText("Master modules","ESTW-Module"),Guard(()=>WorkspacePage(8))),
                MenuItem.Action(WindowText("Configure fixed targets and contacts…","Feste Ziele und Kontakte einrichten …"),Guard(()=>{automationPage=1;WorkspacePage(7);})),
                MenuItem.Action(WindowText("Refresh and inspect installation…","Anlage aktualisieren und prüfen …"),Guard(OpenInstallationAudit)),
                MenuItem.Submenu(WindowText("Extend working area","Arbeitsfläche erweitern"),new List<MenuItem>{
                    MenuItem.Action(WindowText("Add row at top","Zeile oben hinzufügen"),Guard(()=>InsertCanvasAxes(true,0))),
                    MenuItem.Action(WindowText("Add row at bottom","Zeile unten hinzufügen"),Guard(()=>InsertCanvasAxes(true,panel.Rows))),
                    MenuItem.Action(WindowText("Add column at left","Spalte links hinzufügen"),Guard(()=>InsertCanvasAxes(false,0))),
                    MenuItem.Action(WindowText("Add column at right","Spalte rechts hinzufügen"),Guard(()=>InsertCanvasAxes(false,panel.Columns)))
                })
            }));
            items.Add(MenuItem.Action(WindowText("Handbook","Handbuch"),Guard(()=>WorkspacePage(5))));
            }
            if(editorDetailsOpen)items.Add(MenuItem.Action(WindowText("Close properties","Eigenschaften schließen"),Guard(CloseWorkspaceContext)));
            var canvas=editorSurface?.GetComponentInParent<Canvas>()?.rootCanvas;
            if(canvas==null)throw new InvalidOperationException(WindowText("Editor canvas unavailable.","Editorfläche nicht verfügbar."));
            if(anchor!=null)editorPopup=PopupMenu.PresentUnder(anchor,items,"enusde.editor.categories");
            else {
                var point=Mouse.current?.position.ReadValue()??new Vector2(Screen.width/2f,Screen.height/2f);
                editorPopup=PopupMenu.PresentAt(canvas,point,items,"enusde.editor.categories");
            }
        }
        string FieldCategoryCaption(EditorFieldCategory category)=>category==EditorFieldCategory.Detection?WindowText("Track detection / sections","Gleisfreimeldung / Abschnitte"):
            category==EditorFieldCategory.TrainNumber?WindowText("Train number and repeated display","Zugnummer und Anzeigekopie"):category==EditorFieldCategory.History?WindowText("History","Verlauf"):WindowText("Properties and assignment","Eigenschaften und Zuordnung");
        void OpenEditorField(int x,int y,EditorFieldCategory category,bool showProperties=true) {
            RequirePanelEdit();panelPage=1;editorMode=1;tool=-1;regionAction=0;region=null;ResetEditor();selectedX=x;selectedY=y;
            tileDraft=panel.At(x,y)?.Clone();if(tileDraft==null)throw new InvalidOperationException(WindowText("Select an existing field.","Vorhandenes Feld auswählen."));
            LoadTileFields();editorFieldCategory=category;if(showProperties)OpenWorkspaceContext();else CloseWorkspaceContext();panelView?.Refresh();
        }
        void BuildEditorFieldCategory(UIPanelBuilder c) {
            if(editorFieldCategory==EditorFieldCategory.Properties){BuildFieldAssignment(c);return;}
            if(editorFieldCategory==EditorFieldCategory.History){BuildHistory(c);return;}
            if(tileDraft==null)return;
            var binding=new PanelDraftBinding(tileDraft,()=>tileDraft);
            c.AddSection(FieldCategoryCaption(editorFieldCategory));
            if(editorFieldCategory==EditorFieldCategory.TrainNumber&&tileDraft.Kind==PanelTileKind.TrainNumber)BuildNumberField(c,binding);
            else if(editorFieldCategory==EditorFieldCategory.Detection) {
                if(tileDraft.Kind==PanelTileKind.Switch){BuildEditorPointDetection(c,binding);}
                else {if(CircuitField(tileDraft))BuildBoundarySymbol(c,binding);BuildCircuitEditor(c,new PanelRegion{X=tileDraft.X,Y=tileDraft.Y});}
            }
            c.AddButton(WindowText("Save field","Feld speichern"),()=>binding.Do(()=>Act(SaveTile)));
        }
        void BuildEditorPointDetection(UIPanelBuilder c,PanelDraftBinding binding) {
            string id=binding.Draft.Reference,worldScope=scope,owner=deskId;
            if(string.IsNullOrEmpty(id)||!desks.Points.TryGetValue(id,out var point)){c.AddLabel(WindowText("Assign this point to the game world first.","Diese Weiche zuerst der Spielwelt zuordnen."));return;}
            c.AddLabel(InspectPointCircuit(id));
            foreach(string signal in SharedPointSignals(id)) {
                string sharedSignal=signal;
                c.AddButton(WindowText("Open shared A/B box","Gemeinsamen A/B-Kasten öffnen")+" · "+Code(sharedSignal),()=>Entry.OpenMarker(sharedSignal));
            }
            c.AddLabel(WindowText("Disabled points are unmonitored. Detection creates boundary proposals and visible boxes.","Deaktivierte Weichen sind unüberwacht. Die Erkennung erstellt Grenzvorschläge und sichtbare Kästen."));
            void Change(bool enabled,bool detect)=>binding.Do(()=>Act(()=>{
                RequirePanelEdit();if(scope!=worldScope||deskId!=owner)return;
                ConfigurePointCircuit(id,enabled,detect);RefreshWorkspaceFields();panelView?.Refresh();
            }));
            c.AddButton(WindowText("Detect boundaries and show boxes","Grenzen erkennen und Kästen anzeigen"),()=>Change(true,true));
            if(point.ShowCircuit)c.AddButton(WindowText("Disable point detection","Weichen-GFM deaktivieren"),()=>Change(false,false));
            c.AddSection(WindowText("Physical point arms in this diagram","Reale Weichenäste in dieser Tafel"));
            c.AddLabel(WindowText("Arrows refer to the visible diagram. Correct the common and native normal arms if needed, save the field, then rescan GFM. Automatic correction requires two unambiguous boundary connections.","Pfeile beziehen sich auf die sichtbare Tafel. Bei Bedarf Stammgleis und realen Grundast korrigieren, Feld speichern und GFM neu erkennen. Die automatische Korrektur benötigt zwei eindeutige Grenzverbindungen."));
            var draft=binding.Draft;int mask=PanelConnections.Transform(PanelConnections.Ports(draft),draft.Rotation,draft.Mirror,true);
            var ports=Enumerable.Range(0,8).Where(p=>(mask&(1<<p))!=0).ToArray();
            if(ports.Length==3) {
                int stem,normal,branch;if(draft.Ports==0){stem=16;normal=1;branch=2;}else PanelConnections.Arms(draft,out stem,out normal,out branch);
                string Name(int p){int world=PanelConnections.Transform(1<<p,draft.Rotation,draft.Mirror);return new[]{"→","↗","↑","↖","←","↙","↓","↘"}[Enumerable.Range(0,8).Single(i=>world==(1<<i))];}
                c.AddSearchChoice(WindowText("Common arm","Stammgleis"),ports.Select(Name).ToList(),Array.FindIndex(ports,p=>(1<<p)==stem),i=>binding.Do(()=>{draft.Ports=mask;draft.Stem=1<<ports[i];if(draft.Normal==0||draft.Normal==draft.Stem)draft.Normal=1<<ports.First(p=>(1<<p)!=draft.Stem);RefreshWorkspaceFields();}));
                int currentStem=stem;var branches=ports.Where(p=>(1<<p)!=currentStem).ToArray();
                c.AddSearchChoice(WindowText("Native normal arm","Realer Grundast"),branches.Select(Name).ToList(),Array.FindIndex(branches,p=>(1<<p)==normal),i=>binding.Do(()=>{draft.Ports=mask;draft.Stem=currentStem;draft.Normal=1<<branches[i];RefreshWorkspaceFields();}));
            }
        }
        void SwitchBound(string node,bool? target,string expectedScope,string expectedDesk) {
            RequireDispatcher();if(scope!=expectedScope||deskId!=expectedDesk)throw new InvalidOperationException(WindowText("Interlocking changed. Select the point again.","Stellwerk gewechselt. Weiche erneut auswählen."));
            RequireOperatorPoint(node);
            if(!panel.Tiles.Any(t=>t.Kind==PanelTileKind.Switch&&t.Reference==node))throw new InvalidOperationException(WindowText("Point assignment changed. Select it again.","Weichenzuordnung geändert. Erneut auswählen."));
            var result=SwitchOperation.Execute(node,target,id=>world.SwitchPosition(id,out bool position)?(bool?)position:null,(id,desired)=>engine.Locked(id,desired),(id,desired)=>world.SetSwitch(id,desired));
            ClearAutomaticRouteChoices();status=PointAssignments.Caption(desks,node)+": "+(result.Target?WindowText("− reverse","− Abzweig"):WindowText("+ normal","+ Grundstellung"))+" · "+(result.Changed?WindowText("movement confirmed","Umstellung bestätigt"):WindowText("already in position","bereits in Stellung"));
            Entry.RecordSignalEvent("panel_switch",new{id=node,thrown=result.Target,changed=result.Changed});RefreshWorkspaceFields();panelView?.Refresh();
        }
    }
}
