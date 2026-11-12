using System;
using System.Diagnostics;

namespace EnusDE.Signaltechnik {
    public sealed class MemorySnapshot {
        public long managed_bytes;
        public long? process_private_bytes,process_working_set_bytes;
        public static MemorySnapshot Read() {
            var snapshot=new MemorySnapshot{managed_bytes=GC.GetTotalMemory(false)};
            try{using(var process=Process.GetCurrentProcess()){process.Refresh();snapshot.process_private_bytes=process.PrivateMemorySize64;snapshot.process_working_set_bytes=process.WorkingSet64;}}catch{}
            return snapshot;
        }
        public string Display()=>"Verwaltet "+(managed_bytes/1048576d).ToString("0.0")+" MiB · Prozess privat "+(process_private_bytes.HasValue?(process_private_bytes.Value/1048576d).ToString("0.0")+" MiB":"nicht verfügbar");
    }
}
