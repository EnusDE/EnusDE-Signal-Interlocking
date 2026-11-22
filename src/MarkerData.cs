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
        public bool EnableHlSignalsTest;
        public bool NewSignalsWithGfm=true;
        public bool UseGameSpacing=true;
        public double ProtectionWaitSeconds=15;
        public bool EconomyEnabled,MaterialsInSandbox,FreeInEconomy;
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
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool FixedHeight,GroundSupport;
        public MarkerAnchor Clone()=>new MarkerAnchor{Segment=Segment,Meters=Meters,Lateral=Lateral,Height=Height,Scale=Scale,FixedHeight=FixedHeight,GroundSupport=GroundSupport};
        public void Validate() {
            if(string.IsNullOrWhiteSpace(Segment)||Segment.Length>200)throw new InvalidDataException("Gleissegment fehlt oder ist ungültig.");
            if(!Finite(Meters)||Meters<0||!Finite(Lateral)||Math.Abs(Lateral)>5||!Finite(Height)||Height< -1||Height>2||!Finite(Scale)||Scale<.5||Scale>3)throw new InvalidDataException("Ungültige Gleisposition oder Darstellungsmaße.");
        }
        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    }
    public sealed class MarkerRecord {
        public const string Clearance="clearance",Speed="speed",Main="main",Distant="distant",Shunt="shunt",Auxiliary="auxiliary-destination",CircuitA="circuit-a",CircuitB="circuit-b",CircuitC="circuit-c";
        public string Id,Name;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string PreviousCode;
        [Newtonsoft.Json.JsonProperty(NullValueHandling=Newtonsoft.Json.NullValueHandling.Ignore)]
        public string DisplayName;
        [Newtonsoft.Json.JsonProperty(NullValueHandling=Newtonsoft.Json.NullValueHandling.Ignore)]
        public double? LabelScale;
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayTitle=>Caption(DisplayName,MastName,Name);
        [JsonIgnore] public string MastName;
        [JsonIgnore] public bool SharedPointBoundary;
        public static string Caption(string displayName,string mastName,string code)=>
            !string.IsNullOrWhiteSpace(displayName)?displayName:!string.IsNullOrWhiteSpace(mastName)?mastName:code;
        public string Kind=Clearance;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public int CircuitNumber,CircuitVariant;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public double CircuitOffset;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public double CircuitYaw;
        [System.ComponentModel.DefaultValue(true),JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool CircuitSupport=true;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool CircuitReverse;
        public double SpeedMph=20;
        public bool FacingReverse,Planned;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool LowMain;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool HlSignal;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public string PresentationPack;
        public MastConfiguration Mast;
        public int VisualRevision;
        public bool UseHostPlacement=true;
        [JsonIgnore] public string PhysicalId;
        [JsonIgnore] public MarkerAnchor CircuitAnchor;
        [JsonIgnore] public bool AllowHp1=true,AllowHp2=true,AllowSh1;
        public string LinkedSignal;
        public static bool IsCircuitKind(string kind)=>kind==CircuitA||kind==CircuitB||kind==CircuitC;
        public bool IsCircuitBox=>IsCircuitKind(Kind);
        public bool IsSignal=>Kind==Main||Kind==Distant||Kind==Shunt;
        public static string KindLabel(string kind)=>kind==CircuitC?LocalText.Select("One-sided detection boundary C","Einseitige Freimeldegrenze C"):LocalText.Get(kind==Main?"Signalmast":kind==Distant?"Vorsignal":kind==Shunt?"Rangiersignal":kind==Speed?"Geschwindigkeitsschild":kind==CircuitA||kind==CircuitB?"Gleisfreimeldekasten B":"Grenzzeichen");
        public MarkerAnchor Anchor;
        public MarkerRecord Clone()=>new MarkerRecord{Id=Id,Name=Name,PreviousCode=PreviousCode,MastName=MastName,DisplayName=DisplayName,LabelScale=LabelScale,Kind=Kind,LowMain=LowMain,HlSignal=HlSignal,PresentationPack=PresentationPack,SpeedMph=SpeedMph,CircuitNumber=CircuitNumber,CircuitVariant=CircuitVariant,CircuitOffset=CircuitOffset,CircuitYaw=CircuitYaw,CircuitSupport=CircuitSupport,CircuitReverse=CircuitReverse,FacingReverse=FacingReverse,Planned=Planned,LinkedSignal=LinkedSignal,Mast=Mast?.Clone(),VisualRevision=VisualRevision,UseHostPlacement=UseHostPlacement,PhysicalId=PhysicalId,SharedPointBoundary=SharedPointBoundary,CircuitAnchor=CircuitAnchor?.Clone(),AllowHp1=AllowHp1,AllowHp2=AllowHp2,AllowSh1=AllowSh1,Anchor=Anchor.Clone()};
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
        public MarkerRecord Put(MarkerAnchor anchor,string existingId=null,string kind=null,double? speedMph=null,bool? useHost=null,bool? reverse=null,bool? hlSignal=null,bool newSignalGfm=true,string presentationPack=null) {
            if(kind==MarkerRecord.CircuitA)kind=MarkerRecord.CircuitB;
            anchor.Validate();var next=Copy();MarkerRecord row;
            if(existingId==null) {
                if(next.Markers.Count>=Maximum)throw new InvalidOperationException("P0-Grenze erreicht: höchstens 128 Grenzzeichen je Spielstand.");
                string prefix=kind==MarkerRecord.Main?"HS-":kind==MarkerRecord.Distant?"VS-":kind==MarkerRecord.Shunt?"RS-":kind==MarkerRecord.Speed?"GS-":kind==MarkerRecord.CircuitA?"A-":kind==MarkerRecord.CircuitB?"B-":kind==MarkerRecord.CircuitC?"C-":"GZ-";
                var reserved=new HashSet<string>(next.Markers.Select(m=>m.Name),StringComparer.OrdinalIgnoreCase);
                reserved.UnionWith(SignalCatalogue.Expand(next.Markers,true).Where(m=>m.IsSignal).Select(m=>m.Name));
                reserved.UnionWith(next.Markers.Where(m=>m.Kind==MarkerRecord.Main).SelectMany(m=>new[]{MastConfiguration.Effective(m).Front.Code,MastConfiguration.Effective(m).Back.Code}));
                string name;
                do{name=prefix+(next.NextNumber++).ToString("D3");}while(reserved.Contains(name));
                row=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name=name,Anchor=anchor.Clone(),CircuitVariant=newSignalGfm&&(kind==MarkerRecord.Main||kind==MarkerRecord.Shunt||kind==MarkerRecord.Distant)?1:0};next.Markers.Add(row);
            } else {row=next.Markers.FirstOrDefault(x=>x.Id==existingId)??throw new InvalidOperationException("Grenzzeichen nicht gefunden.");row.Anchor=anchor.Clone();}
            if(kind!=null)row.Kind=kind;if(speedMph.HasValue)row.SpeedMph=speedMph.Value;
            if(useHost.HasValue)row.UseHostPlacement=useHost.Value;
            if(reverse.HasValue)row.FacingReverse=reverse.Value;
            if(hlSignal.HasValue)row.HlSignal=hlSignal.Value;
            if(existingId==null)row.PresentationPack=presentationPack;
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
            RequireSignalCode(row,code);
            if(row.Kind==MarkerRecord.Main){var mast=MastConfiguration.Effective(row);if(mast.Front.Code==row.Name)mast.Front.Code=code;row.Mast=mast;}
            row.Name=code;row.VisualRevision++;Save(next);
        }
        public void ConfigureMast(string id,MastConfiguration config,string name=null,double? labelScale=null) {
            var next=Copy();var row=next.Markers.FirstOrDefault(m=>m.Id==id&&m.Kind==MarkerRecord.Main)??throw new InvalidDataException("Signalmast fehlt.");
            var before=MastConfiguration.Effective(row);RequireSignalCode(row,name??row.Name);
            if(config.Front?.Code!=before.Front.Code||config.Back?.Code!=before.Back.Code||config.Front?.Id!=before.Front.Id||config.Back?.Id!=before.Back.Id)
                throw new InvalidOperationException(LocalText.Select("Signal codes and IDs are assigned automatically. Edit the display name instead.","Signalkennungen und IDs werden automatisch vergeben. Stattdessen den Anzeigenamen bearbeiten."));
            config.Validate(id);row.Mast=config.Clone();if(labelScale.HasValue)row.LabelScale=labelScale;row.VisualRevision++;Save(next);
        }
        public void PromoteShunt(string id) {
            var next=Copy();var row=next.Markers.SingleOrDefault(m=>m.Id==id&&m.Kind==MarkerRecord.Shunt);
            if(row==null)throw new InvalidOperationException("Ein Rangiersignal auswählen.");
            UpgradeSignalCode(next,row);row.Kind=MarkerRecord.Main;row.LowMain=true;row.Mast=MastConfiguration.Legacy(row);
            row.Mast.Front.Sh1=true;row.Mast.Front.DisplayName=row.DisplayName;row.VisualRevision++;
            Save(next);
        }
        public void ConfigureLowMain(string id,bool low) {
            var next=Copy();var row=next.Markers.SingleOrDefault(m=>m.Id==id&&m.Kind==MarkerRecord.Main);
            if(row==null)throw new InvalidOperationException("Hauptsignal auswählen.");
            if(low&&row.PresentationPack!=null)throw new InvalidOperationException(LocalText.Select("Select the built-in presentation first.","Zuerst die eingebaute Darstellung auswählen."));
            if(low&&row.HlSignal)throw new InvalidOperationException(LocalText.Select("The Hl test model requires a high mast. Select H/V first.","Das Hl-Testmodell benötigt einen hohen Mast. Zuerst H/V auswählen."));
            if(low&&(MastConfiguration.Effective(row).Front.DistantHead||MastConfiguration.Effective(row).Back.DistantHead))
                throw new InvalidOperationException("Vorsignalköpfe vor der Umrüstung entfernen.");
            if(row.Name.StartsWith("RS-",StringComparison.OrdinalIgnoreCase))UpgradeSignalCode(next,row);
            row.LowMain=low;row.VisualRevision++;Save(next);
        }
        public void ConfigurePresentation(string id,string pack) {
            var next=Copy();var row=next.Markers.Single(m=>m.Id==id&&m.IsSignal);
            if(row.PresentationPack==pack)return;
            row.PresentationPack=pack;if(pack!=null){row.HlSignal=false;row.LowMain=false;}
            row.VisualRevision++;Save(next);
        }
        public void ConfigureHlSignal(string id,bool hl) {
            var next=Copy();var row=next.Markers.SingleOrDefault(m=>m.Id==id&&(m.Kind==MarkerRecord.Main||m.Kind==MarkerRecord.Distant));
            if(row==null)throw new InvalidOperationException(LocalText.Select("Select a main or distant signal.","Haupt- oder Vorsignal auswählen."));
            if(hl&&row.PresentationPack!=null)throw new InvalidOperationException(LocalText.Select("Select the built-in presentation first.","Zuerst die eingebaute Darstellung auswählen."));
            if(hl&&row.LowMain)throw new InvalidOperationException(LocalText.Select("Convert to a high mast before selecting Hl.","Vor der Hl-Auswahl auf einen hohen Mast umrüsten."));
            if(row.HlSignal==hl)return;
            row.HlSignal=hl;row.VisualRevision++;Save(next);
        }
        static void RequireSignalCode(MarkerRecord row,string code) {
            if(row.IsSignal&&!string.Equals(row.Name,code,StringComparison.Ordinal))throw new InvalidOperationException(LocalText.Select("The technical signal code is automatic and cannot be edited. Use the display name.","Die technische Signalkennung wird automatisch vergeben und kann nicht geändert werden. Den Anzeigenamen verwenden."));
        }
        public void NormalizeSignalCode(string id) {
            var next=Copy();var row=next.Markers.SingleOrDefault(m=>m.Id==id&&m.Kind==MarkerRecord.Main);
            if(row==null||!row.Name.StartsWith("RS-",StringComparison.OrdinalIgnoreCase))return;
            UpgradeSignalCode(next,row);row.VisualRevision++;Save(next);
        }
        static void UpgradeSignalCode(MarkerDocument doc,MarkerRecord row) {
            string old=row.Name;var reserved=new HashSet<string>(SignalCatalogue.Expand(doc.Markers.Where(m=>m.Id!=row.Id),true).Where(m=>m.IsSignal).Select(m=>m.Name),StringComparer.OrdinalIgnoreCase);
            reserved.UnionWith(doc.Markers.Where(m=>m.Id!=row.Id).Select(m=>m.Name));
            reserved.UnionWith(doc.Markers.Where(m=>m.Id!=row.Id&&m.Kind==MarkerRecord.Main).SelectMany(m=>new[]{MastConfiguration.Effective(m).Front.Code,MastConfiguration.Effective(m).Back.Code}));
            string suffix=old.StartsWith("RS-",StringComparison.OrdinalIgnoreCase)?old.Substring(3):"";
            string code=int.TryParse(suffix,out int number)&&number>0?"HS-"+number.ToString("D3",System.Globalization.CultureInfo.InvariantCulture):null;
            while(code==null||reserved.Contains(code)||reserved.Contains(code+"-R"))code="HS-"+(doc.NextNumber++).ToString("D3",System.Globalization.CultureInfo.InvariantCulture);
            row.PreviousCode=old;row.Name=code;
            if(row.Mast!=null){if(row.Mast.Front.Code==old)row.Mast.Front.Code=code;if(row.Mast.Back.Code==old+"-R")row.Mast.Back.Code=code+"-R";}
        }
        static string Name(string code){code=(code??"").Trim();if(code.Length<1||code.Length>48||code.Any(c=>char.IsControl(c)||c=='<'||c=='>'))throw new InvalidOperationException("Name muss 1–48 gültige Zeichen enthalten.");return code;}
        public void ConfigureDetails(string id,string name,double? mph,bool reverse,string linked,string displayName=null,double? labelScale=null) {
            var next=Copy();var row=next.Markers.FirstOrDefault(m=>m.Id==id)??throw new InvalidDataException("Objekt fehlt.");
            RequireSignalCode(row,name);row.Name=Name(name);if(mph.HasValue){if(row.Kind!=MarkerRecord.Speed||!SignUnits.ValidSpeed(mph.Value))throw new InvalidDataException("Ungültige Schildgeschwindigkeit.");row.SpeedMph=mph.Value;}
            if(row.IsSignal&&labelScale.HasValue){row.LabelScale=labelScale;row.DisplayName=SignalLabel.Normalize(displayName);}
            row.FacingReverse=reverse;if(row.Kind==MarkerRecord.Distant)row.LinkedSignal=linked;row.VisualRevision++;Save(next);
        }
        public void ConfigureCircuit(string id,int variant,double offset,double yaw=0,bool support=true,bool reverse=false) {
            var next=Copy();
            var row=next.Markers.FirstOrDefault(m=>m.Id==id&&(m.IsSignal||m.IsCircuitBox))??throw new InvalidOperationException("Signal oder Kasten fehlt.");
            if(row.IsCircuitBox) {
                if(variant<1||variant>3)throw new InvalidOperationException(LocalText.Select("Choose boundary B or C.","Grenze B oder C wählen."));
                row.Kind=variant==3?MarkerRecord.CircuitC:MarkerRecord.CircuitB;
            } else row.CircuitVariant=variant==2?1:variant;
            row.CircuitReverse=reverse;
            row.CircuitOffset=offset;row.CircuitYaw=yaw;row.CircuitSupport=support;
            if(row.IsCircuitBox)row.Anchor.GroundSupport=support;
            row.VisualRevision++;
            Save(next);
        }
        public void ConfigurePlacement(string id,double lateral,double height,double scale,bool useHost,bool? fixedHeight=null,bool? groundSupport=null) {
            var next=Copy();var row=next.Markers.FirstOrDefault(m=>m.Id==id)??throw new InvalidDataException("Objekt fehlt.");
            if(fixedHeight.HasValue)row.Anchor.FixedHeight=fixedHeight.Value;
            if(groundSupport.HasValue)row.Anchor.GroundSupport=groundSupport.Value;
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
                if(row.LowMain&&(row.Kind!=MarkerRecord.Main||MastConfiguration.Effective(row).Front.DistantHead||MastConfiguration.Effective(row).Back.DistantHead))throw new InvalidDataException("Niedriges Hauptsignal: ungültige Bauart oder Vorsignalkopf.");
                SignalLabel.Validate(row.DisplayName,row.LabelScale);SignalPresentationPacks.ValidateSelection(row);
                if(row.HlSignal&&(row.LowMain||row.Kind!=MarkerRecord.Main&&row.Kind!=MarkerRecord.Distant))throw new InvalidDataException(LocalText.Select("Invalid Hl signal model.","Ungültige Hl-Signalbauart."));
                SignalLabel.Validate(row.PreviousCode,null);
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
        public bool Repeating {get;private set;}
        bool fixedHeight,groundSupport;
        public string Kind {get;private set;}=MarkerRecord.Clearance;
        public double SpeedMph {get;set;}=20;
        public bool FacingReverse {get;private set;}
        public bool UseHostPlacement {get;set;}=true;
        public bool HlSignal {get;set;}
        public string PresentationPack {get;set;}
        public bool NewSignalGfm {get;set;}=true;
        public void Start(MarkerOptions options,string kind=MarkerRecord.Clearance,bool repeatSignals=false){Candidate=null;fixedHeight=groundSupport=false;EditingId=null;Aiming=true;Kind=kind;SpeedMph=20;FacingReverse=false;UseHostPlacement=true;HlSignal=false;PresentationPack=null;NewSignalGfm=options.NewSignalsWithGfm;Repeating=repeatSignals&&(kind==MarkerRecord.Main||kind==MarkerRecord.Distant||kind==MarkerRecord.Shunt);}
        public void Edit(MarkerRecord record){Repeating=Repeating&&record.Id==null;Candidate=record.Anchor.Clone();fixedHeight=Candidate.FixedHeight;groundSupport=Candidate.GroundSupport;EditingId=record.Id;Aiming=false;Kind=record.Kind;PresentationPack=record.PresentationPack;HlSignal=record.HlSignal;SpeedMph=record.SpeedMph;FacingReverse=record.FacingReverse;UseHostPlacement=record.UseHostPlacement;}
        public void Offer(MarkerAnchor anchor){if(Aiming){Candidate=anchor?.Clone();if(Candidate!=null){Candidate.FixedHeight=fixedHeight;Candidate.GroundSupport=groundSupport;}}}
        public void Face(bool reverse){FacingReverse=reverse;}
        public void Freeze(){if(Candidate==null)throw new InvalidOperationException("Noch keine gültige Gleisposition gewählt.");Aiming=false;}
        public void Aim(){Aiming=true;}
        public void Cancel(){Candidate=null;EditingId=null;Aiming=false;Repeating=false;}
        public MarkerRecord Commit(MarkerRepository repository,Func<MarkerAnchor,bool> valid) {
            if(Candidate==null||!valid(Candidate))throw new InvalidOperationException("Gleisposition nicht mehr gültig; erneut zielen.");
            var row=repository.Put(Candidate,EditingId,Kind,SpeedMph,UseHostPlacement,FacingReverse,EditingId==null?(bool?)HlSignal:null,NewSignalGfm,PresentationPack);
            if(Repeating&&EditingId==null){Candidate=null;Aiming=true;}else Cancel();return row;
        }
    }
}
