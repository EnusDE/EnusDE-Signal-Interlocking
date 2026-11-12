using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed class SettingsMenu {
        readonly UIState<bool> expanded=new UIState<bool>(false);
        readonly UIState<bool> markerExpanded=new UIState<bool>(false);
        readonly UIState<bool> placementExpanded=new UIState<bool>(true);
        readonly Dictionary<string,UIState<bool>> sections=new Dictionary<string,UIState<bool>>();
        readonly Func<DiagnosticSettings> read;
        readonly Action<DiagnosticSettings> save;
        readonly string path;
        DiagnosticSettings draft;
        bool dirty;
        string message="Änderungen mit Anwenden & Speichern übernehmen.";
        public SettingsMenu(Func<DiagnosticSettings> read,Action<DiagnosticSettings> save,string path) {
            this.read=read;this.save=save;this.path=path;draft=read();
            foreach(string key in DiagnosticSettings.Categories)sections[key]=new UIState<bool>(false);
        }
        public void Build(UIPanelBuilder builder){builder.AddExpandable("EnusDE Signaltechnik",expanded,Content,ExpandableStyle.SectionHeader);}
        public static string Caption(string key) {
            switch(key){case "vehicles":return "Züge und Fahrzeuge";case "signals":return "Signale";case "blocks":return "Belegungen und Stellwerk";case "switches":return "Weichen";case "signs":return "Schilder";case "game_log":return "Relevante Spielmeldungen";case "performance":return "Leistung";default:return key;}
        }
        void Content(UIPanelBuilder b) {
            if(!dirty)draft=read();
            b.AddLabel("Signaltechnik · v"+Entry.Version);
            b.AddExpandable("Bauvorgaben – Abstand, Höhe und Seite",placementExpanded,PlacementSettings,ExpandableStyle.SectionHeader);
            var shortcutNames=new[]{"Rücktaste","Bild auf","Bild ab","Pos1","Ende","Einfg","Aus"};
            b.AddField("Signalliste zuklappen",b.AddDropdown(shortcutNames.ToList(),
                Array.IndexOf(DiagnosticSettings.SearchCloseKeys,draft.SearchCloseKey),
                i=>{draft.SearchCloseKey=DiagnosticSettings.SearchCloseKeys[i];dirty=true;}));
            b.AddLabel("Optionale lokale Diagnose");
            b.AddLabel("Erfassen schaltet Messung und Aufzeichnung. Anzeigen blendet Informationen im Diagnosefenster ein. Abgeschaltete Erfassung wird dort als aus angezeigt.");
            Toggle(b,"Hintergrundaufzeichnung",()=>draft.Recording,v=>draft.Recording=v);
            Toggle(b,"Auf diesem Rechner auch als Mitspieler aufzeichnen",()=>draft.RecordOnClients,v=>draft.RecordOnClients=v);
            Toggle(b,"Aufzeichnungsstatus anzeigen",()=>draft.ShowRecorder,v=>draft.ShowRecorder=v);
            foreach(string category in DiagnosticSettings.Categories) {
                string key=category;
                b.AddExpandable(Caption(key),sections[key],content=>Category(content,key),ExpandableStyle.SectionHeader);
            }
            b.AddExpandable("Objekte und Schilder – P0",markerExpanded,MarkerSettings,ExpandableStyle.SectionHeader);
            b.AddLabel("Schilder: Bestand und Verwaltungsereignisse. Interne Zug-/Schildauslöser sind derzeit nicht zugänglich.");
            b.AddLabel("Nur aktive Datenbereiche werden gemessen. Hohe Häufigkeit und viele Fahrzeuge können zusätzliche Last verursachen.");
            b.AddButton("Anwenden & Speichern",()=>{try{save(draft);draft=read();dirty=false;message="Gespeichert. Auswahl gilt sofort und beim nächsten Spielstart.";}catch(Exception e){message="Nicht übernommen: "+e.Message;}b.SetNeedsRebuild();});
            b.AddButton("Gespeicherte Auswahl laden",()=>{draft=read();dirty=false;message="Aktuell gespeicherte Auswahl geladen.";b.SetNeedsRebuild();});
            b.AddButton("Standardauswahl vorbereiten",()=>{draft=new DiagnosticSettings();dirty=true;message="Standardauswahl vorbereitet; zum Übernehmen speichern.";b.SetNeedsRebuild();});
            b.AddLabel(message);b.AddLabel("Einstellungsdatei: "+path);
            b.AddLabel("Diagnosefenster: /enus_signal · Status: /enus_signal_status");
        }
        void Category(UIPanelBuilder b,string key) {
            var group=draft[key];
            Toggle(b,"Erfassen",()=>group.Enabled,v=>group.Enabled=v);
            Toggle(b,"Anzeigen",()=>group.Visible,v=>group.Visible=v);
            var intervals=key=="performance"?new double[]{5,10,30,60}:new double[]{1,2,5,10,30,60};
            int selected=Array.FindIndex(intervals,x=>x>=group.IntervalSeconds);if(selected<0)selected=intervals.Length-1;
            b.AddField(key=="game_log"?"Meldungen bündeln":"Messabstand",b.AddDropdown(intervals.Select(x=>x+" Sekunden").ToList(),selected,i=>{if(i>=0&&i<intervals.Length){group.IntervalSeconds=intervals[i];dirty=true;}}));
            if(key!="game_log"&&key!="performance")b.AddField("Nur diese IDs",b.AddInputField(group.Ids,value=>{group.Ids=value;dirty=true;},"Leer = gesamter gewählter Bereich; IDs mit Komma trennen",null));
            if(key=="vehicles") {
                Toggle(b,"Nur Lokomotiven",()=>draft.LocomotivesOnly,v=>draft.LocomotivesOnly=v);
                Toggle(b,"Lok-Bedienwerte miterfassen",()=>draft.VehicleControls,v=>draft.VehicleControls=v);
                Toggle(b,"Geschwindigkeit anzeigen",()=>draft.ShowSpeed,v=>draft.ShowSpeed=v);
                Toggle(b,"Gleisposition vorne/hinten anzeigen",()=>draft.ShowPositions,v=>draft.ShowPositions=v);
            }
            if(key=="signs")b.AddLabel("Verwaltungsrückrufe werden bei aktiver Erfassung ereignisbezogen protokolliert; der Messabstand gilt für Bestandsvergleiche.");
        }
        void MarkerSettings(UIPanelBuilder b) {
            b.AddLabel("Automatische Gleisbild-Vorschläge werden überarbeitet. Diese Version zeichnet oder ersetzt keine Tafel automatisch; die Abschnittserkennung bleibt aktiv.");
            if(Game.State.StateManager.Shared==null||Game.State.StateManager.IsHost){Toggle(b,"Materialwirtschaft für neue Anlagen",()=>draft.Markers.EconomyEnabled,v=>draft.Markers.EconomyEnabled=v);Choose(b,"Materialbedarf-Faktor",new double[]{.25,.5,1,1.5,2,3,4},draft.Markers.MaterialFactor,v=>draft.Markers.MaterialFactor=v,"");}
            else b.AddLabel("Materialwirtschaft und Bedarf werden vom Host festgelegt.");
            b.AddButton("Materiallager und Bauaufträge öffnen",Entry.OpenMaterials);
            Toggle(b,"Eigene Objekte und Signalmodelle anzeigen",()=>draft.Markers.Visible,v=>draft.Markers.Visible=v);
            b.AddSection("Signallampen – lokale Darstellung");
            Toggle(b,"Signallinsen selbst leuchten lassen",()=>draft.Markers.Glow.Enabled,v=>draft.Markers.Glow.Enabled=v);
            Choose(b,"Helligkeit der Signallinsen",new double[]{1,2,4,6,8,12,18,24},draft.Markers.Glow.Radiance,v=>draft.Markers.Glow.Radiance=v,"×");
            Toggle(b,"Farbigen Lichthof anzeigen",()=>draft.Markers.Glow.Halo,v=>draft.Markers.Glow.Halo=v);
            Choose(b,"Stärke des Lichthofs",new double[]{.1,.25,.5,.75,1,1.5,2},draft.Markers.Glow.HaloStrength,v=>draft.Markers.Glow.HaloStrength=v,"×");
            b.AddLabel("Rot, Grün, Gelb und Weiß folgen dem tatsächlich angezeigten Signalbegriff. Die Darstellung ist pro Rechner einstellbar; Änderungen unten anwenden und speichern.");
            Toggle(b,"Signaltechnik-Symbol an der Uhrleiste anzeigen",()=>draft.Markers.ShowBuildButton,v=>draft.Markers.ShowBuildButton=v);
            Toggle(b,"Experimentelle AE-Signalanbindung einschalten",()=>draft.Markers.EnableAiSignals,v=>draft.Markers.EnableAiSignals=v);
            Choose(b,"Schutzwartezeit nach Rücknahme / Hilfsauflösung",new double[]{0,5,10,15,20,30,60,90,120,180,300},draft.Markers.ProtectionWaitSeconds,v=>draft.Markers.ProtectionWaitSeconds=v," s");
            b.AddLabel("AE-Anbindung registriert eigene Haupt-/Rangiersignale im Gleisnetz und aktiviert den Fahrstraßenprototyp. Reale KI-Reaktion noch zu prüfen. Aktive Fahrstraßen vor Abschalten sicher auflösen.");
            Toggle(b,"Spieler- und erkannte Karten-Geschwindigkeitsschilder an Einheit anpassen",()=>draft.Markers.AdaptExistingSpeedSigns,v=>draft.Markers.AdaptExistingSpeedSigns=v);
            b.AddLabel("Schildeinheit folgt nur lesend der Auswahl Geschwindigkeit des Übersetzungsmods. Ohne Einstellung: mph. Keine Änderung gespeicherter Spielgeschwindigkeiten.");
            b.AddButton("Baumenü öffnen",()=>{message=Entry.MarkerCommand("open");b.SetNeedsRebuild();});
            b.AddButton("Stellwerksfenster öffnen",Entry.OpenInterlocking);
            if(Game.State.StateManager.Shared!=null&&!Game.State.StateManager.IsHost)
                b.AddButton("Materialbestand ansehen",Entry.OpenMaterials);
        }
        void PlacementSettings(UIPanelBuilder b) {
            b.AddLabel("Vorgaben für neue Signale, Schilder und Grenzzeichen. Bestehende Objekte behalten ihre gespeicherte Position.");
            if(Game.State.StateManager.Shared!=null&&!Game.State.StateManager.IsHost){b.AddLabel("Gemeinsame Bauvorgaben legt der Host fest. Seite: "+(draft.Markers.DefaultRightSide?"rechts":"links")+" · Abstand: "+(draft.Markers.UseGameSpacing?"Spielstandard 2,30 m":draft.Markers.Lateral+" m")+" · Höhe: "+draft.Markers.Height+" m · Größe: "+draft.Markers.Scale+"×");return;}
            Toggle(b,"In Blick-/Fahrtrichtung ausrichten",()=>draft.Markers.FollowView,v=>draft.Markers.FollowView=v);
            b.AddField("Standardseite in Fahrtrichtung",b.AddDropdown(new List<string>{"Rechts","Links"},draft.Markers.DefaultRightSide?0:1,i=>{draft.Markers.DefaultRightSide=i==0;dirty=true;}));
            b.AddFieldToggle("Seitenabstand wie Spielerschilder verwenden (2,30 m)",()=>draft.Markers.UseGameSpacing,v=>{draft.Markers.UseGameSpacing=v;dirty=true;b.SetNeedsRebuild();},true);
            if(!draft.Markers.UseGameSpacing)Choose(b,"Standard-Seitenabstand",new double[]{0,1.5,1.8,2,2.3,2.5,3,4},Math.Abs(draft.Markers.Lateral),v=>draft.Markers.Lateral=v," m");
            Choose(b,"Standard-Höhe zum Gleis",new double[]{-.4,-.25,-.15,0,.15,.3},draft.Markers.Height,v=>draft.Markers.Height=v," m");
            Choose(b,"Modellgröße",new double[]{.5,.75,1,1.5,2,3},draft.Markers.Scale,v=>draft.Markers.Scale=v,"×");
            b.AddLabel("Mit Anwenden & Speichern werden diese Vorgaben für die geladene Welt gespeichert. Eigene Abstände pro Mast: Objekt anklicken → Position → Host-Vorgaben benutzen ausschalten.");
            b.AddButton("Vorgaben speichern und auf gebundene Masten anwenden",()=>{bool stored=false;try{save(draft);draft=read();dirty=false;stored=true;Entry.ApplyHostPlacement();message="Host-Vorgaben übernommen.";}catch(Exception e){message=(stored?"Vorgaben gespeichert, bestehende Masten unverändert: ":"")+e.Message;}b.SetNeedsRebuild();});
        }
        void Choose(UIPanelBuilder b,string label,double[] choices,double current,Action<double> set,string suffix) {
            int index=Enumerable.Range(0,choices.Length).OrderBy(i=>Math.Abs(choices[i]-current)).First();
            b.AddField(label,b.AddDropdown(choices.Select(v=>v.ToString("0.00")+suffix).ToList(),index,i=>{if(i>=0&&i<choices.Length){set(choices[i]);dirty=true;}}));
        }
        void Toggle(UIPanelBuilder b,string text,Func<bool> get,Action<bool> set){b.AddFieldToggle(text,get,value=>{set(value);dirty=true;},true);}
    }
}
