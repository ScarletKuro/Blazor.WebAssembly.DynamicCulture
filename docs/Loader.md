# Blazor.WebAssembly.DynamicCulture.Loader

[![Nuget](https://img.shields.io/nuget/v/Blazor.WebAssembly.DynamicCulture.Loader?color=ff4081&logo=nuget)](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture.Loader/)
[![Nuget](https://img.shields.io/nuget/dt/Blazor.WebAssembly.DynamicCulture.Loader?color=ff4081&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture.Loader/)
[![GitHub](https://img.shields.io/github/license/ScarletKuro/Blazor.WebAssembly.DynamicCulture?color=594ae2&logo=github)](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/blob/master/LICENSE)

A lightweight package for loading satellite culture assemblies at startup in Blazor WebAssembly applications. This package enables dynamic culture switching without requiring page reloads.

## 📢 Important: Native Blazor Alternative Available

**Blazor now provides a native way to load all satellite assemblies** in WebAssembly applications. You can configure this in your `index.html`:

**Before:**
```html
<script src="_framework/blazor.webassembly.js"></script>
```

**After:**
```html
<script src="_framework/blazor.webassembly.js" autostart="false"></script>
<script>
   Blazor.start({ 
       configureRuntime: runtime => runtime.withConfig({ 
           loadAllSatelliteResources: true 
       }) 
   })
</script>
```

**This native approach may eliminate the need for this package** as it provides the same core functionality of loading all satellite resource assemblies at startup. However, this package offers:
- Selective culture loading (load only specific cultures instead of all)
- Simpler API without manual Blazor startup configuration
- Compatibility with older Blazor versions that may not support the native feature

Evaluate which solution best fits your project requirements.

## 📦 Overview

By default, Blazor WebAssembly loads resource assemblies only for the current culture during initial app startup. If you change the culture dynamically later, you must reload the page to load the new resource assemblies.

This package solves that problem by loading all specified localization satellite assemblies simultaneously during startup, eliminating the need for page reloads when switching cultures.

## 🚀 Getting Started

### Installation

Install the package via NuGet:

```bash
dotnet add package Blazor.WebAssembly.DynamicCulture.Loader
```

### Usage Examples

Configure culture assembly loading in your Blazor WASM `Program.cs`:

**Option 1: Load and then run**
```csharp
using Blazor.WebAssembly.DynamicCulture.Loader;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

var host = builder.Build();
await host.LoadSatelliteCultureAssembliesAsync(new[] 
{ 
    new CultureInfo("ru"), 
    new CultureInfo("et") 
});
await host.RunAsync();
```

**Option 2: Load and run in one call**
```csharp
using Blazor.WebAssembly.DynamicCulture.Loader;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

await builder.Build().RunWithSatelliteCultureAssembliesAsync(new[] 
{ 
    new CultureInfo("ru"), 
    new CultureInfo("et") 
});
```

> **⚠️ Note:** Do not use this package for Blazor Server applications. Blazor Server handles resource assemblies differently and does not require this functionality.

## 🔧 When to Use This Package

Use this package if you:
- Need to switch cultures dynamically without page reloads
- Want to load only specific cultures (not all available cultures)
- Are using an older Blazor version without native `loadAllSatelliteResources` support
- Prefer a simpler API over manual Blazor startup configuration

Consider the native Blazor solution if you:
- Want to load all available satellite resources automatically
- Are using a recent Blazor version with native support
- Prefer using built-in Blazor features over third-party packages

## 📖 Additional Resources

- [Main Package: Blazor.WebAssembly.DynamicCulture](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture/) - Full dynamic localization solution with culture providers and automatic component refresh
- [Main Repository Documentation](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture)
- [Sample Project](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/tree/master/samples/Blazor.WebAssembly.Sample.DynamicCulture)

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/blob/master/LICENSE) file for details.
