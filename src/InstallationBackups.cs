using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    // Configuration only: no route runs, aspects, occupancy samples, leases or automatic requests.
    public sealed class InstallationBackupData {
        public InstallationDocument Installation;
        public DeskLibrary Desks;
        public InterlockingDocument Routes;
        public string WorldKey=>Installation?.WorldKey;
        public void Validate() {
            if(Installation==null||Desks==null||Routes==null)throw new InvalidDataException("Anlagensicherung unvollständig.");
            Installation.Validate();Desks.Validate(WorldKey);
            if(Routes.Schema!=1||Routes.WorldKey!=WorldKey||Routes.Routes==null||Routes.Routes.Count>128||Routes.Routes.Any(r=>r==null))throw new InvalidDataException("Fahrstraßensicherung ungültig.");
            foreach(var route in Routes.Routes)route.Validate();
            if(Routes.Routes.Select(r=>r.Id).Distinct().Count()!=Routes.Routes.Count||Routes.Routes.Select(r=>r.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Routes.Routes.Count)throw new InvalidDataException("Doppelte gesicherte Fahrstraße.");
        }
        public static InstallationBackupData Capture(InstallationDocument installation,DeskLibrary desks,IEnumerable<RouteDefinition> routes) {
            var result=new InstallationBackupData{Installation=installation.Clone(),Desks=desks.Clone(),Routes=new InterlockingDocument{WorldKey=installation.WorldKey,Routes=routes.Select(r=>r.Clone()).ToList()}};
            result.Installation.TrainNumbers.Clear();result.Installation.SignalNumberFields.Clear();result.Desks.Automation.Locomotives.Clear();result.Validate();return result;
        }
        public InstallationBackupData RestoreFor(string map,string world) {
            Validate();if(Installation.Map!=map||!InstallationBackups.ValidKey(world))throw new InvalidDataException("Sicherung gehört zu einer anderen Karte oder die Zielkennung ist ungültig.");
            var result=Capture(Installation,Desks,Routes.Routes);result.Installation.WorldKey=world;result.Installation.Objects.WorldKey=world;
            result.Desks.WorldKey=world;foreach(var desk in result.Desks.Desks)desk.Panel.WorldKey=world;result.Routes.WorldKey=world;
            result.Desks.Automation.Enabled=false;result.Validate();return result;
        }
        public HashSet<string> Resources() {
            var ids=new HashSet<string>(Installation.Objects.Markers.Select(m=>"object:"+m.Id));
            ids.UnionWith(SignalCatalogue.Expand(Installation.Objects.Markers,true).Select(m=>"face:"+m.Id));
            ids.UnionWith(Desks.Desks.SelectMany(d=>d.Panel.Tiles).Where(t=>t.NumberId!=null).Select(t=>"number-source:"+t.NumberId));
            ids.UnionWith(Desks.Desks.Select(d=>"desk:"+d.Id));ids.UnionWith(Desks.Blocks.Keys.Select(id=>"block:"+id));
            ids.UnionWith(Desks.FreeLines.Select(l=>"line:"+l.Id));
            ids.UnionWith(Desks.Automation.Contacts.Select(c=>"contact:"+c.Id));ids.UnionWith(Routes.Routes.Select(r=>"route:"+r.Id));
            ids.UnionWith(Desks.Desks.SelectMany(d=>d.Panel.Tiles).Where(t=>t.Target!=null).Select(t=>"target:"+t.Target.Id));return ids;
        }
    }
    public sealed class InstallationBackupEnvelope {
        public int Protocol=1;
        public string CreatedUtc,ModVersion,Reason,SourceKey,Hash,Payload;
    }
    public sealed class InstallationBackupWorld {
        public int Protocol=1;
        public string WorldKey,Map,Name,UpdatedUtc;
        public List<string> Sources=new List<string>();
    }
    public sealed class InstallationBackupSelection {
        public string WorldKey,FileName;
        public string Caption=>FileName=="latest.json.gz"?"Letzter extern gesicherter Stand":FileName.EndsWith("-keep.json.gz",StringComparison.Ordinal)?FileName+" · dauerhaft aufbewahrt":FileName;
    }
    public sealed class InstallationBackupRead {
        public InstallationBackupEnvelope Envelope;
        public InstallationBackupData Data;
    }
    public sealed class InstallationBackups {
        public const int PayloadLimit=24*1024*1024,EnvelopeLimit=52*1024*1024,AutomaticCopies=32;
        static readonly UTF8Encoding utf8=new UTF8Encoding(false,true);
        static readonly JsonSerializerSettings json=new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=128};
        readonly Func<DateTime> clock;
        public string DirectoryPath {get;}
        public InstallationBackups(string root,Func<DateTime> clock=null){DirectoryPath=Path.GetFullPath(root);this.clock=clock??(()=>DateTime.UtcNow);}
        public static bool ValidKey(string key)=>key!=null&&key.Length==64&&key.All(Uri.IsHexDigit);
        string Folder(string key){if(!ValidKey(key))throw new InvalidDataException("Ungültige Sicherungskennung.");return Path.Combine(DirectoryPath,key.ToLowerInvariant());}
        string PathFor(string key,string file) {
            if(file==null||file.Length>160||file!=Path.GetFileName(file)||file.Any(c=>c=='/'||c=='\\')||!file.EndsWith(".json.gz",StringComparison.Ordinal)||file.Contains(".."))throw new InvalidDataException("Ungültiger Sicherungsdateiname.");
            return Path.Combine(Folder(key),file);
        }
        public InstallationBackupRead Read(InstallationBackupSelection selection) {
            string path=PathFor(selection.WorldKey,selection.FileName);if(new FileInfo(path).Length>EnvelopeLimit)throw new InvalidDataException("Sicherungsdatei zu groß.");
            byte[] raw;
            using(var source=File.OpenRead(path))using(var gzip=new GZipStream(source,CompressionMode.Decompress))using(var output=new MemoryStream()) {
                var buffer=new byte[8192];int length;while((length=gzip.Read(buffer,0,buffer.Length))>0){if(output.Length+length>EnvelopeLimit)throw new InvalidDataException("Entpackte Sicherung zu groß.");output.Write(buffer,0,length);}raw=output.ToArray();
            }
            var envelope=JsonConvert.DeserializeObject<InstallationBackupEnvelope>(utf8.GetString(raw),json);
            if(envelope==null||envelope.Protocol!=1||!ValidKey(envelope.Hash)||!ValidKey(envelope.SourceKey)||envelope.Payload==null||envelope.ModVersion==null||envelope.ModVersion.Length>64||envelope.Reason==null||envelope.Reason.Length>128||
                !DateTime.TryParseExact(envelope.CreatedUtc,"O",CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var when)||when.Kind!=DateTimeKind.Utc)
                throw new InvalidDataException("Sicherungsformat ungültig oder unbekannt.");
            var payload=utf8.GetBytes(envelope.Payload);if(payload.Length>PayloadLimit||SharedDocuments.Hash(payload)!=envelope.Hash)throw new InvalidDataException("Sicherungs-Prüfsumme stimmt nicht.");
            var data=JsonConvert.DeserializeObject<InstallationBackupData>(envelope.Payload,json)??throw new InvalidDataException("Leere Sicherung.");data.Validate();
            if(!string.Equals(data.WorldKey,selection.WorldKey,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Sicherung wurde einer fremden Anlage zugeordnet.");
            return new InstallationBackupRead{Envelope=envelope,Data=data};
        }
        public InstallationBackupSelection Latest(string world)=>File.Exists(PathFor(world,"latest.json.gz"))?new InstallationBackupSelection{WorldKey=world,FileName="latest.json.gz"}:null;
        public InstallationBackupSelection[] Copies(string world) {
            string folder=Folder(world);if(!Directory.Exists(folder))return new InstallationBackupSelection[0];
            return Directory.EnumerateFiles(folder,"*.json.gz").Select(Path.GetFileName).Where(file=>file.EndsWith("-auto.json.gz",StringComparison.Ordinal)||file.EndsWith("-keep.json.gz",StringComparison.Ordinal))
                .OrderByDescending(file=>file,StringComparer.Ordinal).Select(file=>new InstallationBackupSelection{WorldKey=world,FileName=file}).ToArray();
        }
        public InstallationBackupWorld[] Worlds(string map) {
            if(!Directory.Exists(DirectoryPath))return new InstallationBackupWorld[0];var result=new List<InstallationBackupWorld>();
            foreach(string folder in Directory.EnumerateDirectories(DirectoryPath)) {
                string key=Path.GetFileName(folder);if(!ValidKey(key))continue;InstallationBackupWorld value=null;
                foreach(string file in new[]{Path.Combine(folder,"world.json"),Path.Combine(folder,"world.json.previous")})try {
                    if(!File.Exists(file)||new FileInfo(file).Length>16384)continue;var candidate=JsonConvert.DeserializeObject<InstallationBackupWorld>(File.ReadAllText(file,utf8),json);
                    if(candidate!=null&&candidate.Protocol==1&&string.Equals(candidate.WorldKey,key,StringComparison.OrdinalIgnoreCase)&&candidate.Map!=null&&candidate.Name!=null&&candidate.Name.Length<=128&&candidate.Sources!=null&&candidate.Sources.Count<=64&&candidate.Sources.All(ValidKey)){value=candidate;break;}
                }catch(IOException){}catch(JsonException){}catch(DecoderFallbackException){}
                if(value==null) {
                    try{var recovered=Previous(key);if(recovered!=null)value=new InstallationBackupWorld{WorldKey=key,Map=recovered.Data.Installation.Map,Name="Anlage "+key.Substring(0,10),UpdatedUtc=recovered.Envelope.CreatedUtc,Sources=new List<string>{recovered.Envelope.SourceKey}};}
                    catch(IOException){}catch(JsonException){}catch(InvalidDataException){}catch(DecoderFallbackException){}
                }
                if(value?.Map==map)result.Add(value);
            }
            return result.OrderByDescending(w=>w.UpdatedUtc,StringComparer.Ordinal).ToArray();
        }
        // Keep staging names independent of the snapshot name. Appending another GUID
        // reached Windows' 260-character path limit in the standard LocalLow folder.
        static string TemporaryFile(string path)=>Path.Combine(Path.GetDirectoryName(path),Guid.NewGuid().ToString("N")+".tmp");
        static void AtomicFile(string path,byte[] bytes) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=TemporaryFile(path);
            try{using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(path))File.Replace(temp,path,path+".previous");else File.Move(temp,path);
            }finally{if(File.Exists(temp))File.Delete(temp);}
        }
        static byte[] Encode(InstallationBackupEnvelope envelope) {
            byte[] raw=utf8.GetBytes(JsonConvert.SerializeObject(envelope,json));if(raw.Length>EnvelopeLimit)throw new InvalidDataException("Sicherung zu groß.");
            using(var output=new MemoryStream()){using(var zip=new GZipStream(output,CompressionLevel.Optimal,true))zip.Write(raw,0,raw.Length);return output.ToArray();}
        }
        InstallationBackupRead Previous(string world) {
            var latest=new InstallationBackupSelection{WorldKey=world,FileName="latest.json.gz"};
            if(File.Exists(PathFor(world,latest.FileName)))try{return Read(latest);}catch(InvalidDataException){}catch(JsonException){}catch(FormatException){}catch(DecoderFallbackException){}catch(IOException){}
            foreach(var copy in Copies(world))try{return Read(copy);}catch(InvalidDataException){}catch(JsonException){}catch(FormatException){}catch(DecoderFallbackException){}catch(IOException){}
            return null;
        }
        public void SaveJournal(string world,string source,string map,IDictionary<string,string> values) {
            if(!ValidKey(source)||string.IsNullOrWhiteSpace(map))throw new InvalidDataException("Sicherungsherkunft fehlt.");
            byte[] raw=utf8.GetBytes(JsonConvert.SerializeObject(new{Protocol=1,CreatedUtc=clock().ToUniversalTime().ToString("O",CultureInfo.InvariantCulture),SourceKey=source,Map=map,PreviousProperties=values},json));
            if(raw.Length>EnvelopeLimit)throw new InvalidDataException("Wiederherstellungsjournal zu groß.");
            byte[] bytes;using(var output=new MemoryStream()){using(var zip=new GZipStream(output,CompressionLevel.Optimal,true))zip.Write(raw,0,raw.Length);bytes=output.ToArray();}
            AtomicFile(Path.Combine(Folder(world),clock().ToUniversalTime().ToString("yyyyMMdd'T'HHmmssfffffff'Z'",CultureInfo.InvariantCulture)+"-"+Guid.NewGuid().ToString("N")+"-restore-journal.json.gz"),bytes);
        }
        public InstallationBackupSelection Save(InstallationBackupData data,string source,string name,string reason,bool keep=false,bool checkpoint=false) {
            data.Validate();if(!ValidKey(source)||name==null||name.Length>128||reason==null||reason.Length>128)throw new InvalidDataException("Sicherungsherkunft ungültig.");
            string payload=JsonConvert.SerializeObject(data,json);byte[] raw=utf8.GetBytes(payload);if(raw.Length>PayloadLimit)throw new InvalidDataException("Anlagenbestand für Sicherung zu groß.");
            string hash=SharedDocuments.Hash(raw),folder=Folder(data.WorldKey),stamp=clock().ToUniversalTime().ToString("yyyyMMdd'T'HHmmssfffffff'Z'",CultureInfo.InvariantCulture);
            var before=Previous(data.WorldKey);
            bool changed=before==null||before.Envelope.Hash!=hash;
            if(changed||keep) {
                Directory.CreateDirectory(folder);
                if(before!=null&&changed&&(checkpoint||before.Data.Resources().Except(data.Resources()).Any())) {
                    var archived=new InstallationBackupEnvelope{CreatedUtc=before.Envelope.CreatedUtc,SourceKey=before.Envelope.SourceKey,ModVersion=before.Envelope.ModVersion,Hash=before.Envelope.Hash,Payload=before.Envelope.Payload,
                        Reason=checkpoint?"Vor Laden / Wiederherstellung eines anderen Anlagenstands":"Vor Entfernen von Anlagenobjekten"};
                    AtomicFile(Path.Combine(folder,stamp+"-"+Guid.NewGuid().ToString("N")+"-keep.json.gz"),Encode(archived));
                }
                var envelope=new InstallationBackupEnvelope{CreatedUtc=clock().ToUniversalTime().ToString("O",CultureInfo.InvariantCulture),ModVersion=Entry.Version,SourceKey=source,Hash=hash,Payload=payload,Reason=reason};
                byte[] bytes=Encode(envelope);string file=stamp+"-"+Guid.NewGuid().ToString("N")+(keep?"-keep.json.gz":"-auto.json.gz");
                AtomicFile(Path.Combine(folder,file),bytes);
                string latest=Path.Combine(folder,"latest.json.gz");
                if(File.Exists(latest))try{Read(new InstallationBackupSelection{WorldKey=data.WorldKey,FileName="latest.json.gz"});}
                    catch(Exception e)when(e is InvalidDataException||e is JsonException||e is FormatException||e is DecoderFallbackException){File.Copy(latest,Path.Combine(folder,stamp+"-"+Guid.NewGuid().ToString("N")+"-unreadable.json.gz"));}
                AtomicFile(latest,bytes);
                var automatic=Copies(data.WorldKey).Where(c=>c.FileName.EndsWith("-auto.json.gz",StringComparison.Ordinal));
                var expired=keep?automatic.Skip(AutomaticCopies):automatic.Where(c=>c.FileName!=file).Skip(AutomaticCopies-1);
                foreach(var old in expired)File.Delete(PathFor(old.WorldKey,old.FileName));
            }
            var info=Worlds(data.Installation.Map).FirstOrDefault(w=>string.Equals(w.WorldKey,data.WorldKey,StringComparison.OrdinalIgnoreCase))??new InstallationBackupWorld{WorldKey=data.WorldKey,Map=data.Installation.Map};
            if(!info.Sources.Contains(source)){if(info.Sources.Count>=64)throw new InvalidDataException("Zu viele Spielstandzuordnungen für diese Anlage.");info.Sources.Add(source);}
            info.Name=name;info.UpdatedUtc=clock().ToUniversalTime().ToString("O",CultureInfo.InvariantCulture);
            string meta=Path.Combine(folder,"world.json");string content=JsonConvert.SerializeObject(info,json);
            bool writeInfo=changed||keep||!File.Exists(meta);
            if(!writeInfo)try{writeInfo=JsonConvert.DeserializeObject<InstallationBackupWorld>(File.ReadAllText(meta,utf8),json)?.Sources.Contains(source)!=true;}catch(Exception e)when(e is JsonException||e is DecoderFallbackException){writeInfo=true;}
            if(writeInfo)AtomicFile(meta,utf8.GetBytes(content));
            return new InstallationBackupSelection{WorldKey=data.WorldKey,FileName="latest.json.gz"};
        }
    }
}
