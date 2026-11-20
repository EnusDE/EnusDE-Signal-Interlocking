# EnusDE Signal & Interlocking

[Deutsche Zusammenfassung](#deutsch) · [Build from source](docs/BUILD.md) · [Development status](docs/DEVELOPMENT.md)

**Current source: 0.7.59 — Early Alpha for Railroader 0.6.3.** The `main` branch is the canonical source checkpoint. A source commit is not a GitHub release or an installable download; obtain an explicitly published package from the author's distribution channel, or build from source. This README does not assert that a matching GitHub release asset exists.

Place configurable railway signals, draw an interlocking diagram, connect it to the in-game railway, and operate train and shunting routes. Track occupancy, train-number displays, line permission and configurable automation support larger installations.

## Current features

- Configurable main, shunting and distant signals, including a low main-signal variant and optional experimental Hl models.
- Separate Build, Editor and Dispatcher workspaces; direct world-object assignment and an expanding diagram canvas with row/column controls and mouse-centred zoom.
- Train/shunting routes with point, flank, occupancy and locking checks.
- GFM track detection, train-number forwarding and read-only display copies.
- GWB line permission, directional block working and a step-by-step setup tutorial supporting multiple signals per role.
- Checked connection removal, block-division replacement and optional station receiving-capacity settings.
- Editable block assemblies with section selection, horizontal reflection and 180-degree rotation. Additional panels can show reduced track/number views; the operating panel retains the complete source/control view.
- Optional contact automation, master interlocking panels, construction/material rules and external installation-configuration backups.
- Built-in English/German interfaces and F1 help. English is the default; German does not require another mod.

0.7.59 includes corrections to block-preview placement, actions during live train-number updates, and continued editing after saving/reopening. See the [English 0.7.59 notes](docs/BLOCK-VIEW-REEDIT-0.7.59.en.md) or [German notes](docs/BLOCK-VIEW-REEDIT-0.7.59.de.md).

## Compatibility and validation

The manifest requires **Railroader 0.6.3**. The mod uses the game's built-in mod system; no additional mod is required. The optional EnusDE translation mod can supply language/unit preferences. Host and every client must use **the same mod version, 0.7.59**.

The author reports working gameplay, including GWB, but this remains an Early Alpha. The last recorded full offline check run on 19 November 2026 produced **1,792 PASS and 0 FAIL**, including production-window callback and repeated save/reload/reopen checks. That run used the available older game libraries with the 0.6.3 user-path adapter; **a build against verified original 0.6.3 libraries and complete in-game/UI/multiplayer validation remain open**. See [development status and continuation](docs/DEVELOPMENT.md) for the evidence and limits.

## Install and start

1. Back up your save and mod configuration; begin with a test world or save copy.
2. Install a matching published ZIP with a Railroader-compatible mod manager, or build it using [these instructions](docs/BUILD.md). For manual installation, extract `EnusDE-Signaltechnik` directly into `Documents\Railroader\Mods`, with `Info.json` inside that folder.
3. Restart Railroader, enable **EnusDE Signal & Interlocking** in **Mods**, and apply.
4. Open **IXL / STW** beside the clock for Build, Editor and Dispatcher. Assign the diagram objects, then use the GWB tutorial or manual settings as appropriate. Review final activation and test the installation before enabling automation.

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

**Aktueller Quellstand: 0.7.59, Early Alpha für Railroader 0.6.3.** `main` ist der maßgebliche Quellstand für die Weiterentwicklung. Ein Quellcode-Commit ist kein GitHub-Release und kein installierbares Paket.

Die Mod ergänzt eigene Signale, Gleisbildstellwerke, Zug-/Rangierfahrstraßen, Gleisfreimeldung, Zugnummern sowie GWB- und Blockbetrieb. Der GWB-Assistent führt schrittweise durch die Einrichtung; Blockansichten sind verschiebbar, spiegelbar und um 180 Grad drehbar. Zusätzliche Tafeln können vereinfachte Ansichten zeigen. Englisch und Deutsch sind eingebaut; im Mehrspieler brauchen alle dieselbe Modversion.

Der Autor meldet funktionierenden Spielbetrieb. Die letzte belegte Offline-Prüfung ergab **1.792 PASS, 0 FAIL**. Ein Build gegen nachweislich originale 0.6.3-Bibliotheken und eine vollständige Spiel-/UI-/Mehrspielerabnahme stehen noch aus. Vor Updates Spielstand und Konfiguration sichern. [Prüfstand und Weiterarbeit](docs/DEVELOPMENT.md#deutsch) · [Selbst bauen](docs/BUILD.md).
