# 0.7.18 – Eigene Einstellungen, wählbare Weichenkästen und sichtbare Festlegungen

0.7.18 ist eine **P0-Vorabversion für Railroader 0.6.2** und baut auf 0.7.17 auf. Host und alle Clients benötigen dieselbe Modversion. Die Änderungen sind offline geprüft; die Spiel- und Mehrspielerabnahme bleibt offen.

## Eigener Reiter in den Spieloptionen

- Die Einstellungen befinden sich jetzt unter **Spieloptionen → Signaltechnik**. Der bisherige Beitrag dieser Mod im gemeinsamen Mods-Reiter wird durch den eigenen Reiter ersetzt. Andere Mods und ihre Reiter bleiben erhalten.
- Die Optionen sind in Bauvorgaben, Signalbetrieb und Darstellung, Material und Baukosten, Bedienung sowie optionale lokale Diagnose gegliedert. Bauvorgaben umfassen Abstand, Höhe, Gleisseite und Modellgröße.
- Globale Bauvorgaben werden weiterhin ausdrücklich gespeichert. Bereits gespeicherte individuelle Objektpositionen und die Host-Berechtigungen gelten weiter. Objektbezogene Korrekturen bleiben über **Objekt → Position** erreichbar.

## Weichen-Gleisfreimeldung gezielt anzeigen

- Automatische GFM-Kästen und ihre Kabel sind **standardmäßig ausgeblendet**. Die Darstellung wird für jede Weiche einzeln aktiviert; auch alte Anlagen ohne gespeicherte Aktivierung beginnen ausgeblendet.
- Unter **Bauen → Weichen-Gleisfreimeldung** beziehungsweise **Objekte → Weichen-Gleisfreimeldung** lässt sich die erkannte Weiche über eine Suchliste auswählen. **Grenzen erkennen und Kästen anzeigen** aktualisiert die Netzerkennung und aktiviert die Kästen. Unvollständige oder mehrdeutige Grenzen werden gemeldet.
- Die drei Kennungen bleiben auf die Spitzenseite bezogen: **S** für die Spitzenseite, **L/R** für die von dort gesehenen Äste. Erkannte Grenzsegmente und Positionen lassen sich im Menü kontrollieren.
- **Kästen und Kabel ausblenden** schaltet die Darstellung wieder aus. Logische Belegungsgrenzen, Abschnittsbindungen und Verriegelungen bleiben dabei erhalten. Eine bestehende Gleisbildzeichnung wird durch diese Aktion nicht automatisch ersetzt.
- Aktivierung und individuelle Kastenversätze werden in der gemeinsamen Anlage gespeichert und übertragen. Ausblenden verwirft die gespeicherten Positionen nicht. Seitlicher Abstand, Längsversatz, Höhe und Unterbau bleiben pro Kasten bearbeitbar.
- Eine automatische Vermeidung aller Überschneidungen mit Signalen oder benachbarten Gleisen ist weiterhin offen; ungünstige Positionen können manuell korrigiert werden.

## Lesbarere Signalbeschriftung

- Signal- und Hilfszielbeschriftungen erscheinen auf der freien Seite gegenüber den Signalschirmen. Drehung, Spiegelung und geteilte Haupt-/Rangierfelder werden berücksichtigt; bei senkrechten geteilten Feldern stehen die Namen auf der freien Seite untereinander.
- Die Schrift passt sich innerhalb begrenzter Größen an den verfügbaren Platz an. Sehr lange Namen werden weiterhin gekürzt. Der zusätzliche Text „Ziel · “ entfällt, da Goldrand und Z das Hilfsziel kennzeichnen.
- Die alten zusätzlichen Maststriche werden nicht mehr neben den neuen Signalsymbolen gezeichnet. Die eigentliche Gleisverbindung bleibt erhalten.

## Blockierende Fahrstraßen und Restfestlegungen finden

- Konfliktmeldungen nennen die tatsächlich blockierenden Fahrstraßen mit Kennung und Zustand. **Festlegungen – alle Stellwerke** öffnet eine durchsuchbare Übersicht über aktive Fahrstraßen und zurückgehaltene Festlegungen in der gesamten Anlage.
- Bei einem Konflikt wird der betreffende Eintrag ausgewählt. Die Übersicht zeigt unter anderem Start/Ziel, gehaltene Ressourcen und den Grund einer noch ausstehenden Freigabe. Clients erhalten eine lesende Ansicht; fehlen aktuelle Zustandsdaten, wird das ausdrücklich angezeigt.
- Zurückgehaltene Ressourcen abgebrochener oder gestörter Fahrstraßen erhalten im Gleisbild kleine gelbe Markierungen. Bestehende Besetztfarben bleiben sichtbar; die Markierung bedeutet keine Fahrtfreigabe.
- Auflösen erfolgt weiterhin ausschließlich über die vorhandenen gesicherten Rücknahme- und Hilfsauflösefunktionen. Konfliktanzeige und Übersicht umgehen keine Belegungs-, Annäherungs-, Schutzwartezeit- oder Verriegelungsprüfung.

## Weiterhin enthalten

Die automatische Planung aus Start und Ziel, die Wiederverwendung identischer Definitionen, echte Fahrwegalternativen, mitlaufende Rangiersignale und reine Hilfszieltasten aus 0.7.17 bleiben enthalten. Haupt- und Rangiersymbole können auf derselben Gleisseite stehen. Die H/V-Signalmodelle und die bearbeitbaren Bodenauflagen aus 0.7.16 bleiben ebenfalls enthalten.

## Prüfstand und Installation

Die Offline-Prüfungen umfassen **633 erfolgreiche Prüfungen**, einschließlich eigener Einstellungsreiter neben fremden Reitern, gespeicherter Weichenaktivierung und Kastenpositionen, gedrehter Signalbeschriftungen sowie sichtbarer Konflikt- und Restfestlegungen. Die Mod-DLL wird aus dem aktuellen Quellstand erneut gebaut. Diese Prüfungen ersetzen keinen Spieltest.

Im Spiel sind insbesondere **HS-001 → HS-006** mit der neuen Konfliktanzeige, **HS-018 → P**, aktive und mitlaufende Rangierfahrstraßen, Hilfsziele in beiden Fahrarten und echte Weichenalternativen zu prüfen. Offen bleiben außerdem der eigene Einstellungsreiter im Spiel, Aktivieren/Ausblenden und Erkennen der Weichengrenzen, verschobene Kästen bei Signalüberlappungen und benachbarten Gleisen, Beschriftungen bei verschiedenen Zoomstufen, Bodenauflage und Kabel am Hang, Grenzüberfahrten mit Zugschluss beziehungsweise Fahrzeug über der Grenze, Wiederherstellen ausgeblendeter Bestandssignallampen, Speichern/Neuladen und Host-/Client-Beitritt mit gleicher Modversion. Der vollständige Austausch nativer Bestandssignale bleibt offen.

Das Installationspaket heißt **EnusDE-Signal-Interlocking-0.7.18-P0.zip**. Installation beziehungsweise Aktualisierung über den Modmanager, vollständiger Neustart von Railroader und Veröffentlichung erfolgen durch den Nutzer. Die bisherigen Installationspakete bleiben unverändert.
