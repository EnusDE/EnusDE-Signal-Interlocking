using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class LineTutorial {
        public static LineSignalGroup[] SignalSteps(LineTrackMode mode)=>mode==LineTrackMode.Directional?new[]{LineSignalGroup.ADepartures,LineSignalGroup.BArrivals}:new[]{LineSignalGroup.AArrivals,LineSignalGroup.ADepartures,LineSignalGroup.BArrivals,LineSignalGroup.BDepartures};
        public static bool Selected(LineTrackDefinition line,LineSignalGroup group,PanelTile tile) {
            if(tile?.Kind!=PanelTileKind.Signal)return false;
            var selected=LineSignalGroups.Get(line,group);return selected.Contains(tile.Reference)||tile.AlternateSignal!=null&&selected.Contains(tile.AlternateSignal);
        }
        public static bool ToggleSelection(DeskLibrary library,LineTrackDefinition line,LineSignalGroup group,string id,Func<string,MarkerRecord> resolve) {
            var selected=LineSignalGroups.Get(line,group);
            if(selected.Remove(id)){LineSignalGroups.Put(line,group,selected);return false;}
            var signal=resolve(id);
            if(signal?.Kind!=MarkerRecord.Main||signal.Planned||StationAutomationPolicy.Owner(library,id)?.Id!=LineSignalGroups.Station(line,group))throw new InvalidOperationException(LocalText.Select("Choose a completed main signal belonging to the displayed neighbouring interlocking.","Ein fertig gebautes Hauptsignal des angezeigten Nachbarstellwerks wählen."));
            if(selected.Count>=32)throw new InvalidOperationException(LocalText.Select("A step supports up to 32 signals. Deselect a signal before adding another.","Ein Schritt unterstützt bis zu 32 Signale. Vor dem Hinzufügen ein Signal abwählen."));
            selected.Add(id);LineSignalGroups.Put(line,group,selected);return true;
        }
        public static string SelectionIssue(DeskLibrary library,LineTrackDefinition line,LineSignalGroup group,Func<string,MarkerRecord> resolve) {
            var selected=LineSignalGroups.Get(line,group);
            if(selected.Count==0)return LocalText.Select("Choose at least one signal for this physical line track.","Für dieses physische Streckengleis mindestens ein Signal auswählen.");
            if(selected.Count>32)return LocalText.Select("Keep at most 32 signals in this step.","In diesem Schritt höchstens 32 Signale auswählen.");
            string station=LineSignalGroups.Station(line,group);
            var opposite=group==LineSignalGroup.ADepartures?LineSignalGroup.AArrivals:group==LineSignalGroup.AArrivals?LineSignalGroup.ADepartures:group==LineSignalGroup.BDepartures?LineSignalGroup.BArrivals:LineSignalGroup.BDepartures;
            var other=LineSignalGroups.Get(line,opposite);
            foreach(string id in selected) {
                var signal=resolve(id);string name=(signal?.Name??id)+": ";
                if(signal?.Kind!=MarkerRecord.Main||signal.Planned||StationAutomationPolicy.Owner(library,id)?.Id!=station)return name+LocalText.Select("Select a completed main signal in this neighbour's diagram.","Ein fertig gebautes Hauptsignal im Gleisbild dieser Nachbarbetriebsstelle auswählen.");
                if(LineSignalGroups.Departure(group)&&!SignalCatalogue.Supports(signal,false,false)&&!SignalCatalogue.Supports(signal,false,true))return name+LocalText.Select("This departure needs a main signal supporting a train proceed aspect.","Für diese Ausfahrt wird ein Hauptsignal mit einem Zugfahrtbegriff benötigt.");
                if(other.Contains(id))return name+LocalText.Select("This signal face is already selected for the opposite role. Use different inward and outward signal faces.","Diese Signalrichtung ist bereits für die Gegenrolle gewählt. Für Einfahrt und Ausfahrt verschiedene Signalrichtungen verwenden.");
            }
            return null;
        }
    }
    // Construction geometry only: no route is requested, stored, or cleared by this search.
    public static class LineSetupSearch {
        public static string NetworkShape(NetworkInput input)=>NetworkRecognition.Signature(new NetworkInput{World=input.World,Nodes=input.Nodes,Edges=input.Edges});
        public static List<RouteChainPlan> Find(NetworkInput graph,IEnumerable<MarkerRecord> available,MarkerRecord start,MarkerRecord target) {
            if(start?.Kind!=MarkerRecord.Main||start.Planned||target==null||target.Planned||!AuxiliaryTarget.Accepts(target,false)||start.Id==target.Id)throw new InvalidOperationException(LocalText.Select("Select different departure and arrival signals for this direction.","Für diese Richtung verschiedene Ausfahr- und Einfahrsignale auswählen."));
            bool slow=!SignalCatalogue.Supports(start,false,false)&&SignalCatalogue.Supports(start,false,true);
            var signals=available.Where(s=>s.IsSignal&&!s.Planned).ToArray();
            var found=AutomaticRoutePlanner.Find(graph,signals,start,target,slow?RouteKind.Slow:RouteKind.Train);
            if(found.Count==0)throw new InvalidOperationException(start.Name+" → "+target.Name+LocalText.Select(": no path in the selected direction. Recheck this departure toward the neighbour and that neighbour's entry facing into its station. No target button or GFM is needed for this geometry check. If the directions are correct, check that the physical track is connected and available.",": kein Fahrweg in der gewählten Richtung. Die Ausfahrt zum Nachbarn und dessen in den Bahnhof gerichtete Einfahrt erneut prüfen. Für diese Gleiswegprüfung werden noch keine Zieltaste und keine GFM benötigt. Stimmen die Richtungen, die reale Gleisverbindung und deren Verfügbarkeit prüfen."));
            return found.Select(chain=>new RouteChainPlan{Routes=chain.Select(p=>new RouteDefinition{Code=(signals.FirstOrDefault(s=>s.Id==p.Entry)?.Name??p.Entry)+" → "+(signals.FirstOrDefault(s=>s.Id==p.Exit)?.Name??target.Name),Entry=p.Entry,Exit=p.Exit,Path=p.Path.Select(s=>s.Clone()).ToList(),Sections=p.Path.Select(s=>s.Section.Clone()).ToList(),Switches=p.Switches.Select(s=>new RouteSwitch{Node=s.Node,Thrown=s.Thrown}).ToList()}).ToList()}).ToList();
        }
    }
    public sealed class LineControlPlacement {public string Desk,Target;public int TargetX,TargetY,PermissionX,PermissionY;public bool CreatedTarget,CreatedPermission;}
    public sealed class LineControlPlan {
        public DeskLibrary Library;
        public List<LineControlPlacement> Positions=new List<LineControlPlacement>();
    }
    public static class LineControlSetup {
        public static LineControlPlan Preview(DeskLibrary library,LineTrackDefinition line,IEnumerable<MarkerRecord> available) {
            var result=new LineControlPlan{Library=library.Clone()};Apply(result.Library,line,available,result.Positions);return result;
        }
        public static void Apply(DeskLibrary library,LineTrackDefinition line,IEnumerable<MarkerRecord> available,List<LineControlPlacement> positions=null) {
            if(!library.FreeLines.Any(l=>l.Id==line.Id))throw new InvalidOperationException(LocalText.Select("Include the connection in the preview before creating its controls.","Vor dem Erstellen der Bedienfelder die Verbindung in die Vorschau aufnehmen."));
            var signals=available.Where(s=>s.IsSignal&&!s.Planned).ToDictionary(s=>s.Id);
            foreach(var side in new[]{(Station:line.StationA,Neighbour:line.StationB,Entry:line.ReverseEnd),(Station:line.StationB,Neighbour:line.StationA,Entry:line.ForwardEnd)}) {
                if(line.Mode==LineTrackMode.Directional&&side.Station==line.StationB)continue;
                var desk=library.Desks.Single(d=>d.Id==side.Station&&!d.IsMaster);var doc=desk.Panel;
                if(!signals.TryGetValue(side.Entry,out var entry)||entry.Kind!=MarkerRecord.Main||StationAutomationPolicy.Owner(library,entry.Id)?.Id!=desk.Id)throw new InvalidOperationException(desk.Code+LocalText.Select(": select its entry signal before creating the route destination.",": vor dem Erstellen der Zieltaste das Einfahrsignal auswählen."));
                var reference=doc.Tiles.First(t=>t.Kind==PanelTileKind.Signal&&!t.ReadOnlySignal&&(t.Reference==entry.Id||t.AlternateSignal==entry.Id));
                var modules=doc.Tiles.Where(t=>t.Kind==PanelTileKind.LinePermission&&t.Reference==line.Id&&!t.ReadOnlySignal).ToArray();
                var bound=LineTargetBindings.Index(library);
                var module=modules.FirstOrDefault(t=>t.PermissionAuxiliary!=null&&bound.TryGetValue(t.PermissionAuxiliary,out var b)&&b.Line?.Id==line.Id&&b.Station==desk.Id)??modules.FirstOrDefault();
                var target=doc.Tiles.Select(t=>t.Target).FirstOrDefault(t=>t!=null&&t.Function==AuxiliaryFunction.RouteDestination&&t.Train&&bound.TryGetValue(t.Id,out var b)&&b.Line?.Id==line.Id&&b.Station==desk.Id&&(module?.PermissionAuxiliary==null||module.PermissionAuxiliary==t.Id));
                var position=new LineControlPlacement{Desk=desk.Id};
                (int X,int Y) Free() {
                    var occupied=doc.Tiles.Select(t=>(t.X,t.Y)).ToHashSet();
                    bool Fits(int x,int y)=>x>=0&&y>=0&&x<doc.Columns&&y<doc.Rows&&!occupied.Contains((x,y))&&!doc.Areas.Any(a=>(a.BlockLine!=null||a.NumberLine!=null)&&a.Contains(x,y));
                    for(int radius=1;radius<=4;radius++)foreach(var at in new[]{(reference.X+radius,reference.Y),(reference.X-radius,reference.Y),(reference.X,reference.Y+radius),(reference.X,reference.Y-radius)})if(Fits(at.Item1,at.Item2))return at;
                    for(int y=0;y<doc.Rows;y++)for(int x=0;x<doc.Columns;x++)if(Fits(x,y))return(x,y);
                    throw new InvalidOperationException(desk.Code+LocalText.Select(": make room in the diagram for the target and permission fields.",": im Gleisbild Platz für Ziel- und Erlaubnisfeld schaffen."));
                }
                if(target==null) {
                    var neighbour=library.Desks.Single(d=>d.Id==side.Neighbour);target=new AuxiliaryTarget{Name=LocalText.Select("To ","Nach ")+neighbour.Code};AuxiliarySetup.Link(target,entry);target.PermissionLine=line.Id;
                    var at=Free();doc.Put(new PanelTile{X=at.X,Y=at.Y,Kind=PanelTileKind.AuxiliaryTarget,Target=target});target=doc.At(at.X,at.Y).Target;position.CreatedTarget=true;
                }
                if(module==null){var at=Free();doc.Put(new PanelTile{X=at.X,Y=at.Y,Kind=PanelTileKind.LinePermission,Reference=line.Id});module=doc.At(at.X,at.Y);position.CreatedPermission=true;}
                if(module.PermissionAuxiliary!=null&&module.PermissionAuxiliary!=target.Id)throw new InvalidOperationException(desk.Code+LocalText.Select(": the permission field is already linked to another auxiliary. Review that link first.",": das Erlaubnisfeld ist bereits mit einer anderen Hilfstaste verknüpft. Diese Verbindung zuerst prüfen."));
                AuxiliarySetup.Link(target,entry);target.PermissionLine=line.Id;
                int entryDirection=reference.Reference==entry.Id?SignalCellLayout.MainDirection(reference):SignalCellLayout.ShuntDirection(reference);
                var targetField=doc.Tiles.First(t=>t.Target?.Id==target.Id);targetField.Rotation=(entryDirection+2)%4;targetField.Mirror=reference.Mirror;
                int ports=PanelConnections.Ports(module);module.Rotation=side.Station==line.StationA?targetField.Rotation:(targetField.Rotation+2)%4;module.Mirror=reference.Mirror;
                if(module.InlinePermission)module.Ports=PanelConnections.Transform(ports,module.Rotation,module.Mirror,true);
                var departures=LineSignalGroups.Get(line,side.Station==line.StationA?LineSignalGroup.ADepartures:LineSignalGroup.BDepartures);
                foreach(var departure in doc.Tiles.Where(t=>t.Kind==PanelTileKind.Signal&&!t.ReadOnlySignal&&(departures.Contains(t.Reference)||t.AlternateSignal!=null&&departures.Contains(t.AlternateSignal)))) {
                    if(departure.LinkedAuxiliary!=null&&departure.LinkedAuxiliary!=target.Id)throw new InvalidOperationException(desk.Code+LocalText.Select(": a departure already uses a different auxiliary destination. Review that assignment before completing setup.",": eine Ausfahrt verwendet bereits ein anderes Hilfsziel. Diese Zuordnung vor dem Abschluss prüfen."));
                    departure.LinkedAuxiliary=target.Id;
                }
                module.PermissionAuxiliary=target.Id;var field=doc.Tiles.First(t=>t.Target?.Id==target.Id);position.Target=target.Id;position.TargetX=field.X;position.TargetY=field.Y;position.PermissionX=module.X;position.PermissionY=module.Y;positions?.Add(position);
            }
            library.Validate(library.WorldKey);
        }
    }
}
