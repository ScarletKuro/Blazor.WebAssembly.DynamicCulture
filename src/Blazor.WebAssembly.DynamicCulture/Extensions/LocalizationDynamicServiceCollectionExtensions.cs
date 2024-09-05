using System;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
using Blazor.WebAssembly.DynamicCulture.Middleware;
using Blazor.WebAssembly.DynamicCulture.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blazor.WebAssembly.DynamicCulture.Extensions;

public static class LocalizationDynamicServiceCollectionExtensions
{
    /// <summary>
    /// Adds services and options for the localization.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddLocalizationDynamic(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ILocalizationService, LocalizationService>();
        services.TryAddScoped<LocalizationLocalStorageManager>();
        services.TryAddScoped<LocalizationNavigatorManager>();
        services.TryAddScoped<LocalizationQueryManager>();
        services.TryAddScoped<LocalizationContextManager>();
        services.TryAddScoped<LocalizationDynamicMiddleware>();

        return services;
    }

    /// <summary>
    /// Adds services and options for the localization.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="LocalizationDynamicOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddLocalizationDynamic(this IServiceCollection services, Action<LocalizationDynamicOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddLocalizationDynamic();

        return services.Configure(configureOptions);
    }

    /// <summary>
    /// Adds services and options for the localization.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="LocalizationDynamicOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddLocalizationDynamic<TService>(this IServiceCollection services, Action<LocalizationDynamicOptions, TService> configureOptions) where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddLocalizationDynamic();

        services.AddOptions<LocalizationDynamicOptions>().Configure(configureOptions);

        return services;
    }
}