using System;
using System.Collections.Generic;
using EnusDE.Signaltechnik;
using UnityEngine;
using TMPro;
namespace EnusDE.Runenwacht {
    public sealed class RunenwachtModel:MonoBehaviour,ISignalPresentation {
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly List<Head> heads=new List<Head>();readonly List<Face> faces=new List<Face>();
        RunenwachtPalette palette;bool disposed,preview;string kind;float emissionStrength=1;
        public Bounds LocalBounds=>new Bounds(V(0,kind=="main"?2.65f:kind=="distant"?1.65f:.85f,0),V(1.5f,kind=="main"?5.3f:kind=="distant"?3.3f:1.7f,1.5f));
        sealed class Face {public GameObject Root;public Head Main,Shunt,Distant;public TextMeshPro Code,Name;}
        sealed class Head {public GameObject Root;public string Kind;public CrystalTransition Transition=new CrystalTransition();public Dictionary<CrystalChannel,Material[]> Lights=new Dictionary<CrystalChannel,Material[]>();}
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        public void Build(string modelKind,bool ghost) {
            kind=modelKind;preview=ghost;palette=RunenwachtPalette.Acquire();
            float mastHeight=kind=="main"?4.5f:kind=="distant"?2.35f:1.02f;
            MeshObject(transform,"Chiselled stone foundation",RunenwachtDesign.Foundation(),new[]{palette.Stone,palette.Stone,palette.Stone});
            MeshObject(transform,"Riveted steel lattice, bent copper conduits and collars",RunenwachtDesign.Mast(mastHeight),Metals());
            for(int side=0;side<2;side++) {
                var root=Child(transform,"Directional face "+side);root.transform.localRotation=Quaternion.Euler(0,side==1?180:0,0);root.transform.localPosition=V(0,0,side==1?.15f:-.15f);
                var face=new Face{Root=root};faces.Add(face);
                if(kind=="main"){face.Main=MakeHead(root.transform,"main",4.55f);face.Distant=MakeHead(root.transform,"distant",2.95f);face.Shunt=MakeHead(root.transform,"shunt",1.7f);}
                else if(kind=="distant")face.Distant=MakeHead(root.transform,kind,2.65f);else face.Shunt=MakeHead(root.transform,kind,1.25f);
                float y=kind=="main"?3.83f:kind=="distant"?1.9f:.82f;
                face.Code=Plaque(root.transform,"Technical signal code",y,.72f,.17f,.085f);
                face.Name=Plaque(root.transform,"Display name",y-.17f,1.05f,.135f,.06f);
                var maker=Plaque(root.transform,"Maker's plate",.59f,.42f,.105f,.032f);maker.text="RUNENWACHT";
            }
        }
        Material[] Metals()=>new[]{palette.Steel,palette.Copper,palette.Ceramic};
        TextMeshPro Plaque(Transform parent,string label,float y,float width,float height,float textHeight) {
            var frame=Child(parent,label);frame.transform.localPosition=V(0,y,-.27f);MeshObject(frame.transform,"Copper frame, screws and enamel",RunenwachtDesign.Plaque(width,height),Metals());
            var textObject=Child(frame.transform,label+" engraving");textObject.transform.localPosition=V(0,0,-.033f);textObject.transform.localScale=Vector3.one*.01f;
            var text=textObject.AddComponent<TextMeshPro>();text.font=TMP_Settings.defaultFontAsset;text.richText=false;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2((width-.09f)*100,(height-.02f)*100);text.fontSize=textHeight*1000;text.enableAutoSizing=true;text.fontSizeMin=textHeight*450;text.fontSizeMax=textHeight*1000;text.textWrappingMode=TextWrappingModes.NoWrap;text.color=new Color(.065f,.085f,.09f);return text;
        }
        Head MakeHead(Transform parent,string headKind,float y) {
            var root=Child(parent,headKind+" sculpted assembly");root.transform.localPosition=V(0,y,-.12f);
            var head=new Head{Root=root,Kind=headKind};heads.Add(head);var d=RunenwachtDesign.Head(headKind);
            MeshObject(root.transform,"Layered housing, rivets, claws, insulators and copper plumbing",d.Metal,Metals());
            MeshObject(root.transform,"Inset reflector",d.Reflector,new[]{palette.Reflector,palette.Reflector,palette.Reflector});
            foreach(var pair in d.Channels){var edge=palette.Animated(pair.Key);var vein=palette.Animated(pair.Key);owned.Add(edge);owned.Add(vein);head.Lights[pair.Key]=new[]{edge,vein};MeshObject(root.transform,"Permanent cut "+pair.Key+" crystals and fine light veins",pair.Value,new[]{palette.Body(pair.Key),edge,vein});}
            return head;
        }
        public void Apply(SignalViewFrame frame) {
            if(disposed)return;emissionStrength=frame.EmissionStrength;if(frame.Kind!=kind)throw new ArgumentException("Runenwacht instance kind changed.");
            for(int i=0;i<faces.Count;i++){var f=faces[i];var state=i==0?frame.Front:frame.Back;bool active=state?.Enabled==true;f.Root.SetActive(active);if(!active)continue;
                f.Code.text=state.Code;f.Name.text=state.Caption;f.Name.transform.parent.gameObject.SetActive(state.Caption!=state.Code);
                f.Code.fontSizeMax=85f*state.LabelScale;f.Name.fontSizeMax=60*state.LabelScale;
                ApplyHead(f.Main,true,state.Main);ApplyHead(f.Shunt,kind=="shunt"||state.ShuntHead,state.Shunt);ApplyHead(f.Distant,kind=="distant"||state.DistantHead,state.Distant);
            }
        }
        static void ApplyHead(Head head,bool active,LocalSignalAspect aspect){if(head==null)return;head.Root.SetActive(active);head.Transition.Set(active?CrystalLogic.Channel(head.Kind,aspect):null);}
        void LateUpdate(){if(disposed)return;foreach(var head in heads){head.Transition.Advance(Time.unscaledDeltaTime);foreach(var pair in head.Lights){float level=head.Transition.Level(pair.Key)*(pair.Key==CrystalChannel.Red?1:1+.025f*Mathf.Sin(Time.unscaledTime*2.2f))*(preview?.65f:1);RunenwachtPalette.Light(pair.Value[0],pair.Key,level,emissionStrength,false);RunenwachtPalette.Light(pair.Value[1],pair.Key,level,emissionStrength,true);}}}
        static GameObject Child(Transform parent,string name){var obj=new GameObject(name);obj.layer=2;obj.transform.SetParent(parent,false);return obj;}
        void MeshObject(Transform parent,string name,RunenwachtGeometry geometry,Material[] mats){if(geometry.Vertices.Count==0)return;var obj=Child(parent,name);var mesh=geometry.Mesh(name);owned.Add(mesh);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterials=mats;}
        public void Dispose(){if(disposed)return;disposed=true;foreach(var item in owned)if(item!=null)UnityEngine.Object.Destroy(item);owned.Clear();if(palette!=null){RunenwachtPalette.Release();palette=null;}}
        void OnDestroy(){Dispose();}
    }
}
