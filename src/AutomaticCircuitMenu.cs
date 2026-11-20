using System;
using System.Collections.Generic;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        sealed class AutomaticCircuitProposal {public string A,B;public CircuitPath Path;public int Fields;}
        readonly List<AutomaticCircuitProposal> automaticCircuitProposals=new List<AutomaticCircuitProposal>();
        string automaticCircuitDesk,automaticCircuitLimit="";int automaticCircuitX,automaticCircuitY,automaticCircuitChoice;
        string DetectDiagramBoundary(PanelTile tile,int? port) {
            if(tile.CircuitBoundary!=null&&CircuitEndpoint(tile.CircuitBoundary)!=null)return tile.CircuitBoundary;
            if(tile.Kind==PanelTileKind.Switch) {
                if(!port.HasValue||!PointCircuitVisibility.Enabled(desks,tile.Reference))return null;
                var arm=CircuitDiagramAssignment.PointArm(tile,port.Value);
                string id=arm.HasValue?TrackCircuit.PointId(tile.Reference,arm.Value):null;
                return id!=null&&CircuitEndpoint(id)?.Name.StartsWith("GFM-",StringComparison.Ordinal)==true?CanonicalBoundaryId(id):null;
            }
            if(tile.Kind==PanelTileKind.Signal&&!tile.ProjectedMastFace) {
                var ids=new[]{tile.Reference,tile.AlternateSignal}.Select(Object).Where(CircuitIdentity.Enabled)
                    .Select(s=>s.PhysicalId??s.Id).Distinct().ToArray();
                return ids.Length==1?ids[0]:null;
            }
            return null;
        }
        void BuildAutomaticCircuitDetection(UIPanelBuilder c,string desk,PanelTile field) {
            if(field==null||!CircuitDiagramAssignment.RailField(field)&&field.Kind!=PanelTileKind.Signal)return;
            int x=field.X,y=field.Y;
            c.AddSection(LocalText.Get("Gleisabschnitt automatisch erkennen"));
            c.AddField(LocalText.Select("Optional search length (m)","Optionale Suchlänge (m)"),c.AddInputField(automaticCircuitLimit,v=>automaticCircuitLimit=v,LocalText.Select("Empty: no length filter","Leer: kein Längenfilter"),10));
            c.AddLabel(LocalText.Select("Search length only filters proposals. Occupancy always covers the complete saved boundary-to-boundary section until the whole train has left it.","Die Suchlänge filtert nur Vorschläge. Die Belegung gilt immer für den vollständig gespeicherten Abschnitt zwischen den Grenzen, bis der ganze Zug ihn verlassen hat."));
            c.AddLabel(LocalText.Get("Sucht vom Feld bis zu den nächsten eingerichteten GFM-Grenzen. Kein Überqueren von Weichen. Weichengrenzen werden nur bei aktivierter Weichen-GFM angeboten. Vorschläge ändern noch keine Zuordnung."));
            c.AddButton(LocalText.Get("Gleisabschnitt erkennen"),()=>Act(()=>{
                RequirePanelEdit();if(deskId!=desk)throw new InvalidOperationException("Feld erneut auswählen.");
                double limit=double.PositiveInfinity;
                if(!string.IsNullOrWhiteSpace(automaticCircuitLimit)&&(!double.TryParse(automaticCircuitLimit.Replace(',','.'),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out limit)||double.IsNaN(limit)||double.IsInfinity(limit)||limit<1||limit>100000))
                    throw new InvalidOperationException(LocalText.Select("Enter 1–100000 metres, or leave empty for no length filter.","1–100000 Meter eingeben oder für keinen Längenfilter leer lassen."));
                automaticCircuitProposals.Clear();automaticCircuitDesk=desk;automaticCircuitX=x;automaticCircuitY=y;automaticCircuitChoice=0;
                var pairs=CircuitDiagramDiscovery.Find(panel,x,y,DetectDiagramBoundary);var network=CaptureNetwork();
                foreach(var pair in pairs) {
                    var a=CircuitEndpoint(pair.A);var b=CircuitEndpoint(pair.B);if(a==null||b==null)continue;
                    PanelTile[] diagram;try{diagram=CircuitDiagramAssignment.Find(panel,x,y,pair.A,pair.B,DiagramBoundary,CanonicalBoundaryId);}catch(InvalidOperationException){continue;}
                    var paths=TrackCircuit.Find(network,a,b,out bool limited);
                    // An incomplete search must not be presented as a complete set of alternatives.
                    if(limited)continue;
                    foreach(var path in paths.Where(p=>p.Length<=limit&&p.Points.Count==0).Take(8-automaticCircuitProposals.Count))
                        automaticCircuitProposals.Add(new AutomaticCircuitProposal{A=pair.A,B=pair.B,Path=path,Fields=diagram.Length});
                }
                status=automaticCircuitProposals.Count==0?"Kein eindeutiger eingerichteter GFM-Abschnitt innerhalb der Länge gefunden. Grenzmarken, Weichen-GFM und Gleisbildverbindungen prüfen.":
                    automaticCircuitProposals.Count+" Abschnittsvorschlag/-vorschläge gefunden. Grenzen und Länge prüfen, dann übernehmen.";
                RefreshWorkspaceFields();
            }));
            if(automaticCircuitDesk!=desk||automaticCircuitX!=x||automaticCircuitY!=y||automaticCircuitProposals.Count==0)return;
            c.AddSearchChoice(LocalText.Get("Erkannte Abschnitte"),automaticCircuitProposals.Select(p=>BoundaryCaption(p.A)+" → "+BoundaryCaption(p.B)+" · "+SignUnits.Number(Math.Round(p.Path.Length,1))+" m · "+p.Fields+" Felder").ToList(),automaticCircuitChoice,i=>{automaticCircuitChoice=i;RefreshWorkspaceFields();});
            var chosen=automaticCircuitProposals[automaticCircuitChoice];
            c.AddLabel(LocalText.Get("Übernehmen ordnet den gesamten gezeichneten Abschnitt einschließlich Zugnummernfeldern und inneren Signal-/Weichenseiten zu. Außenliegende Seiten bleiben erhalten."));
            c.AddButton(LocalText.Get("Vorschlag übernehmen"),()=>Act(()=>{
                RequirePanelEdit();if(deskId!=desk)throw new InvalidOperationException("Feld erneut auswählen.");
                var block=new TrackBlock{Id="c:"+Guid.NewGuid().ToString("N"),Name=BoundaryCaption(chosen.A)+" → "+BoundaryCaption(chosen.B),BoundaryA=chosen.A,BoundaryB=chosen.B,Path=chosen.Path.Sections};
                if(block.Name.Length>80)block.Name=block.Name.Substring(0,80);
                if(!TrackCircuit.EndsMatch(block,CircuitEndpoint(chosen.A),CircuitEndpoint(chosen.B)))throw new InvalidOperationException("Grenzen inzwischen geändert. Erneut erkennen.");
                var existing=desks.Blocks.Values.FirstOrDefault(b=>b.IsCircuit&&TrackCircuit.Pair(b.BoundaryA,b.BoundaryB)==TrackCircuit.Pair(block.BoundaryA,block.BoundaryB)&&
                    b.Path.Select(p=>p.Section.Key).OrderBy(k=>k).SequenceEqual(block.Path.Select(p=>p.Section.Key).OrderBy(k=>k)));
                if(existing!=null)block=existing;
                int count=0;EditPanel(doc=>count=CircuitDiagramAssignment.Assign(doc,x,y,block,DiagramBoundary,CanonicalBoundaryId),addBlock:block);
                automaticCircuitProposals.Clear();ReloadCircuitDraft();status=LocalText.Get("Erkannten GFM-Abschnitt für ")+count+" Felder übernommen · "+block.Name+".";
            }));
        }
    }
}
