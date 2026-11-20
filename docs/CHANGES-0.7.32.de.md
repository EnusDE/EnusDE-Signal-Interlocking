# 0.7.32-P0 – Blockeinrichtung und Betriebsrückmeldungen

- Fehler im Platzierungswerkzeug behoben: Der angebotene Strecken-/Erlaubnismelder wurde zuvor abgewiesen. Platzierung auf freiem Feld öffnet die Einrichtung, bestehende Melder sind direkt bearbeitbar.
- Örtliche Einrichtung Whittier–Ela mit gemeinsamem Verbindungsdatensatz, Endsignalen, passenden Hin-/Rückwegen und ausdrücklich zu übernehmenden streckenbezogenen GFM-Vorschlägen. Kein automatischer Neubau von Kästen und keine Zugfreigabe durch Erkennung.
- Neue Hl-Bauten und Umrüstung nur mit **Hl-Signalsystem aktivieren (Test)**; vorhandene Hl-Signale bleiben beim Ausschalten betriebsfähig.
- Fahrwegsuche lässt gerichtete Sackgassen zum Ziel aus, statt das Suchbudget auf unbeteiligten Gleisen aufzubrauchen. Zwischenhauptsignale, Sicherungsprüfung und Suchgrenzen bleiben wirksam.
- Auflösungsanzeige unterscheidet blockierte normale Auflösung ohne Countdown von Hilfsauflösung mit verbleibender Schutzwartezeit. Host-Restzeit und Blockierungsgrund werden auch an Clients übertragen; Zeitablauf gilt nicht als Freiprüfung.
- Kurze temporäre Sicherungsnamen verhindern die im gemeldeten Windows-Pfad erreichte 260-Zeichen-Grenze. Sicherungen bleiben im externen Ordner und kompatibel. Die Sicherung vor Betriebswechsel bleibt erforderlich.
- Achstest-Display mit beidseitiger Schrift außerhalb der Platte statt einer in die Platte gedrehten Textseite. Hinweis auf getrennte Zähl-/Displayoptionen und erforderliches Übernehmen.
- Kontaktmenü vor dem Bauen mit Blickrichtung und eindeutiger erster Hauptsignalerkennung; Welt-Auswahl für Kontakt-, Ziel- und Gleisbildsignale. Kontaktstatus in den Signaloptionen.
- Gezielt reduziertes Weichen-Zuordnungsmenü und unmittelbare Übernahme in das vorher gewählte Feld. Elektrische Antriebe nur bei eindeutiger örtlicher Zuordnung; keine weltweite Sammelumrüstung entdeckter Weichen.
- GFM-Bauvorgabe in den Einstellungen und direkte Erkennung für bestehende Gleisbilder, ohne Neuzeichnen. Unvollständige Zuordnungen bleiben sichtbar und werden nicht als vollständige Überwachung ausgegeben.

Keine Veröffentlichung oder Installation. Einrichtung und offene Spieltests: [deutsche Testanleitung](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.32.de.md). Tatsächliche Testwelt, native KI und Mehrspielerbetrieb müssen noch geprüft werden.
