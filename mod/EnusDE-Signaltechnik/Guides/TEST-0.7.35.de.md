# 0.7.35-P0 – Signale fortlaufend platzieren

Teststand für Railroader 0.6.2. Bestehende Testwelt verwenden. Host und Clients benötigen dieselbe Modversion. Enthält die bisherigen Änderungen einschließlich [GFM-Wegprüfung aus 0.7.34](TEST-0.7.34.de.md).

## Bedienung

1. Über **STW → Baumodus** einen **Signalmast**, ein **Vorsignal** oder ein **Rangiersignal** wählen. Bei aktivierter Hl-Testoption gilt derselbe Ablauf auch für Hl-Signale.
2. Vorschau auf dem gewünschten Gleis ausrichten und mit **Linksklick** setzen. Der gewählte Signaltyp bleibt aktiv; der nächste Klick setzt ein weiteres Signal. Kein Signal-Konfigurationsfenster öffnet sich nach dem Setzen.
3. Währenddessen frei bewegen. Die rechte Maustaste bleibt für die Kamera verfügbar. Platzierte Signale lassen sich in diesem Modus nicht versehentlich zum Bearbeiten öffnen. Ein gehaltener linker Mausknopf platziert nicht fortlaufend weitere Signale; jeder Bau benötigt einen neuen Klick.
4. **Escape** beendet die Serie und verwirft nur die noch ungespeicherte Vorschau. Die bereits platzierten Signale bleiben bestehen. Anschließend lassen sich einzelne Signale wieder normal anklicken und bearbeiten.

Ein Wechsel zu Editor/Fahrdienstleiter oder erneutes Öffnen des Baumenüs beendet die Serie ebenfalls. Wird zuerst eine Gleisposition über das Weltmenü vorgemerkt, kann die erste Vorschau wie bisher bestätigt bzw. fein ausgerichtet werden. Nach Bestätigung geht die Platzierung mit demselben Signaltyp in der freien Welt weiter. Das Schließen eines noch unbestätigten Positionierungsfensters beendet dessen Serie.

Signaltyp, Hl-Bauart und die Bauvorgabe für Signal-GFM bleiben innerhalb der Serie erhalten. Bei aktivierter Blickrichtungsanpassung wird jedes neue Signal an der aktuellen Blickrichtung ausgerichtet. Jedes Signal erhält eine eigene automatische Kennung und ID. Das Verschieben vorhandener Objekte sowie das Setzen von Schildern und GFM-Kästen bleiben einzelne Aktionen.

## Spieltest

- Drei Hauptsignale nacheinander setzen, dazwischen laufen und die Kamera drehen. Alle müssen eigene Kennungen erhalten und an ihrer Position bleiben; keine Eigenschaftenfenster dürfen aufgehen.
- Mausknopf halten, ins Leere klicken und über einer geöffneten Spieloberfläche klicken: keine zusätzlichen oder an der alten Position wiederholten Signale.
- Mit Escape beenden, ohne die letzte Vorschau zu setzen; anschließend eines der gebauten Signale bearbeiten.
- Rangier-, Vor- und optional Hl-Signale sowie die GFM-Bauvorgabe ein/aus testen.
- Einstieg über vorgemerktes Gleis, erste Bestätigung, Abbruch, Moduswechsel und Einzelverschieben eines vorhandenen Signals prüfen.

Automatische Prüfungen ersetzen keine Abnahme der nativen Kamera-, Eingabe- und Mehrspielerabläufe. Bei Fehlern Version, Aktion und Player.log angeben.
