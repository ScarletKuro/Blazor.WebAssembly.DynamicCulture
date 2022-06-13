using System;
using System.Threading.Tasks;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;

namespace Blazor.WebAssembly.DynamicCulture.Provider;

public class LocalStorageCultureProvider : CultureProvider
{
    /// <inheritdoc />
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(LocalizationContextManager localizationContextManager)
    {
        ArgumentNullException.ThrowIfNull(localizationContextManager);

        var localStorage = localizationContextManager.LocalStorage;

        string? currentCulture = await localStorage.GetBlazorCultureAsync();

        if (currentCulture is null)
        {
            // No values specified for either so no match
            return await NullProviderCultureResult;
        }

        var providerResultCulture = new ProviderCultureResult(currentCulture);

        return providerResultCulture;
    }
}