using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        DeskStore deskStore;
        DeskLibrary desks;
        string deskId,deskCode="ORT1",deskName="Örtliches Stellwerk",releaseChoice="all",activePanelSignature;
        PanelRegion region;
        int regionAction,cornerX,cornerY;
        DeskDefinition CurrentDesk=>desks?.Desks.FirstOrDefault(d=>d.Id==deskId);
        string CurrentDeskLabel=>CurrentDesk==null?"Stellwerk wird geladen":CurrentDesk.Code+" · "+CurrentDesk.Name;
        static bool IsSetting(RouteRun run)=>run.Stage==RouteStage.Preparing||run.Stage==RouteStage.WaitingProtection||run.ReleaseSelection!=null||!run.ChainReady&&run.ChainId!=null;
        bool RouteHere(RouteDefinition route)=>CurrentDesk?.IsMaster==true||panel!=null&&panel.Tiles.Any(t=>t.Kind==PanelTileKind.Signal&&(t.Reference==route.Entry||t.AlternateSignal==route.Entry||
            route.Flanks?.SelectMany(f=>f.Signals).Any(s=>s.Id==t.Reference||s.Id==t.AlternateSignal)==true)||t.Kind==PanelTileKind.Switch&&route.Switches.Any(p=>p.Node==t.Reference));
        RouteRun[] ActiveHere()=>VisibleRuns.Where(r=>RouteHere(r.Route)).ToArray();
        void RefreshActiveInspector() {
            string signature=string.Join("|",VisibleRuns.Select(r=>r.Route.Id+":"+r.Stage+":"+r.ReleasedSections.Count+":"+r.ReleasedSwitches.Count+":"+r.ReleaseSelection));
            if(signature==activePanelSignature)return;activePanelSignature=signature;if(!editingWorkspace&&PanelVisible&&panelPage<4&&inspector!=null)RefreshWorkspaceFields();
        }
        void SelectDesk(string id,bool rebuild=true) {workspaceFit=true;workspaceContext=editingWorkspace;
            ResetEditor();axisSelection.Clear();panelWorld?.Dispose();panelWorld=new SignalWorldBridge(this,transform,2048);
            var desk=desks.Desks.Single(d=>d.Id==id);if(!editingWorkspace&&!desk.Global&&!desk.ShowInDispatcher)throw new InvalidOperationException("Dieses Stellwerk ist im Fahrdienstleiter ausgeblendet.");deskId=id;panel=DeskPanel(desk);columnsInput=panel.Columns.ToString();rowsInput=panel.Rows.ToString();
            history=null;historyError=null;if(Game.State.StateManager.IsHost)try{history=new PanelHistory(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(directory),"EditorHistory",scope,id),panel);}catch(Exception e){historyError=e.Message;log?.Warning("Editorverlauf: "+e.Message);}
            deskCode=desk.Code;deskName=desk.Name;selectedX=selectedY=-1;tileDraft=null;region=null;regionAction=0;tool=-1;panelButtons.Clear();panelPage=editingWorkspace?1:0;editorMode=1;
            panelReadings.Clear();panelReadTime=-100;nextPanelRead=0;panelScrollPosition=new UnityEngine.Vector2(0,1);
            if(rebuild)Rebuild(false);
        }
        void BuildDeskPicker(UIPanelBuilder c) {
            c.AddField(LocalText.Get("Status"),()=>panelError??status,UIPanelBuilder.Frequency.Periodic);
            if(desks==null){c.AddLabel(panelError??"Spielstand wird geladen.");return;}
            foreach(var desk in desks.Desks.Where(d=>editingWorkspace||d.Global||d.ShowInDispatcher).OrderByDescending(d=>d.IsMaster).ThenBy(d=>d.Code)) {
                string id=desk.Id;c.AddButton((desk.IsMaster?"ESTW · ":"Örtlich · ")+desk.Code+" · "+desk.Name,()=>Act(()=>SelectDesk(id)));
            }
            c.AddButton(WindowText("Close selection","Auswahl schließen"),()=>{CloseWorkspaceContext();Mode(0);});
        }
        void BuildRegionTools(UIPanelBuilder c) {
            string id=deskId;Func<bool> current=()=>deskId==id&&panelPage==1;
            c.AddSection(CurrentDeskLabel);
            if(CurrentDesk?.IsMaster==true)c.AddButton(LocalText.Get("Große ESTW-Fläche (mindestens 96 × 48)"),()=>Act(()=>{RequirePanelEdit();EditPanel(doc=>{doc.Columns=Math.Max(doc.Columns,96);doc.Rows=Math.Max(doc.Rows,48);});columnsInput=panel.Columns.ToString();rowsInput=panel.Rows.ToString();status=LocalText.Get("ESTW-Fläche erweitert. Vorhandene Felder bleiben an ihrer Position.");}));
            c.AddButton(LocalText.Get("Bereich markieren"),()=>{regionAction=1;tool=-1;region=null;status=LocalText.Get("Erste Ecke auf der Tafel anklicken.");});
            c.AddButton(LocalText.Get("Alles markieren"),SelectAll);
            if(region!=null)c.AddButton(LocalText.Get("Markierung aufheben"),()=>{region=null;regionAction=0;panelView?.Refresh();RefreshWorkspaceFields();});
            if(region!=null)c.AddButton(LocalText.Get("Bereich löschen …"),()=>Act(AskDelete));
            if(region!=null){c.AddLabel(LocalText.Get("Markiert: ")+region.Width+" × "+region.Height+" Felder");c.AddButton(LocalText.Get("Bereich verschieben"),()=>{regionAction=3;tool=-1;status=LocalText.Get("Neue linke obere Ecke anklicken. Andere Felder werden nicht überschrieben.");});}
            if(region!=null)BuildCircuitEditor(c,region);
            c.AddField(LocalText.Get("Stellwerkskennung"),c.AddInputField(deskCode,v=>{if(current())deskCode=v;},"ORT1",24));
            c.AddField(LocalText.Get("Name"),c.AddInputField(deskName,v=>{if(current())deskName=v;},"Örtliches Stellwerk",64));
            c.AddButton(LocalText.Get("Stellwerk umbenennen"),()=>Act(()=>{if(!current())return;RequirePanelEdit();desks=deskStore.Rename(desks,id,deskCode,deskName);status=LocalText.Get("Stellwerk umbenannt; feste ID bleibt erhalten.");Rebuild();}));
            c.AddButton(region==null?"Leeres Ortsstellwerk":"Bereich als Ortsstellwerk kopieren",()=>Act(()=>{if(!current())return;RequirePanelEdit();var copied=region==null?new PanelDocument{WorldKey=scope}:region.Extract(panel);desks=deskStore.CreateLocal(desks,deskCode,deskName,copied);SelectDesk(desks.Desks.Last().Id);status=LocalText.Get("Örtliches Gleisbild gespeichert. Über Stellwerke auswählen erreichbar.");}));
        }
        bool PressRegion(int x,int y) {
            if(regionAction==0)return false;
            if(regionAction==1){cornerX=x;cornerY=y;regionAction=2;status=LocalText.Get("Gegenüberliegende Ecke anklicken.");return true;}
            if(regionAction==2){region=new PanelRegion{X=Math.Min(x,cornerX),Y=Math.Min(y,cornerY),Width=Math.Abs(x-cornerX)+1,Height=Math.Abs(y-cornerY)+1};regionAction=0;status=LocalText.Get("Bereich markiert.");OpenWorkspaceContext();}
            else {var selected=region??throw new InvalidOperationException("Bereich neu markieren.");var growth=EditCanvas(new PanelRegion{X=x,Y=y,Width=selected.Width,Height=selected.Height},(doc,dx,dy)=>new PanelRegion{X=selected.X+dx,Y=selected.Y+dy,Width=selected.Width,Height=selected.Height}.Move(doc,x+dx,y+dy));region=new PanelRegion{X=x+growth.Left,Y=y+growth.Top,Width=selected.Width,Height=selected.Height};regionAction=0;status=LocalText.Get("Bereich mit allen Verknüpfungen verschoben.");}
            tileDraft=null;panelView?.Refresh();RefreshWorkspaceFields();return true;
        }
        string RouteStatus(RouteRun run) {
            string stage=run.Stage==RouteStage.Preparing?"läuft ein":run.Stage==RouteStage.WaitingProtection?"Schutzwartezeit / Freiprüfung":run.Stage==RouteStage.Set?"festgelegt":run.Stage==RouteStage.Running?"befahren":run.Stage==RouteStage.Fault?"Störung / Verschluss bleibt":"Halt / Restfestlegung";
            return run.Route.Code+": "+stage+" · "+run.Reason+"\n"+(string.IsNullOrEmpty(run.FaultReason)?"":"Erster Fehler: "+run.FaultReason+"\n")+string.Join(" · ",run.PointStates.Select(p=>PointAssignments.Caption(desks,p.Key)+": "+p.Value))+"\nFrei: "+run.ReleasedSwitches.Count+"/"+run.Route.Switches.Count+" Weichen, "+run.ReleasedSections.Count+"/"+run.Route.Sections.Count+" Abschnitte"+(string.IsNullOrEmpty(run.ReleaseStatus)?"":"\nHilfsauflösung: "+run.ReleaseStatus);
        }
        void BuildRelease(UIPanelBuilder c,RouteRun run) {
            c.AddField(LocalText.Select("Release status","Auflösungsstatus"),()=>ReleaseProgress(run),UIPanelBuilder.Frequency.Periodic);
            if(!Game.State.StateManager.IsHost)return;
            var ids=new[]{"all"}.Concat(run.Route.Switches.Where(p=>!run.ReleasedSwitches.Contains(p.Node)).Select(p=>"point:"+p.Node)).Concat(run.HeldSections.Select(s=>"section:"+s.Key)).ToArray();
            if(!ids.Contains(releaseChoice))releaseChoice="all";
            c.AddSearchChoice(LocalText.Get("Hilfsauflösung"),ids.Select(s=>s=="all"?"Gesamte Restfestlegung":s.StartsWith("point:")?"Weiche "+s.Substring(6):"Abschnitt "+s.Substring(8)).ToList(),Array.IndexOf(ids,releaseChoice),i=>releaseChoice=ids[i]);
            c.AddButton(LocalText.Get("Halt + geprüfte Hilfsauflösung anfordern"),()=>Act(()=>{RequireDispatcher();RequireOperatorSignal(run.Route.Entry,run.Route.Shunting);engine.RequestRelease(run.Route.Id,releaseChoice,Entry.SettingsSnapshot().Markers.ProtectionWaitSeconds);status=LocalText.Get("Halt gesetzt. Hilfsauflösung wartet auf Schutzzeit und bestätigte freie Bereiche / Annäherung.");RefreshWorkspaceFields();}));
            if(run.ReleaseSelection!=null&&!run.OperatorReleaseConfirmed){string selection=run.ReleaseSelection;double deadline=run.ReleaseAt;c.AddButton(LocalText.Get("Örtlich geprüft · ")+(selection=="all"?"gesamte Restfestlegung":selection)+" bestätigen",()=>Act(()=>{RequireDispatcher();RequireOperatorSignal(run.Route.Entry,run.Route.Shunting);if(!engine.Active.Any(r=>ReferenceEquals(r,run))||run.ReleaseSelection!=selection||run.ReleaseAt!=deadline)throw new InvalidOperationException("Hilfsauflösung inzwischen geändert. Aktuellen Bereich erneut prüfen.");engine.ConfirmOperatorRelease(run.Route.Id);Entry.RecordSignalEvent("operator_release_confirmed",new{id=run.Route.Id,selection=selection,operator_confirmed=true,physical_occupancy_overridden=false});status=LocalText.Get("Fdl-Bestätigung gespeichert. Nach Schutzzeit wird nur der gewählte eigene Verschluss gelöst. Belegungsanzeige bleibt unverändert.");RefreshWorkspaceFields();}));}
            c.AddLabel(LocalText.Get("Bestätigen heißt: Der Fdl hat den gewählten Bereich selbst geprüft. Native Spielbelegung und CTC-Sperren werden dadurch nicht geändert."));
        }
        void BuildRouteWait(UIPanelBuilder c,RouteDefinition route) {
            if(route==null)return;
            string input=route.ProtectionWaitSeconds<0?"":SignUnits.Number(route.ProtectionWaitSeconds);
            c.AddField(LocalText.Get("Schutzwartezeit dieser Fahrstraße (s)"),c.AddInputField(input,v=>input=v,"Leer: Host-Vorgabe, Standard 15",8));
            c.AddButton(LocalText.Get("Schutzwartezeit speichern"),()=>Act(()=>{
                if(routeId!=route.Id||!routes.Any(r=>ReferenceEquals(r,route)))return;RequirePanelEdit();if(engine.Active.Any(r=>r.Route.Id==route.Id))throw new InvalidOperationException("Aktive Fahrstraße zuerst sicher auflösen.");
                double wait=-1;if(!string.IsNullOrWhiteSpace(input)&&(!double.TryParse(input.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out wait)||double.IsNaN(wait)||double.IsInfinity(wait)||wait<0||wait>300))throw new InvalidOperationException("0–300 Sekunden oder leer für Host-Vorgabe.");
                var next=routes.Select(r=>r.Clone()).ToList();var selected=next.Single(r=>r.Id==route.Id);selected.ProtectionWaitSeconds=wait;store.Save(next);routes=next;status=LocalText.Get("Zusätzliche Schutzwartezeit gespeichert. Endlage, Belegung und Verschluss werden unabhängig geprüft.");Rebuild();
            }));
        }
    }
}
