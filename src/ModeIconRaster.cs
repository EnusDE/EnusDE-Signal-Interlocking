using System;

namespace EnusDE.Signaltechnik {
    public static class ModeIconRaster {
        public const int Size=48;
        public static string Symbol(WorkspaceMode mode) {
            switch(mode) {
                case WorkspaceMode.Dispatcher:return "DISPATCH";
                case WorkspaceMode.Build:return "BUILD";
                case WorkspaceMode.Editor:return "MAPEDIT";
                default:throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public static byte[] Alpha(WorkspaceMode mode)=>Alpha(Symbol(mode));
        public static byte[] SettingsAlpha()=>Alpha("SETTINGS");
        static byte[] Alpha(string symbol) {
            var pixels=new byte[Size*Size];
            const double unitsPerPixel=32d/Size;
            foreach(var line in WorkspaceSymbols.For(symbol)) {
                double dx=line[2]-line[0],dy=line[3]-line[1];
                double length=dx*dx+dy*dy;
                for(int y=0;y<Size;y++)for(int x=0;x<Size;x++) {
                    double px=(x+.5)*unitsPerPixel-line[0],py=(y+.5)*unitsPerPixel-line[1];
                    double t=Math.Max(0,Math.Min(1,(px*dx+py*dy)/length));
                    double distance=Math.Sqrt((px-t*dx)*(px-t*dx)+(py-t*dy)*(py-t*dy));
                    int alpha=(int)Math.Round(255*Math.Max(0,Math.Min(1,(1.5-distance)/unitsPerPixel)));
                    int index=y*Size+x;
                    if(alpha>pixels[index])pixels[index]=(byte)alpha;
                }
            }
            return pixels;
        }
    }
}
