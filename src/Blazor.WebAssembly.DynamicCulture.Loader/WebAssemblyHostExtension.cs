using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Blazor.WebAssembly.DynamicCulture.Loader;

public static class WebAssemblyHostExtension
{
    public static async Task SetDynamicCultures(this WebAssemblyHost host, CultureInfo[] supportedCultures)
    {
        WebAssemblyCultureProvider.Initialize();
        var cultureProvider = WebAssemblyCultureProvider.Instance!;
        cultureProvider.ThrowIfCultureChangeIsUnsupported();
        foreach (var culture in supportedCultures)
        {
            await cultureProvider.LoadCurrentCultureResourcesAsync(culture);
        }
    }
}