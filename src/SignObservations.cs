using System;
using System.Reflection;
using HarmonyLib;
using Game.Mow;

namespace EnusDE.Signaltechnik {
    // Observes completed existing handlers; it never invokes a game action.
    public sealed class SignObservations : IDisposable {
        const string PatchId="enusde.signaltechnik.sign-observations";
        static Action<string,string> callback;
        Harmony harmony;
        public void Start(Action<string,string> observer) {
            Dispose();
            try {
                harmony=new Harmony(PatchId);
                foreach(string name in new[]{"HandleAddSign","HandleAddPlayerSign","HandleRemoveSign"}) {
                    var method=typeof(SignController).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic);
                    if(method==null)throw new MissingMethodException(typeof(SignController).FullName,name);
                    harmony.Patch(method,postfix:new HarmonyMethod(typeof(SignObservations),nameof(AfterHandler)));
                }
                callback=observer;
            }catch{Dispose();throw;}
        }
        static void AfterHandler(string __0,MethodBase __originalMethod) {
            try{callback?.Invoke(__originalMethod.Name,__0);}catch{}
        }
        public void Dispose(){callback=null;if(harmony!=null){harmony.UnpatchAll(PatchId);harmony=null;}}
    }
}
