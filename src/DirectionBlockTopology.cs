using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public static class DirectionBlockTopology {
        // Names, unrelated objects, construction records and presentation options
        // must not turn an unchanged held axis into a topology change.
        public static string Signature(NetworkInput network,IEnumerable<TrackBlock> blocks,IEnumerable<MarkerAnchor> stations)=>SharedDocuments.Hash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new {
            nodes=network.Nodes.OrderBy(n=>n.Id).Select(n=>new{n.Id,n.Point,n.Common,n.Normal}),
            edges=network.Edges.OrderBy(e=>e.Id).Select(e=>new{e.Id,e.A,e.B,e.Length}),
            detectors=network.Cuts.Where(c=>c.Boundary!=null).OrderBy(c=>c.Segment).ThenBy(c=>c.Meters).ThenBy(c=>c.Boundary).Select(c=>new{c.Segment,c.Meters,c.Boundary,c.BoundaryKind,c.BoundaryReverse}),
            circuits=blocks.Where(b=>b.IsCircuit).OrderBy(b=>b.Id).Select(b=>new{b.Id,b.BoundaryA,b.BoundaryB,b.Path}),
            stations=stations.OrderBy(a=>a.Segment).ThenBy(a=>a.Meters).Select(a=>new{a.Segment,a.Meters})
        })));
    }
}
