# Runenwacht 0.2.1 — Sichtbarkeit und befestigte Bauteile

Benötigt das vorhandene Hauptmod **0.7.64** und Railroader **0.6.3**. Dieses Update ändert nur das optionale Runenwacht-Paket; Hauptmod-Produktionsquellen und kompilierte Hauptmod-DLL bleiben unverändert. Das alte optionale Paket wie üblich ersetzen und das Spiel neu starten. Host und Clients benötigen passende identische Paketversionen. Bestehende Signal-IDs, Positionen, Kopfkonfigurationen, Weltlizenzen und die Testansteuerung bleiben erhalten.

## Leuchten aus der Nähe und Entfernung

Inaktive Kristalle besitzen jetzt dunkle, nicht selbst leuchtende reflektierende Oberflächen. Nur für den aktiven festen Farbkanal wird Körper-/Kanten-/Aderemission aus dem vorhandenen animierten Pegel und der lokalen Helligkeit berechnet. Nah an der Kamera bleibt das Körperleuchten zurückhaltend, damit Schliffflächen erkennbar bleiben. Zwischen 12 und 80 Metern nimmt es weich zu und liefert eine größere Leuchtfläche; oberhalb von 80 Metern bleibt die Verstärkung ohne Entfernungsabschaltung konstant. Die vorher allein leuchtenden feinen Kanten wurden aus der Ferne kleiner als ein Bildpunkt. Körpermaterialien sind je Kopf getrennt, damit kein anderes Signal dessen Lichtzustand übernimmt.

Lichtbilder, Fahrerlaubnis, Schaltzeiten, Geometriesichtbarkeit und Kristallfarben ändern sich dadurch nicht. Dunkle/zurückgenommene Kanäle sowie abgeschaltete Emission bleiben in jeder Entfernung ohne Eigenleuchten. Die Rückgehäuse bleiben lichtdicht. Keine kameragerichteten Sprites, zusätzlichen Punktlichter oder stets sichtbaren Halos um die Rückwände ergänzt. Tatsächliche Tag-/Nachtbelichtung, Pixelabdeckung in der Ferne und Rück-/Schrägansicht müssen weiterhin im Spiel geprüft werden; die Zahlenprüfung der Kurve belegt noch keine Lesbarkeit in Railroader.

Der weiße Kennlichtkristall besitzt eine größere, vor seine Halterung gesetzte Leuchtöffnung und zusätzliche Fernverstärkung. Seine feste Fassung reicht bis zum Rahmen und deckt die gesamte leuchtende Projektion von hinten ab; kein frei schwebendes Licht-Sprite ergänzt.

## Feste Anschlüsse

Kennungs-, Namens- und Typenschilder besitzen jetzt zwei feste Tragarme und eine Klemme bis zur tatsächlichen Ebene der Maststreben. Vorder- und Rückseite verwenden unterschiedliche Tragtiefen. Auch die Kopfhalter sind an die Maststreben angeschlossen.

Die Kupferleitungen verlaufen nun durchgehend am Mast, mit gerundeten Bögen, Fassungen an beiden Enden der Keramikisolatoren und Verbindungen zwischen den exakten Kopf-Auslässen und den Mastleitungen. Die kurze Rangierbaugruppe erhält kürzere Isolatoren, damit die Leitungen über dem Steinfundament bleiben. Ein Mittelstützstück verbindet den Sichelträger mit seinem unteren Balken. Das sind sichtbare mechanische Bauteile, keine neue Elektrosimulation.

## Prüfstand / noch im Spiel zu prüfen

Vollständiger **Test.ps1-Lauf: 2.070 PASS, 0 FAIL** am 23.11.2026. Enthalten: endliche/monotone Nah-/Fernverstärkung, dunkle Kanäle in allen Abständen, Animationspegel, ungültige Eingaben, geschlossene außen orientierte Rohrbögen, vordere/hintere Schildtiefe, exakte Kopf-/Mastanschlüsse und gültige produktive Meshes/UVs. Der SHA-Vergleich der Hauptmod-DLL vor/nach dem Lauf ist identisch. Beide Sprachen dokumentiert; keine neuen Hauptmod-Bedienelemente oder Oberflächentexte.

Noch testen: jede aktive Farbe bei ungefähr 3/12/30/80/150 Metern, Tag/Nacht, Helligkeitsregler und Emission AUS; inaktive Kristalle; Rück-/Schrägansichten; seitliche Nahansichten sämtlicher Schildhalter, Keramikanschlüsse, Abzweige und des niedrigen Rangiersignals. Dafür die vorhandene Einzel-Testansteuerung im Host-Baumodus verwenden. Testlichtbilder bleiben lokal und erteilen keine native Fahrerlaubnis. Durch den Assistenten kein Spielstart und keine Installation erfolgt. Es bleiben Testpakete, kein öffentlicher Release.

Referenzquellen: `CrystalAppearance.cs` (reine Sichtbarkeitskurve), `RunenwachtDesign.cs` (Halter/Anschlüsse), `RunenwachtPalette.cs` und `RunenwachtModel.cs`. Nach der Hauptmod-DLL mit `Build.ps1` bauen, Repository-`Test.ps1` ausführen und danach mit `asset-packs/Runenwacht/Package.ps1` nur das optionale ZIP erstellen. Vorhandene Dateinamen werden abgelehnt; vorbereitete alte Archive bleiben erhalten.
