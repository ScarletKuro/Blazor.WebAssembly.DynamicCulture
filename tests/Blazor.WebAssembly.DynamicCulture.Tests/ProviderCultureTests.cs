using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
using Blazor.WebAssembly.DynamicCulture.Provider;
using Microsoft.JSInterop;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class ProviderCultureTests
{
    [Fact]
    public async Task QueryStringCultureProvider_ReturnsCultureAndUICultureFromQueryString()
    {
        var module = new TestJsObjectReference()
            .SetResult("getQueryValue", ["culture"], "en-US")
            .SetResult("getQueryValue", ["ui-culture"], "ja-JP");
        var context = CreateContext(module);

        var result = await new QueryStringCultureProvider().DetermineProviderCultureResult(context);

        Assert.NotNull(result);
        var culture = Assert.Single(result.Cultures);
        var uiCulture = Assert.Single(result.UICultures);
        Assert.Equal("en-US", culture.Value);
        Assert.Equal("ja-JP", uiCulture.Value);
    }

    [Fact]
    public async Task QueryStringCultureProvider_UsesCultureForUICultureWhenUICultureIsMissing()
    {
        var module = new TestJsObjectReference()
            .SetResult("getQueryValue", ["culture"], "fr-FR");
        var context = CreateContext(module);

        var result = await new QueryStringCultureProvider().DetermineProviderCultureResult(context);

        Assert.NotNull(result);
        var culture = Assert.Single(result.Cultures);
        var uiCulture = Assert.Single(result.UICultures);
        Assert.Equal("fr-FR", culture.Value);
        Assert.Equal("fr-FR", uiCulture.Value);
    }

    [Fact]
    public async Task QueryStringCultureProvider_ReturnsNullWhenQueryValuesAreMissing()
    {
        var result = await new QueryStringCultureProvider().DetermineProviderCultureResult(CreateContext());

        Assert.Null(result);
    }

    [Fact]
    public async Task LocalStorageCultureProvider_ReturnsStoredBlazorCulture()
    {
        var module = new TestJsObjectReference()
            .SetResult("getBlazorCulture", [], "ru-RU");
        var context = CreateContext(module);

        var result = await new LocalStorageCultureProvider().DetermineProviderCultureResult(context);

        Assert.NotNull(result);
        var culture = Assert.Single(result.Cultures);
        var uiCulture = Assert.Single(result.UICultures);
        Assert.Equal("ru-RU", culture.Value);
        Assert.Equal("ru-RU", uiCulture.Value);
    }

    [Fact]
    public async Task LocalStorageCultureProvider_ReturnsNullWhenStoredCultureIsMissing()
    {
        var result = await new LocalStorageCultureProvider().DetermineProviderCultureResult(CreateContext());

        Assert.Null(result);
    }

    [Fact]
    public async Task AcceptLanguageHeaderCultureProvider_ReturnsConfiguredNumberOfNavigatorLanguages()
    {
        var module = new TestJsObjectReference()
            .SetResult("navigatorLanguages", [], new[] { "en-US", "ja-JP", "ru-RU" });
        var provider = new AcceptLanguageHeaderCultureProvider
        {
            MaximumAcceptLanguageHeaderValuesToTry = 2
        };

        var result = await provider.DetermineProviderCultureResult(CreateContext(module));

        Assert.NotNull(result);
        Assert.Equal(["ja-JP", "en-US"], result.Cultures.Select(culture => culture.Value));
    }

    [Fact]
    public async Task AcceptLanguageHeaderCultureProvider_ReturnsNullWhenNavigatorLanguagesAreMissing()
    {
        var module = new TestJsObjectReference()
            .SetResult<string[]?>("navigatorLanguages", [], null);

        var result = await new AcceptLanguageHeaderCultureProvider().DetermineProviderCultureResult(CreateContext(module));

        Assert.Null(result);
    }

    private static LocalizationContextManager CreateContext(TestJsObjectReference? module = null)
    {
        var jsRuntime = new TestJsRuntime(module ?? new TestJsObjectReference());

        return new LocalizationContextManager(
            new LocalizationLocalStorageManager(jsRuntime),
            new LocalizationQueryManager(jsRuntime),
            new LocalizationNavigatorManager(jsRuntime));
    }

    private sealed class TestJsRuntime(IJSObjectReference module) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import")
            {
                return ValueTask.FromResult((TValue)module);
            }

            throw new InvalidOperationException($"Unexpected JS runtime invocation: {identifier}.");
        }
    }

    private sealed class TestJsObjectReference : IJSObjectReference
    {
        private readonly Dictionary<InvocationKey, object?> _results = new();

        public TestJsObjectReference SetResult<TValue>(string identifier, object?[] args, TValue value)
        {
            _results[new InvocationKey(identifier, args)] = value;
            return this;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (_results.TryGetValue(new InvocationKey(identifier, args ?? []), out var result))
            {
                return ValueTask.FromResult((TValue?)result)!;
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        private readonly record struct InvocationKey(string Identifier, object?[] Args)
        {
            public bool Equals(InvocationKey other)
            {
                return Identifier == other.Identifier && Args.SequenceEqual(other.Args);
            }

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(Identifier);

                foreach (var arg in Args)
                {
                    hash.Add(arg);
                }

                return hash.ToHashCode();
            }
        }
    }
}
