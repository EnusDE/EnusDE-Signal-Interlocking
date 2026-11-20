using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Track;

namespace EnusDE.Signaltechnik {
    // Only the prefab choice at CreateSwitchStand is changed. Native control flags stay untouched.
    public sealed class NativeSwitchModels : IDisposable {
        const string Owner="enusde.signaltechnik.switch-models";
        static Func<string,bool> requested;
        static readonly FieldInfo managerProfile=typeof(TrackObjectManager).GetField("profile",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        static readonly FieldInfo electricPrefab=typeof(TrackMeshProfile).GetField("switchStandPrefabCTC",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        Harmony harmony;
        public void Start(Func<string,bool> select) {
            Dispose();
            try {
                var target=typeof(TrackObjectBuilder).GetMethod("CreateSwitchStand",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                if(target==null||managerProfile==null||electricPrefab==null)throw new MissingMethodException("Native elektrische Weichenantriebe nicht verfügbar.");
                requested=select;harmony=new Harmony(Owner);harmony.Patch(target,transpiler:new HarmonyMethod(typeof(NativeSwitchModels),nameof(ChoosePrefab)));
            } catch{Dispose();throw;}
        }
        static bool UseElectricModel(TrackNode node) {
            if(node==null)return false;
            if(node.IsCTCSwitch)return true;
            try {
                if(requested?.Invoke(node.id)!=true)return false;
                var manager=TrackObjectManager.Instance;if(manager==null)return false;
                var profile=managerProfile.GetValue(manager);
                return profile!=null&&electricPrefab.GetValue(profile) as SwitchStand!=null;
            } catch{return false;}
        }
        public static IEnumerable<CodeInstruction> ChoosePrefab(IEnumerable<CodeInstruction> source) {
            var rows=source.Select(r=>new CodeInstruction(r)).ToArray();
            var original=typeof(TrackNode).GetProperty("IsCTCSwitch").GetGetMethod();
            var matches=rows.Where(r=>r.Calls(original)).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Unerwartete native Auswahl des Weichenantriebs.");
            matches[0].opcode=OpCodes.Call;matches[0].operand=typeof(NativeSwitchModels).GetMethod(nameof(UseElectricModel),BindingFlags.Static|BindingFlags.NonPublic);
            return rows;
        }
        public void Dispose(){requested=null;if(harmony!=null){harmony.UnpatchAll(Owner);harmony=null;}}
    }
}
