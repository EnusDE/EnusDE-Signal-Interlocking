# Build from source — 0.7.77 / Railroader 0.6.3

Use PowerShell 7, the .NET 8 SDK, and a local Railroader installation with the required game libraries. Windows is the normal game-installation environment; the recorded Cloud checks used PowerShell/.NET on Linux. The build reads `Railroader_Data/Managed` from the selected installation. Game libraries are not included in Git or install packages.

From the repository directory, substitute your own game installation path:

```powershell
./Build.ps1 -GameDirectory '<Railroader installation folder>'
```

Alternatively set `RAILROADER_DIR`. `Build.ps1` runs the checks and stages the mod package under `build/packages/`; `-OutputDirectory` selects another package destination. This script creates/replaces its computed package filename. **Preserve existing prepared archives: use a new output directory when that name already exists.** Building is not a GitHub release or runtime installation.

For checks only, run `Test.ps1` with the same game path. For the mod DLL only, use `Build-Recorder.ps1`. No game launch is needed. All 297 `src/*.cs` files are compiled; `tools/Build-Checks.ps1` and the fixture scripts build the offline checks. `tools/Test-NativeMaterials.ps1` compiles the production storage/transfer adapter against controlled native stand-ins; the main checks also inspect the installed game's API contracts. Local build output is ignored by Git.

The manifest requires **Railroader 0.6.3**. The user-path adapter can also compile against older libraries; that does not prove original-0.6.3 compatibility. The current code compiles against the local game installation whose Unity player metadata identifies 0.6.3. Complete in-game/multiplayer validation remains open. See [development status](DEVELOPMENT.md) for the actual checks and their limits.

## Deutsch

Quellstand **0.7.77**, Spielabhängigkeit **Railroader 0.6.3**. Benötigt werden PowerShell 7, .NET 8 SDK und lokal bereitgestellte Spielbibliotheken. `Build.ps1` baut und prüft, `Test.ps1` führt Build und Prüfungen aus. Vorhandene Pakete über ein neues Ausgabeverzeichnis erhalten. Spiel-DLLs und Build-Ergebnisse bleiben lokal. Vollständige Spiel- und Mehrspielerprüfungen stehen noch aus.

The full suite also builds `asset-packs/Runenwacht` and checks presentation/persistence/light transitions plus closed procedural geometry with controlled math/mesh stand-ins. Build only the optional pack with its own `Build.ps1` after building the main DLL. Package both already-tested outputs with `tools/Package-Runenwacht.ps1`; this refuses existing archive names and never installs.

Der Gesamttest baut auch das optionale Runenwacht-Paket; die Geometrieprüfung nutzt kontrollierte Mathematik-/Mesh-Stellvertreter und ist kein Unity-Bildtest. `tools/Package-Runenwacht.ps1` erstellt zwei neue Pakete aus vorher geprüften Outputs und überschreibt keine vorhandenen Archive.

`tools/Preview-Runenwacht.py` optionally renders the production geometry export in Blender. This is an external geometry/material study, not an in-game screenshot or a new source of signal authority.
