# 0.7.28-P0 – gemeinsame GFM-Grenzen A/B

Signal-A und vollständig erkannte Weichen-B-Grenzen werden bei exakt gleicher logischer Position gemeinsam dargestellt: B-Gehäuse, Beschriftung A/B, vorhandene GFM-Nummer. Ein zusätzlicher automatischer Weichenkasten an dieser Stelle wird nicht gezeichnet. Die logischen Signal- und Weichen-IDs bleiben unverändert.

Die Netzerfassung verwendet den physischen Signalanker einmal als kanonische Grenze. Dadurch verwirft sie die Grenzstelle nicht mehr wegen zweier unterschiedlicher IDs. Vorhandene passende Abschnittskennungen und Namen werden unter Berücksichtigung der gemeinsamen Grenzverweise wiederverwendet. Alte Weichengrenzverweise bleiben auflösbar.

Signalmenü und Weichen-GFM erläutern die Mitbenutzung. Der gemeinsame Kasten ist über die Weichen-GFM erreichbar; Grenzanzeigen verwenden GFM-A/B. A/B ist berechnete Darstellung und wird nicht als neue Objektart gespeichert. Bei getrennter Nutzung wird A beziehungsweise B wiederhergestellt.

Keine Zusammenführung allein aufgrund räumlicher Nähe, bei abweichenden Segmenten oder bei mehrdeutigen physischen Grenzen. C bleibt wegen der einseitigen Überwachung separat. Dies ersetzt keine allgemeine Kollisions-/Kabelüberarbeitung für andere Kästen.

Enthält alle Änderungen aus 0.7.27. Ältere ZIP-Pakete bleiben unverändert. [Bedienung und Spieltests Deutsch](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.28.de.md) · [English](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.28.en.md).

Prüfprotokoll: `build/checks/0.7.28-tests.log`. Spiel- und Mehrspielerabnahme stehen aus; die bisherigen KI-Halteprobleme sind nicht als behoben bestätigt.

Offline-Ergebnis: **1052 Prüfungen bestanden, 0 fehlgeschlagen**. Zusätzliche Regressionen prüfen gemeinsame A/B-Grenzen, native Positionspräzision, C-/Mehrdeutigkeitsfälle und den Erhalt bestehender Abschnittskennungen.
