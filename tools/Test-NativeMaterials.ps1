param([string]$GameDirectory=$env:RAILROADER_DIR)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build-Environment.ps1')
$project=Split-Path $PSScriptRoot -Parent
$output=Join-Path $project 'build/native-material-checks'
$null=New-Item -ItemType Directory -Path $output -Force
$sdk=Get-BuildSdk
$managed=Get-RailroaderManagedDirectory $GameDirectory
$arguments=@('/nologo','/target:exe','/langversion:9','/nostdlib+',('/out:"'+(Join-Path $output 'NativeMaterialChecks.dll')+'"'))
Get-ChildItem -LiteralPath $sdk.ReferenceAssemblies -Filter '*.dll' | ForEach-Object {$arguments+='/reference:"'+$_.FullName+'"'}
$arguments+='/reference:"'+(Join-Path $managed 'Newtonsoft.Json.dll')+'"'
$arguments+='"'+(Join-Path $PSScriptRoot 'NativeMaterialsFixture.cs')+'"'
foreach($name in @('LocalText','BuiltInLanguage','NativeMaterialAmounts','NativeMaterialLogistics','NativeMaterialTransfer','NativeConnectionMaterials')){$arguments+='"'+(Join-Path $project ('src/'+$name+'.cs'))+'"'}
$response=Join-Path $output 'compiler.rsp';[IO.File]::WriteAllLines($response,$arguments,[Text.UTF8Encoding]::new($false))
& dotnet exec $sdk.Compiler /noconfig ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Native material fixture build failed.'}
@{runtimeOptions=@{tfm='net8.0';framework=@{name='Microsoft.NETCore.App';version='8.0.0'}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'NativeMaterialChecks.runtimeconfig.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $managed 'Newtonsoft.Json.dll') -Destination $output -Force
& dotnet (Join-Path $output 'NativeMaterialChecks.dll')
if($LASTEXITCODE -ne 0){throw 'Native material checks failed.'}
