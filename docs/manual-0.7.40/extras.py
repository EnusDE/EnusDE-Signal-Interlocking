"""Supporting capture agendas and package notes for the 0.7.40 manual edition."""
from pathlib import Path
from reportlab.platypus import PageBreak, Spacer
from build import OUT, Doc, header, para

# The agenda requests actual public-release views; these are not placeholder screenshots.
SHOTS = [
('S01', 'Übersicht des Lernbahnhofs', 'Learning-station overview',
 'Im Fahrdienstleiter einen kleinen funktionierenden Bahnhof öffnen. Signalnamen, Weichen und Gleisbelegung lesbar zeigen; zusätzlich die Versionsanzeige 0.7.40 aufnehmen.',
 'Open a small working station in Dispatcher. Show readable signal names, points and occupancy; also capture the 0.7.40 version display.'),
('S02', 'Signal platzieren', 'Place a signal',
 'Bau → Hauptsignal: Vorschau am echten Gleis mit Fahrtrichtung, Seite und Abstand aufnehmen. Danach denselben Standort mit gesetztem Signal zeigen.',
 'Build → Main signal: capture the preview on real track with facing, side and offset. Then show the same location with the placed signal.'),
('S03', 'Gleisbild mit der Welt verbinden', 'Bind the diagram to the world',
 'Im Editor ein Signalfeld rechtsklicken und die Weltzuordnung zeigen. Eine zweite Aufnahme soll das gespeicherte Eigenschaftenfenster mit zugeordnetem Signal und Richtung zeigen.',
 'Right-click an editor signal field and show world assignment. A second capture should show saved properties with the bound signal and direction.'),
('S04', 'Weichenarme prüfen', 'Inspect point arms',
 'Physische Weichenarme in diesem Gleisbild öffnen. Zeichnung, ausgewählte Weiche sowie Stammgleis/gerader Ast müssen gleichzeitig erkennbar sein.',
 'Open Physical point arms in this diagram. Keep the drawing, selected point and common/normal arm settings visible together.'),
('S05', 'GFM-Abschnitt einrichten', 'Configure a GFM section',
 'Ein eindeutiges Gleisstück zwischen zwei aktivierten Grenzen auswählen. Grenzen und Gleisverlauf suchen sowie anschließend die gespeicherte vollständige Abschnittszuordnung aufnehmen.',
 'Select an unambiguous track between two enabled boundaries. Capture boundary selection and Find track path, followed by the saved complete section assignment.'),
('S06', 'GFM-Prüfung und Lücke', 'GFM inspection and a gap',
 'In der GFM-Prüfung einen gültigen Abschnitt hervorheben: beide Grenzen, verknüpfte Felder und Liste sichtbar. Optional denselben Ablauf an einer gezielt vorbereiteten Lücke zeigen; danach korrigieren.',
 'Highlight a valid section in GFM inspection with both boundaries, linked fields and list visible. Optionally repeat for a deliberately prepared gap, then correct it.'),
('S07', 'Fahrstraße stellen', 'Set a route',
 'Eine kurze Zugfahrt vom Start zum Ziel aufnehmen: richtige Signalhälfte, gegebenenfalls Wegauswahl, danach festgelegter Fahrweg und reales Fahrt zeigendes Signal. Zwei bis drei Bilder derselben Fahrt genügen.',
 'Capture a short train route: correct start half, route choice if applicable, then established track and the real proceed signal. Two or three images of the same movement are sufficient.'),
('S08', 'Hilfsziel richtig ausrichten', 'Orient an auxiliary target',
 'Eigenschaften eines HZ mit Gleis-ID, Meter, realer Anfahrtrichtung und zugelassenen Fahrarten zeigen. Die zugehörige Taste und das anfahrende Signal in der Zeichnung sichtbar lassen.',
 'Show HZ properties with track ID, metres, real approach direction and allowed movements. Keep its button and approaching signal visible on the diagram.'),
('S09', 'Zugnummer und Anzeigekopie', 'Train number and repeated display',
 'Am Original die Zuweisung zum ausgewählten Zug zeigen. Danach Original und Anzeigekopie auf zwei Tafeln mit derselben Nummer aufnehmen; kein zweites unabhängiges Original erzeugen.',
 'Show assignment to the selected train at the original field. Then capture source and repeated display on two panels showing the same number; do not create a second independent source.'),
('S10', 'GWB der öffentlichen Version', 'Public-release GWB',
 'Die eingerichtete Streckenverbindung mit A/B-Endpunktgruppen, Betriebsart und aktivierter Sicherung aufnehmen. Danach das separate Erlaubnisfeld und dessen direkte A→B/B→A-Bedienung in 0.7.40 zeigen.',
 'Capture line setup with A/B endpoint groups, mode and enabled protection. Then show the separate permission field and its direct A→B/B→A controls in 0.7.40.'),
('S11', 'ESTW und örtliche Tafeln', 'Master and local panels',
 'Zwei verknüpfte Ortsmodule im ESTW zeigen, eines ausgewählt. Die Modulgrenzen und der Unterschied zwischen örtlicher und übergeordneter Ansicht sollen erkennbar sein.',
 'Show two linked local modules in the master with one selected. Make module boundaries and the distinction between local and master views visible.'),
('S12', 'Vorhandene Kontakt-Automatik', 'Existing contact automation',
 'Einen konfigurierten Kontaktentwurf mit Richtung/erstem Hauptsignal aufnehmen. Zusätzlich Zielautomatik mit Erst-/Ausweichziel und eine lesbare Anforderung samt Status zeigen.',
 'Capture a configured contact draft with direction/first main signal. Also show target automation with primary/alternate destination and a readable request/status.'),
('S13', 'Material und MOW', 'Supplies and MOW',
 'Optional: Materialladung oder Lager, offener Bauauftrag und Arbeitsverband nahe einer geplanten Anlage. Ausrüstung eingeschaltet und Materialstatus sichtbar aufnehmen.',
 'Optional: supply load or depot, pending construction order and work consist near a planned object. Show enabled equipment and material status.'),
('S14', 'Verschieben im bisherigen Editor', 'Move in the existing editor',
 'Optional: denselben Bereich vor Auswahl, mit markiertem Rechteck und nach Bereich verschieben aufnehmen. Nur den vorhandenen 0.7.40-Ablauf verwenden.',
 'Optional: capture the same region before selection, with the selected rectangle and after Move region. Use the existing 0.7.40 workflow only.'),
('S15', 'Haltfall und sichere Auflösung', 'Signal return and safe release',
 'Optional: drei Zustände derselben Fahrt – vor dem Signal, Spitze jenseits des Haltfallpunkts und Zugschluss hinter der Freiprüfung. Bei einer Störung zusätzlich Restverschluss und angezeigten Grund aufnehmen.',
 'Optional: three states of one movement—before the signal, leading end beyond the passage point and tail beyond the clearance check. For a fault, also show retained lock and displayed reason.'),
('S16', 'Sicherung und Diagnose', 'Backup and diagnostics',
 'Optional: Sicherungsvorschau mit Zeit/Objektzahlen und Diagnosefenster mit Export- bzw. Ordnerfunktion aufnehmen. Keine Wiederherstellung allein für das Bild ausführen.',
 'Optional: capture the backup preview with time/object counts and the diagnostic export/folder controls. No restore operation is needed just for a screenshot.'),
]

def create():
    for lang in ['de','en']:
        de=lang=='de'; title='Aufnahmeliste für echte Spielbilder' if de else 'Capture agenda for genuine game screenshots'
        intro=('Diese Liste ist eine spätere Ergänzung, kein jetzt fälliger Arbeitsauftrag. Die Handbücher sind mit Schaubildern bereits lesbar. Priorität: S01–S12; S13–S16 sind optional. Je Vorgang können mehrere Bilder nötig sein. Alles muss die öffentliche Version 0.7.40 zeigen; spätere Testoberflächen nicht mischen.' if de else 'This is a later addition, not an immediate task. The manuals already work with explanatory diagrams. Prioritize S01–S12; S13–S16 are optional. Some workflows need several captures. Everything must show public 0.7.40; do not mix in later test interfaces.')
        format_=('PNG in voller Fensterauflösung verwenden, möglichst 1920 × 1080 oder größer; Text muss lesbar bleiben. Fenster nicht mit anderen Dialogen verdecken. IDs der Aufnahmeliste als Dateinamen verwenden, z. B. S07-01-DE.png. Oberflächensprache im Dateinamen angeben. Eine Sprache genügt zunächst; wir können Bilder mit nummerierten Hinweisen in beiden Anleitungen verwenden.' if de else 'Use full-window PNG captures, preferably 1920 × 1080 or larger, with readable text. Avoid unrelated dialogs obscuring the view. Name files after shot IDs, for example S07-01-EN.png, and identify UI language in the filename. One language is enough initially; numbered callouts can serve both manuals.')
        story=[header(title,'agenda'),para('EnusDE 0.7.40-P0 · '+('Ergänzung zur Handbuchausgabe 1.0' if de else 'Companion to manual edition 1.0')),para(intro),para(format_)]
        md=['# '+title,'','**EnusDE 0.7.40-P0 · Edition 1.0**','',intro,'',format_]
        # Fixed chunks prevent a single capture description being stranded on its own page.
        for n,s in enumerate(SHOTS):
            if n in [3,8,13]:story.append(PageBreak())
            heading=s[0]+' · '+s[1 if de else 2];body=s[3 if de else 4]
            story.extend([para(heading,'Sub'),para(body)])
            md.extend(['','## '+heading,'',body])
        base='Screenshot-Agenda-'+lang.upper()
        Doc(OUT/(base+'.pdf'),lang).build(story)
        (OUT/(base+'.md')).write_text('\n'.join(md)+'\n')
    (OUT/'README.txt').write_text('''ENUSDE SIGNAL & INTERLOCKING — STANDALONE MANUALS
Public mod release: 0.7.40-P0 | Railroader: 0.6.2 | Manual edition: 1.0

DEUTSCH
Handbuch: EnusDE-0.7.40-Handbuch-DE.pdf
Im Browser lesen: gleichnamige HTML-Datei (Bilder eingebettet, offline nutzbar).
Bearbeitbarer Text: gleichnamige Markdown-Datei; assets-Ordner mitgeben.
Screenshot-Agenda-DE.pdf beschreibt später benötigte echte Spielbilder.
23 Kapitel, 18 Abbildungen pro Handbuch: 17 erklärende Vektorschaubilder und
eine originale GFM-Modellvorschau. Es sind keine aufgenommenen Spielszenen.
Hl bleibt wie vereinbart außerhalb der ausführlichen Bedienungsanleitung.

ENGLISH
Manual: EnusDE-0.7.40-Manual-EN.pdf
Read in a browser: matching HTML file (embedded images, works offline).
Editable text: matching Markdown file; retain the assets folder.
Screenshot-Agenda-EN.pdf lists genuine game captures to add later.
23 chapters, 18 illustrations per manual: 17 explanatory vector diagrams
and one original GFM model preview. They are not captured gameplay scenes.
As agreed, a separate Hl operating tutorial is outside this edition.

SCOPE / GRUNDLAGE
These manuals describe the archived public package, not later test builds.
The sources were checked against its guides/language files; no new in-game
acceptance test or full DB-rulebook audit is claimed. Review the manuals
before publication and add real menu captures when available.
No DLL, mod installer or live diagnostic/user data is included.
The prepared original release ZIP has not been modified.

FEATURE-COVERAGE.csv maps chapters to evidence in the original release.
BUILD-RESULT.json records source hash and documentation scope.
VALIDATION.json records the document checks for this edition.
content.de.json and content.en.json provide structured editable text.

Source package SHA-256:
7fad126628bb6952f90002780fd23c28d26046f43e26a21a9b4c87e35cc2881e
''')

if __name__=='__main__':create()
