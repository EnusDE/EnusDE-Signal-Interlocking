using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class MarkerRuntime {
        void AlignPlacement(MarkerAnchor anchor,MarkerOptions defaults) {
            if(edit.EditingId!=null)return;bool reverse=edit.FacingReverse;
            if(defaults.FollowView&&Camera.main!=null){Vector3 p;Quaternion rotation;string why;if(geometry.Resolve(anchor,out p,out rotation,out why)){var tangent=rotation*Vector3.forward;var view=Camera.main.transform.forward;if(view.x*view.x+view.z*view.z<.0001f)view=Camera.main.transform.up;reverse=PlacementDirection.Reverse(tangent.x,tangent.z,view.x,view.z,reverse);}}
            edit.Face(reverse);anchor.Lateral=PlacementDirection.Side(defaults.Lateral,defaults.DefaultRightSide,reverse);
        }
        MarkerOptions CurrentDefaults() {var value=new MarkerOptions{Visible=options.Visible,UseGameSpacing=options.UseGameSpacing,Lateral=options.Lateral,Height=options.Height,Scale=options.Scale,DefaultRightSide=options.DefaultRightSide,FollowView=options.FollowView,NewSignalsWithGfm=options.NewSignalsWithGfm};if(Entry.Shared?.Ready==true)Entry.Shared.ApplyPlacementDefaults(value);return PlacementDefaults.Resolve(value);}
    }
}
