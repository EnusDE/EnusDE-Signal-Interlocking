# 0.7.28-P0 – gemeinsamer GFM-Kasten A/B

Für Railroader 0.6.2. Paket ersetzen und Spiel neu starten; bestehende Testwelt weiterverwenden. Host und Clients benötigen dieselbe Version. Enthält die Änderungen von [0.7.27](TEST-0.7.27.de.md).

## Gemeinsame Signal- und Weichengrenze

Liegt eine vollständig erkannte B-Weichengrenze **exakt am selben logischen Gleispunkt** wie ein aktivierter A-Kasten am Signal, wird ein gemeinsamer Kasten dargestellt. Er trägt **A/B** und die Kennung **GFM-A/B-…**. Die Darstellung verwendet das B-Gehäuse; die vorhandene Entitätsnummer des Signalkastens bleibt erhalten.

Der zusätzliche Weichenkasten an dieser Stelle wird ausgeblendet. Signal und Weichengrenze behalten ihre bisherigen internen IDs. Bereits eingerichtete passende Freimeldeabschnitte können bei der Erkennung ihre bisherige Kennung und ihren Namen behalten. Die Grenze wird einmal in die automatische Netzerfassung übernommen.

Mehrere Weichengrenzen an derselben eindeutigen Signalgrenze können denselben Kasten mitnutzen. Am Signal unter **Gleisfreimeldung am Signal** stehen die Kennzeichnung A/B und die mitbenutzenden Weichen. In der Weichen-GFM gibt es **Gemeinsamen A/B-Kasten öffnen**. Vorhandene Verweise auf eine solche Weichengrenze öffnen den zugehörigen Signalkasten.

Die A/B-Kennzeichnung erscheint auch bei der Grenzanzeige im Gleisbild. Sie ist keine neue Signalart und keine Fahrterlaubnis. Die technische Signalkennung, etwa RS-009 oder HS-009, wird nicht verändert.

## Grenzen

- Nähe allein reicht nicht: unterschiedliche Meterpositionen oder Gleissegmente werden nicht zusammengezogen. Verglichen wird in der Gleispräzision des Spiels.
- C bleibt wegen seiner einseitigen Wirkung separat. Mehrdeutige physische Grenzen werden nicht automatisch vereinigt.
- Wird die Weichen-GFM deaktiviert, ist der Signalkasten wieder A. Wird der Signalkasten entfernt oder seine logische Grenze verlegt, wird die Weichengrenze wieder getrennt dargestellt. Vorhandene individuelle Weichenkastenversätze bleiben gespeichert.
- Die gemeinsame Nutzung wird aus der Anlage berechnet; es werden keine zusätzlichen persistenten Signalobjekte angelegt. Die allgemeine Platzierungslogik für andere benachbarte Kästen wurde nicht überarbeitet.

## Spieltest

1. An der betroffenen Weiche GFM aktivieren und die Netzerfassung abwarten. Liegt die Weichengrenze auf dem A-Signalpunkt, muss dort ein A/B-Kasten statt zweier Kästen erscheinen.
2. Signal öffnen: A/B-Hinweis und mitbenutzende Weiche kontrollieren. Im Gleisbild die Grenzkennung prüfen.
3. Einen vorhandenen Abschnitt mit bisherigem Weichengrenzverweis prüfen. Er darf durch die gemeinsame Darstellung nicht verloren gehen oder als zusätzlicher Abschnitt dupliziert werden.
4. Weichen-GFM bei gelösten Fahrstraßen deaktivieren und erneut aktivieren. A ↔ A/B muss ohne neue Entitätsnummer wechseln.

Offline geprüft. Darstellung und Bedienung in der Testwelt sowie Mehrspieler müssen noch geprüft werden. Frühere offene KI-Haltetests bleiben bestehen.
