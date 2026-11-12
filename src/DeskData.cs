using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public sealed class DeskDefinition {
        public string Id=Guid.NewGuid().ToString("N"),Code="",Name="";
        public bool Global;
        public PanelDocument Panel;
        public DeskDefinition Clone(){var copy=(DeskDefinition)MemberwiseClone();copy.Panel=Panel?.Clone();return copy;}
    }
    public sealed class DeskLibrary {
        public int Schema=1;
        public string WorldKey;
        public bool AutoInitialized;
        public Dictionary<string,TrackBlock> Blocks=new Dictionary<string,TrackBlock>();
        public Dictionary<string,PointAssignment> Points=new Dictionary<string,PointAssignment>();
        public List<DeskDefinition> Desks=new List<DeskDefinition>();
        public DeskLibrary Clone(){var copy=(DeskLibrary)MemberwiseClone();copy.Desks=Desks?.Select(d=>d?.Clone()).ToList();copy.Blocks=Blocks?.ToDictionary(p=>p.Key,p=>p.Value?.Clone(),Blocks.Comparer);copy.Points=Points?.ToDictionary(p=>p.Key,p=>p.Value?.Clone(),Points.Comparer);return copy;}
        public void Validate(string world) {
            if(Blocks==null||Blocks.Count>4096)throw new InvalidDataException("Abschnittskatalog zu groß.");foreach(var block in Blocks){if(block.Value==null||block.Key!=block.Value.Id)throw new InvalidDataException("Abschnittskennung ungültig.");block.Value.Validate();}if(Blocks.Values.Sum(b=>b.Sections.Count())>16384)throw new InvalidDataException("Abschnittskatalog zu groß.");
            if(Schema!=1||WorldKey!=world||string.IsNullOrEmpty(world)||Desks==null||Desks.Count<1||Desks.Count>32||Desks.Count(d=>d!=null&&d.Global)!=1)throw new InvalidDataException("Stellwerksverwaltung ungültig oder falsche Welt.");
            foreach(var desk in Desks){Guid id;if(desk==null||!Guid.TryParseExact(desk.Id,"N",out id)||string.IsNullOrWhiteSpace(desk.Code)||desk.Code.Length>24||string.IsNullOrWhiteSpace(desk.Name)||desk.Name.Length>64||desk.Code.Any(char.IsControl)||desk.Name.Any(char.IsControl)||desk.Panel==null)throw new InvalidDataException("Ungültige Stellwerkskennung.");desk.Panel.Validate(world);}
            if(Desks.Select(d=>d.Id).Distinct().Count()!=Desks.Count||Desks.Select(d=>d.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Desks.Count)throw new InvalidDataException("Stellwerks-ID oder Kennung doppelt.");
            var targets=AuxiliaryTarget.Index(Desks.SelectMany(d=>d.Panel.Tiles));
            if(Desks.SelectMany(d=>d.Panel.Tiles).Any(t=>t.LinkedAuxiliary!=null&&!targets.ContainsKey(t.LinkedAuxiliary)))
                throw new InvalidDataException("Hilfsziel wird an einem Signalfeld verwendet. Verknüpfung zuerst entfernen.");
            if(targets.Count>256)throw new InvalidDataException("Höchstens 256 Hilfsziele pro Spielstand.");
            PointAssignments.Validate(this);
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
        public DeskLibrary PutPanel(DeskLibrary library,string id,PanelDocument panel,TrackBlock addBlock=null) {var next=library.Clone();var desk=next.Desks.FirstOrDefault(d=>d.Id==id)??throw new InvalidOperationException("Stellwerk fehlt.");desk.Panel=panel.Clone();if(addBlock!=null)next.Blocks[addBlock.Id]=addBlock.Clone();Save(next);return next;}
        public DeskLibrary CreateLocal(DeskLibrary library,string code,string name,PanelDocument panel) {var next=library.Clone();next.Desks.Add(new DeskDefinition{Code=string.IsNullOrWhiteSpace(code)?RouteNaming.DeskCode(name,library.Desks.Select(d=>d.Code)):code.Trim(),Name=(name??"").Trim(),Panel=panel.Clone()});Save(next);return next;}
        public DeskLibrary Rename(DeskLibrary library,string id,string code,string name) {var next=library.Clone();var desk=next.Desks.First(d=>d.Id==id);desk.Code=string.IsNullOrWhiteSpace(code)?RouteNaming.DeskCode(name,next.Desks.Where(d=>d.Id!=id).Select(d=>d.Code)):code.Trim();desk.Name=(name??"").Trim();Save(next);return next;}
    }
    public sealed class PanelRegion {
        public int X,Y,Width=1,Height=1;
        public bool Contains(int x,int y)=>x>=X&&x<X+Width&&y>=Y&&y<Y+Height;
        public PanelDocument Extract(PanelDocument source) {var result=new PanelDocument{WorldKey=source.WorldKey,Columns=Math.Max(8,Width),Rows=Math.Max(6,Height),Tiles=source.Tiles.Where(t=>Contains(t.X,t.Y)).Select(t=>{var copy=t.Clone();copy.X-=X;copy.Y-=Y;return copy;}).ToList()};foreach(var a in source.Areas.Where(a=>Contains(a.X,a.Y)&&Contains(a.X+a.Width-1,a.Y+a.Height-1)))result.Areas.Add(new PanelArea{Code=a.Code,Name=a.Name,Kind=a.Kind,X=a.X-X,Y=a.Y-Y,Width=a.Width,Height=a.Height});result.Validate(source.WorldKey);return result;}
        public void Move(PanelDocument document,int x,int y) {
            var next=document.Clone();var selected=next.Tiles.Where(t=>Contains(t.X,t.Y)).ToArray();
            foreach(var tile in selected){int nx=x+tile.X-X,ny=y+tile.Y-Y;if(next.Tiles.Any(t=>!selected.Contains(t)&&t.X==nx&&t.Y==ny))throw new InvalidOperationException("Zielbereich enthält andere Felder.");}
            foreach(var tile in selected){tile.X+=x-X;tile.Y+=y-Y;}foreach(var a in next.Areas.Where(a=>Contains(a.X,a.Y)&&Contains(a.X+a.Width-1,a.Y+a.Height-1))){a.X+=x-X;a.Y+=y-Y;}next.Validate(document.WorldKey);document.Tiles=next.Tiles;document.Areas=next.Areas;
        }
    }
}
