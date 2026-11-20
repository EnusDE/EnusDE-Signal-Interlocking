# Externe Anlagensicherung und Wiederherstellung

Diese Funktion ist im neuen **Testpaket 0.7.19-P0** enthalten. Das ältere 0.7.18-ZIP enthält sie nicht und bleibt unverändert. Das Testpaket wurde noch nicht auf einem Spielrechner installiert oder als 0.8.0 veröffentlicht.

## Speicherorte

Der gemeinsame Anlagenbestand bleibt im Spielstand, damit die Welt einschließlich der Signaltechnik auf einen anderen Rechner übertragbar ist und Host sowie Mitspieler dieselbe Konfiguration erhalten. Zusätzlich legt der Host eine vollständige, eigenständige Dateisicherung an:

```text
<Application.persistentDataPath>/EnusDE-Signaltechnik/Backups/<Anlagen-ID>/
```

Das ist der dauerhafte Unity-Benutzerdatenordner, **außerhalb des installierten Modordners und außerhalb des jeweiligen Spielstandordners**. Der tatsächlich verwendete vollständige Pfad steht unter **Spieloptionen → Signaltechnik → Anlagensicherung und Wiederherstellung** und im Startprotokoll. Es wird kein Windows-Benutzername oder Installationspfad vorausgesetzt.

Normales Löschen beziehungsweise Aktualisieren des Modordners entfernt diese Dateien nicht. Wer zusätzlich den separaten Benutzerdaten-/Sicherungsordner löscht, entfernt damit auch diese Kopien. Der komplette Backups-Ordner kann unabhängig auf ein weiteres Laufwerk kopiert werden. Es gibt keine automatische Cloud-Übertragung.

## Was gesichert wird

- Alle eigenen Bauobjekte mit Kennungen, Signal- und Mastkonfigurationen, Namen, Positionen, Höhen und Ausrichtungen.
- Globale und örtliche Gleisbilder, Hilfsziele, GFM-Abschnitte, Weichenzuordnungen und individuelle Kastenpositionen.
- Fahrstraßendefinitionen und ihre Flankenschutz-/Freimeldezuordnungen.
- Kontaktpunkte, Signalrollen, Automatikregeln und Stellwerkseinstellungen.
- Modinterne Bau-/Materialverwaltung und gemeinsame Bauvorgaben sowie Ungültigkeitsmarkierungen für Bestandssignale.

Aktuelle Fahrtfreigaben, Fahrstraßenläufe, Restverschlüsse, Blockrichtungsbindungen, Belegungsproben, wartende Automatikaufträge, Achstestzähler und die aktuellen Zugnummernpositionen werden nicht übernommen. Lok-Festlegungen werden ebenfalls nicht aus einer alten Sicherung reaktiviert. Bei Wiederherstellung ist die globale Kontakt-Automatik aus; die konfigurierten Signalregeln bleiben für eine spätere bewusste Aktivierung erhalten.

Die Sicherung ersetzt keinen vollständigen Spielstand. Fahrzeuge, Gleisnetz, Wagenladungen und Spielkontostand werden damit nicht zurückgesetzt. Der modinterne Materialbestand entspricht nach einer Wiederherstellung dem gewählten Sicherungsdatum. Lokale Anzeige-, Diagnose- und Spracheinstellungen liegen weiterhin separat in settings.json.

## Zeitpunkt und Aufbewahrung

Nach erfolgreichem Laden beziehungsweise Initialisieren des vollständigen Bestands und nach erfolgreichen Konfigurationsänderungen wird unmittelbar eine Dateisicherung geschrieben, etwa nach Platzieren, Verschieben oder Löschen eines Signals, Speichern eines Gleisbilds, einer Fahrstraße oder eines Kontakts. Zusammengehörige Mehrfachänderungen werden erst nach Abschluss ihrer gesamten Transaktion gesichert. Reine Live-Anzeigen und Zugnummernbewegungen erzeugen keine neue Sicherung.

Es werden **32 automatische Versionsstände pro Anlage** aufbewahrt. Identischer Inhalt erzeugt keine weiteren automatischen Kopien. **Aktuellen Anlagenstand dauerhaft sichern** legt einen zusätzlichen dauerhaften Stand an. Vor dem Entfernen von Bauobjekten, Signalseiten, Stellwerken, Kontakten, Hilfszielen, Abschnitten oder Fahrstraßen wird der vorherige vollständige Stand dauerhaft archiviert. Beim Laden einer vom bisherigen externen Stand abweichenden Spielstandkonfiguration bleibt der vorherige Stand ebenfalls erhalten. Manuelle und diese geschützten Stände werden nicht automatisch ausgedünnt.

Die Dateien sind selbstständige gzip-komprimierte JSON-Dokumente mit Formatkennung, Zeit, Modversion und SHA-256-Prüfsumme. Temporäre Dateien werden vollständig geschrieben und auf den Datenträger geflusht, bevor sie atomar veröffentlicht werden. Ein Schreibfehler überschreibt keine unvollständige Datei als gültigen letzten Stand. Der Fehler wird im Protokoll und im Sicherungsmenü angezeigt; die automatische Sicherung versucht es erneut. Bereits erfolgreiche Spieländerungen bleiben dabei erhalten.

## Wiederherstellen

1. Als Host beziehungsweise im Einzelspieler **Spieloptionen → Signaltechnik → Anlagensicherung und Wiederherstellung** öffnen.
2. Die gesicherte Anlage auf derselben Karte und den gewünschten Versionsstand wählen. **Gewählte Sicherung prüfen und Vorschau anzeigen** prüft Format, Prüfsumme und Dokumente. Die Vorschau nennt Datum, Grund sowie Objekt-, Stellwerks-, Kontakt- und Fahrstraßenanzahl.
3. Alle aktiven Fahrzeuge anhalten und vorhandene Fahrstraßen sowie Blockbindungen regulär vollständig auflösen. Erst danach **Diesen Stand ausdrücklich wiederherstellen** wählen. Die Sicherungsdatei wird nochmals gelesen und mit der geprüften Auswahl verglichen.
4. Der bisherige vollständige Bestand wird vorher dauerhaft gesichert. Überschriebene rohe Synchronisationswerte erhalten zusätzlich ein Wiederherstellungsjournal. Die gewählte Konfiguration wird als zusammengehöriger Bestand übernommen; eine andere Karte wird abgewiesen.
5. Alle eigenen Signale beginnen Halt. Die Kontakt-Automatik bleibt ausgeschaltet, Lok-Betriebsarten und Zugnummernzuordnungen müssen neu festgelegt werden. Anlage prüfen und den Spielstand regulär speichern. Sollte eine Ansicht nicht vollständig neu aufgebaut werden können, meldet die Mod ausdrücklich, dass die Welt nach dem Speichern neu geladen werden muss.

Es gibt **keine automatische Wiederherstellung gelöschter Signale**. Der neue Stand mit der Löschung bleibt gültig; der frühere Stand ist nur als auswählbare Sicherung verfügbar.

Wenn beim Laden im Spielstand überhaupt keine Signaltechnik-Anlagendaten vorhanden sind und passende externe Sicherungen dieses Spielstands gefunden werden, wartet die Initialisierung auf die Auswahl im Sicherungsmenü. Alternativ kann ausdrücklich **Ohne Wiederherstellung initialisieren – Sicherungen behalten** gewählt werden. Eine beschädigte oder unvollständige gespeicherte Anlage wird nicht stillschweigend durch einen leeren Bestand ersetzt.

Sicherungen einer anderen Anlage auf derselben Karte können ausdrücklich übernommen werden. Interne Weltkennungen werden dabei einheitlich der Zielanlage zugeordnet. Tatsächliche native Gleis-/Signalzuordnungen müssen weiterhin zur geladenen Karte passen; die bestehende Fahrstraßenprüfung ist dafür unverändert zuständig. Umbenannte beziehungsweise kopierte Spielstandpfade werden als zusätzliche Herkunft der Anlage registriert.

## Prüfung

Offline geprüft werden vollständige Datei-Rundläufe, Wiederherstellung in derselben Karte, konsistente Weltkennungen, ausgeschaltete Automatik ohne alte Betriebszustände, Versionsaufbewahrung, Schutz vor Löschungen, Prüfsummenfehler, beschädigte Kataloge und letzte Dateien, Pfadbegrenzung sowie abgebrochene Dateiveröffentlichung. Die Produktions-DLL wird gegen Railroader 0.6.2 gebaut. Tatsächlicher Modmanager-Uninstall/Reinstall, Welt-Neuladen und Host-/Client-Wiederherstellung bleiben Spieltests; die Entwicklung greift nicht in bestehende Spielstände des Nutzers ein.
