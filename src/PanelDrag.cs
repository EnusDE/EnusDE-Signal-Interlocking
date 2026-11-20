using System;
using System.Linq;

namespace EnusDE.Signaltechnik {
    public sealed class PanelDrag {
        public PanelRegion Source {get;private set;}
        public int X {get;private set;}
        public int Y {get;private set;}
        public bool Valid {get;private set;}
        int offsetX,offsetY;bool growing;
        string original;
        PanelTile[] moving;
        System.Collections.Generic.HashSet<long> occupied;
        public void Begin(PanelDocument document,PanelRegion selected,int x,int y,bool allowGrowth=false) {
            growing=allowGrowth;
            moving=null;occupied=null;
            if(allowGrowth&&selected!=null&&(selected.X==0&&selected.Width==document.Columns||selected.Y==0&&selected.Height==document.Rows)) {
                selected=new PanelRegion{X=selected.X,Y=selected.Y,Width=selected.Width,Height=selected.Height};bool expanded;
                do {expanded=false;foreach(var a in document.Areas.Where(a=>a.BlockLine!=null||a.NumberLine!=null)) {
                    if(a.X>=selected.X+selected.Width||selected.X>=a.X+a.Width||a.Y>=selected.Y+selected.Height||selected.Y>=a.Y+a.Height)continue;
                    int left=Math.Min(selected.X,a.X),top=Math.Min(selected.Y,a.Y),right=Math.Max(selected.X+selected.Width,a.X+a.Width),bottom=Math.Max(selected.Y+selected.Height,a.Y+a.Height);
                    if(left!=selected.X||top!=selected.Y||right-left!=selected.Width||bottom-top!=selected.Height){selected=new PanelRegion{X=left,Y=top,Width=right-left,Height=bottom-top};expanded=true;}
                }}while(expanded);
            }
            var assembly=document.Areas.FirstOrDefault(a=>(a.BlockLine!=null||a.NumberLine!=null)&&a.Contains(x,y));
            if(assembly!=null&&(selected==null||selected.Width==1&&selected.Height==1||!selected.Contains(assembly.X,assembly.Y)||!selected.Contains(assembly.X+assembly.Width-1,assembly.Y+assembly.Height-1)))selected=new PanelRegion{X=assembly.X,Y=assembly.Y,Width=assembly.Width,Height=assembly.Height};
            Source=selected?.Contains(x,y)==true?new PanelRegion{X=selected.X,Y=selected.Y,Width=selected.Width,Height=selected.Height}:new PanelRegion{X=x,Y=y};
            if(!document.Tiles.Any(t=>Source.Contains(t.X,t.Y))){Source=null;return;}
            offsetX=x-Source.X;offsetY=y-Source.Y;original=PanelHistory.Hash(document);Update(document,x,y);
        }
        public void Update(PanelDocument document,int x,int y) {
            if(Source==null)return;X=x-offsetX;Y=y-offsetY;
            Valid=X>=0&&Y>=0&&X+Source.Width<=document.Columns&&Y+Source.Height<=document.Rows;
            if(growing)try{CanvasGrowth.For(document,new PanelRegion{X=X,Y=Y,Width=Source.Width,Height=Source.Height},0);Valid=true;}catch(InvalidOperationException){Valid=false;}
            if(moving==null){moving=document.Tiles.Where(t=>Source.Contains(t.X,t.Y)).ToArray();occupied=new System.Collections.Generic.HashSet<long>(document.Tiles.Where(t=>!Source.Contains(t.X,t.Y)).Select(t=>((long)t.Y<<32)|(uint)t.X));}
            if(Valid&&document.Areas.Any(a=>a.NumberLine!=null&&Source.Contains(a.X,a.Y)&&Source.Contains(a.X+a.Width-1,a.Y+a.Height-1))) {
                Valid=!document.Areas.Any(a=>(a.BlockLine!=null||a.NumberLine!=null)&&!(Source.Contains(a.X,a.Y)&&Source.Contains(a.X+a.Width-1,a.Y+a.Height-1))&&a.X<X+Source.Width&&X<a.X+a.Width&&a.Y<Y+Source.Height&&Y<a.Y+a.Height);
            }
            if(Valid)foreach(var tile in moving) {
                int nx=tile.X+X-Source.X,ny=tile.Y+Y-Source.Y;
                if(occupied.Contains(((long)ny<<32)|(uint)nx)){Valid=false;break;}
            }
        }
        public void Invalidate(){Valid=false;}
        public CanvasGrowth Growth(PanelDocument document) {
            if(Source==null||!Valid||original!=PanelHistory.Hash(document))throw new InvalidOperationException(LocalText.Select("Move cancelled: occupied destination or changed diagram.","Verschieben abgebrochen: Ziel belegt oder Gleisbild geändert."));
            return growing?CanvasGrowth.For(document,new PanelRegion{X=X,Y=Y,Width=Source.Width,Height=Source.Height}):new CanvasGrowth();
        }
        public void Apply(PanelDocument document) {
            var growth=Growth(document);var next=document.Clone();growth.Apply(next);
            new PanelRegion{X=Source.X+growth.Left,Y=Source.Y+growth.Top,Width=Source.Width,Height=Source.Height}.Move(next,X+growth.Left,Y+growth.Top);
            CanvasGrowth.Commit(document,next);
        }
    }
}
