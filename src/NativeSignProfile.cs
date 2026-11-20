using System;

namespace EnusDE.Signaltechnik {
    public static class NativeSignProfile {
        // Geometry identifies the supported native model, never the speed or face meaning.
        public static bool IsKnownGeometry(string mesh,string material,int vertices,long indices,double width,double depth,double height)=>
            mesh=="Speed Sign"&&material=="SignsGroup01"&&vertices==448&&indices==978&&
            Math.Abs(width-.005588)<.000002&&Math.Abs(depth-.0007175)<.000002&&Math.Abs(height-.0265922)<.000002;
    }
}
