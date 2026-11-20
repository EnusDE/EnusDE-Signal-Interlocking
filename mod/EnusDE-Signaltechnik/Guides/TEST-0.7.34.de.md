# 0.7.34-P0 – GFM-Wegprüfung

Fehlerkorrektur-Teststand für Railroader 0.6.2. Host und Clients benötigen dieselbe Version. Bestehende Testwelt verwenden. Enthält die Änderungen aus [0.7.33](TEST-0.7.33.de.md).

## Anlass

Die bisherige Meldung **GFM-Weg nicht eindeutig / führt über eine Weiche** fasste verschiedene Fehler zusammen. Der Vergleich eines gespeicherten Weges mit einer neuen Suchantwort erfolgte anhand exakt serialisierter Meterwerte. Schon der Float-/Double-Wechsel einer nativen Spielposition konnte einen physisch passenden Abschnitt abweisen. Anders aufgeteilte, aber zusammenhängende Meterstücke und lange Wege konnten ebenfalls an diesem Vergleich bzw. den Vorschlagssuchgrenzen scheitern.

Jetzt wird der tatsächlich gespeicherte Verlauf direkt geprüft: verfügbare Segmente, Anschlussknoten, Meterbereiche, Fahrtrichtung, Endgrenzen und dazwischenliegende GFM-Grenzen. Positionsabweichungen unter 5 mm werden wie bereits bei der Endpunktprüfung toleriert. Es wird weder eine alternative Fahrstraße geraten noch ein fehlender Bereich ergänzt.

Eine echte Weichenquerung, unterbrochene Gleise, widersprüchliche Richtung und übersprungene GFM-Grenzen bleiben gesperrt. Die Meldung nennt nun das betroffene Grenzpaar und gegebenenfalls die Weichenkennung oder die übersprungene Grenze. Neue bzw. geänderte physische GFM-Grenzen invalidieren auch die zwischengespeicherte Blockprüfung. Bestehende Richtungsbindungen müssen weiterhin zuerst sicher freigefahren werden.

Bei gemeinsamen A/B-Kästen bleibt die Weichengrenzen-Funktion bei der Bildung von Richtungsketten erhalten, auch wenn der Kasten unter seiner Signal-GFM-Kennung geführt wird.

## In der Testwelt prüfen

1. Den zuvor mit **BL-494658157A** abgewiesenen Start-/Zielweg erneut anfordern. Bei physisch passendem GFM-Verlauf darf der reine Rundungsfehler keine Ablehnung mehr verursachen.
2. Bleibt eine Ablehnung, das jetzt genannte **GFM-Grenzpaar und die Weichen-/Grenzkennung** prüfen. Ein tatsächlich über eine Weiche führender alter GFM-Abschnitt ist damit nicht automatisch repariert. Weichen-GFM aktivieren bzw. prüfen und im vorhandenen Gleisbild vollständige Abschnitte erneut erkennen und übernehmen.
3. Getrennte Abschnitte vor und hinter der Weiche sowie gemeinsamen A/B-Kasten testen. Eine vollständige Überwachung muss weiterhin vorliegen; gold/unbekannt darf keine Freigabe erhalten.
4. Lange Abschnitte und Gegenrichtung testen. Besetzte oder unbekannte Bereiche müssen die Richtungsfreigabe weiterhin verhindern.
5. Bei freier Anlage eine C-Grenze innerhalb eines vorhandenen Abschnitts ergänzen oder drehen. Die neue Grenze muss bei der nächsten Prüfung berücksichtigt werden; kein alter Freimeldenachweis über sie hinweg.

Die Korrektur ändert weder die KI-Rangierwende noch die Verschluss-/Auflösungsregeln. Spielwelt, native KI und Mehrspieler sind weiterhin praktisch abzunehmen. Bei verbleibendem Fehler die vollständige neue Meldung mit Player.log und Versionsnummer angeben.
