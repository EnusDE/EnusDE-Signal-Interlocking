# 0.7.43-P0 – Einrichtung und Spielprüfung

Mit einer Spielstandkopie beginnen. Host und Clients verwenden 0.7.43.
Die üblichen Signal-/Blockprüfungen bleiben aktiv.

1. **Hilfsziel:** Ein vorhandenes Hilfsziel öffnen → Ziel durch Anklicken → Bezug
   automatisch erkennen. Bei eindeutiger Verbindung muss das Einfahrsignal mit
   Gegenrichtung vorgeschlagen werden. Alternativ im Gleisbild oder in der Welt
   auswählen. Feld speichern. Ohne Speichern darf sich die Anlage nicht ändern.
   Signal umbenennen/verschieben und neue Zielposition prüfen. Aktive Fahrstraße
   muss Umbau verhindern. Fehlender Bezug darf keine alte Zielposition freigeben.
2. **Manuelle Eingabe:** Erweiterte Einstellungen öffnen. Gleis/Meter oder Anfahrt
   selbst ändern. Die ausdrückliche manuelle Eingabe löst den automatischen Bezug.
   Bestehende frei gesetzte Ziele müssen unverändert weiter funktionieren.
3. **Nachbarverbindung:** Im Streckenmodul beide Nachbarn wählen. Eindeutige Rollen
   automatisch ergänzen oder jede Gruppe per Gleisbild anklicken. Einfahrsignal
   des Nachbarn als Anzeige hinzufügen. Es muss den Originalzustand zeigen und
   örtlich unbedienbar bleiben. Erst Verlauf/GFM prüfen, dann Verbindung speichern.
4. **Pfeile:** Nach drei vollständigen Freiprüfungen und vorhandener Erlaubnis muss
   genau deren Pfeil ausgefüllt leuchten. Gegenseitige Anfrage bleibt ein eigener
   Zustand. Bei deaktivierter/fehlerhafter Strecke darf keine Erlaubnis erscheinen.
5. **Blockassistent im Sandkasten:** Eine weichenfreie Strecke mit aktiven GFM an
   den genauen Enden erkennen. Drei Blöcke wählen → Vorschau: zwei innere Grenzen.
   Abstände bearbeiten → Vorschau aktualisieren. Bedienendes Ortsstellwerk wählen
   und Vorschau danach neu erstellen. Optional Kontakte samt Folgezielen mitplanen;
   die globale Automatik bleibt unverändert. Bestätigen erzeugt Signale/GFM und Blockfelder.
   Einrichten in beiden Richtungen, Zugfahrt und Auflösung testen. Blocksignale
   eines anderen zuständigen Stellwerks vom ESTW oder dessen Ortsstellwerk bedienen.
6. **Materialbetrieb:** Gleichen Ablauf mit Materialpflicht wiederholen. Neue
   Objekte müssen als Baupläne erscheinen. Erst nach Fertigstellung dürfen sie für
   den Betrieb verfügbar sein. Veraltete Vorschau nach Bau-/Konfigurationsänderung
   muss zurückgewiesen werden. Weichen im Verlauf müssen den Bauassistenten stoppen.
7. **Ziehen:** Auswahlmodus, Einzelfeld und markierte Gruppe ziehen. Über freien
   Feldern grün, über Konflikten rot. Escape und Loslassen außerhalb der Ansicht
   ändern nichts. Innerhalb der Auswahl versetzter Griffpunkt bleibt erhalten.
   Rückgängig prüfen; Zugnummernquelle/Zuordnungen müssen erhalten bleiben.
   Zeichnen, Setzen, Scrollen und das Schwenken über leerer Fläche separat testen.
8. **Parallelfahrten:** Zwei getrennte Fahrwege mit gemeinsamer, gleich gestellter
   Schutzweiche anfordern. Beide müssen nach Freiprüfung zulässig sein, wenn kein
   tatsächlicher Fahrweg den Schutzbereich verletzt. Gegenstellung oder Einfahrt
   in den Flankenbereich muss ablehnen. Eine Fahrstraße auflösen: andere Schutzlage
   muss verschlossen bleiben. Gegenprobe mit Rangierfahrt durchführen.
9. **Automatik und Wiederladen:** Vorhandene Erst-/Ausweichziele im Gleisbild
   auswählen, speichern und mit Kontakten prüfen. Speichern/laden: neue Bezugs-IDs,
   Anlagenarten und Blockfelder bleiben; alte Fahrtfreigaben starten weiterhin Halt.
   Clients prüfen Anzeige und dieselbe Versionsanforderung.

Bei Fehlern genaue Version, Handlung, Meldung und Diagnoseexport sichern. Dieser
Leitfaden ist die noch ausstehende Spielabnahme, keine Behauptung durchgeführter
Unity- oder Multiplayer-Tests. Neue autonome Anschluss-/Kreuzungssteuerung und
fahrzeugbezogene Bot-Auftragsketten sind in dieser Fassung nicht enthalten.
