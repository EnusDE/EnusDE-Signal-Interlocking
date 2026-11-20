using System;
namespace EnusDE.Signaltechnik {
    public static class PanelQuickActions {
        public static PanelTile Rotate(PanelTile field) {
            if(field==null)throw new ArgumentNullException(nameof(field));var next=field.Clone();next.Rotation=(next.Rotation+1)%4;
            if(next.MainSignalDirection.HasValue)next.MainSignalDirection=(next.MainSignalDirection.Value+1)%4;
            if(next.ShuntSignalDirection.HasValue)next.ShuntSignalDirection=(next.ShuntSignalDirection.Value+1)%4;
            return next;
        }
    }
    public sealed partial class SignalRuntime {
        void QuickFieldTransform(int x,int y,bool mirror=false) {
            RequirePanelEdit();EditPanel(doc=>{var current=doc.At(x,y);var next=mirror?current.Clone():PanelQuickActions.Rotate(current);if(mirror)next.Mirror=!next.Mirror;doc.Put(next);});
            if(tileDraft?.X==x&&tileDraft.Y==y){var saved=panel.At(x,y);tileDraft.Rotation=saved.Rotation;tileDraft.Mirror=saved.Mirror;tileDraft.MainSignalDirection=saved.MainSignalDirection;tileDraft.ShuntSignalDirection=saved.ShuntSignalDirection;}
            status=LocalText.Select("Quick action applied and saved.","Schnellaktion übernommen und gespeichert.");panelView?.Refresh();RefreshWorkspaceFields();
        }
    }
}
