using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class AuxiliarySetup {
        public static void Link(AuxiliaryTarget target,MarkerRecord entry) {
            if(entry?.Kind!=MarkerRecord.Main||entry.Planned||entry.Anchor==null)throw new InvalidOperationException(LocalText.Select("Select an available entry-signal face.","Eine verfügbare Einfahrsignalrichtung auswählen."));
            target.ReferenceSignal=entry.Id;target.Anchor=entry.Anchor.Clone();target.Reverse=!entry.FacingReverse;
        }
        // Stop at the first main signal on each schematic branch. Never infer a distant
        // station merely from a similar name or a geographically close world object.
        public static string[] Entries(PanelDocument panel,PanelTile origin,Func<string,MarkerRecord> signal,Func<string,SignalDuty> duty) {
            var seen=new HashSet<long>();var pending=new Queue<PanelTile>();var found=new HashSet<string>();pending.Enqueue(origin);
            var tiles=panel.Tiles.ToDictionary(t=>((long)t.Y<<32)|(uint)t.X);
            while(pending.Count>0) {
                var tile=pending.Dequeue();if(!seen.Add(((long)tile.Y<<32)|(uint)tile.X))continue;
                if(tile!=origin&&tile.Kind==PanelTileKind.Signal) {
                    foreach(var id in new[]{tile.Reference,tile.AlternateSignal}.Where(id=>id!=null))
                        if(signal(id)?.Kind==MarkerRecord.Main&&duty(id)==SignalDuty.Entry&&!tile.ReadOnlySignal)found.Add(id);
                    if(new[]{tile.Reference,tile.AlternateSignal}.Any(id=>id!=null&&signal(id)?.Kind==MarkerRecord.Main))continue;
                }
                int ports=PanelConnections.Ports(tile);
                for(int p=0;p<8;p++)if((ports&(1<<p))!=0) {
                    int x=tile.X+PanelConnections.Dx[p],y=tile.Y+PanelConnections.Dy[p];
                    if(tiles.TryGetValue(((long)y<<32)|(uint)x,out var next)&&(PanelConnections.Ports(next)&(1<<((p+4)%8)))!=0)pending.Enqueue(next);
                }
            }
            return found.OrderBy(id=>id,StringComparer.Ordinal).ToArray();
        }
    }
}
