using System;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        LineRemoval pendingLineRemoval;
        long removalInstallation,removalConfiguration;
        string removalScope;
        void PreviewLineRemoval(string id) {
            RequirePanelEdit();LineRemoval.RequireClear(desks,id,engine.Active,directionBlocks.Leases,lineDirections.States,world.Occupancy);
            pendingLineRemoval=LineRemoval.Preview(desks,id);removalInstallation=Entry.Shared.InstallationVersion;removalConfiguration=Entry.Shared.ConfigurationVersion;removalScope=scope;RefreshWorkspaceFields();
        }
        void BuildLineRemoval(UIPanelBuilder c) {
            var proposal=pendingLineRemoval;c.AddSection(WindowText("Delete line connection · preview","Streckenverbindung löschen · Vorschau"));c.AddLabel(proposal.Caption);
            c.AddLabel(WindowText("The connection and its permission are removed. World signals, GFM sections, drawn fields and train-number sources stay available. Managed block/number groups become ordinary drawing areas. No object is demolished.","Die Verbindung und ihre Erlaubnis werden entfernt. Weltsignale, GFM-Abschnitte, gezeichnete Felder und Zugnummernquellen bleiben verfügbar. Verwaltete Block-/Zugnummerngruppen werden zu normalen Zeichenbereichen. Kein Weltobjekt wird abgebaut."));
            c.AddLabel(WindowText("Permission fields to unlink: ","Zu lösende Erlaubnisfelder: ")+proposal.Indicators);
            c.AddLabel(WindowText("Auxiliary buttons to leave unassigned: ","Danach unzugeordnete Hilfstasten: ")+proposal.Auxiliaries);
            c.AddLabel(WindowText("Groups to release: ","Zu lösende Gruppenbindungen: ")+proposal.Groups);
            c.AddLabel(WindowText("Block signal automation / contacts to disable: ","Abzuschaltende Blocksignal-Automatik / Kontakte: ")+proposal.SignalRules+" / "+proposal.Contacts);
            c.AddLabel(WindowText("Previously bound route destinations become unassigned permission buttons. Their settings remain for reuse; they cannot silently request an unprotected train departure. Reconfigure affected saved routes before using them again.","Bisher gebundene Fahrstraßenziele werden zu unzugeordneten Erlaubnistasten. Ihre Einstellungen bleiben für eine neue Einrichtung erhalten; sie können nicht unbemerkt eine ungesicherte Zugausfahrt anfordern. Betroffene gespeicherte Fahrstraßen vor erneuter Verwendung neu einrichten."));
            c.AddButton(WindowText("Confirm deletion of this connection","Löschen dieser Verbindung bestätigen"),()=>Act(()=>{
                RequirePanelEdit();if(!ReferenceEquals(proposal,pendingLineRemoval)||scope!=removalScope||Entry.Shared.InstallationVersion!=removalInstallation||Entry.Shared.ConfigurationVersion!=removalConfiguration)throw new InvalidOperationException(WindowText("The installation changed. Cancel and prepare the deletion preview again.","Die Anlage wurde geändert. Abbrechen und Löschvorschau erneut erstellen."));
                LineRemoval.RequireClear(desks,proposal.Id,engine.Active,directionBlocks.Leases,lineDirections.States,world.Occupancy);
                Entry.Shared.CheckpointInstallation();deskStore.Save(proposal.Library);desks=Entry.Shared.Desks();panel=DeskPanel(CurrentDesk);
                pendingLineRemoval=null;lineDraft=null;selectedLine=null;lineIndicatorTarget=null;guidedPermissions.Clear();lineForwardPlans.Clear();lineReversePlans.Clear();ClearLineDetectionProposal();blockBuild=null;blockLayout=null;blockSetupLine=null;
                RefreshDirectionBlocks(true);RefreshLineDirections();panelButtons.Clear();ClearAutomaticRouteChoices();nativeDirty=true;panelView?.ReplaceDocument(panel);
                status=WindowText("Line connection deleted and links cleared. World objects and drawings retained; associated block automation disabled.","Streckenverbindung gelöscht und Verknüpfungen gelöst. Weltobjekte und Zeichnung erhalten; zugehörige Blockautomatik abgeschaltet.");RefreshWorkspaceFields();
            }));
            c.AddButton(WindowText("Cancel deletion","Löschen abbrechen"),()=>{pendingLineRemoval=null;RefreshWorkspaceFields();});
        }
    }
}
