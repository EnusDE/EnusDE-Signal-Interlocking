param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Build-Environment.ps1')
$project=Split-Path $PSScriptRoot -Parent
$output=Join-Path $project 'build/settings-window-checks'
$null=New-Item -ItemType Directory -Path $output -Force
$sdk=Get-BuildSdk
$arguments=@('/nologo','/target:exe','/langversion:9','/nostdlib+',('/out:"'+(Join-Path $output 'SettingsWindowChecks.dll')+'"'))
Get-ChildItem -LiteralPath $sdk.ReferenceAssemblies -Filter '*.dll' | ForEach-Object {$arguments+='/reference:"'+$_.FullName+'"'}
$arguments+='"'+(Join-Path $PSScriptRoot 'SettingsWindowFixture.cs')+'"'
foreach($name in @('LocalText','BuiltInLanguage','SettingsWindow','ModePicker','ModePickerText','ModeIconRaster','WorkspaceSymbols')){
    $arguments+='"'+(Join-Path $project ('src/'+$name+'.cs'))+'"'
}
$response=Join-Path $output 'compiler.rsp'
[IO.File]::WriteAllLines($response,$arguments,[Text.UTF8Encoding]::new($false))
& dotnet exec $sdk.Compiler /noconfig ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Settings window fixture build failed.'}
@{runtimeOptions=@{tfm='net8.0';framework=@{name='Microsoft.NETCore.App';version='8.0.0'}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'SettingsWindowChecks.runtimeconfig.json') -Encoding utf8
& dotnet (Join-Path $output 'SettingsWindowChecks.dll')
if($LASTEXITCODE -ne 0){throw 'Settings window checks failed.'}
