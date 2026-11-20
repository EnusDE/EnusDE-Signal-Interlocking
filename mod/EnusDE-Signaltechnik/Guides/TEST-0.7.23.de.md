# 0.7.23-P0 – Fahrwege auf Anforderung und getrennte Bearbeitungsfenster

Lokaler Teststand für Railroader 0.6.2. Die veröffentlichte 0.7.22-P0 bleibt unverändert. Host und Clients benötigen dieselbe neue Version. Noch keine Spiel- oder Mehrspielerabnahme.

## Fahrwegermittlung

- Neuer Standard: Start und Ziel im Fahrdienstleiter wählen. Der Weg wird aus dem aktuellen Gleis-/Signalbestand berechnet und über die bestehende Engine angefordert. Kontakt-Automatik und Streckensuche verwenden ebenfalls die frische Berechnung.
- Belegung, Endlagen, Verschlüsse, Flankenschutz, Nachbarannahme und Block/GWB bleiben normale Voraussetzungen. Ein neuer Weg ist keine unmittelbare Fahrtfreigabe.
- Berechnete Wege werden nicht mehr als zusätzliche Einträge im dauerhaften Fahrstraßenkatalog gespeichert. Aktive Fahrten behalten vollständige eigene Schnappschüsse. Clients erhalten die benötigten Wegdaten als begrenzte Liveanzeige; diese ist keine wiederherstellbare Fahrtfreigabe.
- Material-/Anschlusskosten für bisher nicht angeschlossene Weichen bleiben erhalten. Das Weglassen des Katalogeintrags umgeht diese Kosten nicht.
- Vorhandene gespeicherte Definitionen werden weder gelöscht noch als Quelle im neuen Modus verwendet. Rückkehr unter **Spieloptionen → Signaltechnik → Automatisierung und KI → Fahrwegermittlung**: „Fahrwege bei Anforderung berechnen“ ausschalten und „Fahrwegmodus übernehmen“. Auch im Editor unter **Menü → Stellwerksfunktionen → Fahrwegmodus einstellen** erreichbar.
- Umschalten erst ohne aktive Fahrstraßen, Richtungsbindungen und wartende/laufende Kontaktanforderungen. Im bisherigen Modus bleiben die alten Planungsfunktionen erreichbar. Keine automatische Rückkehr dorthin bei einer betrieblichen Ablehnung.
- Mehrdeutige Wege bleiben auswählbar; ein eindeutig zur vorhandenen Weichenlage passender Weg wird bevorzugt. Keine allgemeine neue Weicheneinzelsperre implementiert.

## Editor und Zuordnung

- Rechtsklick markiert zuerst das tatsächlich angeklickte Feld, auch wenn vorher Zeichnen oder ein anderes Werkzeug aktiv war. Objektaktionen beziehen sich auf dieses Feld.
- Feldmenü und allgemeiner Kopfknopf sind getrennt. Das Objektmenü enthält passende Eigenschaften/GFM-/Nummernaktionen; Zeichnen ist auf freien Zellen und im allgemeinen Menü verfügbar.
- Die rechte Eigenschaftenfläche wird nicht mehr erzeugt. Die bisherigen Bearbeitungsfunktionen sind in einem eigenen verschiebbaren und skalierbaren Fenster erreichbar. Bestehende Bereichs-/Kopier-/Tafelfunktionen bleiben vorerst gemeinsam über die Auswahlwerkzeuge zugänglich; ihre vollständige weitere Aufteilung bleibt ein nächster Schritt.
- Weltweiche vormerken, Tafel wählen und ein ungebundenes Weichenfeld anklicken: Erstzuordnung erfolgt direkt. Bereits anderweitig gebundene Felder werden nicht durch diesen Linksklick überschrieben.
- Umgekehrt: Weichenfeld rechtsklicken → **In der Welt zuordnen …**. Reale Weiche rechtsklicken → **Weiche zuordnen**. Danach Rückkehr zur Tafel; Erstzuordnung direkt, Ersetzung bestehender Bindung über deren Bearbeitung. Escape bricht die Weltwahl ab.
- Eine unvollständige Startauswahl im Fdl läuft nach fünf Sekunden ab. Vollständige Anforderungen, Fahrstraßen und wartende Automatik werden davon nicht aufgelöst.

## Fahrzeugfenster und Kamera

- Keine eingebetteten Signaltechnik-Formulare mehr im normalen Wagenbereich. Ein eigener nativer Reiter **Signaltechnik** enthält den Einstieg **Signaltechnik-Fahrinformationen …**.
- Dieser öffnet ein separates, skalierbares Fenster für Betriebsart und Verbandsfestlegung. Die Anzeige ist an Fahrzeug/Welt gebunden; Änderungen des Verbands werden vor Übernahme geprüft. Veraltete Aktionen einer anderen Fensterauswahl werden abgewiesen.
- In der Objektplatzierung beendet die rechte Maustaste nicht mehr den Baumodus. Kamera mit rechts bedienen; **Escape** beendet die Platzierung.

## ESTW

- Freie Strecke/GWB steht nicht mehr als erster Knopf in der Stellwerksauswahl. Zugang im allgemeinen Editor-Menü und im ESTW-Modulfenster bleibt erhalten.
- Die Verbindungsliste des ESTW verwendet eingerichtete Streckendefinitionen, keine gespeicherten Zugfahrstraßen. Dadurch bleibt sie auch ohne manuellen Fahrstraßenkatalog vorhanden.
- Modulanschlüsse direkt im Diagramm, zusätzliche unabhängige ESTW, vollständige A/B/C-Migration und Neuaufbau des gesamten Gleisbilds sind noch nicht in diesem Paket enthalten. Die alte automatische Zeichnungsersetzung bleibt deaktiviert.

## Erste Spielprüfung

1. Paket auf einer Kopie der Anlage installieren. Im Fahrzeugfenster Reiter Signaltechnik öffnen, Fahrinformationen öffnen, Betriebsart ändern. Andere Fahrzeugfelder dürfen nicht überlagert werden. Zweite Lok öffnen; alte Aktionen dürfen nicht versehentlich deren Betriebsart ändern.
2. Rechtsklick direkt auf eine andere Weiche: Markierung und Aktionen müssen zum neuen Feld gehören. Eigene Bearbeitung öffnet separat; das Gleisbild behält die volle Breite.
3. Beide beschriebenen Zuordnungsrichtungen mit ungebundenem Weichensymbol testen; anschließend Abbruch und absichtliche Neuzuordnung prüfen.
4. Im Fdl Start/Ziel für einen freien Weg wählen, ohne Definition anzulegen. Danach besetzten Weg und verschlossene Weiche testen. Für Blockprüfungen vollständige GFM einrichten.
5. Neues korrekt eingerichtetes Zielgleis ergänzen und ohne neuen Editor-Fahrplan anfordern. Kontakt-Erst-/Ausweichziel und Gegenrichtung im Block testen.
6. Host/Client: aktive Fahrt, Verschlüsse und Zugnummern kontrollieren. Anschließend Fahrt sicher auflösen, neu laden und überprüfen, dass keine alte Freigabe übernommen wird.
7. Start wählen und länger als fünf Sekunden warten. Nur die Auswahl verfällt. In der Bauplatzierung Kamera rechts drehen; Vorschau muss bestehen bleiben.

Code und Offline-Prüfungen ersetzen diese Spieltests nicht. Die numerische Geschwindigkeitslogik, Mehrfach-Vorsignalbindung und übrigen Konzepte werden hier nicht als fertig behauptet.

## Offline-Prüfung dieses Teststands

938 Prüfungen bestanden, keine fehlgeschlagenen Prüfungen. Der vollständige Testlauf inklusive Sprachprüfung liegt unter `build/checks/0.7.23-tests.log`. Spieloberfläche, Gelände, KI-Fahrbetrieb und Mehrspieler müssen zusätzlich praktisch abgenommen werden.
