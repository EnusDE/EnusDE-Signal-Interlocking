using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EnusDE.Signaltechnik {
    public static class DiagnosticArchive {
        public static string Write(string directory,IReadOnlyDictionary<string,object> parts) {
            if(string.IsNullOrWhiteSpace(directory)||!Path.IsPathRooted(directory))throw new IOException(LocalText.Select("Choose an absolute local export folder.","Einen absoluten lokalen Exportordner wählen."));
            if(parts==null||parts.Count==0||parts.Count>32)throw new InvalidDataException("Diagnostic part count exceeds limit.");
            var data=new Dictionary<string,byte[]>();long total=0;
            var json=new JsonSerializerSettings{Formatting=Formatting.Indented,Converters=new List<JsonConverter>{new StringEnumConverter()}};
            foreach(var part in parts) {
                if(string.IsNullOrWhiteSpace(part.Key)||part.Key!=Path.GetFileName(part.Key)||part.Key.Contains("/")||part.Key.Contains("\\")||!part.Key.EndsWith(".json",StringComparison.Ordinal))throw new InvalidDataException("Invalid diagnostic entry name.");
                var bytes=new UTF8Encoding(false).GetBytes(JsonConvert.SerializeObject(part.Value,json));total+=bytes.Length;
                if(bytes.Length>32*1024*1024||total>64*1024*1024)throw new InvalidDataException(LocalText.Select("Diagnostic snapshot exceeds 64 MiB. No archive was written.","Diagnosemomentaufnahme überschreitet 64 MiB. Es wurde kein Archiv geschrieben."));
                data.Add(part.Key,bytes);
            }
            Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,"EnusDE-diagnostics-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".zip"),temporary=path+".tmp";
            try {
                using(var file=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))using(var zip=new ZipArchive(file,ZipArchiveMode.Create))
                    foreach(var part in data){var entry=zip.CreateEntry(part.Key,CompressionLevel.Optimal);using(var output=entry.Open())output.Write(part.Value,0,part.Value.Length);}
                File.Move(temporary,path);return path;
            }finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
