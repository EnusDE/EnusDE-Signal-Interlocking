using System;
using System.Linq;
using System.Collections.Generic;
using UI.Builder;

namespace EnusDE.Signaltechnik {
    public sealed partial class SignalRuntime {
        readonly List<string> installationAudit=new List<string>();
        string installationAuditStatus;
        void OpenInstallationAudit(){RequirePanelEdit();ReadInstallationAudit();WorkspacePage(14);}
        void ReadInstallationAudit() {
            RequirePanelEdit();installationAudit.Clear();ReadSignalInventory();
            var network=CaptureNetwork();
            foreach(var block in desks.Blocks.Values.Where(b=>b.IsCircuit)) {
                string error;try{error=CircuitPathValidation.Error(network,block,CircuitEndpoint(block.BoundaryA),CircuitEndpoint(block.BoundaryB));}catch(Exception e){error=e.Message;}
                if(error!=null)installationAudit.Add(GfmPair(block)+" · "+error);
            }
            foreach(var desk in desks.Desks)foreach(var tile in desk.Panel.Tiles) {
                string place=desk.Code+" · "+(tile.X+1)+" / "+(tile.Y+1);
                if(tile.Kind==PanelTileKind.Signal&&Object(tile.Reference)==null)installationAudit.Add(place+" · "+WindowText("Missing signal binding","Signalzuordnung fehlt"));
                if(tile.Target?.Function==AuxiliaryFunction.RouteDestination&&!string.IsNullOrEmpty(tile.Target.ReferenceSignal)&&tile.Target.Endpoint(Object)==null)installationAudit.Add(place+" · "+WindowText("Auxiliary reference signal missing or not built. Select the reference again.","Hilfsziel-Bezugssignal fehlt oder ist nicht gebaut. Bezug erneut auswählen."));
                if(tile.Kind==PanelTileKind.TrainNumber&&NumberBindings(Entry.Shared).ConflictingField(tile))installationAudit.Add(place+" · "+WindowText("Several independent number fields own this signal","Mehrere eigenständige Nummernfelder für dieses Signal"));
            }
            foreach(var point in desks.Points.Keys)try{
                if(!world.SwitchPosition(point,out _))installationAudit.Add(PointAssignments.Caption(desks,point)+" · "+WindowText("Point missing from the loaded world","Weiche fehlt in der geladenen Spielwelt"));
                else if(PointCircuitVisibility.Enabled(desks,point)&&PointBoundaries(point).Count(b=>b!=null)!=3)installationAudit.Add(PointAssignments.Caption(desks,point)+" · "+WindowText("Point detection boundaries incomplete","Weichen-GFM-Grenzen unvollständig"));
            }catch(Exception e){installationAudit.Add(PointAssignments.Caption(desks,point)+" · "+e.Message);}
            foreach(var route in routes) {
                if(!world.PathValid(route))installationAudit.Add(route.Code+" · "+WindowText("Saved route bindings need recalculation","Gespeicherte Fahrwegzuordnung muss neu berechnet werden"));
                else try{FlankProtection.RequireCalculated(route);}catch(Exception e){installationAudit.Add(route.Code+" · "+e.Message);}
            }
            installationAuditStatus=WindowText("Objects / points / saved routes: ","Objekte / Weichen / gespeicherte Fahrwege: ")+objects.Objects.Count()+" / "+desks.Points.Count+" / "+routes.Count+
                WindowText(" · Issues: "," · Auffälligkeiten: ")+installationAudit.Count;
        }
        void RefreshInstallationElements() {
            RequirePanelEdit();RequireIdleAdministration();string previous=deskId;
            PauseGfmBackground();Entry.ReloadBackupConfiguration();ReadSignalInventory();
            numberIndex=null;targetLibrary=null;sharedPointCircuits=null;circuitValidationInstall=-1;circuitValidationConfiguration=-1;
            ShowWorkspace(true,4,false);if(desks.Desks.Any(d=>d.Id==previous))EnterEditorDesk(previous);
            ReadInstallationAudit();WorkspacePage(14);
            status=WindowText("All installation elements reloaded. Saved settings and IDs retained; review unresolved bindings below.","Alle Anlagenelemente neu eingelesen. Gespeicherte Einstellungen und IDs erhalten; ungeklärte Zuordnungen unten prüfen.");
        }
        void BuildInstallationAudit(UIPanelBuilder c) {
            c.AddSection(WindowText("Refresh and inspect installation","Anlage aktualisieren und prüfen"));
            c.AddLabel(WindowText("Reloads saved world objects, models, signal and point bindings, routes and occupancy readers. It keeps configuration and stable IDs. Missing or contradictory bindings are listed; detection paths are not guessed or deleted.","Liest gespeicherte Weltobjekte, Modelle, Signal- und Weichenzuordnungen, Fahrwege und Belegungsleser neu ein. Konfiguration und feste IDs bleiben erhalten. Fehlende oder widersprüchliche Zuordnungen werden aufgelistet; Meldewege werden nicht geraten oder gelöscht."));
            c.AddButton(WindowText("Refresh all installation elements","Alle Anlagenelemente aktualisieren"),()=>Act(RefreshInstallationElements));
            c.AddButton(WindowText("Inspect saved configuration again","Gespeicherte Konfiguration erneut prüfen"),()=>Act(()=>{ReadInstallationAudit();RefreshWorkspaceFields();}));
            if(installationAuditStatus!=null)c.AddLabel(installationAuditStatus);
            foreach(string issue in installationAudit.Take(100))c.AddLabel(issue);
            c.AddButton(WindowText("Open section manager / GFM inspection","Abschnittmanager / GFM-Prüfung öffnen"),OpenGfmInspection);
            c.AddButton(WindowText("Open GFM detection / rescan","GFM-Erkennung / Neuscan öffnen"),OpenExistingDetection);
            c.AddButton(WindowText("Export diagnostic ZIP","Diagnose-ZIP exportieren"),Entry.ExportDiagnostics);
            c.AddField(WindowText("Export result","Exportergebnis"),()=>Entry.DiagnosticExportStatus,UIPanelBuilder.Frequency.Periodic);
        }
    }
}
