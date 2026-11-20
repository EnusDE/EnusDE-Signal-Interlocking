using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

static class LocalizationTests {
    const string Literal = "\"(?:[^\"\\\\]|\\\\.)*\"";
    static readonly Regex Pairs=new Regex(@"(?:WindowText|LocalText\.Select)\(\s*("+Literal+@")\s*,\s*("+Literal+@")",RegexOptions.Singleline);
    static readonly Regex Keys=new Regex(@"LocalText\.Get\(\s*("+Literal+@")",RegexOptions.Singleline);
    static readonly Regex RawUi=new Regex(@"\b(?:AddLabel|AddTitle|AddButton|AddSection|AddField|AddToggle|Tooltip)\(\s*("+Literal+@")",RegexOptions.Singleline);
    static readonly HashSet<string> Neutral=new HashSet<string>(StringComparer.Ordinal){"X","Y","EnusDE Signal & Interlocking"};
    static string Read(string literal)=>JsonConvert.DeserializeObject<string>(literal);
    static bool Has(JObject pack,string key)=>pack[key]?.Type==JTokenType.String&&!string.IsNullOrWhiteSpace((string)pack[key]);
    static string[] Errors(string source,JObject en,JObject de) {
        var errors=new List<string>();
        foreach(Match match in Pairs.Matches(source)) {
            string english=Read(match.Groups[1].Value),german=Read(match.Groups[2].Value);
            if(string.IsNullOrWhiteSpace(english)||string.IsNullOrWhiteSpace(german)||!Has(en,german)||!Has(de,german))errors.Add("Bilingual pair missing from language packs: "+german);
        }
        foreach(Match match in Keys.Matches(source)) {
            string key=Read(match.Groups[1].Value);
            if(!Neutral.Contains(key)&&(!Has(en,key)||!Has(de,key)))errors.Add("Missing language key: "+key);
        }
        foreach(Match match in RawUi.Matches(source)) {
            string value=Read(match.Groups[1].Value);
            if(!Neutral.Contains(value)&&!string.IsNullOrWhiteSpace(value))errors.Add("Unlocalized UI literal: "+value);
        }
        return errors.ToArray();
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    public static void Run(string root) {
        var directory=Path.Combine(root,"mod","EnusDE-Signaltechnik","Languages");
        var en=JObject.Parse(File.ReadAllText(Path.Combine(directory,"en.json")));var de=JObject.Parse(File.ReadAllText(Path.Combine(directory,"de.json")));
        Check(en.Properties().Select(p=>p.Name).OrderBy(k=>k).SequenceEqual(de.Properties().Select(p=>p.Name).OrderBy(k=>k))&&
            en.Properties().All(p=>Has(en,p.Name)&&Has(de,p.Name)),"both interface language packs have matching nonempty string entries");
        var problems=Directory.GetFiles(Path.Combine(root,"src"),"*.cs").Where(p=>Path.GetFileName(p)!="BuiltInLanguage.cs")
            .SelectMany(p=>Errors(File.ReadAllText(p),en,de).Select(error=>Path.GetFileName(p)+": "+error)).ToArray();
        Check(problems.Length==0,"literal bilingual UI pairs and translation keys are present in both language packs"+(problems.Length==0?"":"\n"+string.Join("\n",problems)));
        var sampleEn=JObject.Parse("{\"Neu\":\"New\"}");var sampleDe=JObject.Parse("{\"Neu\":\"Neu\"}");
        Check(Errors("c.AddButton(LocalText.Select(\"New\",\"Neu\"), action);",sampleEn,sampleDe).Length==0,
            "localization gate accepts an interface label supplied in both languages");
        Check(Errors("c.AddButton(\"New untranslated button\", action);",sampleEn,sampleDe).Length==1&&
            Errors("LocalText.Get(\"Fehlt\")",sampleEn,sampleDe).Length==1&&
            Errors("WindowText(\"Missing\",\"Fehlt\")",sampleEn,sampleDe).Length==1,
            "localization gate rejects new raw UI labels, missing keys and unpacked bilingual pairs");
        sampleDe["Neu"]="";
        Check(Errors("LocalText.Select(\"New\",\"Neu\")",sampleEn,sampleDe).Length==1,
            "an empty German translation cannot satisfy the bilingual-text requirement");
    }
}
