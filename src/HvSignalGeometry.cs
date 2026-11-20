namespace EnusDE.Signaltechnik {
    // Stylized compact full-function main signal; shares the mast signal authority and lamp plans.
    public static class LowMainGeometry {
        public const float Height=1.73f;
        public static readonly float[][] Main={new[]{-.24f,1.43f},new[]{-.24f,.63f},new[]{-.24f,1.03f}};
        public static readonly float[][] Shunt={new[]{.20f,.83f},new[]{.36f,1.23f},new[]{.20f,.83f},new[]{.36f,1.23f}};
    }
    // Original proportions inspired by classic H/V main and shunting signals.
    public static class HvSignalGeometry {
        // Main indices match SignalLensPlan: green, yellow, red.
        public static float[][] Lamps(string kind)=>kind==MarkerRecord.Main?
            new[]{new[]{.17f,3.85f},new[]{.17f,3.16f},new[]{-.17f,3.50f}}:
            kind==MarkerRecord.Shunt?
            new[]{new[]{-.14f,.85f},new[]{.14f,.85f},new[]{-.14f,.66f},new[]{.14f,1.04f}}:
            new[]{new[]{-.17f,2.95f},new[]{.17f,3.25f},new[]{-.17f,3.25f},new[]{.17f,2.95f}};
        public static float LensScale(string kind)=>kind==MarkerRecord.Shunt?.75f:1;
    }
}
