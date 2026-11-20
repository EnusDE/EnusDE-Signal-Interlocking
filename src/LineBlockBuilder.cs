using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class LineBlockBuild {
        public MarkerDocument Objects;
        public DeskLibrary Library;
        public LineTrackDefinition Line;
        public LineIndicatorTarget Indicator;
        public bool WorldConstruction;
        public int NewSignals,ReusedSignals,NewContacts,RemovedSignals;
    }
    public static class LineBlockBuilder {
        static bool Same(MarkerAnchor a,MarkerAnchor b)=>a.Segment==b.Segment&&Math.Abs(a.Meters-b.Meters)<.005;
        public static LineBlockBuild Build(MarkerDocument objects,DeskLibrary library,LineTrackDefinition line,BlockLayoutPlan plan,string ownerId,bool contacts=false,MarkerOptions placement=null,double stagger=10,IEnumerable<string> displays=null) {
            LineSetupWorkflow.RequireUnique(library,line);plan.Validate();if(double.IsNaN(stagger)||double.IsInfinity(stagger)||stagger<.5||stagger>25)throw new InvalidOperationException(LocalText.Select("Signal stagger must be 0.5–25 m per side.","Signalversatz muss je Seite 0,5–25 m betragen."));
            var result=new LineBlockBuild{Objects=Newtonsoft.Json.JsonConvert.DeserializeObject<MarkerDocument>(Newtonsoft.Json.JsonConvert.SerializeObject(objects)),Library=library.Clone(),Line=line.Clone(),WorldConstruction=true};
            var settings=(placement??new MarkerOptions{UseGameSpacing=false}).Normalize();
            foreach(var unit in line.BlockUnits)if(!objects.Markers.Any(m=>m.Id==unit.Boundary&&plan.Boundaries.Any(at=>Same(m.Anchor,plan.At(at)))))
                throw new InvalidOperationException(LocalText.Select("Retain the existing common detector boundaries in the block proposal.","Vorhandene gemeinsame Meldergrenzen im Blockvorschlag beibehalten."));
            var definition=result.Line;definition.BlockOwner=ownerId;definition.BlockDisplays=(displays??line.BlockDisplays).Where(id=>id!=ownerId).Distinct().ToList();definition.BlockUnits.Clear();
            if(!result.Library.Desks.Any(d=>d.Id==ownerId&&!d.IsMaster))throw new InvalidOperationException(LocalText.Select("Choose a local interlocking for the block signals.","Ein örtliches Stellwerk für die Blocksignale wählen."));
            var known=new HashSet<string>(line.BlockUnits.SelectMany(u=>new[]{u.Forward,u.Reverse}));
            foreach(var signal in objects.Markers.Where(m=>m.Kind==MarkerRecord.Main&&!known.Contains(m.Id))) {
                double? at=plan.Position(signal.Anchor);
                if(at>.01&&at<plan.Length-.01&&!plan.Boundaries.Any(b=>Math.Abs(b-at.Value)<.005))throw new InvalidOperationException(LocalText.Select("Keep the existing signal boundary in the plan: ","Vorhandene Signalgrenze im Plan beibehalten: ")+signal.Name);
            }
            var repository=new MarkerRepository("unused-block-preview",objects.WorldKey,objects.Map,()=>result.Objects,doc=>result.Objects=doc);
            MarkerRecord Object(string id)=>result.Objects.Markers.Single(m=>m.Id==id);
            MarkerAnchor Place(double at,bool reverse,bool signal=true) {
                var a=plan.At(at);a.Lateral=signal?PlacementDirection.Side(settings.Lateral,settings.DefaultRightSide,reverse):PlacementDirection.Side(Math.Max(1.5,settings.Lateral),settings.DefaultRightSide,reverse);
                a.Height=settings.Height;a.Scale=settings.Scale;return a;
            }
            string Unique(string stem) {string value=stem.Substring(0,Math.Min(40,stem.Length));int i=1;while(SignalCatalogue.Expand(result.Objects.Markers,true).Any(s=>s.Name==value)||result.Objects.Markers.Any(s=>s.Name==value))value=stem.Substring(0,Math.Min(36,stem.Length))+"-"+i++;return value;}
            void SetRoot(string id,double distance,bool reverse) {
                var row=Object(id);row.Anchor=Place(distance,reverse);row.FacingReverse=reverse;row.UseHostPlacement=false;row.CircuitVariant=0;row.CircuitNumber=0;row.VisualRevision++;
            }
            string NewSignal(double distance,bool reverse) {
                var row=repository.Put(Place(distance,reverse),kind:MarkerRecord.Main,reverse:reverse,newSignalGfm:false,useHost:false);result.NewSignals++;return row.Id;
            }
            string End(double distance) {
                var at=plan.At(distance);var candidates=result.Objects.Markers.Where(m=>CircuitIdentity.Enabled(m)&&Same(m.Anchor,at)).ToArray();
                if(candidates.Length!=1)throw new InvalidOperationException(LocalText.Select("The line endpoint needs one exact active GFM boundary: ","Der Streckenendpunkt benötigt eine eindeutige aktive GFM-Grenze: ")+at.Segment+" · "+Math.Round(at.Meters,2)+" m");
                return candidates[0].Id;
            }
            string start=End(0),end=End(plan.Length);var cuts=new[]{0d}.Concat(plan.Boundaries).Append(plan.Length).ToArray();var aliases=new Dictionary<string,string>();var boundaries=new List<string>{start};
            for(int i=0;i<plan.Boundaries.Count;i++) {
                double distance=plan.Boundaries[i],offset=Math.Min(stagger,Math.Min(distance-cuts[i],cuts[i+2]-distance)/4);bool reverse=plan.ReverseAt(distance);
                var previous=line.BlockUnits.FirstOrDefault(u=>objects.Markers.Any(m=>m.Id==u.Boundary&&Same(m.Anchor,plan.At(distance))));
                string boundary,forward,opposite="";
                if(previous!=null) {
                    boundary=previous.Boundary;forward=previous.Forward;opposite=previous.Reverse;
                    if(line.Mode!=LineTrackMode.Directional&&opposite=="")opposite=NewSignal(distance+offset,!plan.ReverseAt(distance+offset));
                    SetRoot(forward,distance-offset,plan.ReverseAt(distance-offset));if(opposite!="")SetRoot(opposite,distance+offset,!plan.ReverseAt(distance+offset));result.ReusedSignals++;
                } else {
                    var candidates=objects.Markers.Where(m=>m.Kind==MarkerRecord.Main&&Same(m.Anchor,plan.At(distance))).ToArray();
                    if(candidates.Length>1)throw new InvalidOperationException(LocalText.Select("Several signals occupy this proposed boundary.","Mehrere Signale liegen an dieser vorgeschlagenen Grenze."));
                    var legacy=candidates.SingleOrDefault();
                    var box=repository.Put(Place(distance,reverse,false),kind:MarkerRecord.CircuitB,useHost:false);boundary=box.Id;
                    if(legacy==null) {
                        forward=NewSignal(distance-offset,plan.ReverseAt(distance-offset));
                        if(line.Mode!=LineTrackMode.Directional)opposite=NewSignal(distance+offset,!plan.ReverseAt(distance+offset));
                    } else {
                        var mast=MastConfiguration.Effective(legacy);bool same=legacy.FacingReverse==reverse;
                        forward=same?mast.Front.Id:mast.Back.Id;string other=same?mast.Back.Id:mast.Front.Id;
                        bool needBack=line.Mode!=LineTrackMode.Directional||!same;
                        if(needBack) {
                            var rear=legacy.Clone();rear.Id=mast.Back.Id;rear.Name=mast.Back.Code;rear.DisplayName=mast.Back.DisplayName;
                            rear.Mast=new MastConfiguration{Front=mast.Back.Clone(),Back=new SignalFace{Id=Guid.NewGuid().ToString("N"),Code=Unique(mast.Back.Code+"-off"),Enabled=false}};rear.Mast.Front.Enabled=true;
                            rear.CircuitNumber=rear.CircuitVariant=0;result.Objects.Markers.Add(rear);result.NewSignals++;
                        }
                        var front=Object(legacy.Id);front.CircuitVariant=front.CircuitNumber=0;front.Mast=mast.Clone();front.Mast.Back=new SignalFace{Id=Guid.NewGuid().ToString("N"),Code=Unique(mast.Back.Code+"-old"),Enabled=false};front.Mast.Front.Enabled=true;
                        if(line.Mode!=LineTrackMode.Directional)opposite=other;
                        SetRoot(forward,distance-offset,plan.ReverseAt(distance-offset));if(opposite!="")SetRoot(opposite,distance+offset,!plan.ReverseAt(distance+offset));
                        aliases[legacy.Id]=boundary;if(legacy.CircuitNumber>0)Object(boundary).CircuitNumber=legacy.CircuitNumber;
                        result.ReusedSignals++;
                    }
                }
                var unit=new BlockSignalUnit{Boundary=boundary,Forward=forward,Reverse=opposite,Distance=distance};definition.BlockUnits.Add(unit);boundaries.Add(boundary);
                foreach(string id in new[]{forward,opposite}.Where(id=>id!="")) {
                    var duty=SignalRolePolicy.Current(result.Library,id);
                    if(duty!=SignalDuty.Ordinary&&duty!=SignalDuty.Block)throw new InvalidOperationException(LocalText.Select("A station signal cannot be reclassified by the block assistant: ","Ein Bahnhofssignal kann nicht durch den Blockassistenten umgewidmet werden: ")+Object(id).Name);
                    SignalRolePolicy.Put(result.Library,id,SignalDuty.Block);
                }
            }
            boundaries.Add(end);
            foreach(var block in result.Library.Blocks.Values) {if(block.BoundaryA!=null&&aliases.TryGetValue(block.BoundaryA,out var a))block.BoundaryA=a;if(block.BoundaryB!=null&&aliases.TryGetValue(block.BoundaryB,out var b))block.BoundaryB=b;}
            foreach(var tile in result.Library.Desks.SelectMany(d=>d.Panel.Tiles))if(tile.CircuitBoundary!=null&&aliases.TryGetValue(tile.CircuitBoundary,out var boundary))tile.CircuitBoundary=boundary;
            var blocks=new List<TrackBlock>();
            for(int i=0;i<cuts.Length-1;i++) {
                var matches=result.Library.Blocks.Values.Where(b=>b.IsCircuit&&TrackCircuit.Pair(b.BoundaryA,b.BoundaryB)==TrackCircuit.Pair(boundaries[i],boundaries[i+1])).ToArray();
                if(matches.Length>1)throw new InvalidOperationException(LocalText.Select("Several existing sections use these boundaries. Inspect GFM assignments first.","Mehrere vorhandene Abschnitte benutzen diese Grenzen. Zuerst GFM-Zuordnungen prüfen."));
                var old=matches.SingleOrDefault();var block=new TrackBlock{Id=old?.Id??"c:"+Guid.NewGuid().ToString("N"),Name=old?.Name??line.Code+" · "+(i+1),BoundaryA=boundaries[i],BoundaryB=boundaries[i+1],Path=plan.Slice(cuts[i],cuts[i+1])};block.Validate();blocks.Add(block);result.Library.Blocks[block.Id]=block;
            }
            if(line.BlockSections.Any(id=>!blocks.Any(b=>b.Id==id)))throw new InvalidOperationException(LocalText.Select("This update retains the existing block division. Reuse its block count and detector positions; signal stagger and diagram placement remain adjustable.","Dieses Update behält die vorhandene Blockaufteilung bei. Blockanzahl und Melderpositionen wiederverwenden; Signalversatz und Gleisbildplatzierung bleiben einstellbar."));
            if(contacts)for(int i=0;i<definition.BlockUnits.Count;i++) {
                var unit=definition.BlockUnits[i];
                void Add(string signal,string target,bool reverseMovement) {
                    if(target=="")throw new InvalidOperationException(LocalText.Select("Complete both line destinations before adding automatic contacts.","Vor automatischen Kontakten beide Streckenziele vervollständigen."));
                    var rule=result.Library.Automation.Signals[signal];
                    var existing=result.Library.Automation.Contacts.FirstOrDefault(c=>c.Signal==signal);
                    double signalAt=plan.Position(Object(signal).Anchor).Value;
                    double distance=signalAt+(reverseMovement?1:-1)*Math.Min(150,Math.Min(signalAt-cuts[i],cuts[i+2]-signalAt)/2);
                    var anchor=plan.At(distance);anchor.Lateral=anchor.Height=0;
                    if(existing!=null){if(existing.AutomaticSignal){existing.Anchor=anchor;existing.Reverse=plan.ReverseAt(distance)^reverseMovement;}return;}
                    if(rule.Primary!=""||rule.Enabled||rule.OutOfService)return;
                    rule.Primary=target;rule.Enabled=true;
                    result.Library.Automation.Contacts.Add(new AutomationContact{Anchor=anchor,Signal=signal,AutomaticSignal=true,Enabled=true,AxleTest=false,Reverse=plan.ReverseAt(distance)^reverseMovement,Name=line.Code+" · "+(i+1)+(reverseMovement?" ←":" →")});result.NewContacts++;
                }
                Add(unit.Forward,i+1<definition.BlockUnits.Count?definition.BlockUnits[i+1].Forward:line.ForwardEnd,false);
                if(unit.Reverse!="")Add(unit.Reverse,i>0?definition.BlockUnits[i-1].Reverse:line.ReverseEnd,true);
            }
            definition.BlockSections=blocks.Select(b=>b.Id).ToList();var index=result.Library.FreeLines.FindIndex(l=>l.Id==line.Id);if(index<0)result.Library.FreeLines.Add(definition.Clone());else result.Library.FreeLines[index]=definition.Clone();
            if(definition.Enabled){result.Library.Automation.BlockEnabled=true;var mains=SignalCatalogue.Expand(result.Objects.Markers,true).Where(m=>m.Kind==MarkerRecord.Main).Select(m=>m.Id).ToHashSet();foreach(var group in LineSignalGroups.Used(definition))foreach(string id in LineSignalGroups.Get(definition,group))if(mains.Contains(id))SignalRolePolicy.Put(result.Library,id,LineSignalGroups.Departure(group)?SignalDuty.Exit:SignalDuty.Entry);}
            BlockDiagram.Rebuild(result.Library,definition,blocks);CircuitIdentity.Fill(result.Objects);MarkerRepository.ValidateDocument(result.Objects,objects.WorldKey,objects.Map);result.Library.Validate(library.WorldKey);return result;
        }
    }
}
