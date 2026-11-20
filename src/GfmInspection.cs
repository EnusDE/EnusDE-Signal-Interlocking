using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string gfmInspectionScope,gfmInspectionDesk,gfmInspectionNetwork,gfmInspectionChoice,gfmTestStart,gfmTestEnd;
        long gfmInspectionVersion,gfmInspectionInstallation;
        NetworkInput gfmInspectionInput;
        readonly Dictionary<string,string> gfmInspectionErrors=new Dictionary<string,string>();
        readonly Dictionary<string,TrackBlock> gfmInspectionCatalogue=new Dictionary<string,TrackBlock>();
        readonly Dictionary<(int,int),int> gfmInspectionPorts=new Dictionary<(int,int),int>();
        readonly HashSet<(int,int)> gfmInspectionEnds=new HashSet<(int,int)>(),gfmInspectionGaps=new HashSet<(int,int)>();
        List<List<RouteProposal>> gfmTestPlans;
        int gfmTestChoice;
        List<TrackSection> gfmTestMissing;
        RouteKind gfmTestKind;
        void OpenGfmInspection() {RequirePanelEdit();ReadGfmInspection();WorkspacePage(12);}
        void OpenGfmIssue(GfmDetectionIssue issue) {
            RequirePanelEdit();if(!desks.Desks.Any(d=>d.Id==issue.Desk))return;
            if(deskId!=issue.Desk)SelectDesk(issue.Desk,false);ReadGfmInspection();gfmInspectionCatalogue[issue.Block.Id]=issue.Block.Clone();gfmInspectionChoice=issue.Block.Id;
            WorkspacePage(12);HighlightGfm(new[]{issue.Block.Id});
        }
        void ReturnToGfmDetection() {
            ClearGfmHighlight();if(existingDetectionProposal!=null&&existingDetectionScope==scope&&desks.Desks.Any(d=>d.Id==existingDetectionDesk)){if(deskId!=existingDetectionDesk)SelectDesk(existingDetectionDesk,false);WorkspacePage(11);}else OpenExistingDetection();
        }
        void ReadGfmInspection() {
            RequirePanelEdit();ReadSignalInventory();IndexGfmObjects(Entry.Shared.Objects().Markers);ResetGfmRepair();gfmInspectionInput=CaptureNetwork();gfmInspectionScope=scope;gfmInspectionDesk=deskId;
            gfmInspectionVersion=Entry.Shared.ConfigurationVersion;gfmInspectionNetwork=NetworkRecognition.Signature(gfmInspectionInput);
            gfmInspectionInstallation=Entry.Shared.InstallationVersion;
            gfmInspectionErrors.Clear();gfmTestPlans=null;gfmTestMissing=null;ClearGfmHighlight();
            gfmInspectionCatalogue.Clear();
            var detected=NetworkRecognition.PreserveCircuitIds(desks,DetectionNetwork.Build(gfmInspectionInput),CanonicalBoundaryId);
            foreach(var block in detected.Blocks.Values.Where(b=>b.IsCircuit))gfmInspectionCatalogue[block.Id]=block;
            foreach(var block in desks.Blocks.Values)try{gfmInspectionCatalogue[block.Id]=ReadPointDefinition(block);}catch(Exception e){gfmInspectionCatalogue[block.Id]=block;gfmInspectionErrors[block.Id]=e.Message;}
            foreach(var block in gfmInspectionCatalogue.Values.Where(b=>b.IsCircuit)) {
                string error;try{error=CircuitPathValidation.Error(gfmInspectionInput,block,CircuitEndpoint(block.BoundaryA),CircuitEndpoint(block.BoundaryB),id=>PointAssignments.Caption(desks,id));}catch(Exception e){error=e.Message;}
                if(error!=null)gfmInspectionErrors[block.Id]=error;
            }
        }
        void ClearGfmHighlight(){gfmInspectionPorts.Clear();gfmInspectionEnds.Clear();gfmInspectionGaps.Clear();panelView?.Refresh();}
        bool GfmInspectionCurrent=>gfmInspectionInput!=null&&scope==gfmInspectionScope&&deskId==gfmInspectionDesk&&Entry.Shared.ConfigurationVersion==gfmInspectionVersion&&Entry.Shared.InstallationVersion==gfmInspectionInstallation;
        bool GfmInspectionVisible=>editingWorkspace&&panelPage==12&&GfmInspectionCurrent;
        void RequireFreshGfmInspection(){RequirePanelEdit();if(!GfmInspectionCurrent||NetworkRecognition.Signature(CaptureNetwork())!=gfmInspectionNetwork)throw new InvalidOperationException(WindowText("Network changed. Refresh the inspection.","Gleisnetz geändert. Prüfung aktualisieren."));}
        TrackBlock[] GfmInspectionBlocks()=>GfmInspectionOrder.Sort(gfmInspectionCatalogue.Values.Where(b=>b.Point!=null?panel.Tiles.Any(t=>t.Kind==PanelTileKind.Switch&&t.Reference==b.Point):
            b.IsCircuit&&(gfmShowAllFaults&&desks.Blocks.ContainsKey(b.Id)&&gfmInspectionErrors.ContainsKey(b.Id)||panel.Tiles.Any(t=>DiagramBoundary(t,b.BoundaryA)||DiagramBoundary(t,b.BoundaryB)||GfmDetectionRefresh.References(t).Contains(b.Id)))),panel,panel.At(selectedX,selectedY),DiagramBoundary).OrderByDescending(b=>gfmInspectionErrors.ContainsKey(b.Id)).ToArray();
        IEnumerable<TrackSection> GfmInspectionCoverage()=>desks.Blocks.Values.Where(b=>b.IsCircuit&&!gfmInspectionErrors.ContainsKey(b.Id)).SelectMany(b=>b.Path.Select(p=>p.Section))
            .Concat(desks.Blocks.Values.Where(b=>b.Point!=null&&PointCircuitVisibility.Enabled(desks,b.Point)).SelectMany(UsablePointGuards));
        string GfmPair(TrackBlock block)=>block.Point!=null?PointAssignments.Caption(desks,block.Point):BoundaryCaption(block.BoundaryA)+" ↔ "+BoundaryCaption(block.BoundaryB);
        void HighlightGfm(IEnumerable<string> ids,IEnumerable<TrackSection> missing=null) {
            ClearGfmHighlight();var selected=new HashSet<string>(ids);var gaps=missing?.ToArray()??Array.Empty<TrackSection>();
            foreach(var tile in panel.Tiles) {
                var cell=(tile.X,tile.Y);int ports=GfmCoverage.Ports(tile,selected);if(ports!=0)gfmInspectionPorts[cell]=ports;
                if(selected.Any(id=>gfmInspectionCatalogue.TryGetValue(id,out var b)&&b.IsCircuit&&(DiagramBoundary(tile,b.BoundaryA)||DiagramBoundary(tile,b.BoundaryB))))gfmInspectionEnds.Add(cell);
                bool Gap(TrackSection section)=>section!=null&&gaps.Any(g=>g.Overlaps(section));
                if(Gap(tile.Section)||GfmDetectionRefresh.References(tile).Any(id=>desks.Blocks.TryGetValue(id,out var b)&&b.Sections.Any(Gap))||
                    !string.IsNullOrEmpty(tile.AutoSegment)&&gaps.Any(g=>g.Segment==tile.AutoSegment&&tile.AutoMeters>=g.From&&tile.AutoMeters<=g.To))gfmInspectionGaps.Add(cell);
            }
            panelView?.Refresh();
        }
        void BuildGfmInspection(UIPanelBuilder c) {
            c.AddSection(WindowText("GFM inspection and repair","GFM-Prüfung und Reparatur"));
            c.AddLabel(WindowText("Highlights saved boundaries and sections without setting signals, moving points or reserving routes. Cyan outlines show the selection; orange outlines mark fields associated with gaps. Normal occupancy colours remain live. Unmapped gaps are listed by track and metres.","Zeigt gespeicherte Grenzen und Abschnitte, ohne Signale zu stellen, Weichen zu bewegen oder Fahrstraßen festzulegen. Cyan umrandet die Auswahl; Orange kennzeichnet Felder mit zugehörigen Lücken. Normale Belegungsfarben bleiben live. Nicht zugeordnete Lücken stehen mit Gleis und Metern in der Liste."));
            c.AddButton(WindowText("Refresh GFM inspection","GFM-Prüfung aktualisieren"),()=>Act(()=>{ReadGfmInspection();RefreshWorkspaceFields();}));
            c.AddButton(WindowText("Clear highlight","Hervorhebung aufheben"),ClearGfmHighlight);
            c.AddButton(WindowText("Back to GFM detection","Zur GFM-Erkennung"),ReturnToGfmDetection);
            c.AddLabel(WindowText("A section ends at active GFM boundaries, not at every signal or virtual destination. A long physical section can extend beyond this diagram; inspect both boundary origins below.","Ein Abschnitt endet an aktiven GFM-Grenzen, nicht an jedem Signal oder Hilfsziel. Ein langer physischer Abschnitt kann über diese Tafel hinausreichen; unten die Herkunft beider Grenzen prüfen."));
            if(!GfmInspectionCurrent){c.AddLabel(WindowText("Configuration changed. Refresh the inspection.","Konfiguration geändert. Prüfung aktualisieren."));return;}
            BuildGfmFaultOverview(c);BuildGfmRepairPreview(c);
            var choices=GfmInspectionBlocks();
            if(choices.Length==0)c.AddLabel(WindowText("No configured GFM sections in this diagram.","Keine eingerichteten GFM-Abschnitte in dieser Tafel."));
            else {
                if(!choices.Any(b=>b.Id==gfmInspectionChoice))gfmInspectionChoice=choices[0].Id;
                c.AddSearchChoice(WindowText("Section / point detection","Abschnitt / Weichen-GFM"),choices.Select(b=>GfmPair(b)+(gfmInspectionErrors.ContainsKey(b.Id)?" · !":"")).ToList(),Array.FindIndex(choices,b=>b.Id==gfmInspectionChoice),i=>{gfmInspectionChoice=choices[i].Id;ResetGfmRepair();RefreshWorkspaceFields();});
                var block=choices.Single(b=>b.Id==gfmInspectionChoice);
                c.AddLabel(GfmPair(block)+" · "+SignUnits.Number(block.Sections.Sum(s=>s.To-s.From))+" m");
                if(gfmInspectionErrors.TryGetValue(block.Id,out var error))c.AddLabel(block.IsCircuit?DescribeGfm(block,error):error);
                else if(!desks.Blocks.ContainsKey(block.Id))c.AddLabel(WindowText("Detected physically, not yet saved or assigned. This proposal is not counted as coverage.","Physisch erkannt, noch nicht gespeichert oder zugeordnet. Dieser Vorschlag zählt nicht als Überwachungsabdeckung."));
                else if(block.Point!=null&&!PointCircuitVisibility.Enabled(desks,block.Point))c.AddLabel(WindowText("Point detection is disabled.","Weichen-GFM ist deaktiviert."));
                else c.AddLabel(WindowText("Physical definition is valid. This is not a clear-track report.","Physische Definition ist gültig. Das ist keine Freimeldung."));
                int fields=panel.Tiles.Count(t=>GfmCoverage.Ports(t,new HashSet<string>{block.Id})!=0);
                c.AddLabel(WindowText("Linked diagram fields: ","Verknüpfte Gleisbildfelder: ")+fields);
                if(fields==0)c.AddLabel(WindowText("This section is not linked to the rails in this diagram. The highlight can therefore show only its drawn endpoint(s).","Dieser Abschnitt ist nicht mit den Gleisfeldern dieser Tafel verknüpft. Die Hervorhebung kann deshalb nur seine eingezeichneten Endpunkte zeigen."));
                c.AddButton(WindowText("Show section and boundaries","Abschnitt und Grenzen hervorheben"),()=>Act(()=>{RequireFreshGfmInspection();HighlightGfm(new[]{block.Id});}));
                var editable=panel.Tiles.Where(t=>GfmCoverage.Ports(t,new HashSet<string>{block.Id})!=0).OrderBy(t=>t.Kind==PanelTileKind.Switch||t.Kind==PanelTileKind.Signal?1:0).FirstOrDefault();
                if(editable!=null)c.AddButton(WindowText("Edit section assignment at field…","Abschnittszuordnung am Feld bearbeiten …"),()=>Act(()=>{RequireFreshGfmInspection();ClearGfmHighlight();OpenEditorField(editable.X,editable.Y,EditorFieldCategory.Detection);}));
                foreach(var section in block.Sections.Take(32))c.AddLabel(section.Segment+" · "+SignUnits.Number(section.From)+"–"+SignUnits.Number(section.To)+" m");
                if(block.IsCircuit){BuildGfmBoundaryDetails(c,block.BoundaryA,true);BuildGfmBoundaryDetails(c,block.BoundaryB,false);BuildGfmRepair(c,block);}
            }
            c.AddSection(WindowText("Test a route for detection gaps","Fahrweg auf GFM-Lücken testen"));
            var signals=panel.Tiles.Where(t=>t.Kind==PanelTileKind.Signal).SelectMany(t=>new[]{Object(t.Reference),Object(t.AlternateSignal)}).Where(s=>s?.IsSignal==true&&!s.Planned&&s.Kind!=MarkerRecord.Distant).GroupBy(s=>s.Id).Select(g=>g.First()).OrderBy(s=>s.DisplayTitle).ToArray();
            if(signals.Length<2)return;
            if(!signals.Any(s=>s.Id==gfmTestStart))gfmTestStart=signals[0].Id;
            if(!signals.Any(s=>s.Id==gfmTestEnd))gfmTestEnd=signals.Last().Id;
            void Changed(){gfmTestPlans=null;gfmTestMissing=null;ClearGfmHighlight();RefreshWorkspaceFields();}
            c.AddSearchChoice(WindowText("Start signal","Startsignal"),signals.Select(s=>s.DisplayTitle).ToList(),Array.FindIndex(signals,s=>s.Id==gfmTestStart),i=>{gfmTestStart=signals[i].Id;Changed();});
            c.AddSearchChoice(WindowText("Destination signal","Zielsignal"),signals.Select(s=>s.DisplayTitle).ToList(),Array.FindIndex(signals,s=>s.Id==gfmTestEnd),i=>{gfmTestEnd=signals[i].Id;Changed();});
            c.AddSearchChoice(WindowText("Movement type","Fahrart"),new List<string>{WindowText("Train","Zugfahrt"),WindowText("Reduced train route","Langsamfahrt"),WindowText("Shunting","Rangierfahrt")},(int)gfmTestKind,i=>{gfmTestKind=(RouteKind)i;Changed();});
            c.AddButton(WindowText("Calculate test paths · no operation","Testwege berechnen · keine Bedienung"),()=>Act(()=>{
                RequireFreshGfmInspection();
                gfmTestPlans=AutomaticRoutePlanner.Find(gfmInspectionInput,SignalObjects.Where(s=>!s.Planned),Object(gfmTestStart),Object(gfmTestEnd),gfmTestKind);gfmTestChoice=0;gfmTestMissing=null;ClearGfmHighlight();RefreshWorkspaceFields();
            }));
            if(gfmTestPlans==null)return;
            if(gfmTestPlans.Count==0){c.AddLabel(WindowText("No matching path found for this direction and movement type.","Für diese Richtung und Fahrart keinen passenden Weg gefunden."));return;}
            c.AddSearchChoice(WindowText("Test path","Testweg"),gfmTestPlans.Select((p,i)=>WindowText("Path ","Weg ")+(i+1)+" · "+SignUnits.Number(p.Sum(r=>r.Length))+" m · "+string.Join(" → ",p.Select(r=>SignalCaption(r.Exit)))).ToList(),gfmTestChoice,i=>{gfmTestChoice=i;gfmTestMissing=null;ClearGfmHighlight();RefreshWorkspaceFields();});
            c.AddButton(WindowText("Show coverage and exact gaps","Abdeckung und genaue Lücken anzeigen"),()=>Act(()=>{
                RequireFreshGfmInspection();var path=gfmTestPlans[gfmTestChoice].SelectMany(r=>r.Path).Select(p=>p.Section).ToArray();
                gfmTestMissing=GfmCoverage.Gaps(path,GfmInspectionCoverage());
                HighlightGfm(desks.Blocks.Values.Where(b=>b.Sections.Any(s=>path.Any(s.Overlaps))).Select(b=>b.Id),gfmTestMissing);RefreshWorkspaceFields();
            }));
            if(gfmTestMissing!=null){c.AddLabel(WindowText("Uncovered intervals: ","Nicht abgedeckte Meterbereiche: ")+gfmTestMissing.Count);foreach(var gap in gfmTestMissing.Take(64))c.AddLabel(gap.Segment+" · "+SignUnits.Number(gap.From)+"–"+SignUnits.Number(gap.To)+" m");}
        }
    }
}
