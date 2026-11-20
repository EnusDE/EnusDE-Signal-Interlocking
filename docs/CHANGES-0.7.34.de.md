# 0.7.34-P0 – Falsche GFM-Wegablehnung

- Zeichenweisen Vergleich mit einer erneuten globalen GFM-Suche durch direkte Prüfung des gespeicherten physischen Verlaufs ersetzt. Native Float-/Double-Rundung, zusammenhängende Teilstücke und lange gespeicherte Wege lösen keine pauschale Weichenfehlermeldung mehr aus.
- Echte Unterbrechungen, Weichenquerungen, unlesbare Gleise und übersprungene GFM-Grenzen bleiben gesperrt. Fehlermeldungen nennen Grenzpaar und betroffene Weiche bzw. Grenze.
- Änderungen an physischen GFM-Grenzen und C-Richtung invalidieren die zwischengespeicherte Blockprüfung. Bestehende Richtungsbindungen werden weiterhin nicht vorzeitig aufgehoben.
- Gemeinsame A/B-Kästen auch unter Signal-GFM-Kennung als Weichengrenzen für Richtungsketten berücksichtigen.

[Einrichtung und Spieltests](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.34.de.md). Der konkrete Testweltzustand hinter dem Screenshot ist nicht verfügbar; tatsächliche alte Abschnitte über Weichen werden nicht stillschweigend repariert. Keine Veröffentlichung, Installation oder neue KI-Rangierwende. Ältere Pakete bleiben unverändert.
