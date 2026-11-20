using System;
using System.Linq;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        UIPanel catalogueInspector;
        EditorSurface editorSurface;
        string editorCatalogueForm,editorViewDesk;
        bool editorLinksAfterSelection;

        void BuildEditorWindow(UIPanelBuilder builder) {
            if(!editingWorkspace)return;
            if(!WorkspaceEditable){builder.AddLabel(WindowText("Editor unavailable.","Editor nicht verfügbar."));return;}
            var anchor=builder.VStack(_=>{}).GetComponentInParent<Window>();
            if(anchor==null)throw new InvalidOperationException("Editor window anchor is missing.");
            ReleaseWorkspaceCanvas();
            var root=WorkspaceSurface.Rect(anchor.transform,"EnusDE editor workspace");
            WorkspaceSurface.Fill(root,8,34,8,16);
            root.SetAsLastSibling();
            var surface=editorSurface=root.gameObject.AddComponent<EditorSurface>();
            bool catalogue=panelPage==4;
            surface.Initialize(root,catalogue);
            surface.Title=()=>catalogue?WindowText("Choose an interlocking","Stellwerk auswählen"):CurrentDeskLabel+"  ·  "+Math.Round(panelZoom*100)+" %";
            surface.Heading=ReadEditorPaneTitle;
            surface.Notice=()=>panelError??workspaceActionError??(catalogue?
                WindowText("Choose the interlocking you want to edit.","Wähle das Stellwerk, das du bearbeiten möchtest."):
                status);
            if(!catalogue) {
                surface.CloseDetails=()=>{if(editorSurface==surface&&PanelVisible)CloseWorkspaceContext();};
                surface.ShowDetails(editorDetailsOpen);
                RectTransform menu=null;
                menu=EditorSurface.Button(surface.Header,WindowText("Menu","Menü"),()=>{
                    if(editorSurface==surface&&PanelVisible)Act(()=>ShowEditorCategories(-1,-1,menu));
                }).GetComponent<RectTransform>();
                WorkspaceSurface.Place(menu,new WorkspaceBox(12,9,84,32));
                BuildEditorZoom(surface);
                BuildWorkspaceDiagram(surface.ViewportFrame,()=>editorSurface==surface,true);
            }
            BuildEditorPanelContent(builder,surface,catalogue);
        }

        void BuildEditorPanelContent(UIPanelBuilder builder,EditorSurface surface,bool catalogue) {
            if(!catalogue)return; // Object tools live in their own window, never beside the diagram.
            Action<UIPanelBuilder> fields=catalogue?BuildEditorCatalogue:BuildEditorFields;
            var content=builder.VScrollView(c=>catalogueInspector=c.AddRebuildableSection(fields));
            content.SetParent(surface.Content,false);
            WorkspaceSurface.Fill(content,catalogue?14:0,catalogue?14:0,catalogue?14:0,catalogue?14:0);
            (content.GetComponent<LayoutElement>()??content.gameObject.AddComponent<LayoutElement>()).ignoreLayout=true;
            catalogueInspector.OnRebuild+=surface.ThemeSoon;
            surface.ThemeSoon();
        }

        void OpenEditorCatalogue() {
            if(manager?.GetCustomWindow(EditorToolsWindowId)!=null)manager.CloseWindow(EditorToolsWindowId);
            if(panelScroll!=null){RememberPanelScroll();SaveEditorView();}
            editorCatalogueForm=null;
            pendingDeskRemoval=null;
            editorLinksAfterSelection=false;
            workspaceActionError=null;
            Mode(4);
        }

        void EnterEditorDesk(string id) {
            RequireEditorCatalogue();
            SelectDesk(id,false);
            if(editorViewSaved&&id==editorViewDesk) {
                panelZoom=editorZoom;panelScrollPosition=editorScroll;workspaceFit=false;
            }
            panelPage=editorLinksAfterSelection||assigningPoint?3:1;
            editorLinksAfterSelection=false;
            editorDetailsOpen=panelPage!=1;editorFieldCategory=EditorFieldCategory.Properties;
            if(assigningPoint)pointOwner=id;
            status=panel.Tiles.Count==0?
                WindowText("Empty diagram. Choose Add fields or Draw track.","Leere Tafel. Felder setzen oder Gleis zeichnen wählen."):
                WindowText("Left-click to select; right-click for categories.","Linksklick wählt aus; Rechtsklick öffnet die Kategorien.");
            Rebuild(false);
            if(editorDetailsOpen)OpenEditorToolsWindow();
        }

        void SaveEditorView() {
            editorViewDesk=deskId;
            editorZoom=panelZoom;
            editorScroll=panelScrollPosition;
            editorViewSaved=true;
        }

        void BuildEditorZoom(EditorSurface owner) {
            var strip=WorkspaceSurface.Rect(owner.Header,"Editor zoom");
            strip.anchorMin=strip.anchorMax=strip.pivot=new Vector2(1,.5f);
            strip.sizeDelta=new Vector2(250,32);strip.anchoredPosition=new Vector2(-12,0);
            string[] labels={"−","+","100 %",WindowText("Fit diagram","Tafel einpassen")};
            int[] widths={30,30,58,114};
            Action[] actions={ ()=>ChangeZoom(panelZoom/1.25f),()=>ChangeZoom(panelZoom*1.25f),()=>ChangeZoom(1),FitEditor };
            int left=0;
            for(int i=0;i<labels.Length;i++) {
                var action=actions[i];
                var button=EditorSurface.Button(strip,labels[i],()=>{if(editorSurface==owner&&PanelVisible)action();});
                WorkspaceSurface.Place(button.GetComponent<RectTransform>(),new WorkspaceBox(left,0,widths[i],32));
                left+=widths[i]+6;
            }
        }

        void FitEditor() {
            var viewport=panelScroll?.viewport;
            if(panel==null||viewport==null||viewport.rect.width<16||viewport.rect.height<16)return;
            float width=viewport.rect.width,height=viewport.rect.height;
            var fit=EditorLayout.Fit(panel,width,height);
            ChangeZoom((float)fit.Zoom);
            Canvas.ForceUpdateCanvases();var desired=viewport.TransformPoint(viewport.rect.center);
            var current=panelView.rectTransform.TransformPoint(new Vector3((float)fit.CenterX*PanelGeometry.CellWidth*panelZoom,-(float)fit.CenterY*PanelGeometry.CellHeight*panelZoom));
            panelScroll.StopMovement();panelScroll.content.anchoredPosition+=(Vector2)panelScroll.content.parent.InverseTransformVector(desired-current);
        }

        void SelectEditorTool(int mode) {
            if(!editingWorkspace||!PanelVisible||panelPage==4)return;
            panelPage=1;
            if(mode==1||mode==3)region=null;
            workspaceActionError=null;
            EditorMode(mode);
        }

        string ReadEditorPaneTitle() {
            if(panelPage==15)return WindowText("Setup assistant","Einrichtungsassistent");
            if(panelPage==13)return WindowText("World GFM setup","GFM-Welteinrichtung");
            if(panelPage==12)return WindowText("GFM inspection","GFM-Prüfung");
            if(panelPage==11)return WindowText("Existing diagram detection","GFM im vorhandenen Gleisbild");
            if(panelPage==9)return WindowText("Open line / GWB","Freie Strecke / GWB");
            if(panelPage==10)return WindowText("Automatic recognition","Automatische Erkennung");
            if(panelPage==4)return WindowText("Interlocking management","Stellwerksverwaltung");
            if(panelPage==8)return WindowText("Master modules","ESTW-Module");
            if(panelPage==7)return WindowText("Automation and AI","Automatisierung und KI");
            if(panelPage==2)return WindowText("Configure routes","Fahrstraßen einrichten");
            if(panelPage==3)return WindowText("World links","Spielwelt-Zuordnung");
            if(panelPage==5)return WindowText("Handbook","Handbuch");
            if(pendingDelete!=null)return WindowText("Confirm removal","Löschen bestätigen");
            if(editorMode==0)return WindowText("Draw track","Gleis zeichnen");
            if(editorMode==2)return WindowText("Selection and diagram tools","Auswahl- und Tafelwerkzeuge");
            if(editorMode==3)return WindowText("Add fields","Felder setzen");
            return FieldCategoryCaption(editorFieldCategory);
        }

        void BuildEditorFields(UIPanelBuilder c) {
            if(!WorkspaceEditable){c.AddLabel(WindowText("Editor unavailable.","Editor nicht verfügbar."));return;}
            if(panelPage==15){BuildSetupAssistant(c);return;}
            if(panelPage==14){BuildInstallationAudit(c);return;}
            if(panelPage==13){BuildWorldGfmSetup(c);return;}
            if(panelPage==12){BuildGfmInspection(c);return;}
            if(panelPage==11){BuildExistingDetection(c);return;}
            if(panelPage==9){BuildFreeLines(c);return;}
            if(panelPage==10){BuildRecognition(c);return;}
            if(panelPage==8){BuildMasterMenu(c);return;}
            if(panelPage==7){BuildAutomationMenu(c);return;}
            if(panelPage==5){BuildHelp(c);return;}
            if(panelPage==2){BuildRoutes(c);return;}
            if(panelPage==3){BuildIdentifiers(c);BuildNetworkMenu(c);return;}
            if(pendingDelete!=null){BuildDeleteConfirmation(c);return;}
            if(editorMode==3){BuildFieldPalette(c);return;}
            if(editorMode==2){BuildHistory(c);BuildRegionTools(c);BuildTransfers(c);BuildCanvasSize(c);return;}
            if(editorMode==0) {
                c.AddLabel(WindowText("Click a starting cell, then each corner or endpoint.","Startfeld und danach die Eck- oder Endpunkte anklicken."));
                c.AddButton(WindowText("Finish drawing","Zeichnen beenden"),()=>SelectEditorTool(1));
                return;
            }
            if(tileDraft==null) {
                c.AddLabel(WindowText("Click a field on the diagram.","Klicke ein Feld auf der Tafel an."));
                return;
            }
            if(editorFieldCategory==EditorFieldCategory.Properties)c.AddButton(WindowText("Insert row above","Zeile davor einfügen"),()=>Act(()=>InsertSpace(true)));
            if(editorFieldCategory==EditorFieldCategory.Properties)c.AddButton(WindowText("Insert column before","Spalte davor einfügen"),()=>Act(()=>InsertSpace(false)));
            BuildEditorFieldCategory(c);
        }

        void BuildFieldPalette(UIPanelBuilder c) {
            c.AddLabel(WindowText("Choose a symbol. Each click on the grid places another copy.","Wähle ein Symbol. Jeder Klick ins Raster setzt ein weiteres Feld."));
            var first=c.HStack(_=>{});
            PaletteButton(first,PanelTileKind.Signal,"DISPATCH",WindowText("Signal","Signal"));
            PaletteButton(first,PanelTileKind.Switch,"FS",WindowText("Switch","Weiche"));
            var second=c.HStack(_=>{});
            PaletteButton(second,PanelTileKind.Label,"TEXT",WindowText("Text","Text"));
            PaletteButton(second,PanelTileKind.TrainNumber,"ID",WindowText("Train no.","Zugnummer"));
            var third=c.HStack(_=>{});
            PaletteButton(third,PanelTileKind.LinePermission,"FS",WindowText("Line permission","Streckenerlaubnis"));
            PaletteButton(third,PanelTileKind.AuxiliaryTarget,"GO",WindowText("Auxiliary destination","Hilfsziel"));
            c.AddButton(WindowText("Switch to Select","Zu Auswählen wechseln"),()=>SelectEditorTool(1));
        }

        void PaletteButton(RectTransform row,PanelTileKind kind,string glyph,string caption) {
            var owner=editorSurface;
            var button=AddEditorIcon(row,caption,glyph,()=>{if(editorSurface==owner&&PanelVisible&&panelPage==1&&editorMode==3)ChooseSymbol(kind);});
            if(tool==(int)kind)button.targetGraphic.color=new Color(.11f,.4f,.36f);
        }

        static Button AddEditorIcon(RectTransform row,string caption,string glyph,Action action) {
            var layout=row.GetComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth=false;
            layout.childForceExpandHeight=false;
            layout.spacing=6;
            var button=EditorSurface.IconButton(row,caption,glyph,action);
            var rect=button.GetComponent<RectTransform>();
            rect.sizeDelta=new Vector2(42,42);
            var size=button.gameObject.AddComponent<LayoutElement>();
            size.minWidth=size.preferredWidth=size.minHeight=size.preferredHeight=42;
            return button;
        }
    }
}

