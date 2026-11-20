using System;
using System.Collections.Generic;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public static class GfmInspectionOrder {
        public static IEnumerable<TrackBlock> Sort(IEnumerable<TrackBlock> blocks,PanelDocument panel,PanelTile selected,Func<PanelTile,string,bool> boundary) {
            bool Assigned(TrackBlock b)=>selected!=null&&GfmDetectionRefresh.References(selected).Contains(b.Id);
            int Ends(TrackBlock b)=>b.IsCircuit?(panel.Tiles.Any(t=>boundary(t,b.BoundaryA))?1:0)+(panel.Tiles.Any(t=>boundary(t,b.BoundaryB))?1:0):0;
            return blocks.OrderByDescending(Assigned).ThenByDescending(Ends).ThenBy(b=>b.Sections.Sum(s=>s.To-s.From)).ThenBy(b=>b.Id,StringComparer.Ordinal);
        }
    }
    public sealed partial class SignalRuntime {
        void BuildGfmBoundaryDetails(UIPanelBuilder c,string id,bool first) {
            c.AddSection(first?WindowText("Boundary 1 · origin","Grenze 1 · Herkunft"):WindowText("Boundary 2 · origin","Grenze 2 · Herkunft"));
            var end=CircuitEndpoint(id);
            c.AddLabel(BoundaryCaption(id));
            if(end==null){
                var raw=RawGfm(id);var fault=GfmFault(id);c.AddLabel(GfmFaults.Reason(fault));c.AddLabel(id??"—");
                if(raw!=null){c.AddLabel(GfmFaults.Position(raw.Anchor));string captured=raw.Id;c.AddButton(WindowText("Open this world object's properties","Eigenschaften dieses Weltobjekts öffnen"),()=>Act(()=>{RequireFreshGfmInspection();Entry.OpenMarker(captured);}));}
                c.AddLabel(fault==GfmBoundaryFault.Planned?WindowText("Finish construction at this location. The saved section is retained while its plan is incomplete.","Bau an dieser Stelle fertigstellen. Der gespeicherte Abschnitt bleibt während des unfertigen Bauplans erhalten."):
                    fault==GfmBoundaryFault.Disabled?WindowText("Re-enable this detector or explicitly replace the obsolete section below.","Diesen Melder wieder aktivieren oder den veralteten Abschnitt unten ausdrücklich ersetzen."):
                    WindowText("Review the saved track intervals above. Find a complete replacement between active boundaries or preview removal of obsolete assignments below.","Gespeicherte Gleisbereiche oben prüfen. Einen vollständigen Ersatz zwischen aktiven Grenzen suchen oder das Entfernen veralteter Zuordnungen unten vorschauen."));return;
            }
            string physical=end.PhysicalId??end.Id;
            physicalBoundaries.TryGetValue(physical,out var source);
            if(TrackCircuit.TryPointId(id,out var point,out int arm))c.AddLabel(WindowText("Generated point boundary: ","Erzeugte Weichengrenze: ")+PointAssignments.Caption(desks,point)+" · "+
                (arm==0?WindowText("Common arm","Stammgleis"):arm==1?WindowText("Normal arm","gerader Ast"):WindowText("Reverse arm","Abzweig")));
            if(source!=null)c.AddLabel((source.IsSignal?WindowText("Detector belongs to signal: ","Melder gehört zu Signal: "):WindowText("Independent world object: ","Eigenständiges Weltobjekt: "))+source.DisplayTitle);
            c.AddLabel(WindowText("Track / metres from A: ","Gleis / Meter ab A: ")+end.Anchor.Segment+" · "+SignUnits.Number(end.Anchor.Meters)+" m");
            c.AddLabel(WindowText("Stable boundary ID: ","Stabile Grenzkennung: ")+id);
            var fields=desks.Desks.SelectMany(d=>d.Panel.Tiles.Where(t=>DiagramBoundary(t,id)).Select(t=>new{Desk=d,Tile=t})).ToArray();
            c.AddLabel(fields.Length==0?WindowText("This physical boundary is not drawn in any interlocking diagram.","Diese physische Grenze ist in keinem Stellwerksgleisbild eingezeichnet."):
                WindowText("Drawn at: ","Eingezeichnet bei: ")+string.Join("; ",fields.Take(6).Select(f=>f.Desk.Code+" · "+(f.Tile.X+1)+" / "+(f.Tile.Y+1))));
            if(source!=null) {
                string captured=source.Id;
                c.AddButton(WindowText("Open this world object's properties","Eigenschaften dieses Weltobjekts öffnen"),()=>Act(()=>{RequireFreshGfmInspection();Entry.OpenMarker(captured);}));
            }
        }
    }
}
