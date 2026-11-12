using System;
using System.Collections.Generic;
using System.Linq;
using Track;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string circuitA,circuitB,rememberedBoundary,rememberedWorld;
        readonly List<CircuitPath> circuitPaths=new List<CircuitPath>();
        Dictionary<string,MarkerRecord> physicalBoundaries=new Dictionary<string,MarkerRecord>();
        int circuitPath;
        int? circuitSide;

        static int LocalPort(PanelTile tile,int port) {
            int mask=PanelConnections.Transform(1<<port,tile.Rotation,tile.Mirror,true);
            return Enumerable.Range(0,8).First(i=>(mask&(1<<i))!=0);
        }

        bool AtSignal(PanelTile tile,string boundary) {
            var marker=CircuitEndpoint(boundary);
            if(marker==null)return false;
            foreach(string id in new[]{tile.Reference,tile.AlternateSignal}) {
                var signal=Object(id);
                if(signal!=null&&(signal.PhysicalId??signal.Id)==(marker.PhysicalId??marker.Id))return true;
            }
            return false;
        }

        internal MarkerRecord CircuitEndpoint(string id) {
            if(string.IsNullOrEmpty(id))return null;
            if(TrackCircuit.TryPointId(id,out var point,out int arm))return PointBoundaries(point).ElementAtOrDefault(arm);
            if(physicalBoundaries.TryGetValue(id,out var physical))return CircuitIdentity.Enabled(physical)?physical:null;
            var signal=Object(id);
            return signal?.CircuitNumber>0&&!CircuitIdentity.Enabled(signal)?null:signal;
        }

        MarkerRecord[] PointBoundaries(string point) {
            var result=new MarkerRecord[3];
            var graph=Graph.Shared;
            var node=graph?.GetNode(point);
            if(desks==null||node==null||!desks.Blocks.TryGetValue("p:"+point,out var block)||
                !graph.IsSwitch(node)||!graph.DecodeSwitchAt(node,out var common,out var normal,out var branch))return result;
            var segments=new[]{common,normal,branch};
            for(int i=0;i<segments.Length;i++) {
                var segment=segments[i];
                var guard=block.Guards.FirstOrDefault(g=>g.Segment==segment?.id);
                if(guard==null)continue;
                result[i]=new MarkerRecord{Id=TrackCircuit.PointId(point,i),Kind=MarkerRecord.CircuitA,
                    Name=PointAssignments.Caption(desks,point)+" · "+TrackCircuit.PointArms[i],
                    Anchor=new MarkerAnchor{Segment=segment.id,Meters=segment.a==node?guard.To:guard.From}};
            }
            return result;
        }

        string BoundaryCaption(string id) {
            var marker=CircuitEndpoint(id);
            return marker==null?"Grenze fehlt":CircuitIdentity.Caption(marker);
        }

        IEnumerable<SignalSearchEntry> BoundaryChoices(bool boxesOnly) {
            foreach(var marker in physicalBoundaries.Values.Where(CircuitIdentity.Enabled))
                if(!boxesOnly||marker.IsCircuitBox)
                    yield return new SignalSearchEntry(marker.Id,CircuitIdentity.Caption(marker),marker.IsSignal?marker.DisplayTitle:marker.Name);
            if(boxesOnly)yield break;
            foreach(var tile in panel.Tiles.Where(t=>t.Kind==PanelTileKind.Switch&&t.Reference!="").GroupBy(t=>t.Reference).Select(g=>g.First()))
                foreach(var endpoint in PointBoundaries(tile.Reference))
                    if(endpoint!=null)yield return new SignalSearchEntry(endpoint.Id,endpoint.Name,"Weiche");
        }

        internal void RememberCircuitBoundary(string id) {
            RequireEdit();
            if(!TrackCircuit.IsBoundary(CircuitEndpoint(id)))throw new InvalidOperationException("Abschnittsgrenze noch nicht verfügbar.");
            rememberedBoundary=id;
            rememberedWorld=scope;
        }

        void ResetCircuitSelection() {
            circuitA=circuitB=null;
            circuitSide=null;
            circuitPaths.Clear();
            circuitPath=0;
        }

        static bool CircuitField(PanelTile tile)=>tile!=null&&
            (tile.Kind==PanelTileKind.Straight||tile.Kind==PanelTileKind.Diagonal||tile.Kind==PanelTileKind.Curve||tile.Kind==PanelTileKind.TrainNumber);
        static bool TerminalField(PanelTile tile)=>CircuitField(tile)&&PanelConnections.Count(PanelConnections.Ports(tile))==1;

        void BoundarySearch(UIPanelBuilder c,string label,string selected,SignalSearchEntry[] choices,Action<string> select) {
            c.AddLabel(label+" · "+(string.IsNullOrEmpty(selected)?"keine":BoundaryCaption(selected)));
            var input=c.AddInputField("",_=>{},"Kennung oder Name suchen",64).GetComponent<TMP_InputField>();
            var picker=c.VStack(_=>{}).gameObject.AddComponent<SignalPickerList>();
            var toggle=c.VStack(_=>{});
            var size=toggle.GetComponent<LayoutElement>()??toggle.gameObject.AddComponent<LayoutElement>();
            size.minHeight=size.preferredHeight=32;size.layoutPriority=10;size.flexibleHeight=0;
            var fitter=toggle.GetComponent<ContentSizeFitter>();if(fitter!=null)fitter.enabled=false;
            var button=EditorSurface.Button(toggle,"Treffer anzeigen / schließen",picker.Toggle);
            (button.GetComponent<LayoutElement>()??button.gameObject.AddComponent<LayoutElement>()).ignoreLayout=true;
            WorkspaceSurface.Fill(button.GetComponent<UnityEngine.RectTransform>());
            picker.Initialize(input,button.transform,choices,"Keine passenden Grenzen gefunden.",entry=>select(entry.Id),
                Enum.TryParse(Entry.SearchCloseKey,out Key key)?key:Key.Backspace);
        }

        void BuildBoundarySymbol(UIPanelBuilder c,PanelDraftBinding binding) {
            var tile=binding.Draft;
            c.AddSection(TerminalField(tile)?"Stumpfgleis · Grenzmarke":"Grenzmarke im Gleisbild");
            var choices=BoundaryChoices(TerminalField(tile)).ToArray();
            BoundarySearch(c,"Grenzmarke",tile.CircuitBoundary,choices,id=>binding.Do(()=> {
                tile.CircuitBoundary=id;RefreshWorkspaceFields();panelView?.Refresh();
            }));
            if(tile.CircuitBoundary!=null)c.AddButton("Grenzmarke lösen",()=>binding.Do(()=>{
                tile.CircuitBoundary=null;RefreshWorkspaceFields();panelView?.Refresh();
            }));
        }

        void BuildCircuitEditor(UIPanelBuilder c,PanelRegion selected) {
            string desk=deskId;
            var field=selected.Width==1&&selected.Height==1?panel.At(selected.X,selected.Y):null;
            var choices=BoundaryChoices(false).ToArray();
            if(choices.Length==0)return;
            void Pick(string id,bool first) {
                if(deskId!=desk||!editingWorkspace)return;
                if(first)circuitA=id;else circuitB=id;
                circuitPaths.Clear();
                circuitPath=0;
                RefreshWorkspaceFields();
            }
            c.AddSection("Gleisfreimeldung");
            string linked=field?.BlockId;
            int? selectedSide=null;
            if(field?.Kind==PanelTileKind.Signal) {
                int ports=PanelConnections.Ports(field);
                var sides=Enumerable.Range(0,8).Where(i=>(ports&(1<<i))!=0).Select(i=>(int?)i).ToList();
                if(field.AlternateSignal!=null&&field.AlternateSignal!=field.Reference)sides.Add(null);
                if(sides.Count==0)return;
                if(!sides.Contains(circuitSide))circuitSide=sides[0];
                selectedSide=circuitSide;
                c.AddField("Ausleuchtung",c.AddDropdown(sides.Select(i=>i.HasValue?
                    "Vom Signal nach "+PanelConnections.Names[i.Value]:"Zwischen beiden Signalen").ToList(),
                    sides.IndexOf(circuitSide),i=>{circuitSide=sides[i];RefreshWorkspaceFields();}));
                if(selectedSide.HasValue) {
                    linked=null;
                    field.CircuitRails?.TryGetValue(LocalPort(field,selectedSide.Value),out linked);
                }
                var signal=Object(field.Reference);
                var key=signal?.PhysicalId??signal?.Id;
                if(key!=null&&choices.Any(choice=>choice.Id==key))
                    c.AddButton("Signal am Feld als Grenze 1",()=>Pick(key,true));
            }
            if(linked!=null&&desks.Blocks.TryGetValue(linked,out var bound)&&bound.IsCircuit) {
                c.AddLabel("Zugeordnet · "+bound.Name);
                if(!TrackCircuit.EndsMatch(bound,CircuitEndpoint(bound.BoundaryA),CircuitEndpoint(bound.BoundaryB)))
                    c.AddLabel("Eine Grenze fehlt oder wurde verschoben. Abschnitt neu zuordnen.");
            }
            var firstChoices=TerminalField(field)?BoundaryChoices(true).ToArray():choices;
            BoundarySearch(c,"Grenze 1",circuitA,firstChoices,id=>Pick(id,true));
            BoundarySearch(c,"Grenze 2",circuitB,choices,id=>Pick(id,false));
            var pin=rememberedWorld==scope?choices.FirstOrDefault(choice=>choice.Id==rememberedBoundary):null;
            if(pin!=null) {
                c.AddLabel("In der Spielwelt vorgemerkt · "+pin.Caption);
                if(firstChoices.Any(choice=>choice.Id==pin.Id))c.AddButton("Als Grenze 1 übernehmen",()=>Pick(pin.Id,true));
                c.AddButton("Als Grenze 2 übernehmen",()=>Pick(pin.Id,false));
            }
            c.AddButton("Gleisverlauf suchen",()=>Act(()=>{
                RequirePanelEdit();
                var a=CircuitEndpoint(circuitA);
                var b=CircuitEndpoint(circuitB);
                if(deskId!=desk||!TrackCircuit.IsBoundary(a)||!TrackCircuit.IsBoundary(b)||TerminalField(field)&&!a.IsCircuitBox)
                    throw new InvalidOperationException("Zwei Freimeldegrenzen auswählen.");
                circuitPaths.Clear();
                circuitPaths.AddRange(TrackCircuit.Find(CaptureNetwork(),a.Anchor,b.Anchor,out bool limited));
                circuitPath=0;
                status=circuitPaths.Count==0?
                    limited?"Suche zu weit verzweigt. Nähere Abschnittsgrenzen wählen.":"Kein Gleisverlauf zwischen diesen Grenzen.":
                    limited?"Gleisverläufe gefunden. Weitere Wege sind möglich.":"Gleisverlauf prüfen und den markierten Feldern zuordnen.";
                RefreshWorkspaceFields();
            }));
            if(circuitPaths.Count>0) {
                var captions=circuitPaths.Select(p=>SignUnits.Number(Math.Round(p.Length,1))+" m · "+
                    p.Sections.Count+" Segmente"+(p.Points.Count==0?"":" · "+string.Join(" → ",p.Points.Select(id=>PointAssignments.Caption(desks,id))))).ToList();
                c.AddField("Gleisverlauf",c.AddDropdown(captions,circuitPath,i=>{circuitPath=i;RefreshWorkspaceFields();}));
                c.AddLabel(string.Join(" → ",circuitPaths[circuitPath].Sections.Select(p=>p.Section.Segment)));
                c.AddButton("Abschnitt zuordnen",()=>Act(()=>SaveCircuit(desk,selected,selectedSide)));
            }
            if(linked!=null&&desks.Blocks.TryGetValue(linked,out var old)&&old.IsCircuit)
                c.AddButton("Abschnittszuordnung lösen",()=>Act(()=>{
                    if(deskId!=desk)throw new InvalidOperationException("Gleisfeld erneut auswählen.");
                    EditPanel(doc=>{
                        var tile=doc.At(selected.X,selected.Y);
                        if(tile==null)throw new InvalidOperationException("Gleisfeld erneut auswählen.");
                        if(selectedSide.HasValue) {
                            int port=LocalPort(tile,selectedSide.Value);
                            if(tile.CircuitRails==null||!tile.CircuitRails.TryGetValue(port,out var saved)||saved!=linked)
                                throw new InvalidOperationException("Gleisfeld erneut auswählen.");
                            tile.CircuitRails.Remove(port);
                            if(tile.CircuitRails.Count==0)tile.CircuitRails=null;
                        } else {
                            if(tile.BlockId!=linked)throw new InvalidOperationException("Gleisfeld erneut auswählen.");
                            tile.BlockId="";
                        }
                    });
                    ReloadCircuitDraft();
                    status="Abschnittszuordnung gelöst.";
                }));
        }

        void ReloadCircuitDraft() {
            if(tileDraft!=null) {
                tileDraft=panel.At(tileDraft.X,tileDraft.Y)?.Clone();
                if(tileDraft!=null)LoadTileFields();
            }
            RefreshWorkspaceFields();
            panelView?.Refresh();
        }

        void SaveCircuit(string desk,PanelRegion selected,int? signalSide) {
            RequirePanelEdit();
            if(deskId!=desk||circuitPath>=circuitPaths.Count)throw new InvalidOperationException("Abschnitt erneut suchen.");
            var a=CircuitEndpoint(circuitA);
            var b=CircuitEndpoint(circuitB);
            var path=circuitPaths[circuitPath];
            var block=new TrackBlock{Id="c:"+Guid.NewGuid().ToString("N"),Name=
                BoundaryCaption(circuitA)+" → "+BoundaryCaption(circuitB),BoundaryA=circuitA,BoundaryB=circuitB,Path=path.Sections};
            if(block.Name.Length>80)block.Name=block.Name.Substring(0,80);
            if(!TrackCircuit.EndsMatch(block,a,b))throw new InvalidOperationException("Die Grenzen wurden geändert. Abschnitt erneut suchen.");
            var fields=panel.Tiles.Where(t=>selected.Contains(t.X,t.Y)&&CircuitField(t)).ToArray();
            var field=selected.Width==1&&selected.Height==1?panel.At(selected.X,selected.Y):null;
            bool arm=signalSide.HasValue&&field?.Kind==PanelTileKind.Signal;
            if(arm&&!AtSignal(field,circuitA)&&!AtSignal(field,circuitB))
                throw new InvalidOperationException("Mindestens eine Abschnittsgrenze muss das Signal dieses Feldes sein.");
            bool inner=!arm&&a.Anchor.Segment==b.Anchor.Segment&&fields.Length==0&&panel.Tiles.Any(t=>selected.Contains(t.X,t.Y)&&
                t.Kind==PanelTileKind.Signal&&t.AlternateSignal!=null&&t.Reference!=t.AlternateSignal&&
                TrackCircuit.Pair(Object(t.Reference)?.PhysicalId??t.Reference,Object(t.AlternateSignal)?.PhysicalId??t.AlternateSignal)==TrackCircuit.Pair(circuitA,circuitB));
            if(fields.Length==0&&!inner&&!arm)throw new InvalidOperationException("Gleisfelder oder ein Feld mit beiden Signalen markieren.");
            var existing=desks.Blocks.Values.FirstOrDefault(p=>p.IsCircuit&&
                TrackCircuit.Pair(p.BoundaryA,p.BoundaryB)==TrackCircuit.Pair(block.BoundaryA,block.BoundaryB)&&
                p.Path.Select(s=>s.Section.Key).OrderBy(k=>k).SequenceEqual(block.Path.Select(s=>s.Section.Key).OrderBy(k=>k)));
            if(existing!=null)block=existing;
            EditPanel(doc=>{
                foreach(var tile in doc.Tiles.Where(t=>selected.Contains(t.X,t.Y)&&CircuitField(t))) {
                    tile.Section=null;
                    tile.AutoSegment="";
                    tile.BlockId=block.Id;
                    tile.SecondBlock="";
                    if(fields.Length==1&&TerminalField(tile)&&a.IsCircuitBox)tile.CircuitBoundary=a.Id;
                }
                if(arm) {
                    var tile=doc.At(selected.X,selected.Y);
                    if(tile.CircuitRails==null)tile.CircuitRails=new Dictionary<int,string>();
                    tile.CircuitRails[LocalPort(tile,signalSide.Value)]=block.Id;
                }
                if(inner)foreach(var tile in doc.Tiles.Where(t=>selected.Contains(t.X,t.Y)&&t.Kind==PanelTileKind.Signal&&
                    TrackCircuit.Pair(Object(t.Reference)?.PhysicalId??t.Reference,Object(t.AlternateSignal)?.PhysicalId??t.AlternateSignal)==TrackCircuit.Pair(circuitA,circuitB)))
                    tile.BlockId=block.Id;
            },addBlock:block);
            ReloadCircuitDraft();
            status=arm?"Abschnitt an der gewählten Signalseite gespeichert.":inner?"Abschnitt zwischen beiden Signalen gespeichert.":"Gleisfreimeldung für "+fields.Length+" Gleisfeld(er) gespeichert · "+block.Name+".";
        }

        string BetweenSignals(PanelTile tile) {
            if(tile.Kind!=PanelTileKind.Signal||tile.AlternateSignal==null||tile.AlternateSignal==tile.Reference)return null;
            if(desks==null||tile.BlockId==""||!desks.Blocks.TryGetValue(tile.BlockId,out var block)||!block.IsCircuit)return null;
            var main=Object(tile.Reference);
            var shunt=Object(tile.AlternateSignal);
            if(main?.Anchor==null||shunt?.Anchor==null||main.Anchor.Segment!=shunt.Anchor.Segment)return null;
            var key=TrackCircuit.Pair(main.PhysicalId??main.Id,shunt.PhysicalId??shunt.Id);
            return key==TrackCircuit.Pair(block.BoundaryA,block.BoundaryB)?block.Id:null;
        }
    }
}
