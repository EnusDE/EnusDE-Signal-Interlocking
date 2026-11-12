using System;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    // A single bounded current view. Text getters never allocate UI objects.
    public sealed class LiveViewCache {
        Dictionary<string,string> values=new Dictionary<string,string>();
        string layout;
        public int Count=>values.Count;
        public long Updates {get;private set;}
        public string Get(string key){string value;return values.TryGetValue(key,out value)?value:"—";}
        public void Replace(Dictionary<string,string> next){values=next;Updates++;}
        public bool ChangeLayout(DiagnosticSettings settings) {
            string signature=settings.ShowRecorder+"|"+settings.ShowSpeed+"|"+settings.ShowPositions;
            foreach(string key in DiagnosticSettings.Categories)signature+="|"+key+":"+settings[key].Visible;
            if(layout==signature)return false;layout=signature;return true;
        }
        public void Clear(){values.Clear();layout=null;}
    }
}
