using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public enum ConstructionUnlockMode { MapDefault=0,Immediate=1,Purchase=2 }
    public sealed class ConstructionMapRule {
        public const string FileName="EnusDE-Signaltechnik.progression.json";
        public int Schema=1;
        public string Mode="auto",Prerequisite="";
        public void Validate() {
            if(Schema!=1||(Mode!="auto"&&Mode!="free"&&Mode!="purchase")||Prerequisite==null||Prerequisite.Length>200||Prerequisite.Any(char.IsControl)||Mode=="auto"&&Prerequisite.Length>0)
                throw new InvalidDataException(LocalText.Select("Invalid map progression rule. Use schema 1 and mode auto, free or purchase; auto has no explicit prerequisite.","Ungültige Karten-Freischaltung. Schema 1 und Modus auto, free oder purchase verwenden; auto hat keine explizite Voraussetzung."));
        }
        public static ConstructionMapRule Read(string mapDataPath) {
            if(string.IsNullOrWhiteSpace(mapDataPath))return null;
            string path=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(mapDataPath)),FileName);
            if(!File.Exists(path))return null;
            if(new FileInfo(path).Length>16384)throw new InvalidDataException(LocalText.Select("Map progression rule exceeds 16 KiB.","Karten-Freischaltung überschreitet 16 KiB."));
            var rule=JsonConvert.DeserializeObject<ConstructionMapRule>(File.ReadAllText(path),new JsonSerializerSettings{MissingMemberHandling=MissingMemberHandling.Error});
            if(rule==null)throw new InvalidDataException(LocalText.Select("Map progression rule is empty.","Karten-Freischaltung ist leer."));
            rule.Validate();return rule;
        }
    }
    public sealed class ConstructionMilestoneInfo {
        public string Id;
        public bool Completed;
        public string[] Prerequisites=new string[0];
    }
    public sealed class ConstructionUnlockPlan {
        public bool Free,RequirementsMet,PreviouslyUnlocked;
        public int Cost;
        public string Source,Error;
        public string[] Prerequisites=new string[0],Missing=new string[0];
        public bool CanStart=>PreviouslyUnlocked||Error==null&&RequirementsMet;
        public bool AutoUnlock=>PreviouslyUnlocked||Error==null&&Free&&RequirementsMet;
        public bool ChargeRequired=>!PreviouslyUnlocked&&!Free&&Error==null&&RequirementsMet;
    }
    public static class ConstructionProgressionPolicy {
        // Native Signals: Whittier to Bryson. Share its prerequisites, never require
        // purchasing CTC itself. Capability detection also works with map extensions.
        public const string NativeSignals="sctc";
        public static ConstructionUnlockPlan Resolve(ConstructionUnlockMode mode,string prerequisite,ConstructionMapRule rule,
            IEnumerable<ConstructionMilestoneInfo> milestones,bool previouslyUnlocked=false,bool sandbox=false,string ruleError=null) {
            var available=(milestones??new ConstructionMilestoneInfo[0]).Where(m=>m!=null&&!string.IsNullOrEmpty(m.Id)).ToArray();
            var native=available.FirstOrDefault(m=>m.Id==NativeSignals);
            var plan=new ConstructionUnlockPlan{PreviouslyUnlocked=previouslyUnlocked||sandbox,Cost=ConstructionPolicy.UnlockCost};
            if(!Enum.IsDefined(typeof(ConstructionUnlockMode),mode)||prerequisite==null)throw new ArgumentException("Invalid construction unlock override.");
            if(mode==ConstructionUnlockMode.Immediate){plan.Free=true;plan.Source="host-free";}
            else if(mode==ConstructionUnlockMode.Purchase||!string.IsNullOrEmpty(prerequisite)) {
                plan.Source="host-purchase";plan.Prerequisites=string.IsNullOrEmpty(prerequisite)?new string[0]:new[]{prerequisite};
            } else if(ruleError!=null){plan.Error=ruleError;plan.Source="map-error";}
            else {
                rule?.Validate();
                if(rule!=null&&rule.Mode!="auto") {
                    plan.Free=rule.Mode=="free";plan.Source="map-rule";
                    plan.Prerequisites=string.IsNullOrEmpty(rule.Prerequisite)?new string[0]:new[]{rule.Prerequisite};
                } else if(native!=null) {
                    plan.Source="native-signals";
                    plan.Prerequisites=(native.Prerequisites??new string[0]).Where(id=>!string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal).ToArray();
                } else {plan.Free=true;plan.Source="custom-map-free";}
            }
            if(sandbox)plan.Free=true;
            if(plan.Free)plan.Cost=0;
            plan.Missing=plan.Prerequisites.Where(id=>!available.Any(m=>m.Id==id&&m.Completed)).ToArray();
            plan.RequirementsMet=plan.Error==null&&plan.Missing.Length==0;
            return plan;
        }
    }
}
