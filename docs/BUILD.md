# Build from source — 0.7.59 / Railroader 0.6.3

Use PowerShell 7, the .NET 8 SDK, and a local Railroader installation with the required game libraries. Windows is the normal game-installation environment; the recorded Cloud checks used PowerShell/.NET on Linux. The build reads `Railroader_Data/Managed` from the selected installation. Game libraries are not included in Git or install packages.

From the repository directory, substitute your own game installation path:

```powershell
./Build.ps1 -GameDirectory '<Railroader installation folder>'
```

Alternatively set `RAILROADER_DIR`. `Build.ps1` runs the checks and stages the mod package under `build/packages/`; `-OutputDirectory` selects another package destination. This script creates/replaces its computed package filename. **Preserve existing prepared archives: use a new output directory when that name already exists.** Building is not a GitHub release or runtime installation.

For checks only, run `Test.ps1` with the same game path. For the mod DLL only, use `Build-Recorder.ps1`. No game launch is needed. All 273 `src/*.cs` files are compiled; `tools/Build-Checks.ps1` and the fixture scripts build the offline checks. Local build output is ignored by Git.

The manifest requires **Railroader 0.6.3**. The user-path adapter can also compile against older libraries; that does not prove original-0.6.3 compatibility. The last recorded run used the available older library set. A build against verified original 0.6.3 libraries and complete in-game/multiplayer validation remain open. See [development status](DEVELOPMENT.md) for the last 1,792 passing checks and their limits.

## Deutsch

Quellstand **0.7.59**, Spielabhängigkeit **Railroader 0.6.3**. Benötigt werden PowerShell 7, .NET 8 SDK und lokal bereitgestellte Spielbibliotheken. `Build.ps1` baut und prüft; `Test.ps1` führt nur Build/Prüfungen aus. Kein Spielstart erforderlich. Vorhandene Pakete nicht überschreiben; gegebenenfalls ein neues Ausgabeverzeichnis wählen. Spiel-DLLs und Build-Ergebnisse nicht zu Git hinzufügen. Der Original-0.6.3-Build und vollständige Spiel-/Mehrspielerprüfungen stehen noch aus.
