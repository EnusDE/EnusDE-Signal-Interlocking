using System;
using System.Collections.Generic;
using System.Linq;
namespace EnusDE.Signaltechnik {
    public enum LampTestPart { Main,Distant,Shunt }
    // Ephemeral host/editor test. The native side sees Stop, never the chosen light state.
    public sealed class SignalLampTest {
        readonly HashSet<string> held=new HashSet<string>();
        public string PhysicalId {get;private set;} public string FaceId {get;private set;}
        public LampTestPart Part {get;private set;} public LocalSignalAspect Aspect {get;private set;}
        public double Deadline {get;private set;} public bool Active=>PhysicalId!=null;
        public void Start(string physical,string face,IEnumerable<string> ids,LampTestPart part,LocalSignalAspect aspect,double now,bool host,bool editor,bool ready,bool unlocked) {
            if(!host||!editor||!ready||!unlocked)throw new InvalidOperationException(LocalText.Select("Lamp tests require the loaded host Build editor, a completed signal and released routes/direction locks.","Lampentests benötigen den geladenen Host-Baumodus, ein fertig gebautes Signal und aufgelöste Fahrstraßen/Richtungsbindungen."));
            var keys=(ids??Enumerable.Empty<string>()).ToArray();
            if(!Guid.TryParseExact(physical,"N",out _)||!Guid.TryParseExact(face,"N",out _)||keys.Length<1||keys.Length>2||!keys.Contains(face)||keys.Any(id=>!Guid.TryParseExact(id,"N",out _))||!Enum.IsDefined(typeof(LampTestPart),part)||!Enum.IsDefined(typeof(LocalSignalAspect),aspect)||double.IsNaN(now)||double.IsInfinity(now))throw new ArgumentException("Invalid lamp test request.");
            held.Clear();foreach(var id in keys)held.Add(id);PhysicalId=physical;FaceId=face;Part=part;Aspect=aspect;Deadline=now+120;
        }
        public bool Holds(string id)=>id!=null&&held.Contains(id);
        public bool Expired(double now)=>Active&&(double.IsNaN(now)||double.IsInfinity(now)||now>=Deadline);
        public bool Targets(string id)=>Active&&FaceId==id;
        public LocalSignalAspect Visual(string id,LocalSignalAspect actual)=>!Targets(id)?actual:Part==LampTestPart.Distant?LocalSignalAspect.Clear:Part==LampTestPart.Shunt?(Aspect==LocalSignalAspect.Shunt?LocalSignalAspect.Shunt:LocalSignalAspect.Stop):Aspect;
        public SignalLampPlan Plan(string id,SignalLampPlan actual) {
            if(!Targets(id))return actual;
            if(Part==LampTestPart.Distant)return new SignalLampPlan{Main=LocalSignalAspect.Clear,Shunt=LocalSignalAspect.Dark,Distant=Aspect};
            if(Part==LampTestPart.Shunt)return new SignalLampPlan{Main=LocalSignalAspect.Stop,Shunt=Aspect,Distant=LocalSignalAspect.Dark};
            return SignalLampPlan.For(Aspect,LocalSignalAspect.Stop);
        }
        public bool Blocks(RouteDefinition route)=>Active&&(Holds(route.Entry)||Holds(route.Exit)||(route.ViaSignals?.Any(s=>Holds(s.Id))??false)||(route.Flanks?.Any(f=>f.Signals.Any(s=>Holds(s.Id)))??false));
        public void Clear(){held.Clear();PhysicalId=FaceId=null;Deadline=0;}
    }
}
