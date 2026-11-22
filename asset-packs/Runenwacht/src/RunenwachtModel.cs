using System;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using UnityEngine;
using TMPro;
namespace EnusDE.Runenwacht {
    public sealed class RunenwachtModel:MonoBehaviour,ISignalPresentation {
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly List<Head> heads=new List<Head>();
        readonly List<Face> faces=new List<Face>();
        Material iron,copper,ceramic;bool disposed,preview;string kind;float emissionStrength=1;
        public Bounds LocalBounds=>new Bounds(V(0,kind==MarkerRecord.Main?2.55f:kind==MarkerRecord.Distant?1.6f:.8f,0),V(1.3f,kind==MarkerRecord.Main?5.1f:kind==MarkerRecord.Distant?3.2f:1.6f,1.4f));
        sealed class Face {public GameObject Root;public Head Main,Shunt,Distant;public TextMeshPro Caption;}
        sealed class Head {
            public GameObject Root;public string Kind;public CrystalTransition Transition=new CrystalTransition();
            public Dictionary<CrystalChannel,Material> Materials=new Dictionary<CrystalChannel,Material>();
        }
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        public void Build(string modelKind,bool ghost) {
            kind=modelKind;preview=ghost;
            iron=Material(new Color(.065f,.08f,.085f),false);copper=Material(new Color(.45f,.23f,.085f),false);ceramic=Material(new Color(.24f,.31f,.28f),false);
            var mast=new RunenwachtGeometry();float mastHeight=kind==MarkerRecord.Main?4.5f:kind==MarkerRecord.Distant?2.35f:1.02f;
            mast.Box(V(0,.18f,0),V(.82f,.36f,.82f),2);mast.Box(V(0,.39f,0),V(.48f,.12f,.48f),1);
            mast.Box(V(0,mastHeight/2+.35f,.16f),V(.16f,mastHeight,.18f));
            mast.Box(V(.105f,mastHeight/2+.35f,.17f),V(.035f,mastHeight,.035f),1);
            for(float y=.6f;y<mastHeight;y+=.36f){mast.Box(V(0,y,.28f),V(.25f,.045f,.04f),1);mast.Box(V(-.15f,y,.22f),V(.035f,.36f,.04f));}
            for(int i=0;i<4;i++){float x=(i%2==0?-1:1)*.3f,z=(i<2?-1:1)*.3f;mast.Ring(V(x,.37f,z),.045f,0,.04f,1,8);}
            MeshObject(transform,"Forged mast, copper conduit, ladder and foundation",mast,new[]{iron,copper,ceramic});
            for(int side=0;side<2;side++) {
                var root=Child(transform,"Directional face "+side);root.transform.localRotation=Quaternion.Euler(0,side==1?180:0,0);root.transform.localPosition=V(0,0,side==1?.12f:-.12f);
                var face=new Face{Root=root};faces.Add(face);
                if(kind==MarkerRecord.Main){face.Main=MakeHead(root.transform,MarkerRecord.Main,4.45f);face.Distant=MakeHead(root.transform,MarkerRecord.Distant,3.0f);face.Shunt=MakeHead(root.transform,MarkerRecord.Shunt,2.05f);}
                else if(kind==MarkerRecord.Distant)face.Distant=MakeHead(root.transform,kind,2.65f);
                else face.Shunt=MakeHead(root.transform,kind,1.25f);
                var label=Child(root.transform,"Signal identification");label.transform.localPosition=V(0,kind==MarkerRecord.Main?3.78f:kind==MarkerRecord.Distant?2.1f:.85f,-.39f);label.transform.localRotation=Quaternion.identity;label.transform.localScale=Vector3.one*.06f;
                var text=label.AddComponent<TextMeshPro>();text.font=TMP_Settings.defaultFontAsset;text.richText=false;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(12,2);text.fontSize=12;text.color=new Color(.75f,.87f,.78f);face.Caption=text;
            }
        }
        Head MakeHead(Transform parent,string headKind,float y) {
            var root=Child(parent,headKind+" housing");root.transform.localPosition=V(0,y,-.1f);
            var head=new Head{Root=root,Kind=headKind};heads.Add(head);
            var metal=new RunenwachtGeometry();var channels=new Dictionary<CrystalChannel,RunenwachtGeometry>();
            foreach(CrystalChannel c in Enum.GetValues(typeof(CrystalChannel)))channels[c]=new RunenwachtGeometry();
            if(headKind==MarkerRecord.Main) {
                // A closed rear cup, annular front rim, visor, and permanently installed separate crystal cores.
                metal.Ring(V(0,0,.14f),.55f,0,.12f,0);metal.Ring(V(0,0,-.05f),.57f,.46f,.32f,1);
                metal.Box(V(0,.47f,-.23f),V(.78f,.11f,.48f));
                channels[CrystalChannel.Red].Crystal(V(-.24f,-.03f,-.26f),.18f,.43f,.12f);
                channels[CrystalChannel.Green].Crystal(V(0,.035f,-.26f),.22f,.67f,.14f);
                channels[CrystalChannel.Amber].Crystal(V(.24f,-.03f,-.26f),.18f,.43f,.12f);
                for(int i=0;i<4;i++){float a=45+i*90;float x=Mathf.Cos(a*Mathf.Deg2Rad)*.38f,z=Mathf.Sin(a*Mathf.Deg2Rad)*.38f;channels[CrystalChannel.Red].Box(V(x,z,-.235f),V(.18f,.035f,.035f),0,a);}
                channels[CrystalChannel.White].Crystal(V(0,-.36f,-.27f),.075f,.1f,.06f);
                for(int i=0;i<16;i++){float a=i*Mathf.PI/8;metal.Ring(V(Mathf.Cos(a)*.515f,Mathf.Sin(a)*.515f,-.23f),.021f,0,.035f,0,6);}
            } else if(headKind==MarkerRecord.Distant) {
                // Three unequal spires and a fixed red stop-expectation bar; the left two are amber.
                metal.Box(V(0,-.08f,.09f),V(1.04f,.76f,.15f));metal.Box(V(0,-.46f,-.07f),V(1.07f,.14f,.36f),1);
                metal.Box(V(-.52f,-.04f,-.12f),V(.09f,.85f,.4f),1);metal.Box(V(.52f,-.04f,-.12f),V(.09f,.85f,.4f),1);
                metal.Box(V(0,.37f,-.12f),V(1.1f,.1f,.4f));
                channels[CrystalChannel.Amber].Crystal(V(-.31f,-.16f,-.2f),.16f,.28f,.12f);
                channels[CrystalChannel.Amber].Crystal(V(0,-.06f,-.2f),.18f,.48f,.12f);
                channels[CrystalChannel.Green].Crystal(V(.29f,.025f,-.2f),.19f,.65f,.13f);
                channels[CrystalChannel.Red].Box(V(0,-.4f,-.29f),V(.82f,.065f,.065f));
                channels[CrystalChannel.White].Crystal(V(-.43f,.24f,-.24f),.055f,.09f,.04f);
            } else {
                metal.Box(V(0,0,.08f),V(.83f,.34f,.15f));metal.Box(V(0,-.19f,-.08f),V(.88f,.09f,.43f),1);metal.Box(V(0,.2f,-.12f),V(.89f,.1f,.46f));
                metal.Box(V(-.44f,0,-.1f),V(.08f,.4f,.4f),1);metal.Box(V(.44f,0,-.1f),V(.08f,.4f,.4f),1);
                channels[CrystalChannel.Red].Box(V(0,-.07f,-.24f),V(.63f,.07f,.065f));
                channels[CrystalChannel.Violet].Crystal(V(0,.045f,-.25f),.62f,.13f,.11f);
                channels[CrystalChannel.White].Crystal(V(.35f,.02f,-.25f),.045f,.08f,.04f);
            }
            metal.Box(V(0,-.12f,.27f),V(.12f,.32f,.42f),1);
            // Copper retaining claws, cable gland, ceramic insulators and engraved geometric rune strokes.
            for(int side=-1;side<=1;side+=2){metal.Box(V(side*.2f,-.3f,-.27f),V(.04f,.13f,.12f),1);metal.Ring(V(side*.17f,-.32f,.2f),.055f,.025f,.12f,2,12);}
            for(int i=0;i<5;i++){float x=(i-2)*.11f;metal.Box(V(x,-.29f,-.19f),V(.025f,.08f,.012f),1,25);metal.Box(V(x+.025f,-.27f,-.19f),V(.025f,.045f,.012f),1,-25);}
            MeshObject(root.transform,"Opaque rear cup, copper guards and rune inlay",metal,new[]{iron,copper,ceramic});
            foreach(var pair in channels)if(pair.Value.Vertices.Count>0){var mat=Material(Colour(pair.Key),true);head.Materials.Add(pair.Key,mat);MeshObject(root.transform,"Permanent "+pair.Key+" crystals",pair.Value,new[]{mat,mat,mat});}
            return head;
        }
        public void Apply(SignalViewFrame frame) {
            if(disposed)return;emissionStrength=frame.EmissionStrength;
            if(frame.Kind!=kind)throw new ArgumentException("Runenwacht instance kind changed.");
            for(int i=0;i<faces.Count;i++) {
                var f=faces[i];var state=i==0?frame.Front:frame.Back;
                bool enabled=state?.Enabled==true;f.Root.SetActive(enabled);if(!enabled)continue;
                f.Caption.text=state.Caption;f.Caption.transform.localScale=Vector3.one*(.06f*state.LabelScale);
                ApplyHead(f.Main,true,state.Main);ApplyHead(f.Shunt,kind==MarkerRecord.Shunt||state.ShuntHead,state.Shunt);ApplyHead(f.Distant,kind==MarkerRecord.Distant||state.DistantHead,state.Distant);
            }
        }
        static void ApplyHead(Head head,bool active,LocalSignalAspect aspect){if(head==null)return;head.Root.SetActive(active);head.Transition.Set(active?CrystalLogic.Channel(head.Kind,aspect):null);}
        void LateUpdate() {
            if(disposed)return;
            foreach(var head in heads){head.Transition.Advance(Time.unscaledDeltaTime);foreach(var pair in head.Materials){float intensity=head.Transition.Level(pair.Key);float shimmer=pair.Key==CrystalChannel.Red?1:1+.025f*Mathf.Sin(Time.unscaledTime*2.2f);Emission(pair.Value,Colour(pair.Key),intensity*shimmer*(preview?.65f:1),emissionStrength);}}
        }
        static Color Colour(CrystalChannel c)=>c==CrystalChannel.Red?new Color(1,.025f,.015f):c==CrystalChannel.Amber?new Color(1,.48f,.025f):c==CrystalChannel.Green?new Color(.04f,1,.58f):c==CrystalChannel.Violet?new Color(.72f,.12f,1):new Color(.8f,.93f,1);
        Material Material(Color colour,bool crystal) {
            var shader=ShaderPolicy.FindLit<Shader>(Shader.Find)??ShaderPolicy.Find<Shader>(Shader.Find);if(shader==null)throw new InvalidOperationException("No supported Runenwacht shader.");
            var m=new Material(shader);owned.Add(m);
            // Even an unlit fallback uses intensity-coloured opaque surfaces, never a permanently bright inactive lamp.
            Paint(m,colour*(crystal?.025f:1));if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",crystal?.05f:.65f);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",crystal?.7f:.32f);
            if(crystal)Emission(m,colour,0);return m;
        }
        static void Paint(Material m,Color c){foreach(string key in new[]{"_BaseColor","_Color","_UnlitColor"})if(m.HasProperty(key))m.SetColor(key,c);}
        static void Emission(Material m,Color colour,float intensity,float strength=1){Paint(m,colour*(.025f+intensity*.7f));m.EnableKeyword("_EMISSION");foreach(string key in new[]{"_EmissionColor","_EmissiveColor"})if(m.HasProperty(key))m.SetColor(key,colour*(intensity*5*strength));if(m.HasProperty("_EmissiveIntensity"))m.SetFloat("_EmissiveIntensity",1);}
        static GameObject Child(Transform parent,string name){var g=new GameObject(name);g.layer=2;g.transform.SetParent(parent,false);return g;}
        void MeshObject(Transform parent,string name,RunenwachtGeometry geometry,Material[] materials){var obj=Child(parent,name);var mesh=geometry.Mesh(name);owned.Add(mesh);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterials=materials;}
        public void Dispose(){if(disposed)return;disposed=true;foreach(var asset in owned)if(asset!=null)UnityEngine.Object.Destroy(asset);owned.Clear();}
        void OnDestroy(){Dispose();}
    }
}
