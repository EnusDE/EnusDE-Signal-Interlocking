# 0.7.70 — Fahrstraßenfarben und automatische Fahrartwahl

Entwicklungs-Testfassung für Railroader 0.6.3. Alle Spieler benötigen Hauptmod 0.7.70. Das kompatible Runenwacht 0.2.2 kann ohne neues Assetpaket weiterverwendet werden; die Darstellungsschnittstelle API v1 bleibt unverändert. Dies ist ein Schritt Richtung 0.8.0, keine abgeschlossene Release-Abnahme.

## Gleisanzeige

| Zustand | Gleisfarbe |
|---|---|
| Festgelegte Zugfahrstraße, auch Langsamfahrt | Grün |
| Festgelegte Rangierstraße | Weiß |
| Belegter Abschnitt, auch ein belegtes Rangierziel | Rot |
| Unbekannte/nicht verfügbare Freimeldung | Bernstein |
| Fahrstraße in Vorbereitung / Schutzwartezeit | Bisherige bernsteinfarbene Blinkanzeige |
| Freigegebener / freier unreservierter Abschnitt | Bisheriges Grau |

Maßgeblich ist die Fahrart der tatsächlich festgelegten Fahrstraße (`RouteDefinition.Shunting`), nicht die Signalbauform, ein dazwischenliegendes Sh 1, ein Kennungspräfix oder die zuletzt gewählte Bedienoption. Die Farben gelten für Gleisfelder, getrennt zugeordnete GFM-Halbgleise, aktive Weichenarme, den Abschnitt zwischen Signalen und integrierte Zugnummern-/Spiegelfelder. Signallampen und Erlaubnispfeile behalten ihre bisherige Bedeutung. Nur noch verschlossene Abschnitte und nicht aufgelöste reguläre Weichenfahrwege erhalten die Fahrstraßenfarbe. Weiß/Grün überdecken weder Belegung noch unbekannte Meldungen. Fasst ein grober Anzeigeblock getrennte Fahrarten zusammen, hat Grün eine feste Vorrangregel; genaue örtliche GFM-Zuordnungen bleiben wichtig. Restverschlüsse im Fehlerfall behalten die gesonderte bisherige Darstellung.

Eine zulässige Rangierfahrt in ein belegtes Gleis hebt die Belegung nicht auf: Der freie Zulauf erscheint weiß, das belegte Ziel bleibt rot. Bestehende Prüfungen für Belegung, Weichen, Flankenschutz, Erlaubnis und Zugidentität bleiben erhalten. Daraus entsteht keine Freigabe durch besetzte Weichenschutzbereiche oder unbekannte Freimeldung.

## Automatische Auswahl

Die Wegpunkt-Fahrart an der Lok wird als **Automatisch · Zugfahrt bevorzugen** beschrieben. Ausdrücklich gewählte Zug- und Rangierfahrt bleiben bindend. Die vorhandene automatische Erkennung reiner Rangiersignale bleibt erhalten.

Bei einem Startsignal mit beiden Möglichkeiten hat ein entlang des Wegpunkts grundsätzlich erreichbares Zugfahrziel Vorrang. Auch vorübergehend nicht betriebsbereite Hauptsignale zählen bei dieser strukturellen Auswahl. Deshalb lösen Belegung, ein nicht verfügbares Signal, fehlende Erlaubnis, Mehrdeutigkeit oder ein Planungsfehler keinen Wechsel zur Rangierfahrt aus.

Nur ohne Zugfahrziel kann die automatische Erkennung eine unterstützte örtliche Rangieralternative wählen. Dafür sind dasselbe Ortsstellwerk, die richtige Richtung und ein innerhalb des aktuellen nativen Wegpunktwegs nachgewiesener Verlauf nötig. Die Alternative darf keine eingerichtete freie Strecke überfahren (auch keine deaktivierte), kein Blocksignal verwenden und keinen Weg über den nachgewiesenen Abschnitt hinaus erfinden. Danach gelten weiterhin normale Fahrstraßenberechnung und Zulassungsprüfung; die Anfrage kann abgewiesen werden. Nach abgelehnter Zugfahrstraßenzulassung gibt es keinen zweiten Versuch als Rangierfahrt. Händisch gewählte Fahrarten werden nicht umgestellt.

## Prüfstand und Spielabnahme

Vollständiger **Test.ps1-Lauf: 2.170 PASS, 0 FAIL** am 25.11.2026 gegen lokale originale Railroader-0.6.3-Bibliotheken; beide Assemblies bauen. Neue Prüfungen: Fahrartmetadaten, Rot-/Unbekannt-Vorrang, Vorbereitung/Fehler/Abbruch, aufgelöste Abschnitte, zusammengefasste Anzeigen, Zugvorrang ohne unnötige Alternativsuche, ausdrückliche Auswahl, weitergegebene Zielfehler, örtliche/fremde/entgegen gerichtete Wege, Streckenüberschneidungen und ausgeschlossene Blocksignale. Der gemeinsame Live-Frame enthält die Fahrart bereits; keine Konfigurationsmigration und kein neues Netzwerkformat.

Noch im Spiel prüfen: grüne Zugfahrstraßen einschließlich Langsamfahrt und dazwischenliegendem Rangiersignal; weiße Rangierwege zu roten belegten Zielen; Ablehnung bei besetztem/unbekanntem Weichenschutz; passende Farben an Weichenarmen, Halbgleisen und integrierten/gespiegelten Nummernfeldern; Auflösung und Restverschlüsse; Zugvorrang gegenüber rein örtlichem Rangierziel; kein Wechsel bei belegter Zugfahrstraße oder fehlender Streckenerlaubnis; beide Richtungen und ausdrückliche Fahrartwahl; Host/Client und ESTW-Anzeige. Außerdem bleiben die offenen 0.7.69-Tests für Blockweiterfahrt/Folgezug und native Wendefahrten bestehen. Kein Spielstart, keine Installation, kein Tag und kein öffentlicher Release durch den Assistenten.
