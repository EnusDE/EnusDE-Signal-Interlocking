using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public static class NetworkRecognition {
        public static NetworkResult PreserveCircuitIds(DeskLibrary before,NetworkResult result,Func<string,string> canonical=null) {
            var copy=new NetworkResult{Panel=result.Panel.Clone(),Segments=result.Segments,ScopeSegments=result.ScopeSegments,Chains=result.Chains,Drawn=result.Drawn,Missing=result.Missing.ToList()};
            var remap=new System.Collections.Generic.Dictionary<string,string>();
            foreach(var block in result.Blocks.Values) {
                string Canonical(string id)=>canonical==null?id:canonical(id);
                var existing=block.IsCircuit?before.Blocks.Values.Where(b=>b.IsCircuit&&TrackCircuit.Pair(Canonical(b.BoundaryA),Canonical(b.BoundaryB))==TrackCircuit.Pair(Canonical(block.BoundaryA),Canonical(block.BoundaryB))&&
                    b.Path.Select(p=>p.Section.Key).OrderBy(k=>k).SequenceEqual(block.Path.Select(p=>p.Section.Key).OrderBy(k=>k))).OrderBy(b=>b.Id.StartsWith("c:auto-",StringComparison.Ordinal)).ThenBy(b=>b.Id,StringComparer.Ordinal).FirstOrDefault():null;
                var value=(existing??block).Clone();copy.Blocks[value.Id]=value;remap[block.Id]=value.Id;
            }
            foreach(var tile in copy.Panel.Tiles){if(remap.TryGetValue(tile.BlockId,out var main))tile.BlockId=main;if(remap.TryGetValue(tile.SecondBlock,out var other))tile.SecondBlock=other;}
            return copy;
        }
        public static string Signature(NetworkInput input)=>SharedDocuments.Hash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new {
            input.World,Nodes=input.Nodes.OrderBy(n=>n.Id),Edges=input.Edges.OrderBy(e=>e.Id),Cuts=input.Cuts.OrderBy(c=>c.Segment).ThenBy(c=>c.Meters).ThenBy(c=>c.Signal)
        })));
        public static DeskLibrary Apply(DeskLibrary before,string targetId,NetworkResult result,bool replace) {
            if(result?.Panel?.WorldKey!=before.WorldKey)throw new InvalidOperationException(LocalText.Select("Recognition belongs to a different world.","Erkennung gehört zu einer anderen Welt."));
            if(replace&&(result.Drawn!=result.Chains||result.Missing.Count>0))throw new InvalidOperationException(LocalText.Select("Incomplete diagram proposal. The existing diagram is retained.","Unvollständiger Gleisbildvorschlag. Die bestehende Tafel bleibt erhalten."));
            result=PreserveCircuitIds(before,result);
            var next=before.Clone();var target=next.Desks.Single(d=>d.Id==targetId&&d.IsMaster);
            foreach(var pair in result.Blocks)next.Blocks[pair.Key]=pair.Value.Clone();
            PointAssignments.NameDiscovered(next,result.Blocks.Values.Where(b=>b.Point!=null).Select(b=>b.Point));
            if(replace) {
                var modules=next.Modules.Where(m=>MasterDiagram.Owner(next,m)==target.Id).ToArray();
                if(target.Panel.Tiles.Count>0||modules.Length>0) {
                    if(next.Desks.Count>=32)throw new InvalidOperationException(LocalText.Select("No free interlocking slot for the previous diagram archive.","Kein freier Stellwerksplatz für das Archiv der bisherigen Zeichnung."));
                    var archive=new DeskDefinition{MasterView=true,ShowInDispatcher=false,Code=RouteNaming.DeskCode(target.Code+" OLD",next.Desks.Select(d=>d.Code)),
                        Name=LocalText.Select("Previous diagram: ","Vorherige Zeichnung: ")+target.Name,Panel=target.Panel.Clone()};
                    if(archive.Name.Length>64)archive.Name=archive.Name.Substring(0,64);
                    next.Desks.Add(archive);foreach(var module in modules)module.Master=archive.Id;
                }
                target.Panel=result.Panel.Clone();
            }
            foreach(var desk in next.Desks)Rebind(desk.Panel,result);
            if(replace)next=ExistingDiagramDetection.Build(next,targetId,result,(tile,id)=>tile.CircuitBoundary==id||tile.Kind==PanelTileKind.Signal&&!tile.ProjectedMastFace&&tile.Reference==id).Library;
            next.Validate(before.WorldKey);
            if(Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(next))>16*1024*1024)throw new InvalidOperationException(LocalText.Select("The diagram and its archives exceed the shared 16 MiB limit. Use a smaller drawing scope or a new empty master view.","Gleisbild und Archive überschreiten die gemeinsame Grenze von 16 MiB. Kleineren Zeichenbereich oder eine neue leere ESTW-Ansicht verwenden."));
            return next;
        }
        static void Rebind(PanelDocument doc,NetworkResult result) {
            var bySegment=result.Blocks.Values.Where(b=>b.Point==null).SelectMany(b=>b.Path.Select(p=>new{b.Id,p.Section})).GroupBy(p=>p.Section.Segment).ToDictionary(g=>g.Key,g=>g.ToArray());
            foreach(var tile in doc.Tiles.Where(t=>!string.IsNullOrEmpty(t.AutoSegment)&&t.Kind!=PanelTileKind.Switch&&t.SecondBlock=="")) {
                if(!bySegment.TryGetValue(tile.AutoSegment,out var pieces)){tile.BlockId="";continue;}
                var matches=pieces.Where(p=>p.Section.From<=tile.AutoMeters&&p.Section.To>=tile.AutoMeters).ToArray();tile.BlockId=matches.Length==1?matches[0].Id:"";
            }
        }
    }
}
