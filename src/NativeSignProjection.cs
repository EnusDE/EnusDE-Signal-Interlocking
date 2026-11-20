using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public readonly struct SignTextureVertex {
        public readonly double X,Y,Z,U,V;
        public SignTextureVertex(double x,double y,double z,double u,double v){X=x;Y=y;Z=z;U=u;V=v;}
    }
    public sealed class NativeSignFace {
        public readonly int Side;
        readonly SignTextureVertex[][] triangles;
        const double Radius=.002794,Center=.0197982;
        NativeSignFace(int side,SignTextureVertex[][] triangles){Side=side;this.triangles=triangles;}
        public static NativeSignFace[] Extract(SignTextureVertex[] vertices,int[] indices) {
            var front=new List<SignTextureVertex[]>();var rear=new List<SignTextureVertex[]>();var planes=new List<SignTextureVertex[]>();
            for(int i=0;i+2<indices.Length;i+=3) {
                var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];
                if(Math.Abs(a.Y-b.Y)>.000003||Math.Abs(a.Y-c.Y)>.000003)continue;
                bool OnHead(SignTextureVertex p)=>Math.Abs(p.X)<=Radius*1.01&&Math.Abs(p.Z-Center)<=Radius*1.01;
                if(!OnHead(a)||!OnHead(b)||!OnHead(c))continue;
                if(Math.Abs((b.X-a.X)*(c.Z-a.Z)-(b.Z-a.Z)*(c.X-a.X))<1e-12)continue;
                planes.Add(new[]{a,b,c});
            }
            if(planes.Count>0) {
                double low=double.PositiveInfinity,high=double.NegativeInfinity;
                foreach(var tri in planes){low=Math.Min(low,tri[0].Y);high=Math.Max(high,tri[0].Y);}
                if(high-low>.00003)foreach(var tri in planes)(tri[0].Y<(low+high)/2?front:rear).Add(tri);
            }
            return new[]{new NativeSignFace(-1,front.ToArray()),new NativeSignFace(1,rear.ToArray())};
        }
        static bool Bary(double x,double y,double ax,double ay,double bx,double by,double cx,double cy,out double a,out double b,out double c) {
            double d=(by-cy)*(ax-cx)+(cx-bx)*(ay-cy);a=b=c=0;if(Math.Abs(d)<1e-16)return false;
            a=((by-cy)*(x-cx)+(cx-bx)*(y-cy))/d;b=((cy-ay)*(x-cx)+(ax-cx)*(y-cy))/d;c=1-a-b;
            return a>=-1e-6&&b>=-1e-6&&c>=-1e-6;
        }
        static int Clamp(int x,int size)=>Math.Max(0,Math.Min(size-1,x));
        static double Wrap(double value,bool repeat)=>repeat?value-Math.Floor(value):Math.Max(0,Math.Min(1,value));
        public Color32[] Sample(Color32[] atlas,int width,int height,int size,bool repeat) {
            var result=new Color32[size*size];var depth=new double[size*size];for(int i=0;i<depth.Length;i++)depth[i]=double.NegativeInfinity;
            foreach(var tri in triangles) {
                var p=tri[0];var q=tri[1];var r=tri[2];
                double PX(SignTextureVertex v)=>(1+v.X/Radius*(Side<0?1:-1))*size/2-.5;
                double PY(SignTextureVertex v)=>(1+(v.Z-Center)/Radius)*size/2-.5;
                double ax=PX(p),ay=PY(p),bx=PX(q),by=PY(q),cx=PX(r),cy=PY(r);
                int l=Clamp((int)Math.Floor(Math.Min(ax,Math.Min(bx,cx))),size),right=Clamp((int)Math.Ceiling(Math.Max(ax,Math.Max(bx,cx))),size);
                int bottom=Clamp((int)Math.Floor(Math.Min(ay,Math.Min(by,cy))),size),top=Clamp((int)Math.Ceiling(Math.Max(ay,Math.Max(by,cy))),size);
                for(int y=bottom;y<=top;y++)for(int x=l;x<=right;x++)if(Bary(x,y,ax,ay,bx,by,cx,cy,out double a,out double b,out double c)) {
                    int at=y*size+x;double z=Side*(p.Y*a+q.Y*b+r.Y*c);if(z<depth[at])continue;
                    double u=Wrap(p.U*a+q.U*b+r.U*c,repeat),v=Wrap(p.V*a+q.V*b+r.V*c,repeat);
                    result[at]=atlas[Clamp((int)(v*height),height)*width+Clamp((int)(u*width),width)];depth[at]=z;
                }
            }
            return result;
        }
        public void Paint(Color32[] painted,bool[] changed,int size,int width,int height,bool repeat,Dictionary<int,Color32> writes) {
            foreach(var tri in triangles) {
                var p=tri[0];var q=tri[1];var r=tri[2];
                // Reject wrapping atlas islands; an unknown layout must not repaint a different object.
                if(p.U<0||p.U>1||q.U<0||q.U>1||r.U<0||r.U>1||p.V<0||p.V>1||q.V<0||q.V>1||r.V<0||r.V>1)throw new InvalidOperationException("Gekachelte Schild-UVs nicht eindeutig.");
                double ax=p.U*width-.5,ay=p.V*height-.5,bx=q.U*width-.5,by=q.V*height-.5,cx=r.U*width-.5,cy=r.V*height-.5;
                int l=Clamp((int)Math.Floor(Math.Min(ax,Math.Min(bx,cx))),width),right=Clamp((int)Math.Ceiling(Math.Max(ax,Math.Max(bx,cx))),width);
                int bottom=Clamp((int)Math.Floor(Math.Min(ay,Math.Min(by,cy))),height),top=Clamp((int)Math.Ceiling(Math.Max(ay,Math.Max(by,cy))),height);
                for(int y=bottom;y<=top;y++)for(int x=l;x<=right;x++)if(Bary(x,y,ax,ay,bx,by,cx,cy,out double a,out double b,out double c)) {
                    double px=(p.X*a+q.X*b+r.X*c)/Radius*(Side<0?1:-1),py=(p.Z*a+q.Z*b+r.Z*c-Center)/Radius;
                    int sx=Clamp((int)((px+1)*size/2),size),sy=Clamp((int)((py+1)*size/2),size),at=sy*size+sx;if(!changed[at])continue;
                    int index=y*width+x;var color=painted[at];
                    if(writes.TryGetValue(index,out var old)&&Math.Abs(old.r-color.r)+Math.Abs(old.g-color.g)+Math.Abs(old.b-color.b)>60)
                        throw new InvalidOperationException("Schildseiten teilen widersprüchliche Texturflächen.");
                    writes[index]=color;
                }
            }
        }
    }
}
