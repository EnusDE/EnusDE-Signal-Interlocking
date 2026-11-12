using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public sealed class CategoryOptions {
        public bool Enabled=true,Visible=true;
        public double IntervalSeconds=2;
        public string Ids="";
    }
    public sealed class DiagnosticSettings {
        public static readonly string[] Categories={"vehicles","signals","blocks","switches","signs","game_log","performance"};
        public bool Recording=true,LocomotivesOnly=true,VehicleControls=false,ShowSpeed=true,ShowPositions=true,ShowRecorder=true;
        public bool RecordOnClients;
        public static readonly string[] SearchCloseKeys={"Backspace","PageUp","PageDown","Home","End","Insert","None"};
        public string SearchCloseKey="Backspace";
        public MarkerOptions Markers=new MarkerOptions();
        public Dictionary<string,CategoryOptions> Groups=Defaults();
        static Dictionary<string,CategoryOptions> Defaults() {
            var result=new Dictionary<string,CategoryOptions>();
            foreach(string key in Categories)result[key]=new CategoryOptions();
            result["signs"].Enabled=false;result["signs"].IntervalSeconds=5;
            result["game_log"].IntervalSeconds=5;result["performance"].IntervalSeconds=10;
            return result;
        }
        public CategoryOptions this[string key]=>Groups[key];
        public DiagnosticSettings Clone()=>JsonConvert.DeserializeObject<DiagnosticSettings>(JsonConvert.SerializeObject(this)).Normalize();
        public DiagnosticSettings Normalize() {
            if(!SearchCloseKeys.Contains(SearchCloseKey))SearchCloseKey="Backspace";
            Markers=(Markers??new MarkerOptions()).Normalize();
            var defaults=Defaults();
            foreach(string key in Categories) {
                CategoryOptions group;
                if(Groups==null || !Groups.TryGetValue(key,out group) || group==null)group=defaults[key];
                double interval=group.IntervalSeconds;
                if(double.IsNaN(interval)||double.IsInfinity(interval))interval=defaults[key].IntervalSeconds;
                group.IntervalSeconds=Math.Max(key=="performance"?5:1,Math.Min(60,interval));
                group.Ids=NormalizeIds(group.Ids);defaults[key]=group;
            }
            Groups=defaults;return this;
        }
        public static string NormalizeIds(string ids)=>string.Join(",",(ids??"").Split(new[]{',',';','\n','\r'},StringSplitOptions.RemoveEmptyEntries).Select(s=>s.Trim()).Where(s=>s.Length>0).Distinct(StringComparer.Ordinal).Take(64).Select(s=>s.Length>160?s.Substring(0,160):s));
    }
    public sealed class IdFilter {
        readonly HashSet<string> ids;
        public IdFilter(string text){ids=new HashSet<string>(DiagnosticSettings.NormalizeIds(text).Split(new[]{','},StringSplitOptions.RemoveEmptyEntries),StringComparer.Ordinal);}
        public bool Allows(string id)=>ids.Count==0 || (id!=null && ids.Contains(id));
    }
    public sealed class SettingsStore {
        public readonly string FilePath;
        public string Error {get;private set;}
        public SettingsStore(string path){FilePath=Path.GetFullPath(path);}
        public DiagnosticSettings Load() {
            Error=null;
            if(!File.Exists(FilePath))return new DiagnosticSettings();
            try{return (JsonConvert.DeserializeObject<DiagnosticSettings>(File.ReadAllText(FilePath))??throw new JsonException("Leere Einstellungen.")).Normalize();}
            catch(Exception e){Error="Einstellungen nicht geladen: "+e.Message;return new DiagnosticSettings{Recording=false};}
        }
        public void Save(DiagnosticSettings value) {
            var checkedValue=value.Clone();
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temporary=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {
                File.WriteAllText(temporary,JsonConvert.SerializeObject(checkedValue,Formatting.Indented),new UTF8Encoding(false));
                if(File.Exists(FilePath))File.Replace(temporary,FilePath,null);else File.Move(temporary,FilePath);
                Error=null;
            }catch(Exception e){Error="Einstellungen nicht gespeichert: "+e.Message;throw;}
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
