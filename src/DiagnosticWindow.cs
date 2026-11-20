using System;
using System.Collections;
using System.Reflection;
using Game.State;
using UI.Builder;
using UI.Windows;
using UnityEngine;
using Railroader.Modding;

namespace EnusDE.Signaltechnik {
    public sealed class DiagnosticWindow : MonoBehaviour {
        const string Id="enusde.signaltechnik.diagnose";
        readonly LiveViewCache view=new LiveViewCache();
        WindowManager manager;
        CustomWindowDefinition definition;
        DiagnosticSettings config;
        IModLogger log;
        double nextPoll;
        bool stopped,queuedToggle,layoutDirty,reported;
        public long Builds {get;private set;}
        public long Updates=>view.Updates;
        public void Initialize(IModLogger logger){log=logger;ApplySettings(Entry.SettingsSnapshot());}
        public void ApplySettings(DiagnosticSettings settings){config=settings.Clone();layoutDirty|=view.ChangeLayout(config);}
        bool Open=>manager!=null&&!StateManager.IsUnloading&&CustomWindowHandle.Create(Id)?.IsOpen()==true;
        void Ensure() {
            var next=WindowManager.Shared;if(next==null)return;
            if(manager!=next){Release();manager=next;}
            if(definition!=null&&ReferenceEquals(manager.GetDefinition(Id),definition))return;
            definition=new CustomWindowDefinition{Identifier=Id,Title="EnusDE Signaltechnik",BuilderAction=Build,
                Config=WindowConfig.Resizable(new Vector2Int(620,650),new Vector2Int(400,300),new Vector2Int(1200,1000),Window.Position.CenterRight)};
            manager.RegisterCustomWindow(definition);layoutDirty=false;
        }
        void Update() {
            if(stopped||StateManager.IsUnloading||Time.realtimeSinceStartup<nextPoll)return;
            nextPoll=Time.realtimeSinceStartup+1;
            try {
                Ensure();if(manager==null)return;
                if(queuedToggle){queuedToggle=false;RefreshValues();CustomWindowHandle.Create(Id)?.Toggle();}
                if(!Open)return;
                RefreshValues();
                if(layoutDirty){layoutDirty=false;CustomWindowHandle.Create(Id)?.SetNeedsRebuild();}
            }catch(Exception e){if(!reported){reported=true;log?.Warning("Diagnosefenster: "+e.Message);}}
        }
        public string Command(string command) {
            if(stopped||StateManager.IsUnloading)return "Diagnosefenster nicht bereit.";
            if(command=="status"){RefreshValues();return Entry.RecorderCommand("status")+" | "+view.Get("vehicle_status");}
            if(command!="toggle")return "Unbekannter Fensterbefehl.";
            Ensure();
            if(manager==null){queuedToggle=!queuedToggle;return "Diagnosefenster wird nach dem Laden geöffnet.";}
            RefreshValues();CustomWindowHandle.Create(Id)?.Toggle();return "Diagnosefenster umgeschaltet.";
        }
        void RefreshValues() {
            try {
                string selected=null;
                if(config["vehicles"].Visible&&config["vehicles"].Enabled&&Entry.IsRecording) {
                    var car=Game.Scripting.Modules.Railroader.World.getSelectedCar()??Game.Scripting.Modules.Railroader.World.getInspectorCar();
                    if(car!=null)selected=car.id;
                }
                view.Replace(Entry.DisplayData(selected));
            }catch(Exception e){
                view.Replace(new System.Collections.Generic.Dictionary<string,string>{{"error","Daten nicht verfügbar: "+e.Message},{"vehicle_status","Messung nicht verfügbar"}});
            }
        }
        void Field(UIPanelBuilder b,string label,string key){b.AddField(label,()=>view.Get(key),UIPanelBuilder.Frequency.Periodic);}
        void Build(UIPanelBuilder builder) {
            Builds++;RefreshValues();layoutDirty=false;
            builder.VScrollView(b=>{
                b.AddTitle(LocalText.Get("EnusDE Signaltechnik"),"P0 · Wählbare Diagnose v"+Entry.Version);
                b.AddLabel(LocalText.Get("Einstellungen: Spieloptionen → Signaltechnik."));
                b.AddLabel(LocalText.Get("Werte stammen aus der letzten Aufzeichnung. Die Scrollposition bleibt bei Messwertänderungen erhalten."));
                if(config.ShowRecorder)b.AddSection(LocalText.Get("Hintergrundaufzeichnung"),section=>{
                    section.AddField(LocalText.Select("Recording folder","Aufzeichnungsordner"),()=>Entry.RecordingsDirectory,UIPanelBuilder.Frequency.Periodic);
                    Field(section,"Aufzeichnung","recording");Field(section,"Gespeicherte Datensätze","written");
                    Field(section,"Verworfene Datensätze","dropped");Field(section,"Durch Begrenzung ausgelassen","suppressed");
                });
                Field(b,"Recorderhinweis","error");Field(b,"Instanz","role");
                b.AddButton(LocalText.Select("Export diagnostic ZIP","Diagnose-ZIP exportieren"),Entry.ExportDiagnostics);
                b.AddField(LocalText.Select("Export result","Exportergebnis"),()=>Entry.DiagnosticExportStatus,UIPanelBuilder.Frequency.Periodic);
                if(config["vehicles"].Visible)b.AddSection(LocalText.Get("Ausgewähltes Fahrzeug"),section=>{
                    Field(section,"Messstatus","vehicle_status");Field(section,"Name","name");Field(section,"Fahrzeug-ID","id");Field(section,"Messzeit (UTC)","sampleTime");
                    if(config.ShowSpeed){Field(section,"Geschwindigkeit (km/h)","kmh");Field(section,"Geschwindigkeit (mph)","mph");}
                    if(config.ShowPositions){Field(section,"Gleisposition vorne","front");Field(section,"Gleisposition hinten","rear");}
                });
                foreach(string category in new[]{"signals","blocks","switches","signs"}) {
                    string key=category;if(!config[key].Visible)continue;
                    b.AddSection(SettingsMenu.Caption(key),section=>{Field(section,"Objekte im Umfang",key+"_count");Field(section,"Zustand",key+"_status");});
                }
                if(config["signs"].Visible){Field(b,"Schilder-Verwaltungsereignisse","sign_callbacks");b.AddLabel(LocalText.Get("Keine bestätigten Zug-/Schildauslöser."));}
                if(config["game_log"].Visible)Field(b,"Letzte Spielmeldung","last_log");
                if(config["performance"].Visible){Field(b,"Leistung","performance");Field(b,"Speicher des Spielprozesses","memory");}
                b.AddButton(LocalText.Get("Werte aktualisieren"),RefreshValues);
                b.AddButton(LocalText.Get("Schließen"),()=>{if(manager!=null&&!StateManager.IsUnloading)manager.CloseWindow(Id);});
            });
        }
        // Adapted from the MIT-licensed EnusDE Mod Menu 0.1.2 ownership cleanup.
        void Release() {
            if(manager!=null&&definition!=null&&ReferenceEquals(manager.GetDefinition(Id),definition)) {
                manager.CloseWindow(Id);var instance=manager.GetCustomWindow(Id);definition.BuilderAction=null;
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                (typeof(WindowManager).GetField("_customDefinitions",flags)?.GetValue(manager) as IDictionary)?.Remove(Id);
                (typeof(WindowManager).GetField("_customWindows",flags)?.GetValue(manager) as IDictionary)?.Remove(Id);
                manager.UnregisterActiveWindow(Id);
                if(instance!=null)UnityEngine.Object.Destroy(instance.gameObject);
            }
            manager=null;definition=null;
        }
        public void Shutdown(){if(stopped)return;stopped=true;queuedToggle=false;try{Release();}catch(Exception e){log?.Warning("Diagnosefenster konnte beim Weltabbau nicht vollständig entfernt werden: "+e.Message);}view.Clear();}
        void OnDestroy(){Shutdown();}
    }
}
