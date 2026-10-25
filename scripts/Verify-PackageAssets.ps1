param(
    [string]$PackageDirectory = "$PSScriptRoot/../artifacts/packages"
)

$ErrorActionPreference = 'Stop'

$packages = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter 'Blazor.WebAssembly.DynamicCulture.*.nupkg' -File |
    Where-Object { $_.Name -notlike 'Blazor.WebAssembly.DynamicCulture.Loader.*' })
if ($packages.Count -ne 1) {
    throw "Expected exactly one Blazor.WebAssembly.DynamicCulture .nupkg package, found $($packages.Count)."
}

$package = $packages[0]
$archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
try {
    foreach ($asset in @('LocalStorageManager.js', 'NavigatorManager.js', 'QueryManager.js')) {
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

Write-Host "Verified generated JavaScript in $($package.Name)."
