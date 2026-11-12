using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Track;
using Track.Signals;
using Game.State;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalWorldBridge : IInterlockingWorld,ISectionalWorld,ISwitchFeedbackWorld,ITrainStopWorld,IDisposable {
        readonly SignalRuntime owner;
        readonly Transform parent;
        readonly Dictionary<string,TrackSpan> spans=new Dictionary<string,TrackSpan>();
        readonly Dictionary<string,TrackOccupancy> readings=new Dictionary<string,TrackOccupancy>();
        readonly NativeSwitchFeedback feedback=new NativeSwitchFeedback();
        readonly int spanLimit;
        public SignalWorldBridge(SignalRuntime owner,Transform parent):this(owner,parent,512){}
        public SignalWorldBridge(SignalRuntime owner,Transform parent,int limit){this.owner=owner;this.parent=parent;spanLimit=limit;}
        public static string AnchorKey(MarkerRecord row)=>row.Anchor.Segment+":"+row.Anchor.Meters.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+row.FacingReverse;
        public static Location Location(MarkerRecord row) {
            var graph=Graph.Shared;var segment=graph?.GetSegment(row.Anchor.Segment);
            if(segment==null||!segment.Available||!segment.GroupEnabled||segment.turntable!=null||row.Anchor.Meters>segment.GetLength())throw new InvalidOperationException("Signalgleis nicht verfügbar.");
            var loc=new Location(segment,(float)row.Anchor.Meters,TrackSegment.End.A);return row.FacingReverse?loc.Flipped():loc;
        }
        static bool ResolveTurn(Graph graph,TrackNode node,TrackSegment from,IReadOnlyList<TrackSegment> choices,out TrackSegment next) {
            next=null;TrackSegment common,normal,reverse;
            if(!graph.DecodeSwitchAt(node,out common,out normal,out reverse))return false;
            var branch=node.isThrown?reverse:normal;next=from==common?branch:from==branch?common:null;return next!=null&&choices.Contains(next);
        }
        static List<TrackSection> Walk(Location from,float distance,out List<RouteSwitch> switches,out Location finish,out List<DirectedRouteSection> directed,bool partial=false) {
            var graph=Graph.Shared;var result=new List<TrackSection>();directed=new List<DirectedRouteSection>();switches=new List<RouteSwitch>();finish=from;
            var options=new RouteWalkOptions{TryAdvancePastSwitch=node=>false,ResolveTurn=(TrackNode n,TrackSegment s,IReadOnlyList<TrackSegment> choices,out TrackSegment next)=>ResolveTurn(graph,n,s,choices,out next)};
            var constructor=typeof(RouteWalker).GetConstructor(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new[]{typeof(IGraphView),typeof(Location),typeof(float),typeof(RouteWalkOptions)},null)??throw new MissingMethodException("RouteWalker constructor");
            var walker=(RouteWalker)constructor.Invoke(new object[]{graph.Current,from,distance,options});WalkSegment piece;float traversed=0;
            while(walker.TryNext(out piece)) {
                if(result.Count>=128)throw new InvalidOperationException("Fahrweg zu umfangreich (128 Abschnitte).");
                if(piece.Segment.turntable!=null)throw new InvalidOperationException("Drehscheiben sind noch ausgeschlossen.");
                var a=piece.EntryLocation.WithEnd(TrackSegment.End.A);var b=piece.ExitLocation.WithEnd(TrackSegment.End.A);
                if(Math.Abs(a.distance-b.distance)>.001){var section=new TrackSection{Segment=piece.Segment.id,From=Math.Min(a.distance,b.distance),To=Math.Max(a.distance,b.distance)};result.Add(section);directed.Add(new DirectedRouteSection{Section=section,Reverse=b.distance<a.distance});}
                traversed=piece.ExitDistance;
                finish=piece.ExitLocation;
                if(piece.EnteredVia!=null&&graph.IsSwitch(piece.EnteredVia)&&!switches.Any(s=>s.Node==piece.EnteredVia.id))switches.Add(new RouteSwitch{Node=piece.EnteredVia.id,Thrown=piece.EnteredVia.isThrown,ReleaseAt=piece.EntryDistance});
            }
            if(!partial&&traversed+.1f<distance)throw new InvalidOperationException("Fahrweg endet vor dem Ziel oder führt über eine falsch liegende/mehrdeutige Weiche.");
            return result;
        }
        static List<TrackSection> Approach(Location start) {
            var graph=Graph.Shared;var pending=new Queue<Tuple<Location,double>>();pending.Enqueue(Tuple.Create(start,300d));
            var result=new Dictionary<string,TrackSection>();var expanded=new Dictionary<string,double>();int work=0;
            while(pending.Count>0) {
                if(++work>1024)throw new InvalidOperationException("Annäherungsbereich zu verzweigt.");
                var item=pending.Dequeue();var at=item.Item1;double remaining=item.Item2;
                var segment=at.segment;if(segment==null||segment.turntable!=null||!segment.Available||!segment.GroupEnabled)throw new InvalidOperationException("Annäherungsbereich nicht vollständig prüfbar.");
                double available=at.DistanceUntilEnd(),take=Math.Min(remaining,available);
                var a=at.WithEnd(TrackSegment.End.A);var b=at.Moving((float)take).WithEnd(TrackSegment.End.A);
                if(take>.001) {
                    TrackSection section;double low=Math.Min(a.distance,b.distance),high=Math.Max(a.distance,b.distance);
                    if(!result.TryGetValue(segment.id,out section))result[segment.id]=new TrackSection{Segment=segment.id,From=low,To=high};else{section.From=Math.Min(section.From,low);section.To=Math.Max(section.To,high);}
                    if(result.Count>128)throw new InvalidOperationException("Annäherungsbereich überschreitet 128 Gleise.");
                }
                double rest=remaining-available;if(rest<=.01)continue;
                string direction=segment.id+":"+at.end;double known;if(expanded.TryGetValue(direction,out known)&&known>=rest)continue;expanded[direction]=rest;
                var node=segment.NodeForEnd(at.EndIsA?TrackSegment.End.B:TrackSegment.End.A);
                foreach(var next in graph.SegmentsConnectedTo(node))if(next!=segment)pending.Enqueue(Tuple.Create(new Location(next,0,next.EndForNode(node)),rest));
            }
            if(result.Count==0)throw new InvalidOperationException("Kein prüfbarer Annäherungsbereich.");return result.Values.ToList();
        }
        static string Topology(IEnumerable<TrackSection> sections) {
            var graph=Graph.Shared;
            var parts=new List<string>();
            foreach(string id in sections.Select(s=>s.Segment).Distinct().OrderBy(id=>id,StringComparer.Ordinal)) {
                var segment=graph.GetSegment(id);if(segment==null)return null;
                var a=segment.NodeForEnd(TrackSegment.End.A);var b=segment.NodeForEnd(TrackSegment.End.B);
                string neighbors=string.Join(",",graph.SegmentsConnectedTo(a).Concat(graph.SegmentsConnectedTo(b)).Select(s=>s.id+":"+s.Available+":"+s.GroupEnabled).Distinct().OrderBy(s=>s,StringComparer.Ordinal));
                parts.Add(id+"|"+a.id+"|"+b.id+"|"+segment.GetLength().ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+neighbors);
            }
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",parts)))).Replace("-","");
        }
        public RouteDefinition Design(MarkerRecord entry,MarkerRecord exit,string code,bool reduced,bool shunting=false) {
            if(entry==null||exit==null||entry.Id==exit.Id||!entry.IsSignal||entry.Kind==MarkerRecord.Distant)
                throw new InvalidOperationException("Haupt- oder Rangiersignal als Start und ein davon verschiedenes Ziel wählen.");
            shunting|=entry.Kind==MarkerRecord.Shunt;
            if(!AuxiliaryTarget.Accepts(exit,shunting))throw new InvalidOperationException("Ziel unterstützt diese Fahrart nicht.");
            if(shunting)reduced=false;
            if(!SignalCatalogue.Supports(entry,shunting,reduced))throw new InvalidOperationException("Hp 1, Hp 2 bzw. Sh 1 zuerst in dieser Mastrichtung einrichten.");
            var from=Location(entry);var to=Location(exit);float distance;
            if(!Graph.Shared.TryGetDistanceBetweenSameRoute(from,to,out distance)||distance<1||distance>10000)throw new InvalidOperationException("Kein eindeutiger Fahrweg 1 m–10 km in Signalrichtung. Weichen zuerst passend stellen.");
            List<RouteSwitch> switches;Location finish;List<DirectedRouteSection> path;Walk(from,distance,out switches,out finish,out path);
            if(!finish.WithEnd(TrackSegment.End.A).Equals(to.WithEnd(TrackSegment.End.A),.10f))throw new InvalidOperationException("Das Ziel wird in Signalrichtung nicht erreicht.");
            if(finish.end!=to.end)throw new InvalidOperationException("Zielsignal zeigt entgegen der Fahrtrichtung. Signalrichtung im Baumenü umkehren.");
            return BuildDesign(entry,exit,code,reduced,shunting,from,to,path,switches);
        }

        internal RouteDiscovery DiscoverRoutesTo(MarkerRecord target)=>new RouteDiscovery(FlankGraph(),owner.SignalObjects,target);
        public RouteDiscovery DiscoverRoutes(string auxiliary=null) => new RouteDiscovery(FlankGraph(),owner.SignalObjects,owner.AuxiliaryEndpoint(auxiliary));

        internal Func<RouteProposal,string,RouteDefinition> PrepareProposalDesign(ISet<string> destinations) {
            var graph=FlankGraph();
            var search=new RouteDiscovery(graph,owner.SignalObjects);
            return (proposal,code)=> {
                var extra=owner.AuxiliaryEndpoint(proposal.Exit);
                return DesignProposal(proposal,code,destinations,graph,
                    extra==null?search:new RouteDiscovery(graph,owner.SignalObjects,extra));
            };
        }

        RouteDefinition DesignProposal(RouteProposal proposal,string code,ISet<string> destinations,NetworkInput graph,RouteDiscovery search) {
            var entry=owner.Object(proposal.Entry);
            var exit=owner.RouteEndpoint(proposal.Exit);
            if(entry==null||exit==null)throw new InvalidOperationException("Start oder Ziel fehlt. Vorschläge neu suchen.");
            var current=search.Find(entry,proposal.Kind,destinations)
                .SingleOrDefault(proposal.SamePath);
            if(current==null)throw new InvalidOperationException("Fahrweg geändert oder nicht eindeutig. Vorschläge neu suchen.");
            return BuildDesign(entry,exit,code,proposal.Kind==RouteKind.Slow,proposal.Kind==RouteKind.Shunt,
                Location(entry),Location(exit),current.Path,current.Switches,graph);
        }

        RouteDefinition BuildDesign(MarkerRecord entry,MarkerRecord exit,string code,bool reduced,bool shunting,
            Location from,Location to,List<DirectedRouteSection> path,List<RouteSwitch> switches,NetworkInput graph=null) {
            bool auxiliary=AuxiliaryTarget.IsEndpoint(exit);
            if(auxiliary&&AuxiliaryTarget.HasBlockingSignal(path,owner.SignalObjects,entry.Id,shunting))
                throw new InvalidOperationException("Vor dem Hilfsziel liegt ein weiteres Signal in Fahrtrichtung. Dort eine eigene Teilfahrstraße beginnen.");
            var sections=path.Select(p=>p.Section).ToList();
            // Protect nearby branch ends against a vehicle fouling a route switch.
            foreach(var setting in switches) {
                var node=Graph.Shared.GetNode(setting.Node);
                setting.Protection=new List<TrackSection>();
                foreach(var segment in Graph.Shared.SegmentsConnectedTo(node)) {
                    double length=segment.GetLength(),guard=Math.Min(length,25);
                    var protection=segment.EndForNode(node)==TrackSegment.End.A?new TrackSection{Segment=segment.id,From=0,To=guard}:new TrackSection{Segment=segment.id,From=length-guard,To=length};sections.Add(protection);setting.Protection.Add(protection);
                }
            }
            var approach=Approach(from.Flipped());
            List<RouteSwitch> ignored;Location ignoredEnd;List<DirectedRouteSection> entryApproach,exitContinuation;
            try{Walk(from.Flipped(),30,out ignored,out ignoredEnd,out entryApproach,true);}catch{entryApproach=new List<DirectedRouteSection>();}
            try{Walk(to,2000,out ignored,out ignoredEnd,out exitContinuation,true);}catch{exitContinuation=new List<DirectedRouteSection>();}
            var route=new RouteDefinition{Code=code,Entry=entry.Id,Exit=exit.Id,AuxiliaryExit=auxiliary,EntryAnchor=AnchorKey(entry),ExitAnchor=AnchorKey(exit),Shunting=shunting,Reduced=reduced,Sections=sections,Approach=approach,Switches=switches,Path=path,EntryApproach=entryApproach,ExitContinuation=exitContinuation};
            route.ViaSignals=RouteSignals.Bind(route.Path,owner.SignalObjects,entry.Id,exit.Id);if(shunting)route.Flanks=new List<RouteFlank>();else FlankProtection.Plan(route,graph??FlankGraph(),owner.SignalObjects);RouteSignals.SplitProtection(route);route.ViaSignals=RouteSignals.Bind(route.Path,owner.SignalObjects,entry.Id,exit.Id);route.Topology=Topology(route.Sections.Concat(approach));route.ProgressTopology=Topology(exitContinuation.Select(p=>p.Section));route.Validate();return route;
        }
        public bool PathValid(RouteDefinition route) {
            try {
                var a=owner.Object(route.Entry);var b=owner.RouteEndpoint(route.Exit);
                if(a==null||!a.IsSignal||a.Kind==MarkerRecord.Distant||!AuxiliaryTarget.Accepts(b,route.Shunting)||
                    route.AuxiliaryExit!=AuxiliaryTarget.IsEndpoint(b)||AnchorKey(a)!=route.EntryAnchor||AnchorKey(b)!=route.ExitAnchor)return false;
                if(route.AuxiliaryExit&&route.Path!=null&&AuxiliaryTarget.HasBlockingSignal(route.Path,owner.SignalObjects,a.Id,route.Shunting))return false;
                if(!SignalCatalogue.Supports(a,route.Shunting,route.Reduced))return false;
                if(route.Path==null||!RouteSignals.Same(route.ViaSignals,RouteSignals.Bind(route.Path,owner.SignalObjects,route.Entry,route.Exit)))return false;
                if(!FlankBindingsValid(route))return false;
                Location(a);Location(b);
                if(string.IsNullOrEmpty(route.Topology)||Topology(route.Sections.Concat(route.Approach))!=route.Topology)return false;
                return route.Sections.Concat(route.Approach).All(s=>{var segment=Graph.Shared.GetSegment(s.Segment);return segment!=null&&segment.Available&&segment.GroupEnabled&&segment.turntable==null&&s.From>=0&&s.To<=segment.GetLength()+.001;});
            }catch{return false;}
        }
        public void BeginRead(){readings.Clear();flankReadings.Clear();}
        public TrackOccupancy Occupancy(TrackSection section) {
            TrackOccupancy previous;if(readings.TryGetValue(section.Key,out previous))return previous;
            TrackOccupancy value=TrackOccupancy.Unknown;
            try {
                var graph=Graph.Shared;var trains=TrainController.Shared;var segment=graph?.GetSegment(section.Segment);
                if(trains==null||segment==null||!segment.Available||!segment.GroupEnabled||section.To>segment.GetLength()+.001)return value;
                TrackSpan span;
                if(!spans.TryGetValue(section.Key,out span)) {
                    if(spans.Count>=spanLimit)return value;
                    var go=new GameObject("EnusDE occupancy "+spans.Count);go.SetActive(false);go.transform.SetParent(parent,false);
                    span=go.AddComponent<TrackSpan>();span.id="enusde.signaltechnik.occupancy."+Guid.NewGuid().ToString("N");
                    span.lower=new Location(segment,(float)section.From,TrackSegment.End.A);span.upper=new Location(segment,(float)section.To,TrackSegment.End.A).Flipped();spans[section.Key]=span;
                }
                if(span.IsValid&&span.GetSegments().Count==1&&span.GetSegments().Contains(segment))value=trains.AnyCarsOnSpan(span)?TrackOccupancy.Occupied:TrackOccupancy.Clear;
            }catch{value=TrackOccupancy.Unknown;}
            readings[section.Key]=value;return value;
        }
        public bool SwitchPosition(string id,out bool thrown){var node=Graph.Shared?.GetNode(id);thrown=node!=null&&node.isThrown;return node!=null&&Graph.Shared.IsSwitch(node);}
        public bool SwitchSettled(string id,bool thrown,out string detail)=>feedback.Read(id,thrown,out detail);
        public bool TrainStopped(string[] cars){if(cars==null||cars.Length==0||TrainController.Shared==null)return false;foreach(string id in cars){Model.Car car;if(!TrainController.Shared.TryGetCarForId(id,out car)||car==null||car.IsDerailed||!car.IsStopped(.05f))return false;}return true;}
        public bool CaptureTrain(RouteDefinition route,out string[] cars) {
            cars=null;if(route.Path==null||route.Path.Count==0||TrainController.Shared==null)return false;
            try {
                var candidates=new Dictionary<string,Model.Car>();var sections=new List<TrackSection>();
                if(route.EntryApproach!=null)sections.AddRange(route.EntryApproach.Select(p=>p.Section));
                var first=route.Path[0];sections.Add(new TrackSection{Segment=first.Section.Segment,From=first.Reverse?Math.Max(first.Section.From,first.Section.To-10):first.Section.From,To=first.Reverse?first.Section.To:Math.Min(first.Section.To,first.Section.From+10)});
                foreach(var section in sections) {
                    Occupancy(section);TrackSpan span;if(!spans.TryGetValue(section.Key,out span)||!span.IsValid)continue;
                    var found=new List<Model.Car>();TrainController.Shared.GetCarsOnSpan(span,found,8);
                    foreach(var car in found)if(car!=null)candidates[car.id]=car;
                }
                var ordered=candidates.Values.Select(car=>{double a=0,b=0;bool valid=Progress(route,car.LocationA,out a)&&Progress(route,car.LocationB,out b);return new{car,valid,front=valid?Math.Max(a,b):double.NegativeInfinity};}).Where(c=>c.valid&&c.front<=10&&c.front>=-30).OrderByDescending(c=>c.front).ToArray();
                if(ordered.Length==0)return false;
                var connected=new Dictionary<string,Model.Car>();var pending=new Queue<Model.Car>();pending.Enqueue(ordered[0].car);
                while(pending.Count>0) {
                    var car=pending.Dequeue();if(connected.ContainsKey(car.id))continue;if(connected.Count>=256||car.IsDerailed)return false;connected.Add(car.id,car);
                    foreach(var end in new[]{Model.Car.LogicalEnd.A,Model.Car.LogicalEnd.B}){var neighbor=car.CoupledTo(end);if(neighbor!=null)pending.Enqueue(neighbor);}
                }
                cars=connected.Keys.OrderBy(id=>id,StringComparer.Ordinal).ToArray();return cars.Length>0;
            }catch{return false;}
        }
        public bool TrainProgress(RouteDefinition route,string[] cars,out double head,out double tail) {
            head=double.NaN;tail=double.NaN;if(cars==null||cars.Length==0||cars.Length>256)return false;
            var known=new HashSet<string>(cars);double high=double.NegativeInfinity,low=double.PositiveInfinity;bool complete=true,identity=true;
            try {
                bool continuation=route.ExitContinuation!=null&&route.ProgressTopology!=null&&Topology(route.ExitContinuation.Select(p=>p.Section))==route.ProgressTopology;
                foreach(string id in cars) {
                    Model.Car car;if(!TrainController.Shared.TryGetCarForId(id,out car)||car==null||car.IsDerailed){complete=identity=false;continue;}
                    foreach(var end in new[]{Model.Car.LogicalEnd.A,Model.Car.LogicalEnd.B}){var neighbor=car.CoupledTo(end);if(neighbor!=null&&!known.Contains(neighbor.id))complete=identity=false;}
                    foreach(var at in new[]{car.LocationA,car.LocationB}){double progress;if(!Progress(route,at,out progress,continuation)){complete=false;continue;}high=Math.Max(high,progress);low=Math.Min(low,progress);}
                }
                if(identity&&!double.IsNegativeInfinity(high))head=high;if(complete&&!double.IsPositiveInfinity(low))tail=low;
                return complete&&!double.IsNaN(tail);
            }catch{return false;}
        }
        bool Progress(RouteDefinition route,Location at,out double progress,bool continuation=false) {
            progress=0;if(at.segment==null)return false;var canonical=at.WithEnd(TrackSegment.End.A);
            return RouteSignals.Position(route,canonical.segment.id,canonical.distance,out progress,continuation);
        }
        public bool SetSwitch(string id,bool thrown) {
            if(!StateManager.IsHost)return false;
            var trains=TrainController.Shared;if(trains==null)return false;
            string reason;if(!trains.TrySetSwitch(id,thrown,"EnusDE Signaltechnik",out reason))throw new InvalidOperationException("Spiel verweigert Weiche "+id+": "+reason);
            readings.Clear();
            bool value;return SwitchPosition(id,out value)&&value==thrown;
        }
        public void Dispose(){foreach(var span in spans.Values)if(span!=null)UnityEngine.Object.Destroy(span.gameObject);spans.Clear();readings.Clear();feedback.Dispose();}
    }
}
