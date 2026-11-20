# 0.7.20-P0 – Richtige Standardseite der Gleisbildsignale

Testpaket für Railroader 0.6.2, aufbauend auf 0.7.19-P0. Host und Mitspieler benötigen dieselbe Version. Die bisherigen ZIPs bleiben unverändert.

- Bei Fahrtrichtung nach rechts steht der Signalkopf jetzt standardmäßig **unter dem Gleis**; die Beschriftung steht **darüber**.
- Die Standardseite folgt der rechten Seite in Fahrtrichtung auch bei gedrehten Feldern. Bei Fahrt nach links sitzt der Kopf daher oberhalb, die Kennung unterhalb des Gleises.
- Haupt-, Rangier-, Vor- und Hilfssignalsymbole verwenden denselben seitlichen Versatz. Die Anordnung gemeinsamer Haupt-/Rangierfelder bleibt zusammenhängend.
- Spiegeln wählt weiterhin die gegenüberliegende Gleisseite. Bereits gespiegelte Felder erscheinen entsprechend der korrigierten Grundausrichtung; bei früheren manuellen Ausgleichskorrekturen gegebenenfalls erneut spiegeln.
- Kennung, GFM-Beschriftung und GFM-Kreuz bleiben in getrennten Bereichen des Felds.

Die Änderung betrifft die Darstellung im Gleisbild. Fahrtrichtung, Signalzuordnungen und Fahrstraßen werden nicht umgeschrieben. Die Blocklogik hat gegenüber 0.7.19 keine Erweiterung erhalten; die vollständige Gleiswechselbetriebs-Bedienung bleibt offen.

Die vorhandenen Offline-Prüfungen wurden um die konkrete Standardanordnung und die rechte Gleisseite in allen vier Richtungen ergänzt. Die Darstellung ist noch im Spiel abzunehmen.

Alle Funktionen aus 0.7.19 sind enthalten. Siehe [vorherige Änderungen](CHANGES-0.7.19.de.md) und [Testablauf mit zwei Bahnhöfen](TEST-0.7.19.de.md).
