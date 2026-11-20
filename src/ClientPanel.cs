using System;
using System.Linq;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        void UpdateClient(SharedRuntime shared) {
            if(clientConfiguration!=shared.ConfigurationVersion){string selected=deskId;bool picker=panelPage==4;routes=shared.Routes();LoadPanel();if(desks.Desks.Any(d=>d.Id==selected))SelectDesk(selected,false);if(picker){panelPage=4;workspaceContext=true;}clientConfiguration=shared.ConfigurationVersion;Rebuild();}
            clientRuns=shared.LiveFresh&&shared.Live!=null?shared.Live.Runs.Where(r=>r.Definition!=null||routes.Any(d=>d.Id==r.Id)).Select(r=>r.ToRun(routes.FirstOrDefault(d=>d.Id==r.Id))).ToArray():new RouteRun[0];if(panel!=null&&PanelVisible)BuildRouteCues();RefreshActiveInspector();status=shared.Error??(shared.LiveFresh?"Anlagenansicht vom Host. Bau und Bedienung beim Host.":"Warte auf aktuelle Host-Rückmeldung. Keine Fahrtanzeige aus alten gespeicherten Daten.");
        }
        void BuildClientInspector(UIPanelBuilder c) {
            var selected=panel?.At(selectedX,selectedY);if(NumberMirrors.IsMirror(selected))BuildNumberMirrorStatus(c,selected);if(selected?.Kind==PanelTileKind.Switch)c.AddField(LocalText.Get("Aktuelle Weichenlage"),()=>PointPosition(selected.Reference),UIPanelBuilder.Frequency.Periodic);
            c.AddLabel(LocalText.Get("Gemeinsame Anzeige. Editor und Fahrstraßen-/Weichenbedienung sind in dieser Testfassung beim Host."));
            c.AddField(LocalText.Get("Fahrstraßen"),()=>VisibleRuns.Count==0?"Keine aktuelle Fahrstraße":string.Join("\n",VisibleRuns.Select(r=>r.Route.Code+" · "+Code(r.Route.Entry)+" → "+Code(r.Route.Exit)+" · "+r.Stage)),UIPanelBuilder.Frequency.Periodic);
            c.AddField(LocalText.Select("Release status","Auflösungsstatus"),()=>DispatcherReleaseFeedback()??LocalText.Select("No release pending.","Keine Auflösung ausstehend."),UIPanelBuilder.Frequency.Periodic);
        }
    }
}
