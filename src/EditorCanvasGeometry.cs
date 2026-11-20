using System;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class CanvasGrowth {
        public int Left,Top,Right,Bottom;
        public bool Changed=>Left+Top+Right+Bottom!=0;
        public static CanvasGrowth For(PanelDocument doc,PanelRegion bounds,int margin=2) {
            if(bounds==null||bounds.Width<1||bounds.Height<1||margin<0)throw new ArgumentOutOfRangeException(nameof(bounds));
            var g=new CanvasGrowth();
            void Axis(int size,int start,int length,int limit,out int before,out int after) {
                long end=(long)start+length;long needBefore=Math.Max(0,-(long)start),needAfter=Math.Max(0,end-size);
                if(size+needBefore+needAfter>limit)throw new InvalidOperationException(LocalText.Select("Diagram size limit reached (2048 columns / 512 rows). Make room before continuing.","Tafelgrenze erreicht (2048 Spalten / 512 Zeilen). Vor dem Fortsetzen Platz schaffen."));
                long spare=limit-size-needBefore-needAfter;
                long extraBefore=Math.Min(spare,Math.Max(0,(long)margin-start-needBefore));spare-=extraBefore;
                long extraAfter=Math.Min(spare,Math.Max(0,end+margin-size-needAfter));
                before=(int)(needBefore+extraBefore);after=(int)(needAfter+extraAfter);
            }
            Axis(doc.Columns,bounds.X,bounds.Width,PanelDocument.MaxColumns,out g.Left,out g.Right);
            Axis(doc.Rows,bounds.Y,bounds.Height,PanelDocument.MaxRows,out g.Top,out g.Bottom);return g;
        }
        public void Apply(PanelDocument doc) {
            var next=doc.Clone();next.Columns=checked(next.Columns+Left+Right);next.Rows=checked(next.Rows+Top+Bottom);
            next.OriginX=checked(next.OriginX+Left);next.OriginY=checked(next.OriginY+Top);
            foreach(var tile in next.Tiles){tile.X+=Left;tile.Y+=Top;}
            foreach(var area in next.Areas){area.X+=Left;area.Y+=Top;}
            next.Validate(doc.WorldKey);Commit(doc,next);
        }
        internal static void Commit(PanelDocument target,PanelDocument next){target.Columns=next.Columns;target.Rows=next.Rows;target.OriginX=next.OriginX;target.OriginY=next.OriginY;target.Tiles=next.Tiles;target.Areas=next.Areas;}
    }
    public static class EditorCanvasGeometry {
        public const int Border=3;
        public static string ColumnName(int column) {
            if(column<0)throw new ArgumentOutOfRangeException(nameof(column));string text="";
            for(long n=(long)column+1;n>0;n=(n-1)/26)text=(char)('A'+(n-1)%26)+text;return text;
        }
        public static bool Pick(PanelDocument doc,float zoom,float px,float py,out int x,out int y) {
            x=y=-1;if(zoom<.005f||zoom>2||float.IsNaN(px)||float.IsNaN(py)||float.IsInfinity(px)||float.IsInfinity(py))return false;
            x=(int)Math.Floor(px/(PanelGeometry.CellWidth*zoom));y=(int)Math.Floor(-py/(PanelGeometry.CellHeight*zoom));
            return x>=-Border&&y>=-Border&&x<doc.Columns+Border&&y<doc.Rows+Border;
        }
        public static double ZoomScroll(double scroll,double pointer,double oldZoom,double newZoom)=>((scroll+pointer)/oldZoom)*newZoom-pointer;
        public static void Insert(PanelDocument doc,bool row,int at,int count=1) {
            int size=row?doc.Rows:doc.Columns,limit=row?PanelDocument.MaxRows:PanelDocument.MaxColumns;
            if(at<0||at>size||count<1||count>limit-size)throw new InvalidOperationException(LocalText.Select("Cannot insert here: invalid position or diagram size limit reached.","Hier kann nicht eingefügt werden: ungültige Position oder Tafelgrenze erreicht."));
            var next=doc.Clone();if(row){next.Rows+=count;if(at==0)next.OriginY=checked(next.OriginY+count);}else{next.Columns+=count;if(at==0)next.OriginX=checked(next.OriginX+count);}
            foreach(var t in next.Tiles){if(row&&t.Y>=at)t.Y+=count;if(!row&&t.X>=at)t.X+=count;}
            foreach(var a in next.Areas)if(row){if(a.Y>=at)a.Y+=count;else if(a.Y+a.Height>at)a.Height+=count;}else{if(a.X>=at)a.X+=count;else if(a.X+a.Width>at)a.Width+=count;}
            next.Validate(doc.WorldKey);CanvasGrowth.Commit(doc,next);
        }
    }
    public sealed class EditorAxisSelection {
        public bool? Row {get;private set;}
        public int Anchor {get;private set;}
        public void Clear(){Row=null;}
        public PanelRegion Select(bool row,int index,bool shift,int columns,int rows) {
            if(index<0||index>=(row?rows:columns))throw new ArgumentOutOfRangeException(nameof(index));
            if(!shift||Row!=row||Anchor>=(row?rows:columns))Anchor=index;Row=row;int first=Math.Min(Anchor,index),length=Math.Abs(Anchor-index)+1;
            return row?new PanelRegion{X=0,Y=first,Width=columns,Height=length}:new PanelRegion{X=first,Y=0,Width=length,Height=rows};
        }
    }
}
