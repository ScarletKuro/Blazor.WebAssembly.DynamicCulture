using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.Services;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void InvokeLanguageChanged_RaisesLanguageChangedWithNewCulture()
    {
        var service = new LocalizationService();
        CultureInfo? changedCulture = null;

        service.LanguageChanged += (_, culture) => changedCulture = culture;
        ((ILocalizationService)service).InvokeLanguageChanged(new CultureInfo("ja-JP"));

        Assert.Equal(new CultureInfo("ja-JP"), changedCulture);
    }
}
