# EnusDE Signal & Interlocking

[Deutsche Zusammenfassung](#deutsch) · [Build from source](docs/BUILD.md) · [Development status](docs/DEVELOPMENT.md)

**Current source: 0.7.64 — Early Alpha for Railroader 0.6.3.** The `main` branch is the canonical source checkpoint. A source commit is not a GitHub release or an installable download; obtain an explicitly published package from the author's distribution channel, or build from source. This README does not assert that a matching GitHub release asset exists.

Place configurable railway signals, draw an interlocking diagram, connect it to the in-game railway, and operate train and shunting routes. Track occupancy, train-number displays, line permission and configurable automation support larger installations.

## Current features

- Configurable main, shunting and distant signals, including a low main-signal variant and optional experimental Hl models.
- Separate Build, Editor and Dispatcher workspaces; direct world-object assignment and an expanding diagram canvas with row/column controls and mouse-centred zoom.
- Explicit Build mode with a right-hand icon palette, local virtual plans and construction sites for ordinary viewers. Native captive supply orders and MOW warehouse loading support active MOW work-train construction.
- Train/shunting routes with point, flank, occupancy and locking checks.
- GFM track detection, train-number forwarding and read-only display copies.
- GWB line permission, directional block working and a step-by-step setup tutorial supporting multiple signals per role.
- Checked connection removal, block-division replacement and optional station receiving-capacity settings.
- Editable block assemblies with section selection, horizontal reflection and 180-degree rotation. Additional panels can show reduced track/number views; the operating panel retains the complete source/control view.
- Optional contact automation, master interlocking panels, construction/material rules and external installation-configuration backups.
- Standalone settings through the launcher gear, available before milestone unlock; native game mod-management pages are preserved.
- Built-in English/German interfaces and F1 help. English is the default; German does not require another mod.

0.7.64 / Runenwacht 0.2.0 improves the models and daylight materials, adds framed technical/name plates, and provides host-only individual lamp tests without routes. Native output stays Stop during a test; affected routes are excluded. [English instructions](mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.64.en.md) · [Deutsch](mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.64.de.md). In-game/AI/MP acceptance is still pending.

0.7.63 adds an optional presentation-only API and separate experimental Runenwacht crystal pack. The main mod remains usable alone. [Pack reference](asset-packs/Runenwacht/README.md) · [English guide](mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.63.en.md) · [Deutsch](mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.63.de.md). World test licenses are free and separate from session activation; in-game/MP acceptance remains open.

0.7.62 adds the missing native interchange purchase suppliers for signal cargos. [Buying and cargo appearance](mod/EnusDE-Signaltechnik/Guides/INTERCHANGE-0.7.62.en.md) · [Einkauf und Ladungsdarstellung](mod/EnusDE-Signaltechnik/Guides/INTERCHANGE-0.7.62.de.md).

0.7.61 reorganizes building and material logistics, preserving existing installations and legacy stock. [English instructions and game tests](mod/EnusDE-Signaltechnik/Guides/BUILD-MOW-0.7.61.en.md) · [Deutsche Anleitung und Spieltests](mod/EnusDE-Signaltechnik/Guides/BUILD-MOW-0.7.61.de.md).

0.7.60 moved settings entirely into the launcher. [English instructions](mod/EnusDE-Signaltechnik/Guides/SETTINGS-0.7.60.en.md) · [Deutsche Anleitung](mod/EnusDE-Signaltechnik/Guides/SETTINGS-0.7.60.de.md).

0.7.59 includes corrections to block-preview placement, actions during live train-number updates, and continued editing after saving/reopening. See the [English 0.7.59 notes](docs/BLOCK-VIEW-REEDIT-0.7.59.en.md) or [German notes](docs/BLOCK-VIEW-REEDIT-0.7.59.de.md).

## Compatibility and validation

The manifest requires **Railroader 0.6.3**. The mod uses the game's built-in mod system; no additional mod is required. The optional EnusDE translation mod can supply language/unit preferences. Host and every client must use **the same mod version, 0.7.64**.

The author reports working gameplay of earlier builds, including GWB, but this remains an Early Alpha. The new code compiles against the local game libraries; the installation metadata identifies Railroader 0.6.3. The complete offline build/check run on 20 November 2026 passed **2,024 checks**, including native interchange supplier preparation, material integration and local build-view policy checks. **The new ordering, loading, construction views and multiplayer behavior still require in-game validation.** See [development status and continuation](docs/DEVELOPMENT.md) for the evidence and limits.

## Install and start

1. Back up your save and mod configuration; begin with a test world or save copy.
2. Install a matching published ZIP with a Railroader-compatible mod manager, or build it using [these instructions](docs/BUILD.md). For manual installation, extract `EnusDE-Signaltechnik` directly into `Documents\Railroader\Mods`, with `Info.json` inside that folder.
3. Restart Railroader, enable **EnusDE Signal & Interlocking** in **Mods**, and apply.
4. Open **IXL / STW** beside the clock for Build, Editor, Dispatcher and the settings gear. Assign the diagram objects, then use the GWB tutorial or manual settings as appropriate. Review final activation and test the installation before enabling automation.

[Official Railroader installation guide](https://docs.railroadergame.com/modding/installing-mods/)

## Guides and development

- [GWB tutorial](docs/GWB-TUTORIAL-0.7.53.en.md) · [Deutsch](docs/GWB-TUTORIAL-0.7.53.de.md)
- [Block views and destination links](docs/BLOCK-VIEWS-0.7.55.en.md) · [Deutsch](docs/BLOCK-VIEWS-0.7.55.de.md)
- [Current block-view corrections](docs/BLOCK-VIEW-REEDIT-0.7.59.en.md) · [Deutsch](docs/BLOCK-VIEW-REEDIT-0.7.59.de.md)
- [Changelog](mod/EnusDE-Signaltechnik/CHANGELOG.en.md), [tutorial/user guide — work in progress](mod/EnusDE-Signaltechnik/Guides/TUTORIAL.en.md), [backup guide (German)](docs/BACKUPS.de.md)
- [Development status and source checkpoint workflow](docs/DEVELOPMENT.md), [project instructions](AGENTS.md), [bug reports](https://github.com/EnusDE/EnusDE-Signal-Interlocking/issues)

Versioned older guides and manuals describe their historical versions, not necessarily every current UI detail or validation result. A full dispatcher/job AI, semaphore signals and a remote web dispatcher are not included.

## Licensing

See [Third-party notices](THIRD_PARTY_NOTICES.md) and the included MIT component notices. No project-wide open-source license is currently provided. Game libraries are referenced from a local installation and are not redistributed with the sources or mod package.

## Deutsch

**Aktueller Quellstand: 0.7.64, Early Alpha für Railroader 0.6.3.** `main` ist der maßgebliche Quellstand für die Weiterentwicklung. Ein Quellcode-Commit ist kein GitHub-Release und kein installierbares Paket.

Die Mod ergänzt eigene Signale, Gleisbildstellwerke, Zug-/Rangierfahrstraßen, Gleisfreimeldung, Zugnummern sowie GWB- und Blockbetrieb. Der GWB-Assistent führt schrittweise durch die Einrichtung; Blockansichten sind verschiebbar, spiegelbar und um 180 Grad drehbar. Zusätzliche Tafeln können vereinfachte Ansichten zeigen. Englisch und Deutsch sind eingebaut; im Mehrspieler brauchen alle dieselbe Modversion.

0.7.64 / Runenwacht 0.2.0 verbessert Formen, Kristalloberflächen und Materialien bei Tageslicht, ergänzt eingefasste Kennungs-/Namensschilder und einzelne Lampentests ohne Fahrstraße. Native Ausgabe im Test Halt; betroffene Fahrstraßen gesperrt. [Neue Anleitung](mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.64.de.md). Spiel-/KI-/Mehrspielerprüfung noch offen.

0.7.63 ergänzt eine optionale Darstellungsschnittstelle und das getrennte experimentelle Runenwacht-Kristallpaket mit festen Farben und Lichtanimationen. Das Hauptmod bleibt allein verwendbar; Welt-Testlizenz und Sitzungsaktivierung sind getrennt. [Paket-Anleitung](mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.63.de.md).

0.7.62 ergänzt fehlende native Interchange-Lieferanten für den Materialkauf. 0.7.61 ergänzte den ausdrücklichen Baumodus mit rechter Symbolleiste und Baustellendarstellung sowie normale Materialwagenbestellung und MOW-Lageranbindung. Alte Anlagen und Bestände bleiben erhalten. Der Code kompiliert gegen die lokalen 0.6.3-Spielbibliotheken; 2.024 Offline-Prüfungen sind bestanden. Die neuen Liefer-, Bau-, Darstellungs- und Mehrspielerabläufe müssen noch im Spiel geprüft werden. Einstellungen bleiben am Zahnrad im Auswahlfenster. Vor Updates Spielstand und Konfiguration sichern. [Prüfstand und Weiterarbeit](docs/DEVELOPMENT.md#deutsch) · [Selbst bauen](docs/BUILD.md).
