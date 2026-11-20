using System;
using UI.Builder;
using UI.Windows;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const string EditorToolsWindowId="enusde.signaltechnik.editor-tools";
        CustomWindowDefinition editorToolsDefinition;
        void OpenEditorToolsWindow() {
            EnsureWindow();if(manager==null||!editingWorkspace||panelPage==4&&editorCatalogueForm==null&&pendingDeskRemoval==null)return;
            if(editorToolsDefinition==null||!ReferenceEquals(manager.GetDefinition(EditorToolsWindowId),editorToolsDefinition)) {
                editorToolsDefinition=new CustomWindowDefinition{Identifier=EditorToolsWindowId,
                    Title=WindowText("EnusDE · Edit selection","EnusDE · Auswahl bearbeiten"),
                    BuilderAction=BuildEditorToolsWindow,
                    Config=WindowConfig.Resizable(new Vector2Int(500,650),new Vector2Int(380,300),new Vector2Int(1100,1400),Window.Position.CenterRight)};
                manager.RegisterCustomWindow(editorToolsDefinition);
            }
            var handle=CustomWindowHandle.Create(EditorToolsWindowId);handle?.Show();handle?.SetNeedsRebuild();
        }
        void BuildEditorToolsWindow(UIPanelBuilder builder) {
            if(!editingWorkspace||!WorkspaceEditable){builder.AddLabel(WindowText("Select a field in the editor.","Ein Feld im Editor auswählen."));return;}
            builder.AddField(WindowText("Selection","Auswahl"),()=>CurrentDeskLabel+" · "+ReadEditorPaneTitle(),UIPanelBuilder.Frequency.Periodic);
            builder.AddField(WindowText("Status","Status"),()=>workspaceActionError??status,UIPanelBuilder.Frequency.Periodic);
            builder.VScrollView(c=>inspector=c.AddRebuildableSection(content=>{
                if(panelPage==4){if(pendingDeskRemoval!=null)BuildDeskRemoval(content,pendingDeskRemoval);else if(editorCatalogueForm!=null)BuildCatalogueForm(content,editorCatalogueForm);}
                else BuildEditorFields(content);
            }));
        }
        void RebuildEditorToolsWindow() {
            if(manager!=null&&CustomWindowHandle.Create(EditorToolsWindowId)?.IsOpen()==true)
                CustomWindowHandle.Create(EditorToolsWindowId)?.SetNeedsRebuild();
        }
    }
}
