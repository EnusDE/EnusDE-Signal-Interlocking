# 0.7.38-P0 – Belegungsursachen, Flankenschutz und Diagnoseexport

Interner Teststand für Railroader 0.6.2. Host und Clients benötigen dieselbe Version. Die öffentliche Erstversion enthält diese Ergänzungen nicht. Die tatsächliche Testwelt und Mehrspieler sind noch praktisch zu prüfen.

## Belegungsablehnung gezielt prüfen

Die bisherige Sammelmeldung „Fahrweg/Weichenbereich belegt oder nicht lesbar“ nennt jetzt den betroffenen Fahrweg-/Flankenabschnitt oder die Weiche, die native Gleiskennung, den Meterbereich und getrennt „belegt“ oder „nicht lesbar / unbekannt“. Fehlende Daten werden nicht als tatsächliche Zugbelegung bezeichnet. Vorhandene Belegungs- und Verschlussprüfungen bleiben wirksam.

Bei einer erneuten Ablehnung direkt ein Diagnose-ZIP exportieren. Darin stehen der letzte Stellversuch und bis zu acht vorherige Versuche. `Sampled: false` bedeutet, dass diese Stelle bei diesem Versuch nicht abgefragt wurde; es ist keine gemessene Besetzung. Die aktuelle Momentaufnahme kann sich vom damaligen Ergebnis unterscheiden.

## Flankenschutz

Eine abweisende Schutzweiche reserviert nicht mehr zusätzlich ihre anderen beiden Gleisäste als Flankenabschnitte. Der tatsächliche Gefahrenkorridor zur Fahrstraße und deckende Signale bleiben geschützt. Muss die Schutzweiche umlaufen, müssen ihre Schutzbereiche frei sein. Steht sie bereits abweisend, bleibt sie in dieser Lage verschlossen; Züge auf einem unabhängigen Ast werden nicht allein deshalb abgewiesen. Eine zweite Fahrstraße über dieselbe verschlossene Weiche kann weiterhin widersprechen.

Gespeicherte Zugfahrstraßen mit Weichen aus älteren Testständen müssen neu berechnet werden: **Editor → Menü → Gespeicherte Fahrstraßen planen**, Definition wählen, reale Weichen für den gewünschten Verlauf stellen und **Fahrweg neu berechnen**. Die Planung ist auch bei aktiviertem dynamischem Fahrwegmodus zugänglich; zum Bedienen gespeicherter Definitionen diesen Modus ausschalten. Ihre alte Flankenberechnung wird nicht stillschweigend verändert. Dynamisch neu berechnete Fahrwege verwenden die neue Berechnung sofort. Laufende Festlegungen zuvor regulär auflösen.

## Hilfsziele und Zugnummern

Zugeordnete Hilfsziele werden im Bedienkontext des gewählten Startsignals und eines ausgewählten Signalfeldes angezeigt, auch wenn danach ein anderes Feld ausgewählt wurde. Die Haupt- und Rangierhälfte werden berücksichtigt. Erst Start wählen, dann **Zum Hilfsziel …**. Fahrart, Anfahrtrichtung und dazwischenliegende Signale werden weiter geprüft. Ein normaler Klick auf ein Hauptsignal bleibt dessen gewöhnliche Zieltaste.

Zugnummernwechsel werden erst nach erfolgreicher Übernahme als erledigt markiert. Ein vorübergehend belegtes Nummern-Zielfeld kann erneut geprüft werden; der nachfolgende Zug darf dabei nicht seine Nummer an einen früheren Stellversuch verlieren. Ein leerer Ausgang ohne Nummer erzeugt keine nachträgliche Nummer. Präfixvarianten derselben fünfstelligen Nummer werden beim Weiterreichen nicht als verschiedene Zugnummern behandelt.

Wenn ein Zug mehrere GFM-Abschnitte gleichzeitig belegt, erhält seine automatisch aus Fahrzeugen gelesene Nummer eine gemeinsame, während der Überlappung stabile Anzeigequelle. Die Auswertung ist auf 512 eigenständige Nummernfelder begrenzt; darüber zeigen solche automatischen Anzeigen unbekannt, statt zufällig doppelte Nummern auszugeben. Ausdrückliche, schreibgeschützte Anzeigekopien und ESTW-Projektionen derselben Quelle bleiben möglich.

Mehrere eigenständige Felder für dieselben Signale oder alte doppelte Nummerneinträge werden als **?** angezeigt. Ein Quellenfeld verwenden und weitere Ansichten als Anzeigekopie verknüpfen. Bei alten doppelten gespeicherten Nummern die Nummer im gewünschten Quellenfeld erneut speichern; dadurch werden deren alte Doppeleinträge entfernt. Diese Korrektur wird nicht beim bloßen Öffnen vorgenommen.

## GFM-A-051 finden und Abschnitt verstehen

GFM-Kennungen sind unabhängig von Signalnummern. Eine nicht sichtbare Grenzkennung beweist weder eine gelöschte Grenze noch eine korrekte Zeichnungszuordnung.

**Editor → GFM-Prüfung:** Die Auswahl bevorzugt die Zuordnung des ausgewählten Feldes, danach Abschnitte mit beiden Grenzen in der Tafel. Auch längere Abschnitte mit nur einem sichtbaren Ende bleiben prüfbar. Zu jeder Grenze stehen Quelle (Signal, einzelner Kasten oder erzeugte Weichengrenze), stabile Kennung, natives Gleis/Meter und die Tafeln/Feldpositionen. Bei einem physischen Objekt lassen sich dessen Eigenschaften direkt öffnen. Das ist keine Kamerafahrt.

**Verknüpfte Gleisbildfelder: 0** bedeutet, dass der Abschnitt nicht mit den Gleisfeldern dieser Zeichnung verknüpft ist. Die Hervorhebung kann dann nur vorhandene Endpunkte zeigen. **Physische Definition gültig** prüft den gespeicherten Verlauf, keine Freimeldung und keine richtige örtliche Zuordnung. Ein Signal ohne eigenen aktiven GFM oder ein virtuelles Hilfsziel unterbricht einen physischen Abschnitt nicht. Eine neu eingefügte wirksame C-Grenze erfordert eine erneute Erkennung der angrenzenden Abschnitte. Alte Signal-Endpunkte ohne aktiven Melder können nicht mehr als gültige GFM ausgegeben werden.

Der konkret gemeldete Abschnitt A-051 ↔ A-050 über 2.773,74 m ist ohne aktuelle Spieldaten noch nicht erklärt. Die Darstellung allein beweist keinen erfundenen Melder. Keine Weltobjekte auf Verdacht löschen.

## Diagnose-ZIP und Abgleich mit der Cloud

1. Auf dem **Host** die Meldung auslösen.
2. **Company → Optionen → Mods → Signaltechnik → Optionale lokale Diagnose → Diagnose-ZIP exportieren**. Alternativ im Diagnosefenster exportieren.
3. Das Exportergebnis zeigt den vollständigen Dateinamen. Standard unter Windows: `%USERPROFILE%\AppData\LocalLow\Giraffe Lab LLC\Railroader\EnusDE-Signaltechnik\Diagnostics\Exports`.
4. Optional im Feld **Exportordner** einen absoluten lokalen Ordner eintragen und **Anwenden & Speichern** wählen. Ein durch den Benutzer synchronisierter Ordner ist möglich. Danach erneut exportieren.

Enthalten sind Version/Zeit/Hostrolle, Installation, Gleisbilder, gespeicherte und aktive Fahrstraßen, letzter Stellversuch samt tatsächlich gelesenen Werten, bisherige Versuche, aktuelle Abschnitts-/Block-/Weichenabfragen mit Fahrzeugkennungen, Fahrzeugpositionen/Kupplungen, GFM-Herkunft, Netz und Nummernzuordnungen. Aufnahmefehler und Mengenbegrenzungen werden kenntlich gemacht. Für Host-Belegungsdiagnosen auf dem Host exportieren; Clientdaten sind keine Host-Freigabe.

Jeder Export erhält einen neuen Namen. Er liest den Spielzustand und gibt keine Bedienbefehle. Ein Archiv enthält höchstens 64 MiB unkomprimierte JSON-Daten, einzelne Teile höchstens 32 MiB; größere Exporte scheitern ausdrücklich. Die Momentaufnahme ersetzt weder Spielstand noch Anlagensicherung. Die Hintergrundaufzeichnung bleibt separat im angezeigten Aufzeichnungsordner. Der Export arbeitet auch bei ausgeschalteter Hintergrundaufzeichnung.

Es gibt keinen automatischen Upload und keinen direkten Fernzugriff auf den Windows-Rechner. Den passenden Export hier zur Untersuchung bereitstellen; der Quellcode bleibt im Cloud-Arbeitsbereich. Ein synchronisierter Ordner allein verbindet diesen Chat noch nicht automatisch mit deinem Rechner.

## Neustart

Nach dem Laden starten die Signale absichtlich mit Halt. Aktive Fahrtfreigaben und ihre Laufzeitverschlüsse werden nicht aus der vorherigen Sitzung wiederhergestellt. Konfiguration, gespeicherte Fahrwege und Nummernzuordnungen bleiben erhalten; Fahrstraßen neu anfordern. Wenn das nicht gelingt, Diagnose-ZIP erstellen und die genaue neue Ablehnung prüfen.

## Gezielte Spieltests

- Freie Bahnhofsfahrt HS-044 → HS-036 erneut versuchen. Bei Ablehnung genauen Abschnitt und Host-ZIP prüfen.
- Zugfahrt neben bereits abweisender Schutzweiche einstellen; unabhängigen Ast befahren. Gegenprobe: notwendiger Weichenumlauf bei besetztem Schutzbereich muss scheitern, Flankensignal muss auf Halt bleiben.
- Startsignal wählen, anderes Feld anklicken, zugeordnetes Hilfsziel aus dem Kontext wählen; Gegenrichtung und nicht zugelassene Fahrart müssen abgewiesen werden.
- Einen langen Zug zwei Nummernabschnitte überlappen lassen, Zielnummer vorübergehend belegen, danach freigeben. Kein doppelter normaler Nummerneintrag, korrektes Weiterreichen; Anzeigekopien folgen ihrer Quelle.
- A-051 anhand der neuen Herkunft prüfen. A/B-045 ↔ A-050 und A-051 ↔ A-050 anhand Quelle, Länge und Zeichnungszuordnung unterscheiden. Neue C-Grenze erkennen lassen, alte überspringende Abschnitte prüfen.
- Speichern, beenden, neu laden: Halt erwarten, danach freie neue Fahrstraße anfordern. Export zweimal erstellen und beide Archive behalten.
