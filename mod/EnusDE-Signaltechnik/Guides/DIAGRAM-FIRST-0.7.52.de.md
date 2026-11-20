# 0.7.52-P0 – Im Gleisbild beginnen, per Weltklick zuordnen

Für Railroader 0.6.3. Alle Teilnehmer benötigen dieselbe Modversion.

## Der kurze Ablauf

1. Gewünschte örtliche Tafel im Stellwerkseditor öffnen.
2. **Menü → Einrichtungsassistent → Signale und Weichen aus diesem Gleisbild zuordnen**.
3. Das gewünschte noch nicht zugeordnete Signal-/Weichenfeld direkt im Hauptgleisbild
   anklicken. Ein zuvor ausgewähltes offenes Feld wird bevorzugt; sonst wird das erste
   offene Feld der Tafel markiert.
4. Das aktuelle Feld hat eine **hellgrüne Hinterlegung und grüne Umrandung**. Andere
   offene Signal-/Weichenfelder sind dunkler grün hinterlegt. Beschriftung und
   Tabellenkoordinate stehen im Assistenten. **Grünes Feld zeigen** bringt es in Sicht.
5. **Dieses Signal in der Welt auswählen …** bzw. **Diese Weiche in der Welt auswählen …**
   drücken und das passende reale Objekt anklicken.
6. **Dieser Weltklick speichert die Zuordnung direkt.** Kein zusätzlicher
   Bestätigungsdialog. Das gleiche örtliche Gleisbild öffnet sich wieder; anschließend
   wird das nächste offene Feld grün markiert. Jedes andere offene Feld kann jederzeit
   durch Anklicken gewählt werden.

Am Signal zeigt der Auswahlhinweis währenddessen **Anklicken: grünem Feld zuordnen**
mit der Zielkoordinate. Ein einseitiges Hauptsignal kann auch von der Rückseite seines
Masts aus ausgewählt werden. Bei zwei aktiven Signalrichtungen bleibt die angeklickte
Mastseite maßgeblich.

**Escape** während der vorhandenen Signal-/Weichenauswahl kehrt zum selben Zielfeld
zurück, ohne es zuzuordnen. **Dieses Feld vorerst überspringen** lässt es ausdrücklich
noch offen. **Offene Felder neu prüfen** nimmt übersprungene Felder wieder auf.
Bestätigte Zuordnungen sind gespeichert und bleiben nach erneutem Laden erhalten.

## Was entfällt

Der Hauptablauf benötigt keine Liste sämtlicher Weltsignale und Weichen, keine
Kennungssuche und keinen leeren Kartenbereich. Es werden die gezeichneten offenen
Felder der aktuellen Tafel bearbeitet. Benachbarte Stellwerke erhalten keine Zuordnung
als Nebeneffekt. Der Rücksprung in die Stellwerksübersicht nach dem Weltklick wurde
in diesem Ablauf durch die ausdrückliche Rückkehr zur örtlichen Tafel ersetzt.

## Fehlt das Weltsignal?

Erst dann **Signal fehlt? Neues platzieren …** öffnen. Haupt-, Rangier- oder Vorsignal
wählen und mit den vorhandenen Bauwerkzeugen platzieren. Die bestätigte Platzierung
eines fertig gebauten Signals ordnet es dem ausgewählten Feld zu. Materialpflichtige
Baupläne müssen vor der Betriebszuordnung fertiggestellt werden. Die normale Auswahl
vorhandener Objekte benötigt diese Bauoptionen nicht.

## Prüfungen und Grenzen

- Bereits zugeordnete Felder, Anzeigekopien und Zugnummernfelder werden nicht als offene
  Signal-/Weichenfelder behandelt. Eigene Feldbeschriftungen bleiben erhalten.
- Ein Signal wird nicht versehentlich einer Weiche zugeordnet oder umgekehrt.
- Eine zweite örtliche Bedienzuordnung desselben Objekts wird abgelehnt.
- Wird die Zeichnung während der Weltauswahl geändert, insbesondere durch Zeilen- oder
  Spaltenänderungen, verlangt die Zuordnung einen erneuten Klick auf das Zielfeld.
  Ein veralteter Klick überschreibt kein verschobenes Feld.
- GFM-Erkennung, Weichenast-Kalibrierung und Betriebsprüfung erfolgen anschließend
  über ihre bestehenden Werkzeuge. Die Feldzuordnung allein meldet keine Gleise frei.

Die lokalen Projekt-, Sprach- und Regressionstests prüfen Auswahl, Zuordnung,
Speicherung, Typ-/Eigentumsprüfung und veraltete Zielfelder. Die tatsächliche Anzeige,
Weltklick-Rückkehr und Mehrspielerbedienung benötigen noch einen Spieltest. Die lokale
Kompilierung verwendet die vorhandenen älteren Spielbibliotheken mit dem bestehenden
Pfadadapter; ein Build gegen Original-0.6.3-DLLs ist damit nicht nachgewiesen.
