using System;
using System.Collections.Generic;
using System.Linq;
using Track;
using Helpers;
using UnityEngine;
namespace EnusDE.Signaltechnik {
    // Local build aid. No occupancy reader or operating writer; colours identify sections only.
    public sealed class GfmWorldOverlay:IDisposable {
        public static readonly Color[] Colours={new Color(.1f,.8f,1),new Color(1,.45f,.1f),new Color(.7f,.3f,1),new Color(.3f,1,.55f),new Color(1,.35f,.65f),new Color(1,.85f,.2f),new Color(.3f,.5f,1),new Color(.8f,1,.4f)};
        GameObject root;readonly List<Material> materials=new List<Material>();
        public int Drawn {get;private set;} public int Skipped {get;private set;}
        public void FollowOrigin(){if(root!=null)root.transform.position=Vector3.zero.GameToWorld();}
        public void Draw(TrackBlock[] blocks,string selected,Action<string> pick) {
            Dispose();root=new GameObject("EnusDE GFM build overlay");FollowOrigin();
            var shader=ShaderPolicy.Find<Shader>(Shader.Find);if(shader==null)throw new InvalidOperationException(LocalText.Select("No compatible overlay shader.","Kein kompatibler Shader für die Abschnittsanzeige."));
            foreach(var colour in Colours){var m=new Material(shader);foreach(string key in new[]{"_BaseColor","_Color","_UnlitColor"})if(m.HasProperty(key))m.SetColor(key,colour);materials.Add(m);}
            var palette=GfmWorldPalette.Assign(blocks);int budget=3000;var camera=Camera.main;
            foreach(var block in blocks.OrderByDescending(b=>b.Id==selected)) {
                bool drawn=false;
                foreach(var section in block.Sections) {
                    var segment=Graph.Shared?.GetSegment(section.Segment);
                    if(segment==null||!segment.Available||!segment.GroupEnabled||segment.turntable!=null||section.From<0||section.To>segment.GetLength()+.001){Skipped++;continue;}
                    int samples=Math.Max(1,(int)Math.Ceiling((section.To-section.From)/5));
                    for(int i=0;i<samples;i++) {
                        if(budget<=0){Skipped++;break;}
                        float a=(float)(section.From+(section.To-section.From)*i/samples),b=(float)(section.From+(section.To-section.From)*(i+1)/samples);
                        var start=Graph.Shared.GetPosition(new Location(segment,a,TrackSegment.End.A))+Vector3.up*.18f;
                        var end=Graph.Shared.GetPosition(new Location(segment,b,TrackSegment.End.A))+Vector3.up*.18f;
                        if(camera!=null&&Vector3.Distance(((start+end)*.5f).GameToWorld(),camera.transform.position)>400)continue;
                        Segment(start,end,materials[palette[block.Id]],block.Id==selected?.20f:.10f,block.Id,pick);budget--;drawn=true;
                    }
                }
                if(drawn){Drawn++;var path=block.Path;if(path.Count>0){End(path.First(),true,materials[palette[block.Id]],block.Id,pick);End(path.Last(),false,materials[palette[block.Id]],block.Id,pick);}}
            }
        }
        void End(DirectedRouteSection part,bool first,Material paint,string id,Action<string> pick) {
            var s=part.Section;var segment=Graph.Shared?.GetSegment(s.Segment);double at=first?(part.Reverse?s.To:s.From):(part.Reverse?s.From:s.To);
            if(segment==null||at<0||at>segment.GetLength())return;
            var pos=Graph.Shared.GetPosition(new Location(segment,(float)at,TrackSegment.End.A));
            Segment(pos+Vector3.up*.15f,pos+Vector3.up*1.4f,paint,.14f,id,pick);
        }
        void Segment(Vector3 a,Vector3 b,Material paint,float width,string id,Action<string> pick) {
            var go=new GameObject("GFM section");go.layer=Helpers.Layers.Clickable;go.transform.SetParent(root.transform,false);
            var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=paint;line.positionCount=2;line.SetPosition(0,a);line.SetPosition(1,b);line.startWidth=line.endWidth=width;
            var target=new GameObject("GFM selection");target.layer=Helpers.Layers.Clickable;target.transform.SetParent(root.transform,false);target.transform.localPosition=(a+b)*.5f;
            target.transform.localRotation=Quaternion.LookRotation(b-a);var box=target.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(.65f,.4f,Math.Max(.1f,Vector3.Distance(a,b)));
            var click=target.AddComponent<GfmWorldPickable>();click.Id=id;click.Pick=pick;
        }
        public void Dispose(){if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);root=null;}foreach(var m in materials)UnityEngine.Object.Destroy(m);materials.Clear();Drawn=Skipped=0;}
    }
    public sealed class GfmWorldPickable:MonoBehaviour,IPickable {
        public string Id;public Action<string> Pick;
        public float MaxPickDistance=>400;public int Priority=>0;
        public TooltipInfo TooltipInfo=>new TooltipInfo(LocalText.Select("GFM section","GFM-Abschnitt"),LocalText.Select("Click: select saved detection path","Anklicken: gespeicherten Meldeweg auswählen"));
        public PickableActivationFilter ActivationFilter=>Entry.PlacingObject?PickableActivationFilter.None:PickableActivationFilter.Any;
        public void Activate(PickableActivateEvent e){if(!Entry.PlacingObject&&!Entry.BlockWorldObjectClick&&!WorldPointerInput.OverUi())Pick?.Invoke(Id);}
        public void Deactivate(){}
    }
}
