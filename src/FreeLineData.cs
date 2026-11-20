using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public enum LineTrackMode { SingleTrack,Directional,Gleiswechselbetrieb }
    public sealed class BlockSignalUnit {
        public string Boundary="",Forward="",Reverse="";
        public double Distance;
        public BlockSignalUnit Clone()=>(BlockSignalUnit)MemberwiseClone();
    }
    public sealed class LineTrackDefinition {
        public string Id=Guid.NewGuid().ToString("N"),Code="",Name="",Track="1",StationA="",StationB="";
        public LineTrackMode Mode;
        public bool Enabled,Automatic,RequireRequest=true;
        public int NormalDirection=1;
        public string ForwardStart="",ForwardEnd="",ReverseStart="",ReverseEnd="";
        public List<string> ADepartures=new List<string>(),BArrivals=new List<string>(),BDepartures=new List<string>(),AArrivals=new List<string>();
        public Dictionary<string,string> Anchors=new Dictionary<string,string>();
        public string Topology="";
        public string BlockOwner="";
        public List<string> BlockDisplays=new List<string>();
        public List<BlockSignalUnit> BlockUnits=new List<BlockSignalUnit>();
        public List<string> BlockSections=new List<string>();
        public List<DirectedRouteSection> Forward=new List<DirectedRouteSection>(),Reverse=new List<DirectedRouteSection>();
        [JsonIgnore] public List<DirectedRouteSection> Axis=>LineTrackGeometry.Axis(Forward,Reverse,Mode);
        public LineTrackDefinition Clone()=>new LineTrackDefinition{Id=Id,Code=Code,Name=Name,Track=Track,StationA=StationA,StationB=StationB,Mode=Mode,Enabled=Enabled,Automatic=Automatic,RequireRequest=RequireRequest,NormalDirection=NormalDirection,
            ForwardStart=ForwardStart,ForwardEnd=ForwardEnd,ReverseStart=ReverseStart,ReverseEnd=ReverseEnd,Topology=Topology,Anchors=Anchors?.ToDictionary(p=>p.Key,p=>p.Value),BlockOwner=BlockOwner,BlockDisplays=BlockDisplays?.ToList(),BlockUnits=BlockUnits?.Select(u=>u?.Clone()).ToList(),BlockSections=BlockSections?.ToList(),
            ADepartures=ADepartures?.ToList(),BArrivals=BArrivals?.ToList(),BDepartures=BDepartures?.ToList(),AArrivals=AArrivals?.ToList(),
            Forward=Forward?.Select(LineTrackGeometry.Copy).ToList(),Reverse=Reverse?.Select(LineTrackGeometry.Copy).ToList()};
        public void Validate() {
            if(BlockSections==null||BlockSections.Count>16||BlockSections.Distinct().Count()!=BlockSections.Count||BlockSections.Any(id=>string.IsNullOrEmpty(id)||id.Length>160))throw new InvalidDataException(LocalText.Select("Invalid block assembly sections.","Ungültige Abschnitte des Blockverbunds."));
            if(BlockOwner==null||BlockOwner!=""&&!Guid.TryParseExact(BlockOwner,"N",out _)||BlockDisplays==null||BlockDisplays.Count>32||BlockDisplays.Any(id=>!Guid.TryParseExact(id,"N",out _))||BlockDisplays.Distinct().Count()!=BlockDisplays.Count||BlockUnits==null||BlockUnits.Count>15||BlockUnits.Any(u=>u==null||!Guid.TryParseExact(u.Boundary,"N",out _)||!Guid.TryParseExact(u.Forward,"N",out _)||u.Reverse!=""&&!Guid.TryParseExact(u.Reverse,"N",out _)||double.IsNaN(u.Distance)||double.IsInfinity(u.Distance)||u.Distance<=0)||BlockUnits.Select(u=>u.Boundary).Distinct().Count()!=BlockUnits.Count||BlockUnits.SelectMany(u=>new[]{u.Forward,u.Reverse}).Where(id=>id!="").Distinct().Count()!=BlockUnits.Sum(u=>u.Reverse==""?1:2))throw new InvalidDataException(LocalText.Select("Invalid block assembly binding.","Ungültige Blockverbund-Zuordnung."));
            if(BlockOwner==""?(BlockSections.Count>0||BlockUnits.Count>0||BlockDisplays.Count>0):BlockSections.Count!=BlockUnits.Count+1)throw new InvalidDataException(LocalText.Select("Incomplete block assembly metadata.","Unvollständige Daten des Blockverbunds."));
            if(NormalDirection!=1&&NormalDirection!=-1)throw new InvalidDataException(LocalText.Select("Invalid normal permission direction.","Ungültige Regelrichtung der Erlaubnis."));
            if(!Guid.TryParseExact(Id,"N",out _)||!Text(Code,24)||!Text(Name,64)||!Text(Track,24)||StationA==StationB||!Guid.TryParseExact(StationA,"N",out _)||!Guid.TryParseExact(StationB,"N",out _)||!Enum.IsDefined(typeof(LineTrackMode),Mode))throw new InvalidDataException(LocalText.Select("Invalid free-line identification","Ungültige Streckenkennung oder Betriebsstellen."));
            if(Forward==null||Reverse==null||Forward.Count>512||Reverse.Count>512||Anchors==null||Anchors.Count>128||Anchors.Any(p=>p.Key==null||p.Key.Length>128||p.Value==null||p.Value.Length>512)||Topology==null||Topology.Length>64)throw new InvalidDataException(LocalText.Select("Invalid free-line path","Ungültiger Streckenverlauf."));
            foreach(var list in new[]{ADepartures,BArrivals,BDepartures,AArrivals})if(list==null||list.Count>32||list.Any(id=>!Guid.TryParseExact(id,"N",out _)))throw new InvalidDataException(LocalText.Select("Invalid line signal group.","Ungültige Streckensignalgruppe."));
            foreach(string id in new[]{ForwardStart,ForwardEnd,ReverseStart,ReverseEnd})if(id==null||id!=""&&!Guid.TryParseExact(id,"N",out _))throw new InvalidDataException(LocalText.Select("Invalid line signal","Ungültiges Streckensignal."));
            if(Mode==LineTrackMode.Directional&&Reverse.Count>0)throw new InvalidDataException(LocalText.Select("Directional track has an opposite path","Richtungsgleis enthält einen Gegenfahrweg."));
            LineTrackGeometry.Normalize(Forward);LineTrackGeometry.Normalize(Reverse);
            if(Enabled&&(Forward.Count==0||ForwardStart==""||ForwardEnd==""||ForwardStart==ForwardEnd||Topology.Length!=64||!Anchors.ContainsKey(ForwardStart)||!Anchors.ContainsKey(ForwardEnd)))throw new InvalidDataException(LocalText.Select("Detect the line before enabling","Strecke vor dem Aktivieren erkennen."));
            if(Enabled&&Mode!=LineTrackMode.Directional&&(Reverse.Count==0||ReverseStart==""||ReverseEnd==""||ReverseStart==ReverseEnd||!Anchors.ContainsKey(ReverseStart)||!Anchors.ContainsKey(ReverseEnd)))throw new InvalidDataException(LocalText.Select("Both directions are required","Beide Richtungen müssen eingerichtet sein."));
            if(Enabled&&LineSignalGroups.All(this).Any(id=>!Anchors.ContainsKey(id)))throw new InvalidDataException(LocalText.Select("Detect all configured line signals and targets first.","Alle eingerichteten Streckensignale und Ziele zuerst erkennen."));
            if(Forward.Count>0&&(Mode==LineTrackMode.Directional||Reverse.Count>0))_ = Axis;
        }
        static bool Text(string value,int max)=>!string.IsNullOrWhiteSpace(value)&&value.Length<=max&&!value.Any(c=>char.IsControl(c)||c=='<'||c=='>');
        public static void ValidateLibrary(DeskLibrary library) {
            if(library.FreeLines==null||library.FreeLines.Count>64||library.FreeLines.Any(l=>l==null)||library.FreeLines.Select(l=>l.Id).Distinct().Count()!=library.FreeLines.Count||library.FreeLines.Select(l=>l.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=library.FreeLines.Count)
                throw new InvalidDataException(LocalText.Select("Invalid line catalogue","Ungültiger Streckenkatalog."));
            foreach(var line in library.FreeLines) {
                line.Validate();if(!library.Desks.Any(d=>!d.IsMaster&&d.Id==line.StationA)||!library.Desks.Any(d=>!d.IsMaster&&d.Id==line.StationB))throw new InvalidDataException(LocalText.Select("Neighbouring interlocking missing","Nachbarstellwerk fehlt."));
                if(line.BlockOwner!=""&&!library.Desks.Any(d=>d.Id==line.BlockOwner&&!d.IsMaster)||line.BlockDisplays.Any(id=>id==line.BlockOwner||!library.Desks.Any(d=>d.Id==id)))throw new InvalidDataException(LocalText.Select("The block owner or a display interlocking is missing.","Das bedienende Stellwerk oder eine Anzeigetafel des Blockverbunds fehlt."));
                if(line.BlockSections.Any(id=>!library.Blocks.TryGetValue(id,out var block)||!block.IsCircuit))throw new InvalidDataException(LocalText.Select("A block assembly section is missing.","Ein Abschnitt des Blockverbunds fehlt."));
            }
            if(library.FreeLines.Sum(l=>l.Forward.Count+l.Reverse.Count)>16384)throw new InvalidDataException(LocalText.Select("Line catalogue too large","Streckenkatalog zu groß."));
            var active=library.FreeLines.Where(l=>l.Enabled).ToArray();
            for(int i=0;i<active.Length;i++)for(int j=i+1;j<active.Length;j++)if(active[i].Axis.Any(a=>active[j].Axis.Any(b=>a.Section.Overlaps(b.Section))))throw new InvalidDataException("Line tracks overlap: "+active[i].Code+" / "+active[j].Code+LocalText.Select(". One physical track needs one permission","Ein physisches Gleis benötigt eine gemeinsame Erlaubnis."));
            if(library.Desks.SelectMany(d=>d.Panel.Tiles).Any(t=>t.Kind==PanelTileKind.LinePermission&&t.Reference!=""&&!library.FreeLines.Any(l=>l.Id==t.Reference)))throw new InvalidDataException(LocalText.Select("Unlink line indicators before deleting","Streckenmelder vor dem Löschen lösen."));
        }
    }
    public static class LineTrackGeometry {
        public static DirectedRouteSection Copy(DirectedRouteSection p)=>p==null?null:new DirectedRouteSection{Reverse=p.Reverse,Section=p.Section?.Clone()};
        public static List<DirectedRouteSection> Normalize(IEnumerable<DirectedRouteSection> source) {
            var result=new List<DirectedRouteSection>();
            foreach(var piece in source) {
                if(piece?.Section==null)throw new InvalidDataException(LocalText.Select("Missing directed section","Gerichteter Abschnitt fehlt."));piece.Section.Validate();
                var last=result.LastOrDefault();
                if(last!=null&&last.Section.Segment==piece.Section.Segment&&last.Reverse==piece.Reverse&&Math.Abs((last.Reverse?last.Section.From:last.Section.To)-(piece.Reverse?piece.Section.To:piece.Section.From))<.005) {
                    last.Section.From=Math.Min(last.Section.From,piece.Section.From);last.Section.To=Math.Max(last.Section.To,piece.Section.To);
                } else result.Add(Copy(piece));
            }
            if(result.Select(p=>p.Section.Segment).Distinct().Count()!=result.Count)throw new InvalidDataException(LocalText.Select("Loop / repeated segment in line path","Schleife im Streckenverlauf."));
            return result;
        }
        public static List<DirectedRouteSection> Axis(IEnumerable<DirectedRouteSection> forward,IEnumerable<DirectedRouteSection> reverse,LineTrackMode mode) {
            var a=Normalize(forward);if(mode==LineTrackMode.Directional)return a;
            var b=Normalize(reverse);var common=new List<DirectedRouteSection>();
            foreach(var p in a)foreach(var q in b.Where(q=>q.Section.Segment==p.Section.Segment&&q.Section.Overlaps(p.Section))) {
                if(p.Reverse==q.Reverse)throw new InvalidDataException(LocalText.Select("Both paths face the same direction","Beide Fahrwege zeigen in dieselbe Richtung."));
                common.Add(new DirectedRouteSection{Reverse=p.Reverse,Section=new TrackSection{Segment=p.Section.Segment,From=Math.Max(p.Section.From,q.Section.From),To=Math.Min(p.Section.To,q.Section.To)}});
            }
            if(common.Count==0)throw new InvalidDataException(LocalText.Select("The opposite route uses another track","Gegenfahrweg benutzt ein anderes Gleis."));
            Continuous(a,common,false);Continuous(b,common,true);return common;
        }
        static void Continuous(List<DirectedRouteSection> path,List<DirectedRouteSection> common,bool opposite) {
            double before=0,last=-1;var order=new List<string>();
            foreach(var piece in path) {
                var cut=common.SingleOrDefault(p=>p.Section.Segment==piece.Section.Segment);
                if(cut!=null) {
                    double start=before+(piece.Reverse?piece.Section.To-cut.Section.To:cut.Section.From-piece.Section.From);
                    if(last>=0&&start>last+.05)throw new InvalidDataException(LocalText.Select("Opposite paths diverge inside the line","Gegenfahrwege weichen innerhalb der Strecke voneinander ab."));
                    last=start+cut.Section.To-cut.Section.From;order.Add(cut.Section.Key);
                }
                before+=piece.Section.To-piece.Section.From;
            }
            var expected=common.Select(p=>p.Section.Key).ToArray();
            if(!order.SequenceEqual(opposite?expected.Reverse():expected))throw new InvalidDataException(LocalText.Select("Opposite path order differs","Gegenfahrweg hat eine andere Abschnittsfolge."));
        }
    }
}
