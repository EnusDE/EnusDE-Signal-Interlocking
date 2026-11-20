using System;
using System.Linq;
using UI;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly EditorAxisSelection axisSelection=new EditorAxisSelection();
        CanvasGrowth EditCanvas(PanelRegion bounds,Action<PanelDocument,int,int> edit,int margin=2) {
            var growth=CanvasGrowth.For(panel,bounds,margin);
            EditPanel(doc=>{growth.Apply(doc);edit(doc,growth.Left,growth.Top);});return growth;
        }
        void SaveCanvasTile(PanelTile tile) {
            var growth=EditCanvas(new PanelRegion{X=tile.X,Y=tile.Y},(doc,dx,dy)=>{var saved=tile.Clone();saved.X+=dx;saved.Y+=dy;doc.Put(saved);},panel.At(tile.X,tile.Y)==null?2:0);
            tile.X+=growth.Left;tile.Y+=growth.Top;selectedX=tile.X;selectedY=tile.Y;
        }
        void RebaseEditorCanvas(int dx,int dy) {
            if(dx==0&&dy==0)return;
            if(selectedX>=0||selectedY>=0){selectedX+=dx;selectedY+=dy;}
            if(tileDraft!=null){tileDraft.X+=dx;tileDraft.Y+=dy;}
            if(region!=null){region= new PanelRegion{X=region.X+dx,Y=region.Y+dy,Width=region.Width,Height=region.Height};}
            if(drawingActive){drawX+=dx;drawY+=dy;}
            if(regionAction==2){cornerX+=dx;cornerY+=dy;}
            if(lineIndicatorTarget?.Desk==deskId){lineIndicatorTarget.X+=dx;lineIndicatorTarget.Y+=dy;}
            DismissEditorMenu();ResetCircuitSelection();axisSelection.Clear();
        }
        void SelectEditorAxis(bool row,int index,bool shift,bool context) {
            Act(()=>{
                RequirePanelEdit();if(pendingDelete!=null)return;
                int size=row?panel.Rows:panel.Columns;
                if(index<0||index>=size){InsertCanvasAxes(row,index<0?0:size,1);return;}
                bool keep=context&&axisSelection.Row==row&&region!=null&&(row?index>=region.Y&&index<region.Y+region.Height:index>=region.X&&index<region.X+region.Width)&&!shift;
                var selected=keep?region:axisSelection.Select(row,index,shift,panel.Columns,panel.Rows);
                ResetEditor();regionAction=0;panelPage=1;editorMode=2;tool=-1;tileDraft=null;region=selected;selectedX=selected.X;selectedY=selected.Y;
                status=WindowText("Whole rows/columns selected. Shift-click extends; right-click inserts before or after.","Ganze Zeilen/Spalten markiert. Umschalt-Klick erweitert; Rechtsklick fügt davor oder dahinter ein.");
                panelView?.Refresh();RefreshWorkspaceFields();if(context)ShowEditorAxisMenu(row);
            });
        }
        void InsertCanvasAxes(bool row,int at,int count=1) {
            RequirePanelEdit();if(pendingDelete!=null)throw new InvalidOperationException(WindowText("Finish or cancel the pending removal first.","Die vorgemerkte Löschung zuerst abschließen oder abbrechen."));
            EditPanel(doc=>EditorCanvasGeometry.Insert(doc,row,at,count));ResetEditor();axisSelection.Clear();region=null;tileDraft=null;selectedX=selectedY=-1;
            status=WindowText("Working area extended. All existing bindings retained; internal drawing gaps can be connected with Draw track.","Arbeitsfläche erweitert. Alle vorhandenen Zuordnungen bleiben erhalten; innere Zeichenlücken lassen sich mit Gleis zeichnen verbinden.");panelView?.Refresh();RefreshWorkspaceFields();
        }
        void ShowEditorAxisMenu(bool row) {
            RequirePanelEdit();DismissEditorMenu();long epoch=editorContextEpoch;string desk=deskId,world=scope,snapshot=PanelHistory.Hash(panel);
            int first=row?region.Y:region.X,count=row?region.Height:region.Width;
            Action Guard(Action action)=>()=>Act(()=>{if(epoch!=editorContextEpoch||deskId!=desk||scope!=world||!editingWorkspace)return;if(snapshot!=PanelHistory.Hash(panel))throw new InvalidOperationException(WindowText("Diagram changed. Select the rows or columns again.","Tafel geändert. Zeilen oder Spalten erneut auswählen."));action();});
            var items=new System.Collections.Generic.List<MenuItem>{
                MenuItem.Action(row?WindowText("Insert rows above","Zeilen davor einfügen"):WindowText("Insert columns before","Spalten davor einfügen"),Guard(()=>InsertCanvasAxes(row,first,count))),
                MenuItem.Action(row?WindowText("Insert rows below","Zeilen dahinter einfügen"):WindowText("Insert columns after","Spalten dahinter einfügen"),Guard(()=>InsertCanvasAxes(row,first+count,count))),
                MenuItem.Action(WindowText("Selection tools","Auswahl bearbeiten"),Guard(OpenWorkspaceContext))
            };
            var canvas=editorSurface?.GetComponentInParent<Canvas>()?.rootCanvas;var point=UnityEngine.InputSystem.Mouse.current?.position.ReadValue()??Vector2.zero;
            editorPopup=PopupMenu.PresentAt(canvas,point,items,"enusde.editor.axes");
        }
    }
}
