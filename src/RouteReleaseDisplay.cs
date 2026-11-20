using System;
using System.Linq;

namespace EnusDE.Signaltechnik {
    // Display only: time expiry is never treated as proof that a lock was released.
    public static class RouteReleaseDisplay {
        public static string Caption(RouteRun run,double remaining) {
            if(run.ReleaseSelection!=null) {
                if(remaining>0)return LocalText.Select("Auxiliary release · protection time remaining: ","Hilfsauflösung · Schutzwartezeit noch: ")+Math.Ceiling(remaining)+" s";
                return LocalText.Select("Protection time elapsed · ","Schutzwartezeit abgelaufen · ")+
                    (!run.ReleaseWaitCompleted||string.IsNullOrEmpty(run.ReleaseStatus)?LocalText.Select("release checks pending; locks remain held.","Auflösungsprüfung läuft; Verschlüsse bleiben gehalten."):run.ReleaseStatus);
            }
            if(run.Stage==RouteStage.WaitingProtection&&remaining>0)
                return LocalText.Select("Withdrawal · protection time remaining: ","Rücknahme · Schutzwartezeit noch: ")+Math.Ceiling(remaining)+" s";
            if(run.Stage==RouteStage.Cancelled)return LocalText.Select("Release blocked; no countdown. Check the cause and request release again: ","Auflösung blockiert; kein Zeitablauf. Ursache prüfen und Auflösung erneut anfordern: ")+run.Reason;
            return run.Reason;
        }
    }
    public sealed partial class SignalRuntime {
        string ReleaseProgress(RouteRun run) {
            var current=VisibleRuns.FirstOrDefault(r=>r.Route.Id==run.Route.Id);
            if(current==null)return Game.State.StateManager.IsHost||Entry.Shared?.LiveFresh==true?
                LocalText.Select("No remaining lock for this route.","Für diese Fahrstraße besteht kein Restverschluss mehr."):
                LocalText.Select("Waiting for current host feedback.","Warte auf aktuelle Host-Rückmeldung.");
            return RouteReleaseDisplay.Caption(current,Game.State.StateManager.IsHost?engine.RemainingWait(current):current.ReportedWaitRemaining??0);
        }
        string DispatcherReleaseFeedback() {
            if(editingWorkspace)return null;
            var pending=ActiveHere().Where(r=>r.ReleaseSelection!=null||r.Stage==RouteStage.Cancelled||r.Stage==RouteStage.WaitingProtection)
                .OrderByDescending(r=>r.Route.Id==routeId).ThenByDescending(r=>r.ReleaseSelection!=null||r.Stage==RouteStage.WaitingProtection).FirstOrDefault();
            return pending==null?null:pending.Route.Code+" · "+ReleaseProgress(pending);
        }
    }
}
