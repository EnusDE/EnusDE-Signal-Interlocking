function Get-BuildSdk {
    $sdk = & dotnet --list-sdks | Where-Object { $_ -match '^8\.' } | Select-Object -Last 1
    if ($LASTEXITCODE -ne 0 -or $sdk -notmatch '^([^ ]+) \[(.+)\]') {
        throw 'Install the .NET 8 SDK to build Signaltechnik.'
    }
    $version = $Matches[1]
    $sdkDirectory = $Matches[2]
    $referenceRoot = Join-Path (Split-Path -Parent $sdkDirectory) 'packs/Microsoft.NETCore.App.Ref'
    $referencePack = Get-ChildItem -LiteralPath $referenceRoot -Directory |
        Where-Object { $_.Name -match '^8\.\d+\.\d+$' } |
        Sort-Object { [version]$_.Name } | Select-Object -Last 1
    if (!$referencePack) { throw 'The .NET 8 reference pack is missing.' }
    @{
        Compiler = Join-Path (Join-Path $sdkDirectory $version) 'Roslyn/bincore/csc.dll'
        ReferenceAssemblies = Join-Path $referencePack.FullName 'ref/net8.0'
    }
}

function Get-RailroaderManagedDirectory([string]$GameDirectory) {
    if ([string]::IsNullOrWhiteSpace($GameDirectory)) {
        throw 'Set -GameDirectory or RAILROADER_DIR to the Railroader 0.6.2 installation directory.'
    }
    $managed = Join-Path $GameDirectory 'Railroader_Data/Managed'
    if (!(Test-Path -LiteralPath (Join-Path $managed 'Railroader.dll') -PathType Leaf)) {
        throw 'Railroader.dll was not found. Select the game installation directory, which contains Railroader_Data.'
    }
    (Resolve-Path -LiteralPath $managed).Path
}
