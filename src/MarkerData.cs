using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public sealed class MarkerOptions {
        public bool Visible=true;
        public bool AdaptExistingSpeedSigns=true;
        public bool EnableAiSignals=false,ShowBuildButton=true;
        public bool UseGameSpacing=true;
        public double ProtectionWaitSeconds=15;
        public bool EconomyEnabled;
        public bool DefaultRightSide=true,FollowView=true,AutoPlan=true;
        public double MaterialFactor=1;
        public double Lateral=2.3,Height=0,Scale=1;
        public SignalGlowOptions Glow=new SignalGlowOptions();
        public MarkerOptions Normalize(){Glow=(Glow??new SignalGlowOptions()).Normalize();Lateral=Bound(Lateral,-5,5,2.3);Height=Bound(Height,-1,2,0);Scale=Bound(Scale,.5,3,1);ProtectionWaitSeconds=Bound(ProtectionWaitSeconds,0,300,15);MaterialFactor=Bound(MaterialFactor,.25,4,1);return this;}
        static double Bound(double value,double min,double max,double fallback)=>double.IsNaN(value)||double.IsInfinity(value)?fallback:Math.Max(min,Math.Min(max,value));
    }
    public sealed class MarkerAnchor {
        public string Segment;
        public double Meters,Lateral=1.8,Height=-.15,Scale=1;
        public MarkerAnchor Clone()=>new MarkerAnchor{Segment=Segment,Meters=Meters,Lateral=Lateral,Height=Height,Scale=Scale};
        public void Validate() {
            if(string.IsNullOrWhiteSpace(Segment)||Segment.Length>200)throw new InvalidDataException("Gleissegment fehlt oder ist ungültig.");
            if(!Finite(Meters)||Meters<0||!Finite(Lateral)||Math.Abs(Lateral)>5||!Finite(Height)||Height< -1||Height>2||!Finite(Scale)||Scale<.5||Scale>3)throw new InvalidDataException("Ungültige Gleisposition oder Darstellungsmaße.");
        }
        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    }
    public sealed class MarkerRecord {
        public const string Clearance="clearance",Speed="speed",Main="main",Distant="distant",Shunt="shunt",Auxiliary="auxiliary-destination",CircuitA="circuit-a",CircuitB="circuit-b";
        public string Id,Name;
        [Newtonsoft.Json.JsonProperty(NullValueHandling=Newtonsoft.Json.NullValueHandling.Ignore)]
        public string DisplayName;
        [Newtonsoft.Json.JsonProperty(NullValueHandling=Newtonsoft.Json.NullValueHandling.Ignore)]
        public double? LabelScale;
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayTitle=>Caption(DisplayName,MastName,Name);
        [JsonIgnore] public string MastName;
        public static string Caption(string displayName,string mastName,string code)=>
            !string.IsNullOrWhiteSpace(displayName)?displayName:!string.IsNullOrWhiteSpace(mastName)?mastName:code;
        public string Kind=Clearance;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public int CircuitNumber,CircuitVariant;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public double CircuitOffset;
        public double SpeedMph=20;
        public bool FacingReverse,Planned;
        public MastConfiguration Mast;
        public int VisualRevision;
        public bool UseHostPlacement=true;
        [JsonIgnore] public string PhysicalId;
        [JsonIgnore] public bool AllowHp1=true,AllowHp2=true,AllowSh1;
        public string LinkedSignal;
        public bool IsCircuitBox=>Kind==CircuitA||Kind==CircuitB;
        public bool IsSignal=>Kind==Main||Kind==Distant||Kind==Shunt;
        public static string KindLabel(string kind)=>kind==Main?"Signalmast":kind==Distant?"Vorsignal":kind==Shunt?"Rangiersignal":kind==Speed?"Geschwindigkeitsschild":kind==CircuitA?"Gleisfreimeldekasten A":kind==CircuitB?"Gleisfreimeldekasten B":"Grenzzeichen";
        public MarkerAnchor Anchor;
        public MarkerRecord Clone()=>new MarkerRecord{Id=Id,Name=Name,MastName=MastName,DisplayName=DisplayName,LabelScale=LabelScale,Kind=Kind,SpeedMph=SpeedMph,CircuitNumber=CircuitNumber,CircuitVariant=CircuitVariant,CircuitOffset=CircuitOffset,FacingReverse=FacingReverse,Planned=Planned,LinkedSignal=LinkedSignal,Mast=Mast?.Clone(),VisualRevision=VisualRevision,UseHostPlacement=UseHostPlacement,PhysicalId=PhysicalId,AllowHp1=AllowHp1,AllowHp2=AllowHp2,AllowSh1=AllowSh1,Anchor=Anchor.Clone()};
    }
    public sealed class MarkerDocument {
        public int Schema=1,NextNumber=1,NextCircuitNumber=1;
        public string WorldKey,Map;
        public List<MarkerRecord> Markers=new List<MarkerRecord>();
    }
    public sealed class MarkerRepository {
        public const int Maximum=128;
        public readonly string FilePath,WorldKey,Map;
        MarkerDocument document;
        readonly Func<MarkerDocument> sharedRead;
        readonly Action<MarkerDocument> sharedWrite;
        public IReadOnlyList<MarkerRecord> Records {get{RefreshShared();return document.Markers.Select(x=>x.Clone()).ToArray();}}
        public MarkerRepository(string directory,string worldKey,string map,Func<MarkerDocument> read,Action<MarkerDocument> write){WorldKey=worldKey;Map=map;FilePath=Path.Combine(directory,worldKey+".json");sharedRead=read;sharedWrite=write;RefreshShared();}
        void RefreshShared(){if(sharedRead==null)return;var value=sharedRead();Validate(value);document=value;}
        public MarkerDocument Snapshot(){RefreshShared();return Copy();}
        public MarkerRepository(string directory,string worldKey,string map) {
            if(worldKey==null||worldKey.Length!=64||worldKey.Any(c=>!Uri.IsHexDigit(c)))throw new ArgumentException("Ungültige Weltkennung.");
            WorldKey=worldKey;Map=map;FilePath=Path.Combine(directory,worldKey+".json");
            document=new MarkerDocument{WorldKey=worldKey,Map=map};
            if(File.Exists(FilePath)) {
                if(new FileInfo(FilePath).Length>1024*1024)throw new InvalidDataException("Grenzzeichendatei ist ungewöhnlich groß; Datei bleibt erhalten.");
                document=JsonConvert.DeserializeObject<MarkerDocument>(File.ReadAllText(FilePath))??throw new InvalidDataException("Leere Grenzzeichendatei.");
                Validate(document);
            }
        }
        public static string Key(string railroadPath,string map) {
            if(string.IsNullOrWhiteSpace(railroadPath)||string.IsNullOrWhiteSpace(map))throw new ArgumentException("Lokaler Spielstandpfad und Karte erforderlich.");
            string text=Path.GetFullPath(railroadPath).ToUpperInvariant()+"|"+map;
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();
        }
        public MarkerRecord Put(MarkerAnchor anchor,string existingId=null,string kind=null,double? speedMph=null,bool? useHost=null,bool? reverse=null) {
            anchor.Validate();var next=Copy();MarkerRecord row;
            if(existingId==null) {
                if(next.Markers.Count>=Maximum)throw new InvalidOperationException("P0-Grenze erreicht: höchstens 128 Grenzzeichen je Spielstand.");
                string prefix=kind==MarkerRecord.Main?"HS-":kind==MarkerRecord.Distant?"VS-":kind==MarkerRecord.Shunt?"RS-":kind==MarkerRecord.Speed?"GS-":kind==MarkerRecord.CircuitA?"A-":kind==MarkerRecord.CircuitB?"B-":"GZ-";
                var reserved=new HashSet<string>(next.Markers.Select(m=>m.Name),StringComparer.OrdinalIgnoreCase);
                reserved.UnionWith(SignalCatalogue.Expand(next.Markers,true).Where(m=>m.IsSignal).Select(m=>m.Name));
                string name;
                do{name=prefix+(next.NextNumber++).ToString("D3");}while(reserved.Contains(name));
                row=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name=name,Anchor=anchor.Clone()};next.Markers.Add(row);
            } else {row=next.Markers.FirstOrDefault(x=>x.Id==existingId)??throw new InvalidOperationException("Grenzzeichen nicht gefunden.");row.Anchor=anchor.Clone();}
            if(kind!=null)row.Kind=kind;if(speedMph.HasValue)row.SpeedMph=speedMph.Value;
            if(useHost.HasValue)row.UseHostPlacement=useHost.Value;
            if(reverse.HasValue)row.FacingReverse=reverse.Value;
            if(row.Kind==MarkerRecord.Main&&row.Mast==null)row.Mast=MastConfiguration.Legacy(row);
            Save(next);return document.Markers.First(m=>m.Id==row.Id).Clone();
        }
        public void Remove(string id) {
            var next=Copy();if(next.Markers.RemoveAll(x=>x.Id==id)!=1)throw new InvalidOperationException("Grenzzeichen nicht gefunden.");Save(next);
        }
        public void Rename(string id,string code) {
            code=(code??"").Trim();
            if(code.Length<1||code.Length>48||code.Any(c=>char.IsControl(c)||c=='<'||c=='>')||document.Markers.Any(m=>m.Id!=id&&string.Equals(m.Name,code,StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException("Kennung muss eindeutig sein und 1–48 Zeichen enthalten.");
            var next=Copy();var row=next.Markers.FirstOrDefault(m=>m.Id==id)??throw new InvalidDataException("Objekt fehlt.");
            if(row.Kind==MarkerRecord.Main){var mast=MastConfiguration.Effective(row);if(mast.Front.Code==row.Name)mast.Front.Code=code;row.Mast=mast;}
            row.Name=code;row.VisualRevision++;Save(next);
        }
        public void ConfigureMast(string id,MastConfiguration config,string name=null,double? labelScale=null) {
            var next=Copy();var row=next.Markers.FirstOrDefault(m=>m.Id==id&&m.Kind==MarkerRecord.Main)??throw new InvalidDataException("Signalmast fehlt.");
            config.Validate(id);row.Mast=config.Clone();if(name!=null)row.Name=Name(name);if(labelScale.HasValue)row.LabelScale=labelScale;row.VisualRevision++;Save(next);
        }
        static string Name(string code){code=(code??"").Trim();if(code.Length<1||code.Length>48||code.Any(c=>char.IsControl(c)||c=='<'||c=='>'))throw new InvalidOperationException("Name muss 1–48 gültige Zeichen enthalten.");return code;}
        public void ConfigureDetails(string id,string name,double? mph,bool reverse,string linked,string displayName=null,double? labelScale=null) {
            var next=Copy();var row=next.Markers.FirstOrDefault(m=>m.Id==id)??throw new InvalidDataException("Objekt fehlt.");
            row.Name=Name(name);if(mph.HasValue){if(row.Kind!=MarkerRecord.Speed||!SignUnits.ValidSpeed(mph.Value))throw new InvalidDataException("Ungültige Schildgeschwindigkeit.");row.SpeedMph=mph.Value;}
            if(row.IsSignal&&labelScale.HasValue){row.LabelScale=labelScale;row.DisplayName=SignalLabel.Normalize(displayName);}
            row.FacingReverse=reverse;if(row.Kind==MarkerRecord.Distant)row.LinkedSignal=linked;row.VisualRevision++;Save(next);
        }
        public void ConfigureCircuit(string id,int variant,double offset) {
            var next=Copy();
            var row=next.Markers.FirstOrDefault(m=>m.Id==id&&m.IsSignal)??throw new InvalidOperationException("Signal fehlt.");
            row.CircuitVariant=variant;
            row.CircuitOffset=offset;
            row.VisualRevision++;
            Save(next);
        }
        public void ConfigurePlacement(string id,double lateral,double height,double scale,bool useHost) {
            var next=Copy();var row=next.Markers.FirstOrDefault(m=>m.Id==id)??throw new InvalidDataException("Objekt fehlt.");
            row.Anchor.Lateral=lateral;row.Anchor.Height=height;row.Anchor.Scale=scale;row.UseHostPlacement=useHost;Save(next);
        }
        public void ApplyHostPlacement(MarkerOptions options,Func<MarkerRecord,bool> mayChange) {
            var next=Copy();foreach(var row in next.Markers.Where(r=>r.IsSignal&&r.UseHostPlacement)) {
                if(!mayChange(row))throw new InvalidOperationException("Ein Mast ist verschlossen. Vorgaben erst nach sicherer Fahrstraßenauflösung auf bestehende Masten anwenden.");
                row.Anchor.Lateral=PlacementDirection.Side(options.Lateral,options.DefaultRightSide,row.FacingReverse);row.Anchor.Height=options.Height;row.Anchor.Scale=options.Scale;
            }
            Save(next);
        }
        public void ConfigureSignal(string id,bool reverse,string linked) {
            var next=Copy();var row=next.Markers.FirstOrDefault(m=>m.Id==id)??throw new InvalidDataException("Signal fehlt.");
            if(!row.IsSignal)throw new InvalidDataException("Kein Signal.");
            if(linked!=null&&!next.Markers.Any(m=>m.Id==linked&&m.Kind==MarkerRecord.Main&&m.Id!=id))throw new InvalidDataException("Hauptsignal für Vorsignal fehlt.");
            row.FacingReverse=reverse;row.LinkedSignal=row.Kind==MarkerRecord.Distant?linked:null;Save(next);
        }
        MarkerDocument Copy(){RefreshShared();return new MarkerDocument{WorldKey=document.WorldKey,Map=document.Map,NextNumber=document.NextNumber,NextCircuitNumber=document.NextCircuitNumber,Markers=document.Markers.Select(x=>x.Clone()).ToList()};}
        void Validate(MarkerDocument value)=>ValidateDocument(value,WorldKey,Map);
        public static void ValidateDocument(MarkerDocument value,string key,string map) {
            if(value==null||value.Schema!=1||value.WorldKey!=key||value.Map!=map||value.Markers==null||value.Markers.Count>Maximum||value.NextNumber<1)throw new InvalidDataException("Grenzzeichendatei passt nicht zu diesem Spielstand.");
            var ids=new HashSet<string>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var row in value.Markers) {
                Guid id;if(row==null||!Guid.TryParseExact(row.Id,"N",out id)||!ids.Add(row.Id)||string.IsNullOrWhiteSpace(row.Name)||row.Name.Length>48||(row.Kind!=MarkerRecord.Main||row.Mast==null)&&!names.Add(row.Name)||row.Anchor==null)throw new InvalidDataException("Ungültige oder doppelte Grenzzeichenkennung.");
                SignalLabel.Validate(row.DisplayName,row.LabelScale);
                row.Anchor.Validate();
                if(row.Kind!=MarkerRecord.Clearance&&row.Kind!=MarkerRecord.Speed&&!row.IsSignal&&!row.IsCircuitBox||row.Kind==MarkerRecord.Speed&&!SignUnits.ValidSpeed(row.SpeedMph))throw new InvalidDataException("Ungültiger Objekttyp oder Schildgeschwindigkeit.");
            }
            CircuitIdentity.Validate(value);
            SignalCatalogue.Validate(value.Markers);
        }
        void Save(MarkerDocument next) {
            CircuitIdentity.Fill(next);
            if(sharedWrite!=null){Validate(next);sharedWrite(next);RefreshShared();return;}
            Validate(next);Directory.CreateDirectory(Path.GetDirectoryName(FilePath));string temp=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {
                File.WriteAllText(temp,JsonConvert.SerializeObject(next,Formatting.Indented),new UTF8Encoding(false));
                if(File.Exists(FilePath))File.Replace(temp,FilePath,FilePath+".bak");else File.Move(temp,FilePath);
                document=next;
            }finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
    public sealed class MarkerEditSession {
        public MarkerAnchor Candidate {get;private set;}
        public string EditingId {get;private set;}
        public bool Aiming {get;private set;}
        public string Kind {get;private set;}=MarkerRecord.Clearance;
        public double SpeedMph {get;set;}=20;
        public bool FacingReverse {get;private set;}
        public bool UseHostPlacement {get;set;}=true;
        public void Start(MarkerOptions options,string kind=MarkerRecord.Clearance){Candidate=null;EditingId=null;Aiming=true;Kind=kind;SpeedMph=20;FacingReverse=false;UseHostPlacement=true;}
        public void Edit(MarkerRecord record){Candidate=record.Anchor.Clone();EditingId=record.Id;Aiming=false;Kind=record.Kind;SpeedMph=record.SpeedMph;FacingReverse=record.FacingReverse;UseHostPlacement=record.UseHostPlacement;}
        public void Offer(MarkerAnchor anchor){if(Aiming)Candidate=anchor?.Clone();}
        public void Face(bool reverse){FacingReverse=reverse;}
        public void Freeze(){if(Candidate==null)throw new InvalidOperationException("Noch keine gültige Gleisposition gewählt.");Aiming=false;}
        public void Aim(){Aiming=true;}
        public void Cancel(){Candidate=null;EditingId=null;Aiming=false;}
        public MarkerRecord Commit(MarkerRepository repository,Func<MarkerAnchor,bool> valid) {
            if(Candidate==null||!valid(Candidate))throw new InvalidOperationException("Gleisposition nicht mehr gültig; erneut zielen.");
            var row=repository.Put(Candidate,EditingId,Kind,SpeedMph,UseHostPlacement,FacingReverse);Cancel();return row;
        }
    }
}
