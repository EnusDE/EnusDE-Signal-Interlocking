using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    // Bounded local measurement only. It never sets points and never proves a block clear.
    public static class ContactDistance {
        sealed class Step {public PlanEdge Edge;public string Node;public double Distance;public int Sign;public HashSet<string> Used;}
        public static double? Find(MarkerAnchor from,bool reverse,MarkerAnchor to,Func<string,PlanEdge> edge,Func<string,PlanNode> node,
            Func<string,IEnumerable<PlanEdge>> connected,double limit=300) {
            var first=edge(from.Segment);var target=edge(to.Segment);
            if(first==null||target==null||from.Meters<0||from.Meters>first.Length||to.Meters<0||to.Meters>target.Length)return null;
            if(first.Id==target.Id)return Math.Abs(to.Meters-from.Meters)<=limit?(double?)((to.Meters-from.Meters)*(reverse?-1:1)):null;
            int sign=reverse?-1:1;var used=new HashSet<string>{first.Id};
            var pending=new List<Step>{new Step{Edge=first,Node=first.B,Distance=first.Length-from.Meters,Sign=sign,Used=used},
                new Step{Edge=first,Node=first.A,Distance=from.Meters,Sign= -sign,Used=used}};
            double best=double.PositiveInfinity,value=0;int matches=0,work=0;
            while(pending.Count>0) {
                pending.Sort((a,b)=>a.Distance.CompareTo(b.Distance));var step=pending[0];pending.RemoveAt(0);
                if(step.Distance>limit||step.Distance>best+.01)break;
                if(++work>512||step.Used.Count>64)return null;
                var junction=node(step.Node);var joins=connected(step.Node).Where(e=>e!=null).ToArray();
                if(junction==null||(junction.Point?joins.Length!=3||junction.Common==null:joins.Length!=2))continue;
                foreach(var next in joins) {
                    if(step.Used.Contains(next.Id)||junction.Point&&step.Edge.Id!=junction.Common&&next.Id!=junction.Common)continue;
                    bool a=next.A==step.Node;double along=next.Id==target.Id?(a?to.Meters:next.Length-to.Meters):next.Length;
                    double distance=step.Distance+along;if(distance>limit||distance>best+.01)continue;
                    if(next.Id==target.Id) {
                        if(distance<best-.01){best=distance;matches=0;}
                        if(Math.Abs(distance-best)<=.01){matches++;value=step.Sign*distance;}continue;
                    }
                    pending.Add(new Step{Edge=next,Node=a?next.B:next.A,Distance=distance,Sign=step.Sign,Used=new HashSet<string>(step.Used){next.Id}});
                }
            }
            return matches==1?(double?)value:null;
        }
    }
}
