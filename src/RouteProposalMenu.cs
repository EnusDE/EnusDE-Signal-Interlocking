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
        string proposalDesk;
        bool confirmingRouteBatch;

        void ClearRouteProposals(){routeProposals.Clear();selectedProposal=null;proposalDesk=null;confirmingRouteBatch=false;}

        RouteKind EffectiveRouteKind(MarkerRecord entry)=>entry?.Kind==MarkerRecord.Shunt?RouteKind.Shunt:editorRouteKind;

        HashSet<string> DeskSignals() {
            var relevant=CurrentDesk?.Global==true?desks.Desks.Select(d=>d.Panel):new[]{panel}.AsEnumerable();
            return new HashSet<string>(relevant.Where(p=>p!=null).SelectMany(p=>p.Tiles)
                .Where(t=>t.Kind==PanelTileKind.Signal).SelectMany(t=>new[]{t.Reference,t.AlternateSignal}).Where(ButtonSignal),StringComparer.Ordinal);
        }

        string NextRouteCode(string start,RouteKind kind,IEnumerable<string> used=null,string exit=null) {
            var desk=CurrentDesk;
            if(desk?.Global==true&&!string.IsNullOrEmpty(start)) {
                var local=desks.Desks.Where(d=>!d.Global&&d.Panel.Tiles.Any(t=>t.Kind==PanelTileKind.Signal&&(t.Reference==start||t.AlternateSignal==start))).Take(2).ToArray();
                if(local.Length==1)desk=local[0];
            }
            return RouteNaming.Next(desk?.Code,kind,used??routes.Select(r=>r.Code),AuxiliaryEndpoint(exit)!=null);
        }

        bool AlreadyDefined(RouteProposal p)=>routes.Any(r=>r.Entry==p.Entry&&r.Exit==p.Exit&&
            r.Shunting==(p.Kind==RouteKind.Shunt)&&r.Reduced==(p.Kind==RouteKind.Slow));

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

        void BuildRouteDiscovery(UIPanelBuilder c) {
            var kinds=new[]{"Zugfahrstraße (ZF)","Langsamfahrt (LF)","Rangierfahrstraße (RF)"};
            c.AddField("Fahrart",c.AddDropdown(kinds.ToList(),(int)editorRouteKind,i=>{
                editorRouteKind=(RouteKind)i;
                ClearRouteProposals();
                RefreshWorkspaceFields();
            }));
            string start=selectedProposal?.Entry??panelButtons.Start;
            c.AddField("Kennung",NextRouteCode(start,selectedProposal?.Kind??EffectiveRouteKind(Object(start)),exit:selectedProposal?.Exit??panelButtons.End));
            if(panelButtons.Start!=null&&panelButtons.End==null) {
                var startField=panelLookup.Read(panel,selectedX,selectedY);
                if(startField?.Kind==PanelTileKind.Signal&&startField.LinkedAuxiliary!=null&&
                    (startField.Reference==panelButtons.Start||startField.AlternateSignal==panelButtons.Start)&&
                    AuxiliaryEndpoint(startField.LinkedAuxiliary)!=null) {
                    var target=auxiliaryTargets[startField.LinkedAuxiliary];
                    if(target.Train)c.AddButton("Hilfsziel dahinter · Zugfahrt",()=>Act(()=>ChooseAuxiliary(target,false,true)));
                    if(target.Shunt)c.AddButton("Hilfsziel dahinter · Rangieren",()=>Act(()=>ChooseAuxiliary(target,true,true)));
                }
            }
            c.AddButton("Fahrwege automatisch suchen",()=>Act(SearchRouteProposals));
            if(proposalDesk!=deskId||selectedProposal==null)return;
            var choices=routeProposals.ToArray();
            c.AddField("Vorschlag",c.AddDropdown(choices.Select(p=>RouteNaming.KindCode(p.Kind,AuxiliaryEndpoint(p.Exit)!=null)+" · "+Code(p.Entry)+" → "+Code(p.Exit)).ToList(),
                Array.IndexOf(choices,selectedProposal),i=>{selectedProposal=choices[i];RefreshWorkspaceFields();}));
            var selected=selectedProposal;
            c.AddLabel(Math.Round(selected.Length)+" m · "+(selected.Switches.Count==0?"keine Weichen":
                string.Join(", ",selected.Switches.Select(s=>PointAssignments.Caption(desks,s.Node)+(s.Thrown?" −":" +")))));
            c.AddButton("Vorschlag speichern",()=>Act(()=>AcceptRouteProposals(new[]{selected})));
            if(choices.Length>1) {
                if(confirmingRouteBatch) {
                    c.AddLabel(choices.Length+" Vorschläge als Fahrstraßen speichern?");
                    c.AddButton("Speichern bestätigen",()=>Act(()=>{if(confirmingRouteBatch)AcceptRouteProposals(choices);}));
                    c.AddButton("Abbrechen",()=>{confirmingRouteBatch=false;RefreshWorkspaceFields();});
                } else c.AddButton("Alle Vorschläge speichern …",()=>{confirmingRouteBatch=true;RefreshWorkspaceFields();});
            }
        }
    }
}
