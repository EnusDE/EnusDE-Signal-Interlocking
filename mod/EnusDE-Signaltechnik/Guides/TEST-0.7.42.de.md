# 0.7.42-P0 — Bedienung und Spielabnahme

Die Warteschlange ist automatisch aktiv. Für manuelle Anforderungen sind keine neuen Weltobjekte, Kontakte oder Einstellungen nötig. Host und Clients benötigen dieselbe Version. Bestehende Automatik-Kontakte verwenden dieselbe Vorbereitung.

1. Start und Ziel über mehrere Hauptsignale wählen. Der Auftrag wird eingereiht; das abgeschlossene Tastenpaar wird geleert, sodass sofort das nächste gewählt werden kann. Im Bedienkontext stehen Ausstehende Anforderungen, Fortschritt und Ausstehende Vorbereitungen abbrechen.
2. Mehrere unterschiedliche Fahrwege schnell anfordern. Gleiche Anforderungen nicht doppelt vorbereiten. Nacheinander abarbeiten; Kamera, Oberfläche und laufende Überwachung sollen bedienbar bleiben. Berechneten und gespeicherten Modus sowie gespeicherte Folgen prüfen.
3. Bei mehreren Wegen auswählen oder die Auswahl verwerfen. Eine abgeschlossene ältere Vorbereitung darf eine inzwischen neue Startwahl nicht löschen. Widersprechende Anforderungen dürfen weiterhin an der normalen Prüfung scheitern.
4. Während der Vorbereitung einen späteren Teilabschnitt belegen. Abschließende Prüfung muss die gesamte neue Folge ablehnen/zurückrollen; bestehende Verschlüsse bleiben erhalten. Mit unbekannter GFM, Weichenbelegung, Gegenfahrstraße und Erlaubnis beim Nachbarn wiederholen.
5. Vor/während Momentaufnahme, Suche und Schutzberechnung abbrechen; auch HALT prüfen. Kein späteres Stellen aus alter Vorbereitung. Bestehende Fahrstraßen und Restverschlüsse bleiben normal überwacht.
6. Signalposition/Einstellungen oder natives Gleisnetz während der Vorbereitung ändern. Kein veraltetes Ergebnis stellen. Stellwerk/Editor wechseln, Sicherung wiederherstellen und Spielstand neu laden: alte ausstehende Arbeit verwerfen. Sie wird nicht als Fahrtzustimmung gespeichert.
7. Mehrere bestehende Kontakte, Erst-/Ausweichziel und zentrale Automatikpause prüfen. Verband, Auftrag oder Richtung beim Warten ändern. Automatik muss den tatsächlichen Zug erneut prüfen und fehlende GFM/Erlaubnis weiterhin beachten.
8. Bei 32 wartenden Vorbereitungen weitere unterschiedliche Anforderung mit lesbarer Meldung ablehnen. Bestehende Grenzen von 16 aktiven Teilfahrstraßen und 32 Alternativen bleiben bestehen.
9. Framedauer auf derselben großen Testkarte vor/nach vergleichen, einschließlich langer Signalkette. Fahrweglänge, Signal-/Weichenzahl, Optionen, Queuephase und längsten Frame notieren. Das weiche 2-ms-Budget kann durch einzelne native Aufrufe und die gemeinsame abschließende Reservierung überschritten werden. Synthetische Tests sind keine Spiel-Framemessung.

Automatisch geprüft werden FIFO, Zeit-/Schrittbudget, doppelte Anforderungen und Begrenzung, Abbruch, Fehler, native Aufrufanbindung, Hintergrundsuche über elf Teilfahrstraßen, gemeinsame Ablehnung bei neu entstandener Belegung und unveränderter Flankenschutz bei 6000 zusätzlichen Knoten. Spiel- und Mehrspielerabnahme stehen aus.
