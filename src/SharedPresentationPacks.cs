using System;
using System.Linq;
namespace EnusDE.Signaltechnik {
    public sealed partial class SharedRuntime {
        public bool PresentationExperiment {get;private set;}
        public bool HasPresentationLicense(string id)=>Ready&&installation.PresentationLicenses.Contains(id);
        public bool CanPlacePresentation(string id)=>PresentationExperiment&&HasPresentationLicense(id)&&SignalPresentationPacks.Find(id)!=null;
        public string PresentationCommand(string command) {
            try {
                command=(command??"status").Trim();
                if(command=="status")return PresentationStatus;
                RequireHost();if(!Ready)throw new InvalidOperationException(LocalText.Select("Load a world first.","Zuerst eine Welt laden."));
                if(command=="enable"){PresentationExperiment=true;return PresentationStatus;}
                if(command=="disable"){PresentationExperiment=false;return PresentationStatus;}
                if(command.StartsWith("unlock:",StringComparison.Ordinal)) {
                    string id=command.Substring(7);
                    if(!PresentationExperiment||SignalPresentationPacks.Find(id)==null)throw new InvalidOperationException(LocalText.Select("Enable the experiment and install the compatible pack first.","Zuerst Experiment einschalten und kompatibles Paket installieren."));
                    RequireConstruction();
                    if(!HasPresentationLicense(id)){var next=installation.Clone();next.PresentationLicenses.Add(id);CommitInstall(next);}
                    return LocalText.Select("World test license granted (no purchase or payment): ","Welt-Testlizenz erteilt (kein Kauf und keine Zahlung): ")+id;
                }
                return LocalText.Select("Use: status, enable, disable, unlock:<pack-id>. Experimental test licenses are free; no asset downloads.","Verwendung: status, enable, disable, unlock:<Paket-ID>. Experimentelle Testlizenzen sind kostenlos; keine Asset-Downloads.");
            }catch(Exception e){return LocalText.Select("Presentation pack: ","Darstellungspaket: ")+e.Message;}
        }
        public string PresentationStatus=>LocalText.Select("Experimental presentation packs: ","Experimentelle Darstellungspakete: ")+(PresentationExperiment?LocalText.Select("ON (this session)","AN (diese Sitzung)"):LocalText.Select("OFF","AUS"))+" · "+string.Join(", ",SignalPresentationPacks.Installed.Select(p=>p.Name+" "+p.Version+(HasPresentationLicense(p.Id)?" ✓":" —")))+(SignalPresentationPacks.LastError==null?"":" · "+SignalPresentationPacks.LastError);
    }
}
