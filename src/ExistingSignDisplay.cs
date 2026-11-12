using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Mow;
using Helpers;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Local presentation only: never changes PlayerSign data, speed restrictions or network messages.
    public sealed class ExistingSignDisplay : IDisposable {
        sealed class Binding {public GameObject Root;public TextSynchronizer Text;public string Original,Applied;}
        static readonly FieldInfo instances=typeof(SignController).GetField("_instances",BindingFlags.Instance|BindingFlags.NonPublic);
        readonly Dictionary<string,Binding> bindings=new Dictionary<string,Binding>();
        readonly BakedSignDisplay baked=new BakedSignDisplay();
        public int Count=>bindings.Count;
        public string Error {get;private set;}
        public string Status {get;private set;}="Noch nicht geprüft.";
        public void Refresh(bool enabled,string mode) {
            Error=null;
            if(!enabled||mode=="original"){Dispose();Status=enabled?"Originalbeschriftung (mph).":"Anpassung vorhandener Schilder aus.";return;}
            try{baked.Refresh(enabled,mode);}catch(Exception e){baked.Dispose();Error=e.Message;}
            var controller=SignController.Shared;
            var rows=controller==null?null:instances?.GetValue(controller) as IDictionary<string,GameObject>;
            if(rows==null){Status="Spielerschilderverwaltung nicht verfügbar; "+baked.Count+" Kartenschilder angepasst.";return;}
            var live=new HashSet<string>();
            foreach(var pair in rows) {
                PlayerSign sign;
                if(pair.Value==null||!pair.Key.StartsWith("player:",StringComparison.Ordinal)||!controller.TryGetPlayerSign(SignUnits.PlayerId(pair.Key),out sign)||sign.Kind!=PlayerSign.KindSpeedRestriction||!sign.Mph.HasValue||!SignUnits.ValidSpeed(sign.Mph.Value))continue;
                if(live.Count>=512)break;
                live.Add(pair.Key);Binding value;
                if(!bindings.TryGetValue(pair.Key,out value)||value.Root!=pair.Value) {
                    if(value!=null)Restore(value);
                    var text=pair.Value.GetComponentInChildren<TextSynchronizer>(true);
                    if(text==null)continue;
                    value=new Binding{Root=pair.Value,Text=text,Original=text.text};bindings[pair.Key]=value;
                }
                if(value.Text==null)continue;
                // Respect an unrelated mod's later presentation changes instead of fighting them.
                if(value.Text.text!=value.Original&&value.Text.text!=value.Applied)continue;
                string next=SignUnits.Caption(sign.Mph.Value,mode);
                if(value.Text.text!=next){value.Text.text=next;value.Text.ApplyText();}
                value.Applied=next;
            }
            var removed=new List<string>();foreach(var pair in bindings)if(!live.Contains(pair.Key)){Restore(pair.Value);removed.Add(pair.Key);}
            foreach(string key in removed)bindings.Remove(key);
            Status=bindings.Count+" Spieler- und "+baked.Count+" erkannte Karten-Geschwindigkeitsschilder angepasst.";
        }
        static void Restore(Binding value){if(value.Text!=null&&value.Applied!=null&&value.Text.text==value.Applied){value.Text.text=value.Original;value.Text.ApplyText();}}
        public void Dispose(){foreach(var row in bindings.Values)Restore(row);bindings.Clear();baked.Dispose();}
    }
}
