using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class DirectionCatalogChanges {
        static string Shape(DirectionBlock b)=>b.Id+"|"+b.Error+"|"+string.Join(";",b.Axis.Select(p=>p.Section.Key+":"+p.Reverse));
        public static bool CanReplace(IEnumerable<DirectionBlock> before,IEnumerable<DirectionBlock> after,IEnumerable<DirectionBlockLease> held) {
            var leases=held.ToArray();bool Touches(DirectionBlock block)=>leases.Any(l=>l.Axis.Any(a=>block.Axis.Any(b=>a.Section.Overlaps(b.Section))));
            return before.Where(Touches).Select(Shape).OrderBy(s=>s,StringComparer.Ordinal).SequenceEqual(after.Where(Touches).Select(Shape).OrderBy(s=>s,StringComparer.Ordinal));
        }
    }
}
