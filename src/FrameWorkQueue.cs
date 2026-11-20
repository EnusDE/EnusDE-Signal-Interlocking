using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EnusDE.Signaltechnik {
    // Cooperative main-thread work. False means wait until the next frame; it never means completed.
    public sealed class FrameWorkQueue {
        sealed class Job {
            public string Key;
            public Func<IEnumerator<bool>> Create;
            public Func<bool> Current;
            public Action<Exception> Failed;
            public IEnumerator<bool> Work;
        }
        readonly Queue<Job> jobs=new Queue<Job>();
        readonly Func<double> clock;
        public FrameWorkQueue(Func<double> clock=null){this.clock=clock??(()=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency);}
        public int Count=>jobs.Count;
        public string ActiveKey=>jobs.Count==0?null:jobs.Peek().Key;
        public bool Contains(string key)=>jobs.Any(j=>j.Key==key);
        public bool Enqueue(string key,Func<IEnumerator<bool>> work,Func<bool> current,Action<Exception> failed) {
            if(Contains(key))return false;
            if(jobs.Count>=32)throw new InvalidOperationException(LocalText.Select("Route work queue is full (32 requests). Wait or cancel pending requests.","Fahrstraßen-Warteschlange voll (32 Anforderungen). Warten oder ausstehende Anforderungen abbrechen."));
            jobs.Enqueue(new Job{Key=key,Create=work,Current=current,Failed=failed});return true;
        }
        public void Advance(double budgetSeconds=.002,int maxSteps=512) {
            if(jobs.Count==0)return;var job=jobs.Peek();double started=clock();
            try {
                for(int i=0;i<maxSteps;i++) {
                    if(job.Current?.Invoke()==false)throw new OperationCanceledException(LocalText.Select("Route request is outdated; select start and destination again.","Fahrstraßenanforderung ist veraltet; Start und Ziel erneut wählen."));
                    if(job.Work==null)job.Work=job.Create();
                    if(!job.Work.MoveNext()){Remove(job);return;}
                    if(!job.Work.Current||clock()-started>=budgetSeconds)return;
                }
            }catch(Exception error){Remove(job);job.Failed?.Invoke(error);}
        }
        void Remove(Job job){if(jobs.Count>0&&ReferenceEquals(jobs.Peek(),job))jobs.Dequeue();try{job.Work?.Dispose();}catch{}}
        public void Cancel(string key){var keep=jobs.Where(j=>j.Key!=key).ToArray();foreach(var job in jobs.Where(j=>j.Key==key))try{job.Work?.Dispose();}catch{}jobs.Clear();foreach(var job in keep)jobs.Enqueue(job);}
        public void Clear(){foreach(var job in jobs)try{job.Work?.Dispose();}catch{}jobs.Clear();}
    }
}
