using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using UnityEngine;
using TMPro;

namespace EnusDE.Signaltechnik {
    public sealed class AutomationContactPickable:MonoBehaviour,IPickable {
        public string Id;public Action<string> Open;
        public float MaxPickDistance=>250;public int Priority=>1;
        public TooltipInfo TooltipInfo=>new TooltipInfo(LocalText.Select("Signalling contact","Signaltechnik-Kontakt"),LocalText.Select("Click to configure signal requests and the axle test.","Anklicken: Signalanforderung und Achstest einrichten."));
        public PickableActivationFilter ActivationFilter=>Entry.PlacingObject?PickableActivationFilter.None:PickableActivationFilter.Any;
        public void Activate(PickableActivateEvent value){if(!Entry.PlacingObject&&!Entry.BlockWorldObjectClick)Open?.Invoke(Id);}
        public void Deactivate(){}
    }
    public sealed class AutomationContactScene:IDisposable {
        sealed class Model {public GameObject Root,Screen;public TextMeshPro[] Counters;public Transform Lever;public Renderer Lamp;public long Count=-1;public float Pulse;}
        readonly Dictionary<string,Model> models=new Dictionary<string,Model>();
        readonly Transform parent;readonly Action<string> open;readonly MarkerTrackGeometry geometry=new MarkerTrackGeometry();
        readonly Material casing,off,on;
        public AutomationContactScene(Transform parent,Action<string> open) {
            this.parent=parent;this.open=open;var shader=ShaderPolicy.Find<Shader>(Shader.Find);if(shader==null)return;
            casing=Color(shader,new Color(.21f,.24f,.25f));off=Color(shader,new Color(.12f,.10f,.04f));
            on=Color(shader,new Color(1,.65f,.08f));on.EnableKeyword("_EMISSION");
            foreach(string key in new[]{"_EmissionColor","_EmissiveColor"})if(on.HasProperty(key))on.SetColor(key,new Color(1,.5f,.02f)*2);
        }
        static Material Color(Shader shader,Color color){var material=new Material(shader);foreach(string key in new[]{"_BaseColor","_Color","_UnlitColor"})if(material.HasProperty(key))material.SetColor(key,color);return material;}
        GameObject Box(Transform root,string name,Vector3 position,Vector3 size,Material material,bool collider=false) {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)UnityEngine.Object.Destroy(go.GetComponent<Collider>());return go;
        }
        public void Refresh(IEnumerable<AutomationContact> contacts,AutomationLiveFrame frame,bool visible) {
            if(casing==null)return;
            var wanted=new HashSet<string>();float now=Time.realtimeSinceStartup;
            foreach(var contact in contacts) {
                wanted.Add(contact.Id);
                if(!geometry.Resolve(contact.Anchor,out var position,out var rotation,out _)){if(models.TryGetValue(contact.Id,out var invalid))invalid.Root.SetActive(false);continue;}
                if(!models.TryGetValue(contact.Id,out var model)) {
                    model=new Model{Root=new GameObject("EnusDE axle test contact "+contact.Id)};model.Root.transform.SetParent(parent,false);
                    var body=Box(model.Root.transform,"Rail contact",new Vector3(0,.04f,0),new Vector3(.15f,.08f,.36f),casing,true);
                    body.layer=Layers.Clickable;var pick=body.AddComponent<AutomationContactPickable>();pick.Id=contact.Id;pick.Open=open;
                    var hit=body.GetComponent<BoxCollider>();hit.size=new Vector3(2.3f,3,1.5f);
                    model.Lever=Box(model.Root.transform,"Axle test indicator",new Vector3(0,.09f,0),new Vector3(.06f,.035f,.22f),casing).transform;
                    model.Lamp=Box(model.Root.transform,"Passage indicator",new Vector3(0,.09f,.13f),new Vector3(.06f,.025f,.03f),off).GetComponent<Renderer>();models.Add(contact.Id,model);
                    model.Screen=Box(model.Root.transform,"Axle test display",new Vector3(0,.35f,0),new Vector3(.48f,.25f,.045f),off,true);
                    model.Screen.layer=Layers.Clickable;var screenPick=model.Screen.AddComponent<AutomationContactPickable>();screenPick.Id=contact.Id;screenPick.Open=open;
                    // TMP's front faces -Z. Keep lettering in front of each casing face,
                    // rather than turning the front text into the opaque display plate.
                    model.Counters=new TextMeshPro[2];
                    for(int side=0;side<2;side++) {
                        var text=new GameObject("Axle count "+side,typeof(RectTransform),typeof(TextMeshPro));text.transform.SetParent(model.Root.transform,false);
                        text.transform.localPosition=new Vector3(0,.35f,side==0?-.028f:.028f);text.transform.localRotation=Quaternion.Euler(0,side==0?0:180,0);
                        var counter=text.GetComponent<TextMeshPro>();counter.font=TMP_Settings.defaultFontAsset;counter.fontSize=1.8f;counter.enableAutoSizing=true;counter.fontSizeMin=.6f;counter.fontSizeMax=1.8f;
                        counter.alignment=TextAlignmentOptions.Center;counter.richText=false;counter.color=new Color(1,.85f,.35f);
                        counter.rectTransform.sizeDelta=new Vector2(.45f,.23f);counter.renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;model.Counters[side]=counter;
                    }
                }
                model.Root.SetActive(visible);model.Root.transform.position=position.GameToWorld();model.Root.transform.rotation=rotation*Quaternion.Euler(0,contact.Reverse?180:0,0);
                var live=frame?.Contacts.FirstOrDefault(c=>c.Id==contact.Id);long count=live?.Count??0;
                model.Screen.SetActive(contact.ShowCounter);
                foreach(var counter in model.Counters){counter.gameObject.SetActive(contact.ShowCounter);counter.text="AZ\n"+(!contact.AxleTest||live==null?"—":count.ToString(System.Globalization.CultureInfo.InvariantCulture));}
                if(model.Count>=0&&count!=model.Count)model.Pulse=now+.4f;model.Count=count;
                float animation=Mathf.Clamp01((model.Pulse-now)/.4f);model.Lever.localRotation=Quaternion.Euler(Mathf.Sin(animation*Mathf.PI)*-22,0,0);
                model.Lamp.sharedMaterial=animation>0?on:off;
            }
            foreach(var id in models.Keys.Where(id=>!wanted.Contains(id)).ToArray()){UnityEngine.Object.Destroy(models[id].Root);models.Remove(id);}
        }
        public void Dispose(){foreach(var model in models.Values)UnityEngine.Object.Destroy(model.Root);models.Clear();UnityEngine.Object.Destroy(casing);UnityEngine.Object.Destroy(off);UnityEngine.Object.Destroy(on);}
    }
}
