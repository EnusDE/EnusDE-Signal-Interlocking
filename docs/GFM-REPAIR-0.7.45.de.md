# 0.7.45-P0 – GFM-Fehlerübersicht und gezielte Reparatur

Interne Testfassung für Railroader 0.6.2; Host und Clients brauchen dieselbe Version.
Die Änderungen aus 0.7.44 bleiben enthalten. Ältere ZIPs bleiben unverändert.

## Was geändert wurde

- GFM-Fehler unterscheiden gelöschte Objekte, unfertige Baupläne, deaktivierte
  Melder, versetzte Grenzen und nicht verfügbare Weichenäste. Kennungen und
  gespeicherte Gleis-/Meterpositionen ersetzen Meldungen nur mit zwei internen IDs.
- Die GFM-Prüfung zeigt alle gespeicherten ungültigen Abschnitte gemeinsam,
  einschließlich Einträgen ohne Gleisbildzuordnung und Fehlern anderer Stellwerke.
  Die zugehörigen Tafeln werden genannt. Fehler stehen in der Abschnittsliste oben.
- Vom Strecken-/GWB-Fenster des Fahrdienstleiters führt **Alle GFM-Einrichtungsfehler
  im Editor prüfen** direkt zur Fehlerübersicht.
- **Bereinigung aller Abschnitte mit gelöschten Grenzen vorschauen** sammelt die
  dafür geeigneten Alt-Einträge. Alternativ einen einzelnen Abschnitt auswählen.
- **Ersatz bis zu den nächsten aktiven Grenzen suchen** bietet einen vollständigen,
  physisch geprüften Ersatzabschnitt an. Nach Entfernen eines Zwischenmelders kann
  dieser bis zur nächsten tatsächlichen GFM-Grenze reichen. Ein Signal ohne
  aktiven Melder bildet keine neue Abschnittsgrenze. Weichen oder weitere aktive
  Grenzen werden bei dieser Suche nicht übersprungen.
- Ein Ersatz sammelt auch die enthaltenen ungültigen Nachbarabschnitte mit gelöschten
  Grenzen. Bei Bestätigung werden ihre gespeicherten Verweise auf allen betroffenen
  Tafeln gemeinsam ersetzt. Reines Entfernen löst nur diese Verweise und fehlende
  Grenzmarken; Zeichnung, Weltobjekte, Signalverknüpfungen und Zugnummern-IDs bleiben.
- Die Streckenprüfung nennt mehrere Blockfehler und genaue GFM-Abdeckungslücken
  mit Gleis und Metern. Nur tatsächlich nutzbare Abschnitte zählen als Abdeckung.

## Vorgehen bei einer gelöschten Grenze

1. Im Editor **Abschnittmanager / GFM-Prüfung** öffnen oder den neuen Einstieg
   im Streckenfenster verwenden. **GFM-Prüfung aktualisieren** liest den Stand neu.
2. In **Alle gespeicherten GFM-Fehler** die betroffenen Abschnitte und Tafeln
   gemeinsam ansehen. **Diesen Fehler prüfen** öffnet die Auswahl. Bei beiden
   Grenzen stehen Herkunft, Zustand und gespeicherte Positionen.
3. Für einen vollständigen Ersatz **Ersatz bis zu den nächsten aktiven Grenzen
   suchen** wählen. Den angebotenen Abschnitt und seine Länge kontrollieren,
   dann **Ersatz und zugehörige Alt-Einträge vorschauen** wählen.
4. Alternativ **Entfernen dieser veralteten Zuordnung vorschauen** oder die
   gemeinsame Bereinigung der Abschnitte mit gelöschten Grenzen verwenden.
5. Die Vorschau nennt sämtliche entfernten Abschnitte, geänderten Felder und
   betroffenen Stellwerke. Erst **Diese GFM-Reparatur übernehmen** speichert.
   Abbrechen ändert nichts; vor dem Speichern wird eine Sicherung erstellt.
6. Anschließend Fehlerübersicht und GFM-Lücken erneut prüfen. Reines Entfernen
   erzeugt keine Freimeldung. Die bestehende Einstellung für automatische
   Katalogpflege bleibt erhalten; eine automatische Erkennung kann gültige
   aktuelle Abschnitte nach dieser Host-Einstellung weiterhin ergänzen.

## Bewusste Grenzen

Unfertige Baupläne werden von der Bereinigung ausgenommen: deren Bau ist zuerst
fertigzustellen. Deaktivierte Melder können an ihren Eigenschaften wieder aktiviert
werden. Abschnittsinterne Einträge eines automatisch gebauten Blockverbunds werden
hier nicht gelöscht; dessen gemeinsame Melder müssen zunächst wiederhergestellt
oder der Verbund gesondert bearbeitet werden. Die Meldung nennt diese Abhängigkeit.

Vor Übernahme werden Vorschau, Spielwelt und Konfigurationsstand erneut geprüft.
Betroffene Gleise müssen frei und lesbar sein; zugehörige Fahrstraßen und
Richtungsbindungen müssen aufgelöst sein. Unbeteiligte laufende Fahrstraßen sperren
nicht pauschal. Wartende Fahrstraßen-/Automatikanforderungen müssen vorher
zurückgenommen werden. Die Reparatur hebt keine echten Restverschlüsse auf.

## Spielabnahme

- Gelöschten Zwischenmelder mit zwei gespeicherten Nachbarabschnitten nachstellen:
  beide Fehler gemeinsam sehen, Ersatz bis zur nächsten aktiven Grenze vorschauen,
  beide Alt-Einträge auf mehreren Tafeln ersetzen; keine Doppelnummern erzeugen.
- Reines Entfernen testen: gezeichnete Felder bleiben, alte Verweise verschwinden,
  unüberwachte Lücken werden weiterhin gemeldet.
- Bauplan, deaktivierten Melder und versetzte Grenze getrennt testen. Bauplan muss
  als unfertig erkennbar bleiben und darf nicht als gelöschtes Objekt bereinigt werden.
- Belegte/unlesbare Gleise, betroffene Fahrstraße, Restbindung und veraltete
  Vorschau müssen ablehnen. Unbeteiligte Fahrstraße darf bestehen bleiben.
- Weitere aktive GFM-Grenzen und Weichen dürfen nicht überbrückt werden.
- Speichern/laden, Host/Client-Anzeigen und parallele Fahrstraßen aus 0.7.44 prüfen.

Build/Regressionen prüfen die Logik gegen die vorhandenen Spielbibliotheken.
Unity-Spieltest am Nutzerstand und Mehrspielerabnahme stehen noch aus.
