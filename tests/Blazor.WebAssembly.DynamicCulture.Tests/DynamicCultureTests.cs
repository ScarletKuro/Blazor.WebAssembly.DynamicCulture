using System.Globalization;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class DynamicCultureTests
{
    [Fact]
    public void SingleCulture_UsesSameInstanceForFormattingAndUI()
    {
        var culture = new CultureInfo("fr-FR");

        var result = new DynamicCulture(culture);

        Assert.Same(culture, result.Culture);
        Assert.Same(culture, result.UICulture);
    }

    [Fact]
    public void SeparateNames_KeepFormattingAndUICultureIndependent()
    {
        var result = new DynamicCulture("fr-FR", "ja-JP");

        Assert.Equal("fr-FR", result.Culture.Name);
        Assert.Equal("ja-JP", result.UICulture.Name);
    }

    [Theory]
    [InlineData(true, "culture")]
    [InlineData(false, "uiCulture")]
    public void NullCulture_IsRejectedWithParameterName(bool nullCulture, string parameterName)
    {
        var culture = CultureInfo.InvariantCulture;

        var exception = Assert.Throws<ArgumentNullException>(() => new DynamicCulture(
            nullCulture ? null! : culture,
            nullCulture ? culture : null!));

        Assert.Equal(parameterName, exception.ParamName);
    }
}
