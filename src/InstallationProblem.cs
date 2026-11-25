using System;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    public sealed class InstallationProblemReport {
        public readonly List<InstallationProblem> Items=new List<InstallationProblem>();
        public string Scope {get;private set;}
        public long Configuration {get;private set;}
        public long Installation {get;private set;}
        public void Begin(string scope,long configuration,long installation){Items.Clear();Scope=scope;Configuration=configuration;Installation=installation;}
        public bool Stale(string scope,long configuration,long installation)=>Scope!=scope||Configuration!=configuration||Installation!=installation;
        public void Check(string module,string owner,string item,Action check) {
            try{check();}catch(Exception e){Items.Add(new InstallationProblem{Module=module,Owner=owner,Item=item,Incomplete=true,Technical=e.Message,
                Cause=LocalText.Select("The check could not be completed.","Die Prüfung konnte nicht abgeschlossen werden."),
                Effect=LocalText.Select("This module's condition is not fully verified.","Der Zustand dieses Moduls ist nicht vollständig geprüft."),
                Action=LocalText.Select("Check the loaded world and bindings, then repeat the inspection. Export diagnostics if this persists.","Geladene Welt und Zuordnungen prüfen, dann erneut prüfen. Bei Wiederholung Diagnose exportieren.")});}
        }
    }
    // A diagnostic snapshot, never an operating command or a clear-track report.
    public sealed class InstallationProblem {
        public string Module,Owner,Item,Cause,Effect,Action,Technical;
        public bool Incomplete;
        public static bool RouteFault(RouteStage stage)=>stage==RouteStage.Fault;
        public static string PathCause(CircuitPathFault fault)=>fault==CircuitPathFault.InvalidInterval?
            LocalText.Select("A saved track segment is missing or its saved length is invalid.","Ein gespeichertes Gleisstück fehlt oder sein gespeicherter Meterbereich ist ungültig."):
            fault==CircuitPathFault.Interrupted?LocalText.Select("The saved detection path has a gap or contradictory direction.","Der gespeicherte Meldeweg hat eine Lücke oder eine widersprüchliche Richtung."):
            fault==CircuitPathFault.PointCrossing?LocalText.Select("The detection section crosses a point without ending at its detection boundaries.","Der Meldeabschnitt führt über eine Weiche, statt an deren Freimeldegrenzen zu enden."):
            fault==CircuitPathFault.Ambiguous?LocalText.Select("The detection path branches at a junction and is not unambiguous.","Der Meldeweg verzweigt sich an einem Gleisknoten und ist nicht eindeutig."):
            fault==CircuitPathFault.SkippedBoundary?LocalText.Select("An additional detection boundary lies inside the saved section and is being skipped.","Eine zusätzliche Freimeldegrenze liegt innerhalb des gespeicherten Abschnitts und wird übersprungen."):
            LocalText.Select("A detection boundary is missing, inactive or no longer matches its saved position.","Eine Freimeldegrenze fehlt, ist inaktiv oder passt nicht mehr zu ihrer gespeicherten Position.");
        public static string PathAction(CircuitPathFault fault)=>fault==CircuitPathFault.PointCrossing?
            LocalText.Select("Check the point's detection arms, then detect and review the adjoining sections again.","Meldeäste der Weiche prüfen, dann angrenzende Abschnitte erneut erkennen und prüfen."):
            fault==CircuitPathFault.SkippedBoundary?LocalText.Select("Detect the sections again so the additional boundary separates them, and review the preview before saving.","Abschnitte erneut erkennen, damit die zusätzliche Grenze sie trennt; Vorschau vor dem Speichern prüfen."):
            LocalText.Select("Open GFM inspection, compare both ends and review the path. Do not simply mark the track clear.","GFM-Prüfung öffnen, beide Enden vergleichen und den Meldeweg prüfen. Gleis nicht einfach als frei erklären.");
        public static string BoundaryAction(GfmBoundaryFault fault)=>fault==GfmBoundaryFault.Planned?
            LocalText.Select("Complete construction, then inspect the section again.","Bau fertigstellen und den Abschnitt erneut prüfen."):
            fault==GfmBoundaryFault.Disabled?LocalText.Select("Review whether this detector should be enabled; do not bypass detection.","Prüfen, ob dieser Melder eingeschaltet werden soll; Freimeldung nicht umgehen."):
            fault==GfmBoundaryFault.Moved?LocalText.Select("Compare both boundary positions in GFM inspection and review the repair preview.","Beide Grenzpositionen in der GFM-Prüfung vergleichen und die Reparaturvorschau prüfen."):
            fault==GfmBoundaryFault.PointUnavailable?LocalText.Select("Check the point's three detection arms and their boundary links.","Die drei Meldeäste der Weiche und ihre Grenzverknüpfungen prüfen."):
            LocalText.Select("Open GFM inspection and assign the intended existing boundary; review the path before saving.","GFM-Prüfung öffnen und die vorgesehene vorhandene Grenze zuordnen; vor dem Speichern den Meldeweg prüfen.");
    }
}
