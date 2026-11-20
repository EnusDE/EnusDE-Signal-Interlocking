using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class RequestSectionReading {
        public string Route,Role,Point;
        public TrackSection Section;
        public TrackOccupancy Occupancy;
        public bool Sampled;
    }
    public interface IRequestDiagnosticWorld {
        bool CachedRequestOccupancy(TrackSection section,out TrackOccupancy state);
        bool RequestPointPosition(string point,out bool position);
    }
    public sealed class RouteRequestDiagnostic {
        public DateTime Utc;
        public RouteDefinition[] Definitions;
        public bool Accepted,Truncated;
        public string Error,CaptureError;
        public List<RequestSectionReading> Sections=new List<RequestSectionReading>();
        public Dictionary<string,bool?> PointPositions=new Dictionary<string,bool?>();
    }
    public sealed partial class InterlockingEngine {
        public RouteRequestDiagnostic LastRequest {get;private set;}
        readonly Queue<RouteRequestDiagnostic> recentRequests=new Queue<RouteRequestDiagnostic>();
        public IReadOnlyCollection<RouteRequestDiagnostic> RecentRequests=>recentRequests.ToArray();
        void ObserveRequest(IEnumerable<RouteDefinition> definitions,IInterlockingWorld world,Action request) {
            RouteRequestDiagnostic trace=null;
            try{trace=new RouteRequestDiagnostic{Utc=DateTime.UtcNow,Definitions=definitions.Take(16).Select(r=>r.Clone()).ToArray()};LastRequest=trace;}catch{LastRequest=null;}
            try{request();if(trace!=null)trace.Accepted=true;}
            catch(Exception e){if(trace!=null)trace.Error=e.Message;throw;}
            finally {
                // Diagnostics never replace an admission result, move a point, or clear authority.
                if(trace!=null)try {
                    var diagnostic=world as IRequestDiagnosticWorld;
                    foreach(var route in trace.Definitions) {
                        Action<TrackSection,string,string> read=(section,role,point)=>{
                            if(trace.Sections.Count>=8192){trace.Truncated=true;return;}
                            TrackOccupancy state=TrackOccupancy.Unknown;
                            bool sampled=diagnostic!=null&&diagnostic.CachedRequestOccupancy(section,out state);
                            trace.Sections.Add(new RequestSectionReading{Route=route.Id,Role=role,Point=point,Section=section.Clone(),Occupancy=state,Sampled=sampled});
                        };
                        foreach(var section in route.Sections)read(section,route.Flanks?.Any(f=>f.Sections.Any(s=>s.Overlaps(section)))==true?"flank":"route",null);
                        foreach(var point in route.Switches) {
                            bool position=false;
                            trace.PointPositions[point.Node]=diagnostic!=null&&diagnostic.RequestPointPosition(point.Node,out position)?(bool?)position:null;
                            foreach(var section in point.Protection??Enumerable.Empty<TrackSection>())read(section,"point",point.Node);
                        }
                    }
                }catch(Exception e){trace.CaptureError=e.Message;}
                if(trace!=null){recentRequests.Enqueue(trace);while(recentRequests.Count>8)recentRequests.Dequeue();}
            }
        }
    }
}
