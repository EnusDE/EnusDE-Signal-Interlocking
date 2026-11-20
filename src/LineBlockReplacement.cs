using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    // Operates exclusively on detached documents. The caller commits the finished proposal atomically.
    public static class LineBlockReplacement {
        static InvalidOperationException Conflict(string detail)=>new InvalidOperationException(LocalText.Select("Cannot replace this block layout yet. Remove or reassign the following external reference first: ","Blockaufteilung noch nicht ersetzbar. Zuerst folgende externe Verknüpfung entfernen oder neu zuordnen: ")+detail);
        public static LineBlockBuild Build(MarkerDocument objects,DeskLibrary library,LineTrackDefinition line,BlockLayoutPlan plan,string owner,bool contacts,MarkerOptions placement,double stagger,IEnumerable<string> displays,IEnumerable<RouteDefinition> routes) {
            var old=library.FreeLines.SingleOrDefault(l=>l.Id==line.Id);
            if(old==null||old.BlockSections.Count==0)return LineBlockBuilder.Build(objects,library,line,plan,owner,contacts,placement,stagger,displays);
            var heads=old.BlockUnits.SelectMany(u=>new[]{u.Forward,u.Reverse}).Where(id=>id!="").ToHashSet();
            var physical=heads.Concat(old.BlockUnits.Select(u=>u.Boundary)).ToHashSet();
            var sections=old.BlockSections.ToHashSet();
            foreach(var route in routes??Enumerable.Empty<RouteDefinition>())if(heads.Contains(route.Entry)||heads.Contains(route.Exit)||route.ViaSignals?.Any(s=>heads.Contains(s.Id))==true||route.Flanks?.Any(f=>f.Signals.Any(s=>heads.Contains(s.Id)))==true)throw Conflict(route.Code);
            foreach(var marker in SignalCatalogue.Expand(objects.Markers,true))if(!heads.Contains(marker.Id)&&heads.Contains(marker.LinkedSignal))throw Conflict(marker.Name);
            foreach(var other in library.FreeLines.Where(l=>l.Id!=line.Id))if(other.Anchors.Keys.Any(heads.Contains)||other.BlockSections.Any(sections.Contains)||other.BlockUnits.Any(u=>physical.Contains(u.Boundary)||heads.Contains(u.Forward)||heads.Contains(u.Reverse)))throw Conflict(other.Code);
            foreach(var rule in library.Automation.Signals.Where(p=>!heads.Contains(p.Key)))if(heads.Contains(rule.Value.Primary)||heads.Contains(rule.Value.Alternate))throw Conflict(rule.Key);
            foreach(var contact in library.Automation.Contacts)if(heads.Contains(contact.Signal)&&!contact.AutomaticSignal)throw Conflict(contact.Name);
            foreach(var block in library.Blocks.Values.Where(b=>!sections.Contains(b.Id)))if(physical.Contains(block.BoundaryA)||physical.Contains(block.BoundaryB))throw Conflict(block.Name);
            foreach(var point in library.Points)if(point.Value.BoundaryLinks.Values.Any(physical.Contains))throw Conflict(point.Value.Code);
            foreach(var target in library.Desks.SelectMany(d=>d.Panel.Tiles).Select(t=>t.Target).Where(t=>t!=null))if(physical.Contains(target.ReferenceSignal))throw Conflict(target.Caption);
            foreach(var desk in library.Desks)if(desk.ReceivingTracks.Any(t=>t.Blocks.Any(sections.Contains)))throw Conflict(desk.Code+LocalText.Select(" · receiving track detection"," · Empfangsgleis-Freimeldung"));
            var next=library.Clone();var markers=JsonConvert.DeserializeObject<MarkerDocument>(JsonConvert.SerializeObject(objects));var draft=line.Clone();
            var numberIds=new HashSet<string>(library.Desks.SelectMany(d=>d.Panel.Tiles).Where(t=>t.NumberId!=null&&sections.Contains(t.BlockId)).Select(t=>t.NumberId));
            var ordered=LineNumberOverview.AutomaticSources(library,old).Select(s=>s.Tile.NumberId).ToArray();
            var overviews=new List<(string Desk,PanelArea Area,bool Reverse)>();
            var groups=new Dictionary<string,PanelArea>();var permissions=new Dictionary<string,PanelTile>();
            foreach(var desk in next.Desks) {
                var doc=desk.Panel;var group=BlockDiagram.Area(doc,line.Id);
                foreach(var overview in doc.Areas.Where(a=>a.NumberLine==line.Id).ToArray()) {
                    var fields=doc.Tiles.Where(t=>overview.Contains(t.X,t.Y)).OrderBy(t=>t.X).ToArray();
                    var ids=fields.Select(t=>t.NumberMirror).ToArray();
                    bool reverse=ids.SequenceEqual(ordered.Reverse());
                    if(ordered.Length!=old.BlockSections.Count||fields.Length!=ordered.Length||fields.Any(t=>!NumberMirrors.IsMirror(t))||!ids.SequenceEqual(ordered)&&!reverse)throw Conflict(desk.Code+" · "+overview.Code);
                    overviews.Add((desk.Id,overview.Clone(),reverse));doc.Tiles.RemoveAll(t=>overview.Contains(t.X,t.Y));doc.Areas.Remove(overview);
                }
                if(group!=null) {
                    var fields=doc.Tiles.Where(t=>group.Contains(t.X,t.Y)).ToArray();
                    if(fields.Any(t=>t.Target!=null||t.AlternateSignal!=null||
                        t.Kind==PanelTileKind.Signal&&!heads.Contains(t.Reference)||
                        t.Kind==PanelTileKind.TrainNumber&&!sections.Contains(t.BlockId)&&!numberIds.Contains(t.NumberMirror??"")||
                        t.Kind==PanelTileKind.LinePermission&&t.Reference!=line.Id||
                        t.Kind==PanelTileKind.Label&&!BlockDiagramViews.LabelAt(group,t)||
                        t.Kind!=PanelTileKind.Signal&&t.Kind!=PanelTileKind.TrainNumber&&t.Kind!=PanelTileKind.LinePermission&&t.Kind!=PanelTileKind.Label&&t.Kind!=PanelTileKind.Straight))throw Conflict(desk.Code+" · "+group.Code);
                    var visiblePermission=fields.FirstOrDefault(t=>t.Kind==PanelTileKind.LinePermission);var permission=visiblePermission??group.BlockTemplate?.FirstOrDefault(t=>t.Kind==PanelTileKind.LinePermission);if(permission!=null){var copy=permission.Clone();if(visiblePermission!=null)BlockDiagramViews.Orient(copy,group.BlockView??new BlockViewOptions(),group.Width,group.Height);permissions[desk.Id]=copy;}
                    groups[desk.Id]=group.Clone();doc.Tiles.RemoveAll(t=>group.Contains(t.X,t.Y));doc.Areas.Remove(group);
                }
                foreach(var tile in doc.Tiles)if(physical.Contains(tile.Reference)||physical.Contains(tile.AlternateSignal??"")||physical.Contains(tile.CircuitBoundary??"")||sections.Contains(tile.BlockId)||sections.Contains(tile.SecondBlock)||tile.CircuitRails?.Values.Any(sections.Contains)==true||numberIds.Contains(tile.NumberMirror??"")||numberIds.Contains(tile.NumberId??""))throw Conflict(desk.Code+" · "+(tile.X+1)+" / "+(tile.Y+1));
            }
            // Only objects belonging to this saved block installation are removed. End signals stay in place.
            markers.Markers.RemoveAll(m=>physical.Contains(m.Id));
            foreach(string id in heads)next.Automation.Signals.Remove(id);
            next.Automation.Contacts.RemoveAll(c=>heads.Contains(c.Signal));
            foreach(string id in sections)next.Blocks.Remove(id);
            draft.BlockUnits.Clear();draft.BlockSections.Clear();draft.BlockOwner="";draft.BlockDisplays.Clear();
            next.FreeLines[next.FreeLines.FindIndex(l=>l.Id==line.Id)]=draft.Clone();
            var built=LineBlockBuilder.Build(markers,next,draft,plan,owner,contacts,placement,stagger,displays);
            foreach(var pair in groups) {
                var doc=built.Library.Desks.Single(d=>d.Id==pair.Key).Panel;var group=BlockDiagram.Area(doc,line.Id);if(group==null)continue;
                if(BlockDiagram.CanPlace(doc,line.Id,pair.Value.X,pair.Value.Y))BlockDiagram.Move(doc,line.Id,pair.Value.X,pair.Value.Y);group.Code=pair.Value.Code;group.Name=pair.Value.Name;
                if(permissions.TryGetValue(pair.Key,out var previous)) {
                    var module=doc.Tiles.Single(t=>group.Contains(t.X,t.Y)&&t.Kind==PanelTileKind.LinePermission);module.PermissionAuxiliary=previous.PermissionAuxiliary;module.Label=previous.Label;module.Rotation=previous.Rotation;module.Mirror=previous.Mirror;
                }
            }
            var profiles=groups.ToDictionary(p=>p.Key,p=>BlockDiagramViews.Effective(p.Value.BlockView,built.Line.BlockSections.Count,p.Key==built.Line.BlockOwner));
            BlockDiagram.Rebuild(built.Library,built.Line,built.Line.BlockSections.Select(id=>built.Library.Blocks[id]).ToArray(),profiles);
            foreach(var overview in overviews) {
                var sources=LineNumberOverview.AutomaticSources(built.Library,built.Line,overview.Reverse);
                built.Library=LineNumberOverview.Create(built.Library,line.Id,overview.Desk,sources);
                var doc=built.Library.Desks.Single(d=>d.Id==overview.Desk).Panel;var created=doc.Areas.Last(a=>a.NumberLine==line.Id);
                created.Id=overview.Area.Id;created.Code=overview.Area.Code;created.Name=overview.Area.Name;
                if(LineNumberOverview.CanPlace(built.Library,overview.Desk,created.Id,overview.Area.X,overview.Area.Y))LineNumberOverview.Move(built.Library,overview.Desk,created.Id,overview.Area.X,overview.Area.Y);
            }
            built.RemovedSignals=heads.Count;built.Library.Validate(library.WorldKey);return built;
        }
    }
}
