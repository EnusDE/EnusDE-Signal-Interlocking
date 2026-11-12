using System;
namespace EnusDE.Signaltechnik {
    public struct WorkspaceBox {
        public double X,Y,Width,Height;
        public WorkspaceBox(double x,double y,double width,double height){X=x;Y=y;Width=width;Height=height;}
    }
    // Geometry of our workspace, independent of game templates and Unity layout passes.
    public sealed class WorkspaceLayout {
        public WorkspaceBox Canvas,Diagram,Header,Status,Popup;
        static double Clamp(double value,double min,double max)=>Math.Max(min,Math.Min(max,value));
        public static WorkspaceLayout For(double width,double height,bool large=false,double popupX=double.NaN,double popupY=double.NaN,bool compact=false) {
            if(double.IsNaN(width)||double.IsInfinity(width)||double.IsNaN(height)||double.IsInfinity(height)||width<240||height<180)throw new ArgumentOutOfRangeException("workspace size");
            var layout=new WorkspaceLayout{Canvas=new WorkspaceBox(0,0,width,height),Header=new WorkspaceBox(62,8,width-74,36),Diagram=new WorkspaceBox(62,52,width-74,height-116),Status=new WorkspaceBox(62,height-58,width-74,50)};
            double w=Math.Min(large?720:480,width-90),h=Math.Min(compact?92:large?760:560,height-82);
            layout.Popup=new WorkspaceBox(Clamp(double.IsNaN(popupX)?width-w-22:popupX,68,width-w-12),Clamp(double.IsNaN(popupY)?62:popupY,56,height-h-16),w,h);return layout;
        }
    }
}
