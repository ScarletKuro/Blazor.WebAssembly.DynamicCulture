# Browser compatibility tests

These tests build a real Blazor WebAssembly application referencing both library projects and run it in Chromium. Resource values must be correct before `RunAsync`, after startup, and after language switching. Tests also reject consumed-response reads and unhandled JavaScript exceptions, and verify the actual runtime and the target-framework attributes of both loaded library DLLs.

`frameworks.json` is the compatibility matrix. `framework-matrix.cjs` compares it with both libraries' `TargetFrameworks`; CI fails if any supported TFM is missing. To add .NET 11, add its TFM to both libraries and an entry with its SDK and concrete package/runtime version to `frameworks.json`. The same tests run without another workflow job or a new sample application. Update the workflow's common SDK version when the solution requires a newer compiler.

Install a .NET SDK that can build the solution (currently .NET 10), the ASP.NET Core runtime for the selected TFM, Node 24, and Chromium:

```sh
cd tests/browser
npm ci
npx playwright install chromium
cd ../..
dotnet build tests/Blazor.WebAssembly.DynamicCulture.BrowserTests.App -c Release -p:BrowserTargetFramework=net10.0 -p:BrowserFrameworkVersion=10.0.0
```

Then run `npm test` from `tests/browser`. Set `BROWSER_TFM` to select another matrix entry; its app must have been built with the matching properties. `PLAYWRIGHT_CHROMIUM` optionally specifies an existing browser executable. Playwright starts and stops the fixture's development server automatically. Do not run multiple TFM suites concurrently on the same checkout: they share the fixture's intermediate build directory and port 5290.

To build and run every matrix entry sequentially, use `pwsh ./scripts/Test-BrowserCompatibility.ps1` from the repository root after installing the Node dependencies and browser. Pass `-TargetFrameworks net9.0,net10.0` to limit a local run.

The browser runtime is pinned on the `Microsoft.NETCore.App` framework reference. Setting the global `RuntimeFrameworkVersion` property would also override SDK tool-package versions, which prevents modern SDKs from building the .NET 6/7 fixtures correctly.

The tests cover current and sibling cultures, shared parents, duplicate input, selective loading, repeated/concurrent requests, middleware ordering, separate formatting/UI cultures, fallback, failed-download retries, loading after startup, notification-driven UI changes, and interoperability with native preloading. The native preload configuration test applies to .NET 8+; the remaining scenarios run on every supported TFM.

Failure artifacts are saved under `artifacts/browser/<tfm>/`: HTML and JUnit reports, Playwright traces, screenshots, and satellite-state attachments. CI uploads these even when a test fails. Each test uses a fresh browser context, avoiding cached resources or a prior test's runtime state.

The TypeScript isolation adapter in `src/Blazor.WebAssembly.DynamicCulture.Loader/assets/SatelliteAssemblies.ts` also has focused unit tests. Bun generates its browser JavaScript before static web assets are collected, using the same build integration as the main library. Generated JavaScript is ignored by Git and included in the NuGet package.

Run `npm run typecheck` from `tests/browser` for strict type checking of the adapter. Browser CI runs this check before building the fixture.

```sh
bun test ./tests/browser/satellite-loader.test.ts
```

The modern adapter still uses the runtime's internal satellite-loading API. It gives each operation private asset state and restores the framework's manifest synchronously after the native loader captures the assets. Tests against each runtime deliberately exercise this boundary so a future schema or scheduling change becomes an actionable CI failure. Successful per-asset promises are retained; failed operations are removed for retry, and outstanding registrations are drained before reporting an error. A managed lock serializes callers, including the legacy .NET 6/7 two-stage download/read API.

## Managed browser coverage

CI runs the compatibility suite a second time with Coverlet for every matrix entry. Locally, from the repository root:

```powershell
pwsh ./scripts/Test-BrowserCoverage.ps1
pwsh ./scripts/Test-BrowserCoverage.ps1 -TargetFrameworks net6.0,net10.0
```

How it works:

- The fixture is built with `WasmEnableWebcil=false` and `ContinuousIntegrationBuild=true`, so local and CI runs share the same `/_/` PDB paths. A build target writes MSBuild's source-root mappings for Coverlet's `--source-mapping-file`; without them Coverlet silently skips both libraries.
- Coverlet instruments the two library DLLs in the fixture's output directory. A test-only `loadBootResource` callback serves those PE copies (bypassing integrity hashes); the rest of the boot is unchanged.
- WASM cannot write Coverlet's hit files, so after each test the fixture returns its hit counters through JS interop and `managed-coverage.cjs` merges them into the host's hit files ([tracker format](https://github.com/coverlet-coverage/coverlet/blob/v10.0.0/src/coverlet.core/Instrumentation/ModuleTrackerTemplate.cs)). Playwright global setup fails early if the DLLs are not instrumented.

Reports go to `artifacts/coverage/browser/coverage-<tfm>.cobertura.xml`; diagnostics to `artifacts/browser/<tfm>/coverage/`. The `coverage-` prefix matches Codecov's automatic file discovery. Rebuild the fixture normally before running the ordinary compatibility suite again.

The `verify-coverage` CI job merges browser, desktop and Bun reports, runs `scripts/Verify-Coverage.ps1` (95% combined production line coverage, 85% floor per host/interop file), then uploads everything to Codecov. To run it locally, point `-ReportDirectory` at a folder with all `*.cobertura.xml` files and Bun's `lcov.info` (run Bun from the repository root so paths are unambiguous). Uncovered lines are mostly non-browser guards and the code after `WebAssemblyHost.RunAsync`, which never returns.
