using System;
using System.Linq;
using System.Reflection;
using Core;
using EnusDE.Signaltechnik;
using Game.History;
using Model.Ops;
using Model.Ops.Data;

static class NativeImportTests {
    static void Check(bool value,string message){if(!value)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static IndustryDTO Yard()=>new IndustryDTO{name="Original interchange",usesContract=true,components=new OrdinalSortedDictionary<IIndustryChildDTO>{
        {"exchange",new InterchangeDTO{trackSpans=new[]{"yard-a","yard-b"}}},
        {"fuel",new InterchangedIndustryLoaderDTO{loadId="rr/coal",name="Original supplier",trackSpans=new[]{"yard-a"}}}
    }};
    public static void Run() {
        var warehouse=new IndustryDTO{components=new OrdinalSortedDictionary<IIndustryChildDTO>()};
        Check(ReferenceEquals(NativeMaterialImports.Prepare(warehouse),warehouse)&&NativeMaterialImports.Prepare(null)==null,"native import preparation leaves non-interchange industries and missing data unchanged");
        var source=Yard();var prepared=NativeMaterialImports.Prepare(source);
        var suppliers=prepared.components.Values.OfType<InterchangedIndustryLoaderDTO>().Where(s=>s.name=="EnusDE Signal & Interlocking").ToArray();
        Check(suppliers.Length==2&&suppliers.Any(s=>s.loadId==MaterialEconomy.BuildingLoad&&s.carTypeFilter=="FM,FL*")&&suppliers.Any(s=>s.loadId==MaterialEconomy.TechnologyLoad&&s.carTypeFilter=="XM")&&suppliers.All(s=>s.ledgerCategory==Ledger.Category.MaintenanceOfWay),"native interchange suppliers expose both exact cargos with compatible wagons and the MOW ledger category");
        Check(source.components.Count==2&&prepared.components.Count==4&&prepared.name==source.name&&prepared.usesContract&&ReferenceEquals(source.components["fuel"],prepared.components["fuel"]),"supplier injection preserves original map data, native suppliers and industry settings without modifying cached DTOs");
        var nativeSpans=((InterchangeDTO)source.components["exchange"]).trackSpans;
        Check(suppliers.All(s=>s.trackSpans.SequenceEqual(nativeSpans)&&!ReferenceEquals(s.trackSpans,nativeSpans)),"new suppliers inherit the real interchange track names with isolated arrays rather than invented geometry");
        Check(ReferenceEquals(prepared,NativeMaterialImports.Prepare(prepared)),"repeated industry preparation does not duplicate native material suppliers");
        source=Yard();var existing=new InterchangedIndustryLoaderDTO{loadId=MaterialEconomy.BuildingLoad,name="Other mod"};source.components.Add("existing-building",existing);
        prepared=NativeMaterialImports.Prepare(source);
        Check(prepared.components.Values.OfType<InterchangedIndustryLoaderDTO>().Count(s=>s.loadId==MaterialEconomy.BuildingLoad)==1&&ReferenceEquals(prepared.components["existing-building"],existing),"an existing supplier for a signal cargo is preserved instead of overwritten or duplicated");
        source=Yard();source.components.Add("enusde-signal-building",new InterchangedIndustryLoaderDTO{loadId="foreign"});prepared=NativeMaterialImports.Prepare(source);
        Check(prepared.components.ContainsKey("enusde-signal-building-1")&&((InterchangedIndustryLoaderDTO)prepared.components["enusde-signal-building"]).loadId=="foreign","a foreign component occupying the preferred key retains its identity and receives no replacement");
        source=Yard();source.components.Add("second",new InterchangeDTO{trackSpans=new[]{"other"}});
        Check(ReferenceEquals(source,NativeMaterialImports.Prepare(source)),"ambiguous multiple interchanges are not assigned invented material purchase paths");
        source=Yard();((InterchangeDTO)source.components["exchange"]).trackSpans=Array.Empty<string>();
        Check(ReferenceEquals(source,NativeMaterialImports.Prepare(source)),"interchanges without usable track spans receive no invalid suppliers");
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        var create=typeof(IndustryDataLoader).GetMethod("CreateIndustry",flags,null,new[]{typeof(string),typeof(IndustryDTO),typeof(Area)},null);
        Check(create!=null&&!create.IsStatic&&create.ReturnType==typeof(Industry)&&typeof(InterchangedIndustryLoader).GetMethod("ServeInterchange",flags)!=null&&typeof(Interchange).GetProperty("Loaders",flags)?.PropertyType==typeof(InterchangedIndustryLoader[]),"installed game exposes the exact pre-indexing industry hook and native interchange supplier/service contracts");
        var prefix=typeof(NativeMaterialImports).GetMethod("BeforeCreateIndustry",BindingFlags.Static|BindingFlags.NonPublic);
        var args=new object[]{Yard()};prefix.Invoke(null,args);
        Check(((IndustryDTO)args[0]).components.Values.OfType<InterchangedIndustryLoaderDTO>().Count(s=>s.loadId==MaterialEconomy.TechnologyLoad)==1,"the actual Harmony prefix replaces its by-reference DTO with the prepared supplier definitions");
    }
}
