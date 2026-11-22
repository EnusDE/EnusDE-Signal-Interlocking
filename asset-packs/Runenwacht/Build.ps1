param([string]$GameDirectory=$env:RAILROADER_DIR)
$ErrorActionPreference='Stop'
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
. (Join-Path $project 'tools/Build-Environment.ps1')
$managed=Get-RailroaderManagedDirectory $GameDirectory
$sdk=Get-BuildSdk
$output=Join-Path $project 'build/runenwacht'
$null=New-Item -ItemType Directory -Force -Path $output
$arguments=@('/nologo','/target:library','/langversion:9','/nostdlib+','/deterministic+','/optimize+',('/out:"'+(Join-Path $output 'EnusDE.Runenwacht.dll')+'"'),('/reference:"'+(Join-Path $project 'build/recorder/EnusDE.Signaltechnik.dll')+'"'))
foreach($name in @('mscorlib','System','System.Core','netstandard','Railroader.Modding','UnityEngine.CoreModule','UnityEngine.UI','UnityEngine.UIModule','Unity.TextMeshPro')){$arguments+='/reference:"'+(Join-Path $managed ($name+'.dll'))+'"'}
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | Sort-Object Name | ForEach-Object {$arguments+='"'+$_.FullName+'"'}
$response=Join-Path $output 'compiler.rsp'
[IO.File]::WriteAllLines($response,$arguments,[Text.UTF8Encoding]::new($false))
& dotnet exec $sdk.Compiler /noconfig ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Runenwacht build failed.'}
Get-Item -LiteralPath (Join-Path $output 'EnusDE.Runenwacht.dll') | Select-Object Name,Length
