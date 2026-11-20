using System;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        void BuildNumberField(UIPanelBuilder c,PanelDraftBinding binding) {
            var draft=binding.Draft;
            BuildNumberMirrorEditor(c,binding);if(NumberMirrors.IsMirror(draft))return;
            c.AddLabel(LocalText.Get("Kennziffer (optional, 1 Stelle)"));
            c.AddInputField(draft.Prefix,v=>binding.Do(()=>draft.Prefix=v),"",1);
            c.AddLabel(LocalText.Get("Zugnummer (5 Stellen)"));
            c.AddInputField(draft.Number,v=>binding.Do(()=>draft.Number=v),"00123",5);
            var blocks=desks.Blocks.Values.Where(b=>b.Point==null).OrderBy(b=>b.Name).ToArray();c.AddSearchChoice(LocalText.Get("Mitlaufender Abschnitt"),new[]{"Ohne Bindung: manueller Text"}.Concat(blocks.Select(b=>b.Name)).ToList(),Array.FindIndex(blocks,b=>b.Id==draft.BlockId)+1,i=>binding.Do(()=>draft.BlockId=i==0?"":blocks[i-1].Id));
            c.AddButton(LocalText.Get("Nummer dem ausgewählten Zug zuweisen"),()=>binding.Do(()=>Act(()=>{int moved=Entry.Shared.AssignNumber(TrainController.Shared?.SelectedCar,draft.Prefix,draft.Number);status=moved>0?"Zugnummer auf den gewählten Verband übernommen. Frühere Zuordnung entfernt.":"Zugnummernzuordnung des gewählten Verbands gespeichert.";})));
            c.AddLabel(LocalText.Get("Vorschau für den Editor. Ein gebundener Abschnitt kann eindeutige Hauptsignale automatisch zuordnen. Bei Bedarf unten die Signale für beide Richtungen wählen."));
        }
    }
}
