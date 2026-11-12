using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class StockInvalidityTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidDataException){return true;}}
    sealed class Properties:ISharedProperties {readonly Dictionary<string,string> data=new Dictionary<string,string>();public string Read(string key)=>data.TryGetValue(key,out string value)?value:null;public void Write(string key,string value){data[key]=value;}}
    public static void Run() {
        string key=new string('a',64);var current=new InstallationDocument{WorldKey=key,Map="test",Objects=new MarkerDocument{WorldKey=key,Map="test"}};
        var absent=StockInvalidity.Prepare("wh-em",true,()=>null,null);var saved=StockInvalidity.Put(current,absent);
        Check(current.Retired.Count==0&&saved.Retired.Count==1&&saved.Retired[0].AutoPlacement&&saved.Retired[0].IgnoreAi,"unloaded scenery does not block the immutable saved AI-invalidity request");
        int warnings=0;var failed=StockInvalidity.Prepare("wh-em",true,()=>throw new InvalidOperationException("model unavailable"),e=>warnings++);
        Check(warnings==1&&failed.AutoPlacement&&StockInvalidity.Ignore(true,false,"wh-em",StockInvalidity.Put(current,failed).Retired),"an optional model exception still leaves a valid saved native-AI exclusion");
        var noLogger=StockInvalidity.Prepare("wh-em",true,()=>throw new InvalidOperationException("model unavailable"),e=>throw new IOException("logger unavailable"));
        Check(noLogger.AutoPlacement&&noLogger.IgnoreAi,"a failed optional warning sink cannot cancel the model-independent invalidity request");
        var invalidPose=new RetiredSignal{Id="wh-em",RelativeToModel=true,Offset=new float[]{float.NaN,0,0},Normal=new float[]{0,0,1}};
        var deferred=StockInvalidity.Prepare("wh-em",true,()=>invalidPose,e=>warnings++);
        Check(deferred.AutoPlacement&&deferred.Offset.All(v=>v==0)&&warnings==2,"invalid visual coordinates defer cross placement without corrupting or canceling invalidity");
        var pose=new RetiredSignal{Id="wh-em",RelativeToModel=true,Offset=new float[]{0,6,.2f},Normal=new float[]{0,0,1}};
        var positioned=StockInvalidity.Prepare("wh-em",true,()=>pose,null);pose.Offset[1]=99;
        Check(!positioned.AutoPlacement&&positioned.RelativeToModel&&positioned.Offset[1]==6,"new cross placement uses a detached pose relative to the visible signal model");
        var wrong=StockInvalidity.Prepare("wh-em",true,()=>new RetiredSignal{Id="another",Offset=new float[3],Normal=new float[]{0,0,1}},e=>warnings++);
        Check(wrong.AutoPlacement&&warnings==3,"a pose belonging to another signal cannot move the selected cross");
        var properties=new Properties();var documents=new SharedDocuments(properties);documents.Write("install",saved,1024*1024);var reload=documents.Read<InstallationDocument>("install",1024*1024);reload.Validate();
        Check(reload.Retired.Single().Id=="wh-em"&&reload.Retired.Single().AutoPlacement&&StockInvalidity.Ignore(true,false,"wh-em",reload.Retired),"deferred invalidity survives actual shared-document compression, save and late-join readback");
        Check(!StockInvalidity.Ignore(false,false,"wh-em",reload.Retired)&&!StockInvalidity.Ignore(true,true,"wh-em",reload.Retired)&&!StockInvalidity.Ignore(true,false,"other",reload.Retired),"native-AI filtering affects only the exact marked stock signal on the host, never own proxies or unrelated signals");
        var optical=StockInvalidity.Put(saved,StockInvalidity.Request("wh-em",false));
        Check(optical.Retired.Count==1&&!StockInvalidity.Ignore(true,false,"wh-em",optical.Retired)&&saved.Retired[0].IgnoreAi,"changing to optical-only replaces one request and restores normal AI lookup without mutating the prior document");
        var cleared=StockInvalidity.Remove(saved,"wh-em");
        Check(cleared.Retired.Count==0&&!StockInvalidity.Ignore(true,false,"wh-em",cleared.Retired)&&saved.Retired.Count==1,"restoring validity removes only the explicit request and leaves the previous snapshot isolated");
        Check(StockInvalidity.State(saved.Retired[0],false).Contains("NICHT aktiv")&&!StockInvalidity.State(saved.Retired[0],false).EndsWith("KI-Ausnahme aktiv")&&StockInvalidity.State(null,true)=="Gültig","status distinguishes stored invalidity from an unavailable AI hook instead of claiming success");
        var legacy=JsonConvert.DeserializeObject<RetiredSignal>("{\"Id\":\"old-stock\",\"IgnoreAi\":true,\"Offset\":[0,4,0],\"Normal\":[0,0,1]}");legacy.Validate();
        Check(legacy.Visual&&!legacy.RelativeToModel&&!legacy.AutoPlacement,"existing saved crosses retain their original logical-signal coordinates and visible default");
        var moved=positioned.Clone();moved.Offset[1]++;
        Check(!StockInvalidity.SamePlacement(positioned,moved)&&positioned.Offset[1]==6&&StockInvalidity.SamePlacement(positioned,positioned.Clone()),"changing a cross pose invalidates its visual cache and cloned pose arrays remain isolated");
        Check(Reject(()=>StockInvalidity.Request("enusde.signaltechnik.signal.own",true))&&Reject(()=>new RetiredSignal{Id="stock",Offset=new float[]{float.PositiveInfinity,0,0},Normal=new float[]{0,0,1}}.Validate()),"own signals and nonfinite positions still reject before any shared commit");
    }
}
