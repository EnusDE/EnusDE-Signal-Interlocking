using System;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using EnusDE.Signaltechnik;
static class ConstructionSnapshotTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    public static void Run() {
        string world=new string('a',64),signal=Guid.NewGuid().ToString("N");var basis=new InstallationDocument{WorldKey=world,Map="map",Objects=new MarkerDocument{WorldKey=world,Map="map",Markers=new List<MarkerRecord>{new MarkerRecord{Id=signal,Name="HS-001",Kind=MarkerRecord.Main,Anchor=new MarkerAnchor{Segment="S",Meters=10}}}}};
        string original=ConstructionSnapshot.Signature(basis);Check(ConstructionSnapshot.Signature(basis.Clone())==original,"construction snapshot survives cloning without producing a false stale proposal");
        var live=basis.Clone();for(int i=1;i<=10;i++){live.TrainNumbers["car"]=i.ToString("D5");live.SignalNumberFields[signal]=i.ToString("D5");}
        Check(ConstructionSnapshot.Signature(live)==original,"train-number assignments and field advancement do not invalidate a construction or block-view proposal");
        var moved=basis.Clone();moved.Objects.Markers[0].Anchor.Meters++;
        Check(ConstructionSnapshot.Signature(moved)!=original,"a changed physical signal position invalidates a construction snapshot");
        var turned=basis.Clone();turned.Objects.Markers[0].FacingReverse=true;
        Check(ConstructionSnapshot.Signature(turned)!=original,"a changed physical signal direction still invalidates a construction snapshot");
        var removed=basis.Clone();removed.Objects.Markers.Clear();
        Check(ConstructionSnapshot.Signature(removed)!=original,"removed world signals cannot pass an earlier construction snapshot check");
        var settings=basis.Clone();settings.Placement.RightSide=false;var economy=basis.Clone();economy.Economy.Factor=2;
        Check(ConstructionSnapshot.Signature(settings)!=original&&ConstructionSnapshot.Signature(economy)!=original,"real placement and material settings remain part of construction validity");
        var proposed=JsonConvert.DeserializeObject<MarkerDocument>(JsonConvert.SerializeObject(basis.Objects));proposed.Markers[0].Anchor.Meters=20;var applied=MaterialEconomy.Markers(live,proposed);
        Check(applied.TrainNumbers["car"]=="00010"&&applied.SignalNumberFields[signal]=="00010"&&applied.Objects.Markers[0].Anchor.Meters==20,"applying proposed objects to the latest installation retains current live number registries");
    }
}
