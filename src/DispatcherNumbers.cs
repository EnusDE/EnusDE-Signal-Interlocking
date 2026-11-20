using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed class NumberFieldIndex {
        readonly Dictionary<string,HashSet<PanelTile>> owners=new Dictionary<string,HashSet<PanelTile>>(StringComparer.Ordinal);
        readonly Dictionary<PanelTile,string[]> bindings=new Dictionary<PanelTile,string[]>();
        readonly Dictionary<string,MarkerRecord[]> signals;
        readonly IReadOnlyDictionary<string,TrackBlock> blocks;

        public NumberFieldIndex(DeskLibrary desks,IEnumerable<MarkerRecord> inventory) {
            blocks=desks?.Blocks??new Dictionary<string,TrackBlock>();
            signals=inventory.Where(s=>s.Kind==MarkerRecord.Main&&s.Anchor!=null)
                .GroupBy(s=>s.Anchor.Segment).ToDictionary(g=>g.Key,g=>g.ToArray(),StringComparer.Ordinal);
            foreach(var field in desks?.Desks.SelectMany(d=>d.Panel.Tiles).Where(t=>t.Kind==PanelTileKind.TrainNumber&&!NumberMirrors.IsMirror(t))??Enumerable.Empty<PanelTile>()) {
                var ids=Resolve(field);
                bindings[field]=ids;
                if(ids.Length==0)continue;
                foreach(string id in ids) {
                    if(!owners.TryGetValue(id,out var keys))owners[id]=keys=new HashSet<PanelTile>();
                    keys.Add(field);
                }
            }
        }

        string[] Resolve(PanelTile field) {
            if(NumberMirrors.IsMirror(field))return Array.Empty<string>();
            if(!string.IsNullOrEmpty(field.Reference))return string.IsNullOrEmpty(field.AlternateSignal)?
                new[]{field.Reference}:new[]{field.Reference,field.AlternateSignal};
            IEnumerable<DirectedRouteSection> spans;
            if(field.Section!=null)spans=new[]{new DirectedRouteSection{Section=field.Section}};
            else if(field.BlockId!=""&&blocks.TryGetValue(field.BlockId,out var block)&&block.Point==null)spans=block.Path;
            else return Array.Empty<string>();
            var matches=new Dictionary<bool,HashSet<string>>();
            foreach(var span in spans) {
                if(!signals.TryGetValue(span.Section.Segment,out var onTrack))continue;
                foreach(var signal in onTrack) {
                    if(signal.Anchor.Meters<span.Section.From-.25||signal.Anchor.Meters>span.Section.To+.25)continue;
                    bool direction=signal.FacingReverse!=span.Reverse;
                    if(!matches.TryGetValue(direction,out var ids))matches[direction]=ids=new HashSet<string>();
                    ids.Add(signal.Id);
                }
            }
            if(matches.Values.Any(ids=>ids.Count!=1))return Array.Empty<string>();
            return matches.Values.SelectMany(ids=>ids).Distinct().OrderBy(id=>id,StringComparer.Ordinal).ToArray();
        }

        public string ForSignal(string id)=>!string.IsNullOrEmpty(id)&&owners.TryGetValue(id,out var keys)&&keys.Count==1?ForField(keys.Single()):null;
        public bool ConflictingField(PanelTile field)=>Resolve(field).Any(id=>owners.TryGetValue(id,out var keys)&&keys.Count>1);

        public string ForField(PanelTile field) {
            var ids=bindings.TryGetValue(field,out var known)?known:Resolve(field);
            if(ids.Length==0)return null;
            return ids.Any(id=>owners.TryGetValue(id,out var keys)&&(keys.Count!=1||bindings[keys.Single()][0]!=ids[0]))?null:ids[0];
        }
    }

    public sealed partial class SignalRuntime {
        long numberLinkVersion=-1;
        long numberInstallVersion=-1;
        DeskLibrary numberLinkDesks;
        NumberFieldIndex numberIndex;
        bool numberMoveFailed;

        NumberFieldIndex NumberBindings(SharedRuntime shared) {
            if(numberIndex==null||numberLinkVersion!=shared.ConfigurationVersion||
                numberInstallVersion!=shared.InstallationVersion||numberLinkDesks!=desks) {
                numberIndex=new NumberFieldIndex(desks,inventory.Values);
                numberLinkVersion=shared.ConfigurationVersion;
                numberInstallVersion=shared.InstallationVersion;
                numberLinkDesks=desks;
            }
            return numberIndex;
        }

        string NumberFieldKey(PanelTile tile) {
            var shared=Entry.Shared;
            if(shared==null)return null;
            string key=NumberBindings(shared).ForField(tile);
            return Object(key)?.Kind==MarkerRecord.Main?key:null;
        }

        void ReadNumberPassages(SharedRuntime shared) {
            var passages=engine.Active.Where(TrainNumbers.IsNumberPassage).ToArray();
            if(passages.Length==0)return;
            try {
                var index=NumberBindings(shared);
                var linked=passages.Where(r=>Object(r.Route.Entry)?.Kind==MarkerRecord.Main&&
                    Object(r.Route.Exit)?.Kind==MarkerRecord.Main&&index.ForSignal(r.Route.Entry)!=null&&
                    index.ForSignal(r.Route.Exit)!=null).ToArray();
                if(linked.Length>0) {
                    var completed=new HashSet<string>(shared.AdvanceFieldNumbers(linked,index.ForSignal));
                    foreach(var run in linked)if(completed.Contains(run.Route.Id))run.NumberPassageHandled=true;
                }
                numberMoveFailed=false;
            } catch(Exception e) {
                if(!numberMoveFailed)log?.Warning("Zugnummernwechsel: "+e.Message);
                numberMoveFailed=true;
            }
        }

        string NumberFieldPlace(string key) {
            var shared=Entry.Shared;
            if(desks==null||shared==null)return SignalCaption(key);
            var bindings=NumberBindings(shared);
            foreach(var desk in desks.Desks.OrderBy(d=>d.IsMaster)) {
                var field=desk.Panel.Tiles.FirstOrDefault(t=>t.Kind==PanelTileKind.TrainNumber&&bindings.ForField(t)==key);
                if(field!=null)return desk.Code+" / "+(string.IsNullOrWhiteSpace(field.Label)?SignalCaption(key):field.Label);
            }
            return SignalCaption(key);
        }

        void BuildDispatcherNumberField(UIPanelBuilder c,PanelTile tile) {
            if(NumberMirrors.IsMirror(tile)){BuildNumberMirrorStatus(c,tile);return;}
            string signal=NumberFieldKey(tile);
            if(signal==null) {
                if(NumberBindings(Entry.Shared).ConflictingField(tile)) {
                    c.AddLabel(LocalText.Select("Several independent number fields own this signal. Keep one source field and link the other displays as mirrors.","Mehrere eigenständige Zugnummernfelder sind demselben Signal zugeordnet. Ein Quellenfeld behalten und weitere Anzeigen als Anzeigekopie verknüpfen."));
                    return;
                }
                c.AddLabel(LocalText.Get("Keine eindeutige Zuordnung. Im Editor einen Abschnitt oder die Hauptsignale beider Richtungen wählen."));
                return;
            }
            string desk=deskId;
            var shared=Entry.Shared;
            string shown=shared?.FieldNumber(signal)??"";
            if(TrainNumbers.DisplayFieldNumber(shared?.NumberFields(),signal)=="?")c.AddLabel(LocalText.Select("This saved number has several field owners. Saving it here transfers ownership to this field and clears its stale duplicates.","Diese gespeicherte Nummer besitzt mehrere Quellenfelder. Erneutes Speichern hier übernimmt die Nummer in dieses Feld und entfernt alte Doppeleinträge."));
            string prefix=shown.Length==6?shown.Substring(0,1):"";
            string number=shown.Length==6?shown.Substring(1):shown;
            c.AddSection(LocalText.Get("Zugnummer · ")+Code(signal));
            c.AddLabel(LocalText.Get("Kennziffer (optional)"));
            c.AddInputField(prefix,v=>prefix=v,"",1);
            c.AddLabel(LocalText.Get("Zugnummer (5 Stellen)"));
            c.AddInputField(number,v=>number=v,"00123",5);
            c.AddButton(LocalText.Get("Zugnummer speichern"),()=>Act(()=>{
                RequireDispatcher();
                var current=panel?.At(tile.X,tile.Y);
                if(deskId!=desk||selectedX!=tile.X||selectedY!=tile.Y||current?.Kind!=PanelTileKind.TrainNumber||
                    NumberFieldKey(current)!=signal)
                    throw new InvalidOperationException("Zugnummernfeld erneut auswählen.");
                if(shared.FieldNumber(signal)!=shown)
                    throw new InvalidOperationException("Die Zugnummer ist inzwischen weitergewandert oder geändert worden. Feld erneut auswählen.");
                var moved=shared.WriteFieldNumber(signal,prefix,number);
                var owners=moved.fields.Select(NumberFieldPlace).ToList();
                if(moved.cars>0)owners.Add("bisheriger Zugverband");
                status=owners.Count==0?NumberFieldPlace(signal)+" · Zugnummer aktualisiert.":
                    "Zugnummer "+number+" von "+string.Join(", ",owners.Take(3))+(owners.Count>3?" (und weitere)":"")+
                    " nach "+NumberFieldPlace(signal)+" übernommen.";
                RefreshWorkspaceFields();
                panelView?.Refresh();
            }));
        }
    }
}
