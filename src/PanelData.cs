using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public enum PanelTileKind { Straight,Diagonal,Curve,Switch,Cross,Signal,Label,TrainNumber,AuxiliaryTarget,LinePermission }
    public sealed class PanelArea {
        public string Id=Guid.NewGuid().ToString("N"),Code="",Name="",Kind="Stellwerk",Desk="";
        public int X,Y,Width=1,Height=1;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public string BlockLine;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public string NumberLine;
        public bool Contains(int x,int y)=>x>=X&&x<X+Width&&y>=Y&&y<Y+Height;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public BlockViewOptions BlockView;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public List<PanelTile> BlockTemplate;
        public PanelArea Clone(){var copy=(PanelArea)MemberwiseClone();copy.BlockView=BlockView?.Clone();copy.BlockTemplate=BlockTemplate?.Select(t=>t.Clone()).ToList();return copy;}
    }
    public sealed class PanelTile {
        public int X,Y,Rotation;
        public PanelTileKind Kind;
        public bool Mirror;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool InlinePermission;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string PermissionAuxiliary;
        public int Ports,Stem,Normal;
        public string Label="",Reference="";
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string AlternateSignal;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool ProjectedMastFace;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool ReadOnlySignal;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string NumberId;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string NumberMirror;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)] public bool InlineNumberMirror;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public AuxiliaryTarget Target;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string LinkedAuxiliary;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string CircuitBoundary;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public Dictionary<int,string> CircuitRails;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public int? MainSignalDirection,ShuntSignalDirection;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool SwapSignalHalves;
        public string BlockId="",SecondBlock="",AutoSegment="",Number="",Prefix="";
        public double AutoMeters;
        public TrackSection Section;
        public PanelTile Clone(){var copy=(PanelTile)MemberwiseClone();copy.Section=Section?.Clone();copy.Target=Target?.Clone();copy.CircuitRails=CircuitRails==null?null:new Dictionary<int,string>(CircuitRails);return copy;}
        static bool SameCircuitRails(Dictionary<int,string> a,Dictionary<int,string> b)=>a==null?b==null:
            b!=null&&a.Count==b.Count&&a.All(p=>b.TryGetValue(p.Key,out var id)&&id==p.Value);
        public bool SameContent(PanelTile b)=>b!=null&&X==b.X&&Y==b.Y&&Rotation==b.Rotation&&Kind==b.Kind&&Mirror==b.Mirror&&Ports==b.Ports&&Stem==b.Stem&&Normal==b.Normal&&Label==b.Label&&Reference==b.Reference&&AlternateSignal==b.AlternateSignal&&NumberId==b.NumberId&&NumberMirror==b.NumberMirror&&InlineNumberMirror==b.InlineNumberMirror&&
            InlinePermission==b.InlinePermission&&PermissionAuxiliary==b.PermissionAuxiliary&&MainSignalDirection==b.MainSignalDirection&&ShuntSignalDirection==b.ShuntSignalDirection&&ReadOnlySignal==b.ReadOnlySignal&&ProjectedMastFace==b.ProjectedMastFace&&SwapSignalHalves==b.SwapSignalHalves&&LinkedAuxiliary==b.LinkedAuxiliary&&CircuitBoundary==b.CircuitBoundary&&SameCircuitRails(CircuitRails,b.CircuitRails)&&
            (Target==null?b.Target==null:Target.SameContent(b.Target))&&BlockId==b.BlockId&&SecondBlock==b.SecondBlock&&AutoSegment==b.AutoSegment&&Number==b.Number&&Prefix==b.Prefix&&BitConverter.DoubleToInt64Bits(AutoMeters)==BitConverter.DoubleToInt64Bits(b.AutoMeters)&&(Section==null?b.Section==null:b.Section!=null&&Section.Segment==b.Section.Segment&&BitConverter.DoubleToInt64Bits(Section.From)==BitConverter.DoubleToInt64Bits(b.Section.From)&&BitConverter.DoubleToInt64Bits(Section.To)==BitConverter.DoubleToInt64Bits(b.Section.To));
    }
    public sealed class PanelDocument {
        public const int MaxColumns=2048,MaxRows=512;
        public int Schema=1,Columns=24,Rows=12;
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)] public int OriginX,OriginY;
        public string WorldKey;
        public List<PanelTile> Tiles=new List<PanelTile>();
        public List<PanelArea> Areas=new List<PanelArea>();
        public PanelDocument Clone(){var copy=(PanelDocument)MemberwiseClone();copy.Tiles=Tiles?.Select(t=>t?.Clone()).ToList();copy.Areas=Areas?.Select(a=>a?.Clone()).ToList();return copy;}
        public PanelTile At(int x,int y)=>Tiles.FirstOrDefault(t=>t.X==x&&t.Y==y);
        public void Put(PanelTile tile){var next=Tiles.Where(t=>t.X!=tile.X||t.Y!=tile.Y).ToList();next.Add(tile.Clone());var old=Tiles;Tiles=next;try{Validate(WorldKey);}catch{Tiles=old;throw;}}
        public void Validate(string key) {
            if(OriginX<0||OriginY<0||Schema!=1||WorldKey!=key||string.IsNullOrWhiteSpace(key)||Columns<8||Columns>MaxColumns||Rows<6||Rows>MaxRows||Tiles==null||Tiles.Count>65536)throw new InvalidDataException(LocalText.Select("Invalid diagram or world binding (maximum 2048 × 512 / 65536 fields).","Ungültiges Gleisbild oder falsche Spielstandzuordnung (maximal 2048 × 512 / 65536 Bausteine)."));
            var cells=new HashSet<string>();int sections=0;
            foreach(var tile in Tiles) {
                if(tile==null||tile.X<0||tile.Y<0||tile.X>=Columns||tile.Y>=Rows||tile.Rotation<0||tile.Rotation>3||!Enum.IsDefined(typeof(PanelTileKind),tile.Kind)||tile.Label==null||tile.Label.Length>48||tile.Reference==null||tile.Reference.Length>128||tile.Label.Any(char.IsControl)||tile.Reference.Any(char.IsControl)||!cells.Add(tile.X+":"+tile.Y))throw new InvalidDataException("Ungültiges oder doppeltes Gleisbildfeld.");
                if(tile.AlternateSignal!=null&&(tile.AlternateSignal.Length==0||tile.AlternateSignal.Length>128||
                    tile.AlternateSignal.Any(char.IsControl)||(tile.Kind!=PanelTileKind.Signal&&tile.Kind!=PanelTileKind.TrainNumber)))
                    throw new InvalidDataException("Ungültige zweite Signalzuordnung.");
                if(tile.MainSignalDirection<0||tile.MainSignalDirection>3||tile.ShuntSignalDirection<0||tile.ShuntSignalDirection>3||
                    tile.Kind!=PanelTileKind.Signal&&(tile.MainSignalDirection.HasValue||tile.ShuntSignalDirection.HasValue||tile.SwapSignalHalves))
                    throw new InvalidDataException("Ungültige Ausrichtung der Signaltaste.");
                if(tile.LinkedAuxiliary!=null&&(tile.Kind!=PanelTileKind.Signal||!Guid.TryParseExact(tile.LinkedAuxiliary,"N",out _)))
                    throw new InvalidDataException("Ungültige Hilfszielzuordnung am Signalfeld.");
                if(tile.Target!=null){
                    if(tile.Kind!=PanelTileKind.AuxiliaryTarget&&(tile.Kind!=PanelTileKind.Signal||tile.AlternateSignal!=null||tile.LinkedAuxiliary!=null||tile.Target.Train||!tile.Target.Shunt))
                        throw new InvalidDataException("Hilfsziel nur als eigenes Zielfeld oder Rangierziel neben einem ungeteilten Rangiersignal zulässig.");
                    tile.Target.Validate();
                }
                if(tile.CircuitBoundary!=null&&!TrackCircuit.ValidId(tile.CircuitBoundary))
                    throw new InvalidDataException("Ungültige Freimeldegrenze am Gleisfeld.");
                if(tile.CircuitRails!=null&&(tile.Kind!=PanelTileKind.Signal&&tile.Kind!=PanelTileKind.Switch&&!CircuitDiagramAssignment.RailField(tile)||tile.CircuitRails.Count>(tile.Kind==PanelTileKind.Cross?4:tile.Kind==PanelTileKind.Switch?3:2)||
                    tile.CircuitRails.Any(p=>p.Key<0||p.Key>7||string.IsNullOrEmpty(p.Value)||p.Value.Length>160)))
                    throw new InvalidDataException("Ungültige Abschnittszuordnung einer Gleishälfte.");
                NumberMirrors.Validate(tile);
                if((tile.InlinePermission||tile.PermissionAuxiliary!=null)&&tile.Kind!=PanelTileKind.LinePermission||tile.PermissionAuxiliary!=null&&!Guid.TryParseExact(tile.PermissionAuxiliary,"N",out _))throw new InvalidDataException(LocalText.Select("Invalid permission module binding.","Ungültige Erlaubnismodul-Zuordnung."));
                if(tile.Kind==PanelTileKind.LinePermission&&(!tile.InlinePermission&&(tile.Ports!=0||tile.Section!=null||tile.BlockId!=""||tile.SecondBlock!=""||tile.CircuitRails!=null||tile.CircuitBoundary!=null||tile.AutoSegment!="")||tile.AlternateSignal!=null||tile.LinkedAuxiliary!=null||tile.Target!=null))throw new InvalidDataException(LocalText.Select("Only inline permission modules may preserve diagram track bindings.","Nur Erlaubnismodule im Gleis dürfen Gleisbildzuordnungen behalten."));
                PanelConnections.Validate(tile);
                if(tile.BlockId==null||tile.SecondBlock==null||tile.AutoSegment==null||tile.BlockId.Length>160||tile.SecondBlock.Length>160||tile.AutoSegment.Length>128||double.IsNaN(tile.AutoMeters)||double.IsInfinity(tile.AutoMeters)||tile.AutoMeters<0)throw new InvalidDataException("Abschnittsbindung ungültig.");
                TrainNumbers.Format(tile.Prefix,tile.Number);
                if(tile.Section!=null){tile.Section.Validate();sections++;if(tile.Kind==PanelTileKind.Label||tile.Kind==PanelTileKind.Signal||tile.Kind==PanelTileKind.Switch)throw new InvalidDataException("Abschnitt nur an Gleisfeldern binden.");}
            }
            if(Tiles.Where(t=>t.Target!=null).GroupBy(t=>t.Target.Id).Any(g=>g.Count()>1))
                throw new InvalidDataException("Ein Hilfsziel darf je Tafel nur einmal vorkommen.");
            if(sections>512)throw new InvalidDataException("Höchstens 512 Felder mit Belegungsabschnitten je Tafel.");
            if(Areas==null||Areas.Count>128||Areas.Select(a=>a?.Id).Distinct().Count()!=Areas.Count||Areas.Select(a=>a?.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Areas.Count)throw new InvalidDataException("Bereichskennungen ungültig oder doppelt.");
            foreach(var area in Areas){Guid id;if(area==null||!Guid.TryParseExact(area.Id,"N",out id)||string.IsNullOrWhiteSpace(area.Code)||area.Code.Length>24||string.IsNullOrWhiteSpace(area.Name)||area.Name.Length>64||area.Code.Any(char.IsControl)||area.Name.Any(char.IsControl)||!new[]{"Stellwerk","Strecke","Rangierbereich"}.Contains(area.Kind)||area.X<0||area.Y<0||area.Width<1||area.Height<1||area.X+area.Width>Columns||area.Y+area.Height>Rows)throw new InvalidDataException("Markierter Bereich ungültig.");}
            foreach(var area in Areas)if(area.BlockTemplate!=null) {
                if(area.BlockLine==null||area.BlockTemplate.Count>256)throw new InvalidDataException(LocalText.Select("Invalid saved block view template.","Ungültige gespeicherte Blockansichtsvorlage."));
                new PanelDocument{WorldKey=key,Columns=128,Rows=6,Tiles=area.BlockTemplate}.Validate(key);
            }
            foreach(var area in Areas)if(area.BlockView!=null){if(area.BlockLine==null)throw new InvalidDataException(LocalText.Select("Block view settings need a block assembly.","Blockansicht benötigt einen Blockverbund."));area.BlockView.Validate();}
            if(Areas.Any(a=>a.NumberLine!=null&&(!Guid.TryParseExact(a.NumberLine,"N",out _)||a.BlockLine!=null||a.Kind!="Strecke")))throw new InvalidDataException(LocalText.Select("Invalid line number overview.","Ungültige Strecken-Zugnummernübersicht."));
            if(Areas.Any(a=>a.BlockLine!=null&&(!Guid.TryParseExact(a.BlockLine,"N",out _)||a.Kind!="Strecke"))||Areas.Where(a=>a.BlockLine!=null).GroupBy(a=>a.BlockLine).Any(g=>g.Count()>1))throw new InvalidDataException(LocalText.Select("Invalid or duplicate block assembly.","Ungültiger oder doppelter Blockverbund."));
        }
    }
    public sealed class PanelDraftBinding {
        public readonly PanelTile Draft;
        readonly Func<PanelTile> current;
        public PanelDraftBinding(PanelTile draft,Func<PanelTile> current){Draft=draft;this.current=current;}
        public bool Active=>Draft!=null&&ReferenceEquals(Draft,current());
        // Native periodic getters may run until deferred UI destruction finishes.
        public bool Mirror=>Draft!=null&&Draft.Mirror;
        public void Do(Action action){if(Active)action();}
    }
    public sealed class PanelStore {
        readonly string key;
        public readonly string FilePath;
        public PanelStore(string directory,string worldKey){key=worldKey;FilePath=Path.Combine(directory,worldKey+".json");}
        public PanelDocument Load() {
            if(!File.Exists(FilePath))return new PanelDocument{WorldKey=key};
            if(new FileInfo(FilePath).Length>1024*1024)throw new InvalidDataException("Gleisbilddatei zu groß; Original bleibt erhalten.");
            var doc=JsonConvert.DeserializeObject<PanelDocument>(File.ReadAllText(FilePath));if(doc==null)throw new InvalidDataException("Leere Gleisbilddatei.");doc.Validate(key);return doc.Clone();
        }
        public void Save(PanelDocument doc) {
            doc.Validate(key);Directory.CreateDirectory(Path.GetDirectoryName(FilePath));string temp=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllText(temp,JsonConvert.SerializeObject(doc,Formatting.Indented),new UTF8Encoding(false));if(File.Exists(FilePath))File.Replace(temp,FilePath,FilePath+".bak");else File.Move(temp,FilePath);}finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
    public sealed class PanelButtons {
        readonly Func<double> clock;
        double selectionUntil;
        public PanelButtons(Func<double> clock=null){this.clock=clock??(()=>System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency);}
        public bool ExpireSelection(){if(Start==null||End!=null||clock()<selectionUntil)return false;Clear();return true;}
        public string Start {get;private set;}
        public string End {get;private set;}
        public bool StartShunt {get;private set;}
        public bool StartHalf(PanelTile tile,bool right)=>Start!=null&&
            Start==SignalCellLayout.SignalAt(tile,right)&&StartShunt==SignalCellLayout.ShuntHalf(tile,right);
        public void Clear(){Start=null;End=null;StartShunt=false;}
        public void ClearEnd(){End=null;}
        public bool Press(string id,Func<string,bool> valid,bool startShunt=false) {
            if(string.IsNullOrEmpty(id)||!valid(id))throw new InvalidOperationException("Signalverknüpfung fehlt oder ist nicht bedienbar.");
            if(Start==null||End!=null){Start=id;End=null;StartShunt=startShunt;selectionUntil=clock()+5;return false;}
            if(Start==id){Clear();return false;}
            if(!valid(Start)){Clear();throw new InvalidOperationException("Startsignal ist nicht mehr verfügbar.");}
            End=id;return true;
        }
        public RouteDefinition Match(IEnumerable<RouteDefinition> routes,bool shunting,bool reduced) {
            var choices=routes.Where(r=>r.Entry==Start&&r.Exit==End&&r.Shunting==shunting&&r.Reduced==reduced).ToArray();
            if(choices.Length!=1)throw new InvalidOperationException(choices.Length==0?"Für Start/Ziel und Fahrart ist noch keine Fahrstraße gespeichert. Unter Fahrstraßen den passenden Fahrweg übernehmen.":"Mehrere passende Fahrstraßen: unter Fahrstraßen die gewünschte Definition wählen.");
            return choices[0];
        }
    }
}
