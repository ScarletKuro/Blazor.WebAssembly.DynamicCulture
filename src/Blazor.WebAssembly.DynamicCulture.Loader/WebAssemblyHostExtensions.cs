using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Blazor.WebAssembly.DynamicCulture.Loader;

public static class WebAssemblyHostExtensions
{
    public static async Task LoadDynamicCulturesAsync(this WebAssemblyHost host, IEnumerable<CultureInfo>? supportedCultures)
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

    public static  Task LoadDynamicCulturesAsync(this WebAssemblyHost host, ILocalizationDynamicList localizationDynamicList)
    {
        return LoadDynamicCulturesAsync(host, localizationDynamicList.GetAvailableCultures());
    }

    public static Task RunWithLoadedCulturesAsync(this WebAssemblyHost host, ILocalizationDynamicList localizationDynamicList)
    {
        return RunWithLoadedCulturesAsync(host, localizationDynamicList.GetAvailableCultures());
    }

    public static async Task RunWithLoadedCulturesAsync(this WebAssemblyHost host, IEnumerable<CultureInfo>? supportedCultures)
    {
        await LoadDynamicCulturesAsync(host, supportedCultures);
        await host.RunAsync();
    }
}