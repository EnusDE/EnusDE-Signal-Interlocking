using System;
using System.Linq;
using Newtonsoft.Json;

namespace EnusDE.Signaltechnik {
    // A setup target is chosen in the schematic before any world object is offered.
    public sealed class DiagramFieldTarget {
        public string World,Desk,Shape;
        public int X,Y;
        public bool Point;
        public PanelTile Snapshot;
        public string Cell=>EditorCanvasGeometry.ColumnName(X)+(Y+1);
        public static DiagramFieldTarget Capture(DeskLibrary library,string deskId,int x,int y) {
            var desk=library.Desks.SingleOrDefault(d=>d.Id==deskId&&!d.IsMaster);
            if(desk==null)throw new InvalidOperationException(LocalText.Select("Choose a local interlocking diagram first.","Zuerst das Gleisbild eines örtlichen Stellwerks wählen."));
            var tile=desk.Panel.At(x,y);bool point=tile?.Kind==PanelTileKind.Switch;
            if(x<0||y<0||x>=desk.Panel.Columns||y>=desk.Panel.Rows||!SetupAssignments.CanAssign(tile,point))throw new InvalidOperationException(LocalText.Select("Choose an unassigned signal or point field.","Ein noch nicht zugeordnetes Signal- oder Weichenfeld wählen."));
            return new DiagramFieldTarget{World=library.WorldKey,Desk=deskId,X=x,Y=y,Point=point,Snapshot=tile?.Clone(),Shape=ShapeOf(desk.Panel)};
        }
        static string ShapeOf(PanelDocument panel)=>SharedDocuments.Hash(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(panel)));
        public bool Current(DeskLibrary library) {
            var desk=library?.Desks.FirstOrDefault(d=>d.Id==Desk&&!d.IsMaster);
            return library?.WorldKey==World&&desk!=null&&ShapeOf(desk.Panel)==Shape;
        }
        public void RequireCurrent(DeskLibrary library) {
            if(!Current(library))throw new InvalidOperationException(LocalText.Select("The diagram changed. Click the destination field again before selecting a world object.","Das Gleisbild wurde geändert. Vor der Weltauswahl das Zielfeld erneut anklicken."));
        }
    }
    public static class DiagramFieldSetup {
        public static bool Eligible(PanelTile tile)=>tile!=null&&(tile.Kind==PanelTileKind.Signal||tile.Kind==PanelTileKind.Switch)&&SetupAssignments.CanAssign(tile,tile.Kind==PanelTileKind.Switch);
        public static PanelTile[] Pending(PanelDocument panel)=>panel.Tiles.Where(Eligible).OrderBy(t=>t.Y).ThenBy(t=>t.X).ToArray();
        public static DeskLibrary Assign(DeskLibrary library,DiagramFieldTarget target,SetupObject source,MarkerRecord signal) {
            target.RequireCurrent(library);
            if(source.Point!=target.Point)throw new InvalidOperationException(LocalText.Select("Choose the world object matching the green field: signal for signal, point for point.","Das zum grünen Feld passende Weltobjekt wählen: Signal für Signal, Weiche für Weiche."));
            return SetupAssignments.Assign(library,source,target.Desk,target.X,target.Y,signal);
        }
    }
}
