# EnusDE Signal & Interlocking

[Deutsche Zusammenfassung](#deutsch) · [Releases](https://github.com/EnusDE/EnusDE-Signal-Interlocking/releases) · [Build from source](docs/BUILD.md)

EnusDE Signal & Interlocking adds configurable railway signals and interlocking panels to Railroader. Place signals and track-circuit boxes, draw a track diagram, link it to the in-game railway, and set train and shunting routes from the dispatcher panel. The panel shows track occupancy and supports train-number displays.

## Compatibility

**Railroader 0.6.2** is the exact game dependency declared by this release. The mod uses Railroader's built-in asset-pack and mod system. The game is the only required dependency. If the optional EnusDE translation mod is installed, the mod can read its speed-unit preference.

For multiplayer, use the same mod version on the host and all clients. The host manages the shared installation and interlocking. Live multiplayer validation for 0.7.15 is pending.

## Install

1. Download `EnusDE-Signal-Interlocking-0.7.15-P0.zip` from the release assets.
2. Install the ZIP with your Railroader-compatible mod manager. For a manual install, extract the `EnusDE-Signaltechnik` folder directly into `Documents\Railroader\Mods`. The mod folder should contain `Info.json`.
3. Restart Railroader. Open **Mods** on the main menu, enable **EnusDE Signal & Interlocking**, and apply the change.
4. Load a world and click **STW** next to the clock to open Build, Editor, or Dispatcher.

[Official Railroader installation guide](https://docs.railroadergame.com/modding/installing-mods/)

## Release status

**0.7.15 is a pre-release.** The compiled mod and offline checks are verified. In-game checks for track-circuit boundaries, turnout occupancy, stock signal lamp suppression, and multiplayer remain open. See the [0.7.15 notes and setup guide (German)](docs/CHANGES-0.7.15.de.md).

## Licensing

See [Third-party notices](THIRD_PARTY_NOTICES.md) for the components under MIT and their full notices. The repository currently has no project-wide open-source license.

## Deutsch

EnusDE Signal & Interlocking ergänzt Railroader 0.6.2 um eigene Lichtsignale, Gleisbildstellwerke, Fahrstraßen, Besetztanzeigen und Zugnummernfelder. Lade das Installations-ZIP unter [Releases](https://github.com/EnusDE/EnusDE-Signal-Interlocking/releases), installiere es über deinen Modmanager und aktiviere es im Spiel. Über **STW** neben der Uhr erreichst du Bauen, Editor und Fahrdienstleiter. 0.7.15 ist eine Vorabversion. Im Mehrspieler brauchen alle dieselbe Modversion.
