# 0.7.44-P0 – Einrichtung und Spielprüfung

Interne Testfassung für Railroader 0.6.2. Eine Spielstandkopie verwenden; Host und
Clients brauchen dieselbe Version. Diese Schritte sind die noch ausstehende
Spielabnahme, keine Behauptung bereits durchgeführter Unity-/Mehrspieler-Tests.

## 1. Verbindung wählen

1. Im Editor das Erlaubnismodul öffnen → **Freie Strecke, Block und GWB**.
2. Beide Nachbarstellwerke und ihre Ausfahrten/Ankunftsziele auswählen.
   Suchlisten sowie Auswahl im Gleisbild oder in der Welt bleiben verfügbar.
3. **Streckenverlauf automatisch erkennen** und die passende Hin-/Rückrichtung
   prüfen. Der Blockbau-Assistent benötigt eine weichenfreie Strecke und genau
   einen aktiven GFM-Endpunkt an jedem Ende. Er ersetzt keine Weichen-GFM.
4. Für eine neue Anlage Blockanzahl (1–16) oder ungefähren Abstand wählen.
   Drei Kilometer / drei Blöcke ergeben zwei innere Blockgrenzen.
   Vorhandene Signal-/Meldergrenzen müssen im Vorschlag erhalten bleiben.

## 2. Verbund planen und platzieren

1. **Bedienendes Stellwerk der Blocksignale** wählen, beispielsweise ELA.
   Das ist die Zuständigkeit der Blocksignale, unabhängig von Nachbar A/B.
2. Unter **Zusätzliche schreibgeschützte Anzeigetafeln** nur die gewünschten
   weiteren Tafeln auswählen. Auf diesen Tafeln ist keine Bedienung möglich.
3. **Signalversatz je Seite (m)** einstellen, zunächst 10. Seitenabstand, Seite,
   Höhe und Maßstab vorher in den Platzierungseinstellungen prüfen. Bei kurzen
   Abschnitten wird der Versatz automatisch begrenzt.
4. **Streckensicherung mit dieser Bestätigung aktivieren** ist empfohlen.
   Optionale Anforderungskontakte wählen. **Zusätzlich die globale
   Kontakt-Automatik einschalten** nur setzen, wenn alle eingerichteten Kontakte
   dieser Welt arbeiten sollen. Ohne dieses Kontrollfeld bleibt der bisherige
   globale Automatikzustand erhalten.
5. **Blockaufteilung als Vorschau erstellen**. Signalstandorte, Wiederverwendung
   und Kontaktanzahl prüfen. Bei einer neuen Aufteilung sind Grenzabstände noch
   vor dem ersten Bau bearbeitbar; bestehende 0.7.44-Abschnittsgrenzen bleiben erhalten.
6. Für jede neue Tafel **Verbund platzieren in …** öffnen. Die Vorschau zeigt die
   gesamte Einheit. Freie Position grün, Kollision/außerhalb rot. Die neue linke
   obere Ecke anklicken. Abbrechen verwirft die Platzierung dieses Schrittes.
7. **Blockbau und Gleisbild bestätigen** speichert Weltobjekte, Tafeln und
   Streckensicherung gemeinsam. Bei Materialpflicht bleiben neue Objekte Baupläne
   bis zur Fertigstellung. Der Streckenzustand muss danach vollständige GFM und
   gültige Zuordnungen bestätigen; eine deaktivierte/unfertige Anlage darf keine
   Ausfahrt freigeben.

Schema einer inneren Grenze, Fahrtrichtung A → B nach rechts:

```text
A ───── [Signal →] ───── | gemeinsamer GFM | ───── [← Signal] ───── B
                     eine physische Gleislinie
```

Die zwei Signalstandorte sind getrennte Masten. Der Melder ist eine gemeinsame
Grenze der benachbarten Abschnitte. Er setzt nicht pauschal beide Richtungen frei.

## 3. Vorhandenen Aufbau übernehmen oder verschieben

- **Von 0.7.43:** Verbindung öffnen, bisherige Blockanzahl verwenden, Vorschau
  neu erstellen, Bedienort/Anzeigen wählen und Positionen bestätigen. Ein alter
  Doppelkopf-Mast wird unter Erhalt seiner beiden Signal-IDs auf getrennte
  Standorte verteilt. Der eindeutig erkannte alte automatische Diagrammstreifen
  wird ersetzt. Ein manuell gezeichnetes Gleisbild wird nicht pauschal gelöscht.
- **Nur Bedienort/Anzeigen ändern:** Bei einem 0.7.44-Verbund **Nur Bedienzuordnung /
  Anzeige des bestehenden Verbunds ändern** wählen, neue Positionen bestätigen
  und abschließend übernehmen. Das verändert keine Masten/Melder in der Welt.
- **Verschieben:** Im Editor den Auswahlmodus benutzen. Ein Verbundfeld mit der
  linken Maustaste halten und ziehen; die ganze Einheit folgt. Grün = möglich,
  rot = Konflikt. Escape bricht ab. Auch vom Fahrdienstleiter ist **Diesen Verbund
  im Editor verschieben** erreichbar; die Verschiebung selbst erfolgt im Editor.
- Eigene zusätzliche Felder vor einem Neuaufbau aus dem markierten Verbund
  verschieben. Vorhandene normale Signalbeschriftungen, Hilfstastenverknüpfungen
  und Nummernquellen werden beim Neuaufbau erhalten.

## 4. Betrieb in beiden Richtungen prüfen

1. Ganze Strecke freimelden lassen. Nach drei getrennten freien Prüfungen muss
   die Regelerlaubnis sichtbar sein. Der andere Nachbar fordert sie an; Abgabe
   erfolgt nach den gewählten manuellen/automatischen Bedingungen.
2. Ausfahrt in Erlaubnisrichtung stellen. Eine gleichzeitige Gegenfahrt muss
   auch bei noch freien entfernten Blöcken abgelehnt werden. Eine bereits grüne
   Bahnhofsfahrt vor der gemeinsamen Streckenachse ist allein noch kein Nachweis
   einer freigegebenen Streckengegenfahrt.
3. Einen Zug bis hinter ein Blocksignal, aber vor dessen gemeinsamen Melder
   bewegen: noch kein vorzeitiger Haltfall aus dieser Durchfahrprüfung. Hinter
   dem gemeinsamen Melder und dem normalen Durchfahrabstand müssen dieses Signal
   auf Halt fallen und die Zugnummernweitergabe ausgelöst werden. Andere notwendige
   Schutzprüfungen können Fahrt unabhängig davon zurücknehmen.
4. Richtung wechseln und denselben Test wiederholen. Signalrichtung, Seitenabstand
   und gemeinsame Grenze prüfen. Die Zugspitze löst die Durchfahrprüfung aus;
   die Restverschlüsse warten weiterhin auf den Zugschluss und Freiprüfungen.
5. Bei Kontaktbetrieb Lok auf **Zugfahrt** setzen, vorhandene globale Automatik
   prüfen und Folgeziel kontrollieren. Für Handbedienung am zuständigen Ortsstellwerk
   anfordern. Stellwerksübergreifende Fahrstraßenketten benötigen weiterhin das
   übergeordnete ESTW; ein örtlicher Fahrweg darf am Nachbarsignal enden.
6. Anzeigetafel prüfen: Signalzustand/Nummer folgen dem Original, Anklicken darf
   keine Erlaubnis oder Signalbedienung auslösen. Nach Bedienortwechsel darf nur
   das neue Ortsstellwerk die Blocksignale besitzen.

## 5. Änderungssperren und Regression

- Eine unbeteiligte Fahrstraße/Richtungsbindung bestehen lassen und eine lokale
  Hilfstaste einer anderen Strecke ändern: keine anlagenweite Pauschalsperre.
- Änderung an der tatsächlich gebundenen Strecke versuchen: muss abgewiesen werden.
  **Zuletzt gemeldete Änderungssperren** muss die betroffenen Namen und Gründe
  zeigen, einschließlich belegter/nicht lesbarer Intervalle soweit verfügbar.
  Die Liste ist der letzte Versuch, keine laufende Freimeldung. Nach vollständiger
  Auflösung und drei freien Prüfungen erneut speichern.
- Ist das bisherige Hilfsziel ein Streckenendpunkt, zuerst diese konkrete
  Abhängigkeit ändern. Eine Umstellung der Taste darf den Endpunkt nicht verwaisen.
- Zwei verträgliche parallele Fahrstraßen wie im erfolgreichen 0.7.43-Test stellen.
  Anschließend Gegenstellung/echten Flankenkonflikt testen: weiterhin ablehnen.
- Veraltete Vorschau nach einer Konfigurations-/Weltänderung ablehnen. Vorschau
  abbrechen: keine neuen Objekte, Kosten oder Diagrammänderungen.
- Speichern/laden und Client-Anzeigen prüfen. IDs/Zuordnungen bleiben erhalten;
  frühere Fahrtfreigaben werden weiterhin nicht aus dem Spielstand wiederhergestellt.

Bei Fehlern Version, Handlung, genaue Meldung und Diagnoseexport sichern.
Bot-Aufträge, autonome Kreuzungs-/Anschlussanlagen und neue Signalmodelle folgen
in späteren Schritten und gehören nicht zu dieser Abnahme.
