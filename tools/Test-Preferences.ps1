param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build-Environment.ps1')
$project=Split-Path $PSScriptRoot -Parent
$output=Join-Path $project 'build\preferences-checks'
$null=New-Item -ItemType Directory -Path $output -Force
$sdk=Get-BuildSdk
$compiler=$sdk.Compiler
$arguments=@('/nologo','/target:exe','/langversion:9','/nostdlib+',('/out:"'+(Join-Path $output 'PreferencesChecks.dll')+'"'))
Get-ChildItem -LiteralPath $sdk.ReferenceAssemblies -Filter '*.dll' | ForEach-Object {$arguments+='/reference:"'+$_.FullName+'"'}
$arguments+='"'+(Join-Path $PSScriptRoot 'PreferencesFixture.cs')+'"'
$arguments+='"'+(Join-Path $project 'src\ModsPreferencesRegistration.cs')+'"'
$response=Join-Path $output 'compiler.rsp'
[IO.File]::WriteAllLines($response,$arguments,[Text.UTF8Encoding]::new($false))
& dotnet exec $compiler /noconfig ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Preferences test build failed.'}
@{runtimeOptions=@{tfm='net8.0';framework=@{name='Microsoft.NETCore.App';version='8.0.0'}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'PreferencesChecks.runtimeconfig.json') -Encoding utf8
& dotnet (Join-Path $output 'PreferencesChecks.dll')
if($LASTEXITCODE -ne 0){throw 'Preferences registration checks failed.'}
