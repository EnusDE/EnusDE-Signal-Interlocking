# 0.7.57-P0 – Stabile Platzierung in der Blockvorschau

Für Railroader 0.6.3. Alle Teilnehmer benötigen dieselbe Modversion.

Der Ansichtseditor baut das Fenster nach einem gültigen Platzierungsklick nicht
mehr neu auf und passt dabei auch nicht erneut den Bildausschnitt an. Der aktuelle
Verbund bleibt sichtbar; Mausbewegungen zeigen zusätzlich das grüne/rote Ziel.
Beim Verlassen des Gleisbilds wird diese vorübergehende Vorschau ausgeblendet,
statt an eine ungültige negative Position versetzt zu werden.

Eine freie linke obere Zielposition in der Vorschau anklicken. Der
**Platzierungsstatus** bestätigt **Position übernommen** mit Spalte/Zeile oder
nennt die Kollision. **Nach links, rechts, oben, unten** unter dem Gleisbild erlauben
präzises Verschieben um einzelne Felder. Danach **Diese Ansicht übernehmen** zum
Speichern wählen. Schließen ohne Übernehmen verwirft den Vorschlag weiterhin.
Erneutes Vorbereiten aus den Einstellungen startet wieder vom erfassten Ausgangsstand.

Die Vorschau bietet anklickbaren Platz rechts und unterhalb der bisherigen Tafel.
Eine erfolgreiche Platzierung erweitert die vorgeschlagene Arbeitsfläche bei
Bedarf. Die Einzelschritte unterstützen auch Erweiterung links/oben. Umliegende
Felder, Ansichtsprofile, Nummernkennungen und die Koordinaten vorgemerkter
Erlaubnisfelder bleiben erhalten. Überlappende Felder oder verwaltete Verbünde sowie
die Höchstgrenze von 2048 Spalten/512 Zeilen bleiben gesperrt. Bloßes Darüberfahren
mit der Maus verändert nichts. Die Platzierung beim Blockbau nutzt dieselben
Erweiterungs-/Kollisionsregeln; gespeichert wird dort weiterhin erst beim Bauabschluss.

Lokale Regressionstests prüfen seitliches Platzieren eines tafelbreiten Verbunds,
wiederholtes Platzieren, unveränderte Daten beim Darüberfahren/Anzeigen,
Kollisionsablehnung ohne Teiländerungen, Erweiterung links/oben, Größenlimits und
erhaltene fremde Erlaubnisbindungen. Mausbedienung, UI-Skalierung und Mehrspieler
benötigen weiterhin einen Spieltest. Kompiliert wird mit den verfügbaren älteren
Spielbibliotheken und dem bestehenden 0.6.3-Pfadadapter, nicht mit Original-0.6.3-DLLs.
