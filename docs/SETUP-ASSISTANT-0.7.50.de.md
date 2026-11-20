# 0.7.50-P0 – Einrichtungsassistent, erste Testfassung

Für Railroader 0.6.3. Host und Clients benötigen dieselbe Modversion.
Die bisherige 0.7.49 wurde vom Anwender im Spiel einschließlich GWB erfolgreich
getestet. Die neuen Abläufe dieser Fassung benötigen noch einen Spieltest und einen
Mehrspielertest. Die lokale Kompilierung verwendet weiterhin die hier vorhandenen
älteren Spielbibliotheken mit dem bereits eingeführten Pfadadapter; ein Build gegen
die Originalbibliotheken von 0.6.3 ist damit nicht nachgewiesen.

## Einstieg

Stellwerkseditor öffnen → Menü → **Einrichtungsassistent**.
Der Assistent bietet Objektzuordnung, Signalfeldeinrichtung, Strecken-/GWB-Einrichtung,
GFM-Erkennung, GFM-Prüfung und optionale Annahmekapazitäten. Die vorhandenen manuellen
Werkzeuge bleiben verfügbar. Abgeschlossene Zuordnungen werden gespeichert; nicht
bestätigte Vorschauen werden beim Verlassen nicht automatisch übernommen.

## Vorhandene Signale und Weichen zuordnen

1. **Signale und Weichen schrittweise zuordnen** wählen.
2. Die Liste enthält noch nicht örtlich eingebundene, fertig gebaute Signale und
   Weichen. Vorsignale werden mit angeboten. Reine Anzeigekopien zählen nicht als
   bedienende Zuordnung.
3. Die Gleiskarte zeigt das aktuelle Objekt am roten Kreuz, Norden oben. Sie verwendet
   reale Gleispositionen und abgetastete Gleiskurven; der seitliche Mastversatz wird
   nicht als Kartenverschiebung übernommen.
4. **Feld im Gleisbild wählen** → örtliches Stellwerk wählen → passendes freies
   Gleis-/Signalsymbol bzw. unzugeordnetes Weichensymbol anklicken.
5. Ziel kontrollieren → **Zuordnung bestätigen und weiter**. Bereits eingebundene
   Objekte verschwinden aus der Liste. Übersprungene bleiben ausdrücklich offen.
6. Blickrichtung der Tafelsymbole und reale Weichenäste kontrollieren. Die schematische
   Zeichnung allein bestimmt keine reale Weichenlage. Danach GFM erkennen und prüfen.

Ändern sich Objekt, Tafel oder Auswahl während einer Vorschau, muss erneut gewählt
werden. Ein Signal wird nicht gleichzeitig zwei örtlichen Stellwerken zur Bedienung
zugewiesen. Zeilen und Spalten lassen sich weiterhin im Haupteditor bearbeiten.

## Vom gezeichneten Signalfeld ausgehen

Im Assistenten **Signalfeld einrichten: vorhandenes oder neues Signal** wählen oder
am ungebundenen Signalfeld das entsprechende Kontextmenü öffnen.

- **Ja:** vorhandenes Weltsignal anklicken.
- **Nein:** Haupt-, Rangier- oder Vorsignal wählen und mit der vorhandenen
  Platzierungsvorschau ein einzelnes Signal in der Welt setzen. Escape bricht ab.
- Danach Position/Zuordnung kontrollieren und bestätigen. Materialpflichtige Baupläne
  müssen erst fertiggestellt werden; sie erhalten keine Betriebsfreigabe durch den
  Assistenten.

## GWB und bestehende Verbindungen

**Strecken-/GWB-Verbindung einrichten oder ändern** öffnet vier Schritte:

1. Nachbarstellwerke, Gleis und Betriebsart wählen. Die Bestandsprüfung berücksichtigt
   auch vertauschte Nachbarn, Groß-/Kleinschreibung und Leerzeichen der Gleisbezeichnung.
   Eine vorhandene Verbindung wird zur Bearbeitung geöffnet. Mehrere alte Duplikate
   werden gemeldet. Gleiches physisches Gleis wird beim Speichern ebenfalls auf
   Überschneidung geprüft; parallele Gleise bleiben getrennte Verbindungen.
2. Ausfahrten und Ankunftsziele anhand der echten Stellwerksnamen wählen, bei Bedarf
   direkt in deren Gleisbild. Vorher festgelegte Signalrollen sind dafür nicht nötig.
3. Verlauf erkennen, Alternativen prüfen, GFM-Vorschläge bearbeiten und optional
   Blockbau vorbereiten. Offene GFM-/Topologiefehler verhindern den Abschluss dieses
   Prüfschrittes. Fehlende Freimeldung wird nicht künstlich als frei behandelt.
4. Erlaubnisbedienungen und Zugnummernansichten prüfen, Sicherung aktivieren und
   Konfiguration übernehmen. Vorhandene Erlaubnisfelder bleiben erhalten; ihre
   Verschiebung erfolgt im Editor. Neue Felder lassen sich auf freie Positionen
   setzen. Eine zweite Bedienkopie wird dabei nicht zusätzlich angelegt.

Die optionale Blockbau-Bestätigung in Schritt 3 ist ein eigener, ausdrücklich
beschrifteter Speichervorgang. Die Abschlussseite ist kein Ersatz für diese Bestätigung.

### Vorhandene Blockanzahl ändern, z. B. 2 → 10

Im Abschnitt Blockbau **Neu einrichten: vorhandene Blockaufteilung ersetzen** aktivieren.
Anzahl bzw. Abstand setzen, Vorschau erstellen, Grenzpositionen und Gleisbildpositionen
kontrollieren und den Blockbau bestätigen. Bis dahin bleiben alte Weltobjekte und
Konfiguration bestehen. Gespeicherte Verbindung und Erlaubnisverknüpfungen behalten
ihre Identität. Alte zugehörige Blocksignale, Automatik-Kontakte und Abschnittseinträge
werden beim Ersatz entfernt. Endsignale des Bahnhofs bleiben erhalten.

Vollständige erzeugte Zugnummernübersichten werden für alle neuen Blöcke erneuert;
ihre Gruppenkennung bleibt erhalten. Individuelle Teilansichten und manuell ergänzte
Felder werden nicht nach Vermutung neu verbunden. Externe Fahrstraßen, Vorsignale,
Hilfsziele, Weichengrenzen, Empfangsgleise oder manuelle Anzeigen, die alte Objekte
referenzieren, verhindern den Ersatz mit einer konkreten Verknüpfungsangabe. Diese
zuerst neu zuordnen oder entfernen. Aktive Fahrstraßen, Richtungsbindungen und belegte
betroffene Gleise verhindern den Umbau weiterhin.

## Optionale Annahmekapazität

1. **Annahmekapazität der Betriebsstelle einrichten** und örtliches Stellwerk wählen.
2. Je Empfangsgleis einen Namen vergeben, zulässige Ankunftssignale im Gleisbild
   anklicken und seine vollständigen, tatsächlichen Freimeldeabschnitte auswählen.
   Verschiedene Empfangsgleise dürfen keine reale Freimeldung teilen.
3. Empfangsgleise einzeln speichern. Anschließend maximales Annahmelimit setzen,
   höchstens so viele wie definierte Empfangsgleise. **0** deaktiviert diese Zusatzprüfung.
4. Wird die örtliche Einfahrt erst später gewählt, wird die vorgemerkte Annahme auf
   das tatsächlich befahrene Empfangsgleis umgebucht; ein für einen anderen Zug
   reserviertes Gleis bleibt gesperrt.
5. Zustände und Reservierungen werden je Gleis angezeigt. Belegt und unbekannt zählen
   nicht als frei. Eingestellte Einfahrten reservieren einen Platz; eine erneute
   automatische Fahrstraßenkennung erhält keinen kostenlosen zweiten Platz.
6. Eine Ausfahrstraße allein gibt den Platz nicht frei. Nach Einfahrt bleibt die
   Reservierung bestehen, bis die zugeordnete Empfangsfreimeldung belegt und danach
   wieder frei war, die zugehörigen Fahrstraßen aufgelöst sind und drei getrennte
   Freiprüfungen vorliegen. Eine vor Einfahrt stornierte Fahrstraße kann nach entsprechender
   Freiprüfung freigeben.

Diese erste Fassung benötigt passend und vollständig definierte Empfangswege.
Reservierungen gehören zum laufenden Fahrbetrieb; Empfangsgleisdefinitionen und
Limits werden gespeichert. Noch keine vollständige automatische Kreuzungs-,
Überhol-, Bot- oder Kommunikationsplanung. Zugnummern ersetzen keine Belegungsprüfung.

## Gezielter GFM-Fix

Zwei direkt benachbarte Weichensymbole können jetzt ihren verbindenden Abschnitt
zugeordnet bekommen, auch ohne zusätzliches Gleisfeld dazwischen. Die tatsächlichen
Weichenäste werden weiterhin geprüft; eine falsche Astzuordnung wird nicht übergangen.

## Spieltest

Eine gesicherte Testwelt verwenden. Besonders prüfen:

- Bestand öffnen ohne zweite Streckenkopie; erneuter Aufruf nach Weltneuladen.
- Signalfeld mit vorhandenem und neu platziertem Signal sowie Abbruch/Zeilenänderung
  während einer Auswahl; Haupt-, Rangier- und Vorsignalanzeige.
- Blockanzahl erhöhen/verkleinern; Vorschau abbrechen; externe Verknüpfung gezielt
  auslösen; vollständige Zugnummernübersicht kontrollieren.
- Erlaubniswechsel in beide Richtungen; Ausfahrt entgegen der Erlaubnis muss gesperrt
  bleiben; Blocksignale, Zugschlussauflösung und Nummernübertragung prüfen.
- Ein bzw. zwei Empfangsgleise und einen dritten Zug testen; belegte und unbekannte
  Abschnitte sowie stornierte Einfahrt und erst gestellte Ausfahrt vergleichen.
- Direkt benachbarte Weichen neu erkennen und Belegung im verbindenden Abschnitt prüfen.

Die gemeldeten optischen Verschiebungen der bisherigen Karte sind ohne konkreten
betroffenen Datensatz nicht abschließend diagnostiziert. Die neue Orientierungskarte
ist eine zusätzliche Darstellung, keine bestätigte Reparatur jeder alten Kartenansicht.
