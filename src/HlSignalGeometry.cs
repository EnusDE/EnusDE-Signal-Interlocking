using System.Linq;

namespace EnusDE.Signaltechnik {
    public static class HlSignalGeometry {
        // Green upper right, yellow upper left; stop red and optional Ra12 lamps.
        // Last two elements are the yellow (60) and green (100) light strips.
        public static float[][] Lamps(bool distant=false)=>distant?
            new[]{new[]{.23f,3.29f},new[]{-.23f,3.29f}}:
            new[]{new[]{.23f,3.88f},new[]{-.23f,3.88f},new[]{-.23f,3.53f},
                new[]{.23f,3.18f},new[]{.23f,3.53f},new[]{-.23f,3.18f},new[]{0f,2.84f},new[]{0f,2.64f}};
    }
    public sealed partial class MarkerMeshData {
        public static MarkerMeshData CreateHlDistantBoard() {
            var data=new MarkerMeshData();
            data.Box(-.21f,1.62f,-.045f,.21f,2.28f,.025f,0);
            data.Box(-.18f,1.65f,-.055f,.18f,2.25f,-.045f,1);
            foreach(float side in new[]{-.14f,.14f}) {
                data.Brace(side,2.18f,0,1.95f,-.070f,.022f,.008f,0);
                data.Brace(0,1.95f,side,1.72f,-.070f,.022f,.008f,0);
            }
            return data;
        }
        public static MarkerMeshData CreateHlCombinedMarker() {
            var data=new MarkerMeshData();
            data.Prism(new[]{new[]{-.18f,2.97f},new[]{.18f,2.97f},new[]{0f,2.61f}},-.055f,-.025f,0);
            data.Prism(new[]{new[]{-.135f,2.94f},new[]{.135f,2.94f},new[]{0f,2.68f}},-.065f,-.055f,1);
            return data;
        }
        public static MarkerMeshData CreateHlHead(bool distant=false,bool headOnly=false) {
            var data=new MarkerMeshData();float bottom=distant?2.99f:2.98f,top=distant?3.55f:4.15f;
            if(!headOnly)data.Box(-.045f,0,-.045f,.045f,top,.045f,1);
            data.Prism(new[]{new[]{-.36f,bottom},new[]{.36f,bottom},new[]{.46f,bottom+.10f},new[]{.46f,top-.10f},
                new[]{.36f,top},new[]{-.36f,top},new[]{-.46f,top-.10f},new[]{-.46f,bottom+.10f}},-.055f,.055f,0);
            foreach(var p in HlSignalGeometry.Lamps(distant).Take(distant?2:6))
                data.Box(p[0]-.13f,p[1]+.10f,-.25f,p[0]+.13f,p[1]+.125f,-.055f,0);
            if(!distant)foreach(float y in new[]{2.84f,2.64f})data.Box(-.37f,y-.075f,-.055f,.37f,y+.075f,.055f,0);
            return data;
        }
    }
}
