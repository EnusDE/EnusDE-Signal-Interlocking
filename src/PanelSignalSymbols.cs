namespace EnusDE.Signaltechnik {
    public readonly struct SignalCaptionBox {
        public readonly float X,Y,Width,Height;
        public SignalCaptionBox(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
    }
    public static class PanelSignalSymbols {
        public static SignalCaptionBox Caption(PanelTile tile,bool split=false,bool shared=false,bool alternate=false,bool boundary=false) {
            if(tile.Kind==PanelTileKind.LinePermission)return tile.Rotation%2==0?new SignalCaptionBox(0,-16,70,14):new SignalCaptionBox(22,0,26,40);
            if(SignalCellLayout.HasAuxiliaryHalf(tile)) {
                var point=SignalCellLayout.HalfPoint(tile,alternate);
                if(point.X!=0) {
                    var offset=HeadOffset(SignalCellLayout.MainDirection(tile),tile.Mirror);
                    return new SignalCaptionBox(point.X,(offset.Y>0?-1:1)*(boundary?8.5f:16),34,boundary?9:14);
                }
                var combinedSide=HeadOffset(SignalCellLayout.MainDirection(tile),tile.Mirror);
                return new SignalCaptionBox(combinedSide.X>0?-20:20,point.Y>0?(boundary?11:14):-14,30,point.Y>0&&boundary?8:12);
            }
            bool targetPair=tile.Kind==PanelTileKind.AuxiliaryTarget&&tile.Target?.Train==true&&tile.Target.Shunt;
            split|=targetPair;shared|=targetPair;
            int direction=tile.Kind==PanelTileKind.AuxiliaryTarget?SignalCellLayout.TargetDirection(tile):
                split?SignalCellLayout.HeadSideDirection(tile):SignalCellLayout.MainDirection(tile);
            var head=HeadOffset(direction,tile.Mirror);
            if(head.Y!=0) {
                float x=split&&!shared?(alternate?18:-18)*(tile.SwapSignalHalves?-1:1):0;
                return new SignalCaptionBox(x,(head.Y>0?-1:1)*(boundary?8.5f:16),split&&!shared?34:70,boundary?9:14);
            }
            float side=head.X>0?-20:20;
            return new SignalCaptionBox(side,split?(alternate?-14:boundary?11:14):0,30,split?(boundary&&!alternate?8:12):boundary?24:40);
        }
        // Reserve a separate outer row when a signal and a GFM boundary share a cell.
        public static SignalCaptionBox BoundaryCaption(PanelTile tile,bool split=false) {
            if(tile.Kind!=PanelTileKind.Signal&&tile.Kind!=PanelTileKind.AuxiliaryTarget)
                return new SignalCaptionBox(0,16.5f,70,11);
            var name=Caption(tile,split,boundary:true);
            return name.X==0||name.Width!=30?
                new SignalCaptionBox(4,name.Y>0?19:-19,62,8):
                new SignalCaptionBox(name.X,20,30,8);
        }
        public static PanelPoint BoundaryMark(PanelTile tile,bool split=false) {
            if(tile.Kind!=PanelTileKind.Signal&&tile.Kind!=PanelTileKind.AuxiliaryTarget)return new PanelPoint(0,4);
            var box=BoundaryCaption(tile,split);
            return box.Width==30?new PanelPoint(0,20):new PanelPoint(-32,box.Y);
        }
        // UI Y points up: the right-hand side of an eastbound track is below it.
        public static int HeadSide(bool shunt,bool mirror)=>mirror?1:-1;
        public static PanelPoint HeadOffset(int sideDirection,bool mirror) {
            float offset=11*HeadSide(false,mirror);
            return sideDirection==0?new PanelPoint(0,offset):sideDirection==1?new PanelPoint(offset,0):
                sideDirection==2?new PanelPoint(0,-offset):new PanelPoint(-offset,0);
        }
        public static PanelPoint[] Screen(bool shunt)=>shunt?
            new[]{new PanelPoint(-6,-4),new PanelPoint(6,-4),new PanelPoint(6,4),new PanelPoint(-6,4)}:
            new[]{new PanelPoint(-4,-6),new PanelPoint(3,-6),new PanelPoint(5,-4),new PanelPoint(5,4),new PanelPoint(3,6),new PanelPoint(-4,6)};
        public static PanelPoint[] Lamps(bool shunt,bool permissive)=>!shunt?new[]{new PanelPoint(0,0)}:
            permissive?new[]{new PanelPoint(-3,-1.6f),new PanelPoint(3,1.6f)}:new[]{new PanelPoint(-3,0),new PanelPoint(3,0)};
        public static PanelPoint[] Arrow()=>new[]{new PanelPoint(4,0),new PanelPoint(-2,2.4f),new PanelPoint(-2,-2.4f)};
        public static PanelLine[] TargetMark()=>new[]{new PanelLine(-2,2,2,2),new PanelLine(2,2,-2,-2),new PanelLine(-2,-2,2,-2)};
    }
}
