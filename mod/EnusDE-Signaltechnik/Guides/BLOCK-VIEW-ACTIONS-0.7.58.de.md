# 0.7.58-P0 – Blockansicht bleibt bei laufender Zugnummernweitergabe bedienbar

Für Railroader 0.6.3. Alle Teilnehmer benötigen dieselbe Modversion.

Bisher verglich der Ansichtseditor seine beim Öffnen erfasste Anlagenrevision mit
der laufenden Revision. Auch das Zuweisen oder Weitergeben einer Zugnummer erhöht
diese Revision. Nach einem solchen normalen Betriebsvorgang brachen Einstellungs-,
Vorschau- und Platzierungsaktionen ohne Wirkung ab. Der Entwurf konnte also schon
ohne Änderung an Bau oder Gleisbild unbenutzbar werden.

Die Blockabläufe vergleichen jetzt bauliche Daten gesondert: Weltobjekte,
Material-/Platzierungseinstellungen und ungültig gesetzte Signale bleiben geschützt.
Laufende Zugnummernzuordnungen und Nummernfelder machen den Entwurf nicht ungültig.
Beim Speichern von Weltobjekten wird weiterhin vom aktuellen Anlagenstand
ausgehend gearbeitet; aktuelle Zugnummern bleiben erhalten. Tatsächliche
Konfigurationsänderungen, bauliche Änderungen, Weltwechsel und ein ersetzter
Bauvorschlag sperren den alten Vorschlag weiterhin.

Das Fenster zeigt dauerhaft Status, Fehler und nächste Handlung an. Übernehmen
bleibt sichtbar. Fehlt eine Vorschau, verlangt die Taste deren Vorbereitung und
speichert nichts. Vorschau- und Speicherfehler erscheinen im selben Fenster,
nicht nur im Haupteditor. Wiederholte Steuerelement-Ereignisse mit unverändertem
Wert entwerten keine Aktionen; alte Rückrufe einer früheren Auswahl bleiben gesperrt.
Beim erneuten Öffnen wird die aktuelle gemeinsam gespeicherte Konfiguration gelesen.
Vorübergehend fehlende Anlagendaten werden erklärt; nach Rückkehr kann erneut
versucht werden.

Ablauf: Einstellungen wählen → Ansicht als Vorschau vorbereiten → bei Bedarf
platzieren → Diese Ansicht übernehmen. Bei einem noch nicht bestätigten Bauvorschlag
wird die Ansicht nur vorgemerkt und die Platzierung wieder geöffnet. Der Bau benötigt
weiterhin seine abschließende Bestätigung.

## Prüfung

Ein neuer Test kompiliert die produktive BlockViewWindow.cs mit UI-Adaptern und
führt deren echte Rückrufe mit verzögertem Fensterneuaufbau aus. Geprüft werden
Zugnummernverkehr zwischen Einstellung, Vorschau, Platzierung und Speichern,
unveränderte Ereignisse, alte Rückrufe, fehlende Vorschau, echte Änderungen,
vorübergehende Bereitschaft, sichtbare Fachfehler sowie die Rückkehr zum Bauvorschlag
ohne vorzeitiges Speichern. Reine Datenprüfungen sichern außerdem reale
Signalbewegung/-richtung/-entfernung und Bau-/Materialeinstellungen ab und prüfen,
dass aktuelle Nummern beim Übernehmen von Weltobjekten erhalten bleiben.

Das ist ein Test ohne laufende Spieloberfläche, kein tatsächlicher Maus-/Layouttest
im Spiel. Zusätzlich werden sämtliche Projekt-, Sprach- und Regressionstests
ausgeführt. Spieltest mit UI-Skalierung und Mehrspielerabnahme stehen aus. Lokal wird
mit verfügbaren älteren Spielbibliotheken und dem bestehenden 0.6.3-Pfadadapter
gebaut, nicht mit Original-0.6.3-DLLs.
