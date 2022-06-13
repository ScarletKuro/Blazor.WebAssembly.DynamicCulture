using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Blazor.WebAssembly.DynamicCulture.Loader;

namespace Blazor.WebAssembly.DynamicCulture.Internals;

internal class LocalizationDynamicList : ILocalizationDynamicList
{
    private readonly LocalizationDynamicOptions _localizationDynamicOptions;

    public LocalizationDynamicList(LocalizationDynamicOptions localizationDynamicOptions)
    {
        _localizationDynamicOptions = localizationDynamicOptions;
    }

    public IEnumerable<CultureInfo>? GetAvailableCultures()
    {
        //Merging lists with no duplicates to make sure to load all supported culture resources just to be sure.
        var supportedCultures = _localizationDynamicOptions.SupportedCultures?.Union(_localizationDynamicOptions.SupportedUICultures ?? Enumerable.Empty<CultureInfo>());

        return supportedCultures;
    }
}