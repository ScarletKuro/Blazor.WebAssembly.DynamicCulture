# Blazor.WebAssembly.DynamicCulture.Loader
[![Nuget](https://img.shields.io/nuget/v/Blazor.WebAssembly.DynamicCulture.Loader?color=ff4081&logo=nuget)](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture.Loader/)
[![Nuget](https://img.shields.io/nuget/dt/Blazor.WebAssembly.DynamicCulture.Loader?color=ff4081&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture.Loader/)
[![GitHub](https://img.shields.io/github/license/ScarletKuro/Blazor.WebAssembly.DynamicCulture?color=594ae2&logo=github)](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/blob/master/LICENSE)

Blazor WebAssembly loads the resource assemblies solely during the initial app startup. Consequently, if you wish to dynamically change the culture at a later point, you would need to reload the page in order to obtain the new resource assemblies. However, with the inclusion of this package, you gain the capability to load all localization satellite assemblies simultaneously during startup, eliminating the need to refresh the page in order to access the updated resource assembly.

## Getting Started
### Usage Example
Blazor WASM
```CSharp
	var host = builder.Build();
	await host.LoadSatelliteCultureAssembliesCultureAsync(new[] { new CultureInfo("ru"), new CultureInfo("et") });
	await host.RunAsync();
```
or
```CSharp
    await builder.Build().RunWithSatelliteCultureAssembliesAsync(new[] { new CultureInfo("ru"), new CultureInfo("et") });
```

**NB!** Do not use it for Blazor ServerSide.
