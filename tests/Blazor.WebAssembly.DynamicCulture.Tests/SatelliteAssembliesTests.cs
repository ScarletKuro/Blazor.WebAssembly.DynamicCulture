using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.Loader.Interop;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class SatelliteAssembliesTests
{
    [Theory]
    [InlineData("fr-FR,fr-CA", "fr-FR,fr,fr-CA")]
    [InlineData("fr-FR,fr,fr-FR", "fr-FR,fr")]
    [InlineData("zh-Hant-TW,zh-Hant", "zh-Hant-TW,zh-Hant,zh")]
    [InlineData("en-US,cs-CZ", "en-US,en,cs-CZ,cs")]
    [InlineData("", "")]
    public void GetCultures_LoadsEachCultureAndParentOnce(string input, string expected)
    {
        var cultures = input.Split(',').Select(CultureInfo.GetCultureInfo);

        var result = SatelliteAssemblies.GetCultures(cultures);

        Assert.Equal(expected.Length == 0 ? [] : expected.Split(','), result);
    }

    [Fact]
    public void GetCultures_DoesNotExcludeTheCurrentCulture()
    {
        var result = SatelliteAssemblies.GetCultures([CultureInfo.CurrentCulture]);

        Assert.Equal(SatelliteAssemblies.GetCultures(CultureInfo.CurrentCulture), result);
    }
}
