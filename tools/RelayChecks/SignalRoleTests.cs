using System;
using System.Linq;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;

static class SignalRoleTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    public static void Run() {
        var library=new DeskLibrary();
        Check(SignalRolePolicy.Current(library,"front")==SignalDuty.Ordinary&&library.Automation.Signals.Count==0,"reading the default signal role does not create or activate an automation entry");
        SignalRolePolicy.Put(library,"front",SignalDuty.Entry);
        Check(library.Automation.Signals["front"].Duty==SignalDuty.Entry&&!library.Automation.Signals["front"].Enabled&&library.Automation.Signals["front"].Primary==""&&!library.Automation.BlockEnabled,"assigning an entry role needs no target automation and cannot enable block coordination");
        var original=library.Automation.Signals["front"];original.Enabled=true;original.Primary="destination";original.Alternate="alternate";original.Reduced=true;original.AllowWaypoint=false;original.RetrySeconds=8;
        SignalRolePolicy.Put(library,"front",SignalDuty.Intermediate);SignalRolePolicy.Put(library,"back",SignalDuty.Block);
        Check(ReferenceEquals(original,library.Automation.Signals["front"])&&original.Primary=="destination"&&original.Alternate=="alternate"&&original.Enabled&&original.Reduced&&!original.AllowWaypoint&&original.RetrySeconds==8&&library.Automation.Signals["back"].Duty==SignalDuty.Block,"role-only changes retain all target settings and keep the two mast directions independent");
        var round=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(library));
        Check(round.Automation.Signals["front"].Duty==SignalDuty.Intermediate&&round.Automation.Signals["back"].Duty==SignalDuty.Block&&(int)SignalDuty.Entry==1&&(int)SignalDuty.Exit==2&&(int)SignalDuty.Intermediate==3&&(int)SignalDuty.Block==4,"signal roles retain their existing serialized enum values and survive shared configuration transfer");
        library.FreeLines.Add(new LineTrackDefinition{Enabled=true,Mode=LineTrackMode.Gleiswechselbetrieb,ForwardStart="front",ForwardEnd="arrival",ReverseStart="back",ReverseEnd="return"});
        Check(Reject(()=>SignalRolePolicy.Put(library,"front",SignalDuty.Block))&&original.Duty==SignalDuty.Intermediate&&Reject(()=>SignalRolePolicy.Put(library,"arrival",SignalDuty.Exit))&&!library.Automation.Signals.ContainsKey("arrival"),"an incompatible role at an enabled free-line endpoint is rejected before changing or creating an entry");
        SignalRolePolicy.Put(library,"front",SignalDuty.Exit);SignalRolePolicy.Put(library,"arrival",SignalDuty.Entry);
        Check(original.Duty==SignalDuty.Exit&&library.Automation.Signals["arrival"].Duty==SignalDuty.Entry&&Reject(()=>SignalRolePolicy.Put(library,"other",(SignalDuty)99))&&!library.Automation.Signals.ContainsKey("other"),"matching entry/exit roles remain editable while unknown role values are rejected without mutation");
        LocalText.Configure(()=>"de");var german=Enum.GetValues<SignalDuty>().Select(SignalRoleText.Description).ToArray();var germanNames=SignalRoleText.Choices();
        LocalText.Configure(()=>"en");var english=Enum.GetValues<SignalDuty>().Select(SignalRoleText.Description).ToArray();
        Check(german.Length==5&&german.All(s=>!string.IsNullOrWhiteSpace(s))&&english.All(s=>!string.IsNullOrWhiteSpace(s))&&german.Zip(english).All(p=>p.First!=p.Second)&&germanNames.Contains("Zwischensignal")&&SignalRoleText.Choices().Contains("Intermediate signal"),"all five operating roles provide distinct English and German labels and explanations");
        LocalText.Configure(()=>"auto");
    }
}
