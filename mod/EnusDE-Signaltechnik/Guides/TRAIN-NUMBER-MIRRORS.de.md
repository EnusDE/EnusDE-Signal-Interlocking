# Zugnummern aus dem ESTW örtlich anzeigen

Die Anzeigekopien sind im neuen **Testpaket 0.7.19-P0** enthalten. Das ältere 0.7.18-ZIP bleibt unverändert und enthält diese spätere Ergänzung nicht.

Ein Zugnummernfeld der übergeordneten Tafel kann auf einer örtlichen Tafel zusätzlich angezeigt werden. Die zusätzliche Anzeige verweist auf das ursprüngliche Feld und liest dessen aktuellen Wert. Es entsteht weder eine zweite Nummernzuordnung noch ein weiteres Weitergabeziel. Die vorhandene Zugnummernfortschaltung arbeitet weiter mit den ursprünglichen Quellenfeldern.

## Einrichten

1. Im ESTW ein ursprüngliches Zugnummernfeld für den gewünschten freien Streckenabschnitt anlegen und speichern. Für eine fahrzeugbezogene Anzeige den tatsächlich überwachten Abschnitt binden und dem Zug eine Nummer zuweisen. Für die bestehende signalbezogene Nummernfortschaltung die Hauptsignalzuordnungen des Quellenfelds verwenden.
2. Das örtliche Stellwerk im Editor öffnen und ein Zugnummernfeld als Anzeige platzieren beziehungsweise auswählen.
3. Unter **Zugnummernfeld aus anderer Tafel anzeigen** das **Quellenfeld – ESTW zuerst** auswählen. Angeboten werden gespeicherte ursprüngliche Zugnummernfelder, geordnet nach Stellwerk und Feldposition. Auch andere örtliche Tafeln können Quelle sein.
4. **Anzeigekopie hier anlegen** wählen. Eine feste Quellenkennung wird bei Bedarf zusammen mit der Kopie in derselben Änderung gespeichert. Eine Beschriftung wie „aus Ela“ hilft, mehrere Voranzeigen zu unterscheiden.

Die Kopie ist ein eigenes verschiebbares Anzeigefeld. Sie hat keine Gleisanschlüsse, keine eigene Abschnittsbindung und keine Fahrstraßenfunktion. Unter der Nummer steht die gewählte Beschriftung beziehungsweise „Anzeige“. Die Herkunft und der aktuelle Wert sind im Inspektor lesbar; dort gibt es keine Eingabe zum Überschreiben der Nummer.

## Laufendes Verhalten

- Die Nummer erscheint auf jeder Kopie, auch wenn die Quelltafel geschlossen oder das Quellenfeld außerhalb des sichtbaren Bereichs liegt. Die erforderliche Abschnittslesung wird durch die sichtbare Kopie angefordert und bei mehreren Kopien derselben Quelle nur einmal durchgeführt.
- Eine Änderung der Nummer oder der Signal-/Abschnittsbindung am Original wird in den Kopien gelesen. Das Verschieben des Quellenfelds oder das Einfügen von Zeilen löst die Verknüpfung nicht.
- Wandert die Nummer aus dem Originalfeld weiter, folgt die Kopie dem neuen Zustand dieses Felds. Sie verfolgt nicht eigenständig den Zug in beliebige Folgeabschnitte.
- Fehlt die Quelle nach einer Löschung, erscheint **?** und der Inspektor meldet das fehlende Quellenfeld. Es wird kein anderes Feld anhand seiner alten Position geraten. Ohne aktuelle Host-Rückmeldung zeigen Clients ebenfalls **?**.
- **-----**, **mehrere** und **?** haben dieselbe Bedeutung wie an der Quelle. Ein ohne vergebene Zugnummer eingefahrener Zug erhält durch die Kopie keine erfundene Nummer. Seine Belegung kann bei korrekt eingerichteter GFM dennoch erkannt werden.
- Eine rote Feldumrandung bedeutet, dass der Quellenabschnitt als belegt gelesen wird. Eine unbekannte Belegung wird entsprechend gekennzeichnet. Diese Anzeige erteilt keine Fahrtfreigabe.

Im Editor lässt sich die Quelle einer Kopie ändern. **In normales Zugnummernfeld umwandeln** löst nur diese Anzeigeverknüpfung und erzeugt ein leeres gewöhnliches Feld. Das Original und seine Nummer bleiben bestehen. Kopien können nicht selbst als Quelle weiterer Kopien dienen. Ein Original, das bereits gespiegelt wird, lässt sich nicht unbemerkt in eine Kopie eines anderen Felds umwandeln.

Beim normalen Kopieren von Zeichenbereichen erhalten gewöhnliche Quellenfelder keine doppelte Quellenkennung. Bereits ausdrücklich eingerichtete Anzeigekopien behalten ihren Verweis auf das ursprüngliche Feld. Löschen und Rückgängig funktionieren über den vorhandenen Editorverlauf.

## Speicherung und Test

Die Quellenkennungen und Kopieverknüpfungen liegen in der gemeinsamen Stellwerkskonfiguration und sind in der externen Anlagensicherung enthalten. Aktuelle Zugnummernpositionen werden nach einer Wiederherstellung weiterhin neu festgelegt. Das Entfernen eines Quellenfelds schützt seinen vorherigen vollständigen Anlagenstand als Wiederherstellungspunkt.

Offline geprüft sind unter anderem mehrere Kopien derselben Quelle, unveränderte Weitergabe ohne Doppelnummern, Lesen bei geschlossener Quelltafel, Quellenbewegung und -löschung, JSON-/Backup-Rundläufe und ungültige beziehungsweise zyklische Verknüpfungen. Die DLL wird gegen Railroader 0.6.2 gebaut; die tatsächliche Anzeige im Spiel und die Host-/Client-Synchronisation benötigen noch einen Spieltest.

Für diesen Test einen freien Streckenabschnitt im ESTW binden, eine Kopie im Ortsstellwerk anlegen und die ESTW-Tafel schließen. Einen nummerierten Zug in den Abschnitt fahren lassen, danach die Nummer ändern beziehungsweise weiterwandern lassen. Original und Kopie müssen denselben Feldzustand anzeigen, ohne eine zweite Nummernzuordnung anzulegen.
