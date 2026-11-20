# 0.7.46-P0 – Hilfsziel mit Streckenerlaubnis und Blockfolge

Interne Testfassung für Railroader 0.6.2. Host und Clients benötigen 0.7.46.
Ältere ZIPs bleiben unverändert. Enthält die GFM-Reparatur aus 0.7.45.

## Beabsichtigter Ablauf

Die normale Hilfstaste bleibt ein Fahrstraßenziel. Ihre Verbindung mit einem
Erlaubnismodul legt zusätzlich fest, welche Streckenerlaubnis für die Ausfahrt
erforderlich ist. Fehlt diese, wird die Anforderung abgewiesen – auch wenn die
bisherige örtliche Zielposition noch vor der überwachten Streckenachse liegt.

Bei vorhandener Erlaubnis wird der vollständige Weg zum tatsächlichen
Ankunfts-Hauptsignal des Nachbarn vorbereitet. Dazwischenliegende Blocksignale
werden als Teilfahrstraßen mit angefordert. Alle normalen GFM-, Weichen-, Flanken-,
Betriebszustands- und Annahmeprüfungen bleiben aktiv. Die Gegenrichtung bleibt
Halt; das Einfahrsignal des Nachbarn wird dadurch nicht auf Fahrt gestellt.

## Einrichten: beispielsweise WHB → ELA

1. In der Streckenkonfiguration STR-001 beide Nachbarstellwerke sowie die realen
   Aus-/Ankunftssignale und den erkannten Verlauf prüfen. Das Ankunftsziel am
   anderen Ende muss ein verfügbares Hauptsignal sein. Die Hilfstaste selbst
   darf nicht zugleich dieses Ankunftssignal ersetzen.
2. Die Hilfstaste **Nach Ela** im örtlichen Stellwerk WHB bearbeiten:
   **Funktion der Hilfstaste → Fahrstraßenziel**, **Zugfahrten** aktivieren, speichern.
3. Das Erlaubnismodul **STR-001** bearbeiten. Unter **Verknüpfte Hilfstaste** das
   normale Ziel **Nach Ela · Fahrstraßenziel** auswählen und das Feld speichern.
   Die Erlaubnis wird weiter über das Modul bedient; die Hilfstaste bleibt Ziel.
4. Alternativ kann direkt an der normalen Hilfstaste **Erforderliche Streckenerlaubnis
   für dieses Ziel** gewählt werden. Eine der beiden Bindungen genügt; widersprüchliche
   Zuordnungen werden abgewiesen. Der örtliche Eigentümer der Hilfstaste bestimmt
   die Abfahrtseite A oder B. Globale Ansichten allein ersetzen diese Zuständigkeit nicht.
5. Im Fahrdienstleiter Startsignal und **Nach Ela** wählen. Ohne Erlaubnis für WHB
   muss eine konkrete Ablehnung erscheinen. Mit Erlaubnis werden Ausgangsfahrstraße
   und zugehörige Blockfolge bis zum ELA-Ankunftssignal eingereiht und nach den
   normalen Prüfungen gestellt. Die Kontakt-Automatik muss für diese Handanforderung
   nicht eingeschaltet sein; einzelne Blockkontakte sind dafür nicht erforderlich.
6. Die örtliche Zielposition im gezeichneten Bahnhof begrenzt diese gebundene
   Anforderung nicht mehr. Ziel, reale Fahrtrichtung und Endsignal stammen aus
   der zugeordneten Streckenverbindung.

Eine reine Hilfstaste mit **Streckenerlaubnis bedienen** öffnet weiterhin nur
Anfrage/Abgabe. Wird sie während einer Startwahl angeklickt, erklärt eine Meldung,
dass noch ein normales Fahrstraßenziel erforderlich ist. **Automatische Bestätigung**
im Erlaubnisfenster bleibt auf Erlaubnisanfragen beschränkt.

## Automatische Zuganforderung

Für einen selbsttätigen Zugauslöser den Anforderungskontakt am Startsignal, dessen
Zielautomatik und die Lok-Betriebsart Zugfahrt wie bisher einrichten. Als Vorrangziel
kann die gebundene normale Hilfstaste verwendet werden. Globale Kontakt-Automatik
muss für diesen Kontaktbetrieb aktiv sein. Der gleiche Erlaubnis- und Blockweg wird
geprüft; ein fehlendes Recht wird nicht eigenmächtig übernommen.

Wegpunktaufträge dürfen die Folge am aktuellen Wegpunkt begrenzen. Auch eine
solche verkürzte Folge behält die ausdrückliche Erlaubnisbindung. Nach Kupplungs-
oder Auftragsänderungen gelten weiterhin die bisherigen Neuprüfungen.

## Verbindlichkeit und bestehende Fahrwege

Die Erlaubnis wird vor Vorbereitung und bei der tatsächlichen Aufnahme erneut
geprüft. Ein Wechsel während der Warteschlangenbearbeitung kann deshalb keine
Freigabe in falscher Richtung erzeugen. Alle Teilfahrstraßen behalten die Ziel-
und Zuordnungskennung. Eine geänderte/gelöste Bindung macht alte Folgen ungültig.
Aktive betroffene Fahrstraßen und Richtungsbindungen schützen vor Umzuordnung.

Alte örtliche Fahrwege, die am früheren Hilfsziel vor der Strecke enden, einmal
neu anfordern bzw. neu berechnen. Die vollständige Folge erhält die neue Bindung.
Eine örtliche Bedienung darf dabei ausschließlich die zu dieser Strecke gehörenden
Blocksignale in Abfahrtrichtung mit anfordern; sonstige Nachbarsignale behalten
ihre bisherige Zuständigkeit. Anzeigen erzeugen keine zusätzliche Bedienbefugnis.

## Kurzer GFM-Abschnitt zwischen zwei Weichen

Die Reparatur bietet jetzt auch einen kürzeren gültigen Verlauf zwischen denselben
Grenzen auf demselben Gleisverlauf an. Beispiel des Regressionstests:
57,29 m alter Gesamtverlauf, aktuelle Grenzen bei 25,00 und 32,29 m → 7,29 m
aktueller Zwischenabschnitt. Dies ist ein nachgestelltes Beispiel, kein aus dem
Nutzerspielstand bestätigter aktueller Meterwert.

Unter **GFM-Prüfung und Reparatur** den betroffenen Eintrag auswählen →
**Dieses Grenzpaar neu erkennen / vollständigen Ersatz suchen** → aktuellen
Vorschlag prüfen → **Ersatz und zugehörige Alt-Einträge vorschauen** → übernehmen.
Gespeicherte und aktuelle Positionen stehen nebeneinander. Eine Abweichung wird
nicht mehr pauschal als vom Spieler versetzte Grenze bezeichnet. Der gesamte
alte und neue Bereich muss zur Bestätigung frei und ohne zugehörige Bindungen sein.
Interne Abschnitte eines gebauten Blockverbunds bleiben gegen Verwaisen geschützt.

## Abnahme

- Erlaubnis beim falschen Nachbarn: örtliches Ziel vor der Achse und vollständige
  Blockanforderung müssen gleichermaßen ablehnen; kein grünes Startsignal.
- Richtige Erlaubnis: eine Anforderung stellt Ausgang und passende Blocksignale;
  Gegenrichtung und Nachbar-Einfahrsignal bleiben unverändert.
- Erlaubnis während Vorbereitung abgeben: endgültige Anforderung muss ablehnen.
- Bindung ändern/lösen, fremde Blocksignale, unvollständige GFM, Belegung,
  Wegpunktbegrenzung, Speichern/Laden und Host-/Client-Anzeigen prüfen.
- W198/W212: gespeicherte und aktuelle reale Positionen vergleichen und den
  gültigen kleineren Abschnitt mit Vorschau übernehmen.

Build und Regressionen verwenden die vorhandenen Spielbibliotheken. Unity-Test am
aktuellen Nutzerstand und Mehrspielerabnahme stehen aus. Verknüpfte Gleisfortsetzungen
über Darstellungsumbrüche sind weiterhin der separat vorgemerkte nächste Schritt.
