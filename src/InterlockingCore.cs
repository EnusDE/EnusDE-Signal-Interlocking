using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public enum TrackOccupancy { Unknown,Clear,Occupied }
    public enum RouteStage { Preparing,Set,Running,Fault,Cancelled,WaitingProtection }
    public sealed class TrackSection {
        public string Segment;
        public double From,To;
        public TrackSection Clone()=>(TrackSection)MemberwiseClone();
        public string Key=>Segment+":"+From.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+To.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
        public void Validate(){if(string.IsNullOrWhiteSpace(Segment)||!Finite(From)||!Finite(To)||From<0||To<=From)throw new InvalidDataException("Ungültiger Gleisabschnitt.");}
        public bool Overlaps(TrackSection other)=>Segment==other.Segment&&Math.Max(From,other.From)<Math.Min(To,other.To)-.001;
        static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
    }
    public sealed class RouteSwitch {public string Node,FlankOf;public bool Thrown;public double? ReleaseAt;public List<TrackSection> Protection;}
    public sealed class RouteDefinition {
        public string Id=Guid.NewGuid().ToString("N"),Code,Entry,Exit;
        public string EntryAnchor,ExitAnchor;
        public string Topology;
        public string ProgressTopology;
        public bool Shunting,Reduced;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool AuxiliaryExit;
        public double ProtectionWaitSeconds=-1;
        public List<TrackSection> Sections=new List<TrackSection>(),Approach=new List<TrackSection>();
        public List<RouteSwitch> Switches=new List<RouteSwitch>();
        public List<DirectedRouteSection> Path;
        public List<RouteViaSignal> ViaSignals;
        public List<DirectedRouteSection> EntryApproach;
        public List<DirectedRouteSection> ExitContinuation;
        public List<TrackSection> OccupiedTarget;
        public List<RouteFlank> Flanks;
        public void Validate() {
            Guid id;
            if(!Guid.TryParseExact(Id,"N",out id)||string.IsNullOrWhiteSpace(Code)||Code.Length>48||Entry==Exit||string.IsNullOrWhiteSpace(Entry)||string.IsNullOrWhiteSpace(Exit)||Sections==null||Sections.Count==0||Sections.Count>128||Approach==null||Approach.Count==0||Approach.Count>128||Switches==null||Switches.Count>64)throw new InvalidDataException("Unvollständige Fahrstraßendefinition.");
            foreach(var s in Sections.Concat(Approach)){if(s==null)throw new InvalidDataException("Leerer Gleisabschnitt.");s.Validate();}
            if(Switches.Any(s=>s==null||string.IsNullOrWhiteSpace(s.Node))||Switches.Select(s=>s.Node).Distinct().Count()!=Switches.Count)throw new InvalidDataException("Ungültige Weichendefinition.");
            if(AuxiliaryExit&&(Path==null||string.IsNullOrEmpty(ExitAnchor)))throw new InvalidDataException("Hilfsfahrstraße benötigt einen vollständigen gerichteten Fahrweg.");
            RouteSignals.Validate(this);
            FlankProtection.Validate(this);
            if(double.IsNaN(ProtectionWaitSeconds)||double.IsInfinity(ProtectionWaitSeconds)||ProtectionWaitSeconds< -1||ProtectionWaitSeconds>300)throw new InvalidDataException("Schutzwartezeit muss -1 (Vorgabe) oder 0–300 Sekunden sein.");
        }
        public RouteDefinition Clone()=>JsonConvert.DeserializeObject<RouteDefinition>(JsonConvert.SerializeObject(this));
    }
    public sealed class InterlockingDocument {public int Schema=1;public string WorldKey;public List<RouteDefinition> Routes=new List<RouteDefinition>();}
    public sealed class InterlockingStore {
        readonly string key;
        readonly string path;
        readonly Func<List<RouteDefinition>> sharedRead;
        readonly Action<IEnumerable<RouteDefinition>> sharedWrite;
        public InterlockingStore(string directory,string worldKey){key=worldKey;path=Path.Combine(directory,worldKey+".json");}
        public InterlockingStore(string directory,string worldKey,Func<List<RouteDefinition>> read,Action<IEnumerable<RouteDefinition>> write):this(directory,worldKey){sharedRead=read;sharedWrite=write;}
        public List<RouteDefinition> Load() {
            if(sharedRead!=null){var shared=sharedRead()??throw new InvalidDataException("Gemeinsame Fahrstraßen werden noch übertragen.");Validate(new InterlockingDocument{WorldKey=key,Routes=shared});return shared.Select(r=>r.Clone()).ToList();}
            if(!File.Exists(path))return new List<RouteDefinition>();
            if(new FileInfo(path).Length>4*1024*1024)throw new InvalidDataException("Stellwerksdatei zu groß; Original bleibt erhalten.");
            var doc=JsonConvert.DeserializeObject<InterlockingDocument>(File.ReadAllText(path));Validate(doc);return doc.Routes.Select(r=>r.Clone()).ToList();
        }
        void Validate(InterlockingDocument doc) {
            if(doc==null||doc.Schema!=1||doc.WorldKey!=key||doc.Routes==null||doc.Routes.Count>128)throw new InvalidDataException("Stellwerksdatei passt nicht zu dieser Welt.");
            foreach(var r in doc.Routes){if(r==null)throw new InvalidDataException("Leere Fahrstraße.");r.Validate();}
            if(doc.Routes.Select(r=>r.Id).Distinct().Count()!=doc.Routes.Count||doc.Routes.Select(r=>r.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=doc.Routes.Count)throw new InvalidDataException("Doppelte Fahrstraßenkennung.");
        }
        public void Save(IEnumerable<RouteDefinition> routes) {
            var doc=new InterlockingDocument{WorldKey=key,Routes=routes.Select(r=>r.Clone()).ToList()};Validate(doc);
            if(sharedWrite!=null){sharedWrite(doc.Routes);return;}
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllText(temp,JsonConvert.SerializeObject(doc,Formatting.Indented),new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
    public interface IInterlockingWorld {
        bool PathValid(RouteDefinition route);
        TrackOccupancy Occupancy(TrackSection section);
        bool SwitchPosition(string node,out bool thrown);
        bool SetSwitch(string node,bool thrown);
    }
    public interface ISectionalWorld {
        bool CaptureTrain(RouteDefinition route,out string[] cars);
        bool TrainProgress(RouteDefinition route,string[] cars,out double head,out double tail);
    }
    public interface ISwitchFeedbackWorld {bool SwitchSettled(string node,bool thrown,out string detail);}
    public interface ITrainStopWorld {bool TrainStopped(string[] cars);}
    public sealed class RouteRun {
        public int ApproachStopChecks;
        public string ApproachStopIdentity;
        public double ApproachStopReadTime=-100;
        public bool OperatorReleaseConfirmed;
        public string[] IncorporatedFrom;
        public RouteDefinition Route;
        public RouteStage Stage;
        public string Reason;
        public bool Entered;
        public bool StartPassageVerified,NumberPassageHandled;
        public int ClearChecks;
        public readonly HashSet<string> PassedSignals=new HashSet<string>();
        public readonly HashSet<string> ReleasedSections=new HashSet<string>(),ReleasedSwitches=new HashSet<string>();
        public readonly Dictionary<string,int> ReleaseChecks=new Dictionary<string,int>();
        public string[] TrainCars;
        public double? TailProgress;
        public bool HeadKnown;
        public string FaultReason;
        public string ChainId;
        public bool ChainReady=true;
        public double StartedAt,WaitUntil;
        public int AlignmentChecks,DestinationChecks;
        public readonly Dictionary<string,int> PointChecks=new Dictionary<string,int>();
        public readonly Dictionary<string,string> PointStates=new Dictionary<string,string>();
        public string[] WithdrawnRoutes;
        public string ReleaseSelection,ReleaseStatus;
        public double ReleaseAt;
        public IEnumerable<TrackSection> HeldSections=>Route.Sections.Where(s=>!ReleasedSections.Contains(s.Key));
        public bool EntryReleased=>Route.Path!=null&&ReleasedSections.Contains(Route.Path[0].Section.Key);
        public bool Permitted=>Stage==RouteStage.Set&&!Entered&&ChainReady;
        public bool PermitsVia(string id)=>ChainReady&&(Stage==RouteStage.Set||Stage==RouteStage.Running)&&(!Entered||HeadKnown)&&!PassedSignals.Contains(id)&&Route.ViaSignals!=null&&Route.ViaSignals.Any(s=>s.Id==id);
    }
    // State is independent of Unity, UI and passive diagnostics.
    public sealed partial class InterlockingEngine {
        readonly Dictionary<string,RouteRun> active=new Dictionary<string,RouteRun>();
        public IReadOnlyCollection<RouteRun> Active=>active.Values.ToArray();
        static IEnumerable<string> Controlled(RouteDefinition r)=>new[]{r.Entry}.Concat(r.ViaSignals?.Select(s=>s.Id)??Enumerable.Empty<string>());
        static bool Conflict(RouteDefinition a,RouteRun b)=>Controlled(a).Intersect(Controlled(b.Route).Where(id=>id==b.Route.Entry?!b.EntryReleased:!b.PassedSignals.Contains(id))).Any()||Controlled(a).Intersect(HeldFlankSignals(b.Route,b.ReleasedSwitches).Select(s=>s.Id)).Any()||HeldFlankSignals(a).Select(s=>s.Id).Intersect(Controlled(b.Route).Where(id=>id==b.Route.Entry?!b.EntryReleased:!b.PassedSignals.Contains(id))).Any()||a.Sections.Any(s=>b.HeldSections.Any(s.Overlaps))||a.Switches.Any(s=>b.Route.Switches.Any(t=>s.Node==t.Node&&!b.ReleasedSwitches.Contains(t.Node)));
        static TrackOccupancy Read(IEnumerable<TrackSection> sections,IInterlockingWorld world) {
            bool occupied=false;foreach(var s in sections){var result=world.Occupancy(s);if(result==TrackOccupancy.Unknown)return result;occupied|=result==TrackOccupancy.Occupied;}return occupied?TrackOccupancy.Occupied:TrackOccupancy.Clear;
        }
        public bool Locked(string node,bool thrown)=>active.Values.Any(r=>r.Route.Switches.Any(s=>s.Node==node&&s.Thrown!=thrown&&!r.ReleasedSwitches.Contains(node)));
        public bool UsesSignal(string id)=>active.Values.Any(r=>r.Route.Entry==id||r.Route.Exit==id||r.Route.ViaSignals!=null&&r.Route.ViaSignals.Any(s=>s.Id==id)||HeldFlankSignals(r.Route).Any(s=>s.Id==id));
        public RouteRun ForSignal(string id)=>active.Values.FirstOrDefault(r=>r.Route.Entry==id&&r.Permitted)??active.Values.FirstOrDefault(r=>r.Route.Entry==id);
        public RouteRun ForViaSignal(string id)=>active.Values.FirstOrDefault(r=>r.PermitsVia(id))??active.Values.FirstOrDefault(r=>r.Route.ViaSignals!=null&&r.Route.ViaSignals.Any(s=>s.Id==id));
        static bool Accepts(TrackSection section,RouteDefinition route,TrackOccupancy occupancy)=>occupancy==TrackOccupancy.Clear||occupancy==TrackOccupancy.Occupied&&RouteSignals.TargetContains(route,section);
        static bool RequestClear(RouteDefinition route,IInterlockingWorld world)=>route.Sections.All(s=>Accepts(s,route,world.Occupancy(s)))&&route.Switches.All(s=>s.Protection==null||s.Protection.All(p=>world.Occupancy(p)==TrackOccupancy.Clear));
        public void Request(RouteDefinition definition,IInterlockingWorld world,double protectionWait=15)=>BeginRequest(definition,world,protectionWait);
        public void Tick(IInterlockingWorld world) {
            var done=new List<string>();
            foreach(var run in active.Values.ToArray()) {
                if(!active.ContainsKey(run.Route.Id))continue;
                try {
                    if(run.ReleaseSelection!=null){if(ProcessRelease(run,world))done.Add(run.Route.Id);continue;}
                    if(run.Stage==RouteStage.WaitingProtection){ContinueWithdrawal(run,world);continue;}
                    if(run.Stage==RouteStage.Preparing){ContinuePreparation(run,world);continue;}
                    if(!world.PathValid(run.Route))throw new InvalidOperationException("Fahrweg oder Signalzuordnung nicht mehr gültig.");
                    CheckFlanks(run,world);
                    foreach(var s in run.Route.Switches.Where(s=>!run.ReleasedSwitches.Contains(s.Node))){bool value;if(!world.SwitchPosition(s.Node,out value)||value!=s.Thrown)throw new InvalidOperationException("Verschlossene Weiche stimmt nicht mehr.");}
                    var feedback=world as ISwitchFeedbackWorld;
                    if(feedback!=null)foreach(var point in run.Route.Switches.Where(s=>!run.ReleasedSwitches.Contains(s.Node))){string detail;if(!feedback.SwitchSettled(point.Node,point.Thrown,out detail))throw new InvalidOperationException("Weiche nicht in bestätigter Endlage: "+point.Node+" · "+detail);}
                    var occupied=Read(run.HeldSections,world);
                    if(occupied==TrackOccupancy.Unknown)throw new InvalidOperationException("Belegung nicht verfügbar.");
                    double head=double.NaN,tail=double.NaN;bool progress=false;var sectional=world as ISectionalWorld;
                    if(sectional!=null&&run.Route.Path!=null) {
                        if(run.TrainCars==null){string[] cars;if(sectional.CaptureTrain(run.Route,out cars))run.TrainCars=cars;}
                        if(run.TrainCars!=null)progress=sectional.TrainProgress(run.Route,run.TrainCars,out head,out tail);
                        run.HeadKnown=!double.IsNaN(head);
                        if(run.TrainCars!=null&&!run.HeadKnown)throw new InvalidOperationException("Zuordnung oder Position des eingefahrenen Zuges nicht mehr sicher lesbar.");
                        run.TailProgress=progress?(double?)tail:null;
                        if(head>.1) {
                            run.Entered=true;
                            if(run.ChainReady&&(run.Stage==RouteStage.Set||run.Stage==RouteStage.Running))
                                run.StartPassageVerified=true;
                        }
                        if(run.Route.ViaSignals!=null)foreach(var via in run.Route.ViaSignals)if(head>via.Distance+.1)run.PassedSignals.Add(via.Id);
                    } else if(run.Route.ViaSignals!=null)foreach(var via in run.Route.ViaSignals) {
                        var beyond=Read(via.Downstream,world);if(beyond==TrackOccupancy.Unknown)throw new InvalidOperationException("Belegung hinter Rangiersignal nicht verfügbar: "+via.Id);
                        if(beyond==TrackOccupancy.Occupied)run.PassedSignals.Add(via.Id);
                    }
                    if(!run.Route.Shunting&&occupied==TrackOccupancy.Occupied)run.Entered=true;
                    if(run.Entered&&run.Stage==RouteStage.Set){run.Stage=RouteStage.Running;run.Reason="Startsignal Halt; geschützte Abschnitte bis zum Zugschluss verschlossen";}
                    if(run.Entered&&progress&&run.Route.Path!=null){ReleaseBehind(run,world,tail);ReleaseDestination(run,world,head,tail);}else if(!progress){run.ReleaseChecks.Clear();run.DestinationChecks=0;}
                    if(run.Entered&&run.Route.Path!=null&&!run.HeldSections.Any())done.Add(run.Route.Id);
                    else if(run.Entered&&run.Stage==RouteStage.Running&&run.Route.Path==null&&occupied==TrackOccupancy.Clear){if(++run.ClearChecks>=2)done.Add(run.Route.Id);}
                    else run.ClearChecks=0;
                }catch(Exception e){Fault(run,e.Message);}
            }
            foreach(string id in done)active.Remove(id);
            RefreshChains();
        }
        public void Cancel(string id,IInterlockingWorld world) {
            RouteRun run;if(!active.TryGetValue(id,out run))return;
            run.Stage=RouteStage.Cancelled;run.Reason="Halt; Auflösung angefordert";
            if(!world.PathValid(run.Route)){run.Reason="Halt gesetzt. Verschluss bleibt: Fahrwegzuordnung nicht mehr prüfbar.";throw new InvalidOperationException(run.Reason);}
            if(!run.HeldSections.All(s=>Accepts(s,run.Route,world.Occupancy(s)))||Read(run.Route.Approach,world)!=TrackOccupancy.Clear){run.Reason="Halt gesetzt. Verschluss bleibt: "+BlockingReason(run,world);throw new InvalidOperationException(run.Reason);}
            active.Remove(id);
        }
        public void HaltAll(){foreach(var r in active.Values)Fault(r,"Alle Signale Halt; Verschlüsse bleiben bis zur sicheren Auflösung.");RefreshChains();}
        static bool Twice(RouteRun run,string key,bool clear){int count;run.ReleaseChecks.TryGetValue(key,out count);count=clear?count+1:0;run.ReleaseChecks[key]=count;return count>=2;}
        static void ReleaseBehind(RouteRun run,IInterlockingWorld world,double tail) {
            foreach(var s in run.Route.Switches.Where(s=>!run.ReleasedSwitches.Contains(s.Node))) {
                bool clear=s.ReleaseAt.HasValue&&s.Protection!=null&&tail>s.ReleaseAt.Value+.5&&s.Protection.All(p=>world.Occupancy(p)==TrackOccupancy.Clear);
                if(Twice(run,"switch:"+s.Node,clear))run.ReleasedSwitches.Add(s.Node);
            }
            double distance=0;var ends=new Dictionary<string,double>();foreach(var piece in run.Route.Path){distance+=piece.Section.To-piece.Section.From;ends[piece.Section.Key]=distance;}
            foreach(var section in run.HeldSections.ToArray()) {
                var points=run.Route.Switches.Where(s=>s.Protection!=null&&s.Protection.Any(p=>p.Overlaps(section))).ToArray();
                bool pointsFree=points.All(s=>run.ReleasedSwitches.Contains(s.Node));double end;
                bool behind=ends.TryGetValue(section.Key,out end)?tail>end+.5:points.Length>0;
                bool target=RouteSignals.TargetContains(run.Route,section)&&tail>RouteSignals.TargetStart(run.Route)+.5;
                bool clear=pointsFree&&(target||behind&&world.Occupancy(section)==TrackOccupancy.Clear);
                if(Twice(run,"section:"+section.Key,clear))run.ReleasedSections.Add(section.Key);
            }
        }
    }
}
