using System;
using System.Collections.Generic;
using Track;
using End=Track.TrackSegment.End;
using Helpers;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using UnityEngine.InputSystem;

namespace EnusDE.Signaltechnik {
    public sealed class MarkerTrackGeometry {
        public bool Resolve(MarkerAnchor anchor,out Vector3 gamePosition,out Quaternion rotation,out string reason) {
            gamePosition=Vector3.zero;rotation=Quaternion.identity;reason=null;
            try {
                anchor.Validate();var graph=Graph.Shared;if(graph==null){reason="Gleisnetz noch nicht bereit.";return false;}
                var segment=graph.GetSegment(anchor.Segment);
                if(segment==null){reason="Gleissegment fehlt.";return false;}
                if(segment.turntable!=null){reason="Drehscheiben sind im P0-Test noch ausgeschlossen.";return false;}
                double length=segment.GetLength();if(double.IsNaN(length)||double.IsInfinity(length)||length<=0||anchor.Meters>length+.001){reason="Position liegt außerhalb eines gültigen Gleissegments.";return false;}
                var location=new Location(segment,(float)Math.Min(anchor.Meters,length),End.A);
                var pose=graph.GetPositionRotation(location,PositionAccuracy.High);
                rotation=pose.Rotation;
                gamePosition=pose.Position+rotation*new Vector3((float)anchor.Lateral,(float)anchor.Height,0);
                if(!Finite(gamePosition)){reason="Gleis liefert keine gültige Position.";return false;}
                return true;
            }catch(Exception e){reason=e.Message;return false;}
        }
        public bool Snap(Vector3 worldHit,MarkerOptions options,out MarkerAnchor anchor,out string reason) {
            anchor=null;reason=null;
            var graph=Graph.Shared;Location location;
            if(graph==null||!graph.TryGetLocationFromWorldPoint(worldHit,2,out location)||!location.IsValid){reason="Kein Gleis innerhalb von 2 m am Zielpunkt.";return false;}
            var canonical=location.WithEnd(End.A);
            anchor=new MarkerAnchor{Segment=canonical.segment.id,Meters=canonical.distance,Lateral=options.Lateral,Height=options.Height,Scale=options.Scale};
            Vector3 p;Quaternion q;return Resolve(anchor,out p,out q,out reason);
        }
        public bool Aim(MarkerOptions options,out MarkerAnchor anchor,out string reason) {
            anchor=null;reason=null;
            var camera=Camera.main;
            if(camera==null){reason="Aktive Kamera nicht verfügbar.";return false;}
            Ray ray=camera.ViewportPointToRay(new Vector3(.5f,.5f,0));RaycastHit hit;
            if(!Physics.Raycast(ray,out hit,1500,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){reason="Blickmitte trifft keine Oberfläche.";return false;}
            return Snap(hit.point,options,out anchor,out reason);
        }
        public Vector3[] PreviewLine(MarkerAnchor anchor) {
            var graph=Graph.Shared;var segment=graph.GetSegment(anchor.Segment);float length=segment.GetLength();var points=new Vector3[17];
            for(int i=0;i<points.Length;i++) {
                double distance=Math.Max(0,Math.Min(length,anchor.Meters-3+i*6d/(points.Length-1)));
                points[i]=(graph.GetPosition(new Location(segment,(float)distance,End.A))+Vector3.up*.035f).GameToWorld();
            }
            return points;
        }
        public bool Pointer(MarkerOptions options,out MarkerAnchor anchor,out string reason) {
            anchor=null;reason=null;var camera=Camera.main;var mouse=Mouse.current;
            if(camera==null||mouse==null){reason="Kamera oder Maus nicht verfügbar.";return false;}
            var point=mouse.position.ReadValue();var ray=Cursor.lockState==CursorLockMode.Locked?camera.ViewportPointToRay(new Vector3(.5f,.5f,0)):camera.ScreenPointToRay(new Vector3(point.x,point.y,0));
            int mask=Physics.DefaultRaycastLayers&~(1<<Helpers.Layers.Clickable);
            RaycastHit hit;if(!Physics.Raycast(ray,out hit,1500,mask,QueryTriggerInteraction.Ignore)){reason="Maus zeigt auf keine Oberfläche.";return false;}
            return Snap(hit.point,options,out anchor,out reason);
        }
        static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
    }
    public sealed partial class MarkerScene : IDisposable {
        readonly MarkerTrackGeometry geometry;
        readonly Transform owner;
        readonly Dictionary<string,GameObject> objects=new Dictionary<string,GameObject>();
        readonly Dictionary<string,int> revisions=new Dictionary<string,int>();
        readonly Dictionary<GameObject,WorldTransformer> movers=new Dictionary<GameObject,WorldTransformer>();
        Mesh mesh;
        Mesh speedMesh,circuitAMesh,circuitBMesh,circuitCMesh;
        Material white,red,gray,blue;
        Material black;
        Material circuitWhite,circuitBlack,circuitGray;
        GameObject preview;
        string previewKind;
        readonly Dictionary<GameObject,TextMeshPro[]> captions=new Dictionary<GameObject,TextMeshPro[]>();
        readonly SignalModels signals=new SignalModels();
        public string GlowStatus=>signals.GlowStatus;
        public void ApplyGlow(SignalGlowOptions options)=>signals.ApplyGlow(options);
        public void GlowView(Vector3? position)=>signals.View(position);
        LineRenderer line;
        public int VisibleCount=>objects.Count;
        public MarkerScene(MarkerTrackGeometry geometry,Transform owner=null){this.geometry=geometry;this.owner=owner;}
        void Resources() {
            if(mesh!=null)return;
            Shader shader=ShaderPolicy.Find<Shader>(Shader.Find);
            if(shader==null)throw new InvalidOperationException("Kein geeigneter Darstellungs-Shader im Spiel verfügbar.");
            white=ColorMaterial(shader,Color.white);red=ColorMaterial(shader,new Color(.9f,.03f,.02f));gray=ColorMaterial(shader,new Color(.65f,.65f,.62f));blue=ColorMaterial(shader,new Color(.05f,.65f,1f));black=ColorMaterial(shader,Color.black);
            var paintShader=ShaderPolicy.FindLit<Shader>(Shader.Find);
            circuitWhite=PaintMaterial(paintShader??shader,paintShader==null?new Color(.25f,.25f,.25f):new Color(.8f,.8f,.77f));
            circuitBlack=PaintMaterial(paintShader??shader,new Color(.025f,.025f,.025f));circuitGray=PaintMaterial(paintShader??shader,new Color(.45f,.45f,.43f));
            mesh=BuildMesh(MarkerMeshData.Create(),"EnusDE original Ra12 prototype");
            speedMesh=BuildMesh(MarkerMeshData.CreateSpeedBoard(),"EnusDE original speed board prototype");
            circuitAMesh=BuildMesh(MarkerMeshData.CreateCircuitBox(false),"EnusDE track circuit box A");
            circuitBMesh=BuildMesh(MarkerMeshData.CreateCircuitBox(true),"EnusDE track circuit box B");
            circuitCMesh=BuildMesh(MarkerMeshData.CreateCircuitBoxC(),"EnusDE track circuit box C");
        }
        static Mesh BuildMesh(MarkerMeshData data,string name) {
            var vertices=new Vector3[data.Vertices.Count];
            for(int i=0;i<vertices.Length;i++){var p=data.Vertices[i];vertices[i]=new Vector3(p[0],p[1],p[2]);}
            var result=new Mesh{name=name};result.vertices=vertices;result.subMeshCount=3;
            for(int i=0;i<3;i++)result.SetTriangles(data.Triangles[i],i);
            result.RecalculateNormals();result.RecalculateBounds();return result;
        }
        static Material ColorMaterial(Shader shader,Color color) {
            var m=new Material(shader);if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);if(m.HasProperty("_UnlitColor"))m.SetColor("_UnlitColor",color);return m;
        }
        static Material PaintMaterial(Shader shader,Color color) {
            var value=ColorMaterial(shader,color);value.DisableKeyword("_EMISSION");
            foreach(string key in new[]{"_EmissionColor","_EmissiveColor"})if(value.HasProperty(key))value.SetColor(key,Color.black);
            foreach(string key in new[]{"_Metallic","_Smoothness","_Glossiness","_EmissiveIntensity"})if(value.HasProperty(key))value.SetFloat(key,0);
            return value;
        }
        GameObject Create(string name,bool ghost,string kind,bool low=false,bool hl=false) {
            Resources();var root=new GameObject(name);root.layer=2;
            if(owner!=null)root.transform.SetParent(owner,false);
            if(kind==MarkerRecord.Main||kind==MarkerRecord.Distant||kind==MarkerRecord.Shunt) {
                if(kind==MarkerRecord.Main){signals.BuildMast(root,ghost,low);BuildFoundation(root,ghost);}else signals.Build(root,kind,ghost,false,true,false,hl);WorldTransformer transformer;if(WorldTransformer.TryGetShared(out transformer)){transformer.AddObjectToMove(root.transform);movers[root]=transformer;}return root;
            }
            bool circuit=MarkerRecord.IsCircuitKind(kind);
            bool speed=kind==MarkerRecord.Speed;
            root.AddComponent<MeshFilter>().sharedMesh=circuit?(kind==MarkerRecord.CircuitC?circuitCMesh:circuitBMesh):speed?speedMesh:mesh;
            var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterials=ghost?new[]{blue,white,circuit?blue:gray}:circuit?new[]{circuitBlack,circuitWhite,circuitGray}:new[]{white,speed?black:red,gray};
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(circuit)signals.LabelCircuitBox(root,kind==MarkerRecord.CircuitC?"C":"B");
            if(speed) {
                var texts=new TextMeshPro[2];
                for(int i=0;i<2;i++) {
                    var face=new GameObject("EnusDE speed caption");face.layer=2;face.transform.SetParent(root.transform,false);
                    face.transform.localPosition=new Vector3(0,1.75f,i==0?-.032f:.032f);face.transform.localRotation=Quaternion.LookRotation(i==0?Vector3.forward:Vector3.back,Vector3.up);face.transform.localScale=Vector3.one*.1f;
                    var text=face.AddComponent<TextMeshPro>();text.font=TMP_Settings.defaultFontAsset;text.color=Color.black;text.alignment=TextAlignmentOptions.Center;
                    text.rectTransform.sizeDelta=new Vector2(6,5.5f);text.fontSize=28;text.enableAutoSizing=true;text.fontSizeMin=10;text.fontSizeMax=28;text.textWrappingMode=TextWrappingModes.NoWrap;
                    texts[i]=text;
                }
                captions[root]=texts;
            }
            WorldTransformer mover;if(WorldTransformer.TryGetShared(out mover)){mover.AddObjectToMove(root.transform);movers[root]=mover;}
            return root;
        }
        void Pose(GameObject root,MarkerAnchor anchor,Vector3 gamePosition,Quaternion rotation) {
            root.transform.position=gamePosition.GameToWorld();root.transform.rotation=rotation;root.transform.localScale=Vector3.one*(float)anchor.Scale;root.SetActive(true);
        }
        public bool Show(MarkerRecord record,out string reason,string unitMode="original") {
            Vector3 p;Quaternion q;GameObject root;
            if(!geometry.Resolve(record.Anchor,out p,out q,out reason)){if(objects.TryGetValue(record.Id,out root))root.SetActive(false);return false;}
            int revision;if(objects.TryGetValue(record.Id,out root)&&revisions.TryGetValue(record.Id,out revision)&&(revision!=record.VisualRevision||root.GetComponent<CircuitVisual>() is CircuitVisual circuit&&circuit.Variant!=CircuitIdentity.Variant(record))){Remove(record.Id);root=null;}
            if(!objects.TryGetValue(record.Id,out root)){root=Create("EnusDE "+record.Name,record.Planned,record.Kind,record.LowMain,record.HlSignal);objects[record.Id]=root;revisions[record.Id]=record.VisualRevision;MarkerPickable.Attach(root,record);if(record.IsCircuitBox||record.IsSignal)BuildCircuitVisual(root,record);}
            Pose(root,record.Anchor,p,q);if(record.IsCircuitBox)root.transform.rotation=q*Quaternion.Euler(0,(float)CircuitIdentity.FacingDegrees(record),0);if(record.IsSignal||record.IsCircuitBox)SetGroundPose(root,record.Anchor);if(record.Kind==MarkerRecord.Main)signals.ShowMast(root,record);else if(record.IsSignal)signals.Show(root,record,Entry.SignalAspect(record.Id));else Caption(root,record.SpeedMph,unitMode);if(record.IsSignal||record.IsCircuitBox)PoseGroundSupport(root,record.IsCircuitBox?CircuitSupportAnchor(record):record.Anchor);PoseCircuit(root,record);return true;
        }
        void Caption(GameObject root,double mph,string mode) {
            TextMeshPro[] texts;if(!captions.TryGetValue(root,out texts))return;
            string label=SignUnits.Caption(mph,mode);foreach(var text in texts)if(text!=null&&text.text!=label)text.text=label;
        }
        public void ShowPreview(MarkerAnchor anchor,string kind=MarkerRecord.Clearance,double mph=20,string unitMode="original",bool reverse=false,double circuitYaw=0,bool hl=false) {
            Vector3 p;Quaternion q;string reason;
            if(anchor==null||!geometry.Resolve(anchor,out p,out q,out reason)){HidePreview();return;}
            string modelKey=kind+(hl?":hl":"");
            if(preview!=null&&previewKind!=modelKey){DestroyObject(preview);preview=null;line=null;}
            if(preview==null) {
                preview=Create("EnusDE object preview",true,kind,false,hl);previewKind=modelKey;
                var lines=new GameObject("EnusDE track preview line");lines.layer=2;lines.transform.SetParent(preview.transform,false);
                line=lines.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=blue;line.startWidth=line.endWidth=.035f;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            }
            Pose(preview,anchor,p,q);if(MarkerRecord.IsCircuitKind(kind))preview.transform.rotation=q*Quaternion.Euler(0,(reverse?180:0)+(float)circuitYaw,0);if(kind==MarkerRecord.Main||kind==MarkerRecord.Shunt||kind==MarkerRecord.Distant||MarkerRecord.IsCircuitKind(kind))SetGroundPose(preview,anchor);if(kind==MarkerRecord.Main)signals.ShowMast(preview,new MarkerRecord{Id="00000000000000000000000000000001",Name="VORSCHAU",Kind=kind,FacingReverse=reverse,HlSignal=hl});else if(kind==MarkerRecord.Distant||kind==MarkerRecord.Shunt)signals.Show(preview,new MarkerRecord{Name="VORSCHAU",Kind=kind,FacingReverse=reverse},LocalSignalAspect.Stop);else Caption(preview,mph,unitMode);if(kind==MarkerRecord.Main||kind==MarkerRecord.Shunt||kind==MarkerRecord.Distant||MarkerRecord.IsCircuitKind(kind))PoseGroundSupport(preview,anchor,true);var points=geometry.PreviewLine(anchor);
            // Keep the frozen line attached when the game shifts its floating origin.
            for(int i=0;i<points.Length;i++)points[i]=line.transform.InverseTransformPoint(points[i]);
            line.positionCount=points.Length;line.SetPositions(points);
        }
        public void HidePreview(){if(preview!=null)preview.SetActive(false);}
        public void Hide(string id){GameObject root;if(id!=null&&objects.TryGetValue(id,out root)&&root!=null)root.SetActive(false);}
        public void SetVisible(bool visible){foreach(var value in objects.Values)if(value!=null)value.SetActive(visible);if(!visible)HidePreview();}
        void DestroyObject(GameObject root) {
            if(root==null)return;WorldTransformer mover;
            try{if(movers.TryGetValue(root,out mover)&&mover!=null)mover.RemoveObjectToMove(root.transform);}catch{}
            finally{movers.Remove(root);captions.Remove(root);UnityEngine.Object.Destroy(root);}
        }
        public void Remove(string id){GameObject root;if(objects.TryGetValue(id,out root)){DestroyObject(root);objects.Remove(id);revisions.Remove(id);}}
        public void Dispose() {
            signals.Dispose();DisposeGround();
            foreach(var root in objects.Values)DestroyObject(root);objects.Clear();DestroyObject(preview);preview=null;line=null;
            foreach(var material in new[]{white,red,gray,blue,black,circuitWhite,circuitBlack,circuitGray})if(material!=null)UnityEngine.Object.Destroy(material);
            if(mesh!=null)UnityEngine.Object.Destroy(mesh);if(speedMesh!=null)UnityEngine.Object.Destroy(speedMesh);if(circuitAMesh!=null)UnityEngine.Object.Destroy(circuitAMesh);if(circuitBMesh!=null)UnityEngine.Object.Destroy(circuitBMesh);
            if(circuitCMesh!=null)UnityEngine.Object.Destroy(circuitCMesh);
            mesh=speedMesh=circuitAMesh=circuitBMesh=circuitCMesh=null;white=red=gray=blue=black=circuitWhite=circuitBlack=circuitGray=null;
        }
    }
}
