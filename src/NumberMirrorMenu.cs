using System;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        DeskLibrary numberMirrorLibrary;
        NumberMirrorIndex numberMirrors;
        string numberMirrorChoice;
        NumberMirrorIndex NumberMirrorBindings() {
            if(numberMirrors==null||numberMirrorLibrary!=desks){numberMirrors=new NumberMirrorIndex(desks);numberMirrorLibrary=desks;}
            return numberMirrors;
        }
        void BuildNumberMirrorStatus(UIPanelBuilder c,PanelTile tile) {
            c.AddSection(LocalText.Get("Zugnummer – Anzeigekopie"));
            c.AddField(LocalText.Get("Quelle"),()=>NumberMirrorBindings().Source(tile)?.Caption??"Quellenfeld fehlt oder wurde entfernt.",UIPanelBuilder.Frequency.Periodic);
            c.AddField(LocalText.Get("Aktuelle Anzeige"),()=>NumberCaption(tile),UIPanelBuilder.Frequency.Periodic);
            c.AddLabel(LocalText.Get("Schreibgeschützte Sicht auf das ursprüngliche Zugnummernfeld. Bearbeiten und Weitergeben erfolgen an der Quelle. Eine Anzeige erteilt keine Fahrtfreigabe."));
        }
        void BuildNumberMirrorEditor(UIPanelBuilder c,PanelDraftBinding binding) {
            var draft=binding.Draft;string selectedDesk=deskId,selectedScope=scope;
            c.AddSection(LocalText.Get("Zugnummernfeld aus anderer Tafel anzeigen"));
            if(NumberMirrors.IsMirror(draft)) {
                BuildNumberMirrorStatus(c,draft);
                c.AddButton(LocalText.Get("In normales Zugnummernfeld umwandeln"),()=>binding.Do(()=>Act(()=>{
                    RequirePanelEdit();if(deskId!=selectedDesk||scope!=selectedScope)throw new InvalidOperationException("Feld erneut auswählen.");
                    var normal=NumberMirrors.Detached(draft);EditPanel(doc=>doc.Put(normal));tileDraft=normal.Clone();LoadTileFields();status=LocalText.Get("Anzeigekopie gelöst. Quellenfeld und ursprüngliche Zugnummer bleiben erhalten.");RefreshWorkspaceFields();
                })));
            }
            var sources=desks.Desks.OrderByDescending(d=>d.IsMaster).ThenBy(d=>d.Code).SelectMany(d=>d.Panel.Tiles
                .Where(t=>t.Kind==PanelTileKind.TrainNumber&&!NumberMirrors.IsMirror(t)&&!(d.Id==selectedDesk&&t.X==draft.X&&t.Y==draft.Y))
                .OrderBy(t=>t.Y).ThenBy(t=>t.X).Select(t=>new NumberMirrorSource{Desk=d,Tile=t})).ToArray();
            if(sources.Length==0){c.AddLabel(LocalText.Get("Zuerst ein ursprüngliches Zugnummernfeld auf einer Tafel anlegen und speichern."));return;}
            string Key(NumberMirrorSource source)=>source.Desk.Id+":"+source.Tile.X+":"+source.Tile.Y;
            int choice=Math.Max(0,Array.FindIndex(sources,s=>Key(s)==numberMirrorChoice));numberMirrorChoice=Key(sources[choice]);
            c.AddSearchChoice(LocalText.Get("Quellenfeld – ESTW zuerst"),sources.Select(s=>s.Caption).ToList(),choice,i=>binding.Do(()=>numberMirrorChoice=Key(sources[i])));
            c.AddLabel(LocalText.Get("Die Kopie folgt der Nummer und späteren Bindungsänderungen der Quelle. Sie lässt sich unabhängig platzieren und wird auch bei geschlossener Quelltafel aktualisiert."));
            c.AddButton(NumberMirrors.IsMirror(draft)?"Quelle der Anzeigekopie ändern":"Anzeigekopie hier anlegen",()=>binding.Do(()=>Act(()=>{
                RequirePanelEdit();if(deskId!=selectedDesk||scope!=selectedScope)throw new InvalidOperationException("Feld erneut auswählen.");
                var source=sources.SingleOrDefault(s=>Key(s)==numberMirrorChoice)??throw new InvalidOperationException("Quellenfeld erneut auswählen.");
                var current=desks.Desks.FirstOrDefault(d=>d.Id==source.Desk.Id)?.Panel.At(source.Tile.X,source.Tile.Y);
                if(current==null||!current.SameContent(source.Tile))throw new InvalidOperationException("Quellenfeld inzwischen verschoben oder geändert. Auswahl neu öffnen.");
                var next=NumberMirrors.Link(desks,selectedDesk,draft,source.Desk.Id,source.Tile.X,source.Tile.Y);
                var before=panel;deskStore.Save(next);desks=next;panel=CurrentDesk.Panel;
                if(history!=null)try{history.Record(before,panel);}catch(Exception e){historyError="Anzeigekopie gespeichert; Verlauf nicht geschrieben: "+e.Message;}
                panelWorld?.Dispose();panelWorld=new SignalWorldBridge(this,transform,2048);panelReadings.Clear();panelReadTime=-100;nextPanelRead=0;
                tileDraft=panel.At(draft.X,draft.Y).Clone();LoadTileFields();panelView?.ReplaceDocument(panel);status=LocalText.Get("Zugnummern-Anzeigekopie angelegt · ")+source.Caption+". Keine zweite Nummernzuordnung erstellt.";RefreshWorkspaceFields();
            })));
        }
    }
}
