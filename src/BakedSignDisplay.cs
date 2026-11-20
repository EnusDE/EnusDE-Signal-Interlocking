using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // A private texture/material copy changes only recognized numerals. Native mesh, paint, shader and game data remain intact.
    public sealed class BakedSignDisplay : IDisposable {
        sealed class Prepared {public Material Source,Material;public Texture2D Texture;public int Faces;}
        sealed class Binding {public MeshRenderer Renderer;public Material Original;public Prepared Applied;}
        readonly Dictionary<int,Binding> bindings=new Dictionary<int,Binding>();
        readonly Dictionary<string,Prepared> prepared=new Dictionary<string,Prepared>();
        readonly Dictionary<string,double> failed=new Dictionary<string,double>();
        SignDigitFont[] fonts;double nextScan;string activeMode;int allocatedPixels;
        public int Count=>bindings.Count;
        public string Error {get;private set;}
        public int ConvertedFaces=>bindings.Values.Sum(b=>b.Applied.Faces);
        public int UnchangedFaces=>bindings.Values.Sum(b=>2-b.Applied.Faces);
        public void Refresh(bool enabled,string mode) {
            if(!enabled||mode=="original"){Dispose();return;}
            mode="metric"; // Mixed settings also display the single large world number in km/h.
            if(activeMode!=mode){Dispose();activeMode=mode;}
            var removed=new List<int>();
            foreach(var pair in bindings)if(pair.Value.Renderer==null||pair.Value.Renderer.sharedMaterial!=pair.Value.Applied.Material){Restore(pair.Value);removed.Add(pair.Key);}
            foreach(int id in removed)bindings.Remove(id);
            if(Time.realtimeSinceStartup<nextScan)return;nextScan=Time.realtimeSinceStartup+30;
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                if(bindings.Count>=128)break;
                if(filter==null||bindings.ContainsKey(filter.GetInstanceID()))continue;
                var mesh=filter.sharedMesh;if(mesh==null||mesh.name!="Speed Sign")continue;
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null||!renderer.enabled||renderer.sharedMaterials.Length!=1)continue;
                var source=renderer.sharedMaterial;if(source==null)continue;var bounds=mesh.bounds;
                if(!NativeSignProfile.IsKnownGeometry(mesh.name,source.name,mesh.vertexCount,mesh.subMeshCount==1?mesh.GetIndexCount(0):0,bounds.size.x,bounds.size.y,bounds.size.z))continue;
                string key=source.GetInstanceID()+":"+mesh.GetInstanceID();if(failed.TryGetValue(key,out double retry)&&Time.realtimeSinceStartup<retry)continue;
                try {
                    if(!prepared.TryGetValue(key,out var value)){value=Prepare(mesh,source,mode);prepared[key]=value;}
                    renderer.sharedMaterial=value.Material;bindings[filter.GetInstanceID()]=new Binding{Renderer=renderer,Original=source,Applied=value};
                } catch(Exception e){failed[key]=Time.realtimeSinceStartup+60;Error="Kartenschild nicht eindeutig umgerechnet: "+e.Message;}
            }
        }
        Prepared Prepare(Mesh mesh,Material source,string mode) {
            if(prepared.Count>=8)throw new InvalidOperationException("Grenze für eigene Schildtexturen erreicht.");
            string property=null;Texture2D texture=null;
            foreach(string name in new[]{"_BaseColorMap","_BaseMap","_MainTex","_UnlitColorMap"})if(source.HasProperty(name)&&source.GetTexture(name) is Texture2D candidate&&candidate.width>=32){property=name;texture=candidate;break;}
            if(texture==null)throw new InvalidOperationException("Originale Schildfarbtextur nicht verfügbar.");
            int pixels=checked(texture.width*texture.height);if((long)allocatedPixels+pixels>NativeSignTexture.MaximumPixels)throw new InvalidOperationException("Grenze für eigene Schildtexturen erreicht.");
            var atlas=NativeSignTexture.Read(texture);var vertices=NativeSignTexture.Vertices(mesh,source.GetTextureScale(property),source.GetTextureOffset(property),out var indices);
            var surfaces=NativeSignFace.Extract(vertices,indices);var writes=new Dictionary<int,Color32>();int faces=0;const int size=256;
            if(fonts==null){SignDigitFont native;try{native=NativeSignTexture.Font();}catch{native=SignDigitFont.BuiltIn();}fonts=new[]{native,SignDigitFont.BuiltIn()};}
            foreach(var surface in surfaces) {
                var image=surface.Sample(atlas,texture.width,texture.height,size,texture.wrapMode==TextureWrapMode.Repeat);
                if(!SignFaceConversion.TryRead(image,size,fonts,out var reading))continue;
                var painted=SignFaceConversion.Convert(image,size,reading,mode,out var changed);
                surface.Paint(painted,changed,size,texture.width,texture.height,texture.wrapMode==TextureWrapMode.Repeat,writes);faces++;
            }
            if(faces==0||writes.Count==0)throw new InvalidOperationException("Keine sicher lesbare Geschwindigkeitszahl auf den Schildseiten.");
            foreach(var pair in writes){var color=pair.Value;color.a=atlas[pair.Key].a;atlas[pair.Key]=color;}
            Texture2D copy=null;Material material=null;
            try {
                copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,texture.mipmapCount>1,!texture.isDataSRGB){name="EnusDE metric sign texture",filterMode=texture.filterMode,wrapMode=texture.wrapMode,anisoLevel=texture.anisoLevel};
                copy.SetPixels32(atlas);copy.Apply(texture.mipmapCount>1,true);
                material=new Material(source){name="EnusDE metric sign material"};material.SetTexture(property,copy);
                allocatedPixels+=pixels;return new Prepared{Source=source,Material=material,Texture=copy,Faces=faces};
            } catch {if(copy!=null)UnityEngine.Object.Destroy(copy);if(material!=null)UnityEngine.Object.Destroy(material);throw;}
        }
        static void Restore(Binding binding){if(binding.Renderer!=null&&binding.Renderer.sharedMaterial==binding.Applied.Material)binding.Renderer.sharedMaterial=binding.Original;}
        public void Dispose() {
            foreach(var value in bindings.Values)Restore(value);bindings.Clear();
            foreach(var value in prepared.Values){if(value.Material!=null)UnityEngine.Object.Destroy(value.Material);if(value.Texture!=null)UnityEngine.Object.Destroy(value.Texture);}
            prepared.Clear();failed.Clear();fonts=null;allocatedPixels=0;nextScan=0;activeMode=null;Error=null;
        }
    }
}
