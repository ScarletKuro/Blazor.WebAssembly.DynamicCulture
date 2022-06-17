# Blazor.WebAssembly.DynamicCulture.Loader
[![Nuget](https://img.shields.io/nuget/v/Blazor.WebAssembly.DynamicCulture.Loader?color=ff4081&logo=nuget)](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture.Loader/)
[![Nuget](https://img.shields.io/nuget/dt/Blazor.WebAssembly.DynamicCulture.Loader?color=ff4081&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture.Loader/)
[![GitHub](https://img.shields.io/github/license/ScarletKuro/Blazor.WebAssembly.DynamicCulture?color=594ae2&logo=github)](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/blob/master/LICENSE)

Blazor WebAssembly only loads the resource assemblies during the initial app startup. So if you change the culture dynamically later, you do have to **reload** the page to get new resource assemblies. But this package adds the ability to load all localization satellite assemblies at once during startup and you do not need to refresh the page to get the new resouce assembly.

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