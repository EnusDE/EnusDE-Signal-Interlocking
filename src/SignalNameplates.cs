using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace EnusDE.Signaltechnik {
    public sealed class SignalNameplate : MonoBehaviour {
        public TextMeshPro Lettering;
    }

    public sealed partial class SignalModels {
        Mesh plateMesh;
        Material plateMaterial;
        readonly Dictionary<TMP_FontAsset,Material> nameFonts=new Dictionary<TMP_FontAsset,Material>();

        void BuildNameplate(SignalLamps signal,Transform parent,float height) {
            try {
                if(plateMesh==null) {
                    plateMesh=MeshOf(MarkerMeshData.CreateSignalPlate(),"EnusDE signal nameplate");
                    var shader=Shader.Find("Universal Render Pipeline/Lit");
                    plateMaterial=shader!=null&&shader.isSupported?ColorOf(shader,new Color(.08f,.085f,.087f)):frame;
                }
                var root=new GameObject("EnusDE signal nameplate"){layer=2};
                root.transform.SetParent(parent,false);
                root.transform.localPosition=new Vector3(0,height,-.06f);
                root.AddComponent<MeshFilter>().sharedMesh=plateMesh;
                var body=root.AddComponent<MeshRenderer>();
                body.sharedMaterial=plateMaterial;
                body.shadowCastingMode=ShadowCastingMode.Off;

                var letters=new GameObject("EnusDE nameplate lettering"){layer=2};
                letters.transform.SetParent(root.transform,false);
                letters.transform.localPosition=new Vector3(0,0,-.046f);
                var text=letters.AddComponent<TextMeshPro>();
                var font=TMP_Settings.defaultFontAsset;
                if(font==null)throw new InvalidOperationException("Spielschrift nicht verfügbar.");
                if(!nameFonts.TryGetValue(font,out var material)) {
                    material=new Material(font.material);
                    if(material.HasProperty("_CullMode"))material.SetFloat("_CullMode",(float)CullMode.Back);
                    nameFonts.Add(font,material);
                    materials.Add(material);
                }
                text.font=font;
                text.fontSharedMaterial=material;
                text.fontSize=19;
                text.fontSizeMin=7;
                text.fontSizeMax=19;
                text.enableAutoSizing=true;
                text.alignment=TextAlignmentOptions.Center;
                text.overflowMode=TextOverflowModes.Ellipsis;
                text.textWrappingMode=TextWrappingModes.NoWrap;
                text.richText=false;
                text.color=new Color(.73f,.73f,.69f);
                text.rectTransform.sizeDelta=new Vector2(7.4f,1.9f);
                text.transform.localScale=Vector3.one*.1f;
                text.renderer.shadowCastingMode=ShadowCastingMode.Off;
                text.renderer.receiveShadows=false;
                var plate=root.AddComponent<SignalNameplate>();
                plate.Lettering=text;
                signal.Nameplate=plate;
            } catch(Exception e) {
                Debug.LogWarning("EnusDE Signaltechnik: Beschriftungstafel konnte nicht erstellt werden. "+e.Message);
            }
        }

        public void LabelCircuitBox(GameObject root,string letter) {
            var font=TMP_Settings.defaultFontAsset;
            if(font==null)return;
            var face=new GameObject("EnusDE track circuit letter"){layer=2};
            face.transform.SetParent(root.transform,false);
            face.transform.localPosition=new Vector3(0,.214f,-.17f);
            face.transform.localScale=Vector3.one*.075f;
            var text=face.AddComponent<TextMeshPro>();
            text.font=font;
            text.fontSize=13;
            text.enableAutoSizing=true;text.fontSizeMin=6;text.fontSizeMax=13;
            text.textWrappingMode=TextWrappingModes.NoWrap;
            text.alignment=TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta=new Vector2(3,1.5f);
            text.color=new Color(.83f,.82f,.78f);
            text.text=letter;
            text.renderer.shadowCastingMode=ShadowCastingMode.Off;
        }

        public TextMeshPro LabelCircuitCode(GameObject root,string code) {
            var font=TMP_Settings.defaultFontAsset;
            if(font==null)return null;
            var face=new GameObject("EnusDE GFM Kennung"){layer=2};
            face.transform.SetParent(root.transform,false);
            face.transform.localPosition=new Vector3(0,.135f,-.17f);
            face.transform.localScale=Vector3.one*.04f;
            var text=face.AddComponent<TextMeshPro>();
            text.font=font;
            text.fontSize=7;text.enableAutoSizing=true;text.fontSizeMin=3.5f;text.fontSizeMax=7;
            text.rectTransform.sizeDelta=new Vector2(8,1.2f);text.overflowMode=TextOverflowModes.Ellipsis;
            text.textWrappingMode=TextWrappingModes.NoWrap;
            text.alignment=TextAlignmentOptions.Center;
            text.color=new Color(.92f,.92f,.88f);
            text.text=code;
            text.renderer.shadowCastingMode=ShadowCastingMode.Off;
            return text;
        }

        void UpdateNameplate(SignalNameplate plate,string text) {
            if(plate?.Lettering!=null)plate.Lettering.text=text??"";
        }

        void DisposeNameplates() {
            if(plateMesh!=null)UnityEngine.Object.Destroy(plateMesh);
            plateMesh=null;
            plateMaterial=null;
            nameFonts.Clear();
        }
    }
}
