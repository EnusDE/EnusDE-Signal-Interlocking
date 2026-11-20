using System;
using System.Linq;
using EnusDE.Signaltechnik;

static class EditorContextTests {
    static void Check(bool value,string message){if(!value)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    public static void Run() {
        var number=new PanelTile{Kind=PanelTileKind.TrainNumber};
        Check(EditorContextPolicy.FieldCategories(null).Length==0&&EditorContextPolicy.FieldCategories(number).Contains(EditorFieldCategory.TrainNumber)&&
            EditorContextPolicy.FieldCategories(number).Contains(EditorFieldCategory.Detection)&&
            !EditorContextPolicy.FieldCategories(new PanelTile{Kind=PanelTileKind.Label}).Contains(EditorFieldCategory.Detection)&&
            !EditorContextPolicy.FieldCategories(new PanelTile{Kind=PanelTileKind.LinePermission}).Contains(EditorFieldCategory.Detection),
            "right-click field categories expose number and detection controls only on applicable field types");
        number.NumberMirror="source";
        Check(!EditorContextPolicy.FieldCategories(number).Contains(EditorFieldCategory.Detection),"read-only number mirrors never offer physical detection configuration");
        var panel=new PanelDocument{WorldKey="context"};panel.Put(new PanelTile{X=1,Y=1,Kind=PanelTileKind.Switch,Reference="point-A"});var snapshot=panel.At(1,1).Clone();
        Check(EditorContextPolicy.SameTarget(panel,1,1,snapshot)&&EditorContextPolicy.SameTarget(panel,2,2,null),"context target accepts unchanged fields and unchanged empty cells");
        panel.At(1,1).Reference="point-B";
        Check(!EditorContextPolicy.SameTarget(panel,1,1,snapshot),"reassigned fields invalidate old context actions");
        panel.Put(new PanelTile{X=2,Y=2});
        Check(!EditorContextPolicy.SameTarget(panel,2,2,null),"a newly occupied cell invalidates its previously empty context target");
        bool position=false,locked=false,accepted=true;int writes=0;
        bool? Read(string id)=>id=="W1"?(bool?)position:null;
        bool Locked(string id,bool target)=>locked;
        bool Apply(string id,bool target){writes++;if(!accepted)return false;position=target;return true;}
        var move=SwitchOperation.Execute("W1",null,Read,Locked,Apply);
        Check(move.Changed&&move.Target&&position&&writes==1,"left-click point operation toggles the current native position once");
        locked=true;
        var noOp=SwitchOperation.Execute("W1",true,Read,Locked,Apply);
        Check(!noOp.Changed&&noOp.Target&&writes==1,"requesting an already achieved position does not attempt a locked native movement");
        Check(Reject(()=>SwitchOperation.Execute("W1",false,Read,Locked,Apply))&&writes==1&&position,"route and flank locks reject a real movement before a native command is sent");
        Check(Reject(()=>SwitchOperation.Execute("",null,Read,Locked,Apply))&&Reject(()=>SwitchOperation.Execute("missing",null,Read,Locked,Apply))&&writes==1,
            "unassigned or absent points cannot emit switch commands");
        locked=false;accepted=false;
        Check(Reject(()=>SwitchOperation.Execute("W1",null,Read,Locked,Apply))&&writes==2&&position,"native rejection is reported without claiming a completed point movement");
        accepted=true;move=SwitchOperation.Execute("W1",false,Read,Locked,Apply);
        Check(move.Changed&&!move.Target&&!position&&writes==3,"explicit normal-position operation uses the same guarded path as click toggling");
    }
}
