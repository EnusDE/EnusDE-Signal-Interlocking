using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using EnusDE.Signaltechnik;

// Compile the production window against small UI adapters. Geometry/data code comes
// from the built mod assembly; callbacks and deferred rebuilds run in their real order.
namespace UnityEngine {
    public class Object {public static void Destroy(Object x){}}
    public class Component:Object {
        GameObject owner;public GameObject gameObject=>owner??(owner=new GameObject());
        public void Attach(GameObject x){owner=x;}public T GetComponent<T>() where T:Component,new()=>this is T t?t:gameObject.GetComponent<T>();
        public T[] GetComponents<T>()=>Array.Empty<T>();
    }
    public class MonoBehaviour:Component{}
    public class Transform:Component{}
    public class RectTransform:Transform {public Vector2 anchoredPosition;}
    public class GameObject:Object {
        readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
        public void SetActive(bool value){}
        public T AddComponent<T>() where T:Component,new(){var value=new T();value.Attach(this);components[typeof(T)]=value;return value;}
        public T GetComponent<T>() where T:Component,new()=>components.TryGetValue(typeof(T),out var value)?(T)value:AddComponent<T>();
    }
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);}
    public struct Vector2Int {public Vector2Int(int x,int y){}}
    public struct Color {public Color(float r,float g,float b,float a=1){}public static Color red=>new Color();}
}
namespace UnityEngine.UI {
    public class LayoutGroup:UnityEngine.Component {public bool enabled;}
    public class ContentSizeFitter:UnityEngine.Component {public bool enabled;}
    public class LayoutElement:UnityEngine.Component {public bool ignoreLayout;public float minWidth,preferredWidth,flexibleWidth,minHeight,preferredHeight,flexibleHeight;}
    public class RectMask2D:UnityEngine.Component{}
    public class Scrollbar:UnityEngine.Component{}
    public class ScrollRect:UnityEngine.Component {public enum MovementType{Clamped}public UnityEngine.RectTransform viewport,content;public Scrollbar horizontalScrollbar,verticalScrollbar;public bool horizontal,vertical,inertia;public MovementType movementType;}
}
namespace UI.Builder {
    public struct UIPanelBuilder {
        public enum Frequency{Periodic}
        public void VScrollView(Action<UIPanelBuilder> draw)=>draw(this);
        public UnityEngine.RectTransform VStack(Action<UIPanelBuilder> draw){draw(this);return new UnityEngine.RectTransform();}
        public UnityEngine.RectTransform HStack(Action<UIPanelBuilder> draw){draw(this);return new UnityEngine.RectTransform();}
        public void AddLabel(string value){}
        public void AddButton(string label,Action click){BlockViewActionsFixture.Buttons[label]=click;}
        public void AddField(string label,Func<string> read,Frequency frequency){BlockViewActionsFixture.Readers[label]=read;}
        public void AddField(string label,UnityEngine.RectTransform rect){BlockViewActionsFixture.Choices[label]=BlockViewActionsFixture.PendingChoices[rect];}
        public UnityEngine.RectTransform AddDropdown(List<string> options,int selected,Action<int> change){var rect=new UnityEngine.RectTransform();BlockViewActionsFixture.PendingChoices[rect]=new BlockViewActionsFixture.Choice{Index=selected,Change=change};return rect;}
        public void AddFieldToggle(string label,Func<bool> read,Action<bool> change,bool enabled){BlockViewActionsFixture.Toggles[label]=(read,change);_ = read();}
    }
}
namespace UI.Windows {
    public class Window{public enum Position{CenterRight}}
    public class WindowConfig {public static WindowConfig Resizable(UnityEngine.Vector2Int a,UnityEngine.Vector2Int b,UnityEngine.Vector2Int c,Window.Position p)=>new WindowConfig();}
    public class CustomWindowDefinition {public string Identifier,Title;public WindowConfig Config;public Action<UI.Builder.UIPanelBuilder> BuilderAction;}
    public class WindowManager {
        public CustomWindowDefinition Definition;public bool Open,Pending;public int Builds;
        public void RegisterCustomWindow(CustomWindowDefinition value){Definition=value;}
        public void CloseWindow(string id){Open=false;Pending=false;}
        public void Build(){if(!Open)return;Pending=false;Builds++;BlockViewActionsFixture.ClearControls();Definition.BuilderAction(new UI.Builder.UIPanelBuilder());}
        public void Flush(){if(Pending)Build();}
    }
    public class CustomWindowHandle {
        public static WindowManager Manager;public static CustomWindowHandle Create(string id)=>new CustomWindowHandle();
        public void SetNeedsRebuild(){if(Manager.Open)Manager.Pending=true;}
        public void Show(){Manager.Open=true;Manager.Build();}
    }
}
namespace EnusDE.Signaltechnik {
    public static class Entry {public static FixtureShared Shared;}
    public sealed class FixtureShared {
        public long ConfigurationVersion,InstallationVersion;public DeskLibrary Library;public InstallationDocument Installation;public bool ConfigurationReady=true;
        public DeskLibrary Desks()=>Library.Clone();
        public string ConstructionSignature=>ConstructionSnapshot.Signature(Installation);
        public void LiveNumber(){InstallationVersion++;Installation.TrainNumbers["car"]=InstallationVersion.ToString("D5");Installation.SignalNumberFields["11111111111111111111111111111111"]=InstallationVersion.ToString("D5");}
    }
    public static class WorkspaceSurface {
        public static UnityEngine.RectTransform Rect(UnityEngine.Transform parent,string name)=>new UnityEngine.RectTransform();
        public static void Fill(UnityEngine.RectTransform rect,float left,float top,float right,float bottom){}
    }
    public sealed class PanelView:UnityEngine.Component {
        public float Zoom=1;public Action<int,int> Hover;public Action<int,int,bool> Click;
        public void Initialize(PanelDocument doc,Func<int,int,PanelVisual> read,Action<int,int,bool> click,Func<bool> alive,Action<int,int,bool> right,Action<float> zoom,Func<PanelTile> draft,Func<PanelRegion> region,bool editor){Click=click;BlockViewActionsFixture.View=this;}
        public void PreviewCells(PanelTile[] parts,bool valid){}public void CancelMove(){}public void ReplaceDocument(PanelDocument value){}public void SetZoom(float value){Zoom=value;}public void AttachScroll(UnityEngine.UI.ScrollRect scroll){}
    }
    public struct PanelVisual {public UnityEngine.Color Rail,Lamp;public string Caption;public bool PointKnown,SetupActive;}
    public sealed class PanelViewportFit:UnityEngine.Component {public void Initialize(PanelView view,UnityEngine.RectTransform drawing,PanelDocument doc){}}
    public sealed partial class SignalRuntime:UnityEngine.MonoBehaviour {
        UI.Windows.WindowManager manager=new UI.Windows.WindowManager();DeskLibrary desks;string scope;LineBlockBuild blockBuild;LineIndicatorTarget lineIndicatorTarget;BlockLayoutPlan blockLayout;
        readonly HashSet<string> blockConfirmedPositions=new HashSet<string>();string status;public int Saves,PlacementOpens;public string LastError;
        string WindowText(string en,string de)=>en;void EnsureWindow(){}void RequirePanelEdit(){}
        void Act(Action action){LastError=null;try{action();}catch(Exception e){LastError=e.Message;}}
        void RefreshWorkspaceFields(){}
        void CommitNumberOverview(DeskLibrary next){next.Validate(next.WorldKey);desks=next;Entry.Shared.Library=next.Clone();Entry.Shared.ConfigurationVersion++;Saves++;}
        void OpenBlockPlacement(LineBlockBuild build,string target){PlacementOpens++;}
        string Code(string id)=>id;
        UnityEngine.UI.Scrollbar WorkspaceBar(UnityEngine.RectTransform root,bool vertical)=>new UnityEngine.UI.Scrollbar();
        public UI.Windows.WindowManager Manager=>manager;
        public void TestOpen(string line,string desk,LineBlockBuild build=null){desks=Entry.Shared.Library;scope=desks.WorldKey;blockBuild=build;UI.Windows.CustomWindowHandle.Manager=manager;OpenBlockViewEditor(line,desk,build);}
        public PanelArea CandidateArea()=>blockViewPreview==null?null:BlockDiagram.Area(blockViewPreview.Desks.Single(d=>d.Id==blockViewDesk).Panel,blockViewLine);
    }
}
static class BlockViewActionsFixture {
    public sealed class Choice {public int Index;public Action<int> Change;}
    public static Dictionary<string,Action> Buttons=new Dictionary<string,Action>();public static Dictionary<string,Func<string>> Readers=new Dictionary<string,Func<string>>();
    public static Dictionary<string,Choice> Choices=new Dictionary<string,Choice>();public static Dictionary<UnityEngine.RectTransform,Choice> PendingChoices=new Dictionary<UnityEngine.RectTransform,Choice>();
    public static Dictionary<string,(Func<bool> Read,Action<bool> Change)> Toggles=new Dictionary<string,(Func<bool>,Action<bool>)>();public static PanelView View;
    public static void ClearControls(){Buttons.Clear();Readers.Clear();Choices.Clear();PendingChoices.Clear();Toggles.Clear();View=null;}
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static string Status()=>Readers["View editor status"]();
    static void Main(string[] args){
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{string name=new AssemblyName(e.Name).Name+".dll";var p=Path.Combine(args[0],name);return File.Exists(p)?Assembly.LoadFrom(p):null;};Run();
    }
    static void Run(){
        string world=new string('a',64);var a=new DeskDefinition{Code="A",Name="A",Panel=new PanelDocument{WorldKey=world}};var b=new DeskDefinition{Code="B",Name="B",Panel=new PanelDocument{WorldKey=world}};
        var library=new DeskLibrary{WorldKey=world,Desks=new List<DeskDefinition>{new DeskDefinition{Global=true,Code="MASTER",Name="Master",Panel=new PanelDocument{WorldKey=world}},a,b}};
        var boundaries=Enumerable.Range(0,5).Select(_=>Guid.NewGuid().ToString("N")).ToArray();var blocks=new List<TrackBlock>();
        var line=new LineTrackDefinition{Code="STR",Name="A-B",StationA=a.Id,StationB=b.Id,BlockOwner=a.Id,BlockDisplays=new List<string>{b.Id}};
        for(int i=0;i<4;i++){var block=new TrackBlock{Id="block-"+i,Name="Block "+(i+1),BoundaryA=boundaries[i],BoundaryB=boundaries[i+1],Path=new List<DirectedRouteSection>{new DirectedRouteSection{Section=new TrackSection{Segment="track",From=i*100,To=(i+1)*100}}}};blocks.Add(block);library.Blocks[block.Id]=block;line.BlockSections.Add(block.Id);if(i<3)line.BlockUnits.Add(new BlockSignalUnit{Boundary=boundaries[i+1],Forward=Guid.NewGuid().ToString("N"),Reverse=Guid.NewGuid().ToString("N"),Distance=(i+1)*100});}
        library.FreeLines.Add(line);BlockDiagram.Rebuild(library,line,blocks);library.Validate(world);
        Entry.Shared=new FixtureShared{Library=library,Installation=new InstallationDocument{WorldKey=world,Map="map",Objects=new MarkerDocument{WorldKey=world,Map="map"}}};var runtime=new SignalRuntime();runtime.TestOpen(line.Id,b.Id);
        Check(Buttons.ContainsKey("Apply this view")&&runtime.CandidateArea()!=null&&Status().Contains("Preview ready"),"the production editor opens with a ready editable preview and reachable Apply");
        Entry.Shared.LiveNumber();Toggles["Rotate the complete assembly 180°"].Change(true);runtime.Manager.Flush();
        Check(Toggles["Rotate the complete assembly 180°"].Read(),"live number updates do not disable the actual rotation callback");
        foreach(var value in Choices.Values.ToArray())value.Change(value.Index);foreach(var value in Toggles.Values.ToArray())value.Change(value.Read());
        Check(!runtime.Manager.Pending,"same-value UI notifications do not invalidate the freshly built callbacks");
        Buttons["Refresh preview"]();runtime.Manager.Flush();Check(runtime.CandidateArea()!=null&&Status().Contains("Preview ready"),"explicit preview refresh leaves the production editor ready after number traffic and rebuild");
        Entry.Shared.LiveNumber();int builds=runtime.Manager.Builds;View.Click(4,5,false);
        Check(runtime.CandidateArea().X==4&&runtime.CandidateArea().Y==5&&runtime.Manager.Builds==builds&&Readers["Placement status"]().Contains("Position set"),"the production placement callback retains its requested position without rebuilding after another live-number update");
        Choices["First section (A → B)"].Change(3);runtime.Manager.Flush();
        Check(runtime.CandidateArea()!=null&&runtime.CandidateArea().X==4&&runtime.CandidateArea().Y==5&&runtime.CandidateArea().Width==7,"changing the rotated view to the fourth section keeps its staged position and permits continued placement without another preparation step");
        var oldPlace=View.Click;var oldSave=Buttons["Apply this view"];for(int i=0;i<5;i++)Entry.Shared.LiveNumber();Buttons["Apply this view"]();runtime.Manager.Flush();
        var saved=BlockDiagram.Area(Entry.Shared.Library.Desks.Single(d=>d.Id==b.Id).Panel,line.Id);
        Check(runtime.Saves==1&&runtime.Manager.Open&&saved.X==4&&saved.Y==5&&saved.BlockView.Turn180&&Entry.Shared.Installation.TrainNumbers["car"]=="00007"&&Status().Contains("View saved"),"the production Apply callback saves position and rotation, retains current numbers and rebases the still-open editor");
        oldPlace(12,10,false);oldSave();Check(runtime.Saves==1&&runtime.CandidateArea().X==4&&runtime.CandidateArea().Y==5,"callbacks from before saving cannot modify the newly rebased editor");
        Buttons["Move right"]();Buttons["Move down"]();Buttons["Apply this view"]();runtime.Manager.Flush();
        saved=BlockDiagram.Area(Entry.Shared.Library.Desks.Single(d=>d.Id==b.Id).Panel,line.Id);
        Check(runtime.Saves==2&&saved.X==5&&saved.Y==6&&runtime.CandidateArea()!=null,"another move and save work immediately in the same window after the first save");
        Buttons["Close without applying"]();runtime.TestOpen(line.Id,b.Id);
        Check(runtime.CandidateArea()!=null&&runtime.CandidateArea().X==5&&runtime.CandidateArea().Y==6&&runtime.CandidateArea().BlockView.FirstSection==3&&runtime.CandidateArea().BlockView.Turn180,"reopening a saved rotated fourth-section view restores a fresh movable candidate at its saved position");
        Choices["Display detail"].Change(2);runtime.Manager.Flush();Buttons["Move right"]();Buttons["Apply this view"]();runtime.Manager.Flush();Buttons["Close without applying"]();
        saved=BlockDiagram.Area(Entry.Shared.Library.Desks.Single(d=>d.Id==b.Id).Panel,line.Id);
        Check(runtime.Saves==3&&saved.X==6&&saved.Y==6&&saved.Width==1&&saved.BlockView.Detail==BlockViewDetail.NumbersOnly,"after reopening, changing to numbers-only, moving and saving all work without a separate preparation step");
        for(int cycle=0;cycle<3;cycle++) {
            Entry.Shared.Library=Newtonsoft.Json.JsonConvert.DeserializeObject<DeskLibrary>(Newtonsoft.Json.JsonConvert.SerializeObject(Entry.Shared.Library));Entry.Shared.ConfigurationVersion++;
            runtime.TestOpen(line.Id,b.Id);int previousX=runtime.CandidateArea().X,previousY=runtime.CandidateArea().Y;Choices["Display detail"].Change(cycle);runtime.Manager.Flush();
            Choices["First section (A → B)"].Change(cycle==1?0:3);runtime.Manager.Flush();Toggles["Rotate the complete assembly 180°"].Change(cycle%2==0);runtime.Manager.Flush();
            Check(runtime.CandidateArea().X==previousX&&runtime.CandidateArea().Y==previousY,"changing detail/range in reopened cycle "+(cycle+1)+" preserves the staged position, growing the panel when needed");
            Buttons["Move right"]();int chosen=runtime.CandidateArea().X;Buttons["Apply this view"]();runtime.Manager.Flush();Buttons["Close without applying"]();runtime.TestOpen(line.Id,b.Id);
            Check(runtime.CandidateArea()!=null&&runtime.CandidateArea().X==chosen&&runtime.CandidateArea().BlockView.Detail==(BlockViewDetail)cycle,"save/reload/reopen cycle "+(cycle+1)+" retains the edited view and allows a fresh placement");Buttons["Close without applying"]();
        }
        runtime.TestOpen(line.Id,b.Id);int savedCount=runtime.Saves;var oldApply=Buttons["Apply this view"];var oldClick=View.Click;Choices["Display detail"].Change(0);oldApply();oldClick(20,10,false);runtime.Manager.Flush();
        Check(runtime.Saves==savedCount&&runtime.CandidateArea()!=null,"deferred old controls cannot change or save a candidate after a new display setting is selected");
        var oldBuiltApply=Buttons["Apply this view"];runtime.Manager.Build();oldBuiltApply();
        Check(runtime.Saves==savedCount,"a replaced native window render invalidates the old Apply callback even when the option generation is unchanged");
        Entry.Shared.ConfigurationVersion++;Buttons["Refresh preview"]();Check(runtime.Saves==savedCount&&Status().Contains("configuration changed"),"a genuine configuration edit is still rejected with visible window feedback");Buttons["Close without applying"]();
        runtime.TestOpen(line.Id,b.Id);Entry.Shared.Installation.Placement.RightSide=!Entry.Shared.Installation.Placement.RightSide;Entry.Shared.InstallationVersion++;Buttons["Refresh preview"]();Check(Status().Contains("construction changed"),"a genuine construction setting change still invalidates the production preview action visibly");Buttons["Close without applying"]();
        runtime.TestOpen(line.Id,b.Id);Entry.Shared.ConfigurationReady=false;Buttons["Refresh preview"]();Check(Status().Contains("not ready")&&runtime.Saves==savedCount,"temporarily unavailable data is explained without discarding the edit session");Entry.Shared.ConfigurationReady=true;Buttons["Refresh preview"]();runtime.Manager.Flush();Check(runtime.CandidateArea()!=null,"preview preparation recovers after data readiness returns");Buttons["Close without applying"]();
        Entry.Shared.Library=BlockDiagramViews.Edit(Entry.Shared.Library,line.Id,b.Id,new BlockViewOptions());
        var display=Entry.Shared.Library.Desks.Single(d=>d.Id==b.Id).Panel;var group=BlockDiagram.Area(display,line.Id);display.Put(new PanelTile{X=group.X+1,Y=group.Y,Kind=PanelTileKind.Label,Label="manual content"});Entry.Shared.ConfigurationVersion++;
        runtime.TestOpen(line.Id,b.Id);Buttons["Refresh preview"]();runtime.Manager.Flush();Check(Status().Contains("manually added")&&Buttons.ContainsKey("Apply this view")&&runtime.Saves==savedCount,"an invalid source layout is explained without erasing manual content or disabling the settings controls");
        Buttons["Apply this view"]();Check(runtime.Saves==savedCount&&Status().Contains("manually added"),"Apply without a valid preview remains a visible guarded action and never saves an invalid layout");Buttons["Close without applying"]();display.Tiles.RemoveAll(t=>t.Label=="manual content");Entry.Shared.ConfigurationVersion++;
        var build=new LineBlockBuild{Library=Entry.Shared.Library.Clone(),Line=line.Clone(),Objects=Entry.Shared.Installation.Objects};runtime.TestOpen(line.Id,b.Id,build);Entry.Shared.LiveNumber();Buttons["Refresh preview"]();runtime.Manager.Flush();Buttons["Apply this view"]();
        Check(runtime.PlacementOpens==1&&runtime.Saves==savedCount&&BlockDiagram.Area(build.Library.Desks.Single(d=>d.Id==b.Id).Panel,line.Id)!=null,"the pending-build flow stages its view and returns to construction placement without prematurely saving");
    }
}
