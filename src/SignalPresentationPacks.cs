using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Presentation-only API v1. No mutable markers, routes, native signals or runtime references.
    public sealed class SignalFaceView {
        public bool Enabled {get;} public bool ShuntHead {get;} public bool DistantHead {get;}
        public string Code {get;} public float LabelScale {get;} public string Caption {get;} public LocalSignalAspect Main {get;} public LocalSignalAspect Shunt {get;} public LocalSignalAspect Distant {get;}
        public SignalFaceView(bool enabled,bool shunt,bool distant,string caption,LocalSignalAspect main,LocalSignalAspect shunting,LocalSignalAspect announced,float labelScale=1):this(enabled,shunt,distant,caption,main,shunting,announced,labelScale,null) {}
        public SignalFaceView(bool enabled,bool shunt,bool distant,string caption,LocalSignalAspect main,LocalSignalAspect shunting,LocalSignalAspect announced,float labelScale,string code) {
            Enabled=enabled;ShuntHead=shunt;DistantHead=distant;Caption=caption??"";Code=code??Caption;LabelScale=float.IsNaN(labelScale)||float.IsInfinity(labelScale)?1:Math.Max(.5f,Math.Min(3,labelScale));
            Main=Safe(main);Shunt=Safe(shunting);Distant=Safe(announced);
        }
        static LocalSignalAspect Safe(LocalSignalAspect a)=>Enum.IsDefined(typeof(LocalSignalAspect),a)?a:LocalSignalAspect.Invalid;
    }
    public sealed class SignalViewFrame {
        public string Kind {get;} public bool Preview {get;} public float EmissionStrength {get;} public SignalFaceView Front {get;} public SignalFaceView Back {get;}
        public SignalViewFrame(string kind,bool preview,SignalFaceView front,SignalFaceView back,float emissionStrength=1) {Kind=kind;Preview=preview;EmissionStrength=float.IsNaN(emissionStrength)||float.IsInfinity(emissionStrength)?1:Math.Max(0,Math.Min(4,emissionStrength));Front=front??throw new ArgumentNullException(nameof(front));Back=back;}
    }
    public interface ISignalPresentation : IDisposable { Bounds LocalBounds {get;} void Apply(SignalViewFrame frame); }
    public interface ISignalPresentationProvider {
        int ApiVersion {get;} string Id {get;} string Version {get;} string Name {get;}
        ISignalPresentation Create(GameObject parent,string kind,bool preview);
    }
    public static class SignalPresentationPacks {
        public const int ApiVersion=1;
        public const string Runenwacht="enusde.runenwacht";
        static readonly Dictionary<string,ISignalPresentationProvider> providers=new Dictionary<string,ISignalPresentationProvider>(StringComparer.Ordinal);
        public static int Generation {get;private set;}
        public static string LastError {get;private set;}
        public static ISignalPresentationProvider[] Installed=>providers.Values.OrderBy(p=>p.Id,StringComparer.Ordinal).ToArray();
        public static bool ValidId(string id)=>!string.IsNullOrEmpty(id)&&id.Length<=80&&id.All(c=>c>='a'&&c<='z'||c>='0'&&c<='9'||c=='.'||c=='-');
        public static ISignalPresentationProvider Find(string id)=>id!=null&&providers.TryGetValue(id,out var p)?p:null;
        public static IDisposable Register(ISignalPresentationProvider provider) {
            if(provider==null||provider.ApiVersion!=ApiVersion||!ValidId(provider.Id)||string.IsNullOrWhiteSpace(provider.Name)||provider.Name.Length>100||!System.Version.TryParse(provider.Version,out _))throw new ArgumentException("Incompatible signal presentation provider.");
            if(providers.ContainsKey(provider.Id))throw new InvalidOperationException("Signal presentation provider already registered: "+provider.Id);
            providers.Add(provider.Id,provider);LastError=null;Generation++;return new Registration(provider);
        }
        sealed class Registration:IDisposable {
            ISignalPresentationProvider provider;
            public Registration(ISignalPresentationProvider value){provider=value;}
            public void Dispose(){if(provider==null)return;if(Find(provider.Id)==provider){providers.Remove(provider.Id);Generation++;}provider=null;}
        }
        internal static void Quarantine(ISignalPresentationProvider provider,Exception error) {
            LastError=provider.Id+": "+error.Message;
            if(Find(provider.Id)==provider){providers.Remove(provider.Id);Generation++;}
        }
        public static void ValidateSelection(MarkerRecord row) {
            if(row.PresentationPack!=null&&(!ValidId(row.PresentationPack)||!row.IsSignal||row.LowMain||row.HlSignal))throw new InvalidOperationException(LocalText.Select("Invalid signal presentation selection. Use a standard-height signal without Hl.","Ungültige Signaldarstellung. Standardhohes Signal ohne Hl verwenden."));
        }
        public static bool SelectionChanged(MarkerRecord before,MarkerRecord after)=>after?.PresentationPack!=null&&before?.PresentationPack!=after.PresentationPack;
        public static void RequirePlacement(MarkerRecord before,MarkerRecord after,bool enabled,bool licensed,bool installed) {
            ValidateSelection(after);if(!SelectionChanged(before,after))return;
            if(!enabled||!licensed||!installed)throw new InvalidOperationException(LocalText.Select("Experimental pack placement requires console activation, an installed compatible pack and its world test license.","Experimentelle Paketplatzierung benötigt Konsolenaktivierung, ein installiertes kompatibles Paket und dessen Welt-Testlizenz."));
        }
        internal static SignalViewFrame Frame(MarkerRecord row,bool preview,float emissionStrength=1) {
            if(row.Kind==MarkerRecord.Main){var mast=MastConfiguration.Effective(row);return new SignalViewFrame(row.Kind,preview,Face(mast.Front,row,preview),Face(mast.Back,row,preview),emissionStrength);}
            var aspect=preview?LocalSignalAspect.Stop:Entry.RenderSignalAspect(row.Id,Entry.SignalAspect(row.Id));
            return new SignalViewFrame(row.Kind,preview,new SignalFaceView(true,row.Kind==MarkerRecord.Shunt,row.Kind==MarkerRecord.Distant,row.DisplayTitle,aspect,aspect,aspect,(float)(row.LabelScale??1),row.Name),null,emissionStrength);
        }
        static SignalFaceView Face(SignalFace face,MarkerRecord row,bool preview) {
            var authority=preview?LocalSignalAspect.Stop:Entry.RenderSignalAspect(face.Id,Entry.SignalAspect(face.Id));
            var plan=Entry.RenderLampPlan(face.Id,SignalLampPlan.For(authority,preview?LocalSignalAspect.Stop:Entry.SignalAspect(face.DistantTarget)));
            return new SignalFaceView(face.Enabled,face.Sh1,face.DistantHead,MarkerRecord.Caption(face.DisplayName,row.DisplayName,face.Code),plan.Main,plan.Shunt,plan.Distant,(float)(row.LabelScale??1),face.Code);
        }
    }
    // Owns exactly one provider instance; disposal also runs when Unity destroys the world.
    public sealed class PackageSignalVisual:MonoBehaviour {
        ISignalPresentation instance; ISignalPresentationProvider provider;
        public Bounds LocalBounds {get;private set;}
        public bool Initialize(ISignalPresentationProvider p,string kind,bool preview) {
            provider=p;try{instance=p.Create(gameObject,kind,preview);if(instance==null)throw new InvalidOperationException("Empty presentation instance.");LocalBounds=instance.LocalBounds;var size=LocalBounds.size;var center=LocalBounds.center;if(new[]{size.x,size.y,size.z,center.x,center.y,center.z}.Any(v=>float.IsNaN(v)||float.IsInfinity(v))||size.x<=0||size.y<=0||size.z<=0||size.x>20||size.y>20||size.z>20)throw new InvalidOperationException("Invalid presentation bounds.");return true;}catch(Exception e){SignalPresentationPacks.Quarantine(p,e);Release();return false;}
        }
        public bool Apply(MarkerRecord record,bool preview,float emissionStrength=1) {
            try{instance.Apply(SignalPresentationPacks.Frame(record,preview,emissionStrength));return true;}catch(Exception e){SignalPresentationPacks.Quarantine(provider,e);Release();gameObject.SetActive(false);return false;}
        }
        void Release(){var old=instance;instance=null;try{old?.Dispose();}catch{/* A faulty presentation must not break world teardown. */}}
        void OnDestroy(){Release();}
    }
}
