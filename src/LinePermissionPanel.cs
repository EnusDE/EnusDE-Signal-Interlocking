using System;
using System.Linq;
using UI.Builder;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        PanelVisual LinePermissionVisual(PanelTile tile,PanelVisual value) {
            var line=desks.FreeLines.FirstOrDefault(l=>l.Id==tile.Reference);var live=line==null?null:LineLive(line.Id);
            value.Caption=string.IsNullOrEmpty(tile.Label)?line==null?WindowText("Permission","Erlaubnis"):line.Code+" · "+line.Track:tile.Label;
            bool unknown=live==null||live.State=="fault"||live.State=="disabled";
            if(!tile.InlinePermission)value.Unknown=unknown;
            value.LineDirection=unknown?0:live.Direction;
            value.PermissionRequest=unknown?0:live.RequestedDirection;
            value.Lamp=unknown?new Color(.55f,.32f,.15f):new Color(1,.7f,.15f);
            return value;
        }
        void BuildLineIndicatorEditor(UIPanelBuilder c,PanelDraftBinding binding) {
            var draft=binding.Draft;var lines=desks.FreeLines.OrderBy(l=>l.Code).ToArray();
            c.AddSearchChoice(WindowText("Line track","Streckengleis"),new[]{WindowText("Unassigned","Nicht zugeordnet")}.Concat(lines.Select(l=>l.Code+" · "+l.Name+" · "+l.Track)).ToList(),Array.FindIndex(lines,l=>l.Id==draft.Reference)+1,i=>binding.Do(()=>{draft.Reference=i==0?"":lines[i-1].Id;RefreshWorkspaceFields();}));
            c.AddFieldToggle(WindowText("Inline module with continuous diagram rail","Modul im Gleis mit durchgehender Gleislinie"),()=>draft.InlinePermission,v=>binding.Do(()=>draft.InlinePermission=v),true);
            var targets=AuxiliaryTarget.Index(desks.Desks.SelectMany(d=>d.Panel.Tiles)).Values.OrderBy(t=>t.Caption).ToArray();
            c.AddSearchChoice(WindowText("Linked auxiliary button","Verknüpfte Hilfstaste"),new[]{WindowText("Unassigned","Nicht zugeordnet")}.Concat(targets.Select(t=>t.Caption+" · "+(t.Function==AuxiliaryFunction.RouteDestination?WindowText("Route destination","Fahrstraßenziel"):WindowText("Permission controls","Erlaubnisbedienung")))).ToList(),Array.FindIndex(targets,t=>t.Id==draft.PermissionAuxiliary)+1,i=>binding.Do(()=>draft.PermissionAuxiliary=i==0?null:targets[i-1].Id));
            c.AddLabel(WindowText("Link a normal train destination here: it keeps its destination function, requires this line's permission and requests the block-signal chain through to the neighbour's arrival signal. The module itself continues to operate request/giving. A permission-only auxiliary still opens controls without requesting a route.","Hier ein normales Zugziel verknüpfen: Es behält seine Zielfunktion, verlangt die Erlaubnis dieser Strecke und fordert die Blocksignalkette bis zum Ankunftssignal des Nachbarn an. Das Modul selbst bedient weiterhin Anfrage/Abgabe. Eine reine Erlaubnis-Hilfstaste öffnet weiterhin nur die Bedienung ohne Fahrstraßenanforderung."));
            c.AddLabel(WindowText("The arrow points A → B in the field direction; the opposite arrow means B → A. Rotate the field to match the diagram. An indicated permission is not a signal proceed aspect.","Der Pfeil in Feldrichtung bedeutet A → B, der Gegenpfeil B → A. Das Feld passend zum Gleisbild drehen. Eine angezeigte Erlaubnis ist kein Fahrtbegriff eines Signals."));
            c.AddButton(WindowText("Set up this block / line connection","Diese Block-/Streckenverbindung einrichten"),()=>binding.Do(()=>Act(()=>OpenLineSetupForField(draft.X,draft.Y))));
        }
    }
}
