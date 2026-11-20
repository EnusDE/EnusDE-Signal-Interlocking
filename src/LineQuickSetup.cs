using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class LineQuickSetup {
        public static string[] Suggest(DeskLibrary library,LineTrackDefinition line,IEnumerable<MarkerRecord> source) {
            var signals=source.Where(s=>s.Kind==MarkerRecord.Main&&!s.Planned).ToArray();var unresolved=new List<string>();
            foreach(var group in LineSignalGroups.Used(line)) {
                if(LineSignalGroups.Get(line,group).Count>0)continue;
                string station=LineSignalGroups.Station(line,group);var duty=LineSignalGroups.Departure(group)?SignalDuty.Exit:SignalDuty.Entry;
                var candidates=signals.Where(s=>SignalRolePolicy.Current(library,s.Id)==duty&&StationAutomationPolicy.Owner(library,s.Id)?.Id==station).ToArray();
                if(candidates.Length==1)LineSignalGroups.Put(line,group,new[]{candidates[0].Id});
                else unresolved.Add((group==LineSignalGroup.ADepartures?LocalText.Select("A departures","A Ausfahrten"):group==LineSignalGroup.BDepartures?LocalText.Select("B departures","B Ausfahrten"):group==LineSignalGroup.AArrivals?LocalText.Select("A arrivals","A Ankünfte"):LocalText.Select("B arrivals","B Ankünfte"))+": "+candidates.Length);
            }
            return unresolved.ToArray();
        }
    }
    public sealed partial class SignalRuntime {
        void BuildLineQuickSetup(UI.Builder.UIPanelBuilder c,LineTrackDefinition draft,Func<bool> current) {
            c.AddSection(WindowText("Quick setup by clicking","Schnelleinrichtung durch Anklicken"));
            c.AddLabel(WindowText("Choose the two neighbours, then recognize unique entry/exit roles. Where several signals fit, select the intended endpoint in its diagram. All results remain a draft until Apply.","Beide Nachbarn wählen, dann eindeutige Ein-/Ausfahrrollen erkennen. Bei mehreren passenden Signalen den gewünschten Endpunkt im Gleisbild anklicken. Alle Ergebnisse bleiben bis Übernehmen ein Entwurf."));
            c.AddButton(WindowText("Complete endpoint assignments automatically","Endpunktzuordnungen automatisch vervollständigen"),()=>Act(()=>{
                if(!current())return;RequirePanelEdit();ResetLineDetection(draft);
                var unresolved=LineQuickSetup.Suggest(desks,draft,SignalObjects);
                status=unresolved.Length==0?WindowText("Endpoints proposed. Detect the path, review detection, then Apply.","Endpunkte vorgeschlagen. Verlauf erkennen, GFM prüfen und anschließend übernehmen."):
                    WindowText("Choose the remaining endpoint groups (number of matching signals): ","Offene Endpunktgruppen auswählen (Anzahl passender Signale): ")+string.Join(" · ",unresolved);
                RefreshWorkspaceFields();
            }));
            if(lineIndicatorTarget!=null)c.AddButton(WindowText("Add read-only neighbour entry indication","Einfahrt des Nachbarn als Anzeige hinzufügen"),()=>Act(()=>{
                if(!current())return;RequirePanelEdit();var owner=desks.Desks.First(d=>d.Id==lineIndicatorTarget.Desk);
                bool a=owner.Id==draft.StationA;string id=a?draft.ForwardEnd:draft.ReverseEnd;
                if(owner.IsMaster||Object(id)?.Kind!=MarkerRecord.Main)throw new InvalidOperationException(WindowText("Select a real neighbour entry signal and a local indicator first.","Zuerst ein reales Einfahrsignal des Nachbarn und einen örtlichen Streckenmelder wählen."));
                if(owner.Panel.Tiles.Any(t=>t.Kind==PanelTileKind.Signal&&t.Reference==id&&t.ReadOnlySignal)){status=WindowText("Neighbour indication already exists.","Nachbaranzeige ist bereits vorhanden.");return;}
                var origin=owner.Panel.At(lineIndicatorTarget.X,lineIndicatorTarget.Y);int dx=origin.Rotation==0?1:origin.Rotation==2?-1:0,dy=origin.Rotation==1?1:origin.Rotation==3?-1:0;
                var next=owner.Panel.Clone();int x=origin.X+dx,y=origin.Y+dy;
                while(x>=0&&y>=0&&x<next.Columns&&y<next.Rows&&next.At(x,y)!=null){x+=dx;y+=dy;}
                if(x<0||y<0||x>=next.Columns||y>=next.Rows)throw new InvalidOperationException(WindowText("No free cell beside this module. Move it or enlarge the panel.","Kein freies Feld neben diesem Modul. Modul verschieben oder Tafel vergrößern."));
                var symbol=PanelEditing.PreviewField(next,x,y,PanelTileKind.Signal);symbol.Reference=id;symbol.ReadOnlySignal=true;symbol.Rotation=(origin.Rotation+(a?0:2))%4;
                SaveAutomation(d=>d.Desks.First(s=>s.Id==owner.Id).Panel.Put(symbol));status=WindowText("Read-only neighbour indication added. Ownership is unchanged.","Nachbaranzeige hinzugefügt. Die Bedienzuständigkeit bleibt erhalten.");RefreshWorkspaceFields();
            }));
        }
    }
}
