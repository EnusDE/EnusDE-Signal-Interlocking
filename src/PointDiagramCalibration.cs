using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class PointDiagramCalibration {
        public static int Apply(PanelDocument panel,NetworkResult network,Func<PanelTile,string,bool> boundary,IReadOnlyDictionary<string,string> aliases,NetworkInput input,ICollection<string> unresolved,Func<string,string> pointCaption=null) {
            string Canonical(string id)=>aliases!=null&&id!=null&&aliases.TryGetValue(id,out var shared)?shared:id;
            var evidence=new Dictionary<PanelTile,Dictionary<int,HashSet<int>>>();
            foreach(var block in network.Blocks.Values.Where(b=>b.IsCircuit)) {
                List<PanelTile[]> paths;try{paths=CircuitDiagramAssignment.Paths(panel,block.BoundaryA,block.BoundaryB,boundary,Canonical,false);}catch(InvalidOperationException){continue;}
                if(paths.Count!=1)continue;var path=paths[0];
                for(int end=0;end<2;end++) {
                    var field=path[end==0?0:path.Length-1];if(field.Kind!=PanelTileKind.Switch)continue;
                    string id=end==0?block.BoundaryA:block.BoundaryB;int arm=-1;
                    if(TrackCircuit.TryPointId(id,out string point,out int raw)&&point==field.Reference)arm=raw;
                    else if(input!=null) {
                        var node=input.Nodes.FirstOrDefault(n=>n.Id==field.Reference&&n.Point);
                        string segment=(end==0?block.Path.First():block.Path.Last()).Section.Segment;
                        var edge=input.Edges.FirstOrDefault(e=>e.Id==segment&&(e.A==field.Reference||e.B==field.Reference));
                        if(node!=null&&edge!=null&&!string.IsNullOrEmpty(node.Common)&&!string.IsNullOrEmpty(node.Normal))arm=segment==node.Common?0:segment==node.Normal?1:2;
                    }
                    if(arm<0)continue;
                    if(Canonical(TrackCircuit.PointId(field.Reference,arm))!=Canonical(id))continue;
                    var adjacent=path[end==0?1:path.Length-2];int world=PanelConnections.Direction(adjacent.X-field.X,adjacent.Y-field.Y);
                    int localMask=PanelConnections.Transform(1<<world,field.Rotation,field.Mirror,true);
                    int port=Enumerable.Range(0,8).Single(p=>(1<<p)==localMask);
                    if(!evidence.TryGetValue(field,out var arms))evidence[field]=arms=new Dictionary<int,HashSet<int>>();
                    if(!arms.TryGetValue(arm,out var ports))arms[arm]=ports=new HashSet<int>();ports.Add(port);
                }
            }
            int changed=0;
            foreach(var pair in evidence) {
                var tile=pair.Key;var arms=pair.Value;
                if(arms.Any(p=>p.Value.Count!=1)||arms.Values.Select(p=>p.Single()).Distinct().Count()!=arms.Count) {
                    unresolved.Add(LocalText.Select("Conflicting point-arm mapping: ","Widersprüchliche Weichenast-Zuordnung: ")+(pointCaption?.Invoke(tile.Reference)??tile.Reference));continue;
                }
                if(arms.Count<2)continue;
                int local=PanelConnections.Transform(PanelConnections.Ports(tile),tile.Rotation,tile.Mirror,true);
                var map=arms.ToDictionary(p=>p.Key,p=>p.Value.Single());
                if(map.Count==2){int missing=Enumerable.Range(0,3).Single(a=>!map.ContainsKey(a));var remaining=Enumerable.Range(0,8).Where(p=>(local&(1<<p))!=0&&!map.Values.Contains(p)).ToArray();if(remaining.Length!=1)continue;map[missing]=remaining[0];}
                if(map.Count!=3)continue;
                if(tile.Ports!=local||tile.Stem!=(1<<map[0])||tile.Normal!=(1<<map[1]))changed++;
                tile.Ports=local;tile.Stem=1<<map[0];tile.Normal=1<<map[1];
            }
            return changed;
        }
    }
}
