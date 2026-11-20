using System;
using System.Linq;
using UI.Builder;
namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        void BuildLineSignalGroup(UIPanelBuilder c,LineTrackDefinition line,LineSignalGroup group,string caption,Func<bool> current) {
            c.AddSection(caption);var selected=LineSignalGroups.Get(line,group);
            foreach(string id in selected) {
                string chosen=id;c.AddButton(WindowText("Remove ","Entfernen: ")+Code(id),()=>{if(!current())return;LineSignalGroups.Put(line,group,selected.Where(s=>s!=chosen));ResetLineDetection(line);RefreshWorkspaceFields();});
            }
            string station=LineSignalGroups.Station(line,group);bool departure=LineSignalGroups.Departure(group);
            var signals=SignalObjects.Where(s=>s.Kind==MarkerRecord.Main&&!s.Planned&&SignalStation(s.Id)?.Id==station).Select(s=>s.Id);
            var targets=departure?Enumerable.Empty<string>():AuxiliaryTarget.Index(desks.Desks.SelectMany(d=>d.Panel.Tiles)).Where(p=>p.Value.Function==AuxiliaryFunction.RouteDestination&&p.Value.Train&&StationAutomationPolicy.Owner(desks,p.Key)?.Id==station).Select(p=>p.Key);
            var choices=signals.Concat(targets).Where(id=>!selected.Contains(id)).OrderBy(Code).ToArray();
            void Add(string id){if(!current()||LineSignalGroups.Station(line,group)!=station)throw new InvalidOperationException(WindowText("Line setup changed. Select again.","Streckeneinrichtung geändert. Erneut auswählen."));if(!LineSignalGroups.AcceptsEndpoint(RouteEndpoint(id),departure)||StationAutomationPolicy.Owner(desks,id)?.Id!=station)throw new InvalidOperationException(WindowText("Choose an endpoint belonging to this neighbour.","Einen Endpunkt dieser Nachbarbetriebsstelle wählen."));LineSignalGroups.Put(line,group,LineSignalGroups.Get(line,group).Append(id));ResetLineDetection(line);RefreshWorkspaceFields();}
            c.AddButton(WindowText("Select in diagram…","Im Gleisbild auswählen …"),()=>{if(current())PickSignalInDiagram(station,caption,id=>choices.Contains(id),Add);});
            c.AddButton(WindowText("Select signal in world…","Signal in der Welt auswählen …"),()=>{if(current())BeginWorldSignalPick(Add,()=>{ShowWorkspace(true,4,false);EnterEditorDesk(station);WorkspacePage(9);});});
            if(choices.Length==0&&selected.Count==0)c.AddLabel(WindowText("No assigned main signal or train target at this interlocking. Assign it in the local diagram first.","Kein zugeordnetes Hauptsignal oder Zugziel dieser Betriebsstelle. Zuerst im örtlichen Gleisbild zuordnen."));
            c.AddSearchChoice(departure?WindowText("Add departure signal","Ausfahrsignal hinzufügen"):WindowText("Add entry signal / train target","Einfahrsignal / Zugziel hinzufügen"),new[]{WindowText("Choose…","Auswählen …")}.Concat(choices.Select(Code)).ToList(),0,i=>{if(!current()||i==0)return;LineSignalGroups.Put(line,group,selected.Append(choices[i-1]));ResetLineDetection(line);RefreshWorkspaceFields();});
        }
    }
}
