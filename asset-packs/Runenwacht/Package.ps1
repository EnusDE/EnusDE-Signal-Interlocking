param([string]$OutputDirectory=(Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'build/runenwacht-packages'))
$ErrorActionPreference='Stop'
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source=Join-Path $PSScriptRoot 'mod/EnusDE-Runenwacht'
$manifest=Get-Content -LiteralPath (Join-Path $source 'Info.json') -Raw | ConvertFrom-Json
$main=Get-Content -LiteralPath (Join-Path $project 'mod/EnusDE-Signaltechnik/Info.json') -Raw | ConvertFrom-Json
if($manifest.dependencies.required -notcontains ('enusde/signaltechnik =='+$main.version)){throw 'Pack/main dependency mismatch.'}
$dll=Get-Item -LiteralPath (Join-Path $project 'build/runenwacht/EnusDE.Runenwacht.dll')
$results=Get-Item -LiteralPath (Join-Path $project 'build/checks/RESULTS.txt')
$text=Get-Content -LiteralPath $results.FullName -Raw
if($text -match 'FAIL:' -or $text -notmatch 'PASS: inactive channels and disabled emission remain dark' -or $text -notmatch 'PASS: plaque arms and clamp reach'){throw 'Run the complete successful Test.ps1 suite first.'}
if($results.LastWriteTimeUtc -lt $dll.LastWriteTimeUtc -or (Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | Where-Object {$_.LastWriteTimeUtc -gt $dll.LastWriteTimeUtc})){throw 'Source/build/test checkpoint is stale.'}
$null=New-Item -ItemType Directory -Path $OutputDirectory -Force
$path=Join-Path $OutputDirectory ('EnusDE-Runenwacht-'+$manifest.version+'-for-'+$main.version+'-TEST.zip')
if(Test-Path -LiteralPath $path){throw ('Refusing to overwrite existing archive: '+$path)}
$created=$false
try {
    $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);$created=$true
    $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
    try {
        foreach($file in (Get-ChildItem -LiteralPath $source -Recurse -File | Sort-Object FullName)){
            if($file.Extension -notin @('.json','.md','.txt')){throw 'Unexpected optional-pack source payload.'}
            $name='EnusDE-Runenwacht/'+[IO.Path]::GetRelativePath($source,$file.FullName).Replace('\','/')
            $null=[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.FullName,$name,[IO.Compression.CompressionLevel]::Optimal)
        }
        $null=[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$dll.FullName,'EnusDE-Runenwacht/EnusDE.Runenwacht.dll',[IO.Compression.CompressionLevel]::Optimal)
    }finally{$zip.Dispose();$stream.Dispose()}
}catch{if($created -and (Test-Path -LiteralPath $path)){Remove-Item -LiteralPath $path};throw}
Get-Item -LiteralPath $path | Select-Object Name,Length,FullName
