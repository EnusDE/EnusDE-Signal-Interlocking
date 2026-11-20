using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public enum OperatingSiteKind { Station,PassingLoop,BlockPost,Siding,OvertakingLoop }
    public sealed class DeskDefinition {
        public string Id=Guid.NewGuid().ToString("N"),Code="",Name="";
        public bool Global;
        public bool MasterView;
        [JsonIgnore] public bool IsMaster=>Global||MasterView;
        [System.ComponentModel.DefaultValue(true),JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool ShowInDispatcher=true;
        public bool AutomaticOnly;
        public bool OutOfService;
        public StationAcceptance Acceptance;
        public OperatingSiteKind SiteKind;
        public int MaximumReceivingTrains;
        public List<ReceivingTrack> ReceivingTracks=new List<ReceivingTrack>();
        public PanelDocument Panel;
        public DeskDefinition Clone(){var copy=(DeskDefinition)MemberwiseClone();copy.Panel=Panel?.Clone();copy.ReceivingTracks=ReceivingTracks?.Select(t=>t?.Clone()).ToList();return copy;}
    }
    public sealed class DeskLibrary {
        public int Schema=1;
        public string WorldKey;
        public bool AutoInitialized;
        public AutomationConfiguration Automation=new AutomationConfiguration();
        public Dictionary<string,TrackBlock> Blocks=new Dictionary<string,TrackBlock>();
        public Dictionary<string,PointAssignment> Points=new Dictionary<string,PointAssignment>();
        public Dictionary<string,PointBoxPlacement> PointBoxes=new Dictionary<string,PointBoxPlacement>();
        public List<LineTrackDefinition> FreeLines=new List<LineTrackDefinition>();
        public List<DeskModule> Modules=new List<DeskModule>();
        public List<DeskDefinition> Desks=new List<DeskDefinition>();
        public DeskLibrary Clone(){var copy=(DeskLibrary)MemberwiseClone();copy.Automation=Automation==null?null:JsonConvert.DeserializeObject<AutomationConfiguration>(JsonConvert.SerializeObject(Automation));copy.FreeLines=FreeLines?.Select(l=>l?.Clone()).ToList();copy.Modules=Modules?.Select(m=>m?.Clone()).ToList();copy.Desks=Desks?.Select(d=>d?.Clone()).ToList();copy.Blocks=Blocks?.ToDictionary(p=>p.Key,p=>p.Value?.Clone(),Blocks.Comparer);copy.PointBoxes=PointBoxes?.ToDictionary(p=>p.Key,p=>p.Value?.Clone(),PointBoxes.Comparer);copy.Points=Points?.ToDictionary(p=>p.Key,p=>p.Value?.Clone(),Points.Comparer);return copy;}
        public void Validate(string world) {
            if(Automation==null)throw new InvalidDataException("Automatikkonfiguration fehlt.");Automation.Validate();
            if(Desks?.Any(d=>d!=null&&(!Enum.IsDefined(typeof(StationAcceptance),d.Acceptance)||!Enum.IsDefined(typeof(OperatingSiteKind),d.SiteKind)))==true)throw new InvalidDataException("Ungültige Annahmeart.");
            if(Blocks==null||Blocks.Count>4096)throw new InvalidDataException("Abschnittskatalog zu groß.");foreach(var block in Blocks){if(block.Value==null||block.Key!=block.Value.Id)throw new InvalidDataException("Abschnittskennung ungültig.");block.Value.Validate();}if(Blocks.Values.Sum(b=>b.Sections.Count())>16384)throw new InvalidDataException("Abschnittskatalog zu groß.");
            if(Schema!=1||WorldKey!=world||string.IsNullOrEmpty(world)||Desks==null||Desks.Count<1||Desks.Count>32||Desks.Count(d=>d!=null&&d.Global)!=1)throw new InvalidDataException("Stellwerksverwaltung ungültig oder falsche Welt.");
            foreach(var desk in Desks){Guid id;if(desk==null||!Guid.TryParseExact(desk.Id,"N",out id)||string.IsNullOrWhiteSpace(desk.Code)||desk.Code.Length>24||string.IsNullOrWhiteSpace(desk.Name)||desk.Name.Length>64||desk.Code.Any(char.IsControl)||desk.Name.Any(char.IsControl)||desk.Panel==null)throw new InvalidDataException("Ungültige Stellwerkskennung.");desk.Panel.Validate(world);}
            if(Desks.Select(d=>d.Id).Distinct().Count()!=Desks.Count||Desks.Select(d=>d.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Desks.Count)throw new InvalidDataException("Stellwerks-ID oder Kennung doppelt.");
            LineTrackDefinition.ValidateLibrary(this);
            StationCapacity.Validate(this);
            LineTargetBindings.Index(this);
            MasterDiagram.Validate(this);
            new NumberMirrorIndex(this);
            var targets=AuxiliaryTarget.Index(Desks.SelectMany(d=>d.Panel.Tiles));
            foreach(var desk in Desks)foreach(var tile in desk.Panel.Tiles) {
                if(tile.PermissionAuxiliary!=null&&(!targets.TryGetValue(tile.PermissionAuxiliary,out var auxiliary)||auxiliary.PermissionLine!=""&&tile.Reference!=""&&auxiliary.PermissionLine!=tile.Reference))throw new InvalidDataException(LocalText.Select("Unlink the permission module before deleting or assigning its auxiliary button to another line.","Erlaubnismodul vor Löschen oder Zuordnung seiner Hilfstaste zu einer anderen Strecke lösen."));
                string lineId=tile.Kind==PanelTileKind.LinePermission?tile.Reference:tile.Target?.Function==AuxiliaryFunction.LinePermission?tile.Target.PermissionLine:"";
                if(!string.IsNullOrEmpty(lineId)) {
                    var line=FreeLines.FirstOrDefault(l=>l.Id==lineId);
                    if(line==null||!desk.IsMaster&&desk.Id!=line.StationA&&desk.Id!=line.StationB&&desk.Id!=line.BlockOwner&&!(tile.Kind==PanelTileKind.LinePermission&&tile.ReadOnlySignal&&line.BlockDisplays.Contains(desk.Id)))throw new InvalidDataException(LocalText.Select("A permission control must belong to its line's neighbouring interlockings.","Eine Erlaubnisbedienung muss zu den Nachbarbetriebsstellen ihrer Strecke gehören."));
                }
            }
            if(Desks.SelectMany(d=>d.Panel.Tiles).Any(t=>t.LinkedAuxiliary!=null&&!targets.ContainsKey(t.LinkedAuxiliary)))
                throw new InvalidDataException("Hilfsziel wird an einem Signalfeld verwendet. Verknüpfung zuerst entfernen.");
            if(targets.Count>256)throw new InvalidDataException("Höchstens 256 Hilfsziele pro Spielstand.");
            PointAssignments.Validate(this);
            if(Points.Values.Any(p=>p.BoundaryLinks==null||p.BoundaryLinks.Count>3||p.BoundaryLinks.Any(b=>b.Key<0||b.Key>2||!Guid.TryParseExact(b.Value,"N",out _))))throw new InvalidDataException(LocalText.Select("Invalid point-to-GFM boundary links.","Ungültige Weichen-GFM-Grenzverknüpfungen."));
            if(PointBoxes==null||PointBoxes.Count>12288)throw new InvalidDataException("Weichenkasten-Positionen ungültig.");
            foreach(var pair in PointBoxes){if(!TrackCircuit.TryPointId(pair.Key,out var point,out _)||!Points.ContainsKey(point)||pair.Value==null)throw new InvalidDataException("Ungültige Weichenkasten-Zuordnung.");pair.Value.Validate();}
            if(Desks.SelectMany(d=>d.Panel.Tiles).Any(t=>t.BlockId!=""&&!Blocks.ContainsKey(t.BlockId)||t.SecondBlock!=""&&!Blocks.ContainsKey(t.SecondBlock)||
                t.CircuitRails?.Values.Any(id=>!Blocks.ContainsKey(id))==true))throw new InvalidDataException("Tafelfeld verweist auf fehlenden Belegungsabschnitt.");
        }
    }
    public sealed class DeskStore {
        readonly string key;
        readonly Func<DeskLibrary> sharedRead;
        readonly Action<DeskLibrary> sharedWrite;
        public readonly string FilePath;
        public DeskStore(string directory,string key){this.key=key;FilePath=Path.Combine(directory,key+".json");}
        public DeskStore(string directory,string key,Func<DeskLibrary> read,Action<DeskLibrary> write):this(directory,key){sharedRead=read;sharedWrite=write;}
        public DeskLibrary Load(PanelDocument legacy) {
            if(sharedRead!=null){var shared=sharedRead()??throw new InvalidDataException("Gemeinsame Stellwerke werden noch übertragen.");shared.Validate(key);return shared.Clone();}
            if(File.Exists(FilePath)){if(new FileInfo(FilePath).Length>16*1024*1024)throw new InvalidDataException("Stellwerksverwaltung zu groß; Original bleibt erhalten.");var value=JsonConvert.DeserializeObject<DeskLibrary>(File.ReadAllText(FilePath));if(value==null)throw new InvalidDataException("Leere Stellwerksverwaltung.");value.Validate(key);return value.Clone();}
            legacy.Validate(key);var created=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{new DeskDefinition{Global=true,Code="ESTW",Name="Übergeordnetes ESTW",Panel=legacy.Clone()}}};Save(created);return created;
        }
        public void Save(DeskLibrary value) {
            PointAssignments.Boundaries(value);
            if(sharedWrite!=null){value.Validate(key);sharedWrite(value.Clone());return;}
            value.Validate(key);string json=JsonConvert.SerializeObject(value,Formatting.Indented);if(Encoding.UTF8.GetByteCount(json)>16*1024*1024)throw new InvalidDataException("Stellwerksverwaltung überschreitet 16 MiB.");Directory.CreateDirectory(Path.GetDirectoryName(FilePath));string temp=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllText(temp,json,new UTF8Encoding(false));if(File.Exists(FilePath))File.Replace(temp,FilePath,FilePath+".bak");else File.Move(temp,FilePath);}finally{if(File.Exists(temp))File.Delete(temp);}
        }
        public DeskLibrary PutPanel(DeskLibrary library,string id,PanelDocument panel,TrackBlock addBlock=null) {var next=library.Clone();var desk=next.Desks.FirstOrDefault(d=>d.Id==id)??throw new InvalidOperationException("Stellwerk fehlt.");int dx=panel.OriginX-desk.Panel.OriginX,dy=panel.OriginY-desk.Panel.OriginY;
            if(desk.IsMaster)foreach(var module in next.Modules.Where(m=>MasterDiagram.Owner(next,m)==id)){module.X+=dx;module.Y+=dy;}
            desk.Panel=panel.Clone();if(addBlock!=null)next.Blocks[addBlock.Id]=addBlock.Clone();
            if(dx!=0||dy!=0||panel.Columns!=library.Desks.Single(d=>d.Id==id).Panel.Columns||panel.Rows!=library.Desks.Single(d=>d.Id==id).Panel.Rows)try{MasterDiagram.Validate(next);}catch(InvalidDataException error){throw new InvalidOperationException(LocalText.Select("Make room in the master layout before extending this panel: ","Vor dem Erweitern dieser Tafel im ESTW-Layout Platz schaffen: ")+error.Message);}
            Save(next);return next;}
        public DeskLibrary CreateLocal(DeskLibrary library,string code,string name,PanelDocument panel) {var next=library.Clone();next.Desks.Add(new DeskDefinition{Code=string.IsNullOrWhiteSpace(code)?RouteNaming.DeskCode(name,library.Desks.Select(d=>d.Code)):code.Trim(),Name=(name??"").Trim(),Panel=NumberMirrors.CopyPanel(panel)});Save(next);return next;}
        public DeskLibrary CreateMaster(DeskLibrary library,string code,string name) {
            var next=library.Clone();next.Desks.Add(new DeskDefinition{MasterView=true,
                Code=string.IsNullOrWhiteSpace(code)?RouteNaming.DeskCode(name,library.Desks.Select(d=>d.Code)):code.Trim(),Name=(name??"").Trim(),Panel=new PanelDocument{WorldKey=key,Columns=96,Rows=48}});
            Save(next);return next;
        }
        public DeskLibrary Rename(DeskLibrary library,string id,string code,string name) {var next=library.Clone();var desk=next.Desks.First(d=>d.Id==id);desk.Code=string.IsNullOrWhiteSpace(code)?RouteNaming.DeskCode(name,next.Desks.Where(d=>d.Id!=id).Select(d=>d.Code)):code.Trim();desk.Name=(name??"").Trim();Save(next);return next;}
    }
    public sealed class PanelRegion {
        public int X,Y,Width=1,Height=1;
        public bool Contains(int x,int y)=>x>=X&&x<X+Width&&y>=Y&&y<Y+Height;
        public PanelDocument Extract(PanelDocument source) {var result=new PanelDocument{WorldKey=source.WorldKey,Columns=Math.Max(8,Width),Rows=Math.Max(6,Height),Tiles=source.Tiles.Where(t=>Contains(t.X,t.Y)).Select(t=>{var copy=t.Clone();copy.NumberId=null;copy.X-=X;copy.Y-=Y;return copy;}).ToList()};foreach(var a in source.Areas.Where(a=>Contains(a.X,a.Y)&&Contains(a.X+a.Width-1,a.Y+a.Height-1)))result.Areas.Add(new PanelArea{Code=a.Code,Name=a.Name,Kind=a.Kind,X=a.X-X,Y=a.Y-Y,Width=a.Width,Height=a.Height});result.Validate(source.WorldKey);return result;}
        public void Move(PanelDocument document,int x,int y) {
            var next=document.Clone();var selected=next.Tiles.Where(t=>Contains(t.X,t.Y)).ToArray();
            foreach(var tile in selected){int nx=x+tile.X-X,ny=y+tile.Y-Y;if(next.Tiles.Any(t=>!selected.Contains(t)&&t.X==nx&&t.Y==ny))throw new InvalidOperationException("Zielbereich enthält andere Felder.");}
            foreach(var tile in selected){tile.X+=x-X;tile.Y+=y-Y;}foreach(var a in next.Areas.Where(a=>Contains(a.X,a.Y)&&Contains(a.X+a.Width-1,a.Y+a.Height-1))){a.X+=x-X;a.Y+=y-Y;}next.Validate(document.WorldKey);document.Tiles=next.Tiles;document.Areas=next.Areas;
        }
    }
}
