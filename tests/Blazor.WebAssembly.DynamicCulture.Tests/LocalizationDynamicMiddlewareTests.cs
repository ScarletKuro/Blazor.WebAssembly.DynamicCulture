using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
using Blazor.WebAssembly.DynamicCulture.Middleware;
using Blazor.WebAssembly.DynamicCulture.Provider;
using Bunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Invoke_RespectsIgnoreCultureFlags(bool ignoreCulture, bool ignoreUICulture)
    {
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

        var options = Options(
            supportedCultures: ["fr-FR"],
            supportedUiCultures: ["fr-FR"],
            new TestCultureProvider(new ProviderCultureResult("fr-FR")));
        options.IgnoreCulture = ignoreCulture;
        options.IgnoreUICulture = ignoreUICulture;

        await Invoke(options);

        Assert.Equal(ignoreCulture ? "en-US" : "fr-FR", CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal(ignoreUICulture ? "en-US" : "fr-FR", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Theory]
    [InlineData("ja-JP", "fr-FR", "ja-JP")]
    [InlineData(null, "fr-FR", "fr-FR")]
    [InlineData(null, null, "en-US")]
    [InlineData("unsupported", "unsupported", "en-US")]
    public async Task Invoke_CombinesRealProvidersInQueryStorageNavigatorOrder(string? query, string? storage, string expected)
    {
        await using var js = new BunitContext();
        var queryModule = js.JSInterop.SetupModule("./_content/Blazor.WebAssembly.DynamicCulture/QueryManager.js");
        queryModule.Setup<string?>("getQueryValue", "culture").SetResult(query);
        queryModule.Setup<string?>("getQueryValue", "ui-culture").SetResult(null);
        var storageModule = js.JSInterop.SetupModule("./_content/Blazor.WebAssembly.DynamicCulture/LocalStorageManager.js");
        storageModule.Setup<string?>("getBlazorCulture").SetResult(storage);
        var navigatorModule = js.JSInterop.SetupModule("./_content/Blazor.WebAssembly.DynamicCulture/NavigatorManager.js");
        navigatorModule.Setup<string[]?>("navigatorLanguages").SetResult(["en-US", "ja-JP"]);
        var options = new LocalizationDynamicOptions()
            .AddSupportedCultures("en-US", "fr-FR", "ja-JP")
            .AddSupportedUICultures("en-US", "fr-FR", "ja-JP")
            .SetDefaultCulture("ru-RU");
        var runtime = js.JSInterop.JSRuntime;
        await using var localStorage = new LocalizationLocalStorageManager(runtime);
        await using var queryManager = new LocalizationQueryManager(runtime);
        await using var navigator = new LocalizationNavigatorManager(runtime);

        await Invoke(options, new LocalizationContextManager(localStorage, queryManager, navigator));

        Assert.Equal(expected, CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal(expected, CultureInfo.DefaultThreadCurrentUICulture?.Name);
        if (expected == "ja-JP")
        {
            storageModule.VerifyNotInvoke("getBlazorCulture");
            navigatorModule.VerifyNotInvoke("navigatorLanguages");
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Invoke_ConfiguresParentFallbackForFormattingAndUIIndependently(bool fallbackCulture, bool fallbackUICulture)
    {
        var options = Options(["fr"], ["ja"], new TestCultureProvider(new ProviderCultureResult("fr-CA", "ja-JP")));
        options.DefaultCulture = new DynamicCulture("en-US", "ru-RU");
        options.FallBackToParentCultures = fallbackCulture;
        options.FallBackToParentUICultures = fallbackUICulture;

        await Invoke(options);

        Assert.Equal(fallbackCulture ? "fr" : "en-US", CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal(fallbackUICulture ? "ja" : "ru-RU", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Invoke_UsesDefaultForMissingSupportedCultureList(bool missingCultureList)
    {
        var options = Options(["fr-FR"], ["ja-JP"], new TestCultureProvider(new ProviderCultureResult("fr-FR", "ja-JP")));
        options.DefaultCulture = new DynamicCulture("en-US", "ru-RU");
        if (missingCultureList)
        {
            options.SupportedCultures = null;
        }
        else
        {
            options.SupportedUICultures = null;
        }

        await Invoke(options);

        Assert.Equal(missingCultureList ? "en-US" : "fr-FR", CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal(missingCultureList ? "ja-JP" : "ru-RU", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Fact]
    public async Task Invoke_UsesDefaultWhenProviderListIsNull()
    {
        var options = new LocalizationDynamicOptions { CultureProviders = null };
        options.DefaultCulture = new DynamicCulture("en-US", "ja-JP");

        await Invoke(options);

        Assert.Equal("en-US", CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal("ja-JP", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Fact]
    public async Task Invoke_MatchesSupportedCultureCaseInsensitivelyAndPreservesCustomFormatting()
    {
        var supported = new CultureInfo("fr-FR");
        supported.NumberFormat.CurrencySymbol = "custom";
        var options = Options(["fr-FR"], ["fr-FR"], new TestCultureProvider(new ProviderCultureResult("FR-fr")));
        options.SupportedCultures = [supported];

        await Invoke(options);

        Assert.NotNull(CultureInfo.DefaultThreadCurrentCulture);
        Assert.Equal("custom", CultureInfo.DefaultThreadCurrentCulture.NumberFormat.CurrencySymbol);
        Assert.True(CultureInfo.DefaultThreadCurrentCulture.IsReadOnly);
        Assert.Equal("fr-FR", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("fr-FR", "fr-FR")]
    public async Task Invoke_SkipsNullAndUnsupportedCandidatesAndSupportsInvariantCulture(string candidate, string expected)
    {
        var result = new ProviderCultureResult(new List<StringSegment> { default, "not-a-supported-culture", candidate });
        var options = Options(["", "fr-FR"], ["", "fr-FR"], new TestCultureProvider(result));

        await Invoke(options);

        Assert.Equal(expected, CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal(expected, CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Fact]
    public async Task Invoke_RejectsNullContext()
    {
        var middleware = new LocalizationDynamicMiddleware(OptionsFactory.Create(new LocalizationDynamicOptions()), NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>("context", () => middleware.Invoke(null!));
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

    private static Task Invoke(LocalizationDynamicOptions options, LocalizationContextManager? context = null)
    {
        var middleware = new LocalizationDynamicMiddleware(
            OptionsFactory.Create(options),
            NullLoggerFactory.Instance);

        return middleware.Invoke(context ?? CreateContext());
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
