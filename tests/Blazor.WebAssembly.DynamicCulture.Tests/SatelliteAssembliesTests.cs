using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.Loader.Interop;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class SatelliteAssembliesTests
{
    [Fact]
    public async Task FailedLoad_ReleasesLockAndDoesNotCacheTheFailedCulture()
    {
        var loader = new SatelliteAssemblies();
        var cultures = new[] { CultureInfo.GetCultureInfo("fr-FR") };

        // A real desktop platform failure exercises the managed load coordinator
        // without mocking the browser guard or invoking unsupported JS interop.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await Assert.ThrowsAsync<PlatformNotSupportedException>(() =>
                loader.LoadCurrentCultureResourcesAsync(cultures).AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
        }

        await loader.LoadCurrentCultureResourcesAsync(Array.Empty<CultureInfo>()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }

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
