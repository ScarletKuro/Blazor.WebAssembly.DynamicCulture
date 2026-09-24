using Blazor.WebAssembly.DynamicCulture.Extensions;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
using Blazor.WebAssembly.DynamicCulture.Middleware;
using Blazor.WebAssembly.DynamicCulture.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class LocalizationDynamicServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddLocalizationDynamic_RegistersCoreServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IJSRuntime, TestJsRuntime>();
        services.AddLogging();
        services.AddLocalizationDynamic();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        Assert.IsType<LocalizationService>(scope.ServiceProvider.GetRequiredService<ILocalizationService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<LocalizationLocalStorageManager>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<LocalizationNavigatorManager>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<LocalizationQueryManager>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<LocalizationContextManager>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<LocalizationDynamicMiddleware>());
    }

    [Fact]
    public void AddLocalizationDynamic_WithConfigureOptions_AppliesOptions()
    {
        var services = new ServiceCollection();

        services.AddLocalizationDynamic(options => options.SetDefaultCulture("ru-RU"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LocalizationDynamicOptions>>().Value;

        Assert.Equal("ru-RU", options.DefaultCulture.Culture.Name);
        Assert.Equal("ru-RU", options.DefaultCulture.UICulture.Name);
    }

    private sealed class TestJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            throw new InvalidOperationException("This test only verifies service registration.");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            throw new InvalidOperationException("This test only verifies service registration.");
        }
    }
}
