using System;
using System.Linq;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
static class SignalLampTestTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action a){try{a();return false;}catch{return true;}}
    public static void Run(){
        string physical=Guid.NewGuid().ToString("N"),back=Guid.NewGuid().ToString("N"),other=Guid.NewGuid().ToString("N");var ids=new[]{physical,back};var t=new SignalLampTest();
        Check(!t.Active&&!t.Holds(physical),"lamp test starts inactive without saved or implicit overrides");
        foreach(int gate in Enumerable.Range(0,4))Check(Reject(()=>t.Start(physical,physical,ids,LampTestPart.Main,LocalSignalAspect.Clear,10,gate!=0,gate!=1,gate!=2,gate!=3))&&!t.Active,"lamp test rejects missing host/editor/readiness/unlocked gate · "+gate);
        Check(Reject(()=>t.Start(physical,other,ids,LampTestPart.Main,LocalSignalAspect.Clear,10,true,true,true,true))&&Reject(()=>t.Start(physical,physical,ids,LampTestPart.Main,(LocalSignalAspect)999,10,true,true,true,true)),"stale face identities and undefined lamp aspects cannot enter test mode");
        Check(Reject(()=>t.Start(physical,physical,ids,LampTestPart.Main,LocalSignalAspect.Clear,double.NaN,true,true,true,true)),"non-finite test clocks cannot create unbounded test sessions");
        t.Start(physical,physical,ids,LampTestPart.Main,LocalSignalAspect.Clear,10,true,true,true,true);
        Check(Reject(()=>t.Start(physical,other,ids,LampTestPart.Main,LocalSignalAspect.Clear,10,true,true,true,true))&&t.Holds(physical)&&t.FaceId==physical,"a stale replacement request cannot drop an existing native test hold");
        Check(t.Holds(physical)&&t.Holds(back)&&!t.Holds(other)&&t.Visual(physical,LocalSignalAspect.Invalid)==LocalSignalAspect.Clear&&t.Visual(other,LocalSignalAspect.Stop)==LocalSignalAspect.Stop,"visual Clear is scoped to the selected face while both physical directions are held for native Stop");
        Check(t.Blocks(new RouteDefinition{Entry=physical})&&t.Blocks(new RouteDefinition{Exit=back})&&t.Blocks(new RouteDefinition{ViaSignals=new List<RouteViaSignal>{new RouteViaSignal{Id=physical}}}),"test-held signals block route entry, destination and intermediate signal use");
        Check(t.Blocks(new RouteDefinition{Flanks=new List<RouteFlank>{new RouteFlank{Signals=new List<FlankSignal>{new FlankSignal{Id=physical}}}}})&&!t.Blocks(new RouteDefinition{Entry=other,Exit=other}),"test-held flank signals block admission without suspending unrelated routes");
        Check(!t.Expired(129.9)&&t.Expired(130)&&t.Expired(double.NaN),"lamp test has a bounded two-minute deadline and fails closed for invalid clocks");
        t.Start(physical,back,ids,LampTestPart.Distant,LocalSignalAspect.Slow,20,true,true,true,true);var plan=t.Plan(back,SignalLampPlan.For(LocalSignalAspect.Stop,LocalSignalAspect.Stop));
        Check(plan.Main==LocalSignalAspect.Clear&&plan.Distant==LocalSignalAspect.Slow&&plan.Shunt==LocalSignalAspect.Dark&&t.Visual(back,LocalSignalAspect.Stop)==LocalSignalAspect.Clear,"attached distant test uses an explicitly synthetic valid Clear/expect-slow combination without changing the hold");
        t.Start(physical,physical,ids,LampTestPart.Shunt,LocalSignalAspect.Stop,30,true,true,true,true);plan=t.Plan(physical,default);
        Check(plan.Main==LocalSignalAspect.Stop&&plan.Shunt==LocalSignalAspect.Stop&&t.Visual(physical,LocalSignalAspect.Clear)==LocalSignalAspect.Stop,"shunting Stop test cannot accidentally show a shunting permission on combined Hl heads");
        t.Start(physical,physical,ids,LampTestPart.Shunt,LocalSignalAspect.Shunt,30,true,true,true,true);plan=t.Plan(physical,default);
        Check(plan.Main==LocalSignalAspect.Stop&&plan.Shunt==LocalSignalAspect.Shunt&&t.Holds(physical),"shunting lamp test keeps red at the main head and native hold while showing Sh 1");
        foreach(LocalSignalAspect a in Enum.GetValues(typeof(LocalSignalAspect))){t.Start(physical,physical,ids,LampTestPart.Main,a,0,true,true,true,true);Check(t.Visual(physical,LocalSignalAspect.Stop)==a&&t.Holds(physical),"each supported indication is testable without movement authority · "+a);}
        t.Clear();t.Clear();var actual=SignalLampPlan.For(LocalSignalAspect.Clear,LocalSignalAspect.Slow);
        Check(!t.Active&&!t.Holds(physical)&&!t.Blocks(new RouteDefinition{Entry=physical})&&t.Visual(physical,LocalSignalAspect.Invalid)==LocalSignalAspect.Invalid&&t.Plan(physical,actual).Distant==LocalSignalAspect.Slow,"ending/repeating cleanup restores the actual invalid/operating indication and removes all temporary holds");
    }
}
