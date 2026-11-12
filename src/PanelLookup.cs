using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    // Rebuilt on editor commits, not once for each rendered field of a large map.
    public sealed class PanelLookup {
        List<PanelTile> source;
        Dictionary<int,PanelTile> index=new Dictionary<int,PanelTile>();
        public PanelTile Read(PanelDocument doc,int x,int y){if(doc==null)return null;if(!ReferenceEquals(source,doc.Tiles)||index.Count!=doc.Tiles.Count){source=doc.Tiles;index=source.ToDictionary(t=>t.Y*512+t.X);}PanelTile tile;return index.TryGetValue(y*512+x,out tile)?tile:null;}
    }
}
