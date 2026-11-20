param([string]$GameDirectory=$env:RAILROADER_DIR)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build-Environment.ps1')
$project=Split-Path $PSScriptRoot -Parent
$output=Join-Path $project 'build/block-view-action-checks'
$null=New-Item -ItemType Directory -Path $output -Force
$sdk=Get-BuildSdk
$managed=Get-RailroaderManagedDirectory $GameDirectory
$arguments=@('/nologo','/target:exe','/langversion:12','/nostdlib+','/nowarn:0436',('/out:"'+(Join-Path $output 'BlockViewActions.dll')+'"'))
Get-ChildItem -LiteralPath $sdk.ReferenceAssemblies -Filter '*.dll' | ForEach-Object {$arguments+='/reference:"'+$_.FullName+'"'}
$arguments+='/reference:"'+(Join-Path $project 'build/recorder/EnusDE.Signaltechnik.dll')+'"'
$arguments+='/reference:"'+(Join-Path $managed 'Newtonsoft.Json.dll')+'"'
$arguments+='"'+(Join-Path $project 'src/BlockViewWindow.cs')+'"'
$arguments+='"'+(Join-Path $PSScriptRoot 'BlockViewActionsFixture.cs')+'"'
$response=Join-Path $output 'compiler.rsp'
[IO.File]::WriteAllLines($response,$arguments,[Text.UTF8Encoding]::new($false))
& dotnet exec $sdk.Compiler /noconfig ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Block view action test build failed.'}
Copy-Item -LiteralPath (Join-Path $project 'build/recorder/EnusDE.Signaltechnik.dll') -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $managed 'Newtonsoft.Json.dll') -Destination $output -Force
@{runtimeOptions=@{tfm='net8.0';framework=@{name='Microsoft.NETCore.App';version='8.0.0'}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'BlockViewActions.runtimeconfig.json') -Encoding utf8
& dotnet (Join-Path $output 'BlockViewActions.dll') $managed
if($LASTEXITCODE -ne 0){throw 'Block view action checks failed.'}
