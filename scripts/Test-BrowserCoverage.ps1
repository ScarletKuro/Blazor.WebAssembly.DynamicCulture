param(
    [string[]]$TargetFrameworks = @()
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$frameworks = Get-Content -LiteralPath "$repository/tests/browser/frameworks.json" -Raw | ConvertFrom-Json
if ($TargetFrameworks.Count -gt 0) {
    foreach ($tfm in $TargetFrameworks) {
        if ($tfm -notin $frameworks.tfm) { throw "Unknown browser target: $tfm" }
    }
    $frameworks = @($frameworks | Where-Object { $_.tfm -in $TargetFrameworks })
}

$previousTarget = $env:BROWSER_TFM
$previousCoverage = $env:BROWSER_COVERAGE
Push-Location $repository
try {
    & node tests/browser/framework-matrix.cjs
    if ($LASTEXITCODE -ne 0) { throw 'The browser matrix does not cover the supported TFMs.' }
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Could not restore Coverlet.' }
    $node = (Get-Command node).Source
    foreach ($framework in $frameworks) {
        $tfm = $framework.tfm
        # Keep the ordinary compatibility run too: coverage serves instrumented PE
        # copies, whereas the normal run exercises the SDK's default Webcil output.
        & dotnet build tests/Blazor.WebAssembly.DynamicCulture.BrowserTests.App -c Release --nologo `
            "-p:BrowserTargetFramework=$tfm" "-p:BrowserFrameworkVersion=$($framework.version)" -p:WasmEnableWebcil=false `
            -p:BrowserCoverage=true -p:ContinuousIntegrationBuild=true
        if ($LASTEXITCODE -ne 0) { throw "Coverage fixture build failed for $tfm." }
        $env:BROWSER_TFM = $tfm
        $env:BROWSER_COVERAGE = '1'
        & dotnet tool run coverlet "tests/Blazor.WebAssembly.DynamicCulture.BrowserTests.App/bin/Release/$tfm" `
            --target $node --targetargs 'tests/browser/node_modules/@playwright/test/cli.js test --config tests/browser/playwright.config.cjs' `
            --source-mapping-file "tests/Blazor.WebAssembly.DynamicCulture.BrowserTests.App/bin/Release/$tfm/browser-coverage-source-roots.txt" `
            --include '[Blazor.WebAssembly.DynamicCulture*]*' --format cobertura --output "artifacts/coverage/browser/$tfm.cobertura.xml"
        if ($LASTEXITCODE -ne 0) { throw "Managed browser coverage tests failed for $tfm." }
    }
}
finally {
    $env:BROWSER_TFM = $previousTarget
    $env:BROWSER_COVERAGE = $previousCoverage
    Pop-Location
}
