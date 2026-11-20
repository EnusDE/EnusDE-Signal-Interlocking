using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalModels {
        Mesh invalidCross;
        Material invalidBlack,invalidWhite;
        void ShowOperatingVisual(GameObject root,SignalLamps state,LocalSignalAspect aspect) {
            bool invalid=aspect==LocalSignalAspect.Invalid,identification=aspect==LocalSignalAspect.Kennlicht;
            if(invalid&&state.InvalidityCross==null) {
                if(invalidCross==null)invalidCross=MeshOf(MarkerMeshData.CreateInvalidityCross(),"EnusDE own signal invalidity cross");
                if(invalidBlack==null) {
                    var shader=ShaderPolicy.FindLit<Shader>(Shader.Find)??frame.shader;
                    invalidBlack=ColorOf(shader,new Color(.015f,.015f,.015f));invalidWhite=ColorOf(shader,new Color(.85f,.85f,.82f));
                    foreach(var material in new[]{invalidBlack,invalidWhite}){material.DisableKeyword("_EMISSION");foreach(string property in new[]{"_EmissionColor","_EmissiveColor"})if(material.HasProperty(property))material.SetColor(property,Color.black);}
                }
                var cross=state.InvalidityCross=new GameObject("EnusDE not commissioned cross"){layer=2};cross.transform.SetParent(root.transform,false);
                cross.transform.localPosition=state.OperatingCenter+new Vector3(0,0,-.10f);cross.transform.localScale=Vector3.one*state.CrossScale;
                cross.AddComponent<MeshFilter>().sharedMesh=invalidCross;var renderer=cross.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{invalidBlack,invalidWhite,invalidBlack};renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            if(state.InvalidityCross!=null)state.InvalidityCross.SetActive(invalid);
            if(identification&&state.Identification==null) {
                var part=new GameObject("EnusDE identification light"){layer=2};part.transform.SetParent(root.transform,false);part.transform.localPosition=state.OperatingCenter+new Vector3(0,0,-.09f);part.transform.localScale=Vector3.one*.65f;
                part.AddComponent<MeshFilter>().sharedMesh=lens;var renderer=state.Identification=part.AddComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            if(state.Identification!=null){state.Identification.enabled=identification;state.Identification.sharedMaterial=LensMaterial(SignalLensColor.White,true);}
        }
        void DisposeOperatingVisual(){if(invalidCross!=null)UnityEngine.Object.Destroy(invalidCross);invalidCross=null;invalidBlack=invalidWhite=null;}
    }
}
