using System;
using System.Linq;
using Game.State;
using UI.Builder;
using UnityEngine;
namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly SignalLampTest lampTest=new SignalLampTest();
        internal string LampTestOwner=>lampTest.PhysicalId;
        internal LocalSignalAspect LampVisual(string id,LocalSignalAspect actual)=>lampTest.Visual(id,actual);
        internal SignalLampPlan LampPlan(string id,SignalLampPlan actual)=>lampTest.Plan(id,actual);
        internal void EndLampTest() {
            if(!lampTest.Active)return;lampTest.Clear();nativeDirty=true;
            if(!stopped&&!StateManager.IsUnloading){RefreshProxies();PublishNativeAspects();}
        }
        internal void StartLampTest(MarkerRecord row,string face,LampTestPart part,LocalSignalAspect aspect) {
            RequireEdit();
            var current=objects.Objects.FirstOrDefault(r=>r.Id==row.Id&&r.IsSignal);
            if(current==null)throw new InvalidOperationException(LocalText.Select("Signal no longer exists.","Signal nicht mehr vorhanden."));
            if(current.Kind==MarkerRecord.Main){var mast=MastConfiguration.Effective(current);var f=face==mast.Front.Id?mast.Front:face==mast.Back.Id?mast.Back:null;if(f==null||!f.Enabled||part==LampTestPart.Distant&&!f.DistantHead||part==LampTestPart.Shunt&&!f.Sh1)throw new InvalidOperationException(LocalText.Select("Select an enabled, installed head.","Einen eingeschalteten, angebauten Kopf auswählen."));}
            else if(face!=current.Id||part!=LampTestPart.Main)throw new InvalidOperationException("Invalid standalone lamp test.");
            lampTest.Start(current.Id,face,SignalCatalogue.Ids(current),part,aspect,Time.realtimeSinceStartup,StateManager.IsHost,Entry.BuildModeActive,OperatingReady&&!current.Planned,SignalCatalogue.Ids(current).All(CanModify));
            nativeDirty=true;RefreshProxies();PublishNativeAspects();
        }
        internal void BuildLampTests(UIPanelBuilder c,MarkerRecord row,int direction,Action<Action> execute) {
            c.AddSection(LocalText.Select("Individual signal lamp test · host","Einzelne Signal-Testansteuerung · Host"));
            c.AddLabel(LocalText.Select("TEST: visual indication only, no route or movement authority. Native signalling holds this mast at Stop. Test ends after 2 minutes, selection change or leaving Build mode.","TEST: nur Lichtbild, keine Fahrstraße oder Fahrerlaubnis. Die native Signalisierung hält diesen Mast auf Halt. Ende nach 2 Minuten, Auswahlwechsel oder Verlassen des Baumodus."));
            c.AddField(LocalText.Select("Lamp test","Lampentest"),()=>lampTest.Active?"TEST · "+Code(lampTest.FaceId)+" · "+LampPartCaption(lampTest.Part)+" · "+LampCaption(lampTest.Aspect):LocalText.Select("Off · operating indication","Aus · Betriebsanzeige"),UIPanelBuilder.Frequency.Periodic);
            if(row.Planned){c.AddLabel(LocalText.Select("Complete construction before testing the installed lights.","Vor dem Test der eingebauten Leuchten den Bau fertigstellen."));return;}
            var face=row.Kind==MarkerRecord.Main?(direction==1?MastConfiguration.Effective(row).Back:MastConfiguration.Effective(row).Front):null;
            if(face!=null&&!face.Enabled){c.AddLabel(LocalText.Select("This mast direction is disabled.","Diese Mastrichtung ist ausgeschaltet."));return;}
            string id=face?.Id??row.Id;
            void Buttons(LampTestPart part,string title,LocalSignalAspect[] choices){c.AddLabel(title);foreach(var a in choices){var chosen=a;c.AddButton(LampCaption(a),()=>execute(()=>{if(Entry.SelectedBuildSignal!=row.Id)throw new InvalidOperationException(LocalText.Select("Select the signal again.","Signal erneut auswählen."));StartLampTest(row,id,part,chosen);}));}}
            var normal=new[]{LocalSignalAspect.Stop,LocalSignalAspect.Clear,LocalSignalAspect.Slow,LocalSignalAspect.Dark,LocalSignalAspect.Invalid,LocalSignalAspect.Kennlicht};
            Buttons(LampTestPart.Main,row.Kind==MarkerRecord.Shunt?LocalText.Select("Shunting head","Rangierkopf"):row.Kind==MarkerRecord.Distant?LocalText.Select("Distant head","Vorsignalkopf"):LocalText.Select("Main head","Hauptkopf"),row.Kind==MarkerRecord.Shunt?new[]{LocalSignalAspect.Stop,LocalSignalAspect.Shunt,LocalSignalAspect.Dark,LocalSignalAspect.Invalid,LocalSignalAspect.Kennlicht}:normal);
            if(face?.DistantHead==true){c.AddLabel(LocalText.Select("Distant-head test uses a visual Clear on the main head; native authority remains Stop.","Vorsignalkopftest zeigt am Hauptkopf Test-Fahrt; native Fahrerlaubnis bleibt Halt."));Buttons(LampTestPart.Distant,LocalText.Select("Attached distant head","Angebauter Vorsignalkopf"),normal);}
            if(face?.Sh1==true)Buttons(LampTestPart.Shunt,LocalText.Select("Attached shunting head","Angebauter Rangierkopf"),new[]{LocalSignalAspect.Stop,LocalSignalAspect.Shunt,LocalSignalAspect.Dark,LocalSignalAspect.Invalid,LocalSignalAspect.Kennlicht});
            c.AddButton(LocalText.Select("End lamp test · restore operating indication","Lampentest beenden · Betriebsanzeige wiederherstellen"),()=>execute(EndLampTest));
        }
        static string LampPartCaption(LampTestPart part)=>part==LampTestPart.Distant?LocalText.Select("Distant head","Vorsignalkopf"):part==LampTestPart.Shunt?LocalText.Select("Shunting head","Rangierkopf"):LocalText.Select("Main head","Hauptkopf");
        static string LampCaption(LocalSignalAspect a)=>a==LocalSignalAspect.Stop?LocalText.Select("Stop / expect stop","Halt / Halt erwarten"):a==LocalSignalAspect.Clear?LocalText.Select("Clear / expect clear","Fahrt / Fahrt erwarten"):a==LocalSignalAspect.Slow?LocalText.Select("Slow / expect slow","Langsamfahrt / Langsamfahrt erwarten"):a==LocalSignalAspect.Shunt?LocalText.Select("Shunting permission","Rangierfreigabe"):a==LocalSignalAspect.Dark?LocalText.Select("Dark","Dunkel"):a==LocalSignalAspect.Invalid?LocalText.Select("Invalid","Ungültig"):LocalText.Select("Identification light","Kennlicht");
    }
}
