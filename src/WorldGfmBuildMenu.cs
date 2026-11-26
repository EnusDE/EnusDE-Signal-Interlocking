using System;
using System.Linq;
using UI.Builder;
using UnityEngine;
using Game.State;
namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerRuntime {
        readonly GfmWorldOverlay gfmWorld=new GfmWorldOverlay();
        bool showGfmWorld;string worldGfmSelection,worldGfmError;long worldGfmConfiguration=-1,worldGfmInstallation=-1;Vector3 worldGfmCamera;float worldGfmNext;
        internal void ShowWorldGfm(string id) {
            if(!CanEdit(out var reason))throw new InvalidOperationException(reason);
            OpenFromPicker(WorkspaceMode.Build);
            if(!buildMode.Active)return;
            showGfmWorld=true;worldGfmConfiguration=-1;worldGfmNext=0;SelectWorldGfm(id);
        }
        void ClearWorldGfm(){showGfmWorld=false;worldGfmSelection=null;worldGfmError=null;gfmWorld.Dispose();worldGfmConfiguration=-1;}
        void TickWorldGfm() {
            if(!showGfmWorld)return;
            if(!buildMode.Active||!StateManager.IsHost||StateManager.IsUnloading||Entry.Shared?.WorldKey!=scope||!CanEdit(out _)){ClearWorldGfm();return;}
            gfmWorld.FollowOrigin();if(Time.realtimeSinceStartup<worldGfmNext)return;worldGfmNext=Time.realtimeSinceStartup+1;
            var camera=Camera.main;var at=camera==null?Vector3.zero:camera.transform.position;
            if(worldGfmConfiguration==Entry.Shared.ConfigurationVersion&&worldGfmInstallation==Entry.Shared.InstallationVersion&&Vector3.Distance(at,worldGfmCamera)<30)return;
            try {
                var library=Entry.Shared.Desks();var blocks=library.Blocks.Values.ToArray();
                if(!blocks.Any(b=>b.Id==worldGfmSelection))worldGfmSelection=null;
                gfmWorld.Draw(blocks,worldGfmSelection,SelectWorldGfm);
                worldGfmConfiguration=Entry.Shared.ConfigurationVersion;worldGfmInstallation=Entry.Shared.InstallationVersion;worldGfmCamera=at;worldGfmError=null;
            }catch(Exception e){gfmWorld.Dispose();worldGfmError=e.Message;}
            RebuildTool();
        }
        void SelectWorldGfm(string id) {
            if(!showGfmWorld||!buildMode.Active||!CanEdit(out _))return;
            worldGfmSelection=id;worldGfmConfiguration=-1;worldGfmNext=0;TickWorldGfm();RebuildTool();
        }
        void BuildWorldGfm(UIPanelBuilder c) {
            c.AddButton(showGfmWorld?LocalText.Select("Hide GFM sections","GFM-Abschnitte ausblenden"):LocalText.Select("Show GFM sections and boundaries","GFM-Abschnitte und Grenzen anzeigen"),()=>Act(()=>{
                string reason=null;if(!buildMode.Active||!CanEdit(out reason))throw new InvalidOperationException(reason??LocalText.Select("Build mode required.","Baumodus erforderlich."));
                if(showGfmWorld)ClearWorldGfm();else{edit.Cancel();scene.HidePreview();showGfmWorld=true;worldGfmConfiguration=-1;worldGfmNext=0;TickWorldGfm();}RebuildTool();
            }));
            if(!showGfmWorld)return;
            c.AddLabel(LocalText.Select("Colours distinguish saved sections, not occupancy or permission. Click a coloured track. End posts mark saved path limits; they do not show C direction. Uncoloured track is not confirmed clear. Display range: 400 m; capped for performance.","Farben unterscheiden gespeicherte Abschnitte, keine Belegung oder Erlaubnis. Farbiges Gleis anklicken. Endpfosten markieren gespeicherte Weggrenzen; sie zeigen keine C-Wirkrichtung. Unmarkiertes Gleis ist nicht als frei bestätigt. Anzeigebereich: 400 m; aus Leistungsgründen begrenzt."));
            c.AddLabel(LocalText.Select("Visible sections: ","Sichtbare Abschnitte: ")+gfmWorld.Drawn+LocalText.Select(" · Unavailable or limited pieces: "," · Nicht verfügbare oder begrenzte Teilstücke: ")+gfmWorld.Skipped);
            if(worldGfmError!=null)c.AddLabel(LocalText.Select("Overlay unavailable: ","Abschnittsanzeige nicht verfügbar: ")+worldGfmError);
            var library=Entry.Shared?.Desks();var choices=library?.Blocks.Values.OrderBy(b=>b.Name).ToArray();if(choices==null||choices.Length==0){c.AddLabel(LocalText.Select("No saved sections. Use GFM setup to detect and assign them first.","Keine gespeicherten Abschnitte. Zuerst mit der GFM-Einrichtung erkennen und zuordnen."));return;}
            c.AddSearchChoice(LocalText.Select("Saved section","Gespeicherter Abschnitt"),choices.Select(b=>b.Name).ToList(),Math.Max(0,Array.FindIndex(choices,b=>b.Id==worldGfmSelection)),i=>SelectWorldGfm(choices[i].Id));
            var block=choices.FirstOrDefault(b=>b.Id==worldGfmSelection);if(block==null)return;
            c.AddLabel(block.Name+" · "+SignUnits.Number(block.Sections.Sum(s=>s.To-s.From))+" m");
            if(block.IsCircuit){c.AddLabel(LocalText.Select("Boundary A: ","Grenze A: ")+Entry.MarkerCaption(block.BoundaryA));c.AddLabel(LocalText.Select("Boundary B: ","Grenze B: ")+Entry.MarkerCaption(block.BoundaryB));}
            var owners=library.Desks.Where(d=>d.Panel.Tiles.Any(t=>GfmCoverage.Ports(t,new System.Collections.Generic.HashSet<string>{block.Id})!=0)).ToArray();
            c.AddLabel(LocalText.Select("Linked interlockings: ","Verknüpfte Stellwerke: ")+(owners.Length==0?LocalText.Select("none","keine"):string.Join(", ",owners.Select(d=>d.Code+" · "+d.Name))));
            foreach(var owner in (owners.Length>0?owners:library.Desks.ToArray())) {
                string id=block.Id,desk=owner.Id,world=scope;long version=Entry.Shared.ConfigurationVersion,installation=Entry.Shared.InstallationVersion;
                c.AddButton(LocalText.Select("Inspect / edit diagram assignment: ","Prüfen / Gleisbild-Zuordnung bearbeiten: ")+owner.Code,()=>Act(()=>{
                    if(!buildMode.Active||scope!=world||Entry.Shared.ConfigurationVersion!=version||Entry.Shared.InstallationVersion!=installation)throw new InvalidOperationException(LocalText.Select("Selection is outdated. Select the section again.","Auswahl ist veraltet. Abschnitt erneut auswählen."));
                    Entry.OpenWorldGfm(id,desk);
                }));
            }
            c.AddLabel(LocalText.Select("Edit boundaries and assignments through the existing checked editor. No arbitrary track cuts or automatic repairs are applied here.","Grenzen und Zuordnungen über den bestehenden geprüften Editor bearbeiten. Hier werden keine beliebigen Gleisschnitte oder automatischen Reparaturen angewendet."));
        }
    }
}
