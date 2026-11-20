# 0.7.53-P0 – GWB als schrittweises Einrichtungstutorial

Für Railroader 0.6.3. Alle Teilnehmer benötigen dieselbe Modversion.

Der Assistent konzentriert sich auf die komplexe Strecken-/GWB-Einrichtung. Die
bereits vorhandene Zuordnung einzelner Signale und Weichen per Rechtsklick im
Gleisbild bleibt dafür der direkte Weg; sie muss nicht nochmals durchlaufen werden.

## Einstieg und Voraussetzungen

Örtliches Stellwerk öffnen → Menü → Einrichtungsassistent →
**Strecken-/GWB-Verbindung einrichten oder ändern**.

Die benötigten Hauptsignalrichtungen müssen gebaut und ihren örtlichen Tafeln bereits
zugeordnet sein. Ein-/Ausfahrrollen, Zieltasten und fertige GFM werden für die erste
Gleiswegsuche noch nicht vorausgesetzt. Fehlt ein echtes Ausfahrhauptsignal, muss
diese nach außen gerichtete Signalrichtung zunächst gebaut/zugeordnet werden; eine
Zieltaste ersetzt kein Ausfahrhauptsignal. Ein-/Ausfahrten können unterschiedliche
aktive Signalrichtungen eines gemeinsamen Masts sein.

## Die neun Schritte

1. **Nachbarn und Gleis.** Zwei bestehende örtliche Stellwerke nach ihren echten Namen
   wählen und das physische Streckengleis benennen. Vorhandene Verbindungen werden
   erkannt und wiederverwendet. Das Tutorial behandelt eingleisige Strecken und GWB;
   vorhandene reine Richtungsgleise bleiben über die manuellen Einstellungen bearbeitbar.
2. **Einfahrt des ersten Stellwerks.** Signal wählen, das die Einfahrt vom Nachbarn
   in diesen Bahnhof schützt. Seine Lichter zeigen zum ankommenden Zug vor dem Bahnhof.
3. **Ausfahrt des ersten Stellwerks zum Nachbarn.** Eine nach außen gerichtete
   Hauptsignalrichtung mit Zugfahrtbegriff wählen. Dieselbe Signalrichtung kann nicht
   zugleich die gerade gewählte Einfahrt und diese Ausfahrt sein.
4. **Einfahrt des zweiten Stellwerks.** Das Signal wählen, das die Ankunft vom ersten
   Bahnhof schützt.
5. **Ausfahrt des zweiten Stellwerks zurück zum ersten.** Danach werden beide
   Richtungen und ihr gemeinsames physisches Streckengleis geprüft.
6. **Gleisweg und Freimeldung.** Den geometrischen Vorschlag prüfen; bei Alternativen
   den passenden Weg wählen. Fehlende Weichen-GFM im gewählten Fahrweg lässt sich hier
   einschalten und die Abschnittsvorschläge übernehmen. Die Gleiswegsuche berechnet
   noch keine betriebliche Fahrstraße und stellt kein Signal auf Fahrt. Fehlende GFM
   bleibt als offen sichtbar; sie kann anschließend auch durch den Blockbau ergänzt
   werden. Vor dem Abschluss muss die gesamte benötigte Abdeckung vorhanden sein.
7. **Zieltasten und Erlaubnisbedienung.** Der Assistent schlägt fehlende Felder für
   beide Nachbarn vor. Zieltasten erhalten den örtlichen Einfahrsignalbezug in
   Gegenrichtung und werden mit der gemeinsamen Streckenerlaubnis verknüpft. Passende
   bestehende Felder werden wiederverwendet. Positionen der Erlaubnisfelder lassen
   sich im Gleisbild vorgeben. Die Vorschau selbst speichert noch nichts.
8. **Optionale Zwischenblöcke.** Blockanzahl, Abstände, Automatik-Kontakte und
   Gleisbildpositionen mit den vorhandenen Bauwerkzeugen vorbereiten. Der ausdrücklich
   bestätigte Blockbau ist ein eigener Speichervorgang und übernimmt auch die
   zugehörigen Ziel-/Erlaubnisverknüpfungen. Im Tutorial bleibt die Streckensicherung
   dabei bis zum Abschluss inaktiv. Materialpflichtige Baupläne müssen fertig werden.
9. **Abschlussprüfung.** Erst bei passendem Gleisweg, vollständiger GFM und gültigen
   Verknüpfungen die Einrichtung abschließen und die Streckensicherung aktivieren.
   Danach startet die Erlaubnis ohne Freigabe und wartet auf die normalen Freiprüfungen.

Bei jedem Signalschritt wird nur **eine** Auswahl gezeigt. Das bereits zugeordnete
Signal kann direkt im Hauptgleisbild oder über die Gleisbild-/Weltauswahl gewählt
werden. Die gerade benötigte Tafel wird beim Wechsel zum anderen Nachbarn geöffnet;
die aktuelle Signalwahl wird grün markiert. **Prüfen und weiter** wird erst nach einer
passenden einzelnen Signalwahl angeboten. Weitere Ausfahrten für dasselbe Gleis
können nach der Grundeinrichtung in den manuellen Einstellungen ergänzt werden.

## Hinweise innerhalb des Tutorials

Eine fehlende oder unpassende Auswahl wird nicht als erledigt übersprungen. Der
Assistent bleibt im betreffenden Schritt und nennt die nächste Handlung. Die
Rückkehr aus der Weltauswahl stellt dieselbe Tutorialseite wieder her.

Die bisherige Sammelauswahl aller vier Signalgruppen entfällt im Tutorial. Die
geometrische Streckenerkennung benötigt keine bereits berechnete Betriebsfahrstraße
mit fertigem Flankenschutz. Eine falsche Signalrichtung, ein unterbrochenes Gleis
oder noch fehlende Freimeldung wird dadurch nicht automatisch als gültig behandelt.
Bestehende Fahrstraßen, Belegung und Richtungsbindungen bleiben bei Änderungen
weiterhin geschützt. Gegen die GWB-Erlaubnis darf keine Zugausfahrt freigegeben werden.

Dieses Tutorial richtet die Streckenverbindung zu einer späteren Überhol-/Kreuzungs-
betriebsstelle ein. Vollständige automatische Überhol-/Kreuzungsdisposition, Bot und
Kommunikation bleiben gesonderte Funktionen.

## Prüfstand

Die lokalen Projekt-, Sprach- und Regressionstests decken unter anderem die feste
Signalreihenfolge, gewöhnliche noch nicht umgewidmete Hauptsignale, Gleiswegsuche ohne
GFM/Zieltasten, unveränderte Betriebsdaten bei Vorschauen, erzeugte Bedienverknüpfungen,
Wiederverwendung bestehender Felder und weiterhin gesperrte Gegenrichtung ab.

Der tatsächliche Tutorialablauf in deinem Spielstand ist damit noch nicht nachgewiesen;
Bedienung und Mehrspielerbetrieb müssen im Spiel getestet werden. Die lokale
Kompilierung verwendet weiterhin die vorhandenen älteren Spielbibliotheken mit dem
bestehenden Pfadadapter, nicht nachweislich die Original-0.6.3-DLLs.
