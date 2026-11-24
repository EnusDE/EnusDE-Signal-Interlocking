# Signale bedienen und automatisch anfragen

[English](WAYPOINT-0.7.66.en.md)

Testfassung 0.7.66 für Railroader 0.6.3. Per Modmanager installieren und Railroader vollständig neu starten. Host und Mitspieler benötigen dieselbe Hauptmod-Version. Runenwacht 0.2.2 kann weiter verwendet werden.

## Selbst am Signal anfragen

1. Das Signal in der Spielwelt links anklicken. Bei einem Signal für Zug- und Rangierfahrten die passende Fahrart wählen.
2. Ein Ziel aus dem zuständigen örtlichen Stellwerk auswählen und die Fahrstraße anfragen. Das Stellwerk meldet das Ergebnis nach der Fahrweg- und Verschlussprüfung.
3. Rechtsklick öffnet die Objektaktionen. Berechtigte Spieler können dort zur Bearbeitung im Baumodus wechseln.

Diese Testfassung nimmt manuelle Anfragen am Signal beim Host an. Andere Spieler können den Signalzustand ansehen und den Fahrdienstleiter um eine Fahrstraße bitten.

## Mit Auto Engineer fahren

1. Im Auswahlfenster neben der Uhr das Zahnrad öffnen. Unter **Betrieb und Automatik** auf **Automatische Wegpunktanfragen einschalten** klicken. Damit werden für den Host die Signalanbindung und die Blockabstimmung aktiviert.
2. Im Auto Engineer einen festen Wegpunkt hinter dem nächsten Signal setzen. Das Stellwerk wertet Fahrtrichtung und vorhandenen Wegpunktfahrweg aus und fordert den nächsten passenden Abschnitt an. Der Auto Engineer fährt seinen normalen Auftrag bis zum Wegpunkt.
3. **Lokinfo → Signaltechnik** zeigt die gewählte Fahrart und den Wartegrund. Dort lassen sich Zugfahrt oder Rangierfahrt ausdrücklich wählen, automatische Anfragen pausieren oder eine einzelne Fahrstraße anfragen. **HALT** im Fahrdienstleiter hält die Automatik an. Die Freigabe zur Wiederaufnahme liegt beim Host.

Für den ersten Test zwei betriebsbereite Signale desselben Ortsstellwerks und einen vollständig überwachten Fahrweg wählen. Die Automatik wartet bei unlesbarem Fahrweg, fehlender Gleisfreimeldung oder fremder Zuständigkeit. Sichere Auflösung bestehender Fahrstraßen folgt der normalen Stellwerkslogik. Kuppelaufträge, mehrstufige Richtungswechsel und Fahrten über Stellwerksgrenzen erfordern weitere Arbeit.

## Gleisfreimeldung prüfen

Im **Editor → Menü → Einrichten und prüfen** kannst du vorhandene Meldegrenzen erkennen und zuordnen oder einen Fahrweg auf Lücken prüfen. Wähle Start, Ziel und Fahrart, dann **Freimeldelücken suchen**. Zugeordnete Lücken werden orange markiert. Zu jeder Lücke erscheinen Gleis und Meterbereich. Bestehende Tafeln, Signale und Weichenzuordnungen bleiben bei der Prüfung erhalten.

Teste auch einen Abschnitt mit einem Signal ohne eigenen Melder zwischen aktiven Grenzen sowie die Besetztanzeige am Übergang von Signal zu Weiche. Bei einem Halt ohne Freigabe steht der Wartegrund in der Lokinfo und bei Zustandswechseln im Spielprotokoll. Die Spiel- und Mehrspielerabnahme dieser Fassung steht aus.
