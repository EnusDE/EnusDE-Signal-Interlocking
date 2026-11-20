# 0.7.36-P0 – GFM-Neuscan und Diagnose

Teststand für Railroader 0.6.2. Bestehende Testwelt verwenden. Host und Clients benötigen dieselbe Modversion. Enthält die Änderungen aus 0.7.35. Spiel- und Mehrspielerabnahme bleiben offen.

## Vollständiger GFM-Neuscan

1. Im **Editor → Menü → GFM in diesem Gleisbild erkennen …** öffnen.
2. Für eine vollständige Bereinigung der Testanlage **Alle Stellwerke einbeziehen** aktivieren. Ohne diese Option wird die aktuelle Tafel bearbeitet; bei einer modularen ESTW-Ansicht gehören ihre eingebundenen Ortsstellwerke zum Umfang.
3. **GFM-Zuordnungen vollständig neu erkennen · Vorschau** wählen. Die Vorschau entfernt bisherige Abschnittsbindungen und erkennt anschließend vollständige Abschnitte aus den aktuell vorhandenen Grenzen. Die laufende Anlage wird dabei noch nicht geändert.
4. Anzahl ersetzter Abschnitte, zurückgesetzter Felder, korrigierter Weichenast-Zuordnungen und ungeklärter Verbindungen prüfen.
5. Vor Übernahme alle Fahrstraßen und Richtungsbindungen vollständig auflösen und wartende Automatikanforderungen zurücknehmen. **GFM-Zuordnungen durch diese Vorschau ersetzen** erzeugt zuerst eine Sicherung und speichert danach den neuen Stand. Bei fehlgeschlagener Sicherung oder veraltetem Vorschlag wird nicht übernommen.

Der Neuscan ersetzt GFM-Abschnittsdefinitionen, rohe Meterabschnitte und Abschnittsbindungen an Gleisbildfeldern. Zeichnung, Signal-/Weichenverknüpfungen, GFM-Grenzmarken, Weltkästen, individuelle Kastenpositionen und Zugnummernfeld-Identitäten bleiben bestehen. Es werden weder physische Kästen neu gebaut noch deaktivierte Weichen-GFM eingeschaltet. Die normalen Signalbetriebszustände werden nicht umgeschaltet.

Beim örtlichen Neuscan bleiben alte Abschnitte bestehen, wenn eine andere, nicht bearbeitete Tafel noch darauf verweist. Für die komplette Bereinigung alter gemeinsamer Einträge deshalb **Alle Stellwerke einbeziehen** verwenden; das umfasst auch vorhandene Tafeln früherer Zeichnungsstände. Die externe Sicherung bewahrt den vorherigen Anlagenstand.

Die bisherige Schaltfläche **GFM-Abschnitte im vorhandenen Gleisbild suchen** bleibt als ergänzende Erkennung erhalten. Sie ersetzt alte Einträge nicht vollständig.

## Weichenarme und Besetztanzeige

Ein gerader gezeichneter Ast ist nicht zwingend der reale Grundast der Spielweiche. Die Erkennung leitet die tatsächlichen Rollen aus eindeutigen physisch erkannten Grenzpaaren und ihren gezeichneten Anschlüssen ab. Zwei unabhängig bestimmte Arme reichen, um auch den dritten zuzuordnen. Drehung und Spiegelung der Tafel werden berücksichtigt. Bei nur einem bekannten Anschluss oder widersprüchlichen Verbindungen wird nicht geraten.

Bei Bedarf ein Weichenfeld rechtsklicken → **Gleisfreimeldung**. Unter **Reale Weichenäste in dieser Tafel** Stammgleis und realen Grundast anhand der Richtungspfeile wählen. **Feld speichern**, danach GFM neu erkennen. Die Korrektur setzt eine ruhende Anlage voraus und verändert die Zuordnung im Gleisbild, nicht die reale Weichenstellung.

Die Weichendarstellung färbt jeden Arm nach seinem eigenen gelesenen Schutzabschnitt. Eine Belegung irgendwo in der Gesamtweiche färbt nicht mehr pauschal alle Arme rot. Der betriebliche Schutz betrachtet weiterhin die gesamte Weiche als belegt, sobald einer ihrer Schutzbereiche belegt ist. Ein langer Wagen oder Zug über mehreren Armen kann weiterhin mehrere rote Arme verursachen.

## Abschnittsliste und Hervorhebung

In der GFM-Erkennung **Abschnitte und Überwachungslücken prüfen …** öffnen. Die Liste enthält zugehörige gespeicherte Abschnitte und Weichen sowie aktuell physisch erkannte, noch nicht übernommene Abschnittsvorschläge. Grenzen, Länge, Meterbereiche, Anzahl verknüpfter Gleisbildfelder und Definitionsfehler werden angezeigt.

**Abschnitt und Grenzen hervorheben** markiert die zugeordneten Gleise cyan und die Grenzen mit einem Rahmen. Normale Belegungsfarben bleiben sichtbar. Null verknüpfte Felder bedeutet: kein gespeicherter Anzeigeanschluss für diesen Eintrag. Ein neu erkannter Vorschlag ist noch keine eingerichtete Überwachung und zählt nicht zur Abdeckung.

**Abschnittszuordnung am Feld bearbeiten …** öffnet für einen bereits zugeordneten Abschnitt das passende Gleis-/Signal- oder Weichenfeld. Dort bleiben die vorhandenen manuellen Grenz- und Abschnittswerkzeuge verfügbar. Anschließend die Diagnose aktualisieren.

## Fahrweg im Editor testen

Startsignal, Zielsignal und Fahrart wählen → **Testwege berechnen · keine Bedienung**. Bei Alternativen den gewünschten Testweg auswählen und **Abdeckung und genaue Lücken anzeigen** verwenden.

Das ist ausschließlich eine Berechnung: keine Signalstellung, kein Weichenbefehl, keine Fahrstraßen- oder Richtungsfestlegung, kein neuer gespeicherter Fahrstraßeneintrag. Die Prüfung zählt nur gültige gespeicherte GFM-Abschnitte und aktivierte Weichenüberwachung. Fehlende Meterbereiche werden als genaue Intervalle aufgeführt. Orange Rahmen kennzeichnen Felder, deren gespeicherte Bindungen zu den Lücken gehören. Ohne Gleisbildzuordnung kann die genaue Stelle nicht aus Rasterkoordinaten erraten werden; dann gelten die Gleis-ID und Meterangaben in der Liste.

Änderungen an Anlage oder Gleisnetz erfordern **GFM-Prüfung aktualisieren**. Wird eine Hervorhebung oder Wegprüfung nach einer Änderung angefordert, muss die veraltete Aufnahme abgewiesen werden. **Hervorhebung aufheben** entfernt die Anzeige; außerhalb dieser Editor-Prüfansicht ist sie nicht aktiv.

## Die langen Abschnitte prüfen

Die drei bisherigen Nullwerte bei ungebundenen Signalen, fehlenden Signalkästen und deaktivierten Weichen-GFM beweisen keine lückenlose Abschnittszuordnung. Deshalb zuerst vollständig neu erkennen und dann den langen Abschnitt in der Liste prüfen:

- **Physisch erkannt, aber nicht gespeichert/zugeordnet:** Vorschau und Gleisbildverbindung prüfen, dann übernehmen.
- **Definitionsfehler:** genannte Grenzen bzw. Weiche prüfen.
- **Keine durchgehende Gleisbildverbindung:** Anschlüsse, weitere Grenzmarken und Astzuordnung prüfen; der Scan zeichnet keinen fehlenden Gleisweg hinzu.
- **Gegengrenze nicht zugeordnet:** die genannte Grenze in der Tafel zuordnen.

Die Anzeige nennt jetzt lesbare Abschnittsnamen und den konkreteren Grund statt zusätzlich lange interne Endpunktkennungen anzuhängen. Der gemeldete lange Abschnitt ist ohne den tatsächlichen Spielstand noch nicht praktisch abgenommen.

## Gezielte Abnahme

Vorher Sicherungsbestand kontrollieren. Alte manuelle Einträge, gemeinsamen A/B-Kasten, vertauschten Grund-/Abzweigast, gedrehte/gespiegelte Weichen und lange Abschnitte testen. Vorschau abbrechen muss den Ausgangsstand behalten. Ein Neuscan ohne vollständige Grenzpaare muss die betroffenen Felder unüberwacht lassen. Belegte/unklare Bereiche dürfen danach keine neue Fahrt freigeben. In der Diagnose dürfen weder Signale noch Weichen schalten. Bei Fehlern vollständige Meldung, Mod-/Spielversion und Player.log angeben.
