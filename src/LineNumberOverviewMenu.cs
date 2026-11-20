using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string overviewLine,overviewDesk,overviewGroup;int overviewGeneration;
        readonly List<string> overviewSources=new List<string>();
        void LoadOverviewSources(LineTrackDefinition line,string group=null) {
            overviewGeneration++;overviewGroup=group;overviewSources.Clear();
            var doc=desks.Desks.First(d=>d.Id==overviewDesk).Panel;var area=group==null?null:LineNumberOverview.Area(doc,group);
            if(area!=null) {
                var index=new NumberMirrorIndex(desks);
                foreach(var field in doc.Tiles.Where(t=>area.Contains(t.X,t.Y)&&NumberMirrors.IsMirror(t)).OrderBy(t=>t.Y).ThenBy(t=>t.X)) {
                    var source=index.Source(field);overviewSources.Add(source==null?"":LineNumberOverview.Key(source));
                }
            } else overviewSources.AddRange(LineNumberOverview.AutomaticSources(desks,line).Take(LineNumberOverview.MaxFields).Select(LineNumberOverview.Key));
            if(overviewSources.Count==0)overviewSources.Add("");
        }
        void CommitNumberOverview(DeskLibrary next) {
            var before=panel;deskStore.Save(next);desks=next;panel=DeskPanel(CurrentDesk);
            if(history!=null&&PanelHistory.Hash(before)!=PanelHistory.Hash(panel))try{history.Record(before,panel);}catch(Exception e){historyError=e.Message;}
            panelWorld?.Dispose();panelWorld=new SignalWorldBridge(this,transform,2048);panelReadings.Clear();panelReadTime=-100;nextPanelRead=0;
            panelView?.ReplaceDocument(panel);RefreshWorkspaceFields();
        }
        void BuildLineNumberOverview(UIPanelBuilder c,LineTrackDefinition draft,Func<bool> current) {
            var line=desks.FreeLines.FirstOrDefault(l=>l.Id==draft.Id);if(line==null)return;
            c.AddSection(WindowText("Compact train-number overview","Kompakte Zugnummernübersicht"));
            c.AddLabel(WindowText("Optional read-only number strip. No additional block installation or full line copy is required at the receiving interlocking. The existing source fields provide the numbers; this view grants no route authority.","Optionale, schreibgeschützte Nummernleiste. Am empfangenden Stellwerk ist dafür keine zusätzliche Blockanlage oder vollständige Streckenkopie nötig. Die vorhandenen Quellenfelder liefern die Nummern; diese Ansicht erteilt keine Fahrtfreigabe."));
            if(overviewLine!=line.Id||!desks.Desks.Any(d=>d.Id==overviewDesk)){overviewLine=line.Id;overviewDesk=deskId;LoadOverviewSources(line);}
            if(overviewGroup!=null&&!desks.Desks.Single(d=>d.Id==overviewDesk).Panel.Areas.Any(a=>a.Id==overviewGroup&&a.NumberLine==line.Id))LoadOverviewSources(line);
            int generation=overviewGeneration;bool fresh()=>current()&&generation==overviewGeneration;
            var targets=desks.Desks.OrderBy(d=>d.Code).ToArray();
            c.AddSearchChoice(WindowText("Display on interlocking","Anzeigen auf Stellwerk"),targets.Select(d=>d.Code+" · "+d.Name).ToList(),Array.FindIndex(targets,d=>d.Id==overviewDesk),i=>{if(!fresh())return;overviewDesk=targets[i].Id;LoadOverviewSources(line);RefreshWorkspaceFields();});
            var doc=desks.Desks.Single(d=>d.Id==overviewDesk).Panel;var groups=doc.Areas.Where(a=>a.NumberLine==line.Id).OrderBy(a=>a.Code).ToArray();
            c.AddSearchChoice(WindowText("Number overview","Zugnummernübersicht"),new[]{WindowText("Create new overview","Neue Übersicht anlegen")}.Concat(groups.Select(a=>a.Code+" · "+a.Name)).ToList(),Array.FindIndex(groups,a=>a.Id==overviewGroup)+1,i=>{if(!fresh())return;LoadOverviewSources(line,i==0?null:groups[i-1].Id);RefreshWorkspaceFields();});
            c.AddField(WindowText("Number of fields","Anzahl der Felder"),c.AddDropdown(Enumerable.Range(1,LineNumberOverview.MaxFields).Select(i=>i.ToString()).ToList(),Math.Min(LineNumberOverview.MaxFields,overviewSources.Count)-1,i=>{
                if(!fresh())return;overviewGeneration++;int count=i+1;while(overviewSources.Count>count)overviewSources.RemoveAt(overviewSources.Count-1);while(overviewSources.Count<count)overviewSources.Add("");RefreshWorkspaceFields();
            }));
            void Automatic(bool reverse) {
                if(!fresh())return;var found=LineNumberOverview.AutomaticSources(desks,line,reverse).Take(overviewSources.Count).Select(LineNumberOverview.Key).ToArray();
                if(found.Length==0)throw new InvalidOperationException(WindowText("No unambiguous block number sources found. Choose the original fields below, or create them in your own diagram first.","Keine eindeutigen Block-Nummernquellen gefunden. Unten die ursprünglichen Felder wählen oder diese zuerst im eigenen Gleisbild anlegen."));
                overviewGeneration++;overviewSources.Clear();overviewSources.AddRange(found);RefreshWorkspaceFields();
            }
            c.AddButton(WindowText("Use block order A → B","Blockreihenfolge A → B übernehmen"),()=>Act(()=>Automatic(false)));
            c.AddButton(WindowText("Use block order B → A","Blockreihenfolge B → A übernehmen"),()=>Act(()=>Automatic(true)));
            c.AddLabel(WindowText("A / B follow the line's configured neighbours. Each slot can also mirror an original field from a manually built diagram. For a one-field arrival announcement choose the last block in the travel direction.","A / B entsprechen den eingerichteten Nachbarbetriebsstellen. Jeder Platz kann auch ein ursprüngliches Feld eines selbst gebauten Gleisbilds spiegeln. Für eine einfeldrige Ankunftsvoranzeige den letzten Block in Fahrtrichtung wählen."));
            c.AddField(WindowText("Neighbour A","Nachbarbetriebsstelle A"),LineStationCode(line.StationA));
            c.AddField(WindowText("Neighbour B","Nachbarbetriebsstelle B"),LineStationCode(line.StationB));
            var sources=LineNumberOverview.Sources(desks);
            string Caption(NumberMirrorSource s)=>s.Desk.Code+" · "+(!string.IsNullOrWhiteSpace(s.Tile.Label)?s.Tile.Label:s.Tile.BlockId!=""&&desks.Blocks.TryGetValue(s.Tile.BlockId,out var block)?block.Name:WindowText("Number field","Zugnummernfeld"))+" · "+(s.Tile.X+1)+" / "+(s.Tile.Y+1);
            for(int slot=0;slot<Math.Min(LineNumberOverview.MaxFields,overviewSources.Count);slot++) {
                int n=slot;int selected=Array.FindIndex(sources,s=>LineNumberOverview.Key(s)==overviewSources[n]);
                c.AddSearchChoice(WindowText("Field ","Feld ")+(n+1)+WindowText(" · left to right"," · von links nach rechts"),new[]{WindowText("Choose source / source missing","Quelle wählen / Quelle fehlt")}.Concat(sources.Select(Caption)).ToList(),selected+1,i=>{if(!fresh())return;overviewSources[n]=i==0?"":LineNumberOverview.Key(sources[i-1]);overviewGeneration++;RefreshWorkspaceFields();});
            }
            c.AddButton(WindowText("Preview and place number overview","Zugnummernübersicht als Vorschau platzieren"),()=>Act(()=>{
                if(!fresh())return;RequirePanelEdit();var selected=overviewSources.Select(key=>sources.FirstOrDefault(s=>LineNumberOverview.Key(s)==key)).ToArray();
                var next=LineNumberOverview.Create(desks,line.Id,overviewDesk,selected,overviewGroup);
                var group=overviewGroup??next.Desks.Single(d=>d.Id==overviewDesk).Panel.Areas.Single(a=>a.NumberLine==line.Id&&!doc.Areas.Any(old=>old.Id==a.Id)).Id;
                OpenNumberOverviewPlacement(next,overviewDesk,group);
            }));
            if(overviewGroup!=null)c.AddButton(WindowText("Remove this overview only","Nur diese Übersicht entfernen"),()=>Act(()=>{
                if(!fresh())return;RequirePanelEdit();var next=LineNumberOverview.Delete(desks,overviewDesk,overviewGroup);LoadOverviewSources(line);CommitNumberOverview(next);status=WindowText("Number overview removed. Sources and block installation retained.","Zugnummernübersicht entfernt. Quellen und Blockanlage bleiben erhalten.");RefreshWorkspaceFields();
            }));
            c.AddLabel(WindowText("After placing: drag any field in Select mode to move the complete strip. Missing sources display ?. Number transfer into a station track still requires the existing verified passage and receiving-field setup.","Nach dem Platzieren: Im Auswahlmodus ein Feld greifen, um die ganze Leiste zu verschieben. Fehlende Quellen zeigen ?. Die Nummernübergabe in ein Bahnhofsgleis benötigt weiterhin die bestehende bestätigte Durchfahrt und eingerichtete Empfangsfelder."));
        }
    }
}
