using System;
using System.Linq;
using EnusDE.Signaltechnik;

static class DiagramRegistrationTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);System.Console.WriteLine("PASS: "+message);}
    public static void Run() {
        string id=Guid.NewGuid().ToString("N"),other=Guid.NewGuid().ToString("N");
        var signal=new MarkerRecord{Id=id,Name="RS-016",Kind=MarkerRecord.Shunt,CircuitVariant=1,CircuitNumber=1,Anchor=new MarkerAnchor{Segment="fixture",Meters=20}};
        var panel=new PanelDocument{WorldKey=new string('d',64)};
        panel.Tiles.Add(new PanelTile{Kind=PanelTileKind.Signal,X=4,Y=3,Reference=id,Label="Signal",Ports=17,Rotation=2,MainSignalDirection=2});
        var before=panel.Clone();var found=DiagramRegistration.Inspect(panel,new[]{signal},false);var tile=found.Panel.At(4,3);
        Check(found.Refreshed==1&&tile.Label==""&&tile.CircuitBoundary==id&&tile.Reference==id&&tile.AlternateSignal==null&&panel.At(4,3).SameContent(before.At(4,3)),"bound signal refresh reveals its technical caption and attached GFM identity without changing the source diagram");
        signal.Kind=MarkerRecord.Main;signal.Name="HS-016";signal.PreviousCode="RS-016";signal.LowMain=true;signal.AllowSh1=true;
        found=DiagramRegistration.Inspect(found.Panel,new[]{signal},false);tile=found.Panel.At(4,3);
        Check(tile.Reference==id&&tile.AlternateSignal==id&&tile.X==4&&tile.Y==3&&tile.MainSignalDirection==2&&tile.Ports==17,"upgrading a bound shunt to a main signal creates its main/shunt self-pair while retaining position, direction and stable identity");
        signal.LowMain=false;found=DiagramRegistration.Inspect(found.Panel,new[]{signal},false);
        Check(found.Panel.At(4,3).AlternateSignal==id&&DiagramRegistration.Inspect(found.Panel,new[]{signal},false).Refreshed==0,"conversion from low main signal to high mast retains the correct divided field and repeated registration is idempotent");
        signal.AllowSh1=false;found=DiagramRegistration.Inspect(found.Panel,new[]{signal},false);
        Check(found.Panel.At(4,3).AlternateSignal==null,"removing a head's shunt capability removes only its automatic self-pair");
        var separate=found.Panel.Clone();separate.At(4,3).AlternateSignal=other;
        Check(DiagramRegistration.Inspect(separate,new[]{signal},false).Panel.At(4,3).AlternateSignal==other,"automatic refresh preserves explicitly paired separate signals");
        var empty=panel.Clone();empty.At(4,3).Reference="";empty.At(4,3).Label="HS-016";
        found=DiagramRegistration.Inspect(empty,new[]{signal},true);
        Check(found.Assigned==1&&found.Panel.At(4,3).Reference==id&&empty.At(4,3).Reference=="","station registration binds an unassigned field only from its unique matching signal code");
        empty.At(4,3).Label="Signal";
        Check(DiagramRegistration.Inspect(empty,new[]{signal},true).Assigned==0,"anonymous signal placeholders never acquire an arbitrary physical signal");
        empty.At(4,3).Label="N1";signal.DisplayName="N1";var duplicate=signal.Clone();duplicate.Id=other;duplicate.Name="HS-017";
        Check(DiagramRegistration.Inspect(empty,new[]{signal,duplicate},true).Assigned==0,"ambiguous station display names are left unassigned instead of guessing by list order");
        empty.At(4,3).Reference=other;empty.At(4,3).Label="HS-016";
        Check(DiagramRegistration.Inspect(empty,new[]{signal},true).Panel.At(4,3).Reference==other,"a signal outside the chosen station scope cannot overwrite an existing field binding");
        signal.PhysicalId=other;signal.CircuitVariant=3;found=DiagramRegistration.Inspect(panel,new[]{signal},false);
        Check(found.Panel.At(4,3).CircuitBoundary==other,"reverse or expanded signal faces use their physical mast's one-sided detection boundary rather than a second fake endpoint");
        signal.CircuitVariant=0;found=DiagramRegistration.Inspect(found.Panel,new[]{signal},false);
        Check(found.Panel.At(4,3).CircuitBoundary==null,"disabling the attached detector clears its own stale field boundary");
        var custom=panel.Clone();custom.At(4,3).Label="Custom platform name";custom.At(4,3).CircuitBoundary=id;
        Check(DiagramRegistration.Inspect(custom,new[]{signal},false).Panel.At(4,3).Label=="Custom platform name"&&DiagramRegistration.Inspect(custom,new[]{signal},false).Panel.At(4,3).CircuitBoundary==id,"registration retains custom field captions and independently assigned boundaries");
    }
}
