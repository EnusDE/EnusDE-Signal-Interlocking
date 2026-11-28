using Game.State;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Windows;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Keep the native window for event focus and popup ordering, and cover its entire root canvas.
    internal sealed class InterlockingFullscreen : MonoBehaviour {
        RectTransform window,canvas;
        readonly Vector3[] corners=new Vector3[4];
        internal void Initialize(RectTransform owner,RectTransform gameCanvas) { window=owner;canvas=gameCanvas;Fit(); }
        void LateUpdate()=>Fit();
        void Fit() {
            if(window==null||canvas==null||window.parent==null)return;
            canvas.GetWorldCorners(corners);
            var low=window.parent.InverseTransformPoint(corners[0]);
            var high=window.parent.InverseTransformPoint(corners[2]);
            var size=new Vector2(high.x-low.x,high.y-low.y);
            if(size.x<=0||size.y<=0)return;
            var center=(low+high)*.5f;center.z=window.localPosition.z;
            if(window.anchorMin!=new Vector2(.5f,.5f)||window.anchorMax!=new Vector2(.5f,.5f))window.anchorMin=window.anchorMax=new Vector2(.5f,.5f);
            window.pivot=new Vector2(.5f,.5f);
            if(window.sizeDelta!=size)window.sizeDelta=size;
            if(window.localPosition!=center)window.localPosition=center;
        }
    }

    public sealed partial class SignalRuntime {
        internal const int WorkspaceCommandsWidth=278;
        bool workspaceSessionOpen,workspaceMinimized;
        int workspaceVisibleSince;
        RectTransform endReview;
        readonly Queue<Action> deferredWorkspaceRoutes=new Queue<Action>();
        void FinishOrDeferRoute(Action action) {
            if(!workspaceMinimized){action();return;}
            if(deferredWorkspaceRoutes.Count>=4)throw new InvalidOperationException(WindowText("Resume the interlocking before adding more routes.","Stellwerk vor weiteren Fahrstraßen wieder öffnen."));
            string worldScope=scope,desk=deskId;
            deferredWorkspaceRoutes.Enqueue(()=>{if(scope==worldScope&&deskId==desk)action();});
        }

        void CoverGameViewport(Window owner,RectTransform content) {
            WorkspaceSurface.Fill(content);
            var canvas=owner.GetComponentInParent<Canvas>()?.rootCanvas?.transform as RectTransform;
            content.gameObject.AddComponent<InterlockingFullscreen>().Initialize(owner.transform as RectTransform,canvas);
        }
        void CheckWorkspaceSession() {
            if(workspaceSessionOpen&&!workspaceMinimized&&Time.frameCount-workspaceVisibleSince>8&&!PanelVisible) {
                workspaceSessionOpen=false;
                ReleaseOperatorScope();
                deferredWorkspaceRoutes.Clear();
            }
            if(!StateManager.IsHost||StateManager.IsUnloading)ReleaseOperatorScope();
            if(operatorScopes.Count>0&&!OperatorAuthorityCurrent) {
                ReleaseOperatorScope();
                status=WindowText("The diagram changed. Select the takeover area again.","Das Gleisbild wurde geändert. Wähle den Übernahmebereich erneut.");
            }
            if(PanelVisible&&!editingWorkspace&&deferredWorkspaceRoutes.Count>0&&automaticChoices.Count==0)
                Act(deferredWorkspaceRoutes.Dequeue());
        }
        bool RestoreMinimizedWorkspace() {
            if(!workspaceMinimized)return false;
            workspaceMinimized=false;workspaceSessionOpen=true;workspaceVisibleSince=Time.frameCount;
            CustomWindowHandle.Create(WindowId)?.Show();
            return true;
        }
        void MinimizeWorkspace() {
            if(!PanelVisible)return;
            RememberPanelScroll();
            workspaceMinimized=true;
            CloseRouteChoiceWindow();
            manager.CloseWindow(WindowId);
        }
        void EndWorkspaceSession() {
            ReleaseOperatorScope();
            deferredWorkspaceRoutes.Clear();
            operatorControlRequested=false;
            CloseWorkspaceMode();
            editingWorkspace=false;
            panel=DeskPanel(CurrentDesk);
            tileDraft=null;lineDraft=null;endReview=null;
        }
        bool PendingWorkspaceEdits() {
            if(!editingWorkspace)return false;
            bool tile=tileDraft!=null&&panel!=null&&!tileDraft.SameContent(panel.At(tileDraft.X,tileDraft.Y));
            if(tile||editorCatalogueForm!=null)return true;
            if(panelPage!=9||lineDraft==null)return false;
            var saved=desks.FreeLines.Find(l=>l.Id==lineDraft.Id);
            return saved==null||JsonConvert.SerializeObject(saved)!=JsonConvert.SerializeObject(lineDraft);
        }
        void ReviewWorkspaceExit(Transform parent,System.Action proceed) {
            if(!PendingWorkspaceEdits()){proceed();return;}
            if(endReview!=null){endReview.gameObject.SetActive(false);Destroy(endReview.gameObject);}
            endReview=WorkspaceSurface.Rect(parent,WindowText("Unsaved changes","Ungespeicherte Änderungen"),WorkspaceSurface.Panel);
            endReview.SetAsLastSibling();
            endReview.anchorMin=endReview.anchorMax=endReview.pivot=new Vector2(.5f,.5f);endReview.sizeDelta=new Vector2(410,120);
            var text=WorkspaceSurface.Text(endReview,"Message",WindowText("Save in the editor or discard the draft.","Im Editor speichern oder den Entwurf verwerfen."),15);
            WorkspaceSurface.Place(text.rectTransform,new WorkspaceBox(12,10,386,44));
            WorkspaceSurface.Place(EditorSurface.Button(endReview,WindowText("Back to editor","Zurück zum Editor"),()=>{if(endReview!=null){Destroy(endReview.gameObject);endReview=null;}}).GetComponent<RectTransform>(),new WorkspaceBox(12,67,180,34));
            WorkspaceSurface.Place(EditorSurface.Button(endReview,WindowText("Discard and continue","Verwerfen und fortfahren"),()=>{Destroy(endReview.gameObject);endReview=null;proceed();}).GetComponent<RectTransform>(),new WorkspaceBox(204,67,194,34));
        }
        void BuildWorkspaceCommands(RectTransform header,bool editing) {
            var controls=WorkspaceSurface.Rect(header,"Interlocking commands");
            controls.anchorMin=controls.anchorMax=controls.pivot=new Vector2(1,.5f);
            controls.sizeDelta=new Vector2(WorkspaceCommandsWidth,34);
            var parent=header.parent;
            void Add(string title,int x,int width,System.Action action) {
                var button=EditorSurface.Button(controls,title,()=>{if(PanelVisible)Act(action);});
                WorkspaceSurface.Place(button.GetComponent<RectTransform>(),new WorkspaceBox(x,0,width,32));
            }
            if(editing||WorkspaceEditable)
                Add(editing?WindowText("Operation","Betrieb"):WindowText("Edit","Bearbeiten"),0,82,()=>{
                    if(editing)ReviewWorkspaceExit(parent,()=>{tileDraft=null;lineDraft=null;ShowWorkspace(false,0,false);});
                    else ShowWorkspace(true,1,false);
                });
            Add(WindowText("Minimize","Minimieren"),86,86,MinimizeWorkspace);
            Add(WindowText("End mode","Modus beenden"),176,100,()=>ReviewWorkspaceExit(parent,EndWorkspaceSession));
        }
    }
}
