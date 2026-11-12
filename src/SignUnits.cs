using System;
using System.IO;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace EnusDE.Signaltechnik {
    // Canonical storage is mph. Unit selection affects rendering and explicit input only.
    public static class SignUnits {
        public const double MphToKmh=1.609344;
        public static string PlayerId(string key)=>key!=null&&key.StartsWith("player:",StringComparison.Ordinal)?key.Substring(7):key;
        public static string Mode(JObject settings) {
            string language=(string)settings["language"]??"";
            if(string.Equals(language,"en",StringComparison.OrdinalIgnoreCase)||language.StartsWith("en-",StringComparison.OrdinalIgnoreCase))return "original";
            string value=((string)settings["speedUnits"]??"").Trim().ToLowerInvariant();
            if(value=="")value=(bool?)settings["convertExplicitUnitDisplays"]==false?"original":(bool?)settings["showOriginalUnits"]!=false?"mixed":"metric";
            if(value!="metric"&&value!="original"&&value!="mixed")throw new InvalidDataException("Unbekannte Geschwindigkeitseinheit im Übersetzungsmod.");
            return value;
        }
        public static bool ValidSpeed(double mph)=>!double.IsNaN(mph)&&!double.IsInfinity(mph)&&mph>0&&mph<=200;
        public static double Input(string text,bool kmh) {
            double value;if(!double.TryParse((text??"").Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out value))throw new InvalidDataException("Geschwindigkeit als positive Zahl eingeben.");
            double mph=kmh?value/MphToKmh:value;
            if(!ValidSpeed(mph))throw new InvalidDataException("Geschwindigkeit muss größer als 0 und höchstens 200 mph / 321,86 km/h sein.");
            return mph;
        }
        public static string Number(double value)=>value.ToString("0.##",CultureInfo.InvariantCulture);
        public static string Caption(double mph,string mode) {
            if(!ValidSpeed(mph))throw new InvalidDataException("Ungültige Schildgeschwindigkeit.");
            // World signs have one large number. Mixed UI preferences use metric here;
            // the canonical mph value remains untouched, including on repeated toggles.
            return Math.Round(mph*(mode=="metric"||mode=="mixed"?MphToKmh:1),MidpointRounding.AwayFromZero).ToString("0",CultureInfo.InvariantCulture);
        }
        public static string Description(double mph,string mode)=>Caption(mph,mode)+(mode=="metric"||mode=="mixed"?" km/h":" mph");
    }
    public sealed class SignUnitPreference {
        readonly string path;
        DateTime stamp=DateTime.MinValue;
        long length=-1;
        bool seen;
        public string Mode {get;private set;}="original";
        public string Status {get;private set;}="Ohne Übersetzungseinstellung: mph.";
        public SignUnitPreference(string path){this.path=path;}
        // Caller polls at most once per two seconds; unchanged files are not read again.
        public bool Refresh() {
            string before=Mode;
            try {
                var file=new FileInfo(path);
                if(!file.Exists){seen=false;Mode="original";Status="Keine Übersetzungseinstellung gefunden: mph.";return before!=Mode;}
                if(seen&&stamp==file.LastWriteTimeUtc&&length==file.Length)return false;
                seen=true;stamp=file.LastWriteTimeUtc;length=file.Length;
                if(length>65536)throw new InvalidDataException("Einstellungsdatei zu groß.");
                Mode=SignUnits.Mode(JObject.Parse(File.ReadAllText(path)));
                Status="Übersetzungsmod: "+(Mode=="metric"?"km/h":Mode=="mixed"?"km/h + mph":"mph")+" (nur lesend).";
            }catch(Exception e){Mode="original";Status="Einheit nicht lesbar; mph: "+e.Message;}
            return before!=Mode;
        }
    }
}
