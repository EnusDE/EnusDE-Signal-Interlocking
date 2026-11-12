using System;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public enum PlacementWork { World,Input,Pointer,Objects,Signs }
    // Optional sign appearance cannot prevent world preparation, mouse input or objects.
    public sealed class PlacementWorkLoop {
        readonly Dictionary<PlacementWork,Action> tasks;
        readonly Dictionary<PlacementWork,string> errors=new Dictionary<PlacementWork,string>();
        readonly Action<PlacementWork,string> report;
        double nextWorld,nextPointer,nextObjects,nextSigns;
        public PlacementWorkLoop(Action world,Action input,Action pointer,Action objects,Action signs,Action<PlacementWork,string> report) {
            tasks=new Dictionary<PlacementWork,Action>{{PlacementWork.World,world},{PlacementWork.Input,input},{PlacementWork.Pointer,pointer},{PlacementWork.Objects,objects},{PlacementWork.Signs,signs}};this.report=report;
        }
        public string Failure(PlacementWork work){string value;return errors.TryGetValue(work,out value)?value:null;}
        public void Tick(double now,bool aiming) {
            if(now>=nextWorld){nextWorld=now+1;Run(PlacementWork.World);}
            if(aiming&&now>=nextPointer){nextPointer=now+.1;Run(PlacementWork.Pointer);}
            Run(PlacementWork.Input);
            if(now>=nextObjects){nextObjects=now+1;Run(PlacementWork.Objects);}
            if(now>=nextSigns){nextSigns=now+2;Run(PlacementWork.Signs);}
        }
        void Run(PlacementWork work) {
            try{tasks[work]?.Invoke();if(errors.Remove(work))report?.Invoke(work,null);}
            catch(Exception e){string text=e.Message??e.GetType().Name;if(text.Length>400)text=text.Substring(0,400);string previous;if(!errors.TryGetValue(work,out previous)||previous!=text){errors[work]=text;report?.Invoke(work,text);}}
        }
    }
}
