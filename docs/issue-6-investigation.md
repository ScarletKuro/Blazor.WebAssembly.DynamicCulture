# Investigation of issue #6: satellite loading on .NET 10

Investigation of [issue #6](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/issues/6) against pre-fix HEAD `01c724f3e5ba26414e2e43f30a1afa9f9770bfb6`, the original fix `765360d3f4673f0e193e8c48ca58ceffc1e64391`, and published package `3.2.0-preview1`. The historical findings below describe those versions. The subsequent implementation is described immediately below. Original experimental sources, browser traces, and results are in the local, Git-ignored `artifacts/issue-6/` directory; the permanent regression suite is tracked in `tests/browser/`.

**Finding:** the duplicate-load diagnosis was substantially correct, and the conditional fix executes correctly in a normal `net10.0` application. It is an incomplete workaround. The underlying change is in the JavaScript runtime's satellite asset ownership, with a separate ASP.NET Core change expanding the cultures requested at host startup. The current parent-culture exclusion fixes one additional case but leaves reproducible failures.

## Implemented resolution

The implementation removes current-culture exclusions. Requested cultures and their parents are deduplicated with ordinal case-insensitive comparison, serialized through a managed lock, and recorded as loaded only after successful completion. This permits loading the current culture before or after host startup and coalesces overlapping library calls.

For .NET 8+, a small JavaScript adapter uses the current runtime instance supplied by `JSHost.DotnetInstance`. It gives the native loader isolated per-asset manifest entries with download state removed, then synchronously restores the framework's manifest after the native call captures those entries. The framework's later host load can therefore use its own responses. Per-asset promises retain successful registrations, remove failures for retry, and drain pending operations before reporting failure. Application base paths are respected when importing the module.

The compatibility suite also exposed a separate .NET 6/7 issue: the framework replaces `readSatelliteAssemblies` after every download, but unmarshalled interop caches the previously bound function. Stable forwarding functions now resolve the latest native read function on every invocation. Module imports use the host's configured `IJSRuntime`, so async completion reaches the correct dispatcher. The same managed lock protects this older two-stage download/read operation.

`SetMiddlewareCulturesAsync` now resolves the selected cultures before loading resources. The public host-extension API and selective-loading behavior are retained; consumers do not need to enable native all-resource preloading to use the fix.

The subsequent cleanup removes the unused custom JS runtime and its element-reference converter, together with the copied `WebAssemblyCultureProvider`. The host extension calls the satellite loader directly and preserves the browser platform guard. Blazor's own host retains its ICU validation; the removed copy compared cultures captured immediately before the comparison and could not detect a change.

The tracked browser application references both actual library projects, with concrete browser-runtime and framework-package versions selected from `tests/browser/frameworks.json`. CI validates that the matrix covers both projects' complete TFM lists and runs the same Chromium scenarios for every entry. It checks resource values before `RunAsync`, subsequent loads, notification-driven language changes, storage restoration, fallback, retries, native preloading, subdirectory hosting, and response state. Runtime identity and both library DLL target-framework attributes are asserted to prevent accidental testing of the wrong TFM. See `tests/browser/README.md` for local execution and adding another framework.

Local validation of the implemented fix: 118 Chromium cases passed across `net6.0` (6.0.36), `net7.0` (7.0.20), `net8.0` (8.0.0), `net9.0` (9.0.0), and `net10.0` (10.0.0). The only two skipped cases are the .NET 8+ native-preload configuration test on .NET 6/7. These results supersede the failure matrix below for the fixed implementation; that matrix is retained as historical reproduction evidence.

Subsequent coverage work adds tests for every public `RunWith...` entry point, empty culture inputs, null arguments, and real `LanguageTrackProvider` component refreshes. Every TFM also runs the suite with Coverlet instrumentation of the two library assemblies in the browser. These counters join desktop C# and TypeScript coverage in Codecov, covering the host extensions and both modern and legacy interop paths. See the [coverage instructions](../tests/browser/README.md#managed-browser-coverage) for the collection mechanism and CI thresholds.

## What actually changed

The decisive change is dotnet/runtime commit [`6df065d5668e74dbc52427e36713a72677d62092`](https://github.com/dotnet/runtime/commit/6df065d5668e74dbc52427e36713a72677d62092), merged June 13, 2025, for [PR #116300, Bundler friendly output](https://github.com/dotnet/runtime/pull/116300). It changed boot resources from dictionaries of hashes to arrays of asset objects.

In [.NET 9 satelliteAssemblies.ts](https://github.com/dotnet/runtime/blob/v9.0.0/src/mono/browser/runtime/satelliteAssemblies.ts#L17), every invocation creates a new download asset:

```ts
const asset: AssetEntry = {
    name,
    hash: satelliteResources[culture][name],
    behavior: "resource",
    culture
};
```

In [.NET 10 satelliteAssemblies.ts](https://github.com/dotnet/runtime/blob/v10.0.0/src/mono/browser/runtime/satelliteAssemblies.ts#L20), the loader takes the object directly from the shared configuration:

```ts
const asset = satelliteResources[culture][i] as AssemblyAsset & AssetEntryInternal;
asset.behavior = "resource";
asset.culture = culture;
```

The TypeScript cast does not copy the object. The downloader mutates that shared object, retaining `pendingDownloadInternal.response` and `buffer`.

In [.NET 10 assets.ts](https://github.com/dotnet/runtime/blob/v10.0.0/src/mono/browser/runtime/loader/assets.ts#L429), `retrieve_asset_download` always goes through `start_asset_download`. The latter calls `response.arrayBuffer()`. `start_asset_download_sources` returns an existing pending response **before** checking the existing byte buffer. The satellite-loading path does not call `cleanupAsset` afterward. Consequently, another request for the same configuration asset reads the same consumed response. This can occur in one call containing duplicates or in separate calls.

The response-reading logic was already present in .NET 9; fresh asset objects prevented this particular reuse. Removal of the runtime's custom download cache and changes to fingerprinting are adjacent changes, but are not needed to explain the consumed response. Browser traces show the failing request has a valid asset URL and `bodyUsed === true`.

A controlled experiment changed only the .NET 10 satellite lookup to create a fresh object, equivalent in ownership to .NET 9. All five selected failing scenarios then succeeded: repeated loading, a shared parent, a middleware culture change, loading the startup culture, and the original sibling-culture exclusion problem. This was a temporary Playwright response substitution, with the corresponding import-map integrity hashes recomputed. No installed framework files were edited. This establishes causality; it is not a proposed deployment mechanism or an efficient final runtime implementation.

Separately, ASP.NET Core [PR #62905](https://github.com/dotnet/aspnetcore/pull/62905), merged July 31, 2025, changed native host loading from the `CurrentCulture` hierarchy to the union of the `CurrentCulture` and `CurrentUICulture` hierarchies. This matters particularly for `IgnoreCulture = true`: changing only the UI culture now creates an additional opportunity to request a satellite already loaded by this library.

## The startup timing needs correcting

[`WebAssemblyHostBuilder.CreateDefault`](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/WebAssembly/WebAssembly/src/Hosting/WebAssemblyHostBuilder.cs#L51) calls `WebAssemblyCultureProvider.Initialize`. That method captures initial culture values; it does not fetch satellites.

The framework's managed load happens inside [`WebAssemblyHost.RunAsyncCore`](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/WebAssembly/WebAssembly/src/Hosting/WebAssemblyHost.cs#L124). Native `loadAllSatelliteResources` is a separate, earlier boot-loading option.

With ordinary startup and that option disabled, the observed sequence is:

```text
CreateDefault / Build             no Probe.resources assemblies loaded yet
library loads other cultures      e.g. cs-CZ, cs
library middleware sets culture   e.g. changes en-US to cs-CZ
host.RunAsync                     native loader requests cs-CZ, cs
                                 .NET 10 reads consumed responses and throws
```

The existing comment that Microsoft "already should have loaded" the excluded cultures is therefore inaccurate for this normal startup path. Excluding the current cultures can still work: it reserves those cultures for the later framework load, provided they stay unchanged. It does not establish that they are already loaded.

Replacing or bypassing `WebAssemblyHostBuilder.CreateDefault` to suppress `Initialize` would not solve this. It does not remove the native load from `RunAsync`, and it removes framework initialization that the host expects.

## What the two fixes cover

The original commit excludes the exact current culture and UI culture names. When a requested hierarchy reaches an excluded culture it stops walking. Therefore, asking for the exact current culture also excludes its parents. However, its excluded set is not closed over parent cultures.

Example: current culture `fr-FR`, requested culture `fr-CA`. The original filter produces `fr-CA, fr`. The framework later requests `fr-FR, fr`; the shared `fr` satellite is read twice. The follow-up commit [`2a2231f`](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/commit/2a2231f1a68a5a4d80d6c831a4a9aa78858d1005) correctly expands the exclusions to the parents. This case passes with current HEAD.

Three important holes remain at HEAD:

1. **Duplicate expanded cultures.** `SatelliteAssemblies.GetCultures(IEnumerable<...>)` appends each hierarchy with `AddRange`. `fr-FR, fr-CA` expands to `fr-FR, fr, fr-CA, fr`. Deduplicating the input with `LocalizationDynamicList.Union` does not deduplicate these derived parents. With an unrelated current culture, .NET 10 fails inside the library call.
2. **Culture changes after loading.** `SetMiddlewareCulturesAsync` calls `SetDynamicCulturesAsync` before invoking the culture middleware. The exclusion set reflects the earlier culture. The framework then requests the newly selected culture at `RunAsync`. This fails even with otherwise distinct cultures.
3. **Repeated calls.** A second successful-load request for a non-current culture reuses the response; neither implementation tracks completed or in-progress satellite loads.

There is also a silent limitation when using the loader after startup: if code changes the current culture to an unloaded culture, then asks the public loader to load it, the exclusion skips it. In the published-package probe, switching from `en-US` to `cs-CZ` and calling the loader left the loaded satellite list at `en,en-US`; the Czech resource resolved to the neutral value. The documented use is startup loading, but this demonstrates why current culture is not a general indicator of loaded resources.

## Does the NET10 conditional execute?

Yes, when the `net10.0` asset is selected. A fresh application referencing the actual published `Blazor.WebAssembly.DynamicCulture` package `3.2.0-preview1` restored both packages as:

```text
lib/net10.0/Blazor.WebAssembly.DynamicCulture.dll
lib/net10.0/Blazor.WebAssembly.DynamicCulture.Loader.dll
```

The exact-culture exclusion passed in that application; its public `SetMiddlewareCulturesAsync` still reproduced the ordering failure. The selected NuGet asset entries are saved in `artifacts/issue-6/package-selection.json`.

`NET10_0_OR_GREATER` is a compile-time condition on the library's target framework, not a check of the runtime executing it. An older package or deliberately referenced `net8.0`/`net9.0` DLL does not acquire the exclusion merely by running in a .NET 10 host. The old `Microsoft.AspNetCore.Components.WebAssembly` dependency version in the loader project does not itself determine this symbol.

## Browser verification

The main matrix used source-linked loader files from HEAD, separately namespaced copies of the two original satellite files from `765360d`, and local resource files. No MudBlazor, Humanizer, or other localization package was required. Each scenario ran in a fresh headless Chromium context with an explicit startup culture. Each app pinned its managed ASP.NET Core packages and browser runtime to the version shown. The probe was built using installed SDK 10.0.402. Browser runtime versions were also printed from `RuntimeInformation.FrameworkDescription`.

| Scenario | .NET 9.0.0 | .NET 10.0.0 | .NET 10.0.13 installed locally |
| --- | --- | --- | --- |
| Native startup without library loading | Pass | Pass | Pass |
| Raw load of current hierarchy, followed by RunAsync | Pass | Fail | Fail |
| Original exclusion, current en-US, request en-US + cs-CZ | Pass | Pass | Pass |
| HEAD exclusion, same request | Pass | Pass | Pass |
| Original exclusion, current fr-FR, request fr-CA | Pass | Fail | Fail |
| HEAD exclusion, current fr-FR, request fr-CA | Pass | Pass | Pass |
| HEAD, request fr-FR + fr-CA under en-US | Pass | Fail | Fail |
| HEAD, request fr-FR + fr under en-US | Pass | Fail | Fail |
| HEAD, repeat a load of cs-CZ | Pass | Fail | Fail |
| Set final culture before HEAD loader, then RunAsync | Pass | Pass | Pass |
| HEAD loader, then change culture, then RunAsync | Pass | Fail | Fail |
| Native all-resource preload plus the simple HEAD request | Pass | Pass | Pass |
| Native all-resource preload without manual loading | Pass | Pass | Pass |
| Raw load of fr twice | Pass | Fail | Fail |

All matrix failures produced `Failed to execute 'arrayBuffer' on 'Response': body stream already read`. The error wording in this deliberately small app does not contain the reporter's outer aggregate-exception wrapper, but the inner browser error and response state match.

Representative shared-parent trace on .NET 10:

```text
fr-FR/Probe.resources.<fingerprint>.dll   bodyUsed=false
fr-CA/Probe.resources.<fingerprint>.dll   bodyUsed=false
fr/Probe.resources.<fingerprint>.dll      bodyUsed=false
fr/Probe.resources.<fingerprint>.dll      bodyUsed=true  <-- failure
```

The additional published-package tests confirmed that its simple request passes, its shared-parent request fails, its full public culture middleware fails when it selects cs-CZ after loading, and invoking the middleware before the loader passes. Five runtime ownership-substitution tests and two post-start/native-configuration checks bring the completed browser scenarios to 53.

The traces count `Response.arrayBuffer` invocations, not network downloads. In particular, .NET 9's cache implementation may read a cloned response as well; extra reads with `bodyUsed=false` are not evidence of the same consumed response being reused.

## Why adding an unrelated package exposes it

The runtime ignores requested cultures absent from `satelliteResources`. An application with only neutral English resources can repeatedly request `en-US` and `en` without touching any satellite response for those names. Adding a package with an English satellite makes the same request encounter a real asset. Likewise, adding a neutral parent satellite can expose duplicate parents that previously had no downloadable resource.

The minimal reproduction includes explicit `en`, `en-US`, `cs`, `cs-CZ`, `fr`, `fr-FR`, and `fr-CA` resource assemblies to make these cases observable. Their presence, rather than package-specific application code, is sufficient to reproduce the problem.

## What a complete resolution should do

For applications that can load all translations at boot, the native approach documented in this repository was verified to provide the requested resources without manually using the satellite loader:

```html
<script src="_framework/blazor.webassembly.js" autostart="false"></script>
<script>
Blazor.start({
    configureRuntime: runtime => runtime.withConfig({
        loadAllSatelliteResources: true
    })
});
</script>
```

Retain the application's existing fingerprint placeholder/import-map setup when applying that configuration. If keeping this library's middleware, storage helpers, and change notifications, disable its manual satellite loading:

```csharp
builder.Services.AddLocalizationDynamic(options =>
{
    options.LoadAllSatelliteCultureAssembliesAtOnce = false;
    // Configure the application's default and supported cultures as usual.
});

var host = builder.Build();
await host.SetMiddlewareCulturesAsync();
await host.RunAsync();
```

That combination was tested against the published package and returned the actual Czech resource. Removing the entire library is unnecessary if its other features are wanted. Loading ICU globalization data with `BlazorWebAssemblyLoadAllGlobalizationData` is a separate setting and does not itself preload translation satellites.

For selective startup loading, a library fix should resolve the final cultures before planning downloads, deduplicate the complete expanded hierarchies using ordinal case-insensitive comparison, and reserve the final current/UI hierarchies for the host. It should also define and enforce repeated-call behavior, with shared tracking of successful and in-progress operations if repeated calls are supported. Moving the middleware alone fixes the tested ordering case but leaves the parent-duplication and repeated-call cases.

At the runtime layer, the stronger fix is to make loading a configuration asset safe to repeat or share concurrently, including the managed registration step. Restoring fresh download objects proves the ownership issue but can repeat downloads and registrations. A per-asset shared load promise or another explicit completed/in-flight state is a better candidate for an upstream fix; its handling of boot preloads, failures/retries, and multiple runtime instances would need dedicated tests. This investigation does not claim that such a complete upstream fix was implemented.

The library calls `INTERNAL.loadSatelliteAssemblies`, so it depends on private runtime implementation details. A proposed framework issue should include the minimal repeated-load and duplicate-parent examples, the exact ownership-changing commit, and the response-state trace. No GitHub issue, comment, or pull request was posted as part of this investigation.

## Scope and reproduction evidence

The issue's original .NET 10 RC1 Mono assertion is not the same diagnostic as the later consumed-response report. It was not reproduced here. The reporter's later component-refresh inconsistency also cannot be conclusively assigned to this response-loading bug without testing the particular components and notification path. The known satellite failures are sufficient to explain the browser exception; they do not establish the cause of every symptom in that thread.

Tests used development builds, local HTTP, explicit full ICU data, and DLL resources for straightforward tracing. Release publishing, AOT, service-worker/offline behavior, every shipping .NET 9/10 patch, and .NET 11 were not exhaustively tested. The third version above is the exact locally available 10.0.13 package/runtime tested, not a claim about the current public release schedule. The isolated runtime experiment deliberately altered the ownership expression; all main matrix tests used unmodified framework files.

Evidence files under `artifacts/issue-6/`:

- `browser-results.json`: all 42 main cases, console output, resource values, and response stacks.
- `extra-browser-results.json`: four actual published-package cases.
- `clone-browser-results.json`: five isolated runtime ownership experiments.
- `poststart-browser-results.json`: silent current-culture exclusion and native loading with library middleware.
- `matrix.json` and `package-selection.json`: compact outcomes and NuGet asset selection.
- `framework-source/`: the versioned source files inspected.
- `setup-probes.ps1`, `run-probes.cjs`, and supporting probe sources: the executable experiments.

The original experiment workspace is `%TEMP%\dynamicculture-issue6-investigation`. The setup script recreates the three main apps beside itself and accepts `-Repo` pointing to this repository. Build each `Probe.csproj`, then run the generated `net9`, `net10`, and `net10latest` apps on ports 5285, 5286, and 5287. Run `node run-probes.cjs` to repeat the main matrix. The archived runners accept `PLAYWRIGHT_MODULE` and `PLAYWRIGHT_CHROMIUM` environment variables, defaulting to the local paths used for this investigation. Supplemental apps are preserved in `net10package` and `net10poststart`, using ports 5288 and 5289. The supplemental ownership runner writes `clone-browser-results.json`; the published-package results are separately retained in `extra-browser-results.json`.
