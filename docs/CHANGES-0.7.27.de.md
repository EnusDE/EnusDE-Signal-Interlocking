# 0.7.27-P0 – zugängliche Hauptsignalrollen mit Erklärungen

Die vorhandenen Rollenwerte werden beibehalten: ohne besondere Funktion, Einfahr-, Ausfahr-, Zwischen- und Blocksignal. Keine neue Datenmigration oder Umnummerierung.

Die Auswahl samt Erklärung ist jetzt direkt am Hauptsignal je Mastrichtung und in den Eigenschaften des zugeordneten Gleisbildfelds erreichbar. Eine separate Übernahme ändert ausschließlich die Rolle; Zielautomatik, Ziele und die andere Mastrichtung bleiben erhalten. Die bisherige Automatikseite zeigt dieselben Erklärungen.

Ein-/Ausfahrrollen beeinflussen bereits die vorhandene Blockabstimmung als Bahnhofsgrenzen. Änderungen verlangen gelöste Fahrstraßen und Richtungsbindungen. Unpassende Rollen an aktiven freien Strecken werden vor dem Speichern abgewiesen. Veraltete Welt-/Feld-/Mastrichtungsaktionen werden ebenfalls abgefangen.

Enthält [alle Änderungen von 0.7.26](CHANGES-0.7.26.de.md). Beide älteren ZIP-Pakete bleiben unverändert. Spieltests und die bisherigen KI-Halteprüfungen sind weiter offen.

[Deutsche Bedienung und Spieltests](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.27.de.md) · [English setup and checks](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.27.en.md). Prüfprotokoll: `build/checks/0.7.27-tests.log`.

Offline-Ergebnis: **1037 Prüfungen bestanden, 0 fehlgeschlagen**. Großer Netzprüffall: 1901 Segmente, 554 Gleisketten vollständig gezeichnet.
