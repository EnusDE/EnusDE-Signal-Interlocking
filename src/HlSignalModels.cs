using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalModels {
        Mesh hlMain,hlDistant,hlDistantHead,hlStrip,hlDistantBoard,hlCombinedMarker;
        void HlBoard(GameObject parent,bool distant) {
            Mesh mesh=distant?hlDistantBoard??(hlDistantBoard=MeshOf(MarkerMeshData.CreateHlDistantBoard(),"EnusDE Hl Ne2 board")):
                hlCombinedMarker??(hlCombinedMarker=MeshOf(MarkerMeshData.CreateHlCombinedMarker(),"EnusDE Hl combined function marker"));
            var part=new GameObject(distant?"EnusDE Hl distant identification Ne2":"EnusDE Hl combined function marker"){layer=2};part.transform.SetParent(parent.transform,false);
            part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterials=new[]{frame,distant?white:yellow,post};
        }
        Mesh HlMesh(string kind,bool headOnly) {
            if(kind==MarkerRecord.Main)return hlMain??(hlMain=MeshOf(MarkerMeshData.CreateHlHead(false,true),"EnusDE original Hl main head"));
            if(headOnly)return hlDistantHead??(hlDistantHead=MeshOf(MarkerMeshData.CreateHlHead(true,true),"EnusDE original Hl distant head"));
            return hlDistant??(hlDistant=MeshOf(MarkerMeshData.CreateHlHead(true),"EnusDE original Hl distant signal"));
        }
        Mesh HlStrip() {
            if(hlStrip!=null)return hlStrip;
            hlStrip=new Mesh{name="EnusDE Hl light strip"};
            hlStrip.vertices=new[]{new Vector3(-.31f,-.035f,0),new Vector3(.31f,-.035f,0),new Vector3(.31f,.035f,0),new Vector3(-.31f,.035f,0)};
            hlStrip.triangles=new[]{0,2,1,0,3,2};hlStrip.RecalculateNormals();hlStrip.RecalculateBounds();return hlStrip;
        }
        public void RefreshHlFrame(SignalLamps state) {
            int phase=Time.realtimeSinceStartup%1<.5?1:0;
            if(state.HlPhase==phase)return;state.HlPhase=phase;state.Colors=state.HlPlan.At(Time.realtimeSinceStartup);
            for(int i=0;i<state.Lamps.Length;i++) {
                var color=state.Colors[i];if(state.Lamps[i]!=null)state.Lamps[i].sharedMaterial=LensMaterial(color,!state.Preview);
                if(state.Halos[i]!=null){state.Halos[i].sharedMaterial=HaloMaterial(color);state.Halos[i].enabled=state.HaloVisible&&color!=SignalLensColor.Off&&HaloMaterial(color)!=off;}
            }
        }
        void DisposeHl(){foreach(var mesh in new[]{hlMain,hlDistant,hlDistantHead,hlStrip,hlDistantBoard,hlCombinedMarker})if(mesh!=null)Object.Destroy(mesh);hlMain=hlDistant=hlDistantHead=hlStrip=hlDistantBoard=hlCombinedMarker=null;}
    }
}
