using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace EnusDE.Runenwacht {
    // Original procedural geometry. No game mesh or downloaded artwork is redistributed.
    public sealed class RunenwachtGeometry {
        public readonly List<Vector3> Vertices=new List<Vector3>();
        public readonly List<int>[] Triangles={new List<int>(),new List<int>(),new List<int>()};
        public void Triangle(Vector3 a,Vector3 b,Vector3 c,int material=0){int i=Vertices.Count;Vertices.Add(a);Vertices.Add(b);Vertices.Add(c);Triangles[material].Add(i);Triangles[material].Add(i+1);Triangles[material].Add(i+2);}
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int m=0){Triangle(a,b,c,m);Triangle(a,c,d,m);}
        public void Box(Vector3 center,Vector3 size,int m=0,float angle=0) {
            var q=Quaternion.Euler(0,0,angle);var v=new Vector3[8];for(int i=0;i<8;i++)v[i]=center+q*new Vector3((i&1)==0?-size.x/2:size.x/2,(i&2)==0?-size.y/2:size.y/2,(i&4)==0?-size.z/2:size.z/2);
            foreach(var f in new[]{new[]{0,2,3,1},new[]{4,5,7,6},new[]{0,4,6,2},new[]{1,3,7,5},new[]{0,1,5,4},new[]{2,6,7,3}})Quad(v[f[0]],v[f[1]],v[f[2]],v[f[3]],m);
        }
        public void Ring(Vector3 center,float outer,float inner,float depth,int m=1,int steps=48) {
            for(int i=0;i<steps;i++) {
                float a=i*Mathf.PI*2/steps,b=(i+1)*Mathf.PI*2/steps;
                Vector3 Point(float r,float t,float z)=>center+new Vector3(Mathf.Cos(t)*r,Mathf.Sin(t)*r,z);
                var of=Point(outer,a,-depth/2);var nf=Point(outer,b,-depth/2);var ib=Point(inner,a,depth/2);var nb=Point(inner,b,depth/2);
                if(inner<=0){Triangle(of,Point(0,0,-depth/2),nf,m);Triangle(Point(outer,a,depth/2),Point(outer,b,depth/2),Point(0,0,depth/2),m);}
                else {Quad(of,Point(inner,a,-depth/2),Point(inner,b,-depth/2),nf,m);Quad(Point(outer,a,depth/2),Point(outer,b,depth/2),nb,ib,m);}
                Quad(of,nf,Point(outer,b,depth/2),Point(outer,a,depth/2),m);
                if(inner>0)Quad(Point(inner,a,-depth/2),ib,nb,Point(inner,b,-depth/2),m);
            }
        }
        public void Crystal(Vector3 center,float width,float height,float depth,int m=0) {
            // Six-sided opaque gemstone, beveled shoulders and two tapered tips.
            var lo=new Vector3[6];var hi=new Vector3[6];
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;lo[i]=center+new Vector3(Mathf.Cos(a)*width/2,-height*.26f,Mathf.Sin(a)*depth/2);hi[i]=center+new Vector3(Mathf.Cos(a)*width*.44f,height*.26f,Mathf.Sin(a)*depth*.44f);}
            for(int i=0;i<6;i++){int j=(i+1)%6;Triangle(center+Vector3.down*height/2,lo[i],lo[j],m);Quad(lo[j],lo[i],hi[i],hi[j],m);Triangle(hi[j],hi[i],center+Vector3.up*height/2,m);}
        }
        public Mesh Mesh(string name) {
            var mesh=new Mesh{name=name};if(Vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(Vertices);mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(Triangles[i],i);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
