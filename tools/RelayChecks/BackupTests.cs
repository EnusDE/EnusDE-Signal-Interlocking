using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;

public static class BackupTests {
    static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL: "+label);System.Console.WriteLine("PASS: "+label);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    const string World="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",Other="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",Source="cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    static InstallationBackupData Fixture() {
        var install=new InstallationDocument{WorldKey=World,Map="map",Objects=new MarkerDocument{WorldKey=World,Map="map"}};
        MarkerRecord a=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name="HS-001",Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="S",Meters=10}},b=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Name="HS-002",Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="S",Meters=90}};
        install.Objects.Markers.AddRange(new[]{a,b});install.Economy.Depots.Add(new MaterialDepot{Anchor=a.Anchor.Clone(),Stock=new MaterialCost(12,8)});
        install.TrainNumbers["engine"]="00123";install.SignalNumberFields[a.Id]="00123";
        var desks=new DeskLibrary{WorldKey=World,Desks=new List<DeskDefinition>{new DeskDefinition{Code="ESTW",Name="Global",Global=true,Panel=new PanelDocument{WorldKey=World,Tiles=new List<PanelTile>{new PanelTile{X=1,Y=1,Kind=PanelTileKind.Signal,Reference=a.Id}}}}}};
        desks.Automation.Enabled=true;desks.Automation.BlockEnabled=true;desks.Automation.Signals[a.Id]=new AutomaticSignal{Enabled=true,Primary=b.Id};
        desks.Automation.Contacts.Add(new AutomationContact{Name="Kontakt",Signal=a.Id,Enabled=true,Anchor=new MarkerAnchor{Segment="S",Meters=5}});
        desks.Automation.Locomotives["engine"]=new LocomotiveDuty{Role=MovementRole.Train,Consist=Source};
        var path=new DirectedRouteSection{Section=new TrackSection{Segment="S",From=10,To=90}};
        var route=new RouteDefinition{Code="A-B",Entry=a.Id,Exit=b.Id,Path=new List<DirectedRouteSection>{path},ViaSignals=new List<RouteViaSignal>(),Sections=new List<TrackSection>{path.Section.Clone()},Approach=new List<TrackSection>{new TrackSection{Segment="S",From=0,To=10}}};
        return new InstallationBackupData{Installation=install,Desks=desks,Routes=new InterlockingDocument{WorldKey=World,Routes=new List<RouteDefinition>{route}}};
    }
    static void Rewrite(string path,Action<InstallationBackupEnvelope> change) {
        InstallationBackupEnvelope data;
        using(var stream=File.OpenRead(path))using(var zip=new GZipStream(stream,CompressionMode.Decompress))using(var reader=new StreamReader(zip))data=JsonConvert.DeserializeObject<InstallationBackupEnvelope>(reader.ReadToEnd());
        change(data);using(var stream=File.Create(path))using(var zip=new GZipStream(stream,CompressionMode.Compress))using(var writer=new StreamWriter(zip))writer.Write(JsonConvert.SerializeObject(data));
    }
    sealed class Properties:ISharedProperties {
        public Dictionary<string,string> Values=new Dictionary<string,string>();
        public string Read(string key)=>Values.TryGetValue(key,out var value)?value:null;
        public void Write(string key,string value){if(value==null)Values.Remove(key);else Values[key]=value;}
    }
    public static void Run(string root) {
        string folder=Path.Combine(root,"build","backup-tests-"+Guid.NewGuid().ToString("N"));int seconds=0;
        var store=new InstallationBackups(folder,()=>new DateTime(2026,11,13,1,0,0,DateTimeKind.Utc).AddSeconds(seconds++));
        var original=Fixture();original.Validate();var data=InstallationBackupData.Capture(original.Installation,original.Desks,original.Routes.Routes);
        Check(data.Installation.TrainNumbers.Count==0&&data.Installation.SignalNumberFields.Count==0&&data.Desks.Automation.Locomotives.Count==0&&original.Installation.TrainNumbers.Count==1&&original.Desks.Automation.Locomotives.Count==1,
            "external configuration capture omits live train-number positions and consist roles without altering the working installation");
        var current=store.Save(data,Source,"Testwelt","first");var loaded=store.Read(current);
        Check(loaded.Data.Installation.Objects.Markers.Count==2&&loaded.Data.Desks.Automation.Contacts.Count==1&&loaded.Data.Routes.Routes.Single().Code=="A-B"&&loaded.Data.Installation.Economy.Depots[0].Stock.T==8,
            "independent gzip backup round-trips signals, shared panels, automation contacts, route definitions and material stock together");
        Check(store.Worlds("map").Single().Sources.SequenceEqual(new[]{Source})&&store.Worlds("another-map").Length==0,
            "backup catalog identifies the original save separately from the installation and filters other maps");
        var upper=InstallationBackupData.Capture(original.Installation,original.Desks,original.Routes.Routes).RestoreFor("map",World.ToUpperInvariant());
        var upperStore=new InstallationBackups(Path.Combine(root,"build","backup-uppercase-"+Guid.NewGuid().ToString("N")));
        upperStore.Save(upper,Source,"Uppercase UUID","test");
        Check(upperStore.Worlds("map").Length==1&&upperStore.Read(upperStore.Copies(World).Single()).Data.WorldKey==World.ToUpperInvariant(),
            "valid uppercase hexadecimal installation keys remain recoverable through canonical directory names");
        var first=store.Copies(World).Single();string firstPath=Path.Combine(folder,World,first.FileName);byte[] firstBytes=File.ReadAllBytes(firstPath);
        store.Save(data,Source,"Testwelt","same data");
        Check(store.Copies(World).Length==1,"unchanged configuration does not produce a new automatic backup for every polling tick");
        var again=new InstallationBackups(folder);again.Save(data,Source,"Testwelt","after restart");
        Check(store.Copies(World).Length==1,"deduplication also works after recreating the backup store on a later game session");
        store.Save(data,Other,"Save as","same world new source");
        Check(store.Copies(World).Length==1&&store.Worlds("map").Single().Sources.Count==2,"save-as locations retain recovery bindings without duplicating unchanged full snapshots");
        var prepared=loaded.Data.RestoreFor("map",Other);
        Check(prepared.WorldKey==Other&&prepared.Installation.Objects.WorldKey==Other&&prepared.Desks.WorldKey==Other&&prepared.Desks.Desks.All(d=>d.Panel.WorldKey==Other)&&prepared.Routes.WorldKey==Other,
            "explicit restoration into a same-map save rebases all configuration world keys consistently");
        Check(!prepared.Desks.Automation.Enabled&&prepared.Desks.Automation.Signals.Values.Single().Enabled&&prepared.Desks.Automation.Locomotives.Count==0&&prepared.Installation.TrainNumbers.Count==0,
            "restoration preserves signal automation design but starts global contact automation off and retains no train roles or number positions");
        Check(loaded.Data.Desks.Automation.Enabled&&Reject(()=>loaded.Data.RestoreFor("different-map",Other)),"restoration preparation is isolated and refuses another map");
        prepared.Installation.Objects.Markers[0].Name="edited";
        Check(store.Read(first).Data.Installation.Objects.Markers[0].Name=="HS-001","editing a prepared restore cannot mutate the saved source backup");
        Rewrite(firstPath,e=>e.Payload=e.Payload.Replace("HS-001","forged"));
        Check(Reject(()=>store.Read(first)),"modified backup content is rejected by checksum before being used as an installation");File.WriteAllBytes(firstPath,firstBytes);
        Rewrite(firstPath,e=>e.Protocol=99);Check(Reject(()=>store.Read(first)),"unknown backup protocols do not silently deserialize as the current installation format");File.WriteAllBytes(firstPath,firstBytes);
        Rewrite(firstPath,e=>{var invalid=JsonConvert.DeserializeObject<InstallationBackupData>(e.Payload);invalid.Desks.WorldKey=Other;e.Payload=JsonConvert.SerializeObject(invalid);e.Hash=SharedDocuments.Hash(Encoding.UTF8.GetBytes(e.Payload));});
        Check(Reject(()=>store.Read(first)),"valid checksums do not bypass cross-document world consistency checks");File.WriteAllBytes(firstPath,firstBytes);
        Check(Reject(()=>store.Read(new InstallationBackupSelection{WorldKey="../outside",FileName=first.FileName}))&&Reject(()=>store.Read(new InstallationBackupSelection{WorldKey=World,FileName="../outside.json.gz"})),
            "backup selection refuses directory traversal in both installation keys and filenames");
        Directory.CreateDirectory(Path.Combine(folder,Other));File.WriteAllBytes(Path.Combine(folder,Other,first.FileName),firstBytes);
        Check(Reject(()=>store.Read(new InstallationBackupSelection{WorldKey=Other,FileName=first.FileName})),"a copied file in a different installation folder cannot claim a foreign installation identity");
        byte[] baseline=File.ReadAllBytes(Path.Combine(folder,World,"latest.json.gz"));
        var bad=InstallationBackupData.Capture(data.Installation,data.Desks,data.Routes.Routes);bad.Routes.Routes[0].Code="";
        Check(Reject(()=>store.Save(bad,Source,"Testwelt","invalid"))&&File.ReadAllBytes(Path.Combine(folder,World,"latest.json.gz")).SequenceEqual(baseline),"invalid configuration cannot overwrite the last externally committed installation");
        store.Save(data,Source,"Testwelt","manual",true);var manual=store.Copies(World).Single(c=>c.FileName.EndsWith("-keep.json.gz",StringComparison.Ordinal));
        data.Installation.Objects.Markers.RemoveAt(0);store.Save(data,Source,"Testwelt","removed signal");
        Check(store.Copies(World).Count(c=>c.FileName.EndsWith("-keep.json.gz",StringComparison.Ordinal))==2&&store.Copies(World).Where(c=>c.FileName.EndsWith("-keep.json.gz",StringComparison.Ordinal)).All(c=>store.Read(c).Data.Installation.Objects.Markers.Count==2),
            "removing an object permanently archives the full pre-removal installation in addition to any manual checkpoint");
        for(int i=0;i<36;i++){data.Installation.Objects.Markers[0].Name="updated "+i;store.Save(data,Source,"Testwelt","edit");}
        Check(store.Copies(World).Count(c=>c.FileName.EndsWith("-auto.json.gz",StringComparison.Ordinal))==InstallationBackups.AutomaticCopies&&File.Exists(Path.Combine(folder,World,manual.FileName))&&store.Copies(World).Count(c=>c.FileName.EndsWith("-keep.json.gz",StringComparison.Ordinal))==2,
            "automatic retention stays bounded while manual and pre-deletion recovery points survive later edits");
        string previousAlias=Path.Combine(folder,World,"latest.json.gz.previous");if(File.Exists(previousAlias))File.Delete(previousAlias);Directory.CreateDirectory(previousAlias);
        string latestName=store.Read(current).Data.Installation.Objects.Markers[0].Name;data.Installation.Objects.Markers[0].Name="write interrupted";
        Check(Reject(()=>store.Save(data,Source,"Testwelt","publication failure"))&&store.Read(current).Data.Installation.Objects.Markers[0].Name==latestName,
            "failed atomic latest publication leaves the preceding committed alias readable");
        Directory.Delete(previousAlias);data.Installation.Objects.Markers[0].Name=latestName;
        byte[] goodLatest=File.ReadAllBytes(Path.Combine(folder,World,"latest.json.gz"));File.WriteAllText(Path.Combine(folder,World,"latest.json.gz"),"corrupt");
        data.Installation.Objects.Markers[0].Name="recovered";store.Save(data,Source,"Testwelt","repair mirror");
        Check(store.Read(current).Data.Installation.Objects.Markers[0].Name=="recovered"&&Directory.GetFiles(Path.Combine(folder,World),"*-unreadable.json.gz").Length==1,
            "a damaged latest alias is retained as evidence while valid history and a fresh automatic snapshot remain recoverable");
        store.SaveJournal(World,Source,"map",new Dictionary<string,string>{{"install.commit","broken old header"}});
        Check(Directory.GetFiles(Path.Combine(folder,World),"*-restore-journal.json.gz").Length==1&&store.Copies(World).All(c=>!c.FileName.Contains("journal")),"raw pre-restore property journals stay durable and cannot be mistaken for importable configuration snapshots");
        var p=new Properties();var docs=new SharedDocuments(p);docs.Write("install","old",10000);p.Values["install.commit"]="{broken";
        Check(Reject(()=>docs.Write("install","ordinary overwrite",10000)),"normal synchronization still refuses a malformed committed header");
        docs.Replace("install","explicit restore",10000);
        Check(docs.Read<string>("install",10000)=="explicit restore","explicit backup replacement can recover an unreadable shared document header");
        var beforeLoad=InstallationBackupData.Capture(data.Installation,data.Desks,data.Routes.Routes);beforeLoad.Installation.Objects.Markers[0].Anchor.Meters=75;
        int kept=store.Copies(World).Count(c=>c.FileName.EndsWith("-keep.json.gz",StringComparison.Ordinal));store.Save(beforeLoad,Source,"Testwelt","loaded older save",false,true);
        Check(store.Copies(World).Count(c=>c.FileName.EndsWith("-keep.json.gz",StringComparison.Ordinal))==kept+1,"loading a different save configuration preserves the previous external stand even when no object IDs were removed");
        var resources=InstallationBackupData.Capture(data.Installation,data.Desks,data.Routes.Routes);resources.Desks.Automation.Contacts.Clear();store.Save(resources,Source,"Testwelt","removed contact");
        Check(store.Copies(World).Count(c=>c.FileName.EndsWith("-keep.json.gz",StringComparison.Ordinal))==kept+2,"deleting a new automation contact also protects its prior complete configuration");
        string manifest=Path.Combine(folder,World,"world.json");File.WriteAllText(manifest,"{broken");
        Check(store.Worlds("map").Any(w=>w.WorldKey==World),"a damaged backup catalog recovers its installation from the preceding metadata or self-contained snapshot");
        if(File.Exists(manifest+".previous"))File.Delete(manifest+".previous");
        Check(store.Worlds("map").Any(w=>w.WorldKey==World),"self-contained backup snapshots remain discoverable even when both catalog metadata versions are lost");
        var clockFolder=Path.Combine(root,"build","backup-clock-"+Guid.NewGuid().ToString("N"));DateTime now=new DateTime(2026,11,13,0,0,0,DateTimeKind.Utc);var clockStore=new InstallationBackups(clockFolder,()=>now);
        var clockData=Fixture();for(int i=0;i<33;i++){now=now.AddSeconds(1);clockData.Installation.Objects.Markers[0].Name="clock "+i;clockStore.Save(clockData,Source,"Testwelt","edit");}
        now=now.AddDays(-1);clockData.Installation.Objects.Markers[0].Name="after clock reset";clockStore.Save(clockData,Source,"Testwelt","edit");
        Check(clockStore.Copies(World).Count(c=>c.FileName.EndsWith("-auto.json.gz",StringComparison.Ordinal))==32&&clockStore.Copies(World).Any(c=>clockStore.Read(c).Data.Installation.Objects.Markers[0].Name=="after clock reset")&&clockStore.Read(clockStore.Latest(World)).Data.Installation.Objects.Markers[0].Name=="after clock reset",
            "a backward system-clock adjustment cannot prune the just-published snapshot or hide the current recoverable configuration");
        Check(!Directory.GetFiles(folder,"*.tmp",SearchOption.AllDirectories).Any(),"completed backup operations leave no half-written temporary documents");
        string longRoot=Path.Combine(root,"build","bw-"+Guid.NewGuid().ToString("N"));longRoot+=new string('p',Math.Max(0,88-longRoot.Length));
        var longStore=new InstallationBackups(longRoot);var longData=Fixture();longStore.Save(longData,Source,"Windows path","checkpoint",true);
        var checkpoint=longStore.Copies(World).Single();string checkpointPath=Path.Combine(longRoot,World,checkpoint.FileName);
        var tempMethod=typeof(InstallationBackups).GetMethod("TemporaryFile",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
        string stagingPath=(string)tempMethod.Invoke(null,new object[]{checkpointPath});
        Check(checkpointPath.Length+37>=260&&stagingPath.Length<260&&Path.GetDirectoryName(stagingPath)==Path.GetDirectoryName(checkpointPath),
            "checkpoint staging stays on the same filesystem and below the classic Windows path limit at the reported LocalLow depth");
        longData.Installation.Objects.Markers[0].Name="after operating-state change";longStore.Save(longData,Source,"Windows path","changed configuration",false,true);
        Check(longStore.Read(checkpoint).Data.Installation.Objects.Markers[0].Name!=longStore.Read(longStore.Latest(World)).Data.Installation.Objects.Markers[0].Name&&longStore.Copies(World).Count(c=>c.FileName.EndsWith("-keep.json.gz"))==2,
            "short temporary filenames retain the preceding checkpoint while atomically publishing the changed configuration");
        Check(!Directory.GetFiles(longRoot,"*.tmp",SearchOption.AllDirectories).Any(),"long-path checkpoints leave no temporary files after publication");
    }
}
