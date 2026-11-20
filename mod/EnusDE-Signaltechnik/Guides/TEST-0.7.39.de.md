# 0.7.39-P0 – gerichteter Haltfall, echte Gleisintervalle und Anlagenprüfung

Interner Teststand für Railroader 0.6.2; Host und Clients benötigen dieselbe Version. Offlineprüfungen ersetzen den Test mit dem konkreten Zug und Spielstand nicht.

## Gemeinsamer Haltfallpunkt und Nummernsprung

Unter **Company → Optionen → Mods → Signaltechnik → Automatisierung und KI → Fahrwegermittlung** steht **Haltfallpunkt hinter dem Signal (m)**. Standard: **5 m**, einstellbar **0–25 m**, anschließend **Fahrwegmodus übernehmen**. Alternativ im Editor über **Menü → Stellwerksfunktionen → Fahrwegmodus einstellen**. Vor Änderungen laufende Fahrstraßen, Richtungsbindungen und wartende Automatikanforderungen regulär auflösen. Das bleibt eine gemeinsame Host-Einstellung.

Der führende Endpunkt des zugeordneten Zugverbands muss den Punkt in der gerichteten Fahrstraße überschreiten. Dann fallen Startsignal beziehungsweise durchfahrene Rangiersignale auf Halt. Dasselbe Ereignis bestätigt die Zugnummernweitergabe. Diese wartet nicht auf Ankunft am Ziel oder auf den Zugschluss. Bei sehr kurzen Fahrwegen wird der Abstand auf höchstens die halbe noch verfügbare Weglänge begrenzt. Für 0 m bleibt eine kleine Positionsschwelle gegen Rundungsrauschen bestehen. Es ist keine Zeitverzögerung.

Die frühere pauschale Regel „irgendein festgelegter Abschnitt belegt → Zug eingefahren“ wird bei gerichteter Fahrzeugverfolgung nicht mehr verwendet. Ein fremder Zug im geschützten Fahrweg oder unbekannte Belegung nimmt die Freigabe weiterhin sofort zurück; dafür wird der Haltfallabstand nicht abgewartet. Zugschluss und Auflösung bleiben getrennt und benötigen weiter ihre eigenen Prüfungen.

## Belegungsanzeige und A/B-Grenzen

Die Mod-Belegungsleser verwenden jetzt Fahrzeug-Endpunkte und deren tatsächliche Gleissegment-/Meterintervalle. Ein Fahrzeug über einer Segmentgrenze kann beide Abschnitte belegen. Ein Fahrzeug auf einem anderen Gleis wird nicht durch bloße Projektion seiner Weltposition mitgezählt. Für Fahrzeuge über mehrere Segmente wird der kurze reale Körperweg geprüft; nicht auflösbare Geometrie bleibt unbekannt. Bei völlig unklarer Körperlage über mehrere Segmente wird konservativ keine Freimeldung behauptet. Betroffene Fahrzeugkennungen stehen im Diagnoseexport.

Gleisbild, Fahrweg, GFM, Weichenschutz und Flankenleser benutzen dieselbe Intervallgrundlage. Die Belegung bleibt bestehen, bis das Fahrzeug den Abschnitt tatsächlich verlassen hat; sie wird nicht auf den nächsten Abschnitt verschoben oder verzögert eingefärbt. Ein langer Zug kann mehrere Abschnitte gleichzeitig rot belegen.

Eine zusammenhängende A/B-GFM kann hinter das Startsignal zurückreichen. Für die zusätzliche örtliche Zugfahrwegprüfung wird daraus nur der tatsächlich angeforderte Fahrweganteil gelesen. Eine Belegung hinter dem Start wird dadurch nicht allein wegen derselben langen GFM zur Belegung des freien Weges davor. Die vollständige GFM bleibt als Abschnitt bestehen; ihre Anzeige und eingerichtete freie Strecken-/Richtungsprüfungen bleiben separat. A/B-Kästen werden weder aufgeteilt noch umnummeriert.

## Rangierfahrten

Bekannte Belegung ist auf dem durchfahrenen Rangiergleis auch vor einem späteren Weichenbereich zulässig, sofern der betreffende Teil außerhalb der Weichenschutzbereiche liegt. Das umfasst mehr als nur das letzte Zielgleis. Belegte Weichenschutzbereiche, widersprechende Festlegungen, unbekannte Belegungsdaten und ungültige Zuordnungen bleiben Ablehnungsgründe. Daraus entsteht keine automatische Erlaubnis zum Umstellen einer besetzten Weiche. Die erweiterte Zulassung macht einen frühen Gleisabschnitt nicht zur vorzeitig freizugebenden Zielzone.

## Abschnittmanager und verständliche Meldungen

**Editor → Menü → Abschnittmanager / GFM-Prüfung …** öffnet die vorhandene Grenz-/Abschnittsliste direkt. Herkunft, Länge, Quelle und Gleisbildzuordnung jeder Grenze lassen sich dort prüfen. Anzeigen mit null verknüpften Gleisbildfeldern sind weiterhin als solche gekennzeichnet.

Belegungsablehnungen nennen nach Möglichkeit Start-/Zielsignal, Weichenkennung, zugehöriges GFM-Grenzpaar, natives Gleis und gerundeten Meterbereich. Damit wird beispielsweise aus dem Rohbereich `Sm3i9:19.921890258789063:27.908390045166016` eine lesbare Zuordnung zu **Gleis Sm3i9, 19,92–27,91 m** samt Fahrweg-/GFM-Kontext. Die ursprünglichen präzisen Werte bleiben im Diagnose-ZIP.

## Alle Elemente neu einlesen und prüfen

**Editor → Menü → Anlage aktualisieren und prüfen … → Alle Anlagenelemente aktualisieren** lädt gespeicherte Weltobjekte, Darstellungen, Signal-/Weichenbindungen, Fahrwege und Belegungsleser erneut. Einstellungen und interne IDs bleiben erhalten. Laufende Festlegungen und wartende Automatikaufträge zuvor auflösen; sie werden nicht ungeprüft gelöscht.

Anschließend erscheinen auffällige GFM-Wege, fehlende Signal-/Weichenbindungen, doppelte Nummernquellen sowie ungültige oder veraltete gespeicherte Fahrwege. Einzelne physische Zuordnungen werden dabei nicht geraten. Der separate GFM-Neuscan mit Vorschau und die Werkzeuge von/bis bleiben erreichbar. Laufzeitleser und Kontaktbeobachtungen werden neu initialisiert. Eine Aktualisierung ist kein automatischer Umbau des gespeicherten Gleisbilds.

## Gezielte Prüfung im Spiel

1. Vor dem Start den Wert **5 m** übernehmen. Zug vor HS-044 stehen lassen und den Weg nach HS-036 erneut anfordern. Bei Ablehnung die jetzt lesbare Abschnittszuordnung prüfen und unmittelbar auf dem Host ein Diagnose-ZIP exportieren.
2. Mit dem zugeordneten Zug langsam über das Startsignal fahren. Vor dem Haltfallpunkt keine allein durch Belegung ausgelöste Passage; dahinter Haltfall und Nummernweitergabe gemeinsam. Gegenrichtung ebenfalls prüfen. Anschließend bei Bedarf mit **10 m** wiederholen.
3. Die tatsächliche Besetzung beobachten: mehrere rote Abschnitte bei einem langen Zug sind möglich; jeder Abschnitt wird nach dem Zugschluss wieder frei. Ein Nachbargleis muss getrennt bleiben.
4. Rangierfahrt in einen bekannten besetzten Gleisteil außerhalb der Weichenschutzbereiche anfordern. Gegenprobe mit besetztem Weichenschutz muss abgewiesen werden. Unbekannte Daten dürfen keine bestätigte Freimeldung ergeben.
5. A/B-GFM über den Start hinaus vergleichen: Belegung hinter dem Start darf nicht pauschal einen physisch freien angeforderten Anteil sperren; echte Belegung davor muss sperren. Eingerichtete Strecken-/Erlaubnisverbindungen behalten ihre eigenen Bedingungen.
6. Nach regulärer Auflösung **Alle Anlagenelemente aktualisieren**, Einstellungen/IDs vergleichen und die aufgelisteten Probleme prüfen. GFM-Korrekturen nur über die geprüften Zuordnungs-/Neuscanwerkzeuge vornehmen.

## Neunummerierung – Vorschlag, noch kein Werkzeug in diesem Paket

Empfohlen ist eine eigene Vorschau mit Alt→Neu-Liste: im Stellwerk nach gezeichneten Spalten von links nach rechts, weltweit nach einer festen Kartenachse (zum Beispiel West→Ost). Nur sichtbare Kennungen ändern; Signalrichtungen, interne IDs und Zuordnungen erhalten. Die vollständig geprüfte neue Namensmenge gemeinsam übernehmen, sodass vorübergehende Ersatznummern nicht in den laufenden Daten auftauchen. Das Werkzeug ist in 0.7.39 nicht implementiert. Eine Neunummerierung korrigiert keine fehlerhaften Gleisverbindungen.

[Diagnoseexport und Synchronisationsordner](TEST-0.7.38.de.md) bleiben wie im vorherigen Teststand. Das Paket ist weder veröffentlicht noch auf deinem Windows-Rechner installiert worden. Der konkrete Belegungsfall benötigt weiterhin die praktische Prüfung und bei verbleibender Ablehnung einen aktuellen Host-Export.
