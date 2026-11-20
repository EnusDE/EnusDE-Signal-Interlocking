using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public static class PointCircuitVisibility {
        public static bool Enabled(DeskLibrary library,string id)=>id!=null&&library?.Points.TryGetValue(id,out var point)==true&&point.ShowCircuit;
        public static IEnumerable<string> VisibleIds(DeskLibrary library)=>library.Points.Where(p=>p.Value.ShowCircuit).Select(p=>p.Key);
    }
    public sealed class PointCircuitOption {public string Id,Code;public bool Enabled,Electric;}
    public sealed partial class SignalRuntime {
        internal PointCircuitOption[] PointCircuitOptions()=>desks?.Points.OrderBy(p=>p.Value.Code,StringComparer.Ordinal)
            .Select(p=>new PointCircuitOption{Id=p.Key,Code=p.Value.Code,Enabled=p.Value.ShowCircuit,Electric=PointDrivePolicy.Electric(desks,p.Key)}).ToArray()??Array.Empty<PointCircuitOption>();
        internal string InspectPointCircuit(string point) {
            var rows=PointBoundaries(point).Where(r=>r!=null).Select(r=>SharedPointCircuits().PointAlias(r)).ToArray();
            return rows.Length==3&&rows.All(r=>r.Name.StartsWith("GFM-",StringComparison.Ordinal))?string.Join("\n",rows.Select(r=>r.Name+" · "+r.Anchor.Segment+" · "+SignUnits.Number(r.Anchor.Meters)+" m ab Ende A")):
                "Drei Grenzen mit eindeutiger S/L/R-Zuordnung noch nicht vollständig erkannt. Erkennung anfordern und Anzeige aktualisieren.";
        }
        internal void ConfigurePointCircuit(string point,bool enabled,bool detect) {
            RequireEdit();
            if(!desks.Points.ContainsKey(point))throw new InvalidOperationException("Weiche nicht mehr verfügbar.");
            if(desks.Points[point].ShowCircuit!=enabled)RequireIdleAdministration();
            var next=desks.Clone();next.Points[point].ShowCircuit=enabled;deskStore.Save(next);desks=next;panel=CurrentDesk.Panel;
            if(detect){forceNetwork=true;nextNetwork=0;networkError=null;networkSignature=null;}
        }
        internal void EnableTrackPointCircuit(Track.Location at) {
            RequireEdit();string point=TrackPointAt(at);
            if(!desks.Points.ContainsKey(point)) {
                RequireIdleAdministration();var next=desks.Clone();PointAssignments.NameDiscovered(next,new[]{point});
                next.Points[point].ShowCircuit=true;deskStore.Save(next);desks=next;panel=CurrentDesk.Panel;
                forceNetwork=true;nextNetwork=0;networkError=null;networkSignature=null;
            }else ConfigurePointCircuit(point,true,true);
            status=WindowText("Point detection enabled; missing boundaries are being detected: ","Weichen-GFM aktiviert; fehlende Grenzen werden erkannt: ")+PointAssignments.Caption(desks,point);
            panelView?.Refresh();
        }
    }
    public sealed partial class MarkerRuntime {
        string circuitPointChoice;
        void OpenPointCircuit(string id) {
            if(!CanEdit(out string why))throw new InvalidOperationException(why);
            if(!Entry.PointCircuitOptions().Any(p=>p.Id==id))throw new InvalidOperationException(LocalText.Select("Point no longer available.","Weiche nicht mehr verfügbar."));
            edit.Cancel();scene.HidePreview();circuitPointChoice=id;page=Page.PointCircuits;ShowTool();
        }
        void BuildPointCircuitMenu(UIPanelBuilder c) {
            var choices=Entry.PointCircuitOptions();
            c.AddTitle(LocalText.Get("Weichen-Gleisfreimeldung und Antrieb"),"GFM und Spielmodell je Weiche wählen");
            if(choices.Length==0){c.AddLabel(LocalText.Get("Noch keine Weichen erkannt."));return;}
            if(!choices.Any(p=>p.Id==circuitPointChoice))circuitPointChoice=choices[0].Id;
            c.AddSearchChoice(LocalText.Get("Weiche"),choices.Select(p=>p.Code+(p.Enabled?" · GFM an":" · GFM aus")).ToList(),
                Array.FindIndex(choices,p=>p.Id==circuitPointChoice),i=>{circuitPointChoice=choices[i].Id;RebuildTool();});
            string point=circuitPointChoice,world=scope;var selected=choices.First(p=>p.Id==point);
            c.AddLabel(LocalText.Get("Standardmäßig ohne GFM. Aktivieren schaltet Kästen, Grenzvorschläge und die GFM-Anzeige der Weiche ein. Deaktivierte Weichen erscheinen gelb als unüberwacht; gespeicherte Zuordnungen und Modellversätze bleiben erhalten."));
            c.AddLabel(Entry.InspectPointCircuit(point));
            foreach(string signal in Entry.SharedPointSignals(point)) {
                string id=signal;
                c.AddButton(LocalText.Select("Open shared A/B box","Gemeinsamen A/B-Kasten öffnen")+" · "+Entry.MarkerCaption(id),()=>Entry.OpenMarker(id));
            }
            void Change(bool enabled,bool detect) {
                if(!CanEdit(out string why)||scope!=world)throw new InvalidOperationException(why??"Spielstand gewechselt.");
                Entry.ConfigurePointCircuit(point,enabled,detect);RefreshScene();RebuildTool();
                status=enabled?"Weichenkästen aktiviert. Erkannte Grenzen werden verwendet; fehlende Grenzen werden nach der Erkennung ergänzt.":"Weichen-GFM deaktiviert; Anzeige gelb/unüberwacht. Zuordnungen und Versätze bleiben gespeichert.";
            }
            c.AddButton(LocalText.Get("Grenzen erkennen und Kästen anzeigen"),()=>Act(()=>Change(true,true)));
            if(selected.Enabled)c.AddButton(LocalText.Get("Weichen-GFM deaktivieren"),()=>Act(()=>Change(false,false)));
            c.AddButton(LocalText.Get("Anzeige aktualisieren"),RebuildTool);
            c.AddSection(LocalText.Get("Weichenantrieb des Spiels"));
            c.AddLabel(LocalText.Select("Electric drives are protected against native waypoint-AI commands, including at Stop and with block coordination disabled. Driver and interlocking controls keep their normal lock checks. Native CTC rules remain in force.","Elektrische Antriebe sind gegen die native Wegpunkt-KI geschützt, auch bei Halt und ausgeschalteter Blockabstimmung. TF und Stellwerk behalten ihre normalen Verschlussprüfungen. Native CTC-Regeln bleiben erhalten."));
            void Model(bool electric,bool all) {
                if(!CanEdit(out string why)||scope!=world)throw new InvalidOperationException(why??"Spielstand gewechselt.");
                Entry.ConfigureSwitchModel(point,electric,all);RebuildTool();
                status=LocalText.Get("Weichenantriebsmodell gespeichert. Das Spiel baut die betroffenen Modelle neu auf.");
            }
            c.AddButton(selected.Electric?"Originalen Spielantrieb verwenden":"Elektrischen Spielantrieb verwenden",()=>Act(()=>Model(!selected.Electric,false)));
            c.AddButton(LocalText.Get("Alle Weichen dieses Stellwerks elektrisch darstellen"),()=>Act(()=>Model(true,true)));
            c.AddButton(LocalText.Get("Originalmodelle dieses Stellwerks wiederherstellen"),()=>Act(()=>Model(false,true)));
            BuildPointBoxPicker(c);
        }
    }
}
