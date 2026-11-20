using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using EnusDE.Signaltechnik;

static class NativeMaterialApiTests {
    static void Check(bool value,string text){if(!value)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    public static void Run(string root) {
        var game=Assembly.Load("Railroader");var load=game.GetType("Model.Ops.Definition.Load",true);
        var storage=game.GetType("Model.Ops.IndustryStorageHelper",true);
        const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        Check(storage.GetMethod("QuantityInStorage",new[]{load,typeof(string)})?.ReturnType==typeof(float)&&storage.GetMethod("SetStorage",new[]{load,typeof(float),typeof(string)})!=null,
            "installed native industry storage exposes the exact quantity and assignment API used for signal stocks");
        var component=game.GetType("Model.Ops.IndustryComponent",true);var prefix=component.GetProperty("StoragePrefix",flags);
        Check(prefix?.PropertyType==typeof(string)&&prefix.GetGetMethod(true)!=null&&game.GetType("Game.Mow.TrackConditionController",true).GetField("profile",flags)?.FieldType==game.GetType("Game.Mow.TrackMaintenanceProfile",true),
            "native MOW profile and storage-prefix contracts match the targeted warehouse integration");
        var loader=game.GetType("Model.Ops.StorageLoader",true);var unloader=game.GetType("Model.Ops.StorageUnloader",true);
        Check(loader.GetField("loads")!=null&&unloader.GetField("loads")!=null&&unloader.GetNestedType("LoadEntry").GetField("storage")?.FieldType==typeof(int)&&load.GetProperty("NominalQuantityPerCarLoad")?.PropertyType==typeof(float),
            "native stock capacity is expressed in carloads with a per-cargo nominal quantity");
        var equipment=typeof(UI.Builder.UIPanelBuilder).Assembly.GetType("UI.CompanyWindow.EquipmentPanelBuilder",true);
        Check(equipment.GetMethod("GetCaptiveLoadsForDefinition",flags)!=null&&equipment.GetField("_captiveLoadsByCarType",flags)!=null,
            "the actual game has native captive cargo selection and its language-sensitive label cache");
        var definitions=JObject.Parse(File.ReadAllText(Path.Combine(root,"mod","EnusDE-Signaltechnik","Definitions.json")));
        var rows=definitions["objects"].OfType<JObject>().ToArray();
        Check(rows.Length==2&&rows.All(r=>(bool)r["definition"]["importable"]&&(float)r["definition"]["costPerUnit"]>0)&&
            rows.Any(r=>(string)r["identifier"]==MaterialEconomy.BuildingLoad&&((string)r["definition"]["carTypeFilter"]).Contains("FM")&&((string)r["definition"]["carTypeFilter"]).Contains("FL*"))&&
            rows.Any(r=>(string)r["identifier"]==MaterialEconomy.TechnologyLoad&&(string)r["definition"]["carTypeFilter"]=="XM"),
            "both stable signal cargo IDs meet the native captive-order price, import and wagon-filter requirements");
    }
}
