# GFM-Abschnitte in der Spielwelt — Testversion 0.7.72

## Ausprobieren

1. Spielstand und Anlagenkonfiguration sichern und möglichst eine Kopie verwenden. Railroader 0.6.3; Host und Mitspieler verwenden dieselbe Hauptmodversion 0.7.72.
2. Als Host den Baumodus öffnen. Oben **GFM-Abschnitte und Grenzen anzeigen** wählen.
3. Gespeicherte Abschnitte erscheinen als farbige Linien über den Schienen. Nach Möglichkeit erhalten angrenzende Abschnitte verschiedene Farben. Die automatisch gewählten Farben dürfen sich wiederholen und sind keine Belegungs- oder Erlaubnisfarben. Die Auswahl wird breiter dargestellt.
4. Eine farbige Linie oder einen Endpfosten anklicken. Alternativ den gespeicherten Abschnitt über die Suche auswählen. Das Baumenü zeigt Name, Länge, bei zweiseitigen GFM-Abschnitten die Grenzbezeichnungen und verknüpfte Stellwerke.
5. **Prüfen / Gleisbild-Zuordnung bearbeiten** öffnet das gewählte Stellwerk in der GFM-Prüfung und hebt zugeordnete Felder hervor. Dort stehen die vorhandenen Zuordnungs- und Grenzprüfwerkzeuge bereit. Fehlt eine Feldzuordnung, muss sie mit der bestehenden GFM-/Gleisbild-Einrichtung angelegt werden; diese Ansicht erfindet keine Zuordnung.
6. Umgekehrt in der GFM-Prüfung **Gespeicherten Abschnitt in der Welt anzeigen (Baumodus)** wählen. Keine automatische Kamerafahrt: gegebenenfalls selbst zum betreffenden Gleis bewegen.
7. **GFM-Abschnitte ausblenden** oder Baumodus beenden entfernt die lokalen Linien und Auswahlflächen. Weltwechsel, Berechtigungsverlust und Entladen räumen ebenfalls auf.

## Grenzen dieser ersten Testfassung

- Angezeigt werden gespeicherte Meldewege einschließlich gespeicherter Weichen-/Schutzabschnitte, keine automatisch als gültig bestätigten neuen Vorschläge. Endpfosten stehen an Anfang/Ende des gespeicherten Hauptpfads; sie zeigen nicht die Wirkrichtung einer C-Grenze. Gleisstücke ohne gültigen gespeicherten Verlauf erhalten keine erfundene Darstellung.
- Unmarkiertes Gleis kann unüberwacht, außerhalb der Reichweite, ungültig oder wegen der Darstellungsgrenze ausgelassen sein. Es gilt dadurch nicht als frei. Die Anzeige läuft im Umkreis von 400 m zur Kamera, mit maximal 3.000 gezeichneten Teilstücken; Annäherung/Bewegung aktualisiert die lokale Ansicht. Nicht verfügbare oder begrenzte Teilstücke werden gezählt. Überlagerte gespeicherte Abschnitte können sich optisch überdecken; dafür die Abschnittssuche verwenden.
- Die Farben sind ausschließlich Abschnittskennzeichnungen. Diese Anzeige prüft weder vollständige GFM-Abdeckung noch momentane Belegung oder Fahrberechtigung. Für Gültigkeit und Fehlerursachen die GFM-Prüfung verwenden.
- Bearbeitung nutzt den vorhandenen Editor und dessen Prüfungen. Freies Zeichnen neuer Von-bis-Grenzen per Weltklick, automatische Vereinigung/Teilung, automatisch mit jedem Signal gebaute GFM und C-GFM-Flankenschutz-Ausnahmen sind nicht enthalten. Gespeicherte IDs und Abschnittswege werden durch das Anzeigen nicht verändert.
- Bauhilfe nur lokal beim Host, keine geteilte Mehrspieler-Auswahl. Weichen, Signale, Fahrstraßen, Verschlüsse, Automatik und Freimeldung werden nicht aus Darstellungsfarben gesteuert. Auswahlflächen sind Trigger und keine physischen Hindernisse.

Vor Regelbetrieb prüfen: Farben benachbarter Abschnitte, gebogene Gleise und Weichen, tatsächliche Anfangs-/Endpositionen, Auswahl nahe Signalen, Linienmaterial tags/nachts, Wechsel GFM-Prüfung/Baumodus, ungültige/versetzte Abschnitte, Zoom/Entfernung, Ursprungverschiebung, Ausblenden/Neuladen und Host-/Clientansicht. Diese Abläufe sind noch nicht im Spiel abgenommen. Frühere Testpakete bleiben unverändert. Runenwacht 0.2.2 bleibt optional und unverändert.
