using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.Internals;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class LocalizationDynamicListTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void GetAvailableCultures_HandlesMissingLists(bool hasCultures, bool hasUICultures)
    {
        var options = new LocalizationDynamicOptions
        {
            SupportedCultures = hasCultures ? [new CultureInfo("fr-FR")] : null,
            SupportedUICultures = hasUICultures ? [new CultureInfo("ja-JP")] : null
        };

        var cultures = new LocalizationDynamicList(options).GetAvailableCultures();

        Assert.NotNull(cultures);
        Assert.Equal(hasCultures ? ["fr-FR"] : hasUICultures ? ["ja-JP"] : Array.Empty<string>(),
            cultures.Select(culture => culture.Name));
    }

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
    public void GetAvailableCultures_ReturnsUICulturesWhenSupportedCulturesAreNull()
    {
        var options = new LocalizationDynamicOptions
        {
            SupportedCultures = null,
            SupportedUICultures = [new CultureInfo("ja-JP")]
        };

        var cultures = new LocalizationDynamicList(options).GetAvailableCultures();

        Assert.NotNull(cultures);
        Assert.Equal(["ja-JP"], cultures.Select(culture => culture.Name));
    }
}
