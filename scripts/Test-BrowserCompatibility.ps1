param(
    [string[]]$TargetFrameworks = @()
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$testDirectory = Join-Path $repository 'tests/browser'
$project = Join-Path $repository 'tests/Blazor.WebAssembly.DynamicCulture.BrowserTests.App'
$frameworks = Get-Content -LiteralPath "$testDirectory/frameworks.json" -Raw | ConvertFrom-Json
& node "$testDirectory/framework-matrix.cjs"
if ($LASTEXITCODE -ne 0) { throw 'The browser matrix does not cover the supported TFMs.' }
if ($TargetFrameworks.Count -gt 0) {
    foreach ($tfm in $TargetFrameworks) {
        if ($tfm -notin $frameworks.tfm) { throw "Unknown browser target: $tfm" }
    }
    $frameworks = @($frameworks | Where-Object { $_.tfm -in $TargetFrameworks })
}

$npm = if ($IsWindows) { 'npm.cmd' } else { 'npm' }
$previousTarget = $env:BROWSER_TFM
try {
    foreach ($framework in $frameworks) {
        Write-Host "Testing $($framework.tfm), runtime $($framework.version)"
        & dotnet build $project -c Release --nologo "-p:BrowserTargetFramework=$($framework.tfm)" "-p:BrowserFrameworkVersion=$($framework.version)"
        if ($LASTEXITCODE -ne 0) { throw "Build failed for $($framework.tfm)." }
        $env:BROWSER_TFM = $framework.tfm
        & $npm test --prefix $testDirectory
        if ($LASTEXITCODE -ne 0) { throw "Browser tests failed for $($framework.tfm)." }
    }
}
finally {
    $env:BROWSER_TFM = $previousTarget
}
