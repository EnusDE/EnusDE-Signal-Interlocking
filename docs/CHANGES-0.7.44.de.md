# 0.7.44-P0 – Blockverbund und gezielte Änderungssperren

Interne Testfassung für Railroader 0.6.2. Host und Clients benötigen 0.7.44.
Diese Fassung bearbeitet die Rückmeldungen zum Blockbau aus 0.7.43.

## Blockbau und Darstellung

- Jede innere Blockgrenze erhält einen gemeinsamen B-Freimelder und zwei
  getrennte, längs versetzte Signalmasten für die Gegenrichtungen. Beim
  Richtungsgleis wird nur die benötigte Richtung angelegt.
- Der Versatz ist je Seite von 0,5 bis 25 m einstellbar, zunächst 10 m.
  Kurze Abschnitte begrenzen ihn auf ein Viertel der benachbarten Blocklängen.
  Seitenabstand, Seite, Höhe und Maßstab stammen aus den aktuellen
  Platzierungseinstellungen; der Spielabstand wird bei entsprechender Auswahl übernommen.
- Das Gleisbild zeigt eine Gleislinie mit getrennten Signalpunkten,
  gemeinsamen Meldergrenzen, Zugnummernfeldern und einem Erlaubnismodul.
- Das bedienende Ortsstellwerk ist wählbar, unabhängig von den beiden
  Streckennachbarn. Weitere Tafeln können als reine Anzeige ausgewählt werden.
  Dort sind Signale und Erlaubnismodul schreibgeschützt. Zugnummern sind Spiegel
  derselben Quelle und werden dadurch nicht als weitere Zugnummern geführt.
- Für jede neue Anzeigetafel muss die Position in einer eigenen Gleisbildvorschau
  bestätigt werden: grün = frei, rot = Kollision oder außerhalb der Tafel.
  Bis zur abschließenden Bestätigung bleibt alles ein Vorschlag.
- Im Editor wählt das Anfassen eines Verbundfeldes automatisch den gesamten
  Verbund zum Verschieben. Vom Fahrdienstleiter gibt es einen direkten Einstieg
  zum Verschieben im Editor. Signal-, Melder- und Zugnummern-IDs bleiben erhalten.
- Ein bestehender Verbund kann nur hinsichtlich Bedienzuordnung und Anzeigen
  geändert werden, ohne seine Weltobjekte neu zu platzieren. Bei vollständigem
  Neuaufbau bleiben vorhandene Hilfstastenverknüpfungen und Signalbeschriftungen
  erhalten. Manuell ergänzte fremde Felder werden nicht stillschweigend gelöscht;
  der Neuaufbau fordert dann zum vorherigen Herausverschieben auf.

## Streckensicherung und Signalbedienung

- Blockbau, Gleisbild und Streckendefinition werden gemeinsam gespeichert.
  Die aktivierbare Streckensicherung wird damit auch bei einer neuen,
  zuvor ungespeicherten Verbindung übernommen. Eine installierte, aber nicht
  aktivierte Blockanlage verweigert Fahrten auf ihrer Streckenachse.
- Die gemeinsame Streckenerlaubnis sperrt Gegenfahrten auch dann, wenn die
  beiden angeforderten Fahrwege unterschiedliche, freie Blockabschnitte benutzen.
  Gleichgerichtete Folgefahrten benötigen weiterhin ihre eigenen freien Fahrwege.
- Der Haltfall eines Blocksignals wartet auf den nachgewiesenen Durchlauf des
  zugeordneten Zuges hinter seinem gemeinsamen Melder. Er berücksichtigt die
  tatsächliche Fahrtrichtung und mindestens den normalen Signal-Durchfahrabstand.
  Dieselbe Durchfahrprüfung löst die vorhandene Zugnummernweitergabe aus.
  Zugschlussprüfung und Verschlussauflösung bleiben eigene Schritte.
- Der Assistent kann Anforderungskontakte mit Folgezielen einrichten. Die globale
  Kontakt-Automatik wird nur über ihr ausdrückliches Kontrollfeld eingeschaltet.
  Vorhandene Regeln bleiben erhalten; eine Lok braucht weiterhin die Betriebsart
  Zugfahrt. Der Blockbau allein ist keine neue fahrzeugbezogene Auftragsautomatik.
- Fehlende, unfertige oder falsch zugeordnete Signale/Melder sowie unvollständige
  GFM-Abdeckung verhindern die Freigabe. Ein Eigentümerwechsel überträgt die
  Blocksignale; die ursprünglichen Ein-/Ausfahrsignale bleiben bei ihren Bahnhöfen.

## Lokale Änderungen und Restbindungen

Hilfstastenänderungen prüfen die tatsächlich betroffenen Ziele und Abhängigkeiten.
Änderungen einer Strecke prüfen deren alte und vorgeschlagene Achse. Unbeteiligte
Fahrstraßen oder Richtungsbindungen verhindern solche lokalen Änderungen nicht
mehr pauschal. Eine gehaltene Richtung ohne Restverschluss sperrt für sich allein
keine Umbenennung oder Änderung der zusätzlichen Anzeigetafeln.

Verbleibende Sperren nennen Fahrstraße/Strecke, Zustand, Freiprüfstand und Grund.
Bei Richtungsbindungen wird nach Möglichkeit der erste belegte oder nicht lesbare
Abschnitt genannt. Die Liste im Editor beschreibt den letzten Speicherversuch;
nach Auflösung oder abgeschlossener Freiprüfung erneut speichern. Echte Belegung,
Zugschlussbindungen und Schutzverschlüsse werden nicht pauschal zurückgesetzt.
Ein geänderter GFM-Katalog kann bei einer unbeteiligten Richtungsbindung aktualisiert
werden; die Geometrie einer gehaltenen Blockachse darf dabei nicht wechseln.

## Übernahme und Grenzen

Vorhandene 0.7.43-Anlagen werden nicht beim Laden automatisch umgebaut. Die
Umstellung erfolgt ausdrücklich im Blockassistenten mit Vorschau und Bestätigung.
Bei einem alten Doppelkopf-Mast bleiben beide logischen Signal-IDs und Kennungen
sowie seine Freimeldenummer erhalten; die Köpfe werden auf getrennte Masten verteilt.
Nur der eindeutig erkannte alte automatische Diagrammstreifen wird ersetzt.

Die vorhandene Blockaufteilung eines bereits mit 0.7.44 angelegten Verbunds bleibt
in dieser Fassung bestehen. Signalversatz, Bedienort und Diagrammposition sind
änderbar; ein anderer Zuschnitt vorhandener Abschnitte wird abgewiesen, damit
keine alten Abschnitte oder Melder verwaisen. Der Bauassistent gilt weiterhin nur
für weichenfreie Strecken und berechnet keine vorbildgerechten Bremswege.

Die im Spiel bestätigten parallelen Fahrstraßen bleiben erhalten. Neue autonome
Anschluss-/Kreuzungssteuerung, Bot-Auftragsketten und neue hochwertige Signalmodelle
sind spätere Arbeitsschritte. Die öffentlichen Anleitungen für 0.7.40 und alle
bereits vorbereiteten älteren ZIPs bleiben unverändert.

Build, Regressionen und EN/DE-Prüfungen werden gegen die vorhandenen
Spielbibliotheken ausgeführt. Die genaue Ursache der gemeldeten Live-Situation
ist ohne aktuellen Diagnoseexport nicht bewiesen. Der Unity-/Mehrspieler-Test
dieses Updates steht noch aus; siehe [Einrichtung und Spielprüfung](TEST-0.7.44.de.md).
