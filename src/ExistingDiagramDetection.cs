using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class GfmDetectionIssue {public string Desk,Message;public TrackBlock Block;}
    public sealed class ExistingDetectionResult {
        public DeskLibrary Library;
        public int Circuits,Points,Fields,Shared,Calibrated,Removed,ResetFields;
        public bool Rebuilt,ResetOnly;
        public readonly List<string> Unresolved=new List<string>();
        public readonly List<GfmDetectionIssue> Issues=new List<GfmDetectionIssue>();
    }
    public static class ExistingDiagramDetection {
        public static ExistingDetectionResult Build(DeskLibrary library,string deskId,NetworkResult network,Func<PanelTile,string,bool> boundary,IReadOnlyDictionary<string,string> aliases=null,NetworkInput input=null) {
            var result=new ExistingDetectionResult{Library=library.Clone()};var panel=result.Library.Desks.Single(d=>d.Id==deskId).Panel;
            void Issue(TrackBlock block,string message){result.Unresolved.Add(message);result.Issues.Add(new GfmDetectionIssue{Desk=deskId,Message=message,Block=block.Clone()});}
            string Canonical(string id)=>aliases!=null&&id!=null&&aliases.TryGetValue(id,out var value)?value:id;
            result.Calibrated=PointDiagramCalibration.Apply(panel,network,boundary,aliases,input,result.Unresolved,id=>PointAssignments.Caption(library,id));
            foreach(var block in network.Blocks.Values.OrderBy(b=>b.Id,StringComparer.Ordinal)) {
                if(block.Point!=null) {
                    var fields=panel.Tiles.Where(t=>t.Kind==PanelTileKind.Switch&&t.Reference==block.Point&&PointCircuitVisibility.Enabled(library,block.Point)).ToArray();
                    if(fields.Length==0)continue;
                    result.Library.Blocks[block.Id]=block.Clone();foreach(var field in fields){field.Section=null;field.BlockId=block.Id;}result.Points++;continue;
                }
                if(!block.IsCircuit)continue;
                var starts=panel.Tiles.Where(t=>boundary(t,block.BoundaryA)).ToArray();
                bool hasEnd=panel.Tiles.Any(t=>boundary(t,block.BoundaryB));
                if(starts.Length==0||!hasEnd){if(starts.Length>0||hasEnd)Issue(block,block.Name+LocalText.Select(" · opposite boundary is not assigned in this diagram"," · Gegengrenze ist in dieser Tafel nicht zugeordnet"));continue;}
                var seeds=starts.Where(t=>t.Kind==PanelTileKind.Signal||t.Kind==PanelTileKind.Switch||CircuitDiagramAssignment.RailField(t)).Concat(
                    starts.SelectMany(t=>panel.Tiles.Where(s=>Math.Abs(t.X-s.X)<=1&&Math.Abs(t.Y-s.Y)<=1&&CircuitDiagramAssignment.RailField(s)))).Distinct().ToArray();
                var paths=new Dictionary<string,PanelTile[]>();var seedByPath=new Dictionary<string,PanelTile>();
                foreach(var seed in seeds)try {
                    var path=CircuitDiagramAssignment.Find(panel,seed.X,seed.Y,block.BoundaryA,block.BoundaryB,boundary,Canonical);
                    string key=string.Join("/",path.Select(t=>t.X+","+t.Y));paths[key]=path;seedByPath[key]=seed;
                }catch(InvalidOperationException){}
                if(paths.Count!=1){Issue(block,block.Name+LocalText.Select(" · no unique continuous diagram connection"," · keine eindeutige durchgehende Gleisbildverbindung"));continue;}
                var selected=seedByPath.Single().Value;
                result.Fields+=CircuitDiagramAssignment.Assign(panel,selected.X,selected.Y,block,boundary,Canonical);
                result.Library.Blocks[block.Id]=block.Clone();result.Circuits++;
            }
            if(aliases!=null)foreach(var alias in aliases) {
                if(!TrackCircuit.TryPointId(alias.Key,out var point,out _)||!PointCircuitVisibility.Enabled(library,point))continue;
                var guard=network.Blocks.Values.FirstOrDefault(b=>b.Point==point);
                if(guard==null||!panel.Tiles.Any(t=>t.Kind==PanelTileKind.Switch&&t.Reference==point))continue;
                bool SharedEnd(PanelTile tile,string id)=>id==alias.Key?tile.Kind==PanelTileKind.Switch&&tile.Reference==point:tile.Kind==PanelTileKind.Signal&&boundary(tile,alias.Value);
                foreach(var signal in panel.Tiles.Where(t=>t.Kind==PanelTileKind.Signal&&boundary(t,alias.Value)).ToArray())try {
                    var link=new TrackBlock{Id=guard.Id,BoundaryA=alias.Value,BoundaryB=alias.Key};
                    result.Fields+=CircuitDiagramAssignment.Assign(panel,signal.X,signal.Y,link,SharedEnd);result.Library.Blocks[guard.Id]=guard.Clone();result.Shared++;
                }catch(InvalidOperationException){Issue(guard,guard.Name+LocalText.Select(" · shared A/B diagram connection unresolved"," · gemeinsamer A/B-Gleisbildanschluss ungeklärt"));}
            }
            result.Library.Validate(library.WorldKey);return result;
        }
    }
}
