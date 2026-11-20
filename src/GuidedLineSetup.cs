using System;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        bool guidedLineSetup;
        int guidedLineStep;
        bool LineStep(int step)=>!guidedLineSetup||guidedLineStep==step;
        string SetupStationName(string id) {var station=desks.Desks.FirstOrDefault(d=>d.Id==id);return station==null?id:station.Code+" · "+station.Name;}
        void PickGuidedPermission(LineTrackDefinition draft,string station) {
            string worldScope=scope;var snapshot=draft;
            PickCellInDiagram(station,SetupStationName(station)+" · "+WindowText("Place permission control","Erlaubnisbedienung platzieren"),
                (d,t)=>d.Id==station&&(t==null||t.Kind==PanelTileKind.LinePermission&&(t.Reference==""||t.Reference==draft.Id)||t.Kind==PanelTileKind.Straight&&t.CircuitBoundary==null),
                (desk,x,y)=>{
                    if(scope!=worldScope||!ReferenceEquals(lineDraft,snapshot)||panelPage!=9)throw new InvalidOperationException(WindowText("Line setup changed. Select again.","Streckeneinrichtung geändert. Erneut auswählen."));
                    var previous=desks.Desks.Single(d=>d.Id==desk).Panel.At(x,y)?.Clone();
                    guidedPermissions[station]=new GuidedPermission{Desk=desk,X=x,Y=y,Previous=previous};guidedControlPreview=null;RefreshWorkspaceFields();
                });
        }
        sealed class GuidedPermission {public string Desk;public int X,Y;public PanelTile Previous;}
        readonly System.Collections.Generic.Dictionary<string,GuidedPermission> guidedPermissions=new System.Collections.Generic.Dictionary<string,GuidedPermission>();
        void BuildGuidedPermissionPositions(UIPanelBuilder c,LineTrackDefinition draft) {
            c.AddSection(WindowText("Permission controls at the neighbours","Erlaubnisbedienungen bei den Nachbarn"));
            foreach(string id in new[]{draft.StationA,draft.StationB}) {
                string station=id;var existing=desks.Desks.Single(d=>d.Id==id).Panel.Tiles.FirstOrDefault(t=>t.Kind==PanelTileKind.LinePermission&&t.Reference==draft.Id&&!t.ReadOnlySignal);
                c.AddLabel(SetupStationName(id)+(guidedPermissions.TryGetValue(id,out var pick)?" · "+(pick.X+1)+" / "+(pick.Y+1):existing!=null?WindowText(" · existing control retained"," · vorhandene Bedienung bleibt erhalten"):WindowText(" · optional: choose a position"," · optional: Position wählen")));
                c.AddButton(WindowText("Choose permission position: ","Erlaubnisposition wählen: ")+SetupStationName(id),()=>PickGuidedPermission(draft,station));
            }
        }
        void ApplyGuidedPermissions(DeskLibrary library,LineTrackDefinition line) {
            if(!guidedLineSetup)return;
            foreach(var choice in guidedPermissions.Values.Where(p=>p.Desk==line.StationA||p.Desk==line.StationB)) {
                var doc=library.Desks.Single(d=>d.Id==choice.Desk).Panel;var actual=doc.At(choice.X,choice.Y);
                if(actual?.Kind==PanelTileKind.LinePermission&&actual.Reference==line.Id)continue;
                if(choice.Previous==null?actual!=null:actual==null||!choice.Previous.SameContent(actual))throw new InvalidOperationException(WindowText("A permission destination field changed. Choose its position again.","Ein Zielfeld für die Erlaubnis wurde geändert. Position erneut wählen."));
                var existing=doc.Tiles.Where(t=>t.Kind==PanelTileKind.LinePermission&&t.Reference==line.Id&&!t.ReadOnlySignal).ToArray();
                if(existing.Any(t=>t.X!=choice.X||t.Y!=choice.Y))throw new InvalidOperationException(WindowText("A permission control already exists at this interlocking. Move that field in the editor instead of adding a copy.","Eine Erlaubnisbedienung ist in diesem Stellwerk bereits vorhanden. Dieses Feld im Editor verschieben statt eine Kopie anzulegen."));
                var tile=actual?.Clone()??new PanelTile{X=choice.X,Y=choice.Y};bool inline=tile.Kind==PanelTileKind.Straight||tile.InlinePermission;
                tile.Kind=PanelTileKind.LinePermission;tile.Reference=line.Id;tile.InlinePermission=inline;doc.Put(tile);
            }
        }
    }
}
