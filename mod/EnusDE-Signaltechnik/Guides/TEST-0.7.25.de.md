# 0.7.25-P0 – Einrichtung und kurze Spieltests

Railroader 0.6.2. Modmanager-Installation ersetzen, Spiel neu starten, bestehende Testwelt weiterverwenden. Host und Clients benötigen dieselbe Version. Die Änderungen sind offline geprüft; die folgenden Spieltests stehen aus.

## Gleisbild erkennen

**Editor → Stellwerksauswahl → Automatische Erkennung** öffnen. Standardmäßig wird nur die Umgebung platzierter eigener Signale gezeichnet, mit 1000 m Suchradius entlang der Gleise. Der Datenkatalog erfasst weiterhin das vollständige verfügbare Netz. Der Zeichenbereich darf 100–10000 m betragen. Seine Enden sind als „Netzgrenze“ beschriftet und bedeuten keine physische Gleisunterbrechung oder Fahrtfreigabe.

„Neues Gleisbild vorschlagen“ wählen, die Vorschau prüfen und erst dann übernehmen. Der Generator bildet Stränge aus den realen Stamm-/Grundstellungsästen, stellt sie waagerecht dar und legt Nebengleise daneben. Er ist keine geografische Karte. Für das gesamte Netz die Begrenzung ausschalten; eine ganze Karte kann weiterhin sehr groß und unübersichtlich sein. Ein kleiner Zeichenbereich ist für die erste Einrichtung sinnvoller.

Die vorhandene Zeichnung bleibt beim Übernehmen im Archiv. Unvollständige Vorschläge werden abgewiesen. Tafeln dürfen höchstens 2048 × 512 Zellen und 65536 Bausteine haben; die gemeinsame Stellwerksdatei bleibt auf 16 MiB begrenzt. Auch Archive zählen dazu. Bei Überschreitung den Zeichenbereich verkleinern oder eine neue leere ESTW-Ansicht anlegen.

Vorhandene Signal-IDs und erkannte Weichen werden in die neue Zeichnung eingebunden. Aktivierte GFM-Grenzen werden berücksichtigt; eindeutige passende vorhandene Abschnitts-IDs werden wiederverwendet. Fehlende Kästen werden durch eine Zeichnung nicht zu gültigen Grenzen. Ohne gültiges Grenzpaar bleibt der Bereich unüberwacht, nicht frei.

## ESTW-Module sichtbar bearbeiten

In der Stellwerksauswahl beim ESTW **Module** wählen. Ortsstellwerk auswählen, **Verknüpftes Modul einfügen** klicken. Zum Verschieben X/Y ändern und **Ganzes Modul verschieben** wählen; die Modultafel wird aktualisiert. Einzelne Gleise über **Gewähltes örtliches Gleisbild bearbeiten** ändern. Die Modultafel ist eine Anordnungsansicht, keine Betriebsbedienung. Die eigene ESTW-Zeichnung und die verknüpften örtlichen Zeichnungen bleiben getrennt.

## GFM-Schnellwerkzeug und A/B/C

[Modellvorschau](images/gfm-abc-preview.png) – technische Darstellung aus dem Modellcode, kein Spiel-Screenshot.

**Bauen → GFM-Schnellwerkzeug** öffnen, Aktion auswählen und einschalten. Danach nacheinander Signale in der Welt anklicken. Das Werkzeug bleibt aktiv; **Escape** beendet es. Für einzelne Einstellungen das Werkzeug beenden und das Objekt normal öffnen.

- **A:** Kasten am Signal. Neue Signale erhalten standardmäßig A. Bei bestehenden Signalen ohne Kasten gezielt aktivieren.
- **B:** Frei platzierte oder automatische Weichengrenze. Vorhandene freie A-Kästen werden als B übernommen; Grenz-ID und Entitätsnummer bleiben erhalten.
- **C:** Nur die Pfeilseite begrenzt einen überwachten Abschnitt. Die Rückseite antwortet nicht als Gegenstelle. Bei Signal- und Weichenkästen ist die Überwachungsrichtung umkehrbar. Der C-Pfeil bleibt an die logische Gleisrichtung gebunden; die Gradkorrektur ist bei C ohne Wirkung.

Ein Abschnitt darf keine eingerichtete Zwischengrenze überspringen. Geänderte oder unpassende Grenzen machen einen gespeicherten Abschnitt unbekannt. Änderungen der logischen C-Rolle eines Weichenkastens verlangen aufgelöste Fahrstraßen und Richtungsbindungen. Reine Modellversätze ändern seine logische Grenzposition nicht.

Angebaute Kästen erhalten skalierungsabhängigen Abstand zum Mast. Bei Kollisionen wird eine nahe Alternative gesucht; wenn keine freie Position gefunden wird, bleibt der Kasten ausgeblendet und im Objektmenü steht eine Meldung. Diese Platzierung muss am Modell im Spiel geprüft werden.

## Kennungen

Technische Signalkennung und Richtungs-IDs sind nicht mehr editierbar; der Anzeigename bleibt frei. Das Aufwerten eines Rangiersignals vergibt ein passendes HS-Präfix bei unveränderter interner Signal-ID und GFM-Nummer. Eine frühere RS-Kennung ist nur noch inaktive Information. Bereits aufgewertete RS-Signale werden beim Öffnen korrigiert. Vorhandene andere individuelle Bestandskennungen werden nicht pauschal umnummeriert.

Nummern werden in diesem Stand noch nicht automatisch recycelt. Das verhindert, dass alte Verweise versehentlich einem neuen Objekt zugeordnet werden; eine spätere Wiederverwendung benötigt vollständige Bereinigung der alten Einträge.

## Drei wichtige KI-Tests

1. AE-Signalanbindung eingeschaltet lassen. Ohne eingestellte Fahrstraße vor einem eigenen Halt zeigenden Haupt- oder Rangiersignal einen Wegpunkt dahinter geben. Die KI soll anhalten. Eine Stellwerkszuordnung ist für die native Registrierung dieser eigenen Signale keine Voraussetzung. Gemeldetes Weiterfahren bleibt bis zu diesem Spieltest offen.
2. Eine elektrische Weiche falsch legen und die KI mit Wegpunkt darauf zufahren lassen: einmal von der Spitze und einmal aus dem falschen Zweig. Sie darf weder selbst umstellen noch die Weiche auffahren. Die Korrektur erfasst jetzt zusätzlich die native Umstellbarkeits-Vorausschau.
3. Mit eingerichteter Kontakt-/Signalautomatik einen abzweigenden Wegpunktauftrag anfordern. Der passende Wegpunktweg muss vor der Bevorzugung der aktuellen Weichenlage gewählt werden. Nur das Stellwerk stellt den gesicherten Weg; GFM, Belegung und Verschlüsse bleiben Voraussetzungen.

Automatisches Vor-/Zurücksetzen zur Bergung oder zum Wiederanfahren eines Signals wurde nicht eingebaut. Eine wartende bestehende Anforderung kann erneut geprüft werden; das erlaubt keine ungeprüfte Bewegung im Weichenbereich.
