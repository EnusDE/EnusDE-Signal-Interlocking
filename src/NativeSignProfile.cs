using System;

namespace EnusDE.Signaltechnik {
    // Read from the installed 0.6.2 base scene: all 29 Sign-Speed instances reference
    // the same 25 mph mesh. No arbitrary numeric scenery/material names are parsed.
    public static class NativeSignProfile {
        public static bool TrySpeed(string mesh,string material,int vertices,long indices,double width,double depth,double height,out double mph) {
            mph=0;
            if(mesh!="Speed Sign"||material!="SignsGroup01"||vertices!=448||indices!=978||Math.Abs(width-.005588)>.000002||Math.Abs(depth-.0007175)>.000002||Math.Abs(height-.0265922)>.000002)return false;
            mph=25;return true;
        }
    }
}
