using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Track;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        DeskLibrary targetLibrary;
        Dictionary<string,AuxiliaryTarget> auxiliaryTargets=new Dictionary<string,AuxiliaryTarget>();
        string lastAuxiliaryScope,auxiliarySegment="",auxiliaryMeters="0",targetSegmentInput="",targetMetersInput="0";
        bool targetReverseInput,targetTrainInput=true,targetShuntInput;

        internal MarkerRecord AuxiliaryEndpoint(string id) {
            if(id==null)return null;
            if(!ReferenceEquals(targetLibrary,desks)) {
                auxiliaryTargets=AuxiliaryTarget.Index(desks?.Desks.SelectMany(d=>d.Panel.Tiles)??Enumerable.Empty<PanelTile>());
                targetLibrary=desks;
            }
            return auxiliaryTargets.TryGetValue(id,out var target)?target.Endpoint():null;
        }

        internal MarkerRecord RouteEndpoint(string id)=>Object(id)??AuxiliaryEndpoint(id);
        bool RouteButton(string id)=>ButtonSignal(id)||AuxiliaryEndpoint(id)!=null;

        void CheckTargetEdit(PanelDocument before,PanelDocument after,IEnumerable<PanelTile> otherTiles=null) {
            if(!before.Tiles.Any(t=>t.Target!=null))return;
            var replacement=AuxiliaryTarget.Index(after.Tiles.Concat(otherTiles??Enumerable.Empty<PanelTile>()));
            foreach(var old in before.Tiles.Select(t=>t.Target).Where(t=>t!=null)) {
                replacement.TryGetValue(old.Id,out var next);
                if(old.SamePath(next))continue;
                if(engine.Active.Any(r=>r.Route.Exit==old.Id))throw new InvalidOperationException("Aktive Hilfsfahrstraße zuerst sicher auflösen.");
                if(routes.Any(r=>r.AuxiliaryExit&&r.Exit==old.Id))
                    throw new InvalidOperationException("Vor Änderung oder Löschen zuerst die gespeicherten Fahrstraßen zu "+old.Caption+" entfernen.");
            }
        }

        void RememberTargetClick(Location at) {
            var location=at.WithEnd(TrackSegment.End.A);
            auxiliarySegment=location.segment.id;
            auxiliaryMeters=location.distance.ToString("R",CultureInfo.InvariantCulture);
            lastAuxiliaryScope=scope;
        }

        void LoadTargetFields(PanelTile tile) {
            targetSegmentInput=tile.Target?.Anchor.Segment??"";
            targetMetersInput=tile.Target?.Anchor.Meters.ToString("R",CultureInfo.InvariantCulture)??"0";
            targetReverseInput=tile.Target?.Reverse??false;
            targetTrainInput=tile.Target?.Train??true;
            targetShuntInput=tile.Target?.Shunt??false;
        }

        void BuildAuxiliaryEditor(UIPanelBuilder c,PanelDraftBinding binding) {
            c.AddLabel("Zielpunkt auf dem echten Gleis festlegen. Beide Fahrarten können denselben Punkt verwenden.");
            if(lastAuxiliaryScope==scope&&auxiliarySegment!="")
                c.AddButton("Letzten Gleisklick übernehmen",()=>binding.Do(()=>{
                    targetSegmentInput=auxiliarySegment;targetMetersInput=auxiliaryMeters;
                    RefreshWorkspaceFields();
                }));
            c.AddLabel("Gleis-ID");
            c.AddInputField(targetSegmentInput,v=>binding.Do(()=>targetSegmentInput=v),"Gleis-/Weichenkennungen im Rechtsklickmenü",128);
            c.AddLabel("Meter ab Ende A");
            c.AddInputField(targetMetersInput,v=>binding.Do(()=>targetMetersInput=v),"0",24);
            c.AddField("Anfahrt auf dem Spielgleis",c.AddDropdown(new[]{"Zum Segmentende B","Zum Segmentende A"}.ToList(),targetReverseInput?1:0,i=>binding.Do(()=>{
                targetReverseInput=i==1;
                if(binding.Draft.Target!=null)binding.Draft.Target.Reverse=targetReverseInput;
                panelView?.Refresh();
            })));
            c.AddLabel("Mit 90° drehen richtest du den Pfeil am gezeichneten Gleis aus. Die Anfahrt auf dem Spielgleis stellst du oben ein.");
            c.AddFieldToggle("Zugfahrten als Ziel zulassen",()=>targetTrainInput,v=>binding.Do(()=>targetTrainInput=v),true);
            c.AddFieldToggle("Rangierfahrten als Ziel zulassen",()=>targetShuntInput,v=>binding.Do(()=>targetShuntInput=v),true);
        }

        void SaveAuxiliaryTarget(PanelTile tile) {
            var segment=Graph.Shared?.GetSegment(targetSegmentInput.Trim());
            if(!double.TryParse(targetMetersInput.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double meters)||
                segment==null||!segment.Available||!segment.GroupEnabled||segment.turntable!=null||
                !(meters>=0&&meters<=segment.GetLength()))throw new InvalidOperationException("Gleis-ID oder Zielmeter nicht verfügbar.");
            var target=tile.Target??new AuxiliaryTarget();
            target.Name=tile.Label.Trim();
            target.Anchor=new MarkerAnchor{Segment=segment.id,Meters=meters,Scale=1};
            target.Reverse=targetReverseInput;
            target.Train=targetTrainInput;
            target.Shunt=targetShuntInput;
            target.Validate();
            tile.Target=target;
        }

        void PressAuxiliary(PanelTile tile,bool rightHalf,bool editor)=>ChooseAuxiliary(tile.Target,rightHalf,editor);

        void ChooseAuxiliary(AuxiliaryTarget target,bool rightHalf,bool editor) {
            if(target==null)throw new InvalidOperationException("Hilfsziel zuerst im Editor konfigurieren und speichern.");
            if(panelButtons.Start==null)throw new InvalidOperationException("Zuerst das Startsignal wählen. Das Hilfsziel markiert das Ende.");
            bool yard=target.Train&&target.Shunt?rightHalf:target.Shunt;
            if(editor) {
                var start=Object(panelButtons.Start);
                if(!SignalCatalogue.Supports(start,yard,!yard&&editorRouteKind==RouteKind.Slow))
                    throw new InvalidOperationException("Das Startsignal unterstützt die Fahrart dieser Zielhälfte nicht.");
                editorRouteKind=yard?RouteKind.Shunt:editorRouteKind==RouteKind.Slow?RouteKind.Slow:RouteKind.Train;
                ClearRouteProposals();
                panelButtons.ClearEnd();
                panelButtons.Press(target.Id,RouteButton);
                status="Hilfsziel "+target.Caption+" gewählt. Fahrweg suchen oder speichern.";
                return;
            }
            RequireDispatcher();
            if(shunting!=yard)throw new InvalidOperationException("Die Zielhälfte passt nicht zur gewählten Fahrart am Startsignal.");
            CloseWorkspaceContext();
            RequestSelectedDestination(target.Id);
        }
    }
}
