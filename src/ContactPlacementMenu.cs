using System;
using System.Linq;
using Track;
using UnityEngine;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        AutomationContact contactPlacementDraft,contactEditorDraft;
        string contactPlacementWorld,contactEditorWorld,contactPlacementMessage;
        void PrepareContactPlacement(Location location) {
            RequireEdit();if(!location.IsValid)throw new InvalidOperationException(LocalText.Select("No valid track selected.","Kein gültiges Gleis gewählt."));
            var at=location.WithEnd(TrackSegment.End.A);
            var draft=new AutomationContact{Anchor=new MarkerAnchor{Segment=at.segment.id,Meters=at.distance,Lateral=.82,Height=-.04},Name=LocalText.Select("Contact ","Kontakt ")+((Automation?.Contacts.Count??0)+1),ShowCounter=true};
            if(Camera.main!=null&&new MarkerTrackGeometry().Resolve(draft.Anchor,out _,out var rotation,out _)) {
                var tangent=rotation*Vector3.forward;var view=Camera.main.transform.forward;
                if(view.x*view.x+view.z*view.z<.0001f)view=Camera.main.transform.up;
                draft.Reverse=PlacementDirection.Reverse(tangent.x,tangent.z,view.x,view.z);
                draft.Anchor.Lateral=PlacementDirection.Side(.82,true,draft.Reverse);
            }
            contactPlacementDraft=draft;contactPlacementWorld=scope;DetectContactStart(draft);automationPage=2;OpenAutomation();
        }
        void DetectContactStart(AutomationContact contact) {
            var found=ContactSignalPolicy.NextMains(world.RoutePlanningGraph(),SignalObjects,contact);
            contact.Signal=found.Length==1?found[0]:"";contact.AutomaticSignal=found.Length==1;
            contactPlacementMessage=found.Length==1?LocalText.Select("First main signal detected: ","Erstes Hauptsignal erkannt: ")+Code(contact.Signal):
                found.Length==0?LocalText.Select("No complete unique main-signal detection. Check direction or assign in the world.","Kein vollständig erkennbares Hauptsignal. Richtung prüfen oder in der Welt zuordnen."):
                LocalText.Select("Several possible first main signals. Assign the intended signal in the world.","Mehrere mögliche erste Hauptsignale. Gewünschtes Signal in der Welt zuordnen.");
        }
        void BuildContactPlacement(UIPanelBuilder c) {
            var draft=contactPlacementDraft;bool current()=>ReferenceEquals(draft,contactPlacementDraft)&&scope==contactPlacementWorld;
            c.AddSection(LocalText.Select("Configure contact before building","Kontakt vor dem Bauen einrichten"));
            c.AddField(LocalText.Select("Name","Bezeichnung"),c.AddInputField(draft.Name,v=>draft.Name=v,"",48));
            c.AddFieldToggle(LocalText.Select("Reverse direction","Gegenrichtung"),()=>draft.Reverse,v=>{if(!current())return;draft.Reverse=v;draft.Anchor.Lateral=PlacementDirection.Side(.82,true,v);DetectContactStart(draft);RefreshWorkspaceFields();},true);
            c.AddField(LocalText.Select("Start signal","Startsignal"),()=>string.IsNullOrEmpty(draft.Signal)?LocalText.Select("Not assigned","Nicht zugeordnet"):Code(draft.Signal),UIPanelBuilder.Frequency.Periodic);
            c.AddLabel(contactPlacementMessage??"");
            c.AddButton(LocalText.Select("Detect first main signal","Erstes Hauptsignal erkennen"),()=>Act(()=>{if(!current())return;DetectContactStart(draft);RefreshWorkspaceFields();}));
            c.AddButton(LocalText.Select("Assign signal in world…","Signal in der Welt zuordnen …"),()=>{if(!current())return;BeginWorldSignalPick(id=>{draft.Signal=id;draft.AutomaticSignal=false;},()=>{automationPage=2;OpenAutomation();});});
            c.AddFieldToggle(LocalText.Select("Count experimental axle test","Experimentellen Achstest zählen"),()=>draft.AxleTest,v=>draft.AxleTest=v,true);
            c.AddFieldToggle(LocalText.Select("Show axle-test display on contact","Achstest-Display am Kontakt anzeigen"),()=>draft.ShowCounter,v=>draft.ShowCounter=v,true);
            c.AddFieldToggle(LocalText.Select("Enable contact requests","Kontaktanforderungen aktivieren"),()=>draft.Enabled,v=>draft.Enabled=v,true);
            c.AddLabel(LocalText.Select("Nothing has been built yet. Target automation is configured at the detected signal. Counting and display do not prove track clearance.","Es wurde noch nichts gebaut. Zielautomatik am erkannten Signal einrichten. Zähler und Display sind kein Gleisfreimeldenachweis."));
            c.AddButton(LocalText.Select("Build configured contact","Eingerichteten Kontakt bauen"),()=>Act(()=>{
                if(!current())return;RequireEdit();draft.Validate();
                if(draft.Signal!=""&&!ContactSignalPolicy.IsNextMain(world.RoutePlanningGraph(),SignalObjects,draft,draft.Signal))throw new InvalidOperationException(LocalText.Select("The selected signal is no longer a first main signal. Detect it again.","Das gewählte Signal ist kein erstes Hauptsignal mehr. Erneut erkennen."));
                if(!new MarkerTrackGeometry().Resolve(draft.Anchor,out _,out _,out string error))throw new InvalidOperationException(error);
                SaveAutomation(d=>{d.Automation.Contacts.Add(draft);if(draft.Signal!=""&&!d.Automation.Signals.ContainsKey(draft.Signal))d.Automation.Signals[draft.Signal]=new AutomaticSignal();});
                contactPlacementDraft=null;contactEditorDraft=null;OpenAutomationContact(draft.Id);status=LocalText.Select("Contact built. Configure target automation at its start signal.","Kontakt gebaut. Zielautomatik am Startsignal einrichten.");
            }));
            c.AddButton(LocalText.Select("Cancel without building","Abbrechen ohne zu bauen"),()=>{contactPlacementDraft=null;RefreshWorkspaceFields();});
        }
        internal void BuildSignalContactStatus(UIPanelBuilder c,string signal) {
            var contacts=Automation?.Contacts.Where(a=>a.Signal==signal).ToArray()??Array.Empty<AutomationContact>();
            if(contacts.Length==0)return;
            c.AddField(LocalText.Select("Automatic triggering detected","Automatische Ansteuerung erkannt"),string.Join(", ",contacts.Select(a=>a.Name+(a.Enabled?"":" · "+LocalText.Select("disabled","aus")))));
            c.AddButton(LocalText.Select("Configure target-signal automation","Zielsignal-Automatik einrichten"),()=>{automationSignal=signal;automationPage=1;OpenAutomation();});
        }
    }
}
