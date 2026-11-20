# 0.7.21-P0 – Freie Strecke und GWB einrichten

Neuer Teststand für Railroader 0.6.2. Die Pakete bis 0.7.20 bleiben unverändert. Für Mehrspieler benötigen Host und Clients dieselbe Version.

- Eigener Editorbereich **Freie Strecke / GWB**, direkt aus der Stellwerksauswahl, der Werkzeugleiste und den ESTW-Modulen erreichbar.
- Nachbarbetriebsstellen A/B, Streckenkennung, Gleisbezeichnung und Betriebsart je physischem Streckengleis: eingleisig, Richtungsgleis oder GWB.
- Automatische Erkennung mit Fahrwegvorschau, wählbaren Alternativen und Prüfung des gemeinsamen Hin-/Rückwegs sowie der GFM-Abdeckung. Erst Übernehmen speichert.
- Getrennte Erlaubnisse für parallele Gleise, automatische oder manuelle Richtungswahl, zeitweilige manuelle Übernahme und Wiederübernahme in die Automatik.
- Transaktionale Einbindung in die vorhandene Fahrstraßen- und Blockprüfung. Gegenanforderungen, Restverschlüsse, Belegung, unbekannte Freimeldung und überlappende Blockbindungen verhindern einen unzulässigen Wechsel. Drei vollständige Freiprüfungen vor einer neuen Richtung.
- Platzierbarer **Streckenerlaubnismelder** mit zwei Richtungspfeilen und Liveanzeige im örtlichen Stellwerk und ESTW. Der Melder ist kein Signal und übernimmt keine Gleisbelegung.
- Speicherung, externe Sicherungen, Wiederherstellung und begrenzte Host-/Client-Livemeldungen berücksichtigen die Streckenkonfiguration. Laufende Erlaubnisse werden nicht als alte Freigaben wiederhergestellt.
- Englische/deutsche Einrichtungstexte und F1-Hilfe ergänzt. Die Signalseitenkorrektur aus 0.7.20 bleibt enthalten.

Dies ist die erste einrichtbare Strecken-/Erlaubnislogik. Spielabnahme bleibt erforderlich. Der Wiederanlauf einer bereits belegten Strecke ohne bekannte Richtungsbindung wird noch nicht automatisch übernommen; Wiederanlauftests mit freier Strecke beginnen. Eine getrennte Erlaubnisannahme zwischen zwei menschlichen Fahrdienstleitern ist noch nicht enthalten.

[Einrichtung und Test auf Deutsch](FREE-LINE-GWB.de.md) · [English setup guide](FREE-LINE-GWB.en.md) · [Bisheriger Automatik-Test](TEST-0.7.19.de.md)
