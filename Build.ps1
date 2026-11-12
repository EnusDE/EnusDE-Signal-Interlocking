param(
    [string]$GameDirectory = $env:RAILROADER_DIR,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'build/packages')
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Test.ps1') -GameDirectory $GameDirectory
$manifestRoot = Join-Path $PSScriptRoot 'mod/EnusDE-Signaltechnik'
$manifest = Get-Content -LiteralPath (Join-Path $manifestRoot 'Info.json') -Raw | ConvertFrom-Json
$stage = Join-Path $PSScriptRoot ('build/stage-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $stage,$OutputDirectory -Force
$mod = Join-Path $stage 'EnusDE-Signaltechnik'
Copy-Item -LiteralPath $manifestRoot -Destination $mod -Recurse
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'build/recorder/EnusDE.Signaltechnik.dll') -Destination $mod
$asset = Join-Path $OutputDirectory ("EnusDE-Signal-Interlocking-" + $manifest.version + "-P0.zip")
Compress-Archive -LiteralPath $mod -DestinationPath $asset -Force
Get-Item -LiteralPath $asset
