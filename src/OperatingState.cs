using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public enum SignalOperatingState { Unknown,Unbound,Active,OutOfService }
    // Ownership comes from real bindings, not visibility, acceptance, or the automation pause.
    public sealed class OperatingStateIndex {
        readonly DeskLibrary library;
        readonly Dictionary<string,HashSet<string>> signals=new Dictionary<string,HashSet<string>>(),points=new Dictionary<string,HashSet<string>>();
        public OperatingStateIndex(DeskLibrary library,IEnumerable<RouteDefinition> routes) {
            this.library=library;
            foreach(var desk in library.Desks)foreach(var tile in desk.Panel.Tiles) {
                if(tile.Kind==PanelTileKind.Signal&&!tile.ReadOnlySignal)foreach(string id in new[]{tile.Reference,tile.AlternateSignal})Add(signals,id,desk.Id);
                if(tile.Kind==PanelTileKind.Switch)Add(points,tile.Reference,desk.Id);
            }
            foreach(var point in library.Points)Add(points,point.Key,point.Value.Desk);
            // Retain operating installations which were commissioned through the previous route workflow.
            string global=library.Desks.Single(d=>d.Global).Id;
            foreach(string id in routes.SelectMany(RouteSignals))if(!signals.ContainsKey(id))Add(signals,id,global);
        }
        static void Add(Dictionary<string,HashSet<string>> index,string id,string desk) {
            if(string.IsNullOrEmpty(id))return;if(!index.TryGetValue(id,out var owners))index[id]=owners=new HashSet<string>();owners.Add(desk);
        }
        static IEnumerable<string> RouteSignals(RouteDefinition route)=>new[]{route.Entry,route.Exit}.Concat(route.ViaSignals?.Select(s=>s.Id)??Enumerable.Empty<string>()).Concat(route.Flanks?.SelectMany(f=>f.Signals).Select(s=>s.Id)??Enumerable.Empty<string>());
        string[] Owners(Dictionary<string,HashSet<string>> index,string id) {
            if(string.IsNullOrEmpty(id)||!index.TryGetValue(id,out var values))return Array.Empty<string>();
            var locals=values.Where(owner=>library.Desks.Any(d=>d.Id==owner&&!d.IsMaster)).ToArray();return locals.Length>0?locals:values.ToArray();
        }
        public bool DeskDisabled(string id)=>library.Desks.Any(d=>d.Id==id&&d.OutOfService)||
            library.Modules.Any(m=>m.Desk==id&&library.Desks.Any(d=>d.Id==MasterDiagram.Owner(library,m)&&d.OutOfService));
        public SignalOperatingState Signal(string id,string announced=null) {
            var owners=Owners(signals,id);
            if(owners.Length==0&&announced!=null) {
                var inherited=Signal(announced);if(inherited!=SignalOperatingState.Unbound)return library.Automation.Signals.TryGetValue(id,out var distant)&&distant.OutOfService?SignalOperatingState.OutOfService:inherited;
            }
            if(owners.Length==0)return SignalOperatingState.Unbound;
            if(owners.All(DeskDisabled)||library.Automation.Signals.TryGetValue(id,out var signal)&&signal.OutOfService)return SignalOperatingState.OutOfService;
            return SignalOperatingState.Active;
        }
        public bool PointReleased(string id){var owners=Owners(points,id);return owners.Length>0&&owners.All(DeskDisabled);}
        public void RequireRoute(IReadOnlyList<RouteDefinition> routes,Func<string,SignalOperatingState> state) {
            foreach(var route in routes) {
                if(RouteSignals(route).Any(id=>state(id)!=SignalOperatingState.Active)||route.Switches.Any(p=>PointReleased(p.Node)))
                    throw new InvalidOperationException(LocalText.Select("Route uses an uncommissioned or out-of-service signal or interlocking. Restore operation before requesting a route.","Fahrweg verwendet ein nicht eingebundenes oder außer Betrieb gesetztes Signal beziehungsweise Stellwerk. Vor der Anforderung wieder in Betrieb nehmen."));
            }
        }
    }
    public sealed class OperatingRouteAdmission:IRouteAdmission {
        readonly Action<IReadOnlyList<RouteDefinition>> check;
        sealed class Ticket:IRouteAdmissionTicket {public void Commit(){}public void Dispose(){}}
        public OperatingRouteAdmission(Action<IReadOnlyList<RouteDefinition>> check){this.check=check;}
        public IRouteAdmissionTicket Reserve(IReadOnlyList<RouteDefinition> routes){check(routes);return new Ticket();}
    }
    public static class OperatingTransitionPolicy {
        public static bool Overlap(IEnumerable<TrackSection> held,IReadOnlyList<TrackSection> affected)=>held.Any(a=>affected.Any(a.Overlaps));
        public static bool Conflicts(RouteRun run,ISet<string> signals,ISet<string> points,IReadOnlyList<TrackSection> sections)=>
            signals.Any(id=>run.Route.Entry==id||run.Route.Exit==id||run.Route.ViaSignals?.Any(s=>s.Id==id)==true||run.Route.Flanks?.Any(f=>f.Signals.Any(s=>s.Id==id))==true)||
            run.Route.Switches.Any(p=>points.Contains(p.Node))||Overlap(run.HeldSections,sections);
    }
}
