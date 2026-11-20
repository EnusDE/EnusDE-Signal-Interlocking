# 0.7.54-P0 – Mehrere Signale je GWB-Tutorialschritt

Für Railroader 0.6.3. Host und Clients benötigen dieselbe Modversion.

Die Reihenfolge bleibt: Einfahrt A, Ausfahrt A, Einfahrt B, Ausfahrt B.
Innerhalb jeder Rolle lassen sich jetzt mehrere Hauptsignale für dasselbe physische
Streckengleis auswählen.

1. Im aktuellen Schritt alle zu dieser Rolle gehörenden, zugeordneten Signale
   anklicken. Beispiel: beide Ausfahrsignale HS-008 und HS-007 in HG.
2. Jeder Klick ergänzt ein Signal. Alle ausgewählten Felder bleiben gleichzeitig
   grün. Erneutes Anklicken oder die jeweilige Abwählen-Taste entfernt nur dieses Signal.
3. Anzahl und Namen stehen im Tutorial. Die separate Gleisbildauswahl bleibt für
   mehrere Klicks geöffnet; Zurück zum Tutorial behält die Auswahl. Die Weltauswahl
   ergänzt oder entfernt je Aufruf ein Signal und kehrt zu diesem Schritt zurück.
4. Wenn die Rolle vollständig ist, Prüfen und weiter wählen. Zurück/Weiter behält
   sämtliche Zuordnungen.

Das zuerst gewählte Signal bleibt der geometrische Bezug; bei Einfahrten liefert
es außerdem später den Bezug der verknüpften Zieltaste. Wird es abgewählt, rückt das
nächste Signal nach. Jedes ausgewählte Signal wird auf örtliche Zugehörigkeit und
Rolle geprüft. Vor erfolgreicher Gleiswegübernahme müssen alle Endpunkte denselben
freien Streckenabschnitt in passender Richtung benutzen. Parallele physische Gleise
brauchen weiterhin getrennte Verbindungen. Pro Rolle sind bis zu 32 Signale möglich.
Fehlende Freimeldung, Belegung und entgegenstehende Erlaubnis werden nicht umgangen.

Lokale Prüfungen decken Mehrfachauswahl, unabhängiges Abwählen und grüne Markierung,
Prüfung weiterer Signale, Nachrücken des Bezugs, Speichern/Laden, gemeinsame Fahrwege
über Weichen, Bedienfelder und Ablehnung gegen die Erlaubnis ab. Spiel-, UI- und
Mehrspielertest stehen aus. Lokal wird mit den verfügbaren älteren Spielbibliotheken
und dem bestehenden 0.6.3-Pfadadapter gebaut, nicht mit Original-0.6.3-DLLs.
