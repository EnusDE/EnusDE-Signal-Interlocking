# 0.7.25-P0 – schematische Erkennung, GFM-Rollen und ESTW-Modultafel

Lokaler Teststand für Railroader 0.6.2. Bestehende Spielstände weiterverwenden. Noch keine Installation, Veröffentlichung oder Spiel-/Mehrspielerabnahme durch Codex. Vorbereitete Pakete bis einschließlich 0.7.24 bleiben unverändert.

## Anlass und Umsetzung

Die bisherige Zeichnung skalierte geografische Weltkoordinaten auf 512 × 320 Zellen und verdrängte nahe Knoten. Damit waren vollständige Verbindungen möglich, aber kein verlässlich lesbares Stellwerksbild. Die neuen Zeichnungsprüfungen kontrollieren jetzt auch waagerechte Hauptstränge, getrennte Überholgleise, kompakte kleine Bahnhöfe und Unabhängigkeit von Weltkrümmung, Skalierung und Eingabereihenfolge.

`SchematicNetwork` bildet zusammenhängende Stränge aus Stamm-/Grundstellungsästen. Sie werden auf Zeilen angeordnet. Ein begrenzter Renderer verbindet die tatsächlichen Knoten; Kreise, Richtungen und reale Abschnittsbezüge bleiben erhalten. Kleine Anlagen erhalten passende tatsächliche Tafelmaße statt einer maximal gestreckten Fläche.

Standardmäßig begrenzt `NetworkScope` nur die Zeichnung auf die Umgebung platzierter Signale. Die vollständige physische Abschnittsbasis bleibt erhalten; Randfelder verweisen auf vollständige reale Abschnitte und werden als Netzgrenze gekennzeichnet. Das gesamte Netz ist weiterhin wählbar, wird aber ausdrücklich nicht als immer übersichtlicher Betriebsstellenplan versprochen. Der Offline-Kartenprüffall verwendet geometrisch angenäherte Weichenäste; im laufenden Spiel liefert die native Weichendekodierung diese Daten.

Die Modelltafel erlaubt jetzt 2048 × 512 Zellen; Baustein- und gemeinsame Dateigrenzen bleiben bestehen. Das vorherige Gleisbild bleibt bei Übernahme archiviert. Übernahme wird vor dem Schreiben auf die gemeinsame 16-MiB-Grenze geprüft.

## GFM

- Feste Rollen: A am Signal, B frei beziehungsweise an der Weiche, C einseitig. C hat ein eigenes prozedurales Modell mit Pfeil. Altkennungen bleiben technisch stabil.
- Neue Signale erhalten A standardmäßig. Ein dauerhaft aktives Schnellwerkzeug kann bestehende Signale nacheinander mit A/C ausrüsten oder den angebauten Kasten deaktivieren.
- C wird in der Grenzsuche und in gespeicherten Abschnittsprüfungen richtungsabhängig ausgewertet. Zwischengrenzen dürfen nicht übersprungen werden; eine alte unpassende Zuordnung wird unbekannt.
- Die Erkennung übernimmt vorhandene aktivierte GFM-Grenzen und erzeugt nur gültige direkte Abschnitte. Passende vorhandene Abschnittskennungen werden weiterbenutzt. Das ersetzt keine fehlenden Kästen oder deaktivierte Weichen-GFM.
- Angebauter Kasten mit mehr Abstand zum eigenen Signal; alternative nahe Positionen und Kollisionsprüfung gegen geladene eigene Modelle. Wenn keine Position passt, erfolgt eine Meldung statt überlappendem Modell. Für freie manuelle Weltobjekte besteht keine allgemeine Kollisionsfreiheit mit sämtlichen Grundspielassets.

## Bedienung und Kennungen

- „Module“ direkt in der Stellwerksauswahl öffnet eine sichtbare ESTW-Anordnungsansicht. Einfügen, X/Y-Verschieben, Lösen und Wechsel zur örtlichen Zeichnung sind dort zusammen erreichbar. Verdeckte Ortsmodule bleiben für die Editor-Anordnung sichtbar. Diese Ansicht erteilt keine Fahrtfreigaben.
- Technische Signalkennungen und Richtungs-IDs sind nicht mehr editierbar. Anzeigenamen bleiben frei. Aufwertung RS → HS ändert den Präfix bei erhaltener GUID und GFM-Nummer; frühere Kennung ist inaktive Information. Bereits aufgewertete RS-Signale werden beim Öffnen korrigiert.
- Bestehende andere individuelle Kennungen bleiben erhalten. Automatisches Nummernrecycling samt Bereinigung alter Zuordnungen wurde noch nicht umgesetzt; es erfolgt keine vorzeitige Wiederverwendung.

## AE/KI

0.7.24 blockierte native Umstellbefehle für elektrische Weichen, ließ aber die ursprüngliche Umstellbarkeits-Vorausschau bestehen. 0.7.25 schützt zusätzlich `AutoEngineerPlanner.CheckThrowable`. Normale Handweichen und native CTC-Regeln bleiben über ihre bisherigen Prüfungen bedienbar.

Kontaktanforderungen filtern zuerst nach dem tatsächlichen Wegpunktweg und bevorzugen danach die vorhandene Weichenlage. Dadurch verdrängt eine gerade liegende Weiche keinen gültigen abzweigenden Wegpunktauftrag. Der Fahrstraßenkern stellt und sichert die gewählte Folge weiterhin regulär.

Das gemeldete Weiterfahren bei Halt ist **nicht als im Spiel behoben bestätigt**. AE-Anbindung war laut Autor aktiv; betroffene Signale waren noch nicht zugeordnet. Der Code registriert eigene Haupt-/Rangiersignale auch unabhängig von dieser Zuordnung und soll ohne Fahrstraße Halt liefern. Genau dieser Fall muss im Spiel geprüft werden. Rücksetz-/Bergungsautomatik ist nicht Bestandteil dieser Version.

Die [deutsche Testanleitung](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.25.de.md) und die [englische Anleitung](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.25.en.md) liegen im Installationspaket. Technische Modell-/Zeichnungsvorschauen stammen aus dem aktuellen Code und sind keine Spiel-Screenshots.

## Offline-Prüfung

`Test.ps1`: **1008 Prüfungen bestanden, 0 fehlgeschlagen**. Enthalten sind Kompilierung, Sprachkataloge, native API-Ziele, die Modell-/Grenz-/Kennungsprüfungen, bestehende Sicherungs-/Fahrstraßenprüfungen sowie der große Kartennetz-Prüffall mit 1901 Segmenten und 554 vollständig gezeichneten Gleisketten. Protokoll: `build/checks/0.7.25-tests.log`. Dies ersetzt die beschriebenen Spieltests nicht.
