using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using EnusDE.Signaltechnik;

static class DiagramFieldSetupTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static string Id()=>Guid.NewGuid().ToString("N");
    public static void Run() {
        string key=new string('e',64);var library=new DeskLibrary{WorldKey=key,Desks=new List<DeskDefinition>{new DeskDefinition{Code="MASTER",Name="Master",Global=true,Panel=new PanelDocument{WorldKey=key}},new DeskDefinition{Code="AJ",Name="Alarka Junction",Panel=new PanelDocument{WorldKey=key}},new DeskDefinition{Code="ELA",Name="Ela",Panel=new PanelDocument{WorldKey=key}}}};
        var desk=library.Desks[1];var panel=desk.Panel;
        panel.Put(new PanelTile{X=8,Y=3,Kind=PanelTileKind.Signal,Label="Signal A",Ports=17});
        panel.Put(new PanelTile{X=4,Y=2,Kind=PanelTileKind.Switch,Label="Weiche ?"});
        panel.Put(new PanelTile{X=3,Y=2,Kind=PanelTileKind.Signal,Label="Signal",Ports=17});
        panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Straight,Ports=17});
        panel.Put(new PanelTile{X=2,Y=1,Kind=PanelTileKind.Signal,Reference=Id()});
        panel.Put(new PanelTile{X=3,Y=1,Kind=PanelTileKind.Signal,ReadOnlySignal=true});
        panel.Put(new PanelTile{X=4,Y=1,Kind=PanelTileKind.TrainNumber,NumberMirror=Id()});
        library.Desks[2].Panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Signal});
        var fields=DiagramFieldSetup.Pending(panel);
        Check(fields.Length==3&&fields.Select(t=>(t.X,t.Y)).SequenceEqual(new[]{(3,2),(4,2),(8,3)}),"diagram setup enumerates only its own unassigned signal/point symbols in visual row order rather than global object-name order");
        Check(!DiagramFieldSetup.Eligible(null)&&!DiagramFieldSetup.Eligible(panel.At(1,1))&&!DiagramFieldSetup.Eligible(panel.At(2,1))&&!DiagramFieldSetup.Eligible(panel.At(3,1))&&!DiagramFieldSetup.Eligible(panel.At(4,1)),"empty cells, plain rails, assigned heads and read-only/number displays do not masquerade as open setup targets");
        var target=DiagramFieldTarget.Capture(library,desk.Id,8,3);string before=JsonConvert.SerializeObject(library);
        Check(target.Cell=="I4"&&target.Snapshot.Label=="Signal A"&&!target.Point&&target.Current(library),"clicking a particular drawn signal records its exact cell and caption before choosing any world object");
        Check(JsonConvert.SerializeObject(library)==before,"choosing a diagram target alone makes no persistent assignment");
        var signal=new MarkerRecord{Id=Id(),Name="HS-287",Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="actual-track",Meters=56}};
        var source=new SetupObject{Id=signal.Id,Caption=signal.Name,Anchor=signal.Anchor.Clone()};
        Check(Reject(()=>DiagramFieldSetup.Assign(library,target,new SetupObject{Id="world-point",Point=true},null)),"a point cannot be accepted for the currently green signal field");
        var next=DiagramFieldSetup.Assign(library,target,source,signal);var assigned=next.Desks[1].Panel.At(8,3);
        Check(assigned.Reference==signal.Id&&assigned.Label=="Signal A"&&next.Desks[1].Panel.At(3,2).Reference==""&&JsonConvert.SerializeObject(library)==before,"world picking binds the selected cell, preserves its custom caption and never assigns a different open field by name");
        Check(DiagramFieldSetup.Pending(next.Desks[1].Panel).Select(t=>(t.X,t.Y)).SequenceEqual(new[]{(3,2),(4,2)}),"after confirmation the assigned field leaves the local queue and the next drawing field can be marked");
        var pointTarget=DiagramFieldTarget.Capture(next,desk.Id,4,2);
        Check(pointTarget.Point&&Reject(()=>DiagramFieldSetup.Assign(next,pointTarget,source,signal)),"a green point field permits the native point picker and rejects a signal result");
        next=DiagramFieldSetup.Assign(next,pointTarget,new SetupObject{Id="real-point",Point=true,Caption="W17"},null);
        Check(next.Points["real-point"].Desk==desk.Id&&next.Desks[1].Panel.At(4,2).Reference=="real-point","native point selection updates the exact schematic point and its controlling desk");
        var second=DiagramFieldTarget.Capture(next,desk.Id,3,2);var distant=signal.Clone();distant.Id=Id();distant.Name="VS-14";distant.Kind=MarkerRecord.Distant;
        next=DiagramFieldSetup.Assign(next,second,new SetupObject{Id=distant.Id},distant);
        Check(DiagramFieldSetup.Pending(next.Desks[1].Panel).Length==0&&next.Desks[1].Panel.At(3,2).Label==""&&next.Desks[1].Panel.At(3,2).Reference==distant.Id,"distant signals remain selectable and generic placeholders clear while the final local field completes");
        var reloaded=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(next));
        Check(DiagramFieldSetup.Pending(reloaded.Desks[1].Panel).Length==0&&DiagramFieldSetup.Pending(reloaded.Desks[2].Panel).Length==1,"saved local progress resumes independently of unassigned symbols at other interlockings");
        var changed=library.Clone();EditorCanvasGeometry.Insert(changed.Desks[1].Panel,true,0,1);
        Check(!target.Current(changed)&&Reject(()=>DiagramFieldSetup.Assign(changed,target,source,signal)),"row insertion during world selection invalidates the old target instead of binding to a shifted or lookalike cell");
        changed=library.Clone();changed.Desks[1].Panel.Columns++;
        Check(!target.Current(changed)&&Reject(()=>DiagramFieldSetup.Assign(changed,target,source,signal)),"layout extension also invalidates a pending target and requires an explicit fresh click");
        changed=library.Clone();changed.WorldKey=new string('f',64);
        Check(!target.Current(changed)&&Reject(()=>DiagramFieldSetup.Assign(changed,target,source,signal)),"a world change cannot replay a pending setup assignment");
        changed=library.Clone();changed.Desks.RemoveAll(d=>d.Id==desk.Id);
        Check(!target.Current(changed)&&Reject(()=>DiagramFieldSetup.Assign(changed,target,source,signal)),"removing a target desk invalidates its outstanding world-pick callback");
        Check(WorldSignalPickFace.Resolve(signal,0)==signal.Id&&WorldSignalPickFace.Resolve(signal,1)==signal.Id,"a single enabled main head can be assigned by clicking its mast from either camera side");
        var two=signal.Clone();two.Mast=MastConfiguration.Effective(two);two.Mast.Back.Enabled=true;
        Check(WorldSignalPickFace.Resolve(two,0)==two.Mast.Front.Id&&WorldSignalPickFace.Resolve(two,1)==two.Mast.Back.Id,"two enabled mast directions remain separate and follow the clicked side");
        two.Mast.Front.Enabled=false;two.Mast.Back.Enabled=false;
        Check(WorldSignalPickFace.Resolve(two,0)==null&&WorldSignalPickFace.Resolve(two,1)==null,"disabled mast heads cannot be registered by falling back to a nonexistent active direction");
        Check(WorldSignalPickFace.Resolve(distant,0)==distant.Id&&WorldSignalPickFace.Resolve(distant,1)==distant.Id,"a distant signal is selected as its own world object without inventing a rear face");
        Check(Reject(()=>DiagramFieldTarget.Capture(library,library.Desks[0].Id,0,0)),"controlling setup starts from a local diagram rather than silently claiming objects for a master display");
        var occupied=library.Clone();occupied.Desks[2].Panel.At(1,1).Reference=signal.Id;
        Check(Reject(()=>DiagramFieldSetup.Assign(occupied,target,source,signal)),"a freshly picked signal already owned elsewhere cannot gain a second controlling assignment");
    }
}
