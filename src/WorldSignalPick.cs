using System;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        Action<string> worldSignalPick;
        Action worldSignalPickReturn;
        string worldSignalPickScope;
        bool worldSignalPickMain,worldBoundaryPick;
        readonly WorldSelectionInput worldSelectionInput=new WorldSelectionInput();
        internal bool ConsumeSelectionInput()=>worldSelectionInput.Consumes(Time.frameCount,Mouse.current?.leftButton.isPressed==true||Mouse.current?.rightButton.isPressed==true);
        void BeginWorldSignalPick(Action<string> selected,Action resume,bool mainOnly=true,bool boundaryOnly=false) {
            RequirePanelEdit();worldPointSelection=null;worldPointSelectionReturn=null;pointPickWorld=pointPickDesk=null;pointPickSnapshot=null;worldSignalPick=selected;worldSignalPickReturn=resume;worldSignalPickScope=scope;worldSignalPickMain=mainOnly;
            worldBoundaryPick=boundaryOnly;
            worldSelectionInput.Complete();objects.CloseForWorldSelection();
            CloseWorkspaceMode();status=boundaryOnly?LocalText.Select("Click the real GFM box to assign the boundary. Escape cancels.","Realen GFM-Kasten anklicken: Grenze wird zugeordnet. Escape bricht ab."):LocalText.Select("Click the real signal to assign it directly. Escape cancels.","Reales Signal anklicken: wird direkt zugeordnet. Escape bricht ab.");
        }
        internal bool OfferWorldSignalPick(string physical,int direction) {
            if(ConsumeSelectionInput())return true;
            if(worldSignalPick==null)return false;
            if(WorldPointerInput.OverUi())return true;
            var expected=worldSignalPick;
            Act(()=>{
                RequireEdit();if(!ReferenceEquals(expected,worldSignalPick)||scope!=worldSignalPickScope)throw new InvalidOperationException(LocalText.Select("Assignment changed. Start it again.","Zuordnung geändert. Erneut starten."));
                ReadSignalInventory();var mast=objects.Objects.FirstOrDefault(m=>m.Id==physical);string id=physical;
                if(worldBoundaryPick) {
                    var boundary=CircuitEndpoint(physical);if(boundary==null||!CircuitIdentity.Enabled(boundary))throw new InvalidOperationException(LocalText.Select("Select an active GFM box.","Einen aktiven GFM-Kasten auswählen."));
                    id=CanonicalBoundaryId(physical);
                } else {
                    if(mast!=null)id=WorldSignalPickFace.Resolve(mast,direction);
                    var signal=Object(id);if(signal?.IsSignal!=true||signal.Planned||worldSignalPickMain&&signal.Kind!=MarkerRecord.Main)
                        throw new InvalidOperationException(LocalText.Select("Choose an available signal face.","Eine verfügbare Signalrichtung wählen."));
                }
                var accept=worldSignalPick;var resume=worldSignalPickReturn;worldSignalPick=null;worldSignalPickReturn=null;
                worldSelectionInput.Complete();objects.CloseForWorldSelection();resume?.Invoke();accept(id);RefreshWorkspaceFields();
            });return true;
        }
        internal string WorldSelectionHint(string id) {
            if(worldSignalPick==null||worldSignalPickScope!=scope)return null;
            if(worldBoundaryPick)return LocalText.Select("Click: select GFM boundary","Anklicken: GFM-Grenze auswählen");
            if(panelPage==15&&setupDiagramTarget!=null&&setupDiagramTarget.World==scope&&(setupTask==1||setupTask==3))return LocalText.Select("Click: assign to green field ","Anklicken: grünem Feld zuordnen · ")+setupDiagramTarget.Cell;
            return LocalText.Select("Click: select signal","Anklicken: Signal auswählen");
        }
        void ReadWorldSignalPickCancel() {
            ConsumeSelectionInput();
            if(worldSignalPick==null)return;
            if(scope!=worldSignalPickScope){worldSignalPick=null;worldSignalPickReturn=null;return;}
            if(Keyboard.current?.escapeKey.wasPressedThisFrame!=true)return;
            var resume=worldSignalPickReturn;worldSignalPick=null;worldSignalPickReturn=null;worldSelectionInput.Complete();resume?.Invoke();status=LocalText.Select("Signal assignment cancelled.","Signalzuordnung abgebrochen.");
        }
        void BeginPanelSignalPick(int x,int y,bool readOnly=false) {
            RequirePanelEdit();string desk=deskId,worldScope=scope;var snapshot=panel.At(x,y)?.Clone();
            BeginWorldSignalPick(id=>{
                if(scope!=worldScope||!EditorContextPolicy.SameTarget(panel,x,y,snapshot))throw new InvalidOperationException(LocalText.Select("Target field changed. Start assignment again.","Zielfeld geändert. Zuordnung erneut starten."));
                bool neighbour=readOnly||snapshot.ReadOnlySignal||CurrentDesk?.IsMaster!=true&&StationAutomationPolicy.Owner(desks,id) is DeskDefinition owner&&owner.Id!=desk;
                if(snapshot.Reference!=id||snapshot.ReadOnlySignal!=neighbour)RequireIdleAdministration();
                OpenEditorField(x,y,EditorFieldCategory.Properties,false);
                if(tileDraft.Reference!=id) {
                    string oldBoundary=Object(tileDraft.Reference)?.PhysicalId??tileDraft.Reference;
                    if(tileDraft.CircuitBoundary==oldBoundary)tileDraft.CircuitBoundary=null;
                    tileDraft.CircuitRails=null;tileDraft.BlockId=tileDraft.SecondBlock="";
                }
                tileDraft.Reference=id;
                tileDraft.ReadOnlySignal=neighbour;
                if(tileDraft.CircuitBoundary==null&&CircuitIdentity.Enabled(Object(id)))tileDraft.CircuitBoundary=Object(id).PhysicalId??id;
                tileDraft.AlternateSignal=Object(id).Kind==MarkerRecord.Main&&Object(id).AllowSh1?id:null;LoadTileFields();SaveTile();
            },()=>{ShowWorkspace(true,4,false);EnterEditorDesk(desk);},false);
        }
        void BeginPanelBoundaryPick(int x,int y) {
            RequirePanelEdit();string desk=deskId,worldScope=scope;var snapshot=panel.At(x,y)?.Clone();
            if(snapshot==null||snapshot.Kind==PanelTileKind.Switch||PanelConnections.Count(RailPorts(snapshot))!=2)throw new InvalidOperationException(LocalText.Select("Choose a continuous two-port track field for the boundary.","Für die Grenze ein durchgehendes Gleisfeld mit zwei Anschlüssen wählen."));
            BeginWorldSignalPick(id=>{
                if(scope!=worldScope||!EditorContextPolicy.SameTarget(panel,x,y,snapshot))throw new InvalidOperationException(LocalText.Select("Target field changed. Start assignment again.","Zielfeld geändert. Zuordnung erneut starten."));
                RequireIdleAdministration();var next=snapshot.Clone();next.CircuitBoundary=id;next.BlockId=next.SecondBlock=next.AutoSegment="";next.CircuitRails=null;next.Section=null;EditPanel(doc=>doc.Put(next));selectedX=x;selectedY=y;tileDraft=next;LoadTileFields();CloseWorkspaceContext();
                status=LocalText.Select("GFM boundary assigned. Detect adjoining sections to update occupancy.","GFM-Grenze zugeordnet. Angrenzende Abschnitte für die Belegungsanzeige erkennen.");
            },()=>{ShowWorkspace(true,4,false);EnterEditorDesk(desk);},false,true);
        }
    }
}
