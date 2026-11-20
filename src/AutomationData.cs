using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public enum MovementRole { Unassigned,Shunt,Train }
    public enum SignalDuty { Ordinary,Entry,Exit,Intermediate,Block }
    public enum StationAcceptance { Through,DestinationOnly,Closed }
    public sealed class LocomotiveDuty {
        public MovementRole Role;
        public string Consist="";
    }
    public sealed class AutomaticSignal {
        public SignalDuty Duty;
        public bool Enabled,Reduced,AllowManual=true,AllowWaypoint=true;
        public bool OutOfService;
        public string Primary="",Alternate="";
        public double RetrySeconds=5;
        public void Validate(string id) {
            if(!Enum.IsDefined(typeof(SignalDuty),Duty)||Primary==null||Alternate==null||Primary.Length>128||Alternate.Length>128||Primary==id||Alternate==id||
                Alternate!=""&&Alternate==Primary||Enabled&&Primary==""||double.IsNaN(RetrySeconds)||double.IsInfinity(RetrySeconds)||RetrySeconds<1||RetrySeconds>60)
                throw new InvalidDataException("Automatik: gültiges Erst-/Ausweichziel und 1–60 s Wiederholungsabstand wählen.");
        }
    }
    public sealed class AutomationContact {
        public string Id=Guid.NewGuid().ToString("N"),Name="Kontakt",Signal="";
        public MarkerAnchor Anchor;
        public bool Reverse,Enabled,AxleTest=true,ShowCounter,AutomaticSignal;
        public void Validate() {
            if(!Guid.TryParseExact(Id,"N",out _)||string.IsNullOrWhiteSpace(Name)||Name.Length>48||Name.Any(char.IsControl)||Signal==null||Signal.Length>128||Enabled&&Signal==""||Anchor==null)
                throw new InvalidDataException("Ungültiger Automatik-Kontakt.");
            Anchor.Validate();if(Math.Abs(Anchor.Lateral)>2)throw new InvalidDataException("Kontakt seitlich höchstens 2 m vom Gleis platzieren.");
        }
    }
    public sealed class AutomationConfiguration {
        public bool OnDemandRoutes=true;
        public bool AllowUnmonitoredManualRoutes=true;
        public bool AutomaticGfmCatalog=true;
        public double SignalPassageMeters=5;
        public List<string> PausedStations=new List<string>();
        public bool Enabled,BlockEnabled;
        public bool ProtectWaypointSwitches=true;
        public Dictionary<string,AutomaticSignal> Signals=new Dictionary<string,AutomaticSignal>(StringComparer.Ordinal);
        public Dictionary<string,LocomotiveDuty> Locomotives=new Dictionary<string,LocomotiveDuty>(StringComparer.Ordinal);
        public List<AutomationContact> Contacts=new List<AutomationContact>();
        public void Validate() {
            if(double.IsNaN(SignalPassageMeters)||double.IsInfinity(SignalPassageMeters)||SignalPassageMeters<0||SignalPassageMeters>25)throw new InvalidDataException(LocalText.Select("Signal passage offset must be 0–25 m.","Haltfallpunkt muss 0–25 m hinter dem Signal liegen."));
            if(PausedStations==null||PausedStations.Count>31||PausedStations.Distinct().Count()!=PausedStations.Count||PausedStations.Any(s=>!Guid.TryParseExact(s,"N",out _)))throw new InvalidOperationException("Ungültige Selbststellbetrieb-Pausen.");
            if(Signals==null||Signals.Count>512||Locomotives==null||Locomotives.Count>512||Contacts==null||Contacts.Count>64||Contacts.Any(c=>c==null)||Contacts.Select(c=>c.Id).Distinct().Count()!=Contacts.Count)
                throw new InvalidDataException("Automatikkonfiguration zu groß oder unvollständig.");
            foreach(var pair in Signals){if(string.IsNullOrWhiteSpace(pair.Key)||pair.Key.Length>128||pair.Value==null)throw new InvalidDataException("Signalautomatik ohne Signal.");pair.Value.Validate(pair.Key);}
            foreach(var pair in Locomotives)if(string.IsNullOrWhiteSpace(pair.Key)||pair.Key.Length>128||pair.Value==null||!Enum.IsDefined(typeof(MovementRole),pair.Value.Role)||pair.Value.Consist==null||pair.Value.Consist.Length>64)
                throw new InvalidDataException("Ungültige Lok-Betriebsart.");
            foreach(var contact in Contacts){contact.Validate();if(contact.Signal!=""&&!Signals.ContainsKey(contact.Signal))throw new InvalidDataException("Kontakt verweist auf fehlende Signalautomatik.");}
            if(Enabled&&!BlockEnabled)throw new InvalidDataException("Kontakt-Automatik benötigt die gemeinsame Blockabstimmung.");
        }
        public MovementRole Role(string locomotive,string consist)=>Locomotives.TryGetValue(locomotive,out var value)&&value.Consist==consist?value.Role:MovementRole.Unassigned;
        public void Assign(string locomotive,IEnumerable<string> coupledLocomotives,string consist,MovementRole role) {
            if(!Enum.IsDefined(typeof(MovementRole),role))throw new InvalidDataException("Unbekannte Betriebsart.");
            var ids=coupledLocomotives?.Distinct().ToArray()??throw new InvalidDataException("Verband fehlt.");
            if(!ids.Contains(locomotive)||string.IsNullOrEmpty(consist)||consist.Length>64||ids.Any(id=>string.IsNullOrWhiteSpace(id)||id.Length>128))throw new InvalidDataException("Lok / Verband ungültig.");
            foreach(string id in ids)Locomotives[id]=new LocomotiveDuty{Role=id==locomotive?role:MovementRole.Unassigned,Consist=consist};
        }
    }
    public sealed class PointPassageTracker {
        sealed class Sample {public double Position,Time;public int Side;}
        readonly Dictionary<string,Sample> samples=new Dictionary<string,Sample>();
        public int Observe(string key,double position,double now) {
            if(double.IsNaN(position)||double.IsInfinity(position)||double.IsNaN(now)||double.IsInfinity(now)){samples.Remove(key);return 0;}
            int side=position<-.03?-1:position>.03?1:0;
            if(!samples.TryGetValue(key,out var old)){samples[key]=new Sample{Position=position,Time=now,Side=side};return 0;}
            if(now<=old.Time)return 0;
            if(now-old.Time>2||Math.Abs(position-old.Position)>100){old.Time=now;old.Position=position;old.Side=side;return 0;}
            int passage=side!=0&&old.Side!=0&&side!=old.Side?side:0;
            old.Time=now;old.Position=position;if(side!=0)old.Side=side;return passage;
        }
        public void Prune(double now){foreach(var key in samples.Where(p=>now-p.Value.Time>5).Select(p=>p.Key).ToArray())samples.Remove(key);}
        public void Clear()=>samples.Clear();
    }
}
