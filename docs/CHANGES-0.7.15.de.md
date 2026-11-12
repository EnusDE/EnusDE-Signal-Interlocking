# 0.7.15 – Gleisfreimeldung im Gleisbild und am Signal

## Bauen und zuordnen

- Freie Kästen behalten ihre A/B-Bauform. Die Kennung lautet **GFM-A-001** bzw. **GFM-B-002**, mit einem gemeinsamen Nummernkreis je Spielstand. Vorhandene A/B-Kästen bekommen beim Laden eine Nummer. Ihre Objekt-IDs, Positionen und gespeicherten Abschnittsbindungen bleiben erhalten.
- Ein eigenes Lichtsignal kann unter **Gleisfreimeldung am Signal** einen Kasten der Variante A oder B erhalten. Ein Mast mit mehreren Signalbegriffen hat einen gemeinsamen physischen Gleispunkt. Der Kasten wird auf der Mastseite aufgestellt und lässt sich längs versetzen. Bei **Kein Kasten** bleibt seine Nummer für das spätere Wiederanbringen reserviert. Benutzte Abschnitte mit entfernter Grenze zeigen unbekannt und können neu zugeordnet werden.
- Unter **Grenzmarke im Gleisbild** lässt sich die Kennung an einem Gleisfeld sichtbar hinterlegen. Hat das Feld nur einen Anschluss, erscheint automatisch ein Stumpfgleissymbol. Die Suche für die dortige Grenzmarke listet frei platzierte GFM-Kästen. Im Abschnittseditor wird die Stumpfgleisseite mit einem Kasten belegt. Für das andere Ende stehen auch registrierte Signale und Weichen zur Verfügung. Die GFM-Kennungen und Markierungen erscheinen im Editor.
- **Gleisfreimeldung** verwendet eine Suche nach Kennung oder Namen. Bei zugeordneten Weichen sind **Stammgleis**, **gerader Ast** und **Abzweig** als Grenzen auswählbar. Der Endpunkt wird aus der äußeren Grenze des bereits vorhandenen Weichen-Belegungsbereichs auf dem jeweiligen Strang ermittelt. Die Weiche bleibt als eigener Bereich sichtbar.

## Ausleuchtung und Stellwerk

- Unter Zugnummern läuft die Besetztanzeige durch. Ein Signalfeld zeigt beide Gleishälften einzeln an. Sind links und rechts jeweils Gleisfelder mit Abschnittsbindung vorhanden, übernimmt das Signal die Farbe der jeweils angrenzenden Seite. Die Seite lässt sich unter **Ausleuchtung** auch ausdrücklich zuordnen, etwa wenn im Gleisbild direkt eine Weiche anschließt.
- Ein Hauptsignal mit aktivierter Rangierfunktion erhält beim Zuordnen automatisch zwei Tasten. Separat verknüpfte Signale behalten getrennte Tasten. Liegen sie auf demselben realen Gleissegment, kann der dazwischenliegende Abschnitt am Signalfeld unter **Zwischen beiden Signalen** gespeichert werden. Zwei Funktionen am selben physischen Punkt teilen sich eine Grenze.
- An der Weiche wird ein belegter Strang anhand der Spielbelegung rot gezeichnet. Ein Wechsel der Weichenlage verschiebt die gemessene Belegung nicht. Das Fdl-Gleisbild zeigt Signale, Namen und Betriebsanzeigen ohne die zusätzlichen Editor-Kennungen.
- Lange Schaltflächen der Bereichsauswahl stehen untereinander.

## Modelle und Bestandssignale

- Eigene Lichtsignale und GFM-Kästen suchen eine nahe Geländeoberfläche. Wo das Gelände auf der vorgesehenen Höhe erfasst wird, sitzt das Modell auf dem Boden. Die Höhenkorrektur bleibt einstellbar. Der Hauptmast ist 6,75 m hoch und hat einen Betonsockel. Das freistehende Rangiersignal behält seine Modellhöhe.
- Ein bodennahes Kabel führt vom Kasten zum Schienenanschluss auf derselben Gleisseite. Kasten und Sockel orientieren sich optisch an Streckenanschlusskästen und Signalgründungen.
- Unter **Bestandssignale ungültig kennzeichnen** gibt es **Kreuz setzen · Lampen aus** und eine Variante mit KI-Ausnahme. Die Abschaltung wirkt auf die optische Emission unterstützter Grundspiel-Signalköpfe. Wieder gültig schalten gibt die Lampenanzeige frei. Der ursprüngliche Signalschirm bleibt vorhanden. Der Menüstatus meldet, wenn noch kein unterstützter Kopf geladen ist.

## Installation und Spielprüfung

Die Konfiguration, Nummern, Abschnittsbindungen und Ungültigkeitsoptionen werden über die bestehende gemeinsame Anlage vom Host übertragen. 0.7.15 bei allen Beteiligten über den Modmanager installieren und Railroader vollständig neu starten.

Im Spiel sind besonders Hanglage und Schotter, beide Weichenäste mit einem Zug über der Grenze, der Lampenwechsel einschließlich Rücknahme sowie der Beitritt eines Mitspielers zu prüfen. Der Austausch eines Bestandssignals gegen ein eigenes Signal bleibt ein separater Entwicklungsschritt.
