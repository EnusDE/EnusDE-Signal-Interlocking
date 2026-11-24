using System;
using System.IO;
using System.Runtime.CompilerServices;
using EnusDE.Signaltechnik;
using Railroader.Modding;
using UnityEngine;
namespace EnusDE.Runenwacht {
    public sealed class Entry:IModEntryPoint {
        IDisposable registration;
        public void OnModLoad(IModContext context) {
            try {
                registration=RegisterProvider();
            } catch(Exception error) when(error is ArgumentException||error is InvalidOperationException||
                error is TypeLoadException||error is MissingMemberException||error is FileNotFoundException||error is FileLoadException) {
                context.Log.Warning("Runenwacht: presentation registration unavailable. "+error.Message);
                return;
            }
            context.Log.Info("Runenwacht "+RunenwachtProvider.PackVersion+": presentation API v1 registered.");
        }
        // Keep linkage to the main API inside the guarded call even if the contract is missing.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static IDisposable RegisterProvider()=>SignalPresentationPacks.Register(new RunenwachtProvider());
        public void OnModUnload(){registration?.Dispose();registration=null;}
    }
    public sealed class RunenwachtProvider:ISignalPresentationProvider {
        public const string PackVersion="0.2.2";
        public int ApiVersion=>1;public string Id=>SignalPresentationPacks.Runenwacht;public string Version=>PackVersion;public string Name=>"Runenwacht";
        public ISignalPresentation Create(GameObject parent,string kind,bool preview) {
            if(kind!=MarkerRecord.Main&&kind!=MarkerRecord.Distant&&kind!=MarkerRecord.Shunt)throw new ArgumentException("Unsupported Runenwacht model.");
            var model=parent.AddComponent<RunenwachtModel>();model.Build(kind,preview);return model;
        }
    }
}
