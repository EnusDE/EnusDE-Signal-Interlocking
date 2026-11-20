# 0.7.29-P0 – Ungültigkeitskreuz, Kennlicht und direkter Weicheneditor

Railroader 0.6.2. Modmanager-Paket ersetzen, Spiel neu starten und die bestehende Testwelt weiterverwenden. Host und Clients benötigen dieselbe Version. Alle Änderungen aus [0.7.28](TEST-0.7.28.de.md) sind enthalten. Offline geprüft; Spiel- und Mehrspielerabnahme stehen aus.

## Drei verschiedene Zustände

| Zustand | Welt und Gleisbild | Wirkung |
| --- | --- | --- |
| Nicht eingebunden | Normale Lampen aus, weißes Ungültigkeitskreuz mit schwarzem Rand | Native KI berücksichtigt dieses eigene Signal nicht. |
| Eingebunden und in Betrieb | Normale Signalbilder | Gewohnte Fahrweg-, Belegungs- und Verschlussprüfungen. |
| Eingebunden, betrieblich abgeschaltet | Kennlicht: einzelnes weißes Licht, normale Lampen aus | Dieses eigene Signal wird von der nativen KI übergangen. Es ist keine Fahrtfreigabe des Stellwerks. |

Eine gespeicherte Signal-/Rangiertastenbindung im Gleisbild zählt als Einbindung. Bisherige Anlagen mit eingerichteten gespeicherten oder laufenden Fahrstraßen bleiben ebenfalls eingebunden, auch wenn ihnen ein eigenes Tafelfeld fehlt. Ein freistehendes Vorsignal kann die Einbindung und Abschaltung des zugeordneten Hauptsignals erben. Bloßes Platzieren oder die Wahl einer Signalrolle reicht nicht aus. Der finanzielle Bauzustand bleibt davon getrennt.

Im Gleisbild kennzeichnen Kreuz beziehungsweise einzelner weißer Punkt den Zustand; die Beschriftung erhält × beziehungsweise K. Ein Kennlicht wird nicht als Sh 1 gezeichnet. Bei fehlenden beziehungsweise ungültigen Betriebsdaten bleibt der Mod bei Halt; ein Fehler schaltet die Anlage nicht automatisch ab.

## Ein Signal abschalten

Signal öffnen → bei Hauptsignalen Richtung A/B → **Betriebszustand des Signals → Signal betrieblich abschalten**. Alternativ über die Eigenschaften des zugeordneten Gleisbildfelds. Mit **Einzelabschaltung aufheben** wird diese eigene Einstellung zurückgenommen. Ein abgeschaltetes übergeordnetes Stellwerk behält dabei Vorrang.

Die Einzelabschaltung ist je Signalrichtung gespeichert. Sie gibt keine Weichen für die KI frei und aktiviert keine Zielautomatik.

## Stellwerk für Umbauten außer Betrieb setzen

**Editor → Stellwerksauswahl → beim Stellwerk „Verwalten“ → Betriebszustand des Stellwerks → Stellwerk außer Betrieb setzen**.

Alternativ unter **Automatisierung und KI → Stellwerke und Annahme**. Auch ein übergeordnetes ESTW lässt sich dort beziehungsweise über Verwalten abschalten. Sein Umfang sind die eigenen Zuordnungen und seine verknüpften Ortsstellwerke. Unverknüpfte örtliche Stellwerke bleiben eigenständig.

Die Umschaltung verlangt die sichere Auflösung betroffener Fahrstraßen, Restverschlüsse und Richtungsbindungen. Unabhängige laufende Fahrstraßen dürfen bestehen bleiben. Betroffene wartende Anfragen werden zurückgenommen. Es wird kein bestehender Verschluss ungeprüft gelöscht.

Nach der Abschaltung zeigen eingebundene Signale Kennlicht. Ausschließlich dem abgeschalteten Bereich zugeordnete Weichen sind wieder für die Wegpunkt-KI verfügbar. Eine von einem aktiven Stellwerk mitbenutzte Weiche bleibt geschützt. Die elektrische Modelldarstellung bleibt erhalten. Native CTC-, Belegungs-, Kollisions- und Geschwindigkeitsregeln gelten weiter; fremde/Grundspielsignale werden dadurch nicht stillgelegt.

Mit **Stellwerk wieder in Betrieb nehmen** werden normale Signal- und Weichenschutzregeln wiederhergestellt. Eine bestehende Einzelabschaltung am Signal bleibt erhalten. Alte Fahrtfreigaben werden nicht wiederhergestellt; eine Fahrt benötigt eine neue geprüfte Anforderung. Sichtbarkeit, Annahmesperre und Automatikpause sind separate Einstellungen und geben keine Weichen frei.

## Weiche direkt bearbeiten

In der Welt nahe der Weiche auf das Gleis rechtsklicken → **Weiche bearbeiten**. Es öffnet sich direkt die Konfiguration dieser Weiche für Antrieb, GFM und Kästen. **Weiche zuordnen** bleibt der getrennte Einstieg für die Verbindung zum Gleisbild. Beide Aktionen verwenden die nächstgelegene Weiche innerhalb von 30 m; bei einem Gleichstand näher an die gewünschte Weiche klicken.

## Gezielte Spieltests

1. Neues ungebundenes Haupt- oder Rangiersignal platzieren: Kreuz, normale Lampen dunkel, keine eigene native Haltwirkung. Danach im Gleisbild zuordnen und prüfen, ob das Kreuz verschwindet und regulär Halt gilt.
2. Ein gebundenes Signal einzeln abschalten: Kennlicht prüfen. Seine elektrische Weiche darf allein dadurch nicht für die KI freigegeben werden.
3. Betroffene Fahrstraßen auflösen, örtliches Stellwerk außer Betrieb setzen: Kennlichter und native KI-Weichenbedienung prüfen. Ein aktives Nachbarstellwerk muss geschützt bleiben. Ein Umschaltversuch mit noch betroffener Fahrstraße muss abgewiesen werden.
4. Stellwerk wieder einschalten: Kennlicht verschwindet, regulär Halt ohne Fahrstraße, KI darf elektrische Weichen wieder nicht selbst umstellen. Insbesondere eine zuvor vor dem Signal wartende KI prüfen.
5. Je Mastseite unterschiedliche Einzelzustände sowie niedrige Hauptsignale und freistehende Vorsignale prüfen. Speichern, laden und frische Clientdarstellung prüfen.
6. **Weiche bearbeiten** auf zwei unterschiedlichen Weichen verwenden: jeweils die angeklickte Weiche im Eigenschaftenfenster, ohne Beginn einer Gleisbildzuordnung.

Die bisher gemeldeten allgemeinen KI-Halt-/Weichenauffahrprobleme sind nicht allein durch diese Offline-Prüfungen als im Spiel behoben bestätigt.
