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
        static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        static Vector3 Unit(Vector3 a){float length=(float)Math.Sqrt(a.x*a.x+a.y*a.y+a.z*a.z);return a/length;}
        public void Tube(Vector3 a,Vector3 b,float radius,int m=1,int sides=12) {
            var d=Unit(b-a);var u=Unit(Cross(d,Math.Abs(d.z)<.8f?new Vector3(0,0,1):new Vector3(0,1,0)));var v=Cross(d,u);
            for(int i=0;i<sides;i++){float t=i*Mathf.PI*2/sides,n=((i+1)%sides)*Mathf.PI*2/sides;var r=(u*Mathf.Cos(t)+v*Mathf.Sin(t))*radius;var q=(u*Mathf.Cos(n)+v*Mathf.Sin(n))*radius;
                Quad(a+r,a+q,b+q,b+r,m);Triangle(a,a+q,a+r,m);Triangle(b,b+r,b+q,m);
            }
        }
        public void Pipe(Vector3[] points,float radius,int m=1,int sides=12){for(int i=1;i<points.Length;i++)Tube(points[i-1],points[i],radius,m,sides);if(m==1)for(int i=1;i<points.Length-1;i++)Ball(points[i],radius,m);}
        public void Ball(Vector3 center,float radius,int m=1,int sides=12,int bands=6) {
            Vector3 P(int band,int side){float a=(-.5f+(float)band/bands)*Mathf.PI,b=(side%sides)*Mathf.PI*2/sides;return center+new Vector3(Mathf.Cos(b)*Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,Mathf.Sin(b)*Mathf.Cos(a)*radius);}
            for(int i=0;i<sides;i++){Triangle(center+new Vector3(0,-radius,0),P(1,i),P(1,i+1),m);for(int k=1;k<bands-1;k++)Quad(P(k,i+1),P(k,i),P(k+1,i),P(k+1,i+1),m);Triangle(P(bands-1,i+1),P(bands-1,i),center+new Vector3(0,radius,0),m);}
        }
        public void Arc(Vector3 center,float outer,float inner,float depth,float start,float end,int m=1,int steps=64) {
            Vector3 P(float r,float a,float z)=>center+new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,z);
            float first=start*Mathf.Deg2Rad,last=end*Mathf.Deg2Rad;
            for(int i=0;i<steps;i++){float a=first+(last-first)*i/steps,b=first+(last-first)*(i+1)/steps;
                Quad(P(outer,a,-depth/2),P(inner,a,-depth/2),P(inner,b,-depth/2),P(outer,b,-depth/2),m);
                Quad(P(outer,a,depth/2),P(outer,b,depth/2),P(inner,b,depth/2),P(inner,a,depth/2),m);
                Quad(P(outer,a,-depth/2),P(outer,b,-depth/2),P(outer,b,depth/2),P(outer,a,depth/2),m);
                Quad(P(inner,a,-depth/2),P(inner,a,depth/2),P(inner,b,depth/2),P(inner,b,-depth/2),m);
            }
            Quad(P(outer,first,-depth/2),P(outer,first,depth/2),P(inner,first,depth/2),P(inner,first,-depth/2),m);
            Quad(P(outer,last,-depth/2),P(inner,last,-depth/2),P(inner,last,depth/2),P(outer,last,depth/2),m);
        }
        public void BevelBox(Vector3 center,Vector3 size,float bevel,int m=0) {
            var axes=new[]{new Vector3(1,0,0),new Vector3(0,1,0),new Vector3(0,0,1)};var half=new[]{size.x/2,size.y/2,size.z/2};
            void F(Vector3 a,Vector3 b,Vector3 c){var n=Cross(b-a,c-a);var mid=(a+b+c)/3-center;if(n.x*mid.x+n.y*mid.y+n.z*mid.z<0)Triangle(a,c,b,m);else Triangle(a,b,c,m);}
            void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d){F(a,b,c);F(a,c,d);}
            for(int k=0;k<3;k++)for(int sign=-1;sign<=1;sign+=2){int u=(k+1)%3,v=(k+2)%3;var c=center+axes[k]*(sign*half[k]);Q(c-axes[u]*(half[u]-bevel)-axes[v]*(half[v]-bevel),c+axes[u]*(half[u]-bevel)-axes[v]*(half[v]-bevel),c+axes[u]*(half[u]-bevel)+axes[v]*(half[v]-bevel),c-axes[u]*(half[u]-bevel)+axes[v]*(half[v]-bevel));}
            for(int k=0;k<3;k++){int u=(k+1)%3,v=(k+2)%3;for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2){Vector3 P(int end,bool first)=>center+axes[k]*(end*(half[k]-bevel))+axes[u]*(a*(half[u]-(first?0:bevel)))+axes[v]*(b*(half[v]-(first?bevel:0)));Q(P(-1,true),P(1,true),P(1,false),P(-1,false));}}
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var signs=new[]{x,y,z};Vector3 P(int full){var r=center;for(int k=0;k<3;k++)r+=axes[k]*(signs[k]*(half[k]-(k==full?0:bevel)));return r;}F(P(0),P(1),P(2));}
        }
        public void SpireBack(Vector3 center,float width,float height,float depth,int m=0) {
            var outline=new[]{new Vector3(-width/2,-height/2,0),new Vector3(width/2,-height/2,0),new Vector3(width/2,height*.28f,0),new Vector3(0,height/2,0),new Vector3(-width/2,height*.28f,0)};
            for(int i=0;i<outline.Length;i++){var a=center+outline[i];var b=center+outline[(i+1)%outline.Length];var z=new Vector3(0,0,depth/2);Triangle(center-z,b-z,a-z,m);Triangle(center+z,a+z,b+z,m);Quad(a-z,b-z,b+z,a+z,m);}
        }
        public void Gem(Vector3 center,float width,float height,float depth,bool horizontal=false) {
            // Four offset rings make irregular cut facets. Material 0 is the visible body,
            // 1 the fine polished facet boundaries, 2 restrained luminous surface veins.
            Vector3 P(float x,float y,float z)=>center+(horizontal?new Vector3(y,-x,z):new Vector3(x,y,z));
            const int count=8;var rings=new Vector3[4][];float[] ys={-.36f,-.23f,.21f,.36f},rs={.31f,.5f,.47f,.29f};
            for(int k=0;k<4;k++){rings[k]=new Vector3[count];for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count+(k==2?.08f:0);rings[k][i]=P(Mathf.Cos(a)*width*rs[k],ys[k]*height,Mathf.Sin(a)*depth*rs[k]);}}
            var bottom=P(0,-height/2,0);var top=P(0,height/2,0);
            for(int i=0;i<count;i++){int j=(i+1)%count;Triangle(bottom,rings[0][i],rings[0][j]);Triangle(rings[3][j],rings[3][i],top);Tube(bottom,rings[0][i],.0018f,1,6);Tube(rings[3][i],top,.0018f,1,6);
                for(int k=0;k<3;k++){Quad(rings[k][j],rings[k][i],rings[k+1][i],rings[k+1][j]);Tube(rings[k][i],rings[k+1][i],.0018f,1,6);Tube(rings[k][i],rings[k][j],.0015f,1,6);}
                Tube(rings[3][i],rings[3][j],.0015f,1,6);
            }
            var vein=new[]{P(-width*.08f,-height*.34f,-depth*.47f),P(width*.07f,-height*.12f,-depth*.505f),P(-width*.035f,height*.12f,-depth*.505f),P(width*.05f,height*.32f,-depth*.44f)};Pipe(vein,.003f,2,6);
        }
        public void RotateZ(Vector3 center,int first,float degrees){var rotation=Quaternion.Euler(0,0,degrees);for(int i=first;i<Vertices.Count;i++)Vertices[i]=center+rotation*(Vertices[i]-center);}
        public Mesh Mesh(string name) {
            var mesh=new Mesh{name=name};if(Vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(Vertices);mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(Triangles[i],i);
            mesh.RecalculateNormals();
            var uv=new List<Vector2>();foreach(var v in Vertices)uv.Add(new Vector2(v.x*2.7f+v.z*.9f,v.y*2.7f+v.z*.7f));mesh.SetUVs(0,uv);mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }
}
