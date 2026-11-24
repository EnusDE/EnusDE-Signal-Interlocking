# Wegpunkt-Automatik in 0.7.65

[English](WAYPOINT-0.7.65.en.md)

Testfassung für Railroader **0.6.3**. Nach der Installation per Modmanager das Spiel vollständig neu starten. Host und Mitspieler brauchen dieselbe Hauptmod-Version. Wer Runenwacht verwendet, benötigt das Paket **0.2.1 für 0.7.65**.

## Fahrt starten

1. Im Zahnrad-Menü die **AE-Signalanbindung** einschalten. Unter **Automatisierung und KI** die **Blockrichtungen automatisch abstimmen** und die Automatik-Einstellungen speichern. Für den ersten Versuch ein Ortsstellwerk mit zwei zugeordneten Signalen und vollständig eingerichteter Gleisfreimeldung wählen.
2. Im **Auto Engineer** einen festen Wegpunkt hinter dem nächsten Signal setzen. Für den ersten Versuch sollte das Ziel vor oder am folgenden Signal am selben Ortsstellwerk liegen. Kommt vor dem Wegpunkt kein zuständiges Signal, ist dort keine Anfrage nötig.
3. An der Lok **Signaltechnik → Signaltechnik-Fahrinformationen** öffnen. **Zugfahrt** oder **Rangierfahrt** für den aktuellen Verband übernehmen und **Wegpunkt-Automatik einschalten** drücken. Die Einstellung gilt für diese Spielsitzung. Nach Kuppeln oder Trennen die Betriebsart für den neuen Verband erneut übernehmen.

Die Lokinfo zeigt Vorbereitung, Freigabe und Wartegrund. Die Vorbereitung kann innerhalb von 300 Gleismetern beginnen. Für die eigentliche Festlegung muss der anfragende Zug innerhalb von 25 Gleismetern am Signal eindeutig erkannt werden. Die Automatik arbeitet abschnittsweise entlang des gerichteten Wegpunkts. Bei freiem Fahrweg stellt das Stellwerk die Weichen und prüft Verschlüsse. Der Auto Engineer fährt seinen normalen Auftrag und hält an seinem Wegpunkt.

## Selbst übernehmen

In der Lokinfo auf **Wegpunkt-Automatik pausieren** drücken. Das beendet neue automatische Anfragen und nimmt noch nicht festgelegte eigene Anfragen zurück. Eine bereits freigegebene oder befahrene Straße bleibt unter der normalen Stellwerksüberwachung. Die Fahrsteuerung übernimmst du mit der normalen Bedienung des Auto Engineer.

Während die Mod-Automatik pausiert, kann der Host mit **Fahrstraße zum Wegpunkt anfragen** einmalig die nächste Straße anfordern. Die Anfrage wartet auf einen eindeutig lesbaren Fahrweg zum gesetzten Wegpunkt. Bei einer pausierten nativen Fahrt ist die Einzelanfrage möglich, solange der Auto Engineer den Wegpunkt und seinen passenden Fahrweg noch bereithält. Nach der Freigabe kannst du selbst fahren.

Der Fahrdienstleiter sieht unter **Automatikstatus** Lok, Fahrart, Start, Ziel und Wartegrund. Der Host kann wartende oder noch nicht befahrene Anfragen zurücknehmen. Eine zentrale Stellwerksübernahme blockiert weitere Anfragen in diesem Bereich. **HALT** pausiert alle Lok-Automatiken dieser Sitzung und nutzt die normale Verschlusslogik.

## Erster Spieltest

- **Zugfahrt:** Ein fester Wegpunkt hinter einem eigenen Startsignal, beide Signale demselben Ortsstellwerk zugeordnet. Erst freien, dann belegten Fahrweg prüfen. Wenn der Abschnitt frei wird, soll die wartende Anfrage weiterlaufen.
- **Rangierfahrt:** Fester Wegpunkt im selben Bahnhof, passender Rangierbegriff und freies Ziel. Für einen belegten Rangierweg weiterhin den Fahrdienstleiter verwenden.
- **Übernahme:** Während der Vorbereitung pausieren und prüfen, dass keine erneute automatische Anfrage entsteht. Bei einer bereits gestellten Straße pausieren, den Auto Engineer von Hand übernehmen und prüfen, dass der Fahrweg bis zur normalen Auflösung überwacht wird.
- **Wegpunktgrenze:** Wegpunkt vor oder am nächsten Signal sowie einen eindeutigen Wegpunkt auf dem Nachbargleis ausprobieren. Die Anfrage muss zum tatsächlichen Fahrweg passen. Bei zunächst falsch stehender elektrischer Weiche besonders prüfen, ob der native Auto Engineer den Weg lesen kann und das Stellwerk die Weiche richtig stellt.

Die Testfassung deckt örtliche Zug- und Rangierfahrten mit festen Wegpunkten ab. Kuppel-, Umkehr- und mehrstufige Aufträge sowie selbsttätige Fahrten zwischen Stellwerken sind spätere Schritte. Die erste Kommunikation läuft über Lokinfo und Automatikstatus beim Host. Mitspieler können den übertragenen Anfragestatus lesen. Fehlende Fahrwegdaten, mehrdeutige Signale und unvollständige Gleisfreimeldung führen zum Warten. Die Bedienung mit echten Fahrzeugen, die Reaktion des Auto Engineer und Multiplayer sind noch im Spiel zu prüfen.
