using System;
using System.Linq;
using Game.State;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        bool SignalHasRouteControls(string signal)=>!string.IsNullOrEmpty(signal)&&ActiveHere().Any(r=>
            r.Route.Entry==signal||r.Route.Exit==signal||r.Route.ViaSignals?.Any(s=>s.Id==signal)==true||
            r.Route.Flanks?.SelectMany(f=>f.Signals).Any(s=>s.Id==signal)==true);

        void BuildDispatcherFields(UIPanelBuilder c) {
            if(panelPage==4){BuildDeskPicker(c);return;}
            if(panelPage==5){BuildHelp(c);return;}
            if(!StateManager.IsHost){BuildClientInspector(c);return;}
            if(panelPage==2){BuildDispatcherRoutes(c);return;}
            var tile=panel?.At(selectedX,selectedY);
            if(tile==null){BuildActive(c);return;}
            if(tile.Kind==PanelTileKind.TrainNumber)BuildDispatcherNumberField(c,tile);
            if(tile.Kind==PanelTileKind.AuxiliaryTarget) {
                c.AddLabel(tile.Target==null?"Hilfsziel ist noch unkonfiguriert.":
                    tile.Target.Caption+" · "+(tile.Target.Train&&tile.Target.Shunt?"Zug- und Rangierziel":tile.Target.Shunt?"Rangierziel":"Zugfahrziel"));
                c.AddLabel("Startsignal und anschließend das Hilfsziel auf der Tafel anklicken.");
            }
            if(tile.Kind==PanelTileKind.Signal&&Object(tile.Reference)?.Kind==MarkerRecord.Distant)
                c.AddLabel(Code(tile.Reference)+" · Vorsignal");
            else if(tile.Kind==PanelTileKind.Signal) {
                bool right=dispatcherContextRight;
                bool yard=SignalCellLayout.ShuntHalf(tile,right);
                string picked=SignalCellLayout.SignalAt(tile,right);
                c.HStack(row=>{
                    if(!yard) {
                        row.AddButton(WindowText("Train","Zugfahrt"),()=>Act(()=>StartFrom(picked,false)));
                        row.AddButton(WindowText("Reduced","Langsamfahrt"),()=>Act(()=>StartFrom(picked,false,true)));
                    }
                    if(tile.AlternateSignal==null||yard)
                        row.AddButton(WindowText("Shunt","Rangieren"),()=>Act(()=>StartFrom(picked,true)));
                    row.AddButton(WindowText("Set to here","Ziel / stellen"),()=>PressCell(tile.X,tile.Y,right));
                });
                if(panelButtons.Start!=null&&AuxiliaryEndpoint(tile.LinkedAuxiliary)!=null)
                    c.AddButton("Hilfsziel dahinter",()=>Act(()=>{
                        var endpoint=AuxiliaryEndpoint(tile.LinkedAuxiliary);
                        if(endpoint==null||!AuxiliaryTarget.Accepts(endpoint,shunting))
                            throw new InvalidOperationException("Dieses Hilfsziel ist für die gewählte Fahrart nicht eingerichtet.");
                        CloseWorkspaceContext();
                        RequestSelectedDestination(endpoint.Id);
                    }));
            }
            if(tile.Kind==PanelTileKind.Switch){c.AddField("Weiche",()=>PointAssignments.Caption(desks,tile.Reference)+" · "+PointPosition(tile.Reference),UIPanelBuilder.Frequency.Periodic);c.HStack(row=>{row.AddButton("+ Grundstellung",()=>Act(()=>SwitchSelected(false)));row.AddButton("− Abzweig",()=>Act(()=>SwitchSelected(true)));});}
            else if(tile.Kind!=PanelTileKind.Signal)c.AddField("Belegung",()=>PanelOccupancy(tile).ToString(),UIPanelBuilder.Frequency.Periodic);
            bool HasSignal(RouteRun run,string id)=>!string.IsNullOrEmpty(id)&&(run.Route.Entry==id||run.Route.Exit==id||
                run.Route.ViaSignals?.Any(v=>v.Id==id)==true||run.Route.Flanks?.SelectMany(f=>f.Signals).Any(s=>s.Id==id)==true);
            var related=ActiveHere().Where(r=>HasSignal(r,tile.Reference)||HasSignal(r,tile.AlternateSignal)||
                tile.Reference!=""&&r.Route.Switches.Any(p=>p.Node==tile.Reference)||
                tile.Section!=null&&r.HeldSections.Any(s=>s.Overlaps(tile.Section))).Take(3).ToArray();
            foreach(var run in related){c.AddSection(run.Route.Code);c.AddField("Melder",()=>run.Reason+(string.IsNullOrEmpty(run.ReleaseStatus)?"":" · "+run.ReleaseStatus),UIPanelBuilder.Frequency.Periodic);c.AddField("Flankenschutz",FlankCaption(run.Route));c.AddButton("Halt / auflösen",()=>Act(()=>{routeId=run.Route.Id;CancelRoute();}));BuildRelease(c,run);}
        }
        string dispatcherRouteId;
        void BuildDispatcherRoutes(UIPanelBuilder c) {
            var choices=routes.Where(RouteHere).ToArray();
            if(choices.Length>0) {
                if(!choices.Any(r=>r.Id==dispatcherRouteId))dispatcherRouteId=choices[0].Id;
                c.AddField(WindowText("Route","Fahrstraße"),c.AddDropdown(choices.Select(r=>r.Code+" · "+SignalCaption(r.Entry)+" → "+SignalCaption(r.Exit)).ToList(),Array.FindIndex(choices,r=>r.Id==dispatcherRouteId),i=>dispatcherRouteId=choices[i].Id));
                c.HStack(row=>{
                    row.AddButton(WindowText("Set","Stellen"),()=>Act(()=>{routeId=dispatcherRouteId;SetRoute();}));
                    row.AddButton(WindowText("Set with following routes","Mit Folgestraßen stellen"),()=>Act(()=>{routeId=dispatcherRouteId;SetFollowingRoutes();}));
                });
            } else c.AddLabel(WindowText("No stored routes on this diagram.","Keine gespeicherten Fahrstraßen auf dieser Tafel."));
            BuildActive(c);
        }
        string FlankCaption(RouteDefinition route)=>route.Shunting?"Rangierfahrstraße: kein eigener Flankenschutz erforderlich":route.Flanks==null?"Alter Fahrweg: vor Stellen Fahrweg/Flankenschutz aktualisieren":route.Flanks.Count==0?"Keine seitlichen Weichenäste im Fahrweg":string.Join(" · ",route.Flanks.Select(f=>PointAssignments.Caption(desks,f.Point)+": "+(f.MonitorOnly?"teilweise nur überwacht":"Schutzeinrichtung zugeordnet")+(f.Signals.Count==0?"":" / Halt "+string.Join(",",f.Signals.Select(s=>Code(s.Id))))));
        void BuildHelp(UIPanelBuilder c) {
            c.AddTitle(WindowText("Signal & Interlocking handbook","Signaltechnik – Handbuch"),Entry.Version);
            if(editingWorkspace) {
                c.AddSection(WindowText("Draw and link","Zeichnen und zuordnen"));
                c.AddLabel(WindowText("Draw tracks by clicking each corner. In Fields and links, choose a symbol and click a cell. Right-click a field to edit its links. Delete removes the selected cell. Removing a selection of several cells requires confirmation.","Gleise über Eckpunkte zeichnen. Unter Felder und Zuordnungen ein Symbol wählen und ins Raster setzen. Rechtsklick öffnet die Zuordnung eines Felds. Entf löscht das einzelne ausgewählte Feld. Das Löschen eines markierten Bereichs braucht eine Bestätigung."));
                c.AddLabel(WindowText("To link a real switch, right-click the track near it in the game, choose the interlocking, then select its field in the editor.","Für eine reale Weiche das Gleis in ihrer Nähe im Spiel rechtsklicken, Stellwerk wählen und das Zielfeld im Editor zuordnen."));
                c.AddLabel(WindowText("Use Configure routes to save or recalculate the actual path between two signals.","Unter Fahrstraßen einrichten den tatsächlichen Fahrweg zwischen zwei Signalen speichern oder neu berechnen."));
            } else {
                c.AddSection(WindowText("Operate","Bedienen"));
                c.AddLabel(WindowText("Choose a start signal and a destination. Right-click a signal to select the type of movement. The route list also offers stored routes and route chains.","Startsignal und Zielsignal wählen. Rechtsklick auf das Signal öffnet die Wahl der Fahrart. Die Fahrstraßenliste bietet auch gespeicherte Fahrstraßen und Folgen."));
                c.AddLabel(WindowText("Red indicates occupied track. Green indicates a set route. Flashing yellow indicates setting or protection time. White at a shunting signal indicates permission.","Rot zeigt Belegung, Grün eine festgelegte Fahrstraße. Gelb blinkt beim Einlaufen oder während der Schutzwartezeit. Weiß am Rangiersignal zeigt die Zustimmung."));
                c.AddLabel(WindowText("The active route list shows held locks and release actions. Manual release requires a separate confirmation and the configured protection time.","Die Liste aktiver Fahrstraßen zeigt Restverschlüsse und Auflösung. Die örtlich geprüfte Hilfsauflösung braucht eine eigene Bestätigung und die eingestellte Schutzwartezeit."));
            }
            c.AddButton(WindowText("Back to the diagram","Zurück zum Gleisbild"),()=>{CloseWorkspaceContext();Mode(editingWorkspace?1:0);});
        }
    }
}
