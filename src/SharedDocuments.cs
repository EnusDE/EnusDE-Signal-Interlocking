using System;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    public interface ISharedProperties {string Read(string key);void Write(string key,string value);}
    public sealed class SharedDocumentHeader {
        public int Protocol=1,Slot,Count,Bytes,Length;
        public long Revision;
        public string Hash;
    }
    // Two banks: interrupted writes never overwrite the last committed document.
    public sealed class SharedDocuments {
        public const int ChunkLength=8192;
        readonly ISharedProperties properties;
        public SharedDocuments(ISharedProperties properties){this.properties=properties;}
        public string Header(string channel)=>properties.Read(channel+".commit");
        SharedDocumentHeader Parse(string raw,int limit) {
            if(raw==null)return null;if(raw.Length>1024)throw new InvalidDataException("Synchronisationskopf zu groß.");var h=JsonConvert.DeserializeObject<SharedDocumentHeader>(raw);
            if(h==null||h.Protocol!=1||h.Slot<0||h.Slot>1||h.Count<1||h.Count>(limit*2/ChunkLength+2)||h.Length<1||h.Length>limit||h.Bytes<1||h.Bytes>limit+65536||h.Revision<1||h.Hash==null||h.Hash.Length!=64||h.Hash.Any(c=>!Uri.IsHexDigit(c)))throw new InvalidDataException("Unbekannte oder ungültige Synchronisationsdaten.");return h;
        }
        public T Read<T>(string channel,int limit) {
            string head=Header(channel);var h=Parse(head,limit);if(h==null)return default(T);var encoded=new StringBuilder();
            for(int i=0;i<h.Count;i++){string part=properties.Read(channel+"."+h.Slot+"."+i);if(part==null||part.Length>ChunkLength)throw new InvalidDataException("Synchronisation noch unvollständig.");encoded.Append(part);}
            byte[] bytes=Convert.FromBase64String(encoded.ToString());if(bytes.Length!=h.Bytes||Hash(bytes)!=h.Hash)throw new InvalidDataException("Synchronisations-Prüfsumme stimmt nicht.");
            using(var input=new MemoryStream(bytes))using(var zip=new GZipStream(input,CompressionMode.Decompress))using(var output=new MemoryStream()) {
                var buffer=new byte[8192];int n;while((n=zip.Read(buffer,0,buffer.Length))>0){if(output.Length+n>limit||output.Length+n>h.Length)throw new InvalidDataException("Entpackte Synchronisation zu groß.");output.Write(buffer,0,n);}if(output.Length!=h.Length||Header(channel)!=head)throw new InvalidDataException("Synchronisation während des Lesens geändert.");
                return JsonConvert.DeserializeObject<T>(new UTF8Encoding(false,true).GetString(output.ToArray()));
            }
        }
        public void Write<T>(string channel,T value,int limit) {
            byte[] raw=new UTF8Encoding(false,true).GetBytes(JsonConvert.SerializeObject(value));if(raw.Length<1||raw.Length>limit)throw new InvalidDataException("Gemeinsamer Datenbestand zu groß.");byte[] compressed;
            using(var output=new MemoryStream()){using(var zip=new GZipStream(output,CompressionLevel.Optimal,true))zip.Write(raw,0,raw.Length);compressed=output.ToArray();}
            var previous=Parse(Header(channel),limit);string data=Convert.ToBase64String(compressed);var h=new SharedDocumentHeader{Slot=previous==null?0:1-previous.Slot,Count=(data.Length+ChunkLength-1)/ChunkLength,Bytes=compressed.Length,Length=raw.Length,Hash=Hash(compressed),Revision=checked((previous?.Revision??0)+1)};
            for(int i=0;i<h.Count;i++)properties.Write(channel+"."+h.Slot+"."+i,data.Substring(i*ChunkLength,Math.Min(ChunkLength,data.Length-i*ChunkLength)));
            // Remove obsolete chunks of this bank so repeated editing cannot grow the save indefinitely.
            int oldCount=0;string bankKey=channel+".bank"+h.Slot;int.TryParse(properties.Read(bankKey),out oldCount);for(int i=h.Count;i<oldCount&&i<limit*2/ChunkLength+2;i++)properties.Write(channel+"."+h.Slot+"."+i,null);
            properties.Write(bankKey,h.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));properties.Write(channel+".commit",JsonConvert.SerializeObject(h));
        }
        public static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    }
    public sealed class LiveLease {
        string epoch;
        long sequence;
        double received=-100;
        bool confirmed;
        public bool Accept(string session,long number,double now) {
            Guid id;if(!Guid.TryParseExact(session,"N",out id)||number<1||double.IsNaN(now))return false;
            if(epoch!=session){epoch=session;sequence=number;received=now;confirmed=false;return true;}
            if(number<=sequence)return false;bool recent=now>=received&&now-received<5;sequence=number;received=now;confirmed=recent;return true;
        }
        public bool Fresh(double now)=>confirmed&&now>=received&&now-received<5;
    }
}
