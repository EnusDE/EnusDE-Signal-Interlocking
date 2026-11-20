using System;
using UI.Windows;
using UnityEngine.InputSystem;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string pointPickWorld,pointPickDesk;
        int pointPickX,pointPickY;
        PanelTile pointPickSnapshot;
        Action<string> worldPointSelection;Action worldPointSelectionReturn;string worldPointSelectionScope;
        internal bool PickingPoint=>pointPickWorld!=null||worldPointSelection!=null;
        void BeginWorldPointSelection(Action<string> accept,Action resume) {
            RequirePanelEdit();worldSignalPick=null;worldSignalPickReturn=null;worldPointSelection=accept;worldPointSelectionReturn=resume;worldPointSelectionScope=scope;pointPickWorld=null;
            worldSelectionInput.Complete();objects.CloseForWorldSelection();CloseWorkspaceMode();status=LocalText.Select("Click the real point. Escape cancels.","Reale Weiche anklicken. Escape bricht ab.");
        }
        void CompleteWorldPointSelection(string id) {
            if(scope!=worldPointSelectionScope)throw new InvalidOperationException(LocalText.Select("World changed. Start selection again.","Welt gewechselt. Auswahl erneut starten."));
            var accept=worldPointSelection;var resume=worldPointSelectionReturn;worldPointSelection=null;worldPointSelectionReturn=null;
            worldSelectionInput.Complete();objects.CloseForWorldSelection();resume?.Invoke();accept(id);RefreshWorkspaceFields();
        }
        void BeginPointPick(int x,int y) {
            RequirePanelEdit();pointPickWorld=scope;pointPickDesk=deskId;pointPickX=x;pointPickY=y;pointPickSnapshot=panel.At(x,y)?.Clone();
            worldSelectionInput.Complete();objects.CloseForWorldSelection();
            CloseWorkspaceMode();status=LocalText.Select("Click the real point to assign it directly. Escape cancels.","Reale Weiche anklicken: wird direkt zugeordnet. Escape bricht ab.");
        }
        internal bool TryAssignWorldPoint(Track.TrackNode point) {
            if(ConsumeSelectionInput())return true;
            if(!PickingPoint)return false;
            if(WorldPointerInput.OverUi())return true;
            Act(()=>{RequireEdit();if(point==null||Track.Graph.Shared==null||!Track.Graph.Shared.IsSwitch(point))throw new InvalidOperationException(LocalText.Select("Select a valid point.","Eine gültige Weiche wählen."));AssignWorldPoint(point.id,null);});return true;
        }
        bool FinishPointPick() {
            if(pointPickWorld==null)return false;
            string targetWorld=pointPickWorld,targetDesk=pointPickDesk;int x=pointPickX,y=pointPickY;var snapshot=pointPickSnapshot;
            pointPickWorld=pointPickDesk=null;pointPickSnapshot=null;
            if(scope!=targetWorld)throw new InvalidOperationException(LocalText.Select("World changed. Start point assignment again.","Welt gewechselt. Weichenzuordnung erneut starten."));
            var target=desks.Desks.Find(d=>d.Id==targetDesk);
            if(target==null||!EditorContextPolicy.SameTarget(target.Panel,x,y,snapshot))throw new InvalidOperationException(LocalText.Select("Target field changed. Start point assignment again.","Zielfeld geändert. Weichenzuordnung erneut starten."));
            ShowWorkspace(true,4,false);EnterEditorDesk(targetDesk);pointOwner=targetDesk;
            worldSelectionInput.Complete();objects.CloseForWorldSelection();
            AcceptPoint(x,y);CloseWorkspaceContext();
            return true;
        }
        void ReadPointPickCancel() {
            if(worldPointSelection!=null&&(scope!=worldPointSelectionScope||Keyboard.current?.escapeKey.wasPressedThisFrame==true)) {
                var resume=worldPointSelectionReturn;bool same=scope==worldPointSelectionScope;worldPointSelection=null;worldPointSelectionReturn=null;worldSelectionInput.Complete();if(same)resume?.Invoke();return;
            }
            if(pointPickWorld==null||Keyboard.current?.escapeKey.wasPressedThisFrame!=true)return;
            string targetWorld=pointPickWorld,targetDesk=pointPickDesk;
            pointPickWorld=pointPickDesk=null;pointPickSnapshot=null;
            worldSelectionInput.Complete();
            if(scope!=targetWorld||!desks.Desks.Exists(d=>d.Id==targetDesk))return;
            ShowWorkspace(true,4,false);EnterEditorDesk(targetDesk);status=LocalText.Select("Point assignment cancelled.","Weichenzuordnung abgebrochen.");
        }
    }
}
