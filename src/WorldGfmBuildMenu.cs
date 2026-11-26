using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UI.Builder;
using UnityEngine;
using Game.State;
using Helpers;
using Track;
namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerRuntime {
        sealed class WorldGfmData {
            public readonly TrackBlock[] Blocks;
            public readonly Dictionary<string,int> Palette;
            public readonly List<string> Names;
            public WorldGfmData(TrackBlock[] blocks,CancellationToken cancel) {
                Blocks=blocks.OrderBy(b=>b.Name).ToArray();
                Palette=GfmWorldPalette.Assign(blocks,cancel);
                Names=Blocks.Select(b=>b.Name).ToList();
            }
        }
        readonly GfmWorldOverlay gfmWorld=new GfmWorldOverlay();
        bool showGfmWorld,worldGfmDirty;
        string worldGfmSelection,worldGfmError;
        object worldGfmSource;
        WorldGfmData worldGfmData;
        CancellationTokenSource worldGfmCancel;
        Task<WorldGfmData> worldGfmTask;
        Graph worldGfmGraph;
        long worldGfmInstallation=-1;
        Vector3 worldGfmCamera;
        float worldGfmNext;

        internal void ShowWorldGfm(string id) {
            if(!CanEdit(out var reason))throw new InvalidOperationException(reason);
            OpenFromPicker(WorkspaceMode.Build);
            if(!buildMode.Active)return;
            EnableWorldGfm(id);
            RebuildTool();
        }
        void EnableWorldGfm(string id) {
            if(!showGfmWorld&&Entry.WantsSignalEvents)Entry.RecordSignalEvent("gfm_overlay_state",new{enabled=true});
            showGfmWorld=true;
            worldGfmSelection=id;
            worldGfmDirty=true;worldGfmNext=0;
        }
        void CancelWorldGfmPreparation() {
            worldGfmCancel?.Cancel();worldGfmCancel?.Dispose();
            worldGfmCancel=null;worldGfmTask=null;
        }
        void ClearWorldGfm() {
            if(!showGfmWorld)return;
            showGfmWorld=false;
            CancelWorldGfmPreparation();
            gfmWorld.Dispose();
            worldGfmSource=null;worldGfmData=null;worldGfmGraph=null;
            worldGfmSelection=null;worldGfmError=null;
            if(Entry.WantsSignalEvents)Entry.RecordSignalEvent("gfm_overlay_state",new{enabled=false});
        }

        void TickWorldGfm() {
            if(!showGfmWorld)return;
            var shared=Entry.Shared;
            if(!buildMode.Active||!StateManager.IsHost||StateManager.IsUnloading||shared==null||shared.WorldKey!=scope||!CanEdit(out _)){ClearWorldGfm();return;}
            try {
                if(!PrepareWorldGfmData(shared))return;
                var graph=Graph.Shared;
                if(graph!=worldGfmGraph) {
                    gfmWorld.Dispose();
                    worldGfmGraph=graph;worldGfmDirty=true;worldGfmNext=0;
                }
                if(graph==null)return;
                var origin=Vector3.zero.GameToWorld();
                gfmWorld.FollowOrigin(origin);
                float now=Time.realtimeSinceStartup;
                if(now>=worldGfmNext) {
                    worldGfmNext=now+.25f;
                    var camera=Camera.main;
                    if(camera!=null) {
                        var at=camera.transform.position-origin;
                        float moved=(at-worldGfmCamera).sqrMagnitude;
                        if(worldGfmDirty||moved>=400*400||!gfmWorld.Busy&&(moved>=30*30||worldGfmInstallation!=shared.InstallationVersion)) {
                            worldGfmCamera=at;worldGfmInstallation=shared.InstallationVersion;
                            worldGfmDirty=false;
                            gfmWorld.Begin(graph,worldGfmData.Blocks,worldGfmData.Palette,at,worldGfmSelection,SelectWorldGfm);
                        }
                    }
                }
                gfmWorld.Step();
            }catch(Exception error){
                CancelWorldGfmPreparation();
                gfmWorld.Dispose();
                worldGfmError=error.Message;
            }
        }

        bool PrepareWorldGfmData(SharedRuntime shared) {
            var source=shared.GfmWorldDataVersion;
            if(!ReferenceEquals(source,worldGfmSource)) {
                CancelWorldGfmPreparation();
                gfmWorld.Dispose();
                worldGfmSource=source;worldGfmData=null;worldGfmError=null;
                if(source==null)return false;
                var blocks=shared.GfmWorldBlocks();
                worldGfmCancel=new CancellationTokenSource();
                var cancel=worldGfmCancel.Token;
                // Only an isolated copy of the saved sections is read by the worker.
                worldGfmTask=Task.Run(()=>new WorldGfmData(blocks,cancel),cancel);
                worldGfmTask.ContinueWith(task=>{var ignored=task.Exception;},TaskContinuationOptions.OnlyOnFaulted);
                RebuildTool();
            }
            if(worldGfmTask?.IsCompleted==true) {
                worldGfmData=worldGfmTask.GetAwaiter().GetResult();
                CancelWorldGfmPreparation();
                if(worldGfmSelection!=null&&!worldGfmData.Palette.ContainsKey(worldGfmSelection))worldGfmSelection=null;
                worldGfmDirty=true;worldGfmNext=0;
                RebuildTool();
            }
            return worldGfmData!=null&&worldGfmError==null;
        }
        void SelectWorldGfm(string id) {
            if(!showGfmWorld||!buildMode.Active||!CanEdit(out _)||id==worldGfmSelection||
                worldGfmData?.Palette.ContainsKey(id)!=true||!ReferenceEquals(Entry.Shared.GfmWorldDataVersion,worldGfmSource))return;
            worldGfmSelection=id;
            worldGfmDirty=true;worldGfmNext=0;
            RebuildTool();
        }
        void BuildWorldGfm(UIPanelBuilder c) {
            c.AddButton(showGfmWorld?LocalText.Select("Hide GFM sections","GFM-Abschnitte ausblenden"):LocalText.Select("Show GFM sections and boundaries","GFM-Abschnitte und Grenzen anzeigen"),()=>Act(()=>{
                string reason=null;if(!buildMode.Active||!CanEdit(out reason))throw new InvalidOperationException(reason??LocalText.Select("Build mode required.","Baumodus erforderlich."));
                if(showGfmWorld)ClearWorldGfm();else{edit.Cancel();scene.HidePreview();EnableWorldGfm(null);}RebuildTool();
            }));
            if(!showGfmWorld)return;
            c.AddLabel(LocalText.Select("Colours distinguish saved sections, not occupancy or permission. Click a coloured track. End posts mark saved path limits; they do not show C direction. Uncoloured track is not confirmed clear. Display range: 400 m; capped for performance.","Farben unterscheiden gespeicherte Abschnitte, keine Belegung oder Erlaubnis. Farbiges Gleis anklicken. Endpfosten markieren gespeicherte Weggrenzen; sie zeigen keine C-Wirkrichtung. Unmarkiertes Gleis ist nicht als frei bestätigt. Anzeigebereich: 400 m; aus Leistungsgründen begrenzt."));
            c.AddField(LocalText.Select("Track display","Gleisanzeige"),WorldGfmStatus,UIPanelBuilder.Frequency.Periodic);
            var data=worldGfmData;
            var shared=Entry.Shared;
            if(data==null||shared==null||!ReferenceEquals(shared.GfmWorldDataVersion,worldGfmSource))return;
            var choices=data.Blocks;
            if(choices.Length==0){c.AddLabel(LocalText.Select("No saved sections. Use GFM setup to detect and assign them first.","Keine gespeicherten Abschnitte. Zuerst mit der GFM-Einrichtung erkennen und zuordnen."));return;}
            c.AddSearchChoice(LocalText.Select("Saved section","Gespeicherter Abschnitt"),data.Names,Math.Max(0,Array.FindIndex(choices,b=>b.Id==worldGfmSelection)),i=>SelectWorldGfm(choices[i].Id));
            var block=choices.FirstOrDefault(b=>b.Id==worldGfmSelection);if(block==null)return;
            c.AddLabel(block.Name+" · "+SignUnits.Number(block.Sections.Sum(s=>s.To-s.From))+" m");
            if(block.IsCircuit){c.AddLabel(LocalText.Select("Boundary A: ","Grenze A: ")+Entry.MarkerCaption(block.BoundaryA));c.AddLabel(LocalText.Select("Boundary B: ","Grenze B: ")+Entry.MarkerCaption(block.BoundaryB));}
            var owners=shared.GfmWorldDesks(block.Id,out var desks);
            c.AddLabel(LocalText.Select("Linked interlockings: ","Verknüpfte Stellwerke: ")+(owners.Length==0?LocalText.Select("none","keine"):string.Join(", ",owners.Select(d=>d.Code+" · "+d.Name))));
            foreach(var owner in (owners.Length>0?owners:desks)) {
                string id=block.Id,desk=owner.Id,world=scope;
                var source=worldGfmSource;
                c.AddButton(LocalText.Select("Inspect / edit diagram assignment: ","Prüfen / Gleisbild-Zuordnung bearbeiten: ")+owner.Code,()=>Act(()=>{
                    if(!showGfmWorld||!buildMode.Active||scope!=world||id!=worldGfmSelection||!CanEdit(out _)||
                        !ReferenceEquals(Entry.Shared?.GfmWorldDataVersion,source))
                        throw new InvalidOperationException(LocalText.Select("Selection is outdated. Select the section again.","Auswahl ist veraltet. Abschnitt erneut auswählen."));
                    Entry.OpenWorldGfm(id,desk);
                }));
            }
            c.AddLabel(LocalText.Select("Edit boundaries and assignments through the existing checked editor. No arbitrary track cuts or automatic repairs are applied here.","Grenzen und Zuordnungen über den bestehenden geprüften Editor bearbeiten. Hier werden keine beliebigen Gleisschnitte oder automatischen Reparaturen angewendet."));
        }
        string WorldGfmStatus() {
            string error=worldGfmError??gfmWorld.Error;
            if(error!=null)return LocalText.Select("Overlay unavailable: ","Abschnittsanzeige nicht verfügbar: ")+error;
            string result=LocalText.Select("Visible sections: ","Sichtbare Abschnitte: ")+gfmWorld.Drawn;
            if(worldGfmData==null||worldGfmDirty||gfmWorld.Busy)return result+LocalText.Select(" · Building view…"," · Ansicht wird aufgebaut …");
            return result+LocalText.Select(" · Unavailable or limited pieces: "," · Nicht verfügbare oder begrenzte Teilstücke: ")+gfmWorld.Skipped;
        }
    }
}
