# Runenwacht — experimentelle Darstellungspakete, 0.7.63

Hauptmod **0.7.63**, optionales **Runenwacht 0.1.0**, Railroader **0.6.3**. Early Alpha. Offline-Prüfungen ersetzen keine Spiel-/Darstellungs-/Mehrspielerprüfung.

## Installation und Freischaltung

Beide getrennten Modordner normal installieren und das Spiel neu starten. Das Hauptmod benötigt Runenwacht nicht; Runenwacht verlangt genau diese Hauptmodversion. Host und Clients installieren zunächst manuell dieselben Pakete. Automatische Übertragung von Assets oder ausführbarem Code ist nicht enthalten.

Als Host eine Testwelt laden, normalen Signalbau freischalten (oder Sandbox verwenden) und in der Konsole ausführen:

```
/enus_assets status
/enus_assets enable
/enus_assets unlock:enusde.runenwacht
```

Das Experiment steht nach jedem Laden auf AUS. Die Welt-Testlizenz wird im Anlagenbestand und dessen Konfigurationssicherungen gespeichert. Zum Freischalten muss das kompatible Paket installiert sein. Diese Entwickler-Testlizenz ist kostenlos: Kauf, Bezahlung und eine kommerzielle Lizenzprüfung sind noch nicht umgesetzt. Ein Konsolenbefehl ist bei öffentlichem Quellcode keine Zugriffssicherung.

Nach der Freischaltung den Baumodus erneut öffnen. Dort erscheinen Runenwacht-Haupt-, Vor- und Rangiersignal. Platzierung und Fertigstellung verwenden den vorhandenen Bau-/Materialablauf. Bestehende Signale können in ihren Eigenschaften auf Runenwacht umgestellt werden; eine Fahrstraßensperre verhindert die Umrüstung. Interne Signal-IDs, Fahrstraßenzuordnung, Gleisanker und Betriebsrechte bleiben bestehen. „Eingebaute Darstellung verwenden“ stellt die bisherige Optik wieder her.

Die Richtungsoptionen des Hauptmasts steuern weiterhin Vorder-/Rückseite sowie Vorsignal- und Rangiersignalkopf. Runenwacht benötigt die standardhohe Bauart ohne Hl; eine Umrüstung wählt diese unterstützte Geometrie. Zum Aufwerten eines alleinstehenden Rangiersignals zum niedrigen Hauptsignal zunächst die eingebaute Darstellung wählen. Vorhandene Helligkeits-/Emissions- und Beschriftungsgrößenregler gelten ebenfalls. Halo-Einstellungen betreffen vorerst den eingebauten H/V-/Hl-Renderer; Runenwacht verwendet gerichtete undurchsichtige Kristallflächen ohne zusätzliche Halos oder Punktlichter.

`/enus_assets disable` sperrt neue Paketbauten und Umrüstungen. Bereits ausgewählte installierte Modelle bleiben sichtbar und betriebsfähig. Fehlt das Paket, ist es inkompatibel oder scheitert der Darsteller, behält das Hauptmod die gespeicherte Paketkennung und zeigt eingebaute Signale als Ersatz. Ein fehlerhafter Anbieter wird für die Sitzung aus der Darstellung genommen; der Einstellungsstatus nennt den Fehler. Nach der Korrektur neu starten. Erst die ausdrückliche Rückumrüstung entfernt die gespeicherte Paketwahl. Ein fehlendes Modell erzeugt keine Fahrerlaubnis.

Vor dem Test einen vollständigen Spielstand und die Konfiguration sichern. Versionen vor 0.7.63 erhalten die neuen Paket-/Lizenzfelder beim erneuten Schreiben nicht; für eine Rückkehr die Sicherung verwenden.

## Modelle, Farben und Animationen

Eigene prozedurale Geometrie: geschmiedeter Mast, Leiter, Kupferleitung, Fundament, Halteklauen, Keramikisolatoren, genietete Ringe, Runeneinlagen, facettierte Kristalle sowie geschlossene Rückkappen und Seiten-/Dachblenden. Das ist die erste 3D-Umsetzung des Entwurfs. Gebackene Texturen, LOD, Video, Audio, neue Wagenladungsmodelle und Asset-Streaming sind nicht enthalten. Optik, Tag-/Nachtsichtbarkeit, schräge Blickwinkel, Belichtung, Leistung und Lichtraum bleiben im Spiel zu prüfen.

Jeder Kristallkanal besitzt **eine feste Farbe**. Sämtliche Kristallgeometrien bleiben eingebaut; nur die Leuchtstärke wird angesteuert. Am Hauptkopf sitzen getrennte rote, grüne und bernsteinfarbene Kerne, rote diagonale Randlichtleiter und ein kleiner weißer Melder. Damit wird der frühere einzelne farbwechselnde Kern bewusst durch physisch getrennte Kristalle ersetzt. Das Vorsignal besitzt drei unterschiedlich hohe Spitzen (zwei bernsteinfarben, eine grün), einen dauerhaft verbauten roten Erwartungsbalken und einen weißen Melder. Das Rangiersignal verwendet roten Wartebalken, violetten Freigabekristall und weißen Melder.

| Kopf | Halt / Halt erwarten | Fahrt / Fahrt erwarten | Langsamfahrt / Langsamfahrt erwarten | Rangierfreigabe |
|---|---|---|---|---|
| Hauptsignal | Roter Kern und Randlichtleiter | Grüner Kern | Bernsteinfarbener Kern | Hauptkopf bleibt rot |
| Vorsignal | Roter Grundbalken | Hohe grüne Spitze | Bernsteinfarbene Spitzen | Am kombinierten Mast dunkel |
| Rangiersignal | Roter Balken | Keine eigene Zugfahrerlaubnis | Keine eigene Zugfahrerlaubnis | Violetter Kristall |

Das Hauptmod liefert weiter den vorhandenen `SignalLampPlan`: Bei Halt bleibt das Vorsignal am Mast dunkel; bei Sh 1 bleibt der Hauptkopf rot und nur der Rangierkopf zeigt Freigabe. Dunkel löscht alle Kanäle. Ungültig wird restriktiv rot dargestellt (angehängter Rangierkopf dunkel); Kennlicht verwendet den jeweiligen weißen Kanal. Unbekannte Zustände führen nicht zur Freigabe. Ein Vorsignal kündigt an und erteilt selbst keine Fahrerlaubnis.

Eine Freigabe blendet über 0,24 Sekunden ein. Vorheriges Rot blendet vor dem neuen Freigabekristall aus. Neues restriktives Rot unterdrückt vorherige Freigabelichter sofort; sicherheitsrelevantes Erlöschen erfolgt ebenfalls sofort. Dezentes Schimmern verändert nur die Helligkeit um ±2,5 %, ohne Blinken oder Farbwechsel. Rot bleibt konstant. Rückkappen, Blenden und undurchsichtige Materialien begrenzen die Erkennbarkeit von hinten; die tatsächliche Wirkung muss noch mit Spielkameras bei Tag und Nacht geprüft werden.

## Noch im Spiel zu prüfen

1. Hauptmod allein mit alter Anlage laden: kein Paketzwang und keine automatische Umrüstung.
2. Beide Pakete laden: zunächst AUS, Freischaltung nur als Host, Lizenz nur für diese Welt. Clients dürfen weder Lizenz erteilen noch bauen/umrüsten.
3. Alle drei Bauarten, beide Richtungen und kombinierte Köpfe platzieren; Bodenunterbau, Anklickflächen, Beschriftung/Größe, Materialverbrauch und Baustellendarstellung prüfen.
4. Hp 0 / Hp 1 / Hp 2 / Sh 1, Vorsignalverknüpfung, Ungültig, Kennlicht und Dunkel prüfen; Übergänge, Vorder-/Rückansicht und Helligkeitsregler beobachten.
5. Speichern/laden, verschieben und zurückrüsten: Lizenz/Modell bleiben, Aktivierung ist wieder AUS, IDs bleiben stabil, fahrstraßengesperrte Signale lassen sich nicht umrüsten.
6. Optionales Paket in einer Testweltkopie entfernen: sichtbare Standardsignale und unveränderter Betrieb. Ohne Umrüstung wieder installieren; Entladen/Neuladen und Ressourcenfreigabe prüfen.
7. Mit Host/Client, späterem Beitritt, unterschiedlichen Blickrichtungen und fehlenden/falschen Paketversionen wiederholen; mehrere Signale auf Leistung und Speicher prüfen.

Diese Spielprüfungen sind noch offen. Einstellungen bleiben am Zahnrad des Auswahlfensters; es gibt keinen neuen Spiel-Einstellungsreiter.
