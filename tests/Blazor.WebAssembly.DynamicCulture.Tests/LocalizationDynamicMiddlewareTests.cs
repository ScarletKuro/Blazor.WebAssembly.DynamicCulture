using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
using Blazor.WebAssembly.DynamicCulture.Middleware;
using Blazor.WebAssembly.DynamicCulture.Provider;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class LocalizationDynamicMiddlewareTests : IDisposable
{
    private readonly CultureInfo? _originalCulture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _originalUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

    [Fact]
    public async Task Invoke_UsesFirstProviderWithSupportedCulture()
    {
        var options = Options(
            supportedCultures: ["en-US", "fr-FR"],
            supportedUiCultures: ["en-US", "fr-FR"],
            new TestCultureProvider(null),
            new TestCultureProvider(new ProviderCultureResult("fr-FR")),
            new TestCultureProvider(new ProviderCultureResult("en-US")));

        await Invoke(options);

        Assert.Equal("fr-FR", CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal("fr-FR", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Fact]
    public async Task Invoke_FallsBackToParentCulturesWhenEnabled()
    {
        var options = Options(
            supportedCultures: ["fr"],
            supportedUiCultures: ["fr"],
            new TestCultureProvider(new ProviderCultureResult("fr-FR")));

        await Invoke(options);

        Assert.Equal("fr", CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal("fr", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Fact]
    public async Task Invoke_UsesDefaultCultureWhenProvidersDoNotMatchSupportedCultures()
    {
        var options = Options(
            supportedCultures: ["en-US"],
            supportedUiCultures: ["en-US"],
            new TestCultureProvider(new ProviderCultureResult("fr-FR")));
        options.SetDefaultCulture("ja-JP");

        await Invoke(options);

        Assert.Equal("ja-JP", CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal("ja-JP", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Fact]
    public async Task Invoke_RespectsIgnoreCultureFlags()
    {
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

        var options = Options(
            supportedCultures: ["fr-FR"],
            supportedUiCultures: ["fr-FR"],
            new TestCultureProvider(new ProviderCultureResult("fr-FR")));
        options.IgnoreCulture = true;

        await Invoke(options);

        Assert.Equal("en-US", CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal("fr-FR", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    public void Dispose()
    {
        CultureInfo.DefaultThreadCurrentCulture = _originalCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _originalUiCulture;
    }

    private static LocalizationDynamicOptions Options(
        string[] supportedCultures,
        string[] supportedUiCultures,
        params ICultureProvider[] providers)
    {
        return new LocalizationDynamicOptions
        {
            SupportedCultures = [.. supportedCultures.Select(culture => new CultureInfo(culture))],
            SupportedUICultures = [.. supportedUiCultures.Select(culture => new CultureInfo(culture))],
            CultureProviders = providers
        };
    }

    private static Task Invoke(LocalizationDynamicOptions options)
    {
        var middleware = new LocalizationDynamicMiddleware(
            OptionsFactory.Create(options),
            NullLoggerFactory.Instance);

        return middleware.Invoke(CreateContext());
    }

    private static LocalizationContextManager CreateContext()
    {
        var jsRuntime = new ThrowingJsRuntime();
        return new LocalizationContextManager(
            new LocalizationLocalStorageManager(jsRuntime),
            new LocalizationQueryManager(jsRuntime),
            new LocalizationNavigatorManager(jsRuntime));
    }

    private sealed class TestCultureProvider(ProviderCultureResult? result) : ICultureProvider
    {
        public Task<ProviderCultureResult?> DetermineProviderCultureResult(LocalizationContextManager localizationContextManager)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            throw new InvalidOperationException("The middleware tests should not call JavaScript.");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            throw new InvalidOperationException("The middleware tests should not call JavaScript.");
        }
    }
}
