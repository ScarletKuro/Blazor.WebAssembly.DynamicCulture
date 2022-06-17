using System.Threading.Tasks;
using Blazor.WebAssembly.DynamicCulture.Internals;
using Blazor.WebAssembly.DynamicCulture.Loader;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
using Blazor.WebAssembly.DynamicCulture.Middleware;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Blazor.WebAssembly.DynamicCulture.Extensions;

public static class WebAssemblyHostExtensions
{
    public static Task SetDynamicCulturesAsync(this WebAssemblyHost host)
    {
        var options = host.Services.GetRequiredService<IOptions<LocalizationDynamicOptions>>().Value;
        var localizationDynamicList = new LocalizationDynamicList(options);

        if (options.LoadAllSatelliteCultureAssembliesAtOnce)
        {
            return host.LoadSatelliteCultureAssembliesCultureAsync(localizationDynamicList);
        }

        return Task.CompletedTask;
    }

    public static async Task SetMiddlewareCulturesAsync(this WebAssemblyHost host)
    {
        await host.SetDynamicCulturesAsync();
        var middleware = host.Services.GetRequiredService<LocalizationDynamicMiddleware>();
        var context = host.Services.GetRequiredService<LocalizationContextManager>();

        await middleware.Invoke(context);
    }

    public static async Task RunWithCultureMiddlewareAsync(this WebAssemblyHost host)
    {
        await host.SetDynamicCulturesAsync();
        await host.RunAsync();
    }

    public static async Task RunWithCulturesAsync(this WebAssemblyHost host)
    {
        await host.SetDynamicCulturesAsync();
        await host.RunAsync();
    }
}