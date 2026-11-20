using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public static class WorldPointerInput {
        static readonly List<RaycastResult> hits=new List<RaycastResult>();
        public static bool OverUi() {
            var events=EventSystem.current;if(events==null)return false;
            if(events.IsPointerOverGameObject())return true;
            var mouse=Mouse.current;if(mouse==null)return false;
            hits.Clear();
            try{events.RaycastAll(new PointerEventData(events){position=mouse.position.ReadValue()},hits);return hits.Exists(h=>h.module is GraphicRaycaster);}
            finally{hits.Clear();}
        }
    }
}
