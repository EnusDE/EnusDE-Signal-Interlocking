# 0.7.48-P0 – Editor mit wachsender Arbeitsfläche

Testfassung für Railroader 0.6.2. Host und Clients benötigen 0.7.48.
Enthält die kompakte Zugnummernübersicht aus 0.7.47. Ältere ZIPs bleiben unverändert.

## Bedienung im Editor

Oben stehen Spaltenköpfe **A, B, … Z, AA …**, links Zeilennummern **1, 2, 3 …**.
Die Köpfe bleiben beim Scrollen sichtbar. Bei sehr kleinem Zoom werden einige
Beschriftungen ausgelassen, damit sie lesbar bleiben; jede einzelne Zeile/Spalte
bleibt trotzdem anklickbar.

    Alle | A | B | C | D | …
    -----+---+---+---+---+
       1 |   |   |   |   |
       2 |   |───────|   |
       3 |   |   |   |   |

- **Klick auf einen Kopf:** komplette Zeile beziehungsweise Spalte markieren.
- **Umschalt-Klick:** mehrere zusammenhängende Zeilen/Spalten markieren, ausgehend
  vom ersten Kopf. Ein Wechsel zwischen Zeilen und Spalten setzt den Anfang neu.
- **Rechtsklick auf einen Kopf:** davor/dahinter einfügen. Bei mehreren markierten
  Zeilen/Spalten wird dieselbe Anzahl neu eingefügt.
- **Alle:** die ganze Tafel auswählen. Bestehende Auswahlwerkzeuge bleiben verfügbar.
- **Menü → Arbeitsfläche erweitern:** gezielt oben, unten, links oder rechts ergänzen.
- **Plus-Köpfe am äußeren Rand:** dort eine Zeile/Spalte ergänzen.

Im Fahrdienstleiter-Modus erscheinen diese Köpfe und Bearbeitungsfunktionen nicht.

## Automatische Erweiterung

Außerhalb der gespeicherten Tafel liegt im Editor ein schmaler virtueller Rand.
Beim Zeichnen, Setzen eines Feldsymbols, Einfügen eines kopierten Bereichs und
Verschieben an diesen Rand wird die benötigte Fläche beim Speichern ergänzt.
Wenn möglich bleiben zwei freie Zellen Abstand. Ein normaler Auswahlklick vergrößert
die gespeicherte Tafel nicht.

Oben/links verschieben sich die bisherigen Felder und Bereichsmarkierungen gemeinsam.
Signal-, Weichen-, GFM-, Hilfsziel- und stabile Zugnummern-Quellkennungen bleiben dabei
erhalten. Reine Nummernkopien bleiben mit ihren Quellen verbunden. Die Ansicht wird
um dieselbe Verschiebung ausgeglichen, statt das Gleisbild neu zu zentrieren.

Beim Ziehen einer ganzen Zeile/Spalte werden berührte erzeugte Block- oder
Nummerngruppen vollständig mitgenommen. Die Auswahl kann dafür größer werden;
andere bereits ausgewählte Felder bleiben enthalten. Rot markierte Ziele werden
nicht übernommen. Loslassen außerhalb der Zeichenfläche verwirft das Verschieben.

Zusätzliche Arbeitsfläche und die dazugehörige Änderung werden gemeinsam gespeichert.
Bei einem abgewiesenen Ziel bleibt der bisherige Stand erhalten. **Rückgängig** und
**Wiederherstellen** erfassen auch die Erweiterung. Beim Erweitern eines ESTW oben/links
folgen dessen eingebundene Modulpositionen; beim Rückgängigmachen folgen sie zurück.

Es gelten weiterhin maximal **2048 Spalten × 512 Zeilen**. An der Grenze lässt sich
vorhandener Platz bearbeiten; echte Überschreitungen werden abgewiesen. Wenn eine
vergrößerte örtliche Tafel mit einem anderen ESTW-Modul kollidieren würde, muss zuerst
im ESTW-Layout Platz geschaffen werden. Es werden keine anderen Module überschrieben.

Manuelles Einfügen **innerhalb** eines gezeichneten Gleisbilds erzeugt freie Zellen.
Vorhandene Bindungen bleiben erhalten; solche inneren Zeichenlücken können mit
**Gleis zeichnen** verbunden werden. Automatisches Ergänzen am Außenrand erzeugt
keine Lücke innerhalb des vorhandenen Gleisbilds.

## Zoomen

Das Mausrad im Haupteditor zoomt zum Mauszeiger: Der dort liegende Gleisbildpunkt
bleibt an derselben Bildschirmposition. Die Zoomknöpfe verwenden die Mitte der
Zeichenfläche. **Tafel einpassen** richtet die Ansicht ausdrücklich am gezeichneten
Inhalt aus. Die Fahrdienstleiter-Ansicht behält ihr bisheriges Zoomverhalten.

## Prüfstand und Spieltest

Automatisch geprüft werden Wachstum in vier Richtungen, Größenlimits, Auswahlanker,
Beschriftung, Verschieben ganzer Gruppen, Kollisionen, Erhalt der Zuordnungen,
Speichern/Laden sowie Rücknahme der ESTW-Modulverschiebung. Der Mod wird gegen die
vorhandenen Spielbibliotheken gebaut. Eine visuelle Abnahme der neuen Unity-Oberfläche
und ein Mehrspielertest stehen noch aus.

Später im Spiel prüfen:

1. Kleine Tafel öffnen; an allen vier Rändern zeichnen oder ein Symbol setzen.
2. Nummernkopie und Quelle vor/nach einer Erweiterung oben/links vergleichen.
3. Zeilenkopf anklicken, mit Umschalt erweitern, rechtsklicken und davor/dahinter
   einfügen. Ganze Auswahl verschieben und Rückgängig/Wiederherstellen prüfen.
4. Einen markanten Punkt unter den Mauszeiger nehmen und mehrfach hinein-/herauszoomen.
5. Speichern/laden; im Fahrdienstleiter-Modus prüfen, dass keine Kopfzeilen erscheinen.
