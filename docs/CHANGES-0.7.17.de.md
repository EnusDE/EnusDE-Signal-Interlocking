# 0.7.17 – Automatische Fahrwege und bearbeitbare Weichenkästen

0.7.17 ist eine **P0-Vorabversion für Railroader 0.6.2** und baut auf 0.7.16 auf. Host und alle Clients benötigen dieselbe Modversion. Die Änderungen sind offline geprüft; die Spiel- und Mehrspielerabnahme bleibt offen.

## Fahrstraßen aus Start und Ziel

- Im Fahrdienstleiter genügt die Auswahl von Startsignal und Zielsignal beziehungsweise Hilfszieltaste. Fehlt eine passende gespeicherte Folge, ermittelt das System den tatsächlichen Fahrweg und erzeugt die benötigten Definitionen im Hintergrund. Gültige vorhandene Definitionen werden wiederverwendet; neue Definitionen werden vom Host in der gemeinsamen Anlage gespeichert.
- Hauptsignale im Fahrweg werden als Teilfahrstraßengrenzen berücksichtigt. Mitlaufende Rangiersignale einer Zugfahrstraße werden über die vorhandene Signalanbindung behandelt. Rangierfahrten können als Folge über die jeweiligen Zwischenziele ermittelt werden.
- Identische gespeicherte Definitionen mit unterschiedlichen Kennungen beziehungsweise IDs zählen nicht mehr als mehrere Fahrwege. Unterschiedliche Schutz- und Warteeinstellungen werden weiterhin unterschieden. Das manuelle Speichern einer bereits identischen Definition verwendet die vorhandene Definition erneut.
- Gibt es tatsächlich verschiedene Wege, wird ein eindeutig zur aktuellen Weichenlage passender Weg bevorzugt. Unbekannte Rückmeldungen oder mehrere passende Wege führen nicht zu einer geratenen Auswahl. Die möglichen Wege erscheinen dann als Auswahl mit Weichenlagen und Schutzwartezeiten.
- Beim Stellen gelten weiterhin die normalen Belegungs-, Flankenschutz-, Endlagen-, Annäherungs- und Verriegelungsprüfungen. Eine gespeicherte oder ermittelte Definition bedeutet noch keine Fahrtfreigabe. Bestehende aktive Rangierfahrstraßen werden ausschließlich über die vorhandenen Einbeziehungs- beziehungsweise Rücknahmeprüfungen behandelt.
- Die Grenzen von höchstens 16 Teilfahrstraßen pro Folge und 128 gespeicherten Definitionen bleiben erhalten. Zu umfangreiche oder unzulässige Suchen werden mit einer Meldung beendet.

## Fahrart und Hilfsziele

- Die Standardfahrart wird aus dem gewählten Startsignal beziehungsweise seiner Haupt- oder Rangierhälfte bestimmt. Ein reines Rangiersignal ist damit automatisch ein Rangierstart.
- **Langsamfahrt (Hp 2)** kann gezielt gewählt werden, wenn das Startsignal diese Fahrart unterstützt. Der Editor zeigt die verfügbaren Begriffe und bietet einen direkten Weg zur Konfiguration des Startsignals.
- Hilfszieltasten bleiben **ausschließlich Ziele**, auch bei der automatischen Planung. Die im Ziel freigegebenen Fahrarten und die Anfahrtrichtung am realen Gleis werden weiter geprüft.
- Manuelles Einrichten, Prüfen und Auswählen gespeicherter Fahrstraßen bleibt im Editor möglich; es ist keine Voraussetzung für jeden neuen Start-Ziel-Auftrag.

## Automatische Weichenkästen verschieben

- Automatisch dargestellte GFM-Weichenkästen lassen sich durch Anklicken bearbeiten. Bei Überschneidungen mit Signalen oder anderen Objekten sind sie zusätzlich über **Baumenü → Objekte → Automatische Weichenkästen** in einer Suchliste erreichbar.
- Pro Kasten sind seitlicher Abstand, Gleisseite, Längsversatz auf dem Grenzsegment, Höhenkorrektur beziehungsweise feste Höhe und Unterbau einstellbar. Eine blaue Vorschau zeigt die neue Modellposition vor dem Speichern.
- Die **logische GFM-Grenze und der Kabelanschluss am Gleis bleiben am ursprünglichen Gleispunkt**. Nur das Kastenmodell und der sichtbare Kabelverlauf werden versetzt. Bestehende Abschnittsbindungen und die S/L/R-IDs bleiben erhalten.
- Modellversätze werden in der gemeinsamen Stellwerksanlage gespeichert, mit übertragen und nach dem Laden wieder angewendet. **Automatische Position wiederherstellen** entfernt den individuellen Versatz.
- Der Längsversatz muss innerhalb des vorhandenen Grenzsegments bleiben. Eine automatische Kollisionsvermeidung gegenüber benachbarten Gleisen oder Signalen ist damit noch nicht eingeführt; ungünstige Modellpositionen können manuell korrigiert werden.

## Neue Gleisbildsymbole

- Hauptsignale erhalten einen erkennbaren Schirm mit einem Lichtpunkt; Rangiersignale einen breiteren Schirm mit zwei Lichtpunkten. Die Rangierfreigabe verwendet zwei weiße Punkte.
- Haupt- und Rangierschirme stehen standardmäßig **auf derselben Gleisseite**, bei geteilten Feldern nebeneinander. Das gilt auch bei unterschiedlichen Pfeilrichtungen der beiden Hälften. Spiegeln versetzt beide auf die andere Seite. Die Richtungstasten bleiben mittig auf dem gezeichneten Gleis.
- Hilfsziele verwenden verwandte Zug- und Rangierschirme. **Goldener Rand und Z** kennzeichnen sie als reine Zieltasten ohne eigene Fahrtfreigabe. Sind beide Fahrarten freigegeben, werden Zugziel und Rangierziel getrennt nebeneinander dargestellt.
- Drehen bestimmt den Symbolpfeil im Gleisbild. Es verändert nicht die Anfahrtrichtung am realen Gleis; diese wird weiterhin im Hilfsziel-Editor separat eingestellt. Beim Auswählen eines geteilten Hilfsziels wird die tatsächlich gewählte Fahrart hervorgehoben.

## Prüfstand und Installation

Die Offline-Prüfungen umfassen **618 erfolgreiche Prüfungen**, unter anderem automatische Zug-, Langsam- und Rangierwege, Hilfsziele, doppelte Definitionen, echte Alternativen, gespeicherte Kastenversätze und die Symbolgeometrie. Die automatische Bedienung nutzt den normalen Verriegelungsweg. Diese Prüfungen ersetzen keinen Spieltest.

Im Spiel sind insbesondere die gemeldeten Fälle **HS-018 → P**, aktive und mitlaufende Rangiersignale, echte Weichenalternativen sowie Hilfsziele in beiden zulässigen Fahrarten zu prüfen. Zusätzlich bleiben Versetzen der Weichenkästen bei Signalüberlappungen und benachbarten Gleisen, Kabel und Bodenauflage am Hang, kleine und große Zoomstufen, Speichern/Neuladen und Host-/Client-Beitritt mit gleicher Modversion offen. Die noch offenen Spielprüfungen aus 0.7.16 gelten weiter; der vollständige Austausch nativer Bestandssignale bleibt offen.

Das Installationspaket heißt **EnusDE-Signal-Interlocking-0.7.17-P0.zip**. Die Installation beziehungsweise Aktualisierung über den Modmanager und der vollständige Neustart von Railroader erfolgen durch den Nutzer. Alle Beteiligten müssen dieselbe Modversion verwenden. Das bestehende 0.7.16-Installationspaket bleibt unverändert.
