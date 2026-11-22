using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EnusDE.Signaltechnik;
using EnusDE.Runenwacht;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
public static class PresentationPackTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    sealed class Provider:ISignalPresentationProvider {
        public int ApiVersion{get;set;}=1;public string Id{get;set;}="tests.presentation";public string Version=>"0.1.0";public string Name=>"Fixture";
        public ISignalPresentation Create(GameObject root,string kind,bool preview)=>throw new NotSupportedException();
    }
    static MarkerRecord Signal()=>new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name="HS-TEST",Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="synthetic",Meters=1,Scale=1}};
    public static void Run(string root) {
        int generation=SignalPresentationPacks.Generation;var p=new Provider();
        using(var registration=SignalPresentationPacks.Register(p)) {
            Check(SignalPresentationPacks.Find(p.Id)==p&&SignalPresentationPacks.Generation==generation+1,"optional presentation registration exposes only matching API metadata");
            Check(Reject(()=>SignalPresentationPacks.Register(new Provider())),"duplicate pack registrations cannot replace a live provider");
            Check(Reject(()=>SignalPresentationPacks.Register(new Provider{Id="tests.bad",ApiVersion=2}))&&Reject(()=>SignalPresentationPacks.Register(new Provider{Id="../bad"})),"incompatible API and unsafe pack identifiers are rejected");
        }
        Check(SignalPresentationPacks.Find(p.Id)==null&&SignalPresentationPacks.Generation==generation+2,"unloading a provider invalidates visual caches and permits fallback");
        using(var again=SignalPresentationPacks.Register(p)){again.Dispose();again.Dispose();}
        Check(SignalPresentationPacks.Find(p.Id)==null,"re-registering and repeated disposal leave no stale provider");
        var old=Signal();var chosen=old.Clone();chosen.PresentationPack=SignalPresentationPacks.Runenwacht;
        Check(chosen.Id==old.Id&&chosen.Anchor.Segment==old.Anchor.Segment&&chosen.Clone().PresentationPack==chosen.PresentationPack,"presentation selection and cloning retain operational identity and anchor");
        Check(Reject(()=>SignalPresentationPacks.RequirePlacement(old,chosen,false,true,true))&&Reject(()=>SignalPresentationPacks.RequirePlacement(old,chosen,true,false,true))&&Reject(()=>SignalPresentationPacks.RequirePlacement(old,chosen,true,true,false)),"central placement policy rejects disabled, unlicensed and missing presentation packs independently");
        SignalPresentationPacks.RequirePlacement(old,chosen,true,true,true);
        SignalPresentationPacks.RequirePlacement(chosen,chosen.Clone(),false,false,false);
        SignalPresentationPacks.RequirePlacement(chosen,old,false,false,false);
        Check(true,"licensed placement, unchanged saved models and reverting to built-in presentation remain distinct");
        foreach(string bad in new[]{MarkerRecord.Speed,MarkerRecord.CircuitB}){var row=chosen.Clone();row.Kind=bad;Check(Reject(()=>SignalPresentationPacks.ValidateSelection(row)),"non-signal fields cannot receive a presentation pack · "+bad);}
        var low=chosen.Clone();low.LowMain=true;var hl=chosen.Clone();hl.HlSignal=true;
        Check(Reject(()=>SignalPresentationPacks.ValidateSelection(low))&&Reject(()=>SignalPresentationPacks.ValidateSelection(hl)),"unsupported low and Hl combinations cannot be persisted");
        var install=new InstallationDocument{WorldKey=new string('a',64),Map="synthetic",Objects=new MarkerDocument{WorldKey=new string('a',64),Map="synthetic"}};
        install.Objects.Markers.Add(chosen);install.Validate();string before=ConstructionSnapshot.Signature(install);install.PresentationLicenses.Add(chosen.PresentationPack);install.Validate();
        var restored=install.Clone();Check(restored.PresentationLicenses.SequenceEqual(install.PresentationLicenses)&&restored.Objects.Markers[0].PresentationPack==chosen.PresentationPack&&ConstructionSnapshot.Signature(install)!=before,"world licenses and model choices round-trip and invalidate construction snapshots");
        var legacy=JObject.FromObject(install);legacy.Remove("PresentationLicenses");var loaded=legacy.ToObject<InstallationDocument>();loaded.Validate();
        Check(loaded.PresentationLicenses.Count==0,"older installations receive no automatic world license");
        restored.PresentationLicenses.Add(chosen.PresentationPack);Check(Reject(restored.Validate),"duplicate world licenses are rejected");
        var face=new SignalFaceView(true,true,true,"test",(LocalSignalAspect)999,LocalSignalAspect.Stop,LocalSignalAspect.Clear);
        Check(face.Main==LocalSignalAspect.Invalid&&typeof(SignalFaceView).GetProperties().All(x=>x.SetMethod==null)&&typeof(SignalViewFrame).GetProperties().All(x=>x.SetMethod==null),"visual snapshots are immutable and undefined authority is normalized to invalid");
        var visualControls=new SignalViewFrame(MarkerRecord.Main,false,face,null,float.NaN);
        Check(visualControls.EmissionStrength==1&&new SignalViewFrame(MarkerRecord.Main,false,face,null,-20).EmissionStrength==0&&new SignalFaceView(true,false,false,"test",LocalSignalAspect.Stop,LocalSignalAspect.Dark,LocalSignalAspect.Dark,float.PositiveInfinity).LabelScale==1,"visual brightness and label scale reject non-finite or out-of-range inputs");
        foreach(var kind in new[]{MarkerRecord.Main,MarkerRecord.Distant,MarkerRecord.Shunt})foreach(LocalSignalAspect a in Enum.GetValues(typeof(LocalSignalAspect))) {
            var channel=CrystalLogic.Channel(kind,a);
            Check(a!=LocalSignalAspect.Dark||channel==null,"dark authority never lights a Runenwacht channel · "+kind+" / "+a);
            if(a==LocalSignalAspect.Invalid||a==LocalSignalAspect.Stop)Check(channel==CrystalChannel.Red,"stop/invalid indications cannot become permissive · "+kind+" / "+a);
        }
        Check(CrystalLogic.Channel(MarkerRecord.Shunt,LocalSignalAspect.Shunt)==CrystalChannel.Violet&&CrystalLogic.Channel(MarkerRecord.Main,LocalSignalAspect.Shunt)==CrystalChannel.Red,"violet Sh 1 is restricted to the shunting head; the main head keeps red");
        Check(CrystalLogic.Channel(MarkerRecord.Distant,LocalSignalAspect.Slow)==CrystalChannel.Amber,"the preview head includes slow-running expectation");
        foreach(CrystalChannel from in Enum.GetValues(typeof(CrystalChannel)))foreach(CrystalChannel to in Enum.GetValues(typeof(CrystalChannel))) {
            var fade=new CrystalTransition();fade.Set(from);fade.Advance(1);fade.Set(to);
            for(int i=0;i<80;i++){fade.Advance(.01f);CheckExclusive(fade);}
            Check(fade.Level(to)==1&&Enum.GetValues(typeof(CrystalChannel)).Cast<CrystalChannel>().Where(c=>c!=to).All(c=>fade.Level(c)==0),"exclusive fixed-colour transition settles correctly · "+from+" → "+to);
        }
        var animation=new CrystalTransition();animation.Set(CrystalChannel.Green);animation.Advance(.05f);Check(animation.Level(CrystalChannel.Green)>0&&animation.Level(CrystalChannel.Green)<1,"permissive crystal ignition animates over time");
        animation.Set(CrystalChannel.Red);Check(animation.Level(CrystalChannel.Green)==0&&animation.Level(CrystalChannel.Red)==1,"restrictive red takes effect immediately without residual permissive light");
        animation.Set(CrystalChannel.Green);animation.Advance(.03f);Check(animation.Level(CrystalChannel.Red)>0&&animation.Level(CrystalChannel.Red)<1&&animation.Level(CrystalChannel.Green)==0,"old red dims before the next permissive crystal lights");
        animation.Set(null);animation.Advance(1);Check(Enum.GetValues(typeof(CrystalChannel)).Cast<CrystalChannel>().All(c=>animation.Level(c)==0),"dark cancels outstanding animation without leaving an old clear light");
        animation.Advance(float.NaN);animation.Advance(float.PositiveInfinity);animation.Advance(-1);Check(animation.Level(CrystalChannel.Green)==0,"invalid frame times cannot corrupt crystal emission");
        var manifest=JObject.Parse(File.ReadAllText(Path.Combine(root,"asset-packs/Runenwacht/mod/EnusDE-Runenwacht/Info.json")));
        Check(manifest["dependencies"]["required"].Values<string>().Contains("enusde/signaltechnik =="+EnusDE.Signaltechnik.Entry.Version)&&manifest["assemblies"].Values<string>().SequenceEqual(new[]{"EnusDE.Runenwacht.dll"}),"optional pack declares the exact main version and only its own assembly");
        Check(typeof(RunenwachtProvider).GetInterfaces().Contains(typeof(ISignalPresentationProvider)),"separately compiled Runenwacht assembly implements the public presentation contract");
    }
    static void CheckExclusive(CrystalTransition t){var levels=Enum.GetValues(typeof(CrystalChannel)).Cast<CrystalChannel>().Select(t.Level).ToArray();if(levels.Count(x=>x>0)>1||levels.Any(x=>float.IsNaN(x)||x<0||x>1))throw new Exception("FAIL: overlapping or invalid crystal channels");}
}
