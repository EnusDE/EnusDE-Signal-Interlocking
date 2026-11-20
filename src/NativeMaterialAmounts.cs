using System;

namespace EnusDE.Signaltechnik {
    public static class NativeMaterialAmounts {
        public static float Transfer(float quantity,int units,float capacity) {
            if(float.IsNaN(quantity)||float.IsInfinity(quantity)||quantity<0||units<0||float.IsNaN(capacity)||float.IsInfinity(capacity)||capacity<1||quantity+units>capacity)throw new InvalidOperationException(LocalText.Select("Storage capacity is insufficient; existing stock has been retained.","Lagerkapazität reicht nicht; vorhandener Bestand bleibt erhalten."));
            return quantity+units;
        }
        public static int FitTransfer(float quantity,int requested,float capacity) {
            Transfer(quantity,0,capacity);if(requested<0)throw new ArgumentOutOfRangeException(nameof(requested));
            return (int)Math.Min(requested,Math.Floor(capacity-quantity));
        }
        public static int Packages(float quantity) {
            if(float.IsNaN(quantity)||float.IsInfinity(quantity)||quantity<0||quantity>1000000)throw new InvalidOperationException(LocalText.Select("Invalid native signal stock.","Ungültiger Signalbaubestand im MOW-Lager."));
            return (int)Math.Floor(quantity);
        }
    }
}
