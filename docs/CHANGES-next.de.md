# Änderungen im Quellcode nach dem Paket 0.7.18-P0

Diese Änderungen sind **nicht im bestehenden 0.7.18-ZIP**, jetzt jedoch im neuen Testpaket **0.7.19-P0** enthalten. Siehe [vollständige neue Änderungen](CHANGES-0.7.19.de.md). Es wurde keine neue Version veröffentlicht oder installiert. Railroader bleibt auf 0.6.2 festgelegt. Für Mehrspieler müssen Host und Clients dieselbe gebaute Modversion verwenden.

## Rangiersignal und Hilfszieltaste in einem Feld

Im Editor das reine Rangiersignal verknüpfen und **Rangier-Hilfszieltaste hinzufügen** wählen. Das Feld enthält dann eine Starttaste und eine unabhängige Rangier-Zieltaste ohne zusätzliches reales Signal. Bei Signalrichtung links steht das Ziel rechts, bei rechts links; senkrechte Felder verwenden entsprechend oben/unten. Spiegeln versetzt beide Schirme gemeinsam, ohne Start und Ziel zu vertauschen.

Das Hilfsziel hat eine eigene Kennung, Bezeichnung, Gleisposition und reale Anfahrtrichtung. Als Ausgangspunkt wird die Signalposition mit umgekehrter Anfahrt vorgeschlagen. Vor dem Speichern prüfen und bei Bedarf anpassen. Das Ziel kann keine Zugfahrstraße und keinen Fahrstraßenstart erzeugen. Bestehende Fahrstraßen und aktive Verschlüsse schützen seine Änderung oder Löschung weiter.

## GFM-Kästen

- Signalname und GFM-Name liegen in getrennten Textzeilen; auch das GFM-Kreuz erhält einen eigenen Platz.
- Kästen am Signal folgen standardmäßig seiner Richtung. Eine Drehkorrektur in Grad ist möglich. Frei stehende Kästen können ebenfalls gedreht und auf A/B umgestellt werden.
- Automatische Weichenkästen sind pro S/L/R-Kasten als A oder B wählbar, um 90° beziehungsweise über einen Gradwert drehbar und weiterhin seitlich, längs und in der Höhe verschiebbar. Sichtbare Kennungen enthalten die Bauart, beispielsweise **GFM-B-W283-L**. Stabile Grenz-IDs ändern sich dadurch nicht.
- Buchstabe und Kennung stehen gemeinsam auf der schwarzen Frontfläche. Der alte kleine Abstand zwischen Modellunterkante und Auflage entfällt. Der Kastenunterbau ist standardmäßig aktiv und kann gezielt abgeschaltet werden; die Bodenerkennung muss im Spiel am Hang geprüft werden.

## GFM-Abschnitt erkennen und übernehmen

Ein Gleis-, Zugnummern- oder Signalfeld öffnen und **Gleisabschnitt erkennen** wählen. Die Suche endet an den nächsten eingerichteten Grenzen und überquert keine Weiche. Eine Weichengrenze wird nur bei aktivierter Weichen-GFM angeboten. Die höchste Abschnittslänge ist einstellbar, standardmäßig **100 m**, zulässig 1 bis 2000 m.

Die Vorschläge nennen Grenzkennungen, Länge und Feldanzahl. Erst **Vorschlag übernehmen** ändert die Zuordnung. Der vollständige eindeutige Gleisbildweg wird einschließlich Zugnummernfeldern und inneren Signal-/Weichenseiten zugeordnet. Die äußeren Seiten behalten ihre Zuordnung. Fehlende Verbindungen, fremde Grenzen und nicht eindeutige Wege führen zu einer Meldung; ausdrücklich markierte Felder bleiben als manueller Weg verfügbar.

Vorhandene Abschnitte können über die Suchliste **Vorhandener vollständiger Abschnitt** aus einem Feld dazwischen auf den ganzen Gleisbildweg übernommen werden. Auch bei manuell ausgewählten Grenzen gibt es die vollständige Übernahme.

Ohne aktivierte GFM erscheinen Weichen gelb als unüberwacht und liefern keine GFM-Grenzvorschläge. Nur automatisch erkannte, noch nicht als GFM definierte normale Bereiche erscheinen ebenfalls unüberwacht. Gespeicherte Zuordnungen bleiben erhalten. Die physischen Belegungs- und Verschlussprüfungen für Fahrstraßen bleiben unabhängig davon wirksam. Ein Besetzt-Testmodus wurde nicht ergänzt.

## Elektrische Weichenantriebe des Spiels

Unter **Bauen → Weichen-Gleisfreimeldung** die Weiche auswählen. **Elektrischen Spielantrieb verwenden** aktiviert den vorhandenen elektrischen Antrieb nur als Modellwahl. Alternativ lassen sich alle Weichen des zugehörigen Stellwerks gemeinsam umstellen und wieder auf ihre Originalmodelle zurücksetzen.

Der Austausch ändert weder die CTC-Eigenschaft noch die Handbedienrechte. Die native Klickbindung und Animation bleiben erhalten. TFs können zuvor handbedienbare Weichen weiter umstellen, soweit keine bestehende Verriegelung entgegensteht. Bereits native CTC-Weichen behalten ihre ursprünglichen Regeln. Modellauflage, Handbedienung, Neuladen und Anzeige auf Clients müssen im Spiel geprüft werden. Eine Terrainänderung ist nicht enthalten.

## Geschwindigkeitsschilder

Die Umrechnung gilt wieder auch für unterstützte feste Kartenschilder. Das native Schildmodell und sein Shader bleiben erhalten. Eine eigene Material-/Texturkopie ersetzt nur sicher erkannte Geschwindigkeitszahlen in km/h. Die Zahlen werden auf Vorder- und Rückseite getrennt aus den Originalflächen gelesen; die ursprünglichen Farben für Begrenzung beziehungsweise Aufhebung bleiben erhalten. Eine unbeschriftete Rückseite bleibt unbeschriftet. Beispielsweise werden 25 mph zu 40 km/h; eine anders beschriftete Rückseite wird mit ihrem eigenen Ausgangswert umgerechnet.

Die Darstellung greift nicht in tatsächliche Geschwindigkeitsgrenzen oder Spielstanddaten ein. Originale Meshes, Materialien und Texturen werden nicht überschrieben. Beim Wechsel auf mph oder beim Abschalten werden die ursprünglichen Materialzuordnungen wiederhergestellt. Für importierte Texturen und Meshes ohne CPU-Lesefreigabe gibt es einen GPU-Leseweg. Unbekannte Modelle, nicht sicher erkannte Zahlen oder nicht lesbare Daten werden unverändert belassen und als nicht umgerechnet gemeldet; es wird kein pauschaler Wert aus dem Modellnamen angenommen.

Die Textumrechnung von Spielerschildern bleibt erhalten, ohne ihre Farben zu ersetzen. Die tatsächlichen 0.6.2-Kartentexturen sind im Cloud-Workspace nicht vorhanden; Front/Rückseite, Schriftlesbarkeit, Farbtreue, GPU-Leseweg und Rückkehr zur Originaldarstellung müssen deshalb im Spiel geprüft werden.

## Erstes Automatik-Testpaket

Neu im Quellcode: drei ausdrückliche Lok-Betriebsarten, platzierbare Gleiskontakte mit experimenteller Achspassageanzeige, Erst-/Ausweichziele mit Wiederholungsprüfung und gemeinsame Anforderung vollständiger Fahrstraßenfolgen. Der vorhandene Fahrstraßenkern bleibt für die eigentliche Freigabe zuständig.

Die optionale Blockabstimmung bildet Richtungsgruppen aus eingerichteten GFM-Abschnitten. Sie wirkt auf automatische und manuelle Zuganforderungen, hält die Gegenrichtung auch bei Restverschlüssen gesperrt und löst erst nach drei vollständigen Freiprüfungen ohne zugehörige Fahrstraße auf. Bei aktiver Blockprüfung müssen Zugfahrwege durchgehend GFM-überwacht sein. Eindeutig örtliche Rangierfahrten ohne überquerte Bahnhofsgrenze bleiben in ihrer normalen Fahrstraßenprüfung.

Ein-/Ausfahr-, Zwischen- und Blockfunktionen, Stellwerksannahme, Automatikbetrieb und Sichtbarkeit in der Stellwerksauswahl sind einstellbar. Die native Wegpunkt-KI kann gegen das Umstellen zugeordneter Weichen gesperrt werden, ohne native CTC-/Spielerrechte zu verändern. Wegpunktziele vor dem Startsignal lösen keine Durchstellung aus; zu weit reichende Folgen werden am tatsächlichen Zielabschnitt begrenzt. Änderungen an Verband oder Auftrag sowie Zurücksetzen nehmen noch nicht befahrene Automatikaufgaben zurück. Gesamt-Halt verhindert die erneute Anforderung bereits wartender Aufgaben.

Die Menüs sind nach Status, Signalautomatik, Kontakten, Stellwerken und Lok-Betriebsarten gegliedert. Weltweite Schalter liegen unter **Spieloptionen → Signaltechnik → Automatisierung und KI**. Alle Automatikfunktionen beginnen ausgeschaltet; neue Kontakte sind deaktiviert. Der Host steuert, Clients erhalten Statusdaten. Die Achsanzeige ist ausschließlich ein Versuch und beeinflusst keine Freimeldung.

Einrichtung, Einschränkungen und der A–B–A-Sandbox-Test stehen in [Automatik-Testanleitung](AUTOMATION-TEST.de.md). Das größere Antragssystem am Signal und automatische Betriebsartwechsel bleiben für spätere Arbeiten vorgesehen.

## Unabhängige Anlagensicherung

Der Host schreibt jetzt nach vollständigem Laden und erfolgreichen Anlagenänderungen zusätzlich eigenständige Sicherungsdateien außerhalb von Mod- und Spielstandordner. Der gemeinsame Spielstandbestand bleibt für Übertragung und Mehrspieler erhalten. Pro Anlage werden 32 automatische Versionsstände aufbewahrt; manuelle Stände und Stände vor Löschungen bleiben dauerhaft erhalten. Ein abweichender geladener Spielstand überschreibt den zuvor extern gesicherten Konfigurationsstand nicht ohne geschützte Kopie.

Unter **Spieloptionen → Signaltechnik → Anlagensicherung und Wiederherstellung** stehen Speicherpfad, Status, manuelle Sicherung, Versionsauswahl und geprüfte Wiederherstellung bereit. Fehlen die Moddaten im Spielstand, werden passende vorhandene Sicherungen vor einer Neuinitialisierung angeboten. Gelöschte Objekte kehren ausschließlich durch ausdrückliche Wiederherstellung zurück.

Gesichert werden Objekte, Stellwerke, GFM-Zuordnungen, Fahrstraßendefinitionen, Automatikregeln und Materialdaten. Alle Signale starten nach Wiederherstellung Halt; Kontakt-Automatik, Lok-Festlegungen und aktuelle Zugnummernpositionen werden nicht aus einem alten Betriebszustand reaktiviert. Prüfsummen, atomare Dateien, dauerhafte Vorher-Stände und ein Rohdatenjournal schützen den Wiederherstellungsvorgang. Die aktuelle Haltfalllogik wurde dabei nicht geändert.

Siehe [Speicherorte, Sicherungsumfang und Wiederherstellung](BACKUPS.de.md). Noch nicht im bestehenden 0.7.18-ZIP enthalten.

## Zugnummern als örtliche Anzeigekopie

Ein gespeichertes ursprüngliches Zugnummernfeld aus dem ESTW oder einer anderen Tafel kann im Ortsstellwerk als schreibgeschützte Kopie angezeigt werden. Quelle und Kopie verwenden dieselbe laufende Nummernzuordnung. Das Original erhält eine feste Kennung, sodass die Verknüpfung auch nach Verschieben oder Zeileneinfügen erhalten bleibt.

Im Editor ein Zugnummernfeld wählen, unter **Quellenfeld – ESTW zuerst** das Original auswählen und **Anzeigekopie hier anlegen** verwenden. Die sichtbare Kopie liest benötigte Quellabschnitte auch bei geschlossener Quelltafel. Fehlende Quellen und veraltete Host-Daten werden als **?** angezeigt. Die Kopie besitzt keine Gleisanschlüsse, eigene Nummer oder Fahrstraßenfunktion; Quelle und aktueller Wert sind im Inspektor sichtbar. Beschriftungen wie „aus Ela“ erscheinen unter der zusätzlichen Anzeige.

Sicherung, Wiederherstellung und Zeichnungstransfers berücksichtigen die Quellenkennungen. Der erste verknüpfte modulare Ausbau des übergeordneten Stellwerks ist in 0.7.19-P0 enthalten. Siehe [Einrichten und Prüfen der Zugnummern-Anzeigekopien](TRAIN-NUMBER-MIRRORS.de.md).

## Prüfgrenze

Die Änderungen werden gegen die bereitgestellten Railroader-0.6.2-Bibliotheken gebaut und offline geprüft. Das ist keine Unity-, Gelände- oder Mehrspielerabnahme. Installieren, Spielstart und Veröffentlichung erfolgen durch den Nutzer.
