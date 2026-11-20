using System;
namespace EnusDE.Signaltechnik {
    // Extension seam only. No menu entry, network transport or movement authority.
    public static class SignalCommunication {
        public static event Action<string,int> Requested;
        public static bool Request(string physicalSignal,int direction) {
            if(string.IsNullOrWhiteSpace(physicalSignal)||physicalSignal.Length>128||direction<0||direction>1)throw new ArgumentException("Invalid signal communication target.");
            var handler=Requested;if(handler==null)return false;handler(physicalSignal,direction);return true;
        }
    }
}
