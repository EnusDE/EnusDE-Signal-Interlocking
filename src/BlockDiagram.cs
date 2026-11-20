using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public static class BlockDiagram {
        public static PanelArea Area(PanelDocument panel,string line)=>panel.Areas.FirstOrDefault(a=>a.BlockLine==line);
        static string AreaCode(PanelDocument panel,string code) {
            string stem="BL-"+code.Substring(0,Math.Min(16,code.Length));string value=stem;int i=1;
            while(panel.Areas.Any(a=>string.Equals(a.Code,value,StringComparison.OrdinalIgnoreCase)))value=stem+"-"+i++;
            return value;
        }
        public static bool CanPlace(PanelDocument panel,string line,int x,int y) {
            var group=Area(panel,line);if(group==null||x<0||y<0||x+group.Width>panel.Columns||y+group.Height>panel.Rows)return false;
            return !panel.Tiles.Any(t=>!group.Contains(t.X,t.Y)&&t.X>=x&&t.X<x+group.Width&&t.Y>=y&&t.Y<y+group.Height)&&!panel.Areas.Any(a=>a.Id!=group.Id&&(a.BlockLine!=null||a.NumberLine!=null)&&a.X<x+group.Width&&x<a.X+a.Width&&a.Y<y+group.Height&&y<a.Y+a.Height);
        }
        public static void Move(PanelDocument panel,string line,int x,int y) {
            var group=Area(panel,line)??throw new InvalidOperationException(LocalText.Select("The block assembly is missing.","Der Blockverbund fehlt."));
            if(!CanPlace(panel,line,x,y))throw new InvalidOperationException(LocalText.Select("The block assembly does not fit here. Choose free cells.","Der Blockverbund passt hier nicht. Freie Felder wählen."));
            new PanelRegion{X=group.X,Y=group.Y,Width=group.Width,Height=group.Height}.Move(panel,x,y);
        }
        public static PanelTile[] Tiles(PanelDocument panel,string line) {
            var area=Area(panel,line);return area==null?Array.Empty<PanelTile>():panel.Tiles.Where(t=>area.Contains(t.X,t.Y)).Select(t=>t.Clone()).ToArray();
        }
        public static void Rebuild(DeskLibrary library,LineTrackDefinition line,IReadOnlyList<TrackBlock> blocks,IReadOnlyDictionary<string,BlockViewOptions> requested=null) {
            if(blocks.Count!=line.BlockUnits.Count+1)throw new InvalidOperationException(LocalText.Select("The block assembly sections do not match its signal units.","Abschnitte und Signaleinheiten des Blockverbunds passen nicht zusammen."));
            var signalIds=new HashSet<string>(line.BlockUnits.SelectMany(u=>new[]{u.Forward,u.Reverse}).Where(id=>id!=""));
            var destinations=new HashSet<string>(line.BlockDisplays.Append(line.BlockOwner));
            var positions=new Dictionary<string,(int X,int Y)>();
            var numbers=new Dictionary<string,PanelTile>();
            var permissions=new Dictionary<string,PanelTile>();
            var signals=new Dictionary<string,PanelTile>();
            var areas=new Dictionary<string,PanelArea>();
            foreach(var desk in library.Desks)foreach(var tile in desk.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.Signal&&signalIds.Contains(t.Reference))) {
                string key=desk.Id+":"+tile.Reference;if(!signals.ContainsKey(key))signals[key]=tile.Clone();
            }
            foreach(var desk in library.Desks)foreach(var tile in desk.Panel.Tiles.Where(t=>t.Kind==PanelTileKind.TrainNumber&&t.NumberMirror==null))
                if(blocks.Any(b=>b.Id==tile.BlockId)&&!numbers.ContainsKey(tile.BlockId))numbers[tile.BlockId]=tile.Clone();
            foreach(var desk in library.Desks) {
                var doc=desk.Panel;var area=Area(doc,line.Id);
                if(area!=null) {
                    var entries=doc.Tiles.Where(t=>area.Contains(t.X,t.Y)).ToArray();
                    if(entries.Any(t=>t.Target!=null||t.AlternateSignal!=null||
                        t.Kind==PanelTileKind.Signal&&!signalIds.Contains(t.Reference)||
                        t.Kind==PanelTileKind.TrainNumber&&!blocks.Any(b=>b.Id==t.BlockId)&&t.NumberMirror==null||
                        t.Kind==PanelTileKind.LinePermission&&t.Reference!=line.Id||
                        t.Kind==PanelTileKind.Label&&!BlockDiagramViews.LabelAt(area,t)||
                        t.Kind!=PanelTileKind.Signal&&t.Kind!=PanelTileKind.TrainNumber&&t.Kind!=PanelTileKind.LinePermission&&t.Kind!=PanelTileKind.Label&&t.Kind!=PanelTileKind.Straight))
                        throw new InvalidOperationException(LocalText.Select("Move manually added fields out of the block assembly before rebuilding it: ","Manuell ergänzte Felder vor dem Neuaufbau aus dem Blockverbund verschieben: ")+desk.Code);
                    var permission=entries.FirstOrDefault(t=>t.Kind==PanelTileKind.LinePermission&&t.Reference==line.Id);
                    if(permission!=null)permissions[desk.Id]=permission.Clone();
                    foreach(var saved in area.BlockTemplate??Enumerable.Empty<PanelTile>()) {
                        var copy=saved.Clone();BlockDiagramViews.Orient(copy,area.BlockView??new BlockViewOptions(),blocks.Count*6+1,2);
                        string key=desk.Id+":"+copy.Reference;
                        if(copy.Kind==PanelTileKind.Signal&&signalIds.Contains(copy.Reference)&&!signals.ContainsKey(key))signals[key]=copy;
                        if(copy.Kind==PanelTileKind.LinePermission&&copy.Reference==line.Id&&!permissions.ContainsKey(desk.Id))permissions[desk.Id]=copy;
                    }
                    areas[desk.Id]=area.Clone();
                    positions[desk.Id]=(area.X,area.Y);
                    doc.Tiles.RemoveAll(t=>area.Contains(t.X,t.Y));doc.Areas.Remove(area);
                }
                // Recognize only the exact 0.7.43 strip. Never remove an arbitrary hand-drawn area.
                foreach(var label in doc.Tiles.Where(t=>t.Kind==PanelTileKind.Label&&t.X==0&&t.Label==line.Code+" · "+line.Track).ToArray()) {
                    int top=label.Y-2,width=blocks.Count*3+2;if(top<0)continue;
                    var strip=new PanelRegion{X=0,Y=top,Width=width,Height=3};var entries=doc.Tiles.Where(t=>strip.Contains(t.X,t.Y)).ToArray();
                    if(!entries.Any(t=>t.Kind==PanelTileKind.Signal)||entries.Any(t=>t.Kind==PanelTileKind.Signal&&!signalIds.Contains(t.Reference)||t.Kind==PanelTileKind.Switch||t.Kind==PanelTileKind.AuxiliaryTarget))continue;
                    positions[desk.Id]=(0,top);doc.Tiles.RemoveAll(t=>strip.Contains(t.X,t.Y));
                }
                foreach(var tile in doc.Tiles.ToArray()) {
                    bool signal=tile.Kind==PanelTileKind.Signal&&(signalIds.Contains(tile.Reference)||signalIds.Contains(tile.AlternateSignal));
                    bool number=tile.Kind==PanelTileKind.TrainNumber&&tile.NumberId!=null&&numbers.Values.Any(n=>n.NumberId==tile.NumberId);
                    if(signal&&desk.Id!=line.BlockOwner) {
                        if(destinations.Contains(desk.Id)){tile.ReadOnlySignal=true;continue;}
                        var rail=PanelEditing.TrackBelowSymbol(doc,tile);doc.Tiles.Remove(tile);if(rail!=null)doc.Tiles.Add(rail);
                    }
                    if(number) {
                        if(destinations.Contains(desk.Id)) {
                            string source=tile.NumberId;tile.NumberId=null;tile.NumberMirror=source;tile.BlockId=tile.SecondBlock=tile.AutoSegment="";tile.Section=null;tile.CircuitRails=null;tile.CircuitBoundary=null;tile.Ports=0;tile.AutoMeters=0;tile.Number=tile.Prefix="";tile.Reference="";tile.AlternateSignal=null;
                        } else {var rail=PanelEditing.TrackBelowSymbol(doc,tile);doc.Tiles.Remove(tile);if(rail!=null)doc.Tiles.Add(rail);}
                    }
                }
            }
            var sourceIds=blocks.ToDictionary(b=>b.Id,b=>numbers.TryGetValue(b.Id,out var old)&&old.NumberId!=null?old.NumberId:Guid.NewGuid().ToString("N"));
            foreach(string id in destinations.OrderBy(id=>id==line.BlockOwner?0:1)) {
                var desk=library.Desks.Single(d=>d.Id==id);var doc=desk.Panel;bool readOnly=id!=line.BlockOwner;
                areas.TryGetValue(id,out var oldArea);
                var options=BlockDiagramViews.Effective(requested!=null&&requested.TryGetValue(id,out var chosen)?chosen:oldArea?.BlockView,blocks.Count,!readOnly);
                var template=new PanelDocument{WorldKey=library.WorldKey,Columns=Math.Max(8,blocks.Count*6+1)};
                void Put(PanelTile tile){template.Tiles.RemoveAll(t=>t.X==tile.X&&t.Y==tile.Y);template.Tiles.Add(tile);}
                for(int col=0;col<blocks.Count*6+1;col++) {
                    int block=Math.Min(blocks.Count-1,col/6);Put(new PanelTile{X=col,Y=1,Kind=PanelTileKind.Straight,Ports=17,BlockId=blocks[block].Id});
                }
                PanelTile Signal(string reference,int col,int block,bool reverse) {
                    var tile=signals.TryGetValue(id+":"+reference,out var previous)?previous.Clone():new PanelTile();
                    tile.X=col;tile.Y=1;tile.Kind=PanelTileKind.Signal;tile.Ports=17;tile.Reference=reference;tile.BlockId=blocks[block].Id;tile.ReadOnlySignal=readOnly;
                    tile.Rotation=reverse?2:0;
                    // Rebuild from the physical A-to-B template before applying display reflection.
                    if(oldArea?.BlockView?.Turn180==true){if(tile.MainSignalDirection.HasValue)tile.MainSignalDirection=(tile.MainSignalDirection.Value+2)%4;if(tile.ShuntSignalDirection.HasValue)tile.ShuntSignalDirection=(tile.ShuntSignalDirection.Value+2)%4;}
                    if(oldArea?.BlockView?.Reverse==true){tile.Mirror=!tile.Mirror;if(tile.MainSignalDirection.HasValue)tile.MainSignalDirection=(6-tile.MainSignalDirection.Value)%4;if(tile.ShuntSignalDirection.HasValue)tile.ShuntSignalDirection=(6-tile.ShuntSignalDirection.Value)%4;}
                    tile.CircuitRails=null;tile.CircuitBoundary=null;return tile;
                }
                for(int block=0;block<blocks.Count;block++) {
                    var number=new PanelTile{X=block*6+3,Y=1,Kind=PanelTileKind.TrainNumber,Ports=17,BlockId=blocks[block].Id,NumberId=sourceIds[blocks[block].Id]};
                    if(numbers.TryGetValue(blocks[block].Id,out var old)){number.Number=old.Number;number.Prefix=old.Prefix;}
                    if(readOnly)Put(new PanelTile{X=number.X,Y=1,Kind=PanelTileKind.TrainNumber,NumberMirror=sourceIds[blocks[block].Id],InlineNumberMirror=true,Label=(line.Code+" · "+(block+1)).Substring(0,Math.Min(48,(line.Code+" · "+(block+1)).Length))});
                    else Put(number);
                }
                for(int u=0;u<line.BlockUnits.Count;u++) {
                    var unit=line.BlockUnits[u];int center=(u+1)*6;
                    Put(Signal(unit.Forward,center-1,u,false));
                    Put(new PanelTile{X=center,Y=1,Kind=PanelTileKind.Straight,Ports=17,CircuitBoundary=unit.Boundary,CircuitRails=new Dictionary<int,string>{{4,blocks[u].Id},{0,blocks[u+1].Id}}});
                    if(unit.Reverse!="")Put(Signal(unit.Reverse,center+1,u+1,true));
                }
                template.At(0,1).CircuitBoundary=blocks[0].BoundaryA;template.At(blocks.Count*6,1).CircuitBoundary=blocks.Last().BoundaryB;
                template.Tiles.Add(new PanelTile{X=0,Y=0,Kind=PanelTileKind.Label,Label=line.Code});
                var module=new PanelTile{X=4,Y=0,Kind=PanelTileKind.LinePermission,Reference=line.Id,ReadOnlySignal=readOnly};
                if(permissions.TryGetValue(id,out var oldModule)){module.Label=oldModule.Label;module.Mirror=oldModule.Mirror;module.Rotation=oldModule.Rotation;module.PermissionAuxiliary=oldModule.PermissionAuxiliary;if(oldArea?.BlockView?.Turn180==true)module.Rotation=(module.Rotation+2)%4;if(oldArea?.BlockView?.Reverse==true){module.Rotation=(6-module.Rotation)%4;module.Mirror=!module.Mirror;}}
                template.Tiles.Add(module);
                var projection=BlockDiagramViews.Project(template,blocks,options);int width=projection.Width,height=projection.Height;doc.Columns=Math.Max(doc.Columns,width);
                int x=0,y=0;if(positions.TryGetValue(id,out var previous)){x=previous.X;y=previous.Y;}
                bool Fits()=>x>=0&&y>=0&&x+width<=doc.Columns&&y+height<=PanelDocument.MaxRows&&!doc.Tiles.Any(t=>t.X>=x&&t.X<x+width&&t.Y>=y&&t.Y<y+height)&&!doc.Areas.Any(a=>(a.BlockLine!=null||a.NumberLine!=null)&&a.X<x+width&&x<a.X+a.Width&&a.Y<y+height&&y<a.Y+a.Height);
                if(!Fits()){x=0;y=0;while(y+height<=PanelDocument.MaxRows&&!Fits())y+=height;}
                if(!Fits())throw new InvalidOperationException(LocalText.Select("No room for this block assembly at ","Kein Platz für diesen Blockverbund bei ")+desk.Code);
                doc.Rows=Math.Max(doc.Rows,y+height);
                foreach(var tile in projection.Tiles){tile.X+=x;tile.Y+=y;doc.Tiles.Add(tile);}
                doc.Areas.Add(new PanelArea{Id=line.Id,Code=oldArea?.Code??AreaCode(doc,line.Code),Name=oldArea?.Name??line.Name,Kind="Strecke",BlockLine=line.Id,BlockView=options,BlockTemplate=template.Tiles.Select(t=>t.Clone()).ToList(),X=x,Y=y,Width=width,Height=height});
                doc.Validate(library.WorldKey);
            }
        }
    }
}
