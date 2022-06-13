using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Blazor.WebAssembly.DynamicCulture.Middleware;

public class LocalizationDynamicMiddleware
{
    private const int MaxCultureFallbackDepth = 5;

    private readonly LocalizationDynamicOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new <see cref="LocalizationDynamicMiddleware"/>.
    /// </summary>
    /// <param name="options">The <see cref="LocalizationDynamicMiddleware"/> representing the options for the
    /// <see cref="LocalizationDynamicMiddleware"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
    public LocalizationDynamicMiddleware(IOptions<LocalizationDynamicOptions> options, ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        _logger = loggerFactory?.CreateLogger<LocalizationDynamicMiddleware>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        _options = options.Value;
    }

    /// <summary>
    /// Invokes the logic of the middleware.
    /// </summary>
    /// <param name="context">The <see cref="LocalizationContextManager"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the middleware has completed processing.</returns>
    public async Task Invoke(LocalizationContextManager context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestCulture = _options.DefaultCulture;

        if (_options.CultureProviders is not null)
        {
            foreach (var provider in _options.CultureProviders)
            {
                var providerResultCulture = await provider.DetermineProviderCultureResult(context);
                if (providerResultCulture is null)
                {
                    continue;
                }
                var cultures = providerResultCulture.Cultures;
                var uiCultures = providerResultCulture.UICultures;

                CultureInfo? cultureInfo = null;
                CultureInfo? uiCultureInfo = null;
                if (_options.SupportedCultures is not null)
                {
                    cultureInfo = GetCultureInfo(cultures, _options.SupportedCultures, _options.FallBackToParentCultures);

                    if (cultureInfo == null)
                    {
                        _logger.UnsupportedCultures(provider.GetType().Name, cultures);
                    }
                }

                if (_options.SupportedUICultures is not null)
                {
                    uiCultureInfo = GetCultureInfo(
                        uiCultures,
                        _options.SupportedUICultures,
                        _options.FallBackToParentUICultures);

                    if (uiCultureInfo is null)
                    {
                        _logger.UnsupportedUICultures(provider.GetType().Name, uiCultures);
                    }
                }

                if (cultureInfo is null && uiCultureInfo is null)
                {
                    continue;
                }

                cultureInfo ??= _options.DefaultCulture.Culture;
                uiCultureInfo ??= _options.DefaultCulture.UICulture;

                var result = new DynamicCulture(cultureInfo, uiCultureInfo);
                requestCulture = result;
                break;
            }
        }

        SetCurrentThreadCulture(requestCulture, _options);
    }

    private static void SetCurrentThreadCulture(DynamicCulture requestCulture, LocalizationDynamicOptions options)
    {
        if (!options.IgnoreCulture)
        {
            CultureInfo.DefaultThreadCurrentCulture = requestCulture.Culture;
        }

        if (!options.IgnoreUICulture)
        {
            CultureInfo.DefaultThreadCurrentUICulture = requestCulture.UICulture;
        }
    }

    private static CultureInfo? GetCultureInfo(
        IList<StringSegment> cultureNames,
        IList<CultureInfo> supportedCultures,
        bool fallbackToParentCultures)
    {
        foreach (StringSegment cultureName in cultureNames)
        {
            // Allow empty string values as they map to InvariantCulture, whereas null culture values will throw in
            // the CultureInfo ctor
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (cultureName != null)
            {
                CultureInfo? cultureInfo = GetCultureInfo(cultureName, supportedCultures, fallbackToParentCultures, currentDepth: 0);
                if (cultureInfo is not null)
                {
                    return cultureInfo;
                }
            }
        }

        return null;
    }

    private static CultureInfo? GetCultureInfo(StringSegment name, IList<CultureInfo>? supportedCultures)
    {
        // Allow only known culture names as this API is called with input from users (HTTP requests) and
        // creating CultureInfo objects is expensive and we don't want it to throw either.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (name == null || supportedCultures is null)
        {
            return null;
        }
        var culture = supportedCultures.FirstOrDefault(supportedCulture => StringSegment.Equals(supportedCulture.Name, name, StringComparison.OrdinalIgnoreCase));

        if (culture is null)
        {
            return null;
        }

        return CultureInfo.ReadOnly(culture);
    }

    private static CultureInfo? GetCultureInfo(
        StringSegment cultureName,
        IList<CultureInfo> supportedCultures,
        bool fallbackToParentCultures,
        int currentDepth)
    {
        CultureInfo? culture = GetCultureInfo(cultureName, supportedCultures);

        if (culture is null && fallbackToParentCultures && currentDepth < MaxCultureFallbackDepth)
        {
            var lastIndexOfHyphen = cultureName.LastIndexOf('-');

            if (lastIndexOfHyphen > 0)
            {
                // Trim the trailing section from the culture name, e.g. "fr-FR" becomes "fr"
                var parentCultureName = cultureName.Subsegment(0, lastIndexOfHyphen);

                culture = GetCultureInfo(parentCultureName, supportedCultures, fallbackToParentCultures, currentDepth + 1);
            }
        }

        return culture;
    }
}