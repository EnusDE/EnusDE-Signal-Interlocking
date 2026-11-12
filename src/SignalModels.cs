using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace EnusDE.Signaltechnik {
    public sealed class MastHeads : MonoBehaviour {public FaceHeads Front,Back;public bool Preview;}
    public sealed class FaceHeads {public GameObject Main,Shunt,Distant;}
    public sealed class SignalLamps : MonoBehaviour {
        public MeshRenderer[] Lamps;
        public MeshRenderer[] Halos;
        public SignalLensColor[] Colors;
        public bool Preview;
        public bool HaloVisible;
        public LocalSignalAspect LastAspect;
        public int LastGlowRevision=-1;
        public SignalNameplate Nameplate;
        public string Last;
    }
    // Original low-poly H/V-inspired demonstration models, not railway-certified replicas.
    public sealed partial class SignalModels : IDisposable {
        Mesh main,small,distant,lens,mast,mainHead,smallHead,distantHead;
        Material frame,post,red,green,yellow,white,off,blue;
        readonly List<Material> materials=new List<Material>();
        Material ColorOf(Shader shader,Color color){var value=new Material(shader);foreach(string property in new[]{"_BaseColor","_Color","_UnlitColor"})if(value.HasProperty(property))value.SetColor(property,color);materials.Add(value);return value;}
        static Mesh MeshOf(MarkerMeshData data,string name) {
            var vertices=new Vector3[data.Vertices.Count];for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(data.Vertices[i][0],data.Vertices[i][1],data.Vertices[i][2]);
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(data.Triangles[i],i);mesh.RecalculateBounds();mesh.RecalculateNormals();return mesh;
        }
        void Ensure() {
            if(main!=null)return;
            Shader shader=ShaderPolicy.Find<Shader>(Shader.Find);
            if(shader==null)throw new InvalidOperationException("Kein Signalmodell-Shader verfügbar.");
            frame=ColorOf(shader,new Color(.035f,.035f,.035f));post=ColorOf(shader,new Color(.45f,.48f,.45f));red=ColorOf(shader,Color.red);green=ColorOf(shader,Color.green);yellow=ColorOf(shader,Color.yellow);white=ColorOf(shader,Color.white);off=ColorOf(shader,new Color(.08f,.08f,.08f));blue=ColorOf(shader,Color.cyan);
            try{BuildGlow(shader);}catch(Exception e){GlowStatus="Signalleuchten: optionale Darstellung nicht verfügbar: "+e.Message;}
            main=MeshOf(MarkerMeshData.CreateSignal(false,false),"EnusDE original main signal");small=MeshOf(MarkerMeshData.CreateSignal(true,false),"EnusDE original shunt signal");distant=MeshOf(MarkerMeshData.CreateSignal(false,true),"EnusDE original distant signal");
            mast=MeshOf(MarkerMeshData.CreateMast(),"EnusDE common signal mast");mainHead=MeshOf(MarkerMeshData.CreateSignal(false,false,true),"EnusDE main head");smallHead=MeshOf(MarkerMeshData.CreateSignal(true,false,true),"EnusDE shunt head");distantHead=MeshOf(MarkerMeshData.CreateSignal(false,true,true),"EnusDE distant head");
            var v=new List<Vector3>{Vector3.zero};var t=new List<int>();for(int i=0;i<16;i++){float angle=i*Mathf.PI/8;v.Add(new Vector3(Mathf.Cos(angle)*.10f,Mathf.Sin(angle)*.10f,0));}
            for(int i=0;i<16;i++)t.AddRange(new[]{0,1+(i+1)%16,1+i});lens=new Mesh{name="EnusDE signal lens"};lens.vertices=v.ToArray();lens.triangles=t.ToArray();lens.RecalculateNormals();lens.RecalculateBounds();
        }
        public void Build(GameObject root,string kind,bool ghost,bool headOnly=false,bool label=true) {
            Ensure();root.AddComponent<MeshFilter>().sharedMesh=kind==MarkerRecord.Main?(headOnly?mainHead:main):kind==MarkerRecord.Shunt?(headOnly?smallHead:small):(headOnly?distantHead:distant);
            var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{ghost?blue:frame,post,frame};renderer.shadowCastingMode=ShadowCastingMode.Off;
            Vector2[] positions=kind==MarkerRecord.Main?new[]{new Vector2(0,3.78f),new Vector2(0,3.45f),new Vector2(0,3.12f)}:kind==MarkerRecord.Shunt?new[]{new Vector2(-.14f,.85f),new Vector2(.14f,.85f),new Vector2(-.14f,.7f),new Vector2(.14f,1.0f)}:new[]{new Vector2(-.17f,2.95f),new Vector2(.17f,3.25f),new Vector2(-.17f,3.25f),new Vector2(.17f,2.95f)};
            var state=root.AddComponent<SignalLamps>();state.Lamps=new MeshRenderer[positions.Length];state.Halos=new MeshRenderer[positions.Length];state.Preview=ghost;
            for(int i=0;i<positions.Length;i++){var part=new GameObject("EnusDE signal lamp "+i);part.layer=2;part.transform.SetParent(root.transform,false);part.transform.localPosition=new Vector3(positions[i].x,positions[i].y,-.07f);part.AddComponent<MeshFilter>().sharedMesh=lens;var light=part.AddComponent<MeshRenderer>();light.sharedMaterial=off;light.shadowCastingMode=ShadowCastingMode.Off;state.Lamps[i]=light;}
            if(glareMesh!=null&&!ghost)for(int i=0;i<positions.Length;i++){var part=new GameObject("EnusDE signal glow "+i);part.layer=2;part.transform.SetParent(root.transform,false);part.transform.localPosition=new Vector3(positions[i].x,positions[i].y,-.076f);part.AddComponent<MeshFilter>().sharedMesh=glareMesh;var halo=part.AddComponent<MeshRenderer>();halo.sharedMaterial=off;halo.shadowCastingMode=ShadowCastingMode.Off;halo.receiveShadows=false;halo.enabled=false;state.Halos[i]=halo;}
            if(label&&!ghost)BuildNameplate(state,root.transform,kind==MarkerRecord.Shunt?.43f:headOnly?2.72f:2.5f);
        }
        public void BuildMast(GameObject root,bool ghost) {
            Ensure();root.AddComponent<MeshFilter>().sharedMesh=mast;var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{ghost?blue:frame,ghost?blue:post,frame};renderer.shadowCastingMode=ShadowCastingMode.Off;root.AddComponent<MastHeads>().Preview=ghost;
        }
        GameObject Head(GameObject parent,string kind,float y,bool label,bool ghost) {
            var root=new GameObject("EnusDE attached "+kind);root.layer=2;root.transform.SetParent(parent.transform,false);root.transform.localPosition=new Vector3(0,y,0);Build(root,kind,ghost,true,label);
            return root;
        }
        FaceHeads Face(GameObject root,SignalFace face,bool back,bool ghost) {
            var parent=new GameObject("EnusDE mast direction "+(back?"B":"A"));parent.layer=2;parent.transform.SetParent(root.transform,false);parent.transform.localPosition=new Vector3(0,0,back?.14f:-.14f);parent.transform.localRotation=Quaternion.Euler(0,back?180:0,0);
            return new FaceHeads{Main=Head(parent,MarkerRecord.Main,MarkerMeshData.MainLift,true,ghost),Shunt=face.Sh1?Head(parent,MarkerRecord.Shunt,1.5f,false,ghost):null,Distant=face.DistantHead?Head(parent,MarkerRecord.Distant,MarkerMeshData.MainLift-1.4f,false,ghost):null};
        }
        public void ShowMast(GameObject root,MarkerRecord row) {
            var state=root.GetComponent<MastHeads>();if(state==null)return;var config=MastConfiguration.Effective(row);
            root.transform.rotation=root.transform.rotation*Quaternion.Euler(0,row.FacingReverse?180:0,0);
            if(config.Front.Enabled&&state.Front==null)state.Front=Face(root,config.Front,false,state.Preview);
            if(config.Back.Enabled&&state.Back==null)state.Back=Face(root,config.Back,true,state.Preview);
            ShowFace(state.Front,config.Front,row.Name,row.LabelScale);ShowFace(state.Back,config.Back,row.Name,row.LabelScale);
        }
        void ShowFace(FaceHeads heads,SignalFace face,string mastName,double? labelScale) {
            if(heads==null)return;
            var aspect=Entry.SignalAspect(face.Id);var record=new MarkerRecord{Name=face.Code,MastName=mastName,DisplayName=face.DisplayName,LabelScale=labelScale,Kind=MarkerRecord.Main};
            var plan=SignalLampPlan.For(aspect,Entry.SignalAspect(face.DistantTarget));
            // A shunting permission keeps Hp 0 at the main head and lights only the attached Sh 1 head.
            Show(heads.Main,record,plan.Main);
            if(heads.Shunt!=null)Show(heads.Shunt,new MarkerRecord{Name=face.Code,Kind=MarkerRecord.Shunt},plan.Shunt);
            if(heads.Distant!=null)Show(heads.Distant,new MarkerRecord{Name=face.Code,Kind=MarkerRecord.Distant},plan.Distant);
        }
        public void Show(GameObject root,MarkerRecord record,LocalSignalAspect aspect) {
            var state=root.GetComponent<SignalLamps>();if(state==null)return;
            root.transform.rotation=root.transform.rotation*Quaternion.Euler(0,record.FacingReverse?180:0,0);
            if(state.Nameplate!=null) {
                var size=(float)(record.LabelScale??1);
                if(state.Nameplate.transform.localScale.x!=size)state.Nameplate.transform.localScale=Vector3.one*size;
                string caption=record.DisplayTitle;
                if(state.Last!=caption){state.Last=caption;UpdateNameplate(state.Nameplate,caption);}
            }
            bool changed=state.Colors==null||state.LastAspect!=aspect||state.LastGlowRevision!=glowRevision;
            if(changed){state.LastAspect=aspect;state.LastGlowRevision=glowRevision;state.Colors=SignalLensPlan.For(record.Kind,aspect);for(int i=0;i<state.Lamps.Length;i++){var color=state.Colors[i];state.Lamps[i].sharedMaterial=LensMaterial(color,!state.Preview);if(state.Halos[i]!=null)state.Halos[i].sharedMaterial=HaloMaterial(color);}}
            var towardViewer=viewKnown?viewPosition-root.transform.position:Vector3.zero;bool haloVisible=SignalGlowOptions.ShowHalo(glowEnabled,haloEnabled,state.Preview,viewKnown,towardViewer.sqrMagnitude,Vector3.Dot(towardViewer,-root.transform.forward));if(changed||state.HaloVisible!=haloVisible){state.HaloVisible=haloVisible;for(int i=0;i<state.Halos.Length;i++)if(state.Halos[i]!=null)state.Halos[i].enabled=haloVisible&&state.Colors[i]!=SignalLensColor.Off&&HaloMaterial(state.Colors[i])!=off;}
        }
        public void Dispose(){DisposeNameplates();foreach(var mesh in new[]{main,small,distant,lens,mast,mainHead,smallHead,distantHead,glareMesh})if(mesh!=null)UnityEngine.Object.Destroy(mesh);if(glareTexture!=null)UnityEngine.Object.Destroy(glareTexture);foreach(var m in materials)if(m!=null)UnityEngine.Object.Destroy(m);materials.Clear();main=small=distant=lens=mast=mainHead=smallHead=distantHead=glareMesh=null;glareTexture=null;}
    }
}
