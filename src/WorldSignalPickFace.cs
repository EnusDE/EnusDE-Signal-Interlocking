namespace EnusDE.Signaltechnik {
    public static class WorldSignalPickFace {
        public static string Resolve(MarkerRecord row,int side) {
            if(row==null||!row.IsSignal||row.Planned||side<0||side>1)return null;
            if(row.Kind!=MarkerRecord.Main)return row.Id;
            var mast=MastConfiguration.Effective(row);var chosen=side==0?mast.Front:mast.Back;var other=side==0?mast.Back:mast.Front;
            // A single enabled head is unambiguous even when its mast is clicked from behind.
            return chosen.Enabled?chosen.Id:other.Enabled?other.Id:null;
        }
    }
}
