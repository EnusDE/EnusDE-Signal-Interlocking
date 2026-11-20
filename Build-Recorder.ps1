param([string]$GameDirectory=$env:RAILROADER_DIR)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'tools/Build-Environment.ps1')
$managed=Get-RailroaderManagedDirectory $GameDirectory
$output=Join-Path $PSScriptRoot 'build\recorder';$null=New-Item -ItemType Directory -Path $output -Force
$sdk=Get-BuildSdk
$compiler=$sdk.Compiler
$argsList=@('/nologo','/target:library','/langversion:9','/nostdlib+','/deterministic+','/optimize+',('/out:"'+(Join-Path $output 'EnusDE.Signaltechnik.dll')+'"'))
foreach($name in @('mscorlib','System','System.Core','System.IO.Compression','netstandard','Railroader.Modding','Railroader','Relay','Core','Markroader','Newtonsoft.Json','KeyValue.Runtime','AssetPack.Common','MessagePack','com.rlabrecque.steamworks.net','UnityEngine.CoreModule','UnityEngine.PhysicsModule','Unity.InputSystem','UnityEngine.UIModule','UnityEngine.UI','Unity.TextMeshPro','UnityEngine.TextCoreFontEngineModule','0Harmony')){$argsList+='/reference:"'+(Join-Path $managed ($name+'.dll'))+'"'}
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | Sort-Object Name | ForEach-Object {$argsList+='"'+$_.FullName+'"'}
$response=Join-Path $output 'compiler.rsp';[IO.File]::WriteAllLines($response,$argsList,[Text.UTF8Encoding]::new($false))
& dotnet exec $compiler /noconfig ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Recorder-Build fehlgeschlagen.'}
Get-Item -LiteralPath (Join-Path $output 'EnusDE.Signaltechnik.dll') | Select-Object Name,Length
