# Signalmaterial am Interchange einkaufen — 0.7.62

[English](INTERCHANGE-0.7.62.en.md)

Testfassung für **Railroader 0.6.3**. Manuell installieren und das gesamte Spiel neu starten; Host und Mitspieler benötigen dieselbe **0.7.62**. Wagenauswahl, Lager, Bestände und Anlagenkennungen bleiben erhalten.

## Warum der Einkauf in 0.7.61 fehlte

Die beiden Frachtdefinitionen machten passende Wagen auswählbar und ergänzten die MOW-Lager. Die nativen Lieferanten vom Typ **InterchangedIndustryLoader** fehlten. Die Fahrzeugseite nennt unter Captive Service mögliche Frachten; dadurch wird noch keine Fracht gekauft.

0.7.62 ergänzt vor dem nativen Aufbau einer Interchange-Industrie zwei Lieferanten unter **EnusDE Signal & Interlocking**. Sie verwenden deren originale Übergabegleise. Die native Fabrik registriert die Kennungen; Zielauswahl, Bezahlung, Kapazität, Bedienung und Rücklieferung übernimmt das Spiel. Kein Sofortfüllen und kein zweiter Laden. Vorhandene Lieferanten und zwischengespeicherte Kartendefinitionen bleiben unverändert. Industrien ohne Übergabe, mit mehreren mehrdeutigen Übergaben oder unbrauchbaren Gleisreferenzen werden nicht ergänzt.

## Welche Bedienung du brauchst

1. Über die vorhandene Fahrzeugauswahl einen eigenen passenden Materialwagen besorgen. **FM/FL** trägt Signalbaumaterial, **XM** trägt Signaltechnik. Für den ersten sichtbaren Test einen normalen **FM-Flachwagen** verwenden, keinen Holz-Skelettwagen.
2. Das normale Fracht-/Zielmenü des Wagens öffnen. **Kaufen / Buy** an einem freigeschalteten, erreichbaren Interchange wählen; unter **EnusDE Signal & Interlocking** die gewünschte Signalfracht auswählen. Das ist ein anderes Ziel als das Abholen bereits gelagerter Materialien am MOW-Lager. Die genaue native Beschriftung folgt der Spielsprache.
3. Den Wagen zur gewählten Übergabe bringen und deren normale Bedienung abwarten. Die Übergabe bestellt/befüllt ihn über ihren Außenweltbetrieb und plant seine Rückkehr. **Ein Klick auf das Ziel belädt den Wagen nicht sofort.** Normale Bedienzeiten, Geld- und Kapazitätsbedingungen einschließlich nativer Abfertigungsgebühr gelten.
4. Nach Rücklieferung Fracht und Menge im Wagenfenster prüfen. Am MOW-Lager entladen/lagern/wiederbeladen oder beladen an einen aktiven MOW-Arbeitswagen kuppeln und bauen. Der Bauradius bleibt **250 Gleismeter**.

Du brauchst **keinen Chat- oder Konsolenbefehl**: **Kaufen → Übergabebedienung/Rücklieferung → Entladen oder Bauen**. Andere Fracht wird nicht geändert, kein Frachtauftrag umgangen und keine Übergabe vorzeitig ausgelöst.

## Welche Modelle und Texturen zu erwarten sind

Eigene Signalbau-Ladungsmodelle oder -Texturen werden noch nicht mitgeliefert. Die geprüfte native FM-Definition besitzt ein `LoadModelVolume`; das generische Spielmodell `rr/loadmodel-crate01` akzeptiert Frachten über einen Platzhalter und enthält Kistenmodelle. Auf einem passenden FM mit tatsächlich positiver Ladung sind daher **generische Kisten zu erwarten**. Das endgültige Aussehen muss noch im Spiel geprüft werden.

Der geprüfte FL-Skelettwagen besitzt nur ein spezielles Holzladungsmodell. Technisch mögliche Signalfracht erzeugt dort deshalb nicht automatisch eine Kistendarstellung. Der geprüfte geschlossene XM-Wagen besitzt keine außen sichtbare Ladung. Unverändertes Aussehen bedeutet nicht automatisch leer: maßgeblich ist die native Mengenanzeige. Andere Wagen-/Moddefinitionen können abweichen. Spielmodelle und -texturen bleiben im installierten Spiel und werden nicht mit der Mod verteilt.

## Prüfstand und noch offene Spieltests

Der Code kompiliert gegen die lokalen 0.6.3-Bibliotheken. Offline geprüft sind produktive DTO-Vorbereitung und echter Prefix, beide Lieferanten, originale Gleis-/Wagenfilter-/Buchungsverträge, unveränderte Ursprungsdaten, Wiederholung, bestehende Lieferanten, Kennungskonflikte sowie mehrdeutige/leere Übergaben. Native Kaufen-/Bedien-/Rücklieferungslogik und die Kistenersatzdarstellung wurden statisch gelesen. Der Assistent hat kein Spiel gestartet.

Du hast die vorherige Bauleiste und Wagenauswahl als soweit funktionierend bestätigt. Tatsächlicher Einkauf, Abbuchung, Hin-/Rücklieferung, Ladungsdarstellung, MOW-Entladung und Speicherung/Laden müssen noch geprüft werden, möglichst mit Spielstandkopie. Im Mehrspieler auch passende Clientversion und späteren Beitritt testen. Die Lieferanten werden beim Laden der Karte mit stabilen Kennungen wiederhergestellt; alte Anlagenkennungen und Bestände werden nicht migriert.
