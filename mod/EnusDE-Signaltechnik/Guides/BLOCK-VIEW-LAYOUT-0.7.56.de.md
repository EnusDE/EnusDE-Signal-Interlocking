# 0.7.56-P0 – Fensteraufteilung der Blockansicht korrigiert

Für Railroader 0.6.3. Alle Teilnehmer benötigen dieselbe Modversion.

Der Ansichtseditor führt die Vorschau jetzt innerhalb des normalen scrollbaren
Formulars. Dafür ist eine eigene, 360 UI-Einheiten hohe Zeile reserviert. Die
Vorschau schwebt nicht mehr über Abschnittsauswahl, Spiegelung und Drehung.
Kompakte Auswahlfelder ersetzen die großen Such-/Listenfelder für Tafel,
Darstellungsstufe und Abschnittsbereich. In kleinen Fenstern das Formular scrollen;
Einstellungen und Übernehmen bleiben im selben Ablauf erreichbar.

Das Gleisbild wird nach der Layoutberechnung und beim Ändern der Vorschaugröße an
die tatsächlich verfügbare Fläche angepasst. Eigenes Verschieben und Zoomen bleibt
bis zur nächsten Größenänderung erhalten. Das Fenster kann bis auf 640 × 480
logische UI-Einheiten verkleinert werden. Signalzuordnungen, Nummernquellen, reale
Blöcke, Fahrstraßensicherung und Erlaubnis werden durch diese Korrektur nicht geändert.

Bedienung wie bisher: Verbund rechts anklicken → Ansicht des Blockverbunds bearbeiten
→ Einstellungen wählen → Ansicht als Vorschau vorbereiten → bei Bedarf platzieren
→ Diese Ansicht übernehmen. Rot in der Platzierungsvorschau bedeutet weiterhin
Kollision oder außerhalb der Tafel; es ist keine Meldung über die Streckenerlaubnis.

Lokale Kompilierung und vollständige Projekt-, Sprach- und Regressionstests werden
für dieses Paket ausgeführt. Spieltest mit UI-Skalierung und Mehrspielertest stehen
aus. Lokal wird weiterhin mit den verfügbaren älteren Spielbibliotheken und dem
bestehenden 0.6.3-Pfadadapter gebaut, nicht mit Original-0.6.3-DLLs.
