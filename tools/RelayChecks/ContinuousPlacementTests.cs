using System;
using System.IO;
using System.Linq;
using EnusDE.Signaltechnik;

static class ContinuousPlacementTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static MarkerAnchor At(double meters)=>new MarkerAnchor{Segment="track",Meters=meters,Lateral=2.3};
    public static void Run(string root) {
        string directory=Path.Combine(root,"build","continuous-placement-"+Guid.NewGuid().ToString("N"));
        string key=MarkerRepository.Key(directory,"map");var repository=new MarkerRepository(directory,key,"map");
        var session=new MarkerEditSession();session.Start(new MarkerOptions{NewSignalsWithGfm=false},MarkerRecord.Main,true);session.HlSignal=true;session.Face(true);session.Offer(At(10));
        var first=session.Commit(repository,_=>true);
        Check(session.Repeating&&session.Aiming&&session.Candidate==null&&session.EditingId==null&&first.Kind==MarkerRecord.Main&&first.HlSignal&&first.FacingReverse&&first.CircuitVariant==0,
            "placing the first signal keeps the selected signal series active while clearing its committed anchor");
        Check(Reject(()=>session.Commit(repository,_=>true))&&repository.Records.Count==1,"a continuous signal series cannot duplicate the previous placement without a fresh valid anchor");
        session.Face(false);session.Offer(At(35));var second=session.Commit(repository,_=>true);
        Check(first.Id!=second.Id&&first.Name!=second.Name&&repository.Records.Count==2&&repository.Records.Single(m=>m.Id==first.Id).Anchor.Meters==10&&second.Anchor.Meters==35,
            "successive placement creates distinct signal identities and leaves previously placed signals at their own positions");
        Check(second.HlSignal&&!second.FacingReverse&&second.CircuitVariant==0&&session.Repeating,
            "continuous placement retains the Hl model and GFM default while accepting the next placement direction");
        session.Offer(At(60));session.Freeze();session.Offer(At(999));var third=session.Commit(repository,_=>true);
        Check(third.Anchor.Meters==60&&session.Aiming&&session.Repeating,"confirming a frozen first-track preview resumes the same continuous signal series");
        session.Offer(At(80));
        Check(Reject(()=>session.Commit(repository,_=>false))&&session.Repeating&&session.Candidate.Meters==80&&repository.Records.Count==3,
            "invalid track rejects a repeated placement without ending the mode or losing the pending preview");
        session.Offer(null);
        Check(Reject(()=>session.Commit(repository,_=>true))&&session.Repeating&&repository.Records.Count==3,"moving the cursor off track cannot place at the previous signal position");
        session.Offer(At(90));session.Cancel();
        Check(!session.Aiming&&!session.Repeating&&session.Candidate==null&&repository.Records.Count==3,"ending the series discards only its unsaved preview and retains all placed signals");
        session.Start(new MarkerOptions(),MarkerRecord.Shunt,true);session.Offer(At(100));var shunt=session.Commit(repository,_=>true);
        Check(session.Repeating&&shunt.Kind==MarkerRecord.Shunt&&!shunt.HlSignal&&shunt.CircuitVariant==1,"selecting a new signal type starts a fresh series with its own model and current GFM default");
        session.Start(new MarkerOptions(),MarkerRecord.Distant,true);session.Offer(At(120));var distant=session.Commit(repository,_=>true);
        Check(session.Aiming&&session.Repeating&&distant.Kind==MarkerRecord.Distant,"standalone distant signals support the same continuous placement workflow");
        session.Edit(first);session.Candidate.Meters=15;var moved=session.Commit(repository,_=>true);
        Check(!session.Repeating&&!session.Aiming&&moved.Id==first.Id&&repository.Records.Count==5&&moved.Anchor.Meters==15,
            "editing an existing signal ends creation mode and moves exactly that signal once");
        session.Start(new MarkerOptions(),MarkerRecord.Clearance,true);session.Offer(At(130));session.Commit(repository,_=>true);
        Check(!session.Aiming&&!session.Repeating,"non-signal objects retain their existing single-placement behavior");
        string blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"not a directory");var broken=new MarkerRepository(blocked,key,"map");
        session.Start(new MarkerOptions(),MarkerRecord.Main,true);session.Offer(At(140));
        Check(Reject(()=>session.Commit(broken,_=>true))&&broken.Records.Count==0&&session.Aiming&&session.Repeating&&session.Candidate.Meters==140,
            "a failed write does not fabricate a placed signal or discard the active repeated-placement preview");
    }
}
