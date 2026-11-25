using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Track;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        int setupTask;
        string setupScope;
        void OpenSetupAssistant() {
            RequirePanelEdit();if(setupScope!=scope){setupTask=0;setupDiagramTarget=null;setupConstructionExpanded=false;setupSkippedFields.Clear();setupScope=scope;}
            WorkspacePage(15);
        }
        void BuildSetupAssistant(UIPanelBuilder c) {
            c.AddSection(WindowText("Set up and check","Einrichten und prüfen"));
            if(setupTask==0) {
                c.AddButton(WindowText("Module problems and next steps","Modulprobleme und nächste Schritte"),()=>Act(OpenInstallationAudit));
                c.AddButton(WindowText("Assign signals and points","Signale und Weichen zuordnen"),()=>Act(StartDiagramFieldSetup));
                c.AddButton(WindowText("Detect and assign GFM in this diagram","GFM in dieser Tafel erkennen und zuordnen"),()=>Act(OpenExistingDetection));
                c.AddButton(WindowText("Inspect all detection gaps and stale boundaries","Alle Freimeldelücken und veralteten Grenzen prüfen"),()=>Act(OpenGfmInspection));
                c.AddButton(WindowText("Enable or review waypoint automation…","Wegpunkt-Automatik einschalten oder prüfen …"),Entry.OpenAutomationSettings);
                c.AddButton(WindowText("Set up or change a line / GWB connection","Strecken-/GWB-Verbindung einrichten oder ändern"),()=>Act(()=>{guidedLineSetup=true;guidedLineStep=0;OpenFreeLines();if(lineDraft==null)NewLineDraft();}));
                c.AddButton(WindowText("Configure station receiving capacity","Annahmekapazität der Betriebsstelle einrichten"),()=>{setupTask=2;RefreshWorkspaceFields();});return;
            }
            c.AddButton(WindowText("Choose a different setup task","Andere Einrichtung wählen"),()=>{setupTask=0;setupDiagramTarget=null;setupConstructionExpanded=false;setupFocusPending=false;panelView?.Refresh();RefreshWorkspaceFields();});
            if(setupScope!=scope){c.AddLabel(WindowText("World changed. Restart the assistant.","Welt geändert. Assistent erneut starten."));return;}
            if(setupTask==2){BuildStationCapacitySetup(c);return;}
            BuildDiagramFieldSetup(c);
        }
    }
}
