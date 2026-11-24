param([string]$OutputDirectory=(Join-Path (Split-Path $PSScriptRoot -Parent) 'build/runenwacht-packages'))
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$mainRoot=Join-Path $project 'mod/EnusDE-Signaltechnik'
$packRoot=Join-Path $project 'asset-packs/Runenwacht/mod/EnusDE-Runenwacht'
$main=Get-Content -LiteralPath (Join-Path $mainRoot 'Info.json') -Raw | ConvertFrom-Json
$pack=Get-Content -LiteralPath (Join-Path $packRoot 'Info.json') -Raw | ConvertFrom-Json
if($pack.dependencies.required -notcontains 'enusde/signaltechnik >=0.7.64'){throw 'Runenwacht must require the presentation API baseline (main 0.7.64 or later).'}
$mainDll=Get-Item -LiteralPath (Join-Path $project 'build/recorder/EnusDE.Signaltechnik.dll')
$packDll=Get-Item -LiteralPath (Join-Path $project 'build/runenwacht/EnusDE.Runenwacht.dll')
$results=Get-Item -LiteralPath (Join-Path $project 'build/checks/RESULTS.txt')
$text=Get-Content -LiteralPath $results.FullName -Raw
if($text -match 'FAIL:' -or $text -notmatch 'PASS: separately compiled Runenwacht assembly implements' -or $text -notmatch 'PASS: main head rear cap fills' -or $text -notmatch 'PASS: native dependency parser accepts the presentation baseline'){throw 'Run the full successful Test.ps1 suite first.'}
foreach($pair in @(@((Join-Path $project 'src'),$mainDll),@((Join-Path $project 'asset-packs/Runenwacht/src'),$packDll))){
    if(Get-ChildItem -LiteralPath $pair[0] -Filter '*.cs' | Where-Object {$_.LastWriteTimeUtc -gt $pair[1].LastWriteTimeUtc}){throw 'Sources are newer than their compiled assembly.'}
}
if($results.LastWriteTimeUtc -lt $mainDll.LastWriteTimeUtc -or $results.LastWriteTimeUtc -lt $packDll.LastWriteTimeUtc){throw 'Assemblies are newer than the successful full test result.'}
$null=New-Item -ItemType Directory -Path $OutputDirectory -Force
$plans=@(
    @{Root=$mainRoot;Folder='EnusDE-Signaltechnik';Dll=$mainDll.FullName;Name=('EnusDE-Signal-Interlocking-'+$main.version+'-P0-0.6.3-TEST.zip')},
    @{Root=$packRoot;Folder='EnusDE-Runenwacht';Dll=$packDll.FullName;Name=('EnusDE-Runenwacht-'+$pack.version+'-TEST.zip')}
)
foreach($plan in $plans){if(Test-Path -LiteralPath (Join-Path $OutputDirectory $plan.Name)){throw ('Refusing to overwrite existing archive: '+$plan.Name)}}
$created=@()
try {
    foreach($plan in $plans){
        $path=Join-Path $OutputDirectory $plan.Name
        $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        $created+=$path
        $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
        try {
            foreach($file in (Get-ChildItem -LiteralPath $plan.Root -Recurse -File | Sort-Object FullName)){
                if($file.Extension -in @('.dll','.exe','.pdb','.ps1','.zip','.pyc') -or $file.Name -match '(?i)^(settings|preferences)\.json$'){throw 'Unexpected binary, tool or private file in package source.'}
                $relative=[IO.Path]::GetRelativePath($plan.Root,$file.FullName).Replace('\','/')
                $null=[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.FullName,($plan.Folder+'/'+$relative),[IO.Compression.CompressionLevel]::Optimal)
            }
            $null=[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$plan.Dll,($plan.Folder+'/'+[IO.Path]::GetFileName($plan.Dll)),[IO.Compression.CompressionLevel]::Optimal)
        }finally{$zip.Dispose();$stream.Dispose()}
        Get-Item -LiteralPath $path | Select-Object Name,Length,FullName
    }
}catch{foreach($path in $created){if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}};throw}
