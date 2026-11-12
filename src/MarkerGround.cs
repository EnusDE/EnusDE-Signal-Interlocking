using System;
using Helpers;
using UnityEngine;
using UnityEngine.Rendering;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerScene {
        Mesh foundationMesh;
        Material cableMaterial;

        static bool Surface(Vector3 world,out float level) {
            level=world.y;
            int layer=Helpers.Layers.Terrain;
            if(layer<0)return false;
            if(!Physics.Raycast(world+Vector3.up*1.5f,Vector3.down,out var hit,7f,1<<layer,QueryTriggerInteraction.Ignore))return false;
            level=hit.point.y;
            return true;
        }

        static void SetGroundPose(GameObject root,MarkerAnchor anchor) {
            var world=root.transform.position;
            if(!Surface(world,out float level)||Mathf.Abs(level-world.y)>1.35f)return;
            world.y=level+(float)anchor.Height;
            root.transform.position=world;
        }

        void BuildFoundation(GameObject root,bool ghost) {
            if(foundationMesh==null)foundationMesh=BuildMesh(MarkerMeshData.CreateFoundation(),"EnusDE mast concrete foot");
            var foot=new GameObject("EnusDE mast foundation"){layer=2};
            foot.transform.SetParent(root.transform,false);
            foot.AddComponent<MeshFilter>().sharedMesh=foundationMesh;
            var renderer=foot.AddComponent<MeshRenderer>();
            renderer.sharedMaterials=new[]{ghost?blue:gray,black,gray};
        }

        GameObject Box(GameObject parent,int variant,bool ghost) {
            var box=new GameObject(variant==1?"EnusDE GFM A":"EnusDE GFM B"){layer=2};
            box.transform.SetParent(parent.transform,false);
            box.AddComponent<MeshFilter>().sharedMesh=variant==1?circuitAMesh:circuitBMesh;
            var renderer=box.AddComponent<MeshRenderer>();
            renderer.sharedMaterials=ghost?new[]{blue,white,blue}:new[]{black,white,gray};
            signals.LabelCircuitBox(box,variant==1?"A":"B");
            return box;
        }

        void BuildCircuitVisual(GameObject root,MarkerRecord row) {
            if(!CircuitIdentity.Enabled(row))return;
            var state=root.AddComponent<CircuitVisual>();
            state.Box=row.IsCircuitBox?root:Box(root,CircuitIdentity.Variant(row),false);
            if(!row.IsCircuitBox)MarkerPickable.Attach(state.Box,row,true);
            signals.LabelCircuitCode(state.Box,CircuitIdentity.Caption(row));
            if(cableMaterial==null)cableMaterial=ColorMaterial(ShaderPolicy.Find<Shader>(Shader.Find),new Color(.09f,.082f,.065f));
            var go=new GameObject("EnusDE GFM Kabel"){layer=2};
            go.transform.SetParent(root.transform,false);
            state.Cable=go.AddComponent<LineRenderer>();
            state.Cable.sharedMaterial=cableMaterial;
            state.Cable.useWorldSpace=false;
            state.Cable.startWidth=state.Cable.endWidth=.026f;
            state.Cable.numCornerVertices=2;
            state.Cable.shadowCastingMode=ShadowCastingMode.Off;
            state.Cable.receiveShadows=false;
            var terminal=new GameObject("EnusDE Schienenanschluss"){layer=2};
            terminal.transform.SetParent(root.transform,false);
            terminal.AddComponent<MeshFilter>().sharedMesh=BuildTerminalMesh();
            var renderer=terminal.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=black;
            state.Terminal=terminal.transform;
        }

        Mesh terminalMesh;
        Mesh BuildTerminalMesh() {
            if(terminalMesh!=null)return terminalMesh;
            terminalMesh=new Mesh{name="EnusDE GFM Schienenkontakt"};
            terminalMesh.vertices=new[]{new Vector3(-.07f,0,-.04f),new Vector3(-.07f,0,.04f),new Vector3(.07f,0,.04f),new Vector3(.07f,0,-.04f)};
            terminalMesh.triangles=new[]{0,1,2,0,2,3};
            terminalMesh.RecalculateBounds();
            terminalMesh.RecalculateNormals();
            return terminalMesh;
        }

        void PoseCircuit(GameObject root,MarkerRecord row) {
            var state=root.GetComponent<CircuitVisual>();
            if(state==null)return;
            var rail=row.Anchor.Clone();
            float side=row.Anchor.Lateral<0?-1:1;
            rail.Lateral=.78f*side;
            rail.Height=-.075;
            if(!geometry.Resolve(rail,out var onRail,out var along,out _)) {
                state.Cable.enabled=false;state.Terminal.gameObject.SetActive(false);return;
            }
            var end=onRail.GameToWorld();
            if(state.Box!=root) {
                var baseAnchor=row.Anchor.Clone();
                baseAnchor.Height=0;
                baseAnchor.Lateral+=side*.28;
                if(geometry.Resolve(baseAnchor,out var box,out _,out _)) {
                    state.Box.transform.position=box.GameToWorld()+along*Vector3.forward*(float)row.CircuitOffset;
                    state.Box.transform.rotation=along;
                    SetGroundPose(state.Box,baseAnchor);
                }
            }
            var start=state.Box.transform.position+Vector3.up*.065f;
            state.Terminal.SetPositionAndRotation(end,along);
            state.Terminal.gameObject.SetActive(true);
            var points=state.Points;
            for(int i=0;i<points.Length;i++) {
                var t=i/(float)(points.Length-1);
                var point=Vector3.Lerp(start,end,t)+along*Vector3.forward*(Mathf.Sin(t*Mathf.PI)*.13f);
                if(i!=0&&i!=points.Length-1&&Surface(point,out float level))point.y=level+.026f;
                points[i]=root.transform.InverseTransformPoint(point);
            }
            state.Cable.enabled=true;
            state.Cable.positionCount=points.Length;
            state.Cable.SetPositions(points);
        }

        void DisposeGround() {
            if(foundationMesh!=null)UnityEngine.Object.Destroy(foundationMesh);
            if(terminalMesh!=null)UnityEngine.Object.Destroy(terminalMesh);
            if(cableMaterial!=null)UnityEngine.Object.Destroy(cableMaterial);
        }
    }

    public sealed class CircuitVisual : MonoBehaviour {
        public GameObject Box;
        public LineRenderer Cable;
        public Transform Terminal;
        public readonly Vector3[] Points=new Vector3[7];
    }
}
