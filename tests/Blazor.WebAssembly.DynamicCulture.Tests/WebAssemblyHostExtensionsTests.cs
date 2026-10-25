using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.Extensions;
using Blazor.WebAssembly.DynamicCulture.Loader;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class WebAssemblyHostExtensionsTests
{
    [Fact]
    public async Task AllHostExtensions_RejectNullHost()
    {
        WebAssemblyHost host = null!;
        var list = new UnusedCultureList();
        await Assert.ThrowsAsync<ArgumentNullException>("host", () => host.LoadSatelliteCultureAssembliesCultureAsync((IEnumerable<CultureInfo>?)null));
        await Assert.ThrowsAsync<ArgumentNullException>("host", () => host.LoadSatelliteCultureAssembliesCultureAsync(list));
        await Assert.ThrowsAsync<ArgumentNullException>("host", () => host.RunWithSatelliteCultureAssembliesAsync((IEnumerable<CultureInfo>?)null));
        await Assert.ThrowsAsync<ArgumentNullException>("host", () => host.RunWithSatelliteCultureAssembliesAsync(list));
        await Assert.ThrowsAsync<ArgumentNullException>("host", () => host.SetDynamicCulturesAsync());
        await Assert.ThrowsAsync<ArgumentNullException>("host", () => host.SetMiddlewareCulturesAsync());
        await Assert.ThrowsAsync<ArgumentNullException>("host", () => host.RunWithCulturesAsync());
        await Assert.ThrowsAsync<ArgumentNullException>("host", () => host.RunWithCultureMiddlewareAsync());
    }

    private sealed class UnusedCultureList : ILocalizationDynamicList
    {
        public IEnumerable<CultureInfo> GetAvailableCultures() => throw new InvalidOperationException("Host validation must precede reading the list.");
    }
}
