using System;
using System.Linq;
using UI.Builder;
using UI.Windows;
using UnityEngine;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        const string RouteChoiceWindowId="enusde.signaltechnik.route-choice";
        CustomWindowDefinition routeChoiceDefinition;
        void CloseRouteChoiceWindow(){if(manager?.GetCustomWindow(RouteChoiceWindowId)!=null)manager.CloseWindow(RouteChoiceWindowId);}
        void OpenRouteChoiceWindow() {
            if(automaticChoices.Count==0)return;EnsureWindow();if(manager==null)return;
            if(routeChoiceDefinition==null){routeChoiceDefinition=new CustomWindowDefinition{Identifier=RouteChoiceWindowId,Title=WindowText("Choose route","Fahrweg auswählen"),BuilderAction=BuildRouteChoiceWindow,Config=WindowConfig.Resizable(new Vector2Int(680,460),new Vector2Int(520,280),new Vector2Int(1200,900),Window.Position.CenterRight)};manager.RegisterCustomWindow(routeChoiceDefinition);}
            var handle=CustomWindowHandle.Create(RouteChoiceWindowId);handle?.Show();handle?.SetNeedsRebuild();
        }
        void BuildRouteChoiceWindow(UIPanelBuilder b) {
            var snapshot=automaticChoices.ToArray();string selectedWorld=automaticWorld;
            if(snapshot.Length==0||selectedWorld!=scope||editingWorkspace){b.AddLabel(WindowText("No current route choice.","Keine aktuelle Fahrwegauswahl."));return;}
            b.AddLabel(WindowText("Destination: ","Ziel: ")+SignalCaption(automaticDestination));
            var captions=RouteChoiceLabels.Captions(snapshot,Code,id=>PointAssignments.Caption(desks,id));
            b.VScrollView(c=>{
                for(int i=0;i<snapshot.Length;i++) {
                    int selected=i;var plan=snapshot[i];
                    c.AddButtonLarge(captions[i],()=>Act(()=>{
                        if(automaticWorld!=selectedWorld||scope!=selectedWorld||!snapshot.SequenceEqual(automaticChoices))throw new InvalidOperationException(WindowText("Route choice is outdated. Select start and destination again.","Fahrwegauswahl ist veraltet. Start und Ziel erneut wählen."));
                        CommitAutomaticPlan(snapshot[selected]);CloseWorkspaceContext();RefreshWorkspaceFields();panelView?.Refresh();
                    }));
                    c.AddLabel(Math.Round(RouteChoiceLabels.Length(plan))+" m · "+plan.Routes.SelectMany(r=>r.Switches).Where(s=>s.FlankOf==null).Select(s=>s.Node).Distinct().Count()+WindowText(" points"," Weichen"));
                }
            });
            b.AddButton(WindowText("Cancel","Abbrechen"),()=>{ClearAutomaticRouteChoices();panelButtons.ClearEnd();RefreshWorkspaceFields();});
        }
    }
}
