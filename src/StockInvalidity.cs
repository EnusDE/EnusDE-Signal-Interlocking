using System;
using System.IO;
using System.Linq;
namespace EnusDE.Signaltechnik {
    // A saved invalidity request does not depend on loaded scenery or a shader.
    public static class StockInvalidity {
        public static RetiredSignal Request(string id,bool ignoreAi) {
            var value=new RetiredSignal{Id=id,IgnoreAi=ignoreAi,Visual=true,AutoPlacement=true,RelativeToModel=true,Offset=new float[3],Normal=new float[]{0,0,1}};
            value.Validate();return value;
        }
        public static RetiredSignal Prepare(string id,bool ignoreAi,Func<RetiredSignal> readPose,Action<Exception> warning) {
            var value=Request(id,ignoreAi);
            try{var pose=readPose?.Invoke();if(pose!=null){pose.Validate();if(pose.Id!=id)throw new InvalidDataException("Kreuzposition gehört zu einem anderen Signal.");value.Offset=(float[])pose.Offset.Clone();value.Normal=(float[])pose.Normal.Clone();value.RelativeToModel=pose.RelativeToModel;value.AutoPlacement=false;}}
            catch(Exception error){try{warning?.Invoke(error);}catch{/* Optional reporting cannot cancel invalidity. */}}
            return value;
        }
        public static InstallationDocument Put(InstallationDocument current,RetiredSignal value) {
            if(current==null||value==null)throw new InvalidDataException("Anlagenbestand oder Markierung fehlt.");
            value.Validate();var next=current.Clone();next.Retired.RemoveAll(r=>r.Id==value.Id);next.Retired.Add(value.Clone());next.Validate();return next;
        }
        public static InstallationDocument Remove(InstallationDocument current,string id){if(current==null)throw new InvalidDataException("Anlagenbestand fehlt.");var next=current.Clone();next.Retired.RemoveAll(r=>r.Id==id);next.Validate();return next;}
        public static bool Ignore(bool host,bool own,string id,System.Collections.Generic.IEnumerable<RetiredSignal> records)=>host&&!own&&id!=null&&records!=null&&records.Any(r=>r.Id==id&&r.IgnoreAi);
        public static bool SamePlacement(RetiredSignal a,RetiredSignal b)=>a!=null&&b!=null&&a.Id==b.Id&&a.Visual==b.Visual&&a.AutoPlacement==b.AutoPlacement&&a.RelativeToModel==b.RelativeToModel&&a.Offset.SequenceEqual(b.Offset)&&a.Normal.SequenceEqual(b.Normal);
        public static string State(RetiredSignal value,bool available) {
            if(value==null)return "Gültig";
            if(!value.IgnoreAi)return "Optisch ungültig · KI berücksichtigt das Bestandssignal weiter";
            return available?"Ungültig · KI-Ausnahme aktiv":"Ungültig gespeichert · KI-Ausnahme derzeit NICHT aktiv";
        }
    }
}
