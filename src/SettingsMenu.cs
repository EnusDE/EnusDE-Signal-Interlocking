using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed class SettingsMenu {
        readonly UIState<bool> markerExpanded=new UIState<bool>(false);
        readonly UIState<bool> materialExpanded=new UIState<bool>(false),controlsExpanded=new UIState<bool>(false),diagnosticsExpanded=new UIState<bool>(false);
        readonly UIState<bool> placementExpanded=new UIState<bool>(true),automationExpanded=new UIState<bool>(false),backupExpanded=new UIState<bool>(false);
        readonly Dictionary<string,UIState<bool>> sections=new Dictionary<string,UIState<bool>>();
        readonly Func<DiagnosticSettings> read;
        readonly Action<DiagnosticSettings> save;
        readonly string path;
        DiagnosticSettings draft;
        bool dirty;
        string message=LocalText.Get("Änderungen mit Anwenden & Speichern übernehmen.");
        public SettingsMenu(Func<DiagnosticSettings> read,Action<DiagnosticSettings> save,string path) {
            this.read=read;this.save=save;this.path=path;draft=read();
            foreach(string key in DiagnosticSettings.Categories)sections[key]=new UIState<bool>(false);
        }
        public void BuildStandalone(UIPanelBuilder builder)=>Content(builder);
        public void ResetDraft(){draft=read();dirty=false;message=LocalText.Get("Änderungen mit Anwenden & Speichern übernehmen.");}
        public static string Caption(string key) {
            switch(key){case "vehicles":return "Züge und Fahrzeuge";case "signals":return "Signale";case "blocks":return "Belegungen und Stellwerk";case "switches":return "Weichen";case "signs":return "Schilder";case "game_log":return "Relevante Spielmeldungen";case "performance":return "Leistung";default:return key;}
        }
        void Content(UIPanelBuilder b) {
            if(!dirty)draft=read();
            b.AddField(LocalText.Select("Interface language","Oberflächensprache"),b.AddDropdown(new List<string>{LocalText.Get("Automatic / Automatisch"),LocalText.Get("English"),LocalText.Get("Deutsch")},draft.Language=="de"?2:draft.Language=="en"?1:0,i=>{draft.Language=i==2?"de":i==1?"en":"auto";dirty=true;}));
            b.AddLabel(LocalText.Get("Signaltechnik · v")+Entry.Version);
            b.AddExpandable(LocalText.Get("Bauvorgaben – Abstand, Höhe und Seite"),placementExpanded,PlacementSettings,ExpandableStyle.SectionHeader);
            b.AddExpandable(LocalText.Get("Signalbetrieb und Darstellung"),markerExpanded,MarkerSettings,ExpandableStyle.SectionHeader);
            b.AddExpandable(LocalText.Get("Automatisierung und KI"),automationExpanded,Entry.BuildAutomationSettings,ExpandableStyle.SectionHeader);
            b.AddExpandable(LocalText.Get("Anlagensicherung und Wiederherstellung"),backupExpanded,Entry.BuildBackups,ExpandableStyle.SectionHeader);
            b.AddExpandable(LocalText.Get("Material und Baukosten"),materialExpanded,MaterialSettings,ExpandableStyle.SectionHeader);
            b.AddExpandable(LocalText.Get("Bedienung"),controlsExpanded,controls=>{
                var shortcutNames=new[]{"Rücktaste","Bild auf","Bild ab","Pos1","Ende","Einfg","Aus"};
                controls.AddSearchChoice(LocalText.Get("Signalliste zuklappen"),shortcutNames.ToList(),
                    Array.IndexOf(DiagnosticSettings.SearchCloseKeys,draft.SearchCloseKey),
                    i=>{draft.SearchCloseKey=DiagnosticSettings.SearchCloseKeys[i];dirty=true;});
                controls.AddButton(LocalText.Get("Baumenü öffnen"),()=>Entry.MarkerCommand("open"));
                controls.AddButton(LocalText.Get("Stellwerk öffnen"),Entry.OpenInterlocking);
                controls.AddButton(LocalText.Get("Materiallager und Bauaufträge öffnen"),Entry.OpenMaterials);
            },ExpandableStyle.SectionHeader);
            b.AddExpandable(LocalText.Get("Optionale lokale Diagnose"),diagnosticsExpanded,diagnostics=>{
                diagnostics.AddLabel(LocalText.Get("Optionale lokale Diagnose"));
                diagnostics.AddLabel(LocalText.Get("Erfassen schaltet Messung und Aufzeichnung. Anzeigen blendet Informationen im Diagnosefenster ein. Abgeschaltete Erfassung wird dort als aus angezeigt."));
                Toggle(diagnostics,LocalText.Get("Hintergrundaufzeichnung"),()=>draft.Recording,v=>draft.Recording=v);
                Toggle(diagnostics,LocalText.Get("Auf diesem Rechner auch als Mitspieler aufzeichnen"),()=>draft.RecordOnClients,v=>draft.RecordOnClients=v);
                Toggle(diagnostics,LocalText.Get("Aufzeichnungsstatus anzeigen"),()=>draft.ShowRecorder,v=>draft.ShowRecorder=v);
                diagnostics.AddField(LocalText.Select("Recording folder","Aufzeichnungsordner"),()=>Entry.RecordingsDirectory,UIPanelBuilder.Frequency.Periodic);
                diagnostics.AddButton(LocalText.Select("Open diagnostic window","Diagnosefenster öffnen"),Entry.OpenDiagnostics);
                diagnostics.AddField(LocalText.Select("Export folder (empty = default)","Exportordner (leer = Standard)"),diagnostics.AddInputField(draft.DiagnosticExportDirectory,v=>{draft.DiagnosticExportDirectory=v;dirty=true;},"",1024));
                diagnostics.AddField(LocalText.Select("Active export folder","Aktiver Exportordner"),()=>Entry.DiagnosticExportDirectory,UIPanelBuilder.Frequency.Periodic);
                diagnostics.AddLabel(LocalText.Select("Save folder changes first. You can select a locally synchronized folder. Export is manual; there is no automatic upload or remote control.","Ordneränderungen zuerst speichern. Ein lokal synchronisierter Ordner ist möglich. Der Export wird manuell gestartet; es gibt keinen automatischen Upload und keine Fernsteuerung."));
                diagnostics.AddButton(LocalText.Select("Export diagnostic ZIP","Diagnose-ZIP exportieren"),Entry.ExportDiagnostics);
                diagnostics.AddField(LocalText.Select("Export result","Exportergebnis"),()=>Entry.DiagnosticExportStatus,UIPanelBuilder.Frequency.Periodic);
                foreach(string category in DiagnosticSettings.Categories) {
                    string key=category;
                    diagnostics.AddExpandable(Caption(key),sections[key],content=>Category(content,key),ExpandableStyle.SectionHeader);
                }
                diagnostics.AddLabel(LocalText.Get("Nur aktive Datenbereiche werden gemessen. Diagnose ist getrennt von der Stellwerkssteuerung."));
            },ExpandableStyle.SectionHeader);
            b.AddButton(LocalText.Get("Anwenden & Speichern"),()=>{try{save(draft);draft=read();dirty=false;message=LocalText.Get("Gespeichert. Auswahl gilt sofort und beim nächsten Spielstart.");}catch(Exception e){message=LocalText.Get("Nicht übernommen: ")+e.Message;}b.SetNeedsRebuild();});
            b.AddButton(LocalText.Get("Gespeicherte Auswahl laden"),()=>{draft=read();dirty=false;message=LocalText.Get("Aktuell gespeicherte Auswahl geladen.");b.SetNeedsRebuild();});
            b.AddButton(LocalText.Get("Standardauswahl vorbereiten"),()=>{draft=new DiagnosticSettings();dirty=true;message=LocalText.Get("Standardauswahl vorbereitet; zum Übernehmen speichern.");b.SetNeedsRebuild();});
            b.AddLabel(message);b.AddLabel(LocalText.Get("Einstellungsdatei: ")+path);
            b.AddLabel(LocalText.Get("Diagnosefenster: /enus_signal · Status: /enus_signal_status"));
        }
        void Category(UIPanelBuilder b,string key) {
            var group=draft[key];
            Toggle(b,LocalText.Get("Erfassen"),()=>group.Enabled,v=>group.Enabled=v);
            Toggle(b,LocalText.Get("Anzeigen"),()=>group.Visible,v=>group.Visible=v);
            var intervals=key=="performance"?new double[]{5,10,30,60}:new double[]{1,2,5,10,30,60};
            int selected=Array.FindIndex(intervals,x=>x>=group.IntervalSeconds);if(selected<0)selected=intervals.Length-1;
            b.AddSearchChoice(key=="game_log"?"Meldungen bündeln":"Messabstand",intervals.Select(x=>x+" Sekunden").ToList(),selected,i=>{if(i>=0&&i<intervals.Length){group.IntervalSeconds=intervals[i];dirty=true;}});
            if(key!="game_log"&&key!="performance")b.AddField(LocalText.Get("Nur diese IDs"),b.AddInputField(group.Ids,value=>{group.Ids=value;dirty=true;},"Leer = gesamter gewählter Bereich; IDs mit Komma trennen",null));
            if(key=="vehicles") {
                Toggle(b,LocalText.Get("Nur Lokomotiven"),()=>draft.LocomotivesOnly,v=>draft.LocomotivesOnly=v);
                Toggle(b,LocalText.Get("Lok-Bedienwerte miterfassen"),()=>draft.VehicleControls,v=>draft.VehicleControls=v);
                Toggle(b,LocalText.Get("Geschwindigkeit anzeigen"),()=>draft.ShowSpeed,v=>draft.ShowSpeed=v);
                Toggle(b,LocalText.Get("Gleisposition vorne/hinten anzeigen"),()=>draft.ShowPositions,v=>draft.ShowPositions=v);
            }
            if(key=="signs")b.AddLabel(LocalText.Get("Verwaltungsrückrufe werden bei aktiver Erfassung ereignisbezogen protokolliert; der Messabstand gilt für Bestandsvergleiche."));
        }
        void MaterialSettings(UIPanelBuilder b) {
            if(Game.State.StateManager.Shared==null||Game.State.StateManager.IsHost){Toggle(b,LocalText.Select("Require materials in Sandbox","Materialbedarf auch im Sandkasten"),()=>draft.Markers.MaterialsInSandbox,v=>draft.Markers.MaterialsInSandbox=v);Toggle(b,LocalText.Select("Free construction in economy mode","Ohne Materialbedarf im Wirtschaftsmodus"),()=>draft.Markers.FreeInEconomy,v=>draft.Markers.FreeInEconomy=v);Choose(b,LocalText.Get("Materialbedarf-Faktor"),new double[]{.25,.5,1,1.5,2,3,4},draft.Markers.MaterialFactor,v=>draft.Markers.MaterialFactor=v,"");}
            else b.AddLabel(LocalText.Get("Materialwirtschaft und Bedarf werden vom Host festgelegt."));
            Entry.Shared?.BuildConstructionSettings(b);
            b.AddButton(LocalText.Get("Materiallager und Bauaufträge öffnen"),Entry.OpenMaterials);
        }
        void MarkerSettings(UIPanelBuilder b) {
            b.AddLabel(Entry.Shared?.PresentationStatus??LocalText.Select("Experimental presentation packs: load a world for status.","Experimentelle Darstellungspakete: Welt für Status laden."));
            b.AddLabel(LocalText.Select("Console-only experiment. Installed packs need a separate world test license. Existing models remain visible when disabled; a missing pack uses built-in signals.","Experiment nur per Konsole. Installierte Pakete benötigen eine eigene Welt-Testlizenz. Bestehende Modelle bleiben beim Abschalten sichtbar; fehlende Pakete verwenden eingebaute Signale."));
            Toggle(b,LocalText.Select("Build new signals with a GFM box","Neue Signale mit GFM-Kasten bauen"),()=>draft.Markers.NewSignalsWithGfm,v=>draft.Markers.NewSignalsWithGfm=v);
            b.AddLabel(LocalText.Select("Applies to new signals only. Existing boxes are retained. A box defines a boundary; complete sections still need valid boundary pairs and track recognition.","Gilt nur für neue Signale. Vorhandene Kästen bleiben erhalten. Ein Kasten definiert eine Grenze; vollständige Abschnitte benötigen passende Grenzpaare und Gleiserkennung."));
            Toggle(b,LocalText.Select("Enable Hl signal system (Test)","Hl-Signalsystem aktivieren (Test)"),()=>draft.Markers.EnableHlSignalsTest,v=>draft.Markers.EnableHlSignalsTest=v);
            b.AddLabel(LocalText.Select("Enables new Hl test builds and conversions. Existing Hl signals remain operational when this option is off. Native AI speed handling is unchanged.","Schaltet neue Hl-Testbauten und Umrüstungen frei. Bereits gebaute Hl-Signale bleiben beim Ausschalten betriebsfähig. Die native KI-Geschwindigkeitsbehandlung bleibt unverändert."));
            b.AddLabel(LocalText.Get("Automatische Gleisbild-Vorschläge werden überarbeitet. Diese Version zeichnet oder ersetzt keine Tafel automatisch; die Abschnittserkennung bleibt aktiv."));
            Toggle(b,LocalText.Get("Eigene Objekte und Signalmodelle anzeigen"),()=>draft.Markers.Visible,v=>draft.Markers.Visible=v);
            b.AddSection(LocalText.Get("Signallampen – lokale Darstellung"));
            Toggle(b,LocalText.Get("Signallinsen selbst leuchten lassen"),()=>draft.Markers.Glow.Enabled,v=>draft.Markers.Glow.Enabled=v);
            Choose(b,LocalText.Get("Helligkeit der Signallinsen"),new double[]{1,2,4,6,8,12,18,24},draft.Markers.Glow.Radiance,v=>draft.Markers.Glow.Radiance=v,"×");
            Toggle(b,LocalText.Get("Farbigen Lichthof anzeigen"),()=>draft.Markers.Glow.Halo,v=>draft.Markers.Glow.Halo=v);
            Choose(b,LocalText.Get("Stärke des Lichthofs"),new double[]{.1,.25,.5,.75,1,1.5,2},draft.Markers.Glow.HaloStrength,v=>draft.Markers.Glow.HaloStrength=v,"×");
            b.AddLabel(LocalText.Get("Rot, Grün, Gelb und Weiß folgen dem tatsächlich angezeigten Signalbegriff. Die Darstellung ist pro Rechner einstellbar; Änderungen unten anwenden und speichern."));
            b.AddLabel(LocalText.Select("The clock-toolbar launcher remains available for settings, including before construction is unlocked.","Das Auswahlfenster an der Uhrleiste bleibt für Einstellungen erreichbar, auch vor der Baufreischaltung."));
            Toggle(b,LocalText.Get("Experimentelle AE-Signalanbindung einschalten"),()=>draft.Markers.EnableAiSignals,v=>draft.Markers.EnableAiSignals=v);
            Choose(b,LocalText.Get("Schutzwartezeit nach Rücknahme / Hilfsauflösung"),new double[]{0,5,10,15,20,30,60,90,120,180,300},draft.Markers.ProtectionWaitSeconds,v=>draft.Markers.ProtectionWaitSeconds=v," s");
            b.AddLabel(LocalText.Get("AE-Anbindung registriert eigene Haupt-/Rangiersignale im Gleisnetz und aktiviert den Fahrstraßenprototyp. Reale KI-Reaktion noch zu prüfen. Aktive Fahrstraßen vor Abschalten sicher auflösen."));
            Toggle(b,LocalText.Get("Spieler- und erkannte Karten-Geschwindigkeitsschilder an Einheit anpassen"),()=>draft.Markers.AdaptExistingSpeedSigns,v=>draft.Markers.AdaptExistingSpeedSigns=v);
            b.AddLabel(LocalText.Get("Schildeinheit folgt nur lesend der Auswahl Geschwindigkeit des Übersetzungsmods. Ohne Einstellung: mph. Keine Änderung gespeicherter Spielgeschwindigkeiten."));
            b.AddButton(LocalText.Get("Baumenü öffnen"),()=>{message=Entry.MarkerCommand("open");b.SetNeedsRebuild();});
            b.AddButton(LocalText.Get("Stellwerksfenster öffnen"),Entry.OpenInterlocking);
            if(Game.State.StateManager.Shared!=null&&!Game.State.StateManager.IsHost)
                b.AddButton(LocalText.Get("Materialbestand ansehen"),Entry.OpenMaterials);
        }
        void PlacementSettings(UIPanelBuilder b) {
            b.AddLabel(LocalText.Get("Vorgaben für neue Signale, Schilder und Grenzzeichen. Bestehende Objekte behalten ihre gespeicherte Position."));
            if(Game.State.StateManager.Shared!=null&&!Game.State.StateManager.IsHost){b.AddLabel(LocalText.Get("Gemeinsame Bauvorgaben legt der Host fest. Seite: ")+(draft.Markers.DefaultRightSide?"rechts":"links")+" · Abstand: "+(draft.Markers.UseGameSpacing?"Spielstandard 2,30 m":draft.Markers.Lateral+" m")+" · Höhe: "+draft.Markers.Height+" m · Größe: "+draft.Markers.Scale+"×");return;}
            Toggle(b,LocalText.Get("In Blick-/Fahrtrichtung ausrichten"),()=>draft.Markers.FollowView,v=>draft.Markers.FollowView=v);
            b.AddField(LocalText.Get("Standardseite in Fahrtrichtung"),b.AddDropdown(new List<string>{LocalText.Get("Rechts"),LocalText.Get("Links")},draft.Markers.DefaultRightSide?0:1,i=>{draft.Markers.DefaultRightSide=i==0;dirty=true;}));
            b.AddFieldToggle(LocalText.Get("Seitenabstand wie Spielerschilder verwenden (2,30 m)"),()=>draft.Markers.UseGameSpacing,v=>{draft.Markers.UseGameSpacing=v;dirty=true;b.SetNeedsRebuild();},true);
            if(!draft.Markers.UseGameSpacing)Choose(b,LocalText.Get("Standard-Seitenabstand"),new double[]{0,1.5,1.8,2,2.3,2.5,3,4},Math.Abs(draft.Markers.Lateral),v=>draft.Markers.Lateral=v," m");
            Choose(b,LocalText.Get("Standard-Höhe zum Gleis"),new double[]{-.4,-.25,-.15,0,.15,.3},draft.Markers.Height,v=>draft.Markers.Height=v," m");
            Choose(b,LocalText.Get("Modellgröße"),new double[]{.5,.75,1,1.5,2,3},draft.Markers.Scale,v=>draft.Markers.Scale=v,"×");
            b.AddLabel(LocalText.Get("Mit Anwenden & Speichern werden diese Vorgaben für die geladene Welt gespeichert. Eigene Abstände pro Mast: Objekt anklicken → Position → Host-Vorgaben benutzen ausschalten."));
            b.AddButton(LocalText.Get("Vorgaben speichern und auf gebundene Masten anwenden"),()=>{bool stored=false;try{save(draft);draft=read();dirty=false;stored=true;Entry.ApplyHostPlacement();message=LocalText.Get("Host-Vorgaben übernommen.");}catch(Exception e){message=(stored?"Vorgaben gespeichert, bestehende Masten unverändert: ":"")+e.Message;}b.SetNeedsRebuild();});
        }
        void Choose(UIPanelBuilder b,string label,double[] choices,double current,Action<double> set,string suffix) {
            int index=Enumerable.Range(0,choices.Length).OrderBy(i=>Math.Abs(choices[i]-current)).First();
            b.AddSearchChoice(label,choices.Select(v=>v.ToString("0.00")+suffix).ToList(),index,i=>{if(i>=0&&i<choices.Length){set(choices[i]);dirty=true;}});
        }
        void Toggle(UIPanelBuilder b,string text,Func<bool> get,Action<bool> set){b.AddFieldToggle(text,get,value=>{set(value);dirty=true;},true);}
    }
}
