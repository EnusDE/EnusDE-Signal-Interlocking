using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class PointDrivePolicy {
        public static string LinkedDesk(DeskLibrary library,string point) {
            if(library==null||string.IsNullOrEmpty(point))return null;
            var owners=library.Desks.Where(d=>!d.IsMaster&&d.Panel.Tiles.Any(t=>t.Kind==PanelTileKind.Switch&&t.Reference==point)).Select(d=>d.Id).Distinct().ToArray();
            return owners.Length==1?owners[0]:null;
        }
        public static bool Electric(DeskLibrary library,string point)=>point!=null&&library?.Points.TryGetValue(point,out var assignment)==true&&assignment.ElectricStand&&LinkedDesk(library,point)!=null;
    }
}
