using System;
using System.Linq;

namespace EnusDE.Signaltechnik {
    // The placement proposal may grow; hovering never changes the saved or proposed panel.
    public static class BlockDiagramPlacement {
        public static string Issue(PanelDocument panel,string line,int x,int y) {
            var area=BlockDiagram.Area(panel,line);
            if(area==null)return LocalText.Select("The block assembly is missing.","Der Blockverbund fehlt.");
            try{CanvasGrowth.For(panel,new PanelRegion{X=x,Y=y,Width=area.Width,Height=area.Height},0);}catch(InvalidOperationException e){return e.Message;}
            if(panel.Tiles.Any(t=>!area.Contains(t.X,t.Y)&&t.X>=x&&t.X<x+area.Width&&t.Y>=y&&t.Y<y+area.Height)||panel.Areas.Any(a=>a.Id!=area.Id&&(a.BlockLine!=null||a.NumberLine!=null)&&a.X<x+area.Width&&x<a.X+a.Width&&a.Y<y+area.Height&&y<a.Y+a.Height))return LocalText.Select("This position overlaps another field or assembly. Choose free cells.","Diese Position überlappt ein anderes Feld oder einen Verbund. Freie Felder wählen.");
            return null;
        }
        public static CanvasGrowth Move(PanelDocument panel,string line,int x,int y) {
            string issue=Issue(panel,line,x,y);if(issue!=null)throw new InvalidOperationException(issue);
            var area=BlockDiagram.Area(panel,line);var growth=CanvasGrowth.For(panel,new PanelRegion{X=x,Y=y,Width=area.Width,Height=area.Height},0);
            var next=panel.Clone();growth.Apply(next);var moved=BlockDiagram.Area(next,line);
            new PanelRegion{X=moved.X,Y=moved.Y,Width=moved.Width,Height=moved.Height}.Move(next,x+growth.Left,y+growth.Top);
            CanvasGrowth.Commit(panel,next);return growth;
        }
        public static PanelDocument Display(PanelDocument panel,string line) {
            var copy=panel.Clone();var area=BlockDiagram.Area(panel,line)??throw new InvalidOperationException(LocalText.Select("The block assembly is missing.","Der Blockverbund fehlt."));
            copy.Columns=Math.Min(PanelDocument.MaxColumns,Math.Max(copy.Columns,area.X+area.Width)+area.Width+2);
            copy.Rows=Math.Min(PanelDocument.MaxRows,Math.Max(copy.Rows,area.Y+area.Height)+area.Height+2);return copy;
        }
    }
}
