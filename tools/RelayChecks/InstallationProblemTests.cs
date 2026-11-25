using System;
using System.Linq;
using EnusDE.Signaltechnik;
static class InstallationProblemTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    public static void Run() {
        var report=new InstallationProblemReport();report.Begin("world",1,2);bool continued=false;
        report.Check("Points","Station Oak","W12",()=>throw new InvalidOperationException("native-segment-abc"));
        report.Check("Routes","Station Birch","R1",()=>continued=true);
        var issue=report.Items.Single();
        Check(continued&&issue.Incomplete&&issue.Owner=="Station Oak"&&issue.Item=="W12","failed module check retains location, remains unverified and allows later checks to finish");
        Check(issue.Technical=="native-segment-abc"&&!issue.Cause.Contains("native-segment-abc")&&!string.IsNullOrWhiteSpace(issue.Action)&&!string.IsNullOrWhiteSpace(issue.Effect),"unknown native failure is kept separately from plain-language cause, effect and next action without inventing a diagnosis");
        Check(!report.Stale("world",1,2)&&report.Stale("other",1,2)&&report.Stale("world",2,2)&&report.Stale("world",1,3),"world, configuration and installation changes invalidate the diagnostic snapshot");
        foreach(RouteStage stage in Enum.GetValues(typeof(RouteStage)))Check(InstallationProblem.RouteFault(stage)==(stage==RouteStage.Fault),"diagnosis does not confuse route waiting, preparation, cancellation or normal running with a retained fault · "+stage);
        foreach(string language in new[]{"en","de"}) {
            LocalText.Configure(()=>language);
            foreach(var fault in new[]{GfmBoundaryFault.Missing,GfmBoundaryFault.Planned,GfmBoundaryFault.Disabled,GfmBoundaryFault.Moved,GfmBoundaryFault.PointUnavailable,GfmBoundaryFault.Unavailable}) {
                string action=InstallationProblem.BoundaryAction(fault);
                Check(!string.IsNullOrWhiteSpace(action)&&action.Length>30,"GFM boundary fault has actionable guidance · "+language+" · "+fault);
            }
            Check(InstallationProblem.BoundaryAction(GfmBoundaryFault.Moved)!=InstallationProblem.BoundaryAction(GfmBoundaryFault.Planned)&&InstallationProblem.BoundaryAction(GfmBoundaryFault.PointUnavailable)!=InstallationProblem.BoundaryAction(GfmBoundaryFault.Disabled),"moved, unfinished and point-boundary failures lead to different repair actions · "+language);
        }
        LocalText.Configure(null);report.Begin("new",7,8);
        Check(report.Items.Count==0&&!report.Stale("new",7,8),"fresh inspection clears previous-world findings without retaining a false defect");
    }
}
