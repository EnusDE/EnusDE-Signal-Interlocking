# Baumodus und native MOW-Materialien — 0.7.61

[English](BUILD-MOW-0.7.61.en.md)

Early-Alpha-Testfassung für Railroader **0.6.3**. Selbst über den Modmanager installieren und das gesamte Spiel neu starten. Host und alle Mitspieler benötigen **0.7.61**. Vorher vollständigen Spielstand und Anlagenkonfiguration sichern. Die Build-Werkzeuge installieren nichts und starten kein Spiel.

## Baumodus starten und beenden

**IXL / STW neben der Uhr → Bauen** öffnet rechts eine kompakte, größenveränderbare Bauleiste. Baurechte und Meilensteinvoraussetzungen bleiben verbindlich; Mitspieler erhalten dadurch keine neuen Bearbeitungsrechte.

Ein Werkzeug als Symbol wählen: Hauptsignal, Vorsignal, Rangiersignal, Geschwindigkeitsschild, Grenzzeichen oder GFM-Grenze B/C. Beim Darüberfahren erscheint der Name. Das gewählte Symbol wird türkis hervorgehoben und unter der Auswahl benannt. Mit Linksklick am Gleis platzieren; die fortlaufende Platzierung bleibt aktiv. Die Leiste bleibt erreichbar. Die rechte Maustaste bewegt weiterhin die Kamera. **Escape bricht die aktuelle Platzierung ab**, ohne bestätigte Pläne zu löschen.

**Baumodus beenden**, das native Fenster-X oder der Wechsel zu Editor/Fdl beendet den Modus. Eine unbestätigte Mausvorschau verschwindet; bestätigte Bauaufträge bleiben. Ein Objekt außerhalb des Baumodus anzuklicken öffnet seine Informationen. Der Host kann dort **Im Baumodus bearbeiten** wählen.

Globale Einstellungen bleiben ausschließlich am **Zahnrad im Auswahlfenster**.

## Vorschau und Baustellen

Bei aktivierter Materialpflicht erscheinen bestätigte unfertige Anlagen für den lokalen Erbauer als türkisfarbene virtuelle Modelle. Außerhalb des Baumodus, auch in der normalen Ansicht anderer Spieler, erscheinen eigene Hütchen-/Absperrmodelle mit Objektbeschriftung. Die Baustelle stellt dasselbe gespeicherte Objekt dar. Fertige Anlagen behalten ihr normales Modell.

Die unbestätigte Mausvorschau bleibt privat. Bestätigte Pläne verwenden die vorhandene hostgeführte Anlagensynchronisierung. Die bisherigen Betriebssperren unfertiger Signale bleiben erhalten. Bei kostenlosem Bau können Objekte sofort fertig werden; dann gibt es keine dauerhafte Baustellenphase.

## Bestellen, lagern und bauen

1. In der Bauleiste **Material → Normale Fahrzeugbestellung öffnen** wählen oder die normale **Company-/Unternehmensseite → Equipment/Fahrzeuge** des Spiels öffnen.
2. Einen Materialwagen über die normalen Spielbedienelemente als captive Wagen bestellen. **Signalbaumaterial** gehört in einen **FL/FM-Flachwagen**, **Signaltechnik** in einen **XM-Güterwagen**. Bestellung, Bezahlung und Kapazitätsprüfung übernimmt das Spiel.
3. Am nativen MOW-Baustofflager über dessen normale Bedienung entladen und wieder beladen. Die beiden Signalfrachten ergänzen passende vorhandene Materiallager; ein zusätzliches Mod-Lager ist unnötig. Die MOW-Lagerdefinitionen von Bryson und East Whittier wurden offline geprüft. Andere Karten benötigen passende Be-/Entladekomponenten und ihre jeweiligen Freischaltungen.
4. Materialwagen beladen an einen eigenen MOW-Arbeitswagen kuppeln, dessen native MOW-Ausrüstung aktivieren und innerhalb von **250 Gleismetern** bei einem bestätigten Bauplan anhalten. Beim Fertigstellen wird Material aus den angekuppelten eigenen Materialwagen verbraucht. Andere Fracht bleibt unberührt.
5. Unter **Material → Baustellen** stehen Restbedarf und Status. Auch der Bau-Meilenstein zeigt offene Aufträge und deren Status; das Ausbauprogramm bleibt für spätere Erweiterungen offen. In der Sandbox kann der Host einzelne Anlagen ausdrücklich kostenlos fertigstellen.

Die erste Anbindung einer vorhandenen Weiche behält ihren bisherigen einmaligen Materialbedarf. Dafür kann jetzt ein bestücktes natives MOW-Lager oder ein altes Mod-Lager im bisherigen **250-m-Luftlinienradius** verwendet werden. Dieser Verdrahtungsschritt verlangt noch keinen MOW-Wagen an der Weiche. Bereits angebundene Weichen werden beim Fahrstraßenspeichern nicht erneut berechnet.

## Alte Bestände und Sicherungen

Objekte, Fahrstraßen, Tafeln, Kennungen und alte Materialbestände bleiben erhalten. Unter Material erscheinen nicht leere alte Mod-Lager. Ein MOW-Lager auswählen und ausdrücklich **In das gewählte MOW-Lager übernehmen** anklicken. Nur ganze passende Pakete werden ohne erneute Bezahlung übertragen. Bruchteile im nativen Lager und überzähliger Altbestand bleiben erhalten. Nach normalem Verbrauch kann der Rest übernommen werden.

Ein Übernahmebeleg in den gemeinsamen Speichereigenschaften des Hosts verhindert, dass ein schon übertragener alter Bestand nach alleiniger Wiederherstellung einer älteren Anlagenkonfiguration nochmals gutgeschrieben wird. Stimmen Bestand und Beleg nicht mehr überein, vor weiterer Übernahme einen passenden **vollständigen Spielstand** wiederherstellen. Native Lagerbestände gehören zum Spielstand; die reine Anlagensicherung stellt sie nicht eigenständig wieder her. Die Übernahme ist eine administrative Bestandsmigration, keine animierte Wagenfahrt.

## Noch im Spiel prüfen

Der Code kompiliert gegen die lokalen Spielbibliotheken mit **0.6.3** in den Player-Metadaten. Offline geprüft sind Lagerregistrierung, Host-Berechtigung, Bruchteile/Kapazität, Rücknahme fehlgeschlagener Übernahmen, Wiederherstellungsschutz, Weichenkosten, native API-Verträge, Modusregeln sowie Baustellen- und Symbolgeometrie. Das ersetzt keine Unity-Sitzung.

Bitte mit Spielstandkopie prüfen:

- Beide Frachten regulär bestellen, einmal bezahlen, am freigeschalteten MOW-Lager ent-/beladen und Bestand nach Speichern/Laden kontrollieren.
- Mit aktivem MOW-Wagen und angekuppeltem Material bauen; fehlendes Material, Entfernung und deaktivierte Ausrüstung ausprobieren.
- Kleine und zu große Altbestände übernehmen; Restbestände und Speicherung prüfen.
- Baumodus mehrfach öffnen/beenden, Mausvorschau abbrechen, Editor/Fdl wechseln, Konfiguration wiederherstellen und neu öffnen.
- Rechte Bauleiste, Symbole, Tooltips, Sprachwechsel, transparente Modelle, Bodenkontakt und fertige Modelle bei Tag/Nacht kontrollieren.
- Mit gleicher Version bei Host und Client bestätigte Baustellen, Fertigstellung und späteren Beitritt prüfen. Nur der lokale Erbauer soll virtuelle Pläne sehen; Client-Baurechte dürfen nicht entstehen.

Für 0.7.61 ist noch keine Spiel-, Darstellungs-, Liefer- oder Mehrspielerabnahme erfolgt.
