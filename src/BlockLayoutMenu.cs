using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SharedRuntime {
        internal void PutBlockConstruction(MarkerDocument markers,DeskLibrary library,string installationRevision,long configurationRevision,string physicalLine) {
            RequireConstruction();if(ConstructionSignature!=installationRevision||ConfigurationVersion!=configurationRevision)throw new InvalidOperationException(LocalText.Select("Block proposal is outdated. Preview it again.","Blockvorschlag ist veraltet. Vorschau erneut erstellen."));
            if(physicalLine!=null)Entry.CheckLineConfigurationChange(new[]{physicalLine},library.FreeLines);
            CheckpointInstallation();Atomic(()=>{if(physicalLine!=null)PutObjects(markers);PutDesks(library);});
        }
    }
    public sealed partial class SignalRuntime {
        string blockCountInput="3",blockSpacingInput="",blockOwnerInput;
        BlockLayoutPlan blockLayout;
        LineBlockBuild blockBuild;
        string blockPlanShape,blockPlanWorld,blockPlanNetwork;
        string blockPlanInstall;long blockPlanConfig;
        string[] blockDistances;
        bool blockContacts=true,blockActivate=true,blockEnableGlobal,blockReplace;
        string blockSetupLine,blockStaggerInput="10";
        readonly HashSet<string> blockDisplayInput=new HashSet<string>(),blockConfirmedPositions=new HashSet<string>();
        void BuildBlockLayout(UIPanelBuilder c,LineTrackDefinition line,Func<bool> current) {
            if(blockSetupLine!=line.Id){blockSetupLine=line.Id;blockLayout=null;blockBuild=null;blockOwnerInput=line.BlockOwner!=""?line.BlockOwner:line.StationA;blockDisplayInput.Clear();blockDisplayInput.UnionWith(line.BlockDisplays);blockConfirmedPositions.Clear();if(line.BlockSections.Count>0)blockCountInput=line.BlockSections.Count.ToString();blockSpacingInput="";blockActivate=!guidedLineSetup&&(line.Enabled||line.BlockOwner=="");blockEnableGlobal=false;blockReplace=false;}
            c.AddSection(WindowText("Optional automatic block construction","Optionale automatische Blockeinrichtung"));
            c.AddLabel(WindowText("Preview 1–16 sections along the actual line. Existing intermediate signals are retained. New signals, opposite faces and GFM are constructed under the current material rules only after confirmation. Point areas require their own detection and are excluded from this construction shortcut.","Vorschau für 1–16 Abschnitte entlang des tatsächlichen Gleises. Vorhandene Zwischensignale bleiben erhalten. Neue Signale, Gegenrichtungen und GFM werden erst nach Bestätigung nach den geltenden Materialregeln gebaut. Weichenbereiche benötigen eigene Freimeldung und sind von diesem Bauassistenten ausgenommen."));
            if(line.BlockSections.Count>0) {
                c.AddFieldToggle(WindowText("Set up again: replace existing block division","Neu einrichten: vorhandene Blockaufteilung ersetzen"),()=>blockReplace,v=>{blockReplace=v;blockBuild=null;blockLayout=null;RefreshWorkspaceFields();},true);
                c.AddLabel(WindowText("Replacement keeps the connection and its permission links. Old block signals and detection are replaced only when you confirm the completed preview. External manual references must be reassigned first; complete number overviews are regenerated.","Der Ersatz erhält die Verbindung und ihre Erlaubnisverknüpfungen. Alte Blocksignale und Freimeldung werden erst beim Bestätigen der fertigen Vorschau ersetzt. Externe manuelle Verknüpfungen vorher neu zuordnen; vollständige Zugnummernübersichten werden neu erzeugt."));
            }
            c.AddField(WindowText("Number of blocks","Anzahl der Blöcke"),c.AddInputField(blockCountInput,v=>{blockCountInput=v;blockBuild=null;},"3",2));
            c.AddField(WindowText("Or approximate spacing (m)","Oder ungefährer Abstand (m)"),c.AddInputField(blockSpacingInput,v=>{blockSpacingInput=v;blockBuild=null;},"1000",12));
            c.AddFieldToggle(WindowText("Also configure approach contacts and next-block targets","Auch Anforderungskontakte und Ziele zum nächsten Block einrichten"),()=>blockContacts,v=>{blockContacts=v;blockBuild=null;RefreshWorkspaceFields();},true);
            c.AddLabel(WindowText("Optional contacts use the existing train-duty automation. Existing signal rules/contacts are retained. Global automation changes only through the separate checkbox below. This does not create vehicle-specific orders.","Optionale Kontakte verwenden die vorhandene Zugfahrt-Automatik. Bestehende Signalregeln/Kontakte bleiben erhalten. Die globale Automatik ändert sich nur über das gesonderte Kontrollfeld unten. Es entstehen keine fahrzeugspezifischen Aufträge."));
            c.AddField(WindowText("Signal stagger per side (m)","Signalversatz je Seite (m)"),c.AddInputField(blockStaggerInput,v=>{blockStaggerInput=v;blockBuild=null;},"10",12));
            if(!guidedLineSetup)c.AddFieldToggle(WindowText("Activate line protection with this confirmation","Streckensicherung mit dieser Bestätigung aktivieren"),()=>blockActivate,v=>{blockActivate=v;blockBuild=null;},true);
            c.AddFieldToggle(WindowText("Also enable global contact automation","Zusätzlich die globale Kontakt-Automatik einschalten"),()=>blockEnableGlobal,v=>{blockEnableGlobal=v;blockBuild=null;},true);
            c.AddLabel(WindowText("Block construction and line protection are saved together. Inactive block installations reject departures. Optional global automation affects all configured contacts; locomotive train duty is still required.","Blockbau und Streckensicherung werden gemeinsam gespeichert. Inaktive Blockanlagen weisen Ausfahrten ab. Die optionale globale Automatik gilt für alle eingerichteten Kontakte; die Lok benötigt weiterhin die Betriebsart Zugfahrt."));
            var owners=desks.Desks.Where(d=>!d.IsMaster).OrderBy(d=>d.Code).ToArray();
            if(!owners.Any(d=>d.Id==blockOwnerInput))blockOwnerInput=line.StationA;
            c.AddSearchChoice(WindowText("Block signal operating interlocking","Bedienendes Stellwerk der Blocksignale"),owners.Select(d=>d.Code+" · "+d.Name).ToList(),Math.Max(0,Array.FindIndex(owners,d=>d.Id==blockOwnerInput)),i=>{blockOwnerInput=owners[i].Id;blockDisplayInput.Remove(blockOwnerInput);blockBuild=null;RefreshWorkspaceFields();});
            c.AddSection(WindowText("Additional read-only diagrams","Zusätzliche schreibgeschützte Anzeigetafeln"));
            foreach(var display in desks.Desks.Where(d=>d.Id!=blockOwnerInput).OrderBy(d=>d.Code)) {string id=display.Id;c.AddFieldToggle(display.Code+" · "+display.Name,()=>blockDisplayInput.Contains(id),v=>{if(v)blockDisplayInput.Add(id);else blockDisplayInput.Remove(id);blockBuild=null;},true);}
            if(line.BlockSections.Count>0)c.AddButton(WindowText("Change existing assembly ownership / display only","Nur Bedienzuordnung / Anzeige des bestehenden Verbunds ändern"),()=>Act(()=>{
                if(!current())return;RequirePanelEdit();
                var library=desks.Clone();var changed=line.Clone();changed.BlockOwner=blockOwnerInput;changed.BlockDisplays=blockDisplayInput.Where(id=>id!=blockOwnerInput).ToList();
                var blocks=changed.BlockSections.Select(id=>library.Blocks.TryGetValue(id,out var block)?block:throw new InvalidOperationException(WindowText("An assembly section is missing. Rebuild the block proposal.","Ein Verbundabschnitt fehlt. Blockvorschlag neu aufbauen."))).ToList();
                library.FreeLines[library.FreeLines.FindIndex(l=>l.Id==line.Id)]=changed.Clone();
                CheckLineConfigurationChange(LineConfigurationChanges.Changed(desks.FreeLines,library.FreeLines),library.FreeLines);
                BlockDiagram.Rebuild(library,changed,blocks);library.Validate(scope);
                blockLayout=new BlockLayoutPlan{Axis=line.Axis,Boundaries=line.BlockUnits.Select(u=>u.Distance).ToList()};blockDistances=blockLayout.Boundaries.Select(d=>d.ToString(CultureInfo.InvariantCulture)).ToArray();
                blockBuild=new LineBlockBuild{Objects=Entry.Shared.Objects(),Library=library,Line=changed};CaptureBlockPreview(line,blockBuild);RefreshWorkspaceFields();
            }));
            c.AddButton(WindowText("Preview block layout","Blockaufteilung als Vorschau erstellen"),()=>Act(()=>{
                if(!current())return;RequirePanelEdit();CheckLineConfigurationChange(new[]{line.Id},new[]{line});blockBuild=null;
                if(!int.TryParse(blockCountInput,out int count))throw new InvalidOperationException(WindowText("Enter a valid block count.","Gültige Blockanzahl eingeben."));
                double? spacing=null;if(!string.IsNullOrWhiteSpace(blockSpacingInput)){if(!double.TryParse(blockSpacingInput.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double value))throw new InvalidOperationException(WindowText("Enter a valid distance.","Gültigen Abstand eingeben."));spacing=value;}
                blockLayout=BlockLayoutPlan.Create(line.Axis,count,spacing);var ignored=line.BlockUnits.SelectMany(u=>new[]{u.Forward,u.Reverse}).ToHashSet();
                var existing=Entry.Shared.Objects().Markers.Where(m=>!ignored.Contains(m.Id)).ToList();foreach(var unit in blockReplace?Enumerable.Empty<BlockSignalUnit>():line.BlockUnits){var box=existing.FirstOrDefault(m=>m.Id==unit.Boundary);if(box!=null){var stand=box.Clone();stand.Kind=MarkerRecord.Main;existing.Add(stand);}}
                blockLayout.IncludeExisting(existing);
                blockDistances=blockLayout.Boundaries.Select(d=>d.ToString("0.###",CultureInfo.InvariantCulture)).ToArray();PreviewBlockBuild(line);RefreshWorkspaceFields();
            }));
            if(blockLayout==null||blockPlanWorld!=scope||blockPlanShape!=LineDetectionShape(line))return;
            c.AddLabel(blockLayout.BlockCount+WindowText(" blocks / "," Blöcke / ")+blockLayout.Boundaries.Count+WindowText(" intermediate boundaries / "," Zwischengrenzen / ")+Math.Round(blockLayout.Length)+" m");
            for(int i=0;i<blockDistances.Length;i++) {int selected=i;var at=blockLayout.At(blockLayout.Boundaries[i]);c.AddField(WindowText("Boundary ","Grenze ")+(i+1)+" · "+at.Segment+" / "+Math.Round(at.Meters,2)+" m",c.AddInputField(blockDistances[i],v=>{blockDistances[selected]=v;blockBuild=null;},"0",16));}
            c.AddButton(WindowText("Update boundary distances / preview","Grenzabstände / Vorschau aktualisieren"),()=>Act(()=>{
                if(!current())return;
                var distances=blockDistances.Select(s=>double.Parse(s.Replace(',','.'),CultureInfo.InvariantCulture)).ToList();
                var proposal=BlockLayoutPlan.Create(blockLayout.Axis,blockLayout.BlockCount);proposal.Boundaries=distances;proposal.Validate();blockLayout=proposal;PreviewBlockBuild(line);RefreshWorkspaceFields();
            }));
            if(blockBuild==null)return;var build=blockBuild;
            c.AddLabel(WindowText("New signal masts: ","Neue Signalmasten: ")+build.NewSignals+WindowText(" · reused: "," · wiederverwendet: ")+build.ReusedSignals);
            c.AddLabel(WindowText("The signal offset is limited to one quarter of the adjacent section length on short sections. The central detector stays at the block boundary.","Bei kurzen Abschnitten wird der Signalversatz auf ein Viertel der benachbarten Abschnittslänge begrenzt. Der gemeinsame Melder bleibt auf der Blockgrenze."));
            foreach(var unit in build.Line.BlockUnits){var a=build.Objects.Markers.Single(m=>m.Id==unit.Forward);var b=unit.Reverse==""?null:build.Objects.Markers.Single(m=>m.Id==unit.Reverse);c.AddLabel(a.Name+" · "+a.Anchor.Segment+" / "+Math.Round(a.Anchor.Meters,2)+" m"+(b==null?"":" ↔ "+b.Name+" · "+b.Anchor.Segment+" / "+Math.Round(b.Anchor.Meters,2)+" m"));}
            c.AddLabel(WindowText("Replaced block signal heads: ","Ersetzte Blocksignalköpfe: ")+build.RemovedSignals);
            c.AddLabel(WindowText("New automatic contacts: ","Neue Automatik-Kontakte: ")+build.NewContacts);
            foreach(string target in build.Line.BlockDisplays.Prepend(build.Line.BlockOwner)) {
                var desk=build.Library.Desks.Single(d=>d.Id==target);string captured=target;var area=BlockDiagram.Area(desk.Panel,build.Line.Id);
                c.AddField(desk.Code+(target==build.Line.BlockOwner?WindowText(" · controls"," · Bedienung"):WindowText(" · display only"," · nur Anzeige")),(blockConfirmedPositions.Contains(target)?WindowText("Position confirmed: ","Position bestätigt: "):WindowText("Choose position: ","Position wählen: "))+(area.X+1)+" / "+(area.Y+1));
                c.AddButton(WindowText("Place assembly in ","Verbund platzieren in ")+desk.Code,()=>OpenBlockPlacement(build,captured));
            }
            c.AddButton(WindowText("Confirm block construction and diagram","Blockbau und Gleisbild bestätigen"),()=>Act(()=>{
                RequirePanelEdit();CheckLineConfigurationChange(build.WorldConstruction?new[]{line.Id}:LineConfigurationChanges.Changed(desks.FreeLines,build.Library.FreeLines),build.Library.FreeLines);
                if(build.Line.BlockDisplays.Prepend(build.Line.BlockOwner).Any(id=>!blockConfirmedPositions.Contains(id)))throw new InvalidOperationException(WindowText("Confirm the assembly position in every selected diagram first.","Zuerst die Verbundposition in jeder gewählten Tafel bestätigen."));
                if(!current()||!ReferenceEquals(build,blockBuild)||scope!=blockPlanWorld||LineDetectionShape(line)!=blockPlanShape||NetworkRecognition.Signature(CaptureNetwork())!=blockPlanNetwork)throw new InvalidOperationException(WindowText("Block proposal changed. Preview it again.","Blockvorschlag geändert. Vorschau erneut erstellen."));
                Entry.Shared.PutBlockConstruction(build.Objects,build.Library,blockPlanInstall,blockPlanConfig,build.WorldConstruction?build.Line.Id:null);
                desks=Entry.Shared.Desks();panel=DeskPanel(CurrentDesk);lineDraft=desks.FreeLines.Single(l=>l.Id==line.Id).Clone();lineIndicatorTarget=build.Indicator;ReadSignalInventory();RefreshLineDirections();blockBuild=null;blockLayout=null;panelView?.ReplaceDocument(panel);nativeDirty=true;
                status=WindowText("Block assembly, operating interlocking and diagrams saved. Material-dependent objects remain construction plans. The line status reports missing construction or detection coverage before allowing departures.","Blockverbund, bedienendes Stellwerk und Tafeln gespeichert. Materialpflichtige Objekte bleiben Baupläne. Der Streckenzustand meldet fehlenden Bau oder GFM-Abdeckung vor Freigabe einer Ausfahrt.");RefreshWorkspaceFields();
            }));
        }
        void PreviewBlockBuild(LineTrackDefinition line) {
            var input=CaptureNetwork();var edges=input.Edges.ToDictionary(e=>e.Id);var points=input.Nodes.Where(n=>n.Point).Select(n=>n.Id).ToHashSet();
            foreach(var p in blockLayout.Axis) {
                var edge=edges[p.Section.Segment];
                if(p.Section.From<.01&&points.Contains(edge.A)||p.Section.To>edge.Length-.01&&points.Contains(edge.B))throw new InvalidOperationException(WindowText("This line crosses a point area. Configure its boundaries separately; automatic plain-line construction is unavailable here.","Diese Strecke führt über einen Weichenbereich. Dessen Grenzen gesondert einrichten; der Bauassistent für freie Gleise ist hier nicht verfügbar."));
            }
            blockBuild=null;
            var placement=new MarkerOptions();Entry.Shared.ApplyPlacementDefaults(placement);placement=PlacementDefaults.Resolve(placement);
            if(!double.TryParse(blockStaggerInput.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double stagger))throw new InvalidOperationException(WindowText("Enter a valid signal stagger.","Gültigen Signalversatz eingeben."));
            var proposed=line.Clone();proposed.Enabled=blockActivate;
            var constructionLibrary=desks;
            if(guidedLineSetup&&guidedPermissions.Count>0){constructionLibrary=desks.Clone();LineSetupWorkflow.Put(constructionLibrary,proposed,lineIndicatorTarget);ApplyGuidedPermissions(constructionLibrary,proposed);}
            var build=blockReplace?LineBlockReplacement.Build(Entry.Shared.Objects(),constructionLibrary,proposed,blockLayout,blockOwnerInput,blockContacts,placement,stagger,blockDisplayInput,routes):LineBlockBuilder.Build(Entry.Shared.Objects(),constructionLibrary,proposed,blockLayout,blockOwnerInput,blockContacts,placement,stagger,blockDisplayInput);
            if(guidedLineSetup)LineControlSetup.Apply(build.Library,build.Line,SignalCatalogue.Expand(build.Objects.Markers,true));
            if(blockEnableGlobal)build.Library.Automation.Enabled=true;
            CaptureBlockPreview(line,build);
        }
        void CaptureBlockPreview(LineTrackDefinition line,LineBlockBuild build) {
            build.Indicator=lineIndicatorTarget?.RebindAssembly(desks,build.Library,build.Line);
            blockConfirmedPositions.Clear();foreach(string target in build.Line.BlockDisplays.Prepend(build.Line.BlockOwner))if(BlockDiagram.Area(desks.Desks.Single(d=>d.Id==target).Panel,line.Id)!=null)blockConfirmedPositions.Add(target);
            blockPlanWorld=scope;blockPlanShape=LineDetectionShape(line);blockPlanInstall=Entry.Shared.ConstructionSignature;blockPlanConfig=Entry.Shared.ConfigurationVersion;blockPlanNetwork=NetworkRecognition.Signature(CaptureNetwork());blockBuild=build;
        }
    }
}
