using System;
using System.Linq;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    // Ports run anticlockwise from right. Screen rows increase downwards.
    public static class PanelConnections {
        public static readonly int[] Dx={1,1,0,-1,-1,-1,0,1},Dy={0,-1,-1,-1,0,1,1,1};
        public static readonly string[] Names={"rechts","rechts oben","oben","links oben","links","links unten","unten","rechts unten"};
        static readonly Dictionary<int,IReadOnlyList<PanelLine>> shapes=new Dictionary<int,IReadOnlyList<PanelLine>>();
        public static int Count(int mask){int n=0;for(int i=0;i<8;i++)if((mask&(1<<i))!=0)n++;return n;}
        public static void Validate(PanelTile t) {
            if(t.Kind==PanelTileKind.LinePermission&&t.InlinePermission&&t.Ports!=0&&(Count(t.Ports)<1||Count(t.Ports)>2))throw new InvalidOperationException(LocalText.Select("An inline permission module needs one or two rail ports.","Ein Erlaubnismodul im Gleis benötigt einen oder zwei Gleisanschlüsse."));
            if(t.Ports<0||t.Ports>255||Count(t.Ports)>4||(t.Kind==PanelTileKind.Signal||t.Kind==PanelTileKind.TrainNumber||t.Kind==PanelTileKind.AuxiliaryTarget)&&Count(t.Ports)>2||t.Kind==PanelTileKind.Label&&t.Ports!=0)throw new InvalidOperationException("Ungültige Gleisanschlüsse.");
            if(t.Ports!=0&&(t.Kind==PanelTileKind.Switch&&Count(t.Ports)!=3||t.Kind==PanelTileKind.Cross&&Count(t.Ports)!=4||t.Kind==PanelTileKind.Straight&&Count(t.Ports)>2||(t.Kind==PanelTileKind.Diagonal||t.Kind==PanelTileKind.Curve)&&Count(t.Ports)!=2))throw new InvalidOperationException("Anschlüsse passen nicht zum Symboltyp.");
            foreach(int p in new[]{t.Stem,t.Normal})if(p!=0&&(p<1||p>128||(p&(p-1))!=0||(p&t.Ports)==0))throw new InvalidOperationException("Weichenast nicht an das Symbol angeschlossen.");
            if(t.Stem!=0&&t.Normal!=0&&t.Stem==t.Normal)throw new InvalidOperationException("Stamm- und Grundast müssen verschieden sein.");
        }
        public static int Direction(int dx,int dy){for(int i=0;i<8;i++)if(Dx[i]==Math.Sign(dx)&&Dy[i]==Math.Sign(dy))return i;throw new InvalidOperationException("Gleispunkte müssen verschieden sein.");}
        public static string Name(PanelTile tile,int localPort){int mask=Transform(1<<localPort,tile.Rotation,tile.Mirror);return Names[Enumerable.Range(0,8).First(i=>mask==(1<<i))];}
        public static int Transform(int mask,int rotation,bool mirror,bool inverse=false) {
            int result=0;for(int i=0;i<8;i++)if((mask&(1<<i))!=0){int j=i;if(inverse){j=(j+2*rotation)%8;if(mirror)j=(8-j)%8;}else{if(mirror)j=(8-j)%8;j=(j-2*rotation+8)%8;}result|=1<<j;}return result;
        }
        public static int Ports(PanelTile tile) {
            if(NumberMirrors.IsMirror(tile)||tile.Kind==PanelTileKind.LinePermission&&!tile.InlinePermission)return 0;
            if(tile.Kind==PanelTileKind.LinePermission&&tile.Ports==0)return Transform(17,tile.Rotation,tile.Mirror);
            if(tile.Ports!=0)return Transform(tile.Ports,tile.Rotation,tile.Mirror);
            int mask=0;foreach(var line in PanelGeometry.Lines(tile.Kind))foreach(var p in new[]{line.A,line.B})if(Math.Abs(p.X)==1||Math.Abs(p.Y)==1)mask|=1<<Direction((int)Math.Sign(p.X),-(int)Math.Sign(p.Y));
            return Transform(mask,tile.Rotation,tile.Mirror);
        }
        public static void Arms(PanelTile tile,out int stem,out int normal,out int branch) {
            stem=tile.Stem;normal=tile.Normal;branch=0;if(Count(tile.Ports)!=3)return;
            if(stem==0){for(int i=0;i<4;i++){int pair=(1<<i)|(1<<(i+4));if((tile.Ports&pair)!=pair)continue;int other=0;while((tile.Ports&~pair&(1<<other))==0)other++;int dot=Dx[i]*Dx[other]+Dy[i]*Dy[other];stem=1<<(dot<0?i:i+4);break;}if(stem==0)stem=tile.Ports&-tile.Ports;}
            if(normal==0){int s=0;while(stem!=(1<<s))s++;int opposite=1<<((s+4)%8);int available=tile.Ports&~stem;normal=(tile.Ports&opposite)!=0?opposite:available&-available;}
            branch=tile.Ports&~(stem|normal);
        }
        public static IReadOnlyList<PanelLine> Lines(PanelTile tile) {
            if(tile.Kind==PanelTileKind.LinePermission&&!tile.InlinePermission)return Array.Empty<PanelLine>();
            if(tile.Kind==PanelTileKind.LinePermission&&tile.Ports==0)return PanelGeometry.Lines(PanelTileKind.Straight);
            if(tile.Ports==0)return PanelGeometry.Lines(tile.Kind);
            int key=(int)tile.Kind*256+tile.Ports;lock(shapes){IReadOnlyList<PanelLine> found;if(shapes.TryGetValue(key,out found))return found;var lines=new List<PanelLine>();for(int i=0;i<8;i++)if((tile.Ports&(1<<i))!=0)lines.Add(new PanelLine(0,0,Dx[i],-Dy[i]));if(tile.Kind==PanelTileKind.Signal){lines.Add(new PanelLine(0,0,0,.375f));lines.Add(new PanelLine(0,.375f,.5f,.375f));}var result=lines.ToArray();shapes[key]=result;return result;}
        }
        public static int RailLineCount(PanelTile tile)=>Lines(tile).Count-(tile.Kind==PanelTileKind.Signal?2:0);
        public static bool LineActive(PanelTile tile,int index,bool thrown) {
            if(tile.Kind!=PanelTileKind.Switch)return true;
            if(tile.Ports==0)return index==0||((index==2)==thrown);
            int stem,normal,branch;Arms(tile,out stem,out normal,out branch);for(int i=0,n=0;i<8;i++)if((tile.Ports&(1<<i))!=0){if(n++==index)return (1<<i)==stem||((1<<i)&(thrown?branch:normal))!=0;}return true;
        }
    }
    public static class PanelEditing {
        static void Commit(PanelDocument doc,PanelDocument next){next.Validate(doc.WorldKey);doc.Columns=next.Columns;doc.Rows=next.Rows;doc.OriginX=next.OriginX;doc.OriginY=next.OriginY;doc.Tiles=next.Tiles;}
        public static void PlaceField(PanelDocument doc,int x,int y,PanelTileKind kind)=>doc.Put(PreviewField(doc,x,y,kind));
        public static PanelTile PreviewField(PanelDocument doc,int x,int y,PanelTileKind kind) {
            if(kind!=PanelTileKind.Signal&&kind!=PanelTileKind.Switch&&kind!=PanelTileKind.Label&&kind!=PanelTileKind.TrainNumber&&kind!=PanelTileKind.AuxiliaryTarget&&kind!=PanelTileKind.LinePermission)
                throw new InvalidOperationException("Für Gleise das Zeichenwerkzeug verwenden.");
            var old=doc.At(x,y);
            if(old==null)return new PanelTile{X=x,Y=y,Kind=kind,InlinePermission=kind==PanelTileKind.LinePermission};
            int ports=PanelConnections.Ports(old),count=PanelConnections.Count(ports);
            bool plain=old.Kind==PanelTileKind.Straight||old.Kind==PanelTileKind.Diagonal||old.Kind==PanelTileKind.Curve;
            if(!plain||old.Reference!=""||old.Label!=""||count>2||
                (kind!=PanelTileKind.LinePermission&&kind!=PanelTileKind.TrainNumber&&kind!=PanelTileKind.AuxiliaryTarget&&(kind!=PanelTileKind.Signal||old.Section!=null)))
                throw new InvalidOperationException("Dieses Feld ist bereits belegt oder hat eine eigene Bindung. Freies Feld wählen oder das bestehende Feld bearbeiten.");
            var added=old.Clone();
            added.Kind=kind;
            added.InlinePermission=kind==PanelTileKind.LinePermission;
            added.Ports=PanelConnections.Transform(ports,old.Rotation,old.Mirror,true);
            added.Stem=added.Normal=0;
            return added;
        }

        internal static PanelTile TrackBelowSymbol(PanelDocument doc,PanelTile tile) {
            if(tile==null||(tile.Kind!=PanelTileKind.Signal&&tile.Kind!=PanelTileKind.TrainNumber&&tile.Kind!=PanelTileKind.AuxiliaryTarget&&!(tile.Kind==PanelTileKind.LinePermission&&tile.InlinePermission)))return null;
            int mask=PanelConnections.Ports(tile);
            bool connected=Enumerable.Range(0,8).Any(i=>{
                if((mask&(1<<i))==0)return false;
                var neighbour=doc.At(tile.X+PanelConnections.Dx[i],tile.Y+PanelConnections.Dy[i]);
                return neighbour!=null&&(PanelConnections.Ports(neighbour)&(1<<((i+4)%8)))!=0;
            });
            if(!connected)return null;
            bool bends=PanelConnections.Count(mask)==2&&Enumerable.Range(0,4).All(i=>(mask&((1<<i)|(1<<(i+4))))!=((1<<i)|(1<<(i+4))));
            var track=tile.Clone();
            track.Kind=bends?PanelTileKind.Curve:PanelTileKind.Straight;
            track.Ports=PanelConnections.Transform(mask,tile.Rotation,tile.Mirror,true);
            track.Label=track.Reference=track.Number=track.Prefix="";
            track.AlternateSignal=null;if(tile.Kind!=PanelTileKind.LinePermission)track.CircuitRails=null;track.NumberId=track.NumberMirror=null;track.InlineNumberMirror=false;
            track.MainSignalDirection=track.ShuntSignalDirection=null;track.SwapSignalHalves=false;
            track.Target=null;track.LinkedAuxiliary=null;
            track.InlinePermission=false;track.PermissionAuxiliary=null;
            track.Stem=track.Normal=0;
            return track;
        }
        public static void Insert(PanelDocument doc,bool row,int at)=>EditorCanvasGeometry.Insert(doc,row,at);
        public static void Paste(PanelDocument target,PanelDocument source,int x,int y) {
            if(target.WorldKey!=source.WorldKey)throw new InvalidOperationException("Bereich gehört zu einer anderen Welt.");
            var next=target.Clone();foreach(var tile in source.Tiles){var copy=tile.Clone();copy.NumberId=null;copy.X+=x;copy.Y+=y;if(next.At(copy.X,copy.Y)!=null)throw new InvalidOperationException("Ziel enthält bereits Felder. Erst Platz schaffen oder eine freie Position wählen.");next.Tiles.Add(copy);}
            foreach(var a in source.Areas){var copy=JsonConvertArea(a);copy.Id=Guid.NewGuid().ToString("N");copy.X+=x;copy.Y+=y;if(next.Areas.Any(b=>string.Equals(b.Code,copy.Code,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Bereichskennung im Ziel bereits vorhanden: "+copy.Code);next.Areas.Add(copy);}
            Commit(target,next);target.Areas=next.Areas;
        }
        static PanelArea JsonConvertArea(PanelArea area)=>Newtonsoft.Json.JsonConvert.DeserializeObject<PanelArea>(Newtonsoft.Json.JsonConvert.SerializeObject(area));
        public static void Draw(PanelDocument doc,int x0,int y0,int x1,int y1) {
            if(x0<0||x0>=doc.Columns||x1<0||x1>=doc.Columns||y0<0||y0>=doc.Rows||y1<0||y1>=doc.Rows)throw new InvalidOperationException("Zeichnung außerhalb der Tafel.");
            if(x0==x1&&y0==y1)return;
            var next=doc.Clone();var points=new List<Tuple<int,int>>{Tuple.Create(x0,y0)};int x=x0,y=y0;
            while(x!=x1||y!=y1){x+=Math.Sign(x1-x);y+=Math.Sign(y1-y);points.Add(Tuple.Create(x,y));}
            for(int n=0;n<points.Count;n++) {
                var p=points[n];var old=next.At(p.Item1,p.Item2);if(old?.Kind==PanelTileKind.LinePermission)throw new InvalidOperationException(LocalText.Select("Move the permission indicator before drawing.","Erlaubnismelder vor dem Zeichnen versetzen."));if(old?.Kind==PanelTileKind.Label)throw new InvalidOperationException("Beschriftung im Zeichenweg. Bereich verschieben oder anderes Ziel wählen.");
                int mask=old==null?0:PanelConnections.Ports(old),previous=mask;
                foreach(int j in new[]{n-1,n+1})if(j>=0&&j<points.Count)mask|=1<<PanelConnections.Direction(points[j].Item1-p.Item1,points[j].Item2-p.Item2);
                int count=PanelConnections.Count(mask);if(count>4||(old?.Kind==PanelTileKind.Signal||old?.Kind==PanelTileKind.TrainNumber||old?.Kind==PanelTileKind.AuxiliaryTarget)&&count>2||old?.Kind==PanelTileKind.Switch&&count>3&&!string.IsNullOrEmpty(old.Reference))throw new InvalidOperationException("Zu viele Anschlüsse an einem gebundenen Symbol. Anders führen oder Symbol vorher bearbeiten.");
                var tile=old??new PanelTile{X=p.Item1,Y=p.Item2};
                if(tile.Kind!=PanelTileKind.Signal&&tile.Kind!=PanelTileKind.TrainNumber&&tile.Kind!=PanelTileKind.AuxiliaryTarget){var kind=count==3?PanelTileKind.Switch:count==4?PanelTileKind.Cross:count==2&&!Enumerable.Range(0,4).Any(i=>(mask&(1<<i))!=0&&(mask&(1<<(i+4)))!=0)?PanelTileKind.Curve:PanelTileKind.Straight;if(kind==PanelTileKind.Switch&&tile.Section!=null)throw new InvalidOperationException("Hier ist bereits ein Belegungsabschnitt zugeordnet. Für die neue Weiche die Zuordnung zuerst bearbeiten.");tile.Kind=kind;}
                tile.Ports=PanelConnections.Transform(mask,tile.Rotation,tile.Mirror,true);if(mask!=previous)tile.Stem=tile.Normal=0;if(old==null)next.Tiles.Add(tile);
            }
            Commit(doc,next);
        }
    }
    public readonly struct PanelWindow {
        public readonly int Left,Top,Right,Bottom;
        public PanelWindow(int left,int top,int right,int bottom){Left=left;Top=top;Right=right;Bottom=bottom;}
        public bool Contains(int x,int y)=>x>=Left&&x<=Right&&y>=Top&&y<=Bottom;
        public static PanelWindow Visible(int columns,int rows,float width,float height,float minX,float maxX,float minY,float maxY)=>
            new PanelWindow(Math.Max(0,(int)Math.Floor(minX/width)),Math.Max(0,(int)Math.Floor(-maxY/height)),
                Math.Min(columns-1,(int)Math.Floor(maxX/width)),Math.Min(rows-1,(int)Math.Floor(-minY/height)));
    }
    public static class PanelPicking {
        public static bool RightHalf(int x,float zoom,float localX)=>
            localX>=(x+.5f)*PanelGeometry.CellWidth*zoom;
        public static bool SecondHalf(PanelTile tile,int x,int y,float zoom,float localX,float localY)=>
            SignalCellLayout.HasAuxiliaryHalf(tile)&&SignalCellLayout.MainDirection(tile)%2==1?
                localY<=-(y+.5f)*PanelGeometry.CellHeight*zoom:RightHalf(x,zoom,localX);
        public static bool Cell(int columns,int rows,float zoom,float localX,float localY,out int x,out int y) {
            x=y=-1;if(zoom<.005f||zoom>2||float.IsNaN(localX)||float.IsNaN(localY)||float.IsInfinity(localX)||float.IsInfinity(localY))return false;
            x=(int)Math.Floor(localX/(PanelGeometry.CellWidth*zoom));y=(int)Math.Floor(-localY/(PanelGeometry.CellHeight*zoom));return x>=0&&y>=0&&x<columns&&y<rows;
        }
    }
    public sealed class PanelDeletion {
        readonly string snapshot;
        readonly PanelRegion region;
        public readonly int Count;
        readonly PanelTile retainedTrack;
        public bool KeepsTrack=>retainedTrack!=null;
        public PanelDeletion(PanelDocument document,PanelRegion selected,bool keepConnectedSignalTrack=false) {
            snapshot=Newtonsoft.Json.JsonConvert.SerializeObject(document);
            region=new PanelRegion{X=selected.X,Y=selected.Y,Width=selected.Width,Height=selected.Height};
            Count=document.Tiles.Count(t=>region.Contains(t.X,t.Y));
            if(keepConnectedSignalTrack&&region.Width==1&&region.Height==1)retainedTrack=PanelEditing.TrackBelowSymbol(document,document.At(region.X,region.Y));
        }
        public void Apply(PanelDocument document,bool confirmed) {
            if(!confirmed)throw new InvalidOperationException("Löschen wurde nicht bestätigt.");
            if(Newtonsoft.Json.JsonConvert.SerializeObject(document)!=snapshot)throw new InvalidOperationException("Tafel wurde inzwischen geändert. Löschung neu auswählen und bestätigen.");
            if(retainedTrack!=null){document.Put(retainedTrack);return;}
            document.Tiles=document.Tiles.Where(t=>!region.Contains(t.X,t.Y)).ToList();
            document.Areas=document.Areas.Where(a=>!(region.Contains(a.X,a.Y)&&region.Contains(a.X+a.Width-1,a.Y+a.Height-1))).ToList();
        }
    }
}
