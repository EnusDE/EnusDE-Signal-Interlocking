using System;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerMeshData {
        public static MarkerMeshData CreateConstructionSite() {
            var data=new MarkerMeshData();
            foreach(float x in new[]{-.65f,.65f})foreach(float z in new[]{-.35f,.35f}) {
                data.Box(x-.16f,0,z-.16f,x+.16f,.045f,z+.16f,2);
                for(int band=0;band<3;band++) {
                    float bottom=.045f+band*.14f,top=bottom+.14f;
                    float r0=.115f*(1-band/3f),r1=.115f*(1-(band+1)/3f);
                    for(int side=0;side<8;side++) {
                        double a=side*Math.PI/4,b=(side+1)*Math.PI/4;
                        int index=data.Vertices.Count;
                        data.Vertices.Add(new[]{x+r0*(float)Math.Cos(a),bottom,z+r0*(float)Math.Sin(a)});
                        data.Vertices.Add(new[]{x+r0*(float)Math.Cos(b),bottom,z+r0*(float)Math.Sin(b)});
                        data.Vertices.Add(new[]{x+r1*(float)Math.Cos(b),top,z+r1*(float)Math.Sin(b)});
                        data.Vertices.Add(new[]{x+r1*(float)Math.Cos(a),top,z+r1*(float)Math.Sin(a)});
                        data.Triangles[band==1?0:1].AddRange(band==2?new[]{index,index+2,index+1}:new[]{index,index+2,index+1,index,index+3,index+2});
                    }
                }
            }
            foreach(float x in new[]{-.65f,.65f})data.Box(x-.025f,.05f,.3f,x+.025f,1.05f,.35f,2);
            for(int stripe=0;stripe<8;stripe++)data.Box(-.8f+stripe*.2f,.67f,.28f,-.6f+stripe*.2f,.9f,.37f,stripe%2);
            return data;
        }
    }
}
