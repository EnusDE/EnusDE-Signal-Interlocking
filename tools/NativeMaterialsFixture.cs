using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnusDE.Signaltechnik;

// Production native-storage adapter and transfer callbacks with controlled game
// stand-ins. Actual API metadata is checked separately against the installed DLLs.
namespace HarmonyLib {
    public static class AccessTools {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        public static MethodInfo PropertyGetter(Type t,string n)=>t.GetProperty(n,All)?.GetGetMethod(true);
        public static FieldInfo Field(Type t,string n)=>t.GetField(n,All);
        public static MethodInfo Method(Type t,string n,Type[] args)=>t.GetMethod(n,All,null,args,null);
    }
    public sealed class HarmonyMethod {public HarmonyMethod(Type t,string n){}}
    public sealed class Harmony {public Harmony(string id){}public void Patch(MethodInfo method,HarmonyMethod postfix){}public void UnpatchAll(string id){}}
}
namespace Game.State {
    public sealed class Session {public bool IsSessionReady=true,HasRestoredProperties=true;}
    public static class StateManager {public static Session Shared=new Session();}
}
namespace Game.Mow {
    public sealed class TrackConditionController {public static TrackConditionController Shared;public TrackMaintenanceProfile profile;}
    public sealed class TrackMaintenanceProfile {
        public struct LoadUnloadRate {public string loadId;}
        public struct LoadRequirement {public string loadId;}
        public struct ClassRequirementsByDifficulty {public LoadRequirement[] low,medium,high;}
        public struct ConstructionRequirement {public ClassRequirementsByDifficulty[] requirementsByClass;}
        public LoadUnloadRate[] unloadRates;public ConstructionRequirement[] constructionRequirements;
    }
}
namespace Model.Ops.Definition {
    public sealed class Load {public string id;public string description=>id;public float NominalQuantityPerCarLoad=10;}
    public static class CarLoadLibrary {public static readonly Dictionary<string,Load> Loads=new Dictionary<string,Load>();public static Load GetLoad(string id)=>Loads.TryGetValue(id,out var load)?load:null;}
}
namespace Model.Ops {
    public sealed class CarTypeFilter {public string queryString;public CarTypeFilter(string value){queryString=value;}public bool Matches(string value)=>queryString.Split(',').Any(p=>p==value||p=="*"||p.EndsWith("*")&&value.StartsWith(p.TrimEnd('*')));}
    public class IndustryComponent {public string Identifier;public string Prefix;public CarTypeFilter carTypeFilter=new CarTypeFilter("FL*,XM*");protected string StoragePrefix=>Prefix;}
    public sealed class StorageLoader:IndustryComponent {public sealed class LoadEntry {public string loadId;public int carLoadRate;}public List<LoadEntry> loads=new List<LoadEntry>();}
    public sealed class StorageUnloader:IndustryComponent {public sealed class LoadEntry {public string loadId;public int storage,unloadRate;}public List<LoadEntry> loads=new List<LoadEntry>();}
    public sealed class IndustryStorageHelper {
        readonly Dictionary<string,float> stock=new Dictionary<string,float>();
        public float QuantityInStorage(Definition.Load load,string prefix)=>stock.TryGetValue((prefix??"")+":"+load.id,out var value)?value:0;
        public void SetStorage(Definition.Load load,float value,string prefix){stock[(prefix??"")+":"+load.id]=value;}
    }
    public sealed class Industry {public string DisplayName;public UnityEngine.Vector3 Position;public List<IndustryComponent> Components=new List<IndustryComponent>();public IndustryStorageHelper Storage=new IndustryStorageHelper();}
    public sealed class OpsController {public static OpsController Shared;public Industry[] AllIndustries;}
}
namespace UnityEngine {
    public struct Vector3 {public float x;public Vector3(float value){x=value;}public static float Distance(Vector3 a,Vector3 b)=>Math.Abs(a.x-b.x);}
}
namespace Track {
    public sealed class Node {public UnityEngine.Vector3 Position;}
    public sealed class Graph {public static Graph Shared=new Graph();public readonly Dictionary<string,Node> Nodes=new Dictionary<string,Node>();public Node GetNode(string id)=>Nodes.TryGetValue(id,out var node)?node:null;}
}
namespace UI.Builder {public struct UIPanelBuilder{}public sealed class UIState<T> {public T Value;}}
namespace UI.CompanyWindow {
    public static class EquipmentPanelBuilder {public static Dictionary<string,List<string>> _captiveLoadsByCarType=new Dictionary<string,List<string>>();}
    public sealed class CompanyWindow {
        public static CompanyWindow Shared=new CompanyWindow();UI.Builder.UIState<string> _selectedTabState=new UI.Builder.UIState<string>();
        public string Selected=>_selectedTabState.Value;public int Shows;void Show(){Shows++;}
    }
}
namespace EnusDE.Signaltechnik {
    public static class MaterialEconomy {public const string BuildingLoad="enusde/signal-building",TechnologyLoad="enusde/signal-technology";public static void Debit(MaterialDepot d,MaterialCost c){if(d.Stock.B<c.B||d.Stock.T<c.T)throw new InvalidOperationException();d.Stock.B-=c.B;d.Stock.T-=c.T;}}
    public sealed class MaterialCost {public int B,T;public MaterialCost(){}public MaterialCost(int b,int t){B=b;T=t;}}
    public sealed class MarkerAnchor {public UnityEngine.Vector3 Position;}
    public sealed class MaterialDepot {public string Id;public MarkerAnchor Anchor=new MarkerAnchor();public MaterialCost Stock=new MaterialCost();}
    public sealed class MaterialLedger {public bool Enabled=true;public double Factor=1;public List<string> ConnectedPoints=new List<string>();public List<MaterialDepot> Depots=new List<MaterialDepot>();}
    public sealed class InstallationDocument {
        public MaterialLedger Economy=new MaterialLedger();
        public InstallationDocument Clone(){var next=new InstallationDocument();next.Economy.Enabled=Economy.Enabled;next.Economy.Factor=Economy.Factor;next.Economy.ConnectedPoints=Economy.ConnectedPoints.ToList();foreach(var d in Economy.Depots)next.Economy.Depots.Add(new MaterialDepot{Id=d.Id,Anchor=d.Anchor,Stock=new MaterialCost{B=d.Stock.B,T=d.Stock.T}});return next;}
        public void Validate(){}
    }
    public static class SharedDocuments {public static string Hash(byte[] data)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data));}
    public sealed partial class SharedRuntime {
        public InstallationDocument installation=new InstallationDocument();public string WorldKey="world";public bool Host=true,FailCommit;
        public List<NativeMaterialWarehouse> MaterialWarehouses=new List<NativeMaterialWarehouse>();
        const float WorkRadius=250;
        static UnityEngine.Vector3 Position(MarkerAnchor anchor)=>anchor.Position;
        public void Connect(params string[] ids)=>WithConnectionMaterials(ids,CommitInstall);
        readonly Dictionary<string,string> receipts=new Dictionary<string,string>();
        void RequireReady(){if(!Host)throw new InvalidOperationException("host required");}
        string Read(string key)=>receipts.TryGetValue(key,out var value)?value:null;
        void Write(string key,string value){if(value==null)receipts.Remove(key);else receipts[key]=value;}
        void CommitInstall(InstallationDocument value){if(FailCommit)throw new InvalidOperationException("fixture commit failure");installation=value;}
        void Atomic(Action action,Action rollback){var old=installation;try{action();}catch{installation=old;rollback();throw;}}
    }
}
public static class NativeMaterialsFixture {
    static void Check(bool value,string text){if(!value)throw new Exception("FAIL: "+text);Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static Model.Ops.Industry Store(string name,string input="rr/rails",string output="rr/rails",string unloadPrefix=null) {
        var industry=new Model.Ops.Industry{DisplayName=name};
        var loader=new Model.Ops.StorageLoader{Identifier=name+"/load",Prefix=null};loader.loads.Add(new Model.Ops.StorageLoader.LoadEntry{loadId=output,carLoadRate=7});
        var unloader=new Model.Ops.StorageUnloader{Identifier=name+"/unload",Prefix=unloadPrefix};unloader.loads.Add(new Model.Ops.StorageUnloader.LoadEntry{loadId=input,storage=2,unloadRate=9});
        industry.Components.Add(loader);industry.Components.Add(unloader);return industry;
    }
    public static void Main() {
        LocalText.Configure(()=>"en");
        foreach(string id in new[]{MaterialEconomy.BuildingLoad,MaterialEconomy.TechnologyLoad,"rr/rails","rr/steel","coal"})Model.Ops.Definition.CarLoadLibrary.Loads[id]=new Model.Ops.Definition.Load{id=id};
        Model.Ops.Definition.CarLoadLibrary.Loads[MaterialEconomy.TechnologyLoad].NominalQuantityPerCarLoad=5;
        var yard=Store("MOW");var coal=Store("coal","coal","coal");var mill=Store("mill","rr/steel","rr/rails");var mismatch=Store("split",unloadPrefix:"separate");
        Model.Ops.OpsController.Shared=new Model.Ops.OpsController{AllIndustries=new[]{yard,coal,mill,mismatch}};
        Game.Mow.TrackConditionController.Shared=new Game.Mow.TrackConditionController{profile=new Game.Mow.TrackMaintenanceProfile{unloadRates=new[]{new Game.Mow.TrackMaintenanceProfile.LoadUnloadRate{loadId="rr/rails"},new Game.Mow.TrackMaintenanceProfile.LoadUnloadRate{loadId="rr/steel"}}}};
        var adapter=new NativeMaterialLogistics();adapter.Start();Game.State.StateManager.Shared.IsSessionReady=false;adapter.Refresh();
        Check(adapter.Warehouses.Count==0&&yard.Components.OfType<Model.Ops.StorageLoader>().Single().loads.Count==1,"native warehouse registration waits for session restoration");
        Game.State.StateManager.Shared.IsSessionReady=true;adapter.Refresh();
        Check(adapter.Error==null&&adapter.Warehouses.Count==1&&adapter.Warehouses[0].Name=="MOW","only a matching MOW material loader/unloader pair with shared storage is extended");
        var loader=yard.Components.OfType<Model.Ops.StorageLoader>().Single();var unloader=yard.Components.OfType<Model.Ops.StorageUnloader>().Single();var warehouse=adapter.Warehouses[0];
        Check(loader.loads.Count==3&&unloader.loads.Count==3&&coal.Components.OfType<Model.Ops.StorageLoader>().Single().loads.Count==1&&mill.Components.OfType<Model.Ops.StorageLoader>().Single().loads.Count==1,
            "exactly the two signal cargos are added while unrelated industrial storage is untouched");
        Check(loader.carTypeFilter.Matches("FL")&&loader.carTypeFilter.Matches("FM")&&unloader.carTypeFilter.Matches("FM")&&!coal.Components.OfType<Model.Ops.StorageLoader>().Single().carTypeFilter.Matches("FM"),"native MOW flat cars and existing FM signal-material cars can use the same targeted warehouse");
        Check(warehouse.Capacity(false)==20&&warehouse.Capacity(true)==10&&loader.loads[0].carLoadRate==7&&unloader.loads[0].storage==2,
            "native carload storage capacities convert to each cargo's real quantity without changing original rates or capacity");
        warehouse.Set(false,7.5f);warehouse.Set(true,1.25f);adapter.Refresh();
        Check(loader.loads.Count==3&&warehouse.Quantity(false)==7.5f&&warehouse.Quantity(true)==1.25f,"repeated registration is idempotent and never creates or duplicates stock");
        var foreign=new Model.Ops.StorageLoader.LoadEntry{loadId="foreign",carLoadRate=99};loader.loads.Add(foreign);
        UI.CompanyWindow.EquipmentPanelBuilder._captiveLoadsByCarType["FM"]=new List<string>{"old label"};LocalText.Configure(()=>"de");adapter.Refresh();
        Check(UI.CompanyWindow.EquipmentPanelBuilder._captiveLoadsByCarType.Count==0&&warehouse.Quantity(false)==7.5f,"language changes refresh native order labels without changing stock");
        NativeMaterialLogistics.OpenOrders();
        Check(UI.CompanyWindow.CompanyWindow.Shared.Selected=="equipment"&&UI.CompanyWindow.CompanyWindow.Shared.Shows==1,"the order shortcut opens the existing Equipment page without replacing native menu content");
        var shared=new SharedRuntime();shared.MaterialWarehouses.Add(warehouse);shared.installation.Economy.Depots.Add(new MaterialDepot{Id="old",Stock=new MaterialCost{B=15,T=12}});
        shared.Host=false;
        Check(Reject(()=>shared.TransferLegacyStock("old",warehouse.Id))&&warehouse.Quantity(false)==7.5f,"a client cannot transfer shared material stock");
        shared.Host=true;shared.FailCommit=true;
        Check(Reject(()=>shared.TransferLegacyStock("old",warehouse.Id))&&warehouse.Quantity(false)==7.5f&&warehouse.Quantity(true)==1.25f&&shared.installation.Economy.Depots[0].Stock.B==15,
            "a failed installation commit rolls back native quantities, legacy stock and transfer receipt");
        shared.FailCommit=false;shared.TransferLegacyStock("old",warehouse.Id);
        Check(warehouse.Quantity(false)==19.5f&&warehouse.Quantity(true)==9.25f&&shared.installation.Economy.Depots[0].Stock.B==3&&shared.installation.Economy.Depots[0].Stock.T==4,
            "partial legacy transfer preserves fractions and leaves all excess stock in the original store");
        Check(Reject(()=>shared.TransferLegacyStock("old",warehouse.Id))&&shared.installation.Economy.Depots[0].Stock.B==3,"a full native warehouse never discards remaining legacy packages");
        warehouse.Set(false,0);warehouse.Set(true,0);shared.TransferLegacyStock("old",warehouse.Id);
        Check(warehouse.Quantity(false)==3&&warehouse.Quantity(true)==4&&shared.installation.Economy.Depots[0].Stock.B==0&&shared.installation.Economy.Depots[0].Stock.T==0,
            "a later transfer can move the recorded remainder after normal warehouse consumption");
        shared.installation.Economy.Depots[0].Stock.B=15;
        Check(Reject(()=>shared.TransferLegacyStock("old",warehouse.Id))&&warehouse.Quantity(false)==3,"restoring an older configuration cannot credit an already transferred legacy balance again");
        var connections=new SharedRuntime();connections.MaterialWarehouses.Add(warehouse);warehouse.Set(false,8);warehouse.Set(true,8);Track.Graph.Shared.Nodes["P1"]=new Track.Node();Track.Graph.Shared.Nodes["P2"]=new Track.Node();
        connections.Connect("P1","P1");
        Check(warehouse.Quantity(false)==7&&warehouse.Quantity(true)==6&&connections.installation.Economy.ConnectedPoints.Count==1,"first point connections debit native stock once even for duplicate requested IDs");
        connections.Connect("P1");
        Check(warehouse.Quantity(false)==7&&warehouse.Quantity(true)==6,"previously wired points are not charged again when routes are saved");
        connections.FailCommit=true;
        Check(Reject(()=>connections.Connect("P2"))&&warehouse.Quantity(false)==7&&warehouse.Quantity(true)==6&&!connections.installation.Economy.ConnectedPoints.Contains("P2"),"failed route configuration restores both native material balances and point accounting");
        connections.FailCommit=false;Track.Graph.Shared.Nodes["far"]=new Track.Node{Position=new UnityEngine.Vector3(500)};
        Check(Reject(()=>connections.Connect("far"))&&warehouse.Quantity(false)==7,"a distant MOW warehouse cannot be charged for an unreachable point connection");
        warehouse.Set(false,3);warehouse.Set(true,4);
        adapter.Dispose();
        Check(loader.loads.Count==2&&loader.loads.Contains(foreign)&&unloader.loads.Count==1&&warehouse.Quantity(false)==3,"unload removes only owned storage entries and keeps native stock and other contributions");
        Check(!loader.carTypeFilter.Matches("FM")&&loader.carTypeFilter.Matches("FL"),"warehouse wagon filters return to their previous value on unload");
        Check(NativeMaterialAmounts.Packages(4.9f)==4&&NativeMaterialAmounts.FitTransfer(8.5f,4,10)==1,"native quantity conversion never invents fractional construction packages");
        Check(Reject(()=>NativeMaterialAmounts.Transfer(float.NaN,1,20))&&Reject(()=>NativeMaterialAmounts.Transfer(19,2,20))&&Reject(()=>NativeMaterialAmounts.Transfer(1,1,float.PositiveInfinity)),
            "invalid or overflowing storage amounts are rejected before mutation");
    }
}
