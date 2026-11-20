using System;
using System.Linq;
using UI.Builder;
using Game.State;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        void OpenBlockingRoutes(RouteConflictException conflict) {
            if(editingWorkspace)return;
            routeId=conflict.BlockingRouteIds.FirstOrDefault();
            panelPage=6;ClearAutomaticRouteChoices();OpenWorkspaceContext();
        }
        void BuildAllActive(UIPanelBuilder c) {
            if(!StateManager.IsHost&&Entry.Shared?.LiveFresh!=true){c.AddLabel(LocalText.Get("Betriebsdaten nicht aktuell. Aktive Festlegungen können noch nicht zuverlässig angezeigt werden."));return;}
            var active=VisibleRuns.OrderBy(r=>r.Route.Code,StringComparer.Ordinal).ToArray();
            c.AddSection(LocalText.Get("Alle aktiven Fahrstraßen und Restfestlegungen"));
            c.AddLabel(LocalText.Get("Orange Striche im Gleisbild markieren Restverschlüsse. Die Belegungsanzeige bleibt davon unabhängig."));
            c.AddLabel(LocalText.Get("Auch Halt, Störungen und Restverschlüsse anderer Tafeln erscheinen hier. Ein Signal auf Halt bedeutet nicht, dass alle Verschlüsse frei sind."));
            if(active.Length==0){c.AddLabel(LocalText.Get("Keine aktive Fahrstraße oder Restfestlegung vorhanden."));return;}
            if(!active.Any(r=>r.Route.Id==routeId))routeId=active[0].Route.Id;
            c.AddSearchChoice(LocalText.Get("Festlegung"),active.Select(r=>r.Route.Code+" · "+SignalCaption(r.Route.Entry)+" → "+SignalCaption(r.Route.Exit)+" · "+RouteConflictException.State(r.Stage)).ToList(),
                Array.FindIndex(active,r=>r.Route.Id==routeId),i=>{routeId=active[i].Route.Id;RefreshWorkspaceFields();});
            var selected=active.First(r=>r.Route.Id==routeId);
            c.AddLabel(()=>RouteStatus(selected),UIPanelBuilder.Frequency.Periodic);
            c.AddLabel(LocalText.Get("Noch verschlossene Weichen: ")+string.Join(", ",selected.Route.Switches.Where(s=>!selected.ReleasedSwitches.Contains(s.Node)).Select(s=>PointAssignments.Caption(desks,s.Node))));
            c.AddLabel(LocalText.Get("Noch gehaltene Abschnitte: ")+selected.HeldSections.Count());
            if(!StateManager.IsHost)return;
            c.AddLabel(()=>"Freiprüfung: "+engine.BlockingReason(selected,world),UIPanelBuilder.Frequency.Periodic);
            c.AddButton(LocalText.Get("Halt und sicher auflösen"),()=>Act(()=>{routeId=selected.Route.Id;CancelRoute();RefreshWorkspaceFields();}));
            BuildRelease(c,selected);
        }
    }
}
