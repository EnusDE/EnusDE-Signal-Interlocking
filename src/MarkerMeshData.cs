using System;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    // Original procedural geometry. No game/DB models, images or textures copied.
    public sealed class MarkerMeshData {
        public const float MastHeight=6.75f;
        public const float MainLift=MastHeight-4.15f;
        public readonly List<float[]> Vertices=new List<float[]>();
        public readonly List<int>[] Triangles={new List<int>(),new List<int>(),new List<int>()};
        public static MarkerMeshData Create() {
            var mesh=new MarkerMeshData();const int segments=24;
            for(int ring=0;ring<7;ring++) {
                double y,r;
                if(ring<2){y=ring*.30;r=.095;}
                else {double t=(ring-2)/5d;y=.30+t*.24;r=.16*Math.Sqrt(1-t*t);}
                for(int n=0;n<segments;n++){double angle=n*2*Math.PI/segments;mesh.Vertices.Add(new[]{(float)(r*Math.Cos(angle)),(float)y,(float)(r*Math.Sin(angle))});}
            }
            for(int ring=0;ring<6;ring++)for(int n=0;n<segments;n++) {
                int next=(n+1)%segments,a=ring*segments+n,b=ring*segments+next,c=(ring+1)*segments+n,d=(ring+1)*segments+next;
                int material=ring==0?2:(n/(segments/4))%2;
                mesh.Triangles[material].AddRange(new[]{a,c,b,b,c,d});
            }
            int top=mesh.Vertices.Count;mesh.Vertices.Add(new[]{0f,.54f,0f});
            for(int n=0;n<segments;n++)mesh.Triangles[(n/(segments/4))%2].AddRange(new[]{6*segments+n,top,6*segments+(n+1)%segments});
            return mesh;
        }
        public static MarkerMeshData CreateSpeedBoard() {
            var mesh=new MarkerMeshData();
            mesh.Box(-.035f,0,-.035f,.035f,1.40f,.035f,2);
            mesh.Box(-.35f,1.40f,-.025f,.35f,2.10f,.025f,1);
            mesh.Box(-.32f,1.43f,-.028f,.32f,2.07f,.028f,0);
            return mesh;
        }
        public static MarkerMeshData CreateSignal(bool shunt,bool distant,bool headOnly=false) {
            var data=new MarkerMeshData();float h=shunt?1.15f:distant?3.55f:4.15f;
            if(!headOnly)data.Box(-.045f,0,-.045f,.045f,h,.045f,1);
            data.Box(shunt?-.30f:-.34f,shunt?.57f:distant?2.72f:2.92f,-.055f,shunt?.30f:.34f,h,.055f,0);
            return data;
        }
        public static MarkerMeshData CreateSignalPlate() {
            var mesh=new MarkerMeshData();
            mesh.Box(-.39f,-.105f,-.021f,.39f,.105f,.018f,0);
            return mesh;
        }
        public static MarkerMeshData CreateCircuitBox(bool b) {
            var mesh=new MarkerMeshData();
            mesh.Box(-.19f,.015f,-.14f,.19f,.32f,.14f,2);
            mesh.Box(-.22f,.30f,-.17f,.22f,.36f,.17f,2);
            mesh.Box(-.18f,.11f,-.145f,.18f,.255f,-.14f,0);
            mesh.Box(-.14f,.05f,-.16f,-.11f,.09f,-.135f,1);
            if(b)mesh.Box(-.085f,.05f,-.16f,-.055f,.09f,-.135f,1);
            return mesh;
        }
        public static MarkerMeshData CreateFoundation() {
            var data=new MarkerMeshData();
            data.Box(-.27f,-.14f,-.27f,.27f,.11f,.27f,0);
            data.Box(-.18f,.11f,-.18f,.18f,.17f,.18f,0);
            data.Box(-.12f,.17f,-.12f,.12f,.195f,.12f,1);
            return data;
        }
        public static MarkerMeshData CreateMast(){var data=new MarkerMeshData();data.Box(-.045f,0,-.045f,.045f,MastHeight,.045f,1);return data;}
        void Box(float x0,float y0,float z0,float x1,float y1,float z1,int material) {
            var p=new[]{new[]{x0,y0,z0},new[]{x1,y0,z0},new[]{x1,y1,z0},new[]{x0,y1,z0},new[]{x0,y0,z1},new[]{x1,y0,z1},new[]{x1,y1,z1},new[]{x0,y1,z1}};
            int start=Vertices.Count;Vertices.AddRange(p);
            foreach(int i in new[]{0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,3,7,6,3,6,2,0,1,5,0,5,4})Triangles[material].Add(start+i);
        }
    }
}
