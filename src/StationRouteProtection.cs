using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class StationRouteProtection {
        public static string CircuitError(IEnumerable<RouteDefinition> routes,IEnumerable<TrackBlock> blocks,Func<TrackBlock,bool> usable,Func<TrackBlock,TrackOccupancy> read,Func<TrackBlock,string> caption,Func<TrackSection,TrackOccupancy> readInterval=null) {
            return CircuitError(routes,blocks,usable,read,caption,readInterval,out _);
        }
        public static string CircuitError(IEnumerable<RouteDefinition> routes,IEnumerable<TrackBlock> blocks,Func<TrackBlock,bool> usable,Func<TrackBlock,TrackOccupancy> read,Func<TrackBlock,string> caption,Func<TrackSection,TrackOccupancy> readInterval,out TrackOccupancy conflict) {
            conflict=TrackOccupancy.Clear;
            var path=routes.Where(r=>!r.Shunting&&r.Path!=null).SelectMany(r=>r.Path).Select(p=>p.Section).ToArray();
            foreach(var block in blocks.Where(b=>b.IsCircuit&&b.Path.Any(p=>path.Any(p.Section.Overlaps))&&usable(b))) {
                TrackOccupancy state;
                if(readInterval==null)state=read(block);
                else {
                    var intervals=block.Path.SelectMany(p=>path.Where(p.Section.Overlaps).Select(s=>new TrackSection{Segment=s.Segment,From=Math.Max(s.From,p.Section.From),To=Math.Min(s.To,p.Section.To)})).GroupBy(s=>s.Key).Select(g=>g.First());
                    state=TrackOccupancy.Clear;
                    foreach(var section in intervals){var value=readInterval(section);if(value==TrackOccupancy.Unknown){state=value;break;}if(value==TrackOccupancy.Occupied)state=value;}
                }
                if(state==TrackOccupancy.Clear)continue;
                conflict=state;
                return caption(block)+": "+(state==TrackOccupancy.Occupied?LocalText.Select("GFM section occupied on the requested route.","GFM-Abschnitt im angeforderten Fahrweg belegt."):LocalText.Select("Configured GFM section cannot currently be read.","Eingerichteter GFM-Abschnitt derzeit nicht lesbar."));
            }
            return null;
        }
    }
}
