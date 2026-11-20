using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    // Remove the shared connection as one configuration change; retain world objects and manual drawings.
    public sealed class LineRemoval {
        public DeskLibrary Library;
        public string Id,Caption;
        public string[] Targets;
        public int Indicators,Auxiliaries,Groups,SignalRules,Contacts;
        public static LineRemoval Preview(DeskLibrary library,string id) {
            var line=library.FreeLines.SingleOrDefault(l=>l.Id==id)??throw new InvalidOperationException(LocalText.Select("This line connection no longer exists.","Diese Streckenverbindung ist nicht mehr vorhanden."));
            var next=library.Clone();
            var targets=LineTargetBindings.Index(library).Where(p=>p.Value.Line?.Id==id).Select(p=>p.Key)
                .Concat(library.Desks.SelectMany(d=>d.Panel.Tiles).Where(t=>t.Target?.PermissionLine==id).Select(t=>t.Target.Id)).ToHashSet();
            var result=new LineRemoval{Library=next,Id=id,Caption=line.Code+" · "+line.Name+" · "+line.Track,Targets=targets.ToArray()};
            next.FreeLines.RemoveAll(l=>l.Id==id);
            foreach(var desk in next.Desks) {
                foreach(var tile in desk.Panel.Tiles) {
                    if(tile.Kind==PanelTileKind.LinePermission&&tile.Reference==id){tile.Reference="";tile.PermissionAuxiliary=null;result.Indicators++;}
                    if(tile.Target?.PermissionLine==id)tile.Target.PermissionLine="";
                }
                foreach(var area in desk.Panel.Areas)if(area.BlockLine==id||area.NumberLine==id){area.BlockLine=null;area.NumberLine=null;area.BlockView=null;area.BlockTemplate=null;result.Groups++;}
            }
            var remaining=LineTargetBindings.Index(next);
            foreach(var target in next.Desks.SelectMany(d=>d.Panel.Tiles).Where(t=>t.Target!=null).Select(t=>t.Target)) {
                if(!targets.Contains(target.Id)||remaining.TryGetValue(target.Id,out var binding)&&binding.Line!=null)continue;
                // Keep its settings for reuse, but never turn a protected train destination into an unprotected one.
                if(target.Function==AuxiliaryFunction.RouteDestination)target.Function=AuxiliaryFunction.LinePermission;
                target.PermissionLine="";
            }
            result.Auxiliaries=targets.Count(id=>!remaining.TryGetValue(id,out var binding)||binding.Line==null);
            var heads=line.BlockUnits.SelectMany(u=>new[]{u.Forward,u.Reverse}).Where(s=>s!="").ToHashSet();
            heads.ExceptWith(next.FreeLines.SelectMany(l=>l.BlockUnits).SelectMany(u=>new[]{u.Forward,u.Reverse}));
            foreach(var pair in next.Automation.Signals)if(heads.Contains(pair.Key)&&pair.Value.Enabled){pair.Value.Enabled=false;result.SignalRules++;}
            foreach(var contact in next.Automation.Contacts)if(heads.Contains(contact.Signal)&&contact.Enabled){contact.Enabled=false;result.Contacts++;}
            next.Validate(library.WorldKey);return result;
        }
        public static void RequireClear(DeskLibrary library,string id,IEnumerable<RouteRun> runs,IEnumerable<DirectionBlockLease> leases,IEnumerable<LineDirectionState> states,Func<TrackSection,TrackOccupancy> read) {
            var line=library.FreeLines.SingleOrDefault(l=>l.Id==id)??throw new InvalidOperationException(LocalText.Select("This line connection no longer exists.","Diese Streckenverbindung ist nicht mehr vorhanden."));
            var targets=LineTargetBindings.Index(library).Where(p=>p.Value.Line?.Id==id).Select(p=>p.Key).ToArray();
            var blockers=LineConfigurationChanges.Blockers(library.FreeLines,new[]{id},targets,runs,leases,states,read).ToList();
            var sections=line.Forward.Concat(line.Reverse).Select(p=>p.Section).Concat(line.BlockSections.Where(library.Blocks.ContainsKey).SelectMany(key=>library.Blocks[key].Sections)).GroupBy(s=>s.Key).Select(g=>g.First());
            foreach(var section in sections) {
                TrackOccupancy value;try{value=read?.Invoke(section)??TrackOccupancy.Unknown;}catch{value=TrackOccupancy.Unknown;}
                if(value!=TrackOccupancy.Clear)blockers.Add(section.Segment+" · "+SignUnits.Number(section.From)+"–"+SignUnits.Number(section.To)+" m · "+(value==TrackOccupancy.Occupied?LocalText.Select("occupied","belegt"):LocalText.Select("detection unavailable","Freimeldung nicht verfügbar")));
            }
            if(blockers.Count>0)throw new InvalidOperationException(LocalText.Select("Cannot delete this connection yet:\n","Diese Verbindung kann noch nicht gelöscht werden:\n")+string.Join("\n",blockers.Distinct()));
        }
    }
}
