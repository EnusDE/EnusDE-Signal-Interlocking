using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Track;
using Track.Signals;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    sealed class StockLampVisuals {
        sealed class Lamp {
            public MeshRenderer Renderer;
            public int Index;
            public Behaviour Beam;
            public Color Previous;
            public bool BeamEnabled;
        }

        static readonly System.Reflection.FieldInfo meshField=AccessTools.Field(typeof(SemaphoreHeadController),"lampMeshRenderer");
        static readonly System.Reflection.FieldInfo indexField=AccessTools.Field(typeof(SemaphoreHeadController),"lampMaterialIndex");
        static readonly System.Reflection.FieldInfo beamField=AccessTools.Field(typeof(SemaphoreHeadController),"volumetricLightBeam");
        static readonly System.Reflection.FieldInfo emissionField=AccessTools.Field(typeof(SemaphoreHeadController),"EmissionColor");
        readonly Dictionary<string,List<Lamp>> darkened=new Dictionary<string,List<Lamp>>();
        MaterialPropertyBlock block;
        static int Emission=0;

        static MeshRenderer Mesh(SemaphoreHeadController head)=>meshField?.GetValue(head) as MeshRenderer;
        public static Renderer[] Lenses(CTCSignal signal)=>signal.modelController==null?Array.Empty<Renderer>():
            signal.modelController.GetComponentsInChildren<SemaphoreHeadController>(true)
                .Select(Mesh).Where(r=>r!=null&&r.enabled&&r.gameObject.activeInHierarchy).Cast<Renderer>().ToArray();

        public bool IsBound(string id)=>id!=null&&darkened.TryGetValue(id,out var lamps)&&lamps.Any(l=>l.Renderer!=null);

        public void Sync(IEnumerable<RetiredSignal> rows,Dictionary<string,CTCSignal> models) {
            var wanted=new HashSet<string>(rows.Where(r=>r.Visual&&r.Darken).Select(r=>r.Id));
            foreach(string id in darkened.Keys.Where(id=>!wanted.Contains(id)||!darkened[id].Any(l=>l.Renderer!=null)).ToArray())Release(id);
            if(wanted.Count==0||meshField==null||indexField==null||emissionField==null)return;
            Emission=(int)emissionField.GetValue(null);
            if(block==null)block=new MaterialPropertyBlock();
            foreach(string id in wanted) {
                if(darkened.ContainsKey(id)||!models.TryGetValue(id,out var signal)||signal.modelController==null)continue;
                var lamps=new List<Lamp>();
                foreach(var head in signal.modelController.GetComponentsInChildren<SemaphoreHeadController>(true)) {
                    var renderer=Mesh(head);
                    if(renderer==null)continue;
                    int index=(int)indexField.GetValue(head);
                    var materials=renderer.sharedMaterials;
                    if(index<0||index>=materials.Length||materials[index]==null)continue;
                    renderer.GetPropertyBlock(block,index);
                    var previous=block.HasColor(Emission)?block.GetColor(Emission):
                        materials[index].HasProperty(Emission)?materials[index].GetColor(Emission):Color.black;
                    var beam=beamField?.GetValue(head) as Behaviour;
                    lamps.Add(new Lamp{Renderer=renderer,Index=index,Beam=beam,Previous=previous,BeamEnabled=beam!=null&&beam.enabled});
                }
                if(lamps.Count>0)darkened[id]=lamps;
            }
        }

        public void Enforce() {
            foreach(var lamps in darkened.Values)foreach(var lamp in lamps) {
                if(lamp.Renderer==null)continue;
                lamp.Renderer.GetPropertyBlock(block,lamp.Index);
                bool has=block.HasColor(Emission);
                var current=block.GetColor(Emission);
                if(!has||current!=Color.black) {
                    if(has)lamp.Previous=current;
                    block.SetColor(Emission,Color.black);
                    lamp.Renderer.SetPropertyBlock(block,lamp.Index);
                }
                if(lamp.Beam!=null&&lamp.Beam.enabled){lamp.BeamEnabled=true;lamp.Beam.enabled=false;}
            }
        }

        void Release(string id) {
            foreach(var lamp in darkened[id]) {
                if(lamp.Renderer!=null) {
                    lamp.Renderer.GetPropertyBlock(block,lamp.Index);
                    if(block.HasColor(Emission)&&block.GetColor(Emission)==Color.black) {
                        block.SetColor(Emission,lamp.Previous);
                        lamp.Renderer.SetPropertyBlock(block,lamp.Index);
                    }
                }
                if(lamp.Beam!=null&&lamp.BeamEnabled)lamp.Beam.enabled=true;
            }
            darkened.Remove(id);
        }
        public void Clear(){foreach(string id in darkened.Keys.ToArray())Release(id);}
    }
}
