namespace EnusDE.Signaltechnik {
    // A completed selection owns its input until both buttons have been released
    // for a complete frame. The trailing event must not edit or throw the object.
    public sealed class WorldSelectionInput {
        bool waiting;
        int releasedFrame=-1;
        public void Complete(){waiting=true;releasedFrame=-1;}
        public void Reset(){waiting=false;releasedFrame=-1;}
        public bool Consumes(int frame,bool mouseHeld) {
            if(!waiting)return false;
            if(mouseHeld){releasedFrame=-1;return true;}
            if(releasedFrame<0){releasedFrame=frame;return true;}
            if(frame<=releasedFrame)return true;
            waiting=false;return false;
        }
    }
}
