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
    public void AddLocalizationDynamic_ConfiguresOptionsWithRegisteredDependency()
    {
        var services = new ServiceCollection();
        var settings = new CultureSettings("ja-JP");
        services.AddSingleton(settings);

        var result = services.AddLocalizationDynamic<CultureSettings>((options, dependency) =>
            options.SetDefaultCulture(dependency.Culture));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LocalizationDynamicOptions>>().Value;
        Assert.Same(services, result);
        Assert.Equal("ja-JP", options.DefaultCulture.Culture.Name);
    }

    [Fact]
    public async Task AddLocalizationDynamic_UsesScopedLifetimesAndDoesNotDuplicateRegistrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime, TestJsRuntime>();
        services.AddLogging();
        services.AddLocalizationDynamic();
        services.AddLocalizationDynamic();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var first = provider.CreateAsyncScope();
        await using var second = provider.CreateAsyncScope();

        foreach (var serviceType in new[]
        {
            typeof(ILocalizationService), typeof(LocalizationLocalStorageManager), typeof(LocalizationNavigatorManager),
            typeof(LocalizationQueryManager), typeof(LocalizationContextManager), typeof(LocalizationDynamicMiddleware)
        })
        {
            var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == serviceType);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
            var instance = first.ServiceProvider.GetRequiredService(serviceType);
            Assert.Same(instance, first.ServiceProvider.GetRequiredService(serviceType));
            Assert.NotSame(instance, second.ServiceProvider.GetRequiredService(serviceType));
        }
    }

    [Fact]
    public void AddLocalizationDynamic_PreservesExistingServiceRegistration()
    {
        var services = new ServiceCollection();
        var existing = new LocalizationService();
        services.AddSingleton<ILocalizationService>(existing);

        services.AddLocalizationDynamic();

        using var provider = services.BuildServiceProvider();
        Assert.Same(existing, provider.GetRequiredService<ILocalizationService>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ILocalizationService));
    }

    [Fact]
    public void AddLocalizationDynamic_RejectsNullServicesAcrossOverloads()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>("services", () => services.AddLocalizationDynamic());
        Assert.Throws<ArgumentNullException>("services", () => services.AddLocalizationDynamic(_ => { }));
        Assert.Throws<ArgumentNullException>("services", () => services.AddLocalizationDynamic<CultureSettings>((_, _) => { }));
    }

    [Fact]
    public void AddLocalizationDynamic_RejectsNullConfiguration()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>("configureOptions", () => services.AddLocalizationDynamic((Action<LocalizationDynamicOptions>)null!));
        Assert.Throws<ArgumentNullException>("configureOptions", () => services.AddLocalizationDynamic<CultureSettings>(null!));
    }

    private sealed record CultureSettings(string Culture);

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
