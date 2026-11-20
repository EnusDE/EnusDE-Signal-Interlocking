# 0.7.24-P0 – ESTW-Verwaltung, Erkennung und Automatik-Kontakte

Lokaler Teststand für Railroader 0.6.2. Kein neuer Spielstand erforderlich. Host und Clients benötigen dieselbe Version. Die bisherigen Pakete 0.7.22-P0 und 0.7.23-P0 bleiben unverändert. Keine Veröffentlichung, Installation oder vollständige Spielabnahme durch Codex.

## ESTW und vorhandene Testwelt

- Im Editor bietet die Stellwerksauswahl getrennte Aktionen für neue örtliche und neue übergeordnete Stellwerke. „Verwalten“ öffnet ein eigenes Fenster zum Umbenennen, Öffnen oder Entfernen einer Tafel.
- Weitere ESTW sind Ansichten derselben Anlage. Sie besitzen eigene Zeichnungen und Modulpositionen; Weichen, Signale und Verschlüsse werden dadurch nicht dupliziert. Bestehende Stellwerkskennungen bleiben erhalten.
- Entfernen einer örtlichen Tafel entfernt auch ihre Module, Automatikpause und nach ausdrücklichem Hinweis angebundene Streckendefinitionen. Zugehörige Erlaubnisfelder werden gelöst. Weltobjekte bleiben stehen. Weichenzuordnungen gehen an das Standard-ESTW zurück.
- Das Standard-ESTW kann ebenfalls entfernt werden: Eine vorhandene andere ESTW-Ansicht wird Standard; andernfalls entsteht eine leere Ersatzansicht. Die gemeinsame Anlage bleibt bestehen.
- Vor dem Entfernen müssen alle aktiven Fahrstraßen und Richtungsbindungen aufgelöst sein. Vor der Änderung wird eine externe Anlagensicherung angefordert. Eine nicht mögliche Sicherung verhindert die Änderung.

## Automatische Erkennung

**Editor → Stellwerksauswahl → Automatische Erkennung**; ebenso im allgemeinen Menü einer geöffneten Tafel. Ziel-ESTW auswählen.

1. „Netz erfassen / Abschnitte aktualisieren“ liest die vorhandenen Gleise und Weichen sowie Signalgrenzen ein. Bestehende Zeichnungen bleiben erhalten.
2. „Neues Gleisbild vorschlagen“ erzeugt einen Zeichnungsvorschlag mit einer getrennten schreibgeschützten Vorschau. Erst „Gleisbildvorschlag übernehmen“ ersetzt die gewählte ESTW-Zeichnung.
3. Die bisherige Zeichnung samt Modulen bleibt dabei als im Fahrdienstleiter ausgeblendete Archivansicht erhalten. Örtliche Tafeln, Weltobjekte und deren IDs bleiben erhalten.

Änderungen an Welt, Anlagenkonfiguration, Gleisnetz oder Signalrichtung machen den Vorschlag ungültig. Fehlende Zeichenverbindungen und nicht darstellbare Signalstellen werden gemeldet; ein unvollständiger Vorschlag kann die Tafel nicht ersetzen. Sehr nahe beziehungsweise überlagerte Signale werden nicht still gegenseitig überschrieben.

Die Erkennung aktiviert **keine fehlende Gleisfreimeldung**, erteilt keine Fahrtfreigabe und ordnet handgezeichnete, noch nicht verknüpfte Platzhalter nicht anhand bloßer optischer Nähe zu. A/B/C-Neukonzeption, vollständig automatische Bahnhofseinrichtung und fahrstraßenabhängige Mehrfachbindung von Vorsignalen bleiben spätere Schritte.

## Company-Einstellungen

Neuer Ort: **Company → Optionen → Mods → Signaltechnik**. In den bisherigen Spieloptionen bleibt ein Umzugshinweis mit Öffnen-Schaltfläche; falls die neue Integration nicht geladen werden kann, bleibt dort die vollständige bisherige Einstellungsseite als Rückfallweg verfügbar.

Der Host kann innerhalb der Mods-Seite zur ursprünglichen Verwaltung der installierten Mods wechseln. Clients erhalten hier ihre Signaltechnik-Einstellungen, keinen Zugriff auf die native Modverwaltung des Hosts. Einstellungen und bestehende Werte werden weiterverwendet. Neue Bedienungstexte sind auf Englisch und Deutsch enthalten.

## Elektrische Weichen und native KI

Als elektrisch eingerichtete Weichen sind gegen Umstellbefehle der nativen Wegpunkt-KI geschützt, auch bei ausgeschalteter Blockabstimmung oder ausgeschalteter zusätzlicher Weichenschutzoption. Das gilt unabhängig davon, ob die KI am Halt zeigenden Signal steht oder ihre Geschwindigkeit geändert wird.

TF- und Stellwerksbedienung behalten ihre normalen Verschlussprüfungen. Ursprüngliche CTC-Regeln werden nicht umgeschrieben. Normale Handweichen erhalten durch die elektrische Schutzregel keine neue Sperre; die bisherige zusätzliche Schutzoption für zugeordnete Weichen bleibt erhalten. Das elektrische Modell ist damit nicht mehr ausschließlich eine optische Einstellung.

## Automatik-Kontakte / experimenteller Achstest

- Ein Klick auf den Kontakt öffnet nun seine Automatikseite, statt in der Stellwerksauswahl zu landen. Die anklickbare Fläche wurde vergrößert; ein aktiviertes Display ist ebenfalls anklickbar.
- Optionales Display am Kontakt: beobachtete Achspassagen; bei ausgeschaltetem Achstest oder fehlenden frischen Daten „—“. Keine bestätigte Achszählanlage oder Freimeldegrundlage. Modelle mit nicht lesbaren Achspunkten können unvollständige Werte liefern; Werte werden nicht als wiederherstellbare Freimeldung gespeichert.
- Im Kontaktmenü Startsignal, Richtung, Aktivierung und Display wählen und speichern. Anschließend führt „Startsignal-Automatik einrichten“ zu dessen vorrangigem und alternativem Ziel.
- Ein Kontakt darf nur das erste Hauptsignal in seiner Richtung anfordern. Ein weiter entferntes Ziel wird in Teilfahrstraßen an jedem Zwischenhauptsignal aufgeteilt, auch im bisherigen Katalogmodus. Die normale Prüfung von Belegung, Weichen, Flankenschutz, Blockrichtung und Annahme bleibt erforderlich.
- Der aktuelle Wegpunkt begrenzt die angeforderte Folge. Endet er am nächsten Hauptsignal, wird ab diesem Signal nicht weiter durchgestellt. Ein neuer Wegpunkt löst am alten Kontakt keine dauerhafte neue Folge aus; dafür ist ein erneuter Auslöser oder eine manuelle Anforderung erforderlich.
- Personenzugspezifische Bedingungen und eine vollständige selbstständige Fahrplan-KI sind nicht Teil dieses Grundschritts.

## Gezielte Spieltests

`Test.ps1`: **978 Prüfungen bestanden, 0 fehlgeschlagen**, einschließlich Build, Sprachkatalogen, nativen API-Zielen, Company- und Interaktions-Fixtures sowie bestehender Sicherungs-, Fahrstraßen- und Verschlussprüfungen. Vollständiges lokales Protokoll: `build/checks/0.7.24-tests.log`.

Die kurzen Anweisungen stehen im Paket unter [Guides/TEST-0.7.24.de.md](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.24.de.md). Insbesondere native Klickauswahl, Modelldisplay, KI-Verhalten und Mehrspieler müssen weiterhin im Spiel geprüft werden. Erfolgreiches Kompilieren und Offline-Prüfungen ersetzen diese Abnahme nicht.
