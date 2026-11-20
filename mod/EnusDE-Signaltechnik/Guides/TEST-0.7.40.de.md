# 0.7.40-P0 – gemeinsame physische Weichenschutzgrenzen

Interner Teststand für Railroader 0.6.2. Host und Clients benötigen dieselbe Version. Der konkrete Zug vor HS-044 und Mehrspieler sind noch im Spiel zu prüfen.

## Befund und Korrektur

Die GFM-Anzeige berechnete Weichenbereiche bereits aus realen Gleisarmen, physischen Meldern und ausdrücklichen Weichenarm-Zuordnungen. Beim Aufbau einer Zug- oder Rangierfahrstraße wurden dagegen erneut pauschale Bereiche von bis zu 25 m an jedem Weichenarm ergänzt. Dadurch konnten Anzeige und Fahrstraßenprüfung unterschiedliche Bereiche verwenden. Ein vor dem Signal wartendes Fahrzeug konnte außerhalb der echten Meldegrenze stehen und trotzdem in diese zusätzliche Reservierung geraten.

Zug-, Rangier- und abweisende Flankenschutzweichen verwenden jetzt dieselbe effektive physische Weichendefinition wie die GFM-Anlage. Ein näherer realer Melder begrenzt den zugehörigen Arm; die anderen Arme bleiben geschützt. Ausdrückliche manuelle Weichenarm-Verknüpfungen bleiben wirksam. Ohne nähere physische Grenze bleibt der bisherige Standardbereich von bis zu 25 m bestehen. Ein bloßer Signalpfeil oder ein Signal ohne aktiven GFM kürzt den Bereich nicht.

Die Korrektur löscht keine Belegung und erklärt keine besetzte Weiche für frei. Ein Fahrzeug innerhalb des richtig abgegrenzten Schutzbereichs bleibt relevant. Die bekannte Besetzung eines Rangiergleises außerhalb dieser Bereiche bleibt nach den Regeln aus 0.7.39 zulässig.

## Begriffe

**Flankenschutz:** Seitlich zuführende Signale auf Halt halten oder Weichen abweisend stellen und verschließen, damit keine Fahrt in den geschützten Fahrweg gelangen kann. Zusätzlich können gefährdende seitliche Bereiche überwacht werden.

**Weichenschutzbereich:** Überwachte Gleisbereiche an Stamm/Spitze, Grundast und Abzweig einer Weiche. Dort werden Fahrzeuge erkannt, die die Weiche belegen oder in den Fahrweg hineinragen. Dieser Bereich wird auch für das Stellen und Freigeben der Weiche geprüft. Die Belegung selbst besitzt keine Fahrtrichtung.

**Signalrichtung:** Bestimmt den zulässigen gerichteten Fahrweg und den Haltfallpunkt. Die tatsächliche Richtung des Weltsignals ist maßgeblich. Die Zeichnung allein definiert keine physische Meldegrenze.

## Bestehende Fahrwege

Neu berechnete Fahrwege verwenden die gemeinsame Definition sofort. **Gespeicherte Fahrstraßen mit Weichen aus älteren Testständen einmal neu berechnen:** Editor → Menü → Gespeicherte Fahrstraßen planen → Definition wählen → reale Weichen für den gewünschten Weg ausrichten → **Fahrweg neu berechnen**. Aktive Festlegungen zuvor regulär auflösen. Das erhält die Fahrstraßen-ID und die Schutzwartezeit; der neu ermittelte Verlauf muss dem gewünschten Weg entsprechen.

Eine gespeicherte Fahrstraße enthält jetzt eine Kennung ihrer wirksamen Weichenschutzgeometrie. Verändert sich diese durch eine Melderbewegung oder Weichenarm-Zuordnung, wird der alte Fahrweg nicht weiter ungeprüft verwendet. Bei einer aktiven Fahrt bleiben nach einer solchen Störung die Verschlüsse erhalten. Reines Umbenennen oder eine andere Reihenfolge der gelesenen Schutzarme verändert die Kennung nicht.

Kein erneutes Aufbauen des ganzen Stellwerks und keine Neunummerierung erforderlich. Die Werkzeuge zum Aktualisieren/Prüfen aller Elemente und der Abschnittmanager aus 0.7.39 bleiben vorhanden. Eine tatsächliche Fehlzuordnung eines Melders muss dort weiterhin korrigiert werden.

## Meldungen und gezielter Spieltest

Ablehnungen nennen jetzt ausdrücklich **Zugfahrt** oder **Rangierfahrt** und unterscheiden Gleisabschnitt, Weichenschutz und Flankenbereich. Damit ist sichtbar, welche Fahrart tatsächlich angefordert wurde. Ein Hauptsignal kann beide Fahrarten besitzen; die Rangierhälfte beziehungsweise die Aktion „Rangieren“ wählen.

1. Zug unverändert vor HS-044 stehen lassen. Neue Zugfahrt nach HS-036 berechnen lassen; bei gespeicherten Definitionen vorher neu berechnen.
2. Dieselbe Ausgangslage als Rangierfahrt nach RS-067 prüfen. Die Ablehnung muss gegebenenfalls ausdrücklich „Rangierfahrt“ nennen.
3. Die Grenze GFM-A-019 / GFM-A/B-020 und W207 im Abschnittmanager prüfen. Die früher gemeldeten Meter Sm3i9 19,92–27,91 dürfen nicht allein durch einen zusätzlich pauschal erzeugten Schutzbereich außerhalb der effektiven physischen Grenze sperren.
4. Gegenprobe: Fahrzeug tatsächlich in einen Weichenschutzbereich stellen. Die belegte Schutzzone muss weiterhin sperren. Auch die Gegenrichtung und die anderen Weichenarme prüfen.
5. Bleibt die Ablehnung bestehen, unmittelbar auf dem Host ein Diagnose-ZIP exportieren. Es enthält den neuen Fahrweg mit der Schutzgeometriekennung, tatsächliche Fahrzeugintervalle und Melderpositionen. Das Bild allein beweist nicht, dass alle relevanten Schutzbereiche frei sind.

Der Haltfallpunkt und Nummernsprung bleiben wie in 0.7.39 standardmäßig bei 5 m hinter dem Signal, einstellbar auf 0–25 m. Diese Abstandsregel ersetzt keine korrekte Abgrenzung des Weichenbereichs.
