namespace EnusDE.Signaltechnik {
    // Local view/tool state only. Accepted plans remain in the host's installation.
    public sealed class BuildModeSession {
        public bool Active {get;private set;}
        public bool Begin(bool allowed){if(!allowed)return false;Active=true;return true;}
        public bool End(){bool wasActive=Active;Active=false;return wasActive;}
    }
    public enum ConstructionVisual { Finished, Plan, Site }
    public static class ConstructionViewPolicy {
        public static ConstructionVisual For(bool planned,bool building)=>!planned?ConstructionVisual.Finished:building?ConstructionVisual.Plan:ConstructionVisual.Site;
    }
}
