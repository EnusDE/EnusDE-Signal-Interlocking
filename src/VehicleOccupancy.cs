using System;
using System.Linq;
using System.Collections.Generic;
using Track;

namespace EnusDE.Signaltechnik {
    public sealed class VehicleTrackFootprint {
        public string Id;
        public List<TrackSection> Sections=new List<TrackSection>();
        public HashSet<string> UnknownSegments=new HashSet<string>(StringComparer.Ordinal);
        public bool GlobalUnknown;
    }
    public sealed class VehicleOccupancyIndex {
        readonly Dictionary<string,List<VehicleTrackFootprint>> bySegment=new Dictionary<string,List<VehicleTrackFootprint>>(StringComparer.Ordinal);
        readonly HashSet<string> unknown=new HashSet<string>(StringComparer.Ordinal);
        public readonly List<string> UnresolvedVehicles=new List<string>();
        public bool GlobalUnknown {get;private set;}
        public VehicleOccupancyIndex(IEnumerable<VehicleTrackFootprint> vehicles) {
            foreach(var car in vehicles) {
                if(car.GlobalUnknown||car.UnknownSegments.Count>0)UnresolvedVehicles.Add(car.Id??"vehicle-data-unavailable");
                GlobalUnknown|=car.GlobalUnknown;unknown.UnionWith(car.UnknownSegments);
                foreach(string id in car.Sections.Select(s=>s.Segment).Distinct()) {
                    if(!bySegment.TryGetValue(id,out var rows))bySegment[id]=rows=new List<VehicleTrackFootprint>();
                    rows.Add(car);
                }
            }
        }
        public TrackOccupancy Read(TrackSection section,out string[] cars) {
            cars=null;if(GlobalUnknown||unknown.Contains(section.Segment))return TrackOccupancy.Unknown;
            cars=bySegment.TryGetValue(section.Segment,out var rows)?rows.Where(c=>c.Sections.Any(s=>s.Overlaps(section))).Select(c=>c.Id).Distinct().ToArray():Array.Empty<string>();
            return cars.Length>0?TrackOccupancy.Occupied:TrackOccupancy.Clear;
        }
    }
    public sealed partial class SignalWorldBridge {
        VehicleOccupancyIndex vehicleOccupancy;
        readonly Dictionary<string,string[]> occupancyCars=new Dictionary<string,string[]>();
        void EnsureVehicleOccupancy() {
            if(vehicleOccupancy!=null)return;
            var controller=TrainController.Shared;
            if(controller==null){vehicleOccupancy=new VehicleOccupancyIndex(new[]{new VehicleTrackFootprint{GlobalUnknown=true}});return;}
            var cars=controller.Cars.Where(c=>c!=null&&!c.IsInBardo).Take(4097).ToArray();
            if(cars.Length>4096){vehicleOccupancy=new VehicleOccupancyIndex(new[]{new VehicleTrackFootprint{GlobalUnknown=true}});return;}
            vehicleOccupancy=new VehicleOccupancyIndex(cars.Select(ReadVehicleFootprint));
        }
        static VehicleTrackFootprint ReadVehicleFootprint(Model.Car car) {
            var result=new VehicleTrackFootprint{Id=car.id};var front=car.LocationF;var rear=car.LocationR;
            try {
                if(!front.IsValid||!rear.IsValid||car.IsDerailed)throw new InvalidOperationException("Vehicle endpoints unavailable.");
                var a=front.WithEnd(TrackSegment.End.A);var b=rear.WithEnd(TrackSegment.End.A);
                if(a.segment==b.segment) {
                    double low=Math.Min(a.distance,b.distance),high=Math.Max(a.distance,b.distance);
                    if(high-low<.001)throw new InvalidOperationException("Vehicle endpoints coincide.");
                    result.Sections.Add(new TrackSection{Segment=a.segment.id,From=low,To=high});return result;
                }
                if(!Graph.Shared.TryGetDistanceBetweenSameRoute(rear,front,out float distance)||distance<=0||distance>256)throw new InvalidOperationException("Vehicle body path unavailable.");
                var sections=Walk(rear,distance,out var ignored,out var finish,out var path);
                if(!finish.WithEnd(TrackSegment.End.A).Equals(front.WithEnd(TrackSegment.End.A),.1f))throw new InvalidOperationException("Vehicle body path does not reach its front.");
                result.Sections=sections;return result;
            }catch {
                if(front.IsValid)result.UnknownSegments.Add(front.segment.id);
                if(rear.IsValid)result.UnknownSegments.Add(rear.segment.id);
                result.GlobalUnknown=result.UnknownSegments.Count==0||front.IsValid&&rear.IsValid&&front.segment!=rear.segment;return result;
            }
        }
    }
}
