# 0.7.29-P0 – Betriebszustände und direkter Weicheneditor

Neue gespeicherte Einzelabschaltung je Signalrichtung und Außerbetriebnahme je Stellwerk. Die Zustände sind von Automatikpause, Sichtbarkeit, Annahme und Materialbau getrennt.

- Nicht eingebundene eigene Signale: normale Lampen dunkel, originales weißes Ungültigkeitskreuz mit schwarzem Rand. Einbindung durch Tafelsymbol oder bisherige Fahrstraßeneinrichtung; getrennte Vorsignale können vom zugeordneten Hauptsignal erben.
- Eingebundene abgeschaltete Signale: eigenes einzelnes weißes Kennlicht. Haupt-, Rangier-, niedrige Haupt- und Vorsignalköpfe werden berücksichtigt; Hauptmast und integrierte Rangiertaste erhalten kein falsches Fahrt-/Sh-1-Bild.
- Native Signalsuche überspringt diese eigenen ungültigen/abgeschalteten Signale. Alte eigene Halteziele werden über die native Restriktionsschnittstelle zurückgenommen. Native Vertreter bleiben für eine eindeutige Wiederinbetriebnahme erhalten; Zuordnungs-/Modusänderungen werden vor der nativen Zielprüfung synchronisiert. Fehlgeschlagene Synchronisierung führt zu Halt und erneuter Prüfung.
- Ein abgeschaltetes ESTW umfasst eigene und verknüpfte örtliche Zuordnungen. Nur ausschließlich abgeschalteten Bereichen gehörende Weichen werden für die native KI verfügbar; aktive Mitbenutzer und native CTC-Regeln bleiben geschützt.
- Übergang verlangt sichere Auflösung betroffener Fahrstraßen, Restverschlüsse und Richtungsbindungen. Unabhängiger Betrieb bleibt möglich. Neue Fahrwege mit ungültigen/abgeschalteten Signalen oder freigegebenen Weichen werden zentral vor Reservierung und Umstellen abgewiesen. Alte Fahrtfreigaben werden beim Einschalten nicht übernommen.
- Signalmenü, Gleisbild-Eigenschaften und Stellwerksverwaltung zeigen den Zustand. Diagramm unterscheidet Kreuz, Kennlicht und Sh 1; Clients erhalten explizite Anzeigezustände.
- Neuer Weltmenüpunkt **Weiche bearbeiten** öffnet die geklickte Weiche direkt mit Antrieb, GFM und Kästen. **Weiche zuordnen** bleibt ein eigener Ablauf.

Bestehende Spielstände bleiben verwendbar. Alte gespeicherte Fahrstraßen zählen zur Einbindung; sie werden nicht pauschal außer Betrieb gesetzt. Alle älteren Pakete bleiben unverändert. Enthält sämtliche Änderungen aus 0.7.28.

[Deutsche Bedienung und Spieltests](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.29.de.md) · [English setup and gameplay checks](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.29.en.md).

Offline: **1074 Prüfungen bestanden, 0 fehlgeschlagen**. Dazu zählen Besitz-/Modusregeln, tatsächliche Fahrstraßenannahme vor Weichenbewegung, native Halteziel-Rücknahme und Wiederherstellung, isolierte Nachbarbereiche, Lampen-/Kreuzgeometrie und Clientzustände. Großer Netzprüffall: 1901 Segmente, 554 Gleisketten vollständig gezeichnet. Protokoll: `build/checks/0.7.29-tests.log`.

Reale Anzeige, KI-Fahrt beim Abschalten/Wiedereinschalten und Mehrspieler müssen im Spiel geprüft werden. Die zuvor gemeldeten allgemeinen Halt- und Weichenauffahrprobleme sind nicht als im Spiel behoben bestätigt.
