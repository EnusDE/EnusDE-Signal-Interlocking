using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public sealed class PendingTextBudget {
        readonly long limit;long used;
        public PendingTextBudget(long characters){limit=characters;}
        public long Used=>Interlocked.Read(ref used);
        public bool Reserve(int characters){if(characters<0||characters>limit)return false;long total=Interlocked.Add(ref used,characters);if(total<=limit)return true;Interlocked.Add(ref used,-characters);return false;}
        public void Release(int characters){Interlocked.Add(ref used,-characters);}
    }
    // This class has no game dependency and never writes to a save or shared property.
    public sealed class TelemetryWriter : IDisposable {
        readonly BlockingCollection<string> queue;
        readonly Thread worker;
        readonly long partLimit;
        readonly PendingTextBudget budget;
        long written, dropped;
        volatile string error;
        public readonly string DirectoryPath;
        public string LastError => error;
        public long Written => Interlocked.Read(ref written);
        public long Dropped => Interlocked.Read(ref dropped);
        public long PendingCharacters=>budget.Used;
        public TelemetryWriter(string directory, object metadata, long partBytes=8*1024*1024, int queueLimit=256,long maxQueuedCharacters=2*1024*1024) {
            DirectoryPath=Path.GetFullPath(directory);
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(Path.Combine(DirectoryPath,"session.json"),JsonConvert.SerializeObject(metadata,Formatting.Indented),new UTF8Encoding(false));
            partLimit=partBytes;
            budget=new PendingTextBudget(maxQueuedCharacters);
            queue=new BlockingCollection<string>(queueLimit);
            worker=new Thread(WriteLoop){IsBackground=true,Name="EnusDE Signaltechnik log writer"};
            worker.Start();
        }
        public bool Enqueue(object value) {
            if(error!=null || queue.IsAddingCompleted)return false;
            string line=JsonConvert.SerializeObject(value,Formatting.None);
            if(line.Length>256*1024||!budget.Reserve(line.Length)){Interlocked.Increment(ref dropped);return false;}
            bool accepted=false;
            try { if(queue.TryAdd(line)){accepted=true;return true;} }
            catch(InvalidOperationException){return false;}
            finally{if(!accepted)budget.Release(line.Length);}
            Interlocked.Increment(ref dropped);return false;
        }
        void WriteLoop() {
            StreamWriter output=null;
            long bytes=0;int part=0;
            try {
                while(!queue.IsCompleted) {
                    string line;
                    if(!queue.TryTake(out line,500)){if(output!=null)output.Flush();continue;}
                    budget.Release(line.Length);
                    int size=Encoding.UTF8.GetByteCount(line)+1;
                    if(output==null || (bytes>0 && bytes+size>partLimit)) {
                        if(output!=null)output.Dispose();
                        output=new StreamWriter(new FileStream(Path.Combine(DirectoryPath,"events-"+(++part).ToString("D4")+".jsonl"),FileMode.CreateNew,FileAccess.Write,FileShare.ReadWrite),new UTF8Encoding(false)){NewLine="\n"};
                        bytes=0;
                    }
                    output.WriteLine(line);bytes+=size;Interlocked.Increment(ref written);
                    if(queue.Count==0)output.Flush();
                }
            } catch(Exception problem) { error=problem.GetType().Name+": "+problem.Message; }
            finally {if(output!=null)try{output.Dispose();}catch(Exception problem){if(error==null)error=problem.GetType().Name+": "+problem.Message;}}
        }
        public void Dispose() {
            if(!queue.IsAddingCompleted)queue.CompleteAdding();
            if(!worker.Join(3000))error="Protokollschreiber beendet sich verzögert; Abschluss noch nicht bestätigt.";
        }
    }
    public sealed class ChangeTracker {
        readonly Dictionary<string,string> values=new Dictionary<string,string>();
        public bool Changed(string key,object value) {
            string encoded=JsonConvert.SerializeObject(value,Formatting.None),old;
            if(values.TryGetValue(key,out old)&&old==encoded)return false;
            values[key]=encoded;return true;
        }
        public void Clear(){values.Clear();}
    }
    public sealed class FrameStats {
        double sum,max;int count,slow50,slow100;
        public void Add(double seconds){if(seconds<=0 || double.IsNaN(seconds)||double.IsInfinity(seconds))return;sum+=seconds;max=Math.Max(max,seconds);count++;if(seconds>=.05)slow50++;if(seconds>=.1)slow100++;}
        public object Take() {
            var result=new {frames=count,mean_fps=sum>0?count/sum:0,worst_frame_ms=max*1000,frames_over_50ms=slow50,frames_over_100ms=slow100};
            sum=max=0;count=slow50=slow100=0;return result;
        }
    }
}
