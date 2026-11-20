using System;
namespace EnusDE.Signaltechnik {
    public static class HlTestFeature {
        public static bool RequiresEnable(MarkerRecord before,MarkerRecord after)=>after?.HlSignal==true&&before?.HlSignal!=true;
        public static void Require(bool enabled) {
            if(!enabled)throw new InvalidOperationException(LocalText.Select("Enable Hl signal system (Test) in Company → Options → Mods → Signaltechnik → Signal operation and display first.","Zuerst Hl-Signalsystem aktivieren (Test) unter Company → Optionen → Mods → Signaltechnik → Signalbetrieb und Darstellung einschalten."));
        }
    }
}
