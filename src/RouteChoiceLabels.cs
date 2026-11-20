using System;
using System.Linq;
using System.Collections.Generic;
namespace EnusDE.Signaltechnik {
    public static class RouteChoiceLabels {
        public static double Length(RouteChainPlan plan) {double length=0;foreach(var route in plan.Routes){if(route.Path!=null){foreach(var piece in route.Path)length+=piece.Section.To-piece.Section.From;}else foreach(var section in route.Sections)length+=section.To-section.From;}return length;}
        public static string[] Captions(IReadOnlyList<RouteChainPlan> plans,Func<string,string> signal,Func<string,string> point) {
            var signals=plans.Select(p=>p.Routes.SelectMany((r,i)=>(i==0?Enumerable.Empty<string>():new[]{r.Entry}).Concat((r.ViaSignals??new List<RouteViaSignal>()).OrderBy(v=>v.Distance).Select(v=>v.Id))).Distinct().Select(signal).ToArray()).ToArray();
            var tokens=plans.Select((p,i)=>signals[i].Concat(p.Routes.SelectMany(r=>r.Switches).Where(s=>s.FlankOf==null).Select(s=>point(s.Node)+(s.Thrown?" −":" +"))).Distinct().ToArray()).ToArray();
            var result=new string[plans.Count];
            for(int i=0;i<plans.Count;i++) {
                if(plans.Count==2&&Math.Abs(Length(plans[0])-Length(plans[1]))>.5) {
                    result[i]=Length(plans[i])<Length(plans[1-i])?LocalText.Select("Short route","Kurzer Weg"):LocalText.Select("Long route","Langer Weg");continue;
                }
                var unique=tokens[i].FirstOrDefault(t=>!tokens.Where((_,j)=>j!=i).Any(other=>other.Contains(t)));
                string[] via=unique==null?tokens[i]:new[]{unique};
                result[i]=LocalText.Select("Route ","Weg ")+(i+1)+(via.Length==0?"":LocalText.Select(" via "," über ")+string.Join(" → ",via));
            }
            return result;
        }
    }
}
