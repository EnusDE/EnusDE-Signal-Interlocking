# 0.7.59-P0 – Gespeicherte Blockansichten weiterbearbeiten

Für Railroader 0.6.3. Alle Teilnehmer benötigen dieselbe Modversion.

Beim Öffnen des Ansichtseditors wird aus dem aktuellen gespeicherten Stand direkt
eine bearbeitbare Vorschau erstellt. Ein zusätzlicher Vorbereitungsklick entfällt.
Änderungen an Darstellungsstufe, Abschnittsbereich, Spiegelung und 180°-Drehung
aktualisieren den Arbeitsstand, statt die Vorschau zu verwerfen. Die vorgemerkte
Position bleibt bei freiem Ziel erhalten; beim Wechsel von einer kompakten zu einer
breiteren Ansicht kann die Tafel dafür wachsen. Kollisions- und Größenprüfungen bleiben.

Übernehmen speichert die Ansicht und lässt das Fenster mit einem frischen
Arbeitsstand der gerade gespeicherten Ansicht offen. Danach können weitere
Verschiebungen, Änderungen und Speichervorgänge direkt folgen. Schließen und erneutes
Öffnen lädt ebenfalls einen frischen Entwurf mit gespeicherter Position und
Darstellung. Jeder native Fensteraufbau hat einen eigenen Rückruf-Token. Alte
Rückrufe von vor dem Speichern, einer Einstellungsänderung oder einem Neuaufbau
können den aktuellen Entwurf nicht verändern.

Die optionale Taste heißt jetzt **Vorschau aktualisieren**. Sie prüft den aktuellen
Arbeitsstand erneut und behält dessen Position. Bei einem Fehler bleibt der konkrete
Hinweis auch beim Übernehmen erhalten. Schließen ohne Übernehmen verwirft nur die
Änderungen seit dem letzten erfolgreichen Speichern. Der Ablauf für einen noch
nicht bestätigten Bauvorschlag kehrt weiterhin zur Platzierung zurück und benötigt
den vorhandenen Bauabschluss. Weltobjekte werden dabei nicht vorzeitig gespeichert.

## Prüfung

Der Test mit dem produktiven Fenstercode verwendet jetzt vier Abschnitte und die
gemeldete 180°-gedrehte Ansicht von Abschnitt 4. Er prüft erneutes Verschieben nach
Speichern im selben Fenster, Schließen/Wiederöffnen, Wechsel zu reinen Nummernfeldern
sowie drei vollständige Speichern-/Serialisieren-/Laden-/Wiederöffnen-Zyklen mit allen
Darstellungsstufen. Getrennte gespeicherte und bearbeitete Bibliothekskopien bilden
den Spielspeicher ab. Geprüft werden außerdem Positionserhalt bei Verbreiterung,
Ablehnung alter Rückrufe nach Speichern und nativem Neuaufbau, sichtbare Fehler und
laufende Zugnummernweitergabe. Sämtliche Projekt-, Sprach- und Regressionstests
werden zusätzlich ausgeführt.

Das sind Rückruf- und Datenprüfungen ohne laufende Spieloberfläche. Maus-/Layouttest
im Spiel und Mehrspielerabnahme stehen aus. Lokal wird mit verfügbaren älteren
Spielbibliotheken und dem bestehenden 0.6.3-Pfadadapter gebaut, nicht mit
Original-0.6.3-DLLs.
