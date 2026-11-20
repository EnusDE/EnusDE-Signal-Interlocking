using System;
using System.Linq;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const string DiagramPickId="enusde.signaltechnik.diagram-pick";
        CustomWindowDefinition diagramPickDefinition;
        RectTransform diagramPickRoot;
        Action<string> diagramPickAccept;
        Func<string,bool> diagramPickAllowed,diagramPickSelected;
        Action<string,int,int> diagramCellAccept;
        Func<DeskDefinition,PanelTile,bool> diagramCellAllowed;
        bool DiagramPicking=>diagramPickAccept!=null||diagramCellAccept!=null;
        string diagramPickDesk,diagramPickScope,diagramPickPrompt;
        long diagramPickGeneration;
        void PickSignalInDiagram(string initialDesk,string prompt,Func<string,bool> allowed,Action<string> accept,Func<string,bool> selected=null) {
            diagramCellAccept=null;diagramCellAllowed=null;
            RequirePanelEdit();EnsureWindow();diagramPickDesk=initialDesk;diagramPickPrompt=prompt;diagramPickAllowed=allowed;diagramPickAccept=accept;diagramPickSelected=selected;diagramPickScope=scope;diagramPickGeneration++;
            ShowDiagramPicker();
        }
        void PickCellInDiagram(string initialDesk,string prompt,Func<DeskDefinition,PanelTile,bool> allowed,Action<string,int,int> accept) {
            RequirePanelEdit();EnsureWindow();diagramPickAccept=null;diagramPickAllowed=null;diagramPickSelected=null;diagramCellAccept=accept;diagramCellAllowed=allowed;diagramPickDesk=initialDesk;diagramPickPrompt=prompt;diagramPickScope=scope;diagramPickGeneration++;ShowDiagramPicker();
        }
        void ShowDiagramPicker() {
            if(diagramPickDefinition==null) {
                diagramPickDefinition=new CustomWindowDefinition{Identifier=DiagramPickId,Title=WindowText("Select in diagram","Im Gleisbild auswählen"),BuilderAction=BuildDiagramPicker,
                    Config=WindowConfig.Resizable(new Vector2Int(1050,700),new Vector2Int(700,450),new Vector2Int(3840,2160),Window.Position.CenterRight)};
                manager.RegisterCustomWindow(diagramPickDefinition);
            }
            var handle=CustomWindowHandle.Create(DiagramPickId);handle?.Show();handle?.SetNeedsRebuild();
        }
        void BuildDiagramPicker(UIPanelBuilder c) {
            if(diagramPickRoot!=null){diagramPickRoot.gameObject.SetActive(false);Destroy(diagramPickRoot.gameObject);}
            if(!DiagramPicking||diagramPickScope!=scope)return;
            var locals=desks.Desks.Where(d=>!d.IsMaster).OrderBy(d=>d.Code).ToArray();if(locals.Length==0)return;
            if(!locals.Any(d=>d.Id==diagramPickDesk))diagramPickDesk=locals[0].Id;
            c.AddLabel(diagramPickPrompt);
            c.AddSearchChoice(WindowText("Interlocking","Stellwerk"),locals.Select(d=>d.Code+" · "+d.Name).ToList(),Array.FindIndex(locals,d=>d.Id==diagramPickDesk),i=>{diagramPickDesk=locals[i].Id;CustomWindowHandle.Create(DiagramPickId)?.SetNeedsRebuild();});
            c.AddButton(diagramPickSelected!=null?WindowText("Back to tutorial","Zurück zum Tutorial"):WindowText("Cancel selection","Auswahl abbrechen"),()=>{diagramPickAccept=null;diagramCellAccept=null;diagramPickGeneration++;manager.CloseWindow(DiagramPickId);});
            var window=c.VStack(_=>{}).GetComponentInParent<Window>();
            var root=diagramPickRoot=WorkspaceSurface.Rect(window.transform,"Signal selection diagram");WorkspaceSurface.Fill(root,8,180,8,16);
            var viewport=WorkspaceSurface.Rect(root,"Viewport");WorkspaceSurface.Fill(viewport,0,0,12,12);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=scroll.vertical=true;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var doc=locals.Single(d=>d.Id==diagramPickDesk).Panel.Clone();long generation=diagramPickGeneration;string chosenDesk=diagramPickDesk;
            bool Alive()=>diagramPickRoot==root&&DiagramPicking&&scope==diagramPickScope&&generation==diagramPickGeneration;
            PanelVisual Read(int x,int y) {
                var tile=doc.At(x,y);var signal=tile?.Kind==PanelTileKind.Signal?Object(tile.Reference):null;
                return new PanelVisual{Caption=tile?.Kind==PanelTileKind.Signal?Code(tile.Reference):tile?.Target?.Caption??(tile?.Kind==PanelTileKind.Switch?PointAssignments.Caption(desks,tile.Reference):tile?.Label??""),
                    Rail=new Color(.6f,.65f,.65f),Lamp=Color.red,AlternateLamp=Color.red,Split=tile?.AlternateSignal!=null||tile?.Target!=null,AuxiliaryHalf=SignalCellLayout.HasAuxiliaryHalf(tile),AlternateCaption=tile?.Target?.Caption??Code(tile?.AlternateSignal),HasShunt=tile?.AlternateSignal!=null,ShuntOnly=signal?.Kind==MarkerRecord.Shunt,Distant=signal?.Kind==MarkerRecord.Distant,
                    SetupActive=tile?.Kind==PanelTileKind.Signal&&diagramPickSelected!=null&&(diagramPickSelected(tile.Reference)||tile.AlternateSignal!=null&&diagramPickSelected(tile.AlternateSignal)),
                    Selected=diagramCellAllowed!=null?tile!=null&&diagramCellAllowed(locals.Single(d=>d.Id==chosenDesk),tile):tile!=null&&(diagramPickAllowed(tile.Reference)||tile.Target!=null&&diagramPickAllowed(tile.Target.Id))};
            }
            void Press(int x,int y,bool half)=>Act(()=>{
                RequirePanelEdit();if(!Alive()||chosenDesk!=diagramPickDesk)throw new InvalidOperationException(WindowText("Selection changed. Reopen it.","Auswahl geändert. Erneut öffnen."));
                var shown=doc.At(x,y);var actual=desks.Desks.First(d=>d.Id==chosenDesk).Panel.At(x,y);
                if(shown==null?actual!=null:actual==null||!shown.SameContent(actual))throw new InvalidOperationException(WindowText("The field changed. Reopen selection.","Das Feld wurde geändert. Auswahl erneut öffnen."));
                if(diagramCellAccept!=null) {
                    var destination=desks.Desks.First(d=>d.Id==chosenDesk);
                    if(!diagramCellAllowed(destination,actual))throw new InvalidOperationException(WindowText("This field cannot be used here. Choose an empty or eligible field.","Dieses Feld kann hier nicht verwendet werden. Ein freies oder passendes Feld wählen."));
                    var callback=diagramCellAccept;callback(chosenDesk,x,y);diagramPickAccept=null;diagramCellAccept=null;diagramPickGeneration++;manager.CloseWindow(DiagramPickId);return;
                }
                if(shown==null)return;
                string id=shown.Kind==PanelTileKind.AuxiliaryTarget?shown.Target?.Id:shown.Kind==PanelTileKind.Signal?SignalCellLayout.AuxiliaryAt(shown,half)?shown.Target?.Id:SignalCellLayout.SignalAt(shown,half):null;
                if(id==null||!diagramPickAllowed(id))throw new InvalidOperationException(WindowText("Select a highlighted, eligible signal or destination.","Ein hervorgehobenes, passendes Signal oder Ziel auswählen."));
                var accept=diagramPickAccept;accept(id);if(!Alive()||diagramPickSelected!=null)return;diagramPickAccept=null;diagramCellAccept=null;diagramPickGeneration++;manager.CloseWindow(DiagramPickId);
            });
            var drawing=WorkspaceSurface.Rect(viewport,"Diagram");var view=drawing.gameObject.AddComponent<PanelView>();
            view.Initialize(doc,Read,Press,Alive,(x,y,half)=>{},z=>view.SetZoom(z),()=>null,()=>null,true);scroll.content=drawing;view.AttachScroll(scroll);
            var fit=EditorLayout.Fit(doc,850,400);view.SetZoom((float)fit.Zoom);drawing.anchoredPosition=new Vector2((float)(425-fit.CenterX*PanelGeometry.CellWidth*fit.Zoom),(float)(fit.CenterY*PanelGeometry.CellHeight*fit.Zoom-200));
            scroll.horizontalScrollbar=WorkspaceBar(root,false);scroll.verticalScrollbar=WorkspaceBar(root,true);
        }
    }
}
