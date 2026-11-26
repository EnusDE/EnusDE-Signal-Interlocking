using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.State;
using Game.AccessControl;
using KeyValue.Runtime;
using Railroader.Modding;
using Newtonsoft.Json;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed class SharedRouteStatus {
        public RouteDefinition Definition;
        public string Id,Reason,FaultReason,ChainId,ReleaseSelection,ReleaseStatus;
        public double WaitRemaining;
        public bool ReleaseWaitCompleted;
        public bool ChainReady=true;
        public RouteStage Stage;
        public bool Entered,HeadKnown;
        public List<string> ReleasedSections=new List<string>(),ReleasedSwitches=new List<string>(),PassedSignals=new List<string>();
        public Dictionary<string,string> PointStates=new Dictionary<string,string>();
        public static SharedRouteStatus From(RouteRun run,double waitRemaining=0)=>new SharedRouteStatus{Id=run.Route.Id,Reason=run.Reason,FaultReason=run.FaultReason,ChainId=run.ChainId,ChainReady=run.ChainReady,Stage=run.Stage,Entered=run.Entered,HeadKnown=run.HeadKnown,ReleaseSelection=run.ReleaseSelection,ReleaseStatus=run.ReleaseStatus,WaitRemaining=waitRemaining,ReleaseWaitCompleted=run.ReleaseWaitCompleted,ReleasedSections=run.ReleasedSections.ToList(),ReleasedSwitches=run.ReleasedSwitches.ToList(),PassedSignals=run.PassedSignals.ToList(),PointStates=new Dictionary<string,string>(run.PointStates)};
        public RouteRun ToRun(RouteDefinition route) {
            route=Definition??route;if(route==null||route.Id!=Id)throw new InvalidDataException("Missing or inconsistent live route definition.");route.Validate();
            if(double.IsNaN(WaitRemaining)||double.IsInfinity(WaitRemaining)||WaitRemaining<0||WaitRemaining>300||ReleaseSelection?.Length>1024||ReleaseStatus?.Length>2048)throw new InvalidDataException(LocalText.Select("Invalid live release status.","Ungültiger aktueller Auflösungsstatus."));
            if(!Enum.IsDefined(typeof(RouteStage),Stage)||Reason?.Length>1024||FaultReason?.Length>1024||ReleasedSections==null||ReleasedSections.Count>128||ReleasedSwitches==null||ReleasedSwitches.Count>64||PassedSignals==null||PassedSignals.Count>128||PointStates==null||PointStates.Count>64||ReleasedSections.Any(k=>!route.Sections.Any(s=>s.Key==k))||ReleasedSwitches.Any(k=>!route.Switches.Any(s=>s.Node==k)))throw new InvalidDataException("Fahrstraßenanzeige ungültig.");
            var run=new RouteRun{Route=route,Stage=Stage,Reason=Reason,FaultReason=FaultReason,ChainId=ChainId,ChainReady=ChainReady,Entered=Entered,HeadKnown=HeadKnown,ReleaseSelection=ReleaseSelection,ReleaseStatus=ReleaseStatus,ReportedWaitRemaining=WaitRemaining,ReleaseWaitCompleted=ReleaseWaitCompleted};foreach(string id in ReleasedSections)run.ReleasedSections.Add(id);foreach(string id in ReleasedSwitches)run.ReleasedSwitches.Add(id);foreach(string id in PassedSignals)run.PassedSignals.Add(id);foreach(var item in PointStates)run.PointStates[item.Key]=item.Value;return run;
        }
    }
    public sealed class SharedLiveFrame {
        public string Epoch;
        public long Sequence;
        public Dictionary<string,LocalSignalAspect> Aspects=new Dictionary<string,LocalSignalAspect>();
        public List<SharedRouteStatus> Runs=new List<SharedRouteStatus>();
        public AutomationLiveFrame Automation=new AutomationLiveFrame();
        public void Validate(){if(Automation==null)throw new InvalidDataException("Automatikstatus fehlt.");Automation.Validate();Guid id;if(!Guid.TryParseExact(Epoch,"N",out id)||Sequence<1||Aspects==null||Aspects.Count>384||Runs==null||Runs.Count>16||Aspects.Any(p=>!Guid.TryParseExact(p.Key,"N",out id)||!Enum.IsDefined(typeof(LocalSignalAspect),p.Value))||Runs.Any(r=>r==null)||Runs.Select(r=>r.Id).Distinct().Count()!=Runs.Count)throw new InvalidDataException("Gemeinsame Betriebsanzeige ungültig.");foreach(var run in Runs)if(run.Definition!=null)run.ToRun(null);}
    }
    public sealed partial class SharedRuntime : MonoBehaviour,ISharedProperties {
        public const string ObjectId="enusde.signaltechnik.shared.v1";
        const int InstallLimit=1024*1024,DeskLimit=16*1024*1024,RouteLimit=4*1024*1024,FrameLimit=4*1024*1024;
        KeyValueObject properties;
        SharedDocuments documents;
        IDisposable observer;
        IModLogger log;
        string directory;
        readonly HashSet<string> changed=new HashSet<string>();
        readonly LiveLease lease=new LiveLease();
        readonly string epoch=Guid.NewGuid().ToString("N");
        long sequence;
        double next,nextFrame;
        bool stopped;
        InstallationDocument installation;
        DeskLibrary sharedDesks;
        List<RouteDefinition> sharedRoutes;
        public SharedLiveFrame Live {get;private set;}
        public bool Ready=>!stopped&&installation!=null;
        public bool ConfigurationReady=>Ready&&sharedDesks!=null&&sharedRoutes!=null;
        public long InstallationVersion {get;private set;}
        public long ConfigurationVersion {get;private set;}
        InstallationDocument constructionSignatureSource;
        string constructionSignature;
        public string ConstructionSignature {
            get {
                if(installation==null)return null;
                if(!ReferenceEquals(constructionSignatureSource,installation)){constructionSignature=ConstructionSnapshot.Signature(installation);constructionSignatureSource=installation;}
                return constructionSignature;
            }
        }
        public string Error {get;private set;}
        readonly NativeMaterialLogistics nativeMaterials=new NativeMaterialLogistics();
        double nextNativeMaterialRefresh;
        public IReadOnlyList<NativeMaterialWarehouse> MaterialWarehouses {get {nativeMaterials.Refresh();return nativeMaterials.Warehouses;}}
        public string NativeMaterialError=>nativeMaterials.Error;
        public string WorldKey=>installation?.WorldKey;
        public string LegacyKey {get;private set;}
        public bool LiveFresh=>Ready&&lease.Fresh(Time.realtimeSinceStartup);
        public bool EconomyEnabled=>installation?.Economy.Enabled??false;
        public double EconomyFactor=>installation?.Economy.Factor??1;
        public bool DefaultRightSide=>installation?.Placement.RightSide??true;
        public bool FollowView=>installation?.Placement.FollowView??true;
        public void ApplyPlacementDefaults(MarkerOptions value){installation?.Placement.Apply(value);}
        public bool PlacementMatches(MarkerOptions value)=>installation?.Placement.Matches(value)??true;
        public void Initialize(IModLogger logger,string path) {
            log=logger;directory=path;backups=new InstallationBackups(Path.Combine(path,"Backups"));log.Info("Externe Anlagensicherungen: "+backups.DirectoryPath);properties=gameObject.AddComponent<KeyValueObject>();documents=new SharedDocuments(this);
            try{nativeMaterials.Start();}catch(Exception e){log.Warning("Optional supply labels: "+e.Message);}
            StateManager.Shared.RegisterPropertyObject(ObjectId,properties,AuthorizationRequirement.HostOnly,null);
            // Host mutations already update the validated in-memory documents below. Only
            // initial restore or remote client commits need decompression/deserialization.
            observer=properties.ObserveKeyChanges((key,kind)=>{if(key.EndsWith(".commit",StringComparison.Ordinal)&&(!StateManager.IsHost||installation==null))changed.Add(key.Substring(0,key.Length-7));});
            changed.UnionWith(new[]{"install","desks","routes","live"});
        }
        public string Read(string key){var value=properties.Get(key);return value.IsNull?null:value.StringValue;}
        public void Write(string key,string value){RequireHost();if(restoreJournal!=null&&!restoreJournal.ContainsKey(key))restoreJournal[key]=Read(key);properties.Set(key,value==null?Value.Null():Value.String(value),SetValueOrigin.Local);}
        void RequireHost(){if(stopped||StateManager.IsUnloading||!StateManager.IsHost||properties==null)throw new InvalidOperationException("Gemeinsame Anlage nur vom Host änderbar.");}
        void Update() {
            if(stopped||StateManager.IsUnloading||Time.realtimeSinceStartup<next||StateManager.Shared==null||!StateManager.Shared.IsSessionReady||!StateManager.Shared.HasRestoredProperties)return;next=Time.realtimeSinceStartup+.25;
            if(Time.realtimeSinceStartup>=nextNativeMaterialRefresh){nextNativeMaterialRefresh=Time.realtimeSinceStartup+2;nativeMaterials.Refresh();}
            foreach(string channel in changed.ToArray())try{ReadChannel(channel);changed.Remove(channel);Error=null;}catch(Exception e){if(Error!=e.Message){Error=e.Message;log?.Warning("Anlagensynchronisation: "+e.Message);}}
            if(StateManager.IsHost&&Ready)try{SyncConstructionMilestone();}catch(Exception e){Error=e.Message;}
            if(StateManager.IsHost&&Time.realtimeSinceStartup>=nextBackupRetry)TryBackup();
        }
        void ReadChannel(string channel) {
            if(channel=="install") {var value=documents.Read<InstallationDocument>(channel,InstallLimit);if(value==null)return;CircuitIdentity.Fill(value.Objects);value.Validate();if(value.Map!=StateManager.Shared.CurrentMapIdentifier)throw new InvalidDataException("Gemeinsame Anlage gehört zu einer anderen Karte.");installation=value;InstallationVersion++;}
            else if(channel=="desks"){var value=documents.Read<DeskLibrary>(channel,DeskLimit);if(value!=null){if(WorldKey==null)throw new InvalidDataException("Anlagenkennung wird noch übertragen.");value.Validate(WorldKey);sharedDesks=value;ConfigurationVersion++;}}
            else if(channel=="routes"){var value=documents.Read<InterlockingDocument>(channel,RouteLimit);if(value!=null){if(WorldKey==null)throw new InvalidDataException("Anlagenkennung wird noch übertragen.");ValidateRoutes(value);sharedRoutes=value.Routes;ConfigurationVersion++;}}
            else if(channel=="live"&&!StateManager.IsHost){var value=documents.Read<SharedLiveFrame>(channel,FrameLimit);if(value!=null){value.Validate();if(lease.Accept(value.Epoch,value.Sequence,Time.realtimeSinceStartup))Live=value;}}
        }
        void ValidateRoutes(InterlockingDocument value){if(value.Schema!=1||value.WorldKey!=WorldKey||value.Routes==null||value.Routes.Count>128)throw new InvalidDataException("Gemeinsame Fahrstraßen ungültig.");foreach(var route in value.Routes)route.Validate();if(value.Routes.Select(r=>r.Id).Distinct().Count()!=value.Routes.Count||value.Routes.Select(r=>r.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=value.Routes.Count)throw new InvalidDataException("Doppelte gemeinsame Fahrstraße.");}
        public void ImportIfNeeded() {
            RequireHost();if(Ready)return;if(!StateManager.Shared.HasRestoredProperties||!StateManager.Shared.IsSessionReady)return;
            // Read a committed snapshot first. Never replace corrupt/unfinished shared data by legacy files.
            if(documents.Header("install")!=null){ReadChannel("install");return;}
            string path=StateManager.Shared.SaveManager?.RailroadPath,map=StateManager.Shared.CurrentMapIdentifier;if(string.IsNullOrWhiteSpace(path)||string.IsNullOrWhiteSpace(map))return;
            LegacyKey=MarkerRepository.Key(path,map);if(NeedsRecoveryDecision(LegacyKey,map))return;var legacy=new MarkerRepository(Path.Combine(directory,"Markers"),LegacyKey,map).Snapshot();
            string key=SharedDocuments.Hash(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")));legacy.WorldKey=key;
            var initial=new InstallationDocument{WorldKey=key,Map=map,Objects=legacy};foreach(var row in legacy.Markers)initial.Economy.Built[row.Id]=new BuiltMaterial{Budget=MaterialEconomy.Cost(row)};
            var oldDesksPath=new DeskStore(Path.Combine(directory,"Desks"),LegacyKey);DeskLibrary desks;
            if(File.Exists(oldDesksPath.FilePath))desks=oldDesksPath.Load(null);else {var panel=new PanelStore(Path.Combine(directory,"Panels"),LegacyKey).Load();desks=new DeskLibrary{WorldKey=LegacyKey,Desks=new List<DeskDefinition>{new DeskDefinition{Global=true,Code="ESTW",Name="Übergeordnetes ESTW",Panel=panel}}};}
            desks.WorldKey=key;foreach(var desk in desks.Desks)desk.Panel.WorldKey=key;desks.Validate(key);
            var routes=new InterlockingStore(Path.Combine(directory,"Interlocking"),LegacyKey).Load();initial.Economy.ConnectedPoints=desks.Desks.SelectMany(d=>d.Panel.Tiles).Where(t=>t.Kind==PanelTileKind.Switch&&!string.IsNullOrWhiteSpace(t.Reference)).Select(t=>t.Reference).Concat(routes.SelectMany(r=>r.Switches.Select(p=>p.Node))).Distinct().ToList();
            var defaults=Entry.SettingsSnapshot().Markers;initial.Economy.PolicyVersion=1;initial.Economy.Unlocked=legacy.Markers.Count>0;initial.Economy.MaterialsInSandbox=defaults.MaterialsInSandbox;initial.Economy.FreeInEconomy=defaults.FreeInEconomy;initial.Economy.Enabled=ConstructionPolicy.MaterialsRequired(StateManager.IsSandbox,defaults.MaterialsInSandbox,defaults.FreeInEconomy);initial.Economy.Factor=defaults.MaterialFactor;initial.Placement=ServerPlacement.From(defaults);initial.Validate();
            Atomic(()=>{documents.Write("desks",desks,DeskLimit);documents.Write("routes",new InterlockingDocument{WorldKey=key,Routes=routes},RouteLimit);CommitInstall(initial);sharedDesks=desks;sharedRoutes=routes;ConfigurationVersion++;});
        }
        public MarkerDocument Objects()=>installation==null?null:JsonConvert.DeserializeObject<MarkerDocument>(JsonConvert.SerializeObject(installation.Objects));
        public InstallationDocument Snapshot()=>installation?.Clone();
        public DeskLibrary Desks()=>sharedDesks?.Clone();
        internal object GfmWorldDataVersion=>sharedDesks;
        internal TrackBlock[] GfmWorldBlocks()=>sharedDesks?.Blocks.Values.Select(block=>block.Clone()).ToArray();
        internal (string Id,string Code,string Name)[] GfmWorldDesks(string block,out (string Id,string Code,string Name)[] all) {
            var ids=new HashSet<string>{block};
            all=sharedDesks.Desks.Select(d=>(d.Id,d.Code,d.Name)).ToArray();
            return sharedDesks.Desks.Where(d=>d.Panel.Tiles.Any(tile=>GfmCoverage.Ports(tile,ids)!=0)).Select(d=>(d.Id,d.Code,d.Name)).ToArray();
        }
        public List<RouteDefinition> Routes()=>sharedRoutes?.Select(r=>r.Clone()).ToList();
        public IReadOnlyDictionary<string,string> NumberRegistry()=>installation?.TrainNumbers;
        public IReadOnlyDictionary<string,string> NumberFields()=>installation?.SignalNumberFields;

        internal string FieldNumber(string signal) {
            return installation!=null&&!string.IsNullOrEmpty(signal)&&installation.SignalNumberFields.TryGetValue(signal,out string number)?number:"";
        }

        internal (string[] fields,int cars) WriteFieldNumber(string signal,string prefix,string number) {
            RequireReady();
            if(!Guid.TryParseExact(signal,"N",out _))throw new InvalidOperationException("Zugnummernfeld zuerst einem Hauptsignal zuordnen.");
            string value=TrainNumbers.Format(prefix,number);
            var next=installation.Clone();
            var previous=TrainNumbers.RemoveNumber(next.SignalNumberFields,value,new HashSet<string>{signal});
            int cars=TrainNumbers.RemoveNumber(next.TrainNumbers,value,new HashSet<string>()).Length;
            bool changed=FieldNumber(signal)!=value||previous.Length>0||cars>0;
            if(value=="")next.SignalNumberFields.Remove(signal);else next.SignalNumberFields[signal]=value;
            if(changed)CommitInstall(next,false);
            return (previous,cars);
        }

        internal string[] AdvanceFieldNumbers(IReadOnlyList<RouteRun> movements,Func<string,string> fieldFor) {
            RequireReady();
            var numbers=new Dictionary<string,string>(installation.SignalNumberFields);
            var completed=new HashSet<string>();
            if(TrainNumbers.AdvanceSignalNumbers(numbers,movements,fieldFor,completed)) {
                var next=installation.Clone();
                next.SignalNumberFields=numbers;
                CommitInstall(next,false);
            }
            return completed.ToArray();
        }
        public int AssignNumber(Model.Car selected,string prefix,string number) {
            RequireReady();if(selected==null)throw new InvalidOperationException("Zug oder Fahrzeug im Spiel auswählen.");string formatted=TrainNumbers.Format(prefix,number);var ids=new HashSet<string>();var queue=new Queue<Model.Car>();queue.Enqueue(selected);
            while(queue.Count>0){var car=queue.Dequeue();if(car==null||ids.Contains(car.id))continue;if(ids.Count>=256||car.IsDerailed)throw new InvalidOperationException("Zugverband nicht vollständig zuordenbar.");ids.Add(car.id);foreach(var end in new[]{Model.Car.LogicalEnd.A,Model.Car.LogicalEnd.B}){var other=car.CoupledTo(end);if(other!=null)queue.Enqueue(other);}}
            var next=installation.Clone();var present=new HashSet<string>(TrainController.Shared.Cars.Where(c=>c!=null).Select(c=>c.id));foreach(string old in next.TrainNumbers.Keys.Where(id=>!present.Contains(id)).ToArray())next.TrainNumbers.Remove(old);int transferred=TrainNumbers.RemoveNumber(next.SignalNumberFields,formatted,new HashSet<string>()).Length+
                TrainNumbers.RemoveNumber(next.TrainNumbers,formatted,ids).Length;
            foreach(string id in ids)if(formatted=="")next.TrainNumbers.Remove(id);else next.TrainNumbers[id]=formatted;
            CommitInstall(next,false);
            return transferred;
        }
        void CommitInstall(InstallationDocument value,bool configuration=true){RequireHost();CircuitIdentity.Fill(value.Objects);value.Validate();documents.Write("install",value,InstallLimit);installation=value;InstallationVersion++;if(configuration)BackupChanged();}
        void Atomic(Action action,Action rollback=null) {
            RequireHost();var heads=new[]{"install","desks","routes"}.ToDictionary(k=>k,k=>documents.Header(k));var old=installation;var oldDesks=sharedDesks;var oldRoutes=sharedRoutes;long iv=InstallationVersion,cv=ConfigurationVersion;
            bool wasDirty=backupDirty;backupTransactionDepth++;
            try{using(StateManager.TransactionScope())try{action();}catch{foreach(var head in heads)Write(head.Key+".commit",head.Value);installation=old;sharedDesks=oldDesks;sharedRoutes=oldRoutes;InstallationVersion=iv;ConfigurationVersion=cv;backupDirty=wasDirty;rollback?.Invoke();throw;}}
            finally{backupTransactionDepth--;}
            BackupChanged();
        }
        public void PutObjects(MarkerDocument next){RequireHost();foreach(var marker in next.Markers){var old=installation.Objects.Markers.FirstOrDefault(m=>m.Id==marker.Id);SignalPresentationPacks.RequirePlacement(old,marker,PresentationExperiment,HasPresentationLicense(marker.PresentationPack),SignalPresentationPacks.Find(marker.PresentationPack)!=null);if(HlTestFeature.RequiresEnable(old,marker))HlTestFeature.Require(Entry.SettingsSnapshot().Markers.EnableHlSignalsTest);if(old!=null&&(old.Kind!=marker.Kind||old.LowMain!=marker.LowMain||old.HlSignal!=marker.HlSignal||old.PresentationPack!=marker.PresentationPack)&&!Entry.MastCanModify(old))throw new InvalidOperationException("Signal is locked by a route.");}if(next.Markers.Any(m=>!installation.Objects.Markers.Any(old=>old.Id==m.Id)||m.LowMain&&!installation.Objects.Markers.Any(old=>old.Id==m.Id&&old.LowMain)))RequireConstruction();var value=MaterialEconomy.Markers(installation,next);CommitInstall(value);next.Markers=value.Objects.Markers.Select(m=>m.Clone()).ToList();}
        public void PutDesks(DeskLibrary next){RequireConstruction();next.Validate(WorldKey);if(sharedDesks!=null){var changed=LineConfigurationChanges.Changed(sharedDesks.FreeLines,next.FreeLines);if(changed.Length>0)Entry.CheckLineConfigurationChange(changed,next.FreeLines);var targets=LineTargetBindings.ChangedTargets(sharedDesks,next,out var lines);if(targets.Length>0)Entry.CheckLineConfigurationChange(lines,next.FreeLines,targets);}Atomic(()=>{documents.Write("desks",next,DeskLimit);sharedDesks=next.Clone();ConfigurationVersion++;});}
        public void PutRoutes(IEnumerable<RouteDefinition> next){RequireConstruction();var doc=new InterlockingDocument{WorldKey=WorldKey,Routes=next.Select(r=>r.Clone()).ToList()};ValidateRoutes(doc);WithConnectionMaterials(doc.Routes.SelectMany(r=>r.Switches.Select(p=>p.Node)),value=>{documents.Write("routes",doc,RouteLimit);CommitInstall(value);sharedRoutes=doc.Routes;ConfigurationVersion++;});}
        internal void ConnectRoutePoints(IEnumerable<string> points) {
            RequireHost();var added=points.Distinct().Where(p=>!installation.Economy.ConnectedPoints.Contains(p)).ToArray();
            if(added.Length==0)return;
            WithConnectionMaterials(added,value=>CommitInstall(value));
        }
        public void ApplyEconomy(bool enabled,double factor){RequireHost();if(!Ready||EconomyEnabled==enabled&&EconomyFactor==factor)return;var value=installation.Clone();value.Economy.Enabled=enabled;value.Economy.Factor=factor;value.Validate();CommitInstall(value);}
        public void ApplyPlacement(MarkerOptions options){RequireHost();if(!Ready||installation.Placement.Size!=null&&PlacementMatches(options))return;var value=installation.Clone();value.Placement=ServerPlacement.From(options);CommitInstall(value);}
        public LocalSignalAspect ClientAspect(string id){LocalSignalAspect aspect;return LiveFresh&&Live!=null&&Live.Aspects.TryGetValue(id,out aspect)?aspect:LocalSignalAspect.Stop;}
        public void Publish(SignalRuntime signals) {
            if(!Ready||!StateManager.IsHost||Time.realtimeSinceStartup<nextFrame)return;nextFrame=Time.realtimeSinceStartup+1;
            var frame=new SharedLiveFrame{Epoch=epoch,Sequence=++sequence};foreach(var row in signals.SignalObjects.Where(s=>s.IsSignal))frame.Aspects[row.Id]=signals.Aspect(row.Id);frame.Runs=signals.Engine.Active.Select(run=>{var state=SharedRouteStatus.From(run,signals.Engine.RemainingWait(run));if(!sharedRoutes.Any(r=>r.Id==run.Route.Id))state.Definition=run.Route.Clone();return state;}).ToList();frame.Automation=signals.AutomationFrame();frame.Validate();documents.Write("live",frame,FrameLimit);
        }
        public void Shutdown(){if(stopped)return;TryBackup();stopped=true;try{nativeMaterials.Dispose();}finally{observer?.Dispose();observer=null;try{if(StateManager.Shared!=null)StateManager.Shared.UnregisterPropertyObject(ObjectId);}finally{if(properties!=null)Destroy(properties);properties=null;changed.Clear();installation=null;sharedDesks=null;sharedRoutes=null;Live=null;}}}
    }
}
