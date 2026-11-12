using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class Observation {
        public string Id;
        public string FilterId;
        public object State;
        public Observation(string id,object state,string filterId=null){Id=id;State=state;FilterId=filterId??id;}
    }
    public sealed class ReadBatch {
        public bool Available=true;
        public string Reason;
        public List<Observation> Rows=new List<Observation>();
        public static ReadBatch Unavailable(string reason)=>new ReadBatch{Available=false,Reason=reason};
    }
    public interface IDiagnosticSource {
        ReadBatch Read(string category,DiagnosticSettings settings,bool discover);
        void Reset();
    }
    // Scheduling/filtering are independent of Unity so disabled reads can be tested.
    public sealed class DiagnosticSampler {
        DiagnosticSettings config;
        readonly Dictionary<string,double> due=new Dictionary<string,double>(),discovery=new Dictionary<string,double>();
        readonly Dictionary<string,Dictionary<string,string>> previous=new Dictionary<string,Dictionary<string,string>>();
        readonly Dictionary<string,string> availability=new Dictionary<string,string>();
        public readonly Dictionary<string,int> Counts=new Dictionary<string,int>();
        public readonly Dictionary<string,string> Views=new Dictionary<string,string>();
        public readonly Dictionary<string,object> LatestVehicles=new Dictionary<string,object>();
        public string VehicleSampleUtc {get;private set;}="";
        public DiagnosticSampler(DiagnosticSettings settings){Configure(settings);}
        public void Configure(DiagnosticSettings settings) {
            config=settings.Clone();due.Clear();discovery.Clear();previous.Clear();availability.Clear();Counts.Clear();Views.Clear();LatestVehicles.Clear();VehicleSampleUtc="";
            foreach(string key in DiagnosticSettings.Categories){Counts[key]=0;Views[key]=config[key].Enabled?"Noch keine Messung.":"Erfassung aus";}
        }
        public void Tick(double now,IDiagnosticSource source,Action<string,object> emit) {
            if(!config.Recording)return;
            foreach(string key in new[]{"vehicles","signals","blocks","switches","signs"}) {
                if(!config[key].Enabled)continue;
                double next;if(due.TryGetValue(key,out next)&&now<next)continue;
                due[key]=now+config[key].IntervalSeconds;
                try {
                    bool discover=!discovery.TryGetValue(key,out next)||now>=next;
                    var batch=source.Read(key,config,discover);
                    if(discover)discovery[key]=now+30;
                    string status=batch.Available?"available":batch.Reason??"Nicht verfügbar";
                    string oldStatus;
                    if(!availability.TryGetValue(key,out oldStatus)||oldStatus!=status) {
                        emit("category_status",new {category=key,available=batch.Available,reason=batch.Reason});availability[key]=status;
                        if(!batch.Available)previous.Remove(key);
                    }
                    if(!batch.Available){Counts[key]=0;if(key=="vehicles")LatestVehicles.Clear();Views[key]="Nicht verfügbar: "+batch.Reason;continue;}
                    var filter=new IdFilter(config[key].Ids);
                    var rows=batch.Rows.FindAll(row=>filter.Allows(row.FilterId));Counts[key]=rows.Count;
                    if(key=="vehicles") {
                        LatestVehicles.Clear();foreach(var row in rows)LatestVehicles[row.Id]=row.State;
                        VehicleSampleUtc=DateTime.UtcNow.ToString("o");
                        emit("vehicles",new {full=!config.LocomotivesOnly,cars=rows.ConvertAll(row=>row.State).ToArray()});
                        Views[key]=rows.Count+" Fahrzeuge im gewählten Umfang";continue;
                    }
                    Dictionary<string,string> before;
                    if(!previous.TryGetValue(key,out before))before=new Dictionary<string,string>();
                    var after=new Dictionary<string,string>();
                    foreach(var row in rows) {
                        string encoded=JsonConvert.SerializeObject(row.State,Formatting.None),old;
                        bool existed=before.TryGetValue(row.Id,out old);after[row.Id]=encoded;
                        if(!existed||encoded!=old) {
                            emit("infrastructure",new {key=key+":"+row.Id,snapshot=!existed,state=row.State});
                            Views[key]=Describe(row.State);
                        }
                    }
                    foreach(var item in before)if(!after.ContainsKey(item.Key)) {
                        emit("object_removed",new {category=key,id=item.Key,source="snapshot_comparison"});
                        Views[key]="Nicht mehr im Bestand: "+item.Key;
                    }
                    previous[key]=after;
                    if(rows.Count==0)Views[key]="Keine Objekte im gewählten Umfang beobachtet.";
                }catch(Exception e){Counts[key]=0;previous.Remove(key);availability[key]="error:"+e.Message;if(key=="vehicles")LatestVehicles.Clear();Views[key]="Lesefehler: "+e.Message;emit("category_status",new {category=key,available=false,reason=Views[key]});emit("collector_error",new {category=key,message=e.GetType().Name+": "+e.Message});}
            }
        }
        static string Trim(string text)=>text.Length>350?text.Substring(0,350)+"…":text;
        public static string Describe(object state) {
            var row=JObject.FromObject(state);string id=(string)row["id"]??"?";
            if(row["error"]!=null)return id+": Lesefehler · "+Trim((string)row["error"]);
            switch((string)row["kind"]) {
                case "signal":return "Signal "+id+": "+row["aspect"]+" · Richtung "+row["direction"];
                case "block":return "Block "+id+": "+((bool?)row["occupied"]==true?"besetzt":"frei");
                case "interlocking":return "Stellwerk "+id+": Richtung "+row["direction"];
                case "switch":return "Weiche "+id+": "+((bool?)row["thrown"]==true?"abweichende Stellung":"Grundstellung");
                case "sign":return "Schild "+id+": "+(string)row["sign_type"]+(row["text"]==null?"":" · "+Trim((string)row["text"]));
                default:return id+": Zustand beobachtet.";
            }
        }
    }
}
