# 0.7.27-P0 – Hauptsignalrollen direkt einstellen

Enthält sämtliche Änderungen und offenen Spieltests aus [0.7.26](TEST-0.7.26.de.md): einfachere ESTW-Modulansicht, direkte Weichengrenzen, begrenzte Bahnhofssuche, Aktualisierung gebundener Signalfelder und matte C-Pfeile. Bestehende Testwelt weiterverwenden, Paket ersetzen und Spiel neu starten; Host und Clients benötigen dieselbe Version.

## Wo finde ich die Rolle?

- **In der Welt:** Hauptsignal öffnen → Richtung A oder B → **Betriebliche Hauptsignalrolle**. Die Richtung muss aktiviert und gespeichert sein. Niedrige und hohe Hauptsignale verwenden dieselbe Auswahl.
- **Im Gleisbild:** Rechtsklick auf ein zugeordnetes Hauptsignal → **Eigenschaften und Zuordnung** → **Betriebliche Hauptsignalrolle**.
- Weiterhin verfügbar unter **Automatisierung und KI → Signalrollen und Zielautomatik**.

Rolle wählen und **Signalrolle übernehmen** klicken. Die Rollenstatuszeile zeigt den gespeicherten Wert oder einen Fehler. Vorder- und Rückrichtung eines Masts können unterschiedliche Rollen haben.

| Rolle | Bedeutung |
| --- | --- |
| Einfahrsignal | Regelt die Einfahrt von der freien Strecke in den Bahnhof. |
| Zwischensignal | Regelt Zugfahrten zwischen Bahnhofsteilen. |
| Ausfahrsignal | Regelt die Ausfahrt aus dem Bahnhof auf die freie Strecke. |
| Blocksignal | Hauptsignal für einen Blockabschnitt auf freier Strecke. |
| Ohne besondere Funktion | Keine besondere betriebliche Rolle zugeordnet. |

Ein- und Ausfahrrollen werden von der bestehenden Blockabstimmung als Bahnhofsgrenzen ausgewertet. Zwischen- und Blocksignale setzen dort keine äußere Bahnhofsgrenze. Eine Rolle allein stellt weder eine Fahrstraße noch eine freie Strecke/GWB-Verbindung her. Zielautomatik, Ziele, Fahrtbegriffe und Signal-ID werden dadurch nicht geändert.

Für Rollenänderungen Fahrstraßen und Richtungsbindungen auflösen und wartende Automatikanforderungen zurücknehmen. Ein Endpunktsignal einer aktiven freien Strecke muss seine passende Ein-/Ausfahrrolle behalten; zuerst die Streckeneinrichtung ändern, wenn eine andere Rolle benötigt wird.

## Kurzer Spieltest

1. Am zugeordneten Hauptsignal Einfahr-, Zwischen-, Ausfahr- und Blockrolle auswählen, jeweils übernehmen und neu öffnen. Kennung und GUID bleiben gleich.
2. Am beidseitigen Mast unterschiedliche Rollen für A und B speichern. Gegenseite und vorhandene Zielautomatik dürfen sich nicht mitändern.
3. Rolle im Weltmenü ändern, dann im Gleisbild und der Automatikseite kontrollieren.

Offline-Prüfungen ersetzen diese Spieltests nicht. Die bisherigen offenen KI-Haltetests bleiben bestehen.
