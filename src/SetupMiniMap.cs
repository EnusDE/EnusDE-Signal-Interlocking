using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    // Track-centre coordinates only. Mast lateral offsets never enter the map projection.
    public sealed class SetupMiniMap : MaskableGraphic {
        public readonly List<Vector2[]> Rails=new List<Vector2[]>();
        public Vector2 Center;
        public float Radius=400;
        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();var rect=rectTransform.rect;float scale=Math.Min(rect.width,rect.height)/(Radius*2);
            Vector2 Project(Vector2 at)=>rect.center+(at-Center)*scale;
            void Line(Vector2 a,Vector2 b,Color tint,float width) {
                var delta=b-a;if(delta.sqrMagnitude<.0001)return;var normal=new Vector2(-delta.y,delta.x).normalized*width/2;int at=vh.currentVertCount;
                vh.AddVert(a-normal,tint,Vector2.zero);vh.AddVert(a+normal,tint,Vector2.zero);vh.AddVert(b+normal,tint,Vector2.zero);vh.AddVert(b-normal,tint,Vector2.zero);vh.AddTriangle(at,at+1,at+2);vh.AddTriangle(at,at+2,at+3);
            }
            foreach(var rail in Rails)for(int i=1;i<rail.Length;i++)Line(Project(rail[i-1]),Project(rail[i]),new Color(.45f,.72f,.75f),2);
            var center=Project(Center);Line(center+new Vector2(-6,0),center+new Vector2(6,0),Color.red,3);Line(center+new Vector2(0,-6),center+new Vector2(0,6),Color.red,3);
        }
    }
}
