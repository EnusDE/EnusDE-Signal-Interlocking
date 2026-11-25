using System;
using System.Linq;
using System.Collections.Generic;
using Track;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalWorldBridge {
        public void PruneSpans(HashSet<string> keep){foreach(string key in spans.Keys.Where(k=>!keep.Contains(k)).ToArray()){if(spans[key]!=null)UnityEngine.Object.Destroy(spans[key].gameObject);spans.Remove(key);}readings.Clear();}
        string[] Cars(TrackSpan span) {var found=new List<Model.Car>();TrainController.Shared.GetCarsOnSpan(span,found,512);return found.Count>=512?null:found.Where(c=>c!=null).Select(c=>c.id).Distinct().ToArray();}
        public TrackOccupancy SectionWithCars(TrackSection section,out string[] cars){var result=Occupancy(section);occupancyCars.TryGetValue(section.Key,out cars);return result;}
        static Location Boundary(DirectedRouteSection piece,bool end) {double from=piece.Reverse?piece.Section.To:piece.Section.From,to=piece.Reverse?piece.Section.From:piece.Section.To;double margin=Math.Min(.02,Math.Abs(to-from)*.05);double at=end?to+Math.Sign(from-to)*margin:from+Math.Sign(to-from)*margin;var loc=new Location(Graph.Shared.GetSegment(piece.Section.Segment),(float)at,TrackSegment.End.A);return piece.Reverse?loc.Flipped():loc;}
        public TrackOccupancy Block(TrackBlock block,out string[] cars) {
            cars=null;try {
                if(block.Point!=null)block=owner.ReadPointDefinition(block);
                var controller=TrainController.Shared;var graph=Graph.Shared;if(controller==null||graph==null)return TrackOccupancy.Unknown;
                if(block.IsCircuit) {
                    if(!owner.CircuitUsable(block))return TrackOccupancy.Unknown;
                    bool unknown=false,occupied=false;
                    var vehicles=new HashSet<string>();
                    TrackSegment previous=null;
                    bool previousReverse=false;
                    foreach(var piece in block.Path) {
                        var segment=graph.GetSegment(piece.Section.Segment);
                        if(segment==null||previous!=null&&(previousReverse?previous.a:previous.b)!=(piece.Reverse?segment.b:segment.a))
                            return TrackOccupancy.Unknown;
                        var state=SectionWithCars(piece.Section,out var ids);
                        occupied|=state==TrackOccupancy.Occupied;
                        unknown|=state==TrackOccupancy.Unknown||ids==null;
                        if(ids!=null)vehicles.UnionWith(ids);
                        previous=segment;
                        previousReverse=piece.Reverse;
                    }
                    cars=unknown?null:vehicles.ToArray();
                    return occupied?TrackOccupancy.Occupied:unknown?TrackOccupancy.Unknown:TrackOccupancy.Clear;
                }
                if(block.Point!=null){bool occupied=false,unknown=false;var ids=new HashSet<string>();foreach(var guard in block.Guards){string[] found;var state=SectionWithCars(guard,out found);if(state==TrackOccupancy.Clear)state=FlankOccupancy(guard);occupied|=state==TrackOccupancy.Occupied;unknown|=state==TrackOccupancy.Unknown||found==null;if(found!=null)ids.UnionWith(found);}cars=unknown?null:ids.ToArray();return occupied?TrackOccupancy.Occupied:unknown?TrackOccupancy.Unknown:TrackOccupancy.Clear;}
                if(block.Path.Count==0||block.Path.Any(p=>{var segment=graph.GetSegment(p.Section.Segment);return segment==null||!segment.Available||!segment.GroupEnabled||segment.turntable!=null||p.Section.To>segment.GetLength()+.001;}))return TrackOccupancy.Unknown;
                bool anyOccupied=false,anyUnknown=false;var vehicleIds=new HashSet<string>();
                foreach(var piece in block.Path){var state=SectionWithCars(piece.Section,out var found);anyOccupied|=state==TrackOccupancy.Occupied;anyUnknown|=state==TrackOccupancy.Unknown||found==null;if(found!=null)vehicleIds.UnionWith(found);}
                cars=anyUnknown?null:vehicleIds.ToArray();return anyUnknown?TrackOccupancy.Unknown:anyOccupied?TrackOccupancy.Occupied:TrackOccupancy.Clear;
            }catch{return TrackOccupancy.Unknown;}
        }
    }
    public sealed partial class SignalRuntime {
        readonly Dictionary<string,string[]> blockCars=new Dictionary<string,string[]>();
        readonly HashSet<string> routeBlocks=new HashSet<string>(),settingBlocks=new HashSet<string>(),routePoints=new HashSet<string>(),settingPoints=new HashSet<string>();
        readonly Dictionary<string,PanelRouteKind> routeBlockKinds=new Dictionary<string,PanelRouteKind>(),routePointKinds=new Dictionary<string,PanelRouteKind>();
        IEnumerable<string> TileBlocks(PanelTile tile) {
            if(tile==null||tile.Section!=null)yield break;
            if(tile.Kind==PanelTileKind.Switch&&!string.IsNullOrEmpty(tile.Reference)){if(desks?.Blocks.ContainsKey("p:"+tile.Reference)==true)yield return "p:"+tile.Reference;yield break;}
            if(!string.IsNullOrEmpty(tile.BlockId))yield return tile.BlockId;
            else if(tile.Kind==PanelTileKind.Switch&&desks?.Blocks.ContainsKey("p:"+tile.Reference)==true)yield return "p:"+tile.Reference;
            if(!string.IsNullOrEmpty(tile.SecondBlock))yield return tile.SecondBlock;
            if(tile.CircuitRails!=null)foreach(string id in tile.CircuitRails.Values)yield return id;
        }
        void ReadBlockPanel() {
            var visible=panel.Tiles.Where(t=>panelView==null||panelView.IsVisible(t)).ToArray();
            var remote=NumberMirrorBindings().Inputs(visible);
            var numberInputs=NumberDisplayInputs().Select(s=>s.Tile);
            var related=visible.Concat(visible.SelectMany(OccupancyNeighbours)).Concat(remote).Concat(numberInputs).Distinct().ToArray();
            var ids=related.SelectMany(TileBlocks).Concat(visible.Select(BetweenSignals).Where(id=>id!=null));
            var invalidPoints=new HashSet<string>();
            var blocks=ids.Distinct().Where(id=>desks.Blocks.ContainsKey(id)).Select(id=>{var b=desks.Blocks[id];try{return ReadPointDefinition(b);}catch{invalidPoints.Add(id);return b;}}).ToArray();
            var sections=related.Where(t=>t.Section!=null).Select(t=>t.Section).GroupBy(s=>s.Key).Select(g=>g.First()).ToArray();
            var keep=new HashSet<string>(sections.Select(s=>s.Key));foreach(var block in blocks)if(block.Point!=null||block.IsCircuit)keep.UnionWith(block.Sections.Select(s=>s.Key));else keep.Add("block:"+block.Id);panelWorld.PruneSpans(keep);panelWorld.BeginRead();blockCars.Clear();
            foreach(var section in sections){string[] cars;panelReadings[section.Key]=panelWorld.SectionWithCars(section,out cars);blockCars[section.Key]=cars;}
            foreach(var block in blocks) {
                panelReadings["block:"+block.Id]=panelWorld.Block(block,out var cars);
                blockCars["block:"+block.Id]=cars;
                if(block.Point!=null)foreach(var guard in block.Guards)
                    panelReadings[guard.Key]=invalidPoints.Contains(block.Id)?TrackOccupancy.Unknown:panelWorld.FlankOccupancy(guard);
            }
            ElectNumberDisplays();BuildRouteCues();
        }
        void BuildRouteCues() {
            routeBlocks.Clear();settingBlocks.Clear();routePoints.Clear();settingPoints.Clear();routeBlockKinds.Clear();routePointKinds.Clear();
            foreach(var run in VisibleRuns) {
                var kind=PanelRouteStyle.Kind(run);bool valid=kind!=PanelRouteKind.None,setting=IsSetting(run);if(!valid&&!setting)continue;
                foreach(var point in run.Route.Switches.Where(p=>p.FlankOf==null&&!run.ReleasedSwitches.Contains(p.Node))) {
                    (valid?routePoints:settingPoints).Add(point.Node);
                    if(valid){routePointKinds.TryGetValue(point.Node,out var previous);routePointKinds[point.Node]=PanelRouteStyle.Merge(previous,kind);}
                }
                var held=run.HeldSections.Where(s=>run.Route.Path==null||run.Route.Path.Any(p=>p.Section.Overlaps(s))).GroupBy(s=>s.Segment).ToDictionary(g=>g.Key,g=>g.ToArray());
                foreach(var block in desks.Blocks.Values)if(block.Sections.Any(s=>held.ContainsKey(s.Segment)&&held[s.Segment].Any(s.Overlaps))) {
                    (valid?routeBlocks:settingBlocks).Add(block.Id);
                    if(valid){routeBlockKinds.TryGetValue(block.Id,out var previous);routeBlockKinds[block.Id]=PanelRouteStyle.Merge(previous,kind);}
                }
            }
        }
        PanelRouteKind TileRouteKind(PanelTile tile) {
            if(tile==null)return PanelRouteKind.None;
            if(tile.Kind==PanelTileKind.Switch&&routePointKinds.TryGetValue(tile.Reference,out var point))return point;
            if(tile.Section!=null)return PanelRouteStyle.OnSection(tile.Section,VisibleRuns);
            var kind=PanelRouteKind.None;foreach(var id in TileBlocks(tile))if(routeBlockKinds.TryGetValue(id,out var next))kind=PanelRouteStyle.Merge(kind,next);return kind;
        }
        bool InRoute(PanelTile tile,bool setting=false) {
            if(tile.Kind==PanelTileKind.Switch&&(setting?settingPoints:routePoints).Contains(tile.Reference))return true;
            if(tile.Section!=null)return VisibleRuns.Any(r=>(setting?IsSetting(r):r.ChainReady&&(r.Stage==RouteStage.Set||r.Stage==RouteStage.Running))&&r.HeldSections.Any(s=>s.Overlaps(tile.Section)&&(r.Route.Path==null||r.Route.Path.Any(p=>p.Section.Overlaps(tile.Section)))));
            return TileBlocks(tile).Any(id=>(setting?settingBlocks:routeBlocks).Contains(id));
        }
        string NumberCaption(PanelTile tile) {
            if(!Game.State.StateManager.IsHost&&Entry.Shared?.LiveFresh!=true)return "?";
            if(NumberMirrors.IsMirror(tile))return NumberMirrorBindings().Caption(tile,NumberCaption);
            if(Entry.Shared!=null&&NumberBindings(Entry.Shared).ConflictingField(tile))return "?";
            string field=NumberFieldKey(tile);
            if(!string.IsNullOrEmpty(field)) {
                string assigned=TrainNumbers.DisplayFieldNumber(Entry.Shared?.NumberFields(),field);
                if(assigned!="")return assigned;
                if(tile.Reference!="")return editingWorkspace&&tile.Number!=""?(tile.Prefix??"")+tile.Number:"-----";
            }
            if(Time.realtimeSinceStartup-panelReadTime>=5||numberDisplayOverflow)return "?";
            if(tile.Section==null&&!TileBlocks(tile).Any())return editingWorkspace?(tile.Prefix??"")+(tile.Number??""):"-----";
            return numberDisplayOwners.Caption(NumberDisplayKey(tile),CarNumberCaption(tile));
        }
    }
}
