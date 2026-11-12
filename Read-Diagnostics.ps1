param(
    [Parameter(Mandatory)][string]$DiagnosticsDirectory,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'build/diagnostics-report'),
    [string]$GameDirectory = $env:RAILROADER_DIR
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'tools/Build-Environment.ps1')
$managed = Get-RailroaderManagedDirectory $GameDirectory
$checker = Join-Path $PSScriptRoot 'build/checks/RelayChecks.dll'
if (!(Test-Path -LiteralPath $checker)) {
    & (Join-Path $PSScriptRoot 'Build-Recorder.ps1') -GameDirectory $GameDirectory
    & (Join-Path $PSScriptRoot 'tools/Build-Checks.ps1') -GameDirectory $GameDirectory
}
& dotnet $checker $managed Relay.dll analyze $DiagnosticsDirectory $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw 'Diagnostic report generation failed.' }
