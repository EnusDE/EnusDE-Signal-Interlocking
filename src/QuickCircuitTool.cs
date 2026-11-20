using System;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerRuntime {
        bool quickCircuitActive,quickCircuitReverse;
        int quickCircuitVariant=1;
        string quickCircuitWorld;
        void BuildQuickCircuitTool(UIPanelBuilder c) {
            c.AddSection(LocalText.Select("Detection boundary quick tool","GFM-Schnellwerkzeug"));
            c.AddLabel(LocalText.Select("Keep the tool active and click successive signals in the world. Existing IDs, numbers and signal positions are retained. Escape stops the tool.","Werkzeug aktiv lassen und nacheinander Signale in der Welt anklicken. Vorhandene IDs, Nummern und Signalpositionen bleiben erhalten. Escape beendet das Werkzeug."));
            c.AddSearchChoice(LocalText.Select("Click action","Klickaktion"),new System.Collections.Generic.List<string>{LocalText.Select("Enable A on signal / B on free box","A am Signal / B am freien Kasten aktivieren"),LocalText.Select("Enable one-sided C","Einseitiges C aktivieren"),LocalText.Select("Disable attached detection box","Angebauten GFM-Kasten deaktivieren")},quickCircuitVariant==3?1:quickCircuitVariant==0?2:0,i=>quickCircuitVariant=i==1?3:i==2?0:1);
            c.AddFieldToggle(LocalText.Select("Reverse C monitoring direction","C-Überwachungsrichtung umkehren"),()=>quickCircuitReverse,v=>quickCircuitReverse=v,true);
            c.AddField(LocalText.Select("Tool status","Werkzeugstatus"),()=>quickCircuitActive?LocalText.Select("Active: click a signal; Escape stops.","Aktiv: Signal anklicken; Escape beendet."):LocalText.Select("Inactive","Inaktiv"),UIPanelBuilder.Frequency.Periodic);
            c.AddButton(quickCircuitActive?LocalText.Select("Stop quick tool","Schnellwerkzeug beenden"):LocalText.Select("Activate quick tool","Schnellwerkzeug einschalten"),()=>Act(()=>{
                if(!CanEdit(out string why))throw new InvalidOperationException(why);
                quickCircuitActive=!quickCircuitActive;quickCircuitWorld=scope;edit.Cancel();scene.HidePreview();
                status=quickCircuitActive?LocalText.Select("Detection tool active. Click successive signals; Escape stops.","GFM-Werkzeug aktiv. Nacheinander Signale anklicken; Escape beendet."):LocalText.Select("Detection tool stopped.","GFM-Werkzeug beendet.");
                if(quickCircuitActive)CloseTool();else RebuildTool();
            }));
        }
        bool QuickCircuitClick(string id) {
            if(!quickCircuitActive)return false;
            Act(()=>{
                if(scope!=quickCircuitWorld||!CanEdit(out string why)){quickCircuitActive=false;throw new InvalidOperationException(LocalText.Select("World or permissions changed. Restart the quick tool.","Welt oder Rechte geändert. Schnellwerkzeug erneut starten."));}
                var row=records.FirstOrDefault(r=>r.Id==id);if(row==null||!row.IsSignal&&!row.IsCircuitBox)return;
                if(row.IsCircuitBox&&quickCircuitVariant==0)throw new InvalidOperationException(LocalText.Select("Remove free-standing boxes through their object menu.","Freie Kästen über ihr Objektmenü entfernen."));
                selectedId=id;RequireSelectedChange();
                repository.ConfigureCircuit(id,quickCircuitVariant,row.CircuitOffset,row.CircuitYaw,row.CircuitSupport,quickCircuitReverse);
                RefreshRecords();RefreshScene();selectedEditFor=null;
                status=LocalText.Select("Detection boundary updated: ","GFM-Grenze geändert: ")+row.Name;
                Entry.RecordMarkerEvent("quick_detection_boundary",records.First(r=>r.Id==id));
                RebuildTool();
            });
            return true;
        }
    }
}
