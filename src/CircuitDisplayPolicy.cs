namespace EnusDE.Signaltechnik {
    public static class CircuitDisplayPolicy {
        public static bool Monitored(DeskLibrary library,string id)=>!string.IsNullOrEmpty(id)&&
            library?.Blocks.TryGetValue(id,out var block)==true&&
            (block.IsCircuit||block.Point!=null&&PointCircuitVisibility.Enabled(library,block.Point));
    }
}
