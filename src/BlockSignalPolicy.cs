using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class BlockSignalPolicy {
        public static BlockSignalUnit Unit(IEnumerable<LineTrackDefinition> lines,string signal)=>lines.SelectMany(l=>l.BlockUnits).FirstOrDefault(u=>u.Forward==signal||u.Reverse==signal);
        public static double Passage(RouteDefinition route,MarkerRecord boundary,double normal) {
            if(!CircuitIdentity.Enabled(boundary)||route.Path==null||!RouteSignals.Position(route,boundary.Anchor.Segment,boundary.Anchor.Meters,out double at,false)||at<0||at>=route.Path.Sum(p=>p.Section.To-p.Section.From)-.001)
                throw new InvalidOperationException(LocalText.Select("The shared block detector is missing or not beyond this signal on the route.","Der gemeinsame Blockmelder fehlt oder liegt nicht hinter diesem Signal im Fahrweg."));
            return Math.Max(normal,at+.1);
        }
    }
    public sealed partial class SignalRuntime {
        internal double BlockPassage(RouteDefinition route,double normal) {
            var unit=BlockSignalPolicy.Unit(desks.FreeLines,route.Entry);return unit==null?normal:BlockSignalPolicy.Passage(route,Object(unit.Boundary),normal);
        }
        internal bool BlockSignalPathValid(RouteDefinition route) {
            try {BlockPassage(route,0);return true;}catch{return false;}
        }
        string BlockBoundary(string id)=>desks==null?null:BlockSignalPolicy.Unit(desks.FreeLines,id)?.Boundary;
    }
    public sealed partial class SignalWorldBridge:ISignalPassageWorld {
        public double EntryPassageOffset(RouteDefinition route,double normal)=>owner.BlockPassage(route,normal);
    }
}
