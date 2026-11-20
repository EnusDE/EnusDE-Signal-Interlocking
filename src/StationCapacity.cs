using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class ReceivingTrack {
        public string Id=Guid.NewGuid().ToString("N"),Name="";
        public List<string> Blocks=new List<string>(),Arrivals=new List<string>();
        public ReceivingTrack Clone()=>new ReceivingTrack{Id=Id,Name=Name,Blocks=Blocks.ToList(),Arrivals=Arrivals.ToList()};
    }
    public static class StationCapacity {
        public static void Validate(DeskLibrary library) {
            foreach(var desk in library.Desks) {
                var tracks=desk.ReceivingTracks;
                if(tracks==null||tracks.Count>16||desk.MaximumReceivingTrains<0||desk.MaximumReceivingTrains>tracks.Count||tracks.Select(t=>t?.Id).Distinct().Count()!=tracks.Count)
                    throw new InvalidOperationException(LocalText.Select("Invalid receiving capacity at ","Ungültige Annahmekapazität bei ")+desk.Code);
                var used=new HashSet<string>();
                foreach(var track in tracks)if(track==null||!Guid.TryParseExact(track.Id,"N",out _)||string.IsNullOrWhiteSpace(track.Name)||track.Name.Length>48||track.Blocks==null||track.Blocks.Count==0||track.Blocks.Count>64||track.Blocks.Any(id=>!library.Blocks.ContainsKey(id)||!used.Add(id))||track.Arrivals==null||track.Arrivals.Count==0||track.Arrivals.Count>64||track.Arrivals.Distinct().Count()!=track.Arrivals.Count||track.Arrivals.Any(id=>StationAutomationPolicy.Owner(library,id)?.Id!=desk.Id))
                    throw new InvalidOperationException(LocalText.Select("Choose a name, separate detection sections and owned arrival signals for each receiving track: ","Je Empfangsgleis Name, getrennte Freimeldeabschnitte und eigene Ankunftssignale wählen: ")+desk.Code);
                for(int i=0;i<tracks.Count;i++)for(int j=i+1;j<tracks.Count;j++)
                    if(tracks[i].Blocks.SelectMany(id=>library.Blocks[id].Sections).Any(a=>tracks[j].Blocks.SelectMany(id=>library.Blocks[id].Sections).Any(a.Overlaps)))
                        throw new InvalidOperationException(LocalText.Select("Receiving tracks must not share physical detection: ","Empfangsgleise dürfen keine reale Freimeldung teilen: ")+desk.Code);
            }
        }
    }
    public sealed class StationCapacityAuthority:IRouteAdmission {
        public Func<DeskLibrary> Library;
        public Func<TrackBlock,TrackOccupancy> Occupancy;
        public Func<IEnumerable<RouteRun>> Active;
        public Func<double> Time;
        sealed class Lease {
            public string Station,Track;public HashSet<string> Arrivals=new HashSet<string>();public HashSet<string> Routes=new HashSet<string>();public Dictionary<string,RouteRun> Runs=new Dictionary<string,RouteRun>();public bool SeenOccupied;public int ClearChecks;public double LastRead=double.NegativeInfinity;
            public Lease Clone()=>new Lease{Station=Station,Track=Track,Arrivals=new HashSet<string>(Arrivals),Routes=new HashSet<string>(Routes),Runs=new Dictionary<string,RouteRun>(Runs),SeenOccupied=SeenOccupied,ClearChecks=ClearChecks,LastRead=LastRead};
        }
        List<Lease> leases=new List<Lease>();
        string capacityWorld;
        DeskLibrary CurrentLibrary(){var library=Library?.Invoke();if(library!=null&&capacityWorld!=library.WorldKey){leases.Clear();capacityWorld=library.WorldKey;}return library;}
        public int Reservations=>leases.Count;
        public string TrackStatus(string station,string track) {
            var library=Library?.Invoke();var value=library?.Desks.FirstOrDefault(d=>d.Id==station)?.ReceivingTracks.FirstOrDefault(t=>t.Id==track);
            if(value==null)return LocalText.Select("Missing assignment","Zuordnung fehlt");
            var occupancy=Read(library,value);bool reserved=leases.Any(l=>l.Station==station&&l.Track==track);
            return (occupancy==TrackOccupancy.Unknown?LocalText.Select("Detection unknown","Freimeldung unbekannt"):occupancy==TrackOccupancy.Occupied?LocalText.Select("Occupied","Belegt"):LocalText.Select("Clear","Frei"))+(reserved?LocalText.Select(" · reserved for an arrival"," · für eine Einfahrt reserviert"):"");
        }
        TrackOccupancy Read(DeskLibrary library,ReceivingTrack track) {
            TrackOccupancy SafeRead(string id){try{return library.Blocks.TryGetValue(id,out var block)?Occupancy?.Invoke(block)??TrackOccupancy.Unknown:TrackOccupancy.Unknown;}catch{return TrackOccupancy.Unknown;}}
            var readings=track.Blocks.Select(SafeRead).ToArray();
            return readings.Any(s=>s==TrackOccupancy.Unknown)?TrackOccupancy.Unknown:readings.Any(s=>s==TrackOccupancy.Occupied)?TrackOccupancy.Occupied:TrackOccupancy.Clear;
        }
        public void Observe(double now) {
            if(leases.Count==0)return;var library=CurrentLibrary();if(library==null)return;var runs=(Active?.Invoke()??Enumerable.Empty<RouteRun>()).ToArray();
            foreach(var lease in leases.ToArray()) {
                if(now<=lease.LastRead)continue;lease.LastRead=now;
                var track=library.Desks.FirstOrDefault(d=>d.Id==lease.Station)?.ReceivingTracks.FirstOrDefault(t=>t.Id==lease.Track);if(track==null)continue;
                var occupancy=Read(library,track);if(occupancy==TrackOccupancy.Occupied)lease.SeenOccupied=true;
                foreach(var run in runs.Where(r=>lease.Routes.Contains(r.Route.Id)))if(!lease.Runs.ContainsKey(run.Route.Id))lease.Runs.Add(run.Route.Id,run);
                bool active=runs.Any(r=>lease.Runs.Values.Contains(r));
                // A cancelled, unentered request can be released. A completed approach keeps its slot until actual receiving detection was occupied and cleared.
                bool cancelled=lease.Routes.All(id=>lease.Runs.TryGetValue(id,out var run)&&run.Stage==RouteStage.Cancelled&&!run.Entered);
                lease.ClearChecks=!active&&occupancy==TrackOccupancy.Clear&&(lease.SeenOccupied||cancelled)?lease.ClearChecks+1:0;
                if(lease.ClearChecks>=3)leases.Remove(lease);
            }
        }
        sealed class Ticket:IRouteAdmissionTicket {
            readonly StationCapacityAuthority owner;readonly List<Lease> before;bool committed;
            public Ticket(StationCapacityAuthority owner){this.owner=owner;before=owner.leases.Select(l=>l.Clone()).ToList();}
            public void Commit(){foreach(var lease in owner.leases)foreach(var run in owner.Active?.Invoke()??Enumerable.Empty<RouteRun>())if(lease.Routes.Contains(run.Route.Id)&&!lease.Runs.ContainsKey(run.Route.Id))lease.Runs.Add(run.Route.Id,run);committed=true;}
            public void Dispose(){if(!committed)owner.leases=before;}
        }
        public IRouteAdmissionTicket Reserve(IReadOnlyList<RouteDefinition> routes) {
            var current=CurrentLibrary();Observe(Time?.Invoke()??0);var ticket=new Ticket(this);
            try {
                var library=current;
                // An approach can reserve capacity before its final receiving route is chosen.
                // Once that local route is requested, bind the reservation to the actual track.
                foreach(var route in routes.Where(r=>!r.Shunting)) {
                    var station=StationAutomationPolicy.Owner(library,route.Entry);
                    if(station==null||station.MaximumReceivingTrains==0||StationAutomationPolicy.Owner(library,route.Exit)?.Id!=station.Id)continue;
                    var lease=leases.FirstOrDefault(l=>l.Station==station.Id&&!l.SeenOccupied&&l.Arrivals.Contains(route.Entry));if(lease==null)continue;
                    var hits=station.ReceivingTracks.Where(t=>t.Blocks.Any(id=>library.Blocks[id].Sections.Any(s=>route.Path?.Any(p=>p.Section.Overlaps(s))==true))).ToArray();
                    if(hits.Length==0)continue;
                    if(hits.Length!=1)throw new InvalidOperationException(station.Code+LocalText.Select(": the receiving route crosses multiple configured receiving tracks. Check the track definitions.",": die Einfahrt führt über mehrere definierte Empfangsgleise. Gleisdefinitionen prüfen."));
                    var target=hits[0];if(target.Id!=lease.Track&&(Read(library,target)!=TrackOccupancy.Clear||leases.Any(l=>l!=lease&&l.Station==station.Id&&l.Track==target.Id)))throw new InvalidOperationException(station.Code+" · "+target.Name+LocalText.Select(": receiving track occupied, unknown or reserved for another arrival.",": Empfangsgleis belegt, unbekannt oder für eine andere Einfahrt reserviert."));
                    lease.Track=target.Id;lease.Routes.Add(route.Id);
                }
                var blockHeads=library.FreeLines.SelectMany(l=>l.BlockUnits).SelectMany(u=>new[]{u.Forward,u.Reverse}).ToHashSet();
                foreach(var group in routes.Where(r=>!r.Shunting&&!blockHeads.Contains(r.Exit)).Select(r=>new{Route=r,Station=StationAutomationPolicy.Owner(library,r.Exit),Origin=StationAutomationPolicy.Owner(library,r.Entry)})
                    .Where(r=>r.Station!=null&&r.Station.MaximumReceivingTrains>0&&r.Station.Id!=r.Origin?.Id).GroupBy(r=>r.Station.Id)) {
                    var station=group.First().Station;var requested=group.Select(r=>r.Route).ToArray();
                    var stationLeases=leases.Where(l=>l.Station==station.Id).ToArray();
                    bool Busy(ReceivingTrack t)=>stationLeases.Any(l=>l.Track==t.Id)||Read(library,t)!=TrackOccupancy.Clear;
                    if(station.ReceivingTracks.Count(Busy)>=station.MaximumReceivingTrains)throw new InvalidOperationException(station.Code+LocalText.Select(": receiving capacity occupied or reserved.",": Annahmekapazität belegt oder reserviert."));
                    bool Along(ReceivingTrack t)=>t.Blocks.Any(id=>library.Blocks[id].Sections.Any(s=>routes.Any(r=>r.Path?.Any(p=>p.Section.Overlaps(s))==true)));
                    var track=station.ReceivingTracks.Where(t=>requested.Any(r=>t.Arrivals.Contains(r.Exit))&&!Busy(t)).OrderByDescending(Along).FirstOrDefault();
                    if(track==null)throw new InvalidOperationException(station.Code+LocalText.Select(": no proved free receiving track for this arrival signal. Check the receiving-track assignments.",": kein nachgewiesen freies Empfangsgleis für dieses Ankunftssignal. Empfangsgleiszuordnung prüfen."));
                    leases.Add(new Lease{Station=station.Id,Track=track.Id,Arrivals=requested.Select(r=>r.Exit).ToHashSet(),Routes=routes.Select(r=>r.Id).ToHashSet()});
                }
                return ticket;
            }catch{ticket.Dispose();throw;}
        }
    }
}
