# 0.7.16 – Bedienung, H/V-Signale und sichtbare Weichengrenzen

0.7.16 ist eine **P0-Vorabversion für Railroader 0.6.2**. Sie baut auf 0.7.15 auf. Host und alle Clients benötigen dieselbe Modversion. Die Änderungen sind offline geprüft; eine vollständige Spiel- oder Mehrspielerabnahme liegt nicht vor.

## Stellwerkseditor und Auswahllisten

- Fahrstraßen, Hilfsziele, Stellwerke, Signalzuordnungen und weitere größere Auswahllisten lassen sich nach Kennung oder Bezeichnung durchsuchen. Auswahl und mehrzeilige Treffer erhalten mehr Platz als in den bisherigen schmalen Auswahlfeldern.
- Die Zieloptionen für Zug- und Rangierfahrten haben getrennte Beschriftungszeilen.
- Bei einem Hilfsziel werden die verknüpften Signalfelder angezeigt und können direkt geöffnet werden. Vor dem Löschen dort **Hilfsziel dahinter → Ohne Hilfsziel** wählen und das Signalfeld speichern. Die Löschsperre schützt weiterhin bestehende Zuordnungen und Fahrstraßen.

## Hilfstasten bleiben ausschließlich Ziele

- Eine Hilfstaste ist ein virtueller Zielpunkt am echten Gleis. Sie wird nicht zum Startsignal und erzeugt kein reales Signalmodell oder zusätzliches KI-Signal.
- Beim Anklicken bleibt die am Start gewählte Fahrart erhalten. Ein Klick auf die andere Feldhälfte schaltet nicht mehr versehentlich zwischen Zug- und Rangierfahrt um. Die Zielkonfiguration muss die gewählte Fahrart zulassen.
- Unter **Fahrstraßen einrichten** steht zusätzlich die Suchauswahl **Hilfstaste als Ziel** bereit: Startsignal wählen, Fahrart wählen, Hilfstaste auswählen und **Hilfstaste als Ziel übernehmen** drücken. Anschließend den Fahrweg suchen und speichern.
- Gleisposition, Anfahrtrichtung, dazwischenliegende Signale und die bestehenden Fahrweg- und Verriegelungsprüfungen gelten weiterhin. Die Suchfunktion garantiert nicht, dass jeder gewählte Zielpunkt einen zulässigen Fahrweg ergibt.

## Höhe und Bodenauflage

- Für eigene Signale und GFM-Kästen kann die Aufstellhöhe fest ans Gleisbett gebunden werden. In diesem Modus verschiebt die automatische Bodenerkennung das Objekt nicht auf ein tiefer liegendes Gelände.
- **Auf Bodenhöhe ausrichten** prüft mehrere Punkte unter der Aufstellfläche, setzt das Objekt knapp über den höchsten erkannten Punkt und speichert eine feste Höhe. Die Funktion steht beim Konfigurieren und in der Platzierungsvorschau bereit.
- In der Vorschau lässt sich die Höhe in Schritten von **10 cm** nachjustieren. Die bisherigen Höhen- und Abstandsgrenzen gelten weiter.
- Ein optionaler **Unterbau** füllt einen erkannten Abstand unter dem Sockel. Er ist ein zusätzliches Modell; das Landschaftsgelände wird weder aufgeschüttet noch abgegraben. Fehlende oder außerhalb des unterstützten Bereichs liegende Bodentreffer müssen manuell geprüft werden.
- Höhenbindung und Unterbau werden mit dem Objekt gespeichert. Ältere Objekte behalten zunächst die automatische Geländeauflage. Bei der automatischen Auflage wird die Eignung des Bodentreffers vor der Höhenkorrektur geprüft.

## Modelle nach klassischen H/V-Vorbildern

- Eigene Hauptsignale erhalten einen breiteren Schirm mit abgeschrägten Ecken, versetzten Lampen und Schuten. Der Hauptmast ist als Gittermast mit weiß-rot-weißem Mastschild gestaltet.
- Hp 0, Hp 1 und Hp 2 behalten ihre vorhandene Bedeutung. Bei Sh 1 bleibt der Hauptschirm auf Halt und der getrennte Rangierschirm zeigt zwei weiße Lichter. Die Lampen am Rangierschirm sind kleiner und getrennt angeordnet.
- Die Geometrie ist eine eigene, an H/V-Vorbildern orientierte Darstellung. Die Fahrweg- und Verriegelungslogik wird durch die neue Modellform nicht ersetzt.

## Automatische GFM-Kästen an Weichen

- An den drei vorhandenen Astgrenzen einer vollständig erkannten Weiche werden automatisch Kästen mit Kabel und Bodenanpassung dargestellt, zum Beispiel **GFM-W283-S**, **GFM-W283-L** und **GFM-W283-R**.
- **S** bezeichnet die Spitzenseite am Stammgleis. **L/R** bezeichnen die beiden Äste **von der Spitzenseite aus gesehen**. Die Kennungen werden aus der Geometrie bestimmt und hängen nicht von der aktuellen Weichenstellung ab.
- Die Positionen stammen aus den äußeren Grenzen des vorhandenen Weichen-Belegungsbereichs. Die bisherigen internen Ast-IDs bleiben erhalten. Umbenennen einer Weiche aktualisiert die sichtbaren Kennungen.
- Die Kästen werden aus der gemeinsamen Anlage abgeleitet. Es entstehen keine zusätzlichen manuell gespeicherten Platzierungsobjekte und keine neuen Nummern im GFM-A/B-Nummernkreis. Nach dem Laden werden die Modelle schrittweise aufgebaut.
- Bei fehlender oder mehrdeutiger Geometrie werden keine geratenen L/R-Kennungen dargestellt; vorhandene Abschnittsgrenzen bleiben weiterhin verfügbar. Für die Darstellung muss auch der Boden unter der Aufstellfläche erkannt werden.
- Anklicken eines automatischen Kastens merkt die Grenze für den Abschnittseditor vor. Dort kann sie als Abschnittsende übernommen werden.

## Prüfung und Aktualisierung

Die Offline-Prüfungen umfassen **596 erfolgreiche Prüfungen**, darunter Regressionen zu Hilfszielauswahl, gespeicherter Höhenbindung, Unterbau, H/V-Lampen, gespiegelten Weichen und stabilen S/L/R-Grenzkennungen. Diese Ergebnisse ersetzen keine Unity- oder Mehrspielerprüfung.

Offen bleiben insbesondere:

- Zug- und Rangierfahrstraßen zu Hilfstasten auf einem vorhandenen Spielstand;
- Bodenauflage, Höhenkorrektur, Unterbau und Kabel bei Hanglage und Schotter;
- H/V-Darstellung und Lampenwechsel im Spiel;
- alle drei Weichengrenzen, beide Überfahrtrichtungen, Zugschluss und Fahrzeuge über einer Grenze sowie Weichenastbelegung und Gleisbildanzeige;
- optische Abschaltung und Wiederherstellung unterstützter Bestandssignale;
- Speichern/Neuladen und Host-/Client-Beitritt mit gleicher Modversion.

Der vollständige Austausch nativer Bestandssignale bleibt offen. Das Installationspaket heißt **EnusDE-Signal-Interlocking-0.7.16-P0.zip**. Die Installation beziehungsweise Aktualisierung über den Modmanager und der vollständige Neustart von Railroader erfolgen durch den Nutzer. Vor einer Spielprüfung bei allen Beteiligten dieselbe Version installieren.
