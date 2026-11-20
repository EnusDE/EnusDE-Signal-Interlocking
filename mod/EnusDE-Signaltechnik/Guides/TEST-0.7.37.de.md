# 0.7.37-P0 – Bahnhofsfahrt und manuelle GFM-Welteinrichtung

Teststand für Railroader 0.6.2. Host und Clients benötigen dieselbe Version. Bestehende Testwelt verwenden. Die erste öffentliche Version besitzt diese neuen Werkzeuge nicht; die folgende Anleitung gilt ausschließlich für diesen Teststand.

## Warum eine Bahnhofsfahrt als Blockkette abgewiesen wurde

Die bisherige zusätzliche Richtungsabstimmung verband GFM-Abschnitte zu internen **BL-…-Ketten**. Diese sind nicht identisch mit einer vom Spieler eingerichteten Blockstelle oder Freie-Strecke-Verbindung. Auch gewöhnliche Zugfahrten innerhalb eines Bahnhofs wurden dagegen geprüft. Ein Zug hinter dem Startsignal konnte dadurch eine weiterreichende Kette belegen und die eigentlich freie Weiterfahrt verhindern.

Örtliche Zug- und Rangierfahrten zwischen Signalen/Hilfszielen desselben Ortsstellwerks verwenden jetzt die örtliche Fahrstraßenprüfung. Eine innere Ein-/Ausfahrgrenze oder eine eingerichtete freie Strecke wird dabei nicht als rein örtlich behandelt. Die tatsächliche Fahrwegbelegung, Weichenlage, Flankenschutz und vorhandene Verschlüsse bleiben wirksam. Gültige GFM-Abschnitte, die den angeforderten Zugfahrweg unmittelbar überwachen, werden weiterhin gelesen; die Meldung nennt bei Belegung oder fehlenden Daten deren Grenzpaar. Andere Abschnitte hinter dem Startsignal werden nicht allein aufgrund der übergeordneten Richtungskette mitgeprüft.

## Unüberwachte Bereiche bei manueller Bedienung

Unter **Company → Optionen → Mods → Signaltechnik → Automatisierung und KI → Fahrwegermittlung** gibt es **Manuelle Fahrstraßen bei unvollständiger GFM zulassen**. In diesem Teststand standardmäßig aktiv, auch beim Laden älterer Einstellungen. Unüberwachte Bereiche muss der Fahrdienstleiter prüfen. Sie bleiben im Gleisbild unüberwacht und werden nicht als bestätigte Freimeldung dargestellt. Bei einer solchen Anforderung erscheint ein Hinweis und ein Diagnoseereignis mit den fehlenden Intervallen.

Diese Option beseitigt die Pflicht zu vollständiger eingerichteter GFM als zusätzliche Voraussetzung manueller Anforderungen. Sie deaktiviert keine vorhandenen Belegungs-, Weichen-, Flanken- oder Verschlussprüfungen. Ein gültiger belegter/unlesbarer GFM-Abschnitt im Fahrweg bleibt ein Ablehnungsgrund. Für Kontakt-Automatik gilt weiterhin die vollständige GFM-Abdeckung. Eingerichtete Strecken-/Erlaubnisverbindungen behalten ihre eigenen Prüfungen. Zum Umstellen des Betriebsmodus müssen die laufenden Festlegungen zuvor aufgelöst werden.

**Gespeicherte Fahrstraßen planen** ist im Editor-Menü wieder unabhängig vom aktuell gewählten Fahrwegmodus erreichbar. **Fahrwege bei Anforderung berechnen** ist als experimentell bezeichnet. Zum bisherigen gespeicherten Ablauf diese Option ausschalten und **Fahrwegmodus übernehmen** verwenden. Vorhandene Fahrstraßen werden nicht gelöscht.

## Meldung „GFM-/Bahnhofsgrenzen geändert“

Die Kennung der Richtungsblocktopologie berücksichtigt jetzt Gleisverbindungen, aktive GFM-Grenzen, Abschnittswege und Ein-/Ausfahrgrenzen. Allgemeine Anlagenversionszähler, reine Bezeichnungen, Darstellungsabstände und unbeteiligte Signale ohne GFM erzeugen keine neue Richtungsblocktopologie mehr. Tatsächlich geänderte Grenzen bleiben relevant. Vorhandene Richtungsbindungen werden nicht ungeprüft gelöscht; eine echte Änderung kann weiterhin Freifahren und Freiprüfung erfordern.

Die Standard-Weichenschutzbereiche werden aus den realen Gleisarmen und vorhandenen physischen Meldern abgeleitet. Ein bloßes Signal ohne GFM darf sie nicht mehr verkürzen. Alte erzeugte Weichen-B-Grenzen werden nicht selbst wieder als Grundlage ihrer Lage verwendet. Damit lässt sich eine falsch verkürzte Grenze durch erneutes Erkennen berichtigen, statt den alten Fehler fortzuschreiben.

## Werkzeug 1: GFM nur aufheben

Im **Editor → Menü → GFM in diesem Gleisbild erkennen …** den gewünschten Umfang wählen. Für die gesamte Testanlage **Alle Stellwerke einbeziehen** aktivieren. **Nur GFM-Zuordnungen aufheben · Vorschau** und danach **GFM-Zuordnungen laut Vorschau aufheben** verwenden.

Vorher alle Fahrstraßen/Richtungsbindungen auflösen und wartende Automatikaufträge zurücknehmen. Vor Übernahme wird eine Sicherung angelegt. Im gewählten Umfang werden Abschnittsbindungen aufgehoben, nicht anderweitig benötigte GFM-Definitionen entfernt, Weichen-GFM deaktiviert und manuelle Weichen-Grenzverknüpfungen gelöst. Signal-/Weichenfelder, Zeichnung und gebaute Weltobjekte bleiben erhalten. Eine örtliche Bereinigung erhält weiter verwendete Definitionen anderer Tafeln.

Der **automatische GFM-Hintergrundkatalog** wird pausiert; auch ein noch laufender Hintergrundauftrag wird verworfen. Dadurch werden entfernte Einträge nicht gleich wieder angelegt. Die entsprechende experimentelle Option ist ebenfalls unter Fahrwegermittlung erreichbar. Explizite Scans und manuelle Abschnittseinrichtung bleiben möglich. Das ist getrennt von der weiterhin angebotenen vollständigen Neuerkennung mit Vorschau.

## Werkzeug 2: GFM von Weltgrenze bis Weltgrenze

**Editor → Menü → GFM über Weltobjekte einrichten …** öffnen. **Von und bis nacheinander in der Welt auswählen …** wählen und die beiden realen Signal-GFM bzw. Weichenast-Kästen anklicken. Alternativ Anfang und Ende separat auswählen. Escape beendet die jeweilige Auswahl ohne Bau.

**Physischen Abschnitt suchen**, Ergebnis und Länge prüfen, dann **Abschnitt von Grenze bis Grenze speichern**. Der Abschnitt wird physisch gespeichert, auch wenn die bestehende Zeichnung noch keine eindeutige Zuordnung zulässt. Eindeutige Gleisbildverbindungen werden ergänzt; fehlende Zeichnungsanschlüsse anschließend in der GFM-Prüfung bearbeiten. Andere Grenzen und echte Weichen dürfen nicht übersprungen werden. Für lange bereits ermittelte Abschnitte wird zunächst der physische Netzkatalog verwendet; eine unvollständige begrenzte Ersatzsuche wird nicht als vollständiger Vorschlag gespeichert.

## Signal-/GFM-Kasten mit einem Weichenast verbinden

Im selben Werkzeug unter **Signal / GFM einem Weichenast zuordnen** die reale Weiche in der Welt auswählen. **Stammgleis / Spitze**, **Grundast** oder **Abzweigast** bezeichnet den nativen Ast, nicht die gezeichnete Links-/Rechtsseite.

Den Signal-GFM oder einen eigenständigen B-Kasten in der Welt auswählen und **Gemeinsame Weichengrenze übernehmen**. Der Melder muss aktiv auf genau dem nativen Gleissegment dieses Astes liegen. Der überwachte Weichenbereich reicht bis zum gewählten Melder. Bei einem A-Melder am Signal wird ein gemeinsamer A/B-Kasten verwendet; ein B-Kasten bleibt als B gekennzeichnet. C-Grenzen sind einseitig und lassen sich auf diesem Weg nicht zu A/B zusammenfassen. Verbindungen auf andere oder parallele Gleise werden abgewiesen. Mehrsegmentige Weichenast-Verknüpfungen werden in dieser Fassung noch nicht unterstützt.

Anschließend die angrenzenden GFM-Abschnitte von/bis einrichten oder ausdrücklich neu erkennen. **Diese manuelle Weichen-Grenzverknüpfung lösen** entfernt die Zuordnung des gewählten Astes. Verknüpfungen werden mit dem Spielstand übertragen und bleiben bei einer Umbenennung der Weiche erhalten. Entfernte/deaktivierte oder unpassend versetzte Melder erzeugen keine stille Freimeldung; die betroffene Weichen-GFM wird unlesbar. Während eines betroffenen Weichenverschlusses darf der verknüpfte Melder nicht verschoben/umgebaut werden.

## Anklickbare Erkennungsfehler

Die Vorschau enthält bei ungeklärten Abschnittsverbindungen **Prüfen: …**-Schaltflächen. Sie öffnen die GFM-Prüfung für die betreffende Tafel und markieren die bekannten Grenzen, ohne den Vorschlag zu übernehmen. Über **Zur GFM-Erkennung** zurückkehren. Eine Konfigurationsänderung macht alte Vorschläge weiterhin ungültig. Fehlende Zeichnungsanschlüsse werden nicht durch Anklicken erfunden.

## Aufzeichnungen finden

Unter **Company → Optionen → Mods → Signaltechnik → Optionale lokale Diagnose** stehen **Aufzeichnungsordner** und **Diagnosefenster öffnen**. Auch im Diagnosefenster wird der aktuelle Ordner angezeigt.

Windows-Standardpfad:

`%USERPROFILE%\AppData\LocalLow\Giraffe Lab LLC\Railroader\EnusDE-Signaltechnik\Diagnostics\`

Jede Sitzung erhält einen eigenen UTC-Zeitstempelordner. Darin liegen **session.json** und **events-0001.jsonl** bzw. weitere Ereignisteile. Für Betriebsfehler möglichst die Host-Sitzung bereitstellen; Clients besitzen nur ihre lokale Sicht und zeichnen nur bei eingeschalteter Mitspieleraufzeichnung mit. Die **Player.log** liegt direkt im Railroader-LocalLow-Ordner.

## Abnahme in der Testwelt

Zug hinter dem Startsignal stehen lassen, innerhalb desselben Bahnhofs weiterstellen; ein belegtes Gleis außerhalb dieses Fahrwegs darf nicht allein eine BL-Kette blockieren. Danach ein tatsächlich belegtes bzw. unbekanntes eingerichtetes GFM im Fahrweg testen: Ablehnung muss bleiben. Unüberwachte manuelle Fahrt mit Hinweis, Kontaktanforderung mit fehlender GFM, Gegenfahrt auf eingerichteter freier Strecke, echte Grenzänderung und vorhandene Restverschlüsse prüfen.

Reset ohne sofortige Wiedererkennung, nacheinander gewählte Weltgrenzen, gemeinsame A/B-Grenze an jedem Weichenast, falsches Gleis, C-Ablehnung, Umbenennung und Entfernen eines verknüpften Melders testen. Die automatische Erkennung, Anzeige und das Zugverhalten sind ohne tatsächlichen Spielstand nicht praktisch abgenommen. Die bereitgestellte Diagnostics.zip konnte in dieser Arbeitsumgebung wegen eines schreibgeschützten Dateiimports noch nicht eingelesen werden; die beschriebenen Befunde stammen aus Codeprüfung und gezielten Tests.
