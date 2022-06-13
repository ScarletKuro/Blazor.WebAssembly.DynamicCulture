using System.Collections.Generic;
using System.Globalization;

namespace Blazor.WebAssembly.DynamicCulture.Loader;

public interface ILocalizationDynamicList
{
    IEnumerable<CultureInfo>? GetAvailableCultures();
}