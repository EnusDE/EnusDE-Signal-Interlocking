using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class ConstructionProgressionTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static MethodInfo[] Calls(MethodInfo method) {
        var table=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(op=>(ushort)op.Value);
        var calls=new List<MethodInfo>();var bytes=method.GetMethodBody().GetILAsByteArray();
        for(int at=0;at<bytes.Length;) {
            ushort value=bytes[at++];if(value==0xfe)value=(ushort)(0xfe00|bytes[at++]);var op=table[value];int length;
            switch(op.OperandType){case OperandType.InlineNone:length=0;break;case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:length=1;break;case OperandType.InlineVar:length=2;break;case OperandType.InlineI8:case OperandType.InlineR:length=8;break;case OperandType.InlineSwitch:length=4+4*BitConverter.ToInt32(bytes,at);break;default:length=4;break;}
            if(op.OperandType==OperandType.InlineMethod&&method.Module.ResolveMethod(BitConverter.ToInt32(bytes,at)) is MethodInfo called)calls.Add(called);
            at+=length;
        }
        return calls.ToArray();
    }
    static ConstructionMilestoneInfo Phase(string id,bool completed=false,params string[] prerequisites)=>new ConstructionMilestoneInfo{Id=id,Completed=completed,Prerequisites=prerequisites};
    static ConstructionUnlockPlan Resolve(ConstructionMilestoneInfo[] milestones,ConstructionMapRule rule=null,ConstructionUnlockMode mode=ConstructionUnlockMode.MapDefault,string prerequisite="",bool unlocked=false,bool sandbox=false,string error=null)=>
        ConstructionProgressionPolicy.Resolve(mode,prerequisite,rule,milestones,unlocked,sandbox,error);
    public static void Run(string root) {
        var bridge=Phase("s2");var native=Phase("sctc",false,"s2");var baseMap=new[]{bridge,native};
        var before=Resolve(baseMap);
        Check(!before.Free&&!before.CanStart&&!before.AutoUnlock&&before.Cost==7500&&before.Prerequisites.SequenceEqual(new[]{"s2"}),"native-map signal construction shares the CTC expansion prerequisites and stays unpurchasable before the Ela bridge prerequisite");
        bridge.Completed=true;var after=Resolve(baseMap);
        Check(!native.Completed&&after.CanStart&&!after.AutoUnlock&&!after.Free&&after.Cost==7500,"after the native prerequisites complete, the independent signal programme is purchasable without buying CTC");
        Check(after.ChargeRequired&&!before.ChargeRequired&&!Resolve(baseMap,unlocked:true).ChargeRequired&&
            !Resolve(Array.Empty<ConstructionMilestoneInfo>()).ChargeRequired,"only an eligible unpaid purchase may charge; blocked, free and already unlocked programmes never debit funds");
        var renamedPrerequisite=Resolve(new[]{Phase("new-bridge",true),Phase("sctc",false,"new-bridge")});
        Check(renamedPrerequisite.CanStart&&renamedPrerequisite.Prerequisites.SequenceEqual(new[]{"new-bridge"}),"prerequisites come from the live native milestone instead of assuming a fixed bridge ID or translated title");
        Check(!Resolve(new[]{Phase("sctc",false,"missing")}).CanStart,"a referenced prerequisite missing from a native-like progression cannot silently unlock construction");
        var custom=Resolve(new[]{Phase("other-expansion")});
        Check(custom.Free&&custom.AutoUnlock&&custom.CanStart&&custom.Cost==0&&custom.Prerequisites.Length==0,"a custom map without native sctc immediately unlocks signalling without a purchase or a missing vanilla prerequisite");
        Check(Resolve(Array.Empty<ConstructionMilestoneInfo>()).AutoUnlock,"a map with no native milestones also defaults to available signalling");
        var multi=Resolve(new[]{Phase("bridge",true),Phase("track",false),Phase("sctc",false,"bridge","track")});
        Check(!multi.CanStart&&multi.Missing.SequenceEqual(new[]{"track"}),"all native signal prerequisites must complete, not merely one bridge phase");
        var authors=new ConstructionMapRule{Mode="purchase",Prerequisite="custom-bridge"};
        var authorLocked=Resolve(new[]{Phase("custom-bridge")},authors);var authorReady=Resolve(new[]{Phase("custom-bridge",true)},authors);
        Check(!authorLocked.CanStart&&!authorLocked.AutoUnlock&&authorReady.CanStart&&!authorReady.AutoUnlock&&authorReady.Cost==7500,"a custom map author can require its own completed milestone before the signalling purchase");
        Check(!Resolve(Array.Empty<ConstructionMilestoneInfo>(),authors).CanStart,"an absent author-specified milestone stays blocked instead of being treated as completed");
        Check(Resolve(baseMap,new ConstructionMapRule{Mode="free"}).AutoUnlock,"an explicit map free rule overrides the native-map purchase default");
        var freeLater=new ConstructionMapRule{Mode="free",Prerequisite="custom-bridge"};
        Check(!Resolve(new[]{Phase("custom-bridge")},freeLater).AutoUnlock&&Resolve(new[]{Phase("custom-bridge",true)},freeLater).AutoUnlock,"a map author can also grant free signalling only after its custom milestone completes");
        Check(Resolve(baseMap,new ConstructionMapRule()).Source=="native-signals"&&Resolve(Array.Empty<ConstructionMilestoneInfo>(),new ConstructionMapRule()).Source=="custom-map-free","explicit auto rules preserve the built-in capability-based fallback");
        Check(Resolve(Array.Empty<ConstructionMilestoneInfo>(),authors,ConstructionUnlockMode.Immediate).AutoUnlock&&
            !Resolve(baseMap,new ConstructionMapRule{Mode="free"},ConstructionUnlockMode.Purchase,"missing").CanStart,
            "explicit host free and purchase overrides take precedence over map creator rules");
        var host=Resolve(Array.Empty<ConstructionMilestoneInfo>(),null,ConstructionUnlockMode.Purchase);
        Check(host.CanStart&&!host.Free&&!host.AutoUnlock&&host.Cost==7500,"host can opt into an unrestricted purchase on an otherwise free custom map");
        Check(!Resolve(baseMap,null,ConstructionUnlockMode.MapDefault,"old-prerequisite").CanStart&&
            Resolve(baseMap,null,ConstructionUnlockMode.MapDefault,"old-prerequisite").Source=="host-purchase","an existing explicit legacy prerequisite is preserved as a host purchase rule");
        bridge.Completed=false;
        Check(Resolve(baseMap,unlocked:true).AutoUnlock&&Resolve(baseMap,unlocked:true).CanStart&&
            Resolve(Array.Empty<ConstructionMilestoneInfo>(),authors,unlocked:true).AutoUnlock,"existing unlocks survive changed map prerequisites and missing map phases without a second payment");
        Check(Resolve(baseMap,sandbox:true).AutoUnlock&&Resolve(baseMap,sandbox:true).Cost==0,"sandbox remains immediately unlocked and never gains a new purchase gate");
        var lockedAccess=new ModePickerAccess(true,true,true,true,true);
        Check(Enum.GetValues<WorkspaceMode>().All(mode=>!lockedAccess.CanOpen(mode))&&
            ModePickerText.Status(lockedAccess,true).Contains("Meilensteine")&&ModePickerText.Status(lockedAccess,false).Contains("Milestones"),
            "a progression lock blocks build, editor and dispatcher modes and explains the milestone prerequisite in both languages");
        var broken=Resolve(Array.Empty<ConstructionMilestoneInfo>(),error:"bad map rule");
        Check(!broken.CanStart&&!broken.AutoUnlock&&broken.Error!=null&&
            Resolve(baseMap,unlocked:true,error:"bad map rule").AutoUnlock&&Resolve(baseMap,mode:ConstructionUnlockMode.Immediate,error:"bad map rule").AutoUnlock,
            "broken map policies block new default unlocks while preserving existing installations and explicit host overrides");
        Check(Reject(()=>new ConstructionMapRule{Mode="typo"}.Validate())&&Reject(()=>new ConstructionMapRule{Schema=9}.Validate())&&
            Reject(()=>new ConstructionMapRule{Mode="auto",Prerequisite="bridge"}.Validate())&&Reject(()=>new ConstructionMapRule{Prerequisite="bad\n"}.Validate()),
            "malformed map rule versions, modes and prerequisite IDs are rejected");
        string folder=Path.Combine(root,"build","construction-policy-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        string mapPath=Path.Combine(folder,"Map.json"),rulePath=Path.Combine(folder,ConstructionMapRule.FileName);
        Check(ConstructionMapRule.Read(mapPath)==null,"maps without an optional progression file keep automatic behaviour");
        File.WriteAllText(rulePath,"{\"Schema\":1,\"Mode\":\"purchase\",\"Prerequisite\":\"custom-bridge\"}");
        var read=ConstructionMapRule.Read(mapPath);
        Check(read.Mode=="purchase"&&read.Prerequisite=="custom-bridge","map progression settings are read next to the active map data, without adding another mod dependency");
        File.WriteAllText(rulePath,"{\"Mode\":\"free\",\"Prerequisit\":\"typo\"}");Check(Reject(()=>ConstructionMapRule.Read(mapPath)),"a misspelled map field cannot silently turn a gated unlock into an immediate free unlock");
        File.WriteAllText(rulePath,new string('x',16385));Check(Reject(()=>ConstructionMapRule.Read(mapPath)),"oversized author progression files are bounded before parsing");
        string key=new string('c',64);var installation=new InstallationDocument{WorldKey=key,Map="custom",Objects=new MarkerDocument{WorldKey=key,Map="custom"}};
        installation.Economy.UnlockMode=ConstructionUnlockMode.Purchase;installation.Economy.UnlockPrerequisite="custom-bridge";installation.Economy.Unlocked=true;
        var clone=installation.Clone();clone.Validate();
        Check(clone.Economy.UnlockMode==ConstructionUnlockMode.Purchase&&clone.Economy.UnlockPrerequisite=="custom-bridge"&&clone.Economy.Unlocked,"shared save serialization retains the host policy, prerequisite and permanent unlock together");
        clone.Economy.UnlockMode=(ConstructionUnlockMode)88;Check(Reject(clone.Validate),"unknown saved unlock modes cannot pass installation validation");
        var legacy=JsonConvert.DeserializeObject<MaterialLedger>("{\"PolicyVersion\":1,\"Unlocked\":true}");legacy.Validate();
        Check(legacy.UnlockMode==ConstructionUnlockMode.MapDefault&&legacy.Unlocked,"old saves default to map rules without revoking their unlocked installations");
        var game=Assembly.Load("Railroader");var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var progression=game.GetType("Game.Progression.Progression",true);var pay=progression.GetMethod("PayToStartMilestone",flags);
        var instructions=Calls(pay);
        int gate=Array.FindIndex(instructions,i=>i.Name=="get_PrerequisitesCompleted");
        int debit=Array.FindIndex(instructions,i=>i.Name=="ApplyToBalance");
        Check(gate>=0&&debit>gate,"the installed native purchase path checks milestone prerequisites before charging company funds");
        Check(game.GetType("Game.ActiveMapDefinition",true).GetProperty("MapDataAbsolutePath",flags)?.PropertyType==typeof(string)&&
            game.GetType("Game.Progression.Milestone",true).GetProperty("Prerequisites",flags)!=null,"map-file lookup and prerequisite reuse target the actual installed game API");
    }
}
