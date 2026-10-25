param(
    [string]$ReportDirectory = 'artifacts/coverage/combined',
    [double]$MinimumLineCoverage = 95
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$lines = @{}

function Add-Line([string]$File, [int]$Number, [int]$Hits) {
    $normalized = $File.Replace('\', '/')
    $offset = [Math]::Max($normalized.LastIndexOf('Blazor.WebAssembly.DynamicCulture/'), $normalized.LastIndexOf('Blazor.WebAssembly.DynamicCulture.Loader/'))
    if ($offset -lt 0) { return }
    $relative = 'src/' + $normalized.Substring($offset)
    # Only repository production sources: generated logging helpers, fixtures,
    # and dependency code have no matching source file in a clean checkout.
    if ($relative -match '/(obj|bin)/|\.test\.ts$' -or !(Test-Path -LiteralPath (Join-Path $repository $relative) -PathType Leaf)) { return }
    if (!$lines.ContainsKey($relative)) { $lines[$relative] = @{} }
    $lines[$relative][$Number] = [Math]::Max($Hits, [int]$lines[$relative][$Number])
}

$reports = @(Get-ChildItem -LiteralPath $ReportDirectory -Recurse -Filter '*.cobertura.xml')
if ($reports.Count -eq 0) { throw 'No managed coverage reports found.' }
foreach ($report in $reports) {
    [xml]$coverage = Get-Content -LiteralPath $report.FullName -Raw
    foreach ($class in $coverage.coverage.packages.package.classes.class) {
        foreach ($line in $class.lines.line) {
            Add-Line $class.filename ([int]$line.number) ([int]$line.hits)
        }
    }
}

$lcovReports = @(Get-ChildItem -LiteralPath $ReportDirectory -Recurse -Filter 'lcov.info')
if ($lcovReports.Count -eq 0) { throw 'No TypeScript coverage reports found.' }
foreach ($report in $lcovReports) {
    $source = $null
    foreach ($record in Get-Content -LiteralPath $report.FullName) {
        if ($record.StartsWith('SF:')) { $source = $record.Substring(3) }
        elseif ($record -match '^DA:(\d+),(\d+)') { Add-Line $source ([int]$Matches[1]) ([int]$Matches[2]) }
    }
}

# Catch missing reports as well as low percentages. Browser-only entry points
# must remain visible even if instrumentation or artifact collection regresses.
$required = @(
    'Blazor.WebAssembly.DynamicCulture/Extensions/WebAssemblyHostExtensions.cs',
    'Blazor.WebAssembly.DynamicCulture/Extensions/WeakRefCollectionExtensions.cs',
    'Blazor.WebAssembly.DynamicCulture.Loader/WebAssemblyHostExtensions.cs',
    'Blazor.WebAssembly.DynamicCulture.Loader/Interop/SatelliteAssemblies.cs',
    'Blazor.WebAssembly.DynamicCulture.Loader/Interop/SatelliteAssemblies.new.cs',
    'Blazor.WebAssembly.DynamicCulture.Loader/Interop/SatelliteAssemblies.old.cs',
    'Blazor.WebAssembly.DynamicCulture.Loader/assets/SatelliteAssemblies.ts',
    'Blazor.WebAssembly.DynamicCulture/assets/LocalStorageManager.ts',
    'Blazor.WebAssembly.DynamicCulture/assets/NavigatorManager.ts',
    'Blazor.WebAssembly.DynamicCulture/assets/QueryManager.ts'
)
foreach ($file in $required) {
    if (!$lines.ContainsKey("src/$file")) { throw "Missing production coverage: $file" }
}

$summary = foreach ($file in $lines.Keys | Sort-Object) {
    $covered = @($lines[$file].Values | Where-Object { $_ -gt 0 }).Count
    [pscustomobject]@{ File = $file; Covered = $covered; Total = $lines[$file].Count; Percent = [Math]::Round(100 * $covered / $lines[$file].Count, 2) }
}
foreach ($entry in $summary) {
    Write-Host ('{0,6:N2}% {1,3}/{2,-3} {3}' -f $entry.Percent, $entry.Covered, $entry.Total, $entry.File)
}
$coveredTotal = ($summary.Covered | Measure-Object -Sum).Sum
$lineTotal = ($summary.Total | Measure-Object -Sum).Sum
$percent = 100 * $coveredTotal / $lineTotal
Write-Host ('Production line coverage: {0:N2}% ({1}/{2}); required {3}%' -f $percent, $coveredTotal, $lineTotal, $MinimumLineCoverage)
if ($percent -lt $MinimumLineCoverage) { throw 'Production line coverage is below the required threshold.' }
foreach ($file in $required) {
    $entry = $summary | Where-Object File -EQ "src/$file"
    # RunAsync never completes during a normal browser session, so its callers'
    # closing sequence points remain uncovered even when startup succeeds.
    if ($entry.Percent -lt 85) { throw "Coverage below 85% in $file ($($entry.Percent)%)." }
}
