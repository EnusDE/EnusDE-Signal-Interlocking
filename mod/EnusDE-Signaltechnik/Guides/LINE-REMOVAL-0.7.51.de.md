# 0.7.51-P0 – GWB-Streckenverbindungen löschen

Korrektur für Railroader 0.6.3. Host und Clients benötigen dieselbe Modversion.

## Ursache

Bisher entfernte die Löschaktion nur den Eintrag im Streckenkatalog. Erlaubnisfelder
und Hilfstasten verwiesen danach weiter auf diesen Eintrag. Die Prüfung lehnte das
Speichern wegen dieser verwaisten Verknüpfungen ab. Im geführten Assistenten lag die
Aktion zudem erst im letzten Schritt.

## Bedienung

1. Im Stellwerkseditor **Block-/Streckenverbindungen** öffnen.
2. Die gespeicherte Verbindung auswählen.
3. **Diese Streckenverbindung löschen …** wählen. Die Aktion ist unabhängig vom
   aktuellen Assistentenschritt erreichbar, auch für unvollständige Entwürfe.
4. Die Vorschau zeigt die Verbindung sowie die Anzahl betroffener Erlaubnisfelder,
   Hilfstasten, Gruppen und Automatikfunktionen.
5. **Löschen dieser Verbindung bestätigen** oder **Löschen abbrechen** wählen.

Die Vorschau verändert noch nichts. Änderungen an der Anlage seit der Vorschau
verlangen eine neue Vorschau. Vor dem Bestätigen werden betroffene Fahrstraßen,
Richtungsbindungen und physische Belegung nochmals geprüft. Belegte oder nicht
lesbare betroffene Gleise verhindern das Löschen mit einer Orts-/Abschnittsangabe.
Unabhängige Fahrstraßen an anderer Stelle dürfen weiter bestehen.

## Was beim Bestätigen passiert

- Der gemeinsame Streckeneintrag mit seiner GWB-Erlaubnis wird entfernt.
- Alle zugehörigen Erlaubnisfelder werden in sämtlichen Stellwerken gelöst.
- Gebundene Hilfsziele werden zu unzugeordneten Erlaubnistasten. Name, Lage und
  Einstellungen bleiben für eine neue Einrichtung erhalten. Ein bislang gesichertes
  Zugziel wird dadurch nicht unbemerkt zu einem ungesicherten Ausfahrziel.
- Zugehörige verwaltete Block-/Zugnummerngruppen werden zu normalen Zeichenbereichen.
- Die zugehörige Blocksignal-Automatik und ihre Kontakte werden abgeschaltet.
- Andere Streckenverbindungen bleiben erhalten.

Weltsignale, GFM-Abschnitte, gezeichnete Felder und Zugnummernquellen bleiben erhalten;
es wird kein Weltobjekt abgebaut. Bestehende Zugnummernspiegel bleiben gültig.
Betroffene gespeicherte Fahrstraßen vor erneuter Verwendung neu einrichten.

Die lokalen Projekt-, Sprach- und Regressionstests prüfen diese Bereinigung und
ihre Sperren. Der Bedienablauf benötigt noch einen Spiel-/Mehrspielertest. Die lokale
Kompilierung verwendet weiterhin die vorhandenen älteren Spielbibliotheken mit dem
bestehenden Pfadadapter; ein Build gegen Original-0.6.3-DLLs ist damit nicht nachgewiesen.
