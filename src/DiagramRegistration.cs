using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class DiagramRegistrationResult {
        public PanelDocument Panel;public int Assigned,Refreshed,Unresolved;
    }
    public static class DiagramRegistration {
        static readonly HashSet<string> Placeholders=new HashSet<string>(new[]{"Signal","Hauptsignal","Main signal","Rangiersignal","Shunting signal","Vorsignal","Distant signal"},StringComparer.OrdinalIgnoreCase);
        public static DiagramRegistrationResult Inspect(PanelDocument before,IEnumerable<MarkerRecord> candidates,bool assignNames) {
            var signals=candidates.Where(s=>s.IsSignal&&!s.Planned).ToDictionary(s=>s.Id);
            var result=new DiagramRegistrationResult{Panel=before.Clone()};
            foreach(var tile in result.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.Signal)) {
                var original=tile.Clone();
                if(!signals.TryGetValue(tile.Reference,out var signal)&&assignNames&&string.IsNullOrEmpty(tile.Reference)) {
                    string label=(tile.Label??"").Trim();
                    var matches=label.Length==0||Placeholders.Contains(label)?Array.Empty<MarkerRecord>():signals.Values.Where(s=>string.Equals(s.Name,label,StringComparison.OrdinalIgnoreCase)||
                        !string.IsNullOrWhiteSpace(s.DisplayName)&&string.Equals(s.DisplayName,label,StringComparison.OrdinalIgnoreCase)).ToArray();
                    if(matches.Length==1){signal=matches[0];tile.Reference=signal.Id;result.Assigned++;}
                }
                if(signal==null){result.Unresolved++;continue;}
                // Keep explicit second signals and auxiliary targets. Only the automatic self-pair follows the head's capabilities.
                if(tile.Target==null&&(tile.AlternateSignal==null||tile.AlternateSignal==tile.Reference))
                    tile.AlternateSignal=signal.Kind==MarkerRecord.Main&&signal.AllowSh1?signal.Id:null;
                if(Placeholders.Contains(tile.Label)||tile.Label==signal.PreviousCode)tile.Label="";
                string boundary=signal.PhysicalId??signal.Id;
                if(tile.CircuitBoundary==null&&!tile.ProjectedMastFace&&CircuitIdentity.Enabled(signal))tile.CircuitBoundary=boundary;
                else if(tile.CircuitBoundary==boundary&&!CircuitIdentity.Enabled(signal))tile.CircuitBoundary=null;
                if(!tile.SameContent(original))result.Refreshed++;
            }
            return result;
        }
    }
}
