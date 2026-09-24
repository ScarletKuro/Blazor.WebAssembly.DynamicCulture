using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.Internals;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class LocalizationDynamicListTests
{
    [Fact]
    public void GetAvailableCultures_ReturnsSupportedCulturesAndSupportedUICulturesWithoutDuplicates()
    {
        var options = new LocalizationDynamicOptions
        {
            SupportedCultures = [new CultureInfo("en-US"), new CultureInfo("fr-FR")],
            SupportedUICultures = [new CultureInfo("fr-FR"), new CultureInfo("ja-JP")]
        };

        var availableCultures = new LocalizationDynamicList(options)
            .GetAvailableCultures();

        Assert.NotNull(availableCultures);

        var cultures = availableCultures!
            .Select(culture => culture.Name);

        Assert.Equal(["en-US", "fr-FR", "ja-JP"], cultures);
    }

    [Fact]
    public void GetAvailableCultures_ReturnsNullWhenSupportedCulturesAreNull()
    {
        var options = new LocalizationDynamicOptions
        {
            SupportedCultures = null,
            SupportedUICultures = [new CultureInfo("ja-JP")]
        };

        var cultures = new LocalizationDynamicList(options).GetAvailableCultures();

        Assert.Null(cultures);
    }
}
