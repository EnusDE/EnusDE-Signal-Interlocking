using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class ContactSignalPolicy {
        public static string[] NextMains(NetworkInput graph,IEnumerable<MarkerRecord> signals,AutomationContact contact) {
            var mains=signals.Where(s=>s.Kind==MarkerRecord.Main&&!s.Planned).ToArray();
            var marker=new MarkerRecord{Id="contact-origin",Kind=MarkerRecord.Main,Anchor=contact.Anchor.Clone(),FacingReverse=contact.Reverse,AllowHp1=true,AllowHp2=true};
            var report=new RouteSearchReport();var found=new RouteDiscovery(graph,mains).Find(marker,RouteKind.Train,new HashSet<string>(mains.Select(s=>s.Id)),report:report,includeAlternatives:true);
            return report.LimitReached?new string[0]:found.Select(p=>p.Exit).Distinct().ToArray();
        }
        public static bool IsNextMain(NetworkInput graph,IEnumerable<MarkerRecord> signals,AutomationContact contact,string target) {
            var marker=new MarkerRecord{Id="contact-origin",Name="contact",Kind=MarkerRecord.Main,Anchor=contact.Anchor.Clone(),FacingReverse=contact.Reverse,AllowHp1=true,AllowHp2=true};
            var search=new RouteDiscovery(graph,signals.Where(s=>s.Kind==MarkerRecord.Main&&!s.Planned));var report=new RouteSearchReport();
            return search.Find(marker,RouteKind.Train,new HashSet<string>{target},report:report,includeAlternatives:true).Count>0&&!report.LimitReached;
        }
    }
}
