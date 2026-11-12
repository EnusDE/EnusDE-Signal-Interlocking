using System;

namespace EnusDE.Signaltechnik {
    public sealed class EditorLayout {
        public WorkspaceBox Header, Tools, Diagram, Details, Footer, Catalogue;

        public static EditorLayout For(double width,double height) {
            if(double.IsNaN(width)||double.IsInfinity(width)||double.IsNaN(height)||double.IsInfinity(height)||width<640||height<420)
                throw new ArgumentOutOfRangeException(nameof(width));
            double rail=60, details=Math.Min(380,Math.Max(228,width*.27));
            double diagramWidth=width-rail-details-48;
            double catalogueWidth=Math.Min(650,width-40);
            return new EditorLayout {
                Header=new WorkspaceBox(12,10,width-24,50),
                Tools=new WorkspaceBox(12,72,rail,height-122),
                Diagram=new WorkspaceBox(rail+24,72,diagramWidth,height-122),
                Details=new WorkspaceBox(width-details-12,72,details,height-122),
                Footer=new WorkspaceBox(12,height-42,width-24,32),
                Catalogue=new WorkspaceBox((width-catalogueWidth)/2,72,catalogueWidth,height-122)
            };
        }

        public static (double Zoom,double CenterX,double CenterY) Fit(PanelDocument panel,double width,double height) {
            if(panel.Tiles.Count==0&&panel.Areas.Count==0)
                return (Math.Min(1,Math.Max(.65,Math.Min(width/480,height/336))),5,3.5);
            int left=panel.Columns,top=panel.Rows,right=0,bottom=0;
            foreach(var tile in panel.Tiles) {
                left=Math.Min(left,tile.X);top=Math.Min(top,tile.Y);
                right=Math.Max(right,tile.X+1);bottom=Math.Max(bottom,tile.Y+1);
            }
            foreach(var area in panel.Areas) {
                left=Math.Min(left,area.X);top=Math.Min(top,area.Y);
                right=Math.Max(right,area.X+area.Width);bottom=Math.Max(bottom,area.Y+area.Height);
            }
            double zoom=Math.Min(1.25,Math.Max(.025,Math.Min(width/(PanelGeometry.CellWidth*(right-left+2)),height/(PanelGeometry.CellHeight*(bottom-top+2)))));
            return (zoom,(left+right)/2d,(top+bottom)/2d);
        }

        public static int GridStride(float cellSize)=>Math.Max(1,(int)Math.Ceiling(12/Math.Max(.01,cellSize)));
    }
}
