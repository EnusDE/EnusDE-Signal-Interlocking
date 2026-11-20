using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripting.Modules.Railroader;
using Railroader.Modding;
using Relay;
using Relay.Modules;
using Relay.Native;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class Entry : IModEntryPoint,IWorldLifecycle {
        public const string Version="0.7.59";
        static Recorder current;
        static DiagnosticWindow diagnostic;
        static MarkerRuntime markers;
        static SignalRuntime signals;
        static SharedRuntime shared;
        public static SharedRuntime Shared=>shared;
        public static string RecordingsDirectory=>current?.DirectoryPath??Path.Combine(Application.persistentDataPath,"EnusDE-Signaltechnik","Diagnostics");
        public static void OpenDiagnostics()=>diagnostic?.Command("toggle");
        public static string DiagnosticExportDirectory=>string.IsNullOrWhiteSpace(settings.DiagnosticExportDirectory)?Path.Combine(Application.persistentDataPath,"EnusDE-Signaltechnik","Diagnostics","Exports"):settings.DiagnosticExportDirectory;
        public static string DiagnosticExportStatus {get;private set;}="";
        public static void ExportDiagnostics() {
            try {
                if(signals==null)throw new InvalidOperationException(LocalText.Select("Load the session before exporting diagnostics.","Vor dem Diagnoseexport den Spielstand laden."));
                string file=DiagnosticArchive.Write(DiagnosticExportDirectory,signals.DiagnosticSnapshot());
                DiagnosticExportStatus=LocalText.Select("Diagnostic ZIP saved: ","Diagnose-ZIP gespeichert: ")+file;
            }catch(Exception e){DiagnosticExportStatus=LocalText.Select("Diagnostic export failed: ","Diagnoseexport fehlgeschlagen: ")+e.Message;}
        }
        static string startupError;
        static DiagnosticSettings settings=new DiagnosticSettings();
        static SettingsStore store;
        GameObject root;
        IModLogger log;
        IDisposable preferences;
        IDisposable companyPreferences;
        ConstructionIntegration constructionIntegration;
        HandbookIntegration handbookIntegration;
        SettingsMenu menu;
        TrackMenuIntegration trackMenu;
        SignTooltipHooks tooltipHooks;
        TransferHooks transfers;
        internal static string SearchCloseKey=>settings.SearchCloseKey;
        public static DiagnosticSettings SettingsSnapshot(){var copy=settings.Clone();if(shared!=null&&shared.Ready){copy.Markers.MaterialsInSandbox=shared.Snapshot().Economy.MaterialsInSandbox;copy.Markers.FreeInEconomy=shared.Snapshot().Economy.FreeInEconomy;copy.Markers.EconomyEnabled=shared.EconomyEnabled;copy.Markers.MaterialFactor=shared.EconomyFactor;shared.ApplyPlacementDefaults(copy.Markers);}return copy;}
        static DiagnosticSettings SessionSettings(){var copy=SettingsSnapshot();if(!Game.State.StateManager.IsHost&&!copy.RecordOnClients)copy.Recording=false;return copy;}
        public static bool IsRecording=>current!=null&&current.Recording;
        public static long WindowBuilds=>diagnostic==null?0:diagnostic.Builds;
        public void OnModLoad(IModContext context) {
            log=context.Log;startupError=null;
            try{transfers=new TransferHooks();transfers.Start(log);}catch(Exception e){log.Error(e,"Signaltechnik MP-Großübertragung nicht verfügbar.");}
            store=new SettingsStore(Path.Combine(Application.persistentDataPath,"EnusDE-Signaltechnik","settings.json"));
            settings=store.Load();LocalText.Configure(()=>settings.Language);
            if(store.Error!=null)log.Warning(store.Error);
            try{handbookIntegration=new HandbookIntegration(message=>log.Warning(message));}catch(Exception e){log.Warning("F1 handbook: "+e.Message);}
            try{constructionIntegration=new ConstructionIntegration(message=>log.Warning(message));}catch(Exception e){log.Warning("Construction integration: "+e.Message);}
            RegisterBridge(RailroaderModule.Shared);
            var registered=ModuleRegistry.GetModule("railroader");
            if(registered!=null&&!ReferenceEquals(registered,RailroaderModule.Shared))RegisterBridge(registered);
            menu=new SettingsMenu(SettingsSnapshot,SaveSettings,store.FilePath);
            try{companyPreferences=new CompanySettingsRegistration(menu.BuildStandalone,message=>log.Warning(message));}
            catch(Exception e){log.Warning("Company-Einstellungen nicht verfügbar: "+e.Message);}
            try{preferences=SignalPreferencesRegistration.Register(companyPreferences==null?menu.BuildStandalone:menu.BuildMovedNotice,message=>log.Warning(message));}
            catch(Exception e){log.Warning("Signaltechnik-Einstellungen nicht verfügbar: "+e.Message);}
            try{trackMenu=new TrackMenuIntegration();trackMenu.Start(()=>markers!=null&&!Game.State.StateManager.IsUnloading&&Game.State.StateManager.IsHost&&settings.Markers.Visible,(at,kind)=>markers?.FromTrack(at,kind),message=>log.Warning(message),()=>signals?.PickingPoint==true,node=>signals?.TryAssignWorldPoint(node)==true);}
            catch(Exception e){log.Warning("Gleis-Kontextmenü nicht verfügbar: "+e.Message);}
            log.Info("Signaltechnik "+Version+": Einstellungen unter Company > Optionen > Mods > Signaltechnik.");
        }
        public static void SaveSettings(DiagnosticSettings value) {
            var next=value.Clone();
            signals?.CheckSettings(next);
            if(store==null)throw new InvalidOperationException("Einstellungsablage nicht bereit.");
            if(shared!=null&&shared.Ready){if(Game.State.StateManager.IsHost){shared.ApplyMaterialPolicy(next.Markers.MaterialsInSandbox,next.Markers.FreeInEconomy,next.Markers.MaterialFactor);shared.ApplyPlacement(next.Markers);}else if(next.Markers.EconomyEnabled!=shared.EconomyEnabled||next.Markers.MaterialFactor!=shared.EconomyFactor||!shared.PlacementMatches(next.Markers))throw new InvalidOperationException("Materialwirtschaft und Bauvorgaben werden vom Host festgelegt.");}
            store.Save(next);settings=next;
            if(current!=null)current.Apply(SessionSettings());
            if(diagnostic!=null)diagnostic.ApplySettings(settings);
            if(markers!=null)markers.ApplySettings(settings);
            if(signals!=null)signals.ApplySettings(settings);
        }
        static void RegisterBridge(NativeModule module) {
            module.Functions.RemoveAll(f=>f.Name=="enusdeRecorderStatus"||f.Name=="enusdeRecorderCommand"||f.Name=="enusdeVehicleSample"||f.Name=="enusdeDiagnosticWindow"||f.Name=="enusdeGrenzzeichen");
            module.DefineFunction("enusdeRecorderStatus",0,args=>Status());
            module.DefineFunction("enusdeRecorderCommand",1,args=>Value.String(RecorderCommand(args[0].AsString())));
            module.DefineFunction("enusdeDiagnosticWindow",1,args=>{try{return Value.String(diagnostic==null?"Diagnosefenster nicht bereit: noch keine Sitzung geladen.":diagnostic.Command(args[0].AsString()));}catch(Exception e){return Value.String("Diagnosefenster nicht verfügbar: "+e.Message);}});
            module.DefineFunction("enusdeVehicleSample",1,args=>VehicleSample(args[0].AsString()));
            module.DefineFunction("enusdeGrenzzeichen",1,args=>Value.String(MarkerCommand(args[0].AsString())));
        }
        static Value VehicleSample(string id) {
            object data;
            if(current==null||!current.Recording||!settings["vehicles"].Enabled||current.Sampler==null||!current.Sampler.LatestVehicles.TryGetValue(id,out data))return Value.Null();
            var row=JObject.FromObject(data);if(row["error"]!=null)return Value.Null();
            var values=new Dictionary<Value,Value>();
            foreach(var pair in new Dictionary<string,string>{{"id","id"},{"name","name"},{"speedMph","mph"},{"speedKmh","kmh"},{"front","front"},{"rear","rear"}})
                values[Value.String(pair.Key)]=Value.String(row[pair.Value]==null?"":row[pair.Value].Type==JTokenType.String?(string)row[pair.Value]:row[pair.Value].ToString(Formatting.None));
            values[Value.String("locomotive")]=Value.Bool((bool?)row["locomotive"]??false);
            values[Value.String("sampleTime")]=Value.String(current.Sampler.VehicleSampleUtc);
            return Value.Dictionary(values);
        }
        static Value Status() {
            var r=current;
            var values=new Dictionary<Value,Value>{
                {Value.String("recording"),Value.Bool(r!=null&&r.Recording)},
                {Value.String("path"),Value.String(r==null?"":r.DirectoryPath??"")},
                {Value.String("ticks"),Value.Float(r==null?0:r.Ticks)},
                {Value.String("written"),Value.Float(r==null?0:r.Written)},
                {Value.String("dropped"),Value.Float(r==null?0:r.Dropped)},
                {Value.String("suppressed"),Value.Float(r==null?0:r.Suppressed)},
                {Value.String("error"),Value.String((r==null?startupError:r.Error)??store?.Error??"")},
                {Value.String("locomotives"),Value.Integer(r==null?0:r.Reader.LocomotiveCount)},
                {Value.String("show_speed"),Value.Bool(settings.ShowSpeed)},
                {Value.String("show_positions"),Value.Bool(settings.ShowPositions)},
                {Value.String("show_recorder"),Value.Bool(settings.ShowRecorder)},
                {Value.String("performance_text"),Value.String(r==null?"Noch keine Sitzung.":r.LastPerformance)},
                {Value.String("last_log"),Value.String(r==null?"Noch keine Sitzung.":r.LastLogMessage)}
            };
            values[Value.String("sign_callbacks")]=Value.String(r==null||!r.Recording||!settings["signs"].Enabled?"Erfassung aus oder angehalten.":r.SignObserverStatus);
            foreach(string key in DiagnosticSettings.Categories) {
                bool enabled=settings.Recording&&settings[key].Enabled;
                values[Value.String("capture_"+key)]=Value.Bool(enabled);
                values[Value.String("show_"+key)]=Value.Bool(settings[key].Visible);
                int count=r?.Sampler!=null&&r.Sampler.Counts.ContainsKey(key)?r.Sampler.Counts[key]:0;
                values[Value.String(key)]=Value.Integer(count);
                string text=!enabled?"Erfassung aus":r==null?"Noch keine Sitzung.":!r.Recording?"Aufzeichnung angehalten":r.Sampler.Views[key];
                values[Value.String("status_"+key)]=Value.String(text);
            }
            return Value.Dictionary(values);
        }
        public static string RecorderCommand(string requested) {
            string value=(requested??"status").Trim().ToLowerInvariant();
            if(value=="an"||value=="on"||value=="start"||value=="aus"||value=="off"||value=="stop") {
                var next=SettingsSnapshot();next.Recording=value=="an"||value=="on"||value=="start";if(next.Recording&&!Game.State.StateManager.IsHost)next.RecordOnClients=true;
                try{SaveSettings(next);}catch(Exception e){return "Nicht übernommen: "+e.Message;}
            } else if(value!="status"&&value!="")return "Verwendung: /enus_signal_record [status|an|aus]";
            var r=current;if(r==null)return "Recorder nicht bereit: "+(startupError??"noch keine Sitzung geladen.");
            return "Hintergrundaufzeichnung "+(r.Recording?"aktiv":"aus")+" | Datensätze: "+r.Written+" | Verworfen: "+r.Dropped+" | Begrenzt: "+r.Suppressed+" | "+r.DirectoryPath+(string.IsNullOrEmpty(r.Error)?"":" | Fehler: "+r.Error);
        }
        public void OnWorldLoad() {
            OnWorldUnload();
            try {
                root=new GameObject("EnusDE Signaltechnik recorder");
                shared=root.AddComponent<SharedRuntime>();shared.Initialize(log,Path.Combine(Application.persistentDataPath,"EnusDE-Signaltechnik"));
                current=root.AddComponent<Recorder>();current.Begin(SessionSettings());
                diagnostic=root.AddComponent<DiagnosticWindow>();diagnostic.Initialize(log);
                markers=root.AddComponent<MarkerRuntime>();markers.Initialize(log,Path.Combine(Application.persistentDataPath,"EnusDE-Signaltechnik"));
                tooltipHooks=new SignTooltipHooks();tooltipHooks.Start(message=>log.Warning(message));
                signals=root.AddComponent<SignalRuntime>();signals.Initialize(markers,log,Path.Combine(Application.persistentDataPath,"EnusDE-Signaltechnik"));
                if(current.Recording)log.Info("Hintergrundaufzeichnung: "+current.DirectoryPath);
                else if(current.Error!=null)log.Warning("Hintergrundaufzeichnung konnte nicht gestartet werden: "+current.Error);
            }catch(Exception e){startupError=e.Message;log.Error(e,"Recorder konnte nicht gestartet werden.");}
        }
        public void OnWorldUnload() {
            try{tooltipHooks?.Dispose();}catch(Exception e){log?.Warning("Schild-Tooltip-Bereinigung: "+e.Message);}finally{tooltipHooks=null;}
            try{signals?.Shutdown();}catch(Exception e){log?.Warning("Stellwerk-Bereinigung: "+e.Message);}finally{signals=null;}
            try{if(markers!=null)markers.Shutdown();}catch(Exception e){log?.Warning("Grenzzeichen-Bereinigung: "+e.Message);}finally{markers=null;}
            try{if(shared!=null)shared.Shutdown();}catch(Exception e){log?.Warning("Anlagen-Bereinigung: "+e.Message);}finally{shared=null;}
            if(diagnostic!=null)diagnostic.Shutdown();diagnostic=null;
            if(current!=null)current.DisposeRecorder();current=null;
            if(root!=null)UnityEngine.Object.Destroy(root);root=null;
        }
        public void OnModUnload(){try{handbookIntegration?.Dispose();}catch(Exception e){log?.Warning("Guide cleanup: "+e.Message);}finally{handbookIntegration=null;}try{constructionIntegration?.Dispose();}catch(Exception e){log?.Warning("Construction cleanup: "+e.Message);}finally{constructionIntegration=null;}trackMenu?.Dispose();trackMenu=null;OnWorldUnload();transfers?.Dispose();transfers=null;preferences?.Dispose();preferences=null;companyPreferences?.Dispose();companyPreferences=null;menu=null;store=null;}
        public static string MarkerCommand(string command)=>markers==null?"Grenzzeichen-Werkzeug nicht bereit: erst eine Spielwelt laden.":markers.Command(command);
        public static void RecordMarkerEvent(string action,MarkerRecord row){if(current!=null)current.LocalMarkerEvent(action,row);}
        public static LocalSignalAspect SignalAspect(string id)=>signals?.Aspect(id)??LocalSignalAspect.Stop;
        internal static void CheckLineConfigurationChange(string[] affected=null,IEnumerable<LineTrackDefinition> proposed=null,string[] targets=null)=>signals?.CheckLineConfigurationChange(affected,proposed,targets);
        public static bool SignalCanModify(string id)=>signals==null||signals.CanModify(id);
        public static bool MastCanModify(MarkerRecord row)=>SignalCatalogue.Ids(row).All(SignalCanModify);
        public static bool PlacingObject=>markers!=null&&markers.IsPlacing;
        internal static bool BlockWorldObjectClick=>signals?.ConsumeSelectionInput()==true||WorldPointerInput.OverUi();
        public static void OpenMarker(string id,int direction=0){if(signals?.OfferWorldSignalPick(id,direction)==true)return;markers?.OpenObject(id,direction);}
        internal static void ActivateMarker(string id,int direction,bool secondary){if(signals?.OfferWorldSignalPick(id,direction)==true)return;if(secondary)markers?.OpenObjectContext(id,direction);else markers?.OpenObject(id,direction);}
        internal static string WorldSelectionHint(string id)=>signals?.WorldSelectionHint(id);
        internal static bool TryPickWorldObject(string id)=>signals?.OfferWorldSignalPick(id,0)==true;
        internal static void BuildSignalContactStatus(UI.Builder.UIPanelBuilder c,string id)=>signals?.BuildSignalContactStatus(c,id);
        internal static PointCircuitOption[] PointCircuitOptions()=>signals?.PointCircuitOptions()??Array.Empty<PointCircuitOption>();
        internal static string InspectPointCircuit(string id)=>signals?.InspectPointCircuit(id)??"Weichenerkennung noch nicht bereit.";
        internal static void ConfigureSwitchModel(string id,bool electric,bool wholeDesk)=>signals?.ConfigureSwitchModel(id,electric,wholeDesk);
        internal static void ConfigurePointCircuit(string id,bool enabled,bool detect)=>signals?.ConfigurePointCircuit(id,enabled,detect);
        internal static void EnableTrackPointCircuit(Track.Location at){if(signals==null)throw new InvalidOperationException(LocalText.Select("Load a world first.","Zuerst eine Spielwelt laden."));signals.EnableTrackPointCircuit(at);}
        internal static MarkerRecord[] AutomaticPointBoxes()=>signals?.AutomaticPointBoxes()??Array.Empty<MarkerRecord>();
        internal static bool IsSharedSignalCircuit(string id)=>signals?.IsSharedSignalCircuit(id)==true;
        internal static string SharedPointSignal(string id)=>signals?.SharedPointSignal(id);
        internal static string[] SharedPointSignals(string point)=>signals?.SharedPointSignals(point)??Array.Empty<string>();
        internal static string[] SharedSignalPoints(string signal)=>signals?.SharedSignalPoints(signal)??Array.Empty<string>();
        internal static void OpenPointBoundary(string id)=>markers?.OpenPointBox(id);
        internal static MarkerRecord PointBoxDefault(string id)=>signals?.PointBoxDefault(id);
        internal static PointBoxPlacement PointBoxPosition(string id)=>signals?.PointBoxPosition(id);
        internal static void PutPointBoxPosition(string id,PointBoxPlacement placement)=>signals?.PutPointBoxPosition(id,placement);
        public static string MarkerCaption(string id)=>markers?.ObjectCaption(id)??"Signaltechnik";
        public static void ApplyHostPlacement(){if(markers==null)throw new InvalidOperationException("Erst eine Welt laden.");markers.ApplyHostPlacement();}
        internal static void BuildBackups(UI.Builder.UIPanelBuilder b){if(shared==null)b.AddLabel(LocalText.Get("Zuerst eine Spielwelt laden."));else shared.BuildBackups(b);}
        internal static void CheckBackupRestore()=>signals?.CheckBackupRestore();
        internal static void SuspendBackupRestore()=>signals?.SuspendBackupRestore();
        internal static void CancelBackupRestore()=>signals?.CancelBackupRestore();
        internal static void ReloadBackupConfiguration(){try{markers?.ReloadBackupConfiguration();}finally{signals?.ReloadBackupConfiguration();}}
        internal static void OpenAutomation()=>signals?.OpenAutomation();
        internal static void BuildAutomationSettings(UI.Builder.UIPanelBuilder b){if(signals==null)b.AddLabel(LocalText.Get("Zuerst eine Welt laden."));else signals.BuildAutomationSettings(b);}
        internal static void BuildSignalRoleEditor(UI.Builder.UIPanelBuilder b,string id,Action verify=null)=>signals?.BuildSignalRoleEditor(b,id,verify);
        internal static void BuildSignalOperatingEditor(UI.Builder.UIPanelBuilder b,string id,Action verify=null)=>signals?.BuildSignalOperatingEditor(b,id,verify);
        internal static bool IgnoresOwnSignal(string id)=>signals?.IgnoreOwnSignal(id)==true;
        internal static void PlaceAutomationContact(Track.Location at)=>signals?.PlaceAutomationContact(at);
        public static void OpenInterlocking()=>signals?.Open();
        internal static bool WorkspaceAvailable=>signals?.WorkspaceAvailable==true;
        internal static bool WorkspaceEditable=>signals?.WorkspaceEditable==true;
        internal static void OpenWorkspaceMode(bool editing)=>signals?.OpenMode(editing);
        internal static void CloseWorkspaceMode()=>signals?.CloseWorkspaceMode();
        public static void OpenMaterials()=>markers?.OpenMaterials();
        public static void RecordSignalEvent(string kind,object data){if(current!=null)current.SignalEvent(kind,data);}
        public static bool WantsSignalEvents=>current!=null&&current.Recording&&settings["signals"].Enabled;
        public static void FormatSignTooltip(Game.Mow.PlayerSign sign,ref TooltipInfo tooltip){if(markers!=null)markers.FormatSignTooltip(sign,ref tooltip);}
        public static void InspectSignalTrack(Track.Location at){signals?.InspectTrack(at);}
        internal static void AssignPanelSignal(string id) {
            if(signals==null)throw new InvalidOperationException("Stellwerk noch nicht bereit.");
            signals.AssignPanelSignal(id);
        }
        internal static void RememberCircuitBoundary(string id) {
            if(signals==null)throw new InvalidOperationException("Stellwerk noch nicht bereit.");
            signals.RememberCircuitBoundary(id);
        }
        public static void AssignSignalPoint(Track.Location at){signals?.AssignTrackPoint(at);}
        internal static string SelectTrackPoint(Track.Location at){if(signals==null)throw new InvalidOperationException(LocalText.Get("Zuerst eine Welt laden."));return signals.SelectTrackPoint(at);}
        public static Dictionary<string,string> DisplayData(string selectedId) {
            var r=current;
            var d=new Dictionary<string,string>{
                ["recording"]=IsRecording?"Aktiv, auch bei geschlossenem Fenster":"Aus",
                ["written"]=(r==null?0:r.Written).ToString(),["dropped"]=(r==null?0:r.Dropped).ToString(),["suppressed"]=(r==null?0:r.Suppressed).ToString(),
                ["role"]=Game.State.StateManager.IsHost?"Host":"Client",["error"]=(r?.Error??store?.Error??startupError)??"Keine",
                ["last_log"]=settings["game_log"].Enabled&&IsRecording?r.LastLogMessage:"Erfassung aus oder angehalten.",
                ["performance"]=settings["performance"].Enabled&&IsRecording?r.LastPerformance:"Erfassung aus oder angehalten.",
                ["memory"]=settings["performance"].Enabled&&IsRecording?r.LastMemory:"Erfassung aus oder angehalten.",
                ["sign_callbacks"]=settings["signs"].Enabled&&IsRecording?r.SignObserverStatus:"Erfassung aus oder angehalten."
            };
            foreach(string key in new[]{"signals","blocks","switches","signs"}) {
                d[key+"_count"]=IsRecording&&r.Sampler!=null?r.Sampler.Counts[key].ToString():"—";
                d[key+"_status"]=!IsRecording||!settings[key].Enabled?"Erfassung aus oder angehalten.":r.Sampler.Views[key];
            }
            d["vehicle_status"]=!IsRecording||!settings["vehicles"].Enabled?"Fahrzeugerfassung aus oder angehalten.":selectedId==null?"Kein Fahrzeug ausgewählt.":"Noch keine Messung für dieses Fahrzeug im gewählten Umfang.";
            foreach(string key in new[]{"id","name","sampleTime","kmh","mph","front","rear"})d[key]="—";
            object data;
            if(IsRecording&&settings["vehicles"].Enabled&&selectedId!=null&&r.Sampler.LatestVehicles.TryGetValue(selectedId,out data)) {
                var row=JObject.FromObject(data);
                if(row["error"]!=null){d["vehicle_status"]="Fahrzeug nicht lesbar: "+row["error"];return d;}
                d["vehicle_status"]="Letzte aufgezeichnete Messung";
                d["sampleTime"]=r.Sampler.VehicleSampleUtc;
                d["id"]=(string)row["id"]??"—";d["name"]=(string)row["name"]??"—";
                d["kmh"]=((double?)row["kmh"])?.ToString("0.0")??"—";d["mph"]=((double?)row["mph"])?.ToString("0.0")??"—";
                d["front"]=PositionText(row["front"]);d["rear"]=PositionText(row["rear"]);
            }
            return d;
        }
        static string PositionText(JToken row)=>row==null||row.Type==JTokenType.Null?"Nicht verfügbar":(string)row["segment"]+" · Ende "+row["end"]+" · "+((double?)row["meters"])?.ToString("0.0")+" m";
    }
}
