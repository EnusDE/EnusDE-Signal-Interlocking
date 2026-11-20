# Erstes Automatik-Testpaket – 0.7.19-P0

Die erste Kontakt-Automatik ist im neuen **Testpaket 0.7.19-P0** enthalten. Das ältere 0.7.18-ZIP bleibt unverändert. Host und Clients müssen dieselbe neue Version verwenden. Zum Start siehe auch [Testablauf mit Whittier und Ela](TEST-0.7.19.de.md).

## Umfang

- Drei ausdrückliche Lok-Betriebsarten: **Keine Festlegung**, **Rangierfahrt**, **Zugfahrt**. Ausschließlich Zugfahrt kann eine Kontaktanforderung erzeugen. Andere gekuppelte Loks erhalten Keine Festlegung. Kuppeln, Trennen oder Ändern der Kupplungsanordnung macht die gespeicherte Festlegung ungültig, bis sie erneut übernommen wird.
- Platzierbare Gleiskontakte mit Name, Startsignal, Richtung, Position, Höhe und seitlichem Abstand. Neue Kontakte sind deaktiviert. Ein kleines Kontaktmodell zeigt erkannte Achspassagen durch eine bewegte Anzeige und einen Lichtimpuls.
- Pro Hauptsignal: Erstziel, optionales Ausweichziel, Fahrt/Langsamfahrt, Wiederholungsabstand 1–60 Sekunden sowie Zulassung manueller oder Wegpunktfahrten. Gespeicherte passende Fahrwege werden bevorzugt wiederverwendet; fehlt einer, sucht die vorhandene Start-Ziel-Planung. Mehrdeutigkeit erzeugt eine Meldung statt einer zufälligen Auswahl.
- Vollständige Fahrstraßenfolgen werden gemeinsam über die normale Engine angefordert. Eine abgelehnte Teilfahrstraße nimmt die neuen logischen Anforderungen und Blockreservierungen der ganzen Folge zurück. Weichenumlauf, Endlagen, Flankenschutz, Belegung und Zugschluss bleiben normale Engine-Prüfungen.
- Automatische Richtungsgruppen aus eingerichteten GFM-Abschnitten und dem tatsächlichen Gleisnetz. Zwischenblocksignale trennen die gemeinsame Streckenrichtung nicht. Ein-/Ausfahrgrenzen und eingerichtete Weichengrenzen begrenzen die Gruppen. Es ist keine gesonderte manuelle Blockverkabelung erforderlich.
- Neue Gegenrichtungsanforderungen sind bei bestehender Richtungsbindung ausgeschlossen. Freigabe erst, wenn alle zugehörigen Fahrstraßen einschließlich Restverschlüssen verschwunden sind und drei unterschiedliche vollständige Freiprüfungen vorliegen. Unbekannte oder fehlgeschlagene Belegungslesungen halten die Bindung.
- Signalfunktionen ohne besondere Funktion, Einfahrt, Ausfahrt, Zwischen- und Blocksignal. Ortsstellwerke können Durchfahrt, nur Zielankunft oder keine Annahme zulassen. Sie können auf Automatikbetrieb beschränkt und aus der Stellwerksauswahl des Fahrdienstleiters ausgeblendet werden. Ihre bereits gezeichneten Felder werden nicht aus dem globalen ESTW gelöscht.
- Schutz zugeordneter Weichen gegen native Wegpunkt-KI einschließlich ihrer Rückstellversuche, wenn die Blockprüfung und diese Option aktiv sind. Das verändert keine nativen CTC-/Entriegelungsflags. Örtliche Spielerbedienung und normale Stellwerksbefehle behalten ihre bisherigen Regeln.
- Host-gesteuerte Automatik; Clients erhalten Konfiguration und eine lesende Statusanzeige. Anforderungen, Gründe, Richtungsbindungen und Kontaktzähler sind sichtbar.

## Einrichtung im Spiel

1. In einer Sandbox zwei kleine Betriebsstellen **A** und **B** mit einer eingleisigen Verbindung einrichten. Gleisbild, Signalpositionen und bestehende Fahrstraßen zuerst manuell prüfen. Für Rangierbetrieb die Signale und Hilfsziele eindeutig einem Ortsstellwerk zuordnen.
2. Vollständige GFM-Abschnitte einrichten. An benötigten Weichen deren GFM einschalten und die drei Grenzen prüfen. Richtungsgruppen dürfen keine nicht unterteilte Weiche überqueren. Bei eingeschalteter Blockprüfung benötigen **auch manuelle Zugfahrstraßen** vollständige GFM-Abdeckung. Nicht eingerichtete Strecken ergeben keine automatische Freigabe.
3. Im **Stellwerkseditor → Automatisierung und KI → Signalrollen und Zielautomatik** Ein-/Ausfahrsignale als Bahnhofsgrenzen festlegen. Dazwischenliegende Blocksignale entsprechend markieren. Für den ersten Versuch ein eindeutiges Erstziel wählen, danach ein Ausweichziel ergänzen. **Signalrolle und Automatik speichern**.
4. Gleis vor dem Startsignal rechts anklicken → **Automatik-Kontakt platzieren**. Im Kontakteditor das Startsignal auswählen und die Richtung zum Segmentende A beziehungsweise B prüfen. Das Startsignal muss mindestens 0,5 m voraus auf einem eindeutig messbaren örtlichen Gleisweg liegen, höchstens 10 km entfernt. Kontakt zunächst nahe vor dem Signal platzieren; anschließend kann eine weiter entfernte Auslösung geprüft werden. Speichern und aktivieren.
5. Eine Lok auswählen. In ihrer Fahrzeuginfo unter **Signaltechnik – Fahrinformationen** oder im Automatikeditor unter **Lok-Betriebsart** ausdrücklich Zugfahrt übernehmen. Nach jeder Änderung am gekuppelten Verband erneut festlegen. Für den Test eine Zugnummer über die vorhandene Zugnummernverwaltung vergeben.
6. **Spieloptionen → Signaltechnik → Signalbetrieb und Darstellung**: AE-Anbindung einschalten und anwenden. Unter **Automatisierung und KI** Blockabstimmung und anschließend Kontakt-Automatik aktivieren und deren eigene Speichertaste verwenden. Blockabstimmung lässt sich nur ändern, wenn keine aktiven Fahrstraßen oder Richtungsbindungen mehr bestehen.
7. Zum Kontakt fahren oder mit einem Radstand dort anhalten. Die Anforderung bleibt gespeichert und wird bei gesperrtem Erst- und Ausweichziel wiederholt geprüft; erneutes Überfahren ist nicht erforderlich. Der Automatikstatus im Fahrdienstleiter beziehungsweise Editor zeigt den Grund. **Statusliste aktualisieren** übernimmt neu hinzugekommene Einträge; die angezeigten Zustände und Gründe lesen sich laufend nach.

Wird ein Wegpunkt verwendet, muss die native KI ihren tatsächlichen Gleisweg bereits ermittelt haben. Ein Ziel vor/am Startsignal, ein Kuppelauftrag, ein Nachbargleis ohne passenden Weg oder eine nicht eindeutig lesbare Schleife erzeugt keine Durchstellung. Endet der Wegpunkt innerhalb einer früheren Teilfahrstraße, werden nachfolgende Teilfahrstraßen nicht angefordert. Nativer Rangiermodus fordert keine Zugfahrt an. Die Mod ändert die drei Lok-Betriebsarten in diesem Paket noch nicht automatisch anhand des KI-Modus.

## Erste Versuche

| Versuch | Erwartung |
|---|---|
| Keine Festlegung oder Rangierfahrt über Kontakt | Keine automatische Zuganforderung. |
| Zugfahrt A → B, eindeutiger freier Fahrweg | Ganze konfigurierte Folge wird angefordert; Fahrt erst nach bestätigten normalen Prüfungen. |
| Erstziel gesperrt, Ausweichziel frei | Erstziel scheitert, zulässiges Ausweichziel wird versucht. |
| Beide Ziele gesperrt, später eines frei | Sichtbarer Wartegrund und erneute Prüfung ohne erneute Kontaktüberfahrt. |
| Zwei gegengerichtete Züge an A und B | Bereits gebundene Richtung verhindert die Gegenanforderung über die gesamte Gruppe. |
| Zug steht in einem entfernten Abschnitt | Keine neue Richtung, obwohl das erste örtliche Teilstück frei ist. |
| Zugschluss oder Wagen steht auf Grenze | Keine vorzeitige Freigabe aufgrund einer einzelnen freien Anzeige oder des Achstestzählers. |
| Anforderung zurücknehmen / Alle Signale Halt | Keine neue Anforderung derselben wartenden Aufgabe; vorhandene Verschlüsse bleiben normal geprüft. |
| Vor Einfahrt zurücksetzen, Auftrag wechseln oder kuppeln | Wartende Anforderung entfällt; bereits angeforderte Folge wird zurückgenommen, erforderliche Restverschlüsse bleiben erhalten. |
| Wegpunkt vor Start / im ersten Zielabschnitt / auf Nachbargleis | Keine unnötige Durchstellung hinter dem tatsächlichen Ziel. |
| Örtliche Rangierfahrt auf besetztes Rangierziel | Bleibt in normaler Rangierfahrstraßenprüfung, sofern Start und Ziel derselben Betriebsstelle angehören und keine Ein-/Ausfahrgrenze überschritten wird. |
| Nachbar nur Zielankunft / keine Annahme | Endende Fahrt bis in den Zielbahnhof möglich / neue Ankunft abgewiesen; kein Durchstellen zum übernächsten Nachbarn. |
| Wegpunkt-KI versucht zugeordnete Weiche umzustellen | Bei aktivem Schutz abgewiesen; zulässige Spielerbedienung und Stellwerksbefehle separat prüfen. |
| Speichern, Neuladen, Host und Client beitreten | Konfiguration bleibt; neue Laufzeit beginnt ohne alte Fahrtfreigaben. Clients steuern im ersten Paket keine Automatik an. |

Ein A–B–A-Dauerlauf mit Zugnummer ist der anschließende Spieltest. Eine neue Gegenfahrt setzt eine vollständige Freiprüfung voraus. Gleichgerichtete Anforderungen umgehen ebenfalls keine vorhandenen Fahrstraßen-, Belegungs- oder Ressourcenverschlüsse; eine ganze Folge kann deshalb weiterhin warten.

## Prüfgrenzen und späterer Ausbau

Der Achstest zählt die verfügbaren nativen Rad-/Achspunkte, keine aus Wagenlängen erfundenen Achsen. Ob alle Lauf- und Treibachsen eines Modells erfasst werden, muss im Spiel verglichen werden. Fehlende oder verspätete Beobachtungen werden nicht als Freimeldung interpretiert. Das ist **noch kein betrieblicher Achszähler** und ersetzt keine GFM.

Die vorhandenen Offline-Prüfungen und der Build gegen Railroader 0.6.2 ersetzen keine Unity-, KI-Fahr-, Modell- oder Mehrspielerabnahme. Eine lange Strecke mit vielen Kontakten und verschiedenen Fahrzeugmodellen muss zusätzlich auf Laufzeit und vollständige Beobachtung geprüft werden. Es werden höchstens 64 Kontakte, 64 gespeicherte Laufzeit-Anforderungsanzeigen, 16 aktive Teilfahrstraßen und 128 gespeicherte Fahrstraßendefinitionen verwendet.

Später vorgesehen bleiben die Spieleranträge direkt am Signal mit optionalem Antragkasten, feinere Spieler-/KI-/Adminrechte, Auftragsfolgen, automatische Betriebsartwechsel, Gleisnummern und Bot-Logistik sowie der umfangreiche örtliche Rangierbetrieb in Strecken- beziehungsweise Blockbetriebsstellen. Sie sind nicht Bestandteil dieses ersten Tests.
