using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string registrationSignature,registrationDesk,registrationSeed="",registrationRadius="1000",registrationMessage;
        DeskLibrary registrationLibrary;
        string[] registrationFound=Array.Empty<string>();
        void RefreshRegisteredSignals() {
            if(desks==null)return;
            string signature=scope+":"+SharedDocuments.Hash(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(inventory.Values.OrderBy(s=>s.Id))));
            if(signature==registrationSignature&&ReferenceEquals(desks,registrationLibrary))return;
            var next=desks.Clone();bool changed=false;
            foreach(var desk in next.Desks){var result=DiagramRegistration.Inspect(desk.Panel,inventory.Values,false);if(result.Refreshed>0){desk.Panel=result.Panel;changed=true;}}
            if(changed){deskStore.Save(next);desks=next;panel=DeskPanel(CurrentDesk);panelView?.ReplaceDocument(panel);nextPanelRead=0;}
            registrationSignature=signature;
            registrationLibrary=desks;
        }
        void BuildDiagramRegistration(UIPanelBuilder c) {
            string worldScope=scope;
            c.AddSection(WindowText("Find signals for the existing diagram","Signale für vorhandenes Gleisbild suchen"));
            var choices=desks.Desks.OrderBy(d=>d.IsMaster).ThenBy(d=>d.Code).ToArray();
            if(!choices.Any(d=>d.Id==registrationDesk))registrationDesk=CurrentDesk.Id;
            c.AddSearchChoice(WindowText("Existing diagram","Vorhandenes Gleisbild"),choices.Select(d=>d.Code+" · "+d.Name).ToList(),Array.FindIndex(choices,d=>d.Id==registrationDesk),i=>{registrationDesk=choices[i].Id;registrationFound=Array.Empty<string>();RefreshWorkspaceFields();});
            c.AddLabel(WindowText("Search near the signals already linked to this diagram. With no linked signal, choose one station signal as the starting point. The existing drawing is retained.","Sucht in der Umgebung bereits zugeordneter Signale dieser Tafel. Ohne Zuordnung ein Bahnhofssignal als Start wählen. Die vorhandene Zeichnung bleibt erhalten."));
            var signals=inventory.Values.Where(s=>s.IsSignal).OrderBy(s=>s.Name).ToArray();
            c.AddSearchChoice(WindowText("Optional starting signal","Optionales Startsignal"),new[]{WindowText("Use linked diagram signals","Zugeordnete Tafelsignale verwenden")}.Concat(signals.Select(s=>s.Name+" · "+s.DisplayTitle)).ToList(),Math.Max(0,Array.FindIndex(signals,s=>s.Id==registrationSeed)+1),i=>registrationSeed=i==0?"":signals[i-1].Id);
            c.AddField(WindowText("Search distance along track (m)","Suchweite entlang der Gleise (m)"),c.AddInputField(registrationRadius,v=>registrationRadius=v,"1000",6));
            c.AddButton(WindowText("Find station signals and detection boundaries","Bahnhofssignale und GFM-Grenzen suchen"),()=>Act(()=>{
                RequirePanelEdit();if(scope!=worldScope)throw new InvalidOperationException(WindowText("World changed. Reopen recognition.","Welt gewechselt. Erkennung erneut öffnen."));var target=desks.Desks.Single(d=>d.Id==registrationDesk);
                if(!double.TryParse(registrationRadius.Replace(',','.'),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double radius)||double.IsNaN(radius)||double.IsInfinity(radius)||radius<100||radius>10000)
                    throw new InvalidOperationException(WindowText("Search distance must be 100–10000 metres.","Suchweite muss 100–10000 Meter betragen."));
                var ids=new HashSet<string>(target.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.Signal).SelectMany(t=>new[]{t.Reference,t.AlternateSignal}).Where(id=>!string.IsNullOrEmpty(id)));
                if(registrationSeed!="")ids=new HashSet<string>{registrationSeed};
                var input=CaptureRecognitionNetwork();var seeds=input.Cuts.Where(cut=>cut.Signal!=null&&ids.Contains(cut.Signal)).ToList();
                if(seeds.Count==0)throw new InvalidOperationException(WindowText("Choose a starting signal in this station first.","Zuerst ein Startsignal in diesem Bahnhof wählen."));
                var edges=new HashSet<string>(NetworkScope.AroundSignals(new NetworkInput{World=input.World,Nodes=input.Nodes,Edges=input.Edges,Cuts=seeds},radius).Edges.Select(e=>e.Id));
                var found=inventory.Values.Where(s=>s.IsSignal&&edges.Contains(s.Anchor.Segment)).OrderBy(s=>s.Name).ToArray();registrationFound=found.Select(s=>s.Id).ToArray();
                var proposed=DiagramRegistration.Inspect(target.Panel,found,true);int boundaries=input.Cuts.Where(cut=>cut.Boundary!=null&&edges.Contains(cut.Segment)).Select(cut=>cut.Boundary).Distinct().Count();
                if(proposed.Refreshed>0){RequireIdleAdministration();Entry.Shared.CheckpointInstallation();var next=desks.Clone();next.Desks.Single(d=>d.Id==target.Id).Panel=proposed.Panel;deskStore.Save(next);desks=next;panel=DeskPanel(CurrentDesk);panelView?.ReplaceDocument(panel);}
                registrationMessage=WindowText("Found signals / active boundaries: ","Gefundene Signale / aktive Grenzen: ")+found.Length+" / "+boundaries+WindowText(" · Newly linked fields: "," · Neu zugeordnete Felder: ")+proposed.Assigned+WindowText(" · Updated fields: "," · Aktualisierte Felder: ")+proposed.Refreshed+WindowText(" · Unresolved fields: "," · Nicht zugeordnete Felder: ")+proposed.Unresolved;
                RefreshWorkspaceFields();
            }));
            if(registrationMessage!=null)c.AddLabel(registrationMessage);
            if(registrationFound.Length>0)c.AddLabel(string.Join(", ",registrationFound.Take(40).Select(Code))+(registrationFound.Length>40?" …":""));
            c.AddLabel(WindowText("Existing IDs and unique matching field names can be linked. Anonymous symbols need a world assignment; schematic coordinates alone do not identify a physical signal. Detection on connecting tracks is configured separately.","Vorhandene IDs und eindeutige passende Feldnamen werden zugeordnet. Unbenannte Symbole benötigen eine Weltzuordnung; schematische Koordinaten identifizieren kein reales Signal. Die Freimeldung der verbindenden Gleise wird separat eingerichtet."));
        }
    }
}
