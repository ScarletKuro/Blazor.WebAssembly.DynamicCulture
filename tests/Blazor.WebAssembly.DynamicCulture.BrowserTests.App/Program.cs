using System.Globalization;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Versioning;
using Blazor.WebAssembly.DynamicCulture.Extensions;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
using Blazor.WebAssembly.DynamicCulture.Loader;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using DynamicCultureBrowserTests;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddLocalizationDynamic();
var host = builder.Build();
var js = host.Services.GetRequiredService<IJSRuntime>();
BrowserTestApi.Host = host;
try
{
    var options = await js.InvokeAsync<BrowserTestOptions>("browserTests.getOptions");
    var cultures = options.Cultures.Select(CultureInfo.GetCultureInfo).ToArray();
    var settings = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Blazor.WebAssembly.DynamicCulture.LocalizationDynamicOptions>>().Value;
    settings.SetDefaultCulture(options.SelectedCulture);
    settings.AddSupportedCultures(options.Cultures);
    settings.AddSupportedUICultures(options.Cultures);
    settings.IgnoreCulture = options.Mode == "ui-only";
    settings.LoadAllSatelliteCultureAssembliesAtOnce = !options.Native;
    if (options.Mode != "stored")
    {
        settings.CultureProviders = [];
    }

    switch (options.Mode)
    {
        case "none":
        case "run-loader":
        case "run-loader-list":
        case "run-dynamic":
        case "run-middleware":
            break;
        case "middleware":
        case "ui-only":
        case "stored":
            await host.SetMiddlewareCulturesAsync();
            break;
        case "concurrent":
            await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => host.LoadSatelliteCultureAssembliesCultureAsync(cultures)));
            break;
        case "repeated":
            await host.LoadSatelliteCultureAssembliesCultureAsync(cultures);
            await host.LoadSatelliteCultureAssembliesCultureAsync(cultures);
            break;
        case "changed-after":
            await host.LoadSatelliteCultureAssembliesCultureAsync(cultures);
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(options.SelectedCulture);
            break;
        case "changed-before":
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(options.SelectedCulture);
            await host.LoadSatelliteCultureAssembliesCultureAsync(cultures);
            break;
        case "retry":
            try
            {
                await host.LoadSatelliteCultureAssembliesCultureAsync(cultures);
                throw new InvalidOperationException("The injected download failure was not observed.");
            }
            catch (Exception error) when (error is not InvalidOperationException)
            {
                await js.InvokeVoidAsync("browserTests.retryObserved");
            }
            await host.LoadSatelliteCultureAssembliesCultureAsync(cultures);
            break;
        default:
            await host.LoadSatelliteCultureAssembliesCultureAsync(cultures);
            break;
    }

    // Verify that the public loader has completed the requested loads before RunAsync.
    var resources = options.Mode == "none" || options.Mode.StartsWith("run-") || options.Native
        ? new Dictionary<string, string?>()
        : options.Cultures.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(culture => culture, BrowserTestApi.Resource);
    var frameworks = new
    {
        Loader = typeof(Blazor.WebAssembly.DynamicCulture.Loader.WebAssemblyHostExtensions).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName,
        Main = typeof(Blazor.WebAssembly.DynamicCulture.Extensions.WebAssemblyHostExtensions).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName
    };
    await js.InvokeVoidAsync("browserTests.beforeRun", resources, RuntimeInformation.FrameworkDescription, frameworks);
    switch (options.Mode)
    {
        case "run-loader":
            await host.RunWithSatelliteCultureAssembliesAsync(cultures);
            break;
        case "run-loader-list":
            await host.RunWithSatelliteCultureAssembliesAsync(new BrowserCultureList(cultures));
            break;
        case "run-dynamic":
            await host.RunWithCulturesAsync();
            break;
        case "run-middleware":
            await host.RunWithCultureMiddlewareAsync();
            break;
        default:
            await host.RunAsync();
            break;
    }
}
catch (Exception error)
{
    await js.InvokeVoidAsync("browserTests.fail", error.ToString());
}

public sealed class BrowserTestOptions
{
    public string Mode { get; set; } = "loader";
    public string[] Cultures { get; set; } = ["en-US", "cs-CZ"];
    public string SelectedCulture { get; set; } = "cs-CZ";
    public bool Native { get; set; }
}

public sealed class BrowserCultureList(IEnumerable<CultureInfo> cultures) : ILocalizationDynamicList
{
    public IEnumerable<CultureInfo> GetAvailableCultures() => cultures;
}

public static class BrowserTestApi
{
    public static WebAssemblyHost Host { get; set; } = null!;
    private static readonly System.Resources.ResourceManager Resources = new("DynamicCultureBrowserTests.Strings", typeof(BrowserTestApi).Assembly);

    // Coverlet instruments copies in this fixture's bin directory. Export the actual
    // browser counters to its host process; WASM cannot write the host's hit files.
    [JSInvokable]
    public static object[] GetCoverage()
    {
        return new[]
        {
            typeof(Blazor.WebAssembly.DynamicCulture.Loader.WebAssemblyHostExtensions).Assembly,
            typeof(Blazor.WebAssembly.DynamicCulture.Extensions.WebAssemblyHostExtensions).Assembly
        }.Select(assembly =>
        {
            var tracker = assembly.GetTypes().Single(type => type.FullName!.StartsWith("Coverlet.Core.Instrumentation.Tracker."));
            return (object)new
            {
                Name = assembly.GetName().Name,
                HitsFilePath = (string)tracker.GetField("HitsFilePath")!.GetValue(null)!,
                Hits = (int[])tracker.GetField("HitsArray")!.GetValue(null)!
            };
        }).ToArray();
    }

    [JSInvokable]
    public static string? Resource(string culture) => Resources.GetString("Greeting", CultureInfo.GetCultureInfo(culture));

    [JSInvokable]
    public static async Task<string?> SelectAndLoad(string culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(culture);
        await Host.LoadSatelliteCultureAssembliesCultureAsync(new[] { CultureInfo.GetCultureInfo(culture) });
        return Resource(culture);
    }

    [JSInvokable]
    public static async Task ValidateEmptyInputs()
    {
        await Host.LoadSatelliteCultureAssembliesCultureAsync((IEnumerable<CultureInfo>?)null);
        await Host.LoadSatelliteCultureAssembliesCultureAsync(Array.Empty<CultureInfo>());
        await Host.LoadSatelliteCultureAssembliesCultureAsync(new[] { CultureInfo.InvariantCulture });
        foreach (var run in new[] { false, true })
        {
            try
            {
                if (run) await Host.RunWithSatelliteCultureAssembliesAsync((ILocalizationDynamicList)null!);
                else await Host.LoadSatelliteCultureAssembliesCultureAsync((ILocalizationDynamicList)null!);
                throw new InvalidOperationException("A null culture list was accepted.");
            }
            catch (ArgumentNullException error) when (error.ParamName == "localizationDynamicList")
            {
            }
        }
    }
}
