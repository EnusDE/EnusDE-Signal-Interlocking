using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class ExtensionTests {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);System.Console.WriteLine("PASS: "+text);}
    static bool Reject(Action action){try{action();return false;}catch{return true;}}
    static DeskLibrary Library() {
        string key=new string('e',64);return new DeskLibrary{WorldKey=key,Desks=new(){
            new DeskDefinition{Global=true,Code="ESTW",Name="Master",Panel=new PanelDocument{WorldKey=key,Tiles=new(){new PanelTile{X=0,Y=0,Kind=PanelTileKind.Label,Label="Free line"}}}},
            new DeskDefinition{Code="WHB",Name="Whittier",Panel=new PanelDocument{WorldKey=key,Columns=8,Rows=6,Tiles=new(){new PanelTile{X=1,Y=1,Kind=PanelTileKind.Signal,Reference="signal-a"}}}},
            new DeskDefinition{Code="ELA",Name="Ela",Panel=new PanelDocument{WorldKey=key,Columns=8,Rows=6,Tiles=new(){new PanelTile{X=2,Y=2,Kind=PanelTileKind.Signal,Reference="signal-b"}}}}
        }};
    }
    public static void Run(string root) {
        DiagramRegistrationTests.Run();
        SignalRoleTests.Run();
        SharedCircuitTests.Run();
        OperatingTests.Run();
        HlSignalTests.Run(root);
        ConstructionProgressionTests.Run(root);
        LineSetupTests.Run();
        SetupSimplificationTests.Run(root);
        SelectionInputTests.Run();
        RecognitionImprovementsTests.Run();
        CircuitPathValidationTests.Run();
        ContinuousPlacementTests.Run(root);
        GfmRebuildTests.Run();
        StationGfmFixTests.Run();
        RequestRepairTests.Run(root);
        PassageOccupancyTests.Run();
        RoutePointProtectionTests.Run();
        FlexibleSetupTests.Run();
        LocalizationTests.Run(root);
        var library=Library();string a=library.Desks[1].Id,b=library.Desks[2].Id;
        library.Modules.Add(new DeskModule{Desk=a,X=2,Y=2});library.Modules.Add(new DeskModule{Desk=b,X=14,Y=2});library.Validate(library.WorldKey);
        var projected=MasterDiagram.Project(library);
        Check(projected.At(3,3)?.Reference=="signal-a"&&projected.At(16,4)?.Reference=="signal-b"&&projected.At(0,0)?.Label=="Free line","master modules combine both local diagrams with free-line fields while preserving physical signal references");
        library.Desks[1].Panel.At(1,1).Label="Changed";projected=MasterDiagram.Project(library);
        Check(projected.At(3,3).Label=="Changed"&&library.Desks[1].Panel.At(1,1).X==1,"master modules follow local edits without moving or duplicating local source geometry");
        projected.At(3,3).Label="Preview only";Check(library.Desks[1].Panel.At(1,1).Label=="Changed","editing a projected copy never mutates the source diagram");
        var clone=library.Clone();clone.Modules[0].X=20;clone.Automation.PausedStations.Add(a);
        Check(library.Modules[0].X==2&&library.Automation.PausedStations.Count==0,"module positions and central automation pauses are deep-cloned");
        Check(Reject(()=>clone.Validate(clone.WorldKey)),"overlapping modules are rejected before the layout is committed");
        clone=library.Clone();clone.Modules[0].X=PanelDocument.MaxColumns-2;Check(Reject(()=>clone.Validate(clone.WorldKey)),"master modules cannot exceed the diagram bounds");
        clone=library.Clone();clone.Modules[0].Desk=clone.Desks[0].Id;Check(Reject(()=>clone.Validate(clone.WorldKey)),"master cannot recursively contain itself as a module");
        clone=library.Clone();clone.Desks[0].Panel.Tiles.Add(new PanelTile{X=3,Y=3,Kind=PanelTileKind.Label});Check(Reject(()=>clone.Validate(clone.WorldKey)),"module placement protects existing master free-line fields");
        clone=library.Clone();clone.Desks[1].ShowInDispatcher=false;
        Check(MasterDiagram.Project(clone).At(3,3)==null&&clone.Modules.Count==2,"hidden stations retain editor modules but are excluded from the dispatcher projection");
        Check(MasterDiagram.Project(clone,includeHidden:true).At(3,3)?.Reference=="signal-a","module arrangement preview can include hidden stations without exposing them in dispatcher operation");
        Check(MasterDiagram.At(library,library.Desks[0].Id,3,3)?.Desk==a&&MasterDiagram.At(library,library.Desks[0].Id,16,4)?.Desk==b&&MasterDiagram.At(library,library.Desks[0].Id,0,0)==null,"module picking identifies the complete local area while leaving master-owned fields outside module selection");
        clone=library.Clone();MasterDiagram.Move(clone,clone.Desks[0].Id,a,30,4);
        Check(MasterDiagram.Project(clone).At(31,5)?.Reference=="signal-a"&&clone.Desks[1].Panel.At(1,1).Reference=="signal-a"&&library.Modules[0].X==2,"drag placement updates only the module offset and preserves physical bindings and local source geometry");
        int originalX=clone.Modules[0].X;Check(Reject(()=>MasterDiagram.Move(clone,clone.Desks[0].Id,a,14,2))&&clone.Modules[0].X==originalX&&Reject(()=>MasterDiagram.Move(clone,clone.Desks[0].Id,a,-1,2))&&clone.Modules[0].X==originalX,"an overlapping or out-of-bounds module drop leaves the previous position intact");
        clone=library.Clone();clone.Modules.Clear();var suggested=MasterDiagram.Suggest(clone,a);clone.Modules.Add(suggested);clone.Validate(clone.WorldKey);
        Check(!new PanelRegion{X=suggested.X,Y=suggested.Y,Width=8,Height=6}.Contains(0,0),"automatic module placement finds free space without overwriting existing fields");
        library.Automation.PausedStations.Add(a);
        var round=JsonConvert.DeserializeObject<DeskLibrary>(JsonConvert.SerializeObject(library));round.Validate(round.WorldKey);
        Check(round.Modules.Count==2&&round.Automation.PausedStations.SequenceEqual(new[]{a}),"module layout and selective central pause survive save transfer");
        var removed=new DeskRemoval(library,a).Apply(library,Array.Empty<RouteDefinition>());
        Check(removed.Modules.All(m=>m.Desk!=a)&&!removed.Automation.PausedStations.Contains(a)&&removed.Desks.Any(d=>d.Id==b),"removing an interlocking removes its module and pause without affecting its neighbour");
        Check(!ConstructionPolicy.MaterialsRequired(true,false,false)&&ConstructionPolicy.MaterialsRequired(false,false,false)&&ConstructionPolicy.MaterialsRequired(true,true,false)&&!ConstructionPolicy.MaterialsRequired(false,false,true),"sandbox and economy material defaults and both explicit overrides are independent");
        var legacy=new InstallationDocument{WorldKey=new string('e',64),Map="test",Objects=new MarkerDocument{WorldKey=new string('e',64),Map="test"}};
        legacy.Economy.Enabled=true;var legacySandbox=ConstructionPolicy.MigrateLegacy(legacy,true);
        legacy.Economy.Enabled=false;var legacyEconomy=ConstructionPolicy.MigrateLegacy(legacy,false);
        Check(legacySandbox.Economy.MaterialsInSandbox&&legacyEconomy.Economy.FreeInEconomy&&legacy.Economy.PolicyVersion==0,"legacy migration preserves explicitly configured material behaviour without mutating the loaded installation");
        Check(ConstructionPolicy.UnlockCost==7500,"native construction programme has the agreed one-time unlock price");
        var supplies=new[]{new SupplyStock{Car="a",Slot=0,Units=2},new SupplyStock{Car="b",Slot=0,Units=3},new SupplyStock{Car="b",Slot=1,Units=4,Technology=true}};
        var allocation=SignalSupplyPlan.Allocate(new MaterialCost(4,3),supplies);
        Check(allocation.Sum(d=>d.Technology?0:d.Units)==4&&allocation.Sum(d=>d.Technology?d.Units:0)==3&&supplies[0].Units==2,"MOW material planning combines supply wagons and leaves stock untouched until the transaction");
        Check(Reject(()=>SignalSupplyPlan.Allocate(new MaterialCost(6,0),supplies))&&Reject(()=>SignalSupplyPlan.Allocate(new MaterialCost(0,5),supplies)),"insufficient building or technology material rejects the whole installation instead of partially consuming stock");
        Check(Reject(()=>SignalSupplyPlan.Allocate(new MaterialCost(4,0),new[]{new SupplyStock{Car="a",Slot=0,Units=2},new SupplyStock{Car="a",Slot=0,Units=2}})),"a repeated cargo slot cannot fund an installation twice");
        var previous=LocalText.ExternalTranslate;string lang="en";LocalText.Configure(()=>lang);
        Check(LocalText.Get("Rangiersignal")=="Shunting signal"&&LocalText.Select("Main signal","Hauptsignal")=="Main signal","built-in English is available without the translation mod");
        int calls=0;LocalText.ExternalTranslate=(value,language)=>{calls++;return "external "+value;};
        Check(LocalText.Get("Rangiersignal")=="Shunting signal"&&calls==0,"built-in English takes precedence over an external translator");
        lang="de";Check(LocalText.Get("Rangiersignal")=="Rangiersignal"&&LocalText.Select("Main signal","Hauptsignal")=="Hauptsignal"&&calls==0,"built-in German is usable without the translation mod and takes precedence");
        lang="auto";LocalText.AutomaticLanguage="fr";
        Check(LocalText.Get("Rangiersignal")=="external Shunting signal"&&LocalText.Select("Main signal","Hauptsignal")=="external Main signal","other automatic languages use the external translator with an English source");
        LocalText.ExternalTranslate=(value,language)=>throw new Exception("missing provider");
        Check(LocalText.Get("Rangiersignal")=="Shunting signal"&&LocalText.Select("Main signal","Hauptsignal")=="Main signal","unavailable translation providers safely fall back to the built-in English text");
        Check(LocalText.NormalizeLanguage(" DE-de ")=="de"&&LocalText.NormalizeLanguage("en-US")=="en"&&LocalText.NormalizeLanguage(null)=="en","language-region preferences normalize consistently");
        LocalText.ExternalTranslate=previous;LocalText.Configure(()=>"auto");LocalText.AutomaticLanguage="en";
        var en=JObject.Parse(File.ReadAllText(Path.Combine(root,"mod/EnusDE-Signaltechnik/Languages/en.json")));var de=JObject.Parse(File.ReadAllText(Path.Combine(root,"mod/EnusDE-Signaltechnik/Languages/de.json")));
        Check(en.Count>600&&en.Properties().All(p=>de[p.Name]!=null&&LocalText.Get(p.Name)==(string)p.Value),"shipped bilingual language packs agree with all compiled menu translations");
        var publicHelp=SignalHandbook.Chapters(false,false);var hostHelp=SignalHandbook.Chapters(true,true);
        Check(publicHelp.Length==2&&publicHelp.All(c=>!c.EditorOnly)&&hostHelp.Length==6&&hostHelp.Any(c=>c.Id=="automation")&&hostHelp.Any(c=>c.Id=="hl"&&c.EditorOnly)&&!publicHelp.Any(c=>c.Id=="hl"),"F1 handbook limits construction and editor chapters including Hl setup to the existing host permission boundary");
        Check(SignalHandbook.Chapters(false,true).All(c=>!string.IsNullOrWhiteSpace(c.Text))&&hostHelp.All(c=>!string.IsNullOrWhiteSpace(c.Text)),"every handbook chapter contains English and German instructions");
        Check(SignalHandbook.Chapters(false,true).Concat(SignalHandbook.Chapters(true,true)).All(c=>!c.Text.Contains("\\n")),"F1 line-configuration help uses readable paragraphs rather than literal escape sequences");
        var game=Assembly.Load("Railroader");var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var guide=game.GetType("UI.Guide.GuideWindow",true);var doc=guide.GetNestedType("Document",flags);
        Check(guide.GetField("_contents",flags)!=null&&guide.GetMethod("Populate",flags)!=null&&guide.GetField("_panel",flags)!=null&&doc.GetConstructors(flags).Any(c=>c.GetParameters().Length==6),"native F1 integration targets the actual guide document constructor and rebuild points");
        var milestone=game.GetType("Game.Progression.Milestone",true);var custom=typeof(SignalConstructionMilestone);
        Check(custom.GetMethod("BuildPanelInProgress").GetBaseDefinition().DeclaringType==milestone&&custom.GetMethod("DebugAdvance").GetBaseDefinition().DeclaringType==milestone&&milestone.GetField("enableFeaturesOnStart",flags)!=null&&milestone.GetField("cost",flags)!=null,"permanent construction programme uses real virtual milestone hooks and initialized native fields");
        var work=game.GetType("Game.Mow.WorkCar",true);var car=game.GetType("Model.Car",true);var mowProperties=game.GetType("Game.Mow.MowCarProperties",true);
        Check(work.GetMethod("GetSupplyCars",flags).IsStatic&&work.GetMethod("GetSupplyCars",flags).GetParameters().Single().ParameterType==car&&mowProperties.GetConstructor(flags,null,new[]{car},null)!=null&&mowProperties.GetProperty("Enabled",flags)?.PropertyType==typeof(bool),"MOW integration resolves the installed game's supply-car list and native enabled state");
        Check(game.GetType("UI.CarInspector.CarInspector",true).GetMethod("BuildLoads",flags)!=null,"material ordering extends the native wagon Loads panel");
    }
}
