using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    // Dashed lines show presentation links between linked modules. They are never route or occupancy indications.
    internal sealed class MasterLinkGraphic:MaskableGraphic {
        PanelView board;
        PanelPoint[][] paths=Array.Empty<PanelPoint[]>();
        float lastZoom;

        internal static void Attach(PanelView view,DeskLibrary library,string master,bool includeHidden) {
            var connectors=BuildPaths(library,master,includeHidden).ToArray();
            if(connectors.Length==0)return;
            var rect=WorkspaceSurface.Rect(view.transform,"Master line links");
            WorkspaceSurface.Fill(rect);rect.pivot=new Vector2(0,1);rect.SetAsFirstSibling();
            var overlay=rect.gameObject.AddComponent<MasterLinkGraphic>();
            overlay.board=view;overlay.paths=connectors;overlay.raycastTarget=false;
        }
        internal static IEnumerable<PanelPoint[]> BuildPaths(DeskLibrary library,string master,bool includeHidden) {
            var modules=library.Modules.Where(m=>MasterDiagram.Owner(library,m)==master).ToDictionary(m=>m.Desk);
            foreach(var line in library.FreeLines.Where(l=>l.Enabled)) {
                if(!modules.TryGetValue(line.StationA,out var a)||!modules.TryGetValue(line.StationB,out var b))continue;
                var da=library.Desks.First(d=>d.Id==a.Desk);
                var db=library.Desks.First(d=>d.Id==b.Desk);
                if(!includeHidden&&(!da.ShowInDispatcher||!db.ShowInDispatcher))continue;
                var start=da.Panel.Tiles.FirstOrDefault(t=>t.Kind==PanelTileKind.Signal&&!t.ReadOnlySignal&&t.Reference==line.ForwardStart);
                var end=db.Panel.Tiles.FirstOrDefault(t=>t.Kind==PanelTileKind.Signal&&!t.ReadOnlySignal&&t.Reference==line.ForwardEnd);
                if(start==null||end==null)continue;
                float ay=a.Y+start.Y+.5f,by=b.Y+end.Y+.5f;
                float ax=a.X+start.X+.5f,bx=b.X+end.X+.5f;
                bool toRight=a.X+da.Panel.Columns<=b.X,toLeft=b.X+db.Panel.Columns<=a.X;
                bool below=a.Y+da.Panel.Rows<=b.Y,above=b.Y+db.Panel.Rows<=a.Y;
                float horizontal=toRight?b.X-(a.X+da.Panel.Columns):toLeft?a.X-(b.X+db.Panel.Columns):-1;
                float vertical=below?b.Y-(a.Y+da.Panel.Rows):above?a.Y-(b.Y+db.Panel.Rows):-1;
                if(horizontal<0&&vertical<0)continue;
                if(horizontal>=vertical) {
                    ax=toRight?a.X+da.Panel.Columns:a.X;
                    bx=toRight?b.X:b.X+db.Panel.Columns;
                    float mid=(ax+bx)*.5f;
                    yield return new[]{new PanelPoint(ax,ay),new PanelPoint(mid,ay),new PanelPoint(mid,by),new PanelPoint(bx,by)};
                } else {
                    ay=below?a.Y+da.Panel.Rows:a.Y;
                    by=below?b.Y:b.Y+db.Panel.Rows;
                    float mid=(ay+by)*.5f;
                    yield return new[]{new PanelPoint(ax,ay),new PanelPoint(ax,mid),new PanelPoint(bx,mid),new PanelPoint(bx,by)};
                }
            }
        }
        void LateUpdate(){if(board!=null&&lastZoom!=board.Zoom){lastZoom=board.Zoom;SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper output) {
            output.Clear();
            if(board==null)return;
            var color=new Color(.28f,.65f,.80f,.63f);
            foreach(var path in paths)for(int i=1;i<path.Length;i++){
                Vector2 a=Pixels(path[i-1]),b=Pixels(path[i]);
                float length=Vector2.Distance(a,b);
                if(length<.01f)continue;
                float dash=Math.Max(8,length/48);
                for(float start=0;start<length;start+=2*dash)
                    PanelChunk.Line(output,Vector2.LerpUnclamped(a,b,start/length),Vector2.LerpUnclamped(a,b,Math.Min(1,(start+dash)/length)),1.5f,color);
            }
        }
        Vector2 Pixels(PanelPoint point)=>new Vector2(point.X*PanelGeometry.CellWidth*board.Zoom,-point.Y*PanelGeometry.CellHeight*board.Zoom);
    }
}
