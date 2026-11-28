using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Game.State;
using Newtonsoft.Json;
using UI.Builder;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const int WebPort=47327;
        const string WebJob="browser/preview";
        InterlockingWebServer web;
        string webError,webScope;
        WebBoard webBoard;
        WebPreview webPreview;
        SignalWorldBridge webWorld;

        sealed class WebAction {
            public string Action {get;set;}
            public string Desk {get;set;}
            public string Start {get;set;}
            public string Target {get;set;}
            public string Movement {get;set;}
            public long Revision=-1,Installation=-1;
            public int X=-1,Y=-1;
        }
        sealed class WebPreview {
            public string Id=Guid.NewGuid().ToString("N"),Desk,Stage="working",Message;
            public long Revision,Installation;
            public double Expires;
            public RouteChainPlan Plan;
            public int[] Highlight=Array.Empty<int>();
        }
        sealed class WebBoard {
            public readonly string Layout=Guid.NewGuid().ToString("N");
            public DeskLibrary Library;
            public DeskDefinition Desk;
            public PanelDocument Panel;
            public Dictionary<int,PanelTile> Cells;
            public object Geometry;
            public readonly Dictionary<string,(TrackOccupancy State,double Time)> Reads=new Dictionary<string,(TrackOccupancy,double)>();
            public KeyValuePair<string,TrackSection>[] Sensors;
            public int Next;
            public int FirstX,FirstY;
            public double LastRequest,NextSweep;
        }

        bool WebAvailable=>StateManager.IsHost&&OperatingDataReady&&WorkspaceAvailable;
        bool WebMayDispatch=>WebAvailable&&OperatingReady&&aiEnabled&&!Entry.BuildModeActive&&(!editingWorkspace||!PanelVisible)&&worldSignalPick==null&&worldPointSelection==null;
        bool WebMayRelease=>WebAvailable&&!Entry.BuildModeActive&&(!editingWorkspace||!PanelVisible)&&worldSignalPick==null&&worldPointSelection==null;
        string WebDispatchIssue()=>!aiEnabled?WindowText("Enable Auto Engineer signal connection in the game settings.","Aktiviere die Auto-Engineer-Signalanbindung in den Spieleinstellungen."):
            Entry.BuildModeActive||editingWorkspace&&PanelVisible||worldSignalPick!=null||worldPointSelection!=null?
            WindowText("Finish editing or building in the game before setting a route.","Beende die Bearbeitung oder den Baumodus im Spiel, bevor du eine Fahrstraße stellst."):
            WindowText("The interlocking is not ready.","Das Stellwerk ist noch nicht betriebsbereit.");

        internal void DrawWebSettings(UIPanelBuilder b) {
            b.AddField(WindowText("Status","Status"),()=>web?.Running==true?web.Address:webError??WindowText("Off","Aus"),UIPanelBuilder.Frequency.Periodic);
            if(!StateManager.IsHost){b.AddLabel(WindowText("Only the game host can start browser access.","Nur der Spielhost kann den Browserzugang starten."));return;}
            if(!WebAvailable){b.AddLabel(WindowText("Load an unlocked interlocking first.","Lade zuerst einen Spielstand mit freigeschalteter Stellwerktechnik."));return;}
            if(web?.Running==true) {
                b.AddButton(WindowText("Open in browser","Im Browser öffnen"),()=>{if(web?.Running==true)Application.OpenURL(web.Link);});
                b.AddButton(WindowText("Copy access link","Zugangslink kopieren"),()=>{if(web?.Running==true)GUIUtility.systemCopyBuffer=web.Link;});
                b.AddButton(WindowText("Turn off browser access","Browserzugang ausschalten"),()=>{StopWeb();b.SetNeedsRebuild();});
                return;
            }
            b.AddButton(WindowText("Start on this PC","Auf diesem PC starten"),()=>{StartWeb(IPAddress.Loopback);b.SetNeedsRebuild();});
            b.AddLabel(WindowText("For a trusted home network, choose an address and share the private access link with the operator. Access expires when stopped or on world change. Home network connections use unencrypted HTTP.",
                "Wähle für ein vertrautes Heimnetz eine Adresse und teile den privaten Zugangslink mit der bedienenden Person. Der Zugang endet beim Ausschalten oder Weltwechsel. Die Heimnetzverbindung nutzt unverschlüsseltes HTTP."));
            try {
                foreach(var address in NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up)
                    .SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address).Where(InterlockingWebServer.PrivateAddress).Distinct().Take(8)) {
                    var selected=address;
                    b.AddButton(WindowText("Start on home network · ","Im Heimnetz starten · ")+selected,()=>{StartWeb(selected);b.SetNeedsRebuild();});
                }
            }catch(NetworkInformationException){}
        }

        void StartWeb(IPAddress address) {
            if(!WebAvailable)return;
            StopWeb();
            try {
                using(var html=typeof(SignalRuntime).Assembly.GetManifestResourceStream("EnusDE.Interlocking.html"))
                using(var buffer=new MemoryStream()) {
                    html.CopyTo(buffer);web=new InterlockingWebServer(address,WebPort,buffer.ToArray());
                }
                webScope=scope;
                if(IPAddress.IsLoopback(address))Application.OpenURL(web.Link);
            }catch(Exception error) {
                StopWeb();
                webError=WindowText("Browser access could not start: ","Browserzugang konnte nicht starten: ")+error.Message;
            }
        }
        void StopWeb() {
            var server=web;
            web=null;webScope=null;webError=null;
            if(server==null)return;
            server.Dispose();
            ResetWebPreview();
            webBoard=null;webWorld?.Dispose();webWorld=null;
        }
        void ResetWebPreview(){webPreview=null;routeWork.Cancel(WebJob);}
        void TickWeb() {
            if(web==null)return;
            if(!WebAvailable||scope!=webScope||!web.Running){StopWeb();return;}
            if(webPreview!=null&&Time.realtimeSinceStartup>webPreview.Expires)ResetWebPreview();
            web.Pump((path,data,layout,view)=>path=="/state"?ReadWeb(data,layout,view):HandleWeb(JsonConvert.DeserializeObject<WebAction>(data,new JsonSerializerSettings{MaxDepth=6})));
            if(webBoard!=null&&Time.realtimeSinceStartup-webBoard.LastRequest<4&&ReferenceEquals(webBoard.Library,desks)) {
                try{TickWebSensors(webBoard);}catch(Exception){webBoard.Reads.Clear();}
            }
        }

        DeskDefinition WebDesk(string id)=>desks.Desks.FirstOrDefault(d=>d.Id==id&&(d.Global||d.ShowInDispatcher));
        int WebCell(int x,int y)=>y*PanelDocument.MaxColumns+x;
        WebBoard WebDeskBoard(DeskDefinition desk) {
            if(webBoard!=null&&ReferenceEquals(webBoard.Library,desks)&&webBoard.Desk.Id==desk.Id)return webBoard;
            var diagram=desk.IsMaster?MasterDiagram.Project(desks,desk.Id):desk.Panel;
            var result=new WebBoard{Desk=desk,Panel=diagram,Library=desks};
            result.Cells=diagram.Tiles.ToDictionary(t=>WebCell(t.X,t.Y));
            if(diagram.Tiles.Count>0){result.FirstX=diagram.Tiles.Min(t=>t.X);result.FirstY=diagram.Tiles.Min(t=>t.Y);}
            var sourceCells=ReferenceEquals(diagram,desk.Panel)?result.Cells:desk.Panel.Tiles.ToDictionary(t=>WebCell(t.X,t.Y));
            var sensors=new Dictionary<string,TrackSection>();
            foreach(var tile in diagram.Tiles) {
                if(tile.Section!=null)sensors[tile.Section.Key]=tile.Section;
                foreach(string id in TileBlocks(tile))if(DisplayMonitored(id)&&desks.Blocks.ContainsKey(id))sensors["block:"+id]=null;
            }
            result.Sensors=sensors.ToArray();
            result.Geometry=diagram.Tiles.Select(t=>new {
                cell=WebCell(t.X,t.Y),x=t.X,y=t.Y,kind=(int)t.Kind,label=t.Label,ports=RailPorts(t),
                swap=t.SwapSignalHalves,mirror=t.Mirror,rotation=t.Rotation,
                alternate=t.Kind==PanelTileKind.Signal&&t.AlternateSignal!=null,
                auxiliary=t.Target!=null,
                mainDirection=t.Kind==PanelTileKind.Signal?SignalCellLayout.MainDirection(t):0,
                shuntDirection=t.Kind==PanelTileKind.Signal?SignalCellLayout.ShuntDirection(t):0,
                targetDirection=t.Kind==PanelTileKind.Signal?SignalCellLayout.AuxiliaryDirection(t):SignalCellLayout.TargetDirection(t),
                targetFirst=t.Kind==PanelTileKind.Signal&&SignalCellLayout.AuxiliaryAt(t,false),
                editable=sourceCells.TryGetValue(WebCell(t.X,t.Y),out var own)&&own.SameContent(t)
            }).ToArray();
            webWorld?.Dispose();webWorld=new SignalWorldBridge(this,transform,2048);
            return webBoard=result;
        }
        void TickWebSensors(WebBoard board) {
            double now=Time.realtimeSinceStartup;
            if(board.Sensors.Length==0||now<board.NextSweep)return;
            webWorld.BeginRead();
            long start=Stopwatch.GetTimestamp();
            for(int i=0;i<12;i++) {
                var sensor=board.Sensors[board.Next++];
                TrackOccupancy read;
                try {
                    read=sensor.Value!=null?webWorld.Occupancy(sensor.Value):
                        webWorld.Block(ReadPointDefinition(desks.Blocks[sensor.Key.Substring(6)]),out _);
                }catch(Exception){read=TrackOccupancy.Unknown;}
                board.Reads[sensor.Key]=(read,now);
                if(board.Next==board.Sensors.Length){board.Next=0;board.NextSweep=now+1;return;}
                if((Stopwatch.GetTimestamp()-start)/(double)Stopwatch.Frequency>=.001)return;
            }
        }
        TrackOccupancy WebOccupancy(WebBoard board,string key)=>
            board.Reads.TryGetValue(key,out var sample)&&Time.realtimeSinceStartup-sample.Time<5?sample.State:TrackOccupancy.Unknown;
        object WebSignal(string id,bool readOnly,bool shuntHalf=false) {
            var signal=Object(id);
            if(signal?.IsSignal!=true)return null;
            bool present=ButtonSignal(id);
            bool enabled=!readOnly&&present;
            return new {id,name=SignalCaption(id),aspect=Aspect(id).ToString(),
                type=signal.Kind==MarkerRecord.Shunt?"shunt":signal.Kind==MarkerRecord.Distant?"distant":"main",
                train=enabled&&!shuntHalf&&SignalCatalogue.Supports(signal,false,false),
                slow=enabled&&!shuntHalf&&SignalCatalogue.Supports(signal,false,true),
                shunt=enabled&&SignalCatalogue.Supports(signal,true,false),
                arrival=present,shuntHalf};
        }
        PanelRailInk WebInk(WebBoard board,PanelTile tile) {
            var readings=tile.Section!=null?new[]{WebOccupancy(board,tile.Section.Key)}:TileBlocks(tile).Select(id=>WebOccupancy(board,"block:"+id)).ToArray();
            var occupancy=readings.Contains(TrackOccupancy.Occupied)?TrackOccupancy.Occupied:
                readings.Length>0&&readings.All(r=>r==TrackOccupancy.Clear)?TrackOccupancy.Clear:TrackOccupancy.Unknown;
            return PanelRouteStyle.Resolve(occupancy,TileRouteKind(tile));
        }
        object WebField(WebBoard board,PanelTile t) {
            int ports=RailPorts(t);
            var ink=ports==0?PanelRailInk.Free:WebInk(board,t);
            Dictionary<int,int> portInk=null;
            if(t.Kind==PanelTileKind.Signal||t.Kind==PanelTileKind.TrainNumber||t.CircuitRails!=null) {
                portInk=new Dictionary<int,int>();
                for(int port=0;port<8;port++) {
                    if((ports&(1<<port))==0)continue;
                    if(t.CircuitRails!=null&&t.CircuitRails.TryGetValue(LocalPort(t,port),out string block)) {
                        routeBlockKinds.TryGetValue(block,out var kind);
                        portInk[port]=(int)PanelRouteStyle.Resolve(WebOccupancy(board,"block:"+block),kind);
                    } else if(t.Section==null&&t.BlockId=="") {
                        int x=t.X+PanelConnections.Dx[port],y=t.Y+PanelConnections.Dy[port];
                        if(x<0||y<0||x>=board.Panel.Columns||y>=board.Panel.Rows||!board.Cells.TryGetValue(WebCell(x,y),out var next)||
                            next.Kind==PanelTileKind.Signal||next.Kind==PanelTileKind.Switch||next.Kind==PanelTileKind.Cross||
                            (RailPorts(next)&(1<<((port+4)%8)))==0)continue;
                        portInk[port]=(int)WebInk(board,next);
                    }
                }
            }
            int? pointPorts=null;
            string pointName=null;
            if(t.Kind==PanelTileKind.Switch&&!string.IsNullOrEmpty(t.Reference)) {
                if(desks.Points.TryGetValue(t.Reference,out var point))pointName=point.Code;
                if(world.SwitchPosition(t.Reference,out bool thrown)) {
                    int stem,normal,branch;
                    if(t.Ports==0){stem=16;normal=1;branch=2;}
                    else PanelConnections.Arms(t,out stem,out normal,out branch);
                    pointPorts=PanelConnections.Transform(stem|(thrown?branch:normal),t.Rotation,t.Mirror);
                }
            }
            var line=t.Kind==PanelTileKind.LinePermission?desks.FreeLines.FirstOrDefault(l=>l.Id==t.Reference):null;
            var permission=line==null?null:LineLive(line.Id);
            var target=t.Target;
            return new {
                cell=WebCell(t.X,t.Y),ink=(int)ink,portInk,pointPorts,pointName,
                permission=permission!=null&&permission.State!="fault"&&permission.State!="disabled"?permission.Direction:0,
                main=t.Kind==PanelTileKind.Signal?WebSignal(t.Reference,t.ReadOnlySignal):null,
                shunt=t.Kind==PanelTileKind.Signal?WebSignal(t.AlternateSignal,t.ReadOnlySignal,true):null,
                number=t.Kind==PanelTileKind.TrainNumber?NumberCaption(t):null,
                target=target!=null&&!t.ReadOnlySignal&&AuxiliaryEndpoint(target.Id)!=null?new {id=target.Id,name=target.Caption,train=target.Train,shunt=target.Shunt}:null
            };
        }
        IEnumerable<PanelTile> VisibleWebFields(WebBoard board,string view) {
            int x=board.FirstX,y=board.FirstY,width=24,height=16;
            var parts=view?.Split(',');
            if(parts?.Length==4&&int.TryParse(parts[0],out int left)&&int.TryParse(parts[1],out int top)&&
                int.TryParse(parts[2],out int columns)&&int.TryParse(parts[3],out int rows)) {
                x=Math.Min(Math.Max(0,left),board.Panel.Columns-1);y=Math.Min(Math.Max(0,top),board.Panel.Rows-1);
                width=Math.Min(Math.Max(1,columns),120);height=Math.Min(Math.Max(1,rows),90);
            }
            for(int row=y;row<Math.Min(y+height,board.Panel.Rows);row++)
                for(int column=x;column<Math.Min(x+width,board.Panel.Columns);column++)
                    if(board.Cells.TryGetValue(WebCell(column,row),out var tile))yield return tile;
        }
        object ReadWeb(string requestedDesk,string layout,string view) {
            var list=desks.Desks.Where(d=>d.Global||d.ShowInDispatcher).OrderByDescending(d=>d.Global).ThenBy(d=>d.Code).ToArray();
            var desk=WebDesk(requestedDesk)??list.First();
            var board=WebDeskBoard(desk);board.LastRequest=Time.realtimeSinceStartup;
            BuildRouteCues();
            var preview=webPreview?.Desk==desk.Id?webPreview:null;
            var active=WebMayRelease?WebRoutes(desk).Select(run=>new {id=run.Route.Id,start=run.Route.Entry,code=run.Route.Code}).ToArray():null;
            return new {
                language=WindowText("en","de"),revision=Entry.Shared.ConfigurationVersion,installation=Entry.Shared.InstallationVersion,
                desks=list.Select(d=>new {id=d.Id,code=d.Code,name=d.Name}).ToArray(),desk=desk.Id,
                layout=board.Layout,geometry=layout==board.Layout?null:board.Geometry,
                fields=VisibleWebFields(board,layout==board.Layout?view:null).Select(t=>WebField(board,t)).ToArray(),
                canDispatch=WebMayDispatch,canEdit=WorkspaceEditable,reason=WebMayDispatch?null:WebDispatchIssue(),
                active,
                preview=preview==null?null:new {id=preview.Id,stage=preview.Stage,message=preview.Message,highlight=preview.Highlight}
            };
        }

        object HandleWeb(WebAction action) {
            if(action==null||action.Revision!=Entry.Shared.ConfigurationVersion||action.Installation!=Entry.Shared.InstallationVersion||WebDesk(action.Desk) is not DeskDefinition desk)
                throw new InvalidOperationException(WindowText("The installation changed. Select the field again.","Die Anlage wurde geändert. Wähle das Feld erneut."));
            switch(action.Action) {
                case "preview":
                    QueueWebPreview(desk,action);return new {ticket=webPreview.Id};
                case "set":
                    SetWebRoute(desk,action.Target);break;
                case "release":
                    RequireEdit();
                    if(!WebMayRelease||!WebRoutes(desk).Any(run=>run.Route.Id==action.Target))
                        throw new InvalidOperationException(WindowText("This route is no longer available here.","Diese Fahrstraße ist hier nicht mehr verfügbar."));
                    CancelAdmittedRoute(action.Target);break;
                case "edit":
                case "pick":
                    OpenWebField(desk,action);break;
                case "build":
                    RequireEdit();
                    if(worldSignalPick!=null||worldPointSelection!=null)
                        throw new InvalidOperationException(WindowText("Finish the current game tool first.","Beende zuerst das aktive Werkzeug im Spiel."));
                    ResetWebPreview();Entry.OpenBuildFromWeb();break;
                default: throw new InvalidOperationException(WindowText("Unknown action.","Unbekannte Aktion."));
            }
            return new {ok=true};
        }
        IEnumerable<RouteRun> WebRoutes(DeskDefinition desk) {
            var runs=engine.Active.ToArray();
            foreach(var group in runs.GroupBy(run=>run.ChainId??run.Route.Id))
                if(StationAutomationPolicy.CanControl(desks,desk.Id,group.Select(run=>run.Route)))
                    foreach(var run in group)yield return run;
        }
        void OpenWebField(DeskDefinition desk,WebAction action) {
            RequireEdit();
            if(!WorkspaceEditable||Entry.BuildModeActive||worldSignalPick!=null||worldPointSelection!=null)
                throw new InvalidOperationException(WindowText("Finish the current game tool first.","Beende zuerst das aktive Werkzeug im Spiel."));
            var own=desk.Panel.At(action.X,action.Y);
            if(own==null||action.Action=="pick"&&own.Kind!=PanelTileKind.Signal)
                throw new InvalidOperationException(WindowText("Choose the field on its owning interlocking.","Wähle das Feld auf dem zuständigen Stellwerk."));
            var board=WebDeskBoard(desk);
            if(!board.Cells.TryGetValue(WebCell(action.X,action.Y),out var projected)||!own.SameContent(projected))
                throw new InvalidOperationException(WindowText("Choose the field on its owning interlocking.","Wähle das Feld auf dem zuständigen Stellwerk."));
            ResetWebPreview();
            ShowWorkspace(true,4,false);EnterEditorDesk(desk.Id);
            if(action.Action=="pick")BeginPanelSignalPick(action.X,action.Y);
            else OpenEditorField(action.X,action.Y,EditorFieldCategory.Properties);
        }
        void QueueWebPreview(DeskDefinition desk,WebAction action) {
            RequireEdit();
            if(!WebMayDispatch)throw new InvalidOperationException(WebDispatchIssue());
            RouteKind kind;
            switch(action.Movement) {
                case "train": kind=RouteKind.Train;break;
                case "slow": kind=RouteKind.Slow;break;
                case "shunt": kind=RouteKind.Shunt;break;
                default: throw new InvalidOperationException(WindowText("Choose a movement type.","Wähle eine Fahrart."));
            }
            var board=WebDeskBoard(desk);
            bool yard=kind==RouteKind.Shunt;
            bool OnBoard(string id,bool source) {
                if(string.IsNullOrEmpty(id))return false;
                foreach(var tile in board.Panel.Tiles) {
                    if(tile.ReadOnlySignal&&source)continue;
                    if(tile.Kind==PanelTileKind.Signal&&ButtonSignal(id)&&
                        (tile.Reference==id&&(!yard||tile.AlternateSignal==null)||yard&&tile.AlternateSignal==id))return true;
                    if(!source&&!tile.ReadOnlySignal&&tile.Target?.Id==id&&(yard?tile.Target.Shunt:tile.Target.Train)&&AuxiliaryEndpoint(id)!=null)return true;
                }
                return false;
            }
            if(action.Start==action.Target||!OnBoard(action.Start,true)||!OnBoard(action.Target,false)||
                !SignalCatalogue.Supports(Object(action.Start),yard,kind==RouteKind.Slow))
                throw new InvalidOperationException(WindowText("Select a compatible start signal and destination on this interlocking.","Wähle ein passendes Startsignal und Ziel auf diesem Stellwerk."));
            ResolveLineDestination(action.Start,action.Target,kind);
            ResetWebPreview();
            var preview=new WebPreview{Desk=desk.Id,Revision=action.Revision,Installation=action.Installation,Expires=Time.realtimeSinceStartup+60,
                Message=WindowText("Calculating route…","Fahrweg wird berechnet …")};
            webPreview=preview;
            bool Current()=>ReferenceEquals(webPreview,preview)&&WebMayDispatch&&web?.Running==true&&
                Entry.Shared.ConfigurationVersion==preview.Revision&&Entry.Shared.InstallationVersion==preview.Installation;
            IEnumerable<bool> Prepare() {
                var plans=new List<RouteChainPlan>();
                foreach(bool step in PrepareRoutePlans(action.Start,action.Target,kind,plans,message=>preview.Message=message))yield return step;
                plans=AutomaticRoutePlanner.PreferCurrent(plans,id=>world.SwitchPosition(id,out bool thrown)?(bool?)thrown:null);
                if(plans.Count!=1)throw new InvalidOperationException(plans.Count==0?WindowText("No verifiable path for this movement.","Kein prüfbarer Fahrweg für diese Fahrart."):
                    WindowText("Several paths fit. Choose a nearer destination or select a path in the game.","Mehrere Fahrwege sind möglich. Wähle ein näheres Ziel oder einen Fahrweg im Spiel."));
                var plan=plans[0];
                StationAutomationPolicy.RequireControl(desks,desk.Id,plan.Routes);
                RequireManualStation(plan.Routes.First().Entry);
                RequireOperatingRoutes(plan.Routes);
                var path=plan.Routes.SelectMany(r=>r.Path).Select(port=>port.Section).GroupBy(s=>s.Segment).ToDictionary(g=>g.Key,g=>g.ToArray());
                bool OnPath(TrackSection section)=>path.TryGetValue(section.Segment,out var parts)&&parts.Any(section.Overlaps);
                var blockIds=new HashSet<string>(desks.Blocks.Values.Where(b=>b.Sections.Any(OnPath)).Select(b=>b.Id));
                var highlight=new List<int>();
                foreach(var tile in board.Panel.Tiles) {
                    if(tile.Kind!=PanelTileKind.Switch&&tile.Kind!=PanelTileKind.Cross&&
                        (tile.Section!=null&&OnPath(tile.Section)||TileBlocks(tile).Any(blockIds.Contains)))highlight.Add(WebCell(tile.X,tile.Y));
                    yield return true;
                }
                preview.Plan=plan;preview.Highlight=highlight.ToArray();preview.Stage="ready";preview.Expires=Time.realtimeSinceStartup+30;
                preview.Message=WindowText("Path found. The game checks occupancy and protection when setting the route.","Fahrweg gefunden. Das Spiel prüft Belegung und Schutz beim Stellen.");
            }
            try {
                routeWork.Enqueue(WebJob,()=>Prepare().GetEnumerator(),Current,error=>{
                    preview.Stage="failed";preview.Message=LocalText.Get(error.Message);
                });
            }catch{webPreview=null;throw;}
        }
        void SetWebRoute(DeskDefinition desk,string ticket) {
            RequireEdit();
            var next=webPreview;
            if(!WebMayDispatch)throw new InvalidOperationException(WebDispatchIssue());
            if(next==null||ticket!=next.Id||desk.Id!=next.Desk||next.Stage!="ready"||next.Expires<=Time.realtimeSinceStartup||
                next.Revision!=Entry.Shared.ConfigurationVersion||next.Installation!=Entry.Shared.InstallationVersion)
                throw new InvalidOperationException(WindowText("The preview expired or changed. Check the route again.","Die Vorschau ist abgelaufen oder geändert. Prüfe den Fahrweg erneut."));
            next.Stage="failed";next.Highlight=Array.Empty<int>();
            try {
                AdmitAutomaticPlan(next.Plan,desk.Id);
                next.Stage="submitted";next.Message=WindowText("Route requested. The track display shows when it is set.","Fahrstraße angefordert. Die Gleisanzeige zeigt, sobald sie gestellt ist.");
            }catch(Exception error){next.Message=LocalText.Get(error.Message);throw;}
            finally{next.Plan=null;}
        }
    }
}
