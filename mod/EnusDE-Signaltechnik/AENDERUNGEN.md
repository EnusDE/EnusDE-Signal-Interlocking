## 0.7.71 — Modulprobleme im Klartext

- Host-/Editorübersicht nach Stellwerk: betroffenes Element und Funktionsmodul, Ursache, Auswirkung und nächster Schritt. Originaldetails zuschaltbar, Direktverweis zum Gleisbild und Seiten mit 25 Befunden.
- Strukturierte GFM-Ursachen für fehlende/ungültige Gleisstücke, Lücken/Richtungswidersprüche, Weichenquerungen, mehrdeutige Knoten und übersprungene Grenzen. Bisherige Prüfentscheidungen und Originalmeldungen bleiben erhalten.
- Gespeicherte Konfiguration und bekannte Fahrweg-, Strecken-, Spiel-/Wegpunkt- und Modellprobleme. Gescheiterte Prüfungen bleiben ungeklärt; Belegung, Warten und bewusste Pausen sind allein keine Störung.
- Manueller Schnappschuss mit Hinweis auf veraltete Welt-/Konfigurationsdaten; Wartung gesondert. Keine automatische Reparatur oder Lockerung des Schutzes. Spiel-/Mehrspielerabnahme offen.

- Vollständiges Test.ps1: 2.194 PASS, 0 FAIL gegen lokale Originalbibliotheken von Railroader 0.6.3; Hauptmod und optionales Paket kompiliert. Keine Spiel-/MP-Abnahme, Installation oder öffentliche Veröffentlichung.

## 0.7.70 — getrennte Fahrstraßenfarben und automatische Fahrartwahl

- Zugfahrstraßen bleiben grün; Rangierstraßen werden weiß dargestellt, einschließlich Gleisen, aktiven Weichenarmen, GFM-Halbgleisen und integrierten Nummernspiegeln. Rot bei Belegung und Bernstein bei unbekannter Meldung behalten Vorrang; Auflösung und Restverschlüsse bleiben erkennbar.
- Automatische Wegpunktfahrart bevorzugt ein grundsätzlich erreichbares Zugfahrziel. Nur dessen Fehlen erlaubt eine nachgewiesene örtliche Rangieralternative; kein Wechsel nach abgewiesener Zugfahrstraßenzulassung. Ausdrückliche Auswahl bleibt bindend, freie Strecke und Blocksignal bleiben ausgeschlossen.
- Zweisprachige Legende und Erklärung; keine Lockerung der Betriebsprüfungen, keine Änderung der Signalbegriffe oder des Live-Frame-Formats.
- Vollständige Offline-Prüfung: 2.170 PASS, 0 FAIL. Spiel-/Automatik-/Host-Client-Abnahme noch offen.

# 0.7.69-P0 · Blockweiterfahrt und Folgezüge

- Die Wegpunkt-Automatik prüft jeden eingerichteten Blockabschnitt in Fahrtrichtung, einschließlich der Fahrt vom letzten Blocksignal zum Einfahrsignal des Nachbarstellwerks.
- Ist ein späterer Abschnitt belegt, kann eine manuelle Anforderung an ein verknüpftes Streckenziel bis zu einem erreichbaren Blocksignal reichen. Der Fdl sieht das Ende der Fahrstraße und den belegten Abschnitt. Weitere manuelle Teilfahrstraßen separat anfordern. Richtungs-, Belegungs-, Weichen-, Flanken- und Verschlussprüfungen gelten weiterhin.

# 0.7.68-P0 · Wegpunktziele und Folgefahrstraßen

- Ein bereits freigegebenes mitlaufendes Rangiersignal bleibt beim Einfahren demselben Zug zugeordnet.
- Liegt der Wegpunkt kurz vor einem Signal auf einem anderen Gleissegment, prüft die Zielsuche die eindeutige Gleisfortsetzung bis 300 m. An Weichen und unklaren Verbindungen endet die Suche.

# 0.7.67-P0 · Wegpunkt-Vorlauf und Umrangieren

- Bis zu drei geprüfte Teilfahrstraßen für denselben Zug frühzeitig stellen. Blockierte oder unklare Abschnitte halten die Folge auf.
- Geplante Richtungswechsel in einzelne Fahrabschnitte aufteilen. Stillstand des ganzen Verbandes am Wendepunkt und freie Weichen prüfen.
- Direkte Zugausfahrten auf eingerichteten Strecken mit der bestehenden Streckenerlaubnis und Blockprüfung zulassen.

# 0.7.66-P0 · Signalbedienung und Wegpunktanfragen

- Linksklick auf ein Signal öffnet beim Host die Fahrstraßenanfrage. Rechtsklick öffnet die Objektaktionen.
- Ein Schalter im Zahnrad aktiviert automatische Anfragen für feste Wegpunkte. Die Lokinfo und das Spielprotokoll zeigen den Wartegrund.
- Der Editor gruppiert die Einrichtung nach Aufgaben. Der Hintergrundkatalog verbindet aktive Meldegrenzen über Signale ohne eigenen Melder hinweg.
- Die Spiel- und Mehrspielerabnahme steht aus.

[Bedienungsanleitung](Guides/WAYPOINT-0.7.66.de.md)

# 0.7.65-P0 · Örtliche Wegpunkt-Automatik

- In der Lokinfo Wegpunkt-Automatik einschalten, pausieren oder einmalig den nächsten Fahrweg anfragen.
- Gerichteten Wegpunkt-Fahrweg, Fahrart und eindeutig erkannten Verband prüfen. Jeweils eine örtliche Fahrstraße vorbereiten. Bei Übernahme gelten die normalen Verschlüsse weiter.
- Automatikstatus zeigt Lok, Fahrart, Start, Ziel und Wartegrund. Der Host kann wartende Anfragen zurücknehmen. HALT pausiert weitere automatische Anfragen.
- Feste örtliche Wegpunkte mit freien und vollständig überwachten Fahrwegen. Echte Auto-Engineer-, Bedienungs- und Mehrspielerprüfung offen.
- Runenwacht 0.2.1 mit aktualisierter Hauptmod-Abhängigkeit gebaut.

[Wegpunkt-Anleitung](Guides/WAYPOINT-0.7.65.de.md)

## 0.7.64 — Runenwacht-Qualität und einzelne Lampentests

- Runenwacht 0.2.0: mittige Rot-/Bernstein-/Grün-Gruppe, Sichelträger, ausgearbeitete Gehäuse/Leitungen/Keramik, UV-/PBR-Texturen für die aktive Pipeline, facettierte Körper und getrennte feine Leuchtadern.
- Eingefasste technische Kennungs-/Namensschilder und Typenschild; stabile IDs und lokale Helligkeitsregler erhalten.
- Host-Baumodus: einzelne vorhandene Köpfe ohne Fahrstraße testen; native Haltbindung beider Mastrichtungen und Sperre betroffener Start-/Ziel-/Zwischen-/Flankenschutzfahrstraßen. Keine Testfreigabe in der Betriebs-API.
- Ende, Auswahlwechsel, Baumodusende, 120-Sekunden-Grenze und Welt-/Datenwechsel stellen reale Anzeigen wieder her. Lokale Host-Testansicht; Clients sehen Betriebs-Halt. Keine gespeicherten Testlichtbilder.
- Beide Assemblies und vollständige Offline-Prüfungen bestanden; tatsächliche Unity-/Shader-/KI-/Mehrspieler-/Leistungsprüfung bleibt offen.

## 0.7.63 — optionale Darstellungspakete / Runenwacht-Experiment

- Hauptmod liefert unveränderliche Signalzustände; separates optionales Runenwacht 0.1.0 zeichnet eigene prozedurale Modelle, gerichtete Blenden, feste Kristallfarben und widerspruchsfreie Lichtübergänge.
- Gespeicherte Modellwahl und kostenlose Welt-Testlizenz; Konsolenaktivierung nur als Host, nach jedem Laden zunächst AUS. Kein Kauf und keine automatische Asset-Übertragung.
- Haupt-, Vor-, Rangier- und kombinierte Köpfe behalten Signal-IDs, Fahrstraßen, Materialablauf und Betriebslogik. Fehlende/fehlerhafte Pakete erhalten sichtbaren Ersatz; gesperrte Umrüstungen bleiben gesperrt.
- Beschriftungs-/Emissionsregler, passende Anklickflächen und Ressourcenfreigabe; zweisprachige Anleitung und öffentliche Entwicklerreferenz.
- Offline-Build und Prüfungen; Darstellung, Sichtbarkeit, Leistung, Speichern/Laden und Mehrspielerbetrieb noch im Spiel zu prüfen.

# 0.7.62-P0 – Materialkauf am Interchange

- Fehlende native Übergabelieferanten für Signalbaumaterial und Signaltechnik ergänzt. Wagenauswahl und MOW-Lager bleiben erhalten.
- Isolierte Industrie-DTO vor nativem Aufbau/Registrierung vorbereiten. Originale Übergabegleise verwenden, andere Lieferanten und zwischengespeicherte Kartendaten erhalten; mehrdeutige/leere Definitionen überspringen.
- Einkauf über native Wagenziele mit Preisen/Gebühren, Zahlung, Außenweltbedienung und Rücklieferung. Kein Sofortfüllen und kein zweites Kaufmenü.
- Kaufhinweise und Anleitung Englisch/Deutsch. Keine Chatbefehle nötig. Erwartete generische FM-Kisten, FL-Skelettgrenzen und geschlossenes XM-Aussehen erklärt; keine eigenen Texturen enthalten.
- Elf produktive Lieferanten-/Prefix-/API-Regressionen ergänzt. Tatsächlicher Einkauf, Zahlung, Hin-/Rücklieferung, Ladungsdarstellung und Mehrspielerabnahme offen. Vorherige Bauleiste/Wagenauswahl vom Nutzer bestätigt; das belegt den neuen Kaufweg noch nicht.

Siehe Guides/INTERCHANGE-0.7.62.de.md. Manuell installieren; alle benötigen 0.7.62 und vollständigen Neustart.

# 0.7.61-P0 – Baumodus und native MOW-Logistik

- Ausdrücklicher Baumodus mit rechter Symbolleiste, Werkzeuganzeige und Beenden-Taste. Fenster-X und Moduswechsel beenden den Bau; bestätigte Pläne bleiben.
- Virtuelle türkise Pläne für den lokalen Erbauer; eigene Hütchen/Absperrung für bestätigte unfertige Objekte außerhalb des Baumodus. Mausvorschauen bleiben privat, fertige Modelle unverändert.
- Normale captive Materialwagenbestellung sowie Be-/Entladung an passenden nativen MOW-Lagern. FL/FM für Signalbaumaterial, XM für Signaltechnik; Bau mit aktivem eigenem MOW-Arbeitszug.
- Eigene Kauf-/Lagerbedienung durch native Bestellung, Lagerbestand und Übersicht offener Aufträge/Status ersetzt. Auch der Bau-Meilenstein zeigt offene Arbeit.
- Ausdrückliche kapazitätsbegrenzte Altbestandsübernahme mit Restbestand, Rücknahme bei Fehlern und gespeichertem Übernahmebeleg. Anlagen, Straßen und Frachtkennungen bleiben erhalten.
- Erstmalige Weichenanbindung kann nahe native Lagerbestände verwenden; einmaliger Bedarf bleibt, noch ohne Arbeitswagenpflicht an der Weiche.
- Einstellungen bleiben am Zahnrad. Englisch/Deutsch sowie gezielte Material-, API- und Modusprüfungen enthalten.
- Gegen lokale 0.6.3-Bibliotheken kompiliert; neue Spiel-, Liefer-, Darstellungs- und Mehrspielerabnahme offen.

Siehe Guides/BUILD-MOW-0.7.61.de.md. Alle Mitspieler benötigen 0.7.61 und einen vollständigen Neustart.

# 0.7.60-P0 – Einstellungen im Auswahlfenster

- Viertes Symbol als Zahnrad: eigenes vergrößerbares Einstellungsfenster.
- Einstellungen bleiben vor Meilensteinfreischaltung und für Mitspieler erreichbar; ältere Ausblendeinstellungen verstecken den Zugang nicht.
- Company-Auswahl für die gesamte Modliste und eigener Spieloptionen-Reiter entfernt. Native Modverwaltung bleibt erhalten.
- Einstellungsablage, Welt-/Anlagendaten, Host-Berechtigungen und die drei bisherigen Modussymbole bleiben erhalten.
- Aktive Menü-/Handbuchverweise und beide Sprachpakete angepasst.
- Regressionen für produktive Tastenrückrufe und Fensterlebenszyklus ergänzt.
- Bestehende Fahrzeugreiter-Anbindung an den umbenannten BuildContent-Rückruf in 0.6.3 angepasst. Exakte Signatur bleibt erforderlich; unbekannte/mehrdeutige Rückrufe werden abgewiesen.
- Kompiliert gegen lokale Spielbibliotheken mit 0.6.3 in den Player-Metadaten; Spiel-/UI-/Mehrspielerabnahme bleibt offen.

Anleitung: Guides/SETTINGS-0.7.60.de.md. Im Mehrspieler benötigen alle 0.7.60 und einen vollständigen Neustart.

# 0.7.59-P0 – Gespeicherte Blockansichten weiterbearbeiten

Öffnen mit vorbereiteter Vorschau; Einstellungsänderungen aktualisieren sie mit erhaltener Platzierung. Übernehmen setzt den offenen Editor auf den gespeicherten Stand zurück, sodass weitere Änderungen direkt möglich sind. Breitere Ansichten können die Tafel erweitern, statt die Position zurückzusetzen. Alte Rückrufe eines früheren nativen Aufbaus werden abgewiesen. Tests mit vier Abschnitten, 180°-Drehung und wiederholtem Speichern/Laden/Wiederöffnen ergänzt. Details: Guides/BLOCK-VIEW-REEDIT-0.7.59.de.md. Spiel-/UI-Test steht aus.

# 0.7.58-P0 – Blockansicht bei laufenden Nummernänderungen

Bauliche Entwürfe getrennt von laufenden Zugnummern prüfen; normale Weitergabe sperrt die Bedienung nicht mehr stillschweigend. Sichtbarer Status/Fehler und dauerhaftes Übernehmen, Schutz vor unveränderten Ereignissen und Laden des aktuellen gemeinsamen Stands ergänzt. Beim Blockbau bleiben echte Änderungsprüfungen und aktuelle Zugnummern erhalten. Neue Ablauftests mit produktiven Fenster-Callbacks. Details: Guides/BLOCK-VIEW-ACTIONS-0.7.58.de.md. Spiel-/UI-Test steht aus.

# 0.7.57-P0 – Stabile Vorschauplatzierung

Platzierungsklicks ändern den Vorschlag direkt ohne Neuaufbau oder erneutes Einpassen. Die aktuelle Position bleibt sichtbar; vorübergehende Mausvorschauen werden beim Verlassen ausgeblendet. Platzierungsstatus und Einzelschritt-Tasten ergänzt. Vorschau und Blockbau können die Tafel mit Kollisions-/Größenprüfung und erhaltenen Quellen-/Erlaubnisbindungen erweitern. Details: Guides/BLOCK-PLACEMENT-0.7.57.de.md. Spiel-/UI-Test steht aus.

# 0.7.56-P0 – Fensteraufteilung der Blockansicht

Die Blockvorschau erhält eine eigene Zeile im scrollbaren Formular und überdeckt die Einstellungen nicht mehr. Kompakte Auswahlfelder und Anpassung an die tatsächliche Vorschaufläche halten die Bedienung auch in kleineren Fenstern erreichbar. Ansichts- und Betriebsdaten bleiben erhalten. Details: Guides/BLOCK-VIEW-LAYOUT-0.7.56.de.md. Spiel-/UI-Test steht aus.

# 0.7.55-P0 – Bearbeitbare Blockansichten und Hilfszielverknüpfung

Darstellungsstufe, Abschnittsauswahl, horizontale Spiegelung und 180°-Drehung je Tafel mit Vorschau und Verbundplatzierung. Gespiegelte Zugnummern sitzen in der Gleiszeile. Ursprüngliche Quellen und eindeutige Signalzuständigkeit bleiben erhalten. Das Tutorial verknüpft alle gewählten Ausfahrten mit dem Hilfsziel und richtet dieses nach der örtlichen Einfahrt aus; Erlaubnismodule unterstützen schematische Gleisenden. Details: Guides/BLOCK-VIEWS-0.7.55.de.md. Spieltest steht aus.

# 0.7.54-P0 – Mehrere Signale je Tutorialrolle

GWB-Signalklicks ergänzen einzelne Signale oder wählen sie ab, statt die Auswahl zu ersetzen. Alle ausgewählten Felder bleiben grün, auch in der geöffnet bleibenden Gleisbildauswahl. Anzahl, Namen und einzelne Abwähltasten sind sichtbar. Jedes Signal wird geprüft; gemeinsamer Gleisweg und passende Richtung bleiben erforderlich. Details: Guides/GWB-MULTISELECT-0.7.54.de.md. Spieltest steht aus.

# 0.7.53-P0 – GWB als Einrichtungstutorial

Einzelauswahl in fester Reihenfolge: Einfahrt A, Ausfahrt A, Einfahrt B, Ausfahrt B. Je Schritt Erklärung und Prüfen/Weiter; offene Punkte bleiben mit nächster Handlung sichtbar. Geometrische Streckenerkennung unabhängig von fertigen Betriebsfahrstraßen, Zieltasten und GFM. Fehlende Ziel-/Erlaubnisfelder werden als verknüpfte Vorschau ergänzt, vorhandene passende Felder wiederverwendet. Weichen-GFM im gewählten Fahrweg vorbereiten; im Tutorial erst beim geprüften Abschluss aktivieren. Einfache Objektzuordnung bleibt optional. Details: Guides/GWB-TUTORIAL-0.7.53.de.md.

# 0.7.52-P0 – Feld zuerst, Weltklick ordnet direkt zu

Im Hauptgleisbild offene Signal-/Weichenfelder grün markieren, das aktuelle Ziel hellgrün umranden und in Sicht bringen. Passendes Weltobjekt anklicken: Zuordnung wird ohne zusätzliche Bestätigung gespeichert, dieselbe örtliche Tafel wieder geöffnet und das nächste Feld markiert. Die globale Weltobjektliste und der leere Kartenbereich entfallen im Hauptablauf. Neue Signalplatzierung erst auf Wunsch aufklappen. Auswahlhinweis am Signal nennt das grüne Zielfeld; einseitige Masten sind von beiden Kameraseiten auswählbar. Details: Guides/DIAGRAM-FIRST-0.7.52.de.md.

# 0.7.51-P0 – GWB-Verbindungen löschen

Löschen bereinigt jetzt verknüpfte Erlaubnisfelder und Hilfstasten gemeinsam mit dem Streckeneintrag. Vorschau und Bestätigung in jedem Assistentenschritt verfügbar. Gruppenbindungen werden gelöst, zugehörige Blockautomatik und Kontakte abgeschaltet. Weltobjekte, GFM und Zeichnung bleiben erhalten. Betroffene Belegung, Fahrstraßen und Richtungsbindungen werden vor dem Löschen geprüft. Details: Guides/LINE-REMOVAL-0.7.51.de.md.

# 0.7.50-P0 – Einrichtungsassistent (Testfassung)

Objekte schrittweise per Gleisbildklick zuordnen, Gleiskarte zur Orientierung, vorhandenes oder neues Signal direkt am Tafelfeld einrichten. GWB in vier Schritten mit Bestandsprüfung; bestätigter Ersatz einer Blockaufteilung einschließlich vollständiger Zugnummernübersichten. Optionale Annahmekapazität mit Empfangs-GFM und Einfahrreservierungen. Direkte GFM-Zuordnung zwischen benachbarten Weichen korrigiert.

Spiel- und Mehrspielertest der neuen Abläufe stehen aus. Einzelheiten und Grenzen: Guides/SETUP-ASSISTANT-0.7.50.de.md.

# 0.7.49-P0 – Unbestätigter Kompatibilitätstest für Railroader 0.6.3

Spielabhängigkeit auf 0.6.3 gesetzt. Die verschobene Benutzerdatenpfad-API wird beim
Start aufgelöst; neue HomeDirectory-Variante bevorzugt, bisherige API als Rückfall.
Alle bisherigen Modfunktionen bleiben enthalten. Keine Spielbibliotheken im Paket.

Wichtig: Noch kein Build gegen Original-0.6.3-DLLs. Im laufenden Arbeitsbereich
liegen weiterhin die bisherigen Bibliotheken; die neue Pfadvariante wird simuliert
geprüft. Der Kandidat ist keine bestätigte Kompatibilitätsfreigabe. Details:
Guides/GAME-063-0.7.49.de.md. Alte ZIPs unverändert, nichts veröffentlicht.

# 0.7.48-P0 – Editor-Arbeitsfläche und Kopfzeilen

Nur im Editor: Excel-artige Spalten-/Zeilenköpfe, ganze Zeilen/Spalten markieren,
Umschalt-Klick für Bereiche, Rechtsklick zum Einfügen davor/dahinter. Die Arbeitsfläche
wächst beim Zeichnen, Platzieren, Einfügen und Verschieben an allen vier Rändern.
Bestehende Zuordnungen und verschiebbare Gruppen bleiben erhalten. Oben/links wird
die Ansicht ausgeglichen; das Mausrad zoomt zum Mauszeiger. Rückgängig erfasst auch
die Erweiterung. Fahrdienstleiter-Modus ohne Kopfzeilen und mit bisheriger Ansicht.

Host und Clients benötigen 0.7.48. Anleitung: Guides/EDITOR-CANVAS-0.7.48.de.md.
Visuelle Spiel- und Mehrspielerabnahme offen. Ältere ZIPs bleiben unverändert.

# 0.7.47-P0 – Kompakte Zugnummernübersicht

Optionale, schmale Nummernleisten mit 1–16 Feldern auf frei gewählter Zieltafel.
Blockreihenfolge A → B / B → A oder jede Quelle manuell wählen. Die Felder spiegeln
vorhandene Quellen, erzeugen keine zweite Blockanlage und bleiben als Gruppe
verschiebbar. Vorschau mit Kollisionsschutz; Entfernen betrifft nur die Anzeige.
Bestehende Quellenverknüpfungen überstehen Speichern/Laden, Block-Neuaufbau und
Bedienerwechsel. Anleitung: Guides/NUMBER-OVERVIEW-0.7.47.de.md.

Host und Clients benötigen 0.7.47. Vorige Archive unverändert.
Spiel- und Mehrspielerabnahme noch offen. Gleisbildfortsetzungen, Signal-Gleisseite
und Umbau der ausführlichen Streckendarstellung sind separate nächste Schritte.

# 0.7.46-P0 – Hilfsziel mit Streckenerlaubnis und Blockfolge

Interne Testfassung für Railroader 0.6.2. Host und Clients benötigen 0.7.46.
Ältere ZIPs bleiben unverändert. Enthält die GFM-Reparatur aus 0.7.45.

## Beabsichtigter Ablauf

Die normale Hilfstaste bleibt ein Fahrstraßenziel. Ihre Verbindung mit einem
Erlaubnismodul legt zusätzlich fest, welche Streckenerlaubnis für die Ausfahrt
erforderlich ist. Fehlt diese, wird die Anforderung abgewiesen – auch wenn die
bisherige örtliche Zielposition noch vor der überwachten Streckenachse liegt.

Bei vorhandener Erlaubnis wird der vollständige Weg zum tatsächlichen
Ankunfts-Hauptsignal des Nachbarn vorbereitet. Dazwischenliegende Blocksignale
werden als Teilfahrstraßen mit angefordert. Alle normalen GFM-, Weichen-, Flanken-,
Betriebszustands- und Annahmeprüfungen bleiben aktiv. Die Gegenrichtung bleibt
Halt; das Einfahrsignal des Nachbarn wird dadurch nicht auf Fahrt gestellt.

## Einrichten: beispielsweise WHB → ELA

1. In der Streckenkonfiguration STR-001 beide Nachbarstellwerke sowie die realen
   Aus-/Ankunftssignale und den erkannten Verlauf prüfen. Das Ankunftsziel am
   anderen Ende muss ein verfügbares Hauptsignal sein. Die Hilfstaste selbst
   darf nicht zugleich dieses Ankunftssignal ersetzen.
2. Die Hilfstaste **Nach Ela** im örtlichen Stellwerk WHB bearbeiten:
   **Funktion der Hilfstaste → Fahrstraßenziel**, **Zugfahrten** aktivieren, speichern.
3. Das Erlaubnismodul **STR-001** bearbeiten. Unter **Verknüpfte Hilfstaste** das
   normale Ziel **Nach Ela · Fahrstraßenziel** auswählen und das Feld speichern.
   Die Erlaubnis wird weiter über das Modul bedient; die Hilfstaste bleibt Ziel.
4. Alternativ kann direkt an der normalen Hilfstaste **Erforderliche Streckenerlaubnis
   für dieses Ziel** gewählt werden. Eine der beiden Bindungen genügt; widersprüchliche
   Zuordnungen werden abgewiesen. Der örtliche Eigentümer der Hilfstaste bestimmt
   die Abfahrtseite A oder B. Globale Ansichten allein ersetzen diese Zuständigkeit nicht.
5. Im Fahrdienstleiter Startsignal und **Nach Ela** wählen. Ohne Erlaubnis für WHB
   muss eine konkrete Ablehnung erscheinen. Mit Erlaubnis werden Ausgangsfahrstraße
   und zugehörige Blockfolge bis zum ELA-Ankunftssignal eingereiht und nach den
   normalen Prüfungen gestellt. Die Kontakt-Automatik muss für diese Handanforderung
   nicht eingeschaltet sein; einzelne Blockkontakte sind dafür nicht erforderlich.
6. Die örtliche Zielposition im gezeichneten Bahnhof begrenzt diese gebundene
   Anforderung nicht mehr. Ziel, reale Fahrtrichtung und Endsignal stammen aus
   der zugeordneten Streckenverbindung.

Eine reine Hilfstaste mit **Streckenerlaubnis bedienen** öffnet weiterhin nur
Anfrage/Abgabe. Wird sie während einer Startwahl angeklickt, erklärt eine Meldung,
dass noch ein normales Fahrstraßenziel erforderlich ist. **Automatische Bestätigung**
im Erlaubnisfenster bleibt auf Erlaubnisanfragen beschränkt.

## Automatische Zuganforderung

Für einen selbsttätigen Zugauslöser den Anforderungskontakt am Startsignal, dessen
Zielautomatik und die Lok-Betriebsart Zugfahrt wie bisher einrichten. Als Vorrangziel
kann die gebundene normale Hilfstaste verwendet werden. Globale Kontakt-Automatik
muss für diesen Kontaktbetrieb aktiv sein. Der gleiche Erlaubnis- und Blockweg wird
geprüft; ein fehlendes Recht wird nicht eigenmächtig übernommen.

Wegpunktaufträge dürfen die Folge am aktuellen Wegpunkt begrenzen. Auch eine
solche verkürzte Folge behält die ausdrückliche Erlaubnisbindung. Nach Kupplungs-
oder Auftragsänderungen gelten weiterhin die bisherigen Neuprüfungen.

## Verbindlichkeit und bestehende Fahrwege

Die Erlaubnis wird vor Vorbereitung und bei der tatsächlichen Aufnahme erneut
geprüft. Ein Wechsel während der Warteschlangenbearbeitung kann deshalb keine
Freigabe in falscher Richtung erzeugen. Alle Teilfahrstraßen behalten die Ziel-
und Zuordnungskennung. Eine geänderte/gelöste Bindung macht alte Folgen ungültig.
Aktive betroffene Fahrstraßen und Richtungsbindungen schützen vor Umzuordnung.

Alte örtliche Fahrwege, die am früheren Hilfsziel vor der Strecke enden, einmal
neu anfordern bzw. neu berechnen. Die vollständige Folge erhält die neue Bindung.
Eine örtliche Bedienung darf dabei ausschließlich die zu dieser Strecke gehörenden
Blocksignale in Abfahrtrichtung mit anfordern; sonstige Nachbarsignale behalten
ihre bisherige Zuständigkeit. Anzeigen erzeugen keine zusätzliche Bedienbefugnis.

## Kurzer GFM-Abschnitt zwischen zwei Weichen

Die Reparatur bietet jetzt auch einen kürzeren gültigen Verlauf zwischen denselben
Grenzen auf demselben Gleisverlauf an. Beispiel des Regressionstests:
57,29 m alter Gesamtverlauf, aktuelle Grenzen bei 25,00 und 32,29 m → 7,29 m
aktueller Zwischenabschnitt. Dies ist ein nachgestelltes Beispiel, kein aus dem
Nutzerspielstand bestätigter aktueller Meterwert.

Unter **GFM-Prüfung und Reparatur** den betroffenen Eintrag auswählen →
**Dieses Grenzpaar neu erkennen / vollständigen Ersatz suchen** → aktuellen
Vorschlag prüfen → **Ersatz und zugehörige Alt-Einträge vorschauen** → übernehmen.
Gespeicherte und aktuelle Positionen stehen nebeneinander. Eine Abweichung wird
nicht mehr pauschal als vom Spieler versetzte Grenze bezeichnet. Der gesamte
alte und neue Bereich muss zur Bestätigung frei und ohne zugehörige Bindungen sein.
Interne Abschnitte eines gebauten Blockverbunds bleiben gegen Verwaisen geschützt.

## Abnahme

- Erlaubnis beim falschen Nachbarn: örtliches Ziel vor der Achse und vollständige
  Blockanforderung müssen gleichermaßen ablehnen; kein grünes Startsignal.
- Richtige Erlaubnis: eine Anforderung stellt Ausgang und passende Blocksignale;
  Gegenrichtung und Nachbar-Einfahrsignal bleiben unverändert.
- Erlaubnis während Vorbereitung abgeben: endgültige Anforderung muss ablehnen.
- Bindung ändern/lösen, fremde Blocksignale, unvollständige GFM, Belegung,
  Wegpunktbegrenzung, Speichern/Laden und Host-/Client-Anzeigen prüfen.
- W198/W212: gespeicherte und aktuelle reale Positionen vergleichen und den
  gültigen kleineren Abschnitt mit Vorschau übernehmen.

Build und Regressionen verwenden die vorhandenen Spielbibliotheken. Unity-Test am
aktuellen Nutzerstand und Mehrspielerabnahme stehen aus. Verknüpfte Gleisfortsetzungen
über Darstellungsumbrüche sind weiterhin der separat vorgemerkte nächste Schritt.

---

# 0.7.45-P0 – GFM-Fehlerübersicht und gezielte Reparatur

Interne Testfassung für Railroader 0.6.2; Host und Clients brauchen dieselbe Version.
Die Änderungen aus 0.7.44 bleiben enthalten. Ältere ZIPs bleiben unverändert.

## Was geändert wurde

- GFM-Fehler unterscheiden gelöschte Objekte, unfertige Baupläne, deaktivierte
  Melder, versetzte Grenzen und nicht verfügbare Weichenäste. Kennungen und
  gespeicherte Gleis-/Meterpositionen ersetzen Meldungen nur mit zwei internen IDs.
- Die GFM-Prüfung zeigt alle gespeicherten ungültigen Abschnitte gemeinsam,
  einschließlich Einträgen ohne Gleisbildzuordnung und Fehlern anderer Stellwerke.
  Die zugehörigen Tafeln werden genannt. Fehler stehen in der Abschnittsliste oben.
- Vom Strecken-/GWB-Fenster des Fahrdienstleiters führt **Alle GFM-Einrichtungsfehler
  im Editor prüfen** direkt zur Fehlerübersicht.
- **Bereinigung aller Abschnitte mit gelöschten Grenzen vorschauen** sammelt die
  dafür geeigneten Alt-Einträge. Alternativ einen einzelnen Abschnitt auswählen.
- **Ersatz bis zu den nächsten aktiven Grenzen suchen** bietet einen vollständigen,
  physisch geprüften Ersatzabschnitt an. Nach Entfernen eines Zwischenmelders kann
  dieser bis zur nächsten tatsächlichen GFM-Grenze reichen. Ein Signal ohne
  aktiven Melder bildet keine neue Abschnittsgrenze. Weichen oder weitere aktive
  Grenzen werden bei dieser Suche nicht übersprungen.
- Ein Ersatz sammelt auch die enthaltenen ungültigen Nachbarabschnitte mit gelöschten
  Grenzen. Bei Bestätigung werden ihre gespeicherten Verweise auf allen betroffenen
  Tafeln gemeinsam ersetzt. Reines Entfernen löst nur diese Verweise und fehlende
  Grenzmarken; Zeichnung, Weltobjekte, Signalverknüpfungen und Zugnummern-IDs bleiben.
- Die Streckenprüfung nennt mehrere Blockfehler und genaue GFM-Abdeckungslücken
  mit Gleis und Metern. Nur tatsächlich nutzbare Abschnitte zählen als Abdeckung.

## Vorgehen bei einer gelöschten Grenze

1. Im Editor **Abschnittmanager / GFM-Prüfung** öffnen oder den neuen Einstieg
   im Streckenfenster verwenden. **GFM-Prüfung aktualisieren** liest den Stand neu.
2. In **Alle gespeicherten GFM-Fehler** die betroffenen Abschnitte und Tafeln
   gemeinsam ansehen. **Diesen Fehler prüfen** öffnet die Auswahl. Bei beiden
   Grenzen stehen Herkunft, Zustand und gespeicherte Positionen.
3. Für einen vollständigen Ersatz **Ersatz bis zu den nächsten aktiven Grenzen
   suchen** wählen. Den angebotenen Abschnitt und seine Länge kontrollieren,
   dann **Ersatz und zugehörige Alt-Einträge vorschauen** wählen.
4. Alternativ **Entfernen dieser veralteten Zuordnung vorschauen** oder die
   gemeinsame Bereinigung der Abschnitte mit gelöschten Grenzen verwenden.
5. Die Vorschau nennt sämtliche entfernten Abschnitte, geänderten Felder und
   betroffenen Stellwerke. Erst **Diese GFM-Reparatur übernehmen** speichert.
   Abbrechen ändert nichts; vor dem Speichern wird eine Sicherung erstellt.
6. Anschließend Fehlerübersicht und GFM-Lücken erneut prüfen. Reines Entfernen
   erzeugt keine Freimeldung. Die bestehende Einstellung für automatische
   Katalogpflege bleibt erhalten; eine automatische Erkennung kann gültige
   aktuelle Abschnitte nach dieser Host-Einstellung weiterhin ergänzen.

## Bewusste Grenzen

Unfertige Baupläne werden von der Bereinigung ausgenommen: deren Bau ist zuerst
fertigzustellen. Deaktivierte Melder können an ihren Eigenschaften wieder aktiviert
werden. Abschnittsinterne Einträge eines automatisch gebauten Blockverbunds werden
hier nicht gelöscht; dessen gemeinsame Melder müssen zunächst wiederhergestellt
oder der Verbund gesondert bearbeitet werden. Die Meldung nennt diese Abhängigkeit.

Vor Übernahme werden Vorschau, Spielwelt und Konfigurationsstand erneut geprüft.
Betroffene Gleise müssen frei und lesbar sein; zugehörige Fahrstraßen und
Richtungsbindungen müssen aufgelöst sein. Unbeteiligte laufende Fahrstraßen sperren
nicht pauschal. Wartende Fahrstraßen-/Automatikanforderungen müssen vorher
zurückgenommen werden. Die Reparatur hebt keine echten Restverschlüsse auf.

## Spielabnahme

- Gelöschten Zwischenmelder mit zwei gespeicherten Nachbarabschnitten nachstellen:
  beide Fehler gemeinsam sehen, Ersatz bis zur nächsten aktiven Grenze vorschauen,
  beide Alt-Einträge auf mehreren Tafeln ersetzen; keine Doppelnummern erzeugen.
- Reines Entfernen testen: gezeichnete Felder bleiben, alte Verweise verschwinden,
  unüberwachte Lücken werden weiterhin gemeldet.
- Bauplan, deaktivierten Melder und versetzte Grenze getrennt testen. Bauplan muss
  als unfertig erkennbar bleiben und darf nicht als gelöschtes Objekt bereinigt werden.
- Belegte/unlesbare Gleise, betroffene Fahrstraße, Restbindung und veraltete
  Vorschau müssen ablehnen. Unbeteiligte Fahrstraße darf bestehen bleiben.
- Weitere aktive GFM-Grenzen und Weichen dürfen nicht überbrückt werden.
- Speichern/laden, Host/Client-Anzeigen und parallele Fahrstraßen aus 0.7.44 prüfen.

Build/Regressionen prüfen die Logik gegen die vorhandenen Spielbibliotheken.
Unity-Spieltest am Nutzerstand und Mehrspielerabnahme stehen noch aus.

---

# 0.7.44-P0 – Blockverbund und gezielte Änderungssperren

Interne Testfassung für Railroader 0.6.2. Host und Clients benötigen 0.7.44.
Diese Fassung bearbeitet die Rückmeldungen zum Blockbau aus 0.7.43.

## Blockbau und Darstellung

- Jede innere Blockgrenze erhält einen gemeinsamen B-Freimelder und zwei
  getrennte, längs versetzte Signalmasten für die Gegenrichtungen. Beim
  Richtungsgleis wird nur die benötigte Richtung angelegt.
- Der Versatz ist je Seite von 0,5 bis 25 m einstellbar, zunächst 10 m.
  Kurze Abschnitte begrenzen ihn auf ein Viertel der benachbarten Blocklängen.
  Seitenabstand, Seite, Höhe und Maßstab stammen aus den aktuellen
  Platzierungseinstellungen; der Spielabstand wird bei entsprechender Auswahl übernommen.
- Das Gleisbild zeigt eine Gleislinie mit getrennten Signalpunkten,
  gemeinsamen Meldergrenzen, Zugnummernfeldern und einem Erlaubnismodul.
- Das bedienende Ortsstellwerk ist wählbar, unabhängig von den beiden
  Streckennachbarn. Weitere Tafeln können als reine Anzeige ausgewählt werden.
  Dort sind Signale und Erlaubnismodul schreibgeschützt. Zugnummern sind Spiegel
  derselben Quelle und werden dadurch nicht als weitere Zugnummern geführt.
- Für jede neue Anzeigetafel muss die Position in einer eigenen Gleisbildvorschau
  bestätigt werden: grün = frei, rot = Kollision oder außerhalb der Tafel.
  Bis zur abschließenden Bestätigung bleibt alles ein Vorschlag.
- Im Editor wählt das Anfassen eines Verbundfeldes automatisch den gesamten
  Verbund zum Verschieben. Vom Fahrdienstleiter gibt es einen direkten Einstieg
  zum Verschieben im Editor. Signal-, Melder- und Zugnummern-IDs bleiben erhalten.
- Ein bestehender Verbund kann nur hinsichtlich Bedienzuordnung und Anzeigen
  geändert werden, ohne seine Weltobjekte neu zu platzieren. Bei vollständigem
  Neuaufbau bleiben vorhandene Hilfstastenverknüpfungen und Signalbeschriftungen
  erhalten. Manuell ergänzte fremde Felder werden nicht stillschweigend gelöscht;
  der Neuaufbau fordert dann zum vorherigen Herausverschieben auf.

## Streckensicherung und Signalbedienung

- Blockbau, Gleisbild und Streckendefinition werden gemeinsam gespeichert.
  Die aktivierbare Streckensicherung wird damit auch bei einer neuen,
  zuvor ungespeicherten Verbindung übernommen. Eine installierte, aber nicht
  aktivierte Blockanlage verweigert Fahrten auf ihrer Streckenachse.
- Die gemeinsame Streckenerlaubnis sperrt Gegenfahrten auch dann, wenn die
  beiden angeforderten Fahrwege unterschiedliche, freie Blockabschnitte benutzen.
  Gleichgerichtete Folgefahrten benötigen weiterhin ihre eigenen freien Fahrwege.
- Der Haltfall eines Blocksignals wartet auf den nachgewiesenen Durchlauf des
  zugeordneten Zuges hinter seinem gemeinsamen Melder. Er berücksichtigt die
  tatsächliche Fahrtrichtung und mindestens den normalen Signal-Durchfahrabstand.
  Dieselbe Durchfahrprüfung löst die vorhandene Zugnummernweitergabe aus.
  Zugschlussprüfung und Verschlussauflösung bleiben eigene Schritte.
- Der Assistent kann Anforderungskontakte mit Folgezielen einrichten. Die globale
  Kontakt-Automatik wird nur über ihr ausdrückliches Kontrollfeld eingeschaltet.
  Vorhandene Regeln bleiben erhalten; eine Lok braucht weiterhin die Betriebsart
  Zugfahrt. Der Blockbau allein ist keine neue fahrzeugbezogene Auftragsautomatik.
- Fehlende, unfertige oder falsch zugeordnete Signale/Melder sowie unvollständige
  GFM-Abdeckung verhindern die Freigabe. Ein Eigentümerwechsel überträgt die
  Blocksignale; die ursprünglichen Ein-/Ausfahrsignale bleiben bei ihren Bahnhöfen.

## Lokale Änderungen und Restbindungen

Hilfstastenänderungen prüfen die tatsächlich betroffenen Ziele und Abhängigkeiten.
Änderungen einer Strecke prüfen deren alte und vorgeschlagene Achse. Unbeteiligte
Fahrstraßen oder Richtungsbindungen verhindern solche lokalen Änderungen nicht
mehr pauschal. Eine gehaltene Richtung ohne Restverschluss sperrt für sich allein
keine Umbenennung oder Änderung der zusätzlichen Anzeigetafeln.

Verbleibende Sperren nennen Fahrstraße/Strecke, Zustand, Freiprüfstand und Grund.
Bei Richtungsbindungen wird nach Möglichkeit der erste belegte oder nicht lesbare
Abschnitt genannt. Die Liste im Editor beschreibt den letzten Speicherversuch;
nach Auflösung oder abgeschlossener Freiprüfung erneut speichern. Echte Belegung,
Zugschlussbindungen und Schutzverschlüsse werden nicht pauschal zurückgesetzt.
Ein geänderter GFM-Katalog kann bei einer unbeteiligten Richtungsbindung aktualisiert
werden; die Geometrie einer gehaltenen Blockachse darf dabei nicht wechseln.

## Übernahme und Grenzen

Vorhandene 0.7.43-Anlagen werden nicht beim Laden automatisch umgebaut. Die
Umstellung erfolgt ausdrücklich im Blockassistenten mit Vorschau und Bestätigung.
Bei einem alten Doppelkopf-Mast bleiben beide logischen Signal-IDs und Kennungen
sowie seine Freimeldenummer erhalten; die Köpfe werden auf getrennte Masten verteilt.
Nur der eindeutig erkannte alte automatische Diagrammstreifen wird ersetzt.

Die vorhandene Blockaufteilung eines bereits mit 0.7.44 angelegten Verbunds bleibt
in dieser Fassung bestehen. Signalversatz, Bedienort und Diagrammposition sind
änderbar; ein anderer Zuschnitt vorhandener Abschnitte wird abgewiesen, damit
keine alten Abschnitte oder Melder verwaisen. Der Bauassistent gilt weiterhin nur
für weichenfreie Strecken und berechnet keine vorbildgerechten Bremswege.

Die im Spiel bestätigten parallelen Fahrstraßen bleiben erhalten. Neue autonome
Anschluss-/Kreuzungssteuerung, Bot-Auftragsketten und neue hochwertige Signalmodelle
sind spätere Arbeitsschritte. Die öffentlichen Anleitungen für 0.7.40 und alle
bereits vorbereiteten älteren ZIPs bleiben unverändert.

Build, Regressionen und EN/DE-Prüfungen werden gegen die vorhandenen
Spielbibliotheken ausgeführt. Die genaue Ursache der gemeldeten Live-Situation
ist ohne aktuellen Diagnoseexport nicht bewiesen. Der Unity-/Mehrspieler-Test
dieses Updates steht noch aus; siehe [Einrichtung und Spielprüfung](Guides/TEST-0.7.44.de.md).

---

# 0.7.43-P0 – Einrichtung durch Anklicken

Neue interne Testfassung auf Basis 0.7.42. Vorhandene vorbereitete ZIPs bleiben
unverändert. Railroader 0.6.2; Host und Clients benötigen dieselbe Modversion.

## Hilfsziele

Im Editor unter Eigenschaften **Ziel durch Anklicken** öffnen. Ein eindeutiges,
im Gleisbild verbundenes Einfahrsignal kann automatisch vorgeschlagen werden.
Alternativ den Bezug im Gleisbild, in der Welt oder über die Namensliste wählen.
Die Gegenrichtung des Bezugssignals bildet die reale Anfahrt des Hilfsziels.
**Feld speichern** bestätigt den Vorschlag. Gleis-ID, Meter und Richtung bleiben
unter **Manuelle Gleisposition / erweiterte Einstellungen** bearbeitbar.

Der bestätigte Bezug verwendet eine feste Signal-ID. Verschieben und Umbenennen
des Bezugssignals werden bei der Fahrwegprüfung nachgeführt. Ein fehlender oder
noch nicht gebauter Bezug ergibt kein verwendbares Ziel. Aktive Fahrstraßen und
Streckenbindungen zum Hilfsziel schützen auch das Bezugssignal gegen Umbau.
Vorhandene Hilfsziele ohne Bezug bleiben manuell eingerichtet.

## Strecken und Blockeinrichtung

Jede Endsignalgruppe besitzt **Im Gleisbild auswählen** und **Signal in der Welt
auswählen**. Die Gleisbildauswahl bietet eine eigene Stellwerksauswahl und
hervorgehobene geeignete Ziele, ohne die gerade bearbeitete Tafel zu wechseln.
Suchlisten bleiben verfügbar. **Endpunktzuordnungen automatisch vervollständigen**
übernimmt nur eindeutige vorhandene Ein-/Ausfahrrollen; fehlende und mehrdeutige
Gruppen werden benannt. Verlauf und GFM werden danach wie bisher geprüft und
erst mit **Streckenkonfiguration übernehmen** gespeichert.

Von einem örtlichen Streckenmelder kann ein vorhandenes Einfahrsignal des
Nachbarn als schreibgeschützte Anzeige auf ein freies Nachbarfeld gesetzt
werden. Die Zuständigkeit des Originals bleibt erhalten. Eine erteilte Erlaubnis
wird jetzt mit ausgefülltem Pfeil dargestellt; ohne gültigen Erlaubniszustand
erscheint kein solcher Leuchtpfeil. Anfrage und Erlaubnis bleiben getrennt.

Nach erfolgreicher Verlaufserkennung bietet der optionale Blockassistent:

- 1–16 Blöcke oder einen ungefähren Abstand entlang des tatsächlichen Gleises;
- veränderbare Grenzentfernungen, vorhandene Signalstandorte und Wiederverwendung;
- neue Signal-/GFM-Grenzen, gegebenenfalls beide Mastrichtungen;
- vollständige Abschnittsdefinitionen und eine zusätzliche Blockdarstellung im
  ausgewählten örtlichen Stellwerk einschließlich Zugnummernfeldern;
- optional Anforderungskontakte mit Zielen zum nächsten Block in beiden Richtungen;
  globale Automatik bleibt unverändert, vorhandene Regeln werden nicht überschrieben;
- gemeinsame geprüfte Übernahme von Bauobjekten und Stellwerksdaten mit Sicherung.

Drei Kilometer und drei Blöcke ergeben zwei innere Grenzen. Vorhandene Signale
dürfen nicht durch einen nachträglich geänderten Vorschlag übersprungen werden.
Die Enden müssen eindeutige aktive GFM-Grenzen besitzen. Der Assistent gilt nur
für weichenfreie Strecken; Weichenbereiche benötigen weiterhin eigene Einrichtung.
Er berechnet keine vorbildgerechten Bremswegabstände. Mindestlängen sind technische
Spielgrenzen. Materialpflichtige Objekte bleiben nach Übernahme Baupläne und
werden erst nach Fertigstellung betriebsbereit. Ein bloßer Vorschlag baut nichts.

## Editor und bestehende Automatik

Im Auswahlmodus ein vorhandenes Feld oder eine markierte Gruppe mit gedrückter
linker Maustaste ziehen. Die Vorschau ist transparent, gültige Ziele sind grün,
unzulässige rot. Loslassen übernimmt; Escape bricht ab. Belegte Felder werden
nicht überschrieben. Zuordnungen und Nummernquellen bleiben erhalten; Rückgängig
nutzt den bisherigen Editorverlauf. Leere Flächen bleiben zum Verschieben der
Ansicht nutzbar. Zeichnen und Setzen ziehen keine Felder mit.

Start, Erstziel und Ausweichziel der vorhandenen Zugfahrt-Kontaktautomatik können
nun ebenfalls im Gleisbild gewählt werden. Anschlussstelle und Überholbahnhof
sind getrennte Anlagenarten; die bisherigen gespeicherten Arten bleiben kompatibel.
Die zentrale Betriebsstellenansicht bietet einen direkten Einstieg in örtliche
Einstellungen. Die neue Bezeichnung erzeugt allein noch keine Anschlussbedienung,
Aufnahmegleisreservierung oder selbstständige Kreuzungssteuerung.

## Parallele Fahrstraßen

Gemeinsame reine Überwachungsbereiche führen nicht mehr allein zur Ablehnung,
solange keiner der tatsächlichen Fahrwege hineinführt. Eine in gleicher Stellung
benötigte Schutzweiche kann von kompatiblen Fahrstraßen gemeinsam gehalten werden.
Zwei tatsächliche Weichenfahrwege bleiben exklusiv; Gegenstellungen, befahrene
Schutzbereiche, widersprechende Signalbegriffe und unbekannte Belegung bleiben
gesperrt. Beim Auflösen einer Fahrstraße bleibt der Schutz anderer Fahrstraßen
bestehen. Alte Fahrwege ohne gerichtete Geometrie werden konservativ behandelt.

## Abgrenzung und Prüfung

Dies ist der erste technische Umsetzungsschritt der besprochenen Vereinfachung.
Die bestehende Kontakt-Automatik erhält eine einfachere Einrichtung. Neue
fahrzeugbezogene Mehrschrittaufträge, Spielerkommunikation/Bedienrechte,
Anschlussbedienung mit Rückfahrverfahren, automatische Kreuzungsannahme,
fahrstraßenabhängige Mehrziel-Vorsignale und astbezogene Geschwindigkeiten bleiben
gesonderte Arbeitspakete. Die öffentliche 0.7.40-Anleitung wird nicht verändert.

Gebaut und offline gegen die bereitgestellten Spielbibliotheken geprüft.
Drag-Vorschau, Gelände-/Modellpositionen und Host-/Client-Anzeigen brauchen einen
Spieltest. Siehe die bilinguale Anleitung `Guides/TEST-0.7.43.de.md` / `.en.md`.


# 0.7.42-P0 — Fahrstraßen schrittweise vorbereiten

- Manuelle Start-Ziel-Anforderungen, gespeicherte Fahrstraßen/Folgen und vorhandene Kontakt-Automatik verwenden eine begrenzte FIFO-Warteschlange statt vollständiger Berechnung im Klick oder Kontaktaufruf.
- Verarbeitung pro Frame mit weichem 2-ms-Zeitbudget und höchstens 512 kleinen Schritten. Zwischen nativen Schutzberechnungen einzelner Teilfahrstraßen wird abgegeben. Einzelne native Aufrufe und die abschließende atomare Reservierung können dieses Budget überschreiten; keine harte Framedauer-Garantie.
- Reine Wegsuche auf getrennter Datenmomentaufnahme im Hintergrund. Unity-, Gleisnetz- und Stellbefehle bleiben im Hauptthread. Jeweils nur eine Vorbereitung aktiv.
- Fortschritt und Anzahl wartender Anforderungen, Zusammenführung gleicher Anforderungen, Grenze von 32 sowie Abbruch. HALT, Spielstandwechsel und Wiederherstellung verwerfen ausstehende Arbeit; vorhandene Verschlüsse bleiben bestehen.
- Konfigurations-/Anlagenänderungen verwerfen veraltete Vorbereitung. Das native Gleisnetz wird erneut aufgenommen und verglichen. Fahrwege und aktuelle Belegung werden vor dem Stellen erneut geprüft.
- Die gesamte Folge wird weiterhin gemeinsam reserviert oder zurückgerollt. Während der Suche keine Teilfestlegung; GFM, Weichen-, Flanken-, Block- und GWB-Prüfungen sowie bestehende Aktivgrenzen bleiben erhalten.
- Kontakt-Automatik verwendet vorbereitete Fahrwege erst nach erneuter Prüfung von Zug, Verband, Auftrag, Richtung und Betriebszustand; höchstens eine vorbereitete Kontakt-Anforderung je Automatiktakt. Wiederholung und Ausweichziel bleiben eingerichtet.
- Flankenschutz-Gleisnetzzuordnung mit linearem Aufbau statt vollständiger Netzsuche für jeden Knoten. Wiederkehrende Vorschlagsteile werden innerhalb einer Vorbereitung wiederverwendet.
- Englische/deutsche Warteschlangenhilfe; veraltete Hilfstasten-/GWB-Angaben in F1 korrigiert. Kommunikation und allgemeiner Automatik-Ausbau bleiben Konzepte.

Konkreter Spielhänger/Beinaheabsturz, Framedauer und Mehrspieler sind noch im tatsächlichen Spielstand abzunehmen. Synthetische Messungen sind kein Spiel-FPS-Nachweis.

# 0.7.41-P0 — GWB-Erlaubnis und Modul im Gleisbild

- Erlaubnis explizit beim Nachbarn anfragen und nur vom Inhaber abgeben; eigene Anfrage zurücknehmen. Alte Bedienbefehle und fremde Betriebsstellen werden zurückgewiesen.
- Fahrstraßen übernehmen keine Erlaubnis mehr. GFM, Fahrstraßen- und Blockprüfung bleiben aktiv und getrennt.
- Regelerlaubnis nach vollständiger Start-Freiprüfung einstellbar; neue Strecken standardmäßig manuell. Optional Abgabe ohne Anfrage und automatische Bestätigung ausdrücklicher Anfragen.
- Richtungswechsel nur bei vollständig freier Strecke ohne Fahrstraßen/Restverschlüsse, nach drei Freimeldungen und erneuter aktueller Prüfung. Erlaubnis bleibt zwischen Zügen bestehen; parallele Gleise unabhängig.
- Neues Gleisbildmodul mit durchgehendem Gleis und zwei umrandeten Gegenpfeilen. GFM-Zuordnungen bleiben erhalten; alte freistehende Melder bleiben lesbar.
- Hilfstaste wahlweise Fahrstraßenziel oder Erlaubnisbedienung; reine Erlaubnistaste ohne Weltanker und ohne Fahrstraßenzielrolle. Verknüpfung mit Modul, Live-Anfragen, Host-Seitenauswahl und Löschbereinigung.
- Keine Baumaßnahmen, Weltobjekte oder Baumaterialkosten. Englische/deutsche Oberfläche und Anleitungen aktualisiert. Allgemeines Tutorial bleibt in Arbeit; Spiel-/Mehrspielerabnahme offen.

[Einrichtung](Guides/FREE-LINE-GWB.de.md) · [Spieltests](Guides/TEST-0.7.41.de.md)

# 0.7.40-P0 – Fahrstraßen mit tatsächlichen Weichenschutzgrenzen

- Getrennte pauschale 25-m-Weichenschutzbereiche im nativen Fahrstraßenaufbau entfernen. Zug- und Rangierfahrten verwenden dieselbe effektive physische Weichendefinition wie die GFM-Anzeige: reale Meldergrenzen und ausdrückliche Armzuordnungen, Standardbereiche bei fehlender näherer Grenze.
- Dieselbe Definition auch für zusätzlich abweisende Flankenschutzweichen verwenden. Gefahrenkorridore und andere Weichenarme bleiben geschützt.
- Wirksame Schutzgeometrie im Fahrweg kennzeichnen und vor Wiederverwendung abgleichen. Alte gespeicherte Fahrwege mit Weichen müssen einmal neu berechnet werden. Änderungen unter bestehender Fahrt nehmen die Zustimmung zurück und behalten Verschlüsse.
- Belegungsablehnungen nennen Zug- oder Rangierfahrt und den geprüften Bereichstyp.
- Beide Richtungen und beide Fahrarten mit einem vor der tatsächlichen Meldegrenze wartenden Fahrzeug prüfen; Schutzbereiche und Quellen bleiben unabhängig vom Fahrzeug erhalten.

[Deutsche Anleitung/Spieltests](Guides/TEST-0.7.40.de.md), [English](Guides/TEST-0.7.40.en.md). Konkreter Spielstand und Mehrspieler weiterhin offen; keine Veröffentlichung oder Installation. Die früher hochgeladene Diagnostics.zip wurde nicht ausgewertet.

# 0.7.39-P0 – Haltfallpunkt, Belegungsintervalle und Anlagenprüfung

- Gemeinsamer gerichteter Passagepunkt für Haltfall und Nummernweitergabe: standardmäßig 5 m hinter dem Signal, Host-Einstellung 0–25 m; kurze Wege begrenzen den Abstand. Zugschlussauflösung bleibt getrennt.
- Pauschalen Schluss von beliebiger Abschnittsbelegung auf Einfahrt bei gerichteter Fahrzeugverfolgung entfernen. Fremde Einfahrt und unbekannte Daten weiterhin unmittelbar sperren.
- Belegung aus tatsächlichen Fahrzeug-Gleissegment-/Meterintervallen lesen; gemeinsame Grundlage für Gleisbild, GFM, Fahrweg, Weichen und Flanken. Nicht auflösbare Fahrzeuglage bleibt unbekannt.
- Örtliche zusätzliche GFM-Prüfung auf den angeforderten Anteil eines längeren A/B-Abschnitts begrenzen; eine Belegung hinter dem Start nicht pauschal dem freien Anteil davor zuschlagen.
- Bekannte Belegung auch in Zwischenabschnitten einer Rangierfahrt außerhalb der Weichenschutzbereiche zulassen. Weichen-, Konflikt- und Unbekanntprüfungen sowie Zielauflösung erhalten.
- Lesbare Belegungsursachen mit Signal-, Weichen-/GFM-Kontext und gerundeten Metern. Präzise Werte weiterhin exportieren.
- Abschnittmanager direkt im Editor-Menü; neues Einlesen aller Anlagenelemente mit anschließender Prüfung und Erhalt gespeicherter Einstellungen/IDs.
- Neunummerierung nur als separates Vorschaukonzept beschrieben, nicht implementiert.

[Deutsche Anleitung und Spieltests](Guides/TEST-0.7.39.de.md), [English](Guides/TEST-0.7.39.en.md). Der konkrete Spielstand und Mehrspieler sind noch zu prüfen. Keine Veröffentlichung oder Installation. Die frühere hochgeladene Diagnostics.zip ist weiterhin nicht ausgewertet; kein Logbefund daraus wird behauptet.

# 0.7.38-P0 – Belegungsursachen und lokale Diagnosemomentaufnahme

- Fahrweg-/Weichenablehnung mit genauer Herkunft, Gleis und Meterbereich; Belegung von unbekannter Rückmeldung unterscheiden.
- Bei abweisender Flankenschutzweiche den tatsächlichen Gefahrenkorridor reservieren, nicht zusätzlich andere Gleisäste. Vor notwendigem Umlauf weiterhin alle Schutzbereiche freiprüfen. Alte gespeicherte Flankenberechnung ausdrücklich neu berechnen lassen.
- Zugeordnete Hilfsziele unabhängig vom zuletzt gewählten Feld auflösen und im Bedienkontext anbieten. Bestehende Signal-/Fahrartprüfungen erhalten.
- Nummernweitergabe erst nach erfolgreichem Commit erledigen, blockierte Ziele erneut prüfen, fremde nachfolgende Nummer nicht übernehmen. Alte Präfix-Duplikate beim Transfer entfernen.
- Eindeutige normale Anzeigequelle für aus Fahrzeugen gelesene Nummern über mehrere Abschnitte; echte Anzeigekopien erhalten. Doppelte Quellen-/Nummernzuordnung als unbekannt darstellen und gezielte Korrektur anbieten.
- GFM-Prüfung: örtlich nachvollziehbare Reihenfolge sowie Ursprungssignal/-objekt, Gleis/Meter, stabile Kennung und Zeichnungsorte jeder Grenze anzeigen. Reine Endpunktmarkierung bei null Feldzuordnungen erklären. Alte Signalgrenzen ohne aktiven GFM nicht als gültige Detektoren bestätigen.
- Manueller Diagnose-ZIP-Export mit Konfiguration, Grenzherkunft, Netz, Betrieb, Fahrzeugen, Nummern und tatsächlichen Abfragen des letzten Stellversuchs. Optionaler lokaler Export-/Synchronisationsordner, keine automatische Übertragung. Getrennte Archive mit Größenbegrenzung.
- Kaltstart bestätigt: Signale Halt, keine Wiederherstellung früherer Fahrtfreigaben oder Laufzeitverschlüsse.

[Deutsche Bedienung und Spieltests](Guides/TEST-0.7.38.de.md), [English](Guides/TEST-0.7.38.en.md). Keine Veröffentlichung oder Installation. Lokale Spielwelt, GFM-A-051 und Mehrspielerabnahme bleiben offen. Die früher hochgeladene Diagnostics.zip wurde wegen des schreibgeschützten Dateiimports nicht ausgewertet; keine daraus abgeleiteten Befunde.

# 0.7.37-P0 – Bahnhofsfahrt und manuelle GFM-Einrichtung

- Örtliche Zugfahrten nicht an übergreifenden GFM-Richtungsketten hinter dem Startsignal sperren. Direkt betroffene gültige GFM, native Belegung, Weichen, Flankenschutz und Verschlüsse weiterhin prüfen.
- Unvollständige GFM-Abdeckung bei manueller Bedienung als ausdrücklichen Fdl-Modus zulassen; keine bestätigte Freimeldung vortäuschen. Automatik bleibt an vollständige Abdeckung gebunden.
- Unveränderte Richtungstopologie nicht durch allgemeine Versions-/Darstellungsänderungen sperren.
- GFM nur aufheben, Hintergrundkatalog pausieren und Altaufträge verwerfen; Weltobjekte und Zeichnung erhalten.
- Weltwerkzeug von/bis sowie direkte Signal-/B-Kasten-Zuordnung zu nativen Weichenarmen. Erkennungsprobleme anklickbar prüfen.
- Gespeicherte Fahrstraßenplanung zugänglich halten und den Aufzeichnungsordner in Einstellungen/Diagnosefenster anzeigen.
- [Bedienung und Spieltests](Guides/TEST-0.7.37.de.md). Aktueller Teststand, nicht die erste öffentliche Version.

# 0.7.36-P0 – GFM-Neuscan und Diagnose

- Vollständige GFM-Neuerkennung mit Vorschau, Sicherung und örtlichem oder installationsweitem Umfang. Alte Abschnittszuordnungen gezielt ersetzen; Zeichnung und Weltverknüpfungen erhalten.
- Native Weichenarme anhand eindeutiger Grenzpaare zuordnen und bei Bedarf im Editor korrigieren. Keine automatische Vermutung bei widersprüchlicher Topologie.
- Je Weichenarm dessen eigene Belegung darstellen; betriebliche Gesamtweichenprüfung erhalten.
- Abschnittsliste, Grenz-/Abschnittshervorhebung und Fahrweg-Test im Editor ohne Bedienbefehle. Gültige Abdeckung und genaue Meterlücken unterscheiden; ungespeicherte Vorschläge separat anzeigen.
- [Bedienung und Spieltests](Guides/TEST-0.7.36.de.md). Lange Abschnitte, native Eingabe/Belegung und Mehrspieler praktisch prüfen. Fahrwegmodus unverändert.

# 0.7.35-P0 – Signale fortlaufend platzieren

- Signalplatzierung nach jedem Setzen beibehalten, keine zusätzliche Signal-Konfiguration öffnen. Gilt für Haupt-, Vor- und Rangiersignale samt optionalen Hl-Modellen.
- Signaltyp und Bauvorgaben innerhalb der Serie behalten. Frei bewegen und Kamera weiter mit rechter Maustaste bedienen; bestehende Signale währenddessen nicht zum Bearbeiten öffnen.
- Escape, Moduswechsel oder erneutes Öffnen des Baumenüs beendet die Serie. Bereits gebaute Signale bleiben erhalten und sind danach normal bearbeitbar.
- [Bedienung und Spieltests](Guides/TEST-0.7.35.de.md). Einzelverschiebung vorhandener Objekte bleibt erhalten.

# 0.7.34-P0 – GFM-Wegprüfung

- Gespeicherten GFM-Verlauf physisch prüfen statt exakte Metertextfolgen mit einer erneuten globalen Suche zu vergleichen. Rundungswechsel, zusammenhängende Teilstücke und lange Wege werden dadurch nicht mehr fälschlich als Weichenproblem gemeldet.
- Tatsächliche Unterbrechungen, Weichenquerungen und übersprungene Grenzen weiter sperren; betroffene GFM-Grenzen und Weichen benennen.
- Geänderte physische Grenzen in der Blockprüfung berücksichtigen, gemeinsame A/B-Kästen als Weichengrenzen erhalten.
- [Spieltests und verbleibende Grenzen](Guides/TEST-0.7.34.de.md). Keine Änderung der KI-Rangierwende oder bestehenden Verschlussregeln.

# 0.7.33-P0 – Weltzuordnung und Freimeldestrecken

- Signal, Weiche oder GFM in der Welt mit einem Klick zuordnen; keine zusätzlichen Signalfenster und kein natives Weichenwerfen durch den Auswahlklick.
- Freistehende C-Grenzen dem vorhandenen Gleisbild zuordnen und im Fahrdienstleiter sichtbar machen. Nur die konfigurierte Seite wird überwacht.
- Schnellaktionen unmittelbar speichern. Normaler Rechtsklick auf Weltsignale bietet Bearbeiten; Kommunikationsschnittstelle vorerst unsichtbar.
- Durchgehende Abschnitte über virtuelle Ziele und melderlose Zwischensignale, unabhängige Kreuzungsgleise und eindeutige Mastrichtungen erkennen. Gemeinsames A/B an reale Weichenüberwachung anbinden.
- Suchlänge als optionaler Vorschlagsfilter, keine Begrenzung der physischen Belegungsstrecke.
- Lesbares Fahrwegauswahlfenster, mehrere Ein-/Ausfahrten je Streckenrichtung, Zug-Hilfsziele als Ankunftsziele und schreibgeschützte Nachbarsignal-Anzeigen.
- [Einrichtung und Spieltests](Guides/TEST-0.7.33.de.md). KI-Rangierwende hinter deckendem Signal erst im nächsten Update vorgesehen.

# 0.7.32-P0 – Blockeinrichtung und Betriebsrückmeldungen

- Strecken-/Erlaubnismelder tatsächlich platzierbar, direkte örtliche Blockeinrichtung mit gemeinsamer Verbindung und überprüfbaren GFM-Vorschlägen.
- Hl-Signalsystem unter Signalbetrieb und Darstellung ausdrücklich mit Hinweis Test aktivierbar.
- Kontakte vor dem Bauen einrichten, Blickrichtung übernehmen und erstes Hauptsignal vorschlagen. Start-/Zielsignale und Signalfelder direkt in der Welt auswählen.
- GFM-Bauvorgabe für neue Signale und direkte Abschnittserkennung im vorhandenen Gleisbild. Elektrische Sammelumrüstung auf eindeutig zugeordnete örtliche Weichen begrenzen.
- Zielgerichtete Fahrwegsuche ohne Aufhebung der Such- und Sicherungsgrenzen.
- Auflösungsstatus mit Restzeit oder konkretem Blockierungsgrund, auch bei Mitspielern.
- Kurze temporäre Sicherungsnamen beheben die im Windows-LocalLow-Pfad erreichte 260-Zeichen-Grenze vor Betriebswechseln.
- [Einrichtung und offene Spieltests](Guides/TEST-0.7.32.de.md). Enthält die bisherigen Änderungen; alte ZIPs bleiben unverändert.

# 0.7.31-P0 – Freischaltung nach Karte und Meilensteinen

- Vorhandenen nativen Signalausbau Whittier–Bryson erkennen und dessen Voraussetzungen für den unabhängigen Signaltechnik-Meilenstein übernehmen. Kein Kauf des nativen CTC erforderlich; Grundpreis des eigenen Ausbaus bleibt 7.500 $ mit nativer Rabattberechnung.
- Ohne nativen Signalausbau standardmäßig sofort verfügbar. Optionales Regelwerk des Kartenherstellers neben der Kartendatei sowie ausdrückliche Host-Vorgaben unter Company → Optionen → Mods → Signaltechnik → Material und Baukosten.
- Bestehende Freischaltungen und Sandkasten bleiben nutzbar. Keine zweite Zahlung bei bereits freigeschalteten oder kostenlos verfügbaren Anlagen.
- Bau-, Editor- und Fahrdienstleitermodi bis zur Freischaltung sperren und den Grund anzeigen; Einstellungen und Wiederherstellung bleiben erreichbar. Gemeinsame Gleisbild-/Fahrstraßenschreibzugriffe prüfen die Freischaltung ebenfalls.
- Host-Vorgaben im gemeinsamen Anlagenbestand speichern. Fehlerhafte Herstellerdateien werden gemeldet; fehlende Voraussetzungen werden nicht übersprungen.
- [Einrichtung, Kartenvorlage und Spieltests](Guides/PROGRESSION.de.md). Enthält alle Änderungen von 0.7.30. Alte ZIPs bleiben unverändert; Spiel- und Mehrspielertests stehen aus.

# 0.7.30-P0 – Hl-Signalpaket zum Testen

- Zusätzliche selbst erzeugte Hl-Haupt-/Mehrabschnitts- und Vorsignale nach Ril 301.0103. Direkt im Baumenü auswählbar; bestehende hohe H/V-Masten und Vorsignale bei gleichem Standort und gleichen Kennungen umrüstbar.
- Anzeige aus vorhandener Fahrfreigabe und Vorsignalziel: Hp 0, Hl 1/3a/7/9a/10/12a sowie Ra 12. Oberes Ankündigungslicht blinkt; untere Geschwindigkeitslichter bleiben stetig. Ne2 am einzelnen Vorsignal und gelbes Dreieck am kombinierten Hauptsignal.
- Bestehende IDs, Gleisfreimeldung und Stellwerkszuordnungen bleiben erhalten. Standard H/V, keine neuen Pflichtmods und keine automatische Umrüstung alter Anlagen.
- Vollständige 60/100-km/h-Kombinationen nur in der mitgelieferten Vorschau. Native KI-Geschwindigkeitsbehandlung unverändert; keine Zusage exakter 40/60/100-Einhaltung. Reale Spiel- und Mehrspielerabnahme offen.
- [Bedienung, Grenzen und Spieltests](Guides/HL-TEST.de.md). Enthält sämtliche Änderungen aus 0.7.29; frühere ZIPs bleiben unverändert.

# 0.7.29-P0 – Kreuz, Kennlicht und direkter Weicheneditor

- Ungebundene eigene Signale automatisch mit weißem Ungültigkeitskreuz und schwarzen Rand markieren; normale Lampen aus, native KI berücksichtigt sie nicht.
- Einzelne Signalrichtungen oder Stellwerke betrieblich abschalten: Kennlicht, eigene native Signalhalteziele entfallen. ESTW umfasst eigene und verknüpfte örtliche Zuordnungen.
- Nur ausschließlich abgeschalteten Bereichen zugeordnete Weichen werden für die KI freigegeben. Aktive Mitbenutzer und native Regeln behalten Vorrang. Wiederinbetriebnahme stellt normalen Schutz wieder her.
- Betroffene Fahrstraßen und Richtungsbindungen müssen vor der Umschaltung sicher aufgelöst sein; andere Bereiche dürfen weiterlaufen. Neue unzulässige Fahrwege werden zentral abgewiesen.
- **Weiche bearbeiten** öffnet direkt die geklickte Weichenkonfiguration; Zuordnung zum Gleisbild bleibt separat.
- Enthält alle Änderungen von 0.7.28. [Bedienung und Spieltests](Guides/TEST-0.7.29.de.md).

# 0.7.28-P0 – gemeinsamer GFM-Kasten A/B

- Bei exakt gemeinsamem Signal-A-/Weichen-B-Gleispunkt ein Kasten mit Beschriftung A/B; doppelte automatische Weichenkästen entfallen an dieser Stelle.
- Signal-, Weichengrenz- und passende Abschnittskennungen bleiben erhalten. Die Netzerfassung übernimmt die gemeinsame Grenze einmal.
- Mitbenutzende Weichen am Signal anzeigen, gemeinsamen Kasten aus der Weichen-GFM öffnen und A/B in Grenzanzeigen verwenden.
- Keine automatische Zusammenfassung nur aufgrund von Nähe oder bei C-Grenzen. Allgemeine Platzierung anderer Nachbarkästen bleibt unverändert.
- Enthält alle Änderungen von 0.7.27. [Bedienung und Spieltests](Guides/TEST-0.7.28.de.md).

# 0.7.27-P0 – Hauptsignalrollen direkt am Signal

- Bestehende Rollen Einfahr-, Zwischen-, Ausfahr- und Blocksignal direkt am Hauptsignal und im Gleisbild erreichbar, jeweils mit Erklärung.
- Jede Mastrichtung separat einstellen. Rolle ohne Änderung der Zielautomatik übernehmen; feste Kennung und ID bleiben erhalten.
- Rollenwechsel respektieren aktive Fahrstraßen, Richtungsbindungen und passende Ein-/Ausfahrrollen eingerichteter freier Strecken.
- Sämtliche Änderungen von 0.7.26 sind enthalten. [Bedienung und Spieltests](Guides/TEST-0.7.27.de.md).

# 0.7.26-P0 – Modulbedienung, Weichengrenzen und Bahnhofssuche

Lokaler Teststand für Railroader 0.6.2; Spiel- und Mehrspielerabnahme ausstehend.

- ESTW-Module mit der Maus verschieben und per Rechtsklick bearbeiten. Getrennte Steuerfläche und Vorschau, Tafel einpassen statt festem Miniaturzoom.
- Weichen-GFM direkt per Rechtsklick aus Welt und gebundenem Gleisbild aktivieren. Bestehende Identitäten und Kastenversätze bleiben erhalten.
- Bahnhofssignale und GFM-Grenzen für eine bestehende Tafel suchen; Suchgebiet aus vorhandenen Zuordnungen oder gewähltem Startsignal. Eindeutige Feldnamen zuordnen, Zeichnung erhalten. Anonyme Felder werden nicht geraten.
- Gebundene Signalbeschriftungen, eigene Haupt-/Rangiertastenpaare und GFM-Referenzen nach Signalumbau aktualisieren.
- Matter C-Pfeil ohne Emission und ohne gegenläufige doppelte Fläche.

[Einrichtung und Spieltests Deutsch](Guides/TEST-0.7.26.de.md) · [English](Guides/TEST-0.7.26.en.md).

# 0.7.25-P0 – Schematische Erkennung, GFM und sichtbare Modultafel

Neuer lokaler Teststand für Railroader 0.6.2. Bestehende Testwelt weiterverwenden; Host und Clients benötigen dieselbe Version. Spiel- und Mehrspielerabnahme stehen aus.

- Schematische Hauptstränge und Nebengleiszeilen ersetzen die reine Weltkoordinaten-Skalierung. Standardmäßig begrenzter Zeichenbereich um platzierte Signale, vollständige physische Datenbasis bleibt erhalten. Vorschau und ausdrückliche Übernahme; alte Zeichnung bleibt im Archiv.
- Vorhandene Signal-/Weichenreferenzen und aktivierte GFM-Grenzen werden berücksichtigt. Eindeutige bestehende Abschnitts-IDs werden wiederverwendet; fehlende Signal- und Weichen-GFM wird aufgelistet.
- A am Signal, B frei/an der Weiche, C einseitig mit Pfeil und Richtungsprüfung. Neue Signale standardmäßig mit A. Neues dauerhaft aktives GFM-Schnellwerkzeug, Escape beendet es. Mehr Mastabstand, Kollisionsprüfung und Meldung bei nicht platzierbarem angebautem Kasten.
- Sichtbare ESTW-Modultafel über **Module** in der Stellwerksauswahl: Einfügen, X/Y-Verschieben, Lösen und örtliche Bearbeitung zusammen erreichbar.
- Technische Signalkennung und Richtungs-IDs schreibgeschützt; Anzeigenamen bleiben frei. RS-Aufwertung korrigiert den HS-Präfix bei unveränderter interner ID und GFM-Nummer. Keine automatische Wiederverwendung gelöschter Nummern.
- Schutz elektrischer Weichen auch in der nativen Umstellbarkeits-Vorausschau. Wegpunktpassende Fahrwege werden vor der Bevorzugung der aktuellen Weichenlage ausgewählt. Das tatsächliche Anhalten muss im Spiel geprüft werden; kein automatisches Vor-/Zurücksetzen enthalten.

Einrichtung, Grenzen und gezielte Tests: [Deutsch](Guides/TEST-0.7.25.de.md), [English](Guides/TEST-0.7.25.en.md). Die beigefügte technische A/B/C-Modellvorschau ist kein Spiel-Screenshot.

# 0.7.24-P0 – ESTW-Verwaltung, Erkennung und Automatik-Kontakte

Lokaler Teststand für Railroader 0.6.2; bestehende Spielstände weiterverwenden. Host und Clients benötigen dieselbe Version. Noch keine Spiel- oder Mehrspielerabnahme.

- Neue übergeordnete Ansichten anlegen, öffnen, umbenennen und entfernen. Verknüpfte Ortsstellwerke besitzen je ESTW eigene Modulpositionen; die gemeinsame Anlage wird nicht dupliziert.
- Netz-Erkennung auf Anforderung, getrennte Vorschau und ausdrückliche Übernahme. Eine neue Zeichnung archiviert die bisherige Tafel samt Modulen; unvollständige Vorschläge werden nicht übernommen.
- Einstellungen nach **Company → Optionen → Mods → Signaltechnik** verschoben. Alte Spieloptionen verweisen dorthin; vorhandene Werte bleiben erhalten.
- Elektrische Weichen gegen Umstellen durch native Wegpunkt-KI geschützt, unabhängig von Blockabstimmung und zusätzlicher Schutzoption. TF, Stellwerk und native CTC behalten ihre normalen Prüfungen.
- Kontakt anklicken öffnet die richtige Automatikseite. Optionales anklickbares Achstest-Display; keine verlässliche Gleisfreimeldung. Erstes Hauptsignal angeben, anschließend dessen Zielautomatik einrichten. Zwischenhauptsignale werden nicht übersprungen; der aktuelle Wegpunkt begrenzt die Folge.
- Neue und geänderte Bedientexte auf Englisch und Deutsch. Ausführliche Grenzen und die erforderlichen Spieltests: [Deutsch](Guides/TEST-0.7.24.de.md), [English](Guides/TEST-0.7.24.en.md).

# 0.7.23-P0 – Fahrwege auf Anforderung und getrennte Bearbeitungsfenster

Lokaler Teststand für Railroader 0.6.2. Die veröffentlichte 0.7.22-P0 bleibt unverändert. Host und Clients benötigen dieselbe neue Version. Noch keine Spiel- oder Mehrspielerabnahme.

## Fahrwegermittlung

- Neuer Standard: Start und Ziel im Fahrdienstleiter wählen. Der Weg wird aus dem aktuellen Gleis-/Signalbestand berechnet und über die bestehende Engine angefordert. Kontakt-Automatik und Streckensuche verwenden ebenfalls die frische Berechnung.
- Belegung, Endlagen, Verschlüsse, Flankenschutz, Nachbarannahme und Block/GWB bleiben normale Voraussetzungen. Ein neuer Weg ist keine unmittelbare Fahrtfreigabe.
- Berechnete Wege werden nicht mehr als zusätzliche Einträge im dauerhaften Fahrstraßenkatalog gespeichert. Aktive Fahrten behalten vollständige eigene Schnappschüsse. Clients erhalten die benötigten Wegdaten als begrenzte Liveanzeige; diese ist keine wiederherstellbare Fahrtfreigabe.
- Material-/Anschlusskosten für bisher nicht angeschlossene Weichen bleiben erhalten. Das Weglassen des Katalogeintrags umgeht diese Kosten nicht.
- Vorhandene gespeicherte Definitionen werden weder gelöscht noch als Quelle im neuen Modus verwendet. Rückkehr unter **Spieloptionen → Signaltechnik → Automatisierung und KI → Fahrwegermittlung**: „Fahrwege bei Anforderung berechnen“ ausschalten und „Fahrwegmodus übernehmen“. Auch im Editor unter **Menü → Stellwerksfunktionen → Fahrwegmodus einstellen** erreichbar.
- Umschalten erst ohne aktive Fahrstraßen, Richtungsbindungen und wartende/laufende Kontaktanforderungen. Im bisherigen Modus bleiben die alten Planungsfunktionen erreichbar. Keine automatische Rückkehr dorthin bei einer betrieblichen Ablehnung.
- Mehrdeutige Wege bleiben auswählbar; ein eindeutig zur vorhandenen Weichenlage passender Weg wird bevorzugt. Keine allgemeine neue Weicheneinzelsperre implementiert.

## Editor und Zuordnung

- Rechtsklick markiert zuerst das tatsächlich angeklickte Feld, auch wenn vorher Zeichnen oder ein anderes Werkzeug aktiv war. Objektaktionen beziehen sich auf dieses Feld.
- Feldmenü und allgemeiner Kopfknopf sind getrennt. Das Objektmenü enthält passende Eigenschaften/GFM-/Nummernaktionen; Zeichnen ist auf freien Zellen und im allgemeinen Menü verfügbar.
- Die rechte Eigenschaftenfläche wird nicht mehr erzeugt. Die bisherigen Bearbeitungsfunktionen sind in einem eigenen verschiebbaren und skalierbaren Fenster erreichbar. Bestehende Bereichs-/Kopier-/Tafelfunktionen bleiben vorerst gemeinsam über die Auswahlwerkzeuge zugänglich; ihre vollständige weitere Aufteilung bleibt ein nächster Schritt.
- Weltweiche vormerken, Tafel wählen und ein ungebundenes Weichenfeld anklicken: Erstzuordnung erfolgt direkt. Bereits anderweitig gebundene Felder werden nicht durch diesen Linksklick überschrieben.
- Umgekehrt: Weichenfeld rechtsklicken → **In der Welt zuordnen …**. Reale Weiche rechtsklicken → **Weiche zuordnen**. Danach Rückkehr zur Tafel; Erstzuordnung direkt, Ersetzung bestehender Bindung über deren Bearbeitung. Escape bricht die Weltwahl ab.
- Eine unvollständige Startauswahl im Fdl läuft nach fünf Sekunden ab. Vollständige Anforderungen, Fahrstraßen und wartende Automatik werden davon nicht aufgelöst.

## Fahrzeugfenster und Kamera

- Keine eingebetteten Signaltechnik-Formulare mehr im normalen Wagenbereich. Ein eigener nativer Reiter **Signaltechnik** enthält den Einstieg **Signaltechnik-Fahrinformationen …**.
- Dieser öffnet ein separates, skalierbares Fenster für Betriebsart und Verbandsfestlegung. Die Anzeige ist an Fahrzeug/Welt gebunden; Änderungen des Verbands werden vor Übernahme geprüft. Veraltete Aktionen einer anderen Fensterauswahl werden abgewiesen.
- In der Objektplatzierung beendet die rechte Maustaste nicht mehr den Baumodus. Kamera mit rechts bedienen; **Escape** beendet die Platzierung.

## ESTW

- Freie Strecke/GWB steht nicht mehr als erster Knopf in der Stellwerksauswahl. Zugang im allgemeinen Editor-Menü und im ESTW-Modulfenster bleibt erhalten.
- Die Verbindungsliste des ESTW verwendet eingerichtete Streckendefinitionen, keine gespeicherten Zugfahrstraßen. Dadurch bleibt sie auch ohne manuellen Fahrstraßenkatalog vorhanden.
- Modulanschlüsse direkt im Diagramm, zusätzliche unabhängige ESTW, vollständige A/B/C-Migration und Neuaufbau des gesamten Gleisbilds sind noch nicht in diesem Paket enthalten. Die alte automatische Zeichnungsersetzung bleibt deaktiviert.

## Erste Spielprüfung

1. Paket auf einer Kopie der Anlage installieren. Im Fahrzeugfenster Reiter Signaltechnik öffnen, Fahrinformationen öffnen, Betriebsart ändern. Andere Fahrzeugfelder dürfen nicht überlagert werden. Zweite Lok öffnen; alte Aktionen dürfen nicht versehentlich deren Betriebsart ändern.
2. Rechtsklick direkt auf eine andere Weiche: Markierung und Aktionen müssen zum neuen Feld gehören. Eigene Bearbeitung öffnet separat; das Gleisbild behält die volle Breite.
3. Beide beschriebenen Zuordnungsrichtungen mit ungebundenem Weichensymbol testen; anschließend Abbruch und absichtliche Neuzuordnung prüfen.
4. Im Fdl Start/Ziel für einen freien Weg wählen, ohne Definition anzulegen. Danach besetzten Weg und verschlossene Weiche testen. Für Blockprüfungen vollständige GFM einrichten.
5. Neues korrekt eingerichtetes Zielgleis ergänzen und ohne neuen Editor-Fahrplan anfordern. Kontakt-Erst-/Ausweichziel und Gegenrichtung im Block testen.
6. Host/Client: aktive Fahrt, Verschlüsse und Zugnummern kontrollieren. Anschließend Fahrt sicher auflösen, neu laden und überprüfen, dass keine alte Freigabe übernommen wird.
7. Start wählen und länger als fünf Sekunden warten. Nur die Auswahl verfällt. In der Bauplatzierung Kamera rechts drehen; Vorschau muss bestehen bleiben.

Code und Offline-Prüfungen ersetzen diese Spieltests nicht. Die numerische Geschwindigkeitslogik, Mehrfach-Vorsignalbindung und übrigen Konzepte werden hier nicht als fertig behauptet.

## Offline-Prüfung dieses Teststands

938 Prüfungen bestanden, keine fehlgeschlagenen Prüfungen. Der vollständige Testlauf inklusive Sprachprüfung liegt unter `build/checks/0.7.23-tests.log`. Spieloberfläche, Gelände, KI-Fahrbetrieb und Mehrspieler müssen zusätzlich praktisch abgenommen werden.


---

# 0.7.22-P0 – Rechtsklick-Kategorien und Weichenbedienung

Teststand für Railroader 0.6.2. Bestehende Pakete bis 0.7.21 bleiben unverändert. Host und Clients benötigen dieselbe Version.

- Der Editor startet ohne linke Werkzeugleiste und ohne offene Eigenschaften. Ein Linksklick wählt ein Feld aus.
- Rechtsklick öffnet Kategorien: ausgewähltes Feld, Zeichnen und Platzieren, Bereiche sowie Stellwerksfunktionen. Nur passende Feldkategorien werden angeboten, etwa Gleisfreimeldung oder Zugnummern.
- Der Menüknopf oben bietet denselben Zugang. Fahrstraßen, Zuordnungen, Automatik, Freie Strecke / GWB, ESTW-Module und Handbuch bleiben erreichbar.
- Eigenschaften öffnen nach Auswahl der Kategorie rechts. Mit × schließen; die Gleisbildfläche wird wieder breiter. Vorgemerkte Signalzuordnungen und Löschbestätigungen öffnen ihre benötigten Eigenschaften weiterhin direkt.
- Im Fahrdienstleiter schaltet Linksklick auf ein zugeordnetes Weichenfeld zwischen Grundstellung und Abzweig um. Rechtsklick bietet die beiden Stellungen einzeln an.
- Weichenschaltknöpfe sind an die angezeigte Weiche und das Stellwerk gebunden. Ein alter Knopf kann nach einem Stellwerkswechsel keine andere Weiche umstellen. Veraltete Feldmenüs werden ebenfalls verworfen.
- Bereits erreichte Weichenstellungen werden ohne erneuten Schaltbefehl bestätigt. Fahrstraßen- und Flankenschutzverschlüsse bleiben wirksam. Native Belegungs- und CTC-Prüfungen werden weiterhin vom Spiel durchgeführt; eine Ablehnung wird mit ihrem Grund angezeigt. Native CTC-Weichen benötigen weiterhin die Freigabe des Spiels.

## Prüfung im Spiel

1. Editor öffnen und Stellwerk wählen: volle Gleisbildbreite, zunächst keine seitlichen Eigenschaften. Feld linksklicken, anschließend rechtsklicken und eine passende Kategorie öffnen. × schließt die Eigenschaften.
2. Über Rechtsklick → Zeichnen und platzieren ein Gleis zeichnen oder ein Symbol setzen. Über Auswählen / Zeichnen beenden zum Auswahlmodus zurückkehren.
3. Über Rechtsklick → Stellwerksfunktionen die Fahrstraßen, Freie Strecke / GWB und ESTW-Module öffnen. Auch in der Kopfzeile ist das Menü erreichbar.
4. Im Fahrdienstleiter eine freie, zugeordnete, nicht verschlossene Weiche linksklicken. Spielmodell und Symbol müssen ihre Stellung ändern. Rechtsklick → + bzw. − gezielt testen.
5. Bei einer durch Fahrstraße oder Flankenschutz verschlossenen Weiche muss ein abweichender Schaltwunsch abgelehnt werden. Dasselbe gilt für eine vom Spiel verweigerte Bewegung bei Belegung oder nativer CTC-Sperre.

Kompilierung und Offlineprüfungen ersetzen keinen Test der Unity-Oberfläche, der Spielbelegung oder des Mehrspielers. Die bekannten Wiederanlaufgrenzen der freien Strecke aus 0.7.21 gelten weiterhin: keine automatische Richtungsübernahme einer beim Laden bereits belegten Strecke ohne bekannte Bindung.

[Freie Strecke / GWB](Guides/FREE-LINE-GWB.de.md) · [English setup](Guides/FREE-LINE-GWB.en.md)
