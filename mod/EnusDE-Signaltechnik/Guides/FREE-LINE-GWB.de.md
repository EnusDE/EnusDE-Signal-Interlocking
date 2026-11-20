# GWB-Erlaubnismodul — 0.7.41-P0

Das ist Stellwerkstechnik im Hintergrund und ein Modul im Gleisbild. Es baut keine Weltobjekte, verändert keine Gleise, setzt keine Melder und berechnet keine Baumaterialien. Vorhandene Signale und GFM müssen eingerichtet sein. Railroader 0.6.2; Host und Clients benötigen dieselbe Mod-Version. Spiel- und Mehrspielerabnahme stehen aus.

## Strecke einrichten

1. Im Stellwerkseditor unter Felder setzen **Streckenerlaubnis** wählen. In eine leere Zelle oder auf ein einfaches gezeichnetes Gleisfeld setzen. Neue Module enthalten eine durchgehende Gleislinie; vorhandene Anschlüsse und GFM-Zuordnungen bleiben erhalten. Weichen und Signale werden nicht überschrieben.
2. Die Streckeneinrichtung öffnet sich. Nachbarstellwerke A und B, ein physisches Gleis und Betriebsart wählen: eingleisig, festes Richtungsgleis A → B oder GWB.
3. Ausfahrten und Ankunftsziele zuordnen. Mehrere Ausfahrten dürfen eine Strecke verwenden. Eine Hilfstaste im Zielmodus darf Ankunftsziel sein; eine reine Erlaubnistaste nicht.
4. Streckenverlauf erkennen, GFM-Abdeckung prüfen und übernehmen. Bestehende GFM und Weichengrenzen sind erforderlich; die Erlaubnis ersetzt sie nicht.
5. Bei beiden Fahrtrichtungen die Regelerlaubnis nach Freiprüfung beim Start festlegen. Neue Strecken sind standardmäßig manuell mit vorheriger Nachbaranfrage. Wahlweise Abgabe ohne Anfrage oder automatische Bestätigung ausdrücklicher Anfragen bei erfüllten Bedingungen einschalten.
6. Bei zwei parallelen GWB-Gleisen zwei getrennte Streckeneinträge anlegen, normalerweise mit entgegengesetzter Regelerlaubnis. Ein physisches Gleis erhält genau eine gemeinsame Erlaubnis.

## Hilfstaste verbinden

Rechtsklick auf Hilfszielfeld → Eigenschaften. Zwischen **Fahrstraßenziel** und **Streckenerlaubnis bedienen** wählen.

- Fahrstraßenziel behält reale Anfahrt und Fahrarten. Erst Startsignal, danach das Ziel wählen.
- Erlaubnisbedienung benötigt weder Welt-Gleisposition noch Startsignalwahl. Strecke zuordnen, speichern und im Fahrdienstleiter anklicken. Diese Funktion ist kein Zug-, Rangier-, Automatik- oder Streckenankunftsziel.

Am Erlaubnismodul per Rechtsklick → Eigenschaften dieselbe Strecke und optional die verknüpfte Hilfstaste auswählen. Die Verknüpfung ändert deren Funktion nicht stillschweigend. Das Modul öffnet ebenfalls die gemeinsame Erlaubnisbedienung; ein verbundenes Fahrstraßenziel lässt sich dort nach der Startwahl auswählen.

Benutzte Ziele erst aus gespeicherten/aktiven Fahrstraßen und Streckenankunftsgruppen entfernen. Funktionsänderungen sind bei Fahrstraßen und Richtungsbindungen gesperrt.

## Erlaubnis bedienen

Nach dem Laden muss die gesamte Strecke dreimal getrennt vollständig frei gemeldet sein. Dann entsteht die konfigurierte Regelerlaubnis. Live-Erlaubnis, Anfragen, Verschlüsse und Fahrtbegriffe werden nicht aus einer alten grünen Anzeige wiederhergestellt. Bei belegter Strecke wartet die Initialisierung.

Der Pfeil in Feldrichtung bedeutet A → B, der Gegenpfeil B → A. Das Feld passend zum Gleisbild drehen. Der hervorgehobene Pfeil zeigt den Inhaber, der blinkende Gegenpfeil eine Anfrage. Die Gleislinie zeigt unabhängig davon die GFM-Farbe. Betriebsstellennamen und Details in der Bedienung beachten.

1. Ohne Erlaubnis **Erlaubnis beim Nachbarn anfragen** wählen. Die Anfrage darf bei belegter Strecke warten; der Inhaber bleibt erhalten.
2. Beim Inhaber **Erlaubnis an Nachbarn abgeben** wählen. Nur der Inhaber darf abgeben. Voraussetzung: gesamte Strecke frei, keine aktiven Fahrstraßen oder Restverschlüsse, drei getrennte Freimeldungen und erneute aktuelle Freiprüfung beim Befehl.
3. Nur die anfragende Stelle kann ihre eigene Anfrage zurücknehmen. Eine optionale automatische Bestätigung beantwortet ausschließlich ausdrückliche Anfragen nach vollständiger Freiprüfung. Eine Fahrstraßenanforderung fragt die Erlaubnis nicht selbst an und übernimmt sie nicht.
4. Die Erlaubnis bleibt zwischen Zügen bestehen. Jede Ausfahrt braucht weiterhin Fahrstraßen-, Weichen-, Flanken- und Blockprüfung. Die vorhandene Blockkettenprüfung bleibt vollständig aktiv und kann auch einen Folgezug ablehnen; dieses Update lockert sie nicht.

Örtliche Stellwerke bedienen nur ihre eigene Seite. Im übergeordneten Host-Stellwerk muss ausdrücklich die vertretene Betriebsstelle gewählt werden. Das ist kein neues Benutzer-/Spielerkonten-Rollensystem. Clients erhalten Anzeigen; die Bedienung bleibt beim Host. Nach Anfrage oder Wechsel die Bedienung aktualisieren; alte Befehle werden abgelehnt.

## Bestehende Anlagen und Grenzen

Alte freistehende Melder bleiben zunächst freistehend. Über Eigenschaften **Modul im Gleis mit durchgehender Gleislinie** aktivieren, um sie umzustellen. Neue Module sind standardmäßig im Gleis. Bei normal bestätigter Einzel-Symbollöschung bleibt eine angeschlossene Gleislinie erhalten. Durch das Modul zeichnen wird abgelehnt; erst versetzen oder entfernen.

Stabile Strecken-, Signal- und Ziel-IDs bleiben erhalten. Alte Hilfsziele bleiben Ziele. Bereits konfigurierte automatische Strecken beantworten jetzt ausdrückliche Anfragen, statt bei einer Fahrstraßenanforderung selbst die Richtung zu wechseln. Die Regelerlaubnis ist ohne andere Einstellung A → B. Bestehende Anlagen vor Betrieb prüfen.

Die Trennung von Erlaubnis, Blockfreigabe und Fahrstraßensicherung orientiert sich an deutschen Betriebsgrundsätzen. Anfrage/Zustimmung ist eine einstellbare Spielregel und keine Behauptung, jede deutsche Blockbauform nachzubilden. Konkrete DB-Regelwerks- sowie Spielprüfung stehen aus. Dieses Update behebt nicht sämtliche früheren GFM-Geometrieprobleme und enthält keine automatische Neunummerierung.
