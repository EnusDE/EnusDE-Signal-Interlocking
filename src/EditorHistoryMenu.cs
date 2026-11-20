using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        string areaId,areaCode="B1",areaName="Bereich 1",areaKind="Stellwerk";
        void UndoEditor(bool redo) {
            RequirePanelEdit();if(history==null)throw new InvalidOperationException(historyError??"Editorverlauf nicht verfügbar.");var current=history;var target=current.Target(panel,redo);EditPanel(doc=>{doc.Columns=target.Columns;doc.Rows=target.Rows;doc.OriginX=target.OriginX;doc.OriginY=target.OriginY;doc.Tiles=target.Tiles;doc.Areas=target.Areas;},false);current.Accept(panel,redo);ResetEditor();axisSelection.Clear();tileDraft=null;region=null;selectedX=selectedY=-1;columnsInput=panel.Columns.ToString();rowsInput=panel.Rows.ToString();status=redo?"Bearbeitung wiederhergestellt.":"Bearbeitung rückgängig gemacht.";
        }
        void BuildHistory(UI.Builder.UIPanelBuilder c) {
            c.HStack(row=>{row.AddButton(WindowText("Undo","Rückgängig"),()=>Act(()=>UndoEditor(false)));row.AddButton(WindowText("Redo","Wiederherstellen"),()=>Act(()=>UndoEditor(true)));});
        }
        void BuildNamedAreas(UIPanelBuilder c) {
            c.AddSection(LocalText.Get("Benannter Bereich"));var areas=panel.Areas.ToArray();c.AddSearchChoice(LocalText.Get("Bereich"),new[]{"Neuer markierter Bereich"}.Concat(areas.Select(a=>a.Code+" · "+a.Name)).ToList(),Array.FindIndex(areas,a=>a.Id==areaId)+1,i=>{if(i==0){areaId=null;}else {var a=areas[i-1];areaId=a.Id;areaCode=a.Code;areaName=a.Name;areaKind=a.Kind;region=new PanelRegion{X=a.X,Y=a.Y,Width=a.Width,Height=a.Height};}RefreshWorkspaceFields();panelView?.Refresh();});
            c.AddField(LocalText.Get("Bereichskennung"),c.AddInputField(areaCode,v=>areaCode=v,"B1",24));c.AddField(LocalText.Get("Bezeichnung"),c.AddInputField(areaName,v=>areaName=v,"Bahnhof / Strecke",64));
            var kinds=new List<string>{"Stellwerk","Strecke","Rangierbereich"};c.AddField(LocalText.Get("Art"),c.AddDropdown(kinds,Math.Max(0,kinds.IndexOf(areaKind)),i=>areaKind=kinds[i]));
            c.AddButton(LocalText.Get("Markierung als benannten Bereich speichern"),()=>Act(()=>{RequirePanelEdit();var selected=region??throw new InvalidOperationException("Zuerst einen Bereich markieren.");string id=areaId??Guid.NewGuid().ToString("N");EditPanel(doc=>{doc.Areas.RemoveAll(a=>a.Id==id);doc.Areas.Add(new PanelArea{Id=id,Code=(areaCode??"").Trim(),Name=(areaName??"").Trim(),Kind=areaKind,X=selected.X,Y=selected.Y,Width=selected.Width,Height=selected.Height});});areaId=id;status=LocalText.Get("Bereich bezeichnet. Signal-/Weichenzuordnung ergibt sich aus den Feldern innerhalb der Markierung.");}));
            if(region!=null)c.AddLabel(LocalText.Get("Signale in der Auswahl: ")+string.Join(", ",panel.Tiles.Where(t=>t.Kind==PanelTileKind.Signal&&region.Contains(t.X,t.Y)).Select(t=>Code(t.Reference)).Distinct()));
            c.AddLabel(LocalText.Get("Bereichsbezeichnungen ordnen die Tafel. Sie sind noch keine Spielerrechte- oder Blockfreigabe."));
        }
        void SetFollowingRoutes() {
            var first=routes.FirstOrDefault(r=>r.Id==routeId)??throw new InvalidOperationException("Erste Fahrstraße auswählen.");SetRoutePlan(RouteChains.Follow(routes,first));
        }
        void SetRoutePlan(RouteChainPlan plan) {
            RequireDispatcher();QueueSavedPlan(plan);
        }
        void ApplyRoutePlan(RouteChainPlan plan) {
            RequireDispatcher();StationAutomationPolicy.RequireControl(desks,deskId,plan.Routes);RequireManualStation(plan.Routes.First().Entry);
            foreach(var route in plan.Routes){RoutePointProtection.RequireCalculated(route);FlankProtection.RequireCalculated(route);}
            if(!aiEnabled)throw new InvalidOperationException("AE-Anbindung unter Auswahlfenster → Einstellungen einschalten.");foreach(var route in plan.Routes)if(route.Path==null||!SignalCatalogue.Supports(Object(route.Entry),route.Shunting,route.Reduced))throw new InvalidOperationException("Fahrweg oder Signalbegriff der Folge nicht gültig: "+route.Code);
            world.BeginRead();engine.RequestChain(plan.Routes,world,Entry.SettingsSnapshot().Markers.ProtectionWaitSeconds);lastRead=UnityEngine.Time.realtimeSinceStartup;nativeDirty=true;PublishNativeAspects();routeId=plan.Routes[0].Id;status=plan.Routes.Count+" Teilfahrstraße(n) angefordert bis "+Code(plan.Routes.Last().Exit)+". "+plan.EndReason+ManualCoverageNotice(plan.Routes);Entry.RecordSignalEvent("route_chain_requested",new{routes=plan.Routes.Select(r=>r.Id).ToArray(),reason=plan.EndReason});RefreshWorkspaceFields();
        }
    }
}
