$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build-Environment.ps1')
$project=Split-Path $PSScriptRoot -Parent
$output=Join-Path $project 'build/runenwacht-geometry-checks'
$null=New-Item -ItemType Directory -Path $output -Force
$sdk=Get-BuildSdk
$arguments=@('/nologo','/target:exe','/langversion:9','/nostdlib+',('/out:"'+(Join-Path $output 'GeometryChecks.dll')+'"'))
Get-ChildItem -LiteralPath $sdk.ReferenceAssemblies -Filter '*.dll' | ForEach-Object {$arguments+='/reference:"'+$_.FullName+'"'}
$arguments+='"'+(Join-Path $PSScriptRoot 'RunenwachtGeometryFixture.cs')+'"'
$arguments+='"'+(Join-Path $project 'asset-packs/Runenwacht/src/RunenwachtGeometry.cs')+'"'
foreach($name in @('CrystalChannel','RunenwachtDesign')){$arguments+='"'+(Join-Path $project ('asset-packs/Runenwacht/src/'+$name+'.cs'))+'"'}
$response=Join-Path $output 'compiler.rsp';[IO.File]::WriteAllLines($response,$arguments,[Text.UTF8Encoding]::new($false))
& dotnet exec $sdk.Compiler /noconfig ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Runenwacht geometry fixture failed to compile.'}
@{runtimeOptions=@{tfm='net8.0';framework=@{name='Microsoft.NETCore.App';version='8.0.0'}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'GeometryChecks.runtimeconfig.json') -Encoding utf8
& dotnet (Join-Path $output 'GeometryChecks.dll') (Join-Path $project 'build/runenwacht-preview.json')
if($LASTEXITCODE -ne 0){throw 'Runenwacht geometry fixture failed.'}
