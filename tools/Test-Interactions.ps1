param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build-Environment.ps1')
$project=Split-Path $PSScriptRoot -Parent
$output=Join-Path $project 'build\interaction-checks'
$null=New-Item -ItemType Directory -Path $output -Force
$sdk=Get-BuildSdk
$compiler=$sdk.Compiler
$arguments=@('/nologo','/target:exe','/langversion:9','/nostdlib+',('/out:"'+(Join-Path $output 'InteractionChecks.dll')+'"'))
Get-ChildItem -LiteralPath $sdk.ReferenceAssemblies -Filter '*.dll' | ForEach-Object {$arguments+='/reference:"'+$_.FullName+'"'}
foreach($name in @('TrackMenuIntegration','SignalHooks','AutomationHooks')){$arguments+='"'+(Join-Path $project ('src/'+$name+'.cs'))+'"'}
$arguments+='"'+(Join-Path $PSScriptRoot 'InteractionFixture.cs')+'"'
$response=Join-Path $output 'compiler.rsp'
[IO.File]::WriteAllLines($response,$arguments,[Text.UTF8Encoding]::new($false))
& dotnet exec $compiler /noconfig ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Interaction test build failed.'}
@{runtimeOptions=@{tfm='net8.0';framework=@{name='Microsoft.NETCore.App';version='8.0.0'}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'InteractionChecks.runtimeconfig.json') -Encoding utf8
& dotnet (Join-Path $output 'InteractionChecks.dll')
if($LASTEXITCODE -ne 0){throw 'Interaction callback checks failed.'}
