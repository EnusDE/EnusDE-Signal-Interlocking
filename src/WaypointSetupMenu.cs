using System;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string waypointSetupFeedback;

        internal void BuildWaypointSetup(UIPanelBuilder content,Action settingsApplied) {
            if(Automation==null) {
                content.AddLabel(LocalText.Get("Zuerst eine Welt laden."));
                return;
            }
            if(!StateManager.IsHost) {
                content.AddLabel(LocalText.Select("The host configures automatic waypoint requests.","Der Host richtet die automatischen Wegpunktanfragen ein."));
                return;
            }

            var worldAtOpen=scope;
            bool enabled=Automation.AutoDiscoverWaypoints&&aiEnabled;
            bool held=enabled&&WaypointDiscoveryHeld;
            content.AddLabel(held
                ?LocalText.Select("Automatic requests are paused after Stop All.","Automatische Anfragen sind nach „Alle Halt“ pausiert.")
                :enabled
                ?LocalText.Select("Automatic waypoint requests are enabled. Set a waypoint in Auto Engineer.","Wegpunktanfragen sind aktiv. Setze einen Wegpunkt im Auto Engineer.")
                :LocalText.Select("Enable the signal connection and block coordination for automatic requests. Automatic routes require full track detection.","Für automatische Anfragen werden Signalanbindung und Blockabstimmung aktiviert. Automatische Fahrstraßen benötigen eine durchgehende Gleisfreimeldung."));
            if(held)content.AddButton(LocalText.Select("Resume automatic requests","Automatische Anfragen fortsetzen"),()=>{
                if(scope!=worldAtOpen||!StateManager.IsHost)return;
                ResumeWaypointDiscovery();
                content.SetNeedsRebuild();
            });
            content.AddButton(enabled
                ?LocalText.Select("Disable automatic waypoint requests","Automatische Wegpunktanfragen ausschalten")
                :LocalText.Select("Enable automatic waypoint requests","Automatische Wegpunktanfragen einschalten"),()=>{
                    try {
                        if(scope!=worldAtOpen)throw new InvalidOperationException(LocalText.Select("World changed. Reopen settings.","Welt gewechselt. Einstellungen neu öffnen."));
                        SetWorldWaypointDiscovery(!enabled);
                        settingsApplied();
                        waypointSetupFeedback=null;
                    } catch(Exception error) {waypointSetupFeedback=error.Message;}
                    content.SetNeedsRebuild();
                });
            if(waypointSetupFeedback!=null)content.AddLabel(waypointSetupFeedback);
        }

        void SetWorldWaypointDiscovery(bool enabled) {
            RequireEdit();
            if(!enabled) {
                SaveAutomation(d=>d.Automation.AutoDiscoverWaypoints=false);
                return;
            }
            if(automationHookError!=null)throw new InvalidOperationException(automationHookError);
            if(!Automation.BlockEnabled&&(engine.Active.Count>0||directionBlocks.Leases.Count>0))
                throw new InvalidOperationException(LocalText.Select("Finish active routes before enabling block coordination.","Aktive Fahrstraßen abschließen, bevor die Blockabstimmung eingeschaltet wird."));

            var previous=Entry.SettingsSnapshot();
            if(!previous.Markers.EnableAiSignals) {
                var next=previous.Clone();
                next.Markers.EnableAiSignals=true;
                Entry.SaveSettings(next);
            }
            try {
                SaveAutomation(d=>{d.Automation.BlockEnabled=true;d.Automation.AutoDiscoverWaypoints=true;});
            } catch {
                if(!previous.Markers.EnableAiSignals&&Automation?.AutoDiscoverWaypoints!=true)
                    Entry.SaveSettings(previous);
                throw;
            }
            ResumeWaypointDiscovery();
        }
    }
}
