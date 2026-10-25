using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Blazor.WebAssembly.DynamicCulture.Loader.Interop;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#if !NET8_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
#endif

namespace Blazor.WebAssembly.DynamicCulture.Loader;

public static class WebAssemblyHostExtensions
{
    public static async Task LoadSatelliteCultureAssembliesCultureAsync(this WebAssemblyHost host, IEnumerable<CultureInfo>? supportedCultures)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (supportedCultures is null)
        {
            return;
        }

        if (!OperatingSystem.IsBrowser())
        {
            throw new PlatformNotSupportedException("This method is only supported in the browser.");
        }

#if NET8_0_OR_GREATER
        var satelliteAssemblies = new SatelliteAssemblies();
#else
        var satelliteAssemblies = new SatelliteAssemblies((IJSUnmarshalledRuntime)host.Services.GetRequiredService<IJSRuntime>());
#endif
        await satelliteAssemblies.LoadCurrentCultureResourcesAsync(supportedCultures);
    }

    public static Task LoadSatelliteCultureAssembliesCultureAsync(this WebAssemblyHost host, ILocalizationDynamicList localizationDynamicList)
    {
        return LoadSatelliteCultureAssembliesCultureAsync(host, localizationDynamicList.GetAvailableCultures());
    }

    public static Task RunWithSatelliteCultureAssembliesAsync(this WebAssemblyHost host, ILocalizationDynamicList localizationDynamicList)
    {
        return RunWithSatelliteCultureAssembliesAsync(host, localizationDynamicList.GetAvailableCultures());
    }

    public static async Task RunWithSatelliteCultureAssembliesAsync(this WebAssemblyHost host, IEnumerable<CultureInfo>? supportedCultures)
    {
        await LoadSatelliteCultureAssembliesCultureAsync(host, supportedCultures);
        await host.RunAsync();
    }
}
