param(
    [string]$PackageDirectory = "$PSScriptRoot/../artifacts/packages"
)

$ErrorActionPreference = 'Stop'

$requirements = @{
    'Blazor.WebAssembly.DynamicCulture' = @('LocalStorageManager.js', 'NavigatorManager.js', 'QueryManager.js')
    'Blazor.WebAssembly.DynamicCulture.Loader' = @('SatelliteAssemblies.js')
}

foreach ($name in $requirements.Keys) {
    $pattern = '^' + [regex]::Escape($name) + '\.[0-9]'
    $packages = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter '*.nupkg' -File |
        Where-Object { $_.Name -match $pattern })
    if ($packages.Count -ne 1) {
        throw "Expected exactly one $name .nupkg package, found $($packages.Count)."
    }

    $package = $packages[0]
    $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        foreach ($asset in $requirements[$name]) {
            $path = "staticwebassets/$asset"
            $entry = $archive.GetEntry($path)
            if ($null -eq $entry -or $entry.Length -eq 0) {
                throw "Package $($package.Name) is missing a non-empty $path."
            }
        }
    }
    finally {
        $archive.Dispose()
    }

    Write-Host "Verified JavaScript in $($package.Name)."
}
