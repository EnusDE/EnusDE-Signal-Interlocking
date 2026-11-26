using System;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string diagramCircuitId;
        bool DiagramBoundary(PanelTile tile,string id)=>tile.CircuitBoundary==id||tile.Kind==PanelTileKind.Signal&&!tile.ProjectedMastFace&&AtSignal(tile,id)||
            tile.Kind==PanelTileKind.Switch&&PointCircuitVisibility.Enabled(desks,tile.Reference)&&Enumerable.Range(0,3).Any(arm=>CanonicalBoundaryId(TrackCircuit.PointId(tile.Reference,arm))==CanonicalBoundaryId(id));
        void BuildExistingDiagramCircuit(UIPanelBuilder c,string desk,PanelTile field) {
            if(!CircuitDiagramAssignment.RailField(field))return;
            var choices=desks.Blocks.Values.Where(b=>b.IsCircuit).OrderBy(b=>b.Name,StringComparer.OrdinalIgnoreCase).ToArray();
            if(choices.Length==0)return;
            if(!choices.Any(b=>b.Id==diagramCircuitId))diagramCircuitId=choices.FirstOrDefault(b=>b.Id==field.BlockId)?.Id??choices[0].Id;
            c.AddSearchChoice(LocalText.Get("Vorhandener vollständiger Abschnitt"),choices.Select(b=>b.Name).ToList(),Array.FindIndex(choices,b=>b.Id==diagramCircuitId),i=>diagramCircuitId=choices[i].Id);
            c.AddLabel(LocalText.Get("Ein Gleis- oder Zugnummernfeld dazwischen genügt. Übernommen wird der eindeutige verbundene Gleisbildweg einschließlich Zugnummernfeld und der inneren Signalseiten. Abzweigungen und fremde Grenzen werden nicht geraten."));
            int x=field.X,y=field.Y;
            c.AddButton(LocalText.Get("Vollständigen Abschnitt übernehmen"),()=>Act(()=>{
                RequirePanelEdit();
                if(deskId!=desk)throw new InvalidOperationException("Gleisfeld erneut auswählen.");
                if(!desks.Blocks.TryGetValue(diagramCircuitId,out var block)||!block.IsCircuit||
                    !CircuitEndsMatch(block,CircuitEndpoint(block.BoundaryA),CircuitEndpoint(block.BoundaryB)))
                    throw new InvalidOperationException("Abschnittsgrenzen geändert oder nicht verfügbar. Gleisverlauf neu suchen.");
                int count=0;
                EditPanel(doc=>count=CircuitDiagramAssignment.Assign(doc,x,y,block,DiagramBoundary,CanonicalBoundaryId));
                ReloadCircuitDraft();status=LocalText.Get("Vollständigen Abschnitt mit ")+count+" Feldern einschließlich Signalseiten übernommen · "+block.Name+".";
            }));
        }
    }
}
