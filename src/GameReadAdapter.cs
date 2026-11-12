using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Model;
using Track;
using Track.Signals;
using Game.Mow;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Passive reads only. Discovery runs separately for each enabled category.
    public sealed class GameReadAdapter : IDiagnosticSource {
        CTCSignal[] signals=new CTCSignal[0];
        TrackNode[] switches=new TrackNode[0];
        readonly Dictionary<int,object> signalTracks=new Dictionary<int,object>();
        static readonly FieldInfo signInstances=typeof(SignController).GetField("_instances",BindingFlags.Instance|BindingFlags.NonPublic);
        public int LocomotiveCount;
        public static string InterlockingIdentity(string id)=>"interlocking:"+id;
        public static object LocationValue(Location location) {
            if(!location.IsValid || location.segment==null)return null;
            var canonical=location.EndIsA?location:location.Flipped();
            return new {segment=location.segment.id,end=location.EndIsA?"A":"B",meters=location.distance,a_meters=canonical.distance};
        }
        public void Reset(){signals=new CTCSignal[0];switches=new TrackNode[0];signalTracks.Clear();LocomotiveCount=0;}
        public ReadBatch Read(string category,DiagnosticSettings settings,bool discover) {
            if(!settings[category].Enabled)throw new InvalidOperationException("Disabled diagnostic category requested.");
            var result=new ReadBatch();var filter=new IdFilter(settings[category].Ids);
            if(category=="vehicles") {
                var controller=TrainController.Shared;if(controller==null)return ReadBatch.Unavailable("Fahrzeugverwaltung nicht bereit.");
                LocomotiveCount=0;
                foreach(var car in controller.Cars) {
                    if(car==null || !filter.Allows(car.id))continue;
                    var loco=car as BaseLocomotive;if(settings.LocomotivesOnly && loco==null)continue;
                    if(loco!=null)LocomotiveCount++;
                    try {
                        object controls=null;
                        if(settings.VehicleControls && loco!=null && loco.ControlHelper!=null) {
                            var h=loco.ControlHelper;
                            controls=new {throttle=h.Throttle,reverser=h.Reverser,train_brake=h.TrainBrake,locomotive_brake=h.LocomotiveBrake,tractive_effort=loco.TractiveEffort,idle=loco.IsIdle,has_fuel=loco.HasFuel};
                        }
                        result.Rows.Add(new Observation(car.id,new {id=car.id,name=car.Ident.ToString(),locomotive=loco!=null,kmh=car.VelocityMphAbs*1.609344f,mph=car.VelocityMphAbs,signed_velocity=car.velocity,front=LocationValue(car.LocationF),rear=LocationValue(car.LocationR),controls=controls}));
                    }catch(Exception e){result.Rows.Add(new Observation(car.id,new {id=car.id,error=e.Message}));}
                }
            } else if(category=="signals") {
                if(discover) {
                    signals=UnityEngine.Object.FindObjectsByType<CTCSignal>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    signalTracks.Clear();var graph=Graph.Shared;
                    foreach(var signal in signals)if(signal!=null && filter.Allows(signal.id)) {
                        Location nearest;
                        if(graph!=null && graph.TryGetLocationFromWorldPoint(signal.transform.position,20,out nearest))signalTracks[signal.GetInstanceID()]=LocationValue(nearest);
                    }
                }
                foreach(var signal in signals) {
                    if(signal==null || !filter.Allows(signal.id))continue;
                    object track;signalTracks.TryGetValue(signal.GetInstanceID(),out track);
                    result.Rows.Add(new Observation(signal.id+":"+signal.direction,new {kind="signal",id=signal.id,name=signal.DisplayName,aspect=signal.CurrentAspect.ToString(),direction=signal.direction.ToString(),track=track,track_source="nearest_track_approximation"},signal.id));
                }
            } else if(category=="blocks") {
                var panel=CTCPanelController.Shared;if(panel==null)return ReadBatch.Unavailable("Stellwerk/Belegungen auf dieser Karte nicht verfügbar.");
                foreach(var pair in panel.AllBlocks)if(filter.Allows(pair.Key))
                    result.Rows.Add(new Observation(pair.Key,new {kind="block",id=pair.Key,occupied=pair.Value.IsOccupied,traffic_filter=pair.Value.TrafficFilter.ToString()}));
                foreach(var pair in panel.AllInterlockings)if(filter.Allows(pair.Key))
                    result.Rows.Add(new Observation(InterlockingIdentity(pair.Key),new {kind="interlocking",id=pair.Key,direction=pair.Value.Direction.ToString()},pair.Key));
            } else if(category=="switches") {
                var graph=Graph.Shared;if(graph==null)return ReadBatch.Unavailable("Gleisnetz nicht bereit.");
                if(discover)switches=graph.Nodes.Where(n=>graph.IsSwitch(n)).ToArray();
                foreach(var node in switches)if(node!=null && filter.Allows(node.id))
                    result.Rows.Add(new Observation(node.id,new {kind="switch",id=node.id,thrown=node.isThrown,ctc=node.IsCTCSwitch,unlocked=node.IsCTCSwitchUnlocked}));
            } else if(category=="signs") {
                var controller=SignController.Shared;
                if(controller==null || signInstances==null)return ReadBatch.Unavailable("Schilderverwaltung nicht verfügbar.");
                var instances=signInstances.GetValue(controller) as IDictionary<string,GameObject>;
                if(instances==null)return ReadBatch.Unavailable("Schilderbestand nicht lesbar.");
                foreach(var pair in instances) {
                    if(!filter.Allows(pair.Key) || pair.Value==null)continue;
                    PlayerSign sign;object state;
                    if(controller.TryGetPlayerSign(SignUnits.PlayerId(pair.Key),out sign)) {
                        state=new {kind="sign",id=pair.Key,sign_type=sign.Kind,text=sign.Text,mph=sign.Mph,location=sign.Location,segment=sign.SegmentId,source="player_sign_state",train_trigger_available=false};
                    } else {
                        var p=pair.Value.transform.position;
                        state=new {kind="sign",id=pair.Key,sign_type="world_sign",position=new {x=p.x,y=p.y,z=p.z},source="sign_controller_inventory",train_trigger_available=false};
                    }
                    result.Rows.Add(new Observation(pair.Key,state));
                }
            } else return ReadBatch.Unavailable("Unbekannter Datenbereich.");
            return result;
        }
    }
}
