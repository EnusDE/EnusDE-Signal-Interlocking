# 0.7.33-P0 – Einrichtung und Spieltests

Teststand für Railroader 0.6.2. Host und Clients benötigen dieselbe Modversion. Bestehende Testwelt verwenden; kein neuer Spielstand erforderlich. Die folgenden Abläufe ersetzen die entsprechenden Bedienhinweise aus 0.7.32. Automatische Prüfungen ersetzen keine Spielabnahme.

## GFM in der Welt auswählen, einschließlich C

Im **Editor** ein durchgehendes Gleisfeld rechtsklicken → **GFM in der Welt auswählen …**. Den tatsächlichen aktiven GFM-Kasten in der Spielwelt einmal anklicken. Das ursprüngliche Gleisbild öffnet sich wieder und die Grenze ist dem gewählten Feld zugeordnet. Escape bricht ab. Es entsteht kein neuer Kasten.

Danach **Menü → GFM in diesem Gleisbild erkennen … → GFM-Abschnitte im vorhandenen Gleisbild suchen** verwenden. Vorschlag prüfen und **Erkannte GFM ohne Neuzeichnen übernehmen** wählen. Fehlende Gegenbegrenzungen oder mehrdeutige Verbindungen werden dadurch nicht erfunden. Die Zuordnung eines Kastens allein richtet noch keinen vollständigen Freimeldeabschnitt ein.

Eine freistehende **C-Grenze** benötigt kein Signal am selben Ort. Sie erscheint auch in der Fahrdienstleiteransicht mit Grenzmarke und GFM-C-Kennung. Ihre eingerichtete Richtung entscheidet, auf welcher Seite ein Abschnitt angeschlossen werden darf. Die andere Seite wird durch diesen C-Kasten nicht überwacht; dort gibt es keine zugesicherte Frei- oder Besetztmeldung. Eine eigenständig eingerichtete Überwachung auf anderen Bereichen bleibt bestehen. Die Gleiszeichnung hinter C wird nicht gelöscht. Eine alte Freimeldestrecke, die eine zusätzliche physische Grenze überspringt, wird unbekannt statt frei gelesen.

Prüfen: C-Kasten auf freiem Gleis auswählen, vorhandene Zeichnung behalten, korrekte C-Kennung im Fdl sehen, nur zulässige Seite verbinden, Richtung umkehren und erneut erkennen. Ungültige, geplante oder deaktivierte Kästen dürfen keine Zuordnung als aktive Grenze erhalten. Ein verändertes Zielfeld oder veralteter Vorschlag darf nicht übernommen werden.

## Ein Klick für Zuordnung und schnelle Aktionen

- Signalfeld rechtsklicken → **Signal in der Welt zuordnen …**, dann die gewünschte reale Signalseite einmal anklicken. Keine zweite Bestätigung und kein zusätzliches Signal-Bearbeitungsfenster.
- Weichenfeld rechtsklicken → **In der Welt zuordnen …**, dann die reale Weiche einmal anklicken. Dieser Klick darf die native Weiche nicht umstellen. Escape bricht ab.
- **Feld um 90° drehen** speichert unmittelbar. Vier Drehungen müssen wieder die ursprüngliche Geometrie ergeben; Signal- und GFM-Identität bleiben erhalten. Gezielte Feineinstellungen sind weiterhin separat möglich.
- Normaler Rechtsklick auf ein Weltsignal öffnet zunächst **Bearbeiten**. Für spätere Kommunikation besteht eine technische Schnittstelle, noch kein sichtbarer Kommunikationsbefehl.

Prüfen: Startklick auf die Zuordnungsschaltfläche darf nicht ins Weltobjekt durchfallen; nach Zuordnung darf derselbe Klick kein Editorfenster öffnen oder eine Weiche werfen. Drehung ohne geöffnetes Eigenschaftenfenster und mit offenem Feinentwurf testen.

## Durchgehende Freimeldung

Die Erkennung berücksichtigt beide unabhängig eingerichteten Seiten desselben Signalmastes, durchgehende Gleisbildwege über virtuelle Ziele sowie Kreuzungen mit getrennten gegenüberliegenden Gleisen. Eine Kreuzung erfindet keine abbiegende Verbindung. Eindeutig benannte ungebundene Signalfelder können beim Erkennen registriert werden; anonyme oder mehrdeutige Symbole weiter in der Welt zuordnen. Nicht eindeutig darstellbare Fälle bleiben gemeldet.

Ein tatsächlich gemeinsamer **A/B-Kasten** verbindet den passenden gezeichneten Anschluss mit der realen Weichenüberwachung. Es entsteht kein fiktiver Abschnitt zwischen zwei Kennungen desselben Kastens. Nur nahe beieinander stehende A- und B-Kästen bleiben getrennte Grenzen; ihr Zwischenstück braucht einen echten Freimeldeabschnitt.

Die optionale Suchlänge ist anfangs leer: keine Längenfilterung. Ein eingegebener Wert begrenzt die Vorschlagssuche, nicht die Länge eines bereits eingerichteten Belegungsabschnitts. Lange Abschnitte werden vollständig geprüft; ein Signal ohne eigenen Melder unterteilt sie nicht automatisch. Zusätzliche echte GFM-Grenzen bleiben wirksam.

Prüfen: besetzte Weichenspitze und beide Äste, gemeinsames A/B, separate nahe Grenzen, Kreuzverbindungen, Gegenrichtung, virtueller HZ-Punkt zwischen Signalen und einen Abschnitt über 100 m mit Zugschluss. Bei der Kreuzung darf eine Belegung nicht auf das unabhängige Gleis übertragen werden. Gold/unbekannt ist keine Freimeldung.

## Fahrwegauswahl

Mehrere Fahrwege erscheinen in einem eigenen Auswahlfenster. Zwei unterschiedlich lange Wege heißen **Kurzer Weg** und **Langer Weg**; gleich lange oder mindestens drei Alternativen erhalten unterscheidende Signal-/Weichenangaben in Reihenfolge. Interne AUTO-Kennungen entfallen in diesen Beschriftungen. Auswahl, Abbruch, lange Namen und enges Fenster im Spiel prüfen.

## Freie Strecke und Nachbarsignale

In der Block-/Streckeneinrichtung lassen sich je Richtung mehrere Ausfahrten und Einfahrten hinzufügen. Das erste Mitglied ist die geometrische Referenz; weitere müssen denselben gemeinsamen Streckenbereich richtig durchfahren. Es gibt keine allgemeine Pflicht zu vier physischen Signalen. Statt eines fehlenden Einfahrsignals kann ein eingerichtetes Zug-Hilfsziel dienen. Fehlende Ziele werden nicht stillschweigend ergänzt; eine aktive Verbindung braucht weiterhin vollständige Wege und Freimeldeabdeckung. Mehrere Ausfahrten mit nur einer Einfahrt und Gegenrichtung ausdrücklich testen.

**Nachbarsignal zur Anzeige zuordnen …** bindet im örtlichen Gleisbild eine schreibgeschützte Darstellung ein. Signalzustand und eingerichtete GFM bleiben sichtbar, der tatsächliche Besitzer bleibt unverändert. Der örtliche Nachbar darf das Signal nicht als eigenen Fahrstraßenstart bedienen; die übergeordnete ESTW-Steuerung bleibt möglich. Die Abschaltung einer reinen Anzeigetafel darf das Signal beim eigentlichen Stellwerk nicht abschalten. Das ersetzt keine neue Mehrspieler-Rollenverwaltung.

## Für das nächste Update vorgemerkt

Das vollständige Freiziehen des Zuges hinter ein deckendes Signal vor KI-Wende bei Rangier-Wegpunkten ist **nicht Teil dieses Teststands**. Zugschluss, Weichenfreiraum, notwendige neue Anfrage und fehlender Platz müssen dafür gemeinsam behandelt werden. Erst diesen Einrichtungsstand im Spiel testen.

Bei Fehlern Mod-/Spielversion, Aktion, Signal-/Weichen-/GFM-Kennung und Player.log angeben. Installation, native KI, Darstellung und Mehrspieler sind noch praktisch abzunehmen.
