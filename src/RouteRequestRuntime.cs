using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        bool OnDemandRoutes=>Automation?.OnDemandRoutes??true;
        IEnumerable<RouteDefinition> OperationalRoutes=>engine.Active.Select(r=>r.Route).Concat(routes).GroupBy(r=>r.Id).Select(g=>g.First());
        void BuildRouteModeSettings(UIPanelBuilder c) {
            string selectedScope=scope;bool draft=OnDemandRoutes,allow=Automation.AllowUnmonitoredManualRoutes,catalog=Automation.AutomaticGfmCatalog;
            string passage=SignUnits.Number(Automation.SignalPassageMeters);
            c.AddSection(LocalText.Select("Route calculation","Fahrwegermittlung"));
            c.AddField(LocalText.Select("Signal passage point behind signal (m)","Haltfallpunkt hinter dem Signal (m)"),c.AddInputField(passage,v=>passage=v,"5",8));
            c.AddLabel(LocalText.Select("The same directional train-front passage drops the signal and advances its number. Default 5 m; adjustable 0–25 m. Short routes use at most half their length. Occupancy and train-tail release remain separate.","Dieselbe gerichtete Passage der Zugspitze löst Haltfall und Nummernweitergabe aus. Standard 5 m, einstellbar 0–25 m. Bei kurzen Fahrwegen höchstens deren halbe Länge. Belegung und Auflösung hinter dem Zugschluss bleiben getrennt."));
            c.AddFieldToggle(LocalText.Select("Calculate routes on request (experimental)","Fahrwege bei Anforderung berechnen (experimentell)"),()=>draft,v=>draft=v,true);
            c.AddFieldToggle(LocalText.Select("Allow manual routes with incomplete GFM coverage","Manuelle Fahrstraßen bei unvollständiger GFM zulassen"),()=>allow,v=>allow=v,true);
            c.AddFieldToggle(LocalText.Select("Automatic GFM background catalog (experimental)","Automatischer GFM-Hintergrundkatalog (experimentell)"),()=>catalog,v=>catalog=v,true);
            c.AddLabel(LocalText.Select("The dispatcher checks unmonitored areas. They remain marked unmonitored; ordinary occupancy, point, flank and lock checks remain active. Contact automation still requires complete GFM coverage.","Unüberwachte Bereiche prüft der Fahrdienstleiter. Sie bleiben als unüberwacht dargestellt; normale Belegungs-, Weichen-, Flanken- und Verschlussprüfungen bleiben aktiv. Die Kontakt-Automatik benötigt weiterhin vollständige GFM-Abdeckung."));
            c.AddLabel(LocalText.Select(
                "Enabled: calculate each requested route with full protection checks; no new saved route catalogue entries. Disabled: use the previous saved-route workflow. Existing definitions are retained.",
                "Aktiv: jeden angeforderten Fahrweg mit vollständigen Schutzprüfungen berechnen, ohne neue Katalogeinträge. Aus: bisheriger Ablauf mit gespeicherten Fahrstraßen. Vorhandene Definitionen bleiben erhalten."));
            c.AddButton(LocalText.Select("Apply route mode","Fahrwegmodus übernehmen"),()=>Act(()=>{
                if(scope!=selectedScope)throw new InvalidOperationException(LocalText.Select("World changed. Reopen settings.","Welt gewechselt. Einstellungen erneut öffnen."));
                double meters=ReadAutomationNumber(passage);if(meters<0||meters>25)throw new InvalidOperationException(LocalText.Select("Signal passage offset must be 0–25 m.","Haltfallpunkt muss 0–25 m hinter dem Signal liegen."));
                if(draft==OnDemandRoutes&&allow==Automation.AllowUnmonitoredManualRoutes&&catalog==Automation.AutomaticGfmCatalog&&meters==Automation.SignalPassageMeters)return;
                RouteRequestPolicy.RequireModeChange(engine.Active.Count>0,directionBlocks.Leases.Count>0,lineDirections.HasHolds,
                    routeWork.Count>0||automaticRequests.Any(r=>r.Stage==AutomaticRequestStage.Waiting||r.Stage==AutomaticRequestStage.Submitted));
                SaveAutomation(d=>{d.Automation.OnDemandRoutes=draft;d.Automation.AllowUnmonitoredManualRoutes=allow;d.Automation.AutomaticGfmCatalog=catalog;d.Automation.SignalPassageMeters=meters;});ClearAutomaticRouteChoices();panelButtons.Clear();
                status=LocalText.Select("Route mode applied. Protection checks remain active.","Fahrwegmodus übernommen. Schutzprüfungen bleiben aktiv.");Rebuild();
            }));
        }
        void PrepareRequestedPlan(RouteChainPlan plan) {
            foreach(var route in plan.Routes)if(!world.PathValid(route))throw new InvalidOperationException(LocalText.Select(
                "The track layout changed. Select start and destination again.","Fahrweg inzwischen geändert. Start und Ziel erneut wählen."));
            if(OnDemandRoutes) {
                // Only the installation connection/material status is persistent. The calculated
                // route stays with its live engine run and is not appended to the editor catalogue.
                Entry.Shared.ConnectRoutePoints(plan.Routes.SelectMany(r=>r.Switches).Select(p=>p.Node));
                return;
            }
            var additions=plan.Routes.Where(r=>!routes.Any(old=>old.Id==r.Id)).ToList();
            if(routes.Count+additions.Count>128)throw new InvalidOperationException(LocalText.Select(
                "The saved route catalogue is full (128 routes).","Der gespeicherte Fahrstraßenkatalog ist voll (128 Fahrstraßen)."));
            if(additions.Count>0){var next=routes.Concat(additions).ToList();store.Save(next);routes=next;}
        }
    }
}
