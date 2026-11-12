using System;

using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class DiagnosticsReport {
    public sealed class Summary {
        public int analyzer_version;
        public string session,start,role,map,fingerprint;
        public long records,vehicle_samples,loco_samples,signal_changes,block_changes,switch_changes,collector_errors,dropped,invalid;
        public long sign_changes,sign_handlers,removed_objects,suppressed;
        public long repeated_infrastructure,memory_samples;
        public long window_builds_peak;
        public long local_marker_events;
        public List<string> local_markers=new List<string>();
        public long own_signal_events,ai_signal_reads,ai_stop_targets;
        public List<string> ai_stop_history=new List<string>();
        public List<string> own_signal_history=new List<string>();
        public string signal_bridge_status="Nicht aufgezeichnet";
        public long? managed_first,managed_last,managed_peak,private_first,private_last,private_peak;
        public string mod_version;
        public JObject latest_configuration;
        public List<string> configuration_events=new List<string>();
        public Dictionary<string,string> category_status=new Dictionary<string,string>();
        public Dictionary<string,long> observed=new Dictionary<string,long>();
        public List<string> sign_events=new List<string>();
        public bool ended;
        public double mean_fps,worst_frame_ms;
        public long frames,slow_frames;
        public HashSet<string> cars=new HashSet<string>(),locos=new HashSet<string>();
        public Dictionary<string,int> messages=new Dictionary<string,int>();
        public List<string> signal_events=new List<string>(),speed_drops=new List<string>();
    }
    static double Number(JToken token){double value;string text=token==null?"":(token.Type==JTokenType.Float||token.Type==JTokenType.Integer?token.ToString(Formatting.None):token.ToString());return double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&!double.IsInfinity(value)&&!double.IsNaN(value)?value:0;}
    static string Text(JToken token)=>token==null?"":token.ToString();
    static void Message(Summary summary,string text,int amount=1){if(text.Length>400)text=text.Substring(0,400);int count;summary.messages.TryGetValue(text,out count);summary.messages[text]=count+amount;}
    static void Observed(Summary s,string category,long count=1){long value;s.observed.TryGetValue(category,out value);s.observed[category]=value+count;string status;if(s.category_status.TryGetValue(category,out status)&&status.StartsWith("Aktiv"))s.category_status[category]="Erfasst";}
    static void Configuration(Summary s,JToken config,string utc) {
        if(!(config is JObject value))return;
        s.latest_configuration=(JObject)value.DeepClone();
        if(s.configuration_events.Count<100)s.configuration_events.Add(utc+" · "+value.ToString(Formatting.None));
        var groups=value["Groups"] as JObject;
        if(groups!=null)foreach(var group in groups.Properties())s.category_status[group.Name]=(bool?)value["Recording"]==false||(bool?)group.Value["Enabled"]==false?"Erfassung aus":"Aktiv, noch keine Beobachtung";
    }
    public static Summary ReadSession(string directory) {
        var result=new Summary{analyzer_version=9,session=Path.GetFileName(directory)};
        string metaFile=Path.Combine(directory,"session.json");
        try{var meta=JObject.Parse(File.ReadAllText(metaFile));result.mod_version=Text(meta["version"]);result.start=Text(meta["start_utc"]);result.role=Text(meta["role"]);result.map=Text(meta["map"]);Configuration(result,meta["configuration"],result.start);}catch(Exception e){Message(result,"Metadaten: "+e.Message);}
        var lastSpeed=new Dictionary<string,Tuple<double,double>>();
        var lastInfrastructure=new Dictionary<string,string>();
        double fpsSeconds=0;
        foreach(string file in Directory.GetFiles(directory,"events-*.jsonl").OrderBy(x=>x,StringComparer.Ordinal)) {
            using(var reader=new StreamReader(new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite),Encoding.UTF8)) {
                string line;
                while((line=reader.ReadLine())!=null) {
                    if(string.IsNullOrWhiteSpace(line))continue;
                    JObject record;
                    try {record=JObject.Parse(line);}catch(JsonException){result.invalid++;continue;}
                    result.records++;
                    string kind=Text(record["kind"]);
                    var data=record["data"];if(data==null)continue;
                    if(kind=="vehicles") {
                        var rows=data["cars"] as JArray;if(rows==null)continue;
                        Observed(result,"vehicles",rows.Count);
                        bool full=data["full"]!=null&&(bool)data["full"];
                        foreach(var car in rows) {
                            if(car["error"]!=null){Message(result,"Fahrzeug "+Text(car["id"])+": "+Text(car["error"]));continue;}
                            string id=Text(car["id"]);result.cars.Add(id);result.vehicle_samples++;
                            if(car["locomotive"]==null || !(bool)car["locomotive"])continue;
                            result.locos.Add(id);result.loco_samples++;
                            double now=Number(record["real_seconds"]),speed=Number(car["kmh"]);
                            Tuple<double,double> previous;
                            if(lastSpeed.TryGetValue(id,out previous)) {
                                double elapsed=now-previous.Item1,drop=previous.Item2-speed;
                                if(elapsed>=.25&&elapsed<=3&&drop>=8 && result.speed_drops.Count<100)
                                    result.speed_drops.Add(Text(record["utc"])+" · "+id+": "+previous.Item2.ToString("0.0",CultureInfo.InvariantCulture)+" → "+speed.ToString("0.0",CultureInfo.InvariantCulture)+" km/h in "+elapsed.ToString("0.0",CultureInfo.InvariantCulture)+" s");
                            }
                            lastSpeed[id]=Tuple.Create(now,speed);
                        }
                    } else if(kind=="infrastructure") {
                        var currentState=data["state"];string observedKind=Text(currentState?["kind"]);
                        Observed(result,observedKind=="signal"?"signals":observedKind=="block"||observedKind=="interlocking"?"blocks":observedKind=="switch"?"switches":observedKind=="sign"?"signs":observedKind);
                        var state=data["state"];string category=Text(state?["kind"]);
                        if(state?["error"]!=null){Message(result,"Infrastruktur: "+Text(state["error"]));continue;}
                        string identity=category+":"+Text(state?["id"])+(category=="signal"?":"+Text(state?["direction"]):"");
                        string encoded=state?.ToString(Formatting.None)??"",previousState;
                        bool existed=lastInfrastructure.TryGetValue(identity,out previousState);
                        lastInfrastructure[identity]=encoded;
                        if(data["snapshot"]!=null&&(bool)data["snapshot"])continue;
                        if(!existed)continue;
                        if(encoded==previousState){result.repeated_infrastructure++;continue;}
                        if(category=="signal"){result.signal_changes++;if(result.signal_events.Count>=40)result.signal_events.RemoveAt(0);result.signal_events.Add(Text(record["utc"])+" · "+Text(state["id"])+" → "+Text(state["aspect"]));}
                        if(category=="block")result.block_changes++;
                        if(category=="switch")result.switch_changes++;
                        if(category=="sign")result.sign_changes++;
                    } else if(kind=="performance") {
                        Observed(result,"performance");
                        long count=(long)Number(data["frames"]);double fps=Number(data["mean_fps"]);
                        result.frames+=count;if(fps>0)fpsSeconds+=count/fps;
                        result.worst_frame_ms=Math.Max(result.worst_frame_ms,Number(data["worst_frame_ms"]));
                        result.slow_frames+=(long)Number(data["frames_over_100ms"]);
                        long managed=(long)Number(data["managed_bytes"]),privateBytes=(long)Number(data["process_private_bytes"]);
                        if(managed>0){result.memory_samples++;result.managed_first??=managed;result.managed_last=managed;result.managed_peak=Math.Max(result.managed_peak??0,managed);}
                        if(privateBytes>0){result.private_first??=privateBytes;result.private_last=privateBytes;result.private_peak=Math.Max(result.private_peak??0,privateBytes);}
                        result.window_builds_peak=Math.Max(result.window_builds_peak,(long)Number(data["window_builds"]));
                    } else if(kind=="game_log"){Observed(result,"game_log");Message(result,Text(data["level"])+": "+Text(data["message"]),Math.Max(1,(int)Number(data["count"])));}
                    else if(kind=="configuration"){Configuration(result,data,Text(record["utc"]));lastSpeed.Clear();lastInfrastructure.Clear();}
                    else if(kind=="category_status")result.category_status[Text(data["category"])]=(bool?)data["available"]==true?"Verfügbar":"Nicht verfügbar: "+Text(data["reason"]);
                    else if(kind=="sign_handler") {
                        result.sign_handlers++;
                        if(result.sign_events.Count>=40)result.sign_events.RemoveAt(0);
                        result.sign_events.Add(Text(record["utc"])+" · "+Text(data["id"])+" · "+Text(data["handler"])+" (Verwaltungsrückruf, kein bestätigter Zug-Trigger)");
                    }
                    else if(kind=="object_removed")result.removed_objects++;
                    else if(kind=="local_marker"||kind=="local_signal_object") {
                        result.local_marker_events++;if(result.local_markers.Count>=40)result.local_markers.RemoveAt(0);
                        result.local_markers.Add(Text(record["utc"])+" · "+Text(data["name"])+" · "+Text(data["action"])+" (eigene lokale Platzierungsaktion)");
                    }
                    else if(kind=="signal_bridge_health")result.signal_bridge_status=(bool?)data["available"]==true?"Native Signalhooks bereit (Beachtung nach Konfiguration)":"Start fehlgeschlagen: "+Text(data["error"]);
                    else if(kind=="ai_stop_target") {
                        result.ai_stop_targets++;
                        if(result.ai_stop_history.Count>=20)result.ai_stop_history.RemoveAt(0);
                        string distance=data["distance_m"]==null||data["distance_m"].Type==JTokenType.Null?"unbekannte Entfernung":Number(data["distance_m"]).ToString("0.0",CultureInfo.InvariantCulture)+" m";
                        result.ai_stop_history.Add(Text(record["utc"])+" · "+Text(data["vehicle_id"])+" · "+Text(data["code"])+" ("+Text(data["signal_id"])+") · "+distance);
                    }
                    else if(kind=="local_signal_aspect"||kind=="route_set"||kind=="route_cancel"||kind=="route_state"||kind=="ai_signal_read"||kind=="panel_switch") {
                        result.own_signal_events++;if(kind=="ai_signal_read")result.ai_signal_reads++;
                        if(result.own_signal_history.Count>=40)result.own_signal_history.RemoveAt(0);
                        string release=data["released_switches"] is JArray points?" · freigegebene Weichen: "+string.Join(", ",points.Select(p=>Text(p)))+" · freie Abschnitte: "+((data["released_sections"] as JArray)?.Count??0)+" · Zugfahrzeuge: "+((data["train_cars"] as JArray)?.Count??0):"";
                        result.own_signal_history.Add(Text(record["utc"])+" · "+kind+" · "+Text(data["code"]??data["id"])+" · "+Text(data["aspect"])+Text(data["stage"])+" · "+Text(data["reason"])+release+(string.IsNullOrEmpty(Text(data["fault_reason"]))?"":" · erster Fehler: "+Text(data["fault_reason"]))+(string.IsNullOrEmpty(Text(data["release_status"]))?"":" · Hilfsauflösung: "+Text(data["release_status"]))+(data["point_states"] is JObject feedback?" · Weichenrückmeldung: "+string.Join(", ",feedback.Properties().Select(p=>p.Name+"="+Text(p.Value))):""));
                    }
                    else if(kind=="collector_error"){result.collector_errors++;Message(result,"Recorder: "+Text(data["message"]));}
                    else if(kind=="recorder_health"){result.dropped=Math.Max(result.dropped,(long)Number(data["dropped"]));result.suppressed=Math.Max(result.suppressed,(long)Number(data["suppressed"]));}
                    else if(kind=="session_end"){result.ended=true;result.dropped=Math.Max(result.dropped,(long)Number(data["dropped"]));result.suppressed=Math.Max(result.suppressed,(long)Number(data["suppressed"]));}
                }
            }
        }
        result.mean_fps=fpsSeconds>0?result.frames/fpsSeconds:0;
        return result;
    }
    static string Fingerprint(string directory)=>string.Join("|",Directory.GetFiles(directory,"*.json*").OrderBy(x=>x,StringComparer.Ordinal).Select(x=>{var f=new FileInfo(x);return f.Name+":"+f.Length+":"+f.LastWriteTimeUtc.Ticks;}));
    public static void Run(string root,string output) {
        Directory.CreateDirectory(output);
        string cachePath=Path.Combine(output,"analysis-cache.json");
        var summaries=new Dictionary<string,Summary>();
        if(File.Exists(cachePath))try{summaries=JsonConvert.DeserializeObject<Dictionary<string,Summary>>(File.ReadAllText(cachePath))??summaries;}catch(JsonException){}
        var found=new List<Summary>();int updated=0;
        if(Directory.Exists(root))foreach(string dir in Directory.GetDirectories(root).OrderBy(x=>x,StringComparer.Ordinal)) {
            if(!File.Exists(Path.Combine(dir,"session.json")))continue;
            string id=Path.GetFileName(dir),stamp=Fingerprint(dir);Summary summary;
            if(!summaries.TryGetValue(id,out summary)||summary.analyzer_version!=9||summary.fingerprint!=stamp){summary=ReadSession(dir);summary.fingerprint=stamp;summaries[id]=summary;updated++;}
            found.Add(summary);
        }
        File.WriteAllText(cachePath,JsonConvert.SerializeObject(summaries,Formatting.Indented),new UTF8Encoding(false));
        var md=new StringBuilder();
        md.AppendLine("# Signaltechnik – Diagnoseauswertung");
        md.AppendLine();
        md.AppendLine("Stand: "+TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow,"W. Europe Standard Time").ToString("dd.MM.yyyy HH:mm")+" Uhr (Berlin).");
        md.AppendLine();
        md.AppendLine("Automatisch gefunden: "+found.Count+" Sitzungen; neu eingelesen/aktualisiert: "+updated+". Quelle: "+root);
        md.AppendLine();
        if(found.Count==0)md.AppendLine("Noch keine Recorderdaten gefunden. Version 0.1.2 muss nach dem Update einmal in einer geladenen Spielwelt laufen.");
        else {
            md.AppendLine("| Sitzung (UTC) | Rolle | Loks / Fahrzeuge | Datensätze | Signalwechsel | Blockwechsel | Abschluss |");
            md.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | --- |");
            foreach(var s in found.OrderByDescending(s=>s.start).Take(30))md.AppendLine("| "+s.start+" | "+s.role+" | "+s.locos.Count+" / "+s.cars.Count+" | "+s.records+" | "+s.signal_changes+" | "+s.block_changes+" | "+(s.ended?"sauber beendet":"läuft oder ohne Abschluss")+" |");
            foreach(var s in found.OrderByDescending(s=>s.start).Take(5)) {
                md.AppendLine();md.AppendLine("## "+s.session);md.AppendLine();
                md.AppendLine("Karte: "+s.map+". "+(s.frames>0?"Mittelwert "+s.mean_fps.ToString("0.0",CultureInfo.InvariantCulture)+" FPS; längster Frame "+s.worst_frame_ms.ToString("0.0",CultureInfo.InvariantCulture)+" ms; Frames ab 100 ms: "+s.slow_frames+".":"Keine Leistungswerte erfasst; kein Null-FPS-Befund."));
                md.AppendLine();
                md.AppendLine("Recorderfehler: "+s.collector_errors+"; gemeldete verworfene Einträge: "+s.dropped+"; durch Erfassungsgrenzen ausgelassen: "+s.suppressed+"; unlesbare/noch unvollständige Zeilen: "+s.invalid+".");
                md.AppendLine();md.AppendLine("Unverändert wiederholte Infrastrukturmeldungen (keine Zustandswechsel): "+s.repeated_infrastructure+". In 0.1.3 können gleichnamige Blöcke/Stellwerke solche Wiederholungen erzeugen; die Auswertung vergleicht ihre Zustände getrennt.");
                md.AppendLine();
                if(s.memory_samples==0)md.AppendLine("Keine RAM-Messwerte in dieser Sitzung. Ein Speicheranstieg und dessen Ursache lassen sich daraus nicht belegen.");
                else {
                    md.AppendLine("Verwalteter Speicher: Start "+MiB(s.managed_first)+", zuletzt "+MiB(s.managed_last)+", Spitze "+MiB(s.managed_peak)+".");
                    md.AppendLine("Privater Speicher des gesamten Spielprozesses: Start "+MiB(s.private_first)+", zuletzt "+MiB(s.private_last)+", Spitze "+MiB(s.private_peak)+". Dies umfasst auch das Spiel und andere Mods und beweist keine einzelne Ursache.");
                    md.AppendLine("Höchster gemessener Zähler der Diagnose-Fensteraufbauten: "+s.window_builds_peak+". Messwertaktualisierungen sollen diesen Zähler nicht erhöhen.");
                }
                md.AppendLine();md.AppendLine("### Erfassungsumfang");md.AppendLine();
                if(s.latest_configuration==null)md.AppendLine("Ältere Sitzung ohne wählbare Erfassungskonfiguration. Fehlende Daten beweisen keine fehlende Spielreaktion.");
                foreach(var item in s.category_status) {
                    long observed;s.observed.TryGetValue(item.Key,out observed);
                    md.AppendLine("- "+item.Key+": "+item.Value+"; beobachtete Einträge: "+observed+".");
                }
                if(s.latest_configuration!=null) {
                    md.AppendLine();md.AppendLine("Letzte Auswahl einschließlich Filter und Intervalle:");md.AppendLine();
                    md.AppendLine("```json");md.AppendLine(s.latest_configuration.ToString(Formatting.Indented));md.AppendLine("```");
                    md.AppendLine();md.AppendLine("Konfigurationsmarkierungen: "+s.configuration_events.Count+" (mit UTC-Zeit in der JSON-Auswertung). Nicht erfasste Bereiche wurden nicht auf ausbleibende Reaktion geprüft.");
                }
                md.AppendLine();md.AppendLine("Schilder-Zustandswechsel: "+s.sign_changes+"; Verwaltungsrückrufe: "+s.sign_handlers+"; per Bestandsvergleich entfernte Objekte: "+s.removed_objects+". Verwaltungsrückrufe sind keine bestätigten Zug-/Schildauslöser.");
                foreach(string signEvent in s.sign_events.TakeLast(10))md.AppendLine("- "+signEvent);
                md.AppendLine();md.AppendLine("Eigene Objektaktionen: "+s.local_marker_events+". Schilder/Markierungen benötigen aktive Schilderdiagnose, eigene Signale aktive Signaldiagnose; keine Zugauslöser.");
                foreach(string placement in s.local_markers.TakeLast(10))md.AppendLine("- "+placement);
                if(s.ai_stop_targets>0) {
                    md.AppendLine();
                    md.AppendLine("An die native KI übergebene Signal-Halteziele: "+s.ai_stop_targets+". Die Zieleinträge enthalten keine Bestätigung einer Bremswirkung.");
                    foreach(string row in s.ai_stop_history)md.AppendLine("- "+row);
                }
                md.AppendLine();md.AppendLine("Eigene Signal-/Stellwerksereignisse: "+s.own_signal_events+"; beobachtete native AI-Signalaufrufe: "+s.ai_signal_reads+". Ein AI-Aufruf beweist keine erfolgreiche Bremsung, Passage oder regelkonforme Wirkung.");
                md.AppendLine("Start der nativen Signalanbindung: "+s.signal_bridge_status+". Dies ist kein Nachweis einer erfolgreichen Zugreaktion.");
                foreach(string change in s.own_signal_history.TakeLast(10))md.AppendLine("- "+change);
                md.AppendLine();md.AppendLine("### Auffällige Spielmeldungen");md.AppendLine();
                if(s.messages.Count==0)md.AppendLine("Keine passenden Meldungen aufgezeichnet.");
                foreach(var m in s.messages.OrderByDescending(x=>x.Value).Take(12))md.AppendLine("- "+m.Value+"× "+m.Key.Replace("\n"," ").Replace("\r"," "));
                md.AppendLine();md.AppendLine("### Deutliche Geschwindigkeitsabnahmen");md.AppendLine();
                if(s.speed_drops.Count==0)md.AppendLine("Keine Abnahme um mindestens 8 km/h innerhalb von höchstens 3 Sekunden festgestellt.");
                foreach(string item in s.speed_drops.Take(20))md.AppendLine("- "+item);
                md.AppendLine();md.AppendLine("### Letzte Signaländerungen");md.AppendLine();
                foreach(string item in s.signal_events.TakeLast(20))md.AppendLine("- "+item);
            }
        }
        md.AppendLine();md.AppendLine("Grenzen: Erfasst wird die Sicht dieses Rechners. Reine Clientdaten zeigen keine verborgenen Serverentscheidungen oder fremden Rechnerlogs. Sekundenabtastung kann sehr kurze Änderungen verpassen. Zeitliche Zusammenhänge sind keine bewiesene Ursache; ein Regelverstoß oder eine KI-Fehlfunktion wird daraus nicht automatisch behauptet. Signal-Gleiszuordnungen sind als Näherung gekennzeichnet.");
        File.WriteAllText(Path.Combine(output,"Signaltechnik-Diagnose.md"),md.ToString(),new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(output,"Signaltechnik-Diagnose.json"),JsonConvert.SerializeObject(found,Formatting.Indented),new UTF8Encoding(false));
        System.Console.WriteLine("Sitzungen: "+found.Count+"; aktualisiert: "+updated);
        System.Console.WriteLine(Path.Combine(output,"Signaltechnik-Diagnose.md"));
    }
    static string MiB(long? bytes)=>bytes.HasValue?(bytes.Value/1048576d).ToString("0.0",CultureInfo.InvariantCulture)+" MiB":"nicht verfügbar";
}
