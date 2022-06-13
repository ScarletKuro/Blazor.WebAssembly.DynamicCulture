using System;
using System.Linq;
using System.Threading.Tasks;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
using Microsoft.Extensions.Primitives;

namespace Blazor.WebAssembly.DynamicCulture.Provider;

/// <summary>
/// Determines the culture information for a request via the value of the Accept-Language header.
/// </summary>
public class AcceptLanguageHeaderCultureProvider : CultureProvider
{
    /// <summary>
    /// The maximum number of values in the Accept-Language header to attempt to create a <see cref="System.Globalization.CultureInfo"/>
    /// from for the current request.
    /// Defaults to <c>3</c>.
    /// </summary>
    public int MaximumAcceptLanguageHeaderValuesToTry { get; set; } = 3;

    /// <inheritdoc />
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(LocalizationContextManager localizationContextManager)
    {
        ArgumentNullException.ThrowIfNull(localizationContextManager);

        var acceptLanguageHeader = await localizationContextManager.Navigator.GetLanguagesAsync();

        if (acceptLanguageHeader is null || acceptLanguageHeader.Length == 0)
        {
            return await NullProviderCultureResult;
        }

        var languages = acceptLanguageHeader.AsEnumerable();

        if (MaximumAcceptLanguageHeaderValuesToTry > 0)
        {
            // We take only the first configured number of languages from the header and then order those that we
            // attempt to parse as a CultureInfo to mitigate potentially spinning CPU on lots of parse attempts.
            languages = languages.Take(MaximumAcceptLanguageHeaderValuesToTry);
        }

        var orderedLanguages = languages.OrderByDescending(h => h).Select(x => new StringSegment(x)).ToList();

        if (orderedLanguages.Count > 0)
        {
            return new ProviderCultureResult(orderedLanguages);
        }

        return await NullProviderCultureResult;
    }
}