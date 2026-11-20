# 0.7.26-P0 – einfachere Modulbedienung und Weichengrenzen

Lokaler Teststand für Railroader 0.6.2. Bestehende Spielstände weiterverwenden. Vorbereitete ältere Pakete bleiben unverändert. Neue UI-Texte sind auf Englisch und Deutsch enthalten.

- ESTW-Modulansicht erhält getrennte Kopfzeile, Steuerfläche, Vorschau und Statusleiste. Keine überlagerte native Formularfläche; automatische Einpassung statt festem 20-%-Zoom. Module werden mit der Maus gezogen, Rechtsklick öffnet die örtliche Bearbeitung oder löst das Modul. Ungültige Ablage bleibt ohne Änderung.
- Direkter Rechtsklick-Eintrag zum Aktivieren der drei Weichen-GFM-Grenzen in Welt und zugeordnetem Gleisbild. Die Weltauswahl verwendet die nächste Weiche innerhalb von 30 m und lehnt Gleichstände ab. GFM-Änderungen respektieren aktive Fahrstraßen und Richtungsbindungen.
- Suche für ein vorhandenes örtliches Gleisbild: Start über zugeordnete Signale oder ein gewähltes Bahnhofssignal, einstellbare Suchweite 100–10000 m entlang der realen Gleise, Anzeige gefundener Kennungen und aktiver Grenzen. Kein Ersetzen der Zeichnung. Eindeutige passende Feldnamen können zugeordnet werden; anonyme Symbole und verbindende Gleisabschnitte benötigen weitere Einrichtung.
- Gebundene Signalfelder aktualisieren allgemeine Platzhalterbeschriftungen, eigene Haupt-/Rangiertastenpaare und angebaute GFM-Referenzen bei geänderter Weltkonfiguration. Feste IDs, Richtung, individuelle Beschriftungen, eigene zweite Signale und Hilfsziele bleiben erhalten.
- GFM-Kästen erhalten matte, nicht emittierende Materialien mit bevorzugtem Lit-Shader. C-Pfeil hat nur eine nach oben zeigende Dreiecksfläche, damit sich Beleuchtungsnormalen nicht gegenseitig aufheben. Kein Betriebszustand wird durch den Pfeil angezeigt.

`Test.ps1`: **1030 Prüfungen bestanden, 0 fehlgeschlagen**. Prüft zusätzlich Modulanwahl/-verschiebung mit transaktionaler Ablehnung, getrennte UI-Bereiche bei mehreren Fenstergrößen, direkte Weltmenüaktion, Feldregistrierung und Umrüstung sowie Shaderauswahl und Pfeilgeometrie. Protokoll: `build/checks/0.7.26-tests.log`.

[Deutsche Spieltests](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.26.de.md) · [English gameplay checks](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.26.en.md). Reale Mausbedienung, Nachtansicht und Mehrspieler stehen aus. Die KI-Halteprobleme sind damit weiterhin nicht im Spiel als behoben bestätigt.
