using System;
using System.Linq;
using System.Reflection;
using Core;
using Game.History;
using HarmonyLib;
using Model.Ops;
using Model.Ops.Data;

namespace EnusDE.Signaltechnik {
    // Add native suppliers before the game creates components, indexes and schedules.
    // Native Buy destinations, pricing, interchange service and Bardo remain authoritative.
    public static class NativeMaterialImports {
        static readonly MethodInfo clone=typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic);
        public static void Patch(Harmony harmony) {
            var target=AccessTools.Method(typeof(IndustryDataLoader),"CreateIndustry",new[]{typeof(string),typeof(IndustryDTO),typeof(Area)});
            if(target==null||target.IsStatic||target.ReturnType!=typeof(Industry))throw new MissingMethodException("Native industry creation contract");
            harmony.Patch(target,prefix:new HarmonyMethod(typeof(NativeMaterialImports),nameof(BeforeCreateIndustry)));
        }
        static void BeforeCreateIndustry(ref IndustryDTO __1){__1=Prepare(__1);}
        public static IndustryDTO Prepare(IndustryDTO source) {
            var interchanges=source?.components?.Values.OfType<InterchangeDTO>().ToArray();
            // The native supplier resolves one interchange per industry; do not invent
            // a connection for missing/ambiguous maps or an empty set of interchange tracks.
            if(interchanges==null||interchanges.Length!=1)return source;
            var spans=interchanges[0].trackSpans;
            if(spans==null||spans.Length==0||spans.Any(string.IsNullOrWhiteSpace))return source;
            var present=source.components.Values.OfType<InterchangedIndustryLoaderDTO>().Select(s=>s.loadId).ToArray();
            var missing=new[]{MaterialEconomy.BuildingLoad,MaterialEconomy.TechnologyLoad}.Where(id=>!present.Contains(id)).ToArray();
            if(missing.Length==0)return source;
            var result=(IndustryDTO)clone.Invoke(source,null);
            result.components=new OrdinalSortedDictionary<IIndustryChildDTO>(source.components);
            foreach(string load in missing) {
                bool technology=load==MaterialEconomy.TechnologyLoad;
                string stem=technology?"enusde-signal-technology":"enusde-signal-building",key=stem;
                int suffix=1;while(result.components.ContainsKey(key))key=stem+"-"+suffix++;
                result.components.Add(key,new InterchangedIndustryLoaderDTO {
                    name="EnusDE Signal & Interlocking",loadId=load,ledgerCategory=Ledger.Category.MaintenanceOfWay,
                    trackSpans=spans.ToArray(),carTypeFilter=technology?"XM":"FM,FL*"
                });
            }
            return result;
        }
    }
}
