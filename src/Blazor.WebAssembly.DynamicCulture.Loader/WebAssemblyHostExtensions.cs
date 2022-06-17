using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Blazor.WebAssembly.DynamicCulture.Loader;

public static class WebAssemblyHostExtensions
{
    public static async Task LoadSatelliteCultureAssembliesCultureAsync(this WebAssemblyHost host, IEnumerable<CultureInfo>? supportedCultures)
    {
        ArgumentNullException.ThrowIfNull(host);
        WebAssemblyCultureProvider.Initialize();
        var cultureProvider = WebAssemblyCultureProvider.Instance!;
        cultureProvider.ThrowIfCultureChangeIsUnsupported();
        if (supportedCultures is not null)
        {
            await cultureProvider.LoadCurrentCultureResourcesAsync(supportedCultures);
        }
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