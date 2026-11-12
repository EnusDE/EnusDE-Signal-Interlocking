using System;

namespace EnusDE.Signaltechnik {
    public enum WorkspaceMode { Dispatcher, Build, Editor }

    public readonly struct ModePickerAccess {
        public readonly bool Host, WorkspaceReady, BuildReady, EditorReady;

        public ModePickerAccess(bool host,bool workspaceReady,bool buildReady,bool editorReady) {
            Host=host;
            WorkspaceReady=workspaceReady;
            BuildReady=buildReady;
            EditorReady=editorReady;
        }

        public bool CanOpen(WorkspaceMode mode) {
            switch(mode) {
                case WorkspaceMode.Dispatcher: return WorkspaceReady;
                case WorkspaceMode.Build: return Host&&BuildReady;
                case WorkspaceMode.Editor: return Host&&WorkspaceReady&&EditorReady;
                default: return false;
            }
        }
    }

    public static class ModePickerText {
        public static string ToolbarCaption(bool german)=>german?"STW":"IXL";

        public static bool UsesGerman(string language,bool translateInterface) {
            if(!translateInterface)return false;
            language=language?.Trim();
            return string.Equals(language,"de",StringComparison.OrdinalIgnoreCase)
                || language?.StartsWith("de-",StringComparison.OrdinalIgnoreCase)==true;
        }

        public static string Caption(WorkspaceMode mode,bool german) {
            switch(mode) {
                case WorkspaceMode.Dispatcher: return german?"Fahrdienstleiter":"Dispatcher";
                case WorkspaceMode.Build: return german?"Baumodus":"Build mode";
                case WorkspaceMode.Editor: return "Editor";
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public static string Status(ModePickerAccess access,bool german) {
            if(!access.WorkspaceReady&&!access.BuildReady)
                return german?"Die Anlage wird geladen oder ist nicht verfügbar.":"The layout is loading or unavailable.";
            if(!access.Host)
                return german?"Gleisbild nur zur Ansicht. Der Host baut und bedient.":"View only. The host builds and operates.";
            if(!access.BuildReady)
                return german?"Baumodus nicht verfügbar. Prüfe die Objektanzeige unter Mods.":"Build mode unavailable. Check object visibility under Mods.";
            if(!access.EditorReady)
                return german?"Der Editor ist für diese Anlage gerade nicht verfügbar.":"The editor is currently unavailable for this layout.";
            return "";
        }
    }
}
