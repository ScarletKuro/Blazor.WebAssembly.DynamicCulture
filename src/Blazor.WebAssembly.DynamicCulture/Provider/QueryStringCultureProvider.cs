using System;
using System.Threading.Tasks;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
// ReSharper disable InconsistentNaming

namespace Blazor.WebAssembly.DynamicCulture.Provider;

/// <summary>
/// Determines the culture information for a request via values in the query string.
/// </summary>
public class QueryStringCultureProvider : CultureProvider
{
    /// <summary>
    /// The key that contains the culture name.
    /// Defaults to "culture".
    /// </summary>
    public string QueryStringKey { get; set; } = "culture";

    /// <summary>
    /// The key that contains the UI culture name. If not specified or no value is found,
    /// <see cref="QueryStringKey"/> will be used.
    /// Defaults to "ui-culture".
    /// </summary>
    public string UIQueryStringKey { get; set; } = "ui-culture";

    /// <inheritdoc />
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(LocalizationContextManager localizationContextManager)
    {
        ArgumentNullException.ThrowIfNull(localizationContextManager);

        var query = localizationContextManager.Query;

        string? queryCulture = null;
        string? queryUICulture = null;

        if (!string.IsNullOrWhiteSpace(QueryStringKey))
        {
            queryCulture = await query.GetValueAsync(QueryStringKey);
        }

        if (!string.IsNullOrWhiteSpace(UIQueryStringKey))
        {
            queryUICulture = await query.GetValueAsync(UIQueryStringKey);
        }

        if (queryCulture is null && queryUICulture is null)
        {
            // No values specified for either so no match
            return await NullProviderCultureResult;
        }

        if (queryCulture is not null && queryUICulture is null)
        {
            // Value for culture but not for UI culture so default to culture value for both
            queryUICulture = queryCulture;
        }
        else if (queryCulture is null && queryUICulture is not null)
        {
            // Value for UI culture but not for culture so default to UI culture value for both
            queryCulture = queryUICulture;
        }

        var providerResultCulture = new ProviderCultureResult(queryCulture, queryUICulture);

        return providerResultCulture;
    }
}