using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Track;
using Track.Signals;
using Game.State;
using UI.Builder;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        Task<NetworkResult> networkTask;
        CancellationTokenSource networkCancel;
        string networkSignature,requestedSignature,networkError;
        long requestedVersion;
        bool networkDrawing,networkArchive,forceNetwork;
        double nextNetwork;
        NetworkInput CaptureNetwork() {
            var graph=Graph.Shared;var input=new NetworkInput{World=scope};if(graph==null)return input;
            foreach(var node in graph.Nodes){TrackSegment common,normal,reverse;bool point=graph.IsSwitch(node);if(point)graph.DecodeSwitchAt(node,out common,out normal,out reverse);else common=normal=null;input.Nodes.Add(new PlanNode{Id=node.id,X=node.Position.x,Y=node.Position.z,Point=point,Common=common?.id,Normal=normal?.id});}
            foreach(var segment in graph.AllSegments.Where(s=>s!=null&&s.turntable==null&&s.Available&&s.GroupEnabled&&s.a!=s.b))input.Edges.Add(new PlanEdge{Id=segment.id,A=segment.a.id,B=segment.b.id,Length=segment.GetLength()});
            foreach(var marker in inventory.Values.Where(s=>s.IsSignal&&s.Kind!=MarkerRecord.Distant)){var loc=SignalWorldBridge.Location(marker).WithEnd(TrackSegment.End.A);var pos=loc.GetPosition(PositionAccuracy.High);input.Cuts.Add(new PlanCut{Segment=loc.segment.id,Meters=loc.distance,Signal=marker.Id,X=pos.x,Y=pos.z});}
            foreach(var signal in UnityEngine.Object.FindObjectsByType<CTCSignal>(FindObjectsSortMode.None).Where(s=>!(s is RuntimeSignalProxy)&&!string.IsNullOrEmpty(s.id)&&Entry.Shared?.IgnoresStock(s.id)!=true)){Graph.NamedLocation named;if(graph.TryGetNamedLocation(signal.id,out named)&&named.Location.IsValid){var loc=named.Location.WithEnd(TrackSegment.End.A);var pos=loc.GetPosition(PositionAccuracy.High);input.Cuts.Add(new PlanCut{Segment=loc.segment.id,Meters=loc.distance,X=pos.x,Y=pos.z});}}
            return input;
        }
        void UpdateNetwork() {
            if(!StateManager.IsHost||desks==null)return;
            if(networkTask!=null){if(!networkTask.IsCompleted)return;var task=networkTask;networkTask=null;networkCancel?.Dispose();networkCancel=null;try{var result=task.GetAwaiter().GetResult();if(scope!=result.Panel.WorldKey)return;if(requestedSignature!=NetworkSignature(CaptureNetwork())){nextNetwork=0;return;}if(networkDrawing&&Entry.Shared.ConfigurationVersion!=requestedVersion){status="Grundtafel nicht übernommen: Zeichnung wurde inzwischen geändert. Erneut anfordern.";return;}
                    var next=desks.Clone();foreach(var b in result.Blocks)next.Blocks[b.Key]=b.Value;PointAssignments.NameDiscovered(next,result.Blocks.Values.Where(b=>b.Point!=null).Select(b=>b.Point));var global=next.Desks.Single(d=>d.Global);if(networkDrawing){if(networkArchive&&global.Panel.Tiles.Count>0){if(next.Desks.Count>=32)throw new InvalidOperationException("Kein freier Stellwerksplatz für Sicherungskopie.");string code="ALT"+DateTime.UtcNow.ToString("HHmmss");next.Desks.Add(new DeskDefinition{Code=code,Name="Sicherung vor Netz-Neuerzeugung",Panel=global.Panel.Clone()});}global.Panel=result.Panel;next.AutoInitialized=true;}
                    foreach(var desk in next.Desks)RebindAutomatic(desk.Panel,result.Blocks);var refs=new HashSet<string>(next.Desks.SelectMany(d=>d.Panel.Tiles).SelectMany(t=>new[]{t.BlockId,t.SecondBlock}).Where(s=>!string.IsNullOrEmpty(s)));foreach(string old in next.Blocks.Keys.Where(id=>!result.Blocks.ContainsKey(id)&&!refs.Contains(id)&&!next.Blocks[id].IsCircuit).ToArray())next.Blocks.Remove(old);
                    next.Validate(scope);deskStore.Save(next);desks=next;panel=CurrentDesk.Panel;networkSignature=requestedSignature;networkError=null;panelWorld?.Dispose();panelWorld=new SignalWorldBridge(this,transform,2048);panelReadTime=-100;nextPanelRead=0;
                    if(networkDrawing){status="Netzerkennung: "+result.Segments+" Gleissegmente, "+result.Blocks.Count+" Abschnitte; "+result.Drawn+"/"+result.Chains+" Gleisketten gezeichnet."+(result.Missing.Count==0?"":" Nicht gezeichnete Verbindungen: "+result.Missing.Count+". Keine Fahrstraßen daraus ableiten.");if(CurrentDesk.Global){history=new PanelHistory(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(directory),"EditorHistory",scope,deskId),panel);historyError=null;}panelZoom=.08f;Rebuild();}else panelView?.ReplaceDocument(panel);
                }catch(Exception e){networkError=e.Message;log?.Warning("Netzerkennung: "+e.Message);}return;}
            if(Time.realtimeSinceStartup<nextNetwork)return;nextNetwork=Time.realtimeSinceStartup+10;if(networkError!=null&&!forceNetwork)return;var input=CaptureNetwork();if(input.Edges.Count==0)return;string signature=NetworkSignature(input);bool draw=false; /* The rejected generator must not replace a drawing. */if(!draw&&signature==networkSignature)return;networkDrawing=draw;networkArchive=forceNetwork;forceNetwork=false;requestedSignature=signature;requestedVersion=Entry.Shared.ConfigurationVersion;networkCancel=new CancellationTokenSource();var token=networkCancel.Token;networkTask=Task.Run(()=>NetworkPlan.Build(input,token,draw),token);
        }
        static string NetworkSignature(NetworkInput input)=>SharedDocuments.Hash(System.Text.Encoding.UTF8.GetBytes(input.World+"|"+string.Join(";",input.Edges.OrderBy(e=>e.Id).Select(e=>e.Id+":"+e.Length.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))+"|"+string.Join(";",input.Cuts.OrderBy(c=>c.Segment).ThenBy(c=>c.Meters).Select(c=>c.Segment+":"+c.Meters.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))));
        static void RebindAutomatic(PanelDocument doc,Dictionary<string,TrackBlock> blocks){var bySegment=blocks.Values.Where(b=>b.Point==null).SelectMany(b=>b.Path.Select(p=>new{b.Id,p.Section})).GroupBy(p=>p.Section.Segment).ToDictionary(g=>g.Key,g=>g.ToArray());foreach(var tile in doc.Tiles.Where(t=>t.AutoSegment!=""&&t.Kind!=PanelTileKind.Switch&&t.SecondBlock=="")){if(!bySegment.ContainsKey(tile.AutoSegment)){tile.BlockId="";continue;}var matches=bySegment[tile.AutoSegment].Where(p=>p.Section.From<=tile.AutoMeters&&p.Section.To>=tile.AutoMeters).ToArray();tile.BlockId=matches.Length==1?matches[0].Id:"";}}
        void BuildNetworkMenu(UIPanelBuilder c){
            c.AddSection(WindowText("Track section detection","Belegungsabschnitte"));
            c.AddLabel(()=>networkError??(networkTask!=null?WindowText("Updating track sections…","Belegungsabschnitte werden aktualisiert …"):
                WindowText("Detected sections: ","Erkannte Abschnitte: ")+(desks?.Blocks.Count??0)),UIPanelBuilder.Frequency.Periodic);
        }
        void StopNetwork(){networkCancel?.Cancel();networkTask=null;networkCancel?.Dispose();networkCancel=null;}
    }
}
