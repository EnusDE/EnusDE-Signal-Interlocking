# 0.7.55-P0 – Bearbeitbare Blockansichten und vollständige Hilfszielverknüpfung

Für Railroader 0.6.3. Alle Teilnehmer benötigen dieselbe Modversion.

## Vorhandenen Verbund bearbeiten

Im Editor ein Feld im Verbund rechts anklicken und **Ansicht des Blockverbunds
bearbeiten: BL-…** wählen. Derselbe Ansichtseditor ist über die Streckeneinrichtung
und **Darstellungsstufe und Spiegelung bearbeiten** im Platzierungsfenster erreichbar.

Tafel auswählen und die gewünschte Darstellung festlegen:

- Vollständiges Gleisbild mit Signalen.
- Gleis und Zugnummern ohne Signalsymbole.
- Nur Zugnummernfelder als schmale, einzeilige Leiste.

Auf zusätzlichen Anzeigetafeln lassen sich erster Abschnitt in A-nach-B-Reihenfolge
und Anzahl der sichtbaren Abschnitte wählen. Ein Abschnitt genügt für eine
Ankunftsvoranzeige. Spiegeln kehrt die horizontale Reihenfolge um. Die separate
Einstellung **Gesamten Verbund um 180° drehen** dreht Gleis, Signalrichtungen,
Erlaubnissymbol und Beschriftungsposition gemeinsam. Beides verändert ausschließlich
die Zeichnung; reale Signalrichtung, Erlaubnis und Fahrstraßendaten bleiben erhalten.

**Ansicht als Vorschau vorbereiten**, bei Bedarf eine freie neue linke obere
Position in der Vorschau anklicken und **Diese Ansicht übernehmen** wählen.
Schließen ohne Übernehmen lässt die gespeicherte Tafel unverändert. Beim noch
nicht bestätigten Blockbau wird nur der Vorschlag geändert und die Platzierung
wieder geöffnet. Der Bau benötigt weiterhin seine abschließende Bestätigung.

Das bedienende Stellwerk behält alle Signalzuordnungen und ursprünglichen
Nummernquellen in einer vollständigen Ansicht. Spiegeln und 180° drehen sind auch
dort möglich. Reduzierte Ansichten gehören auf zusätzliche Anzeigetafeln. Für einen
Wechsel der Bedienzuständigkeit existiert bereits die Auswahl des Blockbetreibers.
Eine Darstellungsstufe verleiht keine Signalbedienung.

Verbundkennung, Strecke, Abschnittsbindungen und Einstellungen je Tafel bleiben
beim Speichern/Laden, Verschieben und Aktualisieren erhalten. Ausgeblendete
Anzeigebeschriftungen und Erlaubnisverknüpfungen bleiben für die Rückkehr zur
vollständigen Ansicht gespeichert. Bei bestätigtem Ersatz der Blockaufteilung
werden die Anzeigeeinstellungen an die neue Abschnittszahl angepasst und die
Nummernfelder mit den neuen Quellen verbunden. Manuell ergänzte Inhalte im Verbund
verhindern einen automatischen Neuaufbau, statt gelöscht zu werden.

## Zugnummern direkt im Gleis

Neue und aktualisierte Anzeigeverbünde zeichnen die Nummernfelder in derselben
Gleiszeile. Nummer, Belegung und Fahrstraßenanzeige kommen von der vorhandenen
Quelle. Es entstehen keine zweite Nummer, Freimeldung oder Bedienberechtigung.
Vollständige Verbünde haben zwei Zeilen: Beschriftung/Erlaubnis und Gleis/Nummern.
Reine Nummernansichten haben eine Zeile. Gespeicherte alte Verbünde wechseln beim
Übernehmen der Ansicht oder Aktualisieren des Gleisbilds in diese Darstellung;
bloßes Laden des Spielstands verschiebt noch keine Felder.

## Hilfsziele im Tutorial

Alle ausgewählten Ausfahrsignale werden nun mit der zugehörigen Zieltaste verknüpft.
Deren gezeichneter Pfeil zeigt entgegen dem örtlichen Einfahrsignal; ausdrücklich
eingestellte Symbolrichtung und Spiegelseite werden berücksichtigt. Das Ziel bleibt
mit der Streckenerlaubnis verbunden. Abweichende vorhandene Ausfahrtsverknüpfungen
müssen geprüft werden und werden nicht überschrieben. Erlaubnismodule im Gleis
unterstützen auch ein einseitiges schematisches Gleisende. Die reale Freimeldegeometrie
wird dadurch nicht verändert.

## Prüfung

Lokale Regressionstests decken Quellenkennung, reine Anzeigeberechtigung,
Abschnittsauswahl, Spiegelung/180° und Rückkehr, Nummern im Gleis, Speichern/Laden,
Neuaufbau und Ersatz, Schutz manueller Inhalte, Bereinigung beim Löschen, mehrere
Ausfahrtsverknüpfungen und Tastenrichtung ab. Spiel-, UI- und Mehrspielertest stehen
aus. Lokal wird mit verfügbaren älteren Spielbibliotheken und dem bestehenden
0.6.3-Pfadadapter gebaut, nicht mit Original-0.6.3-DLLs.
