using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class DeskRemoval {
        readonly string world;
        readonly HashSet<string> signals,points;
        readonly ILookup<string,TrackSection> sections;
        public readonly string Id,Name;
        public readonly int Fields,OwnedPoints,LinkedAreas;

        public DeskRemoval(DeskLibrary library,string id) {
            var desk=library.Desks.SingleOrDefault(d=>d.Id==id)??throw new InvalidOperationException("Stellwerk fehlt.");
            if(desk.Global)throw new InvalidOperationException("Das übergeordnete ESTW wird als Übersicht benötigt.");
            Id=id;Name=desk.Code+" · "+desk.Name;world=library.WorldKey;
            var tiles=desk.Panel.Tiles;
            Fields=tiles.Count;
            var owned=library.Points.Where(p=>p.Value.Desk==id).Select(p=>p.Key).ToArray();
            OwnedPoints=owned.Length;
            LinkedAreas=library.Desks.Where(d=>d.Id!=id).Sum(d=>d.Panel.Areas.Count(a=>a.Desk==id));
            signals=new HashSet<string>(tiles.Where(t=>t.Kind==PanelTileKind.Signal)
                .SelectMany(t=>new[]{t.Reference,t.AlternateSignal}).Concat(tiles.Where(t=>t.Target!=null).Select(t=>t.Target.Id))
                .Where(id=>!string.IsNullOrEmpty(id)));
            points=new HashSet<string>(tiles.Where(t=>t.Kind==PanelTileKind.Switch&&t.Reference!="").Select(t=>t.Reference).Concat(owned));
            sections=tiles.Where(t=>t.Section!=null).Select(t=>t.Section)
                .Concat(tiles.SelectMany(t=>new[]{t.BlockId,t.SecondBlock}).Where(id=>id!="").Distinct()
                    .SelectMany(block=>library.Blocks[block].Sections)).ToLookup(s=>s.Segment);
        }

        public bool RelatesTo(RouteDefinition route) {
            return signals.Contains(route.Entry)||signals.Contains(route.Exit)
                ||route.ViaSignals?.Any(s=>signals.Contains(s.Id))==true
                ||route.Flanks?.Any(f=>f.Signals.Any(s=>signals.Contains(s.Id)))==true
                ||route.Switches.Any(p=>points.Contains(p.Node))
                ||route.Sections.Concat(route.Approach).Any(s=>sections[s.Segment].Any(s.Overlaps));
        }

        public DeskLibrary Apply(DeskLibrary library,IEnumerable<RouteDefinition> active) {
            if(library.WorldKey!=world)throw new InvalidOperationException("Das Stellwerk gehört zu einer anderen Welt.");
            var current=new DeskRemoval(library,Id);
            if(active.Any(current.RelatesTo))throw new InvalidOperationException("Zuerst betroffene Fahrstraßen und Restverschlüsse auflösen.");
            var next=library.Clone();
            var global=next.Desks.Single(d=>d.Global);
            next.Desks.RemoveAll(d=>d.Id==Id);
            foreach(var point in next.Points.Values.Where(p=>p.Desk==Id))point.Desk=global.Id;
            foreach(var desk in next.Desks)desk.Panel.Areas.RemoveAll(a=>a.Desk==Id);
            next.Validate(world);
            return next;
        }
    }
}
