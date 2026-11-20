using System;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public readonly struct PanelPoint {
        public readonly float X,Y;
        public PanelPoint(float x,float y){X=x;Y=y;}
    }
    public readonly struct PanelLine {
        public readonly PanelPoint A,B;
        public PanelLine(float x0,float y0,float x1,float y1){A=new PanelPoint(x0,y0);B=new PanelPoint(x1,y1);}
    }
    // One coordinate system for layout, track endpoints, rotations and mirrored tiles.
    public static class PanelGeometry {
        public const int CellWidth=72,CellHeight=48;
        static readonly PanelLine[][] shapes={
            new[]{new PanelLine(-1,0,1,0)},
            new[]{new PanelLine(-1,-1,1,1)},
            new[]{new PanelLine(-1,0,0,0),new PanelLine(0,0,1,1)},
            new[]{new PanelLine(-1,0,0,0),new PanelLine(0,0,1,0),new PanelLine(0,0,1,1)},
            new[]{new PanelLine(-1,0,1,0),new PanelLine(0,-1,0,1)},
            new[]{new PanelLine(-1,0,1,0),new PanelLine(0,0,0,.375f),new PanelLine(0,.375f,.5f,.375f)},
            new PanelLine[0],new PanelLine[0],new[]{new PanelLine(-1,0,1,0)},new PanelLine[0]
        };
        public static IReadOnlyList<PanelLine> Lines(PanelTileKind kind)=>shapes[(int)kind];
        public static PanelPoint World(PanelPoint point,int rotation,bool mirror,int x,int y) {
            float px=point.X,py=mirror?-point.Y:point.Y;
            for(int i=0;i<rotation;i++){float old=px;px=py;py=-old;}
            return new PanelPoint(x*CellWidth+px*CellWidth/2f,-y*CellHeight+py*CellHeight/2f);
        }
    }
}
