# 0.7.41-P0 — Spielabnahme

Build, Lokalisierung und Regressionen werden mit Test.ps1 geprüft. Tatsächliche Spiel- und Mehrspielerabnahme stehen aus.

1. Modul auf gedrehtem, überwachtem Gleis platzieren. Anschlüsse und GFM-Farbe vor/nach Platzierung und bestätigter Einzel-Symbollöschung vergleichen. Linkspfeil oben, Rechtspfeil unten sowie Drehung und Zoom prüfen.
2. Zwei Nachbarn und ein Gleis mit beiden Richtungen einrichten. Vor drei getrennten vollständigen Freimeldungen keine Ausfahrt; danach konfigurierte Regelerlaubnis.
3. Gegenseite fragt an. Inhaber bleibt erhalten; Gegenfahrt scheitert. Inhaber kann vorbehaltlich Fahrstraßen-/Blockprüfung weiter ausfahren.
4. Abgabe von falscher Seite, mit alten Bedienelementen, Zug auf Strecke, aktiver Fahrstraße, Restverschluss oder unbekannter GFM muss ohne Erlaubnisänderung scheitern. Erst nach vollständiger Auflösung und Freiprüfung einmal abgeben.
5. Eigene Anfrage zurücknehmen, optionale Abgabe ohne Anfrage, automatische Bestätigung sowie Pause/Wiederübernahme und zentrale Übernahme prüfen. Fahrstraßen dürfen keine Anfrage erfinden.
6. Parallele Gleise unabhängig und mit entgegengesetzter Regelerlaubnis einrichten. Ein Gleis darf das andere nicht umschalten.
7. Erlaubnis-Hilfstaste ohne Weltposition einrichten: Bedienung ohne Startsignal, kein Fahrstraßen-/Automatik-/Ankunftsziel. Normale Hilfsziele behalten ihre Anfahrt und Funktion.
8. Örtliche Bedienung, Host-ESTW-Seitenauswahl, Client-Anzeigen, alte Befehle, Löschen/Verknüpfungen und Laden prüfen. Keine alten Fahrtbegriffe oder Anfragen wiederherstellen.
9. Belegter Block im angeforderten Fahrweg muss trotz Erlaubnis ablehnen. Folgezüge separat prüfen; die aggregierte Blockprüfung bleibt unverändert.
10. Keine neuen Weltobjekte, Gleisänderungen oder Materialabbuchungen durch Einrichtung und Erlaubnisbedienung. Vorher/nachher in vorhandener Testanlage vergleichen.

[Einrichtung](FREE-LINE-GWB.de.md). Das vorbereitete 0.7.40-ZIP bleibt unverändert.
