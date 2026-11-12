using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace EnusDE.Signaltechnik {
    public sealed class PanelHistory {
        sealed class Entry {public string File,Before,After;}
        sealed class Index {public int Schema=1,Position;public List<Entry> Entries=new List<Entry>();}
        sealed class Delta {public int BeforeColumns,BeforeRows,AfterColumns,AfterRows;public List<PanelTile> Before,After;public List<PanelArea> BeforeAreas,AfterAreas;}
        readonly string directory,indexFile;
        Index index=new Index();
        public int UndoCount=>index.Position;
        public int RedoCount=>index.Entries.Count-index.Position;
        static string Json(object o)=>JsonConvert.SerializeObject(o);
        public static string Hash(PanelDocument doc){var view=new PanelDocument{Schema=doc.Schema,Columns=doc.Columns,Rows=doc.Rows,WorldKey=doc.WorldKey,Tiles=doc.Tiles.OrderBy(t=>t.Y).ThenBy(t=>t.X).ToList(),Areas=doc.Areas.OrderBy(a=>a.Id,StringComparer.Ordinal).ToList()};using(var hash=SHA256.Create())using(var sink=new CryptoStream(Stream.Null,hash,CryptoStreamMode.Write)) {using(var writer=new StreamWriter(sink,new UTF8Encoding(false),8192,true))using(var json=new JsonTextWriter(writer){CloseOutput=false}){JsonSerializer.CreateDefault().Serialize(json,view);json.Flush();writer.Flush();}sink.FlushFinalBlock();return BitConverter.ToString(hash.Hash).Replace("-","").ToLowerInvariant();}}
        public PanelHistory(string path,PanelDocument current) {
            directory=Path.GetFullPath(path);indexFile=Path.Combine(directory,"history.json");
            if(File.Exists(indexFile)){if(new FileInfo(indexFile).Length>32768)throw new InvalidDataException("Editorverlauf zu groß.");var loaded=JsonConvert.DeserializeObject<Index>(File.ReadAllText(indexFile));Validate(loaded);index=loaded;if(index.Entries.Count>0&&Expected()!=Hash(current)){index=new Index();SaveIndex(index);Cleanup();}}
        }
        static void Validate(Index value){Guid id;if(value==null||value.Schema!=1||value.Entries==null||value.Entries.Count>10||value.Position<0||value.Position>value.Entries.Count||value.Entries.Any(e=>e==null||!Guid.TryParseExact(e.File,"N",out id)||e.Before?.Length!=64||e.After?.Length!=64))throw new InvalidDataException("Editorverlauf ungültig; Datei bleibt erhalten.");}
        string Expected()=>index.Position==0?index.Entries[0].Before:index.Entries[index.Position-1].After;
        static long Key(PanelTile t)=>((long)t.Y<<32)|(uint)t.X;
        public void Record(PanelDocument before,PanelDocument after) {
            string first=Hash(before),last=Hash(after);if(first==last)return;
            var old=before.Tiles.ToDictionary(Key,t=>t);var next=after.Tiles.ToDictionary(Key,t=>t);var changed=old.Keys.Union(next.Keys).Where(k=>!old.ContainsKey(k)||!next.ContainsKey(k)||!old[k].SameContent(next[k])).ToArray();
            var delta=new Delta{BeforeColumns=before.Columns,BeforeRows=before.Rows,AfterColumns=after.Columns,AfterRows=after.Rows,Before=changed.Where(old.ContainsKey).Select(k=>old[k]).ToList(),After=changed.Where(next.ContainsKey).Select(k=>next[k]).ToList(),BeforeAreas=before.Areas,AfterAreas=after.Areas};
            var copy=index.Entries.Count>0&&Expected()!=first?new Index():JsonConvert.DeserializeObject<Index>(Json(index));copy.Entries=copy.Entries.Take(copy.Position).ToList();var entry=new Entry{File=Guid.NewGuid().ToString("N"),Before=first,After=last};Directory.CreateDirectory(directory);
            using(var file=File.Create(Path.Combine(directory,entry.File+".gz")))using(var zip=new GZipStream(file,CompressionLevel.Optimal))using(var writer=new StreamWriter(zip,new UTF8Encoding(false)))writer.Write(Json(delta));
            copy.Entries.Add(entry);if(copy.Entries.Count>10)copy.Entries.RemoveAt(0);copy.Position=copy.Entries.Count;SaveIndex(copy);index=copy;Cleanup();
        }
        public PanelDocument Target(PanelDocument current,bool redo) {
            if(redo?RedoCount==0:UndoCount==0)throw new InvalidOperationException(redo?"Kein Schritt wiederherstellbar.":"Kein Schritt rückgängig zu machen.");if(Expected()!=Hash(current))throw new InvalidOperationException("Tafelstand passt nicht zum Verlauf. Neu öffnen und erneut bearbeiten.");
            var entry=index.Entries[redo?index.Position:index.Position-1];Delta delta;using(var file=File.OpenRead(Path.Combine(directory,entry.File+".gz")))using(var zip=new GZipStream(file,CompressionMode.Decompress))using(var output=new MemoryStream()){var buffer=new byte[8192];int n;while((n=zip.Read(buffer,0,buffer.Length))>0){if(output.Length+n>32*1024*1024)throw new InvalidDataException("Editorverlaufseintrag zu groß.");output.Write(buffer,0,n);}delta=JsonConvert.DeserializeObject<Delta>(new UTF8Encoding(false,true).GetString(output.ToArray()));}
            if(delta?.Before==null||delta.After==null||delta.BeforeAreas==null||delta.AfterAreas==null)throw new InvalidDataException("Editorverlaufseintrag unvollständig.");
            var target=current.Clone();var keys=new HashSet<long>(delta.Before.Concat(delta.After).Select(Key));target.Tiles=target.Tiles.Where(t=>!keys.Contains(Key(t))).Concat(redo?delta.After:delta.Before).ToList();target.Columns=redo?delta.AfterColumns:delta.BeforeColumns;target.Rows=redo?delta.AfterRows:delta.BeforeRows;target.Areas=redo?delta.AfterAreas:delta.BeforeAreas;target.Validate(current.WorldKey);
            if(Hash(target)!=(redo?entry.After:entry.Before))throw new InvalidDataException("Editorverlauf ist beschädigt.");return target;
        }
        public void Accept(PanelDocument target,bool redo) {var copy=JsonConvert.DeserializeObject<Index>(Json(index));int at=redo?copy.Position:copy.Position-1;if(at<0||at>=copy.Entries.Count)throw new InvalidOperationException("Verlauf nicht verfügbar.");var entry=copy.Entries[at];if(Hash(target)!=(redo?entry.After:entry.Before))throw new InvalidDataException("Verlauf passt nicht zum übernommenen Stand.");copy.Position+=redo?1:-1;SaveIndex(copy);index=copy;}
        void SaveIndex(Index value){Validate(value);Directory.CreateDirectory(directory);string temp=indexFile+".tmp";File.WriteAllText(temp,Json(value),new UTF8Encoding(false));if(File.Exists(indexFile))File.Replace(temp,indexFile,null);else File.Move(temp,indexFile);}
        void Cleanup(){var keep=new HashSet<string>(index.Entries.Select(e=>e.File+".gz"));foreach(string file in Directory.GetFiles(directory,"*.gz")){Guid id;if(Guid.TryParseExact(Path.GetFileNameWithoutExtension(file),"N",out id)&&!keep.Contains(Path.GetFileName(file)))File.Delete(file);}}
    }
}
