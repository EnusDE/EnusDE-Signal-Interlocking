using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Track;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed class PointAssignment { public string Code="",Desk="";public bool ShowCircuit,ElectricStand;public Dictionary<int,string> BoundaryLinks=new Dictionary<int,string>();public PointAssignment Clone(){var copy=(PointAssignment)MemberwiseClone();copy.BoundaryLinks=BoundaryLinks==null?null:new Dictionary<int,string>(BoundaryLinks);return copy;} }
    public static class PointAssignments {
        public static void NameDiscovered(DeskLibrary library,IEnumerable<string> ids){string global=library.Desks.Single(d=>d.Global).Id;int number=1;var used=new HashSet<string>(library.Points.Values.Select(p=>p.Code),StringComparer.OrdinalIgnoreCase);foreach(string id in ids.OrderBy(id=>id,StringComparer.Ordinal)){if(library.Points.ContainsKey(id))continue;string code;do{code="W"+(number++).ToString("D3");}while(!used.Add(code));library.Points[id]=new PointAssignment{Code=code,Desk=global};}}
        public static void Boundaries(DeskLibrary library) {
            var global=library.Desks.SingleOrDefault(d=>d.Global);if(global==null)return;global.Panel.Areas.RemoveAll(a=>!string.IsNullOrEmpty(a.Desk));
            foreach(var local in library.Desks.Where(d=>!d.IsMaster)){var ids=new HashSet<string>(local.Panel.Tiles.Select(t=>t.Reference).Where(id=>!string.IsNullOrEmpty(id)));ids.UnionWith(library.Points.Where(p=>p.Value.Desk==local.Id).Select(p=>p.Key));var blocks=new HashSet<string>(local.Panel.Tiles.Select(t=>t.BlockId).Where(id=>!string.IsNullOrEmpty(id)));var cells=global.Panel.Tiles.Where(t=>ids.Contains(t.Reference)||blocks.Contains(t.BlockId)).ToArray();if(cells.Length==0)continue;string code=local.Code;if(global.Panel.Areas.Any(a=>a.Code==code))continue;int x=Math.Max(0,cells.Min(t=>t.X)-1),y=Math.Max(0,cells.Min(t=>t.Y)-1);global.Panel.Areas.Add(new PanelArea{Id=local.Id,Desk=local.Id,Code=code,Name=local.Name,Kind="Stellwerk",X=x,Y=y,Width=Math.Min(global.Panel.Columns-1,cells.Max(t=>t.X)+1)-x+1,Height=Math.Min(global.Panel.Rows-1,cells.Max(t=>t.Y)+1)-y+1});}
        }
        public static void Validate(DeskLibrary library) {
            if(library.Points==null||library.Points.Count>4096)throw new InvalidDataException("Weichenzuordnung ungültig.");
            foreach(var p in library.Points)if(string.IsNullOrWhiteSpace(p.Key)||p.Key.Length>128||p.Value==null||string.IsNullOrWhiteSpace(p.Value.Code)||p.Value.Code.Length>24||p.Value.Code.Any(char.IsControl)||!library.Desks.Any(d=>d.Id==p.Value.Desk))throw new InvalidDataException("Weichenkennung oder zuständiges Stellwerk ungültig.");
            if(library.Points.Values.Select(p=>p.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=library.Points.Count)throw new InvalidDataException("Weichenkennung bereits vergeben.");
        }
        public static string Caption(DeskLibrary library,string id){PointAssignment p;return id!=null&&library?.Points.TryGetValue(id,out p)==true?p.Code:"Weiche";}
        public static DeskLibrary Put(DeskLibrary library,string id,string code,string owner) {var next=library.Clone();var value=library.Points.TryGetValue(id,out var old)?old.Clone():new PointAssignment();value.Code=(code??"").Trim();value.Desk=owner;next.Points[id]=value;next.Validate(next.WorldKey);return next;}
    }
    public sealed partial class SignalRuntime {
        bool assigningPoint;
        string pointOwner,pointCode="";
        string PointPosition(string id) {bool thrown;if(string.IsNullOrEmpty(id)||world==null||!world.SwitchPosition(id,out thrown))return "Unbekannt · keine gültige Rückmeldung";var moving=VisibleRuns.SelectMany(r=>r.PointStates).Where(p=>p.Key==id).Select(p=>p.Value).FirstOrDefault();return (thrown?"− Abzweig":"+ Grundstellung")+(moving==null?"":" · "+moving);}
        string TrackPointAt(Location at) {
            if(!at.IsValid)throw new InvalidOperationException(LocalText.Select("No valid track selected.","Kein gültiges Gleis gewählt."));
            var nodes=new[]{at.segment.a,at.segment.b}.Where(n=>n!=null&&Graph.Shared.IsSwitch(n)&&at.segment.DistanceBetween(n,at)<=30).OrderBy(n=>at.segment.DistanceBetween(n,at)).ToArray();
            if(nodes.Length==0)throw new InvalidOperationException(LocalText.Select("Click closer to the desired point (within 30 m).","Näher an die gewünschte Weiche klicken (höchstens 30 m)."));
            if(nodes.Length>1&&Math.Abs(at.segment.DistanceBetween(nodes[0],at)-at.segment.DistanceBetween(nodes[1],at))<.5)
                throw new InvalidOperationException(LocalText.Select("Two points are equally close. Click nearer the intended point.","Zwei Weichen sind gleich nah. Näher an die gewünschte Weiche klicken."));
            return nodes[0].id;
        }
        public void AssignTrackPoint(Location at) {
            if(PickingPoint&&(ConsumeSelectionInput()||WorldPointerInput.OverUi()))return;
            if(worldPointSelection!=null){TryAssignWorldPoint(Graph.Shared.GetNode(TrackPointAt(at)));return;}
            RequireEdit();AssignWorldPoint(TrackPointAt(at),at.segment.id);
        }
        void AssignWorldPoint(string point,string segment) {
            if(worldPointSelection!=null){CompleteWorldPointSelection(point);return;}
            switchPicked=point;trackPicked=segment;assigningPoint=true;pointOwner=CurrentDesk?.Id;pointCode=PointAssignments.Caption(desks,switchPicked);if(pointCode=="Weiche")pointCode="W"+(desks.Points.Count+1);
            if(FinishPointPick())return;
            ShowWorkspace(true,4,true);status=LocalText.Select("Point selected. Choose an interlocking, then click an unassigned point field.","Weiche gewählt. Stellwerk wählen und ein ungebundenes Weichenfeld anklicken.");
        }
        internal string SelectTrackPoint(Location at) {
            RequireEdit();string id=TrackPointAt(at);
            if(!desks.Points.ContainsKey(id)){var next=desks.Clone();PointAssignments.NameDiscovered(next,new[]{id});deskStore.Save(next);desks=next;panel=DeskPanel(CurrentDesk);}
            return id;
        }
        void BuildPointAssignment(UIPanelBuilder c) {
            if(!assigningPoint)return;c.AddSection(LocalText.Get("Weiche aus der Spielwelt zuordnen"));c.AddLabel(LocalText.Get("Gewählte Weiche bleibt beim Wechsel der Tafel vorgemerkt. Rechtsklick auf das Zielfeld → Weichenzuordnung übernehmen."));
            c.AddField(LocalText.Get("Gemeinsame Kennung"),c.AddInputField(pointCode,v=>pointCode=v,"W1",24));var owners=desks.Desks.OrderByDescending(d=>d.Global).ToArray();if(!owners.Any(d=>d.Id==pointOwner))pointOwner=owners[0].Id;
            c.AddSearchChoice(LocalText.Get("Zuständiges Stellwerk"),owners.Select(d=>d.Code+" · "+d.Name).ToList(),Array.FindIndex(owners,d=>d.Id==pointOwner),i=>pointOwner=owners[i].Id);
            c.AddButton(LocalText.Get("Zuordnung beenden"),()=>{assigningPoint=false;Rebuild();});
        }
        void AcceptPoint(int x,int y) {
            RequirePanelEdit();if(!assigningPoint||string.IsNullOrEmpty(switchPicked))throw new InvalidOperationException("Zuerst die gewünschte Weiche im Spiel rechtsklicken → Weiche zuordnen.");
            bool thrown;if(!world.SwitchPosition(switchPicked,out thrown))throw new InvalidOperationException("Gewählte Weiche nicht mehr verfügbar.");
            var next=PointAssignments.Put(desks,switchPicked,pointCode,pointOwner);var document=next.Desks.Single(d=>d.Id==deskId).Panel;
            var growth=CanvasGrowth.For(document,new PanelRegion{X=x,Y=y},document.At(x,y)==null?2:0);growth.Apply(document);x+=growth.Left;y+=growth.Top;
            if(CurrentDesk.IsMaster)foreach(var module in next.Modules.Where(m=>MasterDiagram.Owner(next,m)==deskId)){module.X+=growth.Left;module.Y+=growth.Top;}
            var tile=document.At(x,y)?.Clone()??new PanelTile{X=x,Y=y};
            if(tile.Kind==PanelTileKind.Signal||tile.Kind==PanelTileKind.TrainNumber||tile.Kind==PanelTileKind.Label||tile.Kind==PanelTileKind.AuxiliaryTarget)throw new InvalidOperationException("Ein Gleis-/Weichenfeld oder freies Feld wählen.");
            tile.Kind=PanelTileKind.Switch;tile.Reference=switchPicked;tile.Label="";tile.Section=null;tile.BlockId=next.Blocks.ContainsKey("p:"+switchPicked)?"p:"+switchPicked:"";tile.SecondBlock="";if(PanelConnections.Count(tile.Ports)!=3)tile.Ports=tile.Stem=tile.Normal=0;
            next.Desks.Single(d=>d.Id==deskId).Panel.Put(tile);var before=panel;deskStore.Save(next);desks=next;panel=CurrentDesk.Panel;RebaseEditorCanvas(growth.Left,growth.Top);try{history?.Record(before,panel);}catch(Exception e){historyError=e.Message;}assigningPoint=false;tileDraft=panel.At(x,y)?.Clone();selectedX=x;selectedY=y;LoadTileFields();nextPanelRead=0;status=LocalText.Get("Weiche ")+pointCode+" zugeordnet. Kennung, Stellungs- und Belegungsrückmeldung gelten auf allen Tafeln.";Rebuild();
        }
    }
}
