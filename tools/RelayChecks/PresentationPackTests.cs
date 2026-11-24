using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AssetPack.Common;
using AssetPack.Common.Dependencies;
using AssetPack.Common.Versioning;
using EnusDE.Signaltechnik;
using EnusDE.Runenwacht;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railroader.Modding;
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
        Check(typeof(SignalFaceView).GetConstructors().Any(c=>c.GetParameters().Length==8)&&typeof(SignalFaceView).GetConstructors().Any(c=>c.GetParameters().Length==9),"adding separate code/name preserves the original API-v1 frame constructor for compiled consumers");
        var nearLight=CrystalAppearance.For(3,1,1);var farLight=CrystalAppearance.For(150,1,1);
        Check(nearLight.Body>0&&nearLight.Body<.25f&&farLight.Body>=4&&farLight.Body>nearLight.Body*20,"active crystal body retains a restrained close glow and a broad visible far indication");
        float previous=0;foreach(float distance in new float[]{0,12,20,35,50,80,150,1000}){var appearance=CrystalAppearance.For(distance,1,1);if(appearance.Body<previous)throw new Exception("FAIL: non-monotonic crystal visibility");previous=appearance.Body;}
        Check(previous>=4,"smooth distance gain never switches the indication off at long range");
        foreach(float distance in new float[]{0,12,80,1000}){var off=CrystalAppearance.For(distance,0,4);var disabled=CrystalAppearance.For(distance,1,0);if(off.Body!=0||off.Edge!=0||off.Vein!=0||disabled.Body!=0||disabled.Edge!=0||disabled.Vein!=0)throw new Exception("FAIL: inactive crystal emits");}
        Check(true,"inactive channels and disabled emission remain dark at all tested distances");
        foreach(float bad in new float[]{float.NaN,float.PositiveInfinity,-10}){var appearance=CrystalAppearance.For(bad,1,1);if(float.IsNaN(appearance.Body)||float.IsInfinity(appearance.Body)||appearance.Body!=nearLight.Body)throw new Exception("FAIL: invalid distance gain");}
        Check(CrystalAppearance.For(100,float.NaN,1).Body==0&&CrystalAppearance.For(100,1,float.PositiveInfinity).Body==0,"invalid camera/animation/brightness inputs cannot leak non-finite light values");
        Check(CrystalAppearance.For(80,.5f,1).Body==CrystalAppearance.For(80,1,1).Body*.5f,"distance gain respects exclusive animated channel intensity without changing its aspect");
        Check(CrystalAppearance.For(150,1,1,CrystalChannel.White).Body==farLight.Body*2&&CrystalAppearance.For(3,1,1,CrystalChannel.White).Body==nearLight.Body,"identification light gains extra distance brightness without washing out its nearby crystal");
        Check(CrystalAppearance.For(150,0,4,CrystalChannel.White).Body==0&&CrystalAppearance.For(150,1,1,CrystalChannel.Red).Body==farLight.Body,"identification gain cannot activate an off channel or affect other colours");
        var captioned=new SignalFaceView(true,false,false,"Mill exit",LocalSignalAspect.Stop,LocalSignalAspect.Dark,LocalSignalAspect.Dark,1,"HS-087");
        Check(captioned.Code=="HS-087"&&captioned.Caption=="Mill exit","immutable visual frame preserves separate technical code and readable display name");
        Check(RunenwachtPalette.ShaderNameFor("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset")=="Universal Render Pipeline/Lit"&&RunenwachtPalette.ShaderNameFor("UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset")=="HDRP/Lit"&&RunenwachtPalette.ShaderNameFor(null)=="Standard"&&RunenwachtPalette.ShaderNameFor("UnknownPipeline")=="Unlit/Color","asset materials select the active pipeline instead of an unrelated installed shader");
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
        RunCompatibility(root);
    }
    public static void RunCompatibility(string root) {
        var manifest=JObject.Parse(File.ReadAllText(Path.Combine(root,"asset-packs/Runenwacht/mod/EnusDE-Runenwacht/Info.json")));
        var required=manifest["dependencies"]["required"].Values<string>().Select(value=>DependencyInfo.Parse(value,DependencyInfo.DependencyType.Required)).ToArray();
        var dependency=required.Single(d=>d.Identifier=="enusde/signaltechnik");
        var game=required.Single(d=>d.Identifier=="rr/game");
        Check(required.Length==2&&dependency.Constraints.Count==1&&game.IsSatisfiedBy(new PackVersion("0.6.3"))&&
            !game.IsSatisfiedBy(new PackVersion("0.6.4"))&&manifest["assemblies"].Values<string>().SequenceEqual(new[]{"EnusDE.Runenwacht.dll"}),
            "optional pack requires the main mod and tested game line and carries only its own assembly");
        Check(!dependency.IsSatisfiedBy(new PackVersion("0.7.63"))&&
            new[]{"0.7.64","0.7.65","0.7.66","0.8.0","1.0.0"}.All(v=>dependency.IsSatisfiedBy(new PackVersion(v))),
            "native dependency parser accepts the presentation baseline and later main versions while rejecting older ones");
        var addon=new AssetPackInfo {Identifier=(string)manifest["identifier"],Version=(string)manifest["version"],
            Dependencies=new AssetPackDependencies{Required=manifest["dependencies"]["required"].Values<string>().ToList()}};
        var packs=new Dictionary<string,IDependencyNode> {
            {addon.Identifier,addon},{"rr/game",new AssetPackInfo{Identifier="rr/game",Version="0.6.3"}}
        };
        var missing=DependencySorter.ValidateAndSortPacks(packs,null);
        packs.Add("enusde/signaltechnik",new AssetPackInfo{Identifier="enusde/signaltechnik",Version="0.7.66"});
        var available=DependencySorter.ValidateAndSortPacks(packs,null);
        var order=available.ValidPacks.Select(p=>p.Identifier).ToArray();
        Check(!missing.IsSuccess&&missing.ValidPacks.All(p=>p.Identifier!=addon.Identifier)&&available.IsSuccess&&
            order.Length==3&&Array.IndexOf(order,addon.Identifier)>Array.IndexOf(order,"enusde/signaltechnik")&&
            Array.IndexOf(order,addon.Identifier)>Array.IndexOf(order,"rr/game"),
            "native load ordering excludes Runenwacht without the main mod and loads it after a later compatible version");
        var pack=new RunenwachtProvider();
        Check(typeof(RunenwachtProvider).GetInterfaces().Contains(typeof(ISignalPresentationProvider))&&pack.Version==(string)manifest["version"]&&
            typeof(SignalFaceView).GetProperty(nameof(SignalFaceView.Code))?.PropertyType==typeof(string),
            "separately compiled Runenwacht assembly implements the public presentation contract and matches its manifest version");
        var first=new EnusDE.Runenwacht.Entry();var duplicate=new EnusDE.Runenwacht.Entry();var firstContext=new PackContext();var duplicateContext=new PackContext();
        try {
            first.OnModLoad(firstContext);
            var registered=SignalPresentationPacks.Find(pack.Id);
            var mainVersion=(string)typeof(EnusDE.Signaltechnik.Entry).GetField("Version").GetRawConstantValue();
            Check(registered?.Version==pack.Version&&registered.ApiVersion==1&&firstContext.Messages.Infos==1&&firstContext.Messages.Warnings==0,
                "the actual Runenwacht entry registers and exposes its API-v1 provider with main "+mainVersion);
            duplicate.OnModLoad(duplicateContext);duplicate.OnModUnload();
            Check(duplicateContext.Messages.Warnings==1&&ReferenceEquals(SignalPresentationPacks.Find(pack.Id),registered),
                "a rejected Runenwacht registration logs a warning and leaves the existing provider intact");
        } finally {duplicate.OnModUnload();first.OnModUnload();}
        Check(SignalPresentationPacks.Find(pack.Id)==null,"the Runenwacht entry unregisters its provider at unload");
    }
    sealed class PackContext:IModContext {
        public readonly PackLog Messages=new PackLog();
        public string ModIdentifier=>"enusde/runenwacht";
        public string ModDirectory=>"fixture";
        public IModLogger Log=>Messages;
        public bool CheckDependency(string identifier)=>identifier=="enusde/signaltechnik";
    }
    sealed class PackLog:IModLogger {
        public int Infos,Warnings;
        public void Debug(string message){}
        public void Info(string message)=>Infos++;
        public void Warning(string message)=>Warnings++;
        public void Error(string message)=>throw new Exception(message);
        public void Error(Exception error,string message)=>throw new Exception(message,error);
    }
    static void CheckExclusive(CrystalTransition t){var levels=Enum.GetValues(typeof(CrystalChannel)).Cast<CrystalChannel>().Select(t.Level).ToArray();if(levels.Count(x=>x>0)>1||levels.Any(x=>float.IsNaN(x)||x<0||x>1))throw new Exception("FAIL: overlapping or invalid crystal channels");}
}
