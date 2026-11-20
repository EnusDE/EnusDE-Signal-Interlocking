# 0.7.43-P0 – Einrichtung durch Anklicken

Neue interne Testfassung auf Basis 0.7.42. Vorhandene vorbereitete ZIPs bleiben
unverändert. Railroader 0.6.2; Host und Clients benötigen dieselbe Modversion.

## Hilfsziele

Im Editor unter Eigenschaften **Ziel durch Anklicken** öffnen. Ein eindeutiges,
im Gleisbild verbundenes Einfahrsignal kann automatisch vorgeschlagen werden.
Alternativ den Bezug im Gleisbild, in der Welt oder über die Namensliste wählen.
Die Gegenrichtung des Bezugssignals bildet die reale Anfahrt des Hilfsziels.
**Feld speichern** bestätigt den Vorschlag. Gleis-ID, Meter und Richtung bleiben
unter **Manuelle Gleisposition / erweiterte Einstellungen** bearbeitbar.

Der bestätigte Bezug verwendet eine feste Signal-ID. Verschieben und Umbenennen
des Bezugssignals werden bei der Fahrwegprüfung nachgeführt. Ein fehlender oder
noch nicht gebauter Bezug ergibt kein verwendbares Ziel. Aktive Fahrstraßen und
Streckenbindungen zum Hilfsziel schützen auch das Bezugssignal gegen Umbau.
Vorhandene Hilfsziele ohne Bezug bleiben manuell eingerichtet.

## Strecken und Blockeinrichtung

Jede Endsignalgruppe besitzt **Im Gleisbild auswählen** und **Signal in der Welt
auswählen**. Die Gleisbildauswahl bietet eine eigene Stellwerksauswahl und
hervorgehobene geeignete Ziele, ohne die gerade bearbeitete Tafel zu wechseln.
Suchlisten bleiben verfügbar. **Endpunktzuordnungen automatisch vervollständigen**
übernimmt nur eindeutige vorhandene Ein-/Ausfahrrollen; fehlende und mehrdeutige
Gruppen werden benannt. Verlauf und GFM werden danach wie bisher geprüft und
erst mit **Streckenkonfiguration übernehmen** gespeichert.

Von einem örtlichen Streckenmelder kann ein vorhandenes Einfahrsignal des
Nachbarn als schreibgeschützte Anzeige auf ein freies Nachbarfeld gesetzt
werden. Die Zuständigkeit des Originals bleibt erhalten. Eine erteilte Erlaubnis
wird jetzt mit ausgefülltem Pfeil dargestellt; ohne gültigen Erlaubniszustand
erscheint kein solcher Leuchtpfeil. Anfrage und Erlaubnis bleiben getrennt.

Nach erfolgreicher Verlaufserkennung bietet der optionale Blockassistent:

- 1–16 Blöcke oder einen ungefähren Abstand entlang des tatsächlichen Gleises;
- veränderbare Grenzentfernungen, vorhandene Signalstandorte und Wiederverwendung;
- neue Signal-/GFM-Grenzen, gegebenenfalls beide Mastrichtungen;
- vollständige Abschnittsdefinitionen und eine zusätzliche Blockdarstellung im
  ausgewählten örtlichen Stellwerk einschließlich Zugnummernfeldern;
- optional Anforderungskontakte mit Zielen zum nächsten Block in beiden Richtungen;
  globale Automatik bleibt unverändert, vorhandene Regeln werden nicht überschrieben;
- gemeinsame geprüfte Übernahme von Bauobjekten und Stellwerksdaten mit Sicherung.

Drei Kilometer und drei Blöcke ergeben zwei innere Grenzen. Vorhandene Signale
dürfen nicht durch einen nachträglich geänderten Vorschlag übersprungen werden.
Die Enden müssen eindeutige aktive GFM-Grenzen besitzen. Der Assistent gilt nur
für weichenfreie Strecken; Weichenbereiche benötigen weiterhin eigene Einrichtung.
Er berechnet keine vorbildgerechten Bremswegabstände. Mindestlängen sind technische
Spielgrenzen. Materialpflichtige Objekte bleiben nach Übernahme Baupläne und
werden erst nach Fertigstellung betriebsbereit. Ein bloßer Vorschlag baut nichts.

## Editor und bestehende Automatik

Im Auswahlmodus ein vorhandenes Feld oder eine markierte Gruppe mit gedrückter
linker Maustaste ziehen. Die Vorschau ist transparent, gültige Ziele sind grün,
unzulässige rot. Loslassen übernimmt; Escape bricht ab. Belegte Felder werden
nicht überschrieben. Zuordnungen und Nummernquellen bleiben erhalten; Rückgängig
nutzt den bisherigen Editorverlauf. Leere Flächen bleiben zum Verschieben der
Ansicht nutzbar. Zeichnen und Setzen ziehen keine Felder mit.

Start, Erstziel und Ausweichziel der vorhandenen Zugfahrt-Kontaktautomatik können
nun ebenfalls im Gleisbild gewählt werden. Anschlussstelle und Überholbahnhof
sind getrennte Anlagenarten; die bisherigen gespeicherten Arten bleiben kompatibel.
Die zentrale Betriebsstellenansicht bietet einen direkten Einstieg in örtliche
Einstellungen. Die neue Bezeichnung erzeugt allein noch keine Anschlussbedienung,
Aufnahmegleisreservierung oder selbstständige Kreuzungssteuerung.

## Parallele Fahrstraßen

Gemeinsame reine Überwachungsbereiche führen nicht mehr allein zur Ablehnung,
solange keiner der tatsächlichen Fahrwege hineinführt. Eine in gleicher Stellung
benötigte Schutzweiche kann von kompatiblen Fahrstraßen gemeinsam gehalten werden.
Zwei tatsächliche Weichenfahrwege bleiben exklusiv; Gegenstellungen, befahrene
Schutzbereiche, widersprechende Signalbegriffe und unbekannte Belegung bleiben
gesperrt. Beim Auflösen einer Fahrstraße bleibt der Schutz anderer Fahrstraßen
bestehen. Alte Fahrwege ohne gerichtete Geometrie werden konservativ behandelt.

## Abgrenzung und Prüfung

Dies ist der erste technische Umsetzungsschritt der besprochenen Vereinfachung.
Die bestehende Kontakt-Automatik erhält eine einfachere Einrichtung. Neue
fahrzeugbezogene Mehrschrittaufträge, Spielerkommunikation/Bedienrechte,
Anschlussbedienung mit Rückfahrverfahren, automatische Kreuzungsannahme,
fahrstraßenabhängige Mehrziel-Vorsignale und astbezogene Geschwindigkeiten bleiben
gesonderte Arbeitspakete. Die öffentliche 0.7.40-Anleitung wird nicht verändert.

Gebaut und offline gegen die bereitgestellten Spielbibliotheken geprüft.
Drag-Vorschau, Gelände-/Modellpositionen und Host-/Client-Anzeigen brauchen einen
Spieltest. Siehe die bilinguale Anleitung `Guides/TEST-0.7.43.de.md` / `.en.md`.
