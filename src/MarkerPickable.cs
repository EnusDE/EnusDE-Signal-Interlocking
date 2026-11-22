using UnityEngine;

namespace EnusDE.Signaltechnik {
    // Same Clickable layer/IPickable protocol as the game's player signs.
    public sealed class MarkerPickable : MonoBehaviour,IPickable {
        public string ObjectId;
        public float MaxPickDistance=>1000;
        public int Priority=>1;
        public TooltipInfo TooltipInfo=>new TooltipInfo(Entry.MarkerCaption(ObjectId),Entry.WorldSelectionHint(ObjectId)??(TrackCircuit.TryPointId(ObjectId,out _,out _)?LocalText.Select("Automatic point box · click: edit position","Automatischer Weichenkasten · anklicken: Position bearbeiten"):LocalText.Select("Click: configure","Anklicken: konfigurieren")));
        public PickableActivationFilter ActivationFilter=>Entry.PlacingObject?PickableActivationFilter.None:PickableActivationFilter.Any;
        public void Activate(PickableActivateEvent value){if(Entry.PlacingObject||Entry.BlockWorldObjectClick)return;if(TrackCircuit.TryPointId(ObjectId,out _,out _)){if(Entry.TryPickWorldObject(ObjectId))return;if(Entry.WorkspaceEditable)Entry.OpenPointBoundary(ObjectId);return;}var camera=Camera.main;var mast=transform.parent;bool back=camera!=null&&mast!=null&&Vector3.Dot(camera.transform.position-mast.position,mast.forward)>0;Entry.ActivateMarker(ObjectId,back?1:0,value.Activation==PickableActivation.Secondary);}
        public void Deactivate(){}
        public static void Attach(GameObject root,MarkerRecord row,bool box=false,bool site=false) {
            var target=new GameObject("EnusDE object click target");target.layer=Helpers.Layers.Clickable;target.transform.SetParent(root.transform,false);
            var collider=target.AddComponent<BoxCollider>();float height=box?.40f:row.Kind==MarkerRecord.Main?(row.LowMain?LowMainGeometry.Height+.15f:MarkerMeshData.MastHeight+.15f):row.Kind==MarkerRecord.Distant?3.6f:row.Kind==MarkerRecord.Shunt?1.2f:row.Kind==MarkerRecord.Speed?2.1f:.6f;
            collider.center=new Vector3(0,height/2,0);collider.size=new Vector3(row.Kind==MarkerRecord.Clearance?.4f:.8f,height,.7f);
            var package=root.GetComponentInChildren<PackageSignalVisual>();if(package!=null&&!site){collider.center=package.LocalBounds.center;collider.size=package.LocalBounds.size;}
            if(row.Planned)collider.isTrigger=true;
            if(site){collider.center=new Vector3(0,.55f,0);collider.size=new Vector3(1.6f,1.1f,1);}
            target.AddComponent<MarkerPickable>().ObjectId=row.Id;
        }
    }
}
