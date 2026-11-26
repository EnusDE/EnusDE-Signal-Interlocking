using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Track;
using Helpers;
using UnityEngine;
namespace EnusDE.Signaltechnik {
    // Local build aid. No occupancy reader or operating writer; colours identify sections only.
    public sealed class GfmWorldOverlay:IDisposable {
        public static readonly Color[] Colours={new Color(.1f,.8f,1),new Color(1,.45f,.1f),new Color(.7f,.3f,1),new Color(.3f,1,.55f),new Color(1,.35f,.65f),new Color(1,.85f,.2f),new Color(.3f,.5f,1),new Color(.8f,1,.4f)};
        const int VisibleLimit=3000,PoolLimit=128;
        const float Range=400;
        const double SliceSeconds=.001;
        const string ViewJob="gfm-view";

        internal readonly struct Piece:IComparable<Piece> {
            public readonly long Key;
            public readonly string Id;
            public readonly Vector3 A,B;
            public readonly bool Post,Selected;
            public readonly float Distance;
            public Piece(long key,string id,Vector3 a,Vector3 b,bool post,bool selected,float distance){
                Key=key;Id=id;A=a;B=b;Post=post;Selected=selected;Distance=distance;
            }
            public int CompareTo(Piece other) {
                int order=other.Selected.CompareTo(Selected);
                if(order==0)order=Distance.CompareTo(other.Distance);
                return order==0?Key.CompareTo(other.Key):order;
            }
        }

        sealed class Visual {
            public GameObject Object;
            public LineRenderer Line;
            public BoxCollider Collider;
            public GfmWorldPickable Pickable;
            public Piece Piece;
        }

        readonly FrameWorkQueue work=new FrameWorkQueue();
        readonly List<Material> materials=new List<Material>();
        readonly Dictionary<long,Visual> visible=new Dictionary<long,Visual>();
        readonly Stack<Visual> spare=new Stack<Visual>();
        readonly Dictionary<string,int> pieceCounts=new Dictionary<string,int>(StringComparer.Ordinal);
        GameObject root;
        Vector3 origin;
        double started,worstSlice;
        int added,allocated,reused,removed,slices;
        public int Drawn=>pieceCounts.Count;
        public int Skipped {get;private set;}
        public bool Busy=>work.Count>0;
        public string Error {get;private set;}

        public void FollowOrigin(Vector3 value) {
            if(origin==value)return;
            origin=value;
            if(root!=null)root.transform.position=value;
        }

        public void Begin(Graph graph,TrackBlock[] blocks,IReadOnlyDictionary<string,int> palette,Vector3 camera,string selected,Action<string> pick) {
            work.Cancel(ViewJob);
            Error=null;started=Now();worstSlice=0;added=allocated=reused=removed=slices=0;
            work.Enqueue(ViewJob,()=>Populate(graph,blocks,palette,camera,selected,pick).GetEnumerator(),null,error=>{
                Dispose();
                Error=error.Message;
            });
        }

        static double Now()=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
        public void Step() {
            if(!Busy)return;
            double before=Now();
            work.Advance(SliceSeconds,128);
            worstSlice=Math.Max(worstSlice,Now()-before);slices++;
            if(!Busy&&Error==null&&Entry.WantsSignalEvents)
                Entry.RecordSignalEvent("gfm_overlay_refresh",new{sections=Drawn,pieces=visible.Count,added,allocated,reused,removed,slices,
                    elapsed_ms=(Now()-started)*1000,max_slice_ms=worstSlice*1000});
        }

        IEnumerable<bool> Populate(Graph graph,TrackBlock[] blocks,IReadOnlyDictionary<string,int> palette,Vector3 camera,string selected,Action<string> pick) {
            var nearest=new SortedSet<Piece>();
            int spanIndex=0,skipped=0;
            foreach(var block in blocks) {
                bool highlight=block.Id==selected;
                foreach(var section in block.Sections) {
                    long key=(long)spanIndex++<<32;
                    var segment=graph.GetSegment(section.Segment);
                    if(!Usable(segment,section.To)) {skipped++;yield return true;continue;}
                    double length=section.To-section.From;
                    var middle=Position(graph,segment,section.From+length/2);
                    // Track distance bounds the distance from the midpoint to every sample.
                    float reach=Range+(float)(length/2)+1;
                    if((middle-camera).sqrMagnitude>reach*reach){yield return true;continue;}
                    int samples=Math.Max(1,(int)Math.Ceiling(length/5));
                    var a=Position(graph,segment,section.From)+Vector3.up*.18f;
                    for(int i=0;i<samples;i++) {
                        var b=Position(graph,segment,section.From+length*(i+1)/samples)+Vector3.up*.18f;
                        Add(key+i,block.Id,a,b,false,highlight);
                        a=b;
                        yield return true;
                    }
                }
                if(block.Path.Count==0)continue;
                AddPost(block.Path[0],true);
                yield return true;
                AddPost(block.Path[block.Path.Count-1],false);
                yield return true;

                void AddPost(DirectedRouteSection part,bool first) {
                    long key=(long)spanIndex++<<32;
                    var span=part.Section;var segment=graph.GetSegment(span.Segment);
                    double at=first!=part.Reverse?span.From:span.To;
                    if(!Usable(segment,at))return;
                    var position=Position(graph,segment,at);
                    Add(key,block.Id,position+Vector3.up*.15f,position+Vector3.up*1.4f,true,highlight);
                }
            }

            var desired=new HashSet<long>();
            foreach(var piece in nearest){desired.Add(piece.Key);yield return true;}
            foreach(long key in visible.Keys.ToArray()) {
                if(!desired.Contains(key))Remove(key);
                yield return true;
            }
            if(nearest.Count>0)foreach(bool step in PrepareMaterials())yield return step;
            foreach(var piece in nearest) {
                Show(piece,materials[palette[piece.Id]],pick);
                yield return true;
            }
            Skipped=skipped;

            void Add(long key,string id,Vector3 a,Vector3 b,bool post,bool highlight) {
                float distance=((a+b)*.5f-camera).sqrMagnitude;
                if(!(distance<=Range*Range)||(b-a).sqrMagnitude<.000001f)return;
                nearest.Add(new Piece(key,id,a,b,post,highlight,distance));
                if(nearest.Count>VisibleLimit){nearest.Remove(nearest.Max);skipped++;}
            }
        }

        static bool Usable(TrackSegment segment,double at)=>segment!=null&&segment.Available&&segment.GroupEnabled&&segment.turntable==null&&at<=segment.GetLength()+.001;
        static Vector3 Position(Graph graph,TrackSegment segment,double at)=>graph.GetPosition(new Location(segment,(float)at,TrackSegment.End.A));

        IEnumerable<bool> PrepareMaterials() {
            if(materials.Count==Colours.Length)yield break;
            if(root==null) {
                root=new GameObject("EnusDE GFM build overlay");
                root.transform.position=origin;
            }
            var shader=ShaderPolicy.Find<Shader>(Shader.Find);
            if(shader==null)throw new InvalidOperationException(LocalText.Select("No compatible overlay shader.","Kein kompatibler Shader für die Abschnittsanzeige."));
            var properties=new[]{"_BaseColor","_Color","_UnlitColor"};
            for(int i=materials.Count;i<Colours.Length;i++) {
                var material=new Material(shader);
                foreach(string key in properties)if(material.HasProperty(key))material.SetColor(key,Colours[i]);
                materials.Add(material);
                yield return true;
            }
        }

        Visual Create() {
            allocated++;
            var go=new GameObject("GFM section");
            go.SetActive(false);
            go.layer=Helpers.Layers.Clickable;
            go.transform.SetParent(root.transform,false);
            var line=go.AddComponent<LineRenderer>();
            line.useWorldSpace=false;line.positionCount=2;
            var collider=go.AddComponent<BoxCollider>();
            collider.isTrigger=true;
            return new Visual{Object=go,Line=line,Collider=collider,Pickable=go.AddComponent<GfmWorldPickable>()};
        }

        void Show(Piece piece,Material paint,Action<string> pick) {
            bool existing=visible.TryGetValue(piece.Key,out var visual);
            if(existing)reused++;
            else {
                visual=spare.Count>0?spare.Pop():Create();
                visible.Add(piece.Key,visual);
                pieceCounts.TryGetValue(piece.Id,out int count);pieceCounts[piece.Id]=count+1;
                visual.Line.sharedMaterial=paint;
                visual.Pickable.Id=piece.Id;visual.Pickable.Pick=pick;
                added++;
            }
            if(!existing||visual.Piece.A!=piece.A||visual.Piece.B!=piece.B) {
                var delta=piece.B-piece.A;
                float length=delta.magnitude;
                visual.Object.transform.localPosition=(piece.A+piece.B)*.5f;
                visual.Object.transform.localRotation=Quaternion.LookRotation(delta,piece.Post?Vector3.forward:Vector3.up);
                visual.Line.SetPosition(0,Vector3.back*(length*.5f));
                visual.Line.SetPosition(1,Vector3.forward*(length*.5f));
                visual.Collider.size=new Vector3(.65f,.4f,Math.Max(.1f,length));
            }
            if(!existing||visual.Piece.Selected!=piece.Selected) {
                float width=piece.Post ? .14f : piece.Selected ? .20f : .10f;
                visual.Line.startWidth=visual.Line.endWidth=width;
            }
            visual.Piece=piece;
            if(!existing)visual.Object.SetActive(true);
        }

        void Remove(long key) {
            var visual=visible[key];visible.Remove(key);
            string id=visual.Piece.Id;
            if(pieceCounts[id]==1)pieceCounts.Remove(id);else pieceCounts[id]--;
            visual.Object.SetActive(false);
            visual.Pickable.Pick=null;
            if(spare.Count<PoolLimit)spare.Push(visual);
            else UnityEngine.Object.Destroy(visual.Object);
            removed++;
        }

        public void Dispose() {
            work.Clear();
            if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);root=null;}
            foreach(var material in materials)UnityEngine.Object.Destroy(material);
            materials.Clear();visible.Clear();spare.Clear();pieceCounts.Clear();
            Skipped=0;Error=null;
        }
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
