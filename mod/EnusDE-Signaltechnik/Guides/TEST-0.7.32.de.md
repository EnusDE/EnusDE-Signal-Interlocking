# 0.7.32-P0 – Einrichtung und Spieltests

Teststand für Railroader 0.6.2. Host und Clients benötigen dieselbe Modversion. Bestehende Testwelt verwenden; kein neuer Spielstand erforderlich. Noch keine Veröffentlichung auf Nexus und keine Installation durch dieses Update.

## Blockverbindung Whittier–Ela

1. Im Editor Whittier öffnen. Über Zeichnen und Platzieren einen **Strecken-/Erlaubnismelder** in ein freies Rasterfeld setzen. Die Einrichtung öffnet sich direkt. Vorhandene Melder lassen sich per Rechtsklick einrichten. Alternativ Stellwerksverwaltung → **Strecken / Block** oder Menü → **Block-/Streckenverbindungen**.
2. Nachbar A Whittier, Nachbar B Ela wählen. Für beide Richtungen Ausfahrt und gegenüberliegende Einfahrt zuordnen. Es sind vier logische Hauptsignalrichtungen nötig; diese können auf gemeinsamen Masten liegen. Die Signalfelder müssen bereits dem jeweiligen Ortsstellwerk zugeordnet sein.
3. **Gleisweg erkennen** und Vorschläge kontrollieren. Der Fahrweg wird zunächst nur im Entwurf gehalten. Bei mehreren Möglichkeiten eine zusammenpassende Hin-/Rückrichtung wählen.
4. **GFM-Abschnitte für diese Strecke vorschlagen**, prüfen und ausdrücklich **übernehmen**. Verwendet werden vorhandene GFM-Grenzen und bereits aktivierte Weichen-GFM innerhalb des Streckenwegs. Die Zeichnung bleibt erhalten. Fehlende Kästen oder Weichen-GFM werden dadurch nicht automatisch gebaut.
5. **Streckenkonfiguration übernehmen** speichert Verbindung und Melderzuordnung zusammen. Ein unvollständiger Entwurf kann deaktiviert gespeichert werden. Aktivierung erfordert gültige Endsignale, passenden Gleisweg und vollständige Freimeldeabdeckung.
6. In Ela einen Melder setzen und dieselbe bestehende Verbindung wählen. Ein Melder im ESTW kann ebenfalls dieselbe Verbindung anzeigen. Die beiden Ortsstellwerke in der ESTW-Modultafel einbinden; es entsteht eine gemeinsame Erlaubnis, keine unabhängige Kopie je Tafel.

Prüfen: Änderungen sind auf beiden Seiten sichtbar, Gegenfahrten schließen sich aus, belegte oder unbekannte Strecken geben keine Erlaubnis, Befahren und Zugschluss lösen die Richtung erst nach bestätigter Freiprüfung. Streckenerlaubnis allein stellt kein Signal auf Fahrt; Fahrstraße oder separat eingerichtete Zielautomatik bleiben nötig. Bei Änderungen des Netzes/GFM vor Übernahme muss ein veralteter Vorschlag abgewiesen werden.

## Hl-Testoption

**Company → Optionen → Mods → Signaltechnik → Signalbetrieb und Darstellung → Hl-Signalsystem aktivieren (Test)** einschalten und Einstellungen übernehmen. Standardmäßig aus. Neue Hl-Bauten und Umrüstung sind erst dann verfügbar. Ausschalten entfernt keine bestehenden Hl-Signale und verändert ihre Betriebsanzeige nicht. Rückrüstung auf H/V bleibt möglich. Native KI-Geschwindigkeitsbehandlung unverändert.

## GFM-Bauvorgabe und vorhandene Gleiszeichnung

Unter **Company → Optionen → Mods → Signaltechnik → Signalbetrieb und Darstellung** gibt es **Neue Signale mit GFM-Kasten bauen**. Standardmäßig aktiv wie bisher. Ausschalten gilt für neue Signale; vorhandene Kästen werden weder entfernt noch nachgerüstet. Einzelne Signale lassen sich weiterhin separat ändern.

Im vorhandenen örtlichen Gleisbild oben **Menü → GFM in diesem Gleisbild erkennen …** wählen, dann **GFM-Abschnitte im vorhandenen Gleisbild suchen**. Die Vorschau zeigt vollständige Abschnitte und die Anzahl ungebundener Signale bzw. fehlender Signal-/Weichen-GFM. **Erkannte GFM ohne Neuzeichnen übernehmen** ordnet eindeutige durchgehende Gleisbildabschnitte samt innerer Signalseiten zu. Die Zeichnung bleibt erhalten. Unbekannte Symbole werden nicht nach ihrer Rasterposition geraten; dafür die Weltzuordnung verwenden. Fehlende Kästen und deaktivierte Weichen-GFM werden nicht stillschweigend eingeschaltet.

Prüfen: teilfertige Tafel mit bereits zugeordneten Signalen, unterbrochenes Gleisbild, fehlende Weichen-GFM, veraltete Vorschläge sowie neue Signale mit ein-/ausgeschalteter Bauvorgabe. Die Blockprüfung benennt weiterhin nicht abgedeckte physische Gleissegmente und Meterbereiche.

## Elektrische Weichen und Weltzuordnung

Nur eindeutig in einer örtlichen Tafel zugeordnete Weichen erhalten den ausgewählten elektrischen Antrieb. Eine Kennung oder Eintragung in einer übergeordneten Netzerkennungsansicht genügt nicht. Der Sammelbefehl wirkt auf die tatsächlich verknüpften Weichen dieses Ortsstellwerks. Native CTC-Weichen behalten die Spielregeln.

Im Editor eine Weiche rechtsklicken → **In der Welt zuordnen …**. Im Welt-Zuordnungsmodus enthält das Gleismenü nur **Weiche zuordnen**; Auswahl kehrt zur ursprünglichen Tafel zurück und übernimmt die Weiche in das gewählte Feld. Escape bricht ab. Signalfelder bieten entsprechend **Signal in der Welt zuordnen …**; am echten Signal die gewünschte Seite anklicken und **Signal zuordnen** wählen. Geänderte/gelöschte Zielfelder dürfen nicht überschrieben werden.

## Halt und Auflösen

- Normale sichere Auflösung: freie und prüfbare Anlage löst sofort auf. Bleibt sie blockiert, erscheint der Grund mit dem Hinweis **kein Zeitablauf**. Ursache beseitigen und erneut anfordern. Dadurch wird keine zusätzliche automatische Hilfsauflösung ausgelöst.
- Geprüfte Hilfsauflösung: verbleibende Schutzwartezeit in Sekunden im Auflösungsstatus und unten im Gleisbild. Nach Zeitablauf bleiben notwendige Prüfungen wirksam; eine bewegte Annäherung oder unbekannte Belegung darf nicht allein durch Zeitablauf freigegeben werden.
- Die ausdrücklich Fdl-bestätigte Hilfsauflösung behält ihre bisherige Bedeutung und Mindestwartezeit. Die neue Anzeige ersetzt keine örtliche Prüfung.
- Prüfen: Countdown bei Host und Client, danach konkreter Blockierungsgrund; sofortige normale Auflösung einer vollständig freien Anlage; keine alte Warteanzeige nach abgeschlossener Auflösung.

## Außer Betrieb und Sicherung

Temporäre Sicherungsdateien erhalten kurze eindeutige Namen im bisherigen Sicherungsordner. Der bisherige angehängte Name erreichte unter dem gemeldeten Windows-LocalLow-Pfad 260 Zeichen und verhinderte den Sicherungspunkt vor der Abschaltung. Endgültige Dateien, Historie und Wiederherstellung bleiben kompatibel. Sicherungsfehler werden weiterhin gemeldet und nicht übergangen.

Prüfen: freie Anlage ohne betroffene Fahrstraßen-/Richtungsverschlüsse abschalten. Kennlicht, KI-Umgehung und ausschließlich zu diesem abgeschalteten Bereich gehörende Weichen prüfen; danach wieder einschalten. Bei belegtem oder verschlossenem Bereich muss der vorhandene Schutz weiterhin greifen. Im Sicherungsmenü muss der neue Sicherungspunkt lesbar sein.

## Automatik-Kontakte in der Spielwelt

Gleis rechtsklicken → **Automatik-Kontakt platzieren** öffnet zuerst ein Einstellmenü, ohne einen Kontakt zu bauen. Die Blickrichtung gibt die anfängliche Fahrtrichtung vor. Das erste Hauptsignal wird gesucht; nur ein eindeutiges Ergebnis wird vorgeschlagen. Mehrere mögliche erste Hauptsignale erfordern eine bewusste Auswahl. Richtung, Startsignal, Zähler, Display und Aktivierung prüfen und **Eingerichteten Kontakt bauen** wählen. Abbrechen baut nichts.

Start- und Zielsignale lassen sich über **Signal in der Welt zuordnen …** bzw. **Vorrangiges Ziel/Ausweichziel in der Welt zuordnen …** auswählen. Es muss keine Kennung abgetippt werden. Die Signaloptionen zeigen **Automatische Ansteuerung erkannt** mit den verknüpften Kontakten; Zielautomatik bleibt ausdrücklich am Signal einzurichten. Die Erkennung allein aktiviert weder die globale Kontakt-Automatik noch eine Fahrtfreigabe.

**Experimentellen Achstest zählen** und **Achstest-Display am Kontakt anzeigen** sind getrennte Optionen. Änderungen an bestehenden Kontakten mit **Kontakt speichern** übernehmen. Das Display besitzt jetzt lesbare Vorder- und Rückseiten; zuvor war der einzige Text in die Platte gedreht. Bei Überfahrt aus beiden Richtungen Lesbarkeit und Zähländerung prüfen. Fehlende aktuelle Daten werden mit einem Strich statt einer zugesicherten Zählung dargestellt. Der Achstest ist kein Freimeldenachweis und legt keine Gleisabschnitte an. Eine eigene Kontaktanzeige im Stellwerks-Gleisbild ist nicht hinzugefügt.

## Fahrwegsuche

Die Suche grenzt Sackgassen anhand der gerichteten Erreichbarkeit des gewählten Ziels aus. Weichenübergänge, Zwischenhauptsignale, Flankenschutz und Suchgrenzen bleiben wirksam. Bei einem größeren Gleisnetz die zuvor mit „Fahrwegsuche begrenzt“ gescheiterte Start-/Zielwahl erneut testen, auch in Gegenrichtung und über Zwischenhauptsignale.

Automatische Prüfungen ersetzen nicht die Abnahme mit der tatsächlichen Testwelt, nativer KI und Mehrspielerbetrieb. Bei Fehlern Mod-/Spielversion, Aktion, Signal-/Weichenkennungen und Player.log angeben.
