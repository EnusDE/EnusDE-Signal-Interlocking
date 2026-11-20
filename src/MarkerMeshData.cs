using System;
using System.Collections.Generic;
using System.Linq;

namespace EnusDE.Signaltechnik {
    // Original procedural geometry. No game/DB models, images or textures copied.
    public sealed partial class MarkerMeshData {
        public const float MastHeight=6.75f;
        public const float MainLift=MastHeight-4.15f;
        public readonly List<float[]> Vertices=new List<float[]>();
        public readonly List<int>[] Triangles={new List<int>(),new List<int>(),new List<int>()};
        public static MarkerMeshData CreateInvalidityCross() {
            var result=new MarkerMeshData();
            foreach(double angle in new[]{Math.PI/4,-Math.PI/4})for(int material=0;material<2;material++) {
                var beam=new MarkerMeshData();float half=material==0?.095f:.055f,length=material==0?.54f:.50f,z=material==0?0:-.03f;
                beam.Box(-half,-length,z-.014f,half,length,z+.014f,material);int offset=result.Vertices.Count;
                foreach(var vertex in beam.Vertices)result.Vertices.Add(new[]{(float)(vertex[0]*Math.Cos(angle)-vertex[1]*Math.Sin(angle)),(float)(vertex[0]*Math.Sin(angle)+vertex[1]*Math.Cos(angle)),vertex[2]});
                for(int i=0;i<3;i++)result.Triangles[i].AddRange(beam.Triangles[i].Select(index=>index+offset));
            }
            return result;
        }
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
            float left=shunt?-.30f:distant?-.34f:-.43f,right=-left;
            float bottom=shunt?.57f:distant?2.72f:2.92f,corner=shunt?.045f:.10f;
            // A clipped H/V screen with separate forward-projecting lamp shades.
            data.Prism(new[]{new[]{left+corner,bottom},new[]{right-corner,bottom},new[]{right,bottom+corner},
                new[]{right,h-corner},new[]{right-corner,h},new[]{left+corner,h},new[]{left,h-corner},new[]{left,bottom+corner}},-.055f,.055f,0);
            foreach(var position in HvSignalGeometry.Lamps(shunt?MarkerRecord.Shunt:distant?MarkerRecord.Distant:MarkerRecord.Main)) {
                float radius=shunt?.075f:.10f;
                data.Box(position[0]-radius-.025f,position[1]+radius, -.24f,
                    position[0]+radius+.025f,position[1]+radius+.025f,-.055f,0);
            }
            return data;
        }
        public static MarkerMeshData CreateLowMain(bool headOnly=false) {
            var data=new MarkerMeshData();
            if(!headOnly)data.Box(-.075f,0,-.075f,.075f,.43f,.075f,1);
            else {
                data.Prism(new[]{new[]{-.48f,.36f},new[]{.48f,.36f},new[]{.55f,.43f},new[]{.55f,1.66f},new[]{.48f,1.73f},new[]{-.48f,1.73f},new[]{-.55f,1.66f},new[]{-.55f,.43f}},-.055f,.055f,0);
                foreach(var p in LowMainGeometry.Main.Concat(LowMainGeometry.Shunt.Skip(2)))
                    data.Box(p[0]-.12f,p[1]+.10f,-.24f,p[0]+.12f,p[1]+.125f,-.055f,0);
            }
            return data;
        }
        public static MarkerMeshData CreateMastSign() {
            var data=new MarkerMeshData();
            data.Box(-.075f,3,-.025f,.075f,3.25f,.025f,0);
            data.Box(-.075f,3.25f,-.025f,.075f,3.55f,.025f,1);
            data.Box(-.075f,3.55f,-.025f,.075f,3.8f,.025f,0);
            return data;
        }
        public static MarkerMeshData CreateSignalPlate() {
            var mesh=new MarkerMeshData();
            mesh.Box(-.39f,-.105f,-.021f,.39f,.105f,.018f,0);
            return mesh;
        }
        public static MarkerMeshData CreateCircuitBox(bool b) {
            var mesh=new MarkerMeshData();
            mesh.Box(-.19f,0,-.14f,.19f,.32f,.14f,2);
            mesh.Box(-.22f,.30f,-.17f,.22f,.36f,.17f,2);
            mesh.Box(-.18f,.11f,-.145f,.18f,.255f,-.14f,0);
            mesh.Box(-.14f,.05f,-.16f,-.11f,.09f,-.135f,1);
            if(b)mesh.Box(-.085f,.05f,-.16f,-.055f,.09f,-.135f,1);
            return mesh;
        }
        public static MarkerMeshData CreateGroundSupport() {
            var data=new MarkerMeshData();
            data.Box(-.5f,-1,-.5f,.5f,0,.5f,0);
            return data;
        }
        public static MarkerMeshData CreateCircuitBoxC() {
            var mesh=CreateCircuitBox(true);mesh.Box(-.024f,.361f,-.12f,.024f,.365f,.04f,1);
            int start=mesh.Vertices.Count;mesh.Vertices.AddRange(new[]{new[]{-.09f,.366f,.025f},new[]{.09f,.366f,.025f},new[]{0f,.366f,.14f}});
            mesh.Triangles[1].AddRange(new[]{start,start+2,start+1});return mesh;
        }
        public static MarkerMeshData CreateFoundation() {
            var data=new MarkerMeshData();
            data.Box(-.27f,-.14f,-.27f,.27f,.11f,.27f,0);
            data.Box(-.18f,.11f,-.18f,.18f,.17f,.18f,0);
            data.Box(-.12f,.17f,-.12f,.12f,.195f,.12f,1);
            return data;
        }
        public static MarkerMeshData CreateMast(){
            var data=new MarkerMeshData();
            // Four steel uprights and alternating diagonal bracing form the H/V lattice mast.
            foreach(float x in new[]{-.095f,.095f})foreach(float z in new[]{-.07f,.07f})
                data.Box(x-.018f,0,z-.018f,x+.018f,MastHeight,z+.018f,1);
            for(float y=.18f;y<MastHeight-.5f;y+=.5f) {
                foreach(float z in new[]{-.07f,.07f}) {
                    float a=((int)(y*2)%2==0)?-.095f:.095f,b=-a;
                    data.Brace(a,y,b,y+.5f,z,.018f,.018f,1);
                    data.Box(-.113f,y-.012f,z-.018f,.113f,y+.012f,z+.018f,1);
                }
            }
            return data;
        }
        void Brace(float x0,float y0,float x1,float y1,float z,float halfWidth,float halfDepth,int material) {
            float length=(float)Math.Sqrt((x1-x0)*(x1-x0)+(y1-y0)*(y1-y0));
            float dx=-(y1-y0)/length*halfWidth,dy=(x1-x0)/length*halfWidth;
            Prism(new[]{new[]{x0-dx,y0-dy},new[]{x1-dx,y1-dy},new[]{x1+dx,y1+dy},new[]{x0+dx,y0+dy}},z-halfDepth,z+halfDepth,material);
        }
        void Prism(float[][] profile,float front,float back,int material) {
            int start=Vertices.Count,n=profile.Length;
            foreach(float z in new[]{front,back})foreach(var point in profile)Vertices.Add(new[]{point[0],point[1],z});
            for(int i=1;i<n-1;i++){Triangles[material].AddRange(new[]{start,start+i+1,start+i,start+n,start+n+i,start+n+i+1});}
            for(int i=0;i<n;i++){int j=(i+1)%n;Triangles[material].AddRange(new[]{start+i,start+j,start+n+j,start+i,start+n+j,start+n+i});}
        }
        void Box(float x0,float y0,float z0,float x1,float y1,float z1,int material) {
            var p=new[]{new[]{x0,y0,z0},new[]{x1,y0,z0},new[]{x1,y1,z0},new[]{x0,y1,z0},new[]{x0,y0,z1},new[]{x1,y0,z1},new[]{x1,y1,z1},new[]{x0,y1,z1}};
            int start=Vertices.Count;Vertices.AddRange(p);
            foreach(int i in new[]{0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,3,7,6,3,6,2,0,1,5,0,5,4})Triangles[material].Add(start+i);
        }
    }
}
