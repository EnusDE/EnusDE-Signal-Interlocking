# 0.7.47-P0 – Kompakte Zugnummernübersicht

Testfassung für Railroader 0.6.2. Host und Clients benötigen dieselbe Version
0.7.47. Enthält die Änderungen aus 0.7.46. Vor einem Spieltest die Anlage sichern.
Ältere Testarchive bleiben unverändert.

## Was die Übersicht zeigt

Eine schmale Reihe mit 1–16 Zugnummernfeldern spiegelt bereits vorhandene
Quellenfelder. Die Quelle darf in einem anderen Stellwerk oder in einer selbst
gebauten Tafel liegen. Es entstehen keine weitere Blockanlage, keine neuen
Weltsignale und keine zusätzliche Nummernzuordnung. Das gewählte Zielstellwerk
benötigt für die Übersicht keine vollständige Kopie des Streckengleisbilds.

Beispiel: drei physische Blöcke, zwei mögliche Darstellungen derselben Daten:

    Ausführliche Bedienung:  A ── Block 1 ── Signal ── Block 2 ── Signal ── Block 3 ── B
    Kompakte Anzeige:            [12345]                 [-----]                 [-----]

Die kompakte Anzeige selbst besteht nur aus drei benachbarten Feldern, ohne die
Abstände und Signaltasten der ausführlichen Darstellung. Sie ist schreibgeschützt.
Nummer und Belegungszustand kommen aus der jeweiligen Quelle. Eine fehlende Quelle
wird mit `?` angezeigt; veraltete Nummern werden nicht als aktuelle Werte übernommen.

## Einrichten

1. **Stellwerkseditor → Freie Strecke, Block und GWB** öffnen und die bereits
   gespeicherte Strecke wählen. Neue oder geänderte Streckeneinstellungen zuerst
   getrennt übernehmen; die Übersicht verwendet die gespeicherte Strecke.
2. Zum Abschnitt **Kompakte Zugnummernübersicht** gehen.
3. Bei **Anzeigen auf Stellwerk** beispielsweise VITA oder ELA wählen. Die Auswahl
   bestimmt die Zieltafel, nicht die Zuständigkeit für Signale oder Erlaubnis.
4. **Neue Übersicht anlegen** wählen oder eine vorhandene Übersicht bearbeiten.
5. **Anzahl der Felder** einstellen. **Blockreihenfolge A → B übernehmen** oder
   **B → A** ordnet die vorhandenen Blockquellen von links nach rechts an.
   A und B entsprechen den unter Verbindung eingestellten Nachbarbetriebsstellen.
   Bei weniger Feldern werden die ersten Quellen der gewählten Reihenfolge benutzt.
6. Jede Quelle ist auch einzeln wählbar. Für eine einfeldrige Voranzeige das Feld
   des letzten Blocks vor dem gewünschten Bahnhof auswählen. Die Anzeige spiegelt
   diesen Block unabhängig von der Fahrtrichtung; sie ist keine Richtungsfreigabe.
   Selbst gezeichnete Tafeln können ebenfalls als Quelle dienen. Wenn keine
   eindeutige automatische Quelle vorhanden ist, bleibt die manuelle Auswahl nötig.
7. **Zugnummernübersicht als Vorschau platzieren** öffnen. Die gewünschte linke
   Position in der Zieltafel anklicken: Grün bedeutet frei, Rot bedeutet belegt
   oder außerhalb der Tafel. Ein gültiger Klick speichert die gesamte Leiste.
   **Platzierung abbrechen** verwirft den Vorschlag einschließlich neuer Quell-IDs.
8. Später die Zieltafel im Editor öffnen. Im Auswahlmodus ein Nummernfeld greifen:
   Die ganze Leiste folgt der Maus. Bestehende Felder werden nicht überschrieben.
9. Mit **Nur diese Übersicht entfernen** werden nur die Anzeigefelder entfernt.
   Quellen, Nummern und Blockeinrichtung bleiben erhalten.

Mehrere kompakte Übersichten derselben Strecke sind möglich. Eine ausführliche
Anzeige wird weiterhin separat über die vorhandene Blockeinrichtung ausgewählt;
diese Version baut bestehende ausführliche Ansichten nicht automatisch um.

## Übergabe und Zuständigkeit

Das letzte Streckenfeld bleibt seiner bisherigen Quelle zugeordnet. Eine
Ankunftsvoranzeige darf diese Quelle spiegeln. Die tatsächliche Nummernübergabe in
ein Bahnhofsgleis benötigt weiterhin die bestehende bestätigte Durchfahrt und
korrekt eingerichtete Empfangsfelder/Fahrstraßen. Das bloße Anzeigen einer Nummer
ordnet keinen Zug einem Bahnhofsgleis zu und verändert keine GWB-Erlaubnis.

Ein Neuaufbau der ausführlichen Blockdarstellung oder ein Wechsel ihres bedienenden
Stellwerks erhält die stabilen Quellenverknüpfungen. Wird ein Quellenfeld gelöscht,
zeigt die Übersicht `?`; sie errät keine Ersatzquelle. Quelle dann bewusst neu wählen.

## Prüfstand und noch erforderlicher Spieltest

Automatische Prüfungen decken Quellenreihenfolge, Spiegelung, fehlende/veraltete
Quellen, Gruppenziehen, Kollisionen, Speichern/Laden, manuelle Quellen,
Block-Neuaufbau und Bedienerwechsel ab. Sie ersetzen keinen Spiel-/Mehrspielertest.

Im Spiel prüfen: drei Felder an einem Nachbarstellwerk platzieren, einen Zug durch
die Blöcke fahren lassen, Quellenanzeige und Kopie vergleichen, Gruppe verschieben,
speichern/laden und die bestehende Nummernübergabe am Einfahrsignal beobachten.
Die Platzierungsvorschau wurde gegen die Spielbibliotheken gebaut, hier aber noch
nicht in einer laufenden Unity-Oberfläche visuell abgenommen.

Vorgemerkt für getrennte Schritte: logische Gleisbildfortsetzungen über
Darstellungsumbrüche, schmalere ausführliche Streckengruppen mit integrierten
Nummernfeldern, konfigurierbare Gleisseite automatisch platzierter Signale sowie
kleine Nachbarstellwerksansichten. Diese Änderungen gehören nicht zu 0.7.47.
