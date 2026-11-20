param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build-Environment.ps1')
$project=Split-Path $PSScriptRoot -Parent
$output=Join-Path $project 'build/company-settings-checks'
$null=New-Item -ItemType Directory -Path $output -Force
$sdk=Get-BuildSdk
$arguments=@('/nologo','/target:exe','/langversion:9','/nostdlib+',('/out:"'+(Join-Path $output 'CompanySettingsChecks.dll')+'"'))
Get-ChildItem -LiteralPath $sdk.ReferenceAssemblies -Filter '*.dll' | ForEach-Object {$arguments+='/reference:"'+$_.FullName+'"'}
$arguments+='"'+(Join-Path $PSScriptRoot 'CompanySettingsFixture.cs')+'"'
foreach($name in @('LocalText','BuiltInLanguage','CompanySettingsRegistration')){$arguments+='"'+(Join-Path $project ('src/'+$name+'.cs'))+'"'}
$response=Join-Path $output 'compiler.rsp'
[IO.File]::WriteAllLines($response,$arguments,[Text.UTF8Encoding]::new($false))
& dotnet exec $sdk.Compiler /noconfig ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Company settings fixture build failed.'}
@{runtimeOptions=@{tfm='net8.0';framework=@{name='Microsoft.NETCore.App';version='8.0.0'}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'CompanySettingsChecks.runtimeconfig.json') -Encoding utf8
& dotnet (Join-Path $output 'CompanySettingsChecks.dll')
if($LASTEXITCODE -ne 0){throw 'Company settings checks failed.'}
