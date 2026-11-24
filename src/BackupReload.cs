using System;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerRuntime {
        internal void ReloadBackupConfiguration() {
            EndBuildMode();HideTool();edit.Cancel();scene.HidePreview();DisposeCrosses();scene.Dispose();scene=new MarkerScene(geometry,transform);scene.ApplyGlow(options.Glow);
            repository=null;scope=null;records=new MarkerRecord[0];selectedId=null;repositoryError=null;sharedVersion=-1;EnsureRepository();
        }
    }
    public sealed partial class SignalRuntime {
        bool backupSuspended;
        internal void CheckBackupRestore() {
            if(engine.Active.Count>0||directionBlocks.Leases.Count>0||lineDirections.HasHolds)throw new InvalidOperationException("Vor Wiederherstellung alle Fahrstraßen und Blockbindungen vollständig freifahren / sicher auflösen.");
        }
        internal void SuspendBackupRestore(){CheckBackupRestore();CancelRouteWork();backupSuspended=true;lastRead=-100;nativeDirty=true;PublishNativeAspects();}
        internal void CancelBackupRestore(){backupSuspended=false;nativeDirty=true;}
        internal void ReloadBackupConfiguration() {
            CheckBackupRestore();CancelRouteWork();lineDirections.Reset();lineValidationSignature=null;CloseWorkspaceMode();lastRead=-100;RemoveProxies();world.Dispose();world=new SignalWorldBridge(this,transform);
            ResetWaypointSession();automaticRequests.Clear();contactPassages.Clear();contactLatches.Clear();contactObservations.Clear();directionBlocks.Blocks.Clear();blockSignature=null;blockError=null;nativeBlockTopology=null;nextBlockTopology=0;
            ClearAutomaticRouteChoices();panelButtons.Clear();scope=Entry.Shared.WorldKey;store=new InterlockingStore(directory,scope,Entry.Shared.Routes,Entry.Shared.PutRoutes);
            routes=Entry.Shared.Routes();inventory=SignalCatalogue.Expand(objects.Objects).ToDictionary(m=>m.Id);LoadPanel();
            RefreshProxies();nativeDirty=true;PublishNativeAspects();backupSuspended=false;status=LocalText.Get("Anlage aus Sicherung geladen. Alle Signale Halt; Kontakt-Automatik ausgeschaltet.");
        }
    }
}
