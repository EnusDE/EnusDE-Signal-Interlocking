using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.State;
using Newtonsoft.Json;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SharedRuntime {
        InstallationBackups backups;
        Dictionary<string,string> restoreJournal;
        int backupTransactionDepth;
        bool backupDirty=true,backupCheckpoint=true,restoringBackup,recoverySkipped;
        string recoverySource,backupState="Anlagensicherung wird vorbereitet.",backupError;
        double nextBackupRetry;
        public string RecoveryNotice {get;private set;}
        public string BackupDirectory=>backups?.DirectoryPath??Path.Combine(directory,"Backups");
        internal void CheckpointInstallation(){
            RequireHost();
            if(!ConfigurationReady||backups==null||backupTransactionDepth>0||restoringBackup||BackupSource()==null)
                throw new InvalidOperationException(LocalText.Select("External backup is not ready. Wait for the world to finish loading before changing installation diagrams.","Externe Sicherung ist noch nicht bereit. Vor Änderungen an den Anlagentafeln das vollständige Laden der Welt abwarten."));
            TryBackup(true);if(backupError!=null)throw new IOException(backupError);
        }
        string BackupSource() {
            string path=StateManager.Shared.SaveManager?.RailroadPath,map=StateManager.Shared.CurrentMapIdentifier;
            return string.IsNullOrWhiteSpace(path)||string.IsNullOrWhiteSpace(map)?null:MarkerRepository.Key(path,map);
        }
        string BackupName(){string name=Path.GetFileName((StateManager.Shared.SaveManager?.RailroadPath??"Spielwelt").TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar));return name.Length>128?name.Substring(0,128):name;}
        void BackupChanged() {backupDirty=true;if(backupTransactionDepth==0&&!restoringBackup)TryBackup();}
        void TryBackup(bool manual=false) {
            if(!StateManager.IsHost||!ConfigurationReady||backupTransactionDepth>0||restoringBackup||!backupDirty&&!manual)return;
            try {
                string source=BackupSource();if(source==null)return;
                var data=InstallationBackupData.Capture(installation,sharedDesks,sharedRoutes);
                backups.Save(data,source,BackupName(),manual?"Manuelle dauerhafte Sicherung":"Automatische Anlagensicherung",manual,backupCheckpoint);
                backupCheckpoint=false;backupDirty=false;backupError=null;backupState="Extern gesichert: "+DateTime.UtcNow.ToString("u")+" · "+data.Installation.Objects.Markers.Count+" Objekte / "+data.Desks.Desks.Count+" Stellwerke / "+data.Routes.Routes.Count+" Fahrstraßen.";
            }catch(Exception e){if(backupError!=e.Message)log?.Warning("Externe Anlagensicherung fehlgeschlagen; bisherige Sicherungen bleiben erhalten: "+e.Message);backupError=e.Message;backupDirty=true;nextBackupRetry=Time.realtimeSinceStartup+5;}
        }
        bool NeedsRecoveryDecision(string source,string map) {
            if(recoverySkipped)return false;
            if(recoverySource!=source) {
                recoverySource=source;
                RecoveryNotice=backups.Worlds(map).Any(w=>w.Sources.Contains(source))?
                    "Im Spielstand fehlen Signaltechnik-Anlagendaten. Externe Sicherungen gefunden. Unter Auswahlfenster → Einstellungen → Anlagensicherung einen Stand wählen oder ausdrücklich ohne Wiederherstellung beginnen.":null;
            }
            return RecoveryNotice!=null;
        }
        void RequireBackupRestore() {
            RequireHost();Entry.CheckBackupRestore();
            if(TrainController.Shared==null||TrainController.Shared.Cars.Any(c=>c!=null&&!c.IsInBardo&&!c.IsStopped(.05f)))throw new InvalidOperationException("Für eine Wiederherstellung zuerst alle Fahrzeuge anhalten.");
        }
        void RestoreBackup(InstallationBackupSelection selection,string expectedHash) {
            RequireBackupRestore();var selected=backups.Read(selection);
            if(selected.Envelope.Hash!=expectedHash)throw new InvalidOperationException("Sicherungsdatei inzwischen geändert. Vorschau erneut öffnen.");
            string map=StateManager.Shared.CurrentMapIdentifier,source=BackupSource();if(source==null)throw new InvalidOperationException("Spielstandzuordnung noch nicht bereit.");
            var restored=selected.Data.RestoreFor(map,WorldKey??selected.Data.WorldKey);
            if(ConfigurationReady)backups.Save(InstallationBackupData.Capture(installation,sharedDesks,sharedRoutes),source,BackupName(),"Vor ausdrücklich gewählter Wiederherstellung",true);
            foreach(var marker in restored.Installation.Objects.Markers) {
                var before=installation?.Objects.Markers.FirstOrDefault(m=>m.Id==marker.Id);
                marker.VisualRevision=checked(Math.Max(marker.VisualRevision,before?.VisualRevision??0)+1);
            }
            restored.Validate();var oldInstall=installation;var oldDesks=sharedDesks;var oldRoutes=sharedRoutes;long oldIv=InstallationVersion,oldCv=ConfigurationVersion;
            Entry.SuspendBackupRestore();restoringBackup=true;restoreJournal=new Dictionary<string,string>();
            try {
                using(StateManager.TransactionScope()) {
                    documents.Replace("desks",restored.Desks,DeskLimit);documents.Replace("routes",restored.Routes,RouteLimit);
                    documents.Replace("install",restored.Installation,InstallLimit);
                    installation=restored.Installation;sharedDesks=restored.Desks;sharedRoutes=restored.Routes.Routes;InstallationVersion++;ConfigurationVersion++;
                    var stops=new SharedLiveFrame{Epoch=epoch,Sequence=++sequence};foreach(var signal in SignalCatalogue.Expand(installation.Objects.Markers).Where(m=>m.IsSignal))stops.Aspects[signal.Id]=LocalSignalAspect.Stop;
                    documents.Replace("live",stops,FrameLimit);
                    backups.SaveJournal(restored.WorldKey,source,map,restoreJournal);
                }
            }catch {
                var journal=restoreJournal;restoreJournal=null;
                foreach(var pair in journal.Reverse())Write(pair.Key,pair.Value);
                installation=oldInstall;sharedDesks=oldDesks;sharedRoutes=oldRoutes;InstallationVersion=oldIv;ConfigurationVersion=oldCv;Entry.CancelBackupRestore();throw;
            }finally{restoreJournal=null;restoringBackup=false;}
            recoverySkipped=true;RecoveryNotice=null;Error=null;changed.Clear();backupDirty=true;backupCheckpoint=true;
            try{Entry.ReloadBackupConfiguration();}catch(Exception e){log?.Warning("Anlage wiederhergestellt; Ansichten benötigen Neuladen: "+e.Message);TryBackup();backupMessage="Anlagendaten übernommen. Spielstand speichern und Welt neu laden: "+e.Message;return;}
            TryBackup();
            backupMessage="Anlage wiederhergestellt. Alle Signale starten Halt; Kontakt-Automatik und Lok-Festlegungen sind zurückgesetzt. Spielstand jetzt regulär speichern.";
        }
    }
}
