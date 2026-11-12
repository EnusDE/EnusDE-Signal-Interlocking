using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Game.State;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed class Recorder : MonoBehaviour {
        TelemetryWriter writer;
        readonly GameReadAdapter reader=new GameReadAdapter();
        readonly FrameStats frames=new FrameStats();
        readonly SignObservations signs=new SignObservations();
        readonly Dictionary<string,int> messages=new Dictionary<string,int>();
        DiagnosticSettings config=new DiagnosticSettings();
        DiagnosticSampler sampler;
        IdFilter signFilter=new IdFilter("");
        double tickAt,performanceAt,logsAt,eventWindow;
        int eventCount;
        long sequence,ticks,completedWritten,completedDropped,suppressed;
        string session,path,lastError,signObserverError;
        bool disposed,writerErrorReported,logAttached;
        public bool Recording=>writer!=null && writer.LastError==null;
        public string Error=>writer!=null?(writer.LastError??lastError):lastError;
        public string DirectoryPath=>path;
        public long Written=>writer==null?completedWritten:writer.Written;
        public long Dropped=>writer==null?completedDropped:writer.Dropped;
        public long Suppressed=>suppressed;
        public long Ticks=>ticks;
        public DiagnosticSampler Sampler=>sampler;
        public GameReadAdapter Reader=>reader;
        public string LastPerformance {get;private set;}="Noch keine Messung.";
        public string LastMemory {get;private set;}="Noch keine Speichermessung.";
        public string LastLogMessage {get;private set;}="Noch keine passende Meldung.";
        public string SignObserverStatus=>signObserverError??"Schilder-Rückrufe aktiv; keine Zug-Trigger.";
        public void Apply(DiagnosticSettings settings) {
            Detach();FlushLogs();config=settings.Clone();reader.Reset();frames.Take();
            LastPerformance="Noch keine Messung.";LastLogMessage="Noch keine passende Meldung.";
            sampler=new DiagnosticSampler(config);
            if(!config.Recording){Emit("configuration",config);Stop();return;}
            if(!Recording){Begin(config);return;}
            performanceAt=logsAt=tickAt=Time.realtimeSinceStartup;
            Emit("configuration",config);Attach();
        }
        public void Begin(DiagnosticSettings settings) {
            if(disposed)return;
            Stop();config=settings.Clone();sampler=new DiagnosticSampler(config);reader.Reset();
            if(!config.Recording)return;
            try {
                session=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
                path=Path.Combine(Application.persistentDataPath,"EnusDE-Signaltechnik","Diagnostics",session);
                var state=StateManager.Shared;
                writer=new TelemetryWriter(path,new {schema=2,version=Entry.Version,session=session,start_utc=DateTime.UtcNow.ToString("o"),game_version=Application.version,game_module=typeof(StateManager).Assembly.ManifestModule.ModuleVersionId.ToString(),role=state!=null&&StateManager.IsHost?"Host":"Client",map=state==null?null:state.CurrentMapIdentifier,local_visibility_only=true,configuration=config,train_sign_trigger_available=false});
                completedWritten=completedDropped=suppressed=0;frames.Take();writerErrorReported=false;
                sequence=ticks=0;lastError=null;LastPerformance="Noch keine Messung.";LastLogMessage="Noch keine passende Meldung.";
                tickAt=performanceAt=logsAt=Time.realtimeSinceStartup;
                Emit("session_start",new {note="Passive local diagnostics; selected categories only."});
                Emit("configuration",config);Attach();
            }catch(Exception e){lastError=e.GetType().Name+": "+e.Message;Stop();}
        }
        void Attach() {
            if(!Recording)return;
            if(config["game_log"].Enabled){Application.logMessageReceived+=OnGameLog;logAttached=true;}
            signObserverError=null;
            if(config["signs"].Enabled) {
                signFilter=new IdFilter(config["signs"].Ids);
                try{signs.Start(OnSignHandler);Emit("category_status",new {category="sign_callbacks",available=true,reason="Passive Verwaltungsrückrufe; keine Zug-Trigger."});}
                catch(Exception e){signObserverError="Rückrufe nicht verfügbar: "+e.Message;Emit("category_status",new {category="sign_callbacks",available=false,reason=signObserverError});}
                Emit("category_status",new {category="train_sign_trigger",available=false,reason="Kein verifizierter Ereigniszugriff auf Zug-/Schildauslöser; Schilderbestand und Verwaltungsrückrufe sind getrennte Beobachtungen."});
            }
        }
        void Detach() {
            if(logAttached){Application.logMessageReceived-=OnGameLog;logAttached=false;}
            signs.Dispose();
        }
        void Emit(string kind,object data) {
            var output=writer;if(output==null)return;
            try {
                var state=StateManager.Shared;
                output.Enqueue(new {schema=2,session=session,seq=Interlocked.Increment(ref sequence),utc=DateTime.UtcNow.ToString("o"),real_seconds=(double)Time.realtimeSinceStartup,world_play_seconds=state!=null&&state.Storage!=null?(double?)state.Storage.PlayTime:null,role=state!=null&&StateManager.IsHost?"Host":"Client",kind=kind,data=data});
            }catch(Exception e){lastError=e.GetType().Name+": "+e.Message;}
        }
        void OnSignHandler(string handler,string id) {
            if(!Recording || !config["signs"].Enabled || !signFilter.Allows(id))return;
            double now=Time.realtimeSinceStartup;
            if(now>=eventWindow){eventWindow=now+1;eventCount=0;}
            if(eventCount++>=100){suppressed++;return;}
            if(sampler!=null)sampler.Views["signs"]=id+" · "+handler+" (Verwaltungsrückruf)";
            Emit("sign_handler",new {id=id,handler=handler,source="SignController."+handler,observation="completed_game_handler",is_train_trigger=false});
        }
        void OnGameLog(string message,string stack,LogType type) {
            if(!Recording || !config["game_log"].Enabled || message==null || message.IndexOf("signaltechnik",StringComparison.OrdinalIgnoreCase)>=0)return;
            string lower=message.ToLowerInvariant();
            bool relevant=lower.Contains("signal")||lower.Contains("ctc")||lower.Contains("switch")||lower.Contains("autoengineer")||lower.Contains("playersign")||lower.Contains("signcontroller");
            if(!relevant)return;
            string value=type+": "+Limit(message,2048);
            LastLogMessage=Limit(value,350);
            if(type==LogType.Error||type==LogType.Exception)value+="\n"+Limit(stack,1024);
            int count;
            if(messages.TryGetValue(value,out count))messages[value]=count+1;
            else if(messages.Count<128)messages[value]=1;else suppressed++;
        }
        void FlushLogs() {
            foreach(var pair in messages)Emit("game_log",new {level="Game",message=pair.Key,count=pair.Value});
            messages.Clear();
        }
        static string Limit(string value,int max)=>value==null?"":(value.Length<=max?value:value.Substring(0,max));
        public void LocalMarkerEvent(string action,MarkerRecord marker) {
            string category=marker.IsSignal?"signals":"signs";
            if(!Recording||!config[category].Enabled)return;
            var filter=new IdFilter(config[category].Ids);if(!filter.Allows(marker.Id)&&!filter.Allows(marker.Name))return;
            Emit(marker.IsSignal?"local_signal_object":"local_marker",new {action=action,id=marker.Id,name=marker.Name,object_kind=marker.Kind,anchor=marker.Anchor,mast=marker.Mast,speed_mph=marker.Kind==MarkerRecord.Speed?(double?)marker.SpeedMph:null,local_only=true,is_train_trigger=false});
        }
        public void SignalEvent(string kind,object data){if(!Recording||!config["signals"].Enabled)return;if(kind=="signal_bridge_health"){Emit(kind,data);return;}var row=Newtonsoft.Json.Linq.JObject.FromObject(data);var filter=new IdFilter(config["signals"].Ids);if(new[]{"id","code","entry","exit"}.Any(key=>filter.Allows((string)row[key]??"")))Emit(kind,data);}
        void Update() {
            if(writer!=null&&writer.LastError!=null&&!writerErrorReported){writerErrorReported=true;Detach();Debug.LogError("[EnusDE Signaltechnik] Recorder-Schreibfehler: "+writer.LastError);}
            if(!Recording)return;
            if(config["performance"].Enabled)frames.Add(Time.unscaledDeltaTime);
            double now=Time.realtimeSinceStartup;
            if(now<tickAt)return;
            tickAt=now+.25;
            sampler.Tick(now,reader,Emit);ticks++;
            if(config["game_log"].Enabled&&now>=logsAt){logsAt=now+config["game_log"].IntervalSeconds;FlushLogs();}
            if(config["performance"].Enabled&&now>=performanceAt) {
                performanceAt=now+config["performance"].IntervalSeconds;
                var values=frames.Take();var row=Newtonsoft.Json.Linq.JObject.FromObject(values);
                var memory=MemorySnapshot.Read();LastMemory=memory.Display();
                row["managed_bytes"]=memory.managed_bytes;
                row["process_private_bytes"]=memory.process_private_bytes;
                row["process_working_set_bytes"]=memory.process_working_set_bytes;
                row["window_builds"]=Entry.WindowBuilds;
                LastPerformance=((double)row["mean_fps"]).ToString("0.0")+" FPS · längster Frame "+((double)row["worst_frame_ms"]).ToString("0.0")+" ms · Frames ab 100 ms: "+row["frames_over_100ms"];
                Emit("performance",row);
            }
            if(now>=healthAt){healthAt=now+10;Emit("recorder_health",new {written=Written,dropped=Dropped,suppressed=suppressed,pending_characters=writer.PendingCharacters,error=Error});}
        }
        double healthAt;
        public void Stop() {
            Detach();
            if(writer==null)return;
            FlushLogs();
            Emit("session_end",new {ticks=ticks,written=writer.Written,dropped=writer.Dropped,suppressed=suppressed});
            var old=writer;writer=null;old.Dispose();
            completedWritten=old.Written;completedDropped=old.Dropped;
            if(old.LastError!=null)lastError=old.LastError;
            reader.Reset();frames.Take();
        }
        public void DisposeRecorder(){disposed=true;Stop();}
        void OnDestroy(){DisposeRecorder();}
    }
}
