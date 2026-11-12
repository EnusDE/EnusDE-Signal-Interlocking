using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.State;
using Track.Signals;
using UI.Builder;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Track;

namespace EnusDE.Signaltechnik {
    public sealed class RetiredSignal {
        public string Id;
        public bool IgnoreAi;
        [Newtonsoft.Json.JsonProperty(DefaultValueHandling=Newtonsoft.Json.DefaultValueHandling.Ignore)]
        public bool Darken;
        // Legacy records are relative to the logical signal, new ones to the visible model.
        public bool Visual=true,RelativeToModel,AutoPlacement;
        public float[] Offset,Normal;
        public RetiredSignal Clone(){var copy=(RetiredSignal)MemberwiseClone();copy.Offset=(float[])Offset?.Clone();copy.Normal=(float[])Normal?.Clone();return copy;}
        public void Validate(){if(string.IsNullOrWhiteSpace(Id)||Id.Length>128||Id.StartsWith("enusde.",StringComparison.OrdinalIgnoreCase)||Offset==null||Normal==null||Offset.Length!=3||Normal.Length!=3||Offset.Any(v=>float.IsNaN(v)||float.IsInfinity(v)||Math.Abs(v)>100000)||Normal.Any(v=>float.IsNaN(v)||float.IsInfinity(v)||Math.Abs(v)>100)||Normal.Sum(v=>v*v)<.1)throw new InvalidDataException("Ungültigkeitsmarkierung enthält ungültige Signal- oder Positionsdaten.");}
    }
    public sealed partial class SharedRuntime {
        public bool IgnoresStock(string id)=>Ready&&StockInvalidity.Ignore(true,false,id,installation.Retired);
        public void Retire(RetiredSignal signal){RequireReady();CommitInstall(StockInvalidity.Put(installation,signal));}
        public void RestoreStock(string id){RequireReady();CommitInstall(StockInvalidity.Remove(installation,id));}
        public IReadOnlyList<RetiredSignal> Retired=>installation?.Retired;
    }
    public sealed partial class MarkerRuntime {
        readonly StockLampVisuals stockLamps=new StockLampVisuals();
        string stockSelected,stockMessage="",stockResultId;
        string lampError;
        void LateUpdate()=>StockAppearance(()=>stockLamps.Enforce());
        void StockAppearance(Action display) {
            try{display();}
            catch(Exception error){
                if(lampError==error.Message)return;
                lampError=error.Message;
                log?.Warning("Bestandssignal-Lampen: "+lampError);
            }
        }
        CTCSignal[] stockChoices=new CTCSignal[0];
        readonly Dictionary<string,GameObject> crosses=new Dictionary<string,GameObject>();
        readonly Dictionary<string,RetiredSignal> crossPoses=new Dictionary<string,RetiredSignal>();
        readonly Dictionary<string,string> crossErrors=new Dictionary<string,string>();
        Material crossBlack,crossWhite;
        long crossVersion=-1;
        double nextCross;
        string crossError;
        public void OpenStockSignals(){RequireStockEdit();var camera=Camera.main;stockChoices=UnityEngine.Object.FindObjectsByType<CTCSignal>(FindObjectsSortMode.None).Where(s=>!(s is RuntimeSignalProxy)&&!string.IsNullOrEmpty(s.id)&&!s.id.StartsWith("enusde.",StringComparison.OrdinalIgnoreCase)).GroupBy(s=>s.id).Select(g=>g.First()).OrderBy(s=>camera==null?0:((s.modelController!=null?s.modelController.transform:s.transform).position-camera.transform.position).sqrMagnitude).Take(12).ToArray();if(!stockChoices.Any(s=>s.id==stockSelected))stockSelected=stockChoices.FirstOrDefault()?.id;page=Page.Stock;ShowTool();}
        void RequireStockEdit(){string reason;if(!CanEdit(out reason))throw new InvalidOperationException(reason);}
        void BuildStock(UIPanelBuilder c) {
            c.AddTitle("Bestandssignale","Weißes Ungültigkeitskreuz mit schwarzem Rand");
            c.AddLabel("Eines der zwölf nächsten Grundspielsignale wählen. Das Kreuz kennzeichnet dessen sichtbaren Kopf. Eigene Signaltechnik-Signale sind ausgeschlossen.");
            if(stockChoices.Length==0){c.AddLabel("Keine Bestandssignale gefunden.");return;}
            c.AddField("Signal in der Nähe",c.AddDropdown(stockChoices.Select(s=>s!=null?s.DisplayName+" · "+s.id:"Signal entladen").ToList(),Math.Max(0,Array.FindIndex(stockChoices,s=>s!=null&&s.id==stockSelected)),i=>{if(i>=0&&i<stockChoices.Length&&stockChoices[i]!=null){stockSelected=stockChoices[i].id;RebuildTool();}}));
            c.AddButton("Kreuz setzen · Lampen aus",()=>StockAction(()=>PutCross(false,true)));
            c.AddButton("Kreuz setzen · Lampen aus und KI-Ausnahme",()=>StockAction(()=>PutCross(true,true)));
            c.AddButton("Nur Kreuz setzen",()=>StockAction(()=>PutCross(false,false)));
            c.AddField("Ergebnis",()=>stockResultId==stockSelected?stockMessage:"Noch keine Aktion für dieses Signal.",UIPanelBuilder.Frequency.Periodic);
            c.AddField("Kennzeichnung",()=>StockInvalidity.State(Entry.Shared?.Retired?.FirstOrDefault(s=>s.Id==stockSelected),StockSignalHooks.Available),UIPanelBuilder.Frequency.Periodic);
            c.AddField("Kreuzdarstellung",()=>CrossState(),UIPanelBuilder.Frequency.Periodic);
            c.AddField("Lampen",()=>Entry.Shared?.Retired?.Any(r=>r.Id==stockSelected&&r.Darken)==true?
                stockLamps.IsBound(stockSelected)?"Optisch ausgeschaltet":"Wartet auf unterstützten Signalkopf":
                "Spielanzeige aktiv",UIPanelBuilder.Frequency.Periodic);
            c.AddField("KI-Einbindung",()=>StockSignalHooks.Available?"Ausnahme in nativer AE-Signalsuche aktiv":"Nicht verfügbar · gespeicherte KI-Ausnahmen werden nicht angewendet",UIPanelBuilder.Frequency.Periodic);
            c.AddButton("Kreuz entfernen / Signal wieder gültig",()=>StockAction(()=>{RequireStockEdit();Entry.Shared.RestoreStock(stockSelected);stockMessage="Markierung entfernt; Bestandssignal wieder gültig.";crossVersion=-1;nextCross=0;}));
            c.AddLabel("Nur optisch lässt die KI-Wirkung unverändert. Die KI-Ausnahme betrifft die native AE-Signalsuche; native ABS-/CTC-Blöcke, Weichen, Kollisionen und andere Mods bleiben eigenständig. Eigene Signale und Fahrwege vor dem Stilllegen prüfen.");
        }
        string CrossState(){var value=Entry.Shared?.Retired?.FirstOrDefault(s=>s.Id==stockSelected);GameObject root;string error;crossErrors.TryGetValue(stockSelected??"",out error);return value==null?"Kein Kreuz gespeichert":!value.Visual?"Kein optisches Kreuz angefordert":crosses.TryGetValue(stockSelected,out root)&&root!=null?"Kreuz im geladenen Signalmodell vorhanden":(error??crossError)!=null?"Darstellung ausstehend: "+(error??crossError):"Gespeichert · wartet auf geladenes Signalmodell";}
        void StockAction(Action action){stockResultId=stockSelected;bool ok=false;try{action();ok=true;}catch(Exception e){stockMessage="Nicht übernommen: "+e.Message;}finally{RebuildTool();}try{if(ok)log?.Info("Bestandssignal "+stockResultId+": "+stockMessage);else log?.Warning("Bestandssignal "+stockResultId+": "+stockMessage);Entry.RecordSignalEvent("stock_signal_invalidity",new{id=stockResultId,result=stockMessage,success=ok,stored=Entry.Shared?.Retired?.Any(r=>r.Id==stockResultId)==true,ai_hook=StockSignalHooks.Available});}catch{/* Optional reporting must not reverse a confirmed action result. */}}
        void PutCross(bool ai,bool dark) {
            RequireStockEdit();var signal=stockChoices.FirstOrDefault(s=>s!=null&&s.id==stockSelected)??throw new InvalidOperationException("Signal nicht mehr geladen; Liste erneut öffnen.");
            // Optional model work must never prevent saving the explicit request.
            var value=StockInvalidity.Prepare(signal.id,ai,()=>{RetiredSignal pose;return TryCrossPose(signal,out pose)?pose:null;},e=>log?.Warning("Kreuzposition für "+signal.id+" wird später erneut versucht: "+e.Message));
            value.Darken=dark;
            Entry.Shared.Retire(value);var saved=Entry.Shared.Retired.FirstOrDefault(r=>r.Id==signal.id);
            if(saved==null||saved.IgnoreAi!=ai)throw new InvalidOperationException("Speicherung konnte nicht bestätigt werden.");
            stockMessage=StockInvalidity.State(saved,StockSignalHooks.Available)+" · gespeichert"+(value.AutoPlacement?"; Kreuz folgt, sobald das Modell geladen ist":"");crossVersion=-1;nextCross=0;
        }
        bool TryCrossPose(CTCSignal signal,out RetiredSignal pose) {
            pose=null;var model=signal.modelController;if(model==null)return false;
            var renderers=model.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.bounds.size.sqrMagnitude>.001f&&r.sharedMaterial!=crossBlack&&r.sharedMaterial!=crossWhite).ToArray();if(renderers.Length==0)return false;
            var lenses=StockLampVisuals.Lenses(signal);
            var target=lenses.Length>0?lenses.OrderByDescending(r=>r.bounds.center.y).First():
                renderers.OrderByDescending(r=>r.bounds.center.y).First();var camera=Camera.main;var normal=camera==null?-model.transform.forward:(camera.transform.position-target.bounds.center);normal.y=0;if(normal.sqrMagnitude<.1f)normal=-model.transform.forward;normal.Normalize();
            var pos=target.bounds.center+normal*(Math.Max(target.bounds.size.x,target.bounds.size.z)*.5f+.05f);var local=model.transform.InverseTransformPoint(pos);var facing=model.transform.InverseTransformDirection(normal);
            pose=new RetiredSignal{Id=signal.id,RelativeToModel=true,Offset=new[]{local.x,local.y,local.z},Normal=new[]{facing.x,facing.y,facing.z}};pose.Validate();return true;
        }
        void RemoveCross(string id){GameObject root;if(crosses.TryGetValue(id,out root)&&root!=null)Destroy(root);crosses.Remove(id);crossPoses.Remove(id);}
        void RefreshCrosses() {
            if(Time.realtimeSinceStartup<nextCross)return;nextCross=Time.realtimeSinceStartup+2;var shared=Entry.Shared;if(shared?.Ready!=true)return;
            var rows=shared.Retired.Where(r=>r.Visual).ToArray();
            if(crossVersion==shared.InstallationVersion&&crosses.Count==rows.Length&&crosses.Values.All(v=>v!=null)&&rows.All(r=>!r.Darken||stockLamps.IsBound(r.Id)))return;
            var wanted=new HashSet<string>(rows.Select(r=>r.Id));foreach(string id in crosses.Keys.Where(id=>!wanted.Contains(id)||crosses[id]==null).ToArray())RemoveCross(id);foreach(string id in crossErrors.Keys.Where(id=>!wanted.Contains(id)).ToArray())crossErrors.Remove(id);
            if(rows.Length==0){stockLamps.Clear();crossVersion=shared.InstallationVersion;crossError=null;return;}
            var signals=UnityEngine.Object.FindObjectsByType<CTCSignal>(FindObjectsSortMode.None).Where(s=>!(s is RuntimeSignalProxy)&&s.id!=null).GroupBy(s=>s.id).ToDictionary(g=>g.Key,g=>g.First());
            StockAppearance(()=>stockLamps.Sync(rows,signals));
            foreach(var row in rows) {
                try {
                RetiredSignal prior;if(crosses.ContainsKey(row.Id)&&crossPoses.TryGetValue(row.Id,out prior)&&StockInvalidity.SamePlacement(prior,row))continue;
                RemoveCross(row.Id);CTCSignal signal;if(!signals.TryGetValue(row.Id,out signal))continue;var pose=row;if(row.AutoPlacement&&!TryCrossPose(signal,out pose))continue;
                var parent=pose.RelativeToModel?(signal.modelController!=null?signal.modelController.transform:null):signal.transform;if(parent==null)continue;
                if(crossBlack==null){var shader=ShaderPolicy.Find<Shader>(Shader.Find)??throw new InvalidOperationException("Kreuz-Shader fehlt.");crossBlack=CrossMaterial(shader,Color.black);crossWhite=CrossMaterial(shader,Color.white);}
                var root=new GameObject("EnusDE Ungültigkeitskreuz "+row.Id);
                try {
                    root.transform.SetParent(parent,false);var scale=parent.lossyScale;root.transform.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(scale.x)),1/Mathf.Max(.001f,Mathf.Abs(scale.y)),1/Mathf.Max(.001f,Mathf.Abs(scale.z)));
                    root.transform.localPosition=new Vector3(pose.Offset[0],pose.Offset[1],pose.Offset[2]);root.transform.localRotation=Quaternion.LookRotation(new Vector3(pose.Normal[0],pose.Normal[1],pose.Normal[2]),Vector3.up);
                    foreach(float angle in new[]{45f,-45f}){CrossBeam(root,angle,1.15f,.22f,.06f,0,crossBlack);CrossBeam(root,angle,1.08f,.14f,.02f,.041f,crossWhite);}
                    crosses[row.Id]=root;crossPoses[row.Id]=row.Clone();crossErrors.Remove(row.Id);
                }catch{Destroy(root);throw;}
                }catch(Exception error){string priorError;if(!crossErrors.TryGetValue(row.Id,out priorError)||priorError!=error.Message)log?.Warning("Ungültigkeitskreuz "+row.Id+": "+error.Message);crossErrors[row.Id]=error.Message;}
            }
            crossVersion=shared.InstallationVersion;crossError=null;
        }
        static Material CrossMaterial(Shader shader,Color color){var m=new Material(shader);foreach(string key in new[]{"_BaseColor","_Color","_UnlitColor"})if(m.HasProperty(key))m.SetColor(key,color);return m;}
        static void CrossBeam(GameObject root,float angle,float length,float width,float depth,float z,Material material){var beam=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(beam.GetComponent<Collider>());beam.layer=2;beam.transform.SetParent(root.transform,false);beam.transform.localPosition=new Vector3(0,0,z);beam.transform.localRotation=Quaternion.Euler(0,0,angle);beam.transform.localScale=new Vector3(width,length,depth);beam.GetComponent<Renderer>().sharedMaterial=material;}
        void DisposeCrosses(){StockAppearance(stockLamps.Clear);foreach(string id in crosses.Keys.ToArray())RemoveCross(id);crossErrors.Clear();if(crossBlack!=null)Destroy(crossBlack);if(crossWhite!=null)Destroy(crossWhite);crossVersion=-1;crossError=null;stockSelected=stockResultId=null;stockMessage="";}
    }
    // Only native AE signal enumeration; no block, switch or collision override.
    public sealed class StockSignalHooks : IDisposable {
        const string PatchId="enusde.signaltechnik.stock-invalidity";
        Harmony harmony;
        public static bool Available {get;private set;}
        public void Start(Action<string> warning){Dispose();try{harmony=new Harmony(PatchId);foreach(string name in new[]{"Search","DistanceToNextSignalAfter"}){var search=typeof(Model.AI.AutoEngineerPlanner).GetMethod(name,BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)??throw new MissingMethodException("AutoEngineerPlanner."+name);harmony.Patch(search,transpiler:new HarmonyMethod(typeof(StockSignalHooks),nameof(FilterSearch)));}Available=true;}catch(Exception e){Dispose();warning?.Invoke("Ungültigkeitskreuze nur optisch: "+e.Message);}}
        public static IEnumerable<CodeInstruction> FilterSearch(IEnumerable<CodeInstruction> input){var result=input.ToList();int matches=0;var original=typeof(NamedLocationExtensions).GetMethod("GetSignal",new[]{typeof(Graph.NamedLocation)});var replacement=typeof(StockSignalHooks).GetMethod(nameof(SignalForAi),BindingFlags.Static|BindingFlags.Public);foreach(var c in result)if((c.opcode==OpCodes.Call||c.opcode==OpCodes.Callvirt)&&Equals(c.operand,original)){c.opcode=OpCodes.Call;c.operand=replacement;matches++;}if(matches!=1)throw new InvalidOperationException("Unerwartete AE-Signalsuche: "+matches+" Fundstellen.");return result;}
        public static CTCSignal SignalForAi(Graph.NamedLocation marker){var signal=marker.GetSignal();return signal!=null&&StockInvalidity.Ignore(StateManager.IsHost,signal is RuntimeSignalProxy,signal.id,Entry.Shared?.Ready==true?Entry.Shared.Retired:null)?null:signal;}
        public void Dispose(){Available=false;if(harmony!=null){harmony.UnpatchAll(PatchId);harmony=null;}}
    }
}
