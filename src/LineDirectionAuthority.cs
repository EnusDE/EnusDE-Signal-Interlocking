using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public sealed class CombinedRouteAdmission:IRouteAdmission {
        readonly IRouteAdmission[] gates;
        public CombinedRouteAdmission(params IRouteAdmission[] gates){this.gates=gates;}
        sealed class Ticket:IRouteAdmissionTicket {
            public readonly List<IRouteAdmissionTicket> Children=new List<IRouteAdmissionTicket>();
            public void Commit(){foreach(var child in Children)child?.Commit();}
            public void Dispose(){for(int i=Children.Count-1;i>=0;i--)Children[i]?.Dispose();}
        }
        public IRouteAdmissionTicket Reserve(IReadOnlyList<RouteDefinition> routes){var ticket=new Ticket();try{foreach(var gate in gates)ticket.Children.Add(gate?.Reserve(routes));return ticket;}catch{ticket.Dispose();throw;}}
    }
    public sealed class LineDirectionState {
        public LineTrackDefinition Definition;
        public string Signature,Error;
        public List<DirectedRouteSection> Axis;
        public int Direction,ClearChecks,RequestedDirection;
        public long Revision;
        public bool Held,ManualOverride;
        public double LastRead=double.NegativeInfinity;
        public HashSet<string> Routes=new HashSet<string>();
        public LineDirectionState Clone(){var copy=(LineDirectionState)MemberwiseClone();copy.Routes=new HashSet<string>(Routes);return copy;}
    }
    public sealed class LineDirectionAuthority:IRouteAdmission {
        Dictionary<string,LineDirectionState> states=new Dictionary<string,LineDirectionState>();
        long revision;
        public IReadOnlyCollection<LineDirectionState> States=>states.Values.ToArray();
        public bool BlockEnabled;
        public Func<TrackSection,TrackOccupancy> Occupancy;
        public Func<LineDirectionState,bool> AdditionalHold;
        public Func<LineTrackDefinition,bool> AutomaticAllowed;
        public Func<RouteDefinition,LineTargetBinding> TargetBinding;
        public bool IsAutomatic(LineDirectionState state)=>state.Definition.Automatic&&!state.ManualOverride&&AutomaticAllowed?.Invoke(state.Definition)!=false;
        public bool NeedsRefresh {get;private set;}
        public LineDirectionState Get(string id)=>id!=null&&states.TryGetValue(id,out var value)?value:null;
        TrackOccupancy Read(TrackSection section){try{return Occupancy?.Invoke(section)??TrackOccupancy.Unknown;}catch{return TrackOccupancy.Unknown;}}
        public void Reset(){if(HasHolds)throw new InvalidOperationException(LocalText.Select("Line direction still held","Streckenrichtung noch gebunden."));states.Clear();}
        public bool HasHolds=>states.Values.Any(s=>s.Held);
        public void Configure(IEnumerable<LineTrackDefinition> definitions,Func<LineTrackDefinition,string> check) {
            NeedsRefresh=false;var next=new Dictionary<string,LineDirectionState>();
            foreach(var definition in definitions) {
                string signature=LineConfigurationChanges.Technical(definition);var old=Get(definition.Id);
                if(old!=null&&old.Signature!=signature&&old.Held){NeedsRefresh=true;old.Error=LocalText.Select("Line changed while held","Strecke bei bestehender Bindung geändert.");next.Add(definition.Id,old);continue;}
                var state=old!=null&&old.Signature==signature?old:new LineDirectionState{Definition=definition.Clone(),Signature=signature,Revision=++revision,Axis=definition.Forward.Count==0||definition.Mode!=LineTrackMode.Directional&&definition.Reverse.Count==0?new List<DirectedRouteSection>():definition.Axis};
                state.Definition=definition.Clone();string error=definition.Enabled?check?.Invoke(definition):null;if(error!=state.Error)state.ClearChecks=0;state.Error=error;next.Add(definition.Id,state);
            }
            foreach(var old in states.Values.Where(s=>s.Held&&!next.ContainsKey(s.Definition.Id))){NeedsRefresh=true;old.Error=LocalText.Select("Held line removed","Gebundene Strecke entfernt.");next.Add(old.Definition.Id,old);}
            states=next;
        }
        static bool Touches(RouteDefinition route,LineDirectionState state)=>route.Path?.Any(p=>state.Axis.Any(a=>p.Section.Overlaps(a.Section)))==true;
        public void Observe(IEnumerable<RouteRun> active,double now) {
            foreach(var state in states.Values) {
                if(!state.Definition.Enabled&&!state.Held){state.ClearChecks=0;continue;}
                if(double.IsNaN(now)||double.IsInfinity(now)||now<=state.LastRead)continue;state.LastRead=now;
                var relevant=active.Where(r=>Touches(r.Route,state)||state.Routes.Contains(r.Route.Id)).ToArray();
                bool extraHold=AdditionalHold?.Invoke(state)==true;bool clear=!extraHold&&state.Axis.Count>0&&state.Axis.All(p=>Read(p.Section)==TrackOccupancy.Clear);
                state.ClearChecks=relevant.Length==0&&clear?Math.Min(3,state.ClearChecks+1):0;
                if(relevant.Length>0||extraHold)state.Held=true;
                if(state.Held&&state.ClearChecks>=3){state.Held=false;state.Routes.Clear();}
                // Ownership is established independently of a route request. Startup never restores a proceed aspect.
                if(BlockEnabled&&state.Definition.Enabled&&state.Error==null&&state.ClearChecks>=3) {
                    if(state.Direction==0){state.Direction=state.Definition.Mode==LineTrackMode.Directional?1:state.Definition.NormalDirection;state.Revision=++revision;}
                    if(state.RequestedDirection!=0&&IsAutomatic(state))Transfer(state,state.RequestedDirection);
                }
            }
        }
        sealed class Ticket:IRouteAdmissionTicket {
            readonly LineDirectionAuthority owner;readonly Dictionary<string,LineDirectionState> before;bool committed;
            public Ticket(LineDirectionAuthority owner){this.owner=owner;before=owner.states.ToDictionary(p=>p.Key,p=>p.Value.Clone());}
            public void Commit(){committed=true;}
            public void Dispose(){if(!committed)owner.states=before;}
        }
        public IRouteAdmissionTicket Reserve(IReadOnlyList<RouteDefinition> routes) {
            var ticket=new Ticket(this);
            try {
                var requested=routes.Select(r=>TargetBinding?.Invoke(r)).Where(b=>b!=null).ToArray();
                foreach(var binding in requested){if(binding.Error!=null)throw new InvalidOperationException(binding.Error);if(Get(binding.Line.Id)==null)throw new InvalidOperationException(LocalText.Select("Bound line permission is not loaded.","Gebundene Streckenerlaubnis ist nicht geladen."));}
                foreach(var state in states.Values.Where(s=>s.Definition.Enabled||s.Held||s.Definition.BlockOwner!=""||requested.Any(b=>b.Line.Id==s.Definition.Id))) {
                    var relevant=routes.Where(r=>Touches(r,state)||TargetBinding?.Invoke(r)?.Line?.Id==state.Definition.Id).ToArray();if(relevant.Length==0)continue;
                    var definition=state.Definition;
                    if(!definition.Enabled)throw new InvalidOperationException(definition.Code+LocalText.Select(": block installation is not activated. Open line setup.",": Blockanlage ist nicht aktiviert. Streckeneinrichtung öffnen."));
                    if(!BlockEnabled)throw new InvalidOperationException(definition.Code+LocalText.Select(": enable block coordination",": Blockabstimmung einschalten."));
                    if(state.Error!=null)throw new InvalidOperationException(definition.Code+": "+state.Error);
                    int direction=DirectionBlockBuilder.Direction(relevant.SelectMany(r=>r.Path),state.Axis);
                    var explicitDirections=requested.Where(b=>b.Line.Id==definition.Id).Select(b=>b.Direction).Distinct().ToArray();
                    if(explicitDirections.Length>1||explicitDirections.Length==1&&direction!=0&&direction!=explicitDirections[0])throw new InvalidOperationException(definition.Code+LocalText.Select(": route direction conflicts with its bound departure end.",": Fahrtrichtung widerspricht der gebundenen Abfahrtseite."));
                    if(direction==0&&explicitDirections.Length==1)direction=explicitDirections[0];
                    if(definition.Mode==LineTrackMode.Directional&&direction<0)throw new InvalidOperationException(definition.Code+LocalText.Select(": A → B only",": Nur Richtung A → B freigegeben."));
                    if(state.Held&&state.Direction!=direction)throw new InvalidOperationException(definition.Code+LocalText.Select(": opposing permission held",": Gegenrichtung gebunden."));
                    if(state.Direction!=direction)throw new InvalidOperationException(definition.Code+LocalText.Select(": the dispatching end does not hold permission; request it from the neighbour",": Die ablassende Betriebsstelle hat keine Erlaubnis; beim Nachbarn anfragen."));
                    // Following trains may use the same permission. The ordinary route/block gate proves their own path.
                    if(state.Axis.Any(p=>Read(p.Section)==TrackOccupancy.Unknown))throw new InvalidOperationException(definition.Code+LocalText.Select(": line detection unknown",": Streckenfreimeldung unbekannt."));
                    state.Held=true;state.ClearChecks=0;state.Routes.UnionWith(relevant.Select(r=>r.Id));
                }
                return ticket;
            }catch{ticket.Dispose();throw;}
        }
        public void RequirePermission(string id,int direction) {
            var state=Get(id)??throw new InvalidOperationException(LocalText.Select("Bound line permission is not loaded.","Gebundene Streckenerlaubnis ist nicht geladen."));
            if(!BlockEnabled||!state.Definition.Enabled||state.Error!=null)throw new InvalidOperationException(state.Error??LocalText.Select("Enable the assigned line and block coordination first.","Zugeordnete Strecke und Blockabstimmung zuerst aktivieren."));
            if(state.Definition.Mode==LineTrackMode.Directional&&direction!=1||state.Direction!=direction)throw new InvalidOperationException(state.Definition.Code+LocalText.Select(": departure refused: this end does not hold permission.",": Ausfahrt abgewiesen: Diese Betriebsstelle hat keine Erlaubnis."));
        }
        LineDirectionState RequireOperation(string id,string station,long revision) {
            var state=Get(id)??throw new InvalidOperationException(LocalText.Select("Line missing","Strecke fehlt."));
            if(station!=state.Definition.StationA&&station!=state.Definition.StationB)throw new InvalidOperationException(LocalText.Select("Operate from a neighbouring interlocking.","An einer Nachbarbetriebsstelle bedienen."));
            if(revision!=state.Revision)throw new InvalidOperationException(LocalText.Select("Permission changed. Refresh the controls.","Erlaubnis geändert. Bedienung aktualisieren."));
            if(!BlockEnabled||!state.Definition.Enabled||state.Error!=null)throw new InvalidOperationException(state.Error??LocalText.Select("Enable line and block coordination","Strecke und Blockabstimmung aktivieren."));
            if(state.Direction==0)throw new InvalidOperationException(LocalText.Select("Wait for initial complete clear proving.","Erste vollständige Freiprüfung abwarten."));
            return state;
        }
        static int End(LineDirectionState state,string station)=>station==state.Definition.StationA?1:-1;
        public void RequestPermission(string id,string station,long revision) {
            var state=RequireOperation(id,station,revision);int end=End(state,station);
            if(state.Definition.Mode==LineTrackMode.Directional||state.Direction==end)throw new InvalidOperationException(LocalText.Select("This end already holds permission, or the track is directional.","Diese Betriebsstelle hat bereits die Erlaubnis oder das Gleis ist ein Richtungsgleis."));
            if(state.RequestedDirection!=0)throw new InvalidOperationException(LocalText.Select("A permission request is already pending.","Eine Erlaubnisanfrage liegt bereits vor."));
            state.RequestedDirection=end;state.Revision=++this.revision;
        }
        public void CancelRequest(string id,string station,long revision) {
            var state=RequireOperation(id,station,revision);
            if(state.RequestedDirection!=End(state,station))throw new InvalidOperationException(LocalText.Select("Only the requesting end can cancel its request.","Nur die anfragende Betriebsstelle kann ihre Anfrage zurücknehmen."));
            state.RequestedDirection=0;state.Revision=++this.revision;
        }
        public void GivePermission(string id,string station,long revision,IEnumerable<RouteRun> active) {
            var state=RequireOperation(id,station,revision);int end=End(state,station);
            if(state.Definition.Mode==LineTrackMode.Directional||state.Direction!=end)throw new InvalidOperationException(LocalText.Select("Only the current owner may give permission to its neighbour.","Nur der aktuelle Inhaber darf die Erlaubnis an den Nachbarn abgeben."));
            if(state.Definition.RequireRequest&&state.RequestedDirection!=-end)throw new InvalidOperationException(LocalText.Select("The neighbour must request permission first.","Der Nachbar muss die Erlaubnis zuerst anfragen."));
            if(state.Held||AdditionalHold?.Invoke(state)==true||active.Any(r=>Touches(r.Route,state))||state.ClearChecks<3||state.Axis.Any(p=>Read(p.Section)!=TrackOccupancy.Clear))throw new InvalidOperationException(LocalText.Select("Permission change requires the whole line clear and no routes","Erlaubniswechsel erst bei vollständig freier Strecke ohne Fahrstraßen oder Restverschlüsse."));
            Transfer(state,-end);
        }
        void Transfer(LineDirectionState state,int direction){state.Direction=direction;state.RequestedDirection=0;state.Revision=++revision;}
        public void PauseAutomatic(string id,string station,long revision){var state=RequireOperation(id,station,revision);state.ManualOverride=true;state.Revision=++this.revision;}
        public void ResumeAutomatic(string id,string station,long revision){var state=RequireOperation(id,station,revision);if(AutomaticAllowed?.Invoke(state.Definition)==false)throw new InvalidOperationException(LocalText.Select("Neighbouring automatic operation is centrally paused.","Der Selbststellbetrieb eines Nachbarstellwerks ist zentral pausiert."));if(!state.Definition.Automatic)throw new InvalidOperationException(LocalText.Select("Automatic permission not configured","Automatische Erlaubnis nicht eingerichtet."));state.ManualOverride=false;state.Revision=++this.revision;}
    }
}
