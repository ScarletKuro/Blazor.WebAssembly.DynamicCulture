using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.Provider;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class LocalizationDynamicOptionsTests
{
    [Fact]
    public void Constructor_ConfiguresDefaultCultureProvidersInExpectedOrder()
    {
        var options = new LocalizationDynamicOptions();

        Assert.NotNull(options.CultureProviders);
        Assert.Collection(
            options.CultureProviders,
            provider => Assert.IsType<QueryStringCultureProvider>(provider),
            provider => Assert.IsType<LocalStorageCultureProvider>(provider),
            provider => Assert.IsType<AcceptLanguageHeaderCultureProvider>(provider));
    }

    [Fact]
    public void Constructor_AssignsOptionsToDefaultCultureProviders()
    {
        var options = new LocalizationDynamicOptions();

        Assert.NotNull(options.CultureProviders);
        foreach (var provider in options.CultureProviders.Cast<CultureProvider>())
        {
            Assert.Same(options, provider.Options);
        }
    }

    [Fact]
    public void AddSupportedCultures_ReplacesSupportedCultures()
    {
        var options = new LocalizationDynamicOptions();

        options.AddSupportedCultures("en-US", "fr-FR");

        Assert.NotNull(options.SupportedCultures);
        Assert.Equal(
            ["en-US", "fr-FR"],
            options.SupportedCultures.Select(culture => culture.Name));
    }

    [Fact]
    public void AddSupportedUICultures_ReplacesSupportedUICultures()
    {
        var options = new LocalizationDynamicOptions();

        options.AddSupportedUICultures("en-US", "ja-JP");

        Assert.NotNull(options.SupportedUICultures);
        Assert.Equal(
            ["en-US", "ja-JP"],
            options.SupportedUICultures.Select(culture => culture.Name));
    }

    [Fact]
    public void SetDefaultCulture_ConfiguresCultureAndUICulture()
    {
        var options = new LocalizationDynamicOptions();

        options.SetDefaultCulture("ru-RU");

        Assert.Equal(new CultureInfo("ru-RU"), options.DefaultCulture.Culture);
        Assert.Equal(new CultureInfo("ru-RU"), options.DefaultCulture.UICulture);
    }
}
