# 0.7.19-P0: Testen mit Whittier und Ela

Dies ist ein neues Testpaket für Railroader 0.6.2. Es ersetzt keine veröffentlichte 0.8.0. Host und Mitspieler benötigen dieselbe Version. Es wurde gegen die Spielbibliotheken gebaut und offline geprüft; Unity, Spielstandwechsel und Mehrspieler sind noch im Spiel abzunehmen.

## 1. Niedriges Hauptsignal

1. Im Sandkasten ein Rangiersignal platzieren und anklicken.
2. **Zum niedrigen Hauptsignal aufwerten** wählen. Kennung, ID, Richtung, Position, GFM-Anbau und bestehende Verknüpfungen bleiben erhalten.
3. Hp 0, Hp 1, Hp 2 und Sh 1 prüfen. Bei Sh 1 bleibt das rote Hauptsignallicht erhalten; die beiden weißen Lichter zeigen Rangierzustimmung. Es ist ein eigenes, stilisiertes Modell von etwa 1,73 m Höhe, kein exakt nachgebildeter deutscher Regeltyp.
4. Bodenauflage und Unterbau am Hang prüfen. In der normalen Mastkonfiguration sind Vorder- und Rückseite verfügbar; ein integrierter Vorsignalkopf ist bei dieser niedrigen Bauform ausgeschlossen.
5. Eine Rangierstraße vor der Aufwertung speichern und danach erneut stellen. Während einer aktiven Fahrstraße muss der Umbau verweigert werden. Durchgehende Fahrstraßen, in denen das Signal bisher als mitlaufendes Rangiersignal diente, müssen nach der Aufwertung neu berechnet werden. Im Wirtschaftsmodus wird nur das zusätzliche Material gegenüber dem bereits gebauten Rangiersignal verlangt.

Das Gleisbild verwendet dieselbe Hauptsignaldarstellung wie bei einem hohen Mast. Eine vorhandene Rangier-Hilfszieltaste bleibt nutzbar. Bei Bedarf die Rangierfahrt über das Signalmenü auswählen.

## 2. Zwei Bahnhöfe für die Automatik

1. Whittier und Ela als getrennte örtliche Stellwerke einrichten. Hauptsignale für beide Fahrtrichtungen platzieren und die Signalfunktionen Einfahrt / Ausfahrt / Zwischen- oder Blocksignal zuweisen.
2. Sämtliche erforderlichen Freimeldeabschnitte einrichten. Weichen-GFM bei Bedarf ausdrücklich aktivieren. Ein unüberwachter oder unbekannter Teil darf bei aktiver Blockprüfung keine neue Zugfahrstraße freigeben.
3. Unter **Automatisierung und KI → Stellwerke und Annahme** die Betriebsstellenart und Annahme festlegen. Zur Verfügung stehen Bahnhof, Begegnungsbahnhof sowie Blockstelle / Anschluss. Die Schutzlogik entsteht aus den realen Fahrwegen, den Signalfunktionen und den Freimeldegrenzen; die Betriebsstellenart allein ersetzt diese Zuordnung nicht.
4. AE-Signalanbindung, Blockrichtungsprüfung und Kontakt-Automatik einschalten. Für die Startsignale ein Erstziel und gegebenenfalls ein Ausweichziel wählen.
5. Anforderungskontakte über den Gleis-Rechtsklick platzieren. Signal, Fahrtrichtung und Position prüfen und Kontakte aktivieren. Neue Kontakte sind zunächst ausgeschaltet.
6. Im Lokfenster **Signaltechnik – Fahrinformationen → Zugfahrt** festlegen. Nach Kuppeln oder Trennen erneut festlegen. Die anderen gekuppelten Loks erhalten keine eigene Festlegung.
7. Einen Road-/Wegpunktauftrag über das jeweilige Signal hinaus geben. Für Hin- und Rückfahrt Ziele und Kontakte in beiden Richtungen einrichten. Die Mod erstellt keine eigene dauerhafte KI-Pendelroute; dafür wird ein passender Auftrag beziehungsweise ein vorhandenes Wegpunktsystem benötigt.
8. Erst eine Richtung testen, danach mit einem zweiten Zug eine Gegenanforderung erzeugen. Solange der zusammenhängende eingleisige Bereich gebunden oder besetzt ist, darf die Gegenrichtung nicht freikommen. Auch Restverschlüsse müssen berücksichtigt werden.
9. Einen Wegpunkt vor das Startsignal legen: Dieser Auftrag darf keine Fahrstraße darüber hinaus auslösen. Rangierfahrt und Keine Festlegung dürfen die Zugkontakt-Automatik ebenfalls nicht auslösen.

Die Kontakt-Achszählung ist ein Experiment, abhängig vom Fahrzeugmodell. Sie ist **keine Achszähler-Gleisfreimeldung** und löst keine sichernde Belegung auf. Die bisher akzeptierte Haltfalllogik ist unverändert.

## 3. Übergeordnetes Stellwerk

Im Editor **ESTW-Module** öffnen. Die örtlichen Stellwerke als **verknüpfte Module** hinzufügen und mit X/Y verschieben. Überlappungen, belegte Streckenfelder und überschrittene Flächengrenzen werden abgewiesen. Das örtliche Gleisbild bleibt die Quelle; es wird keine zweite physische Anlage angelegt.

Im Fahrdienstleiter die ESTW-Tafel öffnen. Beide örtlichen Tafeln erscheinen zusammen mit den eigenen Streckenfeldern des ESTW. Örtliche Änderungen müssen nach erneutem Öffnen dort erscheinen. Verborgene Stellwerke werden nicht eingeblendet. Die Anbindungen verwenden die tatsächlich eingerichteten Fahrstraßen und Freimeldewege. Streckenfelder zwischen den Modulen werden weiterhin im normalen ESTW-Editor gezeichnet und mit der Welt verknüpft.

Unter **ESTW-Module und zentrale Übernahme** den Selbststellbetrieb eines Bahnhofs pausieren. Neue Kontaktanforderungen müssen warten; bereits festgelegte Fahrstraßen bleiben überwacht. In einem zuvor ausschließlich automatischen Bahnhof sind nach der Übernahme manuelle Anforderungen möglich. Anschließend ausdrücklich wieder einschalten. Die Pause wird pro Welt gespeichert.

Eine Zugnummern-Anzeigekopie aus dem ESTW im Ortsstellwerk platzieren. Auch bei geschlossener Quelltafel muss sie folgen. Sie bleibt schreibgeschützt. Siehe [Zugnummern-Anzeigekopien](TRAIN-NUMBER-MIRRORS.de.md).

## 4. Sprache und F1

Unter **Spieloptionen → Signaltechnik** zwischen Automatisch, English und Deutsch wählen und anwenden. Ohne Übersetzungsmod ist Englisch der Standard. Automatisch übernimmt die Sprachwahl des aktiven EnusDE-Übersetzungsmods. Eigene englische/deutsche Menütexte haben Vorrang; für weitere dort verfügbare Sprachen wird der Übersetzer genutzt, sonst Englisch. Einzelne technische Altfehlermeldungen und zusammengesetzte Diagnosewerte können weiterhin deutsch sein.

F1 öffnen: Unter EnusDE Signal & Interlocking stehen Signalbedeutungen und Bedienung. Mit den bestehenden Host-Rechten erscheinen zusätzlich Bauen, Automatik/ESTW und Sicherungen. Das ist noch kein neues frei konfigurierbares Spielerrollensystem.

## 5. Meilenstein und Materialbau

- Neue Wirtschaftswelt: **Signaltechnik – Ausbau** unter Meilensteine starten. Grundpreis 7.500 $; die normalen Spielregeln für Meilensteinrabatte gelten. Die Voraussetzung kann für die aktuelle Karte unter **Material und Baukosten** ausgewählt werden. Ohne Auswahl ist keine zusätzliche Voraussetzung eingerichtet.
- Der Bauauftrag bleibt dauerhaft offen. Neue geplante Objekte erscheinen automatisch, fertige verschwinden. Es gibt eine Summe des noch benötigten Materials.
- Im Ladungsreiter eines eigenen FM-Flachwagens Baumaterial oder eines XM-Güterwagens Signaltechnik bestellen. Der Wagen muss am aktiven Übergabegleis stehen, darf keinen fremden Frachtauftrag und keine andere Ladung haben.
- Materialwagen passend zum nativen MOW-Arbeitszug ankuppeln, MOW-Ausrüstung einschalten und innerhalb von 250 Gleismetern bei den geplanten Anlagen anhalten. Materialbedarf und Verbrauch prüfen. Bei fehlendem Material darf kein halber Verbrauch stattfinden.
- Der bisherige Weg über Materiallager und manuelles Fertigstellen bleibt verfügbar.
- Neue Sandkastenwelten bauen sofort. **Materialbedarf auch im Sandkasten** und **Ohne Materialbedarf im Wirtschaftsmodus** sind unabhängig einstellbar. Ältere ausdrücklich gesetzte Materialeinstellungen werden bei der Migration beibehalten; vorhandene Anlagen erhalten keine nachträgliche Rechnung.

## 6. Speicherung und Mehrspieler

Vor dem Test unter **Anlagensicherung und Wiederherstellung** einen manuellen Stand anlegen. Danach speichern, Spielstand neu laden und Module, Bauaufträge, niedriges Hauptsignal, Zugnummernkopien und Betriebsstellenkonfiguration prüfen. Wiederherstellung übernimmt keine alten Fahrtfreigaben; Automatik bleibt nach der Wiederherstellung ausgeschaltet. Siehe [Sicherungen](BACKUPS.de.md).

Erst danach mit einem Client prüfen: gleiche Version, identische Signalmodelle und Tafelbilder, aktuelle Zugnummern und Belegungen. In dieser Testfassung bleiben Bau und Bedienung beim Host.

Formsignale, Wartungsstörungen, öffentliche Spieleranträge am Signal, vollständige Auftrags-/Fahrdienstleiter-KI und automatische Betriebsartwechsel bleiben spätere Erweiterungen.
