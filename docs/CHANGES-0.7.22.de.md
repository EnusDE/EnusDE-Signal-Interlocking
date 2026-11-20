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

[Freie Strecke / GWB](FREE-LINE-GWB.de.md) · [English setup](FREE-LINE-GWB.en.md)
