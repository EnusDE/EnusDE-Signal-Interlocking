using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly StationCapacityAuthority stationCapacity=new StationCapacityAuthority();
        string capacityDesk,capacityName="",capacityLimit="1";
        readonly HashSet<string> capacityBlocks=new HashSet<string>(),capacityArrivals=new HashSet<string>();
        void BuildStationCapacitySetup(UIPanelBuilder c) {
            var stations=desks.Desks.Where(d=>!d.IsMaster).OrderBy(d=>d.Code).ToArray();if(stations.Length==0)return;
            if(!stations.Any(d=>d.Id==capacityDesk))capacityDesk=stations.FirstOrDefault(d=>d.Id==deskId)?.Id??stations[0].Id;
            var station=stations.Single(d=>d.Id==capacityDesk);string worldScope=scope;
            c.AddSearchChoice(WindowText("Receiving interlocking","Annehmendes Stellwerk"),stations.Select(d=>d.Code+" · "+d.Name).ToList(),Array.IndexOf(stations,station),i=>{capacityDesk=stations[i].Id;capacityBlocks.Clear();capacityArrivals.Clear();RefreshWorkspaceFields();});
            c.AddLabel(WindowText("Optional admission protection. Define separate physical receiving tracks with their complete detection sections and eligible arrival signals. Unknown detection and reserved incoming routes consume capacity. An outgoing route does not free an occupied track.","Optionale Annahmesicherung. Getrennte reale Empfangsgleise mit vollständigen Freimeldeabschnitten und zulässigen Ankunftssignalen definieren. Unbekannte Freimeldung und reservierte Einfahrten belegen Kapazität. Eine Ausfahrstraße gibt ein belegtes Gleis nicht frei."));
            c.AddField(WindowText("Current maximum (0 = disabled)","Aktuelles Maximum (0 = deaktiviert)"),station.MaximumReceivingTrains.ToString());
            foreach(var track in station.ReceivingTracks) {
                string id=track.Id;c.AddLabel(track.Name+" · "+string.Join(", ",track.Arrivals.Select(Code)));c.AddField(WindowText("Track status","Gleiszustand"),()=>stationCapacity.TrackStatus(station.Id,id),UIPanelBuilder.Frequency.Periodic);
                c.AddButton(WindowText("Remove receiving track: ","Empfangsgleis entfernen: ")+track.Name,()=>Act(()=>{RequireIdleAdministration();if(scope!=worldScope)return;SaveAutomation(d=>{var s=d.Desks.Single(v=>v.Id==station.Id);s.ReceivingTracks.RemoveAll(t=>t.Id==id);s.MaximumReceivingTrains=Math.Min(s.MaximumReceivingTrains,s.ReceivingTracks.Count);});RefreshWorkspaceFields();}));
            }
            c.AddField(WindowText("Maximum simultaneous accepted trains","Maximal gleichzeitig angenommene Züge"),c.AddInputField(capacityLimit,v=>capacityLimit=v,"1",2));
            c.AddButton(WindowText("Apply receiving limit (0 disables)","Annahmelimit übernehmen (0 deaktiviert)"),()=>Act(()=>{
                RequireIdleAdministration();if(scope!=worldScope)return;if(!int.TryParse(capacityLimit,out int limit))throw new InvalidOperationException(WindowText("Enter a valid receiving limit.","Gültiges Annahmelimit eingeben."));
                SaveAutomation(d=>d.Desks.Single(s=>s.Id==station.Id).MaximumReceivingTrains=limit);RefreshWorkspaceFields();
            }));
            c.AddSection(WindowText("Add a receiving track","Empfangsgleis hinzufügen"));
            c.AddField(WindowText("Track name","Gleisname"),c.AddInputField(capacityName,v=>capacityName=v,"1",48));
            c.AddButton(WindowText("Select eligible arrival signal in diagram…","Zulässiges Ankunftssignal im Gleisbild wählen …"),()=>PickSignalInDiagram(station.Id,WindowText("Arrival signal for this receiving track","Ankunftssignal für dieses Empfangsgleis"),id=>Object(id)?.Kind==MarkerRecord.Main&&StationAutomationPolicy.Owner(desks,id)?.Id==station.Id,id=>{capacityArrivals.Add(id);RefreshWorkspaceFields();}));
            c.AddLabel(string.Join(", ",capacityArrivals.Select(Code)));
            var used=station.ReceivingTracks.SelectMany(t=>t.Blocks).ToHashSet();
            var owned=station.Panel.Tiles.SelectMany(t=>new[]{t.BlockId,t.SecondBlock}.Concat(t.CircuitRails?.Values??Enumerable.Empty<string>())).ToHashSet();
            foreach(var block in desks.Blocks.Values.Where(b=>b.IsCircuit&&owned.Contains(b.Id)&&!used.Contains(b.Id)).OrderBy(b=>b.Name)) {string id=block.Id;c.AddFieldToggle(block.Name,()=>capacityBlocks.Contains(id),v=>{if(v)capacityBlocks.Add(id);else capacityBlocks.Remove(id);},true);}
            c.AddButton(WindowText("Clear new track selection","Neue Gleisauswahl leeren"),()=>{capacityName="";capacityBlocks.Clear();capacityArrivals.Clear();RefreshWorkspaceFields();});
            c.AddButton(WindowText("Save receiving track","Empfangsgleis speichern"),()=>Act(()=>{
                RequireIdleAdministration();if(scope!=worldScope)return;SaveAutomation(d=>d.Desks.Single(s=>s.Id==station.Id).ReceivingTracks.Add(new ReceivingTrack{Name=capacityName.Trim(),Blocks=capacityBlocks.ToList(),Arrivals=capacityArrivals.ToList()}));capacityName="";capacityBlocks.Clear();capacityArrivals.Clear();RefreshWorkspaceFields();
            }));
            c.AddLabel(WindowText("After a train has entered, its reservation remains until the selected receiving detection was occupied and subsequently proved clear. Include the full arrival path; a detection gap cannot be treated as free capacity. This is not yet automatic passing-loop scheduling.","Nach Einfahrt bleibt die Reservierung bestehen, bis die gewählte Empfangsfreimeldung belegt war und anschließend frei geprüft wurde. Den vollständigen Ankunftsweg einbeziehen; eine Freimeldelücke gilt nicht als freie Kapazität. Dies ist noch keine automatische Kreuzungsplanung."));
        }
    }
}
