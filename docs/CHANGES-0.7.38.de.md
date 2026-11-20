# 0.7.38-P0 – Belegungsursachen und lokale Diagnosemomentaufnahme

- Fahrweg-/Weichenablehnung mit genauer Herkunft, Gleis und Meterbereich; Belegung von unbekannter Rückmeldung unterscheiden.
- Bei abweisender Flankenschutzweiche den tatsächlichen Gefahrenkorridor reservieren, nicht zusätzlich andere Gleisäste. Vor notwendigem Umlauf weiterhin alle Schutzbereiche freiprüfen. Alte gespeicherte Flankenberechnung ausdrücklich neu berechnen lassen.
- Zugeordnete Hilfsziele unabhängig vom zuletzt gewählten Feld auflösen und im Bedienkontext anbieten. Bestehende Signal-/Fahrartprüfungen erhalten.
- Nummernweitergabe erst nach erfolgreichem Commit erledigen, blockierte Ziele erneut prüfen, fremde nachfolgende Nummer nicht übernehmen. Alte Präfix-Duplikate beim Transfer entfernen.
- Eindeutige normale Anzeigequelle für aus Fahrzeugen gelesene Nummern über mehrere Abschnitte; echte Anzeigekopien erhalten. Doppelte Quellen-/Nummernzuordnung als unbekannt darstellen und gezielte Korrektur anbieten.
- GFM-Prüfung: örtlich nachvollziehbare Reihenfolge sowie Ursprungssignal/-objekt, Gleis/Meter, stabile Kennung und Zeichnungsorte jeder Grenze anzeigen. Reine Endpunktmarkierung bei null Feldzuordnungen erklären. Alte Signalgrenzen ohne aktiven GFM nicht als gültige Detektoren bestätigen.
- Manueller Diagnose-ZIP-Export mit Konfiguration, Grenzherkunft, Netz, Betrieb, Fahrzeugen, Nummern und tatsächlichen Abfragen des letzten Stellversuchs. Optionaler lokaler Export-/Synchronisationsordner, keine automatische Übertragung. Getrennte Archive mit Größenbegrenzung.
- Kaltstart bestätigt: Signale Halt, keine Wiederherstellung früherer Fahrtfreigaben oder Laufzeitverschlüsse.

[Deutsche Bedienung und Spieltests](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.38.de.md), [English](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.38.en.md). Keine Veröffentlichung oder Installation. Lokale Spielwelt, GFM-A-051 und Mehrspielerabnahme bleiben offen. Die früher hochgeladene Diagnostics.zip wurde wegen des schreibgeschützten Dateiimports nicht ausgewertet; keine daraus abgeleiteten Befunde.
