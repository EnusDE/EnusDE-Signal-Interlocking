param([string]$GameDirectory=$env:RAILROADER_DIR)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'tools/Build-Environment.ps1')
if([string]::IsNullOrWhiteSpace($env:DOTNET_CLI_HOME)){$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot 'build\dotnet-home'}
& (Join-Path $PSScriptRoot 'Build-Recorder.ps1') -GameDirectory $GameDirectory
& (Join-Path $PSScriptRoot 'tools\Build-Checks.ps1') -GameDirectory $GameDirectory
$managed=Get-RailroaderManagedDirectory $GameDirectory
$checker=Join-Path $PSScriptRoot 'build\checks\RelayChecks.dll'
$script=Join-Path $PSScriptRoot 'mod\EnusDE-Signaltechnik\Scripts\Main.rl'
$results=@()
$result=& (Join-Path $PSScriptRoot 'tools\Test-SettingsWindow.ps1') 2>&1
$results+=$result
$result | ForEach-Object {Write-Output $_}
$result=& (Join-Path $PSScriptRoot 'tools\Test-NativeMaterials.ps1') -GameDirectory $GameDirectory 2>&1
$results+=$result
$result | ForEach-Object {Write-Output $_}
$result=& (Join-Path $PSScriptRoot 'tools\Test-Window.ps1') 2>&1
$results+=$result
$result | ForEach-Object {Write-Output $_}
$result=& (Join-Path $PSScriptRoot 'tools\Test-Interactions.ps1') 2>&1
$results+=$result
$result | ForEach-Object {Write-Output $_}
$result=& (Join-Path $PSScriptRoot 'tools\Test-BlockViewActions.ps1') -GameDirectory $GameDirectory 2>&1
$results+=$result
$result | ForEach-Object {Write-Output $_}
foreach($mode in @('compile','test')){
    $result=& dotnet $checker $managed Relay.dll $mode $script 2>&1
    $code=$LASTEXITCODE
    $result | ForEach-Object {Write-Output $_}
    $results+=$result
    if($code -ne 0){throw ('Relay-Prüfung fehlgeschlagen: '+$mode)}
}
foreach($mode in @('native','bridge')){
    $result=& dotnet $checker $managed Relay.dll $mode $PSScriptRoot 2>&1
    $code=$LASTEXITCODE
    $result | ForEach-Object {Write-Output $_}
    $results+=$result
    if($code -ne 0){throw ('Recorder-Prüfung fehlgeschlagen: '+$mode)}
}
$mod=Join-Path $PSScriptRoot 'mod\EnusDE-Signaltechnik'
$manifest=Get-Content -LiteralPath (Join-Path $mod 'Info.json') -Raw | ConvertFrom-Json
if($manifest.identifier -ne 'enusde/signaltechnik' -or $manifest.version -ne '0.7.61'){throw 'Unerwartete Paketkennung/Version.'}
if($manifest.dependencies.required.Count -ne 1 -or $manifest.dependencies.required[0] -ne 'rr/game ==0.6.3'){throw 'Unerwartete Spiel-/Mod-Abhängigkeit.'}
if($manifest.scope -ne 'unscoped' -or $manifest.assemblies.Count -ne 1 -or $manifest.assemblies[0] -ne 'EnusDE.Signaltechnik.dll'){throw 'Gemeinsame Anlage muss dieselbe Pflichtversion und nur die eigene Assembly deklarieren.'}
if(Get-ChildItem -LiteralPath $mod -Recurse -File | Where-Object {$_.Extension -in @('.dll','.exe','.ps1')}){throw 'Testwerkzeug im spielbaren Paket gefunden.'}
$results+='PASS: mandatory matching multiplayer scope, exact game dependency and only the own assembly declared'
if(!(Test-Path -LiteralPath (Join-Path $managed '0Harmony.dll'))){throw 'Getestete Harmony-Laufzeit der Spielinstallation fehlt.'}
if(!(Test-Path -LiteralPath (Join-Path $mod 'LICENSE-ModsTab.txt'))){throw 'MIT-Lizenzhinweis für Mods-Reiter fehlt.'}
if(!(Test-Path -LiteralPath (Join-Path $mod 'LICENSE-NativeWindow.txt'))){throw 'MIT-Lizenzhinweis für Fensterbereinigung fehlt.'}
$results+='PASS: retained component notices and the installed game Harmony; no menu dependency'
$results[-1]
$results | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'build\checks\RESULTS.txt') -Encoding utf8
