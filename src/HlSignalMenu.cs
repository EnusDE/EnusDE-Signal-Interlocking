using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerRuntime {
        void BuildHlModelSelection(UIPanelBuilder c,MarkerRecord row) {
            c.AddField(LocalText.Select("Signal system","Signalsystem"),row.HlSignal?LocalText.Select("Hl · Ril 301.0103 · test","Hl · Ril 301.0103 · Test"):LocalText.Select("German H/V","Deutsch H/V"));
            if(!row.LowMain&&(row.HlSignal||options.EnableHlSignalsTest))c.AddButton(row.HlSignal?LocalText.Select("Convert to H/V","Auf H/V umrüsten"):LocalText.Select("Use Hl test model","Hl-Testmodell verwenden"),()=>Act(()=>{
                RequireSelectedChange();
                if(Selected?.Id!=row.Id)return;
                if(!row.HlSignal)HlTestFeature.Require(options.EnableHlSignalsTest);
                repository.ConfigureHlSignal(row.Id,!row.HlSignal);
                Saved(LocalText.Select("Signal model changed. IDs, detection and interlocking assignments retained.","Signalmodell geändert. IDs, Gleisfreimeldung und Stellwerkszuordnungen bleiben erhalten."));
            }));
            else if(row.LowMain&&options.EnableHlSignalsTest)c.AddLabel(LocalText.Select("Hl: convert to a high mast first.","Hl: zuerst auf einen hohen Mast umrüsten."));
            if(row.HlSignal) {
                c.AddLabel(LocalText.Select("Hl test: unrestricted and 40 km/h displays, including next-signal warning. 60/100 km/h are preview only. Native AI speed handling is unchanged and requires separate validation.","Hl-Test: Anzeigen ohne Signalbegrenzung und mit 40 km/h, einschließlich Vorankündigung. 60/100 km/h nur in der Vorschau. Die native KI-Geschwindigkeitsbehandlung bleibt unverändert und muss separat geprüft werden."));
                if(row.Kind==MarkerRecord.Distant)c.AddField(LocalText.Select("Current Hl aspect","Aktuelles Hl-Signalbild"),()=>HlLampPlan.Runtime(row.Kind,Entry.SignalAspect(row.Id),LocalSignalAspect.Stop,false).Code,UIPanelBuilder.Frequency.Periodic);
            }
        }
    }
}
