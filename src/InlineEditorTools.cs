using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        void OpenEditorToolsWindow() {
            if(!editingWorkspace||!WorkspaceEditable||panelPage==4&&editorCatalogueForm==null&&pendingDeskRemoval==null)return;
            editorDetailsOpen=true;
            editorSurface?.ShowDetails(true);
            RebuildEditorToolsWindow();
        }
        void BuildInlineEditorTools(UIPanelBuilder content) {
            if(!editingWorkspace||!WorkspaceEditable)return;
            if(panelPage==4) {
                if(pendingDeskRemoval!=null)BuildDeskRemoval(content,pendingDeskRemoval);
                else if(editorCatalogueForm!=null)BuildCatalogueForm(content,editorCatalogueForm);
            } else BuildEditorFields(content);
        }
        void RebuildEditorToolsWindow()=>inspector?.SetNeedsRebuild();
    }
}
