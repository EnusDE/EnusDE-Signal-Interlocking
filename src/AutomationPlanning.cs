using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public enum AutomationDrivingMode { Manual,Road,Yard,Waypoint }
    public sealed class AutomationIntent {
        public AutomationDrivingMode Mode;
        public bool Ready=true;
        public string Signature="",Reason;
        public MarkerAnchor Target;
        public List<string> Segments=new List<string>();
    }
    public static class AutomationPlanPolicy {
        public static List<RouteChainPlan> MatchingPlans(IEnumerable<RouteChainPlan> candidates,AutomaticSignal rule,AutomationIntent intent) {
            var matches=new List<RouteChainPlan>();InvalidOperationException failure=null;
            foreach(var candidate in candidates) {
                try {var plan=Limit(candidate,rule,intent);if(!matches.Any(p=>p.Routes.Select(r=>r.Id).SequenceEqual(plan.Routes.Select(r=>r.Id))))matches.Add(plan);}
                catch(InvalidOperationException e){failure=e;}
            }
            if(matches.Count==0&&failure!=null)throw failure;return matches;
        }
        public static RouteChainPlan Limit(RouteChainPlan plan,AutomaticSignal rule,AutomationIntent intent) {
            if(plan?.Routes==null||plan.Routes.Count==0||plan.Routes.Any(r=>r.Shunting))throw new InvalidOperationException("Kontakt-Automatik benötigt eine vollständige Zugfahrstraße.");
            if(intent==null||!intent.Ready)throw new InvalidOperationException(intent?.Reason??"Fahrauftrag nicht lesbar.");
            if(intent.Mode==AutomationDrivingMode.Yard)throw new InvalidOperationException("Rangiermodus fordert keine automatische Zugfahrstraße an.");
            if(intent.Mode==AutomationDrivingMode.Manual&&!rule.AllowManual)throw new InvalidOperationException("Handbetrieb ist an diesem Kontakt nicht freigegeben.");
            if(intent.Mode!=AutomationDrivingMode.Waypoint)return plan;
            if(!rule.AllowWaypoint)throw new InvalidOperationException("Wegpunktanforderungen sind an diesem Signal ausgeschlossen.");
            if(intent.Target==null||intent.Segments.Count==0)throw new InvalidOperationException("Wegpunkt-Ziel oder geplanter Gleisweg nicht sicher lesbar.");
            var first=plan.Routes[0].Path?.FirstOrDefault()??throw new InvalidOperationException("Gerichteter Fahrweg fehlt.");
            if(first.Section.Segment==intent.Target.Segment) {
                double start=first.Reverse?first.Section.To:first.Section.From;
                if((intent.Target.Meters-start)*(first.Reverse?-1:1)<=.1)throw new InvalidOperationException("Wegpunkt endet vor/am Startsignal; keine Durchstellung.");
            }
            int previous=-1;
            for(int i=0;i<plan.Routes.Count;i++) {
                foreach(var piece in plan.Routes[i].Path??throw new InvalidOperationException("Gerichteter Fahrweg fehlt.")) {
                    int index=intent.Segments.IndexOf(piece.Section.Segment);
                    if(index<previous||index<0||intent.Segments.LastIndexOf(piece.Section.Segment)!=index)
                        throw new InvalidOperationException("Fahrweg passt nicht eindeutig zum tatsächlichen Wegpunkt-Gleisweg.");
                    previous=index;
                    if(piece.Section.Segment==intent.Target.Segment&&intent.Target.Meters>=piece.Section.From-.01&&intent.Target.Meters<=piece.Section.To+.01)
                        return new RouteChainPlan{Routes=plan.Routes.Take(i+1).ToList(),EndReason="Wegpunkt im Zielabschnitt; folgende Signale bleiben Halt."};
                }
            }
            int target=intent.Segments.IndexOf(intent.Target.Segment);
            if(target<previous||target<0)throw new InvalidOperationException("Wegpunkt endet vor der angeforderten Durchfahrt.");
            return plan;
        }
    }
    public enum AutomaticRequestStage { Waiting,Submitted,Completed,Cancelled }
    public sealed class AutomaticRequest {
        public string Id=Guid.NewGuid().ToString("N"),Contact,Locomotive,Consist,Signal,Target,Reason,Intent;
        public string[] Routes=new string[0];
        public AutomaticRequestStage Stage;
        public double Created,NextAttempt,LastHead=double.NaN;
    }
    public sealed class ContactObservation {
        public string Id;
        public long ForwardAxles,ReverseAxles;
        public double PulseUntil;
        public string Detail="Noch keine Achsdaten erfasst.";
    }
    public sealed class AutomationStatus {
        public string Id="",State="",Reason="",Locomotive="",Signal="",Target="";
        public long Count,Balance;
        public int Direction;
        public int RequestedDirection;
        public long Revision;
    }
    public sealed class AutomationLiveFrame {
        public string Status="";
        public List<AutomationStatus> Requests=new List<AutomationStatus>(),Blocks=new List<AutomationStatus>(),Contacts=new List<AutomationStatus>(),Lines=new List<AutomationStatus>();
        public void Validate() {
            if(Status==null||Status.Length>512||Requests==null||Requests.Count>64||Blocks==null||Blocks.Count>512||Contacts==null||Contacts.Count>64||Lines==null||Lines.Count>64)
                throw new System.IO.InvalidDataException("Automatik-Livemeldungen ungültig.");
            foreach(var s in Requests.Concat(Blocks).Concat(Contacts).Concat(Lines))if(s==null||s.Id==null||s.Id.Length>128||s.State==null||s.State.Length>128||s.Reason==null||s.Reason.Length>512||s.Locomotive==null||s.Locomotive.Length>128||s.Signal==null||s.Signal.Length>128||s.Target==null||s.Target.Length>128||s.Direction< -1||s.Direction>1)
                throw new System.IO.InvalidDataException("Ungültige Automatikmeldung.");
            if(Lines.Any(s=>s.RequestedDirection< -1||s.RequestedDirection>1||s.Revision<0))throw new System.IO.InvalidDataException(LocalText.Select("Invalid permission state.","Ungültiger Erlaubniszustand."));
        }
    }
}
