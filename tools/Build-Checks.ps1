param([string]$GameDirectory=$env:RAILROADER_DIR)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build-Environment.ps1')
$project=Split-Path $PSScriptRoot -Parent
$output=Join-Path $project 'build\checks'
$null=New-Item -ItemType Directory -Path $output -Force
$managed=Get-RailroaderManagedDirectory $GameDirectory
$sdk=Get-BuildSdk
$compiler=$sdk.Compiler
$arguments=@('/nologo','/target:exe','/langversion:12','/nostdlib+','/deterministic+',('/out:"'+(Join-Path $output 'RelayChecks.dll')+'"'))
Get-ChildItem -LiteralPath $sdk.ReferenceAssemblies -Filter '*.dll' | ForEach-Object {$arguments+='/reference:"'+$_.FullName+'"'}
$arguments+='/reference:"'+(Join-Path $managed 'Relay.dll')+'"'
$arguments+='/reference:"'+(Join-Path $managed 'Newtonsoft.Json.dll')+'"'
$arguments+='/reference:"'+(Join-Path $project 'build\recorder\EnusDE.Signaltechnik.dll')+'"'
foreach($name in @('Railroader','Railroader.Modding','MessagePack','com.rlabrecque.steamworks.net','UnityEngine.CoreModule','0Harmony')){$arguments+='/reference:"'+(Join-Path $managed ($name+'.dll'))+'"'}
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'RelayChecks') -Filter '*.cs' | Sort-Object Name | ForEach-Object {$arguments+='"'+$_.FullName+'"'}
$response=Join-Path $output 'compiler.rsp'
[IO.File]::WriteAllLines($response,$arguments,[Text.UTF8Encoding]::new($false))
& dotnet exec $compiler /noconfig ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Prüfwerkzeug konnte nicht gebaut werden.'}
@{runtimeOptions=@{tfm='net8.0';framework=@{name='Microsoft.NETCore.App';version='8.0.0'}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'RelayChecks.runtimeconfig.json') -Encoding utf8
