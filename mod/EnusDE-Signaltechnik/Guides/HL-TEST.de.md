# Hl-Testpaket 0.7.30-P0

Neue Hl-Bauten freischalten: **Company → Optionen → Mods → Signaltechnik → Signalbetrieb und Darstellung → Hl-Signalsystem aktivieren (Test)** einschalten und die Einstellungen übernehmen. Standardmäßig aus. Bereits gebaute Hl-Signale bleiben bei ausgeschalteter Option betriebsfähig und können auf H/V zurückgerüstet werden.

Zusätzliche, selbst erzeugte Hl-Modelle nach deutschem Vorbild. Grundlage der Anzeigen: **Ril 301, 301.0103 und 301.0103A01**, Ausgabe gültig ab 15.12.2024. Das Paket ist ein Modell- und Anzeigetest, keine vollständige Simulation aller Hl-Anlagenbauformen. Es enthält keine kopierten DB-Bilder, Texturen oder Modelle.

## Einrichten

1. ZIP mit dem Modmanager installieren, Spiel neu starten, bestehende Testwelt weiterverwenden. Host und Clients benötigen dieselbe Version. Es werden keine zusätzlichen Mods benötigt.
2. Signaltechnik → Bauen → **Hl-Hauptsignal · Test** oder **Hl-Vorsignal · Test**. Auf dem Gleis platzieren und normal dem Stellwerk zuordnen.
3. Alternativ einen vorhandenen hohen Hauptsignalmast oder ein Vorsignal anklicken → **Hl-Testmodell verwenden**. ID, Kennung, Standort, GFM-Grenze und Stellwerkszuordnungen bleiben erhalten. Niedrige Hauptsignale zuerst auf einen hohen Mast umrüsten. Ein verschlossener Mast darf nicht umgerüstet werden.
4. Hauptsignal: Richtung A oder B einrichten. Zugfahrt, Langsamfahrt oder **Ra 12 · Rangierfahrt** nach Bedarf freigeben. **Vorsignalfunktion im Hl-Schirm** aktivieren und das anzukündigende Hauptsignal zuordnen. Ohne diese Funktion wird keine Vorankündigung dargestellt. Die Einstellungen wie bisher speichern.
5. Einzelnes Vorsignal: das zugehörige Hauptsignal auswählen und speichern.

## Umfang und Grenzen

Im Betrieb werden die vorhandenen Zustände umgesetzt: Hp 0, Hl 1, Hl 3a, Hl 7, Hl 9a, Hl 10, Hl 12a und Ra 12 am Hauptsignal. Ein eigenständiges Vorsignal zeigt Hl 1, Hl 7 oder Hl 10. Die Vorankündigung folgt der gespeicherten Zielbindung; eine neue fahrstraßenabhängige Mehrzielerkennung ist nicht Bestandteil dieses Pakets.

Beim Hl-System werden Haupt- und Vorsignalfunktion in einem Schirm kombiniert. Grün und Gelb oben kündigen den nächsten Zustand an; bei Hl 4/Hl 7 und den zugehörigen Kombinationen blinkt nur das obere Ankündigungslicht. Das untere gelbe Licht und ein vorhandener Geschwindigkeits-Lichtstreifen leuchten stetig. Der Haltbegriff heißt hier gemäß modernem Signalbuch **Hp 0**, nicht Hl 13.

**Wichtig zur Geschwindigkeit:** Die vorhandene Langsamfahrt wird optisch als 40-km/h-Stufe dargestellt. Das Paket ändert keine native KI-Geschwindigkeitsberechnung, Bremskurve, Weichengeschwindigkeit oder mph-Umrechnung. Eine exakte Einhaltung von 40 km/h durch die KI wird damit nicht zugesagt. Langsamfahrende KI kann weiterhin konservativere native Vorgaben erhalten. Die vollständigen Kombinationen für 60/100 km/h sind in der separaten **Hl-Signalbilder.html** enthalten, nicht als neue Fahrfreigaben auswählbar. Diese Vorschau steuert keine Spielsignale und benötigt zur Benutzung des Mods keinen Browser.

Ungebundene Signale behalten das Ungültigkeitskreuz. Betrieblich abgeschaltete Signale behalten das Kennlicht. Das ist die vorhandene modinterne Betriebsartenlogik; die Verwendung zum Freigeben elektrischer Weichen für die KI ist eine Spielvereinfachung. Die Auswahl eines Hl-Modells verändert weder Fahrstraßensicherung noch Stellwerkszuständigkeit. H/V-Signale bleiben die Voreinstellung; zurückrüsten ist möglich.

## Kurzer Spieltest

1. Ein neues Hl-Signal platzieren: noch ungebunden dunkel mit Kreuz, nach Einbindung ohne Fahrstraße nur rotes Hp 0.
2. Hauptsignal mit Vorsignalfunktion: eigene Zugfahrstraße einstellen, Zielsignal auf Halt → Hl 10. Zielsignal auf Fahrt → Hl 1. Eigene Langsamfahrstraße → entsprechend Hl 12a oder Hl 3a.
3. Zielsignal auf Langsamfahrt: Hl 7 beziehungsweise Hl 9a. Nur das obere gelbe Licht blinkt; das untere gelbe Licht bleibt bei Hl 9a an.
4. Ra 12 einschalten: zwei weiße Lichter, kein grünes Fahrtlicht und keine gelbe Vorankündigung.
5. Hl-Vorsignal: Ziel Halt → Hl 10, Fahrt → Hl 1, Langsamfahrt → Hl 7. Es zeigt selbst kein rotes Haltlicht.
6. H/V → Hl → H/V und Speichern/Neuladen prüfen: IDs, Namen, GFM und Verbindungen unverändert. Beide Mastrichtungen prüfen. Umrüstung während einer aktiven Fahrstraße muss abgewiesen werden.
7. Kreuz/Kennlicht und Darstellung bei Tag/Nacht prüfen. Danach mit einem Client dieselben Signalbilder und das Blinken prüfen. Für die KI-Annahme getrennt den Halt vor Hp 0 sowie die tatsächlichen Geschwindigkeiten beobachten.

Offline-Prüfungen ersetzen diese Spiel- und Mehrspielertests nicht.

Referenz: [DB InfraGO, Signalbuch Ril 301](https://www.dbinfrago.com/resource/blob/13174940/cb13a4d1ee7bc79e64ecad219bbd91c4/Ril-301-INB-2026-data.pdf), Abschnitte 301.0103 und 301.0103A01.
