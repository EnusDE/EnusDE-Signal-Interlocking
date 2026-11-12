using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Track;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Reads the game's actual rail-point animation sequences; never drives an animator.
    public sealed class NativeSwitchFeedback : IDisposable {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static readonly FieldInfo nodeField=typeof(SwitchPointRails).GetField("_node",flags),stateField=typeof(SwitchPointRails).GetField("state",flags),normalField=typeof(SwitchPointRails).GetField("_normalSequence",flags),reverseField=typeof(SwitchPointRails).GetField("_reversedSequence",flags);
        static readonly PropertyInfo alive=normalField?.FieldType.GetProperty("isAlive",flags);
        readonly Dictionary<string,SwitchPointRails[]> rails=new Dictionary<string,SwitchPointRails[]>();
        readonly Dictionary<string,Tuple<bool,double>> observations=new Dictionary<string,Tuple<bool,double>>();
        double nextScan;
        public bool Read(string id,bool target,out string detail) {
            detail="Endlage nicht bestätigt";
            if(nodeField==null||stateField==null||normalField==null||reverseField==null||alive==null)throw new MissingMemberException("Native Weichen-Rückmeldung hat eine unbekannte API-Form.");
            var node=Graph.Shared?.GetNode(id);if(node==null||!Graph.Shared.IsSwitch(node))return false;
            double now=Time.realtimeSinceStartup;Tuple<bool,double> seen;
            if(!observations.TryGetValue(id,out seen)||seen.Item1!=node.isThrown){seen=Tuple.Create(node.isThrown,now);observations[id]=seen;}
            if(node.isThrown!=target){detail="Weiche nicht in Sollstellung";return false;}
            if(now>=nextScan){nextScan=now+1;rails.Clear();foreach(var group in UnityEngine.Object.FindObjectsByType<SwitchPointRails>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(r=>r!=null&&nodeField.GetValue(r) is TrackNode).GroupBy(r=>((TrackNode)nodeField.GetValue(r)).id))rails[group.Key]=group.ToArray();}
            SwitchPointRails[] matches;
            if(rails.TryGetValue(id,out matches)&&matches.Any(r=>r!=null&&r.gameObject.activeInHierarchy)) {
                foreach(var rail in matches.Where(r=>r!=null&&r.gameObject.activeInHierarchy)) {
                    if(Convert.ToInt32(stateField.GetValue(rail))!=(target?2:1)){detail="Zungenstellung wird übernommen";return false;}
                    if((bool)alive.GetValue(normalField.GetValue(rail),null)||(bool)alive.GetValue(reverseField.GetValue(rail),null)){detail="Zungen im Umlauf";return false;}
                }
                detail="Zungen-Endlage bestätigt";return true;
            }
            detail=now-seen.Item2<2?"Logische Stellung wird stabil geprüft":"Logische Endlage bestätigt (außer Sicht)";
            return now-seen.Item2>=2;
        }
        public void Dispose(){rails.Clear();observations.Clear();}
    }
}
