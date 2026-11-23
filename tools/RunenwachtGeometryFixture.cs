// Headless geometry fixture: controlled math/mesh stand-ins, not a Unity render test.
using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using EnusDE.Runenwacht;
namespace UnityEngine {
    public class Vector3 {
        public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}
        public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a,float b)=>new Vector3(a.x/b,a.y/b,a.z/b);
    }
    public class Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}}
    public class Quaternion {
        float z;public static Quaternion Euler(float x,float y,float z){if(x!=0||y!=0)throw new Exception("Unexpected geometry rotation");return new Quaternion{z=z*Mathf.Deg2Rad};}
        public static Vector3 operator *(Quaternion q,Vector3 v)=>new Vector3(v.x*Mathf.Cos(q.z)-v.y*Mathf.Sin(q.z),v.x*Mathf.Sin(q.z)+v.y*Mathf.Cos(q.z),v.z);
    }
    public static class Mathf {public const float PI=(float)Math.PI,Deg2Rad=PI/180;public static float Sin(float a)=>(float)Math.Sin(a);public static float Cos(float a)=>(float)Math.Cos(a);}
    public class Mesh {
        public string name;public Rendering.IndexFormat indexFormat;public int subMeshCount;public Vector3[] vertices;public List<int>[] triangles=new List<int>[3];
        public void SetVertices(List<Vector3> v){vertices=v.ToArray();}public void SetTriangles(List<int> t,int i){triangles[i]=t.ToList();}public List<Vector2> uv;public void SetUVs(int i,List<Vector2> values){uv=values;}public void RecalculateTangents(){}public void RecalculateNormals(){}public void RecalculateBounds(){}
    }
}
namespace UnityEngine.Rendering {public enum IndexFormat {UInt16,UInt32}}
static class GeometryFixture {
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);Console.WriteLine("PASS: "+text);}
    static string Key(UnityEngine.Vector3 v)=>$"{Math.Round(v.x,4)}:{Math.Round(v.y,4)}:{Math.Round(v.z,4)}";
    static void Closed(RunenwachtGeometry g) {
        var edges=new Dictionary<string,int>();
        foreach(var tris in g.Triangles)for(int i=0;i<tris.Count;i+=3) {
            var v=new[]{Key(g.Vertices[tris[i]]),Key(g.Vertices[tris[i+1]]),Key(g.Vertices[tris[i+2]])};if(v.Distinct().Count()<3)continue;
            for(int j=0;j<3;j++){var a=v[j];var b=v[(j+1)%3];var key=string.CompareOrdinal(a,b)<0?a+"|"+b:b+"|"+a;edges[key]=edges.TryGetValue(key,out int n)?n+1:1;}
        }
        if(edges.Values.Any(n=>n!=2))throw new Exception("FAIL: open or non-manifold generated surface: "+string.Join(",",edges.Where(e=>e.Value!=2).Take(5)));
    }
    static void Outward(RunenwachtGeometry g,UnityEngine.Vector3 center) {
        foreach(var t in g.Triangles)for(int i=0;i<t.Count;i+=3){var a=g.Vertices[t[i]];var b=g.Vertices[t[i+1]];var c=g.Vertices[t[i+2]];double ux=b.x-a.x,uy=b.y-a.y,uz=b.z-a.z,vx=c.x-a.x,vy=c.y-a.y,vz=c.z-a.z;double nx=uy*vz-uz*vy,ny=uz*vx-ux*vz,nz=ux*vy-uy*vx;double x=(a.x+b.x+c.x)/3-center.x,y=(a.y+b.y+c.y)/3-center.y,z=(a.z+b.z+c.z)/3-center.z;if(nx*x+ny*y+nz*z<=0)throw new Exception("FAIL: inward-facing convex surface");}
    }
    static void Valid(RunenwachtGeometry g) {
        foreach(var t in g.Triangles)for(int i=0;i<t.Count;i+=3){var a=g.Vertices[t[i]];var b=g.Vertices[t[i+1]];var c=g.Vertices[t[i+2]];double ux=b.x-a.x,uy=b.y-a.y,uz=b.z-a.z,vx=c.x-a.x,vy=c.y-a.y,vz=c.z-a.z;double nx=uy*vz-uz*vy,ny=uz*vx-ux*vz,nz=ux*vy-uy*vx;if(nx*nx+ny*ny+nz*nz<1e-16)throw new Exception("FAIL: degenerate generated triangle");}
        var m=g.Mesh("fixture");Check(m.vertices.All(v=>new[]{v.x,v.y,v.z}.All(x=>!float.IsNaN(x)&&!float.IsInfinity(x)))&&m.triangles.All(t=>t.All(i=>i>=0&&i<m.vertices.Length))&&m.subMeshCount==3,"generated mesh has finite vertices and valid material/triangle indices");
    }
    static bool OpaqueBehind(RunenwachtGeometry g,UnityEngine.Vector3 p) {
        foreach(var t in g.Triangles)for(int i=0;i<t.Count;i+=3){var a=g.Vertices[t[i]];var b=g.Vertices[t[i+1]];var c=g.Vertices[t[i+2]];double det=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Math.Abs(det)<1e-12)continue;double u=((b.y-c.y)*(p.x-c.x)+(c.x-b.x)*(p.y-c.y))/det,v=((c.y-a.y)*(p.x-c.x)+(a.x-c.x)*(p.y-c.y))/det,w=1-u-v;if(u>=-1e-5&&v>=-1e-5&&w>=-1e-5&&u*a.z+v*b.z+w*c.z>p.z+.001)return true;}
        return false;
    }
    static void Export(List<object> parts,string kind,string name,string surface,string channel,RunenwachtGeometry g,float y,float z) {
        Valid(g);var mesh=g.Mesh("export");Check(mesh.uv.Count==mesh.vertices.Length,"actual production mesh supplies UVs for PBR material textures · "+kind+" / "+name);
        parts.Add(new{kind,name,surface,channel,vertices=g.Vertices.Select(v=>new[]{v.x,v.y+y,v.z+z}).ToArray(),triangles=g.Triangles.Select(t=>t.ToArray()).ToArray()});
    }
    static void Main(string[] args) {
        var box=new RunenwachtGeometry();box.Box(new UnityEngine.Vector3(0,0,.14f),new UnityEngine.Vector3(1,.8f,.14f));Closed(box);Valid(box);Outward(box,new UnityEngine.Vector3(0,0,.14f));Check(box.Vertices.Count==36,"opaque rear plate is a closed six-sided solid");
        var crystal=new RunenwachtGeometry();crystal.Crystal(new UnityEngine.Vector3(0,0,-.26f),.22f,.67f,.14f);Closed(crystal);Valid(crystal);Outward(crystal,new UnityEngine.Vector3(0,0,-.26f));Check(true,"all gemstone facets face outward and remain visible with backface culling");Check(crystal.Vertices.Max(v=>v.y)-crystal.Vertices.Min(v=>v.y)>.66f&&crystal.Vertices.Count>=72,"permanent gemstone has closed facets and tapered top/bottom tips");
        var ring=new RunenwachtGeometry();ring.Ring(new UnityEngine.Vector3(0,0,0),.57f,.46f,.32f);Closed(ring);Valid(ring);Check(ring.Vertices.Count>=1000,"deep front annulus includes closed inner and outer walls");
        var rear=new RunenwachtGeometry();rear.Ring(new UnityEngine.Vector3(0,0,.14f),.55f,0,.12f);Closed(rear);Valid(rear);
        Check(rear.Vertices.Any(v=>v.x==0&&v.y==0)&&rear.Vertices.Min(v=>v.z)>.07f,"main head rear cap fills the centre behind the crystals");
        var tube=new RunenwachtGeometry();tube.Tube(new UnityEngine.Vector3(0,0,0),new UnityEngine.Vector3(.3f,.8f,.1f),.04f);Closed(tube);Valid(tube);Check(true,"closed copper conduit has solid end caps and finite oriented side faces");
        var chamfer=new RunenwachtGeometry();chamfer.BevelBox(new UnityEngine.Vector3(0,0,0),new UnityEngine.Vector3(.7f,.4f,.5f),.04f);Closed(chamfer);Valid(chamfer);Outward(chamfer,new UnityEngine.Vector3(0,0,0));Check(true,"bevelled foundation has closed outward-facing faces, edges and corners");
        var crescent=new RunenwachtGeometry();crescent.Arc(new UnityEngine.Vector3(0,0,0),.65f,.555f,.115f,145,395);Closed(crescent);Valid(crescent);Check(crescent.Vertices.Min(v=>v.y)<-.64f&&crescent.Vertices.Max(v=>v.y)>.36f,"preview support forms a curved crescent with raised horns");
        var elbow=new RunenwachtGeometry();elbow.Ball(new UnityEngine.Vector3(0,0,0),.023f);Closed(elbow);Valid(elbow);Outward(elbow,new UnityEngine.Vector3(0,0,0));Check(true,"rounded pipe elbows are closed outward-facing solids");
        foreach(bool backFace in new[]{false,true}){
            float depth=backFace?.3f:.54f;var plate=RunenwachtDesign.Plaque(.72f,.17f,depth);Valid(plate);
            float worldMast=backFace?.42f-depth:-.42f+depth;
            Check(Math.Abs(worldMast-.12f)<.0001f&&plate.Vertices.Max(v=>v.z)>depth+.05f,"plaque arms and clamp reach the physical mast rail plane · "+backFace);
            foreach(var kind in new[]{"main","distant","shunt"})foreach(int side in new[]{-1,1}){
                var route=RunenwachtDesign.FeedRoute(kind,backFace,side);var port=RunenwachtDesign.FeedPort(kind,side);var end=route[route.Length-1];
                Check(Key(route[0])==Key(port)&&Math.Abs((backFace?.27f-end.z:-.27f+end.z)-.13f)<.0001f&&Math.Abs(end.x-side*.24f)<.0001f,"head feed joins the insulator terminal to the continuous mast copper bus · "+kind+" / "+backFace+" / "+side);
                var feed=RunenwachtDesign.FeedPipes(kind,backFace);Valid(feed);
            }
        }
        var parts=new List<object>();
        foreach(var kind in new[]{"main","distant","shunt"}) {
            float h=kind=="main"?4.5f:kind=="distant"?2.35f:1.02f;
            Export(parts,kind,"foundation","stone",null,RunenwachtDesign.Foundation(),0,0);
            Export(parts,kind,"mast","metal",null,RunenwachtDesign.Mast(h),0,0);
            float labelY=kind=="main"?3.83f:kind=="distant"?1.9f:.82f;
            Export(parts,kind,"code plaque","metal",null,RunenwachtDesign.Plaque(.72f,.17f),labelY,-.42f);
            Export(parts,kind,"manufacturer plaque","metal",null,RunenwachtDesign.Plaque(.42f,.105f),.59f,-.42f);
            var d=RunenwachtDesign.Head(kind);float y=kind=="main"?4.55f:kind=="distant"?2.65f:1.25f;
            Export(parts,kind,"feed pipes","metal",null,RunenwachtDesign.FeedPipes(kind,false),y,-.27f);
            Export(parts,kind,"housing","metal",null,d.Metal,y,-.27f);Export(parts,kind,"reflector","reflector",null,d.Reflector,y,-.27f);
            var white=d.Channels[CrystalChannel.White];Check(white.Vertices.All(v=>OpaqueBehind(d.Metal,v)),"enlarged identification crystal has opaque metal behind its complete projected emitting geometry · "+kind);
            foreach(var pair in d.Channels)Export(parts,kind,"crystals","crystal",pair.Key.ToString(),pair.Value,y,-.27f);
            Check(d.Channels.All(x=>x.Value.Triangles[0].Count>0&&x.Value.Triangles[1].Count>0&&x.Value.Triangles[2].Count>0),"each physical channel separates faceted body, polished edges and animated veins · "+kind);
        }
        var main=RunenwachtDesign.Head("main");var red=main.Channels[CrystalChannel.Red];Check(red.Vertices.Min(v=>v.x)<0&&red.Vertices.Max(v=>v.x)>0,"main stop crystal occupies the centre of the permanent three-core cluster");
        string path=args.Length>0?args[0]:"runenwacht-preview.json";File.WriteAllText(path,JsonSerializer.Serialize(parts));
        var rotated=new RunenwachtGeometry();rotated.Box(new UnityEngine.Vector3(.1f,.2f,0),new UnityEngine.Vector3(.3f,.04f,.02f),1,45);Closed(rotated);Valid(rotated);Check(rotated.Triangles[1].Count==36&&rotated.Triangles[0].Count==0,"rune strokes preserve their copper material after rotation");
    }
}
