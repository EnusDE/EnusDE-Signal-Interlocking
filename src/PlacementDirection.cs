using System;
using System.IO;

namespace EnusDE.Signaltechnik {
    public sealed class ServerPlacement {
        public bool RightSide=true,FollowView=true;
        public PlacementSize Size;
        public static ServerPlacement From(MarkerOptions value)=>new ServerPlacement{RightSide=value.DefaultRightSide,FollowView=value.FollowView,Size=new PlacementSize{UseGameSpacing=value.UseGameSpacing,Lateral=Math.Abs(value.Lateral),Height=value.Height,Scale=value.Scale}};
        public void Apply(MarkerOptions value){value.DefaultRightSide=RightSide;value.FollowView=FollowView;if(Size!=null){value.UseGameSpacing=Size.UseGameSpacing;value.Lateral=Size.Lateral;value.Height=Size.Height;value.Scale=Size.Scale;}}
        public bool Matches(MarkerOptions v)=>RightSide==v.DefaultRightSide&&FollowView==v.FollowView&&(Size==null||Size.UseGameSpacing==v.UseGameSpacing&&Size.Lateral==Math.Abs(v.Lateral)&&Size.Height==v.Height&&Size.Scale==v.Scale);
        public void Validate(){if(Size!=null&&(double.IsNaN(Size.Lateral)||double.IsInfinity(Size.Lateral)||Size.Lateral<0||Size.Lateral>5||double.IsNaN(Size.Height)||double.IsInfinity(Size.Height)||Size.Height< -1||Size.Height>2||double.IsNaN(Size.Scale)||double.IsInfinity(Size.Scale)||Size.Scale<.5||Size.Scale>3))throw new InvalidDataException("Gemeinsame Bauabstände ungültig.");}
    }
    public sealed class PlacementSize {public bool UseGameSpacing=true;public double Lateral=2.3,Height,Scale=1;}
    public static class PlacementDirection {
        public static bool Reverse(double tx,double tz,double vx,double vz,bool previous=false) {
            double a=tx*tx+tz*tz,b=vx*vx+vz*vz;if(a<.0001||b<.0001||double.IsNaN(a)||double.IsNaN(b)||double.IsInfinity(a)||double.IsInfinity(b))return previous;double dot=(tx*vx+tz*vz)/Math.Sqrt(a*b);return Math.Abs(dot)<.05?previous:dot<0;
        }
        public static double Side(double spacing,bool right,bool reverse)=>Math.Abs(spacing)*(right?1:-1)*(reverse?-1:1);
    }
}
