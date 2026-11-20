using System;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    // Built-in packs first, optional translation-provider fallback second, English last.
    public static partial class LocalText {
        static Func<string> preference=()=>"auto";
        public static string AutomaticLanguage="en";
        public static Func<string,string,string> ExternalTranslate;
        public static void Configure(Func<string> read){preference=read??(()=>"auto");}
        public static string NormalizeLanguage(string language){language=(language??"en").Trim().ToLowerInvariant();if(language=="de"||language.StartsWith("de-",StringComparison.Ordinal))return "de";if(language=="en"||language.StartsWith("en-",StringComparison.Ordinal))return "en";return language.Length>0&&language.Length<32?language:"en";}
        public static string Language {get {string chosen=preference();return chosen=="auto"?NormalizeLanguage(AutomaticLanguage):chosen=="de"?"de":"en";}}
        public static bool German=>Language=="de";
        public static string Select(string english,string german) {
            if(German)return german;if(Language=="en")return english;return Translate(english)??english;
        }
        static string Translate(string english){try{var translated=ExternalTranslate?.Invoke(english,Language);return string.IsNullOrEmpty(translated)||translated==english?null:translated;}catch{return null;}}
        public static string Get(string german) {
            if(german==null)return null;if(German)return german;
            string english=English.TryGetValue(german,out var own)?own:german;
            if(Language=="en")return english;
            return Translate(english)??english;
        }
        static readonly Dictionary<string,string> English=BuiltInLanguage.English();
    }
}
