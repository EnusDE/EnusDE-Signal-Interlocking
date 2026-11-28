using System;
using System.Collections.Generic;
using System.Linq;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public enum OperatorDuty { Observe, Standby, Shunt, Manual }

    public sealed class OperatorScope {
        public readonly string Key,Name;
        public readonly OperatorDuty Duty;
        readonly HashSet<string> signals,points,destinations;
        public OperatorScope(string key,string name,OperatorDuty duty,IEnumerable<string> signals,IEnumerable<string> points,IEnumerable<string> destinations=null) {
            Key=key;Name=name;Duty=duty;
            this.signals=new HashSet<string>(signals.Where(id=>!string.IsNullOrEmpty(id)),StringComparer.Ordinal);
            this.points=new HashSet<string>(points.Where(id=>!string.IsNullOrEmpty(id)),StringComparer.Ordinal);
            if(destinations!=null)this.destinations=new HashSet<string>(destinations.Where(id=>!string.IsNullOrEmpty(id)),StringComparer.Ordinal);
        }
        public bool Accepts(string signal,bool shunting)=>signals.Contains(signal)&&(Duty==OperatorDuty.Manual||Duty==OperatorDuty.Shunt&&shunting);
        public bool AcceptsPoint(string point)=>points.Contains(point)&&(Duty==OperatorDuty.Manual||Duty==OperatorDuty.Shunt);
        public bool AcceptsRoute(RouteDefinition route)=>Accepts(route.Entry,route.Shunting)&&(destinations==null||destinations.Contains(route.Exit)||destinations.Contains(route.LineTarget));
        public static OperatorScope From(DeskDefinition desk,PanelArea area,OperatorDuty duty) {
            var tiles=desk.Panel.Tiles.Where(t=>area==null||area.Contains(t.X,t.Y)).ToArray();
            return new OperatorScope(desk.Id+":"+(area?.Id??""),desk.Code+(area==null?"":" · "+area.Name),duty,
                tiles.Where(t=>t.Kind==PanelTileKind.Signal&&!t.ReadOnlySignal).SelectMany(t=>new[]{t.Reference,t.AlternateSignal}),
                tiles.Where(t=>t.Kind==PanelTileKind.Switch&&!t.ReadOnlySignal).Select(t=>t.Reference),
                area==null?null:tiles.SelectMany(t=>t.Kind==PanelTileKind.Signal?new[]{t.Reference,t.AlternateSignal,t.Target?.Id}:new[]{t.Target?.Id}));
        }
    }

    public sealed partial class SignalRuntime {
        readonly Dictionary<string,OperatorScope> operatorScopes=new Dictionary<string,OperatorScope>();
        OperatorDuty operatorDuty;
        bool operatorControlRequested;
        string operatorDesk,operatorArea;
        DeskLibrary operatorLibrary;
        bool OperatorAuthorityCurrent=>StateManager.IsHost&&ReferenceEquals(desks,operatorLibrary);
        void ReleaseOperatorScope(){operatorScopes.Clear();operatorDuty=OperatorDuty.Observe;operatorLibrary=null;}
        bool SessionAutomaticHeld(string signal,bool shunting)=>OperatorAuthorityCurrent&&operatorScopes.Values.Any(hold=>hold.Accepts(signal,shunting));
        string OperatorTakeoverReason()=>WindowText("Movement is under manual control.","Für diese Fahrart ist der Bereich manuell übernommen.");
        bool SessionDispatchActive=>workspaceSessionOpen&&operatorControlRequested&&!editingWorkspace;
        bool SessionRequiresExplicitScope=>SessionDispatchActive||operatorScopes.Count!=0;
        void RequireOperatorSignal(string signal,bool shunting) {
            if(SessionRequiresExplicitScope&&!SessionAutomaticHeld(signal,shunting))
                throw new InvalidOperationException(WindowText("Take over this area and movement type first.","Übernimm zuerst diesen Bereich und die passende Fahrart."));
        }
        void RequireOperatorPoint(string point) {
            if(SessionRequiresExplicitScope&&(!OperatorAuthorityCurrent||!operatorScopes.Values.Any(hold=>hold.AcceptsPoint(point))))
                throw new InvalidOperationException(WindowText("Take over the point's area first.","Übernimm zuerst den Bereich dieser Weiche."));
        }
        void RequireOperatorRoute(RouteDefinition route) {
            if(SessionRequiresExplicitScope&&(!OperatorAuthorityCurrent||!operatorScopes.Values.Any(hold=>hold.AcceptsRoute(route))))
                throw new InvalidOperationException(WindowText("The route exceeds the current takeover or uses a different movement type.","Die Fahrstraße reicht über die Übernahme hinaus oder nutzt eine andere Fahrart."));
        }
        void BuildOperatorScope(UIPanelBuilder c) {
            c.AddSection(WindowText("Operating responsibility","Bedienzuständigkeit"));
            c.AddField(WindowText("Mode","Modus"),()=>OperatorDutyName(operatorDuty),UIPanelBuilder.Frequency.Periodic);
            if(!StateManager.IsHost)return;
            c.HStack(row=>{
                row.AddButton(WindowText("Observe","Beobachten"),()=>{ReleaseOperatorScope();RefreshWorkspaceFields();});
                row.AddButton(WindowText("Standby","Bereitschaft"),()=>{ReleaseOperatorScope();operatorDuty=OperatorDuty.Standby;RefreshWorkspaceFields();});
            });
            var sites=desks.Desks.Where(d=>!d.IsMaster&&(CurrentDesk?.IsMaster==true||d.Id==deskId)).OrderBy(d=>d.Code).ToArray();
            if(sites.Length==0) {
                var global=CurrentDesk;
                if(global!=null&&desks.Desks.All(d=>d.IsMaster))sites=new[]{global};
            }
            if(sites.Length==0)return;
            var site=sites.FirstOrDefault(d=>d.Id==operatorDesk)??sites[0];
            if(sites.Length>1)c.AddSearchChoice(WindowText("Interlocking","Stellwerk"),sites.Select(d=>d.Code+" · "+d.Name).ToList(),
                Array.IndexOf(sites,site),i=>{operatorDesk=sites[i].Id;operatorArea=null;RefreshWorkspaceFields();});
            var areas=site.Panel.Areas.Where(a=>a.BlockLine==null&&a.NumberLine==null&&string.IsNullOrEmpty(a.Desk)).ToArray();
            c.AddSearchChoice(WindowText("Area","Bereich"),new[]{WindowText("Entire interlocking","Ganzes Stellwerk")}.Concat(areas.Select(a=>a.Name)).ToList(),
                Array.FindIndex(areas,a=>a.Id==operatorArea)+1,i=>{operatorArea=i==0?null:areas[i-1].Id;RefreshWorkspaceFields();});
            var area=areas.FirstOrDefault(a=>a.Id==operatorArea);
            void TakeOver(OperatorDuty duty) {
                RequireDispatcher();
                if(!desks.Desks.Contains(site)||area!=null&&!site.Panel.Areas.Contains(area))throw new InvalidOperationException(WindowText("Choose the area again.","Wähle den Bereich erneut."));
                if(!OperatorAuthorityCurrent)ReleaseOperatorScope();
                var grant=OperatorScope.From(site,area,duty);
                operatorControlRequested=true;
                operatorScopes[grant.Key]=grant;operatorDuty=duty;operatorLibrary=desks;
                panelButtons.Clear();
                status=OperatorDutyName(duty)+" · "+grant.Name;
                RefreshWorkspaceFields();
            }
            c.AddButton(WindowText("Take over shunting","Rangieren übernehmen"),()=>Act(()=>TakeOver(OperatorDuty.Shunt)));
            c.AddButton(WindowText("Take over all movements","Bereich vollständig übernehmen"),()=>Act(()=>TakeOver(OperatorDuty.Manual)));
            foreach(var grant in operatorScopes.Values.ToArray())c.AddButton(WindowText("Return to automation · ","An Automatik zurückgeben · ")+grant.Name,()=>{
                operatorScopes.Remove(grant.Key);
                operatorDuty=operatorScopes.Count==0?OperatorDuty.Observe:operatorScopes.Values.Last().Duty;panelButtons.Clear();RefreshWorkspaceFields();
            });
            if(operatorDuty==OperatorDuty.Standby) {
                c.AddField(WindowText("Latest pending request","Letzte wartende Anfrage"),()=>automaticRequests.LastOrDefault(r=>r.Stage==AutomaticRequestStage.Waiting)?.Reason??"",
                    UIPanelBuilder.Frequency.Periodic);
            }
        }
        string OperatorDutyName(OperatorDuty duty)=>duty switch {
            OperatorDuty.Shunt=>WindowText("Shunting takeover","Rangierübernahme"),
            OperatorDuty.Manual=>WindowText("Manual operation","Manuelle Bedienung"),
            OperatorDuty.Standby=>WindowText("Standby","Bereitschaft"),
            _=>WindowText("Observe","Beobachten")
        };
    }
}
