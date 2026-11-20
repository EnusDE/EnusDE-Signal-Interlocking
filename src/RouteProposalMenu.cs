using System;
using System.Collections.Generic;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const int ProposalLimit=32;
        RouteKind editorRouteKind;
        readonly List<RouteProposal> routeProposals=new List<RouteProposal>();
        RouteProposal selectedProposal;
        string proposalDesk,routeTargetChoice;
        bool confirmingRouteBatch;

        void ClearRouteProposals(){routeProposals.Clear();selectedProposal=null;proposalDesk=null;confirmingRouteBatch=false;}

        RouteKind EffectiveRouteKind(MarkerRecord entry)=>entry?.Kind==MarkerRecord.Shunt?RouteKind.Shunt:editorRouteKind;

        HashSet<string> DeskSignals() {
            var relevant=CurrentDesk?.IsMaster==true?desks.Desks.Select(d=>d.Panel):new[]{panel}.AsEnumerable();
            return new HashSet<string>(relevant.Where(p=>p!=null).SelectMany(p=>p.Tiles)
                .Where(t=>t.Kind==PanelTileKind.Signal).SelectMany(t=>new[]{t.Reference,t.AlternateSignal}).Where(ButtonSignal),StringComparer.Ordinal);
        }

        string NextRouteCode(string start,RouteKind kind,IEnumerable<string> used=null,string exit=null) {
            var desk=CurrentDesk;
            if(desk?.IsMaster==true&&!string.IsNullOrEmpty(start)) {
                var local=desks.Desks.Where(d=>!d.IsMaster&&d.Panel.Tiles.Any(t=>t.Kind==PanelTileKind.Signal&&(t.Reference==start||t.AlternateSignal==start))).Take(2).ToArray();
                if(local.Length==1)desk=local[0];
            }
            return RouteNaming.Next(desk?.Code,kind,used??routes.Select(r=>r.Code),AuxiliaryEndpoint(exit)!=null);
        }

        bool AlreadyDefined(RouteProposal p)=>routes.Any(r=>RouteIdentity.Matches(r,p));

        void SearchRouteProposals() {
            RequirePanelEdit();
            ClearRouteProposals();
            var ids=DeskSignals();
            string start=panelButtons.Start;
            string end=panelButtons.End;
            var starts=string.IsNullOrEmpty(start)?ids.Select(Object).OrderBy(s=>s.Name,StringComparer.OrdinalIgnoreCase).Take(64).ToArray():
                new[]{Object(start)}.Where(s=>s!=null&&ids.Contains(s.Id)).ToArray();
            if(starts.Length==0)throw new InvalidOperationException("Zuerst Haupt- oder Rangiersignale auf dieser Tafel verknüpfen.");
            bool auxiliary=AuxiliaryEndpoint(end)!=null;
            var destinations=string.IsNullOrEmpty(end)?ids:new HashSet<string>(ids.Where(id=>id==end),StringComparer.Ordinal);
            if(auxiliary)destinations.Add(end);
            var scanner=world.DiscoverRoutes(auxiliary?end:null);
            var report=new RouteSearchReport();
            bool existing=false;
            foreach(var signal in starts) {
                var kind=EffectiveRouteKind(signal);
                foreach(var p in scanner.Find(signal,kind,destinations,report:report)) {
                    if(AlreadyDefined(p)){existing=true;continue;}
                    routeProposals.Add(p);
                    if(routeProposals.Count>=ProposalLimit)break;
                }
                if(routeProposals.Count>=ProposalLimit)break;
            }
            proposalDesk=deskId;
            selectedProposal=routeProposals.FirstOrDefault();
            status=routeProposals.Count==0?ExplainRouteSearch(starts,end,destinations,report,existing,scanner):
                routeProposals.Count+" Vorschläge gefunden. Fahrweg auswählen und übernehmen."+
                (routeProposals.Count==ProposalLimit||starts.Length==64?" Für weitere Treffer ein Startsignal auswählen.":"");
            RefreshWorkspaceFields();
        }

        string ExplainRouteSearch(MarkerRecord[] starts,string end,ISet<string> destinations,RouteSearchReport report,
            bool existing,RouteDiscovery scanner) {
            if(starts.Length==1&&end!=null) {
                var start=starts[0];
                var kind=EffectiveRouteKind(start);
                var endpoint=RouteEndpoint(end);
                if(endpoint==null||!destinations.Contains(end))return "Das Ziel ist auf dieser Tafel nicht gültig verknüpft.";
                if(AuxiliaryTarget.IsEndpoint(endpoint)&&!AuxiliaryTarget.Accepts(endpoint,kind==RouteKind.Shunt))
                    return "Das Hilfsziel ist für diese Fahrart nicht eingerichtet.";
                if(existing||routes.Any(r=>r.Entry==start.Id&&r.Exit==end&&
                    r.Shunting==(kind==RouteKind.Shunt)&&r.Reduced==(kind==RouteKind.Slow)))
                    return "Für diese Richtung und Fahrart ist bereits eine Fahrstraße gespeichert.";
                if(report.StartUnavailable)return "Startsignal, Fahrart oder Gleisposition ist nicht verwendbar.";
                if(report.Ambiguous.Contains(end))return "Mehrere Fahrwege oder Zielsignale gefunden. Gewünschte Weichenlage einstellen und den Fahrweg übernehmen.";
                if(report.LimitReached)return "Suche zu weit verzweigt. Näheres Ziel oder Teilfahrstraße wählen.";
                if(scanner.Find(start,kind,destinations,probeThroughSignals:true).Count>0)
                    return "Ein weiteres Signal begrenzt den Fahrweg vor dem Ziel. Das Zwischensignal als Ziel wählen.";
                if(AuxiliaryTarget.IsEndpoint(endpoint)) {
                    var reversed=endpoint.Clone();
                    reversed.FacingReverse=!reversed.FacingReverse;
                    if(world.DiscoverRoutesTo(reversed).Find(start,kind,destinations,probeThroughSignals:true).Count>0)
                        return "Das Hilfsziel ist aus der Gegenrichtung erreichbar. Anfahrtrichtung im Editor prüfen.";
                }
                return "Kein durchgehender Fahrweg in dieser Richtung. Gleisverbindung und Zielposition prüfen.";
            }
            if(existing)return "Gefundene eindeutige Fahrwege sind bereits gespeichert.";
            if(report.LimitReached||report.Ambiguous.Count>0)return "Keine eindeutige Auswahl. Start und Ziel einzeln wählen.";
            return "Keine passenden Fahrwege gefunden. Startsignal, Ziel und Fahrart einzeln wählen.";
        }

        void AcceptRouteProposals(IReadOnlyList<RouteProposal> proposals) {
            RequirePanelEdit();
            if(proposals.Count==0||proposalDesk!=deskId||proposals.Any(p=>!routeProposals.Contains(p)))
                throw new InvalidOperationException("Vorschlag veraltet. Fahrwege erneut suchen.");
            if(routes.Count+proposals.Count>128)throw new InvalidOperationException("Höchstens 128 Fahrstraßen pro Spielstand.");
            var signals=DeskSignals();
            foreach(var proposal in proposals)if(AuxiliaryEndpoint(proposal.Exit)!=null)signals.Add(proposal.Exit);
            var codes=routes.Select(r=>r.Code).ToList();
            var additions=new List<RouteDefinition>();
            var design=world.PrepareProposalDesign(signals);
            foreach(var proposal in proposals) {
                if(!signals.Contains(proposal.Entry)||!signals.Contains(proposal.Exit)||AlreadyDefined(proposal))
                    throw new InvalidOperationException("Zuordnung oder Definition inzwischen geändert. Fahrwege erneut suchen.");
                string code=NextRouteCode(proposal.Entry,proposal.Kind,codes,proposal.Exit);
                try{additions.Add(design(proposal,code));}
                catch(InvalidOperationException e){throw new InvalidOperationException(Code(proposal.Entry)+" → "+Code(proposal.Exit)+": "+e.Message,e);}
                codes.Add(code);
            }
            SaveNewRoutes(additions);
            foreach(var proposal in proposals)routeProposals.Remove(proposal);
            confirmingRouteBatch=false;
            selectedProposal=routeProposals.FirstOrDefault();
            Rebuild();
        }

        void BuildAuxiliaryDestinationPicker(UIPanelBuilder c) {
            var panels=CurrentDesk?.IsMaster==true?desks.Desks.Select(d=>d.Panel):new[]{panel}.AsEnumerable();
            var targets=AuxiliaryTarget.Index(panels.Where(p=>p!=null).SelectMany(p=>p.Tiles)).Values
                .Where(t=>t.Function==AuxiliaryFunction.RouteDestination).OrderBy(t=>t.Caption,StringComparer.OrdinalIgnoreCase).ToArray();
            if(targets.Length==0)return;
            if(!targets.Any(t=>t.Id==routeTargetChoice))routeTargetChoice=targets[0].Id;
            c.AddSearchChoice(LocalText.Get("Hilfstaste als Ziel"),targets.Select(t=>t.Caption+" · "+(t.Train&&t.Shunt?"Zug / Rangieren":t.Shunt?"Rangieren":"Zug")).ToList(),
                Array.FindIndex(targets,t=>t.Id==routeTargetChoice),i=>routeTargetChoice=targets[i].Id);
            c.AddLabel(LocalText.Get("Zuerst ein Startsignal wählen. Die Hilfstaste übernimmt die gewählte Fahrart und bleibt ausschließlich Ziel."));
            c.AddButton(LocalText.Get("Hilfstaste als Ziel übernehmen"),()=>Act(()=>{
                var endpoint=AuxiliaryEndpoint(routeTargetChoice);
                if(endpoint==null)throw new InvalidOperationException("Hilfsziel fehlt. Auswahl erneut öffnen.");
                ChooseAuxiliary(auxiliaryTargets[endpoint.Id],editorRouteKind==RouteKind.Shunt,true);
                RefreshWorkspaceFields();panelView?.Refresh();
            }));
        }

        void BuildRouteDiscovery(UIPanelBuilder c) {
            c.AddLabel(LocalText.Get("Im Fahrdienstleiter genügen Start und Ziel. Gespeicherte Definitionen sind optional."));
            var entry=Object(panelButtons.Start);
            if(entry!=null) {
                var supported=new List<string>();
                if(SignalCatalogue.Supports(entry,false,false))supported.Add("Fahrt (Hp 1)");
                if(SignalCatalogue.Supports(entry,false,true))supported.Add("Langsamfahrt (Hp 2)");
                if(SignalCatalogue.Supports(entry,true,false))supported.Add("Rangieren (Sh 1)");
                c.AddLabel(LocalText.Get("Startsignal kann: ")+string.Join(" · ",supported));
                c.AddButton(LocalText.Get("Startsignal konfigurieren"),()=>Entry.OpenMarker(entry.PhysicalId??entry.Id));
                if(editorRouteKind==RouteKind.Shunt)c.AddLabel(LocalText.Get("Fahrart: Rangierfahrt (Sh 1)"));
                else if(SignalCatalogue.Supports(entry,false,true)&&SignalCatalogue.Supports(entry,false,false)) {
                    c.AddFieldToggle(LocalText.Get("Langsamfahrt (Hp 2)"),()=>editorRouteKind==RouteKind.Slow,v=>{
                        editorRouteKind=v?RouteKind.Slow:RouteKind.Train;ClearRouteProposals();RefreshWorkspaceFields();
                    },true);
                } else c.AddLabel(editorRouteKind==RouteKind.Slow?"Fahrart: Langsamfahrt (Hp 2)":"Fahrart: Zugfahrt (Hp 1)");
                c.AddLabel(LocalText.Get("Rangierfahrt wird über das Rangiersignal beziehungsweise die Rangierhälfte gewählt."));
            }
            string start=selectedProposal?.Entry??panelButtons.Start;
            c.AddField(LocalText.Get("Kennung"),NextRouteCode(start,selectedProposal?.Kind??EffectiveRouteKind(Object(start)),exit:selectedProposal?.Exit??panelButtons.End));
            BuildLinkedTargetActions(c,true);
            BuildAuxiliaryDestinationPicker(c);
            c.AddButton(LocalText.Get("Fahrwege automatisch suchen"),()=>Act(SearchRouteProposals));
            if(proposalDesk!=deskId||selectedProposal==null)return;
            var choices=routeProposals.ToArray();
            c.AddSearchChoice(LocalText.Get("Vorschlag"),choices.Select(p=>RouteNaming.KindCode(p.Kind,AuxiliaryEndpoint(p.Exit)!=null)+" · "+Code(p.Entry)+" → "+Code(p.Exit)).ToList(),
                Array.IndexOf(choices,selectedProposal),i=>{selectedProposal=choices[i];RefreshWorkspaceFields();});
            var selected=selectedProposal;
            c.AddLabel(Math.Round(selected.Length)+" m · "+(selected.Switches.Count==0?"keine Weichen":
                string.Join(", ",selected.Switches.Select(s=>PointAssignments.Caption(desks,s.Node)+(s.Thrown?" −":" +")))));
            c.AddButton(LocalText.Get("Vorschlag speichern"),()=>Act(()=>AcceptRouteProposals(new[]{selected})));
            if(choices.Length>1) {
                if(confirmingRouteBatch) {
                    c.AddLabel(choices.Length+" Vorschläge als Fahrstraßen speichern?");
                    c.AddButton(LocalText.Get("Speichern bestätigen"),()=>Act(()=>{if(confirmingRouteBatch)AcceptRouteProposals(choices);}));
                    c.AddButton(LocalText.Get("Abbrechen"),()=>{confirmingRouteBatch=false;RefreshWorkspaceFields();});
                } else c.AddButton(LocalText.Get("Alle Vorschläge speichern …"),()=>{confirmingRouteBatch=true;RefreshWorkspaceFields();});
            }
        }
    }
}
