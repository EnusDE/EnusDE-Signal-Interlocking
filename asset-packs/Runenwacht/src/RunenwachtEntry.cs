using System;
using EnusDE.Signaltechnik;
using Railroader.Modding;
using UnityEngine;
namespace EnusDE.Runenwacht {
    public sealed class Entry:IModEntryPoint {
        IDisposable registration;
        public void OnModLoad(IModContext context){registration=SignalPresentationPacks.Register(new RunenwachtProvider());context.Log.Info("Runenwacht 0.2.0: presentation API v1 registered; new placements remain experimental and locked by default.");}
        public void OnModUnload(){registration?.Dispose();registration=null;}
    }
    public sealed class RunenwachtProvider:ISignalPresentationProvider {
        public int ApiVersion=>1;public string Id=>SignalPresentationPacks.Runenwacht;public string Version=>"0.2.0";public string Name=>"Runenwacht";
        public ISignalPresentation Create(GameObject parent,string kind,bool preview) {
            if(kind!=MarkerRecord.Main&&kind!=MarkerRecord.Distant&&kind!=MarkerRecord.Shunt)throw new ArgumentException("Unsupported Runenwacht model.");
            var model=parent.AddComponent<RunenwachtModel>();model.Build(kind,preview);return model;
        }
    }
}
