using System;
using System.Linq;
using System.Globalization;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        bool ReferencedAuxiliaryLocked(string id)=>desks?.Desks.SelectMany(d=>d.Panel.Tiles).Where(t=>t.Target?.ReferenceSignal==id).Any(t=>engine.UsesSignal(t.Target.Id)||lineDirections.States.Any(s=>s.Held&&s.Definition.Anchors.ContainsKey(t.Target.Id)))==true;
        void ProposeAuxiliaryReference(string id) {
            var proposed=new AuxiliaryTarget();AuxiliarySetup.Link(proposed,Object(id));
            targetReferenceInput=id;targetSegmentInput=proposed.Anchor.Segment;targetMetersInput=proposed.Anchor.Meters.ToString("R",CultureInfo.InvariantCulture);targetReverseInput=proposed.Reverse;
            status=WindowText("Reference selected. Review the opposite approach and save the field to confirm.","Bezug ausgewählt. Gegenrichtung prüfen und zum Bestätigen das Feld speichern.");
            RefreshWorkspaceFields();panelView?.Refresh();
        }
        void BuildAuxiliarySetup(UIPanelBuilder c,PanelDraftBinding binding,bool embedded) {
            c.AddSection(WindowText("Destination by clicking","Ziel durch Anklicken"));
            c.AddButton(WindowText("Select reference in diagram…","Bezug im Gleisbild auswählen …"),()=>binding.Do(()=>PickSignalInDiagram(deskId,WindowText("Select the entry signal used as the destination reference.","Das Einfahrsignal als Zielbezug auswählen."),id=>Object(id)?.Kind==MarkerRecord.Main&&!Object(id).Planned&&StationAutomationPolicy.Owner(desks,id)?.Id==deskId,id=>binding.Do(()=>ProposeAuxiliaryReference(id)))));
            if(targetReferenceInput!="")c.AddField(WindowText("Reference signal (opposite approach)","Bezugssignal (Anfahrt entgegen der Signalrichtung)"),Code(targetReferenceInput));
            c.AddLabel(WindowText("Select the entry signal at this station boundary. Its position is followed by stable identity; its opposite direction becomes the departure destination. Drawing rotation does not change the real approach. Saving the field confirms this proposal.","Einfahrsignal an dieser Bahnhofsgrenze wählen. Seine Position wird über die feste Kennung nachgeführt; die Gegenrichtung wird zum Ausfahrziel. Drehen der Zeichnung ändert die reale Anfahrt nicht. Feld speichern bestätigt diesen Vorschlag."));
            var entries=AuxiliarySetup.Entries(panel,binding.Draft,Object,id=>SignalRolePolicy.Current(desks,id));
            c.AddButton(WindowText("Find reference automatically","Bezug automatisch erkennen"),()=>binding.Do(()=>Act(()=>{
                if(entries.Length!=1)throw new InvalidOperationException(entries.Length==0?WindowText("No unambiguous connected entry signal. Select a reference in the world or use the list.","Kein eindeutig verbundenes Einfahrsignal. Bezug in der Welt oder über die Liste wählen."):WindowText("Several connected entry signals. Select the intended boundary from the list.","Mehrere verbundene Einfahrsignale. Gewünschte Grenze aus der Liste wählen."));
                ProposeAuxiliaryReference(entries[0]);
            })));
            var choices=SignalObjects.Where(s=>s.Kind==MarkerRecord.Main&&!s.Planned&&StationAutomationPolicy.Owner(desks,s.Id)?.Id==deskId)
                .OrderBy(s=>entries.Contains(s.Id)?0:1).ThenBy(s=>Code(s.Id)).ToArray();
            c.AddSearchChoice(WindowText("Reference signal / manual search","Bezugssignal / manuelle Suche"),new[]{WindowText("Choose…","Auswählen …")}.Concat(choices.Select(s=>Code(s.Id))).ToList(),0,i=>binding.Do(()=>{if(i>0)ProposeAuxiliaryReference(choices[i-1].Id);}));
            c.AddButton(WindowText("Select reference signal in world…","Bezugssignal in der Welt auswählen …"),()=>binding.Do(()=>Act(()=>{
                string originDesk=deskId,originScope=scope;int x=binding.Draft.X,y=binding.Draft.Y;
                var saved=panel.At(x,y)?.Clone();var draft=binding.Draft.Clone();
                BeginWorldSignalPick(id=>{
                    if(scope!=originScope||!EditorContextPolicy.SameTarget(panel,x,y,saved))throw new InvalidOperationException(WindowText("The target field changed. Start again.","Das Zielfeld wurde geändert. Erneut starten."));
                    OpenEditorField(x,y,EditorFieldCategory.Properties,false);tileDraft=draft;LoadTileFields();ProposeAuxiliaryReference(id);
                },()=>{ShowWorkspace(true,4,false);EnterEditorDesk(originDesk);});
            })));
        }
    }
}
