using System;
using System.Linq;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SharedRuntime {
        string backupWorld,backupFile,backupMessage="",backupPreviewHash;
        InstallationBackupRead backupPreview;
        string[] backupWorlds=new string[0];InstallationBackupSelection[] backupCopies=new InstallationBackupSelection[0];
        internal void BuildBackups(UIPanelBuilder c) {
            c.AddLabel(LocalText.Get("Signaltechnik speichert den gemeinsamen Bestand weiterhin im Spielstand und sichert zusätzlich unabhängig im Datenordner des Hosts. Normales Entfernen des Modordners entfernt diese Sicherungen nicht."));
            c.AddField(LocalText.Get("Sicherungsordner"),()=>BackupDirectory,UIPanelBuilder.Frequency.Periodic);
            if(!StateManager.IsHost){c.AddLabel(LocalText.Get("Automatische Sicherung und Wiederherstellung erfolgen ausschließlich beim Host. Auf Clients werden keine fremden Anlagenkopien angelegt."));return;}
            c.AddField(LocalText.Get("Sicherung"),()=>backupError==null?backupState:"Sicherung fehlgeschlagen: "+backupError,UIPanelBuilder.Frequency.Periodic);
            if(RecoveryNotice!=null)c.AddLabel(RecoveryNotice);
            c.AddLabel(LocalText.Get("32 automatische Versionsstände; manuelle Sicherungen und Stände vor Löschungen werden dauerhaft aufbewahrt. Fahrtfreigaben, Belegungen, Zugnummernpositionen und wartende Automatikaufträge werden nicht wiederhergestellt."));
            c.AddButton(LocalText.Get("Aktuellen Anlagenstand dauerhaft sichern"),()=>{try{RequireHost();if(!ConfigurationReady)throw new InvalidOperationException("Vollständigen Anlagenbestand zuerst laden / wiederherstellen.");TryBackup(true);backupMessage=backupError==null?"Dauerhafte Sicherung angelegt.":"Nicht gesichert: "+backupError;}catch(Exception e){backupMessage=e.Message;}c.SetNeedsRebuild();});
            string map=StateManager.Shared?.CurrentMapIdentifier;
            InstallationBackupWorld[] worlds;try{worlds=map==null?new InstallationBackupWorld[0]:backups.Worlds(map);}catch(Exception e){backupMessage="Sicherungsliste nicht lesbar: "+e.Message;worlds=new InstallationBackupWorld[0];}
            backupWorlds=worlds.Select(w=>w.WorldKey).ToArray();
            if(backupWorlds.Length>0) {
                if(backupWorld==null){var preferred=worlds.FirstOrDefault(w=>string.Equals(w.WorldKey,WorldKey,StringComparison.OrdinalIgnoreCase))??worlds.FirstOrDefault(w=>w.Sources.Contains(BackupSource()));backupWorld=preferred?.WorldKey;}
                int at=Math.Max(0,Array.IndexOf(backupWorlds,backupWorld));backupWorld=backupWorlds[at];
                var choices=backupWorlds.ToArray();
                c.AddSearchChoice(LocalText.Get("Gesicherte Anlage auf dieser Karte"),worlds.Select(w=>w.Name+" · "+w.WorldKey.Substring(0,10)).ToList(),at,i=>{backupWorld=choices[i];backupFile=null;backupPreview=null;backupPreviewHash=null;c.SetNeedsRebuild();});
                try{var latest=backups.Latest(backupWorld);backupCopies=(latest==null?new InstallationBackupSelection[0]:new[]{latest}).Concat(backups.Copies(backupWorld)).ToArray();}catch(Exception e){backupMessage="Versionsstände nicht lesbar: "+e.Message;backupCopies=new InstallationBackupSelection[0];}var copies=backupCopies.ToArray();
                if(copies.Length>0) {
                    int index=Math.Max(0,Array.FindIndex(copies,s=>s.FileName==backupFile));backupFile=copies[index].FileName;
                    c.AddSearchChoice(LocalText.Get("Versionsstand"),copies.Select(s=>s.Caption).ToList(),index,i=>{backupFile=copies[i].FileName;backupPreview=null;backupPreviewHash=null;c.SetNeedsRebuild();});
                    var selection=new InstallationBackupSelection{WorldKey=backupWorld,FileName=backupFile};
                    c.AddButton(LocalText.Get("Gewählte Sicherung prüfen und Vorschau anzeigen"),()=>{try{RequireHost();backupPreview=backups.Read(selection);if(backupPreview.Data.Installation.Map!=StateManager.Shared.CurrentMapIdentifier)throw new InvalidOperationException("Falsche Karte.");backupPreviewHash=backupPreview.Envelope.Hash;backupMessage="Sicherung geprüft.";}catch(Exception e){backupPreview=null;backupPreviewHash=null;backupMessage="Nicht verwendbar: "+e.Message;}c.SetNeedsRebuild();});
                    if(backupPreview!=null&&backupPreviewHash!=null) {
                        var preview=backupPreview;string hash=backupPreviewHash,sourceAtPreview=BackupSource();
                        c.AddLabel(LocalText.Get("Gesichert: ")+preview.Envelope.CreatedUtc+" · Mod "+preview.Envelope.ModVersion+" · "+preview.Envelope.Reason);
                        c.AddLabel(preview.Data.Installation.Objects.Markers.Count+" Objekte, "+preview.Data.Desks.Desks.Count+" Stellwerke, "+preview.Data.Desks.Automation.Contacts.Count+" Kontakte und "+preview.Data.Routes.Routes.Count+" Fahrstraßen.");
                        c.AddLabel(LocalText.Get("Ersetzt die aktuelle Anlagenkonfiguration einschließlich Bau-/Materialbestand. Vorher wird der gegenwärtige Bestand gesichert. Nur bei stehenden Fahrzeugen, ohne aktive Fahrstraßen oder Blockbindungen. Nach Wiederherstellung sind alle Signale Halt und die Kontakt-Automatik ist ausgeschaltet."));
                        c.AddButton(LocalText.Get("Diesen Stand ausdrücklich wiederherstellen"),()=>{try{if(sourceAtPreview!=BackupSource()||backupWorld!=selection.WorldKey||backupFile!=selection.FileName||backupPreviewHash!=hash)throw new InvalidOperationException("Auswahl veraltet. Sicherung erneut prüfen.");RestoreBackup(selection,hash);backupPreview=null;backupPreviewHash=null;}catch(Exception e){backupMessage="Nicht wiederhergestellt: "+e.Message;}c.SetNeedsRebuild();});
                    }
                }
            } else c.AddLabel(LocalText.Get("Keine lesbaren externen Sicherungen für diese Karte gefunden."));
            if(!Ready&&RecoveryNotice!=null)c.AddButton(LocalText.Get("Ohne Wiederherstellung initialisieren – Sicherungen behalten"),()=>{try{RequireHost();recoverySkipped=true;RecoveryNotice=null;ImportIfNeeded();backupMessage="Anlage ohne Übernahme einer externen Sicherung initialisiert. Vorhandene Sicherungsdateien bleiben erhalten.";}catch(Exception e){backupMessage=e.Message;}c.SetNeedsRebuild();});
            c.AddLabel(backupMessage);c.AddButton(LocalText.Get("Sicherungsliste aktualisieren"),()=>c.SetNeedsRebuild());
        }
    }
}
