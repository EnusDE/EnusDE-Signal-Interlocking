using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class RetainedRouteDisplay {
        public static bool OnTile(PanelTile tile,DeskLibrary library,IEnumerable<RouteRun> runs) {
            foreach(var run in runs.Where(r=>r.Stage==RouteStage.Fault||r.Stage==RouteStage.Cancelled)) {
                if(tile.Kind==PanelTileKind.Switch&&run.Route.Switches.Any(p=>p.Node==tile.Reference&&!run.ReleasedSwitches.Contains(p.Node)))return true;
                if(tile.Kind==PanelTileKind.Signal&&new[]{tile.Reference,tile.AlternateSignal}.Any(id=>
                    !string.IsNullOrEmpty(id)&&(id==run.Route.Entry&&!run.EntryReleased||run.Route.ViaSignals?.Any(v=>v.Id==id&&!run.PassedSignals.Contains(id))==true)))return true;
                if(tile.Section!=null&&run.HeldSections.Any(s=>s.Overlaps(tile.Section)))return true;
                var ids=new[]{tile.BlockId,tile.SecondBlock}.Concat(tile.CircuitRails?.Values??Enumerable.Empty<string>());
                foreach(string id in ids.Where(id=>!string.IsNullOrEmpty(id)))
                    if(library?.Blocks.TryGetValue(id,out var block)==true&&block.Sections.Any(s=>run.HeldSections.Any(s.Overlaps)))return true;
            }
            return false;
        }
    }
}
