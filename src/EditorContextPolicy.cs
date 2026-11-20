using System;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public enum EditorFieldCategory { Properties,Detection,TrainNumber,History }
    public static class EditorContextPolicy {
        public static EditorFieldCategory[] FieldCategories(PanelTile field) {
            if(field==null)return Array.Empty<EditorFieldCategory>();
            var result=new List<EditorFieldCategory>{EditorFieldCategory.Properties};
            if(!NumberMirrors.IsMirror(field)&&(field.Kind==PanelTileKind.Signal||field.Kind==PanelTileKind.TrainNumber||field.Kind==PanelTileKind.Switch||field.Kind==PanelTileKind.Straight||field.Kind==PanelTileKind.Diagonal||field.Kind==PanelTileKind.Curve||field.Kind==PanelTileKind.Cross||field.Kind==PanelTileKind.LinePermission&&field.InlinePermission))result.Add(EditorFieldCategory.Detection);
            if(field.Kind==PanelTileKind.TrainNumber)result.Add(EditorFieldCategory.TrainNumber);
            result.Add(EditorFieldCategory.History);return result.ToArray();
        }
        public static bool SameTarget(PanelDocument panel,int x,int y,PanelTile snapshot)=>snapshot==null?panel.At(x,y)==null:snapshot.SameContent(panel.At(x,y));
    }
    public sealed class SwitchOperationResult {
        public bool Target,Changed;
    }
    public static class SwitchOperation {
        public static SwitchOperationResult Execute(string id,bool? target,Func<string,bool?> read,Func<string,bool,bool> locked,Func<string,bool,bool> apply) {
            if(string.IsNullOrWhiteSpace(id))throw new InvalidOperationException(LocalText.Select("Assign this point to the game world first.","Diese Weiche zuerst der Spielwelt zuordnen."));
            bool? current=read(id);if(!current.HasValue)throw new InvalidOperationException(LocalText.Select("Point not found in the current track network.","Weiche nicht im aktuellen Gleisnetz gefunden."));
            bool desired=target??!current.Value;
            if(current.Value==desired)return new SwitchOperationResult{Target=desired,Changed=false};
            if(locked(id,desired))throw new InvalidOperationException(LocalText.Select("Point locked by a route or flank protection.","Weiche durch Fahrstraße oder Flankenschutz verschlossen."));
            if(!apply(id,desired))throw new InvalidOperationException(LocalText.Select("The game did not confirm the point movement.","Das Spiel hat die Weichenumstellung nicht bestätigt."));
            return new SwitchOperationResult{Target=desired,Changed=true};
        }
    }
}
