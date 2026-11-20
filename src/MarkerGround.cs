using System;
using Helpers;
using UnityEngine;
using UnityEngine.Rendering;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerScene {
        Mesh foundationMesh,supportMesh;
        Material cableMaterial;

        static bool Surface(Vector3 world,out float level) {
            level=world.y;
            int layer=Helpers.Layers.Terrain;
            if(layer<0)return false;
            if(!Physics.Raycast(world+Vector3.up*1.5f,Vector3.down,out var hit,7f,1<<layer,QueryTriggerInteraction.Ignore))return false;
            level=hit.point.y;
            return true;
        }

        public bool TryGroundHeight(MarkerAnchor anchor,out double correction) {
            correction=0;var basis=anchor.Clone();basis.Height=0;
            if(!geometry.Resolve(basis,out var position,out var rotation,out _))return false;
            var world=position.GameToWorld();float? highest=null;
            float radius=.35f*(float)anchor.Scale;
            foreach(float x in new[]{-radius,radius})foreach(float z in new[]{-radius,radius}) {
                if(!Surface(world+new Vector3(x,0,z),out float level))return false;
                highest=highest.HasValue?Mathf.Max(highest.Value,level):level;
            }
            float up=(rotation*Vector3.up).y;
            if(up<.5f)return false;
            correction=(highest.Value+.03-world.y)/up;
            return true;
        }

        static void SetGroundPose(GameObject root,MarkerAnchor anchor) {
            if(anchor.FixedHeight)return;
            var world=root.transform.position;
            var basis=world-root.transform.rotation*Vector3.up*(float)anchor.Height;
            float? surface=Surface(basis,out float level)?level:(float?)null;
            world.y=(float)MarkerGroundPlacement.Level(basis.y,anchor.Height,surface,false);
            root.transform.position=world;
        }

        void PoseGroundSupport(GameObject root,MarkerAnchor anchor,bool ghost=false) {
            var state=root.GetComponent<MarkerGroundSupport>();
            if(!anchor.GroundSupport){if(state!=null)state.Foot.SetActive(false);return;}
            var top=root.transform.position;
            float width=.7f*(float)anchor.Scale;
            float? lowest=null;
            foreach(float x in new[]{-width/2,width/2})foreach(float z in new[]{-width/2,width/2}) {
                if(!Surface(top+new Vector3(x,0,z),out float level)){if(state!=null)state.Foot.SetActive(false);return;}
                lowest=lowest.HasValue?Mathf.Min(lowest.Value,level):level;
            }
            float depth=(float)MarkerGroundPlacement.SupportDepth(top.y,lowest);
            if(depth<=0){if(state!=null)state.Foot.SetActive(false);return;}
            if(state==null) {
                if(supportMesh==null)supportMesh=BuildMesh(MarkerMeshData.CreateGroundSupport(),"EnusDE adjustable support");
                state=root.AddComponent<MarkerGroundSupport>();
                state.Foot=new GameObject("EnusDE Geländeunterbau"){layer=2};
                state.Foot.transform.SetParent(root.transform,false);
                state.Foot.AddComponent<MeshFilter>().sharedMesh=supportMesh;
                state.Foot.AddComponent<MeshRenderer>().sharedMaterials=new[]{ghost?blue:gray,black,gray};
            }
            var foot=state.Foot.transform;
            foot.SetPositionAndRotation(top,Quaternion.identity);
            var scale=root.transform.lossyScale;
            foot.localScale=new Vector3(width/scale.x,depth/scale.y,width/scale.z);
            state.Foot.SetActive(true);
        }

        void BuildFoundation(GameObject root,bool ghost) {
            if(foundationMesh==null)foundationMesh=BuildMesh(MarkerMeshData.CreateFoundation(),"EnusDE mast concrete foot");
            var foot=new GameObject("EnusDE mast foundation"){layer=2};
            foot.transform.SetParent(root.transform,false);
            foot.AddComponent<MeshFilter>().sharedMesh=foundationMesh;
            var renderer=foot.AddComponent<MeshRenderer>();
            renderer.sharedMaterials=new[]{ghost?blue:gray,black,gray};
        }

        GameObject Box(GameObject parent,int variant,bool ghost,string letter=null) {
            var box=new GameObject(variant==1?"EnusDE GFM A":"EnusDE GFM B"){layer=2};
            box.transform.SetParent(parent.transform,false);
            box.AddComponent<MeshFilter>().sharedMesh=variant==1?circuitAMesh:variant==3?circuitCMesh:circuitBMesh;
            var renderer=box.AddComponent<MeshRenderer>();
            renderer.sharedMaterials=ghost?new[]{blue,white,blue}:new[]{circuitBlack,circuitWhite,circuitGray};
            signals.LabelCircuitBox(box,letter??(variant==1?"A":variant==3?"C":"B"));
            return box;
        }

        void BuildCircuitVisual(GameObject root,MarkerRecord row) {
            if(!CircuitIdentity.Enabled(row))return;
            var state=root.AddComponent<CircuitVisual>();
            state.Variant=CircuitIdentity.Variant(row);
            state.Box=row.IsCircuitBox?root:Box(root,state.Variant,false,row.SharedPointBoundary?"A/B":null);
            if(!row.IsCircuitBox)MarkerPickable.Attach(state.Box,row,true);
            state.CodeLabel=signals.LabelCircuitCode(state.Box,CircuitIdentity.Caption(row));
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

        static MarkerAnchor CircuitSupportAnchor(MarkerRecord row) {
            var anchor=row.Anchor.Clone();anchor.GroundSupport=row.CircuitSupport;return anchor;
        }
        readonly System.Collections.Generic.Dictionary<string,string> circuitWarnings=new System.Collections.Generic.Dictionary<string,string>();
        public string CircuitWarning(string id)=>circuitWarnings.TryGetValue(id,out var value)?value:null;
        bool ClearCircuitPosition(Vector3 position,float scale,GameObject box) {
            var bounds=new Bounds(position+Vector3.up*(.18f*scale),new Vector3(.56f,.44f,.5f)*scale);
            foreach(var other in objects.Values)foreach(var renderer in other.GetComponentsInChildren<MeshRenderer>())
                if(renderer.enabled&&!renderer.transform.IsChildOf(box.transform)&&bounds.Intersects(renderer.bounds))return false;
            return true;
        }
        void PoseCircuit(GameObject root,MarkerRecord row) {
            var state=root.GetComponent<CircuitVisual>();
            if(state==null)return;
            string code=CircuitIdentity.Caption(row);
            if(state.CodeLabel!=null&&state.CodeLabel.text!=code)state.CodeLabel.text=code;
            var rail=(row.CircuitAnchor??row.Anchor).Clone();
            float side=row.Anchor.Lateral<0?-1:1;
            rail.Lateral=.78f*side;
            rail.Height=-.075;
            if(!geometry.Resolve(rail,out var onRail,out var along,out _)) {
                state.Cable.enabled=false;state.Terminal.gameObject.SetActive(false);return;
            }
            var end=onRail.GameToWorld();
            if(state.Box!=root) {
                var baseAnchor=row.Anchor.Clone();
                baseAnchor.Height=row.Anchor.FixedHeight?row.Anchor.Height:0;
                var lateral=CircuitPlacement.AttachedLateral(row.Anchor.Lateral,row.Anchor.Scale);bool placed=false;
                baseAnchor.GroundSupport=row.CircuitSupport;
                if(lateral.HasValue)foreach(double shift in new[]{0d,.65,-.65,1.3,-1.3}) {
                    baseAnchor.Lateral=lateral.Value;baseAnchor.Meters=row.Anchor.Meters+shift;
                    if(baseAnchor.Meters<0||!geometry.Resolve(baseAnchor,out var box,out _,out _))continue;
                    var position=box.GameToWorld()+along*Vector3.forward*(float)row.CircuitOffset;
                    if(!ClearCircuitPosition(position,(float)row.Anchor.Scale,state.Box))continue;
                    state.Box.SetActive(true);state.Box.transform.position=position;
                    state.Box.transform.rotation=along*Quaternion.Euler(0,(float)CircuitIdentity.FacingDegrees(row),0);
                    SetGroundPose(state.Box,baseAnchor);
                    PoseGroundSupport(state.Box,baseAnchor);
                    placed=true;break;
                }
                if(!placed){state.Box.SetActive(false);state.Cable.enabled=false;state.Terminal.gameObject.SetActive(false);circuitWarnings[row.Id]=LocalText.Select("Detection box not drawn: no clear position beside this signal. Adjust signal position, scale or box offset.","GFM-Kasten nicht gezeichnet: kein freier Platz neben diesem Signal. Signalposition, Größe oder Kastenversatz anpassen.");return;}
                circuitWarnings.Remove(row.Id);
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
            if(supportMesh!=null)UnityEngine.Object.Destroy(supportMesh);
            if(terminalMesh!=null)UnityEngine.Object.Destroy(terminalMesh);
            if(cableMaterial!=null)UnityEngine.Object.Destroy(cableMaterial);
        }
    }

    public sealed class MarkerGroundSupport : MonoBehaviour {public GameObject Foot;}

    public sealed class CircuitVisual : MonoBehaviour {
        public int Variant;
        public TMPro.TextMeshPro CodeLabel;
        public GameObject Box;
        public LineRenderer Cable;
        public Transform Terminal;
        public readonly Vector3[] Points=new Vector3[7];
    }
}
