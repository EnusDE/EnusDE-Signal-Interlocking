using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

namespace EnusDE.Signaltechnik {
    // Local, reversible replacement of the identified base-map sign renderer only.
    // Does not alter meshes, materials, map data or actual speed limits of the game.
    public sealed class BakedSignDisplay : IDisposable {
        sealed class Binding {public MeshRenderer Original;public GameObject Overlay;public TextMeshPro[] Labels;public double Mph;}
        readonly Dictionary<int,Binding> bindings=new Dictionary<int,Binding>();
        Mesh model;Material dark;double nextScan;
        public int Count=>bindings.Count;
        public void Refresh(bool enabled,string mode) {
            if(!enabled||mode=="original"){Dispose();return;}
            if(Time.realtimeSinceStartup>=nextScan) {
                nextScan=Time.realtimeSinceStartup+30;
                foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                    if(bindings.Count>=128)break;
                    if(filter==null||bindings.ContainsKey(filter.GetInstanceID()))continue;
                    var mesh=filter.sharedMesh;if(mesh==null||mesh.name!="Speed Sign")continue;var renderer=filter.GetComponent<MeshRenderer>();
                    if(mesh==null||renderer==null||!renderer.enabled||renderer.sharedMaterials.Length!=1)continue;
                    var mat=renderer.sharedMaterial;var bounds=mesh.bounds;double mph;
                    if(mat==null||!NativeSignProfile.TrySpeed(mesh.name,mat.name,mesh.vertexCount,mesh.subMeshCount==1?mesh.GetIndexCount(0):0,bounds.size.x,bounds.size.y,bounds.size.z,out mph))continue;
                    EnsureResources();var binding=Create(filter,renderer,mph);bindings.Add(filter.GetInstanceID(),binding);
                }
            }
            var removed=new List<int>();
            foreach(var pair in bindings) {
                var value=pair.Value;
                if(value.Original==null||value.Overlay==null){Restore(value);removed.Add(pair.Key);continue;}
                string caption=SignUnits.Caption(value.Mph,mode);foreach(var label in value.Labels)if(label!=null&&label.text!=caption)label.text=caption;
            }
            foreach(int id in removed)bindings.Remove(id);
        }
        void EnsureResources() {
            if(model!=null)return;
            var shader=ShaderPolicy.Find<Shader>(Shader.Find);if(shader==null)throw new InvalidOperationException("Kein Shader für Kartenschild verfügbar. Originaldarstellung bleibt erhalten.");
            dark=new Material(shader);
            foreach(string key in new[]{"_BaseColor","_Color","_UnlitColor"})if(dark.HasProperty(key))dark.SetColor(key,new Color(.025f,.025f,.025f));
            // Dimensions measured from the base mesh bounds (local FBX units).
            const float r=.002794f,z=.0197982f,d=.00038f;
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int face=0;face<2;face++) {
                int start=vertices.Count;float y=face==0?-d:d;vertices.Add(new Vector3(0,y,z));
                for(int i=0;i<32;i++){float a=i*Mathf.PI/16;vertices.Add(new Vector3(Mathf.Cos(a)*r,y,z+Mathf.Sin(a)*r));}
                for(int i=0;i<32;i++){int a=start+1+i,b=start+1+(i+1)%32;triangles.AddRange(face==0?new[]{start,a,b}:new[]{start,b,a});}
            }
            // Own post; complete original renderer is hidden so its baked digits cannot shine through.
            int p=vertices.Count;float w=.00018f;
            vertices.AddRange(new[]{new Vector3(-w,-d,-.004f),new Vector3(w,-d,-.004f),new Vector3(w,-d,z),new Vector3(-w,-d,z),new Vector3(-w,d,-.004f),new Vector3(w,d,-.004f),new Vector3(w,d,z),new Vector3(-w,d,z)});
            foreach(int i in new[]{0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5})triangles.Add(p+i);
            model=new Mesh{name="EnusDE own numeric round speed sign"};model.SetVertices(vertices);model.SetTriangles(triangles,0);model.RecalculateNormals();model.RecalculateBounds();
        }
        Binding Create(MeshFilter filter,MeshRenderer original,double mph) {
            var root=new GameObject("EnusDE metric base sign");root.layer=2;root.transform.SetParent(filter.transform,false);
            try {
                root.AddComponent<MeshFilter>().sharedMesh=model;var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=dark;renderer.shadowCastingMode=ShadowCastingMode.Off;
                var labels=new TextMeshPro[2];
                for(int i=0;i<2;i++) {
                    var go=new GameObject("EnusDE numeric face");go.layer=2;go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3(0,i==0?-.00040f:.00040f,.0197982f);go.transform.localRotation=Quaternion.LookRotation(i==0?Vector3.up:Vector3.down,Vector3.forward);go.transform.localScale=Vector3.one*.0008f;
                    var text=go.AddComponent<TextMeshPro>();text.font=TMP_Settings.defaultFontAsset;text.color=new Color(.85f,.52f,.12f);text.alignment=TextAlignmentOptions.Center;text.richText=false;text.textWrappingMode=TextWrappingModes.NoWrap;text.enableAutoSizing=true;text.fontSizeMin=12;text.fontSizeMax=36;text.fontSize=36;text.rectTransform.sizeDelta=new Vector2(6,5);labels[i]=text;
                }
                original.enabled=false;return new Binding{Original=original,Overlay=root,Labels=labels,Mph=mph};
            }catch{UnityEngine.Object.Destroy(root);throw;}
        }
        static void Restore(Binding value){if(value.Original!=null)value.Original.enabled=true;if(value.Overlay!=null){value.Overlay.SetActive(false);UnityEngine.Object.Destroy(value.Overlay);}}
        public void Dispose(){foreach(var value in bindings.Values)Restore(value);bindings.Clear();if(model!=null)UnityEngine.Object.Destroy(model);if(dark!=null)UnityEngine.Object.Destroy(dark);model=null;dark=null;nextScan=0;}
    }
}
