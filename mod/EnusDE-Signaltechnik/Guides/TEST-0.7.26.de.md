# 0.7.26-P0 – kurze Einrichtung und Spieltests

Für Railroader 0.6.2. Modmanager-Paket ersetzen und das Spiel neu starten. Die bestehende Testwelt bleibt verwendbar; Host und Clients brauchen dieselbe Version. Offline geprüft, Spielabnahme ausstehend.

## Einstellungen

**Company → Einstellungen/Optionen → Mods → Signaltechnik**. Oben gegebenenfalls von „Installierte Mods“ auf „Signaltechnik“ umschalten.

## Weichengrenzen schnell aktivieren

In der Welt auf das Gleis nahe der gewünschten Weiche rechtsklicken und **Weichen-GFM-Grenzen aktivieren** wählen. Keine Weichenliste und keine vorherige Feldzuordnung nötig. Es wird die nächste Weiche innerhalb von 30 m verwendet; bei zwei gleich nahen Weichen muss näher an die gewünschte geklickt werden.

Alternativ im Editor auf das bereits zugeordnete Weichensymbol rechtsklicken und denselben Eintrag wählen. Die drei Grenzen werden automatisch erkannt und die Kästen angezeigt, sobald die Netzerfassung abgeschlossen ist. Während Fahrstraßen oder Richtungsbindungen bestehen, werden Änderungen an der GFM abgewiesen. Das Aktivieren allein meldet keine unbekannten Abschnitte frei.

Für einzelne Kästen anschließend den Kasten öffnen: B/C, C-Richtung, Gleisseite und Modellversatz einstellen. Bestehende Zuordnungen und individuelle Kastenpositionen bleiben erhalten.

## Bestehendes Gleisbild: Bahnhofssignale suchen

Das örtliche Stellwerk im Editor öffnen, dann **Menü → Automatische Erkennung**. Oben steht **Signale für vorhandenes Gleisbild suchen**. Vorhandenes Gleisbild kontrollieren, nötigenfalls ein bekanntes Bahnhofssignal als Start wählen, Suchweite einstellen und **Bahnhofssignale und GFM-Grenzen suchen** klicken.

Die Suche folgt realen Gleisverbindungen in der eingestellten Umgebung. Ohne gewähltes Startsignal dienen bereits zugeordnete Tafelsignale als Ausgangspunkte. Das ist keine Suche über alle Signale der Welt. Die Zeichnung wird nicht ersetzt.

Gefundene Kennungen und die Zahl aktiver GFM-Grenzen werden angezeigt. Ungebundene Signalfelder mit eindeutig passender Kennung oder eindeutigem Anzeigenamen werden zugeordnet. Anonyme „Signal“-Platzhalter können ohne weiteren Weltbezug nicht zuverlässig zugeordnet werden; diese müssen einzeln verbunden werden. Verbindende Gleisabschnitte werden durch diese Suche noch nicht automatisch eingerichtet.

Bereits gebundene Signalfelder werden im Hintergrund aktualisiert: eine allgemeine Beschriftung „Signal“ verdeckt die Kennung nicht mehr; Änderungen von Bauart und Sh-1-Fähigkeit aktualisieren die eigene Haupt-/Rangiertastenaufteilung. Eigene zweite Signale und Hilfsziele werden beibehalten. Aktivierte angebaute GFM-Grenzen werden am Feld nachgeführt. Das ist keine zusätzliche Fahrterlaubnis.

## ESTW-Module

**Stellwerksauswahl → beim ESTW „Module“**. Links ein Ortsstellwerk auswählen und bei Bedarf **Modul einfügen**. Im großen Gleisbild ein Modul ziehen; eine transparente Fläche zeigt den Zielbereich. Beim Loslassen wird die Lage übernommen. Überlappungen oder Positionen außerhalb der Tafel werden abgewiesen, die vorige Lage bleibt bestehen.

Rechtsklick auf das Modul öffnet **Örtliches Gleisbild bearbeiten** und **Modul lösen**. Eine leere Fläche ziehen verschiebt die Ansicht. **Tafel einpassen**, +/− und 100 % stehen oben. Die Anordnungsansicht bedient keine Züge.

## Bitte im Spiel prüfen

1. Im Modulfenster sind Auswahl, Schaltflächen und Vorschau getrennt sichtbar. Modul ziehen, örtliche Zeichnung öffnen und Fenster kleiner/größer machen.
2. Weichen-GFM direkt aus der Welt einschalten. Nach abgeschlossener Erkennung drei Kästen prüfen; anschließend einen Kasten bearbeiten.
3. Im vorhandenen Bahnhof Signale suchen. Gefundene Kennungen und tatsächliche Feldzuordnung unterscheiden; nicht zugeordnete Symbole bleiben offen.
4. Ein zugeordnetes Rangiersignal aufwerten und vom niedrigen Hauptsignal auf hohen Mast umstellen. Kennung und Haupt-/Rangiertasten prüfen; GUID und Position müssen erhalten bleiben.
5. C-Kasten nachts ansehen: der Pfeil ist matte Richtungsmarkierung, keine Leuchte oder Belegtanzeige. Die Vorschau im Baumodus darf weiterhin hervorgehoben sein.

Die offenen KI-Haltetests aus [0.7.25](TEST-0.7.25.de.md) gelten weiterhin. Diese Version enthält keine neue KI-Fahrlogik.
