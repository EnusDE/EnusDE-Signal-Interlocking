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
        AuxiliaryFunction targetFunctionInput;
        string targetPermissionLineInput="",targetReferenceInput="";
        bool targetTechnical;

        internal MarkerRecord AuxiliaryEndpoint(string id) {
            if(id==null)return null;
            if(!ReferenceEquals(targetLibrary,desks)) {
                auxiliaryTargets=AuxiliaryTarget.Index(desks?.Desks.SelectMany(d=>d.Panel.Tiles)??Enumerable.Empty<PanelTile>());
                targetLibrary=desks;
            }
            if(!auxiliaryTargets.TryGetValue(id,out var target))return null;
            var binding=BoundLineTarget(id);
            if(binding!=null){var end=Object(binding.Arrival);if(end?.Kind!=MarkerRecord.Main||end.Planned)return null;return new MarkerRecord{Id=target.Id,Name=target.Caption,Kind=MarkerRecord.Auxiliary,Anchor=end.Anchor.Clone(),FacingReverse=end.FacingReverse,AllowHp1=target.Train,AllowHp2=target.Train,AllowSh1=false};}
            return target.Endpoint(Object);
        }

        internal MarkerRecord RouteEndpoint(string id)=>Object(id)??AuxiliaryEndpoint(id);
        bool RouteButton(string id)=>ButtonSignal(id)||AuxiliaryEndpoint(id)!=null;

        void BuildLinkedTargetActions(UIPanelBuilder c,bool editor) {
            if(panelButtons.Start==null||desks==null)return;
            string picked=panel?.At(selectedX,selectedY)?.Kind==PanelTileKind.Signal?SignalCellLayout.SignalAt(panel.At(selectedX,selectedY),dispatcherContextRight):null;
            var tiles=desks.Desks.SelectMany(d=>d.Panel.Tiles).ToArray();
            var targets=AuxiliaryTarget.LinkedForSignal(tiles,panelButtons.Start).Concat(AuxiliaryTarget.LinkedForSignal(tiles,picked)).GroupBy(t=>t.Id).Select(g=>g.First()).ToArray();
            if(targets.Length==0)return;
            bool yard=editor?editorRouteKind==RouteKind.Shunt:shunting;
            c.AddSection(WindowText("Recognized linked destinations","Erkannte zugeordnete Hilfsziele"));
            foreach(var target in targets) {
                var captured=target;
                c.AddLabel(target.Caption+" · "+(target.Function==AuxiliaryFunction.LinePermission?WindowText("Permission controls","Erlaubnisbedienung"):target.Train&&target.Shunt?WindowText("Train / shunt","Zug / Rangieren"):target.Shunt?WindowText("Shunt","Rangieren"):WindowText("Train","Zugfahrt")));
                if(target.Function==AuxiliaryFunction.LinePermission)c.AddButton(WindowText("Open permission controls","Erlaubnisbedienung öffnen"),()=>Act(()=>OpenAuxiliaryPermission(captured)));
                else if(AuxiliaryTarget.Accepts(target.Endpoint(),yard))c.AddButton(WindowText("Set to ","Zum Hilfsziel ")+target.Caption,()=>Act(()=>ChooseAuxiliary(captured,yard,editor)));
                else c.AddLabel(WindowText("This destination does not support the selected movement.","Dieses Hilfsziel unterstützt die gewählte Fahrart nicht."));
            }
        }

        void CheckTargetEdit(PanelDocument before,PanelDocument after,IEnumerable<PanelTile> otherTiles=null) {
            if(!before.Tiles.Any(t=>t.Target!=null))return;
            var replacement=AuxiliaryTarget.Index(after.Tiles.Concat(otherTiles??Enumerable.Empty<PanelTile>()));
            foreach(var old in before.Tiles.Select(t=>t.Target).Where(t=>t!=null)) {
                replacement.TryGetValue(old.Id,out var next);
                if(old.SamePath(next))continue;
                CheckLineConfigurationChange(Array.Empty<string>(),targets:new[]{old.Id});
                var used=desks.FreeLines.Where(l=>LineSignalGroups.All(l).Contains(old.Id)).Select(l=>l.Code).ToArray();
                if(used.Length>0)throw new InvalidOperationException(WindowText("This destination is still a line endpoint. Remove that endpoint binding before changing its function or path: ","Dieses Ziel ist noch Streckenendpunkt. Vor Änderung seiner Funktion oder seines Fahrwegs diese Endpunktzuordnung lösen: ")+string.Join(", ",used));
                if(engine.Active.Any(r=>r.Route.Exit==old.Id))throw new InvalidOperationException("Aktive Hilfsfahrstraße zuerst sicher auflösen.");
                var saved=routes.Where(r=>r.AuxiliaryExit&&r.Exit==old.Id).Select(r=>r.Code).ToArray();
                if(saved.Length>0)throw new InvalidOperationException(WindowText("Saved routes still use this destination: ","Gespeicherte Fahrstraßen verwenden dieses Ziel noch: ")+string.Join(", ",saved));
            }
        }

        void RememberTargetClick(Location at) {
            var location=at.WithEnd(TrackSegment.End.A);
            auxiliarySegment=location.segment.id;
            auxiliaryMeters=location.distance.ToString("R",CultureInfo.InvariantCulture);
            lastAuxiliaryScope=scope;
        }

        void LoadTargetFields(PanelTile tile) {
            targetReferenceInput=tile.Target?.ReferenceSignal??"";
            targetSegmentInput=tile.Target?.Anchor?.Segment??"";
            targetMetersInput=tile.Target?.Anchor?.Meters.ToString("R",CultureInfo.InvariantCulture)??"0";
            targetReverseInput=tile.Target?.Reverse??false;
            targetTrainInput=tile.Target?.Train??true;
            targetShuntInput=tile.Target?.Shunt??false;
            targetFunctionInput=tile.Target?.Function??AuxiliaryFunction.RouteDestination;
            targetPermissionLineInput=tile.Target?.PermissionLine??"";
        }

        void BuildAuxiliaryEditor(UIPanelBuilder c,PanelDraftBinding binding) {
            BuildConfigurationBlockers(c);
            BuildAuxiliaryReferences(c,binding.Draft.Target?.Id);
            bool embedded=SignalCellLayout.HasAuxiliaryHalf(binding.Draft);
            if(!embedded) {
                c.AddField(WindowText("Auxiliary button function","Funktion der Hilfstaste"),c.AddDropdown(new[]{WindowText("Route destination","Fahrstraßenziel"),WindowText("Line permission controls","Streckenerlaubnis bedienen")}.ToList(),(int)targetFunctionInput,i=>binding.Do(()=>{targetFunctionInput=(AuxiliaryFunction)i;RefreshWorkspaceFields();})));
                if(targetFunctionInput==AuxiliaryFunction.LinePermission) {
                    var lines=LineSetupWorkflow.Visible(desks,deskId).ToArray();
                    c.AddSearchChoice(WindowText("Line track","Streckengleis"),new[]{WindowText("Unassigned","Nicht zugeordnet")}.Concat(lines.Select(l=>l.Code+" · "+l.Track)).ToList(),Array.FindIndex(lines,l=>l.Id==targetPermissionLineInput)+1,i=>binding.Do(()=>targetPermissionLineInput=i==0?"":lines[i-1].Id));
                    c.AddLabel(WindowText("This button opens permission request / giving controls without selecting a route start. It creates no route destination, detector or world object. Existing track approach settings are retained for switching back to destination mode.","Diese Taste öffnet Anfrage und Abgabe der Erlaubnis ohne Startsignalwahl. Sie erzeugt kein Fahrstraßenziel, keine Freimeldegrenze und kein Weltobjekt. Vorhandene Anfahrteinstellungen bleiben für die Rückkehr zum Zielmodus erhalten."));
                    return;
                }
            }
            if(!embedded) {
                var lines=desks.FreeLines.Where(l=>CurrentDesk.IsMaster||l.StationA==deskId||l.StationB==deskId).OrderBy(l=>l.Code).ToArray();
                c.AddSearchChoice(WindowText("Required line permission for this destination","Erforderliche Streckenerlaubnis für dieses Ziel"),new[]{WindowText("Through linked permission module / unbound","Über verknüpftes Erlaubnismodul / ohne Bindung")}.Concat(lines.Select(l=>l.Code+" · "+l.Name)).ToList(),Array.FindIndex(lines,l=>l.Id==targetPermissionLineInput)+1,i=>binding.Do(()=>{targetPermissionLineInput=i==0?"":lines[i-1].Id;RefreshWorkspaceFields();}));
                c.AddLabel(WindowText("This remains a train destination. A direct line assignment or a linked permission module requires departure permission and requests the complete route through the block signals to the neighbour's arrival signal. The local drawing position does not shorten that request.","Dies bleibt ein Zugziel. Direkte Streckenzuordnung oder ein verknüpftes Erlaubnismodul verlangen die Abfahrterlaubnis und fordern den vollständigen Fahrweg durch die Blocksignale bis zum Ankunftssignal des Nachbarn an. Die örtliche Zeichenposition verkürzt diese Anforderung nicht."));
                var active=binding.Draft.Target==null?null:BoundLineTarget(binding.Draft.Target.Id);
                if(active!=null)c.AddLabel(active.Error??(active.Line.Code+" · "+LineStationCode(active.Station)+" → "+LineStationCode(active.Direction==1?active.Line.StationB:active.Line.StationA)));
            }
            BuildAuxiliarySetup(c,binding,embedded);
            c.AddLabel(embedded?"Reines Rangierziel für Fahrten aus der Gegenrichtung. Gleisposition und Anfahrt unten prüfen.":"Zugziel und Rangierziel können denselben Gleispunkt verwenden. Zugziel links, Rangierziel rechts; die gewählte Fahrart kommt vom Startsignal.");
            c.AddLabel(embedded?"Goldener Rand und Z kennzeichnen die Hilfszieltaste. Ihr Pfeil folgt automatisch der Gegenrichtung des Rangiersignals. Spiegeln versetzt beide Schirme gemeinsam.":"Goldener Rand und Z markieren reine Zieltasten. Drehen richtet den Pfeil im Gleisbild aus, Spiegeln versetzt den Schirm auf die andere Gleisseite.");
            if(lastAuxiliaryScope==scope&&auxiliarySegment!="")
                c.AddButton(LocalText.Get("Letzten Gleisklick übernehmen"),()=>binding.Do(()=>{
                    targetReferenceInput="";targetSegmentInput=auxiliarySegment;targetMetersInput=auxiliaryMeters;
                    RefreshWorkspaceFields();
                }));
            c.AddFieldToggle(WindowText("Manual track position / advanced settings","Manuelle Gleisposition / erweiterte Einstellungen"),()=>targetTechnical,v=>binding.Do(()=>{targetTechnical=v;RefreshWorkspaceFields();}),true);
            if(targetTechnical) {
            c.AddLabel(LocalText.Get("Gleis-ID"));
            c.AddInputField(targetSegmentInput,v=>binding.Do(()=>{targetSegmentInput=v;targetReferenceInput="";}),"Gleis-/Weichenkennungen im Rechtsklickmenü",128);
            c.AddLabel(LocalText.Get("Meter ab Ende A"));
            c.AddInputField(targetMetersInput,v=>binding.Do(()=>{targetMetersInput=v;targetReferenceInput="";}),"0",24);
            c.AddField(LocalText.Get("Anfahrt auf dem Spielgleis"),c.AddDropdown(new[]{"Zum Segmentende B","Zum Segmentende A"}.ToList(),targetReverseInput?1:0,i=>binding.Do(()=>{
                targetReferenceInput="";targetReverseInput=i==1;
                if(binding.Draft.Target!=null)binding.Draft.Target.Reverse=targetReverseInput;
                panelView?.Refresh();
            })));
            c.AddLabel(LocalText.Get("Mit 90° drehen richtest du den Pfeil am gezeichneten Gleis aus. Die Anfahrt auf dem Spielgleis stellst du oben ein."));
            }
            if(embedded)return;
            c.AddLabel(LocalText.Get("Zugfahrten als Ziel zulassen"));
            c.AddFieldToggle(LocalText.Get("Zugfahrten"),()=>targetTrainInput,v=>binding.Do(()=>targetTrainInput=v),true);
            c.AddLabel(LocalText.Get("Rangierfahrten als Ziel zulassen"));
            c.AddFieldToggle(LocalText.Get("Rangierfahrten"),()=>targetShuntInput,v=>binding.Do(()=>targetShuntInput=v),true);
        }

        void BuildAuxiliaryReferences(UIPanelBuilder c,string targetId) {
            if(string.IsNullOrEmpty(targetId))return;
            var links=desks.Desks.SelectMany(d=>d.Panel.Tiles.Where(t=>t.LinkedAuxiliary==targetId||t.PermissionAuxiliary==targetId)
                .Select(t=>new {Desk=d.Id,Caption=d.Code+" · Feld "+(t.X+1)+" / "+(t.Y+1),X=t.X,Y=t.Y})).ToArray();
            if(links.Length==0)return;
            c.AddSection(LocalText.Get("Verknüpfte Signalfelder"));
            c.AddLabel(LocalText.Get("Vor dem Löschen dort Hilfsziel dahinter auf Ohne Hilfsziel setzen und das Signalfeld speichern. Danach hierher zurückkehren."));
            int selectedLink=0;
            c.AddSearchChoice(LocalText.Get("Signalfeld"),links.Select(link=>link.Caption).ToList(),selectedLink,i=>selectedLink=i);
            c.AddButton(LocalText.Get("Verknüpftes Signalfeld öffnen"),()=>Act(()=>{
                var link=links[selectedLink];
                SelectDesk(link.Desk,false);
                selectedX=link.X;selectedY=link.Y;
                tileDraft=panel.At(link.X,link.Y)?.Clone();
                if(tileDraft!=null)LoadTileFields();
                status=LocalText.Get("Hilfsziel-Zuordnung im Signalfeld lösen und speichern.");
                Rebuild(false);
            }));
        }

        void SaveAuxiliaryTarget(PanelTile tile) {
            if(tile.Kind==PanelTileKind.AuxiliaryTarget&&targetFunctionInput==AuxiliaryFunction.LinePermission) {
                var permission=tile.Target??new AuxiliaryTarget();permission.Name=tile.Label.Trim();permission.Function=AuxiliaryFunction.LinePermission;permission.PermissionLine=targetPermissionLineInput;permission.Validate();tile.Target=permission;return;
            }
            string assignedLine=targetPermissionLineInput;
            if(assignedLine==""&&tile.Target!=null)assignedLine=BoundLineTarget(tile.Target.Id)?.Line?.Id??"";
            if(tile.Kind==PanelTileKind.AuxiliaryTarget&&assignedLine!="") {
                var line=desks.FreeLines.Single(l=>l.Id==assignedLine);string local=CurrentDesk.IsMaster?StationAutomationPolicy.Owner(desks,tile.Target?.Id)?.Id:deskId;
                if(local!=line.StationA&&local!=line.StationB)throw new InvalidOperationException(WindowText("Configure the bound destination in its neighbouring local interlocking.","Das gebundene Ziel im zugehörigen örtlichen Nachbarstellwerk einrichten."));
                var arrival=Object(local==line.StationA?line.ForwardEnd:line.ReverseEnd);
                if(arrival?.Kind!=MarkerRecord.Main||arrival.Planned)throw new InvalidOperationException(WindowText("Configure the neighbour's available arrival main signal first.","Zuerst das verfügbare Ankunfts-Hauptsignal des Nachbarn einrichten."));
                targetReferenceInput="";targetSegmentInput=arrival.Anchor.Segment;targetMetersInput=arrival.Anchor.Meters.ToString("R",CultureInfo.InvariantCulture);targetReverseInput=arrival.FacingReverse;
            }
            if(targetReferenceInput!="") {
                var reference=Object(targetReferenceInput);
                if(reference?.Kind!=MarkerRecord.Main||reference.Planned)throw new InvalidOperationException(WindowText("The reference signal is missing. Select it again.","Das Bezugssignal fehlt. Erneut auswählen."));
                targetSegmentInput=reference.Anchor.Segment;targetMetersInput=reference.Anchor.Meters.ToString("R",CultureInfo.InvariantCulture);targetReverseInput=!reference.FacingReverse;
            }
            var segment=Graph.Shared?.GetSegment(targetSegmentInput.Trim());
            if(!double.TryParse(targetMetersInput.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double meters)||
                segment==null||!segment.Available||!segment.GroupEnabled||segment.turntable!=null||
                !(meters>=0&&meters<=segment.GetLength()))throw new InvalidOperationException("Gleis-ID oder Zielmeter nicht verfügbar.");
            var target=tile.Target??new AuxiliaryTarget();
            target.Name=(tile.Kind==PanelTileKind.Signal?target.Name:tile.Label).Trim();
            target.Anchor=new MarkerAnchor{Segment=segment.id,Meters=meters,Scale=1};
            target.ReferenceSignal=targetReferenceInput;target.Reverse=targetReverseInput;
            target.Function=AuxiliaryFunction.RouteDestination;target.PermissionLine=targetPermissionLineInput;
            target.Train=tile.Kind==PanelTileKind.Signal?false:targetTrainInput;
            target.Shunt=tile.Kind==PanelTileKind.Signal?true:targetShuntInput;
            target.Validate();
            tile.Target=target;
        }

        // A destination keeps the movement selected at the start; either icon is the same endpoint.
        void PressAuxiliary(PanelTile tile,bool rightHalf,bool editor) {
            if(tile.Target?.Function==AuxiliaryFunction.LinePermission){OpenAuxiliaryPermission(tile.Target);if(panelButtons.Start!=null)status=WindowText("Permission controls opened. This button is not a route destination. Select a normal destination linked to this permission module.","Erlaubnisbedienung geöffnet. Diese Taste ist kein Fahrstraßenziel. Ein normales Ziel wählen, das mit diesem Erlaubnismodul verknüpft ist.");return;}
            ChooseAuxiliary(tile.Target,editor?editorRouteKind==RouteKind.Shunt:shunting,editor);
        }
        void OpenAuxiliaryPermission(AuxiliaryTarget target) {
            var line=desks.FreeLines.FirstOrDefault(l=>l.Id==target.PermissionLine)??throw new InvalidOperationException(WindowText("Assign a line to this permission button in the editor first.","Dieser Erlaubnistaste zuerst im Editor eine Strecke zuordnen."));
            if(!LineSetupWorkflow.Visible(desks,deskId).Any(l=>l.Id==line.Id))throw new InvalidOperationException(WindowText("This interlocking is not a neighbour of the line.","Dieses Stellwerk ist kein Nachbar der Strecke."));
            selectedLine=line.Id;lineDraft=line.Clone();panelPage=9;OpenWorkspaceContext();
        }

        void ChooseAuxiliary(AuxiliaryTarget target,bool yard,bool editor) {
            if(target==null)throw new InvalidOperationException("Hilfsziel zuerst im Editor konfigurieren und speichern.");
            if(panelButtons.Start==null)throw new InvalidOperationException("Zuerst das Startsignal wählen. Das Hilfsziel markiert das Ende.");
            if(!AuxiliaryTarget.Accepts(target.Endpoint(),yard))
                throw new InvalidOperationException("Dieses Hilfsziel ist für die gewählte Fahrart nicht eingerichtet. Zieloptionen im Editor prüfen.");
            if(editor) {
                var start=Object(panelButtons.Start);
                if(!SignalCatalogue.Supports(start,yard,!yard&&editorRouteKind==RouteKind.Slow))
                    throw new InvalidOperationException("Das Startsignal unterstützt die gewählte Fahrart nicht.");
                editorRouteKind=yard?RouteKind.Shunt:editorRouteKind==RouteKind.Slow?RouteKind.Slow:RouteKind.Train;
                ClearRouteProposals();
                target.SelectDestination(panelButtons,yard,RouteButton);
                status=LocalText.Get("Hilfsziel ")+target.Caption+" gewählt. Fahrweg suchen oder speichern.";
                return;
            }
            RequireDispatcher();
            if(shunting!=yard)throw new InvalidOperationException("Die gewählte Fahrart passt nicht zum Startsignal.");
            CloseWorkspaceContext();
            RequestSelectedDestination(target.Id);
        }
    }
}
