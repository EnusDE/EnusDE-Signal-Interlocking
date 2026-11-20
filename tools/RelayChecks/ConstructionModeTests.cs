using System;
using System.Linq;
using EnusDE.Signaltechnik;

static class ConstructionModeTests {
    static void Check(bool value,string text){if(!value)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    public static void Run() {
        var mode=new BuildModeSession();
        Check(!mode.Active&&!mode.Begin(false)&&!mode.Active,"construction starts in normal play and denied access cannot enter build mode");
        var row=new MarkerRecord{Id=Guid.NewGuid().ToString("N"),Planned=true,Name="P1",Kind=MarkerRecord.Main};
        Check(ConstructionViewPolicy.For(row.Planned,mode.Active)==ConstructionVisual.Site,"confirmed plans appear as worksites outside the local build session");
        mode.Begin(true);
        Check(mode.Active&&ConstructionViewPolicy.For(row.Planned,mode.Active)==ConstructionVisual.Plan&&row.Planned,"entering build mode changes only the local plan view, never host construction state");
        Check(mode.End()&&!mode.Active&&row.Planned&&!mode.End(),"ending build mode is idempotent and retains the accepted construction plan");
        Check(ConstructionViewPolicy.For(false,true)==ConstructionVisual.Finished&&ConstructionViewPolicy.For(false,false)==ConstructionVisual.Finished,"finished infrastructure has the same model in normal and build views");
        var mesh=MarkerMeshData.CreateConstructionSite();
        Check(mesh.Vertices.Count>80&&mesh.Vertices.All(v=>v.Length==3&&v.All(float.IsFinite))&&mesh.Triangles.All(t=>t.Count%3==0&&t.All(i=>i>=0&&i<mesh.Vertices.Count)),"original cone/barrier geometry contains finite bounded triangle indices");
        var icons=new[]{"BUILD_MAIN","BUILD_DISTANT","BUILD_SHUNT","BUILD_SPEED","BUILD_CLEARANCE","BUILD_GFM","BUILD_GFM_C"};
        Check(icons.Select(s=>Convert.ToBase64String(ModeIconRaster.ForSymbol(s))).Distinct().Count()==icons.Length&&icons.All(k=>BuildSymbols.For(k).All(l=>l.Length==4&&l.All(v=>v>=0&&v<=32)&&(l[0]!=l[2]||l[1]!=l[3]))),"each construction tool has its own bounded language-independent icon");
    }
}
