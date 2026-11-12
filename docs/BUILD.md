# Build from source

Use Windows, PowerShell 7, the .NET 8 SDK, and a local installation of Railroader 0.6.2. The build reads the game libraries from `Railroader_Data/Managed` in your installation. The source repository and release asset carry no game libraries.

Run from the repository folder and substitute the path to your own game installation.

```powershell
./Build.ps1 -GameDirectory '<Railroader installation folder>'
```

Alternatively, set `RAILROADER_DIR` to the game installation folder before building. The build runs the offline checks, then writes the install ZIP to `build/packages/`. Use `-OutputDirectory` to place the ZIP elsewhere. Build outputs, including test results, stay under the ignored `build/` folder by default.

For checks only, use `Test.ps1` with the same game path. For the DLL only, use `Build-Recorder.ps1`. The build and checks run without starting the game.
