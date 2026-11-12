using System;
using System.Linq;
using System.Collections.Generic;
using Track;

namespace EnusDE.Signaltechnik {
    public interface IApproachStopWorld {bool StoppedApproach(RouteDefinition route,out string identity,out string detail);}
    public sealed partial class SignalWorldBridge : IApproachStopWorld {
        public bool StoppedApproach(RouteDefinition route,out string identity,out string detail) {
            identity=null;detail="Annäherung nicht vollständig prüfbar";try {
                var controller=TrainController.Shared;if(controller==null)return false;var seen=new HashSet<string>();var queue=new Queue<Model.Car>();
                foreach(var section in route.Approach){var state=Occupancy(section);if(state==TrackOccupancy.Unknown)return false;if(state==TrackOccupancy.Clear)continue;TrackSpan span;if(!spans.TryGetValue(section.Key,out span)||!span.IsValid)return false;var cars=new List<Model.Car>();controller.GetCarsOnSpan(span,cars,512);if(cars.Count==0||cars.Count>=512)return false;foreach(var car in cars)queue.Enqueue(car);}
                while(queue.Count>0){var car=queue.Dequeue();if(car==null)return false;if(!seen.Add(car.id))continue;if(seen.Count>512||car.IsDerailed||car.IsInBardo||!car.LocationA.IsValid||!car.LocationB.IsValid){detail="Annäherungszug oder Zugschluss nicht vollständig lesbar";return false;}if(!car.IsStopped(.01f)){detail="Annäherungszug bewegt sich";return false;}foreach(var end in new[]{Model.Car.LogicalEnd.A,Model.Car.LogicalEnd.B}){var other=car.CoupledTo(end);if(other!=null)queue.Enqueue(other);}}
                if(seen.Count==0)return false;identity=string.Join("|",seen.OrderBy(id=>id,StringComparer.Ordinal));detail=seen.Count+" Fahrzeuge in vollständig bekanntem stehenden Verband";return true;
            }catch(Exception e){detail="Annäherung nicht prüfbar: "+e.Message;return false;}
        }
    }
    public sealed partial class InterlockingEngine {
        bool ReleaseApproach(RouteRun run,IInterlockingWorld world,out string detail) {
            var state=Read(run.Route.Approach,world);if(state==TrackOccupancy.Clear){run.ApproachStopChecks=0;run.ApproachStopIdentity=null;detail="Annäherung frei";return true;}
            string identity=null;detail=state==TrackOccupancy.Unknown?"Annäherung nicht lesbar":"Annäherungsbereich belegt";
            var stop=world as IApproachStopWorld;if(state!=TrackOccupancy.Unknown&&stop!=null&&stop.StoppedApproach(run.Route,out identity,out detail)&&!string.IsNullOrEmpty(identity)) {
                if(run.ApproachStopIdentity!=identity){run.ApproachStopChecks=1;run.ApproachStopReadTime=clock();}else if(clock()-run.ApproachStopReadTime>=.4){run.ApproachStopChecks++;run.ApproachStopReadTime=clock();}run.ApproachStopIdentity=identity;
                detail="Annäherung belegt, "+detail+" · Stillstandsprüfung "+Math.Min(3,run.ApproachStopChecks)+"/3";return run.ApproachStopChecks>=3;
            }
            run.ApproachStopChecks=0;run.ApproachStopIdentity=null;return false;
        }
    }
}
