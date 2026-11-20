using System;
using System.Collections.Generic;
using System.Linq;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public static class SignalRoleText {
        public static string Caption(SignalDuty role)=>role==SignalDuty.Entry?LocalText.Select("Entry signal","Einfahrsignal"):
            role==SignalDuty.Exit?LocalText.Select("Exit signal","Ausfahrsignal"):role==SignalDuty.Intermediate?LocalText.Select("Intermediate signal","Zwischensignal"):
            role==SignalDuty.Block?LocalText.Select("Block signal","Blocksignal"):LocalText.Select("No special role","Ohne besondere Funktion");
        public static string Description(SignalDuty role)=>role==SignalDuty.Entry?LocalText.Select("Controls entry from the open line into a station. Used as a station boundary by block coordination.","Regelt die Einfahrt von der freien Strecke in den Bahnhof. Wird von der Blockabstimmung als Bahnhofsgrenze verwendet."):
            role==SignalDuty.Exit?LocalText.Select("Controls departure from a station onto the open line. Used as a station boundary by block coordination.","Regelt die Ausfahrt aus dem Bahnhof auf die freie Strecke. Wird von der Blockabstimmung als Bahnhofsgrenze verwendet."):
            role==SignalDuty.Intermediate?LocalText.Select("Controls train movements between parts of a station. Does not mark the outer station boundary.","Regelt Zugfahrten zwischen Bahnhofsteilen. Markiert keine äußere Bahnhofsgrenze."):
            role==SignalDuty.Block?LocalText.Select("Main signal for a block section on the open line. Does not mark a station boundary.","Hauptsignal für einen Blockabschnitt auf der freien Strecke. Markiert keine Bahnhofsgrenze."):
            LocalText.Select("Main signal without a special operating role. No station boundary is inferred from this role.","Hauptsignal ohne besondere betriebliche Rolle. Aus dieser Rolle wird keine Bahnhofsgrenze abgeleitet.");
        public static List<string> Choices()=>Enum.GetValues(typeof(SignalDuty)).Cast<SignalDuty>().Select(Caption).ToList();
    }
    public static class SignalRolePolicy {
        public static SignalDuty Current(DeskLibrary library,string id)=>library.Automation.Signals.TryGetValue(id,out var signal)?signal.Duty:SignalDuty.Ordinary;
        public static void RequireLineCompatibility(DeskLibrary library,string id,SignalDuty role) {
            if(!Enum.IsDefined(typeof(SignalDuty),role))throw new ArgumentOutOfRangeException(nameof(role));
            foreach(var line in library.FreeLines.Where(l=>l.Enabled)) {
                if(LineSignalGroups.Used(line).Any(group=>LineSignalGroups.Get(line,group).Contains(id)&&role!=(LineSignalGroups.Departure(group)?SignalDuty.Exit:SignalDuty.Entry)))
                    throw new InvalidOperationException(LocalText.Select("This signal is an endpoint of an enabled open line. Change that line's configuration before changing its entry/exit role.","Dieses Signal ist Endpunkt einer aktiven freien Strecke. Zuerst deren Einrichtung ändern, bevor die Ein-/Ausfahrrolle geändert wird."));
            }
        }
        public static void Put(DeskLibrary library,string id,SignalDuty role) {
            RequireLineCompatibility(library,id,role);
            if(!library.Automation.Signals.TryGetValue(id,out var signal))library.Automation.Signals[id]=signal=new AutomaticSignal();
            signal.Duty=role;
        }
    }
    public sealed partial class SignalRuntime {
        internal void BuildSignalRoleEditor(UIPanelBuilder c,string id,Action verify=null) {
            if(Object(id)?.Kind!=MarkerRecord.Main){c.AddLabel(WindowText("Save and enable this main signal direction first to select its operating role.","Diese Hauptsignalrichtung zuerst aktivieren und speichern, um ihre betriebliche Rolle auszuwählen."));return;}
            string selectedScope=scope;SignalDuty original=SignalRolePolicy.Current(desks,id),draft=original;
            c.AddSection(WindowText("Main signal operating role","Betriebliche Hauptsignalrolle"));
            c.AddField(WindowText("Signal direction","Signalrichtung"),Code(id));
            c.AddField(WindowText("Role","Rolle"),c.AddDropdown(SignalRoleText.Choices(),(int)draft,i=>draft=(SignalDuty)i));
            c.AddLabel(()=>SignalRoleText.Description(draft),UIPanelBuilder.Frequency.Periodic);
            c.AddLabel(WindowText("The role does not change the automatic code, enable target automation or configure an open line. Each enabled mast direction has its own role.","Die Rolle ändert keine automatische Kennung, schaltet keine Zielautomatik ein und richtet keine freie Strecke ein. Jede aktive Mastrichtung hat eine eigene Rolle."));
            c.AddField(WindowText("Role status","Rollenstatus"),()=>workspaceActionError??SignalRoleText.Caption(SignalRolePolicy.Current(desks,id)),UIPanelBuilder.Frequency.Periodic);
            c.AddButton(WindowText("Apply signal role","Signalrolle übernehmen"),()=>Act(()=>{
                RequireEdit();verify?.Invoke();if(scope!=selectedScope||Object(id)?.Kind!=MarkerRecord.Main||SignalRolePolicy.Current(desks,id)!=original)
                    throw new InvalidOperationException(WindowText("Signal or role changed. Reopen its properties.","Signal oder Rolle geändert. Eigenschaften erneut öffnen."));
                if(draft!=original){RequireIdleAdministration();SaveAutomation(d=>SignalRolePolicy.Put(d,id,draft));}
                status=WindowText("Signal role applied: ","Signalrolle übernommen: ")+Code(id)+" · "+SignalRoleText.Caption(draft);c.SetNeedsRebuild();
            }));
            BuildSignalOperatingEditor(c,id,verify);
        }
    }
}
