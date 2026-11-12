using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class ShuntIncorporation {
        static bool Covers(IEnumerable<TrackSection> sections,TrackSection target){double at=target.From;foreach(var s in sections.Where(s=>s.Segment==target.Segment&&s.To>target.From&&s.From<target.To).OrderBy(s=>s.From)){if(s.From>at+.001)return false;at=Math.Max(at,s.To);}return at>=target.To-.001;}
        public static bool Compatible(RouteDefinition main,RouteRun shunt) {
            var old=shunt.Route;if(main.Shunting||!old.Shunting||main.Path==null||old.Path==null||shunt.ReleaseSelection!=null||shunt.ChainId!=null||shunt.Stage!=RouteStage.Set&&shunt.Stage!=RouteStage.Running)return false;
            var controlled=new HashSet<string>(new[]{main.Entry}.Concat(main.ViaSignals?.Select(s=>s.Id)??Enumerable.Empty<string>()));if(!controlled.Contains(old.Entry)||old.ViaSignals?.Any(s=>!controlled.Contains(s.Id))==true)return false;
            var protectedSignals=new HashSet<string>((main.Flanks??new List<RouteFlank>()).SelectMany(f=>f.Signals).Select(s=>s.Id));if((old.Flanks??new List<RouteFlank>()).Where(f=>!shunt.ReleasedSwitches.Contains(f.Point)).SelectMany(f=>f.Signals).Any(s=>!protectedSignals.Contains(s.Id)&&!controlled.Contains(s.Id)))return false;
            int last=-1;foreach(var piece in old.Path){var indexes=main.Path.Select((p,i)=>new{p,i}).Where(x=>x.p.Section.Segment==piece.Section.Segment&&x.p.Reverse==piece.Reverse&&x.p.Section.Overlaps(piece.Section)).ToArray();if(indexes.Length==0||indexes[0].i<last||!Covers(indexes.Select(x=>x.p.Section),piece.Section))return false;last=indexes[0].i;}
            return shunt.HeldSections.All(s=>Covers(main.Sections,s))&&old.Switches.Where(p=>!shunt.ReleasedSwitches.Contains(p.Node)).All(p=>main.Switches.Any(q=>q.Node==p.Node&&q.Thrown==p.Thrown));
        }
    }
    public sealed partial class InterlockingEngine {
        bool Incorporate(RouteDefinition main,RouteRun[] old,IInterlockingWorld world) {
            if(old.Length==0||!old.All(r=>ShuntIncorporation.Compatible(main,r))||!world.PathValid(main)||old.Any(r=>!world.PathValid(r.Route))||!RequestClear(main,world))return false;
            var next=new RouteRun{Route=main,Stage=RouteStage.Preparing,StartedAt=clock(),WaitUntil=clock(),Reason="Gleichgerichtete Rangierfahrstraße(n) in Zugfahrstraße einbezogen; Endlagen-/Freiprüfung läuft",IncorporatedFrom=old.Select(r=>r.Route.Id).ToArray()};
            // One authority replaces all covered resources before any later switch command can run.
            foreach(var run in old)active.Remove(run.Route.Id);active.Add(main.Id,next);RefreshChains();return true;
        }
    }
}
